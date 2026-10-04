using System.Collections.Generic;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using RoyalThrone.ReferenceGameplay;
using UnityEngine;

namespace RoyalThrone.Campaign
{
    /// <summary>
    /// Early-campaign obstacles on the existing Square board. Layouts and goal totals
    /// come from the inspected data; hit rules are the documented Royal Throne adaptation.
    /// Existing power-up effects still invoke Square.DestroyBlock at their normal impact.
    /// </summary>
    public static class RoyalCampaignBlockers
    {
        sealed class Obstacle
        {
            public string Goal;
            public string Art;
            public int Health;
            public int LastWave = -1;
            public bool Persistent;
            public readonly List<Square> Cells = new List<Square>();
            public SpriteRenderer Visual;
            public TextMesh Count;
        }

        sealed class CellState
        {
            public Obstacle Obstacle;
            public int Grass;
            public int GrassWave = -1;
            public SpriteRenderer GrassVisual;
            public GameObject Marker;
        }

        static readonly Dictionary<Square, CellState> states = new Dictionary<Square, CellState>();
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        static readonly HashSet<Item> collectedPresentation = new HashSet<Item>();
        static readonly HashSet<Item> collectedCoinPresentation = new HashSet<Item>();
        static readonly HashSet<string> reportedPresentationGaps = new HashSet<string>();
        static int wave;
        public static int CurrentWave => wave;
        public static void BeginMatchWave() { if (RoyalReferenceCampaign.Active) wave++; }

        public static Sprite Art(string name)
        {
            var imported = RoyalThrone.ReferenceGameplay.ReferenceGameplayCatalog.Art(name);
            if (imported) return imported;
            if (sprites.TryGetValue(name, out var cached) && cached) return cached;
            var sprite = Resources.Load<Sprite>("RoyalCampaignArt/" + name);
            if (!sprite)
            {
                var texture = Resources.Load<Texture2D>("RoyalCampaignArt/" + name);
                if (texture) sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100);
            }
            if (!sprite)
            {
                string fallback = name.StartsWith("box") ? "crate" : name == "grass" ? "grass-full" :
                    name == "cupboard" ? "chest" : name == "mailbox" ? "chest" :
                    name == "egg" ? "bubble" : name == "potion" ? "vase" : name;
                sprite = RoyalGameplayArt.Art(fallback) ?? RoyalPieceSkin.Art(fallback);
            }
            if (sprite) sprites[name] = sprite;
            return sprite;
        }

        public static Sprite GoalIcon(string goalName)
        {
            switch (goalName)
            {
                case "Box": return Art("box1");
                case "Grass": return Art("grass");
                case "Cupboard": return Art("cupboard");
                case "Mail": return Art("mailbox");
                case "Egg": return Art("egg");
                case "Potion": return Art("potion");
                case "Coin": return Art("coin");
                default: return null;
            }
        }

        public static void Build(LevelManager level)
        {
            states.Clear();
            collectedPresentation.Clear();
            collectedCoinPresentation.Clear();
            wave++;
            if (!RoyalReferenceCampaign.Active || !level || level.squaresArray == null) return;
            foreach (var square in level.squaresArray)
            {
                if (!square || square.IsNone()) continue;
                var state = new CellState();
                states[square] = state;
                int grassCode = RoyalReferenceCampaign.GrassAt(square);
                if (grassCode != 0)
                {
                    state.Grass = grassCode == 32 || grassCode == 2 ? 2 : 1;
                    state.GrassVisual = CreateVisual(square, "Campaign grass", Art("grass"), .98f, 1, "grass");
                    if (state.Grass > 1)
                    {
                        var layered = RoyalThrone.ReferenceGameplay.ReferenceGameplayCatalog.Art("grass2");
                        if (layered) state.GrassVisual.sprite = layered;
                        else if (!RoyalThrone.ReferenceGameplay.ReferenceGameplayCatalog.IsNative(state.GrassVisual.sprite)) state.GrassVisual.color = new Color(.74f, .92f, .60f);
                    }
                }
            }
            foreach (var square in level.squaresArray)
            {
                if (!square || !states.TryGetValue(square, out var state) || state.Obstacle != null) continue;
                int code = RoyalReferenceCampaign.CodeAt(square);
                if (code >= 21 && code <= 23)
                    AddObstacle(level, square, "Box", "box" + (code - 20), code - 20, 1, false);
                else if (code == 34)
                    AddObstacle(level, square, "Mail", "mailbox", 1, 1, true);
                else if (code == 41)
                    AddObstacle(level, square, "Cupboard", "cupboard", 10, 2, false);
                else if (code == 43)
                    AddObstacle(level, square, "Potion", "potion", 4, 2, false);
            }
            foreach (var pair in states) UpdateMarker(pair.Key, pair.Value);
        }

        static void AddObstacle(LevelManager level, Square anchor, string goal, string art, int health, int size, bool persistent)
        {
            var obstacle = new Obstacle { Goal = goal, Art = art, Health = health, Persistent = persistent };
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                var square = level.GetSquare(anchor.col + x, anchor.row + y);
                if (!square || !states.TryGetValue(square, out var state))
                    throw new System.InvalidOperationException("Campaign multi-cell obstacle has an invalid footprint.");
                if (state.Obstacle != null)
                    throw new System.InvalidOperationException("Campaign multi-cell obstacles overlap.");
                state.Obstacle = obstacle;
                obstacle.Cells.Add(square);
                square.type = SquareTypes.SOLIDBLOCK;
            }
            obstacle.Visual = CreateVisual(anchor, "Campaign " + goal, Art(art), size * .96f, 4, art);
            var offset = new Vector3(level.squareWidth * (size - 1) * .5f, -level.squareHeight * (size - 1) * .5f, 0);
            obstacle.Visual.transform.localPosition += anchor.transform.InverseTransformVector(level.GameField.TransformVector(offset));
            // A disabled proxy renderer means CreateVisual installed the native
            // composite. Its artwork has no generated numeric durability overlay.
            if (size > 1 && obstacle.Visual.enabled)
            {
                var counter = new GameObject("Remaining " + goal);
                counter.transform.SetParent(obstacle.Visual.transform, false);
                counter.transform.localPosition = new Vector3(0, -.32f, -.1f);
                obstacle.Count = counter.AddComponent<TextMesh>();
                obstacle.Count.fontSize = 40;
                obstacle.Count.characterSize = .08f;
                obstacle.Count.anchor = TextAnchor.MiddleCenter;
                obstacle.Count.alignment = TextAlignment.Center;
                obstacle.Count.color = Color.white;
                obstacle.Count.text = health.ToString();
                counter.GetComponent<MeshRenderer>().sortingOrder = 7;
            }
        }

        static SpriteRenderer CreateVisual(Square square, string name, Sprite sprite, float cellScale, int order, string nativeKey = null)
        {
            var root = new GameObject(name);
            root.transform.SetParent(square.transform, false);
            root.transform.localPosition = new Vector3(0, 0, -.08f);
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            Fit(renderer, cellScale);
            var nativePrefab = nativeKey == null ? null : Resources.Load<GameObject>("ReferenceGameplay/Blockers/" + nativeKey);
            if (nativePrefab)
            {
                var native = Object.Instantiate(nativePrefab, root.transform, false);
                renderer.enabled = false;
                root.transform.localScale = Vector3.one * RoyalThrone.ReferenceGameplay.ReferenceGameplayCatalog.CellScale(LevelManager.THIS, root.transform.parent);
                var group = native.GetComponent<UnityEngine.Rendering.SortingGroup>();
                if (group) { group.sortingLayerID = renderer.sortingLayerID; group.sortingOrder = order; }
            }
            return renderer;
        }

        static void Fit(SpriteRenderer renderer, float cellScale)
        {
            if (!renderer || !renderer.sprite || !LevelManager.THIS) return;
            if (RoyalThrone.ReferenceGameplay.ReferenceGameplayCatalog.IsNative(renderer.sprite))
            {
                renderer.transform.localScale = Vector3.one * RoyalThrone.ReferenceGameplay.ReferenceGameplayCatalog.CellScale(LevelManager.THIS, renderer.transform.parent);
                return;
            }
            var size = renderer.sprite.bounds.size;
            var level = LevelManager.THIS;
            var fieldScale = level.GameField ? level.GameField.lossyScale : Vector3.one;
            var parentScale = renderer.transform.parent.lossyScale;
            float fit = Mathf.Min(level.squareWidth * Mathf.Abs(fieldScale.x) / Mathf.Max(.01f, size.x * Mathf.Abs(parentScale.x)),
                level.squareHeight * Mathf.Abs(fieldScale.y) / Mathf.Max(.01f, size.y * Mathf.Abs(parentScale.y))) * cellScale;
            renderer.transform.localScale = Vector3.one * fit;
        }

        static void UpdateMarker(Square square, CellState state)
        {
            bool needed = state.Obstacle != null || state.Grass > 0;
            if (needed && !state.Marker)
            {
                state.Marker = new GameObject("Campaign impact receiver");
                state.Marker.transform.SetParent(square.transform, false);
                square.block.Add(state.Marker);
            }
            else if (!needed && state.Marker)
            {
                square.block.Remove(state.Marker);
                Object.Destroy(state.Marker);
                state.Marker = null;
            }
            square.type = state.Obstacle != null ? SquareTypes.SOLIDBLOCK : SquareTypes.EMPTY;
        }

        /// <summary>Returns true only for a board owned by the scoped campaign loader.</summary>
        public static bool HandleHit(Square square, bool rocketClear, int hitWave = 0, bool matchedItemClear = false)
        {
            if (!RoyalReferenceCampaign.Active || !square || !states.TryGetValue(square, out var state)) return false;
            int token = hitWave != 0 ? hitWave : wave;
            if (state.Grass > 0 && state.GrassWave != token)
            {
                state.GrassWave = token;
                state.Grass--;
                NativeGrassHit(square, state.Grass == 0);
                if (state.Grass == 0)
                {
                    RoyalReferenceCampaign.CollectGoal("Grass");
                    if (state.GrassVisual) Object.Destroy(state.GrassVisual.gameObject);
                    UpdateMarker(square, state);
                }
                else if (state.GrassVisual) state.GrassVisual.color = Color.white;
            }
            HitObstacle(state.Obstacle, token);
            // Only a cleared item chips neighboring obstacles. A direct hit on a solid
            // footprint must not recursively damage other solids or every adjacent egg.
            if (matchedItemClear)
            {
                foreach (var neighbor in square.GetAllNeghbors())
                {
                    if (!neighbor) continue;
                    if (states.TryGetValue(neighbor, out var adjacent)) HitObstacle(adjacent.Obstacle, token);
                    if (neighbor.item)
                    {
                        var special = neighbor.item.GetComponent<RoyalCampaignItem>();
                        if (special) special.Hit(token);
                    }
                }
            }
            return true;
        }

        static void HitObstacle(Obstacle obstacle, int token)
        {
            if (obstacle == null || obstacle.LastWave == token) return;
            obstacle.LastWave = token;
            if (!NativeObstacleHit(obstacle) && obstacle.Visual) RoyalPowerupDirector.Get?.Chip(obstacle.Visual.transform.position);
            if (obstacle.Persistent)
            {
                RoyalReferenceCampaign.CollectGoal(obstacle.Goal);
                return;
            }
            obstacle.Health--;
            if (obstacle.Goal == "Cupboard" || obstacle.Goal == "Potion")
                RoyalReferenceCampaign.CollectGoal(obstacle.Goal);
            if (obstacle.Health > 0)
            {
                if (obstacle.Goal == "Box" && obstacle.Visual)
                {
                    obstacle.Visual.sprite = Art("box" + obstacle.Health);
                    Fit(obstacle.Visual, .96f);
                }
                if (obstacle.Count) obstacle.Count.text = obstacle.Health.ToString();
                return;
            }
            if (obstacle.Goal == "Box") RoyalReferenceCampaign.CollectGoal("Box");
            foreach (var square in obstacle.Cells)
            {
                if (!square || !states.TryGetValue(square, out var state)) continue;
                state.Obstacle = null;
                UpdateMarker(square, state);
            }
            if (obstacle.Visual) Object.Destroy(obstacle.Visual.gameObject);
        }

        public static bool ConfigureSpawn(Item item, int tiledCode)
        {
            if (!RoyalReferenceCampaign.Active || !item || (tiledCode != 26 && tiledCode != 60)) return false;
            var special = item.GetComponent<RoyalCampaignItem>() ?? item.gameObject.AddComponent<RoyalCampaignItem>();
            special.Configure(tiledCode);
            return true;
        }

        // TryGetComponent: matching asks this of every piece, and a GetComponent that finds nothing allocates in the editor.
        public static bool IsSpecialItem(Item item) => item && item.TryGetComponent(out RoyalCampaignItem _);
        public static bool HasCoinPresentation(Item item) => item && collectedCoinPresentation.Contains(item);
        public static void OnItemDestroying(Item item)
        {
            if (!RoyalReferenceCampaign.Active || !item) return;
            var special = item.GetComponent<RoyalCampaignItem>();
            if (special)
            {
                special.Collect();
                if (ReferenceGameplayCatalog.Available && collectedPresentation.Add(item))
                {
                    var sprite = special.Visual;
                    if (sprite && sprite == Art("egg"))
                    {
                        NativeEffect("EggItemExplodeParticles", item.transform.position);
                        ReferenceGameplayAudio.Play("egg_break1__15847");
                    }
                    else if (sprite && sprite == Art("coin"))
                    {
                        var effect = NativeEffect("CoinItemExplodeParticles", item.transform.position);
                        if (effect != null && effect.Valid) collectedCoinPresentation.Add(item);
                        ReferenceGameplayAudio.Play("coin_collect__15714");
                    }
                }
            }
        }

        // These are presentation hooks at the existing accepted hit/clear events.
        // They do not change damage waves, durability, goal credits or item ownership.
        static void NativeGrassHit(Square square, bool cleared)
        {
            if (!ReferenceGameplayCatalog.Available) return;
            NativeEffect(cleared ? "GrassItemExplodeParticles" : "GrassItemTapParticles", square.transform.position);
            if (cleared) ReferenceGameplayAudio.Play("grass_clear__15953");
        }
        static bool NativeObstacleHit(Obstacle obstacle)
        {
            if (!ReferenceGameplayCatalog.Available) return false;
            Vector3 position = obstacle.Visual ? obstacle.Visual.transform.position : obstacle.Cells[0].transform.position;
            bool cleared = !obstacle.Persistent && obstacle.Health <= 1;
            switch (obstacle.Goal)
            {
                case "Box":
                    var box = NativeEffect("BoxItemExplodeParticles", position);
                    if (box != null && box.Valid)
                        for (int layer = 1; layer <= 4; layer++) SelectEffectGroup(box, "layer_" + layer, layer == obstacle.Health);
                    ReferenceGameplayAudio.Play(cleared ? "box_break1__15936" : "box_layer_break1__15866");
                    break;
                case "Cupboard":
                    NativeEffect(cleared ? "CupboardItemExplodeParticles" : "CupboardItemSingleExplodeParticles", position);
                    ReferenceGameplayAudio.Play(cleared ? "cupboard_container_break__15639" : "cupboard_plate_1__15752");
                    if (!cleared) ReportGap("Cupboard plate feedback uses the existing accepted hit event. Door mesh/shader feedback and native door-versus-plate stage selection remain incomplete.");
                    break;
                case "Potion":
                    var potion = NativeEffect(cleared ? "PotionItemExplodeParticles" : "PotionItemSingleExplodeParticles", position);
                    if (!cleared && potion != null && potion.Valid)
                    {
                        // Authored potion_colors are empty in these levels; native runtime
                        // bottle colour selection is not recovered. Keep the native common
                        // glass/cork layer, but do not fire every mutually exclusive colour.
                        foreach (var color in new[] { "Blue", "Green", "Red", "Yellow", "Pink" }) SelectEffectGroup(potion, color, false);
                        ReportGap("Potion hit uses recovered common fragments only. Bottle-colour selection and its coloured splash are incomplete.");
                    }
                    ReferenceGameplayAudio.Play(cleared ? "potion_container_break__15636" : "potion_bottle_break__16031");
                    break;
                case "Mail":
                    ReferenceGameplayAudio.Play("mailbox_close__15545");
                    ReportGap("Mailbox close SFX is connected; mailbox lid/mail animation and particles are not recovered.");
                    break;
            }
            return true;
        }
        static ReferenceVfxPool.Handle NativeEffect(string name, Vector3 position)
        {
            var effect = ReferenceVfxPool.Play(name, position);
            if (effect == null && !ReferenceVfxPool.Has(name)) ReportGap("Missing native blocker effect: " + name);
            return effect;
        }
        static void SelectEffectGroup(ReferenceVfxPool.Handle effect, string name, bool active)
        {
            var group = effect.GameObject.transform.Find(name);
            if (!group) return;
            group.gameObject.SetActive(active);
            foreach (var ps in group.GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                if (active && ps.gameObject.activeInHierarchy) ps.Play(false);
            }
        }
        static void ReportGap(string message)
        {
            if (reportedPresentationGaps.Add(message)) Debug.LogWarning("[Reference Gameplay incomplete] " + message);
        }

        // Read-only checks allow editor QA to validate footprint and layer behavior.
        public static string GoalAt(Square square) => square && states.TryGetValue(square, out var state) ? state.Obstacle?.Goal : null;
        public static int HealthAt(Square square) => square && states.TryGetValue(square, out var state) ? state.Obstacle?.Health ?? 0 : 0;
        public static int GrassAt(Square square) => square && states.TryGetValue(square, out var state) ? state.Grass : 0;
        public static bool SharesObstacle(Square first, Square second) => first && second && states.TryGetValue(first, out var a) && states.TryGetValue(second, out var b) && a.Obstacle != null && a.Obstacle == b.Obstacle;
    }
}

// The screen between Play and the first move: what the level hands you.
//
// The kit's own PrePlay is a striped card in the middle of the board carrying
// the level's target — a star and a number, or a block, or two ingredients. Two
// things are wrong with it here. It is the last kit panel on the level screen,
// in the kit's own visual language next to a royal HUD; and it restates the
// goal the level popup stated one screen earlier, which is the one thing on it
// the player has already read.
//
// So this says the other thing instead: the POWER. One full-screen plate per
// power the level is about to put in the player's hands, drawn in the shape
// every match-3 teaches a power in — the recipe, an arrow, the result:
//
//     ┌─────────────────────────────┐
//     │          Rocket             │   the power's name
//     │                             │
//     │   ▣                         │
//     │   ▣                         │
//     │   ▣  ──▶   ✷ ROCKET ✷       │   four in a line makes one
//     │   ▣                         │
//     │                             │
//     │      Match 4 in a line      │   how it is made
//     └─────────────────────────────┘
//
// Every gem, rocket, bomb and rainbow ball on it is the board's OWN sprite,
// read off the kit's Prefabs/Item at runtime: items[] for the six gems,
// stripedItems[] for the rockets, packageItems[] for the bombs, bombItems[0]
// for the rainbow ball. Nothing here is new art, so a plate cannot show the
// player something the board will not.
//
// Which plate shows, in order:
//
//   - one per pre-level booster chosen on the popup, because a chosen booster
//     is already sitting on the board when it opens and the player has no other
//     way of being told so;
//   - failing that, the recipe for a power the player has just become able to
//     make: the rocket on level 2, the rainbow ball on level 3;
//   - failing that, nothing at all. A level with no booster and no new power
//     opens straight onto the board, which is what a player on level 400 wants.
//
// The kit's PrePlay object is deactivated, never deleted. StoryPrePlay.Enabled
// = false gives it straight back.
//
// Nothing here blocks input, exactly as the kit's card did not: every graphic
// is raycastTarget false and the board underneath stays live. A plate is a
// caption on the first second of a level, not a modal.

using System.Collections;
using System.Collections.Generic;
using JellyGarden.Scripts.GUI;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed class StoryPrePlay : MonoBehaviour
    {
        /// <summary>The whole feature, in one switch.</summary>
        public static bool Enabled = true;

        public const string HostName = "StoryPrePlay";

        /// <summary>The name the kit gives the card this one stands in for.</summary>
        private const string KitObjectName = "PrePlay";

        private static StoryPrePlay _instance;

        public static bool IsOpen
        {
            get { return _instance != null; }
        }

        #region Timing

        // A plate is read in one glance, so it is on screen for about as long as
        // a glance takes. 1.4s of hold either side of a fifth of a second in and
        // out — the beat the rest of the story flourishes use — and a second
        // plate follows the first rather than crowding it.
        private const float FadeIn = 0.18f;
        private const float Hold = 1.4f;
        private const float FadeOut = 0.22f;

        /// <summary>Between two plates, so the second reads as a second thing.</summary>
        private const float BetweenPlates = 0.12f;

        #endregion

        #region Metrics, in the 1080x1920 reference canvas

        private const float TitleTop = 0.80f;
        private const float TitleHeight = 0.10f;
        private const int TitleSize = 116;

        private const float FootBottom = 0.13f;
        private const float FootHeight = 0.07f;
        private const int FootSize = 60;

        /// <summary>One cell of the recipe strip, and the gap between two.</summary>
        private const float TileSize = 132f;
        private const float TileGap = 12f;
        private const float StripPad = 18f;

        private const float ArrowWidth = 150f;
        private const float ArrowHeight = 104f;

        /// <summary>The starburst behind the result, and the result inside it.</summary>
        private const float BurstSize = 430f;
        private const float ResultSize = 250f;

        /// <summary>Between the strip, the arrow and the burst.</summary>
        private const float StageGap = 26f;

        /// <summary>How far the plate's own shadow sits below its art.</summary>
        private const float ShadowStep = 7f;

        #endregion

        #region The teaching levels

        /// <summary>
        /// The level a recipe is shown on, when no booster was chosen.
        ///
        /// Level 1 teaches nothing: it is four moves long, its goal is a score,
        /// and a player who has just watched the opening does not need a second
        /// screen before their first swap. The rocket comes on 2 and the rainbow
        /// ball on 3, which is the first pair of levels long enough to make one.
        /// </summary>
        private const int RocketLevel = 2;
        private const int RainbowLevel = 3;

        #endregion

        #region What a plate is

        /// <summary>One screen: a recipe, an arrow and what it makes.</summary>
        private struct Plate
        {
            /// <summary>The power's name, over the top.</summary>
            public string Title;

            /// <summary>How it is made, along the bottom.</summary>
            public string Foot;

            /// <summary>What goes in the strip on the left.</summary>
            public Sprite Recipe;

            /// <summary>How many of it. One means a single tile, not a strip.</summary>
            public int RecipeCount;

            /// <summary>What comes out, inside the starburst.</summary>
            public Sprite Result;
        }

        #endregion

        #region The board's own sprites

        private static Item _itemPrefab;

        /// <summary>
        /// The kit's item prefab, which is where every gem and power sprite on
        /// the board actually lives.
        ///
        /// Textures/Items is not under a Resources folder, so none of it can be
        /// loaded by path. The prefab can — Resources/Prefabs/Item — and it
        /// carries the whole set in its own serialized arrays. This is the same
        /// route StoryLevelHud takes for the block chip's backing.
        /// </summary>
        private static Item ItemPrefab
        {
            get
            {
                if (_itemPrefab != null) return _itemPrefab;
                var prefab = Resources.Load<GameObject>("Prefabs/Item");
                if (prefab == null) return null;
                _itemPrefab = prefab.GetComponent<Item>();
                return _itemPrefab;
            }
        }

        /// <summary>One of the six board gems, by colour index.</summary>
        private static Sprite Gem(int color)
        {
            Item item = ItemPrefab;
            if (item == null || item.items == null || item.items.Length == 0) return null;
            return item.items[Mathf.Clamp(color, 0, item.items.Length - 1)];
        }

        /// <summary>The horizontal rocket of a colour. Horizontal reads as an
        /// arrow across the plate; the vertical one is the same power.</summary>
        private static Sprite Rocket(int color)
        {
            Item item = ItemPrefab;
            if (item == null || item.stripedItems == null || item.stripedItems.Count == 0) return null;
            StripedItem striped = item.stripedItems[Mathf.Clamp(color, 0, item.stripedItems.Count - 1)];
            return striped != null ? striped.horizontal : null;
        }

        /// <summary>The bomb of a colour — the kit calls it a package.</summary>
        private static Sprite Bomb(int color)
        {
            Item item = ItemPrefab;
            if (item == null || item.packageItems == null || item.packageItems.Length == 0) return null;
            return item.packageItems[Mathf.Clamp(color, 0, item.packageItems.Length - 1)];
        }

        /// <summary>The rainbow ball. There is only ever one.</summary>
        private static Sprite Rainbow()
        {
            Item item = ItemPrefab;
            if (item == null || item.bombItems == null || item.bombItems.Length == 0) return null;
            return item.bombItems[0];
        }

        #endregion

        #region Opening

        /// <summary>
        /// Stands in for MenuManager.ShowPrePlay. Returns whether it took the
        /// screen over, so the one call site can fall back to the kit's card
        /// when this is switched off.
        /// </summary>
        public static bool Open()
        {
            if (!Enabled) return false;

            HideKitCard();
            Close();

            // The host is made even when there is no plate to show, because it
            // is what carries the board on to WaitForPopup. See StartTheBoard.
            var go = new GameObject(HostName);
            _instance = go.AddComponent<StoryPrePlay>();
            _instance.Run(PlatesForThisLevel());
            return true;
        }

        /// <summary>
        /// Puts the board on, which the kit's card used to do as a side effect.
        ///
        /// The kit's PrePlay object carries an Animation whose event calls
        /// AnimationManager with the name "PrePlay", and THAT is what sets
        /// gameStatus = WaitForPopup — the state whose setter runs InitLevel and
        /// raises OnLevelLoaded, which is what actually builds the board. Switch
        /// the card off and the level opens on an empty room: HUD, backdrop,
        /// booster tray, no gems. So whatever stands in for the card has to
        /// stand in for that too.
        ///
        /// A frame later, not here: Open runs inside the gameStatus setter's own
        /// PrepareGame call, and setting the state again from inside it would
        /// build the board before the rest of PrepareGame had finished with it.
        /// The kit's event fired well after PrepareGame returned; one frame is
        /// the same side of that line.
        /// </summary>
        private static void StartTheBoard()
        {
            LevelManager level = LevelManager.THIS;
            if (level == null) return;
            if (level.gameStatus != GameState.PrepareGame) return;
            level.gameStatus = GameState.WaitForPopup;
        }

        /// <summary>Takes the plate off screen, whatever it was doing.</summary>
        public static void Close()
        {
            if (_instance == null) return;
            StoryPrePlay instance = _instance;
            _instance = null;
            if (instance != null && instance.gameObject != null)
                Destroy(instance.gameObject);
        }

        /// <summary>
        /// Switches the kit's own card off. By name and not by MenuManager's
        /// field, because the field is private and the object is a child of the
        /// level canvas that survives between levels.
        /// </summary>
        public static void HideKitCard()
        {
            MenuManager menu = MenuManager.Instance;
            if (menu == null) return;

            Transform found = FindDeep(menu.transform, KitObjectName);
            if (found == null)
            {
                // The card is not always under the MenuManager — look through
                // every root of the scene before giving up.
                GameObject[] roots = UnityEngine.SceneManagement.SceneManager
                    .GetActiveScene().GetRootGameObjects();
                for (int i = 0; i < roots.Length && found == null; i++)
                    found = FindDeep(roots[i].transform, KitObjectName);
            }

            if (found != null && found.gameObject.activeSelf)
                found.gameObject.SetActive(false);
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            if (parent == null) return null;
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindDeep(parent.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        #endregion

        #region Choosing the plates

        /// <summary>
        /// What this level has to say, in the order it says it. See the note at
        /// the top of the file for why the order is boosters, then a recipe,
        /// then nothing.
        /// </summary>
        private static List<Plate> PlatesForThisLevel()
        {
            var plates = new List<Plate>();

            foreach (BoostType boost in BoostersOnThisBoard())
            {
                Plate plate;
                if (BoosterPlate(boost, out plate))
                    plates.Add(plate);
            }
            if (plates.Count > 0) return plates;

            int level = LevelManager.THIS != null ? LevelManager.THIS.currentLevel : 0;

            if (level == RocketLevel)
            {
                // The red book, because it is items[0] and so the one colour
                // every board has whatever its COLOR LIMIT is.
                plates.Add(new Plate
                {
                    Title = "Rocket",
                    Foot = "Match 4 in a line",
                    Recipe = Gem(0),
                    RecipeCount = 4,
                    Result = Rocket(0)
                });
            }
            else if (level == RainbowLevel)
            {
                plates.Add(new Plate
                {
                    Title = "Rainbow Ball",
                    Foot = "Match 5 in a line",
                    Recipe = Gem(3),
                    RecipeCount = 5,
                    Result = Rainbow()
                });
            }

            return plates;
        }

        /// <summary>
        /// The pre-level boosters that are about to land on this board.
        ///
        /// Two sources, because the selection is handed to the board and cleared
        /// somewhere between the popup and here depending on how the level was
        /// started: the counters on the LevelManager are the board's own copy,
        /// and StoryBoosters.Selection is the popup's. Whichever still holds it.
        /// </summary>
        private static List<BoostType> BoostersOnThisBoard()
        {
            var chosen = new List<BoostType>();
            LevelManager level = LevelManager.THIS;

            if (level != null)
            {
                if (level.BoostStriped > 0) chosen.Add(BoostType.Stripes);
                if (level.BoostPackage > 0) chosen.Add(BoostType.Packages);
                if (level.BoostColorfullBomb > 0) chosen.Add(BoostType.Colorful_bomb);
            }
            if (chosen.Count > 0) return chosen;

            List<BoostType> selection = StoryBoosters.Selection();
            for (int i = 0; i < selection.Count; i++)
                chosen.Add(selection[i]);
            return chosen;
        }

        /// <summary>
        /// A booster's plate: its tile on the left, the power it plants on the
        /// right. NOT a recipe — the three pre-level boosters are spent by
        /// SetPreBoosts before the first move, so there is nothing to match and
        /// nothing to tap. The plate says what is already waiting.
        /// </summary>
        private static bool BoosterPlate(BoostType boost, out Plate plate)
        {
            plate = new Plate { RecipeCount = 1, Foot = "Already on your board" };
            plate.Recipe = StoryBoosters.IconOf(boost);

            if (boost == BoostType.Stripes)
            {
                plate.Title = "Rocket";
                plate.Result = Rocket(0);
            }
            else if (boost == BoostType.Packages)
            {
                plate.Title = "Bomb";
                plate.Result = Bomb(0);
            }
            else if (boost == BoostType.Colorful_bomb)
            {
                plate.Title = "Rainbow Ball";
                plate.Result = Rainbow();
            }
            else
            {
                return false;
            }

            return plate.Result != null;
        }

        #endregion

        #region Drawing one plate

        private Canvas _canvas;
        private CanvasGroup _group;

        private void Run(List<Plate> plates)
        {
            _canvas = StoryUi.CreateOverlayCanvas("PrePlayCanvas", 400);
            _canvas.transform.SetParent(transform, false);

            _group = _canvas.gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;

            StartCoroutine(PlayAll(plates));
        }

        private IEnumerator PlayAll(List<Plate> plates)
        {
            // One frame for PrepareGame to finish, then the board.
            yield return null;
            StartTheBoard();

            for (int i = 0; i < plates.Count; i++)
            {
                GameObject art = Build(plates[i]);

                yield return Fade(0f, 1f, FadeIn);
                yield return new WaitForSeconds(Hold);
                yield return Fade(1f, 0f, FadeOut);

                Destroy(art);
                if (i < plates.Count - 1)
                    yield return new WaitForSeconds(BetweenPlates);
            }

            Close();
        }

        private IEnumerator Fade(float from, float to, float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                if (_group != null)
                    _group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / seconds));
                yield return null;
            }
            if (_group != null) _group.alpha = to;
        }

        /// <summary>One plate's whole hierarchy, returned so it can be dropped.</summary>
        private GameObject Build(Plate plate)
        {
            RectTransform root = StoryUi.CreateRect("Plate", _canvas.transform);
            StoryUi.Stretch(root);

            Ground(root);
            Title(root, plate);
            Stage(root, plate);
            Foot(root, plate);

            return root.gameObject;
        }

        /// <summary>
        /// The blue the plate stands on: the panel's own dark blue with a wide
        /// soft disc of the lighter one over it. Two flat fills rather than a
        /// gradient texture, which is all the reference is doing.
        /// </summary>
        private void Ground(RectTransform parent)
        {
            Image ground = StoryUi.CreateImage("Ground", parent, null, StoryUi.RoyalBlueDark);
            StoryUi.Stretch(ground.rectTransform);

            Image glow = StoryUi.CreateImage("Glow", parent, StoryUi.CircleSprite,
                new Color(StoryUi.RoyalBlue.r, StoryUi.RoyalBlue.g, StoryUi.RoyalBlue.b, 0.92f));
            RectTransform glowRect = glow.rectTransform;
            glowRect.anchorMin = new Vector2(0.5f, 0.5f);
            glowRect.anchorMax = glowRect.anchorMin;
            glowRect.pivot = new Vector2(0.5f, 0.5f);
            glowRect.sizeDelta = new Vector2(1500f, 1500f);
            glowRect.anchoredPosition = new Vector2(0f, 60f);
        }

        private void Title(RectTransform parent, Plate plate)
        {
            Text title = StoryUi.CreateText("Title", parent, plate.Title, TitleSize,
                StoryUi.RoyalTitle, TextAnchor.MiddleCenter);
            title.font = StoryUi.DisplayFont;
            title.fontStyle = FontStyle.Bold;
            Band(title.rectTransform, TitleTop, TitleHeight);

            var outline = title.gameObject.AddComponent<Outline>();
            outline.effectColor = StoryUi.RoyalBlueDark;
            outline.effectDistance = new Vector2(4f, -4f);

            StoryUi.FitOneLine(title, 60, TitleSize);
        }

        private void Foot(RectTransform parent, Plate plate)
        {
            Text foot = StoryUi.CreateText("Foot", parent, plate.Foot, FootSize,
                StoryUi.RoyalGold, TextAnchor.MiddleCenter);
            foot.font = StoryUi.DisplayFont;
            foot.fontStyle = FontStyle.Bold;
            Band(foot.rectTransform, FootBottom, FootHeight);

            var outline = foot.gameObject.AddComponent<Outline>();
            outline.effectColor = StoryUi.RoyalBlueDark;
            outline.effectDistance = new Vector2(3f, -3f);

            StoryUi.FitOneLine(foot, 34, FootSize);
        }

        /// <summary>A full-width band at a fraction of the screen's height.</summary>
        private static void Band(RectTransform rect, float bottom, float height)
        {
            rect.anchorMin = new Vector2(0f, bottom);
            rect.anchorMax = new Vector2(1f, bottom + height);
            rect.offsetMin = new Vector2(60f, 0f);
            rect.offsetMax = new Vector2(-60f, 0f);
        }

        /// <summary>The recipe, the arrow and the result, laid out left to right
        /// about the middle of the screen.</summary>
        private void Stage(RectTransform parent, Plate plate)
        {
            RectTransform stage = StoryUi.CreateRect("Stage", parent);
            stage.anchorMin = new Vector2(0.5f, 0.5f);
            stage.anchorMax = stage.anchorMin;
            stage.pivot = new Vector2(0.5f, 0.5f);
            stage.anchoredPosition = new Vector2(0f, 40f);

            float stripWidth = TileSize + StripPad * 2f;
            float total = stripWidth + StageGap + ArrowWidth + StageGap + BurstSize;
            stage.sizeDelta = new Vector2(total, BurstSize);

            float x = -total * 0.5f;

            Strip(stage, plate, x + stripWidth * 0.5f);
            x += stripWidth + StageGap;

            Arrow(stage, x + ArrowWidth * 0.5f);
            x += ArrowWidth + StageGap;

            Result(stage, plate, x + BurstSize * 0.5f);
        }

        /// <summary>The cream strip of tiles the recipe sits in.</summary>
        private void Strip(RectTransform parent, Plate plate, float centreX)
        {
            int count = Mathf.Max(1, plate.RecipeCount);
            float height = count * TileSize + (count - 1) * TileGap + StripPad * 2f;

            Image strip = StoryUi.CreateImage("Strip", parent,
                StoryUi.RoundedSprite(28), StoryUi.RoyalCream);
            strip.type = Image.Type.Sliced;
            RectTransform stripRect = strip.rectTransform;
            stripRect.anchorMin = new Vector2(0.5f, 0.5f);
            stripRect.anchorMax = stripRect.anchorMin;
            stripRect.pivot = new Vector2(0.5f, 0.5f);
            stripRect.sizeDelta = new Vector2(TileSize + StripPad * 2f, height);
            stripRect.anchoredPosition = new Vector2(centreX, 0f);

            float top = height * 0.5f - StripPad - TileSize * 0.5f;
            for (int i = 0; i < count; i++)
            {
                float y = top - i * (TileSize + TileGap);
                Tile(stripRect, plate.Recipe, y);
            }
        }

        /// <summary>One cell of the strip: the board's ground colour, then a gem.</summary>
        private void Tile(RectTransform parent, Sprite sprite, float y)
        {
            Image tile = StoryUi.CreateImage("Tile", parent,
                StoryUi.RoundedSprite(20), StoryUi.RoyalBlueBright);
            tile.type = Image.Type.Sliced;
            RectTransform rect = tile.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(TileSize, TileSize);
            rect.anchoredPosition = new Vector2(0f, y);

            if (sprite == null) return;

            Image art = StoryUi.CreateImage("Art", rect, sprite, Color.white);
            art.preserveAspect = true;
            StoryUi.Stretch(art.rectTransform, TileSize * 0.10f);
        }

        /// <summary>The arrow: its own cream silhouette, with the pale blue face
        /// inside it. One sprite drawn twice, which is how every rim on the
        /// royal panels is made.</summary>
        private void Arrow(RectTransform parent, float centreX)
        {
            Image rim = StoryUi.CreateImage("ArrowRim", parent, ArrowSprite(), StoryUi.RoyalCream);
            rim.preserveAspect = true;
            RectTransform rimRect = rim.rectTransform;
            rimRect.anchorMin = new Vector2(0.5f, 0.5f);
            rimRect.anchorMax = rimRect.anchorMin;
            rimRect.pivot = new Vector2(0.5f, 0.5f);
            rimRect.sizeDelta = new Vector2(ArrowWidth, ArrowHeight);
            rimRect.anchoredPosition = new Vector2(centreX, 0f);

            Image face = StoryUi.CreateImage("ArrowFace", rimRect, ArrowSprite(),
                new Color(0.74f, 0.85f, 0.95f, 1f));
            face.preserveAspect = true;
            StoryUi.Stretch(face.rectTransform, ArrowHeight * 0.13f);
        }

        /// <summary>The result, inside its starburst, over its own shadow.</summary>
        private void Result(RectTransform parent, Plate plate, float centreX)
        {
            RectTransform holder = StoryUi.CreateRect("Result", parent);
            holder.anchorMin = new Vector2(0.5f, 0.5f);
            holder.anchorMax = holder.anchorMin;
            holder.pivot = new Vector2(0.5f, 0.5f);
            holder.sizeDelta = new Vector2(BurstSize, BurstSize);
            holder.anchoredPosition = new Vector2(centreX, 0f);

            Image shadow = StoryUi.CreateImage("BurstShadow", holder, BurstSprite(),
                new Color(0f, 0f, 0f, 0.22f));
            shadow.preserveAspect = true;
            StoryUi.Stretch(shadow.rectTransform);
            shadow.rectTransform.anchoredPosition = new Vector2(ShadowStep, -ShadowStep);

            Image burst = StoryUi.CreateImage("Burst", holder, BurstSprite(), StoryUi.RoyalCream);
            burst.preserveAspect = true;
            StoryUi.Stretch(burst.rectTransform);

            if (plate.Result == null) return;

            Image art = StoryUi.CreateImage("Art", holder, plate.Result, Color.white);
            art.preserveAspect = true;
            RectTransform artRect = art.rectTransform;
            artRect.anchorMin = new Vector2(0.5f, 0.5f);
            artRect.anchorMax = artRect.anchorMin;
            artRect.pivot = new Vector2(0.5f, 0.5f);
            artRect.sizeDelta = new Vector2(ResultSize, ResultSize);
            artRect.anchoredPosition = Vector2.zero;
        }

        #endregion

        #region The two shapes nothing in the project already has

        private static Sprite _arrowSprite;
        private static Sprite _burstSprite;

        /// <summary>
        /// A right-pointing arrow, built rather than loaded.
        ///
        /// The kit has no arrow sprite that is not part of a button, and
        /// Story/UI's next.png is a painted purple button with a word on it.
        /// This is the same trick StoryBoardTheme uses for the cell tile: draw
        /// the shape's coverage into a texture, keep the colour on the Image, so
        /// one sprite serves the rim and the face at two sizes and two colours.
        /// </summary>
        private static Sprite ArrowSprite()
        {
            if (_arrowSprite != null) return _arrowSprite;

            const int w = 192;
            const int h = 128;
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Four samples a pixel: the head's slope is diagonal and a
                    // hard test leaves it a staircase.
                    int hits = 0;
                    for (int sy = 0; sy < 2; sy++)
                    {
                        for (int sx = 0; sx < 2; sx++)
                        {
                            float u = (x + 0.25f + sx * 0.5f) / w;
                            float v = (y + 0.25f + sy * 0.5f) / h;
                            if (InArrow(u, v)) hits++;
                        }
                    }
                    pixels[y * w + x] = new Color32(255, 255, 255, (byte)(hits * 255 / 4));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            _arrowSprite = Sprite.Create(texture, new Rect(0f, 0f, w, h),
                new Vector2(0.5f, 0.5f), 100f);
            _arrowSprite.name = "StoryPrePlayArrow";
            _arrowSprite.hideFlags = HideFlags.HideAndDontSave;
            return _arrowSprite;
        }

        /// <summary>The arrow in its own 0..1 space: a shaft, then a head.</summary>
        private static bool InArrow(float u, float v)
        {
            const float shaftEnd = 0.55f;   // where the head starts
            const float shaftHalf = 0.20f;  // half the shaft's thickness

            float fromMiddle = Mathf.Abs(v - 0.5f);

            if (u <= shaftEnd)
                return fromMiddle <= shaftHalf;

            // The head is a triangle from full height at shaftEnd to a point at
            // the right edge.
            float along = (u - shaftEnd) / (1f - shaftEnd);
            return fromMiddle <= 0.5f * (1f - along);
        }

        /// <summary>
        /// The cartoon starburst behind a result: a soft-spiked star, drawn the
        /// same way. Twelve spikes, and the radius never drops below two thirds,
        /// so it reads as a burst rather than as a gear.
        /// </summary>
        private static Sprite BurstSprite()
        {
            if (_burstSprite != null) return _burstSprite;

            const int n = 256;
            const int spikes = 12;
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[n * n];
            float half = n * 0.5f;

            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = x + 0.5f - half;
                    float dy = y + 0.5f - half;
                    float radius = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);

                    // The spiked edge, in texels from the middle.
                    float edge = half * (0.68f + 0.30f * Mathf.Cos(spikes * angle));

                    // One texel of falloff across it.
                    float coverage = Mathf.Clamp01(edge - radius + 0.5f);
                    pixels[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(coverage * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            _burstSprite = Sprite.Create(texture, new Rect(0f, 0f, n, n),
                new Vector2(0.5f, 0.5f), 100f);
            _burstSprite.name = "StoryPrePlayBurst";
            _burstSprite.hideFlags = HideFlags.HideAndDontSave;
            return _burstSprite;
        }

        #endregion

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}

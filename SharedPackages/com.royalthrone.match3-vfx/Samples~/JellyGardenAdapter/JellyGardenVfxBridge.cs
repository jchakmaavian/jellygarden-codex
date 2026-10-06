using System.Collections.Generic;
using JellyGarden.Scripts;
using UnityEngine;

namespace RoyalThrone.Match3Vfx.Adapters
{
    /// <summary>
    /// Connects the kit to a JellyGarden LevelManager and clears effects when a level ends.
    /// Installed automatically on the LevelManager GameObject after scene load; define
    /// MATCH3VFX_NO_AUTO_BRIDGE to add it yourself. The static helpers are what the hook
    /// snippets in HOOKS.md call from Item, Square and LevelManager.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-40)]
    public sealed class JellyGardenVfxBridge : MonoBehaviour
    {
        [Tooltip("Optional. Empty = Resources/Match3VfxSettings or built-in defaults.")]
        public Match3VfxSettings settings;
        public string reducedMotionKey = "RoyalThrone.HomeMotion";
        public string soundKey = "Sound";

        static JellyGardenVfxBridge instance;
        GameState lastState;
        Transform lastField;
        int lastLevel;
        bool observed;

#if !MATCH3VFX_NO_AUTO_BRIDGE
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoInstall()
        {
            var lm = LevelManager.THIS ? LevelManager.THIS : FindAnyObjectByType<LevelManager>();
            if (lm && !lm.GetComponent<JellyGardenVfxBridge>()) lm.gameObject.AddComponent<JellyGardenVfxBridge>();
        }
#endif

        void Awake()
        {
            instance = this;
            Match3Vfx.Configure(settings, new JellyGardenVfxHost { reducedMotionKey = reducedMotionKey, soundKey = soundKey }, new JellyGardenVfxBoard());
            Match3Vfx.Prewarm();
        }

        void Update()
        {
            var lm = LevelManager.THIS;
            if (!lm) return;
            var state = lm.gameStatus;
            // Never let an old board's effects appear over the map or a newly loaded level.
            bool leftBoard = observed && state != lastState && (state == GameState.Map || state == GameState.PrepareGame);
            bool boardReplaced = observed && (lm.GameField != lastField || lm.currentLevel != lastLevel);
            if (leftBoard || boardReplaced) Match3Vfx.Clear();
            lastState = state;
            lastField = lm.GameField;
            lastLevel = lm.currentLevel;
            observed = true;
        }

        void OnDestroy()
        {
            if (instance != this) return;
            instance = null;
            Match3Vfx.Clear();
        }

        // ------------------------------------------------------------------ helpers for hooks

        /// <summary>JellyGarden item type to kit power. HELICOPTER is matched by name so older enums still compile.</summary>
        public static Match3Power PowerOf(ItemsTypes type)
        {
            switch (type)
            {
                case ItemsTypes.HORIZONTAL_STRIPPED: return Match3Power.RocketHorizontal;
                case ItemsTypes.VERTICAL_STRIPPED: return Match3Power.RocketVertical;
                case ItemsTypes.PACKAGE: return Match3Power.Tnt;
                case ItemsTypes.BOMB: return Match3Power.ColorOrb;
            }
            return type.ToString() == "HELICOPTER" ? Match3Power.Helicopter : Match3Power.None;
        }

        public static bool IsPower(ItemsTypes type) => PowerOf(type) != Match3Power.None;

        /// <summary>Cells holding a normal piece of <paramref name="color"/>.</summary>
        public static List<Vector2Int> CellsOfColor(int color)
        {
            var cells = new List<Vector2Int>();
            var lm = LevelManager.THIS;
            if (!lm || lm.squaresArray == null) return cells;
            foreach (var square in lm.squaresArray)
                if (square && square.item && !square.item.destroying && square.item.currentType == ItemsTypes.NONE && square.item.color == color)
                    cells.Add(JellyGardenVfxBoard.CellOf(square));
            return cells;
        }

        /// <summary>Every playable cell, for orb + orb.</summary>
        public static List<Vector2Int> AllCells()
        {
            var cells = new List<Vector2Int>();
            var lm = LevelManager.THIS;
            if (!lm || lm.squaresArray == null) return cells;
            foreach (var square in lm.squaresArray)
                if (square && !square.IsNone()) cells.Add(JellyGardenVfxBoard.CellOf(square));
            return cells;
        }

        /// <summary>The colour with the most normal pieces on the board (0-5).</summary>
        public static int DominantColor()
        {
            var counts = new int[6];
            var lm = LevelManager.THIS;
            if (lm && lm.squaresArray != null)
                foreach (var square in lm.squaresArray)
                    if (square && square.item && square.item.color >= 0 && square.item.color < counts.Length) counts[square.item.color]++;
            int best = 0;
            for (int i = 1; i < counts.Length; i++) if (counts[i] > counts[best]) best = i;
            return best;
        }

        /// <summary>
        /// Activates a power-up item that is being destroyed. For a colour orb pass the colour of the
        /// piece it was swapped with, or -1 to use the dominant colour.
        /// </summary>
        public static Match3VfxHandle Activate(Item item, int orbColor = -1)
        {
            if (!item || !item.square) return Match3VfxHandle.Completed;
            var power = PowerOf(item.currentType);
            List<Vector2Int> targets = null;
            if (power == Match3Power.ColorOrb) targets = CellsOfColor(orbColor >= 0 && orbColor < 6 ? orbColor : DominantColor());
            return Match3Vfx.PlayPowerup(power, JellyGardenVfxBoard.CellOf(item), targets);
        }

        /// <summary>
        /// Two power-ups swapped together. <paramref name="convert"/> turns orb targets into the other
        /// power-up before they are hit (for example: set item.currentType and its sprite).
        /// </summary>
        public static Match3VfxHandle Combine(Item a, Item b, System.Action<Vector2Int, Match3Power> convert = null)
        {
            if (!a || !b || !a.square) return Match3VfxHandle.Completed;
            var pa = PowerOf(a.currentType);
            var pb = PowerOf(b.currentType);
            List<Vector2Int> targets = null;
            if (pa == Match3Power.ColorOrb || pb == Match3Power.ColorOrb)
                targets = pa == pb ? AllCells() : CellsOfColor(DominantColor());
            return Match3Vfx.PlayCombo(pa, pb, JellyGardenVfxBoard.CellOf(a), targets, convert);
        }

        /// <summary>Standard pop for a destroyed item, including the rocket/blast style when a kit sequence hit it.</summary>
        public static Coroutine PopItem(Item item, float intensity, float stagger, Vector3 restScale, SpriteRenderer[] renderers = null)
        {
            if (!item) return null;
            return item.StartCoroutine(Match3PieceMotion.Pop(item.transform, item.sprRenderer, new Match3PopOptions
            {
                kind = JellyGardenVfxBoard.TakePopKind(item),
                burstColor = item.GetJuiceColor(),
                colorIndex = item.currentType == ItemsTypes.INGREDIENT ? -1 : item.color,
                intensity = intensity,
                stagger = stagger,
                restScale = restScale,
                renderers = renderers,
                onFinished = item.SetAnimationDestroyingFinished
            }));
        }
    }
}

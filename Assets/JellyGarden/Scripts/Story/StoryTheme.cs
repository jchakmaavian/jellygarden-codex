// How each arc dresses the board.
//
// Six arcs move through the kingdom — castle, village, mines and roads, border,
// ruins and crypts, siege — and the board should look like the place the player
// is in. Themes are data (Resources/Story/themes.json), so retheming an arc is
// editing a file.
//
// Deliberately NOT themed: the gems. Item derives its own colour by looking its
// sprite up in its array (Item.cs: color = Array.IndexOf(items, sprite)), and
// items spawned mid-cascade come from the prefab, not from anything a theme
// could reach. Swapping gem art would risk a tile whose colour no longer
// matches what it looks like — and the board's behaviour is the one thing that
// must not regress. What is themed is everything around the gems.

using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public sealed class ArcTheme
    {
        public int Arc;
        public string Title = string.Empty;
        public string ThemeKey = string.Empty;
        public string BoardBackdrop = string.Empty;
        public int KitBackdropIndex;
        public Color SquareTint = Color.white;

        /// <summary>
        /// The colour the board is drawn on. The kit's cell sprite is flat black
        /// at half alpha, so the board has never had a ground of its own — it
        /// showed the backdrop through a dark checker. This is that ground: the
        /// cell's own sprite is swapped for a white square and this colour is
        /// what it is drawn in, so the value is the ground exactly.
        ///
        /// Its alpha is how much of the backdrop is let through. Below 1 the
        /// board reads as tinted glass over the arc's picture rather than as a
        /// solid slab, which is the look the boards being aimed at have.
        /// </summary>
        public Color BoardFill = Color.clear;

        /// <summary>False when the arc names no fill, which leaves the kit's look.</summary>
        public bool HasBoardFill { get { return BoardFill.a > 0f; } }

        /// <summary>
        /// The darker half of the board's checker.
        ///
        /// The kit makes its checker out of two identical black tiles at
        /// different alphas, which is a difference that cannot survive the
        /// cells being given a real ground colour. Derived rather than authored
        /// because a checker whose two squares are separately chosen is a
        /// checker somebody will eventually get wrong; 7% is the step the kit's
        /// own two alphas work out to against a lit board.
        /// </summary>
        public Color BoardFillDark
        {
            get
            {
                return new Color(BoardFill.r * 0.93f, BoardFill.g * 0.93f,
                    BoardFill.b * 0.93f, BoardFill.a);
            }
        }
    }

    public static class StoryTheme
    {
        private static bool _loaded;
        private static readonly Dictionary<int, ArcTheme> _byArc = new Dictionary<int, ArcTheme>();

        public static void Load()
        {
            if (_loaded) return;
            _loaded = true;

            var text = Resources.Load<TextAsset>("Story/themes");
            if (text == null)
            {
                Debug.LogWarning("[Story] No themes.json; the board keeps the kit's own look.");
                return;
            }

            JsonValue root = JsonValue.Parse(text.text);
            foreach (JsonValue node in root["themes"].Items)
            {
                JsonValue tint = node["squareTint"];
                JsonValue fill = node["boardFill"];
                var theme = new ArcTheme
                {
                    Arc = node["arc"].AsInt(),
                    Title = node["title"].AsString(),
                    ThemeKey = node["themeKey"].AsString(),
                    BoardBackdrop = node["boardBackdrop"].AsString(),
                    KitBackdropIndex = node["kitBackdropIndex"].AsInt(),
                    SquareTint = new Color(
                        tint[0].AsFloat(1f), tint[1].AsFloat(1f), tint[2].AsFloat(1f), 1f),
                    // An arc with no boardFill keeps Color.clear, and the board
                    // is drawn the way the kit draws it. A fourth component is
                    // the ground's alpha — how much of the backdrop is let
                    // through it — and an arc that names only three is opaque.
                    BoardFill = fill.Count < 3
                        ? Color.clear
                        : new Color(
                            fill[0].AsFloat(0f), fill[1].AsFloat(0f), fill[2].AsFloat(0f),
                            fill.Count >= 4 ? fill[3].AsFloat(1f) : 1f)
                };
                if (theme.Arc > 0) _byArc[theme.Arc] = theme;
            }

            Debug.Log(string.Format("[Story] {0} arc themes loaded.", _byArc.Count));
        }

        public static ArcTheme ForArc(int arc)
        {
            Load();
            ArcTheme theme;
            return _byArc.TryGetValue(arc, out theme) ? theme : null;
        }

        public static ArcTheme ForArea(int areaId)
        {
            StoryArcInfo arc = StoryDatabase.ArcOf(areaId);
            return arc != null ? ForArc(arc.Arc) : null;
        }

        /// <summary>The theme for a level, via the area it belongs to.</summary>
        public static ArcTheme ForLevel(int levelNumber)
        {
            StoryLevelEntry entry = StoryLevelPlan.Get(levelNumber);
            return entry != null ? ForArc(entry.Arc) : null;
        }

        public static int Count
        {
            get { Load(); return _byArc.Count; }
        }

        public static void Unload()
        {
            _loaded = false;
            _byArc.Clear();
        }
    }
}

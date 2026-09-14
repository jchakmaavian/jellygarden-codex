// Compare the six clear effects on the real board, and keep the winner.
//
// WHY THIS EXISTS
//
// An effect was chosen in a browser prototype and then built in Unity, and the
// two did not look the same — not because either was wrong, but because the
// prototype drew on an empty canvas and the game draws on top of the kit's own
// clear: a 4-frame flipbook and a 250ms shrink that no prototype models. Every
// comparison made outside the game was therefore a comparison of the wrong
// thing, and each round trip to find that out cost a Unity build.
//
// So the comparison happens HERE, on a real board, with the real gems, the real
// camera, the real pitch and the kit's own clear underneath. What is previewed
// is what ships, by construction rather than by care.
//
//     tools/run_player.sh -storyvfxlab
//
// SIX BUTTONS, ONE BOARD
//
// The lab launches an ordinary level and draws a panel over it. Each button
// selects a variant and clears three gems with it, in place, as many times as
// asked. "Use this" writes the choice through StoryVfxVariant.Save and the
// whole game — not just the lab — then uses it.
//
// HOW A MATCH IS FORCED
//
// Not by calling DestroyItem on three gems: that skips Square.FindMatchesAround
// and LevelManager.FindMatches, so it would preview a code path the player
// never reaches, and a cascade would not follow. Instead three adjacent gems
// are recoloured with Item.SetColor and the kit is asked to find the match it
// now has. The clear, the cascade, the refill and the scoring are the real
// ones.
//
// Three gems in a ROW, always the same three, near the middle of the board:
// comparing two effects is only possible if nothing else differs between the
// runs, and a match found wherever the board happened to offer one differs in
// position, colour and what falls in behind it.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    [DisallowMultipleComponent]
    public sealed class StoryVfxLab : MonoBehaviour
    {
        private const string Flag = "-storyvfxlab";

        /// <summary>Set by an Editor runner, which cannot pass a command line.</summary>
        private const string Pref = "StoryVfxLab.Requested";

        /// <summary>
        /// The level the lab plays on. 1 is the kit's own first board — nine
        /// wide, no obstacles, no goal that can end the level while comparing.
        /// </summary>
        private const int Level = 1;
        private const int AreaId = 1;

        /// <summary>Above the level's canvas and the shell's screens (300-600).</summary>
        private const int SortingOrder = 700;

        private static bool _installed;

        private VfxVariant _selected;
        private RocketVariant _selectedRocket;
        private BombVariant _selectedBomb;
        private Text _status;
        private readonly List<Button> _buttons = new List<Button>();
        private bool _busy;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Install()
        {
            if (_installed) return;
            if (!Requested()) return;

            _installed = true;
            var host = new GameObject("~StoryVfxLab");
            DontDestroyOnLoad(host);
            host.AddComponent<StoryVfxLab>();
        }

        private static bool Requested()
        {
            foreach (string arg in global::System.Environment.GetCommandLineArgs())
                if (arg == Flag) return true;
            return PlayerPrefs.GetInt(Pref, 0) == 1;
        }

        private IEnumerator Start()
        {
            _selected = StoryVfxVariant.Current;

            // The board does not exist when a scene finishes loading — see the
            // Story CLAUDE.md note on LevelManager assigning THIS in Start.
            yield return new WaitForSecondsRealtime(2f);

            yield return OpenBoard();
            BuildPanel();
        }

        /// <summary>
        /// Get a real level up, tapping through whatever beat stands in the way.
        /// Same shape as StoryThemeTest.Reach: a story beat waits for a tap
        /// forever, and a harness that cannot get past one reports it as a fault
        /// and hides everything behind it.
        /// </summary>
        private IEnumerator OpenBoard()
        {
            StoryFlow.LaunchLevel(Level, AreaId);

            float waited = 0f;
            while (waited < 30f)
            {
                if (LevelManager.THIS != null &&
                    LevelManager.THIS.gameStatus == GameState.Playing) break;

                if (StoryShell.StoryIsPlaying) PressAdvance();

                waited += 0.25f;
                yield return new WaitForSecondsRealtime(0.25f);
            }

            // The kit dims the screen and draws a hand over level 1. Every
            // harness here has these three lines for the same reason.
            var tutorial = FindObjectOfType<JellyGarden.Scripts.GUI.TutorialStuff.TutorialManager>();
            if (tutorial != null) tutorial.gameObject.SetActive(false);

            yield return new WaitForSecondsRealtime(1f);
        }

        private static void PressAdvance()
        {
            Button[] buttons = FindObjectsOfType<Button>();
            for (int i = 0; i < buttons.Length; i++)
                if (buttons[i] != null && buttons[i].name == "Advance")
                    buttons[i].onClick.Invoke();
        }

        private void BuildPanel()
        {
            Canvas canvas = StoryUi.CreateOverlayCanvas("StoryVfxLab", SortingOrder);
            StoryObjects.Persist(canvas.gameObject);

            RectTransform root = StoryUi.CreateRect("Panel", canvas.transform);
            // A status caption, one row holding Bomb and Random, and the keep
            // strip.
            //   40 status + 1*(58+8) + 62 keep + slack = 180
            StoryUi.BottomAnchored(root, 0f, 180f);

            var backing = root.gameObject.AddComponent<Image>();
            backing.color = new Color(0.05f, 0.06f, 0.09f, 0.92f);

            _status = StoryUi.CreateText("Status", root,
                "Tap a variant to clear three gems with it.", 20,
                Color.white, TextAnchor.MiddleCenter);
            StoryUi.TopAnchored(_status.rectTransform, 6f, 34f);

            // Six buttons in two rows of three, measured DOWNWARD from the top
            // of the panel rather than stacked upward off its floor.
            //
            // Stacking upward is what broke the first version: row 0 sat at
            // 74..136 above the bottom and row 1 at 8..70, which is the same
            // band the "Use this" button occupies at 6..64. Row 1 was drawn
            // underneath it and ran off the bottom edge — three buttons
            // visible, three lost, with nothing in the log to say so.
            //
            // Anchored to the TOP of the panel, each row's position depends on
            // the row index alone and cannot drift into whatever is reserved at
            // the bottom. The two bands are now stated once, here, and the
            // arithmetic between them cannot overlap by construction.
            const float StatusBand = 40f;   // the caption at the top
            const float RowHeight = 58f;    // one button
            const float RowGap = 8f;
            const float KeepBand = 62f;     // the strip "Use this" owns

            // The clear variants are NOT offered here any more. GlassBurst was
            // compared on this board and chosen, and StoryVfxVariant.Resolve now
            // returns it on every build — so a button that could change it is a
            // way to lose a settled decision by a mis-tap, not a feature.
            //
            // Nothing about the clear effect was deleted: the registry, the six
            // variants and the -storyvfxvariant flag all still exist, so the
            // comparison can be reopened by putting this loop back.
            // The rocket trails are NOT offered here any more. HeavyExhaust was
            // compared on this board and chosen, and StoryRocketVariant.Resolve
            // returns it on every build — so a button that could change it is a
            // way to lose a settled decision by a mis-tap.
            //
            // Nothing about the trails was deleted: the registry, the five
            // variants and the -storyrocketvariant flag all still exist, so the
            // comparison can be reopened by putting that loop back. Same
            // treatment the clear variants got when GlassBurst was settled.
            float bombTop = StatusBand;

            // ONE bomb button, not five.
            //
            // StoryBombVariant.BlastOf returns the same blast for every entry —
            // the one matched to the owner's reference frames — so five named
            // buttons drew five identical explosions while the status line
            // announced a different name each time. A control that claims a
            // choice it does not have is worse than no control: it is how an
            // afternoon went into comparing effects that were never different.
            //
            // The enum and the registry are deliberately kept. The pref and the
            // -storybombvariant flag still resolve, so giving the cases
            // different bodies again is all it takes to reopen a comparison.
            Button bomb = StoryUi.CreateButton("Bomb", root, "Bomb", 15, OnBombFixed);
            RectTransform bombRect = bomb.GetComponent<RectTransform>();
            bombRect.anchorMin = new Vector2(0f, 1f);
            bombRect.anchorMax = new Vector2(1f / 3f, 1f);
            bombRect.pivot = new Vector2(0.5f, 1f);
            bombRect.offsetMin = new Vector2(4f, -(bombTop + RowHeight));
            bombRect.offsetMax = new Vector2(-4f, -bombTop);
            _buttons.Add(bomb);

            // The colour bomb, beside the package bomb. Its own button rather
            // than a variant of the other: it is a different sequence entirely —
            // charge, mark, wave, staggered pops — and only shares the blast
            // that each of its targets ends with.
            Button colour = StoryUi.CreateButton("ColorBomb", root, "Colour Bomb", 15, OnColorBomb);
            RectTransform colourRect = colour.GetComponent<RectTransform>();
            colourRect.anchorMin = new Vector2(1f / 3f, 1f);
            colourRect.anchorMax = new Vector2(2f / 3f, 1f);
            colourRect.pivot = new Vector2(0.5f, 1f);
            colourRect.offsetMin = new Vector2(4f, -(bombTop + RowHeight));
            colourRect.offsetMax = new Vector2(-4f, -bombTop);
            _buttons.Add(colour);

            // A sixth button in the last free slot of the bomb rows: detonate
            // the CURRENT explosion at a random cell.
            //
            // The five above always fire from the middle of the board, which is
            // right for comparing them — nothing differs between taps but the
            // explosion. It is wrong for judging one: a blast that reads well in
            // open board can cover the HUD from the top row, or spill off the
            // edge from a corner. This is how those show up.
            Button debug = StoryUi.CreateButton("RandomBomb", root, "Random ▸", 15, OnRandomBomb);
            float debugTop = bombTop;

            RectTransform debugRect = debug.GetComponent<RectTransform>();
            debugRect.anchorMin = new Vector2(2f / 3f, 1f);
            debugRect.anchorMax = new Vector2(1f, 1f);
            debugRect.pivot = new Vector2(0.5f, 1f);
            debugRect.offsetMin = new Vector2(4f, -(debugTop + RowHeight));
            debugRect.offsetMax = new Vector2(-4f, -debugTop);
            _buttons.Add(debug);

            // Its own band at the bottom, which no row can reach into.
            Button keep = StoryUi.CreateButton("UseThis", root, "Use this", 20, OnKeep);
            RectTransform keepRect = keep.GetComponent<RectTransform>();
            keepRect.anchorMin = new Vector2(0f, 0f);
            keepRect.anchorMax = new Vector2(1f, 0f);
            keepRect.pivot = new Vector2(0.5f, 0f);
            keepRect.offsetMin = new Vector2(4f, 8f);
            keepRect.offsetMax = new Vector2(-4f, KeepBand);

            Refresh();
        }

        private void OnVariant(VfxVariant variant)
        {
            if (_busy) return;
            _selected = variant;
            StoryVfxVariant.Current = variant;
            Refresh();
            StartCoroutine(Fire());
        }

        private void OnRocket(RocketVariant variant)
        {
            if (_busy) return;
            _selectedRocket = variant;
            StoryRocketVariant.Current = variant;
            if (_status != null)
                _status.text = StoryRocketVariant.NameOf(variant) + " — " +
                               StoryRocketVariant.DescriptionOf(variant);
            StartCoroutine(FireRocket());
        }

        private void OnRandomBomb()
        {
            if (_busy) return;
            StartCoroutine(FireBomb(true));
        }

        private void OnColorBomb()
        {
            if (_busy) return;
            if (_status != null) _status.text = "Colour bomb — charging";
            StartCoroutine(FireColorBomb());
        }

        /// <summary>
        /// Make a colour bomb in the middle of the board and set it off against
        /// whatever colour is most common, so the sequence has plenty of targets
        /// to mark rather than one or two.
        /// </summary>
        private IEnumerator FireColorBomb()
        {
            _busy = true;

            LevelManager manager = LevelManager.THIS;
            if (manager == null || manager.gameStatus != GameState.Playing)
            {
                if (_status != null) _status.text = "No live board — open a level first.";
                _busy = false;
                yield break;
            }

            Square square = manager.GetSquare(manager.maxCols / 2, manager.maxRows / 2);
            if (square == null || square.item == null)
            {
                if (_status != null) _status.text = "Middle cell is empty — try again.";
                _busy = false;
                yield break;
            }

            // The commonest colour on the board, so the wave has a crowd to work
            // through. Picking the bomb's own colour would often mark only a
            // handful and the stagger would not read at all.
            var counts = new Dictionary<int, int>();
            Square[] all = manager.squaresArray;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null || all[i].item == null) continue;
                int c = all[i].item.color;
                counts[c] = counts.ContainsKey(c) ? counts[c] + 1 : 1;
            }

            int best = -1, bestCount = 0;
            foreach (KeyValuePair<int, int> pair in counts)
                if (pair.Value > bestCount) { best = pair.Key; bestCount = pair.Value; }

            if (best < 0)
            {
                if (_status != null) _status.text = "No gems to target — try again.";
                _busy = false;
                yield break;
            }

            Item bomb = square.item;
            bomb.NextType = ItemsTypes.BOMB;
            bomb.ChangeType();

            yield return new WaitForSecondsRealtime(0.45f);

            if (_status != null)
                _status.text = string.Format("Colour bomb — {0} targets", bestCount);

            if (bomb != null && !bomb.destroying)
                bomb.DestroyColor(best);

            // Long enough for the WHOLE sequence, which now scales with the
            // target count rather than being fixed.
            //
            // A full board can hand the bomb twenty-five targets: 0.30s of
            // charge, then 24 * 0.055s of bolts firing one after another, then
            // 0.70s of wave, then the last target's own blast which lives
            // another second or so. That is about 3.4s, and the 2.6s this
            // replaced would have cleared _busy while bolts were still in the
            // air — letting a second tap start over the top of the first.
            yield return new WaitForSecondsRealtime(4.0f);
            _busy = false;
        }

        private void OnBombFixed()
        {
            if (_busy) return;
            if (_status != null) _status.text = "Bomb — centre of the board";
            StartCoroutine(FireBomb());
        }

        /// <summary>
        /// Make a package bomb in the middle of the board and set it off.
        ///
        /// The package rather than the colour bomb: it clears the 3x3 around
        /// itself, so the explosion is compared against a consistent amount of
        /// board every time. A colour bomb clears scattered cells and the amount
        /// of clearing would differ on every tap, which is the same reason the
        /// clear variants always fire on the same three cells.
        /// </summary>
        private IEnumerator FireBomb(bool random = false)
        {
            _busy = true;

            LevelManager manager = LevelManager.THIS;
            if (manager == null || manager.gameStatus != GameState.Playing)
            {
                if (_status != null) _status.text = "No live board — open a level first.";
                _busy = false;
                yield break;
            }

            Square square;

            if (random)
            {
                // A cell with a gem on it, found by trying rather than by
                // scanning: a board mid-cascade has holes, and an obstacle
                // layout has cells that never hold a gem at all.
                square = null;
                for (int attempt = 0; attempt < 20 && square == null; attempt++)
                {
                    Square candidate = manager.GetSquare(
                        Random.Range(0, manager.maxCols),
                        Random.Range(0, manager.maxRows));

                    if (candidate != null && candidate.item != null &&
                        !candidate.item.destroying)
                        square = candidate;
                }
            }
            else
            {
                square = manager.GetSquare(manager.maxCols / 2, manager.maxRows / 2);
            }

            if (square == null || square.item == null)
            {
                if (_status != null)
                    _status.text = random
                        ? "No gem to convert — try again."
                        : "Middle cell is empty — try again.";
                _busy = false;
                yield break;
            }

            if (random && _status != null)
                _status.text = string.Format("Bomb at {0},{1}", square.col, square.row);

            Item bomb = square.item;
            bomb.NextType = ItemsTypes.PACKAGE;
            bomb.ChangeType();

            // Let the appear animation and the sprite swap land, so the blast
            // comes off a piece that actually looks like a bomb.
            yield return new WaitForSecondsRealtime(0.45f);

            if (bomb != null && !bomb.destroying)
                bomb.DestroyPackage();

            // Implosion gathers for 180ms before it expands, and the longest
            // debris lives 0.72s, so the wait covers the slowest variant.
            yield return new WaitForSecondsRealtime(2.0f);
            _busy = false;
        }

        private void OnKeep()
        {
            // The rocket trail alone. The clear effect is settled in code and
            // the panel no longer offers it, so saving it here would write a
            // pref that only shadows the same value — and would quietly become
            // wrong the day the code default changes.
            // The explosion alone. The trail is settled in code and the panel no
            // longer offers it, so saving it here would write a pref that only
            // shadows the same value — and would quietly become wrong the day
            // the code default changes. Same reasoning as when the clear
            // variants were removed.
            StoryBombVariant.Save(_selectedBomb);
            if (_status != null)
                _status.text = "Saved: " + StoryBombVariant.NameOf(_selectedBomb) +
                               ". The whole game uses it now.";
        }

        /// <summary>
        /// Make a rocket in the middle of the board and set it off.
        ///
        /// NextType + ChangeType is the kit's own way in — the same pair
        /// Item.CheckMatches uses when a match of four earns a rocket — so the
        /// piece gets the kit's appear animation, its striped sprite and its
        /// real currentType rather than a faked one.
        ///
        /// ChangeTypeCor waits on `appeared`, which is set by SetAppeared when a
        /// gem finishes falling in. Every gem on a settled board already has it,
        /// so the wait returns at once; on a board still cascading this simply
        /// takes a moment longer rather than hanging.
        /// </summary>
        private IEnumerator FireRocket(bool random = false)
        {
            _busy = true;

            LevelManager manager = LevelManager.THIS;
            if (manager == null || manager.gameStatus != GameState.Playing)
            {
                if (_status != null) _status.text = "No live board — open a level first.";
                _busy = false;
                yield break;
            }

            Square square;
            bool horizontal = true;

            if (random)
            {
                // A cell with an item on it, found by trying rather than by
                // scanning: a board mid-cascade has holes, and a level with an
                // obstacle layout has cells that never hold a gem at all. Twenty
                // tries is far more than enough on any board this game ships.
                square = null;
                for (int attempt = 0; attempt < 20 && square == null; attempt++)
                {
                    Square candidate = manager.GetSquare(
                        Random.Range(0, manager.maxCols),
                        Random.Range(0, manager.maxRows));

                    if (candidate != null && candidate.item != null &&
                        !candidate.item.destroying)
                        square = candidate;
                }

                // Both axes, because a horizontal rocket on the bottom row and a
                // vertical one in the first column are the two cases where the
                // plume has least room to spread — and those are exactly the
                // ones a centre-of-the-board test never shows.
                horizontal = Random.value < 0.5f;
            }
            else
            {
                square = manager.GetSquare(manager.maxCols / 2, manager.maxRows / 2);
            }

            if (square == null || square.item == null)
            {
                if (_status != null)
                    _status.text = random
                        ? "No gem to convert — try again."
                        : "Middle cell is empty — try again.";
                _busy = false;
                yield break;
            }

            Item rocket = square.item;
            rocket.NextType = horizontal
                ? ItemsTypes.HORIZONTAL_STRIPPED
                : ItemsTypes.VERTICAL_STRIPPED;
            rocket.ChangeType();

            if (random && _status != null)
                _status.text = string.Format("{0} rocket at {1},{2} — {3}",
                    horizontal ? "Horizontal" : "Vertical",
                    square.col, square.row,
                    StoryRocketVariant.NameOf(StoryRocketVariant.Current));

            // Let the appear animation and the sprite swap land before it is
            // destroyed, or the rocket fires from a gem that still looks plain.
            yield return new WaitForSecondsRealtime(0.45f);

            if (rocket != null && !rocket.destroying)
                rocket.DestroyItem(true);

            // The plume outlives the head by most of a second, so the wait is
            // longer than the flight.
            yield return new WaitForSecondsRealtime(2.2f);
            _busy = false;
        }

        private void Refresh()
        {
            if (_status == null) return;
            _status.text = StoryVfxVariant.NameOf(_selected) + " — " +
                           StoryVfxVariant.DescriptionOf(_selected);
        }

        /// <summary>
        /// Recolour three gems in a row so the kit finds a real match there,
        /// then let the board clear it exactly as it would in play.
        /// </summary>
        private IEnumerator Fire()
        {
            _busy = true;

            LevelManager manager = LevelManager.THIS;
            if (manager == null || manager.gameStatus != GameState.Playing)
            {
                if (_status != null) _status.text = "No live board — open a level first.";
                _busy = false;
                yield break;
            }

            // The middle of the board, so the three cells are surrounded and the
            // neighbour response has somewhere to land.
            int row = manager.maxRows / 2;
            int firstCol = Mathf.Max(0, manager.maxCols / 2 - 1);

            var trio = new List<Item>(3);
            for (int i = 0; i < 3; i++)
            {
                Square square = manager.GetSquare(firstCol + i, row);
                if (square == null || square.item == null) break;
                trio.Add(square.item);
            }

            if (trio.Count < 3)
            {
                if (_status != null) _status.text = "Middle row is not clear — try again.";
                _busy = false;
                yield break;
            }

            // One colour across the three. Item.color is read back out of the
            // sprite array, so SetColor is the supported way in — see the Story
            // CLAUDE.md warning about deriving colour from a swapped sprite.
            int colour = trio[0].color;
            for (int i = 0; i < trio.Count; i++) trio[i].SetColor(colour);

            yield return null;

            // Ask the kit to find what is now there, rather than destroying the
            // three by hand: this is the path a player's match takes.
            manager.FindMatches();

            yield return new WaitForSecondsRealtime(1.6f);
            _busy = false;
        }
    }
}

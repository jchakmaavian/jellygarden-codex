// The bottom navigation bar: the one piece of the shell that is never a screen.
//
// Royal Match carries the same five-tab strip across every screen outside a
// level — Areas, Leaderboard, Home, Team, Cards, with Home raised and centred
// (docs/royal-match/01-home-area.jpg). It is not part of the home screen: the
// area page shows the same bar without redrawing it, which is why this lives on
// its own persistent canvas rather than inside StoryHomeScreen's NavBand. That
// band stays as the reservation it always was — the bar is laid out on exactly
// the same fraction of the screen — so nothing on home is drawn underneath it.
//
// Two of the five tabs go somewhere. Leaderboard, Team and Cards are features
// this game does not have, and Royal Match's bar is five slots wide whether or
// not you have unlocked them; hiding them would put Home off centre, which is
// the one thing about this bar the player navigates by. They say so when tapped
// rather than doing nothing, because a button that swallows a tap reads as
// broken.
//
// Sorting order 500 is deliberate: above home (300) and the area page (400), so
// the bar is on top of both, and below the level popup (600), the repair
// cutscene (780) and everything above them, so a modal covers it the way one
// should.

using System;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed class StoryNavBar : MonoBehaviour
    {
        public const string HostName = "~StoryNavBar";

        private const int SortingOrder = 500;

        /// <summary>
        /// The signs on the rope, in the order they hang.
        ///
        /// TWO, because two screens exist. The bar this replaces had five slots
        /// and three of them answered a tap with an apology — Team is not in the
        /// game at all, and Leaderboard and Cards are not built. A board hanging
        /// there is a promise the game cannot keep, and it made the two real
        /// destinations harder to find, not easier.
        ///
        /// Adding a third is adding a line to each of these arrays. The signs
        /// share the width evenly, so nothing else moves.
        /// </summary>
        public enum Tab
        {
            Areas = 0,
            Home = 1
        }

        private static readonly StoryUi.NavIcon[] Icons =
        {
            StoryUi.NavIcon.Areas, StoryUi.NavIcon.Home
        };

        private static readonly string[] Labels =
        {
            "Areas", "Home"
        };

        private static StoryNavBar _instance;

        /// <summary>
        /// Where the Areas tab goes. Null is the area the player is restoring,
        /// which is the only browsable thing that exists until US-011 builds the
        /// list of all 150 — that story sets this and nothing in here changes.
        /// The same shape as StoryLevelHud.Source and StoryLevelPopup.Launcher.
        /// </summary>
        public static Action AreasDestination;

        /// <summary>
        /// Whether a level is being played, which is the one condition that hides
        /// this bar. A seam, so a test can put the shell "in a level" without a
        /// board in the scene.
        /// </summary>
        public static Func<bool> LevelIsPlayingSource;

        private Canvas _canvas;
        private StoryUi.NavBarParts _bar;
        private Tab _selected = Tab.Home;
        private bool _shown;

        public static StoryNavBar Instance
        {
            get { return _instance; }
        }

        /// <summary>
        /// Whether the bar is on screen right now. It is the CANVAS that is turned
        /// off, never this component's own object: deactivating the host would
        /// stop Update and the bar could never come back after a level.
        /// </summary>
        public static bool IsVisible
        {
            get
            {
                return _instance != null && _instance._canvas != null
                       && _instance._canvas.gameObject.activeSelf;
            }
        }

        /// <summary>Which tab is lit.</summary>
        public Tab Selected
        {
            get { return _selected; }
        }

        /// <summary>The bar's five tabs, for a test that wants to press one.</summary>
        public StoryUi.NavBarParts Parts
        {
            get { return _bar; }
        }

        /// <summary>The button for a tab, or null before the bar is built.</summary>
        public Button ButtonFor(Tab tab)
        {
            if (_bar == null || _bar.Tabs == null) return null;
            int index = (int)tab;
            if (index < 0 || index >= _bar.Tabs.Length) return null;
            return _bar.Tabs[index].Button;
        }

        /// <summary>The name of a tab, as the bar knows it.</summary>
        public static string NameOf(Tab tab)
        {
            int index = (int)tab;
            return index >= 0 && index < Labels.Length ? Labels[index] : tab.ToString();
        }

        /// <summary>
        /// True while a level is up. StoryFlow knows because it launched it, and
        /// the board itself is asked as well, because a level can be started
        /// through paths the story never sees.
        ///
        /// MenuManager's <c>menuPlay</c> is NOT the test, tempting as the name is:
        /// it is the kit's pre-play popup, shown from the map before a level, not
        /// the board. The board's own <c>gameStatus</c> is the thing, and it is
        /// GameState.Map for exactly as long as the player is out of a level.
        /// </summary>
        public static bool LevelIsPlaying
        {
            get
            {
                if (LevelIsPlayingSource != null) return LevelIsPlayingSource();
                if (StoryFlow.PlayingArea > 0) return true;
                return LevelManager.THIS != null && LevelManager.THIS.gameStatus != GameState.Map;
            }
        }

        /// <summary>
        /// Whether the shell is showing something the bar belongs under. It is not
        /// "always on": on the kit's own map, on the menu scene and in a level
        /// there is nothing for it to navigate away from.
        /// </summary>
        public static bool ShouldShow
        {
            get
            {
                if (LevelIsPlaying) return false;

                // A repair takes the whole screen for four seconds, and the tabs
                // standing on top of it would be the one thing on screen that is
                // not the room. Nothing navigates during a cutscene anyway.
                if (RepairCutscene.IsPlaying) return false;

                return StoryHomeScreen.IsOpen || AreaScreen.IsOpen || StoryAreasList.IsOpen;
            }
        }

        #region Lifecycle

        /// <summary>Called once per scene by StorySceneHooks; safe to repeat.</summary>
        public static void Install()
        {
            Ensure();
            Sync();
        }

        private static void Ensure()
        {
            if (_instance != null) return;

            var go = new GameObject(HostName);
            StoryObjects.Persist(go);
            _instance = go.AddComponent<StoryNavBar>();
            _instance.Build();
        }

        /// <summary>
        /// Brings the bar into line with whatever is on screen. Called every frame
        /// in the game, because a level starts and ends without telling this — and
        /// called directly by a test, which has no frame loop.
        /// </summary>
        public static void Sync()
        {
            Ensure();
            _instance.Apply();
        }

        private void Update()
        {
            Apply();
        }

        private void Apply()
        {
            bool show = ShouldShow;
            if (show != _shown)
            {
                _shown = show;
                if (_canvas != null) _canvas.gameObject.SetActive(show);

                // One line per change, never per frame. Whether the bar is up is
                // the whole of what this component decides, and it is decided from
                // three things that are not visible in the hierarchy.
                if (Application.isPlaying)
                    Debug.Log(string.Format(
                        "[Story] nav bar {0}: home {1}, area page {2}, in a level {3}.",
                        show ? "shown" : "hidden", StoryHomeScreen.IsOpen,
                        AreaScreen.IsOpen, LevelIsPlaying));
            }

            if (!show) return;

            // The Areas tab is lit by anything the Areas tab reaches — the list
            // of 150 and the area page it opens. Home lights Home. Home sorts
            // under both, so when several are open the topmost is the one being
            // read, and that is what the tab reports.
            Tab current = AreaScreen.IsOpen || StoryAreasList.IsOpen ? Tab.Areas : Tab.Home;
            if (current != _selected)
            {
                _selected = current;
                StoryUi.SetNavSelected(_bar, (int)current);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        #endregion

        #region Build

        private void Build()
        {
            _canvas = StoryUi.CreateOverlayCanvas("NavCanvas", SortingOrder);
            _canvas.transform.SetParent(transform, false);

            _bar = StoryUi.NavBar("Nav", _canvas.transform, Icons, Labels,
                (int)Tab.Home, OnTab);
            StoryUi.SetNavSelected(_bar, (int)Tab.Home);

            _shown = true;

            if (Application.isPlaying)
                Debug.Log(string.Format("[Story] nav bar built: {0} tabs [{1}], '{2}' raised.",
                    _bar.Tabs.Length, string.Join(" ", Labels), NameOf(Tab.Home)));
        }

        #endregion

        #region Tabs

        private void OnTab(int index)
        {
            var tab = (Tab)index;
            Debug.Log("[Story] nav: " + NameOf(tab) + " tapped.");

            switch (tab)
            {
                case Tab.Areas:
                    GoToAreas();
                    break;

                case Tab.Home:
                    GoHome();
                    break;

                default:
                    // Nothing reaches here while the rope carries two signs and
                    // both of them go somewhere. Kept as the landing for a sign
                    // added before the screen behind it is written.
                    StoryToast.Show(NameOf(tab) + " is not in this game yet.");
                    break;
            }
        }

        /// <summary>
        /// Where Areas goes. In the game StoryAreasList.Install points this at the
        /// list of all 150; the fallback is the one area the player is restoring,
        /// which is what a test that has not installed the list gets.
        /// </summary>
        private static void GoToAreas()
        {
            if (AreasDestination != null)
            {
                AreasDestination();
                return;
            }

            AreaScreen.Open(StoryHomeScreen.CurrentRoom);
        }

        /// <summary>
        /// Home, with the area page and the areas list closed behind it: home
        /// sorts below both, so leaving either open would put the screen the
        /// player just left back over the one they asked for.
        /// </summary>
        private static void GoHome()
        {
            AreaScreen.Close();
            StoryAreasList.Close();
            StoryHomeScreen.Open();
        }

        #endregion
    }
}

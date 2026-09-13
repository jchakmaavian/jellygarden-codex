// A persistent floating dock shared by the home and area screens.
// It sits above those screens and below their modals, and hides during gameplay
// and repair cinematics. Areas and Home retain their existing destinations;
// Ranks and Cards retain their existing unavailable-feature toast.

using System;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed class StoryNavBar : MonoBehaviour
    {
        public const string HostName = "~StoryNavBar";

        private const int SortingOrder = 500;

        public enum Tab
        {
            Areas = 0,
            Leaderboard = 1,
            Home = 2,
            Card = 3
        }

        private static readonly StoryUi.NavIcon[] Icons =
        {
            StoryUi.NavIcon.Areas, StoryUi.NavIcon.Leaderboard,
            StoryUi.NavIcon.Home, StoryUi.NavIcon.Cards
        };

        private static readonly string[] Labels =
        {
            "Areas", "Ranks", "Home", "Cards"
        };

        private static readonly Tab[] BookTabs = { Tab.Home, Tab.Areas, Tab.Card };
        private static int SlotOf(Tab tab) => StorybookUi.Enabled ? Array.IndexOf(BookTabs, tab) : (int)tab;
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

        /// <summary>The four tabs, for navigation checks.</summary>
        public StoryUi.NavBarParts Parts
        {
            get { return _bar; }
        }

        /// <summary>The button for a tab, or null before the bar is built.</summary>
        public Button ButtonFor(Tab tab)
        {
            if (_bar == null || _bar.Tabs == null) return null;
            int index = SlotOf(tab);
            if (index < 0 || index >= _bar.Tabs.Length) return null;
            return _bar.Tabs[index].Button;
        }

        /// <summary>The name of a tab, as the bar knows it.</summary>
        public static string NameOf(Tab tab)
        {
            if (StorybookUi.Enabled)
                return tab == Tab.Areas ? "Map" : tab == Tab.Card ? "Collection" : tab.ToString();
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
                if (StorybookUi.Enabled && AreaScreen.IsOpen) return false;

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
            Tab current = StorybookCollection.IsOpen ? Tab.Card : AreaScreen.IsOpen || StoryAreasList.IsOpen ? Tab.Areas : Tab.Home;
            if (current != _selected)
            {
                _selected = current;
                StoryCastleUi.SelectNav(_bar, SlotOf(current));
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
            _canvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(360, 720);

            _bar = StoryCastleUi.NavBar(_canvas.transform,
                StorybookUi.Enabled ? new[] { StoryUi.NavIcon.Home, StoryUi.NavIcon.Areas, StoryUi.NavIcon.Cards } : Icons,
                StorybookUi.Enabled ? new[] { "Home", "Map", "Collection" } : Labels, OnTab);
            StoryCastleUi.SelectNav(_bar, SlotOf(Tab.Home));

            _shown = true;

            if (Application.isPlaying)
                Debug.Log(string.Format("[Story] floating dock built: {0} tabs [{1}], '{2}' selected.",
                    _bar.Tabs.Length, string.Join(" ", StorybookUi.Enabled ? new[] { "Home", "Map", "Collection" } : Labels), NameOf(Tab.Home)));
        }

        #endregion

        #region Tabs

        private void OnTab(int index)
        {
            var tab = StorybookUi.Enabled ? BookTabs[index] : (Tab)index;
            Debug.Log("[Story] nav: " + NameOf(tab) + " tapped.");

            switch (tab)
            {
                case Tab.Areas:
                    GoToAreas();
                    break;

                case Tab.Home:
                    GoHome();
                    break;

                case Tab.Card:
                    if (StorybookUi.Enabled)
                    {
                        AreaScreen.Close();
                        StoryAreasList.Close();
                        StorybookCollection.Open();
                    }
                    else StoryToast.Show(NameOf(tab) + " is not open yet.");
                    break;

                default:
                    // Leaderboard and Card. The screens behind them are not
                    // written, and the sign says that rather than opening
                    // something empty.
                    StoryToast.Show(NameOf(tab) + " is not open yet.");
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
            StorybookCollection.Close();
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
            StorybookCollection.Close();
            AreaScreen.Close();
            StoryAreasList.Close();
            StoryHomeScreen.Open();
        }

        #endregion
    }
}

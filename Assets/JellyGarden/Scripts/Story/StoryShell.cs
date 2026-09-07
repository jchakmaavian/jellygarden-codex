// Which screen the game opens on, and what is on screen whenever a level is not.
//
// The kit opens on its winding path map: 150 stone nodes, a walking character
// and a pre-play popup. Royal Match has no map at all — the game opens on the
// room you are restoring, and everything else is reached from the five-tab bar
// along the bottom of it (docs/royal-match/01-home-area.jpg). This file is the
// piece that swaps one entry point for the other.
//
// The map is NOT deleted, and nothing in the kit is edited to hide it. Set
// StoryShell.UseMap = true and the map is the entry point again, exactly as the
// kit shipped it — which is the only honest way to keep a purchased screen "in
// place" while replacing it.
//
// Three invariants, all decided here so that no screen has to know about any
// other:
//
//   a level is never played under the shell   — a shell canvas sorts at 300..600
//                                               and would draw over the board
//   a level always ends somewhere             — home, with the star just earned
//                                               already counted on it
//   closing a screen never leaves nothing     — EnsureFloor puts home back under
//                                               whatever was just dismissed
//
// The third is what "no dead ends" actually costs. Every screen in this shell
// closes onto whatever opened it, which is correct while the player walks
// forwards — but the area page can also be opened by a level ending, by the
// preview menu and (with the map on) by a map node, and a red X with nothing
// underneath it is a black screen with a nav bar. EnsureFloor is called on every
// close path rather than trusted to the caller.

using System;
using JellyGarden.Scripts.GUI;
using JellyGarden.Scripts.MapScripts;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public sealed class StoryShell : MonoBehaviour
    {
        public const string HostName = "~StoryShell";

        /// <summary>
        /// Whether the kit's path map is the entry point. False is the shell:
        /// the game opens on the room. True hands the game back to the kit's own
        /// map and this class stops touching anything — which is what the map
        /// being "left in place" has to mean if it is to be believed.
        /// </summary>
        public static bool UseMap = false;

        /// <summary>The shell is on when the map is not.</summary>
        public static bool IsOn
        {
            get { return !UseMap; }
        }

        /// <summary>
        /// Whether the scene the player is in is the one the shell belongs to.
        /// The game opens on `main`, the kit's own menu, which has neither a
        /// board nor a map and is not the shell's business. A seam, so an editor
        /// test with neither in the scene can say the shell is home.
        /// </summary>
        public static Func<bool> InGameSceneSource;

        private static StoryShell _instance;

        // Set once this has taken the map away, so turning UseMap back on gives
        // back exactly what was taken and nothing else. Without it, restoring
        // would mean deciding whether the kit wanted the map up — and during a
        // level the kit wants it down.
        private static bool _mapTakenAway;

        private bool _shellDown;

        public static StoryShell Instance
        {
            get { return _instance; }
        }

        public static bool InGameScene
        {
            get
            {
                if (InGameSceneSource != null) return InGameSceneSource();
                return LevelManager.THIS != null;
            }
        }

        /// <summary>Whether any of the shell's four screens is on screen.</summary>
        public static bool AnyScreenOpen
        {
            get
            {
                return StoryHomeScreen.IsOpen || AreaScreen.IsOpen
                       || StoryAreasList.IsOpen || StoryLevelPopup.IsOpen
                       || StorySettings.IsOpen;
            }
        }

        /// <summary>
        /// Whether a story beat is on screen. The floor is not put back under a
        /// cutscene: the beat IS the screen, and opening home behind it would
        /// both log a screen the player never asked for and leave the room drawn
        /// under the one the cutscene is showing.
        /// </summary>
        public static bool StoryIsPlaying
        {
            get
            {
                return StoryPresentation.IsBusy
                       || RepairCutscene.IsPlaying
                       || OpeningSequence.IsPlaying
                       || DialogueSystem.IsPlaying;
            }
        }

        /// <summary>Whether the kit's map is on screen right now.</summary>
        public static bool MapIsUp
        {
            get
            {
                return LevelsMap._instance != null
                       && LevelsMap._instance.gameObject.activeSelf;
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
            _instance = go.AddComponent<StoryShell>();
        }

        /// <summary>
        /// Brings the shell into line with what the game is doing. Called every
        /// frame in the game, because a level starts and ends without telling
        /// this — and called directly by an editor test, which has no frame loop.
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
            // Install runs at sceneLoaded, before LevelManager.Start assigns THIS,
            // so the first pass through here is deliberately a no-op: the kit's
            // own Start still needs the map it is about to be relieved of.
            // InitScript reads a sprite off it with FindObjectOfType, which skips
            // inactive objects and would throw.
            if (!InGameScene) return;

            ApplyMap();

            if (StoryNavBar.LevelIsPlaying)
            {
                if (_shellDown) return;
                _shellDown = true;
                CloseForLevel();
                return;
            }

            _shellDown = false;
            EnsureFloor();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        #endregion

        #region The map

        /// <summary>
        /// Takes the kit's map off screen while the shell owns the entry point,
        /// and gives it back untouched when it does not.
        ///
        /// The map is deactivated rather than left running underneath, because
        /// every one of its 150 nodes polls Physics2D.OverlapPoint in Update and
        /// a node tapped through a gap in the shell would launch a level from a
        /// screen the player cannot see. The kit reactivates it itself at the end
        /// of every level (LevelManager.ShowMap), which is why this is a
        /// per-frame assertion rather than a one-off.
        /// </summary>
        private void ApplyMap()
        {
            LevelsMap map = LevelsMap._instance;
            if (map == null) return;

            if (UseMap)
            {
                // The kit owns the map again. Give back the one thing that was
                // taken, once — during a level the kit wants it off, and second
                // guessing that is how a map ends up drawn over a board.
                if (!_mapTakenAway) return;
                _mapTakenAway = false;
                map.gameObject.SetActive(true);
                Debug.Log("[Story] shell: the kit's map is the entry point again.");
                return;
            }

            if (!map.gameObject.activeSelf) return;

            map.gameObject.SetActive(false);
            _mapTakenAway = true;
            if (Application.isPlaying)
                Debug.Log("[Story] shell: kit map put away; the room is the entry point.");
        }

        #endregion

        #region Navigation

        /// <summary>
        /// Home, with everything that sorts above it closed. Home is the floor of
        /// the shell, so anything left open would be put back over the screen the
        /// player just asked for.
        /// </summary>
        public static void GoHome()
        {
            GoHome(StoryHomeScreen.CurrentRoom);
        }

        public static void GoHome(int areaId)
        {
            StorySettings.Close();
            StoryLevelPopup.Close();
            StoryAreasList.Close();
            AreaScreen.Close();
            StoryHomeScreen.Open(areaId);
        }

        /// <summary>
        /// Where the player lands when a level ends, won or lost. Royal Match
        /// comes back to the room, not to a task list: the star just earned is
        /// already counted on home's badge, and the area page is one tap away on
        /// the gold card. With the map on, the destination is what it always was.
        /// </summary>
        public static void ShowAfterLevel(int areaId)
        {
            if (UseMap)
            {
                AreaScreen.Open(areaId);
                return;
            }

            GoHome(areaId);
        }

        /// <summary>
        /// Takes the shell off screen for a level. Every shell canvas sorts above
        /// the kit's level canvas, so a screen left open is a screen drawn over
        /// the board.
        /// </summary>
        public static void CloseForLevel()
        {
            StorySettings.Close();
            StoryLevelPopup.Close();
            StoryAreasList.Close();
            AreaScreen.Close();
            StoryHomeScreen.Close();
        }

        /// <summary>
        /// Puts home back under nothing. Called from every close path in the
        /// shell: a screen that closes onto an empty canvas is a dead end, and
        /// which screen opened which is not something a red X can know.
        ///
        /// A no-op whenever anything at all is up, so calling it after a close
        /// that did have somewhere to go costs one comparison.
        /// </summary>
        public static void EnsureFloor()
        {
            if (UseMap) return;

            // The kit's menu scene has no shell to keep a floor under, and an
            // editor test case that is not about the shell must not have home
            // appear behind it because it closed a panel.
            if (!InGameScene) return;

            if (StoryNavBar.LevelIsPlaying) return;
            if (StoryIsPlaying) return;
            if (AnyScreenOpen) return;

            // Nothing to come home to yet — the database has not loaded, or this
            // is a scene with no story in it. Silent, because this is asked every
            // frame and a warning here would be a warning per frame.
            if (StoryDatabase.GetArea(StoryHomeScreen.CurrentRoom) == null) return;

            if (LaunchOpeningLevelIfDue()) return;

            StoryHomeScreen.Open();
        }

        /// <summary>Set once the opening board has been asked for, so a level left without winning does not relaunch it.</summary>
        private static bool _openingLevelLaunched;

        /// <summary>
        /// Rule zero of 00-OPENING.md: the app opens on a board and the first tap
        /// is a match. No menu to read, no room to tour, nothing to press but a
        /// gem — the story arrives after the first win, which is what
        /// OpeningSequence already enforces from the other end.
        ///
        /// So on a save with nothing beaten, the floor under the shell is level 1
        /// rather than home. Every later launch lands on the room as before,
        /// because by then something has been won.
        ///
        /// It fires at most once per process. A player who backs out of level 1
        /// without finishing it has still seen the board, and relaunching it
        /// under them would be a screen they cannot leave.
        /// </summary>
        private static bool LaunchOpeningLevelIfDue()
        {
            if (_openingLevelLaunched) return false;
            if (StoryProgress.OpeningSeen) return false;
            if (StoryProgress.LevelsBeatenCount > 0) return false;

            int areaId = StoryHomeScreen.CurrentRoom;
            int levelNumber = StoryLevelPlan.NextLevelFor(areaId);
            if (levelNumber <= 0) return false;

            // Only from a scene that can actually run one. Without this the flag
            // would be spent in the menu scene and the board never opened.
            if (LevelManager.THIS == null) return false;

            // And only once the kit has finished standing itself up. THIS is
            // assigned in LevelManager.Start, but the board, the level UI and
            // MenuManager are not ready in that same frame — LoadLevel called
            // there takes the shell down and puts nothing up, which is a black
            // screen. GameState.Map is the kit settled and waiting, which is
            // exactly the state it is in when a player taps a level normally.
            if (LevelManager.THIS.gameStatus != GameState.Map) return false;
            if (MenuManager.Instance == null) return false;

            _openingLevelLaunched = true;

            StoryFlow.LaunchLevel(levelNumber, areaId);

            // StartLoadedLevel sets PrepareGame synchronously, so still being on
            // Map means the launch did not take. The shell has already been
            // closed by LaunchLevel at that point, and returning true would
            // leave the player looking at nothing — which is the black screen
            // this whole path produced before. Fall through to home instead: a
            // room the player did not ask for beats a screen with nothing on it.
            if (LevelManager.THIS.gameStatus == GameState.Map)
            {
                Debug.LogWarning(string.Format(
                    "[Story] first run: level {0} did not start; opening the room instead.",
                    levelNumber));
                return false;
            }

            Debug.Log(string.Format(
                "[Story] first run: opening on level {0} of area {1}, not on the room.",
                levelNumber, areaId));
            return true;
        }

        #endregion
    }
}

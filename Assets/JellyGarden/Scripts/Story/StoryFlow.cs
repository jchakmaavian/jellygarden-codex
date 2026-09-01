// The seam between the story layer and the Jelly Garden kit.
//
// Everything the kit needs to ask the story is asked here, through three entry
// points called from two additive hooks in InitScript and AnimationManager.
// Each returns true when the story has taken over, so the kit's own behaviour is
// skipped rather than removed — turning the story off is deleting two if
// statements.
//
// Star awarding rides on LevelManager's existing OnWin event, so nothing in the
// board, its animations or its particle systems is touched.

using JellyGarden.Scripts.GUI;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public sealed class StoryFlow : MonoBehaviour
    {
        private static StoryFlow _instance;

        /// <summary>The area the level currently being played belongs to.</summary>
        public static int PlayingArea { get; private set; }

        /// <summary>The level number the story launched, so a win can be attributed.</summary>
        public static int PlayingLevel { get; private set; }

        private static bool _returningFromLevel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Install()
        {
            if (_instance != null) return;

            var go = new GameObject("~StoryFlow");
            go.hideFlags = HideFlags.HideAndDontSave;
            StoryObjects.Persist(go);
            _instance = go.AddComponent<StoryFlow>();
        }

        private void OnEnable()
        {
            LevelManager.OnWin += HandleWin;
        }

        private void OnDisable()
        {
            LevelManager.OnWin -= HandleWin;
        }

        #region Kit hooks

        /// <summary>
        /// Called when a map node is tapped. A node is a story area, so the area
        /// screen opens instead of the kit's pre-play popup — the area decides
        /// which level to play.
        /// </summary>
        public static bool HandleMapNodeClicked(int areaId)
        {
            if (StoryDatabase.GetArea(areaId) == null) return false;

            if (!StoryProgress.IsAreaUnlocked(areaId))
            {
                Debug.Log("[Story] Area " + areaId + " tapped but still locked.");
                ShowLockedToast(areaId);
                return true;
            }

            StoryArea area = StoryDatabase.GetArea(areaId);
            Debug.Log(string.Format("[Story] Opening area {0} ({1}): {2} tasks, {3} stars held.",
                areaId, area.Name, area.Tasks.Count, StoryProgress.StarsIn(areaId)));

            AreaScreen.Open(areaId);
            if (_instance != null) _instance.StartCoroutine(ReportOpenMenus());
            return true;
        }

        /// <summary>
        /// Lists the kit menus that are on screen a frame after the area screen
        /// opens. The story is supposed to replace the pre-play popup, not sit on
        /// top of it, and only the game can say which is happening.
        /// </summary>
        private static global::System.Collections.IEnumerator ReportOpenMenus()
        {
            yield return null;

            if (MenuManager.Instance == null) yield break;
            Transform canvas = MenuManager.Instance.GetCanvasGlobal();
            if (canvas == null) yield break;

            var open = new global::System.Text.StringBuilder("[Story] Kit menus open: ");
            bool any = false;
            foreach (Transform child in canvas)
            {
                if (!child.gameObject.activeSelf) continue;
                open.Append(child.name).Append(' ');
                any = true;
            }
            if (!any) open.Append("(none)");
            Debug.Log(open.ToString());
        }

        /// <summary>
        /// Called when the complete menu is dismissed. The kit would step to the
        /// next level number and reopen its pre-play popup; the story goes back to
        /// the area so the player can spend the star they just earned.
        /// </summary>
        public static bool HandleLevelComplete()
        {
            if (PlayingArea <= 0) return false;

            ReturnToArea();
            return true;
        }

        /// <summary>
        /// Called when the failed menu is dismissed. Same destination as a win,
        /// minus the star. No life is taken and no offer is shown.
        /// </summary>
        public static bool HandleLevelFailed()
        {
            if (PlayingArea <= 0) return false;

            ReturnToArea();
            return true;
        }

        #endregion

        #region Launching and returning

        /// <summary>Starts the given level, remembering which area it was played for.</summary>
        public static void LaunchLevel(int levelNumber, int areaId)
        {
            PlayingArea = areaId;
            PlayingLevel = levelNumber;
            _returningFromLevel = false;

            PlayerPrefs.SetInt("OpenLevel", levelNumber);
            PlayerPrefs.Save();
            InitScript.openLevel = levelNumber;

            if (LevelManager.THIS == null)
            {
                Debug.LogWarning("[Story] No LevelManager in this scene; cannot launch a level.");
                return;
            }

            // The one level where the board starts fighting back gets its line
            // first, so the new obstacle reads as Grimm rather than as difficulty.
            if (StorySabotage.PlayIntroIfDue(levelNumber, StartLoadedLevel))
                return;

            StartLoadedLevel();
        }

        private static void StartLoadedLevel()
        {
            if (LevelManager.THIS == null) return;
            LevelManager.THIS.LoadLevel();
            LevelManager.THIS.gameStatus = GameState.PrepareGame;
        }

        private static void ReturnToArea()
        {
            int areaId = PlayingArea;
            _returningFromLevel = true;
            PlayingArea = 0;
            PlayingLevel = 0;

            if (LevelManager.THIS != null)
                LevelManager.THIS.gameStatus = GameState.Map;

            if (MenuManager.Instance != null)
                MenuManager.Instance.ShowMenuPlay(false);

            // Gameplay before story: the opening is owed after the first win,
            // never before it, so this is the only place it can play.
            if (OpeningSequence.PlayIfDue(areaId, () => OpenAfterLevel(areaId)))
                return;

            OpenAfterLevel(areaId);
        }

        private static void OpenAfterLevel(int areaId)
        {
            // The clue and the arc beat are owed from the last repair, not from
            // the level; if one is pending it plays before the area screen so the
            // player is not looking at a task list during a story beat.
            if (StoryPresentation.PlayPending(areaId, () => AreaScreen.Open(areaId)))
                return;

            AreaScreen.Open(areaId);
        }

        public static bool CameBackFromLevel
        {
            get { return _returningFromLevel; }
        }

        #endregion

        #region Star awarding

        private void HandleWin()
        {
            if (PlayingArea <= 0 || LevelManager.THIS == null) return;

            int levelNumber = LevelManager.THIS.currentLevel;

            // Replaying a level the player has already beaten pays nothing. The
            // level itself is still fully playable — retry is free and unlimited —
            // it just cannot be farmed for repair stars.
            if (StoryProgress.IsLevelBeaten(levelNumber))
            {
                Debug.Log(string.Format(
                    "[Story] Level {0} replayed; no star awarded.", levelNumber));
                return;
            }

            StoryProgress.MarkLevelBeaten(levelNumber);
            StoryProgress.AddStar(PlayingArea);
            StorySfx.PlayStarEarned();

            // Boosters are earned, never sold. Winning is the price.
            StoryFreeGame.GrantBoosterForWin(levelNumber);

            Debug.Log(string.Format("[Story] Level {0} won: area {1} now holds {2} star(s).",
                levelNumber, PlayingArea, StoryProgress.StarsIn(PlayingArea)));
        }

        #endregion

        private static void ShowLockedToast(int areaId)
        {
            StoryArea previous = StoryDatabase.GetArea(areaId - 1);
            string message = previous != null
                ? string.Format("Restore the {0} first.", previous.Name.ToLowerInvariant())
                : "Not yet.";

            if (AreaScreen.Instance != null && AreaScreen.IsOpen)
                AreaScreen.Instance.Toast(message);
            else
                StoryToast.Show(message);
        }
    }
}

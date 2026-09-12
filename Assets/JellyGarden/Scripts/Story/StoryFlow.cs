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
            LevelManager.OnLose += HandleLose;
        }

        private void OnDisable()
        {
            LevelManager.OnWin -= HandleWin;
            LevelManager.OnLose -= HandleLose;
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

            // Every shell canvas sorts above the kit's level canvas, so the shell
            // comes down before the board goes up rather than a frame later when
            // StoryShell's own Update notices.
            StoryShell.CloseForLevel();

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

            // LoadLevel rewrites star1..star3 from the level file, so the single
            // star has to be re-applied here rather than once when the scene
            // loaded.
            StoryOneStar.Forget();
            StoryOneStar.Apply();

            LevelManager.THIS.gameStatus = GameState.PrepareGame;
        }

        private static void ReturnToArea()
        {
            int areaId = PlayingArea;
            _returningFromLevel = true;
            PlayingArea = 0;
            PlayingLevel = 0;

            // The win panel closes itself when its own Continue is pressed, but
            // that is only one of the doors out of a level: anything else that
            // ends one — the playtest, a retry, a beat that leaves early — left
            // the panel standing on the persistent canvas, over whatever came
            // next. It survived the next LaunchLevel too, so from the second
            // level of a run the player was looking at the previous level's
            // result with a live board playing underneath it. Leaving the level
            // is what closes it, whichever door was used.
            //
            // The kit's own two end panels go with them: the story panels stand
            // in for those, and one left active is a second result screen behind
            // the first.
            StoryWinPopup.Close();
            StoryLosePopup.Close();
            StoryLosePopup.HideKitPanel();

            // The power plate is a second of a level's opening; leaving the
            // level part-way through one would otherwise leave it on the area
            // screen that comes next.
            StoryPrePlay.Close();

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
            // the level; if one is pending it plays before the shell comes back so
            // the player is not looking at a task list during a story beat.
            //
            // Where the level ends is the shell's decision, not this one's: with
            // the map on it is the area page as it always was, and with the shell
            // on it is home, which is what Royal Match comes back to.
            if (StoryPresentation.PlayPending(areaId, () => StoryShell.ShowAfterLevel(areaId)))
                return;

            StoryShell.ShowAfterLevel(areaId);
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
                ShowWinPanel(levelNumber, false);
                return;
            }

            StoryProgress.MarkLevelBeaten(levelNumber);
            StoryProgress.AddStar(PlayingArea);
            StorySfx.PlayStarEarned();

            // Boosters are earned, never sold. Winning is the price.
            StoryFreeGame.GrantBoosterForWin(levelNumber);

            Debug.Log(string.Format("[Story] Level {0} won: area {1} now holds {2} star(s).",
                levelNumber, PlayingArea, StoryProgress.StarsIn(PlayingArea)));

            ShowWinPanel(levelNumber, true);
        }

        /// <summary>
        /// Puts the story's own level-complete panel up in place of the kit's.
        ///
        /// It is opened from here rather than from LevelManager's OnMenuComplete
        /// because of the order the kit raises those two: OnMenuComplete fires at
        /// the top of the Win branch and OnWin — which is what called this —
        /// twenty lines below it, so a panel opened on OnMenuComplete would read
        /// the area's totals from before this method added to them.
        ///
        /// Continue goes through HandleLevelComplete, the same door the kit's own
        /// panel used, so nothing about where a level ends changes here.
        /// </summary>
        private static void ShowWinPanel(int levelNumber, bool paidStar)
        {
            int areaId = PlayingArea;
            StoryWinPopup.Open(levelNumber, areaId, paidStar,
                () => HandleLevelComplete());
        }

        /// <summary>
        /// Puts the story's own level-failed panel up in place of the kit's.
        ///
        /// The kit raises OnLose in the same breath as ShowMenuFailed(true), so
        /// unlike the win panel there is nothing to wait for: no total changes on
        /// a loss. Continue goes through HandleLevelFailed, the same door the
        /// kit's own Give Up used.
        /// </summary>
        private void HandleLose()
        {
            if (PlayingArea <= 0 || LevelManager.THIS == null) return;

            int levelNumber = LevelManager.THIS.currentLevel;
            int areaId = PlayingArea;
            bool outOfTime = LevelManager.THIS.limitType == LIMIT.TIME;

            Debug.Log(string.Format("[Story] Level {0} lost; no star awarded.", levelNumber));

            StoryLosePopup.Open(levelNumber, areaId, outOfTime,
                () => HandleLevelFailed());
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

// Brings the story layer up before anything in a scene runs.
//
// Order matters: LevelsMap.OnEnable calls Reset(), which asks the progress
// manager whether each node is locked. If the story manager is not installed by
// then the map draws one frame of the kit's own PlayerPrefs progress before
// correcting itself. RuntimeInitializeOnLoadMethod(BeforeSceneLoad) runs before
// any Awake in the first scene, which is early enough.

using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryBoot
    {
        private static bool _booted;

        public static bool IsBooted
        {
            get { return _booted; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Boot()
        {
            if (_booted) return;
            _booted = true;

            StoryDatabase.Load();
            StoryProgress.Load();
            StoryLevelPlan.Load();
            CutsceneRunner.Load();
            StoryMapProgressManager.Install();
            StoryFreeGame.GrantStarterBoosters();
            // MusicBase does not exist yet at BeforeSceneLoad, so the theme swap
            // installs itself once a scene is up.

            Debug.Log(string.Format(
                "[Story] Booted. Area {0} of {1}, {2} levels beaten.",
                StoryProgress.CurrentArea, StoryDatabase.AreaCount,
                StoryProgress.LevelsBeatenCount));
        }

        /// <summary>Editor and test hook — lets a headless run boot explicitly.</summary>
        public static void BootForTests()
        {
            _booted = false;
            Boot();
        }
    }
}

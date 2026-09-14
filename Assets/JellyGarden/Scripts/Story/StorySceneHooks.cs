// Installs the per-scene parts of the story layer every time a scene loads.
//
// RuntimeInitializeOnLoadMethod runs ONCE, at startup, in whichever scene the
// app opens with. This game opens on `main` (the menu) and loads `game` (the
// map and the board) afterwards, so anything that needs the map or the board to
// exist found nothing and quietly did nothing. That is why the map shipped
// without its area names.
//
// StoryBoot still does the once-per-process work — loading data, installing the
// progress manager. This does the once-per-scene work.

using UnityEngine;
using UnityEngine.SceneManagement;

namespace JellyGarden.Scripts.Story
{
    public static class StorySceneHooks
    {
        private static bool _subscribed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;

            SceneManager.sceneLoaded += OnSceneLoaded;

            // The scene that was already up when this ran still needs the pass.
            InstallForCurrentScene();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            InstallForCurrentScene();
        }

        private static void InstallForCurrentScene()
        {
            // Each of these is a no-op when the thing it needs is not in the
            // scene, so this is safe to run on the menu as well as the game.
            // Not an Install — a clear. StoryPunchVfx holds a set of the gems
            // currently springing, and a level unloaded mid-cascade leaves
            // destroyed Items in it. Unity's fake-null makes those entries
            // neither removable by the coroutine (it never resumes) nor equal to
            // any live Item, so the set would grow by a few entries per level
            // for the whole session. Dropping it here costs nothing: a scene
            // load means no spring can still be in flight.
            StoryPunchVfx.Reset();

            // Same reason: a level unloaded mid-blast leaves pooled pieces
            // marked alive and blasts holding references to them, and neither
            // the coroutine nor the frame loop that would have retired them
            // ever runs again. A scene load means nothing can still be in
            // flight, so dropping the lot is free.
            StoryBombVfx.Reset();
            StoryColorBombVfx.Reset();

            StorySplash.Install();
            StoryMusic.Install();
            StoryLevelFullBleed.Install();
            StoryBoardTheme.Install();
            StoryBoardCentre.Install();
            StoryPixelSnap.Install();
            StoryMapLabels.Install();
            StoryOneStar.Install();
            StoryLevelHud.Install();
            StoryWinBanner.Install();
            StorybookKitMenus.Install();
            StoryDeadBoard.Install();
            StoryBoosters.Install();
            StoryNavBar.Install();
            StoryAreasList.Install();
            // Last of the shell: it decides what is on screen, so the screens it
            // decides between have to have installed their seams first.
            StoryShell.Install();
            StoryPowerupTest.Install();
            StoryExplosionProbe.Install();
            StoryVfxLab.Install();
            // Last of all: it waits for the board self-test above to have had its
            // run before it opens a screen of its own.
            StoryShellTest.Install();
        }
    }
}

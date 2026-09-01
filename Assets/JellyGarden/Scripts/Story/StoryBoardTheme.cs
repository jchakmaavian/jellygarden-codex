// Dresses the board for the arc the player is in.
//
// Two things change, both of them safe:
//
//   the backdrop behind the board — Background picks one of five shipped
//   pictures by currentLevel / 20, which with 1209 levels means one picture for
//   ninety per cent of the game; this picks by arc instead
//
//   the tint on the board's own cells — a colour on the existing sprites, so
//   every animation, particle and material stays exactly as it was
//
// The gems are deliberately left alone; see the note in StoryTheme.

using JellyGarden.Scripts.GUI;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    [DisallowMultipleComponent]
    public sealed class StoryBoardTheme : MonoBehaviour
    {
        private static StoryBoardTheme _instance;

        private int _themedLevel = -1;

        /// <summary>Called by StorySceneHooks on every scene load; a no-op when this scene has no map or board.</summary>
        public static void Install()
        {
            if (_instance != null) return;

            var go = new GameObject("~StoryBoardTheme");
            go.hideFlags = HideFlags.HideAndDontSave;
            StoryObjects.Persist(go);
            _instance = go.AddComponent<StoryBoardTheme>();
        }

        private void OnEnable()
        {
            LevelManager.OnLevelLoaded += Apply;
            LevelManager.OnStartPlay += Apply;
        }

        private void OnDisable()
        {
            LevelManager.OnLevelLoaded -= Apply;
            LevelManager.OnStartPlay -= Apply;
        }

        /// <summary>Applies the current level's arc theme. Cheap and idempotent.</summary>
        public void Apply()
        {
            if (LevelManager.THIS == null) return;

            int levelNumber = LevelManager.THIS.currentLevel;
            ArcTheme theme = StoryTheme.ForLevel(levelNumber);
            if (theme == null) return;

            ApplyBackdrop(theme);
            ApplySquareTint(theme);
            _themedLevel = levelNumber;
        }

        /// <summary>
        /// Overrides Background's level-bucket choice with the arc's. A story
        /// backdrop is used when one exists; otherwise the kit's own picture that
        /// suits the arc's setting.
        /// </summary>
        private static void ApplyBackdrop(ArcTheme theme)
        {
            var background = Object.FindFirstObjectByType<Background>(FindObjectsInactive.Include);
            if (background == null) return;

            var image = background.GetComponent<Image>();
            if (image == null) return;

            Sprite story = StoryArt.Load(theme.BoardBackdrop);
            if (story != null)
            {
                image.sprite = story;
                return;
            }

            Sprite[] pictures = background.pictures;
            if (pictures == null || pictures.Length == 0) return;

            int index = Mathf.Clamp(theme.KitBackdropIndex, 0, pictures.Length - 1);
            image.sprite = pictures[index];
        }

        /// <summary>
        /// Tints the board's cells. A tint only — the sprite, the material, the
        /// sorting and every animation on the square are untouched, and the gems
        /// sitting on top keep their own colour.
        /// </summary>
        private static void ApplySquareTint(ArcTheme theme)
        {
            if (LevelManager.THIS == null || LevelManager.THIS.squaresArray == null) return;

            foreach (Square square in LevelManager.THIS.squaresArray)
            {
                if (square == null) continue;

                var renderer = square.GetComponent<SpriteRenderer>();
                if (renderer == null) continue;

                // Only the cell itself. A blocker sitting on it is a story object
                // in its own right and keeps its authored colour.
                renderer.color = theme.SquareTint;
            }
        }

        private void LateUpdate()
        {
            // The board is rebuilt on retry and on a new level without always
            // firing OnLevelLoaded, so a cheap check catches the rest.
            if (LevelManager.THIS == null) return;
            if (LevelManager.THIS.currentLevel == _themedLevel) return;
            Apply();
        }
    }
}

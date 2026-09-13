// The gear in the top bar, and what it opens.
//
// The gear has been drawn since the top bar was built and has never done
// anything — `StoryUi.TopBar` takes an `onSettings` and both screens passed
// null. This is the panel behind it.
//
// It holds one option, because there is one thing worth doing here: wipe the
// save and start the game over. Everything else a settings screen usually
// carries — sound, music, language, support — belongs to the kit or does not
// exist yet, and a panel of dead toggles is worse than a panel with one live
// one.
//
// Resetting is destructive and cannot be undone, so it asks first. The panel
// swaps its own body rather than opening a second panel over itself: a
// confirmation that arrives on the screen you are already reading is harder to
// dismiss by accident than one that appears under your thumb.

using System;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed class StorySettings : MonoBehaviour
    {
        public const string HostName = "StorySettings";

        private static StorySettings _instance;

        private Canvas _canvas;
        private StoryUi.RoyalPanelParts _panel;
        private Text _blurb;
        private Button _action;
        private Text _actionLabel;
        private Action _onClose;

        /// <summary>True while the panel is asking whether the player means it.</summary>
        private bool _confirming;

        public static bool IsOpen
        {
            get { return _instance != null && _instance.gameObject.activeSelf; }
        }

        /// <summary>
        /// Where a reset lands the player. Null is StoryShell.GoHome, which is
        /// what the game passes; a test swaps it to prove the wipe happened
        /// without a shell in the scene.
        /// </summary>
        public static Action Restarted;

        #region Lifecycle

        public static void Open(Action onClose)
        {
            if (_instance == null)
            {
                var go = new GameObject(HostName);
                StoryObjects.Persist(go);
                _instance = go.AddComponent<StorySettings>();
                _instance.Build();
            }

            _instance._onClose = onClose;
            _instance.gameObject.SetActive(true);

            // Always opens on the question, never on the confirmation a previous
            // visit was left sitting on.
            _instance.ShowMenu();
        }

        public static void Close()
        {
            if (_instance != null) _instance.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        #endregion

        #region Build

        private void Build()
        {
            // Above home (300) and the area page (400), below the level popup
            // (600) — the gear is reachable from the screens the shell shows,
            // and never over a board.
            _canvas = StoryUi.CreateOverlayCanvas("SettingsCanvas", 560);
            _canvas.transform.SetParent(transform, false);

            // Eats the taps that miss the panel without closing it, so a tap
            // beside "Reset Progress" cannot dismiss the question.
            StoryUi.CreateFullScreenButton("Scrim", _canvas.transform,
                new Color(0f, 0f, 0f, 0.45f), null);

            RectTransform band = StoryUi.CreateRect("Band", _canvas.transform);
            band.anchorMin = Vector2.zero;
            band.anchorMax = Vector2.one;
            band.offsetMin = new Vector2(0f, 520f);
            band.offsetMax = new Vector2(0f, -520f);

            if (StorybookUi.Enabled)
            {
                band.anchorMin = new Vector2(0, .25f);
                band.anchorMax = new Vector2(1, .75f);
                band.offsetMin = band.offsetMax = Vector2.zero;
            }
            _panel = StoryUi.RoyalPanel("Panel", band, "Settings", OnClose);

            _blurb = StoryUi.CreateText("Blurb", _panel.Content, string.Empty, 46,
                StorybookUi.Enabled ? StorybookUi.Ink : StoryUi.RoyalTitle, TextAnchor.UpperCenter);
            _blurb.rectTransform.anchorMin = new Vector2(0.06f, 0.42f);
            _blurb.rectTransform.anchorMax = new Vector2(0.94f, 0.92f);
            _blurb.rectTransform.offsetMin = Vector2.zero;
            _blurb.rectTransform.offsetMax = Vector2.zero;
            _blurb.resizeTextForBestFit = true;

            _action = StoryUi.GreenButton("Action", _panel.Content, "Reset Progress", 56, OnAction);
            RectTransform rect = _action.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.10f);
            rect.anchorMax = new Vector2(0.5f, 0.30f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(620f, 0f);
            rect.anchoredPosition = Vector2.zero;

            // The green button is three layers and its label is a child, so it is
            // found once here rather than looked up on every state change.
            _actionLabel = _action.GetComponentInChildren<Text>(true);

            gameObject.SetActive(false);
        }

        #endregion

        #region The one option

        private void ShowMenu()
        {
            _confirming = false;
            _blurb.text = "Start the game over from Area 1.\n\nEvery star, repair and story beat is forgotten.";
            SetActionLabel("Reset Progress");
        }

        private void ShowConfirm()
        {
            _confirming = true;
            _blurb.text = "This cannot be undone.\n\nTap again to wipe your progress, or close to keep it.";
            SetActionLabel("Yes, wipe it");
        }

        private void SetActionLabel(string label)
        {
            if (_actionLabel != null) _actionLabel.text = label;
        }

        private void OnAction()
        {
            if (!_confirming)
            {
                ShowConfirm();
                return;
            }

            Reset();
        }

        /// <summary>
        /// Wipes both halves of what the player has done. The story's own save is
        /// JSON in persistentDataPath; the kit keeps its levels, coins and lives
        /// in PlayerPrefs, and a reset that clears one leaves the game claiming
        /// area 1 is untouched while the map still shows twenty levels beaten.
        ///
        /// StoryProgress.ResetAll clears the in-memory statics as well as the
        /// file, which matters more than it looks: with Disable Domain Reload on
        /// — this project's setting — deleting the file alone changes nothing,
        /// because Load() early-returns on a cache that never emptied.
        /// </summary>
        private void Reset()
        {
            StoryProgress.ResetAll();

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            Debug.Log("[Story] settings: progress wiped; the game starts over at area 1.");

            Close();

            if (Restarted != null) { Restarted(); return; }

            StoryShell.GoHome(1);
        }

        private void OnClose()
        {
            Close();

            Action done = _onClose;
            _onClose = null;
            if (done != null) done();

            StoryShell.EnsureFloor();
        }

        #endregion
    }
}

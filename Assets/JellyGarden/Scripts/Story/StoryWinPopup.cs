// The level-complete panel, the way Royal Match draws it.
//
// The kit's own panel is a wooden board with three star sockets, the word
// "Score" and a number — see the note in StoryOneStar for why three stars is
// the wrong count in this game. Every level here pays exactly ONE star into the
// area's pool, and the score is a pass mark rather than a grade, so the kit's
// panel states two things that are not true of this game and omits the one that
// is: where the star went.
//
// So the panel says: the level, the star, and what that star moved.
//
//     ┌──────────── Level 3 ────────────┐
//     │              ★                  │   the star just earned
//     │           +1 Star               │
//     │  ┌───────────────────────────┐  │
//     │  │ Castle Gates              │  │   the area it paid into
//     │  │ ▓▓▓▓▓░░░░░  2/4        🧰 │  │   repairs bought / owed
//     │  └───────────────────────────┘  │
//     │           [ Continue ]          │
//     └─────────────────────────────────┘
//
// Panel language measured off docs/royal-match/05-earn-stars.jpg: an arched
// blue plate with a double gold rim, cream title with a dark outline, a body in
// the same blue, a caption in a sunken darker pill, and the one green button.
// That capture is the tutorial that explains stars, and it draws exactly one
// star for exactly this reason.
//
// No close X. A win has one way out, and it is the green button — an X beside
// it would ask the player to choose between two doors into the same room.
//
// The kit's panel is deactivated, never deleted. StoryWinPopup.Enabled = false
// gives it straight back.

using System;
using JellyGarden.Scripts.GUI;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed class StoryWinPopup : MonoBehaviour
    {
        /// <summary>The whole feature, in one switch.</summary>
        public static bool Enabled = true;

        public const string HostName = "StoryWinPopup";

        /// <summary>The name the kit gives the panel this one stands in for.</summary>
        private const string KitPanelName = "MenuComplete";

        private static StoryWinPopup _instance;

        public static bool IsOpen
        {
            get { return _instance != null && _instance.gameObject.activeSelf; }
        }

        #region Metrics

        // Vertical measurements are fractions of the panel's own content height,
        // never pixels: the reference is a 9:19.5 phone and the Editor's Game
        // view is 4:3, and a canvas at match 0.5 hands the two very different
        // amounts of reference height. A column of fixed pixel heights fits one
        // and overlaps on the other, and no editor test can see it. Horizontal
        // measurements stay in reference points, because the scaler keeps the
        // width honest. Same rule as StoryLevelPopup — see the note there.
        private const float DesignHeight = 1146f;

        /// <summary>The panel band. Home stays visible above and below it.</summary>
        private const float PopupTop = 330f;
        private const float PopupBottom = 330f;

        private const float StarTopFrac = 60f / DesignHeight;
        private const float StarHeightFrac = 340f / DesignHeight;

        private const float CaptionTopFrac = 30f / DesignHeight;
        private const float CaptionHeightFrac = 110f / DesignHeight;
        private const float CaptionSide = 150f;

        private const float CardTopFrac = 44f / DesignHeight;
        private const float CardHeightFrac = 250f / DesignHeight;
        private const float CardSide = 40f;

        private const float ButtonWidth = 574f;
        private const float ButtonHeightFrac = 209f / DesignHeight;
        private const float ButtonBottomFrac = 60f / DesignHeight;

        #endregion

        #region Parts

        private Canvas _canvas;
        private StoryUi.RoyalPanelParts _panel;
        private Image _star;
        private Image _starGlow;
        private Text _caption;
        private Text _areaName;
        private StoryUi.ProgressBarParts _progress;

        private int _level;
        private int _areaId;
        private bool _paidStar;
        private Action _onContinue;

        /// <summary>What the caption reads. Read by tests.</summary>
        public static string CaptionText
        {
            get { return _instance == null || _instance._caption == null ? null : _instance._caption.text; }
        }

        /// <summary>What the title plate reads. Read by tests.</summary>
        public static string TitleText
        {
            get
            {
                return _instance == null || _instance._panel == null || _instance._panel.Title == null
                    ? null : _instance._panel.Title.text;
            }
        }

        #endregion

        #region Opening

        /// <summary>
        /// Shows the panel for a level just won.
        ///
        /// Called by StoryFlow at the end of its own win handling rather than
        /// off LevelManager's OnMenuComplete, and the ordering is the reason:
        /// the kit raises OnMenuComplete at the top of the Win branch and OnWin
        /// twenty lines further down, so a panel opened on OnMenuComplete would
        /// draw the area's star total from before the star was added to it.
        /// </summary>
        public static void Open(int levelNumber, int areaId, bool paidStar, Action onContinue)
        {
            if (!Enabled)
            {
                // The kit's panel was left standing, so its own Continue is what
                // carries the player on and there is nothing to do here.
                return;
            }

            HideKitPanel();

            if (_instance == null)
            {
                var go = new GameObject(HostName);
                StoryObjects.Persist(go);
                _instance = go.AddComponent<StoryWinPopup>();
                _instance.Build();
            }

            _instance._level = levelNumber;
            _instance._areaId = areaId;
            _instance._paidStar = paidStar;
            _instance._onContinue = onContinue;
            _instance.gameObject.SetActive(true);
            _instance.Refresh();

            Debug.Log(string.Format(
                "[Story] win panel: level {0}, area {1}, {2}.",
                levelNumber, areaId, paidStar ? "one star paid" : "replay, no star"));
        }

        public static void Close()
        {
            if (_instance != null) _instance.gameObject.SetActive(false);
        }

        /// <summary>
        /// Takes the kit's wooden panel off the screen.
        ///
        /// Deactivated directly rather than through MenuManager.ShowMenuComplete
        /// (false): that routes into AnimationManager.CloseMenu, which is where
        /// the story's own "a level ended" hook lives, and closing it that way
        /// would send the player home the instant this panel opened.
        /// </summary>
        private static void HideKitPanel()
        {
            foreach (AnimationManager menu in
                     UnityEngine.Object.FindObjectsByType<AnimationManager>(FindObjectsInactive.Include))
            {
                if (menu != null && menu.gameObject.name == KitPanelName)
                    menu.gameObject.SetActive(false);
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
            _canvas = StoryUi.CreateOverlayCanvas("WinPopupCanvas", 620);
            _canvas.transform.SetParent(transform, false);

            // A scrim, so the board behind cannot be tapped while the panel is
            // up and the panel reads as being in front of something.
            Image scrim = StoryUi.CreateImage("Scrim", _canvas.transform, null, StoryUi.Dim);
            StoryUi.Stretch(scrim.rectTransform);
            scrim.raycastTarget = true;

            RectTransform band = StoryUi.CreateRect("Band", _canvas.transform);
            band.anchorMin = Vector2.zero;
            band.anchorMax = Vector2.one;
            band.offsetMin = new Vector2(0f, PopupBottom);
            band.offsetMax = new Vector2(0f, -PopupTop);

            // No close X: the green button is the only way out of a win.
            _panel = StoryUi.RoyalPanel("Panel", band, "Level", null);

            BuildStar();
            BuildCaption();
            BuildAreaCard();
            BuildContinue();
        }

        private void BuildStar()
        {
            RectTransform slot = StoryUi.CreateRect("StarSlot", _panel.Content);
            FracBand(slot, StarTopFrac, StarHeightFrac);

            // A soft disc behind the star, so a gold shape on a blue panel has
            // something to sit in rather than floating on flat colour.
            _starGlow = StoryUi.CreateImage("Glow", slot, StoryUi.CircleSprite,
                new Color(1f, 0.85f, 0.35f, 0.16f));
            Square(_starGlow.rectTransform, 1.15f);

            _star = StoryUi.CreateImage("Star", slot, StoryUi.StarSprite, Color.white);
            Square(_star.rectTransform, 1f);
            _star.preserveAspect = true;
        }

        private void BuildCaption()
        {
            RectTransform pill = StoryUi.CreateRect("Caption", _panel.Content);
            FracBand(pill, StarTopFrac + StarHeightFrac + CaptionTopFrac, CaptionHeightFrac,
                CaptionSide);

            Image fill = StoryUi.CreateImage("Fill", pill, StoryUi.RoundedSprite(30),
                StoryUi.RoyalBlueDeep);
            StoryUi.Stretch(fill.rectTransform);
            fill.type = Image.Type.Sliced;

            _caption = StoryUi.CreateText("Label", pill, "", 52, StoryUi.RoyalTitle,
                TextAnchor.MiddleCenter);
            StoryUi.Stretch(_caption.rectTransform, 14f);
            _caption.fontStyle = FontStyle.Bold;
        }

        private void BuildAreaCard()
        {
            Image card = StoryUi.CreamCard("AreaCard", _panel.Content, 26);
            FracBand(card.rectTransform,
                StarTopFrac + StarHeightFrac + CaptionTopFrac + CaptionHeightFrac + CardTopFrac,
                CardHeightFrac, CardSide);

            // The name gets the card's full width: 150 area names run from
            // "Castle Gates" to a good deal longer, and a best-fit Text wraps
            // before it shrinks, so a narrowed rect turns a two-word name into
            // two lines that overflow the card.
            _areaName = StoryUi.CreateText("AreaName", card.rectTransform, "", 46,
                StoryUi.RoyalNavy, TextAnchor.MiddleCenter);
            RectTransform nameRect = _areaName.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 0.52f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = new Vector2(28f, 0f);
            nameRect.offsetMax = new Vector2(-28f, -14f);
            _areaName.fontStyle = FontStyle.Bold;
            _areaName.resizeTextForBestFit = true;

            _progress = StoryUi.ProgressBar("Repairs", card.rectTransform);
            RectTransform barRect = _progress.Root;
            barRect.anchorMin = new Vector2(0f, 0f);
            barRect.anchorMax = new Vector2(1f, 0.52f);
            barRect.offsetMin = new Vector2(28f, 18f);
            barRect.offsetMax = new Vector2(-28f, -4f);
        }

        private void BuildContinue()
        {
            Button button = StoryUi.GreenButton("Continue", _panel.Content, "Continue", 64,
                OnContinue);

            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(ButtonWidth, 0f);

            // Height as a fraction, placed from the bottom of the content rect.
            rect.anchorMin = new Vector2(0.5f, ButtonBottomFrac);
            rect.anchorMax = new Vector2(0.5f, ButtonBottomFrac + ButtonHeightFrac);
            rect.offsetMin = new Vector2(-ButtonWidth * 0.5f, 0f);
            rect.offsetMax = new Vector2(ButtonWidth * 0.5f, 0f);
        }

        /// <summary>A band of the content rect, measured downward from its top.</summary>
        private static void FracBand(RectTransform rect, float topFrac, float heightFrac,
            float side = 0f)
        {
            rect.anchorMin = new Vector2(0f, 1f - (topFrac + heightFrac));
            rect.anchorMax = new Vector2(1f, 1f - topFrac);
            rect.offsetMin = new Vector2(side, 0f);
            rect.offsetMax = new Vector2(-side, 0f);
        }

        /// <summary>A centred square sized off its parent's height.</summary>
        private static void Square(RectTransform rect, float scale)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            var fitter = rect.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            fitter.aspectRatio = 1f;
            rect.anchorMin = new Vector2(0.5f, 0.5f - 0.5f * scale);
            rect.anchorMax = new Vector2(0.5f, 0.5f + 0.5f * scale);
            rect.offsetMin = new Vector2(rect.offsetMin.x, 0f);
            rect.offsetMax = new Vector2(rect.offsetMax.x, 0f);
        }

        #endregion

        #region Filling it in

        private void Refresh()
        {
            if (_panel != null && _panel.Title != null)
                _panel.Title.text = "Level " + _level;

            if (_caption != null)
            {
                // A replay is still a win and still says so; it just has no star
                // to show for it, and saying "+1 Star" would be a lie the area's
                // own counter contradicts one screen later.
                _caption.text = _paidStar ? "+1 Star" : "Already beaten";
            }

            if (_star != null) _star.color = _paidStar ? Color.white : StoryUi.RoyalGrey;
            if (_starGlow != null) _starGlow.enabled = _paidStar;

            StoryArea area = StoryDatabase.GetArea(_areaId);
            if (_areaName != null)
                _areaName.text = area != null ? area.Name : string.Empty;

            if (_progress != null)
            {
                StoryUi.SetProgress(_progress,
                    StoryProgress.TasksCompletedIn(_areaId),
                    area != null ? area.Tasks.Count : 0);
            }
        }

        private void OnContinue()
        {
            Action go = _onContinue;
            Close();
            if (go != null) go();
        }

        #endregion
    }
}

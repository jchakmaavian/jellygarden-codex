// The level-failed panel, the way Royal Match draws it.
//
// This is StoryWinPopup's other half, and it was the last kit screen still on
// show. The kit's own MenuFailed is a grey diagonal-hatch card that says "Level
// Failed!", offers five more moves for twelve gems and then a "Give Up" button
// with a heart on it. Three things there are not true of this game: nothing is
// for sale (StoryBoosters — boosters are earned, winning is the price), there
// are no lives to spend, and the panel is not in the story's visual language at
// all. A player saw it on every level they lost, which in arc 1 is most of them
// the first time through.
//
// So the panel says: the level, that no star was earned, and where the player
// stands in the area they were playing for.
//
//     ┌──────────── Level 5 ────────────┐
//     │              ☆                  │   the star NOT earned, grey
//     │         Out of moves            │   or "Out of time" on a timed level
//     │  ┌───────────────────────────┐  │
//     │  │ Outer Courtyard           │  │   the area it was played for
//     │  │ ▓▓▓░░░░░  1/5 repairs     │  │
//     │  └───────────────────────────┘  │
//     │           [ Continue ]          │
//     └─────────────────────────────────┘
//
// Every measurement, colour and shape is StoryWinPopup's, deliberately: a win
// and a loss are the same moment in the game's shape and reading them as two
// different screens is how a panel ends up feeling borrowed. The two differ in
// exactly what the game differs in — the star is grey and unlit, and the
// caption names what ran out.
//
// One green button, no close X, for the same reason the win panel has one: a
// level that has ended has one way out. Retry is not offered here because the
// story has no retry door — the level is reached from home, where it still is,
// and the kit's own Give Up went to exactly the same place.
//
// The kit's panel is deactivated, never deleted. StoryLosePopup.Enabled = false
// gives it straight back.

using System;
using JellyGarden.Scripts.GUI;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed class StoryLosePopup : MonoBehaviour
    {
        /// <summary>The whole feature, in one switch.</summary>
        public static bool Enabled = true;

        public const string HostName = "StoryLosePopup";

        /// <summary>The name the kit gives the panel this one stands in for.</summary>
        private const string KitPanelName = "MenuFailed";

        /// <summary>
        /// The kit's "buy five more moves" banner, which slides in ahead of the
        /// failed panel. It is the same offer in a different frame and there is
        /// nothing to buy it with, so it goes away with the panel it belongs to.
        /// </summary>
        private const string KitBannerName = "PreFailed";

        private static StoryLosePopup _instance;

        public static bool IsOpen
        {
            get { return _instance != null && _instance.gameObject.activeSelf; }
        }

        #region Metrics

        // StoryWinPopup's, to the point. See the note at the top of that file for
        // why every vertical measurement is a fraction of the panel's own content
        // height rather than a pixel count.
        private const float DesignHeight = 1146f;

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
        private Text _caption;
        private Text _areaName;
        private StoryUi.ProgressBarParts _progress;

        private int _level;
        private int _areaId;
        private bool _outOfTime;
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
        /// Shows the panel for a level just lost.
        ///
        /// Called off LevelManager.OnLose, which the kit raises in the same
        /// breath as ShowMenuFailed(true). There is no ordering problem here of
        /// the kind the win panel has — nothing about a loss changes a total —
        /// so the hook is the plain one.
        /// </summary>
        public static void Open(int levelNumber, int areaId, bool outOfTime, Action onContinue)
        {
            if (!Enabled)
            {
                // The kit's panel was left standing, so its own Give Up is what
                // carries the player on and there is nothing to do here.
                return;
            }

            HideKitPanel();

            // The HUD is still behind the panel, so the king is on screen to take
            // the news. Fired before the panel is built, which is when the strip
            // is last drawn undimmed.
            StoryAvatar.React("slump");

            if (_instance == null)
            {
                var go = new GameObject(HostName);
                StoryObjects.Persist(go);
                _instance = go.AddComponent<StoryLosePopup>();
                _instance.Build();
            }

            _instance._level = levelNumber;
            _instance._areaId = areaId;
            _instance._outOfTime = outOfTime;
            _instance._onContinue = onContinue;
            _instance.gameObject.SetActive(true);
            _instance.Refresh();

            Debug.Log(string.Format(
                "[Story] fail panel: level {0}, area {1}, {2}.",
                levelNumber, areaId, outOfTime ? "out of time" : "out of moves"));
        }

        public static void Close()
        {
            if (_instance != null) _instance.gameObject.SetActive(false);
        }

        /// <summary>
        /// Takes the kit's failed card and its buy-more-moves banner off the
        /// screen.
        ///
        /// Deactivated directly rather than through MenuManager.ShowMenuFailed
        /// (false): that routes into AnimationManager.CloseMenu, which is where
        /// the story's own "a level ended" hook lives, and closing it that way
        /// would send the player home the instant this panel opened.
        /// </summary>
        public static void HideKitPanel()
        {
            foreach (AnimationManager menu in
                     UnityEngine.Object.FindObjectsByType<AnimationManager>(FindObjectsInactive.Include))
            {
                if (menu == null) continue;
                string name = menu.gameObject.name;
                if (name == KitPanelName || name == KitBannerName)
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
            _canvas = StoryUi.CreateOverlayCanvas("LosePopupCanvas", 620);
            _canvas.transform.SetParent(transform, false);

            // A scrim, so the dead board behind cannot be tapped while the panel
            // is up and the panel reads as being in front of something.
            Image scrim = StoryUi.CreateImage("Scrim", _canvas.transform, null, StoryUi.Dim);
            StoryUi.Stretch(scrim.rectTransform);
            scrim.raycastTarget = true;

            RectTransform band = StoryUi.CreateRect("Band", _canvas.transform);
            band.anchorMin = Vector2.zero;
            band.anchorMax = Vector2.one;
            band.offsetMin = new Vector2(0f, PopupBottom);
            band.offsetMax = new Vector2(0f, -PopupTop);

            // No close X: the green button is the only way out of a loss.
            _panel = StoryUi.RoyalPanel("Panel", band, "Level", null);

            BuildStar();
            BuildCaption();
            BuildAreaCard();
            BuildContinue();
        }

        /// <summary>
        /// The star that was not earned: the same shape and the same place as
        /// the win panel's, drawn as an empty socket. Drawing nothing at all
        /// leaves a hole where the player is used to looking; drawing it lit
        /// would be a lie.
        ///
        /// It is RoyalGrey, the same tint the win panel puts on a replay's star,
        /// and it has to be a tint of that kind rather than a colour of its own:
        /// StarSprite is gold artwork and Image.color MULTIPLIES it, so nothing
        /// can turn it blue or neutral. RoyalBlueDark was tried, to read as a
        /// recess in the plate, and multiplied out to a dark green blob that
        /// looked like a bug. Grey over gold is bronze, which is what an unearned
        /// star has looked like in this genre for twenty years — and it matches
        /// the panel a player sees when they replay a level they have beaten.
        /// </summary>
        private void BuildStar()
        {
            RectTransform slot = StoryUi.CreateRect("StarSlot", _panel.Content);
            FracBand(slot, StarTopFrac, StarHeightFrac);

            _star = StoryUi.CreateImage("Star", slot, StoryUi.StarSprite, StoryUi.RoyalGrey);
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

            // The name gets the card's full width, for the reason the win panel
            // gives: a best-fit Text wraps before it shrinks, so a narrowed rect
            // turns a two-word area name into two lines that overflow the card.
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
            rect.pivot = new Vector2(0.5f, 0f);
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

            // What ran out, rather than "Level Failed!". The player knows they
            // lost; what they cannot see once the board is behind a scrim is
            // which of the two limits ended it.
            if (_caption != null)
                _caption.text = _outOfTime ? "Out of time" : "Out of moves";

            StoryArea area = StoryDatabase.GetArea(_areaId);
            if (_areaName != null)
                _areaName.text = area != null ? area.Name : string.Empty;

            if (_progress != null)
            {
                int done = StoryProgress.TasksCompletedIn(_areaId);
                int total = area != null ? area.Tasks.Count : 0;
                StoryUi.SetProgress(_progress, done, total);

                // Named for the same reason it is named on the win panel: a bare
                // "1/5" under an area name reads as stars, and it counts repairs.
                if (_progress.Label != null)
                    _progress.Label.text = done + "/" + total + " repairs";
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

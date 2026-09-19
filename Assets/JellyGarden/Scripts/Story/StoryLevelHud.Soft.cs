using JellyGarden.Scripts.GUI;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed partial class StoryLevelHud
    {
        RectTransform _softRoot, _softCard, _softTray, _softCrest;
        RectTransform _royalPause, _royalMovesInset;
        Text _royalExtra;
        Image _softProgress;
        readonly Button[] _softBoosts = new Button[4];
        readonly Image[] _softBoostArt = new Image[4];
        readonly Text[] _softBoostCounts = new Text[4];
        readonly BoostType[] _softTypes = { BoostType.ExtraMoves, BoostType.Hand, BoostType.Bomb, BoostType.Random_color };
        const float GoalRowWidth = 146;
        const float GoalRowHeight = 53;
        float _softUnit, _softInset;

        void BuildSoftHud(Transform strip)
        {
            _kitStrip = strip;
            if (_softRoot != null) { _softRoot.gameObject.SetActive(true); LayoutSoft(); return; }
            Canvas canvas = strip.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            RectTransform pause = FindPause(strip) ?? FindPause(FindBoosterRow(strip));
            Button nativePause = pause == null ? null : pause.GetComponent<Button>();
            Fade(strip);
            RectTransform boosters = FindBoosterRow(strip);
            if (boosters != null) Fade(boosters.parent);

            _softRoot = StoryUi.CreateRect("RoyalBoardHud", canvas.transform);
            _softRoot.SetAsLastSibling();
            _softCard = StoryRoyalChrome.Plate("TargetCard", _softRoot).rectTransform;
            _targetTitle = RoyalText("Title", _softCard, "TARGETS", 16, StoryRoyalChrome.Paper);
            _targetRow = StoryUi.CreateRect("Goals", _softCard);
            _slots.Clear();
            var track = StoryCastleUi.Plate("ScoreProgress", _softCard, 3, new Color32(3, 31, 93, 255));
            track.raycastTarget = false;
            _softProgress = StoryCastleUi.Plate("Fill", track.transform, 3, new Color32(255, 210, 70, 255));
            _softProgress.raycastTarget = false;

            _softCrest = StoryRoyalChrome.Plate("MovesCard", _softRoot).rectTransform;
            _limitTitle = RoyalText("Title", _softCrest, "MOVES", 16, StoryRoyalChrome.Paper);
            _royalMovesInset = StoryRoyalChrome.Plate("IvoryInset", _softCrest, StoryRoyalChrome.Finish.Ivory).rectTransform;
            _limitValue = RoyalText("Value", _royalMovesInset, "0", 42, StoryRoyalChrome.Ink, false);
            _softTray = StoryRoyalChrome.Plate("BoosterTray", _softRoot).rectTransform;
            for (int i = 0; i < 4; i++)
            {
                int slot = i;
                Image face = StoryRoyalChrome.Plate("Booster" + i, _softTray);
                _softBoosts[i] = StoryCastleUi.Clickable(face, () => UseSoftBoost(slot));
                var colors = _softBoosts[i].colors;
                colors.pressedColor = new Color(.65f, .79f, 1f);
                colors.disabledColor = new Color(.68f, .73f, .82f, 1f);
                _softBoosts[i].colors = colors;
                _softBoostArt[i] = StoryUi.CreateImage("Icon", face.transform, StorybookBoardArt.Booster(_softTypes[i]), Color.white);
                _softBoostArt[i].preserveAspect = true;
                _softBoostArt[i].raycastTarget = false;
                if (i == 0) _royalExtra = RoyalText("Extra", face.transform, "+5", 42, new Color32(255, 225, 99, 255));
                Image count = StoryRoyalChrome.Plate("CountBadge", face.transform, StoryRoyalChrome.Finish.Green, true);
                _softBoostCounts[i] = RoyalText("Count", count.transform, "0", 18, Color.white);
            }
            var pauseFace = StoryRoyalChrome.Plate("Pause", _softRoot, StoryRoyalChrome.Finish.Blue, true);
            _royalPause = pauseFace.rectTransform;
            StoryCastleUi.Clickable(pauseFace, () => { if (nativePause != null) nativePause.onClick.Invoke(); });
            StoryCastleUi.Plate("LeftBar", _royalPause, 2, new Color32(255, 225, 99, 255)).raycastTarget = false;
            StoryCastleUi.Plate("RightBar", _royalPause, 2, new Color32(255, 225, 99, 255)).raycastTarget = false;
            LayoutSoft();
        }

        static Text RoyalText(string name, Transform parent, string value, int size, Color color, bool shadow = true)
        {
            Text text = StoryUi.CreateText(name, parent, value, size, color, TextAnchor.MiddleCenter);
            text.font = StoryUi.DisplayFont;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            if (shadow)
            {
                var depth = text.gameObject.AddComponent<Shadow>();
                depth.effectColor = new Color32(9, 31, 68, 220);
                depth.effectDistance = new Vector2(0, -1.2f);
            }
            return text;
        }

        void PlaceSoft(RectTransform rect, float x, float y, float width, float height)
        {
            if (rect.parent == _softRoot) x += _softInset;
            StoryCastleUi.Place(rect, x * _softUnit, -y * _softUnit, width * _softUnit, height * _softUnit);
        }

        void FontSize(Text text, float size, float minimum = 0)
        {
            text.fontSize = Mathf.Max(8, Mathf.RoundToInt(size * _softUnit));
            text.resizeTextForBestFit = minimum > 0;
            if (minimum > 0)
            {
                text.resizeTextMinSize = Mathf.Max(8, Mathf.RoundToInt(minimum * _softUnit));
                text.resizeTextMaxSize = text.fontSize;
            }
        }

        void LayoutSoft()
        {
            Rect safe = StorybookBoardLayout.Safe;
            _softRoot.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            _softRoot.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            _softRoot.offsetMin = _softRoot.offsetMax = Vector2.zero;
            _softUnit = Mathf.Max(.01f, Mathf.Min(_softRoot.rect.width / 393f, _softRoot.rect.height / 720f));
            _softInset = (_softRoot.rect.width / _softUnit - 393f) * .5f;
            float height = _softRoot.rect.height / _softUnit;
            // The drawing and the reserved board field use the same header/footer.
            float top = 28;
            PlaceSoft(_softCard, 12, top + 10, 170, 96);
            PlaceSoft(_targetTitle.rectTransform, 10, 5, 150, 23);
            PlaceSoft(_targetRow, 12, 32, GoalRowWidth, GoalRowHeight);
            PlaceSoft(_softProgress.transform.parent as RectTransform, 17, 83, 136, 4);
            PlaceSoft(_softCrest, 211, top + 10, 170, 96);
            PlaceSoft(_limitTitle.rectTransform, 10, 5, 150, 23);
            PlaceSoft(_royalMovesInset, 11, 32, 148, 53);
            StoryUi.Stretch(_limitValue.rectTransform, 5 * _softUnit);
            PlaceSoft(_softTray, 12, height - 107, 369, 85);
            for (int i = 0; i < 4; i++)
            {
                PlaceSoft(_softBoosts[i].transform as RectTransform, 6 + i * 90, -2, 87, 84);
                PlaceSoft(_softBoostArt[i].rectTransform, 5, 4, 77, 73);
                PlaceSoft(_softBoostCounts[i].transform.parent as RectTransform, 60, -7, 29, 29);
                StoryUi.Stretch(_softBoostCounts[i].rectTransform, 2 * _softUnit);
                FontSize(_softBoostCounts[i], 18);
            }
            PlaceSoft(_royalExtra.rectTransform, 5, 8, 77, 63);
            PlaceSoft(_royalPause, 170.5f, top + 3, 52, 52);
            PlaceSoft(_royalPause.Find("LeftBar") as RectTransform, 17, 15, 6, 22);
            PlaceSoft(_royalPause.Find("RightBar") as RectTransform, 29, 15, 6, 22);
            FontSize(_royalExtra, 42, 25);
            FontSize(_limitTitle, 16, 13);
            FontSize(_targetTitle, 16, 13);
            FontSize(_limitValue, 42, 27);
            foreach (Shadow shadow in _softRoot.GetComponentsInChildren<Shadow>())
                shadow.effectDistance = new Vector2(0, -1.2f * _softUnit);
            foreach (Image image in _softRoot.GetComponentsInChildren<Image>())
                if (image.type == Image.Type.Sliced) image.pixelsPerUnitMultiplier = 2.5f / _softUnit;
        }

        void RefreshSoft(HudModel model)
        {
            LayoutSoft();
            LevelManager manager = LevelManager.THIS;
            SetText(_limitTitle, model.LimitTitle.ToUpperInvariant());
            SetText(_limitValue, model.LimitValue);
            _limitValue.color = model.LimitLeft <= (model.LimitTitle == "Time" ? 10 : 3)
                ? new Color32(189, 51, 36, 255) : StoryRoyalChrome.Ink;
            int count = Mathf.Min(MaxTargets, model.Targets.Count);
            _visibleSlots = count;
            bool score = count == 1 && model.Targets[0].IsScore;
            SetText(_targetTitle, score ? "SCORE" : "TARGETS");
            _softProgress.transform.parent.gameObject.SetActive(score);
            while (_slots.Count < count)
            {
                var slot = new Slot { Root = StoryUi.CreateRect("Goal" + _slots.Count, _targetRow) };
                slot.Backing = StoryUi.CreateImage("Backing", slot.Root, null, Color.white);
                slot.Icon = StoryUi.CreateImage("Icon", slot.Root, null, Color.white);
                slot.Backing.preserveAspect = slot.Icon.preserveAspect = true;
                slot.Backing.raycastTarget = slot.Icon.raycastTarget = false;
                slot.Count = RoyalText("Count", slot.Root, "0", 17, Color.white);
                _slots.Add(slot);
            }
            for (int i = 0; i < _slots.Count; i++)
            {
                Slot slot = _slots[i];
                slot.Root.gameObject.SetActive(i < count);
                if (i >= count) continue;
                HudTarget target = model.Targets[i];
                float width = GoalRowWidth / count;
                PlaceSoft(slot.Root, i * width, 0, width, GoalRowHeight);
                float icon = count >= 3 ? 31 : 39;
                PlaceSoft(slot.Icon.rectTransform, (width - icon) * .5f, -3, icon, icon);
                slot.Backing.rectTransform.anchorMin = slot.Icon.rectTransform.anchorMin;
                slot.Backing.rectTransform.anchorMax = slot.Icon.rectTransform.anchorMax;
                slot.Backing.rectTransform.pivot = slot.Icon.rectTransform.pivot;
                slot.Backing.rectTransform.anchoredPosition = slot.Icon.rectTransform.anchoredPosition;
                slot.Backing.rectTransform.sizeDelta = slot.Icon.rectTransform.sizeDelta;
                slot.Backing.sprite = target.Backing;
                slot.Backing.enabled = target.Backing != null;
                slot.Icon.sprite = target.Icon;
                PlaceSoft(slot.Count.rectTransform, 1, score ? 32 : icon - 4, width - 2, 22);
                FontSize(slot.Count, score ? 16 : 19, count >= 3 ? 12 : 13);
                slot.Count.color = target.Done ? new Color32(167, 255, 121, 255) : Color.white;
                SetText(slot.Count, target.Done ? "✓" : score ? target.Display : target.Remaining.ToString("N0"));
                if (score)
                {
                    float fraction = model.PassMark <= 0 ? 0 : 1f - (float)target.Remaining / model.PassMark;
                    _softProgress.rectTransform.anchorMin = Vector2.zero;
                    _softProgress.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1);
                    _softProgress.rectTransform.offsetMin = _softProgress.rectTransform.offsetMax = Vector2.zero;
                    _softProgress.enabled = fraction > 0;
                }
            }
            _softTypes[0] = model.LimitTitle == "Time" ? BoostType.ExtraTime : BoostType.ExtraMoves;
            SetText(_royalExtra, _softTypes[0] == BoostType.ExtraTime ? "+30s" : "+5");
            for (int i = 0; i < 4; i++)
            {
                int owned = StoryBoosters.Owned(_softTypes[i]);
                _softBoostArt[i].sprite = StorybookBoardArt.Booster(_softTypes[i]);
                _softBoostArt[i].enabled = i != 0;
                SetText(_softBoostCounts[i], owned.ToString());
                _softBoosts[i].interactable = owned > 0 && manager != null && manager.gameStatus == GameState.Playing && !manager.DragBlocked;
                bool selected = manager != null && manager.activatedBoost != null && manager.activatedBoost.type == _softTypes[i];
                _softBoostArt[i].color = owned > 0 ? Color.white : new Color(.72f, .76f, .83f, 1);
                _softBoostCounts[i].transform.parent.GetComponent<Image>().color = owned > 0 ? Color.white : new Color(.75f, .79f, .84f, 1);
                _softBoosts[i].GetComponent<Image>().color = selected ? new Color(1f, .88f, .57f) : Color.white;
            }
        }

        void UseSoftBoost(int index)
        {
            LevelManager manager = LevelManager.THIS;
            if (manager == null || manager.gameStatus != GameState.Playing || manager.DragBlocked || StoryBoosters.Owned(_softTypes[index]) <= 0) return;
            foreach (BoostIcon boost in manager.InGameBoosts)
                if (boost != null && boost.type == _softTypes[index]) { boost.ActivateBoost(); Refresh(); return; }
        }
        void OnDisable() { if (_softRoot != null) _softRoot.gameObject.SetActive(false); }
        void OnEnable() { if (_softRoot != null) _softRoot.gameObject.SetActive(true); }
    }
}

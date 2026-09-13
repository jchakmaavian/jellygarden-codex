using JellyGarden.Scripts.GUI;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed partial class StoryLevelHud
    {
        RectTransform _softRoot, _softHeader, _softCard, _softTray;
        Text _softLevel;
        Image _softProgress;
        readonly Button[] _softBoosts = new Button[4];
        readonly Image[] _softBoostArt = new Image[4];
        readonly Text[] _softBoostCounts = new Text[4];
        readonly BoostType[] _softTypes = { BoostType.ExtraMoves, BoostType.Hand, BoostType.Bomb, BoostType.Random_color };
        static readonly Color SoftInk = new Color32(66, 86, 123, 255);
        float _softUnit;

        void BuildSoftHud(Transform strip)
        {
            _kitStrip = strip;
            if (_softRoot != null) { _softRoot.gameObject.SetActive(true); LayoutSoft(); return; }
            Canvas canvas = strip.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            RectTransform pause = FindPause(strip) ?? FindPause(FindBoosterRow(strip));
            Button nativePause = pause == null ? null : pause.GetComponent<Button>();
            Fade(strip);
            RectTransform boosterRow = FindBoosterRow(strip);
            if (boosterRow != null) Fade(boosterRow.parent);
            _softRoot = StoryUi.CreateRect("SoftStorybookBoardHud", canvas.transform);
            _softRoot.SetAsLastSibling();
            _softHeader = StoryUi.CreateRect("Header", _softRoot);
            _softLevel = SoftText("Level", _softHeader, "Level 1", 24);
            _softLevel.font = StorybookArt.Font;
            SoftControl("Pause", _softHeader, "Ⅱ", () => { if (nativePause != null) nativePause.onClick.Invoke(); });
            SoftControl("Settings", _softHeader, "⚙", () =>
            {
                // Use the game's pause flow: it also suspends timed levels.
                if (nativePause != null) nativePause.onClick.Invoke();
            });
            _softCard = SoftPlate("TargetCard", _softRoot, 20, new Color32(255, 252, 242, 255)).rectTransform;
            AddSoftShadow(_softCard, new Color32(213, 200, 173, 255), 3);
            _targetTitle = SoftText("Title", _softCard, "TARGETS", 9);
            _targetRow = StoryUi.CreateRect("Goals", _softCard);
            _slots.Clear();
            Image moves = SoftPlate("Moves", _softCard, 15, new Color32(208, 223, 241, 255));
            AddSoftShadow(moves.rectTransform, new Color32(158, 189, 216, 255), 2);
            _limitTitle = SoftText("Title", moves.transform, "MOVES", 9);
            _limitValue = SoftText("Value", moves.transform, "0", 32);
            _limitValue.font = StorybookArt.Font;
            Image track = SoftPlate("ScoreProgress", _softCard, 3, new Color32(224, 226, 216, 255));
            _softProgress = SoftPlate("Fill", track.transform, 3, new Color32(127, 170, 141, 255));
            _softTray = SoftPlate("BoosterTray", _softRoot, 28, new Color32(229, 225, 238, 255)).rectTransform;
            AddSoftShadow(_softTray, new Color32(196, 188, 215, 255), 3);
            for (int i = 0; i < 4; i++)
            {
                int slot = i;
                Image face = StoryUi.CreateImage("Booster" + i, _softTray, StoryUi.CircleSprite, new Color32(255, 254, 249, 255));
                _softBoosts[i] = StoryCastleUi.Clickable(face, () => UseSoftBoost(slot));
                _softBoostArt[i] = StoryUi.CreateImage("Icon", face.transform, StorybookBoardArt.Booster(_softTypes[i]), Color.white);
                _softBoostArt[i].preserveAspect = true;
                Image count = StoryUi.CreateImage("CountBadge", face.transform, StoryUi.CircleSprite, new Color32(135, 157, 184, 255));
                _softBoostCounts[i] = SoftText("Count", count.transform, "0", 12, Color.white);
            }
            LayoutSoft();
        }
        Image SoftPlate(string name, Transform parent, int radius, Color color)
        {
            Image image = StoryCastleUi.Plate(name, parent, radius, color);
            image.raycastTarget = false;
            return image;
        }
        static Text SoftText(string name, Transform parent, string value, int size, Color? color = null)
        {
            Text text = StoryUi.CreateText(name, parent, value, size, color ?? SoftInk, TextAnchor.MiddleCenter);
            text.font = StoryUi.BodyFont;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }
        void AddSoftShadow(RectTransform host, Color color, float offset)
        {
            var shadow = host.gameObject.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = new Vector2(0, -offset);
            shadow.useGraphicAlpha = false;
        }
        void SoftControl(string name, Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            Image face = StoryUi.CreateImage(name, parent, StoryUi.CircleSprite, new Color32(255, 253, 243, 255));
            StoryCastleUi.Clickable(face, action);
            if (name == "Settings")
            {
                Image gear = StoryUi.CreateImage("Label", face.transform, StoryUi.GearSprite, new Color32(115, 148, 176, 255));
                gear.raycastTarget = false;
            }
            else SoftText("Label", face.transform, label, 16, new Color32(115, 148, 176, 255));
        }
        void PlaceSoft(RectTransform rect, float x, float y, float width, float height)
        {
            StoryCastleUi.Place(rect, x * _softUnit, -y * _softUnit, width * _softUnit, height * _softUnit);
        }
        void LayoutSoft()
        {
            Rect safe = StorybookBoardLayout.Safe;
            _softRoot.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            _softRoot.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            _softRoot.offsetMin = _softRoot.offsetMax = Vector2.zero;
            _softUnit = _softRoot.rect.width / 393f;
            if (_softUnit <= 0) _softUnit = 1;
            float height = _softRoot.rect.height / _softUnit;
            PlaceSoft(_softHeader, 22, 18, 349, 34);
            PlaceSoft(_softLevel.rectTransform, 45, 0, 259, 34);
            PlaceSoft(_softHeader.Find("Pause") as RectTransform, 0, 0, 34, 34);
            PlaceSoft(_softHeader.Find("Settings") as RectTransform, 315, 0, 34, 34);
            foreach (string name in new[] { "Pause", "Settings" })
                StoryUi.Stretch((_softHeader.Find(name + "/Label") as RectTransform), name == "Settings" ? 10 * _softUnit : 0);
            PlaceSoft(_softCard, 22, 70, 349, 96);
            PlaceSoft(_targetTitle.rectTransform, 12, 6, 233, 15);
            PlaceSoft(_targetRow, 12, 22, 233, 66);
            PlaceSoft(_limitValue.transform.parent as RectTransform, 266, 12, 71, 72);
            PlaceSoft(_limitTitle.rectTransform, 0, 7, 71, 15);
            PlaceSoft(_limitValue.rectTransform, 0, 23, 71, 44);
            PlaceSoft(_softProgress.transform.parent as RectTransform, 16, 80, 225, 6);
            PlaceSoft(_softTray, 19, height - 103, 355, 75);
            for (int i = 0; i < 4; i++)
            {
                PlaceSoft(_softBoosts[i].transform as RectTransform, 25 + 79 * i, 8, 57, 57);
                PlaceSoft(_softBoostArt[i].rectTransform, 3, 1, 51, 51);
                PlaceSoft(_softBoostCounts[i].transform.parent as RectTransform, 39, 39, 21, 21);
                StoryUi.Stretch(_softBoostCounts[i].rectTransform);
            }
            foreach (Text text in _softRoot.GetComponentsInChildren<Text>(true))
            {
                float size = text == _softLevel ? 24 : text == _limitValue ? 32 :
                    text == _targetTitle || text == _limitTitle ? 9 :
                    text.transform.parent.name == "CountBadge" ? 12 : 16;
                text.fontSize = Mathf.Max(8, Mathf.RoundToInt(size * _softUnit));
            }
            foreach (Shadow shadow in _softRoot.GetComponentsInChildren<Shadow>())
                shadow.effectDistance = new Vector2(0, -2.5f * _softUnit);
            foreach (Image plate in _softRoot.GetComponentsInChildren<Image>())
                if (plate.type == Image.Type.Sliced) plate.pixelsPerUnitMultiplier = 1f / _softUnit;
        }
        void RefreshSoft(HudModel model)
        {
            LayoutSoft();
            LevelManager manager = LevelManager.THIS;
            SetText(_softLevel, "Level " + (manager == null ? 1 : manager.currentLevel));
            SetText(_limitTitle, model.LimitTitle.ToUpperInvariant());
            SetText(_limitValue, model.LimitValue);
            _limitValue.color = model.LimitLeft <= (model.LimitTitle == "Time" ? 10 : 3)
                ? new Color32(193, 89, 75, 255) : SoftInk;
            int count = Mathf.Min(MaxTargets, model.Targets.Count);
            _visibleSlots = count;
            bool single = count == 1 && model.Targets[0].IsScore;
            SetText(_targetTitle, single ? "SCORE" : "TARGETS");
            _softProgress.transform.parent.gameObject.SetActive(single);
            while (_slots.Count < count)
            {
                var slot = new Slot { Root = StoryUi.CreateRect("Goal" + _slots.Count, _targetRow) };
                slot.Backing = StoryUi.CreateImage("Backing", slot.Root, null, Color.white);
                slot.Icon = StoryUi.CreateImage("Icon", slot.Root, null, Color.white);
                slot.Backing.preserveAspect = slot.Icon.preserveAspect = true;
                slot.Count = SoftText("Count", slot.Root, "0", 16);
                _slots.Add(slot);
            }
            for (int i = 0; i < _slots.Count; i++)
            {
                Slot slot = _slots[i];
                slot.Root.gameObject.SetActive(i < count);
                if (i >= count) continue;
                HudTarget target = model.Targets[i];
                float width = 233f / count;
                PlaceSoft(slot.Root, i * width, 0, width, 66);
                float icon = count == 4 ? 34 : 42;
                PlaceSoft(slot.Icon.rectTransform, single ? 2 : (width - icon) / 2, single ? 5 : 0, icon, icon);
                slot.Backing.rectTransform.anchorMin = slot.Icon.rectTransform.anchorMin;
                slot.Backing.rectTransform.anchorMax = slot.Icon.rectTransform.anchorMax;
                slot.Backing.rectTransform.pivot = slot.Icon.rectTransform.pivot;
                slot.Backing.rectTransform.anchoredPosition = slot.Icon.rectTransform.anchoredPosition;
                slot.Backing.rectTransform.sizeDelta = slot.Icon.rectTransform.sizeDelta;
                slot.Backing.sprite = target.Backing;
                slot.Backing.enabled = target.Backing != null;
                slot.Icon.sprite = target.Icon;
                slot.Icon.raycastTarget = slot.Backing.raycastTarget = false;
                PlaceSoft(slot.Count.rectTransform, single ? 52 : 0, single ? 6 : icon, single ? width - 52 : width, 23);
                slot.Count.alignment = single ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
                slot.Count.fontSize = Mathf.RoundToInt(16 * _softUnit);
                slot.Count.color = target.Done ? new Color32(79, 132, 99, 255) : SoftInk;
                SetText(slot.Count, target.Done ? "✓" : single ? target.Display.Replace("/", " / ") :
                    target.Remaining.ToString("N0") + (target.IsScore ? "<size=" + Mathf.RoundToInt(8 * _softUnit) + "> pts</size>" : ""));
                if (single)
                {
                    float fraction = model.PassMark <= 0 ? 0 : 1f - (float)target.Remaining / model.PassMark;
                    _softProgress.rectTransform.anchorMin = Vector2.zero;
                    _softProgress.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1);
                    _softProgress.rectTransform.offsetMin = _softProgress.rectTransform.offsetMax = Vector2.zero;
                    _softProgress.enabled = fraction > 0;
                }
            }
            _softTypes[0] = model.LimitTitle == "Time" ? BoostType.ExtraTime : BoostType.ExtraMoves;
            for (int i = 0; i < 4; i++)
            {
                int owned = StoryBoosters.Owned(_softTypes[i]);
                _softBoostArt[i].sprite = StorybookBoardArt.Booster(_softTypes[i]);
                SetText(_softBoostCounts[i], owned.ToString());
                _softBoosts[i].interactable = owned > 0 && manager != null && manager.gameStatus == GameState.Playing;
                bool selected = manager != null && manager.activatedBoost != null && manager.activatedBoost.type == _softTypes[i];
                _softBoosts[i].GetComponent<Image>().color = selected ? new Color32(171, 214, 194, 255) : Color.white;
            }
        }
        void UseSoftBoost(int index)
        {
            LevelManager manager = LevelManager.THIS;
            if (manager == null || StoryBoosters.Owned(_softTypes[index]) <= 0) return;
            foreach (BoostIcon boost in manager.InGameBoosts)
                if (boost != null && boost.type == _softTypes[index]) { boost.ActivateBoost(); Refresh(); return; }
        }
        void OnDisable() { if (_softRoot != null) _softRoot.gameObject.SetActive(false); }
        void OnEnable() { if (_softRoot != null) _softRoot.gameObject.SetActive(true); }
    }
}

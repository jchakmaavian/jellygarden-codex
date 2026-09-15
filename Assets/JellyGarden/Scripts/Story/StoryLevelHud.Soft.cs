using JellyGarden.Scripts.GUI;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed partial class StoryLevelHud
    {
        RectTransform _softRoot, _softCard, _softTray;

        /// <summary>
        /// The blue plate on the right of the strip. It carries the level number
        /// over the move count as one object, and the pause button hangs off its
        /// bottom edge.
        ///
        /// The level used to sit in a header row of its own above the card, with
        /// Moves inside the card. Two rows of chrome over a board that is already
        /// width-limited is a row too many, and the header's other control was a
        /// Settings button that called the pause button's own action — see the
        /// note on <see cref="SoftControl"/>. Folding the level in here buys the
        /// header's 52 units back and leaves the cream card to say only what the
        /// level asks for.
        /// </summary>
        RectTransform _softCrest;

        Text _softLevel;
        Image _softProgress;
        readonly Button[] _softBoosts = new Button[4];
        readonly Image[] _softBoostArt = new Image[4];
        readonly Text[] _softBoostCounts = new Text[4];
        readonly BoostType[] _softTypes = { BoostType.ExtraMoves, BoostType.Hand, BoostType.Bomb, BoostType.Random_color };
        static readonly Color SoftInk = new Color32(66, 86, 123, 255);

        /// <summary>
        /// The goal row's width, and the divisor every chip's slot is cut from.
        ///
        /// It is the cream card's inner width, so the slots add up to the row
        /// exactly. Anything that changes the card's width changes this with it
        /// — while the two disagreed, each chip was laid out wider than the room
        /// it had and the last one ran off the plate.
        /// </summary>
        const float GoalRowWidth = 212f;

        /// <summary>
        /// How tall one chip is: its icon over its count, and nothing else.
        ///
        /// 58 = a 38-unit icon plus a 20-unit number. It was 66 around a 42-unit
        /// icon, sized when the card had a header row above it to fill. The row
        /// is the tallest thing the card holds, so this is what sets the card's
        /// height, and through it StorybookBoardLayout.HeaderBottom — measure it
        /// from the chip rather than padding it to fit the plate.
        /// </summary>
        const float GoalRowHeight = 58f;

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
            _softCard = SoftPlate("TargetCard", _softRoot, 20, new Color32(255, 252, 242, 255)).rectTransform;
            AddSoftShadow(_softCard, new Color32(213, 200, 173, 255), 3);
            _targetTitle = SoftText("Title", _softCard, "TARGETS", 9);
            _targetRow = StoryUi.CreateRect("Goals", _softCard);
            _slots.Clear();

            // The crest, and the gold rim that separates it from the board.
            //
            // The board's own ground is indigo (see StorybookBoardLayout's
            // checker, 64,72,112 / 78,87,130) and this plate is royal blue, so
            // the two would read as one surface where they meet. The rim is a
            // second plate drawn OVER the first at the same rect, carrying only
            // an inner edge — the same two-nested-rects trick the Royal kit uses,
            // rather than an Outline, which offsets the whole quad and squares
            // off the corners.
            _softCrest = SoftPlate("Crest", _softRoot, 20, StorybookUi.Blue).rectTransform;
            AddSoftShadow(_softCrest, new Color32(9, 58, 130, 255), 3);
            Image crestRim = SoftPlate("Rim", _softCrest, 20, StorybookUi.Gold);
            Image crestFace = SoftPlate("Face", crestRim.transform, 18, StorybookUi.Blue);
            StoryUi.Stretch(crestRim.rectTransform);
            StoryUi.Stretch(crestFace.rectTransform, 2);

            _softLevel = SoftText("Level", crestFace.transform, "Level 1", 15, StorybookUi.Paper);
            _softLevel.font = StorybookArt.Font;
            _limitTitle = SoftText("Title", crestFace.transform, "MOVES", 9, new Color32(255, 228, 159, 255));
            _limitValue = SoftText("Value", crestFace.transform, "0", 34, Color.white);
            _limitValue.font = StorybookArt.Font;

            // The hairline between the level and the count. A child of the face,
            // so it is inside the rim rather than lying across it.
            SoftPlate("Rule", crestFace.transform, 1, new Color32(255, 255, 255, 64));

            // Pause hangs off the crest's bottom edge, half on and half off, so
            // it reads as seated in the plate rather than floating beside it.
            // It is the LAST child of the root so it draws over the crest, and
            // it is built on the root rather than on the crest because the crest
            // is what a level-open animation scales.
            SoftControl("Pause", _softRoot, "Ⅱ", () => { if (nativePause != null) nativePause.onClick.Invoke(); });
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
        /// <summary>
        /// A round control seated in the crest: a gold ring around a cream face.
        ///
        /// There is only one of these now. The strip used to carry a Settings
        /// gear beside the pause button whose action was, in full,
        /// <c>nativePause.onClick.Invoke()</c> — the same pause flow, opening the
        /// same kit panel, because that panel is where the game's settings are.
        /// Two buttons one tap apart doing one thing is a choice the player has
        /// to make and cannot get right, so the gear is gone and the pause button
        /// is the way in. Do not re-add one without giving it somewhere else to
        /// go: <see cref="StorySettings"/> is the shell's screen and deliberately
        /// sorts BELOW the level popup, so it cannot open over a board.
        /// </summary>
        void SoftControl(string name, Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            Image ring = StoryUi.CreateImage(name, parent, StoryUi.CircleSprite, StorybookUi.Gold);
            StoryCastleUi.Clickable(ring, action);
            Image face = StoryUi.CreateImage("Face", ring.transform, StoryUi.CircleSprite,
                new Color32(255, 253, 243, 255));
            face.raycastTarget = false;
            SoftText("Label", face.transform, label, 16, StorybookUi.Blue);
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

            // The cream card and the crest stand on one top edge at y24 and one
            // bottom edge at y120, so the strip reads as a pair rather than as
            // two plates that happen to be near each other.
            //
            // 96 tall, not 120. The plates were sized to the room the old
            // two-row header left rather than to what they hold: the card is a
            // 15-unit title over a 66-unit goal row, and the crest is an
            // 18-unit level over a 15-unit label and a 44-unit count. Both come
            // to about 90, so a 120-unit plate carried 30 units of empty cream
            // under its last chip — which on a SCORE level, where the card
            // draws ONE chip, was most of the plate. The strip now ends at 120
            // and StorybookBoardLayout.HeaderBottom comes down with it; the
            // board is width-limited on every phone this ships to, so the 44
            // units this frees go to the board's own top edge rather than to
            // the gap above it.
            // 78 tall, and every number in it is measured from the content
            // rather than chosen: the card is a 12-unit title over a
            // GoalRowHeight chip, and the crest is a 15-unit level over a
            // 12-unit label and a 34-unit count. The plates ended at 120 while
            // they were 96, and the band of empty cream under the last chip was
            // still most of the plate on a SCORE level, which draws one chip.
            PlaceSoft(_softCard, 22, 18, 236, 78);
            PlaceSoft(_targetTitle.rectTransform, 12, 4, GoalRowWidth, 12);
            _targetTitle.alignment = TextAnchor.MiddleLeft;
            PlaceSoft(_targetRow, 12, 16, GoalRowWidth, GoalRowHeight);
            // The score track sits inside the card, which now has 204 units of
            // width to give it rather than the 225 it borrowed from the old
            // full-width plate.
            PlaceSoft(_softProgress.transform.parent as RectTransform, 16, 68, 204, 5);

            PlaceSoft(_softCrest, 268, 18, 103, 78);
            PlaceSoft(_softLevel.rectTransform, 0, 5, 99, 15);
            PlaceSoft(_softCrest.Find("Rim/Face/Rule") as RectTransform, 18, 21, 63, 1);
            PlaceSoft(_limitTitle.rectTransform, 0, 24, 99, 12);
            PlaceSoft(_limitValue.rectTransform, 0, 37, 99, 34);

            // Half on the crest and half off it: the disc's centre sits on the
            // crest's bottom edge (96), so its top half overlaps the plate and
            // its lower half hangs into the gap above the board.
            PlaceSoft(_softRoot.Find("Pause") as RectTransform, 304, 80, 32, 32);
            StoryUi.Stretch(_softRoot.Find("Pause/Face") as RectTransform, 3 * _softUnit);
            StoryUi.Stretch(_softRoot.Find("Pause/Face/Label") as RectTransform);
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
                // Each of these is a point or two under the box it draws in, so
                // a descender or a tall digit does not clip: the count is 30 in
                // a 34-unit box, the level 13 in 15.
                float size = text == _softLevel ? 13 : text == _limitValue ? 30 :
                    text == _targetTitle || text == _limitTitle ? 9 :
                    text.transform.parent.name == "CountBadge" ? 12 : 15;
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
            // The count sits on the crest's royal blue now, not on the cream
            // card's pale plate, so both of these moved up in value: navy on
            // blue is unreadable, and the old warning red (193,89,75) is darker
            // than the plate it would warn on. Warm coral carries on blue, and
            // the level number above it stays paper so the warning is the only
            // thing on the crest that changes colour.
            _limitValue.color = model.LimitLeft <= (model.LimitTitle == "Time" ? 10 : 3)
                ? new Color32(255, 173, 145, 255) : Color.white;
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
                // 212, not 233: the row is as wide as the card the card was
                // narrowed to. While this said 233 every chip was laid out a
                // tenth wider than the room it had, so the rightmost one ran
                // off the plate — the divisor has to be the row's own width or
                // the slots do not add up to it.
                float width = GoalRowWidth / count;
                PlaceSoft(slot.Root, i * width, 0, width, GoalRowHeight);
                float icon = count == 4 ? 30 : 38;
                PlaceSoft(slot.Icon.rectTransform, single ? 2 : (width - icon) / 2, single ? 3 : 0, icon, icon);
                slot.Backing.rectTransform.anchorMin = slot.Icon.rectTransform.anchorMin;
                slot.Backing.rectTransform.anchorMax = slot.Icon.rectTransform.anchorMax;
                slot.Backing.rectTransform.pivot = slot.Icon.rectTransform.pivot;
                slot.Backing.rectTransform.anchoredPosition = slot.Icon.rectTransform.anchoredPosition;
                slot.Backing.rectTransform.sizeDelta = slot.Icon.rectTransform.sizeDelta;
                slot.Backing.sprite = target.Backing;
                slot.Backing.enabled = target.Backing != null;
                slot.Icon.sprite = target.Icon;
                slot.Icon.raycastTarget = slot.Backing.raycastTarget = false;
                PlaceSoft(slot.Count.rectTransform, single ? 48 : 0, single ? 4 : icon, single ? width - 48 : width, 20);
                slot.Count.alignment = single ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
                // Set here rather than left to the size table in LayoutSoft,
                // which runs before the slots exist on the first pass. 15, to
                // match what that table gives every other chip-sized string —
                // the count's box is 20 units now and 16 left nothing for a
                // descender or for the "pts" run a score chip appends.
                slot.Count.fontSize = Mathf.RoundToInt(15 * _softUnit);
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

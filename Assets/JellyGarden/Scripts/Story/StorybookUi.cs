using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    /// <summary>The Royal Storybook's shared, resolution-independent UI materials.</summary>
    public static class StorybookUi
    {
        public static bool Enabled => StoryCampaign.IsTreasureHunt;
        public static readonly Color Paper = new Color32(255, 249, 231, 255);
        public static readonly Color PaperShade = new Color32(247, 228, 191, 255);
        public static readonly Color Gold = new Color32(220, 174, 79, 255);
        public static readonly Color GoldLight = new Color32(255, 228, 159, 255);
        public static readonly Color Ink = new Color32(20, 46, 94, 255);
        public static readonly Color Muted = new Color32(117, 105, 85, 255);
        public static readonly Color Blue = new Color32(14, 78, 174, 255);
        public static readonly Color BlueLight = new Color32(49, 140, 240, 255);

        public static Image PaperCard(string name, Transform parent, float unit = 1f, bool folds = true)
        {
            if (StorybookLayers.Get("task-card") != null)
            {
                Image layer = folds ? StorybookLayers.Sliced(name, parent, "area-sheet")
                    : StorybookLayers.Sliced(name, parent, name == "TaskCard" ? "task-card" : "area-row");
                layer.raycastTarget = true;
                return layer;
            }
            Image rim = StoryCastleUi.Plate(name, parent, 20 * unit, Gold);
            Image face = StoryCastleUi.Plate("Paper", rim.transform, 18 * unit, Color.white);
            StoryUi.Stretch(face.rectTransform, 3 * unit);
            StoryCastleUi.Gradient(face, Paper, PaperShade);
            if (folds) StorybookArt.PaperEdges(rim.rectTransform, unit, true);
            else
            {
                rim.color = new Color32(213,171,107,255);
                Image highlight = StoryCastleUi.Plate("InnerEdge",rim.transform,17*unit,new Color32(255,253,235,255));
                StoryUi.Stretch(highlight.rectTransform,unit);
                face.transform.SetAsLastSibling();
                StoryUi.Stretch(face.rectTransform,2*unit);
            }
            rim.raycastTarget = true;
            return rim;
        }

        public static Text Ribbon(string name, Transform parent, string title, int size, float unit = 1f)
        {
            Image rim = StorybookArt.BlankRibbon(name, parent);
            Text text = StoryUi.CreateText("Title", rim.transform, title, size, Paper, TextAnchor.MiddleCenter);
            text.font = StorybookArt.Font;
            text.rectTransform.anchorMin = new Vector2(.16f, .31f);
            text.rectTransform.anchorMax = new Vector2(.84f, .94f);
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            StoryUi.FitOneLine(text, size / 2, size);
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = Ink;
            shadow.effectDistance = new Vector2(0, -1.5f * unit);
            return text;
        }

        public static Button Primary(string name, Transform parent, string label, int size, UnityAction action,
            float unit = 3f)
        {
            Image face = StorybookArt.BlankButton(name, parent, unit);
            Button button = StoryCastleUi.Clickable(face, action);
            var colors = button.colors;
            colors.pressedColor = new Color(.72f, .82f, 1f);
            colors.disabledColor = new Color(.60f, .60f, .60f, .65f);
            button.colors = colors;
            Text text = StoryUi.CreateText("Label", face.transform, label, size, Paper, TextAnchor.MiddleCenter);
            StoryUi.Stretch(text.rectTransform, 6 * unit);
            text.font = StorybookArt.Font;
            StoryUi.FitOneLine(text, size / 2, size);
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = Ink;
            shadow.effectDistance = new Vector2(0, -1.3f * unit);
            return button;
        }

        public static RectTransform TaskIcon(string name, Transform parent, int areaId, StoryTask task)
        {
            if (areaId == 1 && (task.Index == 3 || task.Index == 4))
            {
                Image icon = StorybookArt.Image(name, parent,
                    task.Index == 3 ? StorybookArt.Flag : StorybookArt.Lantern);
                icon.preserveAspect = true;
                return icon.rectTransform;
            }
            return StoryUi.TaskIcon(name, parent, areaId, task);
        }

        public static RectTransform Padlock(Transform parent)
        {
            RectTransform root = StoryUi.Stretch(StoryUi.CreateRect("StorybookPadlock", parent));
            LockPart(root, "Bow", .28f, .46f, .72f, .97f, Gold, true);
            LockPart(root, "Opening", .37f, .50f, .63f, .85f, Paper, true);
            LockPart(root, "Body", .17f, .09f, .83f, .65f, Gold, false);
            LockPart(root, "Face", .20f, .13f, .80f, .61f, GoldLight, false);
            LockPart(root, "Keyhole", .44f, .35f, .56f, .49f, Ink, true);
            LockPart(root, "Stem", .475f, .24f, .525f, .40f, Ink, false);
            return root;
        }

        private static void LockPart(Transform parent, string name, float x1, float y1, float x2, float y2,
            Color tint, bool round)
        {
            Image part = StoryUi.CreateImage(name, parent, round ? StoryUi.CircleSprite : StoryUi.RoundedSprite(14), tint);
            if (!round) part.type = Image.Type.Sliced;
            part.rectTransform.anchorMin = new Vector2(x1, y1);
            part.rectTransform.anchorMax = new Vector2(x2, y2);
            part.rectTransform.offsetMin = part.rectTransform.offsetMax = Vector2.zero;
        }

        public static Button Close(Transform parent, UnityAction action, float unit = 3f)
        {
            Image rim = StorybookArt.Image("Close", parent, StorybookArt.CloseButton);
            var rect = rim.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.sizeDelta = Vector2.one * (49 * unit);
            rect.anchoredPosition = new Vector2(-4,-4) * unit;
            return StoryCastleUi.Clickable(rim, action);
        }

        public static StoryUi.RoyalPanelParts Panel(string name, Transform parent, string title, UnityAction close)
        {
            Image paper = PaperCard(name, parent, 3);
            StoryUi.Stretch(paper.rectTransform, 35);
            RectTransform content = StoryUi.CreateRect("Content", paper.transform);
            StoryUi.Stretch(content, 42);
            content.offsetMax = new Vector2(-42, -140);
            Text heading = Ribbon("TitlePlate", paper.transform, title, 76, 3);
            var banner = (RectTransform)heading.transform.parent;
            banner.anchorMin = new Vector2(0, 1);
            banner.anchorMax = Vector2.one;
            banner.pivot = new Vector2(.5f, 1);
            banner.offsetMin = new Vector2(24, -132);
            banner.offsetMax = new Vector2(-24, 35);
            var result = new StoryUi.RoyalPanelParts
            { Root = paper.rectTransform, Body = paper.rectTransform, Content = content, Title = heading };
            if (close != null)
            {
                result.Close = Close(paper.transform, close);
                result.Close.GetComponent<RectTransform>().anchoredPosition = new Vector2(5, 32);
                heading.rectTransform.offsetMin = new Vector2(100, 0);
                heading.rectTransform.offsetMax = new Vector2(-100, -6);
            }
            return result;
        }
    }

    public sealed class StorybookPageFold : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect r = rectTransform.rect;
            mesh.AddVert(new Vector3(r.xMin, r.yMax), StorybookUi.GoldLight, Vector2.zero);
            mesh.AddVert(new Vector3(r.xMax, r.yMax), StorybookUi.Paper, Vector2.zero);
            mesh.AddVert(new Vector3(r.xMin + r.width * .25f, r.yMin), StorybookUi.Gold, Vector2.zero);
            mesh.AddTriangle(0, 1, 2);
        }
    }
}

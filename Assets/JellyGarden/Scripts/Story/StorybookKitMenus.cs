using JellyGarden.Scripts.GUI;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    /// <summary>Dress scene instances while retaining the kit's menu animations and callbacks.</summary>
    public static class StorybookKitMenus
    {
        public static void Install()
        {
            if (!StorybookUi.Enabled) return;
            foreach (AnimationManager menu in Object.FindObjectsByType<AnimationManager>(FindObjectsInactive.Include))
            {
                if (menu.name != "MenuPause" && menu.name != "Settings") continue;
                Transform panel = menu.transform.Find("Image");
                if (panel == null || panel.Find("StorybookPaper") != null) continue;
                Image original = panel.GetComponent<Image>();
                if (original != null) original.color = Color.clear;
                Image paper = StorybookUi.PaperCard("StorybookPaper", panel, 2);
                StoryUi.Stretch(paper.rectTransform);
                paper.transform.SetAsFirstSibling();
                Text title = StorybookUi.Ribbon("StorybookRibbon", panel,
                    menu.name == "MenuPause" ? "Paused" : "Settings", 62, 2);
                StoryUi.TopAnchored((RectTransform)title.transform.parent, -24, 150, 20);
                if (menu.name != "MenuPause") continue;
                DressButton(panel.Find("Continue"), "Keep Playing", 485, 108);
                DressButton(panel.Find("Map"), "Back to Castle", 610, 75);
                int i = 0;
                foreach (string name in new[] { "Sound", "Music" })
                {
                    var control = panel.Find(name) as RectTransform;
                    if (control == null) continue;
                    StoryCastleUi.Place(control, i++ == 0 ? 155 : 395, -214, 103, 103);
                    Text label = StoryCastleUi.Label("StorybookLabel", control, name, 32,
                        StorybookUi.Ink, true, TextAnchor.MiddleCenter);
                    StoryCastleUi.Place(label.rectTransform, -35, -122, 173, 52);
                }
            }
        }

        private static void DressButton(Transform target, string label, float top, float height)
        {
            if (target == null) return;
            Image original = target.GetComponent<Image>();
            Button action = target.GetComponent<Button>();
            if (original == null || action == null) return;
            StoryUi.TopAnchored(original.rectTransform, top, height, 72);
            original.color = Color.clear;
            Button decoration = StorybookUi.Primary("StorybookButton", target, label, 44, null, 2);
            StoryUi.Stretch(decoration.GetComponent<RectTransform>());
            decoration.enabled = false;
            foreach (Graphic graphic in decoration.GetComponentsInChildren<Graphic>()) graphic.raycastTarget = false;
            action.targetGraphic = decoration.targetGraphic;
            action.colors = decoration.colors;
        }
    }
}

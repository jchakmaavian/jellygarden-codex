// A small illustrated story stage. Shapes are UI geometry; Pip is authored art.
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public static class TreasureMapDisplay
    {
        private static GameObject _scene;
        private static Image _picture, _map;
        private static bool _opening;

        public static void AddHomeButton(RectTransform frame)
        {
            if (!StoryCampaign.IsTreasureHunt) return;
            Image card = StorybookArt.Image("PipsMap", frame, StorybookArt.PipHome);
            StoryCastleUi.Place(card.rectTransform, 252, -181, 95, 121);
            StoryCastleUi.Clickable(card, () =>
            {
                if (!StoryProgress.OpeningSeen)
                {
                    StoryToast.Show("Play your first puzzle to meet Pip!");
                    return;
                }
                TreasureHuntPresentation.Play(StoryCampaign.OpeningScene, 1,
                    () => StoryShell.GoHome(), true);
            });
        }

        public static void ShowScene(int areaId, bool opening)
        {
            HideScene();
            _opening = opening;
            Canvas canvas = StoryUi.CreateOverlayCanvas("TreasureMapStage", 830);
            _scene = canvas.gameObject;
            StoryObjects.Persist(_scene);
            RectTransform root = StoryUi.Stretch(StoryUi.CreateRect("Stage", canvas.transform));
            Image plate = StoryUi.CreateImage("Picture", root,
                opening ? StoryArt.Portrait("Pip") : StoryArt.ClueImage(areaId), Color.white);
            plate.preserveAspect = true;
            plate.rectTransform.anchorMin = new Vector2(0.20f, 0.48f);
            plate.rectTransform.anchorMax = new Vector2(0.80f, 0.84f);
            plate.rectTransform.offsetMin = plate.rectTransform.offsetMax = Vector2.zero;
            _picture = plate;
            plate.gameObject.SetActive(!opening);
            Image map = StoryUi.CreateImage("Map", root, null, new Color32(255, 245, 209, 245));
            map.rectTransform.anchorMin = new Vector2(0.14f, 0.40f);
            map.rectTransform.anchorMax = new Vector2(0.86f, 0.49f);
            map.rectTransform.offsetMin = map.rectTransform.offsetMax = Vector2.zero;
            _map = map;
            map.gameObject.SetActive(!opening);
            string[] shapes = {"Music", "Ribbon", "Crystal", "Snow", "Gear", "Sun"};
            for (int i = 0; i < 6; i++)
            {
                Text key = StoryUi.CreateText("KeySlot" + i, map.transform, "?", 60,
                    new Color32(176, 131, 54, 255), TextAnchor.MiddleCenter);
                key.rectTransform.anchorMin = new Vector2(i / 6f, 0.28f);
                key.rectTransform.anchorMax = new Vector2((i + 1) / 6f, 0.95f);
                key.rectTransform.offsetMin = key.rectTransform.offsetMax = Vector2.zero;
                Text name = StoryUi.CreateText("Shape" + i, map.transform, shapes[i], 30,
                    StoryUi.RoyalNavy, TextAnchor.MiddleCenter);
                name.rectTransform.anchorMin = new Vector2(i / 6f, 0.05f);
                name.rectTransform.anchorMax = new Vector2((i + 1) / 6f, 0.32f);
                name.rectTransform.offsetMin = name.rectTransform.offsetMax = Vector2.zero;
            }
            // The dialogue canvas above this stage owns all input.
            foreach (Graphic graphic in _scene.GetComponentsInChildren<Graphic>()) graphic.raycastTarget = false;
        }

        public static void OnLine(DialogueLine line)
        {
            if (_scene == null || !_opening || line.StageCue != "map") return;
            if (_picture.gameObject.activeSelf) return;
            _picture.gameObject.SetActive(true);
            _map.gameObject.SetActive(true);
            if (Application.isPlaying) _picture.gameObject.AddComponent<PipEntrance>();
        }

        public static void ShowBannerMimi()
        {
            ShowScene(1, false);
            _picture.sprite = StoryArt.Portrait("Mimi");
            _map.gameObject.SetActive(false);
        }

        public static void HideScene()
        {
            if (_scene == null) return;
            _scene.SetActive(false);
            if (Application.isPlaying) Object.Destroy(_scene);
            else Object.DestroyImmediate(_scene);
            _scene = null;
        }
    }

    // Pip springs from the decoration cupboard into the map presentation.
    public sealed class PipEntrance : MonoBehaviour
    {
        private float _started;
        private RectTransform _rect;
        private void Start() { _started = Time.unscaledTime; _rect = (RectTransform)transform; }
        private void Update()
        {
            float t = Mathf.Clamp01((Time.unscaledTime - _started) / 0.8f);
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            _rect.anchoredPosition = Vector2.Lerp(new Vector2(230f, -320f), Vector2.zero, ease);
            _rect.localScale = Vector3.one * Mathf.Lerp(0.08f, 1f, ease);
            _rect.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-24f, 0f, ease));
            if (t >= 1f) Destroy(this);
        }
    }
}

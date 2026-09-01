// Arc beats: every fifth area gets a scene rather than a card.
//
// Scenes are data — Resources/Story/cutscenes.json, built from the arc markdown
// by tools/cutscene_build.py — and are played through the dialogue system over
// the area's own artwork. Nothing here is authored in code.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed class Cutscene
    {
        public int AreaId;
        public string Title = string.Empty;
        public readonly List<DialogueLine> Lines = new List<DialogueLine>();
    }

    public sealed class CutsceneRunner : MonoBehaviour
    {
        private static CutsceneRunner _instance;
        private static bool _loaded;
        private static readonly Dictionary<int, Cutscene> _cutscenes = new Dictionary<int, Cutscene>();

        private Canvas _canvas;
        private RectTransform _stage;
        private Text _title;
        private bool _playing;

        public static bool IsPlaying
        {
            get { return _instance != null && _instance._playing; }
        }

        #region Data

        public static void Load()
        {
            if (_loaded) return;
            _loaded = true;

            var text = Resources.Load<TextAsset>("Story/cutscenes");
            if (text == null)
            {
                Debug.Log("[Story] No cutscenes.json yet; arc beats fall back to the clue card.");
                return;
            }

            JsonValue root = JsonValue.Parse(text.text);
            foreach (JsonValue node in root["cutscenes"].Items)
            {
                var scene = new Cutscene
                {
                    AreaId = node["areaId"].AsInt(),
                    Title = node["title"].AsString()
                };

                foreach (JsonValue line in node["lines"].Items)
                    scene.Lines.Add(new DialogueLine(
                        line["speaker"].AsString(),
                        line["text"].AsString(),
                        line["voice"].AsString()));

                if (scene.AreaId > 0 && scene.Lines.Count > 0)
                    _cutscenes[scene.AreaId] = scene;
            }

            Debug.Log(string.Format("[Story] {0} arc-beat cutscenes loaded.", _cutscenes.Count));
        }

        public static bool Has(int areaId)
        {
            Load();
            return _cutscenes.ContainsKey(areaId);
        }

        public static Cutscene Get(int areaId)
        {
            Load();
            Cutscene scene;
            return _cutscenes.TryGetValue(areaId, out scene) ? scene : null;
        }

        public static void Unload()
        {
            _loaded = false;
            _cutscenes.Clear();
        }

        #endregion

        #region Playback

        /// <summary>
        /// Plays an area's beat over its artwork. Falls straight through to the
        /// callback when the area has no scene.
        /// </summary>
        public static void Play(int areaId, Action onDone)
        {
            Cutscene scene = Get(areaId);
            if (scene == null)
            {
                if (onDone != null) onDone();
                return;
            }

            Ensure();
            _instance.Begin(scene, onDone);
        }

        private static void Ensure()
        {
            if (_instance != null) return;

            var go = new GameObject("~StoryCutscene");
            StoryObjects.Persist(go);
            _instance = go.AddComponent<CutsceneRunner>();
            _instance.Build();
        }

        private void Build()
        {
            _canvas = StoryUi.CreateOverlayCanvas("CutsceneCanvas", 820);
            _canvas.transform.SetParent(transform, false);

            RectTransform root = StoryUi.Stretch(StoryUi.CreateRect("Root", _canvas.transform));

            Image blackout = StoryUi.CreateImage("Blackout", root, null, Color.black);
            StoryUi.Stretch(blackout.rectTransform);

            _stage = StoryUi.CreateRect("Stage", root);
            StoryUi.Stretch(_stage);

            _title = StoryUi.CreateText("Title", root, "", 52, Color.white, TextAnchor.UpperCenter);
            StoryUi.TopAnchored(_title.rectTransform, 120f, 90f, 60f);
            _title.fontStyle = FontStyle.Bold;

            gameObject.SetActive(false);
        }

        private void Begin(Cutscene scene, Action onDone)
        {
            _playing = true;
            gameObject.SetActive(true);
            _title.text = scene.Title;

            // The area as the player just left it — every repair visible. The beat
            // is the reward for the state of the room, so it plays over the room.
            foreach (Transform child in _stage) Destroy(child.gameObject);
            AreaSceneView.Attach(_stage, scene.AreaId);

            StoryProgress.MarkCutsceneSeen(scene.AreaId);

            DialogueSystem.Play(scene.Lines, () =>
            {
                _playing = false;
                gameObject.SetActive(false);
                if (onDone != null) onDone();
            });
        }

        #endregion
    }
}

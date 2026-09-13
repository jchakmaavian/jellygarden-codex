// Authored dialogue and resumable scene delivery for the new adventure.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class TreasureHuntPresentation
    {
        private static Dictionary<string, JsonValue> _scenes;
        public static bool IsPlaying { get; private set; }

        private static void Load()
        {
            if (_scenes != null) return;
            _scenes = new Dictionary<string, JsonValue>();
            TextAsset asset = StoryCampaign.LoadText("scenes");
            if (asset == null) return;
            foreach (JsonValue scene in JsonValue.Parse(asset.text)["scenes"].Items)
                _scenes[scene["id"].AsString()] = scene;
        }

        public static bool Has(string id)
        {
            Load();
            return !string.IsNullOrEmpty(id) && _scenes.ContainsKey(id);
        }

        public static IList<DialogueLine> LinesFor(string id)
        {
            Load();
            var lines = new List<DialogueLine>();
            JsonValue scene;
            if (!_scenes.TryGetValue(id, out scene)) return lines;
            foreach (JsonValue line in scene["lines"].Items)
                lines.Add(new DialogueLine(line["speaker"].AsString(), line["text"].AsString())
                    { StageCue = line["cue"].AsString() });
            return lines;
        }

        public static bool Play(string id, int areaId, Action onDone, bool replay = false)
        {
            if (!Has(id) || IsPlaying) return false;
            if (!replay && StoryProgress.IsSceneSeen(id)) return false;
            IsPlaying = true;
            StoryDialogueRoom.Show(areaId);
            TreasureMapDisplay.ShowScene(areaId, id == StoryCampaign.OpeningScene);
            DialogueSystem.LineStarted += TreasureMapDisplay.OnLine;
            DialogueSystem.Play(LinesFor(id), () =>
            {
                DialogueSystem.LineStarted -= TreasureMapDisplay.OnLine;
                // Skip and normal completion use the same continuation. The
                // completed area itself makes an interrupted payoff recoverable.
                if (!replay) StoryProgress.MarkSceneSeen(id);
                TreasureMapDisplay.HideScene();
                StoryDialogueRoom.Hide();
                IsPlaying = false;
                if (onDone != null) onDone();
            });
            return true;
        }

        public static bool PlayPending(int areaId, Action onDone)
        {
            if (!StoryCampaign.IsTreasureHunt || IsPlaying) return false;
            if (!StoryProgress.OpeningSeen && StoryProgress.LevelsBeatenCount > 0)
                return Play(StoryCampaign.OpeningScene, 1, () =>
                {
                    StoryProgress.MarkOpeningSeen();
                    if (onDone != null) onDone();
                });
            StoryArea area = StoryDatabase.GetArea(areaId);
            return area != null && StoryProgress.IsAreaComplete(areaId) &&
                   Play(area.PayoffScene, areaId, onDone);
        }

        public static void AfterTask(int areaId, StoryTask task, Action onDone)
        {
            StoryArea area = StoryDatabase.GetArea(areaId);
            if (StoryProgress.IsAreaComplete(areaId))
            {
                if (!Play(area.PayoffScene, areaId, onDone) && onDone != null) onDone();
                return;
            }
            if (string.IsNullOrWhiteSpace(task.OnComplete))
            {
                if (onDone != null) onDone();
                return;
            }
            StoryDialogueRoom.Show(areaId);
            if (areaId == 1 && task.Index == 3) TreasureMapDisplay.ShowBannerMimi();
            DialogueSystem.Play(new[] {new DialogueLine(task.Speaker, task.OnComplete)}, () =>
            {
                TreasureMapDisplay.HideScene();
                StoryDialogueRoom.Hide();
                if (onDone != null) onDone();
            });
        }
    }
}

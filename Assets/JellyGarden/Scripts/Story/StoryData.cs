// King Leo story layer — typed view over Resources/Story/*.json.
//
// The markdown in /Users/avian/Desktop/story is the source of truth; these
// classes are only a read model. Nothing here writes, and nothing here knows a
// specific area id — everything is driven by the data.

using System;
using System.Collections.Generic;

namespace JellyGarden.Scripts.Story
{
    /// <summary>One repair task inside an area. Costs stars, changes the scene.</summary>
    public sealed class StoryTask
    {
        public int Index;
        public string Label = string.Empty;
        public int Stars = 1;

        /// <summary>What the player sees when this task lands. Arc 1 only; may be empty.</summary>
        public string OnComplete = string.Empty;

        public static StoryTask From(JsonValue json)
        {
            return new StoryTask
            {
                Index = json["index"].AsInt(),
                Label = json["label"].AsString(),
                Stars = Math.Max(1, json["stars"].AsInt(1)),
                OnComplete = json["onComplete"].AsString()
            };
        }
    }

    /// <summary>The board spec for an area — which obstacle dresses this room.</summary>
    public sealed class StoryBoard
    {
        public string Obstacle = string.Empty;
        public string ObstacleNote = string.Empty;
        public readonly List<string> LevelTypes = new List<string>();

        public static StoryBoard From(JsonValue json)
        {
            var board = new StoryBoard
            {
                Obstacle = json["obstacle"].AsString(),
                ObstacleNote = json["obstacleNote"].AsString()
            };
            foreach (JsonValue item in json["levelTypes"].Items)
                board.LevelTypes.Add(item.AsString());
            return board;
        }
    }

    /// <summary>
    /// A hand-tuned level definition. Only Area 001 ships these; every other
    /// area's levels come from the generated level plan instead.
    /// </summary>
    public sealed class StoryLevelDef
    {
        public string Id = string.Empty;
        public int Index;
        public int Task;
        public int Moves;
        public int Colors;
        public string ObjectiveType = string.Empty;
        public int ObjectiveCount;
        public bool Unloseable;
        public float TargetWinRate;

        /// <summary>Raw node, kept so obstacle detail stays available without a schema.</summary>
        public JsonValue Raw = JsonValue.Null;

        public static StoryLevelDef From(JsonValue json)
        {
            return new StoryLevelDef
            {
                Id = json["id"].AsString(),
                Index = json["index"].AsInt(),
                Task = json["task"].AsInt(),
                Moves = json["moves"].AsInt(),
                Colors = json["colors"].AsInt(5),
                ObjectiveType = json["objective"]["type"].AsString(),
                ObjectiveCount = json["objective"]["count"].AsInt(),
                Unloseable = json["unloseable"].AsBool(),
                TargetWinRate = json["targetWinRate"].AsFloat(),
                Raw = json
            };
        }
    }

    /// <summary>One of the 150 areas. The unit of story, repair and map node.</summary>
    public sealed class StoryArea
    {
        public int Id;
        public int Arc;
        public string Name = string.Empty;
        public string Phase = string.Empty;
        public string BeatType = string.Empty;
        public string Goal = string.Empty;
        public string Clue = string.Empty;
        public int PointsTo;
        public string Status = "locked";
        public string Source = string.Empty;

        public readonly List<StoryTask> Tasks = new List<StoryTask>();
        public StoryBoard Board = new StoryBoard();
        public readonly List<StoryLevelDef> Levels = new List<StoryLevelDef>();

        /// <summary>Stars needed to finish the area. Summed from tasks, not trusted from the file.</summary>
        public int StarCost
        {
            get
            {
                int total = 0;
                for (int i = 0; i < Tasks.Count; i++) total += Tasks[i].Stars;
                return total;
            }
        }

        /// <summary>Every fifth area is an arc beat and gets a cutscene, not a clue card.</summary>
        public bool IsArcBeat
        {
            get { return Id % 5 == 0; }
        }

        /// <summary>The six arc finales — the big payoffs.</summary>
        public bool IsArcFinale
        {
            get { return Id % 25 == 0; }
        }

        public static StoryArea From(JsonValue json, int fallbackArc)
        {
            var area = new StoryArea
            {
                Id = json["id"].AsInt(),
                Arc = json["arc"].AsInt(fallbackArc),
                Name = json["name"].AsString(),
                Phase = json["phase"].AsString(),
                BeatType = json["beatType"].AsString(),
                Goal = json["goal"].AsString(),
                Clue = json["clue"].AsString(),
                PointsTo = json["pointsTo"].AsInt(),
                Status = json["status"].AsString("locked"),
                Source = json["source"].AsString(),
                Board = StoryBoard.From(json["board"])
            };

            foreach (JsonValue task in json["tasks"].Items)
                area.Tasks.Add(StoryTask.From(task));
            foreach (JsonValue level in json["levels"].Items)
                area.Levels.Add(StoryLevelDef.From(level));

            return area;
        }
    }

    /// <summary>One arc's summary row from index.json — used for chapter banners.</summary>
    public sealed class StoryArcInfo
    {
        public int Arc;
        public string Title = string.Empty;
        public int FirstArea;
        public int LastArea;
        public int AreaCount;
        public int TaskCount;
        public int StarCount;

        public static StoryArcInfo From(JsonValue json)
        {
            return new StoryArcInfo
            {
                Arc = json["arc"].AsInt(),
                Title = json["title"].AsString(),
                FirstArea = json["firstArea"].AsInt(),
                LastArea = json["lastArea"].AsInt(),
                AreaCount = json["areaCount"].AsInt(),
                TaskCount = json["taskCount"].AsInt(),
                StarCount = json["starCount"].AsInt()
            };
        }
    }
}

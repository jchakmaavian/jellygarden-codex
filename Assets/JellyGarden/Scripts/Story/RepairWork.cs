// What kind of work a repair task actually is.
//
// The 807 tasks use 83 different verbs, but they collapse into ten kinds of
// work: you clear a thing away, you rebuild it, you light it, you hang it, you
// polish it, you restock it, or you force it open. Classifying by the story's
// own words means a repair cutscene can be specific to the task without anyone
// authoring 807 of them — "Repave the cracked stones" gets stonework and dust,
// "Relight the gate lanterns" gets a flame taking hold.
//
// The classifier reads the whole label, not just the first word, because
// "Clear the flooded corridor" is water and "Clear the last rubble" is stone.
// Which also means a new needle has to be checked against all 807 labels before
// it is added: "oil" would have caught "Fix the boiler", so the needle is
// "re-oil".

using System;

namespace JellyGarden.Scripts.Story
{
    public enum RepairWorkType
    {
        Clear,      // haul, sweep, cut back, dig out — debris leaves
        Water,      // drain, pump, dry, douse — water leaves
        Rebuild,    // repair, fix, shore, repave, re-lay — structure returns
        Light,      // light, relight — flame takes hold
        Hang,       // rehang, raise, right, set — something lifts into place
        Polish,     // restore, clean, polish, recover — surfaces come back
        Restock,    // restock, refill, plant — things fill up again
        Force,      // break, force, fight — something gives way
        Search,     // search, prepare, rig — quieter work
        General
    }

    public static class RepairWork
    {
        /// <summary>
        /// The kind of work a task label describes. Matching is ordered: the
        /// specific cases (water, fire) are tested before the general ones,
        /// because "clear the flooded corridor" is water before it is clearing.
        /// </summary>
        public static RepairWorkType Classify(string label)
        {
            if (string.IsNullOrEmpty(label)) return RepairWorkType.General;
            string text = label.ToLowerInvariant();

            // Water first — it hides inside clear/repair labels.
            if (ContainsAny(text, "flood", "water", "drain", "pump", "bilge",
                    "damp", "dry out", "douse", "well", "cistern", "trough", "moat"))
                return RepairWorkType.Water;

            // Then fire, for the same reason.
            if (ContainsAny(text, "light", "lantern", "lamp", "torch", "candle",
                    "brazier", "hearth", "fire", "forge"))
                return ContainsAny(text, "douse", "put out")
                    ? RepairWorkType.Water
                    : RepairWorkType.Light;

            if (ContainsAny(text, "haul", "clear", "sweep", "cut", "dig", "remove",
                    "rubble", "debris", "weed", "ivy", "ash", "clean out", "muck"))
                return RepairWorkType.Clear;

            if (ContainsAny(text, "break", "force", "fight", "smash", "batter", "ram"))
                return RepairWorkType.Force;

            if (ContainsAny(text, "rehang", "re-hang", "raise", "right", "hang",
                    "banner", "hoist", "set up", "stand"))
                return RepairWorkType.Hang;

            if (ContainsAny(text, "restock", "refill", "plant", "stock", "fill",
                    "sow", "provision"))
                return RepairWorkType.Restock;

            // "re-oil", not "oil": "Fix the boiler" is rebuilding a boiler, not
            // dressing leather.
            if (ContainsAny(text, "polish", "restore", "clean", "recover", "scrub",
                    "shine", "wash", "re-oil"))
                return RepairWorkType.Polish;

            if (ContainsAny(text, "repair", "rebuild", "fix", "shore", "repave",
                    "re-lay", "relay", "seat", "secure", "assemble", "mend",
                    "replace", "re-set", "reset", "patch", "brace"))
                return RepairWorkType.Rebuild;

            if (ContainsAny(text, "search", "prepare", "rig", "sort", "read", "check"))
                return RepairWorkType.Search;

            return RepairWorkType.General;
        }

        /// <summary>
        /// A line for the work itself, used while it is happening. The story
        /// authors an onComplete line for arc 1 only, and a repair with nothing
        /// to say is the thing that made this feel like a checklist.
        /// </summary>
        public static string WorkingLine(RepairWorkType work)
        {
            switch (work)
            {
                case RepairWorkType.Clear: return "Hauling it out, load by load.";
                case RepairWorkType.Water: return "The water goes down. Slowly.";
                case RepairWorkType.Rebuild: return "New timber, new stone. It holds.";
                case RepairWorkType.Light: return "The wick catches.";
                case RepairWorkType.Hang: return "Up, and straight, and fast.";
                case RepairWorkType.Polish: return "Under the grime, it was never ruined.";
                case RepairWorkType.Restock: return "Filling it back to where it was.";
                case RepairWorkType.Force: return "It gives.";
                case RepairWorkType.Search: return "Working through it, piece by piece.";
                default: return "The work goes on.";
            }
        }

        /// <summary>
        /// Fallback for the line after the work, when the story did not author
        /// one. Deliberately plain — the story's own words win wherever they
        /// exist, and these only fill the gap.
        /// </summary>
        public static string CompletedLine(RepairWorkType work)
        {
            switch (work)
            {
                case RepairWorkType.Clear: return "Clear. You can see the floor again.";
                case RepairWorkType.Water: return "Dry. The damp smell is going.";
                case RepairWorkType.Rebuild: return "Sound again. It will take weight.";
                case RepairWorkType.Light: return "Warm light, where there was none.";
                case RepairWorkType.Hang: return "Back where it belongs.";
                case RepairWorkType.Polish: return "It shines. Someone cared once.";
                case RepairWorkType.Restock: return "Full again. Like a house with people in it.";
                case RepairWorkType.Force: return "Open. Whatever is in there is ours now.";
                case RepairWorkType.Search: return "Found what there was to find.";
                default: return "Done. One more room the castle gets back.";
            }
        }

        /// <summary>Which story sound suits this work.</summary>
        public static string SoundKey(RepairWorkType work)
        {
            switch (work)
            {
                case RepairWorkType.Clear: return StorySfx.VineCut;
                case RepairWorkType.Rebuild:
                case RepairWorkType.Force: return StorySfx.TimberBreak;
                case RepairWorkType.Light: return StorySfx.LanternLight;
                default: return StorySfx.RepairDone;
            }
        }

        private static bool ContainsAny(string text, params string[] needles)
        {
            for (int i = 0; i < needles.Length; i++)
                if (text.IndexOf(needles[i], StringComparison.Ordinal) >= 0) return true;
            return false;
        }
    }
}

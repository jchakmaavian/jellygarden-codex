// Whether a board VFX layer is switched on, on every platform.
//
// WHY THIS EXISTS
//
// The five board effects — rocket, pop, shatter, blast, finish — each read
// their own switch out of the command line:
//
//     foreach (string arg in Environment.GetCommandLineArgs())
//         if (arg == "-storyrocket") ...
//
// That works on a desktop player launched by tools/run_player.sh and nowhere
// else. **An iOS app has no command line**: GetCommandLineArgs() returns the
// executable path and nothing more, so every one of the five reads false and
// the phone shows the stock kit board. The Editor has the same problem for the
// same reason, which is why StoryThemeTest already carries a PlayerPrefs
// fallback and says in its own comment that "an Editor runner cannot pass a
// command line".
//
// So the rule lives here, once, rather than in five copies that would drift
// apart the first time one of them was tuned — which is exactly what happened
// to the rocket's own constants.
//
// THE ORDER, AND WHY
//
//   1. THE COMMAND LINE WINS when the flag is present. A desktop run that says
//      -storyrocket gets the rocket, and a sweep that deliberately leaves the
//      flags off still gets the kit's untouched behaviour. Nothing about the
//      existing Mac workflow changes.
//
//   2. PLAYERPREFS NEXT, when the key has been written. This is the seam an
//      Editor menu item or a device-side toggle uses, and it is how a phone
//      build can be switched without a rebuild.
//
//   3. OTHERWISE ON IN A DEVELOPMENT BUILD, off in a release one.
//
// Point 3 is the part worth arguing about, so here is the argument. On a fresh
// iOS install there is no command line AND no pref yet, so a rule that stopped
// at point 2 would still show the stock board on the first launch — the phone
// would be exactly as blind as before. Defaulting on under Debug.isDebugBuild
// makes a development build simply work when it is installed on a device,
// while a release build keeps the kit's behaviour reachable with no flags at
// all, which is what the five layers were gated for in the first place.
//
// This does change what a development build looks like by default, on every
// platform. That is deliberate and it is the whole point of the file; anything
// that needs the old behaviour can still pass the flag's OFF form or set the
// pref to 0.

using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryVfxFlags
    {
        /// <summary>
        /// Whether the layer named by <paramref name="flag"/> is on.
        /// </summary>
        /// <param name="flag">
        /// The command-line switch, with its leading dash — "-storyrocket".
        /// Its OFF form is the same word with "-no" in front: "-nostoryrocket",
        /// which forces the layer off even in a development build.
        /// </param>
        /// <param name="pref">
        /// The PlayerPrefs key for the same layer. 1 is on, 0 is off, absent
        /// means "not decided here".
        /// </param>
        public static bool On(string flag, string pref)
        {
            string off = "-no" + flag.Substring(1);

            foreach (string arg in global::System.Environment.GetCommandLineArgs())
            {
                if (arg == off) return false;
                if (arg == flag) return true;
            }

            // Only when the key actually exists: GetInt with a default cannot
            // tell "set to 0" from "never set", and those mean different things
            // at point 3 below.
            if (PlayerPrefs.HasKey(pref)) return PlayerPrefs.GetInt(pref, 0) == 1;

            // ON in every build, development and release alike.
            //
            // This returned Debug.isDebugBuild, and that was right while the
            // five layers were experiments a launch opted into. They are not
            // any more: the clear effect, the rocket trail and the bomb were
            // each compared on a real board and chosen, and the three variant
            // registries now return those choices unconditionally.
            //
            // Leaving the fallback on isDebugBuild would have shipped a release
            // build where StoryVfxVariant reports GlassBurst, StoryRocketVariant
            // reports HeavyExhaust and StoryBombVariant reports its blast — and
            // NONE of them draw, because every layer that renders them reads
            // this method and would have got false. The registries would have
            // been telling the truth about a decision nothing acted on.
            //
            // Both off-ramps survive: "-nostoryrocket" and friends still force a
            // layer off, and a pref set to 0 still wins over this. So the kit's
            // untouched behaviour is still reachable, which is what the gating
            // existed for in the first place.
            return true;
        }

        /// <summary>
        /// Write the pref for one layer, so a device or Editor run can turn it
        /// on or off without a rebuild. Takes effect on the next scene load,
        /// because each layer caches its own answer on first read.
        /// </summary>
        public static void Set(string pref, bool on)
        {
            PlayerPrefs.SetInt(pref, on ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}

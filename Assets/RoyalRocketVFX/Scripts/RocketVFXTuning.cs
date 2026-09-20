using System;
using UnityEngine;

namespace RoyalRocketVFX
{
    /// <summary>
    /// Every tuning value the rocket effect uses. Production defaults below are the values
    /// signed off in Royal Throne; change them freely, nothing else in the package assumes them.
    /// </summary>
    [Serializable]
    public sealed class RocketVFXTuning
    {
        [Header("Motion")]
        [Tooltip("Constant travel speed in cells per second. One speed for every direction and " +
                 "origin, so horizontal and vertical rockets read at the same pace.")]
        [Range(4f, 40f)] public float travelSpeed = 15f;

        [Tooltip("World units per board cell. Play() can override this per call.")]
        [Min(0.01f)] public float cellSize = 1f;

        [Tooltip("Squash-and-hold before ignition, in seconds. Keep it short so activation feels instant.")]
        [Range(0f, 0.4f)] public float anticipation = 0.12f;

        [Tooltip("Rocket sprite length along its travel axis, in cells. Also sets how far ahead of the " +
                 "rocket centre a cell counts as reached.")]
        [Range(0.2f, 1.5f)] public float rocketScale = 0.64f;

        [Header("Exhaust")]
        [Tooltip("Visual plume length in cells. Trail lifetimes are derived as length / speed, so the " +
                 "plume keeps this length at any speed - never slow the rocket to lengthen the trail.")]
        [Range(2f, 12f)] public float exhaustLengthInCells = 7f;

        [Tooltip("Brightness of the flame ribbon and nozzle flame.")]
        [Range(0.2f, 2.5f)] public float flameIntensity = 1f;

        [Tooltip("Multiplier on the four-point star sparkles emitted along the plume.")]
        [Range(0f, 3f)] public float starDensity = 1f;

        [Tooltip("Opacity multiplier for the soft white smoke tail. 0 disables smoke entirely.")]
        [Range(0f, 2f)] public float smokeAmount = 1f;

        [Header("Rendering")]
        [Tooltip("Sorting layer for every rocket renderer. Must exist in the importing project; " +
                 "'Default' always does.")]
        public string sortingLayer = "Default";

        [Tooltip("Sorting order of the rocket sprite. Trail, flame and particles are offset from it.")]
        public int sortingOrder = 36;

        [Header("Pool")]
        [Tooltip("How many rockets can be in flight at once. Two per cross activation.")]
        [Range(2, 32)] public int poolSize = 8;

        [Tooltip("Extra cells travelled past the screen edge before the rocket retires.")]
        [Range(0f, 6f)] public float screenExitPadding = 2.2f;

        public RocketVFXTuning Clone() => (RocketVFXTuning)MemberwiseClone();
    }
}

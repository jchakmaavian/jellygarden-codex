namespace RoyalThrone.Match3Vfx
{
    /// <summary>Snapshot of pool usage for QA overlays and profiling. All zero before the rig exists.</summary>
    public struct Match3VfxStats
    {
        public bool rigReady;
        public int pendingOperations;
        public int activeProjectiles, peakProjectiles, projectileCapacity;
        public int activeDebris, peakDebris, debrisCapacity;
        /// <summary>Colour indices with painted chips from the burst library.</summary>
        public int paintedChipColors;
        public int activeOverlays;
        public int particles;
        public int activeVoices, peakVoices, voiceCapacity;
        public int blasts, rocketLaunches, rocketCellContacts, explosions, helicopterFlights, orbActivations;
        public int bannerPresentations;
        public int impactPrefabsActive;

        public override string ToString()
        {
            return "Match3Vfx • ops " + pendingOperations + " • projectiles " + activeProjectiles + "/" + projectileCapacity +
                   " • debris " + activeDebris + "/" + debrisCapacity + " • particles " + particles +
                   " • voices " + activeVoices + "/" + voiceCapacity;
        }
    }

    public static class Match3VfxDiagnostics
    {
        /// <summary>Current pool usage. Cheap enough for a once-per-frame QA label.</summary>
        public static Match3VfxStats Read()
        {
            var rig = Match3Vfx.ExistingRig;
            if (!rig || rig.Burst == null) return default;
            return new Match3VfxStats
            {
                rigReady = true,
                pendingOperations = rig.Sequences.PendingOperations,
                activeProjectiles = rig.Powerups.ActiveProjectiles,
                peakProjectiles = rig.Powerups.PeakProjectiles,
                projectileCapacity = rig.Powerups.ProjectileCapacity,
                activeDebris = rig.Shatter.ActiveFragments,
                peakDebris = rig.Shatter.PeakFragments,
                debrisCapacity = rig.Shatter.Capacity,
                paintedChipColors = rig.Shatter.AuthoredColors,
                activeOverlays = rig.Burst.ActiveOverlays,
                particles = rig.Burst.ParticleCount + rig.Powerups.ParticleCount,
                activeVoices = rig.Sfx.ActiveVoices,
                peakVoices = rig.Sfx.PeakVoices,
                voiceCapacity = rig.Sfx.Capacity,
                blasts = rig.Powerups.Blasts,
                rocketLaunches = rig.Sequences.RocketLaunches,
                rocketCellContacts = rig.Sequences.RocketCellContacts,
                explosions = rig.Sequences.Explosions,
                helicopterFlights = rig.Sequences.HelicopterFlights,
                orbActivations = rig.Sequences.OrbActivations,
                bannerPresentations = rig.Banner.Presentations,
                impactPrefabsActive = rig.Impacts.ActiveCount
            };
        }
    }
}

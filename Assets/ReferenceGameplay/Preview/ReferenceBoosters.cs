using UnityEngine;

namespace RoyalThrone.ReferenceGameplay.Preview
{
    // The Rocket, Propeller and Color Ball imported from royal-throne, with the combos,
    // match and win effects, by the names and timings the game used there
    // (ReferencePieceVisual, PropellerVisualV5, ColorBallVisualV4, ReferenceDualColorBallVisual,
    // RoyalRocketSequence, ReferenceWinFinish, RoyalWinConfetti). Loading goes through ReferenceTnt.
    public static class ReferenceBoosters
    {
        public const string RocketKey = "Rocket", PropellerKey = "Propeller", ColorBallKey = "ColorBall";
        public const string ColorBallComboKey = "ColorBallColorBallComboRfv387";

        public const string RocketHorizontalState = "HorizontalRocketCreationAnimation", RocketVerticalState = "VerticalRocketCreationAnimation";
        public const string PropellerCreationState = "PropellerCreationAnimation", PropellerTakeoffState = "PropellerTapStartFlyAnimation";
        public const string PropellerFlyState = "PropellerFlyAnimation";
        public const string ColorBallCreationState = "LightballCreationAnimation", ColorBallDefaultState = "LightballDefaultAnimation";
        public const string ColorBallUseState = "LightballUseAnimation", ColorBallComboState = "BallBallExplodeAnimation";

        public const string RocketTrailVfx = "RocketItemParticles";
        public const string PropellerTakeoffVfx = "PropellerItemExplodeParticles", PropellerImpactVfx = "PropellerTargetExplodeParticles";
        public const string PropellerRocketVfx = "PropellerRocketComboParticles", PropellerTntFuseVfx = "TntPropellerComboFuseParticles";
        public const string TntRocketChargeVfx = "TntRocketComboUseParticles", TntRocketVfx = "TntRocketComboParticles";
        public const string ColorBallUseVfx = "LightballUseParticles", ColorBallExplodeVfx = "LightballItemExplodeParticles";
        public const string ColorBallContactVfx = "LightballEffectedItemExplodeParticles", ColorBallRayVfx = "LightballRayPrefab";
        public const string ColorBallComboChargeVfx = "BallBallComboUseParticles", ColorBallComboExplodeVfx = "BallBallComboExplodeParticles";
        public const string MatchVfx = "MatchItemExplodeParticles", SwapVfx = "MatchItemSwapParticles";
        public const string MovesFlightVfx = "RemainingMovesParticles", MovesHitVfx = "RemainingMovesHitParticles";
        public const string StarHitVfx = "StarHitParticles", StarIdleVfx = "StarIdleParticles";

        public const string RocketCreationAudio = "rocket_creation__15581", RocketAudio = "rocket_explode__15712";
        public const string PropellerCreationAudio = "propeller_creation__15500", PropellerTakeoffAudio = "propeller_take_off__15745";
        public const string PropellerFlyAudio = "propeller_fly__15648", PropellerHitAudio = "propeller_hit__15578";
        public const string PropellerTntAudio = "propeller_tnt_combo__rfv387_17641", RocketTntAudio = "rocket_tnt_combo__rfv387_17431";
        public const string ColorBallCreationAudio = "lightball_creation__15586", ColorBallStartAudio = "lightball_start__15692";
        public const string ColorBallExplodeAudio = "lightball_explode__15567", ColorBallComboAudio = "lightball_lightball_combo__rfv387_17531";
        public static readonly string[] ColorBallRayAudio = { "lightball_ray1__15747", "lightball_ray2__15961", "lightball_ray3__15912" };
        public static readonly string[] MatchAudio =
        {
            "match_explode1__15971", "match_explode2__15612", "match_explode3__15652",
            "match_explode4__15914", "match_explode5__15888", "match_explode6__15850"
        };

        // Board art a Propeller carries in a Propeller+TNT / Propeller+Rocket swap (ReferenceGameplayCatalog "tnt", "rocket").
        public const string TntSprite = "Items-SpecialItems-TntItem-tnt__16602", RocketSprite = "Items-SpecialItems-RocketItem-horizontal_rocket__16419";

        // The Rocket prefab's two orientations share it as branches (ReferencePieceVisual.RocketBranches).
        public const string RocketHorizontalBranch = "Horizontal", RocketVerticalBranch = "Vertical";
        // The APK's flight art for each half: nose forward, fins behind.
        public const string RocketLeftSprite = "Items-SpecialItems-RocketItem-horizontal_rocket_part_left__16907";
        public const string RocketRightSprite = "Items-SpecialItems-RocketItem-horizontal_rocket_part_right__21917";
        public const string RocketTopSprite = "Items-SpecialItems-RocketItem-vertical_rocket_part_top__16348";
        public const string RocketBottomSprite = "Items-SpecialItems-RocketItem-vertical_rocket_part_bottom__21611";

        // EffectsProfile rocketCellsPerSecond 15 × rocketAnimationRate 0.95.
        public const float RocketCellsPerSecond = 15 * .95f;
        // PropellerVisualV5.
        public const float PropellerActivationSeconds = .46f, PropellerTakeoffAt = .355f, PropellerImpactSeconds = .18f;
        // ColorBallVisualV4 and AnimateColorBallV4Targets.
        public const float ColorBallRevealSeconds = .76f, ColorBallChargeSeconds = .32f, ColorBallFinalPulseAt = .21f;
        public const float ColorBallFinishSeconds = .30f, ColorBallBeamGap = .09f, ColorBallBeamFade = .14f;
        // Sorting orders the game uses: VFX 35, combo body 36, a booster in play 38, a carried booster 37/39, rays 30.
        public const int RayOrder = 30, FlyerOrder = 36, BoosterOrder = 38;

        public static Sprite Sprite(string name) => Resources.Load<Sprite>("ReferenceGameplay/Sprites/" + name);

        // PropellerMotionV5: flight length, quadratic arc height and eased progress, by distance in cells.
        public static float PropellerFlightDuration(float cells) => Mathf.Clamp(.40f + .06f * cells, .45f, .90f);
        public static float PropellerArc(float cells) => Mathf.Clamp(.35f + .06f * cells, .42f, .85f);
        public static float PropellerEase(float t, float duration)
        {
            t = Mathf.Clamp01(t);
            float entry = 1.2f * duration / (3 * .30f);
            return (-2 * t * t * t + 3 * t * t) + entry * (t * t * t - 2 * t * t + t) + .22f * (t * t * t - t * t);
        }

        // ColorBallVfxV4.Launch: a beam crosses its distance at 17 cells a second, within 0.14-0.37s.
        public static float RayDuration(float cells) => Mathf.Clamp(cells / 17f, .14f, .37f);
    }
}

namespace RoyalThrone.Characters
{
    /// <summary>One recent positive response, never a cascade backlog. The presenter owns visibility and board-attempt scope.</summary>
    public sealed class KingVanguardFeedbackQueue
    {
        public enum Feedback { None, Match, Powerup, Impact }
        public const float ImpactLifetime = 1.25f;
        public const float ImpactThreshold = .75f;
        public static Feedback FromImpactStrength(float strength) => strength >= ImpactThreshold ? Feedback.Impact : strength > .5f ? Feedback.Powerup : Feedback.None;
        public const float MatchLifetime = 2f;
        public const float PowerupLifetime = 3f;
        public const float MatchSpacing = 2f;
        public const float PowerupSpacing = 4f;
        Feedback pending;
        float expiresAt, nextAllowedAt;

        public void Offer(Feedback feedback, float now)
        {
            Expire(now);
            if (feedback == Feedback.None || feedback < pending) return;
            pending = feedback;
            expiresAt = now + (feedback == Feedback.Impact ? ImpactLifetime : feedback == Feedback.Powerup ? PowerupLifetime : MatchLifetime);
        }

        public bool TryGet(float now, out Feedback feedback)
        {
            Expire(now);
            feedback = pending;
            return pending != Feedback.None && (pending == Feedback.Impact || now >= nextAllowedAt);
        }

        // Call only after the animation accepted the response. A busy actor leaves the one pending request intact.
        public void Accepted(float now)
        {
            if (pending == Feedback.None) return;
            nextAllowedAt = now + (pending == Feedback.Impact ? KingVanguardCharacter.ImpactLaughCooldown : pending == Feedback.Powerup ? PowerupSpacing : MatchSpacing);
            pending = Feedback.None;
        }

        public void Clear()
        { pending = Feedback.None; expiresAt = nextAllowedAt = 0; }

        void Expire(float now)
        { if (pending != Feedback.None && now >= expiresAt) pending = Feedback.None; }
    }
}

namespace RoyalThrone.Characters
{
    /// <summary>One recent positive response, never a cascade backlog. The presenter owns visibility and board-attempt scope.</summary>
    public sealed class KingVanguardFeedbackQueue
    {
        public enum Feedback { None, Match, Powerup }
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
            expiresAt = now + (feedback == Feedback.Powerup ? PowerupLifetime : MatchLifetime);
        }

        public bool TryGet(float now, out Feedback feedback)
        {
            Expire(now);
            feedback = pending;
            return pending != Feedback.None && now >= nextAllowedAt;
        }

        // Call only after the animation accepted the response. A busy actor leaves the one pending request intact.
        public void Accepted(float now)
        {
            if (pending == Feedback.None) return;
            nextAllowedAt = now + (pending == Feedback.Powerup ? PowerupSpacing : MatchSpacing);
            pending = Feedback.None;
        }

        public void Clear()
        { pending = Feedback.None; expiresAt = nextAllowedAt = 0; }

        void Expire(float now)
        { if (pending != Feedback.None && now >= expiresAt) pending = Feedback.None; }
    }
}

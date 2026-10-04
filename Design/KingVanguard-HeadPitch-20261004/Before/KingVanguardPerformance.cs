using UnityEngine;

namespace RoyalThrone.Characters
{
    // Presentation only: observes gameplay, never queues actions or consumes input.
    // Every channel retains its velocity when an activity is interrupted.
    public sealed class KingVanguardPerformance
    {
        struct Spring
        {
            public float value, velocity;
            public void Step(float target, float seconds, float dt)
            {
                float omega = 2f / seconds, offset = value - target;
                float carry = velocity + omega * offset, decay = Mathf.Exp(-omega * dt);
                value = target + (offset + carry * dt) * decay;
                velocity = (velocity - omega * carry * dt) * decay;
            }
        }

        Spring worry, sad, happy, excited, laugh, eyeX, eyeY, headX, headY, roll, bodyY, bodyRoll, jaw, brow, wink;
        KingVanguardCharacter.Activity previous;
        float clock, idleAge, gestureAge = 99, nextGesture = 3.2f;
        float tapAge = 99, previousTap, lastTap = -10, blinkAt = 3.1f, blinkAge = 99;
        int gesture, variation, blinkIndex, sourceLast;
        bool initialized;
        float lookTime;
        // Screen convention: negative Y looks toward the board below the portrait.
        static readonly Vector2[] LookRoute = {
            new Vector2(0,-.78f),new Vector2(-.62f,-.70f),new Vector2(.62f,-.70f),
            new Vector2(0,-.75f),Vector2.zero,
            new Vector2(-.72f,.62f),new Vector2(-.72f,-.62f),
            new Vector2(.72f,-.62f),new Vector2(.72f,.62f),Vector2.zero
        };
        static readonly float[] LookHolds = {1.5f,1.65f,1.65f,1.1f,.9f,1.45f,1.45f,1.45f,1.45f,1.2f};
        public Vector2 LookTarget { get; private set; }
        public int LookStage { get; private set; } = -1;

        void UpdateLookAround(float delta, bool allowed)
        {
            if (!allowed) { lookTime=0; LookTarget=Vector2.zero; LookStage=-1; return; }
            lookTime+=delta;
            float at=Mathf.Repeat(lookTime,20.2f)-1.8f;
            LookTarget=Vector2.zero; LookStage=-1;
            if(at<0)return;
            for(int i=0;i<LookRoute.Length;i++)
            {
                if(at<LookHolds[i])
                {
                    LookStage=i;
                    LookTarget=Vector2.Lerp(i==0?Vector2.zero:LookRoute[i-1],LookRoute[i],Mathf.SmoothStep(0,1,at/.52f));
                    return;
                }
                at-=LookHolds[i];
            }
        }
        public float Happy => happy.value;
        public float Excited => excited.value;
        public float Laugh => laugh.value;
        public float Worry => worry.value;
        public float Sad => sad.value;
        public float Jaw => jaw.value;
        public Vector2 Eyes => new Vector2(eyeX.value, eyeY.value);
        public Vector4 Head => new Vector4(headX.value, headY.value, roll.value, brow.value);
        public Vector2 Body => new Vector2(bodyY.value, bodyRoll.value);
        public Vector2 Lids { get; private set; }
        public float Clock => clock;
        public int GestureCount { get; private set; }

        public void Reset()
        {
            worry = sad = happy = excited = laugh = eyeX = eyeY = headX = headY = roll = bodyY = bodyRoll = jaw = brow = wink = default;
            clock = idleAge = previousTap = 0;
            gestureAge = tapAge = blinkAge = 99; lastTap = -10; nextGesture = 3.2f; blinkAt = 3.1f;
            gesture = variation = blinkIndex = sourceLast = 0; GestureCount = 0;
            previous = KingVanguardCharacter.Activity.Idle; initialized = true; Lids = Vector2.zero;
            lookTime=0; LookTarget=Vector2.zero; LookStage=-1;
        }

        static float Bell(float age, float start, float duration)
        {
            float u = (age - start) / duration;
            return u <= 0 || u >= 1 ? 0 : Mathf.Pow(Mathf.Sin(u * Mathf.PI), 2);
        }
        static float BlinkCurve(float age)
        {
            return Mathf.SmoothStep(0, 1, age / .075f) * (1 - Mathf.SmoothStep(0, 1, (age - .105f) / .135f));
        }

        public void Step(KingVanguardCharacter.Activity activity, float delta, float phase, Vector2 gaze, float lean, float touch, float laughTime, int source)
        {
            if (!initialized) Reset();
            if (delta <= 0) return; // Rebuilds and paused callbacks must not change a visible pose.
            float dt = Mathf.Min(delta, .1f);
            if (activity != previous) { variation++; previous = activity; }
            clock += dt; tapAge += dt; gestureAge += dt; blinkAge += dt;
            bool idle = activity == KingVanguardCharacter.Activity.Idle;
            bool concerned = activity == KingVanguardCharacter.Activity.Worried;
            bool failed = activity == KingVanguardCharacter.Activity.Sad;
            bool free = idle || concerned;
            idleAge = idle ? idleAge + dt : 0;
            if (touch > .04f && previousTap <= .04f && clock - lastTap > .65f && free)
            {
                tapAge = 0; lastTap = clock; variation++;
            }
            previousTap = touch;
            // A hint gets a deliberate small nod, once on acquisition, not every frame.
            if (source == 3 && sourceLast != 3 && free) { gesture = 3; gestureAge = 0; GestureCount++; }
            sourceLast = source;
            if (idleAge > nextGesture && source == 0 && gaze.sqrMagnitude < .01f)
            {
                gesture = GestureCount % 3; GestureCount++; gestureAge = 0;
                nextGesture = idleAge + 5.1f + (GestureCount % 3) * .85f;
            }
            if (!idle) nextGesture = 3.2f;
            if (clock >= blinkAt)
            {
                blinkAge = 0; blinkIndex++;
                blinkAt = clock + (blinkIndex % 5 == 0 ? .36f : 3.3f + (blinkIndex % 4) * .43f);
            }

            float idleGesture = idle && source == 0 ? Bell(gestureAge, 0, 2.4f) : 0;
            float curious = gesture == 0 ? idleGesture : 0;
            float pleased = gesture == 1 ? idleGesture : 0;
            float greeting = gesture == 2 ? idleGesture : 0;
            float hintNod = free && source == 3 ? Bell(gestureAge, .15f, .9f) : 0;
            float tapNod = free ? Bell(tapAge, 0, .65f) : 0;
            float touchSmile = free && !concerned ? Mathf.SmoothStep(0, 1, tapAge / .14f) * (1 - Mathf.SmoothStep(0, 1, (tapAge - .48f) / .72f)) * .65f : 0;
            float envelope = Mathf.SmoothStep(0, 1, phase / .13f) * (1 - Mathf.SmoothStep(0, 1, (phase - .72f) / .28f));
            float targetHappy = 0, targetExcited = 0, nod = 0, tilt = 0, chest = 0, eyebrow = 0;
            float side = variation % 2 == 0 ? 1 : -1;
            switch (activity)
            {
                case KingVanguardCharacter.Activity.Idle:
                    targetHappy = Mathf.Max(touchSmile, pleased * .48f + greeting * .3f);
                    tilt = curious * -1.6f + pleased * .8f;
                    nod = greeting * 6 - tapNod * 9 - hintNod * 5;
                    eyebrow = curious * 2.8f + tapNod * 2;
                    break;
                case KingVanguardCharacter.Activity.MatchAcknowledgement:
                    targetHappy = envelope * .82f;
                    nod = -Bell(phase, .02f, .62f) * 13 + Bell(phase, .54f, .4f) * 3;
                    tilt = side * envelope * .6f; chest = envelope * 2;
                    break;
                case KingVanguardCharacter.Activity.Happy:
                    targetHappy = envelope;
                    nod = envelope * (5 + Mathf.Sin(phase * Mathf.PI * 4) * 7);
                    tilt = side * envelope * 1.25f; chest = envelope * 3;
                    break;
                case KingVanguardCharacter.Activity.Excited:
                case KingVanguardCharacter.Activity.ExtraMoves:
                    targetExcited = envelope;
                    float bounce = Mathf.Sin(phase * Mathf.PI * 6);
                    nod = -Bell(phase, 0, .17f) * 7 + envelope * (6 + bounce * 10);
                    chest = envelope * (3 + Mathf.Sin(phase * Mathf.PI * 6 - .45f) * 4);
                    tilt = side * envelope * Mathf.Sin(phase * Mathf.PI * 2) * 1.6f;
                    eyebrow = envelope * 2;
                    break;
                case KingVanguardCharacter.Activity.Relief:
                    targetHappy = envelope;
                    nod = envelope * 9 - Bell(phase, .5f, .5f) * 5;
                    chest = -envelope * 4; tilt = -envelope * 1.3f;
                    break;
                case KingVanguardCharacter.Activity.Worried:
                    nod = -6 - tapNod * 3 - hintNod * 3;
                    tilt = Mathf.Sin(clock * .83f) * .55f; chest = -2;
                    break;
                case KingVanguardCharacter.Activity.Sad:
                    nod = -13; tilt = -1.2f; chest = -5;
                    break;
                case KingVanguardCharacter.Activity.Laughing:
                    float chuckle = KingVanguardLaughTiming.PosePulse(laughTime);
                    nod = chuckle * 11; chest = chuckle * 5;
                    tilt = Mathf.Sin(laughTime * 4.3f) * .85f;
                    break;
            }
            float targetLaugh = activity == KingVanguardCharacter.Activity.Laughing ? KingVanguardLaughTiming.Intensity(laughTime) : 0;
            // All expression weights use the same critically damped response. Their
            // sum stays bounded, so interruption cannot layer two complete faces.
            worry.Step(concerned ? 1 : 0, .16f, dt); sad.Step(failed ? 1 : 0, .16f, dt);
            happy.Step(targetHappy, .16f, dt); excited.Step(targetExcited, .16f, dt); laugh.Step(targetLaugh, .16f, dt);
            float total = worry.value + sad.value + happy.value + excited.value + laugh.value;
            if (total > 1) { worry.value /= total; sad.value /= total; happy.value /= total; excited.value /= total; laugh.value /= total; }

            UpdateLookAround(dt,idle && source==0 && gaze.sqrMagnitude<.01f);
            float autonomy = free && source == 0 && gaze.sqrMagnitude < .01f ? 1 : 0;
            Vector2 idleLook = LookStage>=0 ? LookTarget : new Vector2(curious * -.3f, curious * -.22f + greeting * .08f);
            Vector2 targetEyes = gaze + autonomy * idleLook;
            if (failed) targetEyes = new Vector2(-.03f, -.22f);
            if (!free && !failed) targetEyes *= 1 - Mathf.Max(happy.value, excited.value, laugh.value);
            targetEyes = Vector2.ClampMagnitude(targetEyes, 1);
            eyeX.Step(targetEyes.x, .085f, dt); eyeY.Step(targetEyes.y, .085f, dt);
            float breathe = Mathf.Sin(clock * 1.58f) * 2.2f + Mathf.Sin(clock * .69f) * .45f;
            headX.Step(Eyes.x * 9, .22f, dt);
            headY.Step(nod + Eyes.y * 4 + breathe, .19f, dt);
            roll.Step(tilt + Eyes.x * 2.1f + lean * .85f + Mathf.Sin(clock * .9f) * .18f, .24f, dt);
            bodyY.Step(chest + breathe * .4f, .32f, dt);
            bodyRoll.Step(-roll.value * .23f, .4f, dt);
            jaw.Step(laugh.value * (1 - KingVanguardLaughTiming.PosePulse(laughTime)) * 7, .11f, dt);
            brow.Step(eyebrow, .15f, dt);
            float winkAge = gestureAge - .65f;
            float winkTarget = Mathf.SmoothStep(0, 1, winkAge / .055f) * (1 - Mathf.SmoothStep(0, 1, (winkAge - .19f) / .11f));
            wink.Step(idle && source == 0 && gesture == 2 ? winkTarget : 0, .035f, dt);
            float closed = BlinkCurve(blinkAge) * (1 - laugh.value);
            Lids = new Vector2(Mathf.Max(closed, wink.value), closed) * (1 - sad.value * .7f);
        }
    }
}

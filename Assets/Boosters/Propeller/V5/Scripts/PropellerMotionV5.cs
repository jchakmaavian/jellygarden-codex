using UnityEngine;
namespace RoyalThrone.Boosters.Propeller.V5
{
    public static class PropellerMotionV5
    {
        public static float Duration(float cells)=>Mathf.Clamp(.40f+.06f*cells,.45f,.90f);
        // Quadratic control-point height; the visible midpoint lift is half this amount.
        public static float Arc(float cells)=>Mathf.Clamp(.35f+.06f*cells,.42f,.85f);
        public static float Ease(float t,float duration)
        {
            t=Mathf.Clamp01(t);
            // Match the lift curve's 1.2-cell/sec exit tangent, then accelerate along the arc.
            float entry=1.2f*duration/(3*.30f);
            return (-2*t*t*t+3*t*t)+entry*(t*t*t-2*t*t+t)+.22f*(t*t*t-t*t);
        }
    }
}

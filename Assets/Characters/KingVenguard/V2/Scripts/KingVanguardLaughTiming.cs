using UnityEngine;
namespace RoyalThrone.Characters {
 public static class KingVanguardLaughTiming {
  public const float LeadIn=.22f, AudioDuration=1.75f, Duration=2.42f;
  public static float Pulse(float time)=>PosePulse(time);
  // A held, joyful open-mouth pose with two small chuckles, independent of the removed audio.
  public static float PosePulse(float time){
   float a=(time-.66f)/.24f,b=(time-1.28f)/.27f;
   return Mathf.Clamp01(.35f+.65f*Mathf.Exp(-a*a)+.50f*Mathf.Exp(-b*b));
  }
  public static float Intensity(float time)=>Mathf.SmoothStep(0,1,time/.3f)*(1-Mathf.SmoothStep(0,1,(time-1.88f)/.54f));
  public static float PortraitOpacity(float time)=>time>=.16f&&time<2.22f?1:0;
 }
}

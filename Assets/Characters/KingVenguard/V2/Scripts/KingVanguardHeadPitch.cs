using UnityEngine;
namespace RoyalThrone.Characters {
 // A single textured surface rotates about the neck. Inferred depth gives the
 // crown, cheeks, nose and beard different projected arcs, without image fades.
 public static class KingVanguardHeadPitch {
  public const float MaximumDegrees=32f;
  public static float HeadWeight(float y)=>1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(760,1030,y));
  public static float Depth(Vector2 source){
   float y=source.y;
   float depth=y<=300 ? -255+1.1f*y : y<=500 ? Mathf.Lerp(75,255,(y-300)/200) :
    y<=600 ? Mathf.Lerp(255,230,(y-500)/100) : y<=900 ? Mathf.Lerp(230,35,(y-600)/300) : Mathf.Lerp(35,0,Mathf.InverseLerp(900,1030,y));
   float cheek=1-.30f*Mathf.Clamp01(Mathf.Abs(source.x-627)/370);
   float face=Mathf.SmoothStep(0,1,Mathf.InverseLerp(300,400,y));
   depth*=Mathf.Lerp(1,cheek,face);
   float nose=38*Mathf.Exp(-Mathf.Pow((source.x-627)/110,2)-Mathf.Pow((y-490)/105,2));
   return depth+nose;
  }
  public static Vector2 Project(Vector2 point,Vector2 source,float amount){
   float weight=HeadWeight(source.y),bend=Mathf.Clamp01(amount)*weight;
   if(bend<=0)return point;
   float depth=Depth(point);
   // Unroll the resting tilt slightly as he leans toward the board.
   float roll=8f*bend*Mathf.Deg2Rad;
   Vector2 d=point-new Vector2(627,600);
   point=new Vector2(627+d.x*Mathf.Cos(roll)-d.y*Mathf.Sin(roll),600+d.x*Mathf.Sin(roll)+d.y*Mathf.Cos(roll));
   float pitch=MaximumDegrees*bend*Mathf.Deg2Rad;
   point.y=925+(point.y-925)*Mathf.Cos(pitch)+depth*Mathf.Sin(pitch);
   return point;
  }
 }
}

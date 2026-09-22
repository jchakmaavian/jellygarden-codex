using UnityEngine;
using RoyalThrone.Boosters.ColorBall.V4;
namespace RoyalThrone.Boosters.ColorBall.V5 {
 [DefaultExecutionOrder(1150)]
 public sealed class ColorBallV5Presentation : MonoBehaviour {
  public Renderer finishRim,auraFront;
  public ColorBallPresentationSettings referenceMatch,enhanced;
  public static ColorBallPresentationProfile ActiveProfile=ColorBallPresentationProfile.Enhanced;
  public ColorBallPresentationProfile Profile {get;private set;}
  public ColorBallPresentationSettings Settings=>Profile==ColorBallPresentationProfile.Enhanced?enhanced:referenceMatch;
  ColorBallVisualV4 view;MaterialPropertyBlock block;float clock;
  void Awake(){view=GetComponent<ColorBallVisualV4>();block=new MaterialPropertyBlock();}
  void OnEnable(){Profile=ActiveProfile;ResetPose();}
  void OnDisable(){ResetPose();}
  void ResetPose(){clock=0;if(finishRim)finishRim.enabled=false;if(auraFront)auraFront.enabled=false;if(!view)return;
   view.bodyLayer.localPosition=view.colorSurfaceLayer.localPosition=Vector3.zero;
   view.bodyLayer.localRotation=view.colorSurfaceLayer.localRotation=Quaternion.identity;
   view.vfxRoot.localPosition=view.beamOrigin.localPosition=view.core.transform.localPosition=Vector3.zero;
   view.vfxRoot.localRotation=Quaternion.identity;view.originFlare.transform.localPosition=Vector3.zero;
   view.orbitBack.transform.localPosition=view.orbitFront.transform.localPosition=Vector3.zero;
  }
  static float Ease(float x)=>Mathf.SmoothStep(0,1,x);
  void Light(Renderer r,Color tint,Vector3 scale){if(!r)return;block.Clear();block.SetColor("_Color",tint);block.SetFloat("_Phase",clock);block.SetFloat("_Pulse",view.SourcePulse);block.SetFloat("_Aim",view.beamOrigin.localEulerAngles.z*Mathf.Deg2Rad);block.SetFloat("_Detail",Settings?Settings.detail:0);r.SetPropertyBlock(block);r.transform.localScale=scale;r.enabled=tint.a>.001f;}
  void Surface(Renderer r,float charge,float opacity,Quaternion pose){r.GetPropertyBlock(block);block.SetFloat("_MotionPhase",clock);block.SetFloat("_Opacity",opacity);block.SetFloat("_Charge",charge);block.SetFloat("_Angle",view.Angle+charge*(.18f*Mathf.Sin(clock*9)+.07f*Mathf.Sin(clock*13)));block.SetFloat("_Exposure",Mathf.Lerp(.001f,1.28f,charge));block.SetFloat("_Samples",24);block.SetFloat("_Core",charge*.22f);block.SetFloat("_Energy",charge*.42f);block.SetMatrix("_RigidPose",Matrix4x4.Rotate(Quaternion.Inverse(pose)));r.SetPropertyBlock(block);}
  void LateUpdate(){
   if(!view||view.Current==ColorBallVisualV4.Phase.Free||ColorBallVisualV4.Paused)return;
   if(finishRim)finishRim.enabled=false;if(auraFront)auraFront.enabled=false;
   bool idle=view.Current==ColorBallVisualV4.Phase.Idle,reveal=view.Current==ColorBallVisualV4.Phase.Reveal,finish=view.Current==ColorBallVisualV4.Phase.Finish;
   if(idle)clock=0;else clock+=Time.deltaTime;
   // Brief visual startup; the existing conversion-ready event remains authoritative.
   float charge=idle?0:reveal?1-Ease((view.Age-.30f)/.24f):view.Current==ColorBallVisualV4.Phase.Charge?Ease(view.Age/.15f):1;
   float alive=charge*(finish?1-Ease(view.Age/.20f):1);
   Vector3 offset=new Vector3(.014f*Mathf.Sin(clock*23)+.006f*Mathf.Sin(clock*13),.017f*Mathf.Sin(clock*19)+.005f*Mathf.Sin(clock*31),0)*alive;
   if(reveal)offset+=Vector3.up*(.10f*(1-Ease(view.Age/.43f)));
   Quaternion pose=Quaternion.Euler((7+6*Mathf.Sin(clock*11))*alive,0,(10.3f*Mathf.Sin(clock*17)+3.15f*Mathf.Sin(clock*9))*alive);
   // Rigid sphere: orientation cannot change the metal silhouette or radius.
   view.bodyLayer.localScale=view.colorSurfaceLayer.localScale=new Vector3(.84f,.84f,1);
   view.bodyLayer.localPosition=view.colorSurfaceLayer.localPosition=offset;
   view.bodyLayer.localRotation=view.colorSurfaceLayer.localRotation=Quaternion.identity;
   view.vfxRoot.localPosition=view.beamOrigin.localPosition=view.core.transform.localPosition=offset;
   if(auraFront)auraFront.transform.localPosition=offset;if(finishRim)finishRim.transform.localPosition=offset;
   float opacity=reveal?Ease(view.Age/.07f):finish?1-Ease((view.Age-.185f)/.075f):1;
   Surface(view.body,charge,opacity,pose);Surface(view.energy,charge,opacity,pose);
   // Replace the former continuous neon ellipse with flowing near/far wisps.
   view.orbitBack.enabled=view.orbitFront.enabled=view.orbitGlint.enabled=false;
   if(idle){view.halo.enabled=view.core.enabled=view.originFlare.enabled=false;return;}
   float aura=Settings?Settings.aura:1,discharge=Settings?Settings.discharge:1,source=view.SourcePulse;
   float intensity=charge*(.88f+.12f*Mathf.Sin(clock*14))*(1+source*.30f*discharge);
   Light(view.halo,new Color(1,.90f,.64f,intensity*.88f*aura),new Vector3(2.35f,2.25f,1));
   Light(auraFront,new Color(1,.97f,.86f,intensity*.40f*aura),new Vector3(2.25f,2.05f,1));
   Light(view.core,new Color(1,1,.96f,charge*(.06f+source*.18f)),new Vector3(.46f,.35f,1));
   view.originFlare.transform.localPosition=Vector3.zero;
   Light(view.originFlare,new Color(1,.99f,.91f,source*.88f*discharge),new Vector3(.52f,.32f,1));
   if(!finish)return;
   float t=view.Age,contract=Ease(t/.18f),peak=Ease((t-.16f)/.04f)*(1-Ease((t-.25f)/.18f));
   float rim=Ease((t-.18f)/.035f)*(1-Ease((t-.30f)/.16f)),release=Settings?Settings.release:1;
   // Energy contracts. Metal fades into the release without changing shape or size.
   Light(view.halo,new Color(1,.93f,.69f,(.7f*(1-Ease((t-.17f)/.09f))+peak*.9f)*release),Vector3.one*Mathf.Lerp(1.70f,1.15f,contract));
   Light(auraFront,new Color(1,.98f,.87f,peak*.75f),Vector3.one*1.75f);
   Light(view.core,new Color(1,1,.97f,contract*.34f*opacity+peak*1.3f),Vector3.one*Mathf.Lerp(.6f,1.25f,Ease((t-.17f)/.14f)));
   view.core.GetPropertyBlock(block);block.SetFloat("_Release",peak);view.core.SetPropertyBlock(block);
   Light(view.originFlare,new Color(1,.99f,.89f,peak),Vector3.one*1.1f);
   Light(finishRim,new Color(1,.92f,.61f,rim*release),Vector3.one*Mathf.Lerp(.85f,2.3f,Ease((t-.18f)/.27f)));
  }
 }
}

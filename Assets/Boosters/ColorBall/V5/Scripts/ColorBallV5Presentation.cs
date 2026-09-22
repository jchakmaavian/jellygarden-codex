using UnityEngine;
using RoyalThrone.Boosters.ColorBall.V4;

namespace RoyalThrone.Boosters.ColorBall.V5
{
    // V4 continues to own the state machine, spin integration, pool lease and gameplay events.
    // This variant only draws the reference's light, source pulse and ending on that clock.
    [DefaultExecutionOrder(1150)]
    public sealed class ColorBallV5Presentation : MonoBehaviour
    {
        public Renderer finishRim, auraFront;
        ColorBallVisualV4 view;
        MaterialPropertyBlock block;
        void Awake(){view=GetComponent<ColorBallVisualV4>();block=new MaterialPropertyBlock();}
        float clock;
        void OnEnable(){ResetPose();}
        void OnDisable(){ResetPose();}
        void ResetPose(){
            clock=0;if(finishRim)finishRim.enabled=false;if(auraFront)auraFront.enabled=false;
            if(!view)return;
            view.bodyLayer.localPosition=view.colorSurfaceLayer.localPosition=Vector3.zero;
            view.bodyLayer.localRotation=view.colorSurfaceLayer.localRotation=Quaternion.identity;
            view.vfxRoot.localPosition=view.beamOrigin.localPosition=view.core.transform.localPosition=Vector3.zero;
            view.vfxRoot.localRotation=Quaternion.identity;
            view.orbitBack.transform.localPosition=view.orbitFront.transform.localPosition=Vector3.zero;
        }
        static float Ease(float x)=>Mathf.SmoothStep(0,1,x);
        void Light(Renderer r,Color tint,Vector3 scale){if(!r)return;block.Clear();block.SetColor("_Color",tint);block.SetFloat("_Phase",clock);block.SetFloat("_Pulse",view.SourcePulse);r.SetPropertyBlock(block);r.transform.localScale=scale;r.enabled=tint.a>.001f;}
        void Surface(Renderer r,float opacity=-1){r.GetPropertyBlock(block);block.SetFloat("_MotionPhase",clock);if(opacity>=0)block.SetFloat("_Opacity",opacity);r.SetPropertyBlock(block);}
        void LateUpdate()
        {
            if(!view||view.Current==ColorBallVisualV4.Phase.Free||ColorBallVisualV4.Paused)return;
            if(finishRim)finishRim.enabled=false;if(auraFront)auraFront.enabled=false;
            if(view.Current==ColorBallVisualV4.Phase.Idle||view.Current==ColorBallVisualV4.Phase.Reveal){ResetPose();return;}
            clock+=Time.deltaTime;
            float charge=view.Charge,source=view.SourcePulse;
            float alive=Ease(charge)*(view.Current==ColorBallVisualV4.Phase.Finish?1-Ease(view.Age/.20f):1);
            // Bounded, continuous waves in seconds; no random frame jitter or RPM changes.
            Vector3 offset=new Vector3(.018f*Mathf.Sin(clock*24)+.009f*Mathf.Sin(clock*17),.024f*Mathf.Sin(clock*21)+.008f*Mathf.Sin(clock*13),0)*alive;
            var tilt=Quaternion.Euler(0,0,(3.2f*Mathf.Sin(clock*16)+.8f*Mathf.Sin(clock*25))*alive);
            view.bodyLayer.localPosition=view.colorSurfaceLayer.localPosition=offset;
            view.bodyLayer.localRotation=view.colorSurfaceLayer.localRotation=tilt;
            view.vfxRoot.localPosition=view.beamOrigin.localPosition=view.core.transform.localPosition=offset;
            view.vfxRoot.localRotation=tilt;
            view.orbitBack.transform.localPosition=view.orbitFront.transform.localPosition=offset;
            view.orbitGlint.transform.localPosition+=offset;
            if(auraFront){auraFront.transform.localPosition=offset;auraFront.transform.localRotation=tilt;}
            if(view.Current==ColorBallVisualV4.Phase.Charged){
                view.bodyLayer.localScale=view.colorSurfaceLayer.localScale=new Vector3(.84f*1.035f*ColorBallVisualV4.ChargedWiden,.84f,1);
                Light(view.originFlare,new Color(1,.97f,.84f,source*.90f),new Vector3(.64f,.34f,1));
            }
            var scale=view.bodyLayer.localScale;
            scale.x*=1+.028f*alive*Mathf.Sin(clock*13);scale.y*=1+alive*(.22f+.095f*Mathf.Sin(clock*16)+.025f*Mathf.Sin(clock*25));
            view.bodyLayer.localScale=view.colorSurfaceLayer.localScale=scale;
            Surface(view.body);Surface(view.energy);
            Light(view.halo,new Color(1,.86f,.48f,charge*(.86f+.30f*source)),new Vector3(2.24f,1.92f+.10f*alive*Mathf.Sin(clock*16),1));
            Light(auraFront,new Color(1,.96f,.77f,charge*(.60f+.27f*source)),new Vector3(2.04f,1.68f+.09f*alive*Mathf.Sin(clock*16),1));
            Light(view.core,new Color(1,1,.96f,charge*(.36f+.28f*source)),new Vector3(.62f,.38f,1));
            if(view.Current!=ColorBallVisualV4.Phase.Finish)return;
            float t=view.Age;
            float contract=Ease(t/.18f),fade=1-Ease((t-.20f)/.065f);
            view.bodyLayer.localScale=view.colorSurfaceLayer.localScale=new Vector3(.84f*1.035f*ColorBallVisualV4.ChargedWiden,.89f,1)*Mathf.Lerp(1,.52f,contract);
            Surface(view.body,fade);Surface(view.energy,fade);
            float peak=Ease((t-.155f)/.04f)*(1-Ease((t-.25f)/.18f));
            float rim=Ease((t-.17f)/.045f)*(1-Ease((t-.29f)/.17f));
            float gather=(1-Ease((t-.18f)/.055f))*(.70f+.6f*contract);
            Light(view.halo,new Color(1,.91f,.58f,gather*.7f+peak*1.05f),Vector3.one*Mathf.Lerp(1.5f,2.35f,Ease((t-.18f)/.24f)));
            Light(auraFront,new Color(1,.97f,.80f,gather*.40f+peak*.65f),Vector3.one*Mathf.Lerp(1.2f,1.9f,Ease((t-.18f)/.20f)));
            Light(view.core,new Color(1,1,.97f,contract*.28f*fade+peak*1.6f),Vector3.one*Mathf.Lerp(1.15f,1.60f,Ease((t-.18f)/.16f)));
            view.core.GetPropertyBlock(block);block.SetFloat("_Release",peak);view.core.SetPropertyBlock(block);
            Light(view.originFlare,new Color(1,.98f,.84f,peak),Vector3.one*1.30f);
            Light(finishRim,new Color(1,.91f,.52f,rim*1.15f),Vector3.one*Mathf.Lerp(1.05f,3.20f,Ease((t-.17f)/.28f)));
            view.orbitBack.enabled=view.orbitFront.enabled=view.orbitGlint.enabled=false;
        }
    }
}

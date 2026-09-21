using UnityEngine;
using RoyalThrone.Boosters.ColorBall.V4;

namespace RoyalThrone.Boosters.ColorBall.V5
{
    // V4 continues to own the state machine, spin integration, pool lease and gameplay events.
    // This variant only draws the reference's light, source pulse and ending on that clock.
    [DefaultExecutionOrder(1150)]
    public sealed class ColorBallV5Presentation : MonoBehaviour
    {
        public Renderer finishRim;
        ColorBallVisualV4 view;
        MaterialPropertyBlock block;
        void Awake(){view=GetComponent<ColorBallVisualV4>();block=new MaterialPropertyBlock();}
        void OnEnable(){if(finishRim)finishRim.enabled=false;}
        void OnDisable(){if(finishRim)finishRim.enabled=false;}
        static float Ease(float x)=>Mathf.SmoothStep(0,1,x);
        void Light(Renderer r,Color tint,Vector3 scale){block.Clear();block.SetColor("_Color",tint);r.SetPropertyBlock(block);r.transform.localScale=scale;r.enabled=tint.a>.001f;}
        void Opacity(Renderer r,float opacity){r.GetPropertyBlock(block);block.SetFloat("_Opacity",opacity);r.SetPropertyBlock(block);}
        void LateUpdate()
        {
            if(!view||view.Current==ColorBallVisualV4.Phase.Free)return;
            finishRim.enabled=false;
            if(view.Current==ColorBallVisualV4.Phase.Idle||view.Current==ColorBallVisualV4.Phase.Reveal)return;
            float charge=view.Charge,source=view.SourcePulse;
            Light(view.halo,new Color(.96f,.98f,1,charge*.70f),new Vector3(2.06f,1.30f,1));
            Light(view.core,new Color(1,1,.96f,charge*(.55f+.20f*source)),new Vector3(.72f,.35f,1));
            if(view.Current==ColorBallVisualV4.Phase.Charged){
                // Discharge is a light pulse, not another squash/bounce of the ball.
                view.bodyLayer.localScale=view.colorSurfaceLayer.localScale=new Vector3(.84f*1.035f*ColorBallVisualV4.ChargedWiden,.84f,1);
                Light(view.originFlare,new Color(1,1,.93f,source),new Vector3(.56f,.34f,1));
            }
            if(view.Current!=ColorBallVisualV4.Phase.Finish)return;
            float t=view.Age;
            float contract=Ease(t/.18f),fade=1-Ease((t-.20f)/.065f);
            view.bodyLayer.localScale=view.colorSurfaceLayer.localScale=new Vector3(.84f*1.035f*ColorBallVisualV4.ChargedWiden,.84f,1)*Mathf.Lerp(1,.52f,contract);
            Opacity(view.body,fade);Opacity(view.energy,fade);
            float peak=Ease((t-.155f)/.04f)*(1-Ease((t-.215f)/.16f));
            float rim=Ease((t-.17f)/.045f)*(1-Ease((t-.25f)/.21f));
            Light(view.halo,new Color(1,.98f,.86f,peak*.92f),Vector3.one*Mathf.Lerp(1.45f,2.25f,Ease((t-.18f)/.24f)));
            Light(view.core,new Color(1,1,1,peak),Vector3.one*Mathf.Lerp(.78f,1.4f,Ease((t-.18f)/.16f)));
            Light(view.originFlare,new Color(1,1,.96f,peak*.84f),Vector3.one*1.10f);
            Light(finishRim,new Color(1,.97f,.73f,rim*.64f),Vector3.one*Mathf.Lerp(.9f,2.25f,Ease((t-.17f)/.28f)));
            view.orbitBack.enabled=view.orbitFront.enabled=view.orbitGlint.enabled=false;
        }
    }
}

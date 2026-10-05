using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    // Cosmetic actors are pooled separately from Item: damage remains in the director.
    public sealed class TNT3DAnimator : MonoBehaviour
    {
        public enum VisualState { Reveal, Idle, Activated, Ignition, Explosion, Debris, Complete }
        public VisualState State { get; private set; }
        public Item Owner { get; private set; }
        public Transform model, body, head, fuse, flameSocket;
        public Renderer[] flames, blast, stars;
        public Renderer[] impacts;
        readonly float[] impactBorn=new float[25];int impactCursor;
        public float HeadLift => head.localPosition.y-.22f;
        public float FlameIntensity { get; private set; }
        public static int Reuses { get; private set; }
        public static int ActiveCount { get { int n=0;foreach(var a in pool)if(a&&a.gameObject.activeSelf)n++;return n; } }
        static readonly List<TNT3DAnimator> pool=new List<TNT3DAnimator>();
        static Transform poolRoot;
        MaterialPropertyBlock props;
        float age, strength=1; bool detached, spriteReveal, frameReveal; int uses;
        public float VisualAge => age;
        public const float RevealDuration=.40f, IgnitionDuration=.12f;
        // TNT.mp4 is a 0.25x recording. Keys below are normal-speed samples.
        static readonly AnimationCurve spritePop=new AnimationCurve(
            new Keyframe(0,.55f),new Keyframe(.04f,.86f),new Keyframe(.10f,1.18f),
            new Keyframe(.17f,1.25f),new Keyframe(.22f,1.05f),new Keyframe(.28f,.68f),
            new Keyframe(.34f,.92f),new Keyframe(RevealDuration,1));
        public static TNT3DAnimator RevealSprite(Item item)
        {
            if(!item||!item.sprRenderer)return null;
            // The approved Reveal/Idle frame sequences replace the procedural pop and rim glow once loaded.
            bool frames=TNTFrameAnimator.PlayReveal(item);
            var a=Find(item)??Take(item.transform.position);if(!a)return null;
            a.Owner=item;a.spriteReveal=true;a.frameReveal=frames;a.State=VisualState.Reveal;a.model.gameObject.SetActive(false);
            item.sprRenderer.enabled=!frames;RoyalAudioDirector.Get?.Play(RoyalCue.Create);return a;
        }
        public void PoseSprite(Transform tr)
        {
            if(!spriteReveal||frameReveal||State!=VisualState.Reveal)return;
            float s=spritePop.Evaluate(age),stretch=Mathf.Sin(Mathf.Clamp01(age/.22f)*Mathf.PI)*.09f;
            tr.localScale=Vector3.Scale(tr.localScale,new Vector3(s*(1+stretch),s*(1-stretch),1));
            tr.localRotation=Quaternion.Euler(0,0,Mathf.Sin(age*28)*5*Mathf.Sin(Mathf.Clamp01(age/RevealDuration)*Mathf.PI));
        }
        readonly Color gold=new Color(1,.68f,.075f), warm=new Color(1,.97f,.78f);
        public static float Cell { get {var l=LevelManager.THIS;return l&&l.GameField?Mathf.Min(l.squareWidth*Mathf.Abs(l.GameField.lossyScale.x),l.squareHeight*Mathf.Abs(l.GameField.lossyScale.y)):1;} }
        public static void Prewarm()
        {
            // While the imported TNT is live the board draws its own view and the blast its own particles, so the 240
            // frames (~28 MB on phones) and the 32 actors would sit unused. Both still load on first use without it.
            if(!RoyalThrone.ReferenceGameplay.ReferencePieceVisual.Has(ItemsTypes.PACKAGE))TNTFrameAnimator.Prewarm();
            if(!RoyalThrone.ReferenceGameplay.ReferenceVfxPool.Has("TntItemExplodeParticles"))BuildPool();
        }
        static void BuildPool()
        {
            if(poolRoot)return;pool.Clear();Reuses=0;
            var prefab=Resources.Load<TNT3DAnimator>("RoyalTNT/TNT_3D");if(!prefab)return;
            poolRoot=new GameObject("TNT visual pool").transform;
            for(int i=0;i<32;i++){var a=Instantiate(prefab,poolRoot);a.gameObject.SetActive(false);pool.Add(a);}
        }
        public static TNT3DAnimator Find(Item item){foreach(var a in pool)if(a&&a.gameObject.activeSelf&&a.Owner==item&&!a.detached)return a;return null;}
        static TNT3DAnimator Take(Vector3 position)
        {
            BuildPool();foreach(var a in pool)if(a&&!a.gameObject.activeSelf){a.gameObject.SetActive(true);a.ResetVisualState();a.transform.position=position+Vector3.back*.7f;a.transform.localScale=Vector3.one*Cell;if(a.uses++>0)Reuses++;return a;}return null;
        }
        public static TNT3DAnimator Attach(Item item,bool reveal=false)
        {
            if(!item||!item.square||item.destroying||!item.gameObject.activeInHierarchy)return null;
            var a=Find(item)??Take(item.transform.position);if(!a)return null;
            a.Owner=item;if(reveal)a.Reveal();item.sprRenderer.enabled=false;return a;
        }
        public static TNT3DAnimator ActivateAt(Square square)
        {
            TNT3DAnimator a=null;foreach(var candidate in pool)if(candidate&&candidate.gameObject.activeSelf&&candidate.Owner&&candidate.Owner.square==square&&!candidate.detached){a=candidate;break;}
            if(!a)a=Take(square.transform.position);if(a){a.detached=true;a.Owner=null;a.transform.position=square.transform.position+Vector3.back*.7f;a.Activate();}return a;
        }
        public static TNT3DAnimator PreviewAt(Vector3 position){var a=Take(position);if(a)a.Activate();return a;}
        public static void ClearAll(){foreach(var a in pool)if(a&&a.gameObject.activeSelf)a.Cleanup();}
        public void ResetVisualState()
        {
            props=props??new MaterialPropertyBlock();Owner=null;detached=false;spriteReveal=false;frameReveal=false;strength=1;age=0;
            transform.rotation=Quaternion.identity;model.gameObject.SetActive(true);model.localScale=Vector3.one;
            body.localScale=Vector3.one;body.localPosition=Vector3.zero;head.localPosition=new Vector3(0,.22f,0);head.localRotation=Quaternion.identity;head.localScale=Vector3.one;fuse.localRotation=Quaternion.identity;
            foreach(var r in flames)r.enabled=false;foreach(var r in blast){r.enabled=false;r.transform.localPosition=Vector3.back*global::System.Array.IndexOf(blast,r)*.01f;r.transform.localRotation=Quaternion.identity;}foreach(var r in stars)r.enabled=false;FlameIntensity=0;State=VisualState.Idle;
            impactCursor=0;if(impacts!=null)foreach(var r in impacts)r.enabled=false;
        }
        public void Reveal(){ResetVisualStateKeepingOwner();State=VisualState.Reveal;model.localScale=Vector3.one*.15f;}
        void ResetVisualStateKeepingOwner(){var owner=Owner;ResetVisualState();Owner=owner;}
        public void SetIdle(){ResetVisualStateKeepingOwner();}
        public void Activate(){ResetVisualState();State=VisualState.Activated;detached=true;model.gameObject.SetActive(false);}
        public void Explode(float intensity=1){age=0;strength=intensity;State=VisualState.Explosion;model.gameObject.SetActive(false);foreach(var r in flames)r.enabled=false;FlameIntensity=0;}
        public void Cleanup(){ResetVisualState();State=VisualState.Complete;gameObject.SetActive(false);}
        public void CellImpact(Vector3 position)
        {
            if(impacts==null||impacts.Length==0)return;int i=impactCursor++%impacts.Length;
            impacts[i].transform.position=position+Vector3.back*1.3f;impactBorn[i]=age;impacts[i].enabled=true;
        }
        void LateUpdate()
        {
            if(!detached){if(!Owner||!Owner.gameObject.activeInHierarchy||Owner.currentType!=ItemsTypes.PACKAGE){Cleanup();return;}transform.position=Owner.transform.position+Vector3.back*.7f;Owner.sprRenderer.enabled=spriteReveal&&!frameReveal;}
            var l=LevelManager.THIS;if(l&&l.gameStatus==GameState.Pause)return;
            age+=Time.deltaTime;
            if(impacts!=null)for(int i=0;i<impacts.Length;i++)if(impacts[i].enabled){float t=age-impactBorn[i];Layer(impacts[i],1.22f+t*.65f,warm,t<.60f?1.45f:0,7,t,i*.731f);}
            if(State==VisualState.Reveal && spriteReveal && frameReveal)
            {
                // The frame sequence carries the lift, flame and sparkles; keep only the actor's reveal lifetime.
                if(age>=RevealDuration)Cleanup();
            }
            else if(State==VisualState.Reveal && spriteReveal)
            {
                float pop=spritePop.Evaluate(age);
                float heat=Mathf.SmoothStep(0,1,age/.07f)*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.18f,.27f,age)));
                FlameIntensity=heat;
                Layer(blast[0],.74f*pop,warm,heat*.85f,0);
                Layer(blast[1],1.08f*pop,gold,heat*1.25f,0);
                Layer(blast[3],.90f*pop,new Color(1,.80f,.035f),heat*2.0f,6);
                for(int n=0;n<4;n++){if(n==2)continue;
                    blast[n].transform.localPosition=new Vector3(0,.10f*pop,-.08f-n*.01f);
                    var size=blast[n].transform.localScale;size.y*=n==3?.40f:.31f;blast[n].transform.localScale=size;
                }
                Sparkles(age-.17f,.23f,.74f,false);
                if(age>=RevealDuration)Cleanup();
            }
            else if(State==VisualState.Reveal)
            {
                float t=age;
                float pop=t<.16f?Mathf.Lerp(.15f,1.15f,Mathf.SmoothStep(0,1,t/.16f)):1+.15f*Mathf.Exp(-(t-.16f)*12)*Mathf.Cos((t-.16f)*29);
                model.localScale=Vector3.one*pop;
                float stretch=Mathf.Sin(Mathf.Clamp01(t/.30f)*Mathf.PI)*.17f;
                body.localScale=new Vector3(1-stretch*.6f,1+stretch,1);body.localPosition=Vector3.up*stretch*.13f;
                float lift=Mathf.Sin(Mathf.Clamp01((t-.06f)/.38f)*Mathf.PI)*.16f;
                head.localPosition=new Vector3(0,.22f+lift,0);head.localRotation=Quaternion.Euler(0,0,Mathf.Sin(t*34)*8*Mathf.Sin(Mathf.Clamp01(t/.5f)*Mathf.PI));
                fuse.localRotation=Quaternion.Euler(0,0,Mathf.Sin((t-.06f)*34)*12*Mathf.Sin(Mathf.Clamp01(t/.5f)*Mathf.PI));
                HeadFlame(Mathf.Sin(Mathf.Clamp01((t-.12f)/.28f)*Mathf.PI));Sparkles(t-.15f,.34f,.65f,false);
                if(t>=.5f)SetIdle();
            }
            else if(State==VisualState.Activated||State==VisualState.Ignition)
            {
                float k=Mathf.Clamp01(age/IgnitionDuration);model.localScale=new Vector3(1+.14f*Mathf.Sin(k*Mathf.PI),1-.2f*Mathf.Sin(k*Mathf.PI),1);
                head.localPosition=new Vector3(0,.22f+k*.07f,0);fuse.localRotation=Quaternion.Euler(0,0,Mathf.Sin(age*75)*8);
                HeadFlame(k);Layer(blast[0],.18f+.40f*k,warm,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,1,k))*1.5f);if(age>=.08f)State=VisualState.Ignition;
            }
            else if(State==VisualState.Explosion||State==VisualState.Debris)
            {
                float t=age;if(t>.25f)State=VisualState.Debris;
                // The compact flash opens into textured rays while the broken outer
                // wave is still expanding. Cell sparkles overlap the falling debris.
                float expand=Mathf.SmoothStep(0,1,t/.15f);
                float ballFade=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.075f,.19f,t));
                float rayFade=Mathf.SmoothStep(0,1,t/.09f)*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.20f,.40f,t)));
                float waveFade=Mathf.SmoothStep(0,1,t/.10f)*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.19f,.36f,t)));
                Layer(blast[0],Mathf.Lerp(.30f,4.55f,expand)*strength,new Color(1,.98f,.86f),ballFade*1.45f,5);
                Layer(blast[1],Mathf.Lerp(.55f,5.9f,expand)*strength,new Color(1,.76f,.025f),rayFade*.62f,8);
                Layer(blast[2],Mathf.Lerp(1.1f,6.6f,Mathf.Clamp01(t/.24f))*strength,new Color(1,.82f,.25f),rayFade*1.3f,4);
                Layer(blast[3],Mathf.Lerp(.45f,6.3f,Mathf.Clamp01(t/.23f))*strength,new Color(1,.90f,.58f),waveFade*1.1f,1);
                Layer(blast[4],Mathf.Lerp(2.3f,5.7f,Mathf.Clamp01(t/.35f))*strength,warm,Mathf.Sin(Mathf.Clamp01(t/.66f)*Mathf.PI)*.68f,8);
                Sparkles(t,.72f,3.0f*strength,true);if(t>=.86f)Cleanup();
            }
        }
        void HeadFlame(float k)
        {
            FlameIntensity=k;for(int i=0;i<flames.Length;i++){var r=flames[i];r.enabled=k>.001f;if(!r.enabled)continue;props.Clear();props.SetColor("_Color",new Color(1,i%2==0?.85f:1,i%2==0?.1f:.7f,k*(1.3f+.15f*Mathf.Sin(age*70+i))));props.SetFloat("_FxTime",age+i*.17f);r.SetPropertyBlock(props);}
        }
        void Layer(Renderer r,float size,Color color,float opacity,float mode=-1,float localTime=-1,float seed=0)
        {r.enabled=opacity>.001f;if(!r.enabled)return;r.transform.localScale=Vector3.one*size;props.Clear();color.a=opacity;props.SetColor("_Color",color);props.SetFloat("_FxTime",localTime>=0?localTime:age);props.SetFloat("_Seed",seed);if(mode>=0)props.SetFloat("_Mode",mode);r.SetPropertyBlock(props);}
        void Sparkles(float t,float life,float radius,bool explosion)
        {
            for(int i=0;i<stars.Length;i++)
            {
                float delay=(i%5)*(explosion?.028f:.012f);float u=(t-delay)/life;var r=stars[i];r.enabled=u>0&&u<1;if(!r.enabled)continue;
                float angle=i*2.39996f;Vector3 dir=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);
                r.transform.localPosition=dir*(explosion?radius*(.04f+.11f*(i%5)+u*.32f):radius*(.4f+u*.6f))+Vector3.up*(explosion?-u*u*.8f:.25f)+Vector3.back*.7f;
                r.transform.localRotation=Quaternion.Euler(0,0,i*47+t*(i%2==0?70:-90));
                float size=(i%6==0?.25f:.09f+(i%3)*.035f)*(1-u*.6f);
                if(explosion)size*=1.55f;
                Layer(r,size,i%3==0?gold:warm,Mathf.Sin(u*Mathf.PI)*(explosion?1.1f+.6f*Mathf.Sin(t*37+i*2):1.5f));
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    // Separate, bounded reusable VFX; positions come exclusively from the live board.
    public sealed class ColorBallTargetVfx : MonoBehaviour
    {
        public enum Kind { Beam, TargetGlow, TargetImpact }
        const int Capacity = 24;
        public const float ArrivalTime = 10f / 60f;
        static ColorBallTargetVfx instance;
        static Sprite[][] clips;
        sealed class Slot
        {
            public SpriteRenderer renderer;
            public Kind kind;
            public float age;
            public bool active;
        }
        readonly List<Slot> slots = new List<Slot>(Capacity);
        public static int Active { get { int count=0; if(instance) foreach(var s in instance.slots) if(s.active) count++; return count; } }
        public static bool Ready => clips != null;
        public static int PoolSize => instance ? instance.slots.Count : 0;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { instance=null; }

        public static void Prewarm()
        {
            if (instance) return;
            if (clips == null)
            {
                clips = new[] {
                    Load("Beam/ColorBall_Beam_SequenceBody_",34,new Vector2(.025f,.5f)),
                    Load("Glow/ColorBall_TargetGlow_On_",10,new Vector2(.5f,.5f)),
                    Load("Impact/ColorBall_TargetImpact_",46,new Vector2(.5f,.5f)) };
            }
            instance = new GameObject("GameplayVFX - pooled Color Ball targets (unmasked)").AddComponent<ColorBallTargetVfx>();
            for(int i=0;i<Capacity;i++)
            {
                var go=new GameObject("Color Ball reusable target VFX");
                go.transform.SetParent(instance.transform,false);
                var r=go.AddComponent<SpriteRenderer>();
                r.maskInteraction=SpriteMaskInteraction.None;r.sortingOrder=39;r.enabled=false;
                instance.slots.Add(new Slot { renderer=r });
            }
        }
        static Sprite[] Load(string path,int count,Vector2 pivot)
        {
            var frames=new Sprite[count];
            for(int i=0;i<count;i++)
            {
                var source=Resources.Load<Sprite>("RoyalColorBall/VFX/"+path+(i+1).ToString("000"));
                if(!source) throw new global::System.InvalidOperationException("Missing approved Color Ball VFX: "+path+(i+1));
                frames[i]=pivot==new Vector2(.5f,.5f)?source:Sprite.Create(source.texture,source.rect,pivot,100,0,SpriteMeshType.FullRect);
            }
            return frames;
        }
        public static void Play(Kind kind,Vector3 position,Vector3 target)
        {
            Prewarm();
            Slot slot=null;
            foreach(var s in instance.slots) if(!s.active){slot=s;break;}
            if(slot==null) return; // Bound visual load even during simultaneous combos.
            slot.active=true;slot.kind=kind;slot.age=0;
            var r=slot.renderer;
            var lm=LevelManager.THIS;if(lm&&lm.GameField)r.gameObject.layer=lm.GameField.gameObject.layer;
            r.sortingLayerID=ColorBallFrameAnimator.SeatedSortingLayer;
            r.color=Color.white;r.sprite=clips[(int)kind][0];r.enabled=true;
            r.transform.position=position+Vector3.back*.3f;
            float cell=ColorBallFrameAnimator.Cell;
            r.transform.rotation=Quaternion.identity;
            r.transform.localScale=Vector3.one*(cell/1.92f);
            if(kind==Kind.Beam)
            {
                var delta=target-position;
                r.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
                r.transform.localScale=new Vector3(delta.magnitude/5.44f,cell/1.92f,1);
            }
        }
        void LateUpdate()
        {
            if(LevelManager.THIS&&LevelManager.THIS.gameStatus==GameState.Pause)return;
            foreach(var s in slots)
            {
                if(!s.active)continue;
                s.age+=Time.deltaTime;
                int index=(int)(s.age*60);
                if(s.kind==Kind.Beam && index>=12)index+=14; // Arrival -> short authored fade; no long hold.
                if(s.kind==Kind.TargetGlow && index>=10)
                {
                    index=9;
                    s.renderer.color=new Color(1,1,1,1-Mathf.Clamp01((s.age-10f/60f)/.12f));
                    if(s.age>=10f/60f+.12f) { Release(s);continue; }
                }
                if(index>=clips[(int)s.kind].Length){Release(s);continue;}
                s.renderer.sprite=clips[(int)s.kind][index];
            }
        }
        static void Release(Slot s)
        {
            s.active=false;s.age=0;s.renderer.enabled=false;s.renderer.sprite=null;
            s.renderer.color=Color.white;s.renderer.transform.localScale=Vector3.one;
            s.renderer.transform.rotation=Quaternion.identity;
        }
        public static void ClearAll(){if(instance)foreach(var s in instance.slots)Release(s);}
        void OnDisable(){foreach(var s in slots)Release(s);}
    }
}

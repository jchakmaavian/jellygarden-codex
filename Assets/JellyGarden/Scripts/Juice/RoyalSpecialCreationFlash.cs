using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    // Reconstructed from matching Tranform.mp4, 2.30-2.65 s. The original
    // piece Animator remains untouched; this short overlay travels with its owner.
    public sealed class RoyalSpecialCreationFlash : MonoBehaviour
    {
        public const float Duration = .36f;
        static Material additive;
        Item owner;
        SpriteRenderer core, ring;
        readonly SpriteRenderer[] stars = new SpriteRenderer[8];
        float elapsed;
        public float Elapsed => elapsed;

        public static void Attach(Item item)
        {
            if (!item || item.destroying) return;
            var go = new GameObject("Special creation flash");
            go.transform.SetParent(item.transform, false);
            var effect = go.AddComponent<RoyalSpecialCreationFlash>();
            effect.owner = item;
            if (!additive) {
                var source = Resources.Load<Material>("JuiceAdditive");
                if (!source) { Destroy(go); return; }
                additive = new Material(source) { name = "Special creation additive", hideFlags = HideFlags.HideAndDontSave };
                additive.EnableKeyword("SPRITE_RENDERER_COLOR");
                additive.SetFloat("_UseRendererColor", 1f);
                additive.SetFloat("_Boost", 2f);
            }
            effect.core = effect.Make("Creation core", "add_light_blast02__22096");
            effect.ring = effect.Make("Creation ring", "additive_ring_01__16380");
            for (int i=0;i<effect.stars.Length;i++) effect.stars[i]=effect.Make("Creation sparkle", "add_sparkle__16835");
            effect.Pose();
        }
        SpriteRenderer Make(string name,string sprite)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);
            var r=go.AddComponent<SpriteRenderer>();r.sprite=Resources.Load<Sprite>("ReferenceGameplay/Sprites/"+sprite);
            r.sharedMaterial=additive;r.sortingLayerID=owner.sprRenderer.sortingLayerID;r.sortingOrder=35;
            return r;
        }
        void LateUpdate()
        {
            if(!owner||owner.destroying){Destroy(gameObject);return;}
            if(LevelManager.THIS&&LevelManager.THIS.gameStatus==GameState.Pause)return;
            elapsed+=Time.deltaTime;
            if(elapsed>=Duration){Destroy(gameObject);return;}
            Pose();
        }
        void Pose()
        {
            var scale=owner.transform.lossyScale;
            float cell=LevelManager.THIS.squareWidth*Mathf.Abs(LevelManager.THIS.GameField.lossyScale.x);
            transform.localScale=new Vector3(cell/Mathf.Abs(scale.x),cell/Mathf.Abs(scale.y),1);
            float grow=Mathf.Clamp01(elapsed/.17f);
            float coreAlpha=1-Mathf.Clamp01((elapsed-.18f)/.08f);
            Set(core,Vector2.zero,Mathf.Lerp(.65f,1.28f,grow),coreAlpha,new Color(1f,.94f,.65f));
            float ringT=Mathf.Clamp01((elapsed-.14f)/.20f);
            float ringAlpha=Mathf.Clamp01((elapsed-.14f)/.07f)*(1-Mathf.Clamp01((elapsed-.25f)/.09f));
            Set(ring,Vector2.zero,Mathf.Lerp(.7f,1.23f,ringT),ringAlpha,Color.white);
            for(int i=0;i<stars.Length;i++) {
                float phase=Mathf.Clamp01((elapsed-.09f-i%3*.013f)/.26f);
                float angle=(i*45+13)*Mathf.Deg2Rad;
                Vector2 pos=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*Mathf.Lerp(.19f,.62f,phase);
                float alpha=Mathf.Sin(phase*Mathf.PI);
                Set(stars[i],pos,Mathf.Lerp(.40f,.24f,phase),alpha,i%2==0?Color.white:new Color(1f,.9f,.35f));
            }
        }
        static void Set(SpriteRenderer r,Vector2 position,float width,float alpha,Color color)
        {
            if(!r||!r.sprite)return;
            r.transform.localPosition=position;r.transform.localScale=Vector3.one*(width/r.sprite.bounds.size.x);
            color.a=alpha;r.color=color;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetMaterial(){if(additive)Destroy(additive);additive=null;}
    }
}

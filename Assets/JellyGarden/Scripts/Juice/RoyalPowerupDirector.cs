using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random=UnityEngine.Random;

namespace JellyGarden.Scripts.Juice
{
    // Board mutations belong to these impact sequences. Decorative tails never hold input.
    public sealed class RoyalPowerupDirector : MonoBehaviour
    {
        static RoyalPowerupDirector instance;
        ParticleSystem smoke, sparks, embers, flash, ring;
        ParticleSystem[] systems;
        sealed class Flyer { public GameObject go,rotor; public SpriteRenderer sprite; public TrailRenderer trail; public bool used; }
        Flyer[] flyers;
        readonly HashSet<Square> reserved=new HashSet<Square>();
        int pending;
        readonly global::System.Random visualRandom=new global::System.Random(72813);
        Camera shakeCamera; Vector3 shakeOffset; float shakeEnergy;
        float Range(float min,float max)=>min+(float)visualRandom.NextDouble()*(max-min);
        public int Impacts {get;private set;}
        public int PeakProjectiles {get;private set;}
        public int ActiveOperations=>pending;
        public static bool Busy=>instance&&instance.pending>0;
        public static event Action<float> Reaction;
        public static bool IsPower(ItemsTypes t)=>t!=ItemsTypes.NONE&&t!=ItemsTypes.INGREDIENT;
        public static RoyalPowerupDirector Get
        {
            get
            {
                if(!Application.isPlaying||!RoyalEffectsProfile.Get)return null;
                if(!instance)
                {
                    var prefab=Resources.Load<GameObject>("RoyalGameplay/EffectsRig");
                    if(!prefab)return null;
                    instance=Instantiate(prefab).GetComponent<RoyalPowerupDirector>();
                    instance.name="Royal effects — pooled particles and projectiles";
                }
                return instance;
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetStatics(){instance=null;Reaction=null;}
        void Awake()
        {
            instance=this;
            smoke=transform.Find("Smoke").GetComponent<ParticleSystem>(); sparks=transform.Find("Sparkles").GetComponent<ParticleSystem>();
            embers=transform.Find("Embers").GetComponent<ParticleSystem>();flash=transform.Find("Impact").GetComponent<ParticleSystem>();ring=transform.Find("Shockwave").GetComponent<ParticleSystem>();
            systems=new[]{smoke,sparks,embers,flash,ring};
            flyers=new Flyer[RoyalEffectsProfile.Get.projectilePool];
            for(int i=0;i<flyers.Length;i++)
            {
                var go=new GameObject("Projectile "+i);go.transform.SetParent(transform);
                var sr=go.AddComponent<SpriteRenderer>();sr.sortingOrder=36;
                var trail=go.AddComponent<TrailRenderer>();trail.time=.16f;trail.minVertexDistance=.06f;trail.numCapVertices=3;trail.numCornerVertices=2;
                trail.sharedMaterial=Resources.Load<Material>("RoyalGameplay/Trail");trail.sortingOrder=32;trail.widthMultiplier=.35f;
                trail.widthCurve=AnimationCurve.EaseInOut(0,1,1,0);trail.startColor=new Color(1,.95f,.6f,1);trail.endColor=new Color(1,.45f,.08f,0);
                var rotor=new GameObject("Spinning rotor");rotor.transform.SetParent(go.transform,false);rotor.transform.localPosition=new Vector3(0,.23f,-.04f);
                for(int blade=0;blade<2;blade++)
                {var arm=new GameObject("Rotor blade").AddComponent<LineRenderer>();arm.transform.SetParent(rotor.transform,false);arm.useWorldSpace=false;arm.positionCount=2;arm.SetPosition(0,blade==0?Vector3.left*.55f:Vector3.down*.55f);arm.SetPosition(1,blade==0?Vector3.right*.55f:Vector3.up*.55f);arm.widthMultiplier=.045f;arm.numCapVertices=3;arm.sharedMaterial=trail.sharedMaterial;arm.sortingOrder=38;arm.startColor=arm.endColor=new Color(1,.92f,.55f,.65f);}
                flyers[i]=new Flyer{go=go,rotor=rotor,sprite=sr,trail=trail};go.SetActive(false);
            }
        }
        float Cell => LevelManager.THIS ? LevelManager.THIS.squareWidth*LevelManager.THIS.GameField.lossyScale.x:1.2f;
        bool Paused=>LevelManager.THIS&&LevelManager.THIS.gameStatus==GameState.Pause;
        IEnumerator Wait(float seconds){float t=0;while(t<seconds){if(!Paused)t+=Time.deltaTime;yield return null;}}
        public void Queue(Item item)
        {
            var sq=item.square;var type=item.currentType;int colour=item.color;
            StartOperation(Single(type,sq,colour));
        }
        void StartOperation(IEnumerator sequence){pending++;StartCoroutine(Own(sequence));}
        IEnumerator Own(IEnumerator sequence){try{yield return sequence;}finally{pending=Mathf.Max(0,pending-1);}}
        void OnDisable(){StopAllCoroutines();pending=0;reserved.Clear();if(shakeCamera)shakeCamera.transform.position-=shakeOffset;shakeOffset=Vector3.zero;shakeEnergy=0; if(flyers!=null)foreach(var f in flyers){f.used=false;f.go.SetActive(false);} }
        public void Cancel(){OnDisable();foreach(var ps in GetComponentsInChildren<ParticleSystem>())ps.Clear();}
        void Update()
        {
            if(pending>0&&LevelManager.THIS&&(LevelManager.THIS.gameStatus==GameState.Map||LevelManager.THIS.gameStatus==GameState.PrepareGame))Cancel();
            foreach(var ps in systems) { if(Paused&&!ps.isPaused)ps.Pause(); else if(!Paused&&ps.isPaused)ps.Play(); }
            if(!Paused)foreach(var f in flyers)if(f.used&&f.rotor.activeSelf)f.rotor.transform.Rotate(0,0,Time.deltaTime*2400);
        }
        void LateUpdate()
        {
            if(!shakeCamera)shakeCamera=Camera.main;if(!shakeCamera)return;
            shakeCamera.transform.position-=shakeOffset;shakeOffset=Vector3.zero;
            shakeEnergy=Mathf.MoveTowards(shakeEnergy,0,Time.deltaTime*9);
            if(!Paused&&PlayerPrefs.GetInt("RoyalThrone.HomeMotion",1)!=0&&shakeEnergy>0)
                shakeOffset=new Vector3(Mathf.Sin(Time.time*83),Mathf.Cos(Time.time*97),0)*RoyalEffectsProfile.Get.impactShake*Cell*shakeEnergy;
            shakeCamera.transform.position+=shakeOffset;
        }
        public static bool TrySwap(Item a,Item b)
        {
            if(!Get||!a||!b||a.currentType==ItemsTypes.INGREDIENT||b.currentType==ItemsTypes.INGREDIENT)return false;
            if(!IsPower(a.currentType)&&!IsPower(b.currentType))return false;
            if(IsPower(a.currentType)&&IsPower(b.currentType))
            {var ta=a.currentType;var tb=b.currentType;var origin=a.square; Consume(a);Consume(b);Get.StartOperation(Get.Pair(ta,tb,origin));}
            else
            {var power=IsPower(a.currentType)?a:b;var normal=power==a?b:a;var type=power.currentType;var origin=power.square;int col=normal.color;Consume(power);Get.StartOperation(Get.Single(type,origin,col));}
            return true;
        }
        static void Consume(Item item){item.royalConsumed=true;item.NextType=ItemsTypes.NONE;item.DestroyItem(true);}
        public static bool Tap(Item item)
        {
            var lm=LevelManager.THIS;
            if(!Get||!item||!IsPower(item.currentType)||item.destroying||item.falling||!lm||lm.DragBlocked||!item.square.CanGoOut()||lm.gameStatus!=GameState.Playing||lm.Limit<=0)return false;
            lm.DragBlocked=true;if(lm.limitType==LIMIT.MOVES)lm.Limit--;lm.moveID++;item.DestroyItem(true);lm.FindMatches();return true;
        }
        IEnumerator Single(ItemsTypes type,Square origin,int colour)
        {
            if(!origin)yield break;
            if(type==ItemsTypes.HORIZONTAL_STRIPPED||type==ItemsTypes.VERTICAL_STRIPPED)yield return Rocket(origin,type==ItemsTypes.HORIZONTAL_STRIPPED,1);
            else if(type==ItemsTypes.PACKAGE)yield return Explosion(origin,2);
            else if(type==ItemsTypes.HELICOPTER)yield return Helicopter(origin,ItemsTypes.NONE,1);
            else if(type==ItemsTypes.BOMB)yield return Orb(origin,ItemsTypes.NONE,colour,false);
        }
        IEnumerator Pair(ItemsTypes a,ItemsTypes b,Square origin)
        {
            if(a==ItemsTypes.BOMB||b==ItemsTypes.BOMB){yield return Orb(origin,a==ItemsTypes.BOMB?b:a,-1,a==ItemsTypes.BOMB&&b==ItemsTypes.BOMB);yield break;}
            if(a==ItemsTypes.HELICOPTER||b==ItemsTypes.HELICOPTER){yield return Helicopter(origin,a==ItemsTypes.HELICOPTER?b:a,a==b?3:1);yield break;}
            if(a==ItemsTypes.PACKAGE&&b==ItemsTypes.PACKAGE){yield return Explosion(origin,4);yield break;}
            int lanes=a==ItemsTypes.PACKAGE||b==ItemsTypes.PACKAGE?3:1;
            StartOperation(Rocket(origin,true,lanes));yield return Rocket(origin,false,lanes);
        }
        Flyer Take(string art,Vector3 p,float scale=1)
        {
            foreach(var f in flyers)if(!f.used){f.used=true;f.go.transform.position=p+Vector3.back*.6f;f.go.transform.rotation=Quaternion.identity;f.go.transform.localScale=Vector3.one*Cell*scale;f.sprite.sprite=RoyalPieceSkin.Art(art);f.sprite.color=Color.white;f.sprite.enabled=true;f.rotor.SetActive(art=="helicopter");f.go.SetActive(true);f.trail.Clear();f.trail.emitting=true;int count=0;foreach(var t in flyers)if(t.used)count++;PeakProjectiles=Mathf.Max(PeakProjectiles,count);return f;}
            return null;
        }
        IEnumerator Retire(Flyer f){if(f==null)yield break;f.sprite.enabled=false;f.rotor.SetActive(false);f.trail.emitting=false;yield return Wait(.17f);f.trail.Clear();f.go.SetActive(false);f.used=false;f.sprite.enabled=true;}
        void Emit(ParticleSystem ps,Vector3 p,int n,float size,float speed,float life,Color tint)
        {
            if(!ps)return;
            var e=new ParticleSystem.EmitParams{position=p+Vector3.back*.5f,startLifetime=life,startSize=size*Cell,startColor=tint,applyShapeToPosition=false};
            bool motion=PlayerPrefs.GetInt("RoyalThrone.HomeMotion",1)!=0;
            n=motion?n:Mathf.Max(1,n/3);
            for(int i=0;i<n;i++){float angle=i*Mathf.PI*2/n+Range(-.25f,.25f);e.velocity=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*speed*Cell*Range(.65f,1.2f);e.rotation=Range(0,360);ps.Emit(e,1);}
        }
        public void Burst(Vector3 p,float strength=1)
        {
            Emit(flash,p,1,2.8f*strength,0,.10f,new Color(1,.94f,.7f,1));
            Emit(ring,p,1,1.5f*strength,0,.27f,new Color(1,.82f,.3f,.85f));
            Emit(smoke,p,7,1.05f*strength,3*strength,RoyalEffectsProfile.Get.smokeLifetime,new Color(1,.94f,.8f,.8f));
            Emit(sparks,p,22,.43f,8*strength,.43f,new Color(1,.91f,.45f,1));
            Emit(embers,p,16,.16f,10*strength,.37f,new Color(1,.56f,.08f,1));
            Reaction?.Invoke(strength);shakeEnergy=Mathf.Min(1.5f,Mathf.Max(shakeEnergy,strength)); Impacts++;
        }
        public void Creation(Vector3 p)
        {Emit(ring,p,1,.6f,0,.2f,new Color(.75f,.9f,1,1));Emit(sparks,p,10,.33f,3,.34f,new Color(1,.9f,.3f,1));RoyalAudioDirector.Get?.Play(RoyalCue.Create);Reaction?.Invoke(.6f);}
        public void Match(Vector3 p,float intensity)
        {Reaction?.Invoke(Mathf.Min(.45f,intensity*.22f));}
        public void Chip(Vector3 p)
        {Emit(smoke,p,3,.43f,1.8f,.22f,new Color(1,.86f,.65f,.75f));Emit(embers,p,6,.13f,3,.3f,new Color(1,.73f,.2f,1));}
        void Hit(Square sq)
        {
            if(!sq||sq.IsNone()||sq.type==SquareTypes.UNDESTROYABLE)return;
            if(sq.item&&sq.item.currentType!=ItemsTypes.INGREDIENT) {sq.item.juiceStagger=0;sq.item.DestroyItem(true);}
            else if(sq.block.Count>0) sq.DestroyBlock();
        }
        IEnumerator Rocket(Square origin,bool horizontal,int lanes)
        {
            Vector3 p=origin.transform.position;
            Creation(p); yield return Wait(RoyalEffectsProfile.Get.rocketAnticipation);
            RoyalAudioDirector.Get?.Play(RoyalCue.Rocket);
            Emit(flash,p,1,1.5f,0,.09f,Color.white);Emit(smoke,p,4,.7f,2,.24f,Color.white);
            var lm=LevelManager.THIS;var hit=new HashSet<Square>();var active=new List<Flyer>();
            for(int lane=-lanes/2;lane<=lanes/2;lane++)for(int d=-1;d<=1;d+=2)
            {Vector3 offset=(horizontal?Vector3.up:Vector3.right)*lane*Cell;var f=Take("rocket",p+offset,.8f);if(f!=null){f.go.transform.rotation=Quaternion.Euler(0,0,horizontal?0:90);active.Add(f);}else active.Add(null);}
            int reach=horizontal?lm.maxCols:lm.maxRows;float t=0;float duration=(reach+1)/RoyalEffectsProfile.Get.rocketCellsPerSecond;
            while(t<duration)
            {
                if(Paused){yield return null;continue;}
                t+=Time.deltaTime;float distance=t*RoyalEffectsProfile.Get.rocketCellsPerSecond;int index=0;
                for(int lane=-lanes/2;lane<=lanes/2;lane++)for(int d=-1;d<=1;d+=2)
                {
                    Vector3 offset=(horizontal?Vector3.up:Vector3.right)*lane*Cell;Vector3 direction=(horizontal?Vector3.right:Vector3.up)*d;
                    var f=active[index++];if(f!=null)f.go.transform.position=p+offset+direction*distance*Cell+Vector3.back*.6f;
                    for(int step=0;step<=Mathf.Min(reach,Mathf.FloorToInt(distance));step++)
                    {int c=origin.col+(horizontal?step*d:lane),r=origin.row+(horizontal?-lane:-step*d);var sq=lm.GetSquare(c,r);if(sq&&hit.Add(sq)){Hit(sq);Emit(sparks,sq.transform.position,2,.26f,2,.22f,Color.white);}}
                }
                yield return null;
            }
            foreach(var f in active)StartCoroutine(Retire(f));Reaction?.Invoke(.8f);
        }
        IEnumerator Explosion(Square origin,int radius)
        {
            Vector3 p=origin.transform.position;var fuse=Take("tnt",p,.9f);
            float elapsed=0,duration=RoyalEffectsProfile.Get.tntAnticipation;
            while(elapsed<duration){if(!Paused)elapsed+=Time.deltaTime;if(fuse!=null)fuse.go.transform.localScale=Vector3.one*Cell*(.9f+.18f*Mathf.Sin(elapsed/duration*Mathf.PI));yield return null;}
            if(fuse!=null)StartCoroutine(Retire(fuse));
            Burst(p,radius>2?1.45f:1);RoyalAudioDirector.Get?.Play(RoyalCue.Tnt);
            var lm=LevelManager.THIS;
            for(int distance=0;distance<=radius;distance++)
            {
                for(int y=-distance;y<=distance;y++)for(int x=-distance;x<=distance;x++)if(Mathf.Max(Mathf.Abs(x),Mathf.Abs(y))==distance)Hit(lm.GetSquare(origin.col+x,origin.row+y));
                yield return Wait(RoyalEffectsProfile.Get.blastWavePerCell);
            }
        }
        Square Target(Square origin)
        {
            var lm=LevelManager.THIS;Square best=null;float score=-100000;
            for(int r=0;r<lm.maxRows;r++)for(int c=0;c<lm.maxCols;c++)
            {var sq=lm.GetSquare(c,r);if(!sq||sq==origin||sq.IsNone()||sq.type==SquareTypes.UNDESTROYABLE||reserved.Contains(sq)||(!sq.item&&sq.block.Count==0)||sq.item&&sq.item.currentType==ItemsTypes.INGREDIENT)continue;
                float value=(sq.block.Count>0?100:0)+Vector3.Distance(origin.transform.position,sq.transform.position)+Random.value*3;
                if(value>score){score=value;best=sq;}}
            if(best)reserved.Add(best);return best;
        }
        IEnumerator Helicopter(Square origin,ItemsTypes payload,int count)
        {
            foreach(var n in origin.GetAllNeghbors())Hit(n);
            for(int i=0;i<count;i++){var target=Target(origin);if(target)StartOperation(Flight(origin,target,payload));yield return Wait(.08f);}
        }
        IEnumerator Flight(Square origin,Square target,ItemsTypes payload)
        {
            var f=Take("helicopter",origin.transform.position,.85f);RoyalAudioDirector.Get?.Play(RoyalCue.Helicopter);
            Vector3 start=origin.transform.position+Vector3.back*.8f,end=target.transform.position+Vector3.back*.8f;
            float t=0;float lift=RoyalEffectsProfile.Get.helicopterLift;
            while(t<lift){if(!Paused)t+=Time.deltaTime;if(f!=null){f.go.transform.position=start+Vector3.up*Cell*.5f*Mathf.Clamp01(t/lift);f.go.transform.rotation=Quaternion.Euler(0,0,Mathf.Sin(t*80)*12);}yield return null;}
            Vector3 control=(start+end)*.5f+Vector3.up*Cell*2;float duration=RoyalEffectsProfile.Get.helicopterFlight;t=0;
            while(t<duration){if(!Paused)t+=Time.deltaTime;float k=Mathf.SmoothStep(0,1,t/duration);if(f!=null){f.go.transform.position=(1-k)*(1-k)*start+2*(1-k)*k*control+k*k*end;f.go.transform.rotation=Quaternion.Euler(0,0,Mathf.Sin(k*Mathf.PI)*-25);}yield return null;}
            if(f!=null)StartCoroutine(Retire(f));reserved.Remove(target);
            Burst(end,.48f);
            if(payload==ItemsTypes.PACKAGE)yield return Explosion(target,2);
            else if(payload==ItemsTypes.HORIZONTAL_STRIPPED||payload==ItemsTypes.VERTICAL_STRIPPED)yield return Rocket(target,payload==ItemsTypes.HORIZONTAL_STRIPPED,1);
            else Hit(target);
        }
        IEnumerator Orb(Square origin,ItemsTypes payload,int colour,bool all)
        {
            var lm=LevelManager.THIS;
            if(colour<0||colour>5){int[] counts=new int[6];foreach(var i in lm.GetItems())if(i&&i.color>=0&&i.color<6)counts[i.color]++;colour=0;for(int k=1;k<6;k++)if(counts[k]>counts[colour])colour=k;}
            var targets=new List<Square>();for(int r=0;r<lm.maxRows;r++)for(int c=0;c<lm.maxCols;c++){var sq=lm.GetSquare(c,r);if(sq&&(all||sq.item&&sq.item.color==colour))targets.Add(sq);}
            Creation(origin.transform.position);RoyalAudioDirector.Get?.Play(RoyalCue.Create);yield return Wait(.16f);
            foreach(var sq in targets)
            {
                Emit(sparks,sq.transform.position,5,.35f,2,.3f,new Color(.8f,.9f,1,1));
                if(IsPower(payload)&&payload!=ItemsTypes.BOMB&&sq.item&&sq.item.currentType==ItemsTypes.NONE)
                {sq.item.currentType=payload;sq.item.debugType=payload;sq.item.NextType=ItemsTypes.NONE;}
            }
            yield return Wait(.18f);
            foreach(var sq in targets){Hit(sq);yield return Wait(all?.005f:.014f);}Reaction?.Invoke(1.4f);
        }
        // Workshop calls the same visual paths, without touching the player's board or save.
        public void Preview(string effect,Vector3 p)
        {
            if(effect=="TNT") {Burst(p);RoyalAudioDirector.Get?.Play(RoyalCue.Tnt);}
            else if(effect=="Match 3") {for(int i=-1;i<=1;i++)MatchVFXService.Instance.PlayMatchBurst(p+Vector3.right*i*Cell,new Color(.2f,.7f,1),1,i*.018f);RoyalAudioDirector.Get?.Play(RoyalCue.Match);}
            else if(effect=="Match 4")Creation(p);
            else StartCoroutine(PreviewFlight(effect,p));
        }
        IEnumerator PreviewFlight(string effect,Vector3 p)
        {
            bool heli=effect=="Helicopter";var f=Take(heli?"helicopter":"rocket",p,.9f);RoyalAudioDirector.Get?.Play(heli?RoyalCue.Helicopter:RoyalCue.Rocket);
            float t=0;while(t<.55f){t+=Time.deltaTime;float k=Mathf.Clamp01(t/.55f);if(f!=null){f.go.transform.position=p+new Vector3((k-.5f)*Cell*7,heli?Mathf.Sin(k*Mathf.PI)*Cell*2:0,-.6f);f.go.transform.rotation=Quaternion.Euler(0,0,heli?-15:0);}yield return null;}Burst(p+Vector3.right*Cell*3.5f,.5f);yield return Retire(f);
        }
    }
}

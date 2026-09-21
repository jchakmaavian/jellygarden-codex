using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random=UnityEngine.Random;

namespace JellyGarden.Scripts.Juice
{
    // Board mutations belong to these impact sequences. Decorative tails never hold input.
    public sealed partial class RoyalPowerupDirector : MonoBehaviour
    {
        static RoyalPowerupDirector instance;
        ParticleSystem smoke, sparks, embers, flash, ring, rays;
        ParticleSystem[] systems;
        sealed class Flyer { public GameObject go,rotor; public SpriteRenderer sprite; public TrailRenderer trail, core; public LineRenderer flame; public MeshRenderer exhaust; public Gradient preRocketGradient; public readonly MaterialPropertyBlock flameProperties=new MaterialPropertyBlock(); public bool used, rocket; }
        Flyer[] flyers;
        readonly HashSet<Square> reserved=new HashSet<Square>();
        int pending;
        GameState previousState;
        bool observedState;
        float lastCoreFlash = -10f, lastFireworkSound = -10f;
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
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetStatics(){instance=null;Reaction=null;ResetPropellerEvents();}
        void Awake()
        {
            instance=this;
            TNT3DAnimator.Prewarm();
            smoke=transform.Find("Smoke").GetComponent<ParticleSystem>(); sparks=transform.Find("Sparkles").GetComponent<ParticleSystem>();
            embers=transform.Find("Embers").GetComponent<ParticleSystem>();flash=transform.Find("Impact").GetComponent<ParticleSystem>();ring=transform.Find("Shockwave").GetComponent<ParticleSystem>();
            var rayObject=transform.Find("Aura rays");rays=rayObject?rayObject.GetComponent<ParticleSystem>():null;
            systems=rays?new[]{smoke,sparks,embers,flash,ring,rays}:new[]{smoke,sparks,embers,flash,ring};
            BuildRocketVfx();
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
                var coreObject=new GameObject("White-hot rocket core");coreObject.transform.SetParent(go.transform,false);
                var core=coreObject.AddComponent<TrailRenderer>();core.sharedMaterial=rocketCoreMaterial;core.time=.3f;core.minVertexDistance=.025f;core.numCapVertices=4;core.sortingOrder=35;core.widthCurve=AnimationCurve.EaseInOut(0,1,1,0);core.startColor=Color.white;core.endColor=new Color(1,1,.85f,0);core.emitting=false;
                var fireObject=new GameObject("Continuous flame ribbon");fireObject.transform.SetParent(go.transform,false);
                var flame=fireObject.AddComponent<LineRenderer>();flame.sharedMaterial=rocketRibbonMaterial;flame.sortingOrder=33;
                flame.useWorldSpace=true;flame.numCapVertices=0;flame.numCornerVertices=3;flame.textureMode=LineTextureMode.Stretch;
                flame.widthCurve=AnimationCurve.Linear(0,1,1,1);flame.enabled=false;flame.positionCount=0;
                flyers[i]=new Flyer{go=go,rotor=rotor,sprite=sr,trail=trail,core=core,flame=flame,exhaust=CreateRocketExhaust(go.transform,i)};go.SetActive(false);
            }
        }
        float Cell => LevelManager.THIS ? LevelManager.THIS.squareWidth*LevelManager.THIS.GameField.lossyScale.x:1.2f;
        bool Paused=>LevelManager.THIS&&LevelManager.THIS.gameStatus==GameState.Pause;
        IEnumerator Wait(float seconds){float t=0;while(t<seconds){if(!Paused)t+=Time.deltaTime;yield return null;}}
        public void Queue(Item item)
        {
            ColorBallFrameAnimator.HideForActivation(item);
            var sq=item.square;var type=item.currentType;int colour=item.color;
            StartOperation(Single(type,sq,colour));
        }
        void StartOperation(IEnumerator sequence){pending++;StartCoroutine(Own(sequence));}
        IEnumerator Own(IEnumerator sequence){try{yield return sequence;}finally{pending=Mathf.Max(0,pending-1);}}
        void OnDisable(){RoyalPropellerV2.ClearAll();RoyalPropellerPieceV2.ClearSeated();ColorBallBoostView.ClearAll();ColorBallTargetVfx.ClearAll();ColorBallState=ColorBallPhase.Complete;TNT3DAnimator.ClearAll();StopAllCoroutines();pending=0;reserved.Clear();if(shakeCamera)shakeCamera.transform.position-=shakeOffset;shakeOffset=Vector3.zero;shakeEnergy=0; if(flyers!=null)foreach(var f in flyers){if(f.rocket&&f.preRocketGradient!=null)f.trail.colorGradient=f.preRocketGradient;f.used=false;f.flame.enabled=false;f.flame.positionCount=0;f.trail.Clear();f.core.Clear();f.go.SetActive(false);} }
        public void Cancel(){RoyalThrone.Boosters.Propeller.V5.PropellerVisualV5.Clear();RoyalThrone.Boosters.Propeller.V4.PropellerVisualV4.Clear();RoyalThrone.Boosters.Propeller.V3.PropellerVisualV3.Clear();RoyalThrone.Boosters.Propeller.V2.PropellerVisualV2.Clear();OnDisable();if(systems!=null)foreach(var ps in systems)if(ps)ps.Clear();RoyalToolkitImpact.Existing?.Clear();ClearRocketVfx();}
        void Update()
        {
            var level=LevelManager.THIS;
            if(level)
            {
                var state=level.gameStatus;
                // Decorative coronas and trails can outlive the board operation.
                // Clear them when leaving the board even when pending is already zero.
                // Staying on Map permits deliberate Effects Workshop previews.
                bool transition=observedState&&state!=previousState;
                if((pending>0||transition)&&(state==GameState.Map||state==GameState.PrepareGame))Cancel();
                previousState=state;observedState=true;
            }
            foreach(var ps in systems) { if(Paused&&!ps.isPaused)ps.Pause(); else if(!Paused&&ps.isPaused)ps.Play(); }
            UpdateRocketVfx();
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
        static void Consume(Item item){ColorBallFrameAnimator.HideForActivation(item);item.royalConsumed=true;item.NextType=ItemsTypes.NONE;item.DestroyItem(true);}
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
            foreach(var f in flyers)if(!f.used){f.used=true;f.rocket=false;f.go.transform.SetParent(transform,true);f.exhaust.enabled=false;f.flame.positionCount=0;f.flame.enabled=false;f.trail.widthCurve=AnimationCurve.EaseInOut(0,1,1,0);f.trail.sharedMaterial=Resources.Load<Material>("RoyalGameplay/Trail");f.core.Clear();f.core.emitting=false;f.trail.time=.16f;f.trail.widthMultiplier=.35f;f.trail.startColor=new Color(1,.95f,.6f,1);f.trail.endColor=new Color(1,.45f,.08f,0);f.go.transform.position=p+Vector3.back*.6f;f.go.transform.rotation=Quaternion.identity;f.go.transform.localScale=Vector3.one*Cell*scale;f.sprite.sprite=RoyalPieceSkin.Art(art);f.sprite.color=Color.white;f.sprite.enabled=true;f.rotor.SetActive(art=="helicopter");f.go.SetActive(true);f.trail.Clear();f.trail.emitting=true;int count=0;foreach(var t in flyers)if(t.used)count++;PeakProjectiles=Mathf.Max(PeakProjectiles,count);return f;}
            return null;
        }
        IEnumerator Retire(Flyer f)
        {
            if(f==null)yield break;
            f.sprite.enabled=false;f.rotor.SetActive(false);f.exhaust.enabled=false;
            f.trail.emitting=false;f.core.emitting=false;
            if(f.rocket)
            {
                // Let the bounded plume and its speed-derived trails finish before pool reuse.
                float elapsed=0, lifetime=Mathf.Max(f.trail.time,f.core.time)+.08f;
                while(elapsed<lifetime)
                {
                    if(!Paused)elapsed+=Time.deltaTime;
                    float opacity=.88f*(1-Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/Mathf.Max(.1f,lifetime-.08f))));
                    f.flame.startColor=f.flame.endColor=new Color(1,1,1,opacity);
                    yield return null;
                }
            }
            else yield return Wait(.17f);
            f.trail.Clear();f.core.Clear();f.flame.enabled=false;f.flame.positionCount=0;
            if(f.rocket&&f.preRocketGradient!=null)f.trail.colorGradient=f.preRocketGradient;
            f.go.SetActive(false);f.used=false;f.sprite.enabled=true;
        }
        void Emit(ParticleSystem ps,Vector3 p,int n,float size,float speed,float life,Color tint)
        {
            if(!ps)return;
            var e=new ParticleSystem.EmitParams{position=p+Vector3.back*.5f,startLifetime=life,startSize=size*Cell,startColor=tint,applyShapeToPosition=false};
            bool motion=PlayerPrefs.GetInt("RoyalThrone.HomeMotion",1)!=0;
            n=motion?n:Mathf.Max(1,n/3);
            for(int i=0;i<n;i++){float angle=i*Mathf.PI*2/n+Range(-.25f,.25f);e.velocity=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*speed*Cell*Range(.65f,1.2f);e.rotation=Range(0,360);ps.Emit(e,1);}
            if((ps==rocketImpact||ps==rocketGlow||ps==rocketStars||ps==rocketEmbers)&&!ps.isPlaying)ps.Play(false);
        }
        void PuffRing(Vector3 p,float strength)
        {
            int count=PlayerPrefs.GetInt("RoyalThrone.HomeMotion",1)==0?4:8;
            for(int i=0;i<count;i++){
                float a=(i+.15f)*Mathf.PI*2/count;var dir=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
                var e=new ParticleSystem.EmitParams{position=p+dir*Cell*.40f*strength+Vector3.back*.5f,velocity=dir*Cell*4.5f*strength,
                    startLifetime=RoyalEffectsProfile.Get.smokeLifetime*Range(.85f,1.1f),startSize=Cell*.87f*strength*Range(.85f,1.1f),startColor=Color.white,rotation=-a*Mathf.Rad2Deg,applyShapeToPosition=false};
                smoke.Emit(e,1);
            }
        }
        public void Burst(Vector3 p,float strength=1)
        {
            strength=Mathf.Clamp(strength,.35f,1.45f);
            if (strength >= .9f && RoyalToolkitImpact.TryPlayFire(p, Cell, strength))
            {
                Reaction?.Invoke(strength); shakeEnergy=Mathf.Max(shakeEnergy,strength);
                return;
            }
            bool motion=PlayerPrefs.GetInt("RoyalThrone.HomeMotion",1)!=0;
            // A warm core, expanding corona and trailing sparks give the blast a
            // readable sequence. Closely chained TNTs share the bright core beat.
            if(Time.time-lastCoreFlash>.11f){Emit(flash,p,1,(motion?4.3f:2.5f)*strength,0,.10f,new Color(1,.96f,.74f,motion?.92f:.38f));lastCoreFlash=Time.time;}
            Emit(ring,p,1,2.1f*strength,0,.34f,new Color(1,.88f,.43f,.9f));
            PuffRing(p,strength);
            Emit(rays,p,22,.19f,10*strength,.31f,new Color(1,.94f,.69f,1));
            Emit(sparks,p,24,.27f,6.2f*strength,.58f,new Color(1,.96f,.73f,1));
            Emit(embers,p,20,.12f,7*strength,.61f,new Color(1,.70f,.2f,1));
            StartCoroutine(Corona(p,strength));
            Reaction?.Invoke(strength);shakeEnergy=Mathf.Min(1.5f,Mathf.Max(shakeEnergy,strength)); Impacts++;
        }
        IEnumerator Corona(Vector3 p,float strength)
        {
            yield return Wait(.075f);
            Emit(ring,p,1,1.45f*strength,0,.33f,new Color(1,.72f,.24f,.5f));
            Emit(sparks,p,14,.17f,4.6f*strength,.66f,new Color(1,.93f,.67f,1));
        }
        public void Firework(Vector3 p,float intensity)
        {
            int count=Mathf.RoundToInt(Mathf.Lerp(9,19,Mathf.InverseLerp(1,1.55f,intensity)));
            Emit(sparks,p,count,.18f,3.3f,.44f,new Color(1,.94f,.7f,.95f));
            Emit(embers,p,count/2,.085f,3.8f,.55f,new Color(1,.75f,.27f,1));
            MatchAccentSound(intensity);
        }
        public void MatchAccentSound(float intensity)
        {if(intensity>=1.17f&&Time.time-lastFireworkSound>.35f){RoyalAudioDirector.Get?.Play(RoyalCue.Firework);lastFireworkSound=Time.time;}}
        public void Creation(Vector3 p)
        {Emit(ring,p,1,.6f,0,.2f,new Color(.75f,.9f,1,1));Emit(sparks,p,10,.33f,3,.34f,new Color(1,.9f,.3f,1));RoyalAudioDirector.Get?.Play(RoyalCue.Create);Reaction?.Invoke(.6f);}
        public void Match(Vector3 p,float intensity)
        {Reaction?.Invoke(Mathf.Min(.45f,intensity*.22f));}
        public void Chip(Vector3 p)
        {Emit(smoke,p,3,.43f,1.8f,.22f,new Color(1,.86f,.65f,.75f));Emit(embers,p,6,.13f,3,.3f,new Color(1,.73f,.2f,1));}
        void Hit(Square sq)
        {
            if(!sq||sq.IsNone()||sq.type==SquareTypes.UNDESTROYABLE)return;
            if(sq.item&&sq.item.currentType!=ItemsTypes.INGREDIENT) {sq.item.royalBlastClear=true;sq.item.juiceStagger=0;sq.item.DestroyItem(true);}
            else if(sq.block.Count>0) sq.DestroyBlock();
        }
        IEnumerator Explosion(Square origin,int radius)
        {
            Vector3 p=origin.transform.position;var visual=TNT3DAnimator.ActivateAt(origin);
            var fuse=TakeTntSprite(p);
            yield return TntAnticipation(fuse);
            if(fuse!=null){fuse.go.SetActive(false);fuse.used=false;}
            if(visual)visual.Explode(radius>2?1.45f:1);
            RoyalAudioDirector.Get?.Play(RoyalCue.Tnt);
            yield return Wait(.012f);
            var lm=LevelManager.THIS;
            for(int distance=0;distance<=radius;distance++)
            {
                for(int y=-distance;y<=distance;y++)for(int x=-distance;x<=distance;x++)if(Mathf.Max(Mathf.Abs(x),Mathf.Abs(y))==distance)
                {
                    var sq=lm.GetSquare(origin.col+x,origin.row+y);
                    if(sq&&!sq.IsNone()&&sq.type!=SquareTypes.UNDESTROYABLE&&(!sq.item||sq.item.currentType!=ItemsTypes.INGREDIENT))
                    {
                        if(visual)visual.CellImpact(sq.transform.position);
                        if(sq.item){sq.item.royalTntClear=true;sq.item.royalTntOrigin=p;}
                        if(sq.block.Count>0&&sq.block[sq.block.Count-1])CandyShatterService.Instance.Shatter(sq.block[sq.block.Count-1].GetComponentInChildren<SpriteRenderer>(),1,true,false,true);
                    }
                    Hit(sq);
                }
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
            yield return PropellerSequenceV5(origin,payload,count);
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
            if(payload==ItemsTypes.NONE||payload==ItemsTypes.HORIZONTAL_STRIPPED||payload==ItemsTypes.VERTICAL_STRIPPED)
            {yield return AnimateColorBallTargets(origin,targets,payload);yield break;}
            Creation(origin.transform.position);RoyalAudioDirector.Get?.Play(RoyalCue.Create);
            // The approved Boosting sequence grows out of the cell the ball fired from and holds its
            // loop while the targets are marked. Target selection and destruction are untouched.
            var boosting=ColorBallBoostView.Begin(origin.transform.position);
            yield return Wait(.16f);
            foreach(var sq in targets)
            {
                Emit(sparks,sq.transform.position,5,.35f,2,.3f,new Color(.8f,.9f,1,1));
                if(IsPower(payload)&&payload!=ItemsTypes.BOMB&&sq.item&&sq.item.currentType==ItemsTypes.NONE)
                {sq.item.currentType=payload;sq.item.debugType=payload;sq.item.NextType=ItemsTypes.NONE;}
            }
            yield return Wait(.18f);
            foreach(var sq in targets){Hit(sq);yield return Wait(all?.005f:.014f);}Reaction?.Invoke(1.4f);
            if(boosting)boosting.Finish();
        }
        // Workshop calls the same visual paths, without touching the player's board or save.
        public void Preview(string effect,Vector3 p)
        {
            if(effect=="TNT") StartCoroutine(PreviewTnt(p));
            else if(effect=="Match 3") {for(int i=-1;i<=1;i++)MatchVFXService.Instance.PlayMatchBurst(p+Vector3.right*i*Cell,new Color(.2f,.7f,1),1,i*.018f);MatchVFXService.Instance.PlayMatchCenter(p,Color.white,1);RoyalAudioDirector.Get?.Play(RoyalCue.Match);}
            else if(effect=="Match 4"||effect=="Match 5") {MatchVFXService.Instance.PlayMatchCenter(p,Color.white,effect=="Match 5"?1.36f:1.18f);RoyalAudioDirector.Get?.Play(RoyalCue.Create);}
            else if(effect=="Celebration")RoyalCelebration.Play(4);
            else if(effect=="Rocket"||effect=="Rocket V")StartCoroutine(PreviewRocket(p,effect!="Rocket V"));
            else StartCoroutine(PreviewFlight(effect,p));
        }
        IEnumerator PreviewTnt(Vector3 p)
        {
            var visual=TNT3DAnimator.PreviewAt(p);var fuse=TakeTntSprite(p);yield return TntAnticipation(fuse);
            if(fuse!=null){fuse.go.SetActive(false);fuse.used=false;}
            if(visual)visual.Explode();RoyalAudioDirector.Get?.Play(RoyalCue.Tnt);
        }
        IEnumerator TntAnticipation(Flyer f)
        {
            Vector3 scale=f!=null?f.go.transform.localScale:Vector3.one;float t=0;
            while(t<TNT3DAnimator.IgnitionDuration){
                if(!Paused)t+=Time.deltaTime;
                float k=Mathf.Clamp01(t/TNT3DAnimator.IgnitionDuration),compress=Mathf.Sin(k*Mathf.PI);
                if(f!=null){f.go.transform.localScale=Vector3.Scale(scale,new Vector3(1+.10f*compress,1-.12f*compress,1));f.sprite.color=Color.Lerp(Color.white,new Color(1,1,.55f),k*k*.6f);}
                yield return null;
            }
        }
        Flyer TakeTntSprite(Vector3 p)
        {
            var f=Take("tnt",p,.9f);if(f==null)return null;
            f.sprite.sprite=RoyalPieceSkin.For(ItemsTypes.PACKAGE,0);
            if(f.sprite.sprite){var size=f.sprite.sprite.bounds.size;f.go.transform.localScale=Vector3.one*(Cell*.9f/Mathf.Max(size.x,size.y));}
            f.trail.emitting=false;return f;
        }
        IEnumerator PreviewFlight(string effect,Vector3 p)
        {
            bool heli=effect=="Helicopter";var f=Take(heli?"helicopter":"rocket",p,.9f);RoyalAudioDirector.Get?.Play(heli?RoyalCue.Helicopter:RoyalCue.Rocket);
            float t=0;while(t<.55f){t+=Time.deltaTime;float k=Mathf.Clamp01(t/.55f);if(f!=null){f.go.transform.position=p+new Vector3((k-.5f)*Cell*7,heli?Mathf.Sin(k*Mathf.PI)*Cell*2:0,-.6f);f.go.transform.rotation=Quaternion.Euler(0,0,heli?-15:0);}yield return null;}Burst(p+Vector3.right*Cell*3.5f,.5f);yield return Retire(f);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace RoyalThrone.Editor
{
    public static class RoyalRocketChecks
    {
        const string Folder="Design/Effects/Rocket-review-v5/";
        public static IEnumerator Run(LevelManager lm,Action seed,Action colours,Action<bool,string> check)
        {
            // The internal QA menu also owns Time.timeScale in Update. Suspend that
            // owner while the Lab drives this review, then restore its prior state.
#if !ROYAL_DISABLE_QA
            var qa=RoyalThrone.Testing.RoyalQaService.Instance;bool qaEnabled=qa&&qa.enabled;
            if(qa)qa.enabled=false;
#endif
            try {yield return RoyalVfxLab.ReviewRocketSpeeds(speed=>FocusedAtSpeed(lm,seed,colours,check,speed));}
            finally {
#if !ROYAL_DISABLE_QA
                if(qa)qa.enabled=qaEnabled;
#endif
            }
        }
        static IEnumerator FocusedAtSpeed(LevelManager lm,Action seed,Action colours,Action<bool,string> check,float labSpeed)
        {
            Directory.CreateDirectory(Folder);
            check(Mathf.Approximately(Time.timeScale,labSpeed),"focused Rocket check uses actual game speed "+labSpeed);
            var fx=RoyalPowerupDirector.Get;
            float originalLength=fx.RocketExhaustLengthCells;
            fx.RocketExhaustLengthCells=7f;
            try
            {
                for(int direction=0;direction<2;direction++)
                {
                    fx.Cancel();lm.StopAllCoroutines();AI.THIS.StopAllCoroutines();AI.THIS.allowShowTip=false;lm.DragBlocked=true;
                    CandyShatterService.Instance.Clear();seed();yield return null;colours();yield return null;
                    bool vertical=direction==1;
                    string name=(vertical?"vertical":"horizontal")+"-"+labSpeed;
                    var origin=lm.GetSquare(vertical?3:0,vertical?7:3).item;
                    origin.currentType=vertical?ItemsTypes.VERTICAL_STRIPPED:ItemsTypes.HORIZONTAL_STRIPPED;
                    yield return null;

                    float pitch=Mathf.Abs(vertical?lm.squareHeight*lm.GameField.lossyScale.y:lm.squareWidth*lm.GameField.lossyScale.x);
                    int launchStart=fx.RocketLaunches,sparkStart=fx.RocketSparkParticlesEmitted;
                    int objectCount=fx.GetComponentsInChildren<Transform>(true).Length;
                    float started=Time.time,timeout=Time.realtimeSinceStartup+12f,maxRibbon=0,minVelocity=float.PositiveInfinity,maxVelocity=0;
                    bool attached=true,lifetime=true,shader=true;int captures=0;
                    var previous=new Dictionary<SpriteRenderer,Vector3>();
                    var previousTime=new Dictionary<SpriteRenderer,float>();
                    origin.DestroyItem(true);
                    do
                    {
                        yield return new WaitForEndOfFrame();
                        foreach(var head in fx.GetComponentsInChildren<SpriteRenderer>())
                        {
                            if(!head.enabled||!head.gameObject.name.StartsWith("Projectile"))continue;
                            var ribbon=head.transform.Find("Continuous flame ribbon").GetComponent<LineRenderer>();
                            if(!ribbon.enabled||ribbon.positionCount!=2)continue;
                            float cells=Vector3.Distance(ribbon.GetPosition(0),ribbon.GetPosition(1))/pitch;
                            maxRibbon=Mathf.Max(maxRibbon,cells);
                            attached&=Vector3.Distance(ribbon.GetPosition(1),head.transform.position)<.001f;
                            shader&=ribbon.sharedMaterial&&ribbon.sharedMaterial.shader.isSupported&&ribbon.sharedMaterial.shader.name=="RoyalThrone/RocketFire";
                            var core=head.transform.Find("White-hot rocket core").GetComponent<TrailRenderer>();
                            lifetime&=Mathf.Abs(core.time*10f-7f)<.01f&&Mathf.Abs(head.GetComponent<TrailRenderer>().time*10f-7f)<.01f;
                            if(previous.TryGetValue(head,out var old)&&Time.time>previousTime[head])
                            {
                                float velocity=Vector3.Distance(old,head.transform.position)/(Time.time-previousTime[head])/pitch;
                                if(velocity>.1f){minVelocity=Mathf.Min(minVelocity,velocity);maxVelocity=Mathf.Max(maxVelocity,velocity);}
                            }
                            previous[head]=head.transform.position;previousTime[head]=Time.time;
                        }
                        if(maxRibbon>=6.75f&&captures<8)
                        {
                            var tex=ScreenCapture.CaptureScreenshotAsTexture();
                            File.WriteAllBytes(Folder+name+"-mid-"+captures.ToString("D2")+".png",tex.EncodeToPNG());
                            Object.Destroy(tex);captures++;
                        }
                        if(!RoyalPowerupDirector.Busy&&!lm.IsResolvingMatches)
                        {lm.DragBlocked=true;lm.StopAllCoroutines();AI.THIS.StopAllCoroutines();}
                    }while((RoyalPowerupDirector.Busy||lm.IsResolvingMatches||fx.RocketVisualsActive)&&Time.realtimeSinceStartup<timeout);

                    check(fx.RocketLaunches==launchStart+1,name+": exactly one two-headed Rocket sequence launches");
                    check(maxRibbon>=6.75f&&maxRibbon<=7.015f,name+": mature FireExhaust measures about seven actual cells");
                    check(attached,name+": exhaust remains attached directly to every nozzle");
                    check(lifetime,name+": trailLifetime equals seven cells divided by the fixed speed");
                    check(shader,name+": custom layered Rocket shader is active and supported");
                    check(captures>0,name+": captured mid-flight frames with the mature exhaust visible");
                    check(minVelocity<float.PositiveInfinity&&Mathf.Abs((minVelocity+maxVelocity)*.5f-10f)<.65f&&maxVelocity/minVelocity<1.12f,name+": projectile holds constant 10 cells/sec");
                    check(fx.RocketSparkParticlesEmitted-sparkStart>=200,name+": dense spatial four-point stars fill the plume");
                    check(!RoyalPowerupDirector.Busy&&!fx.RocketVisualsActive&&fx.RocketParticleCount==0,name+": Rocket, trails, smoke and sparkles return to pools");
                    check(CandyShatterService.Instance.ActiveFragments==0,name+": coloured debris returns to its pool");
                    check(fx.GetComponentsInChildren<Transform>(true).Length==objectCount,name+": no pooled VFX object leaks");
                    check(Time.time-started<8f,name+": focused flight and tail complete without a hang");
                }
            }
            finally {fx.Cancel();fx.RocketExhaustLengthCells=originalLength;}
        }
        static IEnumerator CompareLengths(LevelManager lm,Action seed,Action colours,Action<bool,string> check,float speed)
        {
            var fx=RoyalPowerupDirector.Get;float original=fx.RocketExhaustLengthCells;
            try
            {
                foreach(float length in new[]{6.5f,7f,7.5f})for(int direction=0;direction<2;direction++)
                {
                    fx.Cancel();lm.StopAllCoroutines();AI.THIS.StopAllCoroutines();AI.THIS.allowShowTip=false;lm.DragBlocked=true;
                    CandyShatterService.Instance.Clear();seed();yield return null;colours();yield return null;
                    fx.RocketExhaustLengthCells=length;
                    bool vertical=direction==1;var origin=lm.GetSquare(vertical?3:0,vertical?7:3).item;
                    origin.currentType=vertical?ItemsTypes.VERTICAL_STRIPPED:ItemsTypes.HORIZONTAL_STRIPPED;
                    yield return null;
                    string name="length-"+length.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+(vertical?"-vertical-":"-horizontal-")+speed;
                    float pitch=Mathf.Abs(vertical?lm.squareHeight*lm.GameField.lossyScale.y:lm.squareWidth*lm.GameField.lossyScale.x);
                    float started=Time.time,nextCapture=.15f,maxLength=0;int frame=0,objects=fx.GetComponentsInChildren<Transform>(true).Length;
                    bool bounded=true,attached=true,lifeMatches=true,validShader=true;
                    var lastPositions=new Dictionary<SpriteRenderer,Vector3>();var lastTimes=new Dictionary<SpriteRenderer,float>();
                    origin.DestroyItem(true);
                    do
                    {
                        yield return new WaitForEndOfFrame();
                        foreach(var head in fx.GetComponentsInChildren<SpriteRenderer>())
                        {
                            if(!head.enabled||!head.gameObject.name.StartsWith("Projectile"))continue;
                            var ribbon=head.transform.Find("Continuous flame ribbon").GetComponent<LineRenderer>();
                            if(!ribbon.enabled||ribbon.positionCount!=2)continue;
                            float cells=Vector3.Distance(ribbon.GetPosition(0),ribbon.GetPosition(1))/pitch;maxLength=Mathf.Max(maxLength,cells);
                            bounded&=cells<=length+.015f;attached&=Vector3.Distance(ribbon.GetPosition(1),head.transform.position)<.001f;
                            validShader&=ribbon.sharedMaterial.shader.isSupported&&ribbon.textureMode==LineTextureMode.Stretch;
                            if(lastPositions.TryGetValue(head,out var prev)&&Time.time>lastTimes[head])
                            {
                                float velocity=Vector3.Distance(prev,head.transform.position)/(Time.time-lastTimes[head])/pitch;
                                if(velocity>.1f)
                                {
                                    var core=head.transform.Find("White-hot rocket core").GetComponent<TrailRenderer>();
                                    lifeMatches&=Mathf.Abs(core.time*velocity-length)<.12f&&Mathf.Abs(head.GetComponent<TrailRenderer>().time*velocity-length)<.12f;
                                }
                            }
                            lastPositions[head]=head.transform.position;lastTimes[head]=Time.time;
                        }
                        if(Time.time-started>=nextCapture&&frame<30)
                        {
                            var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+name+"-"+frame.ToString("D2")+".png",tex.EncodeToPNG());Object.Destroy(tex);
                            frame++;nextCapture+=.08f;
                        }
                        if(!RoyalPowerupDirector.Busy){lm.StopAllCoroutines();AI.THIS.StopAllCoroutines();lm.DragBlocked=true;}
                    }while((RoyalPowerupDirector.Busy||fx.RocketVisualsActive)&&Time.time-started<8);
                    check(bounded&&Mathf.Abs(maxLength-length)<.02f,name+": measured mature ribbon "+maxLength.ToString("F3")+" actual gem cells, never exceeds budget");
                    check(attached,name+": flame stays attached to nozzle every frame");
                    check(lifeMatches,name+": measured speed times core/glow lifetime equals cell-length target");
                    check(validShader,name+": normalized plume shader supported");
                    check(!fx.RocketVisualsActive,name+": all trails and soft-tail particles fade and return to pool");
                    check(fx.GetComponentsInChildren<Transform>(true).Length==objects,name+": no VFX object growth");
                }
            }
            finally {fx.Cancel();fx.RocketExhaustLengthCells=original;}
        }
        static float DistanceToSegment(Vector2 point,Vector2 start,Vector2 end)
        {
            Vector2 delta=end-start;float t=delta.sqrMagnitude>.000001f?Mathf.Clamp01(Vector2.Dot(point-start,delta)/delta.sqrMagnitude):0;
            return Vector2.Distance(point,start+delta*t);
        }
        static Vector3 NoseOffset(SpriteRenderer head,float cell)
        {
            string n=head.sprite.name;
            return (n.EndsWith("Right")?Vector3.right:n.EndsWith("Left")?Vector3.left:n.EndsWith("Up")?Vector3.up:Vector3.down)*cell*.45f;
        }
        static IEnumerator AtSpeed(LevelManager lm,Action seed,Action colours,Action<bool,string> check,float speed)
        {
            Directory.CreateDirectory(Folder);
            check(Mathf.Approximately(Time.timeScale,speed),"VFX Lab uses actual game speed "+speed);
            var fx=RoyalPowerupDirector.Get;
            yield return CompareLengths(lm,seed,colours,check,speed);
            for(int scenario=0;scenario<9;scenario++)
            {
                fx.Cancel();lm.StopAllCoroutines();AI.THIS.StopAllCoroutines();AI.THIS.allowShowTip=false;lm.DragBlocked=true;CandyShatterService.Instance.Clear();seed();yield return null;colours();yield return null;
                bool vertical=scenario==1||scenario==6||scenario==7;
                string name=new[]{"horizontal","vertical","blockers","chain","cascade","left-edge","under-hud","above-bottom-bar","opposite-rockets"}[scenario]+"-"+speed;
                int originX=scenario==5||scenario==8?0:3,originY=scenario==6?0:scenario==7?7:3;
                Item At(int x,int y)=>lm.GetSquare(x,y).item;
                var origin=At(originX,originY);origin.currentType=vertical?ItemsTypes.VERTICAL_STRIPPED:ItemsTypes.HORIZONTAL_STRIPPED;
                Square blocked=null;
                if(scenario==2)
                {
                    blocked=lm.GetSquare(6,3);Object.DestroyImmediate(blocked.item.gameObject);blocked.item=null;
                    var go=Object.Instantiate(Resources.Load<GameObject>("Prefabs/WireBlock"),blocked.transform);
                    go.transform.localPosition=Vector3.zero;blocked.block.Add(go);blocked.type=SquareTypes.WIREBLOCK;
                }
                if(scenario==3)At(5,3).currentType=ItemsTypes.VERTICAL_STRIPPED;
                if(scenario==8)At(7,3).currentType=ItemsTypes.HORIZONTAL_STRIPPED;
                yield return null;
                var beforeItems=lm.squaresArray.Where(s=>s&&s.item).ToDictionary(s=>s,s=>s.item);
                var poolGradients=fx.GetComponentsInChildren<TrailRenderer>(true).Where(t=>t.gameObject.name.StartsWith("Projectile")).ToDictionary(t=>t,t=>t.colorGradient);
                int exitStart=fx.RocketScreenExits;
                int sparkStart=fx.RocketSparkParticlesEmitted;
                int before=fx.RocketCellImpacts,launches=fx.RocketLaunches,objects=fx.GetComponentsInChildren<Transform>(true).Length;
                float started=Time.time,until=Time.realtimeSinceStartup+20,nextCapture=.10f;int frame=0,inspected=before,peakParticles=0,peakExhaust=0;bool leads=true,untouched=true,correctArt=true;
                bool underTop=false,underBottom=false,outsideBoard=false,continuousFire=false;
                float minVelocity=float.PositiveInfinity,maxVelocity=0;
                var previousPositions=new Dictionary<SpriteRenderer,Vector3>();var previousTimes=new Dictionary<SpriteRenderer,float>();
                var moving=new HashSet<SpriteRenderer>();
                var sweptStarts=new Dictionary<SpriteRenderer,Vector3>();var sweptEnds=new Dictionary<SpriteRenderer,Vector3>();
                float maximumSampleStep=0,maximumEndDistance=0;
                int[] colourRows={Mathf.RoundToInt(Screen.height*.993f),Mathf.RoundToInt(Screen.height*.099f)};
                yield return new WaitForEndOfFrame();
                var baseline=ScreenCapture.CaptureScreenshotAsTexture();
                int screenX=Mathf.RoundToInt(Camera.main.WorldToViewportPoint(origin.transform.position).x*Screen.width);
                var panelColours=colourRows.Select(y=>baseline.GetPixel(screenX,y)).ToArray();Object.Destroy(baseline);
                bool protectedUi=true,speedStable=true,bounded=true;
                origin.DestroyItem(true);
                if(scenario==8)At(7,3).DestroyItem(true);
                if(scenario==4)lm.FindMatches();
                do
                {
                    yield return new WaitForEndOfFrame();
                    speedStable&=Mathf.Approximately(Time.timeScale,speed);
                    peakParticles=Mathf.Max(peakParticles,fx.RocketParticleCount);
                    peakExhaust=Mathf.Max(peakExhaust,fx.RocketExhaustCount);
                    foreach(var head in fx.GetComponentsInChildren<SpriteRenderer>())
                    {
                        if(!head.enabled||!head.gameObject.name.StartsWith("Projectile"))continue;
                        var view=Camera.main.WorldToViewportPoint(head.transform.position);
                        underTop|=view.y>.94f;underBottom|=view.y<.06f;
                        var first=lm.GetSquare(0,0).transform.position;var last=lm.GetSquare(7,7).transform.position;
                        outsideBoard|=head.transform.position.x<first.x-lm.squareWidth||head.transform.position.x>last.x+lm.squareWidth||head.transform.position.y>first.y+lm.squareHeight||head.transform.position.y<last.y-lm.squareHeight;
                        var id=head;
                        if(previousPositions.TryGetValue(id,out var prev)&&Time.time>previousTimes[id])
                        {
                            float delta=Vector3.Distance(head.transform.position,prev);
                            if(delta>.001f&&moving.Contains(id)&&scenario!=3&&scenario!=4)
                            {float velocity=delta/(Time.time-previousTimes[id]);minVelocity=Mathf.Min(minVelocity,velocity);maxVelocity=Mathf.Max(maxVelocity,velocity);}
                            if(delta>.001f)moving.Add(id);
                        }
                        sweptStarts[id]=previousPositions.TryGetValue(id,out var old)?old:head.transform.position;
                        sweptEnds[id]=head.transform.position;
                        maximumSampleStep=Mathf.Max(maximumSampleStep,Vector3.Distance(sweptStarts[id],sweptEnds[id]));
                        previousPositions[id]=head.transform.position;previousTimes[id]=Time.time;
                        var ribbon=head.transform.Find("Continuous flame ribbon").GetComponent<LineRenderer>();
                        if(ribbon.enabled&&ribbon.positionCount==2)
                        {
                            var start=ribbon.GetPosition(0);var end=ribbon.GetPosition(ribbon.positionCount-1);
                            continuousFire|=Vector3.Distance(start,end)>lm.squareWidth*2.5f && ribbon.widthMultiplier>=lm.squareWidth*.90f;
                            bounded&=Vector3.Distance(start,end)<=fx.RocketExhaustLengthCells*Mathf.Abs(lm.squareWidth*lm.GameField.lossyScale.x)+.01f;
                        }
                    }
                    foreach(var sr in fx.GetComponentsInChildren<SpriteRenderer>())
                        if(sr.enabled&&sr.gameObject.name.StartsWith("Projectile"))
                            correctArt &= sr.sprite&&sr.sprite.name.StartsWith("RoyalRocket_") &&
                                (sr.sprite.texture==RoyalGameplayArt.Art("rocket").texture || sr.sprite.texture==RoyalGameplayArt.Art("rocket-vertical").texture);
                    if(scenario<3)
                    {
                        var impacted=new HashSet<Vector3>();for(int n=before;n<fx.RocketCellImpacts;n++)impacted.Add(fx.RocketImpactPositions[n%512]);
                        foreach(var entry in beforeItems)
                            if(entry.Value!=origin&&!impacted.Contains(entry.Key.transform.position)&&entry.Key!=blocked)
                                untouched &= entry.Key.item==entry.Value && entry.Value && !entry.Value.destroying;
                    }
                    for(int n=inspected;n<fx.RocketCellImpacts;n++)
                    {
                        var pos=fx.RocketImpactPositions[n%512];float cell=Mathf.Abs(lm.squareWidth*lm.GameField.lossyScale.x);
                        if(scenario<3)
                        {
                            // PNG capture can span a substantial part of a fast sweep. Check the
                            // actual segment traversed since the last sample, not only its end.
                            var heads=sweptEnds.Keys.Where(sr=>sr&&sr.enabled).ToArray();
                            maximumEndDistance=Mathf.Max(maximumEndDistance,heads.Length>0?heads.Min(sr=>Vector2.Distance(sr.transform.position,pos))/cell:0);
                            leads&=heads.Any(sr=>DistanceToSegment(pos,sweptStarts[sr]+NoseOffset(sr,cell),sweptEnds[sr]+NoseOffset(sr,cell))<=cell*.55f);
                        }
                    }
                    inspected=fx.RocketCellImpacts;
                    if(!RoyalPowerupDirector.Busy&&!lm.IsResolvingMatches){lm.DragBlocked=true;lm.StopAllCoroutines();AI.THIS.StopAllCoroutines();}
                    if(Time.time-started>=nextCapture && frame<40)
                    {var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+name+"-"+frame.ToString("D2")+".png",tex.EncodeToPNG());
                        if(vertical)for(int y=0;y<colourRows.Length;y++)
                        {var c=tex.GetPixel(screenX,colourRows[y]);var b=panelColours[y];protectedUi&=Mathf.Abs(c.r-b.r)+Mathf.Abs(c.g-b.g)+Mathf.Abs(c.b-b.b)<.025f;}
                        Object.Destroy(tex);frame++;nextCapture+=.09f;}
                }while((RoyalPowerupDirector.Busy||lm.IsResolvingMatches||fx.RocketVisualsActive)&&Time.realtimeSinceStartup<until);
                // The resolver restarts its idle checker. Hold this disposable board
                // while particle tails expire, so it cannot start a shuffle/banner.
                if(!RoyalPowerupDirector.Busy&&!lm.IsResolvingMatches)
                {lm.DragBlocked=true;lm.StopAllCoroutines();AI.THIS.StopAllCoroutines();}
                yield return new WaitForSeconds(1f);
                check(speedStable,name+": Lab speed remains correct throughout flight");
                check(bounded,name+": exhaust remains within the seven-cell budget throughout flight");
                check(!RoyalPowerupDirector.Busy&&!lm.IsResolvingMatches,name+": rocket/chains/cascades settle");
                check(fx.LastRocketSweepDuration>=.42f&&fx.LastRocketSweepDuration<=.86f,name+": sweep lasts 0.42-0.86 game seconds at one constant cells-per-second speed");
                check(fx.RocketImpactTimes[before%512]-started>=.15f,name+": visible launch before first contact");
                check(fx.RocketCellImpacts-before>=8,name+": travel reaches every row/column cell");
                for(int n=before;n<fx.RocketCellImpacts;n++)
                    check(fx.RocketBreakTimes[n%512]>=fx.RocketImpactTimes[n%512]+.05f,name+": cell"+(n-before)+" white impact precedes mutation");
                if(scenario<3){check(leads,name+": physical rocket nose sweeps every contacted cell (max sample step "+(maximumSampleStep/Mathf.Abs(lm.squareWidth*lm.GameField.lossyScale.x)).ToString("F3")+" cells; max end distance "+maximumEndDistance.ToString("F3")+")");check(untouched,name+": cells remain intact until reached");}
                if(scenario==0||scenario==1)
                {
                    for(int n=0;n<8;n++)check(!(vertical?lm.GetSquare(3,n).item:lm.GetSquare(n,3).item),name+": intended cell cleared"+n);
                    check(lm.GetItems().Count(i=>i)==56,name+": unrelated cells stay intact");
                }
                if(blocked)check(blocked.block.Count==0,name+": crossed blocker removed by existing service");
                if(scenario==3)check(fx.RocketLaunches-launches==2,name+": chained rocket activates once");
                if(scenario==4)check(lm.GetItems().Where(i=>i).All(i=>!i.destroying&&!i.falling&&i.square.item==i),name+": cascade restores valid ownership");
                check(fx.RocketUsesCustomVfx,name+": custom rocket shader and six fixed pooled emitters");
                check(correctArt,name+": moving heads preserve supplied rocket textures");
                check(peakExhaust>=2,name+": both heads show animated fire exhaust");
                check(fx.RocketExhaustCount==0,name+": fire exhaust releases with projectile pool");
                check(fx.RocketSparkParticlesEmitted-sparkStart>=160,name+": dense spatial star trail emits along travel");
                check(peakParticles>=70,name+": layered custom effects are visible during sweep");
                check(fx.GetComponentsInChildren<Transform>(true).Length==objects,name+": no additional projectile or VFX objects allocated");
                check(poolGradients.All(pair=>new[]{0f,.25f,.5f,.75f,1f}.All(t=>Vector4.Distance(pair.Key.colorGradient.Evaluate(t),pair.Value.Evaluate(t))<.001f)),name+": rocket returns the original trail gradient before another booster borrows the pool");
                check(CandyShatterService.Instance.ActiveFragments==0,name+": debris pool cleans up");
                check(fx.RocketParticleCount==0,name+": trail sparks fade after exit");
                int exits=fx.RocketScreenExits-exitStart;
                check(exits>=2,name+": both projectiles retire past the screen, not the board");
                for(int n=exitStart;n<fx.RocketScreenExits;n++)
                {var vp=fx.RocketExitViewports[n%128];check(vp.x<0||vp.x>1||vp.y<0||vp.y>1,name+": exit "+(n-exitStart)+" lies outside camera viewport");}
                check(outsideBoard,name+": rendered projectile crosses beyond board bounds");
                check(continuousFire,name+": broad flame ribbon remains across several travelled cells");
                if(scenario!=3&&scenario!=4)check(maxVelocity>0&&maxVelocity/minVelocity<1.12f,name+": steady velocity after launch");
                if(vertical){check(underTop&&underBottom,name+": both heads travel underneath HUD and bottom bar");check(protectedUi,name+": opaque HUD and bottom rail pixels stay unchanged over rocket effects");}
                var root=fx.transform.Find("GameplayVFX - unmasked rocket line clear");
                check(root&&root.GetComponentInParent<Canvas>()==null&&root.GetComponentInParent<RectMask2D>()==null&&root.GetComponentInParent<Mask>()==null,name+": rocket container is outside all board/UI masks");
                var hud=lm.transform.Find("Royal gameplay HUD").GetComponent<Canvas>();
                check(hud.renderMode==RenderMode.ScreenSpaceOverlay&&hud.sortingOrder>=20,name+": HUD and boosters render above world VFX");
                var popup=GameObject.Find("CanvasGlobal").GetComponent<Canvas>();
                check(SortingLayer.GetLayerValueFromID(popup.sortingLayerID)>SortingLayer.GetLayerValueFromName("Default"),name+": pause/menu UI sorting layer exceeds rocket layer");
            }
            // Exercise the exact visual entry point used by QA and the Effects Workshop.
            for(int direction=0;direction<2;direction++)
            {
                fx.Cancel();lm.StopAllCoroutines();AI.THIS.StopAllCoroutines();lm.DragBlocked=true;
                CandyShatterService.Instance.Clear();seed();yield return null;colours();yield return null;
                var items=lm.squaresArray.ToDictionary(sq=>sq,sq=>sq.item);
                int moves=lm.Limit,score=LevelManager.Score,coins=InitScript.Gems,lives=InitScript.lifes;
                int objects=fx.GetComponentsInChildren<Transform>(true).Length;
                string effect=direction==0?"Rocket":"Rocket V",name="preview-"+direction+"-"+speed;
                for(int replay=0;replay<2;replay++)
                {
                    int launch=fx.RocketLaunches;float start=Time.time;int exhaust=0;bool captured=false;
                    fx.Preview(effect,lm.GetSquare(3,3).transform.position);
                    while((Time.time-start<.5f||fx.RocketVisualsActive)&&Time.time-start<8f)
                    {
                        yield return new WaitForEndOfFrame();exhaust=Mathf.Max(exhaust,fx.RocketExhaustCount);
                        if(!captured&&Time.time-start>.68f)
                        {var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+name+"-"+replay+".png",tex.EncodeToPNG());Object.Destroy(tex);captured=true;}
                    }
                    check(fx.RocketLaunches==launch+1&&exhaust==2,name+": replay "+replay+" uses the two-headed fire sequence");
                    check(items.All(pair=>pair.Value&&pair.Key.item==pair.Value&&!pair.Value.destroying),name+": preview preserves every gem");
                    check(lm.Limit==moves&&LevelManager.Score==score&&InitScript.Gems==coins&&InitScript.lifes==lives,name+": preview grants no rewards or moves");
                    check(!RoyalPowerupDirector.Busy&&fx.RocketExhaustCount==0&&fx.RocketParticleCount==0,name+": replay returns all effects to pool");
                    check(fx.GetComponentsInChildren<Transform>(true).Length==objects,name+": replay does not allocate new effect objects");
                }
            }
        }
    }
}

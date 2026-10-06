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
        const string Folder="Design/Effects/Rocket-review-v7/";
        public static IEnumerator Run(LevelManager lm,Action seed,Action colours,Action<bool,string> check)
        {
            // A level-one host can have a tutorial overlay. Hide only its graphics
            // during disposable Rocket captures and restore them without UI events.
            var tutorial=Object.FindAnyObjectByType<JellyGarden.Scripts.GUI.TutorialStuff.TutorialManager>();
            var overlay=tutorial&&tutorial.tutorial?tutorial.tutorial.GetComponentsInChildren<Graphic>(true).Where(g=>g.enabled).ToArray():Array.Empty<Graphic>();
            foreach(var graphic in overlay)graphic.enabled=false;
            // The internal QA menu also owns Time.timeScale in Update. Suspend that
            // owner while the Lab drives this review, then restore its prior state.
#if !ROYAL_DISABLE_QA
            var qa=RoyalThrone.Testing.RoyalQaService.Instance;bool qaEnabled=qa&&qa.enabled;
            if(qa)qa.enabled=false;
#endif
            try {yield return RoyalVfxLab.ReviewRocketSpeeds(speed=>FocusedAtSpeed(lm,seed,colours,check,speed));}
            finally {
                foreach(var graphic in overlay)if(graphic)graphic.enabled=true;
#if !ROYAL_DISABLE_QA
                if(qa)qa.enabled=qaEnabled;
#endif
            }
        }
        // The Rocket's travel speed with the profile's animation rate (15 cells/s x 0.95 since 2026-10-04).
        static float RocketCps=>RoyalEffectsProfile.Get.rocketCellsPerSecond*RoyalEffectsProfile.Get.rocketAnimationRate;
        static IEnumerator FocusedAtSpeed(LevelManager lm,Action seed,Action colours,Action<bool,string> check,float labSpeed)
        {
            Directory.CreateDirectory(Folder);
            check(Mathf.Approximately(Time.timeScale,labSpeed),"focused Rocket check uses actual game speed "+labSpeed);
            var fx=RoyalPowerupDirector.Get;
            float originalLength=fx.RocketExhaustLengthCells,originalCapture=Time.captureDeltaTime;
            fx.RocketExhaustLengthCells=7f;
            // A fixed 60 fps step makes every sampled animation frame reproducible at both Lab speeds.
            Time.captureDeltaTime=1f/60f;
            try
            {
                for(int direction=0;direction<2;direction++)
                {
                    fx.Cancel();lm.StopAllCoroutines();AI.THIS.StopAllCoroutines();AI.THIS.allowShowTip=false;lm.DragBlocked=true;
                    CandyShatterService.Instance.Clear();seed();yield return null;colours();yield return null;
                    bool vertical=direction==1;
                    string name=(vertical?"vertical":"horizontal")+"-"+labSpeed;
                    bool record=Mathf.Approximately(labSpeed,1f);
                    if(record)BeginReplay(name);
                    var creationCell=lm.GetSquare(vertical?3:0,vertical?7:3);
                    yield return CheckCreation(lm,creationCell,vertical,name,check,record);
                    var origin=creationCell.item;
                    yield return CheckIdle(lm,origin,name,check,record);

                    float pitch=Mathf.Abs(vertical?lm.squareHeight*lm.GameField.lossyScale.y:lm.squareWidth*lm.GameField.lossyScale.x);
                    int launchStart=fx.RocketLaunches,sparkStart=fx.RocketSparkParticlesEmitted;
                    int objectCount=fx.GetComponentsInChildren<Transform>(true).Length;
                    float started=Time.time,timeout=Time.realtimeSinceStartup+12f,maxRibbon=0,minVelocity=float.PositiveInfinity,maxVelocity=0;
                    bool attached=true,lifetime=true,shader=true,directional=true,flightFit=true;int captures=0;
                    var seenDirections=new HashSet<string>();
                    var previous=new Dictionary<SpriteRenderer,Vector3>();
                    var previousTime=new Dictionary<SpriteRenderer,float>();
                    // Activate before rendering, like a tap or chain hit in Update. The sprite still holds
                    // the last Idle pose on screen, measured along the travel axis.
                    yield return null;
                    var idleBounds=origin.sprRenderer.bounds;var idleSprite=origin.sprRenderer.sprite;
                    float idleAlong=vertical?idleBounds.size.y:idleBounds.size.x;
                    check(fx.RocketLaunchStandInsActive==0,name+": launch stand-ins are released before activation");
                    bool continues=false,noGap=true,switched=false,stretchShot=false;int standInFrames=0;
                    float minAlong=float.PositiveInfinity,maxAlong=0,compressAt=-1,stretchAt=-1,switchAt=-1,firstAlong=0;
                    float activation=Time.time;
                    origin.DestroyItem(true);
                    do
                    {
                        yield return new WaitForEndOfFrame();
                        if(record&&Time.time-activation<.75f)ReplayFrame();
                        SpriteRenderer standIn=null;bool flyers=false;
                        foreach(var head in fx.GetComponentsInChildren<SpriteRenderer>())
                        {
                            if(head.enabled&&head.gameObject.name.StartsWith("Rocket launch stand-in"))standIn=head;
                            if(!head.enabled||!head.gameObject.name.StartsWith("Projectile"))continue;
                            flyers=true;
                            string expected=vertical?(head.sprite.name.EndsWith("Up")?"up":"down"):(head.sprite.name.EndsWith("Right")?"right":"left");
                            seenDirections.Add(expected);
                            directional&=head.sprite.texture.name=="rocket_"+expected&&Quaternion.Angle(head.transform.rotation,Quaternion.identity)<.01f;
                            var bounds=head.bounds;
                            flightFit&=Mathf.Max(bounds.size.x,bounds.size.y)/pitch<=.641f;
                            float nozzle=expected=="up"?bounds.min.y:expected=="down"?bounds.max.y:expected=="right"?bounds.min.x:bounds.max.x;
                            attached&=Mathf.Abs(nozzle-(vertical?head.transform.position.y:head.transform.position.x))<.005f;
                            var ribbon=head.transform.Find("Continuous flame ribbon").GetComponent<LineRenderer>();
                            if(!ribbon.enabled||ribbon.positionCount!=2)continue;
                            float cells=Vector3.Distance(ribbon.GetPosition(0),ribbon.GetPosition(1))/pitch;
                            maxRibbon=Mathf.Max(maxRibbon,cells);
                            attached&=Vector3.Distance(ribbon.GetPosition(1),head.transform.position)<.001f;
                            shader&=ribbon.sharedMaterial&&ribbon.sharedMaterial.shader.isSupported&&ribbon.sharedMaterial.shader.name=="RoyalThrone/RocketFire";
                            var core=head.transform.Find("White-hot rocket core").GetComponent<TrailRenderer>();
                            lifetime&=Mathf.Abs(core.time*RocketCps-7f)<.01f&&Mathf.Abs(head.GetComponent<TrailRenderer>().time*RocketCps-7f)<.01f;
                            if(previous.TryGetValue(head,out var old)&&Time.time>previousTime[head])
                            {
                                float velocity=Vector3.Distance(old,head.transform.position)/(Time.time-previousTime[head])/pitch;
                                if(velocity>.1f){minVelocity=Mathf.Min(minVelocity,velocity);maxVelocity=Mathf.Max(maxVelocity,velocity);}
                            }
                            previous[head]=head.transform.position;previousTime[head]=Time.time;
                        }
                        if(!switched)
                        {
                            if(standIn&&!flyers)
                            {
                                // Idle -> Launch: the stand-in continues the piece in place, then compresses and stretches.
                                var b=standIn.bounds;float along=(vertical?b.size.y:b.size.x)/idleAlong;
                                if(standInFrames==0){firstAlong=along;continues=standIn.sprite==idleSprite&&Vector2.Distance(b.center,idleBounds.center)<.01f&&along>.85f&&along<1.02f;CaptureRocket(name+"-launch-first-frame");}
                                standInFrames++;
                                if(along<minAlong){minAlong=along;compressAt=Time.time;}
                                if(along>maxAlong){maxAlong=along;stretchAt=Time.time;}
                                if(along>=1.30f&&!stretchShot){stretchShot=true;CaptureRocket(name+"-launch-stretch");}
                            }
                            else if(flyers&&!standIn){switched=true;switchAt=Time.time;CaptureRocket(name+"-launch-passing");}
                            else noGap=false;
                        }
                        if(maxRibbon>=6.75f&&captures<3)
                        {
                            var tex=ScreenCapture.CaptureScreenshotAsTexture();
                            File.WriteAllBytes(Folder+name+"-mid-"+captures.ToString("D2")+".png",tex.EncodeToPNG());
                            Object.Destroy(tex);captures++;
                        }
                        if(!RoyalPowerupDirector.Busy&&!lm.IsResolvingMatches)
                        {lm.DragBlocked=true;lm.StopAllCoroutines();AI.THIS.StopAllCoroutines();}
                    }while((RoyalPowerupDirector.Busy||lm.IsResolvingMatches||fx.RocketVisualsActive||CandyShatterService.Instance.ActiveFragments>0)&&Time.realtimeSinceStartup<timeout);

                    check(continues,name+": Idle -> Launch continues the idle Rocket in place on the first frame (length "+firstAlong.ToString("F3")+", no lag or pop)");
                    check(minAlong<=.90f&&maxAlong>=1.20f&&compressAt<stretchAt,name+": launch compresses to "+minAlong.ToString("F2")+" then stretches to "+maxAlong.ToString("F2")+" along its travel axis");
                    check(switched&&noGap&&standInFrames>=2,name+": Launch -> Passing switches in one frame with no missing or doubled Rocket ("+standInFrames+" anticipation frames)");
                    check(switchAt-activation>=0f&&switchAt-activation<=.04f,name+": the launch follows the tap at once ("+(switchAt-activation).ToString("F3")+" s)");
                    check(fx.RocketLaunches==launchStart+1,name+": exactly one two-headed Rocket sequence launches");
                    check(fx.LastRocketAnticipation<.001f,name+": a tapped Rocket launches with no anticipation hold");
                    check(directional&&seenDirections.SetEquals(vertical?new[]{"up","down"}:new[]{"left","right"}),name+": both dedicated directional sprites launch without rotation or missing artwork");
                    check(flightFit,name+": Passing artwork has its independent 0.64-cell size");
                    check(maxRibbon>=6.75f&&maxRibbon<=7.015f,name+": mature FireExhaust measures about seven actual cells");
                    check(attached,name+": exhaust remains attached directly to every nozzle");
                    check(lifetime,name+": trailLifetime equals seven cells divided by the fixed speed");
                    check(shader,name+": custom layered Rocket shader is active and supported");
                    check(captures>0,name+": captured mid-flight frames with the mature exhaust visible");
                    check(minVelocity<float.PositiveInfinity&&Mathf.Abs((minVelocity+maxVelocity)*.5f-RocketCps)<.65f&&maxVelocity/minVelocity<1.12f,name+": projectile holds constant "+RocketCps.ToString("F2")+" cells/sec (measured "+minVelocity.ToString("F2")+"–"+maxVelocity.ToString("F2")+")");
                    check(fx.RocketSparkParticlesEmitted-sparkStart>=200,name+": dense spatial four-point stars fill the plume");
                    check(!RoyalPowerupDirector.Busy&&!fx.RocketVisualsActive&&fx.RocketParticleCount==0&&fx.RocketLaunchStandInsActive==0,name+": Rocket, stand-in, trails, smoke and sparkles return to pools");
                    check(CandyShatterService.Instance.ActiveFragments==0,name+": coloured debris returns to its pool");
                    check(fx.GetComponentsInChildren<Transform>(true).Length==objectCount,name+": no pooled VFX object leaks");
                    check(Time.time-started<8f,name+": focused flight and tail complete without a hang");
                }
            }
            finally {fx.Cancel();fx.RocketExhaustLengthCells=originalLength;Time.captureDeltaTime=originalCapture;replayFolder=null;}
        }
        static IEnumerator CheckCreation(LevelManager lm,Square cell,bool vertical,string name,Action<bool,string> check,bool record)
        {
            // Only a four-match fixture: the real detector, destruction and replacement
            // path run, without starting the full board/cascade regression.
            for(int n=0;n<4;n++)lm.GetSquare(cell.col+(vertical?0:n),cell.row-(vertical?n:0)).item.SetColor(5);
            var dragged=lm.lastDraggedItem;var switched=lm.lastSwitchedItem;
            List<Item> group;
            try
            {
                lm.lastDraggedItem=cell.item;lm.lastSwitchedItem=null;
                group=new JellyGarden.Scripts.System.CombineManager().GetCombine().Single(g=>g.Contains(cell.item));
            }
            finally {lm.lastDraggedItem=dragged;lm.lastSwitchedItem=switched;}
            var type=vertical?ItemsTypes.VERTICAL_STRIPPED:ItemsTypes.HORIZONTAL_STRIPPED;
            check(group.Count==4&&cell.item.NextType==type,name+": real Match-4 detector selects the correct idle Rocket");
            var old=cell.item;Vector3 restScale=old.transform.localScale;
            float start=Time.time,firstVisible=-1,lastReveal=-1,peak=0;int frames=0;
            bool inPlace=true,visible=true,fresh=false,smallPop=false,handoff=false;Item created=null;
            float firstScale=0,maxScale=0,minAfterPeak=float.PositiveInfinity,maxStretch=0,maxSquash=0,maxTurn=0,lastAxis=0,lastCross=0,lastTurn=0,handoffStep=0;
            bool turnedBack=false;float firstTurnSign=0;Vector3 lastSize=Vector3.zero;
            var shots=new[]{0f,.05f,.13f,.25f};int shot=0;
            foreach(var item in group){item.royalMatchSize=4;item.DestroyItem(true);}
            while(Time.time-start<1.25f)
            {
                yield return new WaitForEndOfFrame();
                if(record)ReplayFrame();
                if(cell.item&&cell.item!=old&&cell.item.currentType==type)
                {
                    created=cell.item;var anim=created.GetComponent<RoyalRocketPieceAnimator>();
                    var size=created.sprRenderer.bounds.size;
                    if(firstVisible<0)
                    {
                        firstVisible=Time.time;
                        fresh=anim&&anim.Current==RoyalRocketPieceAnimator.Phase.Reveal&&anim.Age==0;
                        firstScale=anim?Mathf.Max(anim.AxisScale,anim.CrossScale):1;
                        smallPop=anim&&firstScale<.45f&&anim.HighlightRenderer.enabled&&Mathf.Abs(anim.Turn)>=10f;
                        firstTurnSign=anim?Mathf.Sign(anim.Turn):0;
                    }
                    visible&=created.sprRenderer.enabled&&created.sprRenderer.color.a>.9f&&created.sprRenderer.sprite==RoyalPieceSkin.For(type,created.color);
                    inPlace&=Vector2.Distance(created.transform.position,cell.transform.position)<.002f&&!created.falling;
                    peak=Mathf.Max(peak,size.x/Mathf.Abs(lm.squareWidth*lm.GameField.lossyScale.x),size.y/Mathf.Abs(lm.squareHeight*lm.GameField.lossyScale.y));
                    frames++;
                    if(anim&&anim.Current==RoyalRocketPieceAnimator.Phase.Reveal)
                    {
                        float uniform=(anim.AxisScale+anim.CrossScale)*.5f;
                        if(uniform>maxScale){maxScale=uniform;minAfterPeak=float.PositiveInfinity;}else minAfterPeak=Mathf.Min(minAfterPeak,uniform);
                        maxStretch=Mathf.Max(maxStretch,anim.AxisScale/anim.CrossScale);maxSquash=Mathf.Max(maxSquash,anim.CrossScale/anim.AxisScale);
                        maxTurn=Mathf.Max(maxTurn,Mathf.Abs(anim.Turn));turnedBack|=Mathf.Sign(anim.Turn)==-firstTurnSign&&Mathf.Abs(anim.Turn)>=3f;
                        if(shot<shots.Length&&anim.Age>=shots[shot]){CaptureRocket(name+"-reveal-"+shot+"-"+(shots[shot]*1000).ToString("000")+"ms");shot++;}
                        lastAxis=anim.AxisScale;lastCross=anim.CrossScale;lastTurn=anim.Turn;lastSize=size;
                    }
                    else if(anim&&lastSize!=Vector3.zero&&!handoff)
                    {
                        // First Idle frame: compare with the last Reveal frame on screen.
                        handoffStep=Mathf.Max(Mathf.Abs(anim.AxisScale-lastAxis),Mathf.Abs(anim.CrossScale-lastCross),Mathf.Abs(size.x-lastSize.x)/lastSize.x,Mathf.Abs(size.y-lastSize.y)/lastSize.y);
                        handoff=handoffStep<.03f&&Mathf.Abs(anim.Turn-lastTurn)<1f&&anim.Age==0;
                        CaptureRocket(name+"-reveal-4-settled");
                    }
                    if(!created.RoyalRocketRevealing){lastReveal=Time.time;break;}
                }
            }
            check(created&&firstVisible-start<=.30f,name+": Rocket appears in-place as Match-4 finishes ("+(firstVisible-start).ToString("F3")+"s including gem break)");
            check(visible&&inPlace&&frames>2,name+": reveal has no missing sprite, falling spawn or next-frame scale reset");
            check(fresh,name+": the new Rocket starts its own Reveal at 0 s, with no stale Idle pose");
            check(smallPop,name+": Rocket appears small ("+firstScale.ToString("F2")+"x) inside a light pop, tilted");
            check(maxStretch>=1.15f&&maxSquash>=1.12f,name+": fast stretch "+maxStretch.ToString("F2")+" and squash "+maxSquash.ToString("F2"));
            check(maxTurn>=8f&&turnedBack,name+": directional flick of "+maxTurn.ToString("F1")+"° with a counter-swing");
            check(maxScale>=1.10f&&minAfterPeak<=.96f,name+": scale overshoots to "+maxScale.ToString("F2")+" and rebounds to "+minAfterPeak.ToString("F2")+" before settling");
            check(lastReveal>0&&lastReveal-firstVisible>=.40f&&lastReveal-firstVisible<=.55f,name+": responsive reveal settles in "+(lastReveal-firstVisible).ToString("F3")+"s");
            check(handoff,name+": Reveal -> Idle has no sprite or scale pop (largest change "+(handoffStep*100).ToString("F1")+"%)");
            check(peak<.96f,name+": playful reveal stays inside its cell (peak "+peak.ToString("F3")+")");
            check(Vector3.Distance(created.transform.localScale,restScale)<.002f,name+": reveal restores the original Item scale");
            var idle=created.sprRenderer.bounds.size;
            float idleFill=Mathf.Max(idle.x/Mathf.Abs(lm.squareWidth*lm.GameField.lossyScale.x),idle.y/Mathf.Abs(lm.squareHeight*lm.GameField.lossyScale.y));
            check(Mathf.Abs(idleFill-.78f)<.015f,name+": stable idle art fills 78% of its actual cell");
        }
        static IEnumerator CheckIdle(LevelManager lm,Item piece,string name,Action<bool,string> check,bool record)
        {
            var anim=piece?piece.GetComponent<RoyalRocketPieceAnimator>():null;
            check(anim&&anim.Current==RoyalRocketPieceAnimator.Phase.Idle,name+": Idle loop starts after the Reveal");
            float cellX=Mathf.Abs(lm.squareWidth*lm.GameField.lossyScale.x),cellY=Mathf.Abs(lm.squareHeight*lm.GameField.lossyScale.y);
            Vector2 centre=piece.square.transform.position;
            float end=Time.time+RoyalRocketPieceAnimator.IdlePeriod+.35f;
            float maxFill=0,maxDrift=0,maxTurn=0,minAxis=9,maxAxis=0,maxSheen=0,maxSparkle=0,maxStep=0,maxTurnStep=0,lastAxis=-1,lastTurn=0,lastPhase=-1;
            bool wrapped=false,sheenShot=false,sparkleShot=false;
            while(Time.time<end)
            {
                yield return new WaitForEndOfFrame();
                if(record)ReplayFrame();
                var b=piece.sprRenderer.bounds;
                maxFill=Mathf.Max(maxFill,b.size.x/cellX,b.size.y/cellY);
                maxDrift=Mathf.Max(maxDrift,Vector2.Distance(b.center,centre));
                maxTurn=Mathf.Max(maxTurn,Mathf.Abs(anim.Turn));
                minAxis=Mathf.Min(minAxis,anim.AxisScale);maxAxis=Mathf.Max(maxAxis,anim.AxisScale);
                maxSheen=Mathf.Max(maxSheen,anim.SheenStrength);maxSparkle=Mathf.Max(maxSparkle,anim.SparkleStrength);
                if(anim.SheenStrength>=.97f&&!sheenShot){sheenShot=true;CaptureRocket(name+"-idle-sheen");}
                if(anim.SparkleStrength>=.97f&&!sparkleShot){sparkleShot=true;CaptureRocket(name+"-idle-sparkle");}
                float phase=anim.Age%RoyalRocketPieceAnimator.IdlePeriod;
                if(lastAxis>=0){maxStep=Mathf.Max(maxStep,Mathf.Abs(anim.AxisScale-lastAxis));maxTurnStep=Mathf.Max(maxTurnStep,Mathf.Abs(anim.Turn-lastTurn));if(phase<lastPhase)wrapped=true;}
                lastAxis=anim.AxisScale;lastTurn=anim.Turn;lastPhase=phase;
            }
            check(wrapped,name+": Idle runs through its "+RoyalRocketPieceAnimator.IdlePeriod+" s loop point");
            check(maxFill<=.84f,name+": Idle stays comfortably inside one cell (max "+maxFill.ToString("F3")+" of the cell)");
            check(maxDrift<.003f,name+": Idle has no position drift ("+maxDrift.ToString("F4")+" world units)");
            check(maxTurn>=1.2f&&maxTurn<=2f,name+": Idle rocks "+maxTurn.ToString("F2")+"° either way");
            check(minAxis>=.999f&&maxAxis<=1.03f&&maxAxis-minAxis>=.015f,name+": Idle breathing stays very small ("+((maxAxis-1)*100).ToString("F1")+"%)");
            check(maxSheen>=.97f&&maxSparkle>=.97f,name+": a highlight sheen crosses the body, then a small sparkle glints at the nose it leaves");
            check(maxStep<.004f&&maxTurnStep<.12f,name+": Idle is smooth through the loop point (largest frame step "+(maxStep*100).ToString("F2")+"%, "+maxTurnStep.ToString("F3")+"°)");
        }
        static string replayFolder;static int replayIndex;
        static void BeginReplay(string name)
        {
            replayFolder="Temp/Rocket-v7-replay/"+name+"/";
            if(Directory.Exists(replayFolder))Directory.Delete(replayFolder,true);
            Directory.CreateDirectory(replayFolder);replayIndex=0;
        }
        static void ReplayFrame()
        {
            if(replayFolder==null)return;
            var texture=ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(replayFolder+(replayIndex++).ToString("D4")+".jpg",texture.EncodeToJPG(95));Object.Destroy(texture);
        }
        static void CaptureRocket(string name)
        {
            var texture=ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Folder+name+".png",texture.EncodeToPNG());Object.Destroy(texture);
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

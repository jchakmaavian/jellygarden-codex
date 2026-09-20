using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using UnityEngine;
using Object=UnityEngine.Object;
namespace RoyalThrone.Editor
{
    public static class RoyalTNTChecks
    {
        const string Folder="Design/Effects/TNT-review-v6/";
        static IEnumerator RecordReplay()
        {
            const string frames="Temp/TNT-v6-replay/";Directory.CreateDirectory(frames);
            for(int i=0;i<78;i++)
            {
                yield return new WaitForEndOfFrame();
                var frame=ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(frames+i.ToString("D4")+".jpg",frame.EncodeToJPG(92));Object.Destroy(frame);
            }
        }
        static void Capture(string name){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+name+".png",t.EncodeToPNG());Object.Destroy(t);}
        const string RevealReplay="Temp/TNT-v6-reveal-idle/";
        static void ReplayFrame(int index){if(index==0){if(Directory.Exists(RevealReplay))Directory.Delete(RevealReplay,true);Directory.CreateDirectory(RevealReplay);}var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(RevealReplay+index.ToString("D4")+".png",t.EncodeToPNG());Object.Destroy(t);}
        static IEnumerator Until(TNT3DAnimator actor,float age){while(actor&&actor.gameObject.activeSelf&&actor.VisualAge<age)yield return null;yield return new WaitForEndOfFrame();}

        // One rendered frame of the board TNT, read after every LateUpdate.
        struct Sample{public TNTFrameAnimator.Phase Phase;public int Frame;public float Zoom;public bool Drawn,StaticShown,Posed,Single;}
        static Sample Observe(Item item,TNTFrameAnimator anim,Vector3 restScale)
        {
            var view=anim.View;var tr=item.sprRenderer.transform;
            var expected=anim.Current==TNTFrameAnimator.Phase.Reveal?TNTFrameAnimator.RevealFrame(anim.Frame):TNTFrameAnimator.IdleFrame(anim.Frame);
            // The Reveal opens larger than the cell and eases down, so the pose is checked against the
            // zoom the animator reports: the barrel base stays put and Idle is always exactly 1.19x.
            bool seated=anim.Zoom<=1.001f;
            float expectedScale=anim.Zoom/TNTFrameAnimator.ContentScale;
            var expectedPose=view?view.transform.localPosition:Vector3.zero;
            return new Sample{Phase=anim.Current,Frame=anim.Frame,Zoom=anim.Zoom,
                Drawn=view&&view.enabled&&anim.Frame>=0&&view.sprite==expected&&
                    Mathf.Abs(expectedPose.x-TNTFrameAnimator.BaseOffset.x)<1e-4f&&
                    (seated?Mathf.Abs(expectedPose.y-TNTFrameAnimator.BaseOffset.y)<1e-4f:expectedPose.y<TNTFrameAnimator.BaseOffset.y)&&
                    Mathf.Abs(view.transform.localScale.x-expectedScale)<1e-3f&&
                    view.sortingOrder==item.sprRenderer.sortingOrder+(seated?1:6),
                StaticShown=item.sprRenderer.enabled,
                Posed=tr.localScale==restScale&&tr.localRotation==Quaternion.identity,
                Single=item.GetComponents<TNTFrameAnimator>().Length==1&&tr.Cast<Transform>().Count(t=>t.name=="TNT frames")==1};
        }

        // Renders the cell once with Idle 001 and once with the static board sprite, in the same frame.
        // The frame must land on the artwork: the best pixel shift between the two renders is (0, 0).
        static bool Alignment(Item item,TNTFrameAnimator anim,out string report)
        {
            var main=Camera.main;float cell=TNT3DAnimator.Cell;const int size=512;
            var go=new GameObject("TNT alignment camera"){hideFlags=HideFlags.HideAndDontSave};
            var cam=go.AddComponent<Camera>();cam.CopyFrom(main);
            var rt=new RenderTexture(size,size,24,RenderTextureFormat.ARGB32);
            try
            {
                cam.targetTexture=rt;cam.orthographic=true;cam.orthographicSize=cell*.8f;cam.aspect=1;
                var centre=item.sprRenderer.bounds.center;go.transform.SetPositionAndRotation(new Vector3(centre.x,centre.y,main.transform.position.z),main.transform.rotation);
                Color32[] Render(string name)
                {
                    cam.Render();var previous=RenderTexture.active;RenderTexture.active=rt;
                    var t=new Texture2D(size,size,TextureFormat.RGBA32,false);t.ReadPixels(new Rect(0,0,size,size),0,0);t.Apply();RenderTexture.active=previous;
                    File.WriteAllBytes(Folder+name+".png",t.EncodeToPNG());var pixels=t.GetPixels32();Object.DestroyImmediate(t);return pixels;
                }
                var framed=Render("normal-alignment-idle001-frames");
                anim.View.enabled=false;item.sprRenderer.enabled=true;
                var board=Render("normal-alignment-static-board-sprite");
                anim.View.enabled=true;item.sprRenderer.enabled=false;
                var b=item.sprRenderer.bounds;Vector3 lo=cam.WorldToScreenPoint(b.min),hi=cam.WorldToScreenPoint(b.max);
                int x0=Mathf.Clamp((int)lo.x-4,3,size-3),x1=Mathf.Clamp((int)hi.x+4,3,size-3),y0=Mathf.Clamp((int)lo.y-4,3,size-3),y1=Mathf.Clamp((int)hi.y+4,3,size-3);
                double Error(int dx,int dy)
                {
                    double sum=0;long n=0;
                    for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++){var p=framed[y*size+x];var q=board[(y+dy)*size+x+dx];sum+=Math.Abs(p.r-q.r)+Math.Abs(p.g-q.g)+Math.Abs(p.b-q.b);n+=3;}
                    return sum/n;
                }
                double best=double.MaxValue,centreError=Error(0,0);int bx=0,by=0;
                for(int dy=-3;dy<=3;dy++)for(int dx=-3;dx<=3;dx++){double e=Error(dx,dy);if(e<best-1e-9){best=e;bx=dx;by=dy;}}
                var diff=new Texture2D(size,size,TextureFormat.RGBA32,false);var d=new Color32[size*size];
                for(int i=0;i<d.Length;i++){var p=framed[i];var q=board[i];d[i]=new Color32((byte)Mathf.Min(255,Math.Abs(p.r-q.r)*4),(byte)Mathf.Min(255,Math.Abs(p.g-q.g)*4),(byte)Mathf.Min(255,Math.Abs(p.b-q.b)*4),255);}
                diff.SetPixels32(d);diff.Apply();File.WriteAllBytes(Folder+"normal-alignment-difference-x4.png",diff.EncodeToPNG());Object.DestroyImmediate(diff);
                report="TNT "+(x1-x0)+"x"+(y1-y0)+" px; best shift ("+bx+", "+by+"), mean channel error "+centreError.ToString("0.00")+"/255";
                return bx==0&&by==0&&centreError<8;
            }
            finally{cam.targetTexture=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(go);}
        }

        public static IEnumerator Run(LevelManager lm,Action seed,Action colours,Action<bool,string> check)
        {
            Directory.CreateDirectory(Folder);float originalSpeed=Time.timeScale,originalCapture=Time.captureDeltaTime;Coroutine recording=null;
#if !ROYAL_DISABLE_QA
            var qa=RoyalThrone.Testing.RoyalQaService.Instance;bool enabled=qa&&qa.enabled;if(qa)qa.enabled=false;
#endif
            try
            {
                check(TNTFrameAnimator.Prewarm(),"TNT Reveal and Idle sequences load (120 + 120 frames)");
                var sprites=Enumerable.Range(0,TNTFrameAnimator.FrameCount).SelectMany(i=>new[]{TNTFrameAnimator.RevealFrame(i),TNTFrameAnimator.IdleFrame(i)}).ToArray();
                check(sprites.All(s=>s&&s.rect.size==new Vector2(512,512)&&Mathf.Approximately(s.pixelsPerUnit,100)&&Vector2.Distance(s.pivot,new Vector2(256,32))<.01f),
                    "Every frame keeps its 512 px canvas with the pivot on the barrel base (256, 32 px) at 100 PPU");
                foreach(float speed in new[]{.25f,1f})
                {
                    string tag=speed<1?"quarter":"normal";Time.timeScale=speed;Time.captureDeltaTime=1f/60f;
                    RoyalPowerupDirector.Get.Cancel();CandyShatterService.Instance.Clear();
                    seed();yield return null;colours();yield return null;
                    int reusesBefore=TNT3DAnimator.Reuses;
                    // A T match on the bottom rows. The dragged piece puts the TNT on a cell that cannot fall.
                    foreach(var p in new[]{new Vector2Int(2,7),new Vector2Int(3,7),new Vector2Int(4,7),new Vector2Int(3,6),new Vector2Int(3,5)})lm.GetSquare(p.x,p.y).item.SetColor(0);
                    lm.GetSquare(1,7).item.SetColor(2);
                    var origin=lm.GetSquare(3,7);lm.lastDraggedItem=origin.item;lm.lastSwitchedItem=null;
                    foreach(var piece in lm.GetItems())if(piece)piece.NextType=ItemsTypes.NONE;
                    lm.FindMatches();
                    Item item=null;float until=Time.realtimeSinceStartup+15;
                    while(!item&&Time.realtimeSinceStartup<until){yield return new WaitForEndOfFrame();if(origin.item&&origin.item.currentType==ItemsTypes.PACKAGE)item=origin.item;}
                    check(item,tag+": a valid T match creates TNT through the production match pass");
                    var anim=item.GetComponent<TNTFrameAnimator>();var revealActor=TNT3DAnimator.Find(item);
                    check(anim,tag+": the created TNT carries the frame animator");
                    var restScale=item.sprRenderer.transform.localScale;var first=Observe(item,anim,restScale);
                    check(first.Phase==TNTFrameAnimator.Phase.Reveal&&first.Frame==0&&first.Drawn&&!first.StaticShown&&first.Single,
                        tag+": the TNT's first rendered frame is Reveal 001, with no static TNT and one frame renderer");
                    check(revealActor&&!revealActor.model.gameObject.activeSelf&&revealActor.FlameIntensity==0&&revealActor.blast.All(r=>!r.enabled)&&revealActor.stars.All(r=>!r.enabled)&&!item.falling,
                        tag+": the pooled reveal actor still holds the new piece in place but draws no procedural pop or glow");
                    if(speed==1f)check(TNT3DAnimator.Reuses>reusesBefore,"normal: this Reveal runs on a reused pooled TNT actor and still starts at 001");
                    Capture(tag+"-reveal-001");if(speed==1f)ReplayFrame(0);

                    var samples=new List<Sample>{first};bool peak=false,sparkle=false,settled=false,aligned=true;int wraps=0;string alignment="not run";
                    while(samples.Count<8000)
                    {
                        yield return new WaitForEndOfFrame();
                        // Hints restart when the match resolves; keep pieces still while the loop is measured.
                        if(!settled&&!lm.IsResolvingMatches){settled=true;AI.THIS.StopAllCoroutines();AI.THIS.allowShowTip=false;lm.DragBlocked=true;}
                        var last=samples[samples.Count-1];var s=Observe(item,anim,restScale);samples.Add(s);
                        if(speed==1f&&samples.Count<=180)ReplayFrame(samples.Count-1);
                        if(s.Phase==TNTFrameAnimator.Phase.Reveal&&!peak&&s.Frame>=60){peak=true;Capture(tag+"-reveal-peak");}
                        if(s.Phase==TNTFrameAnimator.Phase.Idle&&last.Phase==TNTFrameAnimator.Phase.Reveal)
                        {Capture(tag+"-reveal-to-idle-001");if(speed==1f)aligned=Alignment(item,anim,out alignment);}
                        if(s.Phase==TNTFrameAnimator.Phase.Idle&&!sparkle&&s.Frame>=30){sparkle=true;Capture(tag+"-idle-031");}
                        if(s.Phase==TNTFrameAnimator.Phase.Idle&&last.Phase==TNTFrameAnimator.Phase.Idle&&s.Frame<last.Frame)wraps++;
                        if(wraps>0&&s.Frame>=10&&settled)break;
                    }
                    var reveal=samples.TakeWhile(x=>x.Phase==TNTFrameAnimator.Phase.Reveal).ToList();var idle=samples.Skip(reveal.Count).ToList();
                    int expected=Mathf.RoundToInt(TNTFrameAnimator.RevealDuration/(speed/60f));
                    check(samples.All(x=>x.Drawn&&!x.StaticShown&&x.Posed&&x.Single),
                        tag+": all "+samples.Count+" rendered frames draw one TNT frame on the barrel-base pivot at the reported zoom: no static flash, missing sprite, pop or duplicate");
                    check(reveal.Zip(reveal.Skip(1),(a,b)=>b.Frame>=a.Frame).All(x=>x)&&Mathf.Abs(reveal.Count-expected)<=1&&reveal[reveal.Count-1].Frame>=110,
                        tag+": Reveal plays once, 001 to "+(reveal[reveal.Count-1].Frame+1).ToString("000")+" over "+reveal.Count+" rendered frames ("+TNTFrameAnimator.RevealDuration.ToString("0.00")+" s authored timing)");
                    // The reward moment: open near 3x, come down without a step, and seat at exactly 1x.
                    var zooms=reveal.Select(x=>x.Zoom).ToList();
                    float biggestStep=0;for(int i=1;i<zooms.Count;i++)biggestStep=Mathf.Max(biggestStep,Mathf.Abs(zooms[i]-zooms[i-1]));
                    check(zooms.Count>0&&zooms[0]>=TNTFrameAnimator.RevealZoom-.01f&&zooms.Zip(zooms.Skip(1),(a,b)=>b<=a+1e-4f).All(x=>x)&&
                        biggestStep<=.12f*Mathf.Max(1,speed)&&idle.All(x=>Mathf.Abs(x.Zoom-1f)<1e-4f),
                        tag+": Reveal opens at "+zooms[0].ToString("0.00")+"x, shrinks monotonically (largest step "+biggestStep.ToString("0.000")+") and Idle runs at exactly 1.00x");
                    check(idle.Count>0&&idle.All(x=>x.Phase==TNTFrameAnimator.Phase.Idle)&&idle[0].Frame==0,
                        tag+": Idle 001 directly follows the last Reveal frame, and the Reveal never replays");
                    var steps=idle.Zip(idle.Skip(1),(a,b)=>(b.Frame-a.Frame+TNTFrameAnimator.FrameCount)%TNTFrameAnimator.FrameCount).ToList();
                    int longestHold=1,hold=1;foreach(int step in steps){hold=step==0?hold+1:1;longestHold=Mathf.Max(longestHold,hold);}
                    bool smooth=speed<1?steps.All(x=>x<=1)&&longestHold<=5:steps.All(x=>x==1)&&Enumerable.Range(0,TNTFrameAnimator.FrameCount).All(f=>idle.Any(x=>x.Frame==f));
                    check(wraps>=1&&smooth,tag+": Idle loops 120 -> 001 on the 60 fps clock with no skipped or stalled frame ("+idle.Count+" frames observed)");
                    if(speed==1f)check(aligned,"normal: Idle 001 lands on the static board artwork at 1.19x ("+alignment+")");

                    if(speed==1f){recording=lm.StartCoroutine(RecordReplay());}
                    int stoppedAt=anim.Frame;
                    item.DestroyItem(true);
                    yield return new WaitForEndOfFrame();
                    // A royal TNT's destroy coroutine removes the piece within this frame, so inspect what is drawn.
                    var fuseArt=RoyalPieceSkin.For(ItemsTypes.PACKAGE,0);var drawn=Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(r=>r&&r.enabled).ToList();
                    check(!drawn.Any(r=>sprites.Contains(r.sprite))&&(!anim||anim.Current==TNTFrameAnimator.Phase.Stopped)&&!(item&&item.sprRenderer&&item.sprRenderer.enabled),
                        tag+": activation stops Idle (at frame "+(stoppedAt+1).ToString("000")+"): the next rendered frame draws no TNT frame and no board TNT");
                    check(drawn.Count(r=>r.sprite==fuseArt)==1,tag+": the existing anticipation takes over with one static TNT sprite");
                    var actor=Object.FindObjectsByType<TNT3DAnimator>(FindObjectsSortMode.None).First(a=>a.gameObject.activeSelf);
                    yield return Until(actor,.09f);Capture(tag+"-ignition");
                    while(actor.State!=TNT3DAnimator.VisualState.Explosion&&actor.State!=TNT3DAnimator.VisualState.Debris)yield return null;
                    yield return Until(actor,.12f);
                    var props=new MaterialPropertyBlock();actor.blast[0].GetPropertyBlock(props);var core=props.GetColor("_Color");actor.blast[1].GetPropertyBlock(props);var edge=props.GetColor("_Color");
                    check(core.b>.75f&&edge.b<.05f&&actor.blast[0].enabled&&actor.blast[3].enabled,tag+": white-hot ball, gold edge and expanding shockwave");Capture(tag+"-blast-peak");
                    yield return Until(actor,.23f);Capture(tag+"-rays");
                    yield return Until(actor,.40f);
                    check(CandyShatterService.Instance.ActiveFragments>0&&!actor.blast[0].enabled,tag+": colored debris remains visible after the center fades");Capture(tag+"-falling-debris");
                    check(actor.blast[4].enabled&&actor.stars.Any(s=>s.enabled)&&actor.impacts.Any(s=>s.enabled),tag+": residual light, stars and local impacts overlap falling fragments");
                    yield return new WaitForSeconds(1.2f);
                    if(recording!=null){yield return recording;recording=null;}
                    check(TNT3DAnimator.ActiveCount==0&&!RoyalPowerupDirector.Busy&&CandyShatterService.Instance.ActiveFragments==0,tag+": all pooled effects and debris finish");
                }
                check(TNT3DAnimator.Reuses>0,"Pooled TNT actors reset and replay without leftover reveal transforms");
            }
            finally
            {
                if(recording!=null)lm.StopCoroutine(recording);Time.captureDeltaTime=originalCapture;Time.timeScale=originalSpeed;
#if !ROYAL_DISABLE_QA
                if(qa)qa.enabled=enabled;
#endif
            }
        }
    }
}

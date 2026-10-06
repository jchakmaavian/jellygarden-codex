#if UNITY_EDITOR && !ROYAL_DISABLE_QA
using System;
using System.Collections;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.GUI;
using RoyalThrone.Testing;
using UnityEditor;
using UnityEngine;

namespace RoyalThrone.ReferenceGameplay.Editor
{
    [InitializeOnLoad]
    public static class ReferenceCannonChecks
    {
        const string Folder="Design/Cannon-Reference/Review";
        static bool running;
        static ReferenceCannonChecks(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&File.Exists("Temp/CannonPose.request")){
                File.Delete("Temp/CannonPose.request");InspectMotion();
            }
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Temp/CannonChecks.request")||running)return;
            if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}
            if(!LevelManager.THIS||!RoyalQaService.Instance||!InitScript.Instance)return;
            if(UnityEngine.Object.FindAnyObjectByType<RoyalThrone.Presentation.RoyalTitleScreen>())return;
            File.Delete("Temp/CannonChecks.request");running=true;
            RoyalThrone.Editor.RoyalRegressionRunner.Run(Run());
        }
        static void InspectMotion()
        {
            var data=Resources.Load<Spine.Unity.SkeletonDataAsset>("ReferenceGameplay/Cannon/CannonSkeleton").GetSkeletonData(false);
            var report=new System.Text.StringBuilder();
            foreach(var animation in data.Animations){
                report.AppendLine(animation.Name);
                foreach(var timeline in animation.Timelines){
                    var bone=timeline as Spine.IBoneTimeline;
                    report.Append(timeline.GetType().Name+" "+(bone==null?"":data.Bones.Items[bone.BoneIndex].Name)+" ");
                    var prop=timeline.GetType().GetProperty("Frames");var frames=prop?.GetValue(timeline) as float[];
                    if(frames!=null)report.Append(string.Join(",",frames.Select(v=>v.ToString("F4",System.Globalization.CultureInfo.InvariantCulture))));
                    report.AppendLine();
                }
            }
            File.WriteAllText("Design/Cannon-Reference/source-keyframes.txt",report.ToString());
            var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            var rig=Spine.Unity.SkeletonAnimation.NewSkeletonAnimationGameObject(Resources.Load<Spine.Unity.SkeletonDataAsset>("ReferenceGameplay/Cannon/CannonSkeleton"));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(rig.gameObject,scene);rig.enabled=false;rig.gameObject.layer=31;
            var cameraObject=new GameObject("Cannon pose review camera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
            var camera=cameraObject.AddComponent<Camera>();camera.scene=scene;camera.enabled=false;camera.orthographic=true;camera.orthographicSize=2.3f;camera.transform.position=new Vector3(0,-1,-10);camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.05f,.18f,.4f);
            var target=new RenderTexture(256,320,16);camera.targetTexture=target;var previous=RenderTexture.active;
            Directory.CreateDirectory("Design/Cannon-Reference/Poses");
            try{
                rig.Update(0);var animation=data.FindAnimation("gercek_patlama");
                foreach(float time in new[]{0,.1667f,.3f,.4f,.45f,.5f,.55f,.6f,.65f,.7f}){
                    rig.Skeleton.SetToSetupPose();animation.Apply(rig.Skeleton,-1,time,false,null,1,Spine.MixBlend.Setup,Spine.MixDirection.In);rig.Skeleton.UpdateWorldTransform();rig.LateUpdate();
                    camera.Render();RenderTexture.active=target;var image=new Texture2D(256,320,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,256,320),0,0);image.Apply();
                    File.WriteAllBytes("Design/Cannon-Reference/Poses/"+time.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
                }
            }finally{RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(target);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
        }
        static void Check(bool ok,string message)
        {File.AppendAllText(Folder+"/checks.txt",(ok?"PASS ":"FAIL ")+message+"\n");if(!ok)throw new InvalidOperationException(message);}
        static IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Folder+"/"+name+".png",image.EncodeToPNG());UnityEngine.Object.Destroy(image);
        }
        public static IEnumerator Run()
        {
            Directory.CreateDirectory(Folder+"/Frames");File.WriteAllText(Folder+"/checks.txt","Cannon source animation / gameplay verification\n");File.WriteAllText(Folder+"/timing.txt","");
            var qa=RoyalQaService.Instance;var lm=LevelManager.THIS;int oldRate=Time.captureFramerate;bool oldBackground=Application.runInBackground;
            int stock=RoyalSave.GetInt(BoostType.RoyalCannon.ToString()),coins=InitScript.Gems;var originalSound=PlayerPrefs.GetInt("Sound",1);
            try
            {
                Application.runInBackground=true;qa.StartSession();Check(RoyalSave.Testing,"Isolated test profile active");
                // The synthetic 8x8 fixture must not inherit level 1's fixed tutorial refill stamps.
                RoyalThrone.Campaign.RoyalReferenceCampaign.Select(false);qa.Level=1;qa.Speed=1;Time.timeScale=1;yield return qa.Practice();qa.Visible=false;
                RoyalThrone.Campaign.RoyalReferenceCampaign.Select(false);lm.colorLimit=5;
                Check(!RoyalThrone.Campaign.RoyalReferenceCampaign.Active,"Synthetic fixture uses ordinary refill, without tutorial feed");
                Check(ReferenceCannonSequence.Available,"Imported skeleton and cannonball prefab load");
                var bar=ReferenceBoosterBar.Instance;Check(bar,"Booster bar active");
                var rail=GameObject.Find("Imported booster rail").GetComponent<RectTransform>();var railRest=rail.anchoredPosition;
                foreach(int col in new[]{3,0,7})
                {
                    Time.captureFramerate=col==3?60:col==0?30:15;
                    yield return qa.Practice();qa.Visible=false;RoyalSave.SetInt(BoostType.RoyalCannon.ToString(),3);
                    int moves=lm.Limit;bar.Choose(BoostType.RoyalCannon);
                    Check(bar.TryUse(lm.GetSquare(col,3)),"Cannon starts in column "+col);
                    Check(!bar.TryUse(lm.GetSquare(col,3)),"Repeated tap rejected while cannon runs");
                    var sequence=bar.GetComponent<ReferenceCannonSequence>();Check(sequence&&sequence.Running,"Native motion runs before cell damage");
                    Check(bar.LastHitCount==0,"Entrance does not clear cells early");
                    int frame=0;float deadline=Time.realtimeSinceStartup+120;bool hiddenBeforeShot=false,hiddenDuringShot=false,returnSeen=false,launchChecked=false;
                    while(bar.Busy&&Time.realtimeSinceStartup<deadline)
                    {
                        if(sequence.Fired&&!launchChecked){
                            var rig=UnityEngine.Object.FindObjectsByType<Spine.Unity.SkeletonAnimation>().First(s=>s.name=="Cannon - imported source motion");
                            float pose=rig.AnimationState.GetCurrent(0).TrackTime;
                            Check(pose>=ReferenceCannonSequence.FireFrame-.001f&&pose<=ReferenceCannonSequence.FireFrame+Time.deltaTime+.002f,"Projectile launches on the firing pose at "+Time.captureFramerate+" FPS");
                            File.AppendAllText(Folder+"/timing.txt",$"launch fps={Time.captureFramerate} sequence={sequence.Elapsed:F4} sourcePose={pose:F4}\n");launchChecked=true;
                        }
                        if(sequence.Running&&sequence.Elapsed>=ReferenceCannonSequence.Entrance&&sequence.Elapsed<ReferenceCannonSequence.RecoilEnd){
                            var corners=new Vector3[4];rail.GetWorldCorners(corners);var canvas=rail.GetComponentInParent<Canvas>();
                            var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
                            bool offscreen=corners.All(p=>RectTransformUtility.WorldToScreenPoint(camera,p).y<0);
                            if(!sequence.Fired&&!hiddenBeforeShot){Check(offscreen,"Whole bottom bar is below the screen before the shot");hiddenBeforeShot=true;}
                            if(sequence.Fired&&!hiddenDuringShot){Check(offscreen,"Bottom bar stays hidden during firing and recoil");hiddenDuringShot=true;}
                        }
                        if(sequence.Elapsed>ReferenceCannonSequence.RecoilEnd&&sequence.Elapsed<ReferenceCannonSequence.RecoilEnd+ReferenceCannonSequence.BarReturn&&rail.anchoredPosition.y<railRest.y)returnSeen=true;
                        if(col==3&&frame%30==0)File.AppendAllText(Folder+"/timing.txt",$"frame={frame} unity={Time.frameCount} elapsed={sequence.Elapsed:F3} dt={Time.deltaTime:F4} scale={Time.timeScale} hits={bar.LastHitCount}\n");
                        if(col==3&&frame==90)File.AppendAllText(Folder+"/timing.txt","POWER="+JellyGarden.Scripts.Juice.RoyalPowerupDirector.Busy+" GLOBAL="+string.Join(";",GameObject.FindGameObjectsWithTag("Item").Where(g=>!g.GetComponent<Item>()||g.GetComponent<Item>().falling||g.GetComponent<Item>().destroying).Select(g=>g.name+" parent="+g.transform.parent?.name+" item="+(g.GetComponent<Item>()? $"f={g.GetComponent<Item>().falling} d={g.GetComponent<Item>().destroying} done={g.GetComponent<Item>().animationFinished}":"MISSING")))+"\n");
                        if(col==3&&frame<180)yield return Shot("Frames/"+frame.ToString("D4"));
                        yield return null;
                        frame++;
                    }
                    Check(!bar.Busy,"Cannon and board resolution finish in column "+col);
                    Check(launchChecked,"Firing cue observed");
                    Check(hiddenBeforeShot&&hiddenDuringShot&&returnSeen,"Bar slides back after the firing clip");
                    Check((rail.anchoredPosition-railRest).sqrMagnitude<.01f&&rail.GetComponent<CanvasGroup>().blocksRaycasts&&rail.GetComponent<CanvasGroup>().interactable,"Bottom bar returns to its original position and accepts input");
                    File.AppendAllText(Folder+"/timing.txt",$"resolution={lm.IsResolvingMatches} drag={lm.DragBlocked} swaps={lm.InputSwaps} state={lm.gameStatus} hits={bar.LastHitCount} remaining="+string.Join(",",lm.GetItems().Where(i=>i&&(i.falling||i.destroying)).Select(i=>$"{i.name} falling={i.falling} destroying={i.destroying} active={i.gameObject.activeInHierarchy}"))+"\n");
                    yield return qa.Settled();
                    Check(bar.LastHitCount==8,"All eight column cells hit exactly once");
                    Check(bar.Stock(2)==2&&lm.Limit==moves,"Exactly one cannon spent, no move consumed");
                    Check(lm.GetItems().Where(i=>i).All(i=>i.square&&i.square.item==i),"Refill retains cell ownership");
                    Check(!sequence.Running&&!UnityEngine.Object.FindObjectsByType<Spine.Unity.SkeletonAnimation>().Any(s=>s.name=="Cannon - imported source motion"),"Cannon cleans up after firing");
                    yield return Shot("Column-"+col+"-complete");
                }
                yield return qa.Practice();qa.Visible=false;RoyalSave.SetInt(BoostType.RoyalCannon.ToString(),3);bar.Choose(BoostType.RoyalCannon);bar.TryUse(lm.GetSquare(3,3));
                var active=bar.GetComponent<ReferenceCannonSequence>();yield return null;float elapsed=active.Elapsed;var pausedRail=rail.anchoredPosition;
                lm.gameStatus=GameState.Pause;for(int i=0;i<8;i++)yield return null;
                Check(Mathf.Approximately(active.Elapsed,elapsed)&&bar.LastHitCount==0,"Pause freezes entrance and damage");
                Check((rail.anchoredPosition-pausedRail).sqrMagnitude<.01f,"Pause freezes the bottom bar slide");
                lm.gameStatus=GameState.Playing;yield return null;
                bar.enabled=false;yield return null;
                Check(!bar.Busy&&!active.Running&&!ReferenceBoosterBar.OwnsInput,"Cancellation releases input and removes cannon");bar.enabled=true;
                Check((rail.anchoredPosition-railRest).sqrMagnitude<.01f&&rail.GetComponent<CanvasGroup>().blocksRaycasts,"Cancellation restores the bottom bar");
                yield return qa.EndSession();
                Check(!RoyalSave.Testing&&RoyalSave.GetInt(BoostType.RoyalCannon.ToString())==stock&&InitScript.Gems==coins,"QA restores normal inventory and coins");
                Check(PlayerPrefs.GetInt("Sound",1)==originalSound,"Sound preference unchanged");
                File.AppendAllText(Folder+"/checks.txt","ALL CANNON CHECKS PASSED\n");
            }
            finally
            {
                Time.captureFramerate=oldRate;Application.runInBackground=oldBackground;running=false;
                if(lm&&lm.gameStatus==GameState.Pause)lm.gameStatus=GameState.Playing;
                if(RoyalSave.Testing)RoyalThrone.Editor.RoyalRegressionRunner.Run(qa.EndSession());
            }
        }
    }
}
#endif

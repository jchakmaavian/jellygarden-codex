using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using RoyalThrone.Presentation.AreaMotion;

[InitializeOnLoad]
public static class RoyalAreaMotionVerification
{
    const string RunKey="RoyalAreaMotionVerification.Pending";
    static RoyalAreaMotionVerification(){if(SessionState.GetBool(RunKey,false))EditorApplication.update+=BeginWhenPlaying;}
    static void BeginWhenPlaying(){if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;EditorApplication.update-=BeginWhenPlaying;SessionState.SetBool(RunKey,false);RunChecks();}
    public static void Run(){SessionState.SetBool(RunKey,true);EditorApplication.update-=BeginWhenPlaying;EditorApplication.update+=BeginWhenPlaying;EditorApplication.EnterPlaymode();}
    static readonly List<string> lines=new List<string>();
    static readonly List<string> errors=new List<string>();
    static readonly List<string> editorIssues=new List<string>();
    static void Check(bool ok,string message){lines.Add((ok?"PASS: ":"FAIL: ")+message);if(!ok)throw new Exception(message);}
    static void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){if(stack.Contains("UnityEditor.Search."))editorIssues.Add(text+"\n"+stack);else errors.Add(text);}}
    static string Signature(RoyalAreaMotionPlayer player)
    {
        double sum=0;int count=0;
        foreach(var r in player.StageCamera.transform.parent.GetComponentsInChildren<MeshRenderer>()){
            if(!r.enabled)continue;count++;var f=r.GetComponent<MeshFilter>();if(!f||!f.sharedMesh)continue;
            var t=r.transform;sum+=t.localPosition.x*7+t.localPosition.y*13+t.localScale.x*17+t.localScale.y*19;
            var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);var color=block.GetColor("_Color");sum+=color.r*23+color.g*29+color.b*31+color.a*37;
            foreach(var v in f.sharedMesh.vertices)sum+=v.x*.031+v.y*.017;
        }
        return count+":"+sum.ToString("F5",System.Globalization.CultureInfo.InvariantCulture);
    }
    static void Shot(RoyalAreaMotionPlayer p,string name,float time)
    {
        p.RenderForReview(time);var rt=p.StageCamera.targetTexture;var old=RenderTexture.active;RenderTexture.active=rt;
        var im=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);im.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);im.Apply();File.WriteAllBytes("Results/"+name+".png",im.EncodeToPNG());RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(im);
    }
    static void RunChecks()
    {
        Application.logMessageReceived+=Log;
        try{
            Directory.CreateDirectory("Results");
            var shader=Resources.Load<Shader>(RoyalAreaMotionPlayer.Folder+"Shaders/RoyalAreaLayer");Check(shader&&shader.isSupported,"Area shader supported");
            var audio=Resources.LoadAll<AudioClip>(RoyalAreaMotionPlayer.Folder+"Audio");Check(audio.Length==27&&audio.All(a=>a.length>0),"27 area sound assets decode");
            for(int area=2;area<=3;area++){int oldCount=area==2?6:7,newCount=area==2?7:9;int[] oldCosts=area==2?new[]{1,2,2,2,2,3}:new[]{2,2,2,2,2,2,3};int[] newCosts=area==2?new[]{1,2,1,2,2,2,2}:new[]{1,2,1,2,2,2,2,1,2};for(int m=0;m<(1<<oldCount);m++){int count=0,paid=0;for(int i=0;i<oldCount;i++)if((m&(1<<i))!=0){count++;paid+=oldCosts[i];}int expected=count==oldCount?(1<<newCount)-1:(1<<count)-1;Check(AreaProgressMigration.Map(area,m)==expected,"Save migration area "+area+" mask "+m);int remaining=0;for(int i=0;i<newCount;i++)if((expected&(1<<i))==0)remaining+=newCosts[i];Check(paid+Mathf.Max(0,remaining-AreaProgressMigration.Credit(area,m))==oldCosts.Sum(),"Migration never charges existing stars twice "+area+":"+m);}}
            var root=new GameObject("Verification canvas",typeof(RectTransform),typeof(Canvas));root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var frame=new GameObject("Area viewport",typeof(RectTransform));frame.transform.SetParent(root.transform,false);var rect=frame.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(720,1280);
            var player=frame.AddComponent<RoyalAreaMotionPlayer>();player.SoundEnabled=false;
            new GameObject("Verification audio listener",typeof(AudioListener));
            for(int area=1;area<=3;area++){
                Check(player.Load(area,0),"Load area "+area);Canvas.ForceUpdateCanvases();Check(player.TaskCount==new[]{6,7,9}[area-1],"Area "+area+" task count");
                Shot(player,"area"+area+"-before",0);
                for(int task=0;task<player.TaskCount;task++){
                    player.SeekForReview(task,.03f);player.RenderForReview(0);string first=Signature(player);
                    player.SeekForReview(task,player.Duration(task)*.45f);player.RenderForReview(.5f);string mid=Signature(player);
                    Shot(player,"area"+area+"-task"+(task+1)+"-mid",.5f);
                    player.SeekForReview(task,player.Duration(task));player.RenderForReview(1);string last=Signature(player);
                    Check(first!=mid||mid!=last,"Area "+area+" task "+(task+1)+" moves through distinct native poses");
                    foreach(var mf in player.StageCamera.transform.parent.GetComponentsInChildren<MeshFilter>())if(mf.sharedMesh)foreach(var v in mf.sharedMesh.vertices)CheckFinite(v);
                }
                player.SetProgress((1<<player.TaskCount)-1);
                foreach(var ps in player.StageCamera.transform.parent.GetComponentsInChildren<ParticleSystem>())ps.Simulate(2,true,true,false);
                Shot(player,"area"+area+"-restored",3);
                if(area==1){Check(player.ActiveParticleCount>20,"Enhanced fountain emits real particles");Shot(player,"area1-water-early",.5f);Shot(player,"area1-water-later",3.5f);Check(!File.ReadAllBytes("Results/area1-water-early.png").SequenceEqual(File.ReadAllBytes("Results/area1-water-later.png")),"Water and flags animate across time");}
                player.enabled=false;Check(!player.StageCamera.gameObject.activeInHierarchy,"Hidden area stops its camera");player.enabled=true;Check(player.StageCamera.gameObject.activeInHierarchy,"Area resumes after visibility change");
            }
            Check(ShaderUtil.GetShaderMessages(shader).All(m=>m.severity!=UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error),"No shader compiler errors");
            Check(errors.Count==0,"No runtime errors: "+string.Join(" | ",errors.Take(4)));
            player.StartCoroutine(Lifecycle(player));
        }catch(Exception e){File.WriteAllLines("Results/checks.txt",lines);File.WriteAllText("Results/failure.txt",e.ToString()+"\n"+string.Join("\n",errors));Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static IEnumerator Lifecycle(RoyalAreaMotionPlayer p)
    {
        p.Load(1,63);p.SoundEnabled=true;yield return null;yield return null;
        var sounds=p.StageCamera.transform.parent.GetComponentsInChildren<AudioSource>();
        Check(sounds.Count(a=>a.isPlaying)>=2,"Garden and fountain ambience play");
        p.SoundEnabled=false;yield return null;yield return null;Check(sounds.All(a=>a.mute),"Sound setting mutes all area audio");
        p.SetVisible(false);Check(!p.StageCamera.gameObject.activeInHierarchy&&sounds.All(a=>!a.isPlaying),"Canvas-hidden Home stops rendering and audio");p.SetVisible(true);yield return null;
        p.MotionEnabled=false;yield return null;float clock=p.Clock;yield return new WaitForSecondsRealtime(.15f);Check(Mathf.Approximately(clock,p.Clock),"Reduced-motion setting freezes water and flag clock");
        Check(p.StageCamera.transform.parent.GetComponentsInChildren<ParticleSystem>().All(s=>!s.isPlaying),"Reduced-motion setting pauses particles");
        p.MotionEnabled=true;yield return new WaitForSecondsRealtime(.4f);Check(p.Clock>clock+.1f,"Motion resumes without reset");
        Shot(p,"area1-live-fountain",p.Clock);
        p.enabled=false;Check(!p.StageCamera.gameObject.activeInHierarchy,"Disabled area stops render stage");p.enabled=true;
        p.SetProgress(0);p.StartCoroutine(p.Reveal(0,true));yield return new WaitForSecondsRealtime(.15f);Check(p.Revealing,"Reveal coroutine starts");
        p.SetProgress(1);yield return null;yield return null;Check(!p.Revealing&&p.CompletedMask==1,"Cancelled reveal preserves supplied saved progress");
        Check(errors.Count==0,"No lifecycle runtime errors: "+string.Join(" | ",errors.Take(4)));
        lines.Add("ALL NATIVE AREA RENDER AND LIFECYCLE CHECKS PASSED");File.WriteAllLines("Results/checks.txt",lines);File.WriteAllLines("Results/errors.txt",errors);File.WriteAllLines("Results/editor-issues.txt",editorIssues);RoyalAreaMotionCapture.Capture();
    }
    static void CheckFinite(Vector3 v){if(float.IsNaN(v.x)||float.IsInfinity(v.x)||float.IsNaN(v.y)||float.IsInfinity(v.y))throw new Exception("Nonfinite mesh vertex");}
}

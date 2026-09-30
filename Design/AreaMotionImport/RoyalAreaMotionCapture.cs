using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Media;
using RoyalThrone.Presentation.AreaMotion;

// Isolated review capture only; never changes player saves or the main scene.
[InitializeOnLoad]
public static class RoyalAreaMotionCapture
{
    const string Key="RoyalAreaMotionCapture.Pending";
    static RoyalAreaMotionCapture(){if(SessionState.GetBool(Key,false))EditorApplication.update+=Start;}
    public static void Run(){SessionState.SetBool(Key,true);EditorApplication.update+=Start;EditorApplication.EnterPlaymode();}
    static void Start(){if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;EditorApplication.update-=Start;SessionState.SetBool(Key,false);Capture();}
    public static void Capture()
    {
        try{
            var root=new GameObject("Review",typeof(RectTransform),typeof(Canvas));root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var frame=new GameObject("Area",typeof(RectTransform));frame.transform.SetParent(root.transform,false);frame.GetComponent<RectTransform>().sizeDelta=new Vector2(720,1280);
            var p=frame.AddComponent<RoyalAreaMotionPlayer>();p.SoundEnabled=false;p.Load(1,63);Canvas.ForceUpdateCanvases();p.RenderForReview(0);
            int width=p.StageCamera.targetTexture.width,height=p.StageCamera.targetTexture.height;
            var im=new Texture2D(width,height,TextureFormat.RGBA32,false);
            var attrs=new VideoTrackAttributes{frameRate=new MediaRational(30),width=(uint)width,height=(uint)height,includeAlpha=false};
            using(var encoder=new MediaEncoder("Results/Royal-Throne-Areas-Unity-Preview.mp4",attrs)){
                float clock=0;
                for(int segment=0;segment<5;segment++){
                    int area=segment<3?segment+1:1;bool closeup=segment==4;
                    p.Load(area,0);int task=area==1?1:area==2?6:7;
                    bool reveal=segment<3;float duration=reveal?p.Duration(task)+1.5f:5f;
                    if(!reveal)p.SetProgress(63);
                    for(int f=0;f<Mathf.CeilToInt(duration*30);f++){
                        float t=f/30f;if(reveal)p.SeekForReview(task,Mathf.Min(t,p.Duration(task)));
                        foreach(var ps in p.StageCamera.transform.parent.GetComponentsInChildren<ParticleSystem>())ps.Simulate(1f/30,false,false,false);
                        p.RenderForReview(clock,closeup);clock+=1f/30;
                        var old=RenderTexture.active;var rt=p.StageCamera.targetTexture;RenderTexture.active=rt;im.ReadPixels(new Rect(0,0,width,height),0,0);im.Apply();RenderTexture.active=old;
                        if(!encoder.AddFrame(im))throw new Exception("Video encoder rejected frame");
                        if(f==Mathf.CeilToInt(duration*30)-1)File.WriteAllBytes("Results/review-segment-"+segment+".png",im.EncodeToPNG());
                    }
                }
            }
            File.WriteAllText("Results/capture-success.txt","Native Unity capture, 30 fps, silent review video; game audio verified separately.");
            EditorApplication.Exit(0);
        }catch(Exception e){File.WriteAllText("Results/capture-failure.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
}

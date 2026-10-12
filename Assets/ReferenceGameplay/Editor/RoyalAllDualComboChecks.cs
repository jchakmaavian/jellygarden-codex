#if UNITY_EDITOR && !ROYAL_DISABLE_QA
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using RoyalThrone.Testing;
using RoyalThrone.Boosters.Propeller.V5;
using RoyalThrone.ReferenceGameplay;
using Object=UnityEngine.Object;
namespace RoyalThrone.Editor
{
 public static class RoyalAllDualComboChecks
 {
  const string Folder="Logs/port-checks/DualCombo-Reference/Verification/";
  static readonly List<string> notes=new List<string>();
  static readonly List<string> errors=new List<string>();
  public static bool Running{get;private set;}
  public static string Status{get;private set;}
  struct Pair{public string name;public ItemsTypes a,b;public Pair(string n,ItemsTypes x,ItemsTypes y){name=n;a=x;b=y;}}
  static readonly Pair[] cases={
   new Pair("RocketH-RocketH",ItemsTypes.HORIZONTAL_STRIPPED,ItemsTypes.HORIZONTAL_STRIPPED),new Pair("RocketH-RocketV",ItemsTypes.HORIZONTAL_STRIPPED,ItemsTypes.VERTICAL_STRIPPED),new Pair("RocketV-RocketV",ItemsTypes.VERTICAL_STRIPPED,ItemsTypes.VERTICAL_STRIPPED),
   new Pair("TNT-TNT",ItemsTypes.PACKAGE,ItemsTypes.PACKAGE),new Pair("Propeller-Propeller",ItemsTypes.HELICOPTER,ItemsTypes.HELICOPTER),new Pair("Colorball-Colorball",ItemsTypes.BOMB,ItemsTypes.BOMB),
   new Pair("RocketH-TNT",ItemsTypes.HORIZONTAL_STRIPPED,ItemsTypes.PACKAGE),new Pair("RocketV-TNT",ItemsTypes.VERTICAL_STRIPPED,ItemsTypes.PACKAGE),
   new Pair("Propeller-RocketH",ItemsTypes.HELICOPTER,ItemsTypes.HORIZONTAL_STRIPPED),new Pair("Propeller-RocketV",ItemsTypes.HELICOPTER,ItemsTypes.VERTICAL_STRIPPED),new Pair("Propeller-TNT",ItemsTypes.HELICOPTER,ItemsTypes.PACKAGE),
   new Pair("Colorball-RocketH",ItemsTypes.BOMB,ItemsTypes.HORIZONTAL_STRIPPED),new Pair("Colorball-RocketV",ItemsTypes.BOMB,ItemsTypes.VERTICAL_STRIPPED),new Pair("Colorball-TNT",ItemsTypes.BOMB,ItemsTypes.PACKAGE),new Pair("Colorball-Propeller",ItemsTypes.BOMB,ItemsTypes.HELICOPTER)};
  static void Check(bool ok,string message){notes.Add((ok?"PASS ":"FAIL ")+message);File.WriteAllLines(Folder+"checks.txt",notes);if(!ok)throw new InvalidOperationException(message);}
  static void Log(string message,string stack,LogType type)
  {
   if(type!=LogType.Error&&type!=LogType.Exception&&type!=LogType.Assert)return;
   // Unity 6 search startup can throw independently of gameplay in a fresh fixture.
   if(message.StartsWith("ArgumentOutOfRangeException:")&&stack.Contains("UnityEditor.Search.SearchDatabase")){notes.Add("EDITOR ONLY: Unity Search startup exception excluded from runtime checks");return;}
   errors.Add(message+"\n"+stack);
  }
  [MenuItem("Tools/Royal Throne/QA/Verify all dual booster combinations")]
  public static void Run(){var qa=RoyalQaService.Instance;if(Running||!Application.isPlaying||!qa||!qa.Ready||qa.Busy||RoyalSave.Testing)throw new InvalidOperationException("Ready normal profile required.");Running=true;notes.Clear();errors.Clear();Directory.CreateDirectory(Folder);RoyalRegressionRunner.Run(Suite(qa));}
  static object EndFrame()=>Application.isBatchMode?null:new WaitForEndOfFrame();
  static void Capture(string name,int frame)
  {
   string dir=Folder+"Frames/"+name+"/";Directory.CreateDirectory(dir);
   var image=Application.isBatchMode?null:ScreenCapture.CaptureScreenshotAsTexture();var previous=RenderTexture.active;var rt=RenderTexture.GetTemporary(360,640,0,RenderTextureFormat.ARGB32);var small=new Texture2D(360,640,TextureFormat.RGB24,false);
   try{if(image)Graphics.Blit(image,rt);else{var camera=Camera.main;var previousTarget=camera.targetTexture;camera.targetTexture=rt;camera.Render();camera.targetTexture=previousTarget;}RenderTexture.active=rt;small.ReadPixels(new Rect(0,0,360,640),0,0);small.Apply();File.WriteAllBytes(dir+frame.ToString("D4")+".png",small.EncodeToPNG());}finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Object.Destroy(image);Object.Destroy(small);}
  }
  static void Prepare(Pair pair,bool vertical=false)
  {
   var level=LevelManager.THIS;var a=level.GetSquare(3,3).item;var b=level.GetSquare(vertical?3:4,vertical?4:3).item;a.NextType=pair.a;b.NextType=pair.b;a.ChangeType();b.ChangeType();
  }
  static IEnumerator Suite(RoyalQaService qa)
  {
   float capture=Time.captureDeltaTime;int limit=Application.targetFrameRate,vsync=QualitySettings.vSyncCount;int coins=InitScript.Gems,lives=InitScript.lifes;bool complete=false;Application.logMessageReceived+=Log;
   try
   {
    qa.StartSession();qa.Level=6;qa.Visible=false;Check(RoyalSave.Testing,"Player profile isolated before all combo checks");
    foreach(int fps in new[]{30,15,60})foreach(var pair in cases)
    {
     Time.captureDeltaTime=1f/fps;Application.targetFrameRate=fps;QualitySettings.vSyncCount=0;Status=pair.name+" at "+fps+" FPS";
     yield return qa.Practice();qa.Visible=false;Prepare(pair,fps==15);yield return new WaitForSeconds(1.5f);
     var level=LevelManager.THIS;var director=RoyalPowerupDirector.Get;int tnt=director.NativeTntComboPlays,dual=director.NativeDualColorBallPlays,rt=director.NativeRocketTntComboPlays,prop=director.PropellerPayloadHandoffs,moves=level.Limit;
     var a=level.GetSquare(3,3).item;var b=level.GetSquare(fps==15?3:4,fps==15?4:3).item;
     Check(RoyalPowerupDirector.TrySwap(a,b),Status+": real pair starts");int frame=0,saved=0;bool shown=false,contact=false,charge=false,unsupported=false,paused=false,premature=false;float deadline=Time.realtimeSinceStartup+60;
     while((RoyalPowerupDirector.Busy||frame==0)&&Time.realtimeSinceStartup<deadline)
     {
      yield return EndFrame();var systems=Object.FindObjectsByType<ParticleSystem>();
      foreach(var ps in systems){if(ps.particleCount<=0)continue;shown=true;var renderer=ps.GetComponent<ParticleSystemRenderer>();unsupported|=renderer&&(!renderer.sharedMaterial||!renderer.sharedMaterial.shader||!renderer.sharedMaterial.shader.isSupported);}
      bool pairedFlight=Object.FindObjectsByType<PropellerVisualV5>().Any(v=>v.Current==PropellerVisualV5.Phase.Flight&&v.HasCarriedBooster);
      if(pair.name.StartsWith("Propeller-Rocket"))premature|=pairedFlight&&systems.Any(ps=>ps.particleCount>0&&ps.GetComponentsInParent<Transform>().Any(parent=>parent.name.StartsWith("PropellerRocketComboParticles")));
      charge|=systems.Any(ps=>ps.particleCount>0&&(ps.name=="add_energy_lines"||ps.name=="sparks_colored"));
      contact|=systems.Any(ps=>ps.particleCount>0&&(ps.name=="add_ring"||ps.name=="explode_TNT"||ps.name=="explode"||ps.name=="bang"));
      if(fps==60&&!paused&&frame>=3){level.gameStatus=GameState.Pause;yield return null;yield return EndFrame();var frozen=systems.Where(ps=>ps&&ps.particleCount>0).ToDictionary(ps=>ps,ps=>ps.time);yield return null;yield return EndFrame();yield return null;yield return EndFrame();Check(frozen.All(k=>!k.Key||Mathf.Abs(k.Key.time-k.Value)<.00001f),Status+": native particles pause without advancing");level.gameStatus=GameState.Playing;paused=true;}
      if(fps==60&&frame%2==0)Capture(pair.name,saved++);frame++;
     }
     Check(!RoyalPowerupDirector.Busy,Status+": gameplay completes without stuck resolver");Check(shown&&!unsupported,Status+": live VFX render with supported shaders");if(pair.name.StartsWith("Propeller-Rocket"))Check(!premature,Status+": combo explosion waits for contact");Check(level.Limit==moves,Status+": presentation adds no extra move debit");
     if(pair.name=="TNT-TNT"){Check(director.NativeTntComboPlays==tnt+1,Status+": dedicated native body actually used");Check(Mathf.Abs(director.LastTntComboPreparation-1.583333f)<.0001f,Status+": authored duration retained");Check(charge&&contact,Status+": fuse and combo blast particles render");}
     if(pair.name=="Colorball-Colorball"){Check(director.NativeDualColorBallPlays==dual+1&&director.DualColorBallHits==64,Status+": one native dual sequence and 64 selected cells");}
     if(pair.name.EndsWith("-TNT")&&pair.name.StartsWith("Rocket")){Check(director.NativeRocketTntComboPlays==rt+1,Status+": dedicated combo owner used");}
     if(pair.name.StartsWith("Propeller-")&&pair.name!="Propeller-Propeller")Check(director.PropellerPayloadHandoffs==prop+1,Status+": carried booster handed to existing damage owner once");
     if(pair.name.StartsWith("Colorball-")&&pair.name!="Colorball-Colorball")Check(director.ColorBallConversions>0&&director.ColorBallConversions==director.ColorBallSelectedTargets,Status+": every selected gem converts before booster handoff");
     float tail=Time.realtimeSinceStartup+30;while((ReferenceVfxPool.Active>0||ReferenceDualColorBallVisual.ActiveCount>0||PropellerVisualV5.Active>0)&&Time.realtimeSinceStartup<tail){yield return EndFrame();var tailSystems=Object.FindObjectsByType<ParticleSystem>();charge|=tailSystems.Any(ps=>ps.particleCount>0&&(ps.name=="add_energy_lines"||ps.name=="sparks_colored"));contact|=tailSystems.Any(ps=>ps.particleCount>0&&(ps.name=="add_ring"||ps.name=="explode_TNT"||ps.name=="explode"||ps.name=="bang"));if(fps==60&&frame++%2==0&&saved<360)Capture(pair.name,saved++);}
     if(pair.name=="Colorball-Colorball")Check(charge&&contact,Status+": native charge and rainbow blast render");if(pair.name.EndsWith("-TNT")&&pair.name.StartsWith("Rocket"))Check(charge&&contact,Status+": dedicated charge and combo blast render");Check(ReferenceVfxPool.Active==0&&ReferenceDualColorBallVisual.ActiveCount==0&&PropellerVisualV5.Active==0,Status+": actors and VFX tails retire");
     Check(level.GetItems().All(i=>!i||i.inputConversionLocks==0),Status+": every conversion input lock released");director.Cancel();
    }
    foreach(var pair in cases)
    {
     Status="Cancel "+pair.name;yield return qa.Practice();qa.Visible=false;Prepare(pair);yield return new WaitForSeconds(1.5f);var level=LevelManager.THIS;RoyalPowerupDirector.TrySwap(level.GetSquare(3,3).item,level.GetSquare(4,3).item);yield return null;RoyalPowerupDirector.Get.Cancel();yield return null;
     Check(!RoyalPowerupDirector.Busy&&ReferenceVfxPool.Active==0&&ReferenceDualColorBallVisual.ActiveCount==0&&PropellerVisualV5.Active==0,Status+": no active presentation leases");Check(level.GetItems().All(i=>!i||i.inputConversionLocks==0),Status+": all target locks released");
    }
    yield return qa.EndSession();Check(!RoyalSave.Testing&&InitScript.Gems==coins&&InitScript.lifes==lives,"Normal player coins, lives and progress restored");Check(errors.Count==0,"No runtime errors during all combo checks");complete=true;Status="ALL DUAL COMBO CHECKS PASSED";
   }
   finally
   {
    RoyalPowerupDirector.Get?.Cancel();Time.captureDeltaTime=capture;Application.targetFrameRate=limit;QualitySettings.vSyncCount=vsync;Application.logMessageReceived-=Log;Running=false;if(RoyalSave.Testing)RoyalRegressionRunner.Run(qa.EndSession());notes.Add(complete?"ALL DUAL COMBO CHECKS PASSED":"INCOMPLETE: "+Status);File.WriteAllLines(Folder+"checks.txt",notes);
   }
  }
 }
}
#endif

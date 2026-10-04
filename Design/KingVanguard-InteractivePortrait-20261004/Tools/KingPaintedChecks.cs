using System.IO;
using UnityEngine;
public static class KingPaintedChecks {
 public static void Run(){
  var text=KingImpactLaughChecks.Run();
  var shader=Resources.Load<Shader>("KingVanguard/Interactive/KingVanguardPainted");
  if(!shader||UnityEditor.ShaderUtil.ShaderHasError(shader))throw new System.Exception("Portrait shader import failed");
  File.WriteAllText("../JellyGarden-Codex/Design/KingVanguard-InteractivePortrait-20261004/Review/all-rig-checks.txt",text+"\nPASS portrait shader has no import errors\n");
 }
}

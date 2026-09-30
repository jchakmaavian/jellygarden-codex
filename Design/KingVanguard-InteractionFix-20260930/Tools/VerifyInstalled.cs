var output="Design/KingVanguard-InteractionFix-20260930/Validation";
System.IO.Directory.CreateDirectory(output);
var report=new System.Text.StringBuilder();
foreach(var name in new[]{"King_Vanguard","AreaCompleted_Vanguard"}){
 string path="Assets/Characters/KingVenguard/V2/Spine/"+name+".png";
 using(var sha=System.Security.Cryptography.SHA256.Create())report.AppendLine(name+" PNG SHA256 "+System.BitConverter.ToString(sha.ComputeHash(System.IO.File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant());
 var texture=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(path);
 report.AppendLine(name+" imported "+texture.width+"x"+texture.height+" "+texture.format);
 var data=UnityEditor.AssetDatabase.LoadAssetAtPath<Spine.Unity.SkeletonDataAsset>("Assets/Characters/KingVenguard/V2/Spine/"+name+"_SkeletonData.asset");
 report.AppendLine(name+" source animation tracks "+data.GetSkeletonData(false).Animations.Count);
}
foreach(var name in new[]{"KingVanguard","KingVanguardCelebration"}){
 var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Characters/KingVenguard/V2/Resources/KingVanguard/"+name+".prefab");
 var face=prefab.GetComponentInChildren<RoyalThrone.Characters.KingVanguardFaceRig>(true);
 if(!face||!face.StabilizeTeeth)throw new System.Exception("Missing enabled teeth stabilization on "+name);
 report.AppendLine("PASS "+name+" prefab has teeth stabilization enabled");
}
if(RoyalThrone.Testing.RoyalSave.Testing||UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Expected restored profile and stopped editor");
report.AppendLine("PASS normal profile restored; editor stopped");
System.IO.File.WriteAllText(output+"/installed-state.txt",report.ToString());return report.ToString();

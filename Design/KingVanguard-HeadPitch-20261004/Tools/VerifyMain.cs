if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Edit mode required");
string root="Design/KingVanguard-HeadPitch-20261004/Review/";
string[] paths={"Design/KingVanguard-Lively-20261004/Review/motion-checks.txt","Design/KingVanguard-InteractivePortrait-20261004/Review/rig-checks.txt"};
var saved=new System.Collections.Generic.Dictionary<string,byte[]>();
foreach(var p in paths)if(System.IO.File.Exists(p))saved[p]=System.IO.File.ReadAllBytes(p);
try{
 var report=KingHeadPitchChecks.Run()+"\n"+KingLivelyChecks.Run()+"\n"+RoyalThrone.Characters.EditorTools.KingVanguardRuntimeChecks.Run();
 System.IO.File.WriteAllText(root+"main-rig-checks.txt",report);return report;
}finally{foreach(var pair in saved)System.IO.File.WriteAllBytes(pair.Key,pair.Value);}

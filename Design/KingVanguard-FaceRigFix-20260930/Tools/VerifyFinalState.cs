var qa=RoyalThrone.Testing.RoyalQaService.Instance;
bool restored=!RoyalThrone.Testing.RoyalSave.Testing&&!qa.Busy;
string report="Design/KingVanguard-FaceRigFix-20260930/Validation/final-state.txt";
System.IO.File.WriteAllText(report,(restored?"PASS":"FAIL")+" normal player profile restored; isolated QA inactive\nStatus: "+qa.Status+"\nUnity: "+UnityEngine.Application.unityVersion+"\n");
if(!restored)throw new System.Exception("QA profile has not finished restoring");
return "PASS normal player profile restored";

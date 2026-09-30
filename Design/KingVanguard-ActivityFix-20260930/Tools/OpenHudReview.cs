UnityEngine.Application.runInBackground=true;
var qa=RoyalThrone.Testing.RoyalQaService.Instance;
if(!qa||!qa.Ready)throw new System.Exception("Game not ready");
qa.StartSession();
if(!RoyalThrone.Testing.RoyalSave.Testing)throw new System.Exception("QA profile required");
qa.Visible=false;qa.Counters=false;qa.Level=33;qa.Request("Open level");
return qa.Status;

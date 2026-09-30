var qa=RoyalThrone.Testing.RoyalQaService.Instance;
if(!RoyalThrone.Testing.RoyalSave.Testing)throw new System.Exception("QA required");
qa.Amount=3;qa.Request("Set moves");
return qa.Status;

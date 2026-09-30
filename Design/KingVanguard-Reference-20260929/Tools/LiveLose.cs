var qa=RoyalThrone.Testing.RoyalQaService.Instance;
if(!RoyalThrone.Testing.RoyalSave.Testing)throw new System.Exception("QA required");
qa.Request("Lose instantly");return qa.Status;

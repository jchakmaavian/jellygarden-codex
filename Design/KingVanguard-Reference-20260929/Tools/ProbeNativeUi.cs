var qa=RoyalThrone.Testing.RoyalQaService.Instance;qa.StartSession();qa.Visible=false;
UnityEngine.Object.FindAnyObjectByType<RoyalThrone.Presentation.RoyalHomeScreen>().OpenPanel("leagues");
return "opened";

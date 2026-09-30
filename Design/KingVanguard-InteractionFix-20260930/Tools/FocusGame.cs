var gameType=typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.GameView");
UnityEditor.EditorWindow.GetWindow(gameType).Focus();
var qa=RoyalThrone.Testing.RoyalQaService.Instance;
return "Focused="+UnityEngine.Application.isFocused+" Ready="+(qa&&qa.Ready)+" Busy="+(qa&&qa.Busy)+" Testing="+RoyalThrone.Testing.RoyalSave.Testing;

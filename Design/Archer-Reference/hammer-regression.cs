System.Collections.IEnumerator Run() {
 var settings=UnityEngine.InputSystem.InputSystem.settings;var oldInput=settings.editorInputBehaviorInPlayMode;var oldBackground=settings.backgroundBehavior;
 try {
 settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
 settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
 yield return RoyalThrone.ReferenceGameplay.Editor.ReferenceHammerChecks.Run();
 System.IO.File.Copy("Design/Hammer-Reference/Review/gameplay-checks.txt","Design/Archer-Reference/Review/hammer-regression-checks.txt",true);
 } finally {
 UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=oldInput;UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=oldBackground;
 }
}
RoyalThrone.Editor.RoyalRegressionRunner.Run(Run());return true;

{
 try {
  var report=RoyalThrone.Characters.EditorTools.KingVanguardAnimationReview.RenderContactSheets("Design/KingVanguard-Reference-20260929/AnimationReview");
  System.IO.File.WriteAllText("Design/KingVanguard-Reference-20260929/Validation/render-all-status.txt",report);
 } catch(System.Exception e) {System.IO.File.WriteAllText("Design/KingVanguard-Reference-20260929/Validation/render-all-status.txt",e.ToString());UnityEngine.Debug.LogException(e);}
}
return System.IO.File.ReadAllText("Design/KingVanguard-Reference-20260929/Validation/render-all-status.txt");

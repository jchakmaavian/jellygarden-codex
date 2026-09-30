var view=UnityEngine.Object.FindAnyObjectByType<RoyalThrone.Characters.Activities.NativeKingDrillUiSlot>();
if(!view)return "missing";
var text="show="+view.IsShowing+" time="+UnityEngine.Time.unscaledTime+"\n";
foreach(var clip in view.Actor.Animator.GetCurrentAnimatorClipInfo(0))text+=clip.clip.name+" length="+clip.clip.length+"\n";
foreach(var field in typeof(RoyalThrone.Characters.Activities.NativeKingDrillUiSlot).GetFields(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic))if(field.Name=="idleAt"||field.Name=="waitingForIdle")text+=field.Name+"="+field.GetValue(view)+"\n";
return text;

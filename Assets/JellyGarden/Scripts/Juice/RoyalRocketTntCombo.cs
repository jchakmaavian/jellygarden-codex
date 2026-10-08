using System.Collections;
using System.Linq;
using RoyalThrone.ReferenceGameplay;
using UnityEngine;
namespace JellyGarden.Scripts.Juice
{
 public sealed partial class RoyalPowerupDirector
 {
  public int NativeRocketTntComboPlays {get;private set;}
  IEnumerator NativeRocketTntCombo(Square origin)
  {
   var position=origin.transform.position;
   var charge=ReferenceVfxPool.Play("TntRocketComboUseParticles",position,worldCell:Cell,sustained:true);
   bool fired=false;
   try
   {
    NativeRocketTntComboPlays++;
    ReferenceGameplayAudio.Play("rocket_tnt_combo__rfv387_17431",.45f);
    // Retain the existing launch anticipation and board lanes. The APK has no
    // authored Rocket+TNT clip; its compiled preparation duration is unverified.
    yield return Wait(Mathf.Max(0,RoyalEffectsProfile.Get.rocketAnticipation));
    // A zero-delay rocket profile must not stop the source charge in its start
    // frame. Let the native emitter simulate and present its first particles.
    float chargeAge=0;
    while(charge!=null&&charge.Valid&&chargeAge<.5f)
    {
     yield return null;
     if(!charge.Valid)yield break;
     if(Paused)continue;
     chargeAge+=Time.deltaTime;
     if(charge.GameObject.GetComponentsInChildren<ParticleSystem>().Any(ps=>ps.particleCount>0))
     {yield return null;break;}
    }
    charge?.Stop();
    ReferenceVfxPool.Play("TntRocketComboParticles",position,worldCell:Cell);
    fired=true;
    StartOperation(Rocket(origin,true,3,true,true,true));
    yield return Rocket(origin,false,3,true,true,true);
   }
   finally{charge?.Stop();if(!fired)RoyalAudioDirector.Get?.StopClip(ReferenceGameplayAudio.Load("rocket_tnt_combo__rfv387_17431"));}
  }
 }
}

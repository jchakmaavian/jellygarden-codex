var lines = new System.Collections.Generic.List<string>();
int oldSound = UnityEngine.PlayerPrefs.GetInt("Sound",1);
var host = new UnityEngine.GameObject("Hammer media verification");
var probe = host.AddComponent<RoyalThrone.ReferenceGameplay.ReferenceHammerSequence>();
var owner = JellyGarden.Scripts.Juice.RoyalAudioDirector.Get;
System.Action<bool,string> check=(ok,message)=>{lines.Add((ok?"PASS ":"FAIL ")+message);if(!ok)throw new System.Exception(message);};
try
{
    UnityEngine.PlayerPrefs.SetInt("Sound",1);
    probe.Begin(UnityEngine.Vector3.zero,UnityEngine.Vector3.down*3,1,()=>{});
    var voices=owner.GetComponents<UnityEngine.AudioSource>();
    check(System.Linq.Enumerable.Any(voices,v=>v.isPlaying&&v.clip&&v.clip.name=="hammer_use_appear"),"Original appearance cue plays through the game audio director");
    probe.Advance(.45f,false);
    check(System.Linq.Enumerable.Any(voices,v=>v.isPlaying&&v.clip&&v.clip.name=="hammer_use_hit"),"Original hit cue plays at impact");
    var effect=UnityEngine.GameObject.Find("Hammer - native impact VFX");
    var systems=effect.GetComponentsInChildren<UnityEngine.ParticleSystem>();
    var times=System.Linq.Enumerable.Select(systems,p=>p.time).ToArray();
    check(System.Linq.Enumerable.Sum(systems,p=>p.particleCount)>0,"Recovered native impact emits visible particles");
    probe.Advance(1,true);
    check(System.Linq.Enumerable.Range(0,systems.Length).All(i=>UnityEngine.Mathf.Approximately(times[i],systems[i].time)),"Pause freezes native particle simulation");
    check(!System.Linq.Enumerable.Any(voices,v=>v.isPlaying&&v.clip&&v.clip.name.StartsWith("hammer_use_")),"Pause freezes owned hammer sound cues");
    probe.Advance(.5f,false);
    check(System.Linq.Enumerable.Any(voices,v=>v.isPlaying&&v.clip&&v.clip.name=="hammer_use_disappear"),"Original return cue plays");
    probe.Cancel();
    check(!System.Linq.Enumerable.Any(voices,v=>v.isPlaying&&v.clip&&v.clip.name.StartsWith("hammer_use_")),"Cancellation stops only hammer cues");
    UnityEngine.PlayerPrefs.SetInt("Sound",0);
    probe.Begin(UnityEngine.Vector3.zero,UnityEngine.Vector3.down*3,1,()=>{});probe.Advance(.5f,false);
    check(!System.Linq.Enumerable.Any(voices,v=>v.isPlaying&&v.clip&&v.clip.name.StartsWith("hammer_use_")),"Muted sound preference suppresses appearance and hit cues");
}
finally
{
    probe.Cancel();UnityEngine.Object.Destroy(host);UnityEngine.PlayerPrefs.SetInt("Sound",oldSound);
    System.IO.File.WriteAllLines("Design/Hammer-Reference/Review/media-checks.txt",lines);
}
return true;

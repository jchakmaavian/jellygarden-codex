using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    public enum RoyalCue { Match, Land, Rocket, Tnt, Helicopter, Create, Click, Firework, Celebrate }
    public sealed class RoyalAudioDirector : MonoBehaviour
    {
        static RoyalAudioDirector instance;
        // Effects only: King Leo's MusicBase keeps the music, so this director plays none.
        AudioSource[] voices;
        readonly AudioClip[] clips = new AudioClip[11];
        readonly float[] last = new float[9];
        float[] voiceGains;
        int variation;
        public int PeakVoices { get; private set; }
        public static RoyalAudioDirector Get
        {
            get
            {
                if (!Application.isPlaying || !RoyalEffectsProfile.Get) return null;
                if (!instance) { var go = new GameObject("Royal audio — capped voices"); instance=go.AddComponent<RoyalAudioDirector>(); DontDestroyOnLoad(go); instance.Build(); }
                return instance;
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatics() { instance=null; }
        void Build()
        {
            // Slot 6 held Royal Throne's castle theme; music stays King Leo's.
            string[] names={"match-a","match-b","rocket","tnt","helicopter","create",null,"land","click","firework","celebrate"};
            for(int i=0;i<names.Length;i++) if(names[i]!=null) clips[i]=Resources.Load<AudioClip>("RoyalGameplay/Audio/"+names[i]);
            voices=new AudioSource[RoyalEffectsProfile.Get.soundVoices];
            voiceGains=new float[voices.Length];
            for(int i=0;i<voices.Length;i++) { voices[i]=gameObject.AddComponent<AudioSource>(); voices[i].playOnAwake=false; voices[i].spatialBlend=0; voices[i].priority=160; }
            for(int i=0;i<last.Length;i++) last[i]=-10;
        }
        public void Play(RoyalCue cue,float strength=1)
        {
            if (PlayerPrefs.GetInt("Sound",1)==0) return;
            int k=(int)cue;
            float interval=cue==RoyalCue.Celebrate?1.2f:cue==RoyalCue.Firework?.28f:cue==RoyalCue.Match?.065f:cue==RoyalCue.Land?.09f:.07f;
            if(Time.unscaledTime-last[k]<interval)return;
            last[k]=Time.unscaledTime;
            AudioClip clip=cue==RoyalCue.Match?clips[(variation++)%2]:cue==RoyalCue.Land?clips[7]:cue==RoyalCue.Click?clips[8]:cue==RoyalCue.Firework?clips[9]:cue==RoyalCue.Celebrate?clips[10]:clips[k];
            var imported = RoyalThrone.ReferenceGameplay.ReferenceGameplayAudio.For(cue);
            if (imported) clip = imported;
            float gain=cue==RoyalCue.Land?.16f:cue==RoyalCue.Match?.58f:cue==RoyalCue.Tnt?.95f:cue==RoyalCue.Click?.42f:cue==RoyalCue.Firework?.50f:cue==RoyalCue.Celebrate?.70f:.72f;
            int priority=cue==RoyalCue.Land?200:cue==RoyalCue.Match?160:cue==RoyalCue.Firework?90:60;
            // Restrained variation keeps repeated contacts organic without squeaky pitch climbs.
            PlayClip(clip,gain*Mathf.Clamp(strength,.6f,1.2f),priority, !imported&&cue==RoyalCue.Match?1+((variation%5)-2)*.012f:1);
        }
        public void PlayClip(AudioClip clip,float gain=1,int priority=150,float pitch=1)
        {
            if(!clip||PlayerPrefs.GetInt("Sound",1)==0)return;
            AudioSource chosen=null;
            foreach(var v in voices) if(!v.isPlaying) {chosen=v;break;}
            if(!chosen) foreach(var v in voices) if(v.priority>priority) {chosen=v;break;}
            if(!chosen)return;
            chosen.Stop(); chosen.clip=clip; chosen.priority=priority; chosen.pitch=pitch;
            voiceGains[global::System.Array.IndexOf(voices,chosen)]=gain*RoyalEffectsProfile.Get.effectsVolume;
            chosen.Play(); BalanceVoices();
        }
        public static void StopReferenceVoices()
        {
            if (!instance || instance.voices == null) return;
            for (int i = 0; i < instance.voices.Length; i++)
            {
                var voice = instance.voices[i];
                if (!voice || !RoyalThrone.ReferenceGameplay.ReferenceGameplayAudio.Owns(voice.clip)) continue;
                voice.Stop(); voice.clip = null; instance.voiceGains[i] = 0;
            }
        }
        // Cancel only the owning presentation's cue; other gameplay voices continue.
        public void StopClip(AudioClip clip)
        {
            if (!clip || voices == null) return;
            for (int i = 0; i < voices.Length; i++)
                if (voices[i] && voices[i].clip == clip)
                { voices[i].Stop(); voices[i].clip = null; voiceGains[i] = 0; }
        }
        void BalanceVoices()
        {
            // Reserve headroom for the unchanged music mix, even on dense cascades.
            float total=0;
            for(int i=0;i<voices.Length;i++) if(voices[i].isPlaying) total+=voiceGains[i];
            float headroom=Mathf.Min(1, .68f/Mathf.Max(.001f,total));
            for(int i=0;i<voices.Length;i++) voices[i].volume=voiceGains[i]*headroom*sfxGate;
        }
        // Switching Sound off fades the effects over 0.1 s instead of cutting them.
        const float SfxFade=.1f;
        float sfxGate=1;
        void Update()
        {
            if(voices==null)return;
            float dt=Time.unscaledDeltaTime;
            int count=0; bool soundOff=PlayerPrefs.GetInt("Sound",1)==0;   // one read per frame, not one per voice
            sfxGate=Mathf.MoveTowards(sfxGate,soundOff?0:1,dt/SfxFade);
            foreach(var v in voices){ if(v.isPlaying)count++; if(soundOff&&sfxGate<=0&&v.isPlaying)v.Stop(); }
            PeakVoices=Mathf.Max(PeakVoices,count);
            BalanceVoices();
        }
    }
}

using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    public enum RoyalCue { Match, Land, Rocket, Tnt, Helicopter, Create, Click }
    public sealed class RoyalAudioDirector : MonoBehaviour
    {
        static RoyalAudioDirector instance;
        AudioSource[] voices; AudioSource[] music;
        readonly AudioClip[] clips = new AudioClip[9];
        readonly float[] last = new float[7];
        int variation, musicIndex; float duck, fade=-1;
        public int PeakVoices { get; private set; }
        public static RoyalAudioDirector Get
        {
            get
            {
                if (!Application.isPlaying || !RoyalEffectsProfile.Get) return null;
                if (!instance) { var go = new GameObject("Royal audio — capped voices + music crossfade"); instance=go.AddComponent<RoyalAudioDirector>(); DontDestroyOnLoad(go); instance.Build(); }
                return instance;
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatics() { instance=null; }
        void Build()
        {
            string[] names={"match-a","match-b","rocket","tnt","helicopter","create","castle-theme","land","click"};
            for(int i=0;i<names.Length;i++) clips[i]=Resources.Load<AudioClip>("RoyalGameplay/Audio/"+names[i]);
            voices=new AudioSource[RoyalEffectsProfile.Get.soundVoices];
            for(int i=0;i<voices.Length;i++) { voices[i]=gameObject.AddComponent<AudioSource>(); voices[i].playOnAwake=false; voices[i].spatialBlend=0; voices[i].priority=160; }
            music=new AudioSource[2];
            for(int i=0;i<2;i++) { music[i]=gameObject.AddComponent<AudioSource>(); music[i].playOnAwake=false; music[i].clip=clips[6]; music[i].priority=220; music[i].volume=0; }
            for(int i=0;i<last.Length;i++) last[i]=-10;
            music[0].Play();
        }
        public void Play(RoyalCue cue,float strength=1)
        {
            if (PlayerPrefs.GetInt("Sound",1)==0) return;
            int k=(int)cue; float interval=cue==RoyalCue.Match?.055f:cue==RoyalCue.Land?.08f:.06f;
            if(Time.unscaledTime-last[k]<interval)return;
            last[k]=Time.unscaledTime;
            AudioClip clip=cue==RoyalCue.Match?clips[(variation++)%2]:cue==RoyalCue.Land?clips[7]:cue==RoyalCue.Click?clips[8]:clips[k];
            float gain=cue==RoyalCue.Land?.22f:cue==RoyalCue.Match?.70f:cue==RoyalCue.Tnt?.92f:cue==RoyalCue.Click?.55f:.78f;
            int priority=cue==RoyalCue.Land?200:cue==RoyalCue.Match?160:60;
            PlayClip(clip,gain*Mathf.Clamp(strength,.6f,1.2f),priority, cue==RoyalCue.Land?1:1+((variation%5)-2)*.028f);
            if(cue==RoyalCue.Tnt||cue==RoyalCue.Rocket) duck=.32f;
        }
        public void PlayClip(AudioClip clip,float gain=1,int priority=150,float pitch=1)
        {
            if(!clip||PlayerPrefs.GetInt("Sound",1)==0)return;
            AudioSource chosen=null;
            foreach(var v in voices) if(!v.isPlaying) {chosen=v;break;}
            if(!chosen) foreach(var v in voices) if(v.priority>priority) {chosen=v;break;}
            if(!chosen)return;
            chosen.Stop(); chosen.clip=clip; chosen.priority=priority; chosen.pitch=pitch;
            chosen.volume=gain*RoyalEffectsProfile.Get.effectsVolume; chosen.Play();
        }
        void Update()
        {
            if(voices==null)return;
            int count=0; foreach(var v in voices){ if(v.isPlaying)count++; if(PlayerPrefs.GetInt("Sound",1)==0)v.Stop(); }
            PeakVoices=Mathf.Max(PeakVoices,count);
            if(!clips[6])return;
            duck=Mathf.Max(0,duck-Time.unscaledDeltaTime);
            float target=PlayerPrefs.GetInt("Music",1)*RoyalEffectsProfile.Get.musicVolume*(duck>0?.55f:1);
            float duration=RoyalEffectsProfile.Get.musicCrossfade;
            if(fade<0&&music[musicIndex].time>=clips[6].length-duration) { fade=0; music[1-musicIndex].time=0; music[1-musicIndex].Play(); }
            if(fade>=0)
            {
                fade+=Time.unscaledDeltaTime; float t=Mathf.Clamp01(fade/duration);
                music[musicIndex].volume=Mathf.Cos(t*Mathf.PI*.5f)*target;
                music[1-musicIndex].volume=Mathf.Sin(t*Mathf.PI*.5f)*target;
                if(t>=1){music[musicIndex].Stop(); musicIndex=1-musicIndex; fade=-1;}
            }
            else { music[musicIndex].volume=Mathf.MoveTowards(music[musicIndex].volume,target,Time.unscaledDeltaTime); music[1-musicIndex].volume=0; if(!music[musicIndex].isPlaying)music[musicIndex].Play(); }
        }
    }
}

using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    public enum RoyalCue { Match, Land, Rocket, Tnt, Helicopter, Create, Click, Firework, Celebrate }
    public sealed class RoyalAudioDirector : MonoBehaviour
    {
        static RoyalAudioDirector instance;
        AudioSource[] voices; AudioSource[] music;
        readonly AudioClip[] clips = new AudioClip[11];
        readonly float[] last = new float[9];
        float[] voiceGains;
        int variation, musicIndex; float duck, fade=-1, winSince=-1;
        // Home keeps the castle theme; level n plays track (n-1) mod count, each an original seamless loop (Design/Audio/LevelMusic).
        // The user rejected the first ten tracks on 7 Oct 2026; The King's Quest, written in the castle theme's style, is on trial.
        public static readonly string[] LevelMusic={"level-kings-quest"};
        readonly AudioClip[] levelClips=new AudioClip[LevelMusic.Length];
        readonly bool[] levelTried=new bool[LevelMusic.Length];
        AudioLowPassFilter musicFilter;
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
            string[] names={"match-a","match-b","rocket","tnt","helicopter","create","castle-theme","land","click","firework","celebrate"};
            for(int i=0;i<names.Length;i++) clips[i]=Resources.Load<AudioClip>("RoyalGameplay/Audio/"+names[i]);
            voices=new AudioSource[RoyalEffectsProfile.Get.soundVoices];
            voiceGains=new float[voices.Length];
            for(int i=0;i<voices.Length;i++) { voices[i]=gameObject.AddComponent<AudioSource>(); voices[i].playOnAwake=false; voices[i].spatialBlend=0; voices[i].priority=160; }
            // The music has a child of its own, so the card can muffle it (a low-pass filter acts on its GameObject's sources)
            // without muffling the effects.
            var musicHost=new GameObject("Music"); musicHost.transform.SetParent(transform,false);
            music=new AudioSource[2];
            for(int i=0;i<2;i++) { music[i]=musicHost.AddComponent<AudioSource>(); music[i].playOnAwake=false; music[i].clip=clips[6]; music[i].priority=0; music[i].volume=0; }
            musicFilter=musicHost.AddComponent<AudioLowPassFilter>(); musicFilter.cutoffFrequency=22000; musicFilter.lowpassResonanceQ=1;
            for(int i=0;i<last.Length;i++) last[i]=-10;
            music[0].Play();
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
            // No ducking while a won level plays out: the win sets the music's level itself (MusicTarget).
            var level = LevelManager.THIS; bool winning = level && (level.gameStatus == GameState.PreWinAnimations || level.gameStatus == GameState.Win);
            if((cue==RoyalCue.Tnt||cue==RoyalCue.Rocket||cue==RoyalCue.Celebrate) && !winning) duck=.42f;
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
        // Cancel only the owning presentation's cue; music and other gameplay voices continue.
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
            for(int i=0;i<voices.Length;i++) voices[i].volume=voiceGains[i]*headroom;
        }
        // The end of a won level follows the user's Royal Match Level 13 clip (the user, 2026-10-06: "music like the clip",
        // replacing 10-05's music that held through the win): the level music fades out once the goals are met, so the
        // fanfare and the boosters play alone; on the card it comes back softly and muffled, after the star has landed (about
        // 0.9 s); Home brings it back in full.
        float MusicTarget(float normal)
        {
            var level=LevelManager.THIS; var state=level?level.gameStatus:GameState.Map;
            if(state==GameState.Win){ if(winSince<0)winSince=Time.unscaledTime; } else winSince=-1;
            if(musicFilter) musicFilter.cutoffFrequency=state==GameState.Win?1100:22000;
            if(state==GameState.PreWinAnimations) return 0;
            if(state==GameState.Win) return normal*.45f*Mathf.Clamp01((Time.unscaledTime-winSince-.9f)/1f);
            return normal;
        }
        AudioClip DesiredMusic()
        {
            var level=LevelManager.THIS;
            if(!level||level.gameStatus==GameState.Map) return clips[6];
            int i=((level.currentLevel-1)%LevelMusic.Length+LevelMusic.Length)%LevelMusic.Length;
            if(!levelTried[i]) { levelTried[i]=true; levelClips[i]=Resources.Load<AudioClip>("RoyalGameplay/Music/"+LevelMusic[i]); }
            return levelClips[i]?levelClips[i]:clips[6];
        }
        void StartFade(AudioClip clip)
        {
            var next=music[1-musicIndex]; next.clip=clip; next.loop=clip!=clips[6]; next.time=0; next.volume=0; next.Play(); fade=0;
        }
        void Update()
        {
            if(voices==null)return;
            int count=0; bool soundOff=PlayerPrefs.GetInt("Sound",1)==0;   // one read per frame, not one per voice
            foreach(var v in voices){ if(v.isPlaying)count++; if(soundOff)v.Stop(); }
            PeakVoices=Mathf.Max(PeakVoices,count);
            BalanceVoices();
            if(!clips[6])return;
            duck=Mathf.Max(0,duck-Time.unscaledDeltaTime);
            float target=MusicTarget(PlayerPrefs.GetInt("Music",1)*RoyalEffectsProfile.Get.musicVolume*(duck>0?.55f:1));
            float duration=RoyalEffectsProfile.Get.musicCrossfade;
            var current=music[musicIndex]; var want=DesiredMusic();
            if(fade<0&&want&&current.clip!=want)
            {
                // Entering a level or coming Home crossfades to that place's music once its data has loaded in the background.
                if(want.loadState==AudioDataLoadState.Unloaded) want.LoadAudioData();
                if(want.loadState==AudioDataLoadState.Loaded) StartFade(want);
            }
            // The castle theme loops by crossfading into itself; level tracks are seamless loops and repeat on their own.
            else if(fade<0&&!current.loop&&current.clip&&current.time>=current.clip.length-duration) StartFade(current.clip);
            if(fade>=0)
            {
                fade+=Time.unscaledDeltaTime; float t=Mathf.Clamp01(fade/duration);
                music[musicIndex].volume=Mathf.Cos(t*Mathf.PI*.5f)*target;
                music[1-musicIndex].volume=Mathf.Sin(t*Mathf.PI*.5f)*target;
                if(t>=1)
                {
                    var old=music[musicIndex]; old.Stop();
                    if(old.clip&&old.clip!=clips[6]&&old.clip!=music[1-musicIndex].clip) old.clip.UnloadAudioData();
                    musicIndex=1-musicIndex; fade=-1;
                }
            }
            else { music[musicIndex].volume=Mathf.MoveTowards(music[musicIndex].volume,target,Time.unscaledDeltaTime); music[1-musicIndex].volume=0; if(!music[musicIndex].isPlaying)music[musicIndex].Play(); }
        }
    }
}

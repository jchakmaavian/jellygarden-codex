#if UNITY_EDITOR && !ROYAL_DISABLE_QA
using System;
using System.IO;
using Unity.Collections;
using UnityEngine;
namespace RoyalThrone.Boosters.Propeller.V5.Editor
{
    // Records Unity's actual audio mix, on the same capture clock as the rendered frames.
    public static class PropellerCapture
    {
        static BinaryWriter audio; static string folder; static int frame,channels; static long samples;
        public static float Peak {get;private set;} public static int Clipped {get;private set;}
        public static void Begin(string path)
        {
            End();folder=path;Directory.CreateDirectory(folder);frame=0;samples=0;Peak=0;Clipped=0;
            channels=AudioSettings.speakerMode==AudioSpeakerMode.Mono?1:2;
            if(AudioSettings.speakerMode!=AudioSpeakerMode.Stereo&&channels!=1)throw new Exception("Capture requires mono or stereo output.");
            if(!AudioRenderer.Start())throw new Exception("Unity audio recording could not start.");
            audio=new BinaryWriter(File.Create(folder+"/audio.wav"));audio.Write(new byte[44]);
        }
        public static void Frame()
        {
            var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(folder+"/"+(frame++).ToString("D4")+".jpg",texture.EncodeToJPG(88));UnityEngine.Object.Destroy(texture);
            int n=AudioRenderer.GetSampleCountForCaptureFrame()*channels;
            using(var buffer=new NativeArray<float>(n,Allocator.Temp)){
                if(!AudioRenderer.Render(buffer))throw new Exception("Unity audio render failed.");
                foreach(float value in buffer){Peak=Mathf.Max(Peak,Mathf.Abs(value));if(Mathf.Abs(value)>=1)Clipped++;audio.Write((short)Mathf.RoundToInt(Mathf.Clamp(value,-1,1)*32767));samples++;}
            }
        }
        public static void End()
        {
            if(audio==null)return;AudioRenderer.Stop();int rate=AudioSettings.outputSampleRate;audio.Seek(0,SeekOrigin.Begin);
            audio.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));audio.Write((int)samples*2+36);audio.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));audio.Write(16);audio.Write((short)1);audio.Write((short)channels);audio.Write(rate);audio.Write(rate*channels*2);audio.Write((short)(channels*2));audio.Write((short)16);audio.Write(System.Text.Encoding.ASCII.GetBytes("data"));audio.Write((int)samples*2);audio.Dispose();audio=null;
            File.WriteAllText(folder+"/capture.json",JsonUtility.ToJson(new Info{frames=frame,channels=channels,sampleRate=rate,samples=samples,peak=Peak,clipped=Clipped},true));
        }
        [Serializable] class Info{public int frames,channels,sampleRate,clipped;public long samples;public float peak;}
    }
}
#endif

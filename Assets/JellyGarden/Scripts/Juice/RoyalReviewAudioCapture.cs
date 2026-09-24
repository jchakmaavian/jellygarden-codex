#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RoyalThrone.Editor
{
    // Review capture only. Never included in a player or used for performance measurement.
    public sealed class RoyalReviewAudioCapture : MonoBehaviour
    {
        readonly object gate=new object();
        BinaryWriter audio;byte[] pcm;int channels=2,bytes;bool running;
        float started;int frame;readonly List<string> timeline=new List<string>();
        const string Folder="Library/RoyalEffectsCapture/";
        public static RoyalReviewAudioCapture Begin()
        {
            var listener=UnityEngine.Object.FindAnyObjectByType<AudioListener>();if(!listener)return null;
            var capture=listener.gameObject.AddComponent<RoyalReviewAudioCapture>();capture.StartCapture();return capture;
        }
        void StartCapture()
        {
            Directory.CreateDirectory(Folder);audio=new BinaryWriter(File.Create(Folder+"unity-audio.wav"));audio.Write(new byte[44]);
            started=Time.realtimeSinceStartup;running=true;StartCoroutine(Frames());
        }
        void OnAudioFilterRead(float[] data,int count)
        {
            lock(gate)
            {
                if(!running||audio==null)return;channels=count;
                if(pcm==null||pcm.Length!=data.Length*2)pcm=new byte[data.Length*2];
                for(int i=0;i<data.Length;i++){short sample=(short)Mathf.Clamp(data[i]*32767,-32768,32767);pcm[i*2]=(byte)sample;pcm[i*2+1]=(byte)(sample>>8);}
                audio.Write(pcm);bytes+=pcm.Length;
            }
        }
        IEnumerator Frames()
        {
            while(running)
            {
                yield return new WaitForEndOfFrame();if(!running)yield break;
                var tex=ScreenCapture.CaptureScreenshotAsTexture();string name="frame-"+(frame++).ToString("D5")+".jpg";
                File.WriteAllBytes(Folder+name,tex.EncodeToJPG(85));Destroy(tex);
                timeline.Add(name+","+(Time.realtimeSinceStartup-started).ToString("F6",System.Globalization.CultureInfo.InvariantCulture));
                yield return new WaitForSecondsRealtime(1f/15);
            }
        }
        public void End()
        {
            StopAllCoroutines();lock(gate)
            {
                running=false;if(audio!=null)
                {
                    audio.Seek(0,SeekOrigin.Begin);audio.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));audio.Write(bytes+36);audio.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));audio.Write(16);audio.Write((short)1);audio.Write((short)channels);audio.Write(AudioSettings.outputSampleRate);audio.Write(AudioSettings.outputSampleRate*channels*2);audio.Write((short)(channels*2));audio.Write((short)16);audio.Write(System.Text.Encoding.ASCII.GetBytes("data"));audio.Write(bytes);audio.Dispose();audio=null;
                }
            }
            File.WriteAllLines(Folder+"frames.csv",timeline);Debug.Log("[Royal Review] Captured "+frame+" frames and "+bytes+" PCM bytes.");Destroy(this);
        }
        void OnDisable(){if(running)End();}
    }
}
#endif

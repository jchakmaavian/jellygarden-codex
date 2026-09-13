using System;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RoyalThrone.Editor
{
    public sealed class RoyalEffectsWorkshop : EditorWindow
    {
        const string PathRoot="Assets/JellyGarden/Resources/RoyalGameplay/";
        UnityEditor.Editor profileEditor;
        Vector2 scroll;
        [MenuItem("Tools/Royal Throne/Effects/Workshop #F9")]
        public static void Open()=>GetWindow<RoyalEffectsWorkshop>("Royal Effects");
        void OnGUI()
        {
            scroll=EditorGUILayout.BeginScrollView(scroll);
            GUILayout.Label("ROYAL THRONE • EFFECTS WORKSHOP",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Edit the profile, then preview in Play Mode. Preview buttons do not spend moves, lives or coins. The same effect rig drives real board impacts.",MessageType.Info);
            if(GUILayout.Button("Select effects prefab"))Selection.activeObject=AssetDatabase.LoadAssetAtPath<GameObject>(PathRoot+"EffectsRig.prefab");
            using(new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
            {
                foreach(string s in new[]{"Match 3","Match 4","Rocket","TNT","Helicopter"})if(GUILayout.Button("Preview "+s))
                {var camera=Camera.main;var p=camera?camera.ViewportToWorldPoint(new Vector3(.5f,.52f,10)):Vector3.zero;RoyalPowerupDirector.Get?.Preview(s,p);}
            }
            var profile=AssetDatabase.LoadAssetAtPath<RoyalEffectsProfile>(PathRoot+"EffectsProfile.asset");
            if(profile){UnityEditor.Editor.CreateCachedEditor(profile,null,ref profileEditor);profileEditor.OnInspectorGUI();}
            if(GUILayout.Button("Save profile"))AssetDatabase.SaveAssets();
            EditorGUILayout.EndScrollView();
        }
        [MenuItem("Tools/Royal Throne/Effects/Build royal effects and piece art #F7")]
        public static void Build()
        {
            RoyalHomeScreenBuilder.RequireCopy();
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
            AssetDatabase.Refresh();
            var profile=AssetDatabase.LoadAssetAtPath<RoyalEffectsProfile>(PathRoot+"EffectsProfile.asset");
            if(!profile){profile=ScriptableObject.CreateInstance<RoyalEffectsProfile>();AssetDatabase.CreateAsset(profile,PathRoot+"EffectsProfile.asset");}
            foreach(string path in Directory.GetFiles(PathRoot+"Audio"))
            {
                if(!(AssetImporter.GetAtPath(path.Replace('\\','/')) is AudioImporter importer))continue;
                bool music=path.Contains("castle-theme");var s=importer.defaultSampleSettings;
                s.compressionFormat=music?AudioCompressionFormat.Vorbis:AudioCompressionFormat.ADPCM;
                s.loadType=music?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;s.quality=.55f;
                // SFX sources are already mono and mastered; avoid automatic downmix normalization.
                s.preloadAudioData=!music;importer.forceToMono=false;importer.defaultSampleSettings=s;importer.SaveAndReimport();
            }
            Texture2D SaveTexture(string name,Texture2D source)
            {
                string path=PathRoot+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var rt=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;
                var copy=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false);copy.name=name;
                try{Graphics.Blit(source,rt);RenderTexture.active=rt;copy.ReadPixels(new Rect(0,0,source.width,source.height),0,0);copy.Apply();}
                finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);}
                if(old){EditorUtility.CopySerialized(copy,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(copy);return old;}
                AssetDatabase.CreateAsset(copy,path);return copy;
            }
            var glow=SaveTexture("ImpactShape",SparkleTextureFactory.SoftGlow);
            var star=SaveTexture("SparkleShape",SparkleTextureFactory.FourPointSparkle);
            var ring=SaveTexture("RingShape",SparkleTextureFactory.Ring);
            var diamond=SaveTexture("EmberShape",SparkleTextureFactory.Diamond);
            Material Mat(string name,Texture tex,bool additive)
            {var path=PathRoot+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=additive?new Material(Resources.Load<Material>("JuiceAdditive")):new Material(Shader.Find("RoyalThrone/Particle Alpha"));AssetDatabase.CreateAsset(m,path);}m.mainTexture=tex;if(additive){m.DisableKeyword("SPRITE_RENDERER_COLOR");m.SetFloat("_UseRendererColor",0);m.SetFloat("_Boost",1.25f);}EditorUtility.SetDirty(m);return m;}
            var smokeTexture=AssetDatabase.LoadAllAssetsAtPath(PathRoot+"smoke.webp").OfType<Texture2D>().First();
            var root=new GameObject("Royal Effects Rig");
            void System(string name,Material material,int cap,int order,float gravity,AnimationCurve sizes)
            {
                var go=new GameObject(name);go.transform.SetParent(root.transform,false);var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=ps.main;main.loop=true;main.playOnAwake=true;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=cap;main.startSpeed=0;main.startSize=1;main.startLifetime=.4f;main.gravityModifier=gravity;main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
                var emission=ps.emission;emission.rateOverTime=0;var shape=ps.shape;shape.enabled=false;
                var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,sizes);
                var col=ps.colorOverLifetime;col.enabled=true;var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.95f,.2f),new GradientAlphaKey(0,1)});col.color=g;
                var drag=ps.limitVelocityOverLifetime;drag.enabled=true;drag.limit=100;drag.dampen=0;drag.drag=2;
                var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=order;renderer.alignment=ParticleSystemRenderSpace.View;
            }
            System("Smoke",Mat("Smoke",smokeTexture,false),96,26,.08f,new AnimationCurve(new Keyframe(0,.35f),new Keyframe(.2f,1),new Keyframe(1,1.4f)));
            System("Sparkles",Mat("Sparkles",star,true),profile.sparkleBudget,35,0,new AnimationCurve(new Keyframe(0,.4f),new Keyframe(.15f,1),new Keyframe(.6f,.5f),new Keyframe(1,0)));
            System("Embers",Mat("Embers",diamond,true),256,30,.4f,AnimationCurve.Linear(0,1,1,0));
            System("Impact",Mat("Impact",glow,true),24,34,0,AnimationCurve.EaseInOut(0,.4f,1,1.4f));
            System("Shockwave",Mat("Shockwave",ring,true),24,29,0,AnimationCurve.EaseInOut(0,.2f,1,3.3f));
            Mat("Trail",glow,true);root.AddComponent<RoyalPowerupDirector>();PrefabUtility.SaveAsPrefabAsset(root,PathRoot+"EffectsRig.prefab");UnityEngine.Object.DestroyImmediate(root);
            string itemPath="Assets/JellyGarden/Resources/Prefabs/Item.prefab";var item=PrefabUtility.LoadPrefabContents(itemPath);
            if(!item.GetComponent<RoyalPieceSkin>())item.AddComponent<RoyalPieceSkin>();
            PrefabUtility.SaveAsPrefabAsset(item,itemPath);PrefabUtility.UnloadPrefabContents(item);
            var juice=Resources.Load<JuiceConfig>("JuiceConfig");
            juice.EnableHitStop=false;juice.StarSize=.29f;juice.StarParticleCount=5;juice.MicroParticleCount=7;juice.MicroSize=.11f;juice.FragmentCount=3;juice.FragmentSize=.15f;
            juice.ParticleLifetime=.25f;juice.ParticleVelocity=4.8f;juice.FlashEndScale=1.25f;juice.FlashDuration=.065f;juice.CenterStarCount=3;juice.CenterRingEndScale=1.15f;juice.ShatterLifetime=.32f;juice.ShatterBurstSpeed=2.1f;
            juice.MatchAnticipation=.02f;juice.MatchSquashDuration=.035f;juice.MatchReboundDuration=.02f;juice.MatchDestroyDuration=.06f;juice.MatchStagger=.014f;juice.FallSpeed=12;juice.FallAcceleration=55;juice.FallMaxSpeed=32;
            EditorUtility.SetDirty(juice);AssetDatabase.SaveAssets();
            Debug.Log("[Royal Effects] Built editable particle rig, profile, WebP piece skins and compressed audio. Shift+F9 opens the workshop.");
        }
    }
}

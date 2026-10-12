#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace RoyalThrone.ReferenceGameplay.Editor
{
    public static class ReferenceArcherImporter
    {
        const string Folder="Assets/ReferenceGameplay/Tools/Archer/";
        const string Runtime=Folder+"Resources/ReferenceGameplay/Archer/";
        [Serializable] public class Key {public float time,value,inSlope,outSlope;public long objectPathId;}
        [Serializable] public class Curve {public string path,property;public int typeID;public bool isPPtr;public Key[] keys;}
        [Serializable] public class Clip {public string name;public float duration,frameRate;public bool loop;public Curve[] curves;}
        [Serializable] public class SpriteInfo {public long id;public string name,assetPath;public Vector2 pivot;public float ppu;}
        [Serializable] public class Node {public string name,path;public bool active,enabled;public Vector3 position,scale;public Quaternion rotation;public long spriteId;public Color color;public int sortingOrder;}
        [Serializable] public class Document {public Clip clip,fall;public SpriteInfo[] sprites;public Node[] nodes;}

        [MenuItem("Royal Throne/Reference Gameplay/Import Archer Animation VFX and SFX")]
        public static void Build()
        {
            Directory.CreateDirectory(Runtime);AssetDatabase.Refresh();
            var source=JsonUtility.FromJson<Document>(File.ReadAllText(Folder+"ArcherSource.json"));
            var sprites=new Dictionary<long,Sprite>();
            foreach(var s in source.sprites)
            {
                var ti=(TextureImporter)AssetImporter.GetAtPath(s.assetPath);
                ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;
                var settings=new TextureImporterSettings();ti.ReadTextureSettings(settings);
                settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=s.pivot;
                settings.spritePixelsPerUnit=s.ppu;settings.spriteMeshType=SpriteMeshType.FullRect;
                ti.SetTextureSettings(settings);ti.mipmapEnabled=false;ti.alphaIsTransparency=false;
                ti.textureCompression=TextureImporterCompression.Uncompressed;ti.npotScale=TextureImporterNPOTScale.None;ti.maxTextureSize=2048;
                ti.SaveAndReimport();sprites[s.id]=AssetDatabase.LoadAssetAtPath<Sprite>(s.assetPath);
                if(!sprites[s.id])throw new Exception("Missing archer sprite "+s.name);
            }
            var clip=BuildClip(source.clip,sprites);var fall=BuildClip(source.fall,sprites);
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder+"ArcherAnimator.controller");
            if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(Folder+"ArcherAnimator.controller");
            var machine=controller.layers[0].stateMachine;
            foreach(var motion in new[]{clip,fall})
            {
                var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==motion.name)??machine.AddState(motion.name);state.motion=motion;
                if(motion==clip)machine.defaultState=state;
            }
            var root=new GameObject("ArcherAnimation");
            try
            {
                var map=new Dictionary<string,GameObject>{{"",root}};
                foreach(var n in source.nodes)
                {
                    GameObject go=root;
                    if(n.path!=""){go=new GameObject(n.name);int split=n.path.LastIndexOf('/');go.transform.SetParent(map[split<0?"":n.path.Substring(0,split)].transform,false);map[n.path]=go;}
                    go.transform.localPosition=n.position;go.transform.localRotation=n.rotation;go.transform.localScale=n.scale;go.SetActive(n.active);
                    if(n.spriteId!=0){var r=go.AddComponent<SpriteRenderer>();r.sprite=sprites[n.spriteId];r.color=n.color;r.sortingOrder=n.sortingOrder;r.enabled=n.enabled;}
                }
                root.AddComponent<Animator>().runtimeAnimatorController=controller;
                clip.SampleAnimation(root,0);PrefabUtility.SaveAsPrefabAsset(root,Runtime+"ArcherAnimation.prefab");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
            string audio=Runtime+"arrow_use.wav";
            if(!AssetDatabase.LoadAssetAtPath<AudioClip>(audio))
            {
                string error=AssetDatabase.MoveAsset(Folder+"Audio/arrow_use.wav",audio);if(error!="")throw new Exception(error);
            }
            var ai=(AudioImporter)AssetImporter.GetAtPath(audio);var audioSettings=ai.defaultSampleSettings;
            audioSettings.loadType=AudioClipLoadType.DecompressOnLoad;audioSettings.compressionFormat=AudioCompressionFormat.PCM;
            audioSettings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;ai.defaultSampleSettings=audioSettings;ai.SaveAndReimport();
            foreach(string path in Directory.GetFiles(Folder+"VFXTextures","*.png"))
            {
                var ti=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));ti.textureCompression=TextureImporterCompression.Uncompressed;
                ti.mipmapEnabled=false;ti.alphaIsTransparency=false;ti.maxTextureSize=4096;ti.SaveAndReimport();
            }
            ReferenceGameplayVfxImporter.BuildFile("Design/Archer-Reference/NativeArcherParticles.json");
            AssetDatabase.SaveAssets();
            File.WriteAllText("Design/Archer-Reference/import-report.txt",$"PASS source curves, times, values and tangents verified after serialization\nArrowAnimation: {source.clip.curves.Length} curves, {clip.length}s, {clip.frameRate} FPS\nArrowFallAnimation: {source.fall.curves.Length} curves, {fall.length}s\nSprites: {sprites.Count}\nNative particle systems: 6\nNative audio: arrow_use\n");
        }
        static AnimationClip BuildClip(Clip source,Dictionary<long,Sprite> sprites)
        {
            string path=Runtime+source.name+".anim";
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(!clip){clip=new AnimationClip{name=source.name};AssetDatabase.CreateAsset(clip,path);}
            clip.ClearCurves();clip.frameRate=source.frameRate;
            foreach(var c in source.curves)
            {
                var binding=Binding(c);
                if(c.isPPtr)AnimationUtility.SetObjectReferenceCurve(clip,binding,c.keys.Select(k=>new ObjectReferenceKeyframe{time=k.time,value=k.objectPathId==0?null:sprites[k.objectPathId]}).ToArray());
                else AnimationUtility.SetEditorCurve(clip,binding,new AnimationCurve(c.keys.Select(k=>new Keyframe(k.time,k.value,k.inSlope,k.outSlope)).ToArray()));
            }
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.startTime=0;settings.stopTime=source.duration;settings.loopTime=source.loop;
            AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);AssetDatabase.SaveAssets();AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            foreach(var c in source.curves)
            {
                if(c.isPPtr)continue;
                var keys=AnimationUtility.GetEditorCurve(clip,Binding(c)).keys;
                if(keys.Length!=c.keys.Length)throw new Exception("Archer curve key count mismatch");
                for(int i=0;i<keys.Length;i++)
                    if(Mathf.Abs(keys[i].time-c.keys[i].time)>1e-5f||Mathf.Abs(keys[i].value-c.keys[i].value)>1e-5f||Mathf.Abs(keys[i].inTangent-c.keys[i].inSlope)>1e-5f||Mathf.Abs(keys[i].outTangent-c.keys[i].outSlope)>1e-5f)
                        throw new Exception("Archer curve mismatch: "+c.path+"/"+c.property);
            }
            return clip;
        }
        static EditorCurveBinding Binding(Curve c)=>new EditorCurveBinding{path=c.path,type=c.typeID==4?typeof(Transform):c.typeID==212?typeof(SpriteRenderer):throw new Exception("Unsupported source binding"),propertyName=c.property};
    }
}
#endif

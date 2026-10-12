#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>Imports decoded native reference curves and render-only hierarchies.
/// Source gameplay scripts and callbacks are deliberately excluded.</summary>
public static class ReferenceGameplayMediaImporter
{
    const string Base = "Assets/ReferenceGameplay/Animation/";
    [Serializable] public class Document { public PrefabData[] prefabs; public ClipData[] clips; public ControllerData[] controllers; public long[] missingSprites; }
    [Serializable] public class PrefabData { public string name,animatorPath; public long controllerPathId; public Node[] nodes; }
    [Serializable] public class Node { public long pathId,transformId,parentTransformId; public string name; public bool active; public Vector3 localPosition,localScale; public Quaternion localRotation; public ComponentData[] components; }
    [Serializable] public class ComponentData { public long pathId; public string type,json; }
    [Serializable] public class ClipData { public string name; public long pathId; public float duration,frameRate; public bool loop; public CurveData[] curves; public MissingBinding[] missingBindings; }
    [Serializable] public class MissingBinding { public uint path,attribute; public int typeID; }
    [Serializable] public class CurveData { public string path,property; public int typeID; public bool isPPtr; public KeyData[] keys; }
    [Serializable] public class KeyData { public float time,value,inSlope,outSlope; public long objectPathId; }
    [Serializable] public class ControllerData { public string name; public long pathId; public int defaultState; public StateData[] states; }
    [Serializable] public class StateData { public string name; public long clipPathId; public float speed; public TransitionData[] transitions; }
    [Serializable] public class TransitionData { public TransitionBody data; }
    [Serializable] public class TransitionBody { public uint m_DestinationState; public float m_TransitionDuration,m_TransitionOffset,m_ExitTime; public bool m_HasExitTime,m_HasFixedDuration; }
    [Serializable] public class ObjectReference { public UnityEngine.Object value; }
    public sealed class ReferenceHolder : ScriptableObject { public UnityEngine.Object value; }
    static readonly Dictionary<UnityEngine.Object,string> PersistentReferences=new Dictionary<UnityEngine.Object,string>();
    public static string PersistentReferenceJson(UnityEngine.Object value)
    {
        if(!value)return "{\"fileID\":0}";
        if(PersistentReferences.TryGetValue(value,out var cached))return cached;
        if(!EditorUtility.IsPersistent(value))throw new InvalidOperationException("Unexpected transient native reference: "+value.name);
        var holder=ScriptableObject.CreateInstance<ReferenceHolder>();
        try
        {
            holder.value=value;
            var match=Regex.Match(EditorJsonUtility.ToJson(holder),"\\\"value\\\"\\s*:\\s*(\\{[^}]*\\})");
            if(!match.Success)throw new InvalidOperationException("Unable to serialize persistent native reference: "+value.name);
            return PersistentReferences[value]=match.Groups[1].Value;
        }
        finally {UnityEngine.Object.DestroyImmediate(holder);}
    }
    public static string RemoveComponentIdentity(string json)=>Regex.Replace(json,"\\\"m_GameObject\\\"\\s*:\\s*\\{[^}]*\\}\\s*,?","");
    [Serializable] public class SpriteDocument { public SpriteInfo[] sprites; }
    [Serializable] public class SpriteInfo { public long sourcePathId; public string assetPath; }
    static readonly Regex Ptr = new Regex(@"\{\s*""m_FileID""\s*:\s*(-?\d+)\s*,\s*""m_PathID""\s*:\s*(-?\d+)\s*\}");
    static string Project => Path.GetFullPath(Path.Combine(Application.dataPath,".."));

    [MenuItem("Royal Throne/Reference Gameplay/Import Native Media")]
    public static void Build()
    {
        var input=JsonUtility.FromJson<Document>(File.ReadAllText(Path.Combine(Project,"Design/ReferenceGameplayImport/Media/NativeVisuals.json")));
        var spriteData=JsonUtility.FromJson<SpriteDocument>(File.ReadAllText("Assets/ReferenceGameplay/Art/NativeSpriteCatalog.json"));
        Directory.CreateDirectory(Base+"Clips"); Directory.CreateDirectory(Base+"Controllers"); Directory.CreateDirectory(Base+"Resources/ReferenceGameplay/Visuals");
        AssetDatabase.Refresh();
        foreach(string path in Directory.GetFiles("Assets/ReferenceGameplay/Audio/Resources/ReferenceGameplay/Audio","*.wav"))
        {
            if(!(AssetImporter.GetAtPath(path.Replace('\\','/')) is AudioImporter audio))continue;
            var settings=audio.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
            audio.defaultSampleSettings=settings;audio.forceToMono=false;audio.SaveAndReimport();
        }
        var sprites=spriteData.sprites.ToDictionary(x=>x.sourcePathId,x=>AssetDatabase.LoadAssetAtPath<Sprite>(x.assetPath));
        var clips=new Dictionary<long,AnimationClip>();
        var notes=new List<string>();
        foreach(var source in input.clips)
        {
            string path=Base+"Clips/"+source.name+".anim";
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}else clip.ClearCurves();
            clip.name=source.name;clip.frameRate=source.frameRate;clip.legacy=false;
            foreach(var curve in source.curves)
            {
                var type=curve.typeID==4?typeof(Transform):curve.typeID==212?typeof(SpriteRenderer):curve.typeID==210?typeof(SortingGroup):curve.typeID==1?typeof(GameObject):null;
                if(type==null)throw new InvalidOperationException("Unsupported native curve type "+curve.typeID);
                var binding=new EditorCurveBinding {path=curve.path,type=type,propertyName=curve.property};
                if(curve.isPPtr)
                {
                    var keys=curve.keys.Select(k=>new ObjectReferenceKeyframe {time=k.time,value=k.objectPathId==0?null:sprites[k.objectPathId]}).ToArray();
                    AnimationUtility.SetObjectReferenceCurve(clip,binding,keys);
                }
                else
                {
                    var keys=curve.keys.Select(k=>new Keyframe(k.time,k.value,k.inSlope,k.outSlope)).ToArray();
                    var anim=new AnimationCurve(keys);
                    if(curve.property=="m_IsActive" || curve.property=="m_Enabled" || curve.property=="m_SortingOrder")
                        for(int k=0;k<keys.Length;k++){AnimationUtility.SetKeyLeftTangentMode(anim,k,AnimationUtility.TangentMode.Constant);AnimationUtility.SetKeyRightTangentMode(anim,k,AnimationUtility.TangentMode.Constant);}
                    AnimationUtility.SetEditorCurve(clip,binding,anim);
                }
            }
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.startTime=0;settings.stopTime=source.duration;settings.loopTime=source.loop;AnimationUtility.SetAnimationClipSettings(clip,settings);
            // No source gameplay event receiver is imported, preventing duplicate hits/conversions.
            AnimationUtility.SetAnimationEvents(clip,Array.Empty<AnimationEvent>());
            EditorUtility.SetDirty(clip);clips[source.pathId]=clip;
            foreach(var missing in source.missingBindings)notes.Add(source.name+": source binding has no matching packaged child/property: path hash "+missing.path+", attribute "+missing.attribute);
        }
        var controllers=new Dictionary<long,AnimatorController>();
        foreach(var source in input.controllers)
        {
            string path=Base+"Controllers/"+source.name+".controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(path);
            var machine=controller.layers[0].stateMachine;
            foreach(var st in machine.states)machine.RemoveState(st.state);
            var states=new AnimatorState[source.states.Length];
            for(int i=0;i<states.Length;i++)
            {
                var native=source.states[i];states[i]=machine.AddState(native.name);states[i].motion=clips[native.clipPathId];states[i].speed=native.speed;states[i].writeDefaultValues=true;
            }
            machine.defaultState=states[source.defaultState];
            for(int i=0;i<states.Length;i++)foreach(var tr in source.states[i].transitions)
            {
                var t=tr.data;if(t.m_DestinationState>=states.Length){notes.Add(source.name+": unresolved transition "+t.m_DestinationState);continue;}
                var transition=states[i].AddTransition(states[t.m_DestinationState]);transition.hasExitTime=t.m_HasExitTime;transition.exitTime=t.m_ExitTime;transition.hasFixedDuration=t.m_HasFixedDuration;transition.duration=t.m_TransitionDuration;transition.offset=t.m_TransitionOffset;
            }
            EditorUtility.SetDirty(controller);controllers[source.pathId]=controller;
        }
        var defaultMaterial=AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
        foreach(var prefab in input.prefabs)
        {
            var objects=new Dictionary<long,UnityEngine.Object>();var nodes=new Dictionary<long,GameObject>();
            foreach(var n in prefab.nodes)
            {
                var go=new GameObject(n.name);nodes[n.transformId]=go;objects[n.pathId]=go;objects[n.transformId]=go.transform;
            }
            var root=nodes[prefab.nodes[0].transformId];
            try
            {
                foreach(var n in prefab.nodes)
                {
                    var go=nodes[n.transformId];var tr=go.transform;
                    if(n.parentTransformId!=0)tr.SetParent(nodes[n.parentTransformId].transform,false);
                    tr.localPosition=n.localPosition;tr.localRotation=n.localRotation;tr.localScale=n.localScale;
                    foreach(var c in n.components)
                    {
                        if(c.type=="SpriteRenderer")objects[c.pathId]=go.AddComponent<SpriteRenderer>();
                        else if(c.type=="SpriteMask")objects[c.pathId]=go.AddComponent<SpriteMask>();
                        else if(c.type=="SortingGroup")objects[c.pathId]=go.AddComponent<SortingGroup>();
                        else if(c.type=="Animator")objects[c.pathId]=go.AddComponent<Animator>();
                    }
                }
                string Remap(string json)=>Ptr.Replace(RemoveComponentIdentity(json),m=>
                {
                    int f=int.Parse(m.Groups[1].Value);long id=long.Parse(m.Groups[2].Value);UnityEngine.Object value=null;
                    if(id!=0){if(f==1&&id==2)value=defaultMaterial;else if(f==0){if(objects.TryGetValue(id,out var obj))value=obj;else if(sprites.TryGetValue(id,out var sprite))value=sprite;else if(controllers.TryGetValue(id,out var ctrl))value=ctrl;}}
                    return PersistentReferenceJson(value);
                });
                foreach(var n in prefab.nodes)
                {
                    foreach(var c in n.components)
                    {
                        if(c.type=="Transform"||!objects.TryGetValue(c.pathId,out var component))continue;
                        EditorJsonUtility.FromJsonOverwrite("{\""+component.GetType().Name+"\":"+Remap(c.json)+"}",component);
                        if(component is Renderer r)r.sortingLayerID=0;
                        if(component is SortingGroup sg)sg.sortingLayerID=0;
                        if(component is Animator a){a.runtimeAnimatorController=controllers[prefab.controllerPathId];a.cullingMode=AnimatorCullingMode.AlwaysAnimate;a.applyRootMotion=false;}
                    }
                    nodes[n.transformId].SetActive(n.active);
                }
                root.name=prefab.name;root.transform.localPosition=Vector3.zero;root.transform.localRotation=Quaternion.identity;root.transform.localScale=Vector3.one;
                root.SetActive(true);
                PrefabUtility.SaveAsPrefabAsset(root,Base+"Resources/ReferenceGameplay/Visuals/"+prefab.name+".prefab");
            }
            finally {UnityEngine.Object.DestroyImmediate(root);}
        }
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        File.WriteAllLines(Path.Combine(Project,"Design/ReferenceGameplayImport/Media/ImportNotes.txt"),notes);
        Debug.Log("REFERENCE_MEDIA_IMPORT_COMPLETE: "+input.prefabs.Length+" render prefabs; "+clips.Count+" native clips; "+notes.Count+" flagged source limitations.");
    }
}
#endif

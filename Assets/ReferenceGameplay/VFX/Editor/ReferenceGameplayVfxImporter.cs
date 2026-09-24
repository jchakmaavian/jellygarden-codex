#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

/// <summary>Imports native ParticleSystem modules and renderer settings with original atlas sprites.</summary>
public static class ReferenceGameplayVfxImporter
{
    const string Base="Assets/ReferenceGameplay/VFX/";
    [Serializable] public class Document {public Effect[] prefabs; public MaterialData[] materials;public TextureData[] textures;public SpriteData[] sprites;}
    [Serializable] public class Effect {public string name;public ReferenceGameplayMediaImporter.Node[] nodes;}
    [Serializable] public class MaterialData {public long pathId;public string name,shaderName,keywords;public int queue;public TextureProperty[] textures;public FloatProperty[] floats;public ColorProperty[] colors;}
    [Serializable] public class TextureProperty {public string name;public long pathId;public Vector2 scale,offset;}
    [Serializable] public class FloatProperty {public string name;public float value;}
    [Serializable] public class ColorProperty {public string name;public Color color;}
    [Serializable] public class TextureData {public long pathId;public string name,assetPath;}
    [Serializable] public class SpriteData {public long sourcePathId,sourceTexturePathId;public string name;public RectData rect;public Vector2 pivot;public Vector4 border;public float pixelsPerUnit;}
    [Serializable] public class RectData {public float x,y,width,height;}
    static readonly Regex Ptr=new Regex(@"\{\s*""m_FileID""\s*:\s*(-?\d+)\s*,\s*""m_PathID""\s*:\s*(-?\d+)\s*\}");
    [MenuItem("Royal Throne/Reference Gameplay/Import Native VFX")]
    public static void Build()
    {
        string project=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
        var source=JsonUtility.FromJson<Document>(File.ReadAllText(Path.Combine(project,"Design/ReferenceGameplayImport/Media/NativeParticles.json")));
        var incomplete=new List<string>();
        Directory.CreateDirectory(Base+"AtlasSprites");Directory.CreateDirectory(Base+"Materials");Directory.CreateDirectory(Base+"Resources/ReferenceGameplay/VFX");AssetDatabase.Refresh();
        var references=new Dictionary<long,UnityEngine.Object>();
        foreach(var t in source.textures)
        {
            if(AssetImporter.GetAtPath(t.assetPath) is TextureImporter ti && t.assetPath.StartsWith(Base))
            {ti.textureCompression=TextureImporterCompression.Uncompressed;ti.mipmapEnabled=false;ti.maxTextureSize=4096;ti.alphaIsTransparency=false;ti.SaveAndReimport();}
            references[t.pathId]=AssetDatabase.LoadAssetAtPath<Texture2D>(t.assetPath);
        }
        foreach(var s in source.sprites)
        {
            string path=Base+"AtlasSprites/"+s.sourcePathId+".asset";
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(!sprite)
            {
                var r=s.rect;sprite=Sprite.Create((Texture2D)references[s.sourceTexturePathId],new Rect(r.x,r.y,r.width,r.height),s.pivot,s.pixelsPerUnit,0,SpriteMeshType.FullRect,s.border);
                sprite.name=s.name;AssetDatabase.CreateAsset(sprite,path);
            }
            references[s.sourcePathId]=sprite;
        }
        foreach(var m in source.materials)
        {
            var shader=Shader.Find(m.shaderName);
            if(!shader)throw new InvalidOperationException("Original native shader unavailable: "+m.shaderName);
            string path=Base+"Materials/"+m.pathId+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat){mat=new Material(shader);AssetDatabase.CreateAsset(mat,path);}else mat.shader=shader;
            mat.name=m.name;mat.renderQueue=m.queue;mat.shaderKeywords=(m.keywords??"").Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);
            foreach(var p in m.floats)if(mat.HasProperty(p.name))mat.SetFloat(p.name,p.value);
            foreach(var p in m.colors)if(mat.HasProperty(p.name))mat.SetColor(p.name,p.color);
            foreach(var p in m.textures)if(mat.HasProperty(p.name)){mat.SetTexture(p.name,p.pathId!=0?(Texture)references[p.pathId]:null);mat.SetTextureScale(p.name,p.scale);mat.SetTextureOffset(p.name,p.offset);}
            EditorUtility.SetDirty(mat);references[m.pathId]=mat;
        }
        var spriteMaterial=AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
        foreach(var prefab in source.prefabs)
        {
            var map=new Dictionary<long,UnityEngine.Object>(references);var nodes=new Dictionary<long,GameObject>();
            foreach(var n in prefab.nodes){var go=new GameObject(n.name);nodes[n.transformId]=go;map[n.pathId]=go;map[n.transformId]=go.transform;}
            var root=nodes[prefab.nodes[0].transformId];
            try
            {
                foreach(var n in prefab.nodes)
                {
                    var go=nodes[n.transformId];if(n.parentTransformId!=0)go.transform.SetParent(nodes[n.parentTransformId].transform,false);
                    go.transform.localPosition=n.localPosition;go.transform.localRotation=n.localRotation;go.transform.localScale=n.localScale;
                    // ParticleSystem creates its renderer; preserve that single renderer rather than adding a duplicate.
                    foreach(var c in n.components)if(c.type=="ParticleSystem")map[c.pathId]=go.AddComponent<ParticleSystem>();
                    foreach(var c in n.components)
                    {
                        if(c.type=="ParticleSystemRenderer")map[c.pathId]=go.GetComponent<ParticleSystemRenderer>();
                        else if(c.type=="SpriteRenderer")map[c.pathId]=go.AddComponent<SpriteRenderer>();
                        else if(c.type=="LineRenderer")map[c.pathId]=go.AddComponent<LineRenderer>();
                        else if(c.type=="SortingGroup")map[c.pathId]=go.AddComponent<SortingGroup>();
                    }
                }
                string Remap(string json)=>Ptr.Replace(ReferenceGameplayMediaImporter.RemoveComponentIdentity(json),m=>
                {
                    int f=int.Parse(m.Groups[1].Value);long p=long.Parse(m.Groups[2].Value);UnityEngine.Object value=null;
                    if(p!=0){if(f==1&&p==2)value=spriteMaterial;else if(f==0)map.TryGetValue(p,out value);}
                    return ReferenceGameplayMediaImporter.PersistentReferenceJson(value);
                });
                foreach(var n in prefab.nodes)
                {
                    foreach(var c in n.components)
                    {
                        if(c.type=="Transform"||!map.TryGetValue(c.pathId,out var component)||!component)continue;
                        EditorJsonUtility.FromJsonOverwrite("{\""+component.GetType().Name+"\":"+Remap(c.json)+"}",component);
                        if(component is ParticleSystem ps){ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);var main=ps.main;main.stopAction=ParticleSystemStopAction.None;}
                        if(component is Renderer r)r.sortingLayerID=0;
                        if(component is SortingGroup sg)sg.sortingLayerID=0;
                    }
                    nodes[n.transformId].SetActive(n.active);
                }
                foreach(var ps in root.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var sheet=ps.textureSheetAnimation;
                    if(!sheet.enabled||sheet.mode!=ParticleSystemAnimationMode.Sprites)continue;
                    bool missing=false;for(int i=0;i<sheet.spriteCount;i++)if(!sheet.GetSprite(i))missing=true;
                    if(!missing)continue;
                    // Native PotionItemSingleExplodeParticles/Potion is a runtime-filled template.
                    // Keep its available child effects, but never render an invented white rectangle.
                    sheet.enabled=false;var emission=ps.emission;emission.enabled=false;
                    var renderer=ps.GetComponent<ParticleSystemRenderer>();if(renderer)renderer.enabled=false;
                    incomplete.Add(prefab.name+"/"+AnimationUtility.CalculateTransformPath(ps.transform,root.transform)+": source sprite entry is null; unconfigured template disabled, child effects retained.");
                }
                root.transform.localPosition=Vector3.zero;root.transform.localRotation=prefab.nodes[0].localRotation;root.transform.localScale=prefab.nodes[0].localScale;root.SetActive(true);
                PrefabUtility.SaveAsPrefabAsset(root,Base+"Resources/ReferenceGameplay/VFX/"+prefab.name+".prefab");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();File.WriteAllLines(Path.Combine(project,"Design/ReferenceGameplayImport/Media/VfxIncomplete.txt"),incomplete);Debug.Log("REFERENCE_VFX_IMPORT_COMPLETE: "+source.prefabs.Length+" native particle/beam hierarchies; "+incomplete.Count+" explicitly disabled source templates.");
    }
    [Serializable] public class VerifiedData {public List<VerifiedComponent> components=new List<VerifiedComponent>();}
    [Serializable] public class VerifiedComponent {public string prefab,path,type,json;public long sourcePathId;}
    public static void DumpImportedModules()
    {
        string project=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
        var source=JsonUtility.FromJson<Document>(File.ReadAllText(Path.Combine(project,"Design/ReferenceGameplayImport/Media/NativeParticles.json")));
        var result=new VerifiedData();
        foreach(var effect in source.prefabs)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Base+"Resources/ReferenceGameplay/VFX/"+effect.name+".prefab");
            if(!prefab)throw new Exception("Missing imported native effect "+effect.name);
            var byId=effect.nodes.ToDictionary(n=>n.transformId);
            var resolved=new Dictionary<long,Transform>();
            foreach(var node in effect.nodes)
            {
                var segments=new List<string>();var current=node;
                while(current.parentTransformId!=0){segments.Insert(0,current.name);current=byId[current.parentTransformId];}
                string path=string.Join("/",segments);Transform tr;
                if(node.parentTransformId==0)tr=prefab.transform;
                else
                {
                    var siblings=effect.nodes.Where(n=>n.parentTransformId==node.parentTransformId).ToArray();
                    int sibling=Array.IndexOf(siblings,node);tr=resolved[node.parentTransformId].GetChild(sibling);
                }
                resolved[node.transformId]=tr;
                if(!tr)throw new Exception("Missing native transform "+effect.name+"/"+path);
                foreach(var c in node.components)
                {
                    Component component=null;
                    if(c.type=="ParticleSystem")component=tr.GetComponent<ParticleSystem>();
                    else if(c.type=="ParticleSystemRenderer")component=tr.GetComponent<ParticleSystemRenderer>();
                    else if(c.type=="LineRenderer")component=tr.GetComponent<LineRenderer>();
                    if(component)result.components.Add(new VerifiedComponent {prefab=effect.name,path=path,type=c.type,sourcePathId=c.pathId,json=EditorJsonUtility.ToJson(component)});
                }
            }
        }
        File.WriteAllText(Path.Combine(project,"Design/ReferenceGameplayImport/Media/ImportedNativeModules.json"),JsonUtility.ToJson(result));
        Debug.Log("REFERENCE_VFX_NATIVE_MODULE_DUMP: "+result.components.Count+" components.");
    }
}
#endif

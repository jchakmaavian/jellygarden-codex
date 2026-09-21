#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
namespace RoyalThrone.Boosters.ColorBall.V2.Editor
{
    public static class ColorBallV2Builder
    {
        public const string Root="Assets/Boosters/ColorBall/V2/";
        const string Revision="sphere-contact-v1";
        [InitializeOnLoadMethod]static void Init(){EditorApplication.delayCall+=Ensure;}
        static void Ensure(){if(!EditorApplication.isPlayingOrWillChangePlaymode&&(!File.Exists(Root+"Assets/revision.txt")||File.ReadAllText(Root+"Assets/revision.txt")!=Revision))Build();}
        [MenuItem("Tools/Royal Throne/Color Ball V2/Build prefabs")]
        public static void Build(){
            var sphere=Shader.Find("RoyalThrone/ColorBallV2/Sphere");var light=Shader.Find("RoyalThrone/ColorBallV2/Light");if(!sphere||!light)return;
            var quad=new Mesh{name="Color Ball quad"};quad.vertices=new[]{new Vector3(-.5f,-.5f),new Vector3(.5f,-.5f),new Vector3(.5f,.5f),new Vector3(-.5f,.5f)};quad.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};quad.triangles=new[]{0,1,2,0,2,3};quad.RecalculateBounds();
            var path=Root+"Assets/Quad.asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(quad,old);Object.DestroyImmediate(quad);quad=old;}else AssetDatabase.CreateAsset(quad,path);
            var body=Mat("Stable blue sphere",sphere);body.SetFloat("_Layer",0);var energy=Mat("Spherical candy surface",sphere);energy.SetFloat("_Layer",1);
            var glow=Mat("Soft energy",light);glow.SetFloat("_Kind",0);var star=Mat("Four point star",light);star.SetFloat("_Kind",1);var ring=Mat("Orbit highlight",light);ring.SetFloat("_Kind",2);var beam=Mat("Moving beam tail",light);beam.SetFloat("_Kind",3);
            var root=new GameObject("ColorBallV2_Root");var v=root.AddComponent<ColorBallVisualV2>();
            v.bodyLayer=Child(root.transform,"BodyLayer");v.body=Part(v.bodyLayer,"Stable shading",quad,body,35);
            v.colorEnergyLayer=Child(root.transform,"ColorEnergyLayer");v.energy=Part(v.colorEnergyLayer,"Independent spherical dots",quad,energy,36);
            v.vfxRoot=Child(root.transform,"VFXRoot");v.halo=Part(v.vfxRoot,"Restrained halo",quad,glow,34);v.core=Part(v.vfxRoot,"White charged center",quad,glow,38);v.orbit=Part(v.vfxRoot,"Small energy orbit",quad,ring,37);
            PrefabUtility.SaveAsPrefabAsset(root,Root+"Prefabs/Resources/ColorBall-V2.prefab");Object.DestroyImmediate(root);
            var fx=new GameObject("ColorBall-V2-VFX");var f=fx.AddComponent<ColorBallVfxV2>();f.quad=quad;f.glow=glow;f.star=star;f.beam=beam;PrefabUtility.SaveAsPrefabAsset(fx,Root+"Prefabs/Resources/ColorBall-V2-VFX.prefab");Object.DestroyImmediate(fx);
            foreach(var m in new[]{body,energy,glow,star,ring,beam}){EditorUtility.SetDirty(m);AssetDatabase.SaveAssetIfDirty(m);}AssetDatabase.SaveAssetIfDirty(quad);
            File.WriteAllText(Root+"Assets/revision.txt",Revision);Debug.Log("[Color Ball V2] Independent prefabs built.");
        }
        static Transform Child(Transform p,string name){var t=new GameObject(name).transform;t.SetParent(p,false);return t;}
        static Renderer Part(Transform p,string name,Mesh mesh,Material material,int order){var t=Child(p,name);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.sortingOrder=order;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;return r;}
        static Material Mat(string name,Shader shader){var path=Root+"Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}m.shader=shader;return m;}
    }
}
#endif

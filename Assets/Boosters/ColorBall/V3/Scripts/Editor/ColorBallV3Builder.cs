#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
namespace RoyalThrone.Boosters.ColorBall.V3.Editor
{
    public static class ColorBallV3Builder
    {
        public const string Root="Assets/Boosters/ColorBall/V3/";
        const string Revision="depth-orbit-v3-glint";
        [InitializeOnLoadMethod]static void Init(){EditorApplication.delayCall+=Ensure;}
        static void Ensure(){if(!EditorApplication.isPlayingOrWillChangePlaymode&&(!File.Exists(Root+"Assets/revision.txt")||File.ReadAllText(Root+"Assets/revision.txt")!=Revision))Build();}
        [MenuItem("Tools/Royal Throne/Color Ball V3/Build prefabs")]
        public static void Build(){
            var sphere=Shader.Find("RoyalThrone/ColorBallV3/Sphere");var light=Shader.Find("RoyalThrone/ColorBallV3/Light");if(!sphere||!light)return;
            var quad=new Mesh{name="Color Ball quad"};quad.vertices=new[]{new Vector3(-.5f,-.5f),new Vector3(.5f,-.5f),new Vector3(.5f,.5f),new Vector3(-.5f,.5f)};quad.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};quad.triangles=new[]{0,1,2,0,2,3};quad.RecalculateBounds();
            var path=Root+"Assets/Quad.asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(quad,old);Object.DestroyImmediate(quad);quad=old;}else AssetDatabase.CreateAsset(quad,path);
            var body=Mat("Stable blue sphere",sphere);body.SetFloat("_Layer",0);var energy=Mat("Spherical candy surface",sphere);energy.SetFloat("_Layer",1);
            var glow=Mat("Soft energy",light);glow.SetFloat("_Kind",0);var star=Mat("Four point star",light);star.SetFloat("_Kind",1);var ring=Mat("Orbit highlight",light);ring.SetFloat("_Kind",4);var beam=Mat("Moving beam tail",light);beam.SetFloat("_Kind",3);
            var root=new GameObject("ColorBallV3_Root");var v=root.AddComponent<ColorBallVisualV3>();
            v.bodyLayer=Child(root.transform,"BodyLayer");v.body=Part(v.bodyLayer,"Stable shading",quad,body,35);
            v.colorSurfaceLayer=Child(root.transform,"ColorSurfaceLayer");v.energy=Part(v.colorSurfaceLayer,"Independent spherical dots",quad,energy,36);
            v.vfxRoot=Child(root.transform,"VFXRoot");v.halo=Part(v.vfxRoot,"Restrained halo",quad,glow,33);v.core=Part(root.transform,"CoreGlow",quad,glow,38);
            v.orbitBack=Part(root.transform,"OrbitBack",OrbitMesh("OrbitBack",0),ring,34);v.orbitFront=Part(root.transform,"OrbitFront",OrbitMesh("OrbitFront",Mathf.PI),ring,39);
            v.orbitGlint=Part(root.transform,"Orbit glint",quad,star,39);
            v.beamOrigin=Child(root.transform,"BeamOrigin");v.originFlare=Part(v.beamOrigin,"Source pulse",quad,star,40);
            PrefabUtility.SaveAsPrefabAsset(root,Root+"Prefabs/Resources/ColorBall-V3.prefab");Object.DestroyImmediate(root);
            var fx=new GameObject("ColorBall-V3-VFX");var f=fx.AddComponent<ColorBallVfxV3>();f.quad=quad;f.glow=glow;f.star=star;f.beam=beam;PrefabUtility.SaveAsPrefabAsset(fx,Root+"Prefabs/Resources/ColorBall-V3-VFX.prefab");Object.DestroyImmediate(fx);
            foreach(var m in new[]{body,energy,glow,star,ring,beam}){EditorUtility.SetDirty(m);AssetDatabase.SaveAssetIfDirty(m);}AssetDatabase.SaveAssetIfDirty(quad);
            File.WriteAllText(Root+"Assets/revision.txt",Revision);Debug.Log("[Color Ball V3] Independent prefabs built.");
        }
        static Mesh OrbitMesh(string name,float start){
            const int segments=64;var vertices=new Vector3[(segments+1)*2];var uv=new Vector2[vertices.Length];var indices=new int[segments*6];
            for(int i=0;i<=segments;i++){float t=start+Mathf.PI*i/segments;var center=new Vector3(.66f*Mathf.Cos(t),.22f*Mathf.Sin(t),.25f*Mathf.Sin(t));var normal=new Vector3(.22f*Mathf.Cos(t),.66f*Mathf.Sin(t),0).normalized*.045f;vertices[2*i]=center-normal;vertices[2*i+1]=center+normal;uv[2*i]=new Vector2(t/(2*Mathf.PI),0);uv[2*i+1]=new Vector2(t/(2*Mathf.PI),1);if(i<segments){int j=i*6,k=i*2;indices[j]=k;indices[j+1]=k+1;indices[j+2]=k+3;indices[j+3]=k;indices[j+4]=k+3;indices[j+5]=k+2;}}
            var mesh=new Mesh{name=name,vertices=vertices,uv=uv,triangles=indices};mesh.RecalculateBounds();string path=Root+"Assets/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);return old;}AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static Transform Child(Transform p,string name){var t=new GameObject(name).transform;t.SetParent(p,false);return t;}
        static Renderer Part(Transform p,string name,Mesh mesh,Material material,int order){var t=Child(p,name);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.sortingOrder=order;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;return r;}
        static Material Mat(string name,Shader shader){var path=Root+"Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}m.shader=shader;return m;}
    }
}
#endif

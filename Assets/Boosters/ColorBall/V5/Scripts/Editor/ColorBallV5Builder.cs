#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using RoyalThrone.Boosters.ColorBall.V4;
namespace RoyalThrone.Boosters.ColorBall.V5.Editor
{
    public static class ColorBallV5Builder
    {
        public const string Root="Assets/Boosters/ColorBall/V5/";
        const string Base="Assets/Boosters/ColorBall/V4/";
        const string Revision="v5.1-local-motion-energy-1";
        [InitializeOnLoadMethod]static void Init(){EditorApplication.delayCall+=Ensure;}
        static void Ensure(){if(!EditorApplication.isPlayingOrWillChangePlaymode&&(!File.Exists(Root+"Assets/revision.txt")||File.ReadAllText(Root+"Assets/revision.txt")!=Revision))Build();}
        [MenuItem("Tools/Royal Throne/Color Ball V5/Build material and prefab variants")]
        public static void Build()
        {
            var sphere=Shader.Find("RoyalThrone/ColorBallV5/Sphere");var light=Shader.Find("RoyalThrone/ColorBallV5/Light");if(!sphere||!light)return;
            var body=Mat("Luminous body",sphere,0);var energy=Mat("Rotating bands",sphere,1);
            var glow=Mat("Soft light",light,0);var star=Mat("Four point star",light,1);var rim=Mat("Finish rim",light,2);
            var beam=Mat("Connected beam glow",light,3);var beamCore=Mat("Connected white core",light,5);var halo=Mat("Charged white halo",light,6);
            // Retain warm shoulders on the bright blue board. Core/aura still use additive light.
            beam.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            var aura=Mat("Luminous aura edges",light,7);
            var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Base+"Prefabs/Resources/ColorBall-V4.prefab"));
            root.name="ColorBall-V5";var v=root.GetComponent<ColorBallVisualV4>();v.referenceV5=true;
            v.body.sharedMaterial=body;v.energy.sharedMaterial=energy;v.halo.sharedMaterial=halo;v.core.sharedMaterial=glow;v.originFlare.sharedMaterial=v.orbitGlint.sharedMaterial=star;
            var presentation=root.AddComponent<ColorBallV5Presentation>();
            var g=new GameObject("Final discharge rim");g.transform.SetParent(root.transform,false);g.AddComponent<MeshFilter>().sharedMesh=v.body.GetComponent<MeshFilter>().sharedMesh;
            presentation.finishRim=g.AddComponent<MeshRenderer>();presentation.finishRim.sharedMaterial=rim;presentation.finishRim.sortingOrder=40;presentation.finishRim.enabled=false;
            presentation.finishRim.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;presentation.finishRim.receiveShadows=false;
            var front=new GameObject("Charged aura front edges");front.transform.SetParent(root.transform,false);front.AddComponent<MeshFilter>().sharedMesh=v.body.GetComponent<MeshFilter>().sharedMesh;
            presentation.auraFront=front.AddComponent<MeshRenderer>();presentation.auraFront.sharedMaterial=aura;presentation.auraFront.sortingOrder=37;presentation.auraFront.enabled=false;
            presentation.auraFront.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;presentation.auraFront.receiveShadows=false;
            PrefabUtility.SaveAsPrefabAsset(root,Root+"Prefabs/Resources/ColorBall-V5.prefab");Object.DestroyImmediate(root);
            var fx=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Base+"Prefabs/Resources/ColorBall-V4-VFX.prefab"));
            fx.name="ColorBall-V5-VFX";var f=fx.GetComponent<ColorBallVfxV4>();f.referenceV5=true;f.glow=glow;f.star=star;f.beam=beam;f.beamCore=beamCore;
            PrefabUtility.SaveAsPrefabAsset(fx,Root+"Prefabs/Resources/ColorBall-V5-VFX.prefab");Object.DestroyImmediate(fx);
            foreach(var m in new[]{body,energy,glow,star,rim,beam,beamCore,halo,aura}){EditorUtility.SetDirty(m);AssetDatabase.SaveAssetIfDirty(m);}
            File.WriteAllText(Root+"Assets/revision.txt",Revision);Debug.Log("[Color Ball V5] V4 prefab variants ready.");
        }
        static Material Mat(string name,Shader shader,float kind){var path=Root+"Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}m.shader=shader;if(shader.name.EndsWith("Sphere"))m.SetFloat("_Layer",kind);else m.SetFloat("_Kind",kind);return m;}
    }
}
#endif

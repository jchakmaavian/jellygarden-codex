#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace RoyalThrone.Boosters.Propeller.V4.Editor
{
    public static class PropellerV4Builder
    {
        public const string Root="Assets/Boosters/Propeller/V4/";
        [InitializeOnLoadMethod]static void Initialize(){EditorApplication.delayCall+=Ensure;}
        const string Revision="distance-flight-audio-1";
        static void Ensure(){if(!EditorApplication.isPlayingOrWillChangePlaymode&&(!System.IO.File.Exists(Root+"Assets/rig-revision.txt")||System.IO.File.ReadAllText(Root+"Assets/rig-revision.txt")!=Revision))Build();}
        [MenuItem("Tools/Royal Throne/Propeller V4/Build independent prefab")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Build in Edit Mode.");
            var surface=Shader.Find("RoyalThrone/PropellerV4/PolishedSurface");var light=Shader.Find("RoyalThrone/PropellerV4/AirAndStars");
            if(!surface||!light)return;
            var blue=Mat("Body royal blue",surface,new Color(.025f,.20f,.70f));
            var gold=Mat("Polished gold",surface,new Color(1,.73f,.015f));
            var red=Mat("Blade ruby tips",surface,new Color(.98f,.015f,.095f));
            var pink=Mat("Ruby hub",surface,new Color(1,.018f,.22f));
            var ghostGold=Mat("Swept gold blades",surface,new Color(1,.73f,.14f,.12f));ghostGold.SetFloat("_DepthWrite",0);
            var ghostRed=Mat("Swept ruby tips",surface,new Color(1,.08f,.13f,.12f));ghostRed.SetFloat("_DepthWrite",0);
            var sphere=Mesh("BodySphere",PropellerGeometry.Sphere());var belt=Mesh("GoldBelt",PropellerGeometry.Band());
            var blade=Mesh("GoldPaddle",PropellerGeometry.Blade(0,.76f));var tip=Mesh("RubyPaddleTip",PropellerGeometry.Blade(.76f,1));
            var rotorBlade=Mesh("GoldRotor",JoinedBlades(blade));var rotorTip=Mesh("RubyRotor",JoinedBlades(tip));var quad=Mesh("LightQuad",PropellerGeometry.Quad());
            var root=new GameObject("Propeller-V4");var visual=root.AddComponent<PropellerVisualV4>();
            var pose=Child(root.transform,"Pose");visual.pose=pose;
            var body=Child(pose,"BodyRoot");visual.body=body;body.localPosition=Vector3.down*.11f;
            var solid=new List<Renderer>();var ghosts=new List<Renderer>();
            solid.Add(Part(body,"Royal blue body",sphere,blue,Vector3.zero,new Vector3(.296f,.272f,.27f)));
            solid.Add(Part(body,"Gold belt",belt,gold,Vector3.up*.01f,Vector3.one));
            var plane=Child(pose,"RotorPlane");visual.rotorPlane=plane;plane.localPosition=new Vector3(0,.17f,-.20f);plane.localRotation=Quaternion.Euler(53,0,0);
            visual.rotor=Child(plane,"Rotor");Blades(visual.rotor,solid,rotorBlade,rotorTip,gold,red);
            solid.Add(Part(plane,"Gold hub",sphere,gold,new Vector3(0,0,-.013f),new Vector3(.123f,.123f,.055f)));
            solid.Add(Part(plane,"Ruby hub cap",sphere,pink,new Vector3(0,0,-.063f),new Vector3(.082f,.082f,.049f)));
            visual.echoes=new Transform[6];
            for(int i=0;i<6;i++){visual.echoes[i]=Child(plane,"Blade sweep "+i);Blades(visual.echoes[i],ghosts,rotorBlade,rotorTip,ghostGold,ghostRed);}
            visual.solidViews=solid.ToArray();visual.echoViews=ghosts.ToArray();
            PrefabUtility.SaveAsPrefabAsset(root,Root+"Prefabs/Resources/Propeller-V4.prefab");Object.DestroyImmediate(root);
            var fx=new GameObject("Propeller-V4-VFX");var pool=fx.AddComponent<PropellerVfxV4>();pool.quad=quad;
            pool.glow=Mat("Compact white gold light",light,Color.white);pool.glow.SetFloat("_Kind",0);
            pool.star=Mat("Four point stars",light,Color.white);pool.star.SetFloat("_Kind",1);
            pool.air=Mat("Thin air pulse",light,Color.white);pool.air.SetFloat("_Kind",2);
            pool.pressure=Mat("Directional pressure",light,Color.white);pool.pressure.SetFloat("_Kind",3);
            BuildChips(pool);
            foreach(var m in new[]{blue,gold,red,pink,ghostGold,ghostRed,pool.glow,pool.star,pool.air,pool.pressure}){EditorUtility.SetDirty(m);AssetDatabase.SaveAssetIfDirty(m);}
            PrefabUtility.SaveAsPrefabAsset(fx,Root+"Prefabs/Resources/Propeller-V4-VFX.prefab");Object.DestroyImmediate(fx);
            System.IO.File.WriteAllText(Root+"Assets/rig-revision.txt",Revision);
            Debug.Log("[Propeller V4] Independent prefabs built. All references stay inside "+Root);
        }
        static void Blades(Transform parent,List<Renderer> views,Mesh blade,Mesh tip,Material gold,Material red)
        {views.Add(Part(parent,"Gold blades",blade,gold,Vector3.zero,Vector3.one));views.Add(Part(parent,"Ruby tips",tip,red,Vector3.zero,Vector3.one));}
        static void BuildChips(PropellerVfxV4 pool)
        {
            var library=Resources.Load<JellyGarden.Scripts.Juice.RoyalBurstLibrary>("RoyalGameplay/Toolkit/BurstPieces");
            if(!library)throw new System.InvalidOperationException("Existing painted gem chips not found.");
            pool.chipMaterials=new Material[library.bursts.Length];pool.chipMeshes=new Mesh[library.bursts.Length*3];
            for(int c=0;c<library.bursts.Length;c++){
                var pieces=library.bursts[c].pieces;var texture=pieces[0].sprite.texture;
                var source=AssetDatabase.GetAssetPath(texture);var destination=Root+"Assets/Gem-chips-"+c+System.IO.Path.GetExtension(source);
                if(!System.IO.File.Exists(destination))AssetDatabase.CopyAsset(source,destination);
                var material=Mat("Local gem chips "+c,Shader.Find("RoyalThrone/PropellerV4/LocalChips"),Color.white);
                material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(destination);pool.chipMaterials[c]=material;EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);
                for(int i=0;i<3;i++){
                    var sprite=pieces[i%pieces.Length].sprite;var r=sprite.rect;float diagonal=Mathf.Sqrt(r.width*r.width+r.height*r.height);
                    float x=r.width/diagonal*.5f,y=r.height/diagonal*.5f;
                    var mesh=new Mesh();mesh.vertices=new[]{new Vector3(-x,-y),new Vector3(x,-y),new Vector3(x,y),new Vector3(-x,y)};
                    mesh.uv=new[]{new Vector2(r.xMin/texture.width,r.yMin/texture.height),new Vector2(r.xMax/texture.width,r.yMin/texture.height),new Vector2(r.xMax/texture.width,r.yMax/texture.height),new Vector2(r.xMin/texture.width,r.yMax/texture.height)};
                    mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();pool.chipMeshes[c*3+i]=Mesh("Local-chip-"+c+"-"+i,mesh);
                }
            }
        }
        static Mesh JoinedBlades(Mesh mesh){var parts=new CombineInstance[3];for(int b=0;b<3;b++)parts[b]=new CombineInstance{mesh=mesh,transform=Matrix4x4.Rotate(Quaternion.Euler(0,0,30+b*120))};var result=new Mesh();result.CombineMeshes(parts,true,true);return result;}
        static Transform Child(Transform p,string name){var t=new GameObject(name).transform;t.SetParent(p,false);return t;}
        static Renderer Part(Transform p,string name,Mesh mesh,Material material,Vector3 position,Vector3 scale)
        {var t=Child(p,name);t.localPosition=position;t.localScale=scale;t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.sortingOrder=35;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;return r;}
        static Material Mat(string name,Shader shader,Color color){var path=Root+"Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}m.shader=shader;m.color=color;return m;}
        static Mesh Mesh(string name,Mesh mesh){mesh.name=name;var path=Root+"Assets/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);AssetDatabase.SaveAssetIfDirty(old);return old;}AssetDatabase.CreateAsset(mesh,path);return mesh;}
    }
}
#endif

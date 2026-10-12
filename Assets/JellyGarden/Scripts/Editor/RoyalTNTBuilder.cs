using System.IO;
using JellyGarden.Scripts.Juice;
using UnityEditor;
using UnityEngine;

namespace RoyalThrone.Editor
{
    [InitializeOnLoad]
    public static class RoyalTNTBuilder
    {
        const string Folder="Assets/JellyGarden/Resources/RoyalTNT/";
        static RoyalTNTBuilder(){EditorApplication.delayCall+=Ensure;}
        static void Ensure()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(!File.Exists(Folder+"TNT_3D.prefab")){Build();return;}
            var existing=AssetDatabase.LoadAssetAtPath<TNT3DAnimator>(Folder+"TNT_3D.prefab");
            if(existing&&(existing.impacts==null||existing.impacts.Length==0))
            {
                var root=PrefabUtility.LoadPrefabContents(Folder+"TNT_3D.prefab");var a=root.GetComponent<TNT3DAnimator>();
                var cells=Root("CellImpacts",root.transform);a.impacts=new Renderer[25];var glow=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Glow.mat");
                for(int i=0;i<25;i++){a.impacts[i]=Shape("Cell impact "+i,cells,PrimitiveType.Quad,Vector3.zero,Vector3.one,glow,34);a.impacts[i].enabled=false;}
                PrefabUtility.SaveAsPrefabAsset(root,Folder+"TNT_3D.prefab");PrefabUtility.UnloadPrefabContents(root);
            }
        }
        static Material Material(string name,string shader,Color color,float mode=0)
        {var m=new Material(Shader.Find(shader));m.name=name;m.color=color;if(m.HasProperty("_Mode"))m.SetFloat("_Mode",mode);AssetDatabase.CreateAsset(m,Folder+name+".mat");return m;}
        static Transform Root(string name,Transform parent,Vector3 pos=default)
        {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=pos;return t;}
        static Renderer Shape(string name,Transform parent,PrimitiveType type,Vector3 pos,Vector3 scale,Material mat,int order=25)
        {
            var g=GameObject.CreatePrimitive(type);g.name=name;Object.DestroyImmediate(g.GetComponent<Collider>());g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=scale;
            var r=g.GetComponent<Renderer>();r.sharedMaterial=mat;r.sortingOrder=order;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;return r;
        }
        [MenuItem("Tools/Royal Throne/TNT/Build TNT visual prefab")]
        public static void Build()
        {
            RoyalHomeScreenBuilder.RequireCopy();Directory.CreateDirectory(Folder);
            if(File.Exists(Folder+"TNT_3D.prefab"))return;
            var red=Material("Barrel red","RoyalThrone/TNTBody",new Color(.92f,.035f,.02f));
            var gold=Material("Polished gold","RoyalThrone/TNTBody",new Color(1,.65f,.035f));
            var rope=Material("Fuse rope","RoyalThrone/TNTBody",new Color(.93f,.75f,.4f));
            var dark=Material("Badge red","RoyalThrone/TNTBody",new Color(.38f,.02f,.025f));
            var glow=Material("Glow","RoyalThrone/TNTGlow",Color.white);
            var ring=Material("Shockwave","RoyalThrone/TNTGlow",Color.white,1);
            var star=Material("Four point stars","RoyalThrone/TNTGlow",Color.white,2);
            var flame=Material("Head flame","RoyalThrone/TNTGlow",Color.white,3);
            var rays=Material("Radial rays","RoyalThrone/TNTGlow",Color.white,4);
            var go=new GameObject("TNT_3D");var a=go.AddComponent<TNT3DAnimator>();
            a.model=Root("Model",go.transform);a.model.localRotation=Quaternion.Euler(-18,0,0);
            a.body=Root("BodyRoot",a.model);
            Shape("Body",a.body,PrimitiveType.Cylinder,Vector3.zero,new Vector3(.58f,.22f,.58f),red);
            for(int i=0;i<12;i++){float angle=i*Mathf.PI/6;var slat=Shape("Barrel stave "+i,a.body,PrimitiveType.Cube,new Vector3(Mathf.Sin(angle)*.28f,0,Mathf.Cos(angle)*.28f),new Vector3(.018f,.40f,.025f),dark);slat.transform.localRotation=Quaternion.Euler(0,angle*Mathf.Rad2Deg,0);}
            Shape("GoldBandTop",a.body,PrimitiveType.Cylinder,new Vector3(0,.14f,0),new Vector3(.615f,.032f,.615f),gold);
            Shape("GoldBandBottom",a.body,PrimitiveType.Cylinder,new Vector3(0,-.18f,0),new Vector3(.6f,.025f,.6f),gold);
            Shape("Gold centre band",a.body,PrimitiveType.Cylinder,new Vector3(0,-.045f,0),new Vector3(.61f,.065f,.61f),gold);
            Shape("Badge",a.body,PrimitiveType.Cube,new Vector3(0,-.035f,-.319f),new Vector3(.19f,.115f,.018f),dark);
            for(int i=0;i<3;i++){var tooth=Shape("Crown badge "+i,a.body,PrimitiveType.Cube,new Vector3((i-1)*.043f,-.032f,-.334f),new Vector3(.026f,i==1?.067f:.045f,.012f),gold);tooth.transform.localRotation=Quaternion.Euler(0,0,(i-1)*-14);}
            a.head=Root("HeadRoot",a.model,new Vector3(0,.22f,0));
            Shape("Roof",a.head,PrimitiveType.Sphere,Vector3.zero,new Vector3(.6f,.13f,.6f),red);
            Shape("Head gold rim",a.head,PrimitiveType.Cylinder,new Vector3(0,-.02f,0),new Vector3(.63f,.018f,.63f),gold);
            a.fuse=Root("FuseRoot",a.head,new Vector3(0,.06f,0));
            for(int i=0;i<5;i++){var seg=Shape("Fuse segment "+i,a.fuse,PrimitiveType.Sphere,new Vector3(.025f*i,.028f*i,-.03f),new Vector3(.045f,.055f,.045f),rope);}
            Shape("FuseTip",a.fuse,PrimitiveType.Sphere,new Vector3(.125f,.14f,-.03f),Vector3.one*.052f,gold);
            a.flameSocket=Root("FlameSocket",a.head,new Vector3(0,.01f,-.34f));
            a.flames=new Renderer[9];
            for(int i=0;i<7;i++)a.flames[i]=Shape("HeadFlame "+i,a.flameSocket,PrimitiveType.Quad,new Vector3((i-3)*.084f,.10f,0),new Vector3(.19f,.34f,1),flame,31);
            a.flames[7]=Shape("WhiteHotCore",a.flameSocket,PrimitiveType.Quad,new Vector3(0,.04f,-.01f),new Vector3(.58f,.16f,1),glow,32);
            a.flames[8]=Shape("HeadRimGlow",a.flameSocket,PrimitiveType.Quad,Vector3.zero,new Vector3(.8f,.28f,1),ring,32);
            var socket=Root("ExplosionSocket",go.transform,Vector3.back*.4f);a.blast=new Renderer[5];
            string[] names={"WhiteHotCore","GoldBlastGlow","RadialRays","Shockwave","SmokeGlow"};var mats=new[]{glow,glow,rays,ring,glow};
            for(int i=0;i<5;i++)a.blast[i]=Shape(names[i],socket,PrimitiveType.Quad,Vector3.back*i*.01f,Vector3.one,mats[i],33-i);
            var sparkles=Root("RevealSparkles",go.transform);a.stars=new Renderer[28];
            for(int i=0;i<a.stars.Length;i++)a.stars[i]=Shape("Star "+i,sparkles,PrimitiveType.Quad,Vector3.zero,Vector3.one*.12f,star,35);
            var cells=Root("CellImpacts",go.transform);a.impacts=new Renderer[25];
            for(int i=0;i<a.impacts.Length;i++){a.impacts[i]=Shape("Cell impact "+i,cells,PrimitiveType.Quad,Vector3.zero,Vector3.one,glow,34);a.impacts[i].enabled=false;}
            foreach(var r in a.flames)r.enabled=false;foreach(var r in a.blast)r.enabled=false;foreach(var r in a.stars)r.enabled=false;
            PrefabUtility.SaveAsPrefabAsset(go,Folder+"TNT_3D.prefab");Object.DestroyImmediate(go);AssetDatabase.SaveAssets();Debug.Log("[TNT] Lightweight mesh prefab built.");
        }
    }
}

#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Spine.Unity;

namespace RoyalThrone.ReferenceGameplay.Editor
{
    [InitializeOnLoad]
    public static class ReferenceCannonImporter
    {
        const string Base="Assets/ReferenceGameplay/Tools/Cannon/";
        const string Data=Base+"Resources/ReferenceGameplay/Cannon/CannonSkeleton.asset";
        static ReferenceCannonImporter(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Temp/CannonImport.request"))return;
            File.Delete("Temp/CannonImport.request");
            try{Build();}catch(System.Exception e){File.WriteAllText("Design/Cannon-Reference/import-error.txt",e.ToString());Debug.LogException(e);}
        }
        [MenuItem("Royal Throne/Reference Gameplay/Import Cannon Animation")]
        public static void Build()
        {
            foreach(var path in Directory.GetFiles(Base,"*.png"))
            {
                var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.textureType=TextureImporterType.Default;ti.mipmapEnabled=false;
                ti.alphaIsTransparency=false;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.maxTextureSize=4096;ti.SaveAndReimport();
            }
            var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(Base+"export_anim_cannon.png");
            var mat=AssetDatabase.LoadAssetAtPath<Material>(Base+"Cannon.mat");
            if(!mat){mat=new Material(Shader.Find("Spine/Skeleton"));AssetDatabase.CreateAsset(mat,Base+"Cannon.mat");}
            // The packaged material uses premultiplied alpha; preserve it on the extracted texture.
            mat.mainTexture=tex;mat.SetFloat("_StraightAlphaInput",0);mat.DisableKeyword("_STRAIGHT_ALPHA_INPUT");EditorUtility.SetDirty(mat);
            var atlas=AssetDatabase.LoadAssetAtPath<SpineAtlasAsset>(Base+"CannonAtlas.asset");
            if(!atlas){atlas=SpineAtlasAsset.CreateRuntimeInstance(AssetDatabase.LoadAssetAtPath<TextAsset>(Base+"export_anim_cannon.atlas.txt"),new[]{mat},true);AssetDatabase.CreateAsset(atlas,Base+"CannonAtlas.asset");}
            var data=AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(Data);
            if(!data){data=SkeletonDataAsset.CreateRuntimeInstance(AssetDatabase.LoadAssetAtPath<TextAsset>(Base+"export_anim_cannon.skel.bytes"),atlas,true,1f/140f);AssetDatabase.CreateAsset(data,Data);}
            data.scale=1f/140f;data.Clear();var skeleton=data.GetSkeletonData(false);
            ReferenceGameplayVfxImporter.BuildFile("Design/Cannon-Reference/NativeCannonParticles.json");
            EditorUtility.SetDirty(data);AssetDatabase.SaveAssets();
            File.WriteAllText("Design/Cannon-Reference/import-report.txt","Spine "+skeleton.Version+"\nSize "+skeleton.Width+" x "+skeleton.Height+"\n"+string.Join("\n",skeleton.Animations.Select(a=>a.Name+" duration="+a.Duration))+"\nBones: "+string.Join(", ",skeleton.Bones.Select(b=>b.Name)));
            Debug.Log("Cannon source animation and native VFX imported.");
        }
    }
}
#endif

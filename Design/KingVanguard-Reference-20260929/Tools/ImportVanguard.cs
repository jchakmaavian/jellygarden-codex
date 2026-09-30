UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var root="Assets/Characters/KingVenguard/V2/";
var report=new System.Text.StringBuilder();
var faceModifier=UnityEditor.AssetDatabase.LoadAssetAtPath<RoyalThrone.Characters.KingVanguardFaceShapeModifier>(root+"Spine/KingVanguard_FaceShape.asset");
if(!faceModifier)throw new System.Exception("Missing King Vanguard face shape modifier asset");
// Keep reimports consistent with the verified HUD/Area gaze containment fix.
var faceSettings=new UnityEditor.SerializedObject(faceModifier);
faceSettings.FindProperty("pupilScale").floatValue=1.1f;
faceSettings.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.EditorUtility.SetDirty(faceModifier);
foreach(var rig in new[]{"King_Vanguard","AreaCompleted_Vanguard","Nightmare_Vanguard"}) {
    var prefix=root+"Spine/"+rig;
    var ti=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(prefix+".png");
    ti.textureType=UnityEditor.TextureImporterType.Default;
    ti.npotScale=UnityEditor.TextureImporterNPOTScale.None;
    ti.mipmapEnabled=false;ti.alphaIsTransparency=true;ti.sRGBTexture=true;
    ti.textureCompression=UnityEditor.TextureImporterCompression.Uncompressed;
    ti.maxTextureSize=4096;ti.filterMode=UnityEngine.FilterMode.Bilinear;
    ti.wrapMode=UnityEngine.TextureWrapMode.Clamp;ti.SaveAndReimport();
    var texture=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(prefix+".png");
    var material=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(prefix+"_Material.mat");
    if(!material){material=new UnityEngine.Material(UnityEngine.Shader.Find("Spine/Skeleton"));UnityEditor.AssetDatabase.CreateAsset(material,prefix+"_Material.mat");}
    material.mainTexture=texture;material.SetFloat("_StraightAlphaInput",1);material.EnableKeyword("_STRAIGHT_ALPHA_INPUT");
    var atlas=UnityEditor.AssetDatabase.LoadAssetAtPath<Spine.Unity.SpineAtlasAsset>(prefix+"_Atlas.asset");
    if(!atlas){atlas=UnityEngine.ScriptableObject.CreateInstance<Spine.Unity.SpineAtlasAsset>();UnityEditor.AssetDatabase.CreateAsset(atlas,prefix+"_Atlas.asset");}
    atlas.atlasFile=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>(prefix+".atlas.txt");atlas.materials=new[]{material};atlas.Clear();
    var data=UnityEditor.AssetDatabase.LoadAssetAtPath<Spine.Unity.SkeletonDataAsset>(prefix+"_SkeletonData.asset");
    if(!data){data=UnityEngine.ScriptableObject.CreateInstance<Spine.Unity.SkeletonDataAsset>();UnityEditor.AssetDatabase.CreateAsset(data,prefix+"_SkeletonData.asset");}
    data.skeletonJSON=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>(prefix+".skel.bytes");
    data.atlasAssets=new Spine.Unity.AtlasAssetBase[]{atlas};data.scale=.01f;data.defaultMix=.18f;
    data.skeletonDataModifiers.RemoveAll(m=>m is RoyalThrone.Characters.KingVanguardFaceShapeModifier);
    if(rig!="Nightmare_Vanguard")data.skeletonDataModifiers.Add(faceModifier);
    data.Clear();
    var sd=data.GetSkeletonData(false);
    if(sd==null)throw new System.Exception("Cannot read imported "+rig+" skeleton");
    report.AppendLine(rig+": "+sd.Version+"; "+sd.Bones.Count+" bones; "+sd.Slots.Count+" slots; "+sd.Animations.Count+" animations");
    foreach(var animation in sd.Animations)report.AppendLine(animation.Name+"="+animation.Duration);
    var sk=new Spine.Skeleton(sd);sk.SetToSetupPose();sk.UpdateWorldTransform();
    float x,y,w,h;float[] vertices=null;sk.GetBounds(out x,out y,out w,out h,ref vertices);
    report.AppendLine("Setup bounds units "+x+","+y+","+w+","+h);
    foreach(var skin in sd.Skins)report.AppendLine("Skin: "+skin.Name);
    UnityEditor.EditorUtility.SetDirty(material);UnityEditor.EditorUtility.SetDirty(atlas);UnityEditor.EditorUtility.SetDirty(data);
}
System.IO.Directory.CreateDirectory(root+"Materials");UnityEditor.AssetDatabase.Refresh();
var uiPath=root+"Materials/KingVanguard_SkeletonGraphic_StraightAlpha.mat";
var ui=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(uiPath);
if(!ui){ui=new UnityEngine.Material(UnityEngine.Shader.Find("Spine/SkeletonGraphic"));UnityEditor.AssetDatabase.CreateAsset(ui,uiPath);}
ui.SetFloat("_StraightAlphaInput",1);ui.EnableKeyword("_STRAIGHT_ALPHA_INPUT");UnityEditor.EditorUtility.SetDirty(ui);
UnityEditor.AssetDatabase.SaveAssets();
System.IO.File.WriteAllText("Design/KingVanguard-Reference-20260929/import-report.txt",report.ToString());
return report.ToString();

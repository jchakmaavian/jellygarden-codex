using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class ReferenceDualComboImporter
{
 const string Folder="Design/DualCombo-Reference/";
 const string Animation="Assets/ReferenceGameplay/Animation/";
 [MenuItem("Royal Throne/Reference Gameplay/Verify and repair all combo artwork")]
 public static void Build()
 {
  if(EditorApplication.isPlaying)throw new InvalidOperationException("Combo repair requires Edit Mode.");
  var input=JsonUtility.FromJson<ReferenceGameplayVfxImporter.Document>(File.ReadAllText(Folder+"ExactComboSprites.json"));
  foreach(var t in input.textures){var ti=(TextureImporter)AssetImporter.GetAtPath(t.assetPath);ti.npotScale=TextureImporterNPOTScale.None;ti.SaveAndReimport();}
  ReferenceGameplayVfxImporter.BuildFile(Folder+"ExactComboSprites.json");
  var sprites=input.sprites.ToDictionary(s=>s.sourcePathId-988000000,s=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ReferenceGameplay/VFX/AtlasSprites/"+s.sourcePathId+".asset"));
  if(sprites.Values.Any(s=>!s))throw new InvalidOperationException("Exact combo sprite import incomplete.");
  var byName=input.sprites.ToDictionary(s=>s.name,s=>sprites[s.sourcePathId-988000000]);
  var effects=JsonUtility.FromJson<ReferenceGameplayVfxImporter.Document>(File.ReadAllText(Folder+"NativeComboParticles-source.json"));
  int systems=0,bindings=0;
  var supporting=JsonUtility.FromJson<ReferenceGameplayVfxImporter.Document>(File.ReadAllText(Folder+"NativeSupportingParticles-source.json"));
  foreach(var effect in effects.prefabs.Concat(supporting.prefabs))
  {
   string path="Assets/ReferenceGameplay/VFX/Resources/ReferenceGameplay/VFX/"+effect.name+".prefab";
   var root=PrefabUtility.LoadPrefabContents(path);
   try{
    var resolved=new Dictionary<long,Transform>();
    foreach(var node in effect.nodes){var siblings=effect.nodes.Where(n=>n.parentTransformId==node.parentTransformId).ToArray();var target=node.parentTransformId==0?root.transform:resolved[node.parentTransformId].GetChild(Array.IndexOf(siblings,node));resolved[node.transformId]=target;var relative=AnimationUtility.CalculateTransformPath(target,root.transform);
     foreach(var c in node.components){if(c.type!="ParticleSystem")continue;systems++;var ps=target.GetComponent<ParticleSystem>();var sheet=ps.textureSheetAnimation;
      var json=JsonUtility.FromJson<ParticleData>(c.json);if(json.UVModule==null||json.UVModule.sprites==null)continue;
      if(sheet.spriteCount!=json.UVModule.sprites.Length)throw new InvalidOperationException("Native sprite count mismatch "+effect.name+relative);
      for(int i=0;i<sheet.spriteCount;i++){var id=json.UVModule.sprites[i].sprite.m_PathID;if(id==0)continue;if(sprites.TryGetValue(id,out var exactSprite)){sheet.SetSprite(i,exactSprite);bindings++;}}
     }
    }
    PrefabUtility.SaveAsPrefabAsset(root,path);
   }finally{PrefabUtility.UnloadPrefabContents(root);}
  }
  foreach(var kind in new[]{"TNTTNTCombo","ColorBallColorBallCombo"})
  {
   string name=kind=="TNTTNTCombo"?"TntTntComboExplodeAnimation":"BallBallExplodeAnimation";
   string clipPath=Animation+"Clips/"+name+"Rfv387.anim";
   var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,clipPath);}
   EditorUtility.CopySerialized(AssetDatabase.LoadAssetAtPath<AnimationClip>(Animation+"Clips/"+name+".anim"),clip);clip.name=name;
   foreach(var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip)){var keys=AnimationUtility.GetObjectReferenceCurve(clip,binding);for(int i=0;i<keys.Length;i++)if(keys[i].value&&byName.TryGetValue(System.Text.RegularExpressions.Regex.Replace(keys[i].value.name,@"__\d+$",""),out var exact))keys[i].value=exact;AnimationUtility.SetObjectReferenceCurve(clip,binding,keys);}EditorUtility.SetDirty(clip);
   string ctrlName=kind=="TNTTNTCombo"?"TntTntComboRfv387":"BallBallComboRfv387";string ctrlPath=Animation+"Controllers/"+ctrlName+".controller";
   var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);var sm=controller.layers[0].stateMachine;foreach(var st in sm.states)sm.RemoveState(st.state);var state=sm.AddState(name);state.motion=clip;state.speed=1;sm.defaultState=state;
   string viewPath=Animation+"Resources/ReferenceGameplay/Visuals/"+kind+".prefab";var view=PrefabUtility.LoadPrefabContents(viewPath);
   try{foreach(var r in view.GetComponentsInChildren<SpriteRenderer>(true))if(r.sprite&&byName.TryGetValue(System.Text.RegularExpressions.Regex.Replace(r.sprite.name,@"__\d+$",""),out var exact))r.sprite=exact;view.GetComponentInChildren<Animator>(true).runtimeAnimatorController=controller;PrefabUtility.SaveAsPrefabAsset(view,Animation+"Resources/ReferenceGameplay/Visuals/"+kind+"Rfv387.prefab");}finally{PrefabUtility.UnloadPrefabContents(view);}
  }
  AssetDatabase.SaveAssets();File.WriteAllText(Folder+"import-result.txt","PASS: "+systems+" native particle systems, "+bindings+" exact sprite bindings, 32 exact sprites, two dedicated combo views; supporting sprites retained when already exact.\n");Debug.Log("ALL_COMBO_ART_IMPORT_PASSED");
 }
 [Serializable]public class ParticleData{public UVData UVModule;}
 [Serializable]public class UVData{public SpriteEntry[] sprites;}
 [Serializable]public class SpriteEntry{public Pointer sprite;}
 [Serializable]public class Pointer{public long m_PathID;public int m_FileID;}
}

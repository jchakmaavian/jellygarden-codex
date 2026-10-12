var lines = new global::System.Collections.Generic.List<string>();
int systems=0;
foreach(var name in new[]{"StarHitParticles","StarIdleParticles"}) {
 var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/ReferenceGameplay/WinVFX/Resources/ReferenceGameplay/VFX/"+name+".prefab");
 if(!prefab) throw new global::System.Exception("Missing prefab "+name);
 foreach(var ps in prefab.GetComponentsInChildren<UnityEngine.ParticleSystem>(true)) {
  systems++;
  var r=ps.GetComponent<UnityEngine.ParticleSystemRenderer>();
  if(!r || !r.sharedMaterial || !r.sharedMaterial.shader || !r.sharedMaterial.shader.isSupported) throw new global::System.Exception("Missing renderer/material "+ps.name);
  var sheet=ps.textureSheetAnimation;
  if(sheet.enabled && sheet.mode==UnityEngine.ParticleSystemAnimationMode.Sprites)
   for(int i=0;i<sheet.spriteCount;i++) if(!sheet.GetSprite(i))throw new global::System.Exception("Missing sprite "+ps.name);
  var shape=ps.shape;
  if(shape.enabled && shape.shapeType==UnityEngine.ParticleSystemShapeType.Mesh && !shape.mesh)throw new global::System.Exception("Missing shape mesh "+ps.name);
 }
 lines.Add(name+": validated particle systems, materials, shaders, sprite references and emitter mesh");
}
if(systems!=13)throw new global::System.Exception("Expected 13 native particle systems; got "+systems);
ReferenceWinVfxImporter.DumpImportedModules();
lines.Add("PASS 13 native ParticleSystems; no missing referenced sprites, materials or active emitter meshes.");
global::System.IO.File.WriteAllLines("Design/Cards/LevelCompleted-v14/native-asset-checks.txt",lines);
return lines;

var mesh = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/ReferenceGameplay/WinVFX/991-star-emitter.obj").OfType<UnityEngine.Mesh>().First();
ReferenceWinVfxImporter.BuildFile("Design/Cards/LevelCompleted-v14/NativeWinParticles.json",new global::System.Collections.Generic.Dictionary<long,UnityEngine.Object>{{991,mesh}});
return "Imported native hit and idle particles with source emitter mesh";

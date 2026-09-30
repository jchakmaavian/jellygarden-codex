var report=RoyalThrone.Characters.EditorTools.KingVanguardPrefabBuilder.Build();
System.IO.Directory.CreateDirectory("Design/KingVanguard-InteractionFix-20260930/Validation");
System.IO.File.WriteAllText("Design/KingVanguard-InteractionFix-20260930/Validation/prefab-build.txt",report);
return report;

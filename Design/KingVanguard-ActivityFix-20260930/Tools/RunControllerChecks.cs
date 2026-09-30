var report=RoyalThrone.Characters.EditorTools.KingVanguardRuntimeChecks.Run();
System.IO.Directory.CreateDirectory("Design/KingVanguard-ActivityFix-20260930/Validation");
System.IO.File.WriteAllText("Design/KingVanguard-ActivityFix-20260930/Validation/controller-checks.txt",report);
return report;

# Copy verification

Completed on 8 September 2026.

| Check | Result |
| --- | --- |
| Source files copied and initially SHA-256 verified | 1,664 files; 91,505,766 bytes |
| Asset files copied | 1,611 |
| Original asset `.meta` files preserved | All 853; identical hashes |
| Source files and original setup files checked after import | 1,671 |
| Changes detected in those original files | None |
| Shared source-folder links | None |
| Package dependencies using `file:` paths | None |
| Git remotes in the new repository | None |
| Git access from the user's normal Windows account | Verified after the explicitly approved ownership correction on this copy |
| Unity Editor version | 6000.5.10f1 |
| Unity import and C# compilation | Completed; process exit code 0 |
| Unity Hub registration | Added as JellyGarden-Codex |
| Visible Editor launch | Opened from Hub; copied game scene and level map verified visually |
| C# compiler errors found | None |
| Compiler warnings | Present in the inherited code; not changed as part of copying |
| Play Mode / device / executable build validation | Not performed |

The imported copy differs from the saved game source only in `ProjectSettings/ProjectSettings.asset` and `ProjectSettings/UnityConnectSettings.asset`: a separate development identity and disabled/unlinked local Unity Services settings. All copied assets, scripts, levels, package manifests, rendering settings, and metadata remained identical after import. The README, project boundary instructions, provenance records, and per-machine Git ignore entries are added development documentation/configuration.

Unity resolved packages, compiled script assemblies, completed its initial Asset Pipeline Refresh, and logged `Exiting batchmode successfully now!`. The first attempt from the restricted shell could not connect to the licensing client and was stopped. A second attempt using the existing signed-in Windows environment completed successfully. Only the copied project was passed to either import command.

The baseline commit `7194b95` preserves the verified source copy. The next commit `e84a1d2` introduces the development identity and project boundary documentation. `SourceReference/COPY-MANIFEST.json` records the copied files before those intentional changes.

The original Unity Editor and VS Code project were not saved, closed, reconfigured, or built during this task. No remote repository, cloud project, or store submission was created.

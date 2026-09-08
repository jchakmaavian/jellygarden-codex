# Royal Throne

This is an independent copy of the saved files from `J:\Unity Projects\Match3-Games`, created on 8 September 2026. Work on this project when developing with Codex. The original belongs to the existing VS Code workflow and must remain untouched.

## Game direction

Current work focuses on game logic, level design, story, characters, UX/UI, visualization, animation, and VFX. The first working milestone is one polished playable level. See [GAME-DIRECTION.md](GAME-DIRECTION.md) for priorities and review criteria. Android/iOS export is deferred until requested by the user or boss.

The active workflow and ordered backlog are in [PRODUCTION-PLAN.md](PRODUCTION-PLAN.md). The first live review and reconciliation with the inherited reports are in [PLAYTEST-AND-AUDIT-2026-09-08.md](PLAYTEST-AND-AUDIT-2026-09-08.md). See [PLAYTEST-TOOLS.md](PLAYTEST-TOOLS.md) for Editor input tracing and explicitly labelled developer-assisted board checks.

The user locked **Royal Throne** as the game name on 8 September 2026. The descriptive store-title form is **Royal Throne: Match 3**. The creative direction includes a castle, king, queen, game trailer, and animated story scenes. See [NAMING-BRIEF.md](NAMING-BRIEF.md) for the naming decision, [NAMING-RESEARCH.md](NAMING-RESEARCH.md) for historical research and a proposed player journey, and [NAME-STORY-AND-CINEMATICS.md](NAME-STORY-AND-CINEMATICS.md) for the draft treatment. The development folder and backup repository remain `JellyGarden-Codex` and `jchakmaavian/jellygarden-codex`. The user also locked the [final splash logo](Design/Branding/royal-throne-splash-logo-v1.md), now installed in the opening scene, and the Unity product title is **Royal Throne**.

The opening flow now has a castle background, an emerald-and-gold Play button, and an animated loading page with real asynchronous scene progress. See [splash and loading design](Design/Branding/SPLASH-AND-LOADING.md) for assets, editing instructions and the F7 motion-preview shortcut in Play Mode.

## Open this project

- This folder is now registered in Unity Hub as **JellyGarden-Codex**. Open that entry with **Unity 6000.5.10f1**. If you later remove the entry, use Add > Add project from disk and select this folder again.
- Add this exact folder to Codex when you want a dedicated project workspace. If you use VS Code, open this folder in a separate window.
- The first import has already completed successfully with Unity 6000.5.10f1. Unity regenerates its cache if that cache is removed later.
- Existing build scenes: `Assets/JellyGarden/Scenes/main.unity`, followed by `Assets/JellyGarden/Scenes/game.unity`.

## What was copied

All saved files in `Assets`, `Packages`, `ProjectSettings`, and `Project_Audit`, plus the Unity `.gitignore`. This includes uncommitted and untracked game content from the original. All asset `.meta` files are preserved, keeping scene, prefab, and asset references intact.

The original `CLAUDE.md` and `PLAN.md` are archived under `SourceReference` for context. Their old CURRENT TASK entries are historical information, not the user's current assignment. `SourceReference/COPY-MANIFEST.json` records SHA-256 hashes of the original copied files.

Not copied: generated `Library`, `Temp`, `Logs`, `UserSettings`, solution/project files; the original `.git` repository; `.vscode` and `.mcp.json` configurations. There are no junctions or shared source folders.

Unity has since generated fresh cache, solution, and `.vscode` files inside this copy. The new VS Code settings point to `JellyGarden-Codex.slnx`; the original VS Code configuration was not copied or changed. These generated files are excluded from this copy's Git history.

## Independence

- A new Git repository has a baseline commit containing the verified source copy. Its `origin` points to the separate private repository `https://github.com/jchakmaavian/jellygarden-codex.git` for the user's requested online backup. It does not synchronize with the original repository.
- The copy uses company `CodexSandbox`, product `Royal Throne`, a new product GUID, and app identifier `com.codexsandbox.jellygarden`. This separates the usual local game-save identity and installation identity from the original. On the branding update, the copy's previous Windows Editor preferences and local data were preserved under Royal Throne; the earlier data was retained.
- The copied Unity Cloud project/organization link is cleared, and the copied built-in Unity Services enable/startup flags are disabled. This changes only local files in this copy, not the original project or any online project.
- The initial source snapshot is preserved in Git. Subsequent development changes are recorded in this copy's history; package versions and rendering configuration remain at the copied baseline.
- Third-party integration settings inside the copied game remain as source content. Check ads, purchases, social login, and backend configuration before enabling those features. This copy is not a published or fully offline game.
- Installed Unity tools, account sign-in, and global editor preferences remain shared on this computer; this is a separate project, not a separate machine.

## Working practice

1. Establish a working baseline in this copy with the same Unity version before upgrading packages or changing the render pipeline.
2. Make one focused feature or fix at a time. Use a local branch for larger experiments and commit known-good results.
3. Test the affected flow in Unity and review the Console. Use Level 1 for initial gameplay and visual iteration.
4. Put executable builds under `Builds` in this project. Do not overwrite the original game's builds or connect this copy to production services.
5. Any future transfer back to the original is a separate, explicitly requested review and merge. Opening or saving this copy does not update the original.

The source baseline commit is `7194b95`. The original source revision was `a82c4df33a929614fd4d7095b59aa26cb0274a8b`, with additional saved changes captured in this copy. Unsaved changes in an open editor are not part of a filesystem copy.

## Online backup

Backup destination: [jchakmaavian/jellygarden-codex](https://github.com/jchakmaavian/jellygarden-codex), a private repository owned by the user. This project's `origin` uses that repository. The initial README commit created on GitHub is preserved in the merged history.

The first game upload completed on 8 September 2026. A fresh download from GitHub restored all 1,670 tracked files, including 853 asset `.meta` files, and passed Git integrity and clean-checkout checks. This recovery check did not launch Unity; the earlier import and C# compilation result is recorded below.

See [BACKUP-AND-RESTORE.md](BACKUP-AND-RESTORE.md) for saving future work online and restoring the project after a Windows or drive failure. Only successfully pushed changes are backed up on GitHub.

## Verification

See `VERIFICATION.md` for the initial copy checks and Unity import result. The import and C# compilation passed. A later readiness check observed the copied game running on Level 4 with 0 Console errors and 5 warnings; a successful candy swap was not confirmed by the automated input check. Complete gameplay validation remains part of the first milestone.

Git access from the normal Windows account is verified. With the user's explicit approval, ownership was corrected only on this new project folder and its `.git` directory. No global Git settings or original-project permissions were changed.

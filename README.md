# JellyGarden Codex development copy

This is an independent copy of the saved files from `J:\Unity Projects\Match3-Games`, created on 8 September 2026. Work on this project when developing with Codex. The original belongs to the existing VS Code workflow and must remain untouched.

## Open this project

- In Unity Hub, add this folder as a project and open it with **Unity 6000.5.10f1**.
- Add this exact folder to Codex when you want a dedicated project workspace. If you use VS Code, open this folder in a separate window.
- Unity recreates its import cache on the first open, which can take several minutes.
- Existing build scenes: `Assets/JellyGarden/Scenes/main.unity`, followed by `Assets/JellyGarden/Scenes/game.unity`.

## What was copied

All saved files in `Assets`, `Packages`, `ProjectSettings`, and `Project_Audit`, plus the Unity `.gitignore`. This includes uncommitted and untracked game content from the original. All asset `.meta` files are preserved, keeping scene, prefab, and asset references intact.

The original `CLAUDE.md` and `PLAN.md` are archived under `SourceReference` for context. Their old CURRENT TASK entries are historical information, not the user's current assignment. `SourceReference/COPY-MANIFEST.json` records SHA-256 hashes of the original copied files.

Not copied: generated `Library`, `Temp`, `Logs`, `UserSettings`, solution/project files; the original `.git` repository; `.vscode` and `.mcp.json` configurations. There are no junctions or shared source folders.

## Independence

- A new local Git repository has a baseline commit containing the verified source copy. It has no remote and does not synchronize with the original repository.
- The copy uses company `CodexSandbox`, product `JellyGarden Codex Sandbox`, a new product GUID, and app identifier `com.codexsandbox.jellygarden`. This separates the usual local game-save identity and installation identity from the original.
- The copied Unity Cloud project/organization link is cleared, and the copied built-in Unity Services enable/startup flags are disabled. This changes only local files in this copy, not the original project or any online project.
- Game assets, gameplay scripts, levels, package versions, and rendering configuration are preserved.
- Third-party integration settings inside the copied game remain as source content. Check ads, purchases, social login, and backend configuration before enabling those features. This copy is not a published or fully offline game.
- Installed Unity tools, account sign-in, and global editor preferences remain shared on this computer; this is a separate project, not a separate machine.

## Working practice

1. Establish a working baseline in this copy with the same Unity version before upgrading packages or changing the render pipeline.
2. Make one focused feature or fix at a time. Use a local branch for larger experiments and commit known-good results.
3. Test the affected flow in Unity and review the Console. Use Level 1 for initial gameplay and visual iteration.
4. Put executable builds under `Builds` in this project. Do not overwrite the original game's builds or connect this copy to production services.
5. Any future transfer back to the original is a separate, explicitly requested review and merge. Opening or saving this copy does not update the original.

The source baseline commit is `7194b95`. The original source revision was `a82c4df33a929614fd4d7095b59aa26cb0274a8b`, with additional saved changes captured in this copy. Unsaved changes in an open editor are not part of a filesystem copy.

# GitHub backup and recovery

The backup destination for this independent development copy is the private repository:

https://github.com/jchakmaavian/jellygarden-codex

## Save work online

After each completed feature or work session:

1. Save changes in Unity and your code editor.
2. Review the changed files, including new assets and their `.meta` files. Keep passwords, credentials, signing keys, and generated build files out of Git.
3. Commit the intended changes and push the working branch to this repository.
4. Confirm that the push succeeded and that the commit appears on GitHub.

For work on `main`, run these commands from this project's folder in PowerShell:

```powershell
git status
git diff
git add --all
git diff --cached --stat
git commit -m "Describe the completed change"
git push origin main
```

Only work successfully pushed to GitHub is backed up online. A local commit alone, unsaved editor changes, and ignored files are not included. No scheduled backup job is configured.

## Restore after a Windows or drive failure

1. Install Git, Unity Hub, and Unity Editor **6000.5.10f1**, with the build modules needed for your target platform. Sign in to your GitHub and Unity accounts as required.
2. Choose a new empty destination and clone the private repository. For example:

   ```powershell
   git clone https://github.com/jchakmaavian/jellygarden-codex.git "C:\Unity Projects\JellyGarden-Codex"
   ```

3. In Unity Hub, choose **Add > Add project from disk** and select the cloned `JellyGarden-Codex` folder.
4. Open it with **6000.5.10f1** and allow Unity to download packages and regenerate its cache.
5. Open `Assets/JellyGarden/Scenes/main.unity`, check the Console, and test the game before making further changes.

The repository contains `Assets` (including `.meta` files), `Packages`, `ProjectSettings`, project documentation, and committed revision history. Unity's `Library`, `Temp`, `Logs`, and other generated files are intentionally excluded and regenerated. Per-machine editor settings, installed tools, Unity licenses, ignored credentials/signing keys, and executable builds require separate setup or backup.

This repository belongs to the copied development project. Restoring it does not restore or change the boss's original repository or VS Code setup.

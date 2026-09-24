# Scoped private backup

The two path lists are explicit repository-relative allowlists, not commands. Review them before staging. They preserve existing metadata and include the imported gameplay source plus the inherited campaign prerequisites described in `../Validation/DeliveryAudit.md`. No staging or commit was performed when preparing them.

- `Backup-Source-Paths.txt`: runtime/imported assets, required adapters, and Editor checks.
- `Backup-Evidence-Paths.txt`: editable recovery inputs, provenance, reviewed outputs and these helper files. Root should append the final compact gameplay MP4 and final new validation reports after verification. Failed/old reports remain clearly named. This list excludes extracted compiled APK packages, caches and raw frames.

Do not add unrelated dirty files to make the diff clean. Do not stage entire Design, Backup or Compile directories. The scoped task checkpoint remains local at `Design/Backup/ReferenceGameplay-Checkpoint-20260924-121611`.

After the intended sources are staged, run from PowerShell 7:

```powershell
& ./Design/ReferenceGameplayImport/Compile/Compile-Staged.ps1
```

This writes exact indexed source blobs to a new ignored `StagedSnapshot-*` directory, compiles runtime then Editor using Unity's existing response files and SDK references, and points Editor compilation at the newly compiled runtime reference DLL. It omits untracked/unstaged C# files even when the working Editor RSP includes them. `-PrepareOnly` exercises snapshot creation without claiming compilation.

A staged source absent from the current default-assembly response files fails early instead of being silently omitted. Use `-RspDirectory` only if Unity has moved its current response files. Source-index changes during compilation invalidate the result. The report lists every source path, index blob ID, omitted file and compiler exit code.

This check covers the two default C# assemblies. It does not validate staged assets/serialization, gameplay behavior, custom assemblies, or ProjectSettings; those remain separate Unity checks. It leaves the real index and all main-project source files unchanged.

"""Capture the authorized host-file overlay; never edits Assets or the Git index."""
from __future__ import annotations

import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
from datetime import datetime, timezone
from zipfile import ZipFile, ZIP_DEFLATED


HERE = Path(__file__).resolve().parent
PROJECT = HERE.parents[2]
BEFORE = HERE.parent / "Before" / "Source"
PRIOR_ZIP = PROJECT / "Design/KingVanguard-Reference-20260929/Delivery/host-snapshots-20260930-043948.zip"
HOSTS = (
    "Assets/JellyGarden/Scripts/GUI/RoyalGameplayPresentation.cs",
    "Assets/JellyGarden/Scripts/LevelManager.cs",
    "Assets/JellyGarden/Scripts/Item.cs",
)


def sha(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def git(*args: str, cwd: Path = PROJECT) -> subprocess.CompletedProcess:
    return subprocess.run(["git", "-c", "core.autocrlf=false", "-c", "core.safecrlf=false", *args], cwd=cwd, capture_output=True, check=False)


def checked_git(*args: str, cwd: Path = PROJECT) -> str:
    result = git(*args, cwd=cwd)
    if result.returncode:
        raise RuntimeError(result.stderr.decode("utf-8", errors="replace"))
    return result.stdout.decode("utf-8").strip()


def main() -> None:
    assert (PROJECT / "Assets/JellyGarden").is_dir(), PROJECT
    stamp = datetime.now(timezone.utc).strftime("%Y%m%d-%H%M%S")
    paths = [item for host in HOSTS for item in (host, host + ".meta")]
    current = {path: (PROJECT / path).read_bytes() for path in paths}
    baseline = {path: (BEFORE / path).read_bytes() for path in HOSTS}
    patches = HERE / "Patches"
    patches.mkdir(exist_ok=True)
    patch_records = []
    for path in HOSTS:
        old_path = (BEFORE / path).relative_to(PROJECT).as_posix()
        result = git("-c", "core.quotePath=false", "diff", "--no-index", "--no-ext-diff", "--binary", "--", old_path, path)
        if result.returncode != 1 or not result.stdout:
            raise RuntimeError(f"Expected a focused baseline change for {path}: {result.stderr!r}")
        # Change only the patch's old-file headers, preserving every hunk byte,
        # including the original files' mixed CRLF/LF line endings.
        lines = result.stdout.splitlines(keepends=True)
        old_bytes, path_bytes = old_path.encode(), path.encode()
        for i, line in enumerate(lines):
            if line.startswith(b"diff --git ") or line.startswith(b"--- "):
                lines[i] = line.replace(b"a/" + old_bytes, b"a/" + path_bytes)
        patch_data = b"".join(lines)
        patch_path = patches / (Path(path).name + ".patch")
        patch_path.write_bytes(patch_data)
        patch_records.append({
            "restorePath": path,
            "patch": patch_path.relative_to(HERE).as_posix(),
            "sha256": sha(patch_data),
            "baselinePath": (BEFORE / path).relative_to(PROJECT).as_posix(),
            "baselineSha256": sha(baseline[path]),
            "resultSha256": sha(current[path]),
        })

    # Apply each patch against the exact saved baseline in an isolated directory,
    # outside the project/repository. This does not use or update any Git index.
    temp_root = Path(tempfile.gettempdir()).resolve()
    scratch = Path(tempfile.mkdtemp(prefix="king-vanguard-host-patch-", dir=temp_root)).resolve()
    assert scratch.parent == temp_root and scratch.name.startswith("king-vanguard-host-patch-")
    verification = []
    try:
        for path, data in baseline.items():
            dest = scratch / path
            dest.parent.mkdir(parents=True, exist_ok=True)
            dest.write_bytes(data)
        patch_args = [str(HERE / record["patch"]) for record in patch_records]
        checked_git("apply", "--check", "--whitespace=nowarn", *patch_args, cwd=scratch)
        checked_git("apply", "--whitespace=nowarn", *patch_args, cwd=scratch)
        for path in HOSTS:
            restored_sha = sha((scratch / path).read_bytes())
            assert restored_sha == sha(current[path]), f"Patch byte mismatch: {path}"
            verification.append(f"PASS focused patch reproduces current bytes: {path} {restored_sha}")
    finally:
        # The resolved target was explicitly checked against the known temp root.
        if scratch.parent != temp_root or not scratch.name.startswith("king-vanguard-host-patch-"):
            raise RuntimeError("Refusing to clean unexpected verification directory")
        shutil.rmtree(scratch)

    archive = HERE / f"host-snapshots-{stamp}.zip"
    assert not archive.exists()
    with ZipFile(archive, "w", compression=ZIP_DEFLATED, compresslevel=9) as z:
        for path in paths:
            z.writestr(path, current[path])

    records = []
    with ZipFile(archive) as z, ZipFile(PRIOR_ZIP) as prior:
        assert z.namelist() == paths
        for path in paths:
            assert z.read(path) == current[path]
            assert (PROJECT / path).read_bytes() == current[path], f"Source changed during capture: {path}"
            record = {"restorePath": path, "bytes": len(current[path]), "sha256": sha(current[path])}
            if (BEFORE / path).is_file():
                before_bytes = (BEFORE / path).read_bytes()
                record.update({"baselinePath": (BEFORE / path).relative_to(PROJECT).as_posix(), "baselineSha256": sha(before_bytes), "changedFromBaseline": before_bytes != current[path]})
            elif path in prior.namelist():
                prior_bytes = prior.read(path)
                record.update({"baselineArchive": PRIOR_ZIP.relative_to(PROJECT).as_posix(), "baselineEntry": path, "baselineSha256": sha(prior_bytes), "changedFromBaseline": prior_bytes != current[path]})
            records.append(record)
            verification.append(f"PASS archive entry and unchanged source SHA-256: {path} {record['sha256']}")

    manifest = {
        "createdUtc": datetime.now(timezone.utc).isoformat(),
        "projectRootAtCapture": str(PROJECT),
        "workingBranch": checked_git("branch", "--show-current"),
        "workingHead": checked_git("rev-parse", "HEAD"),
        "privateBackupBranchHeadAtCapture": checked_git("rev-parse", "king-vanguard-v2-backup"),
        "scope": "Six complete current host files, including earlier/preexisting edits. Three focused source patches use only the saved ActivityFix Before/Source baselines; no HEAD diff is used.",
        "archive": archive.name,
        "archiveSha256": sha(archive.read_bytes()),
        "archiveEntriesVerified": True,
        "focusedPatchesAppliedAndByteVerified": True,
        "gitIndexOrRefsModified": False,
        "files": records,
        "focusedPatches": patch_records,
    }
    manifest_path = HERE / "manifest.json"
    manifest_path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    checksum_lines = [f"{sha(archive.read_bytes())}  {archive.name}", f"{sha(manifest_path.read_bytes())}  manifest.json"]
    checksum_lines += [f"{record['sha256']}  {record['patch']}" for record in patch_records]
    (HERE / "SHA256SUMS.txt").write_text("\n".join(checksum_lines) + "\n", encoding="utf-8")
    (HERE / "verification.txt").write_text("\n".join(verification) + "\nPASS no Assets files, Git index, commits or refs modified by this tool.\n", encoding="utf-8")
    print(json.dumps({"archive": str(archive), "sha256": manifest["archiveSha256"], "filesVerified": len(records), "patchesVerified": len(patch_records), "metaChanges": [r['restorePath'] for r in records if r['restorePath'].endswith('.meta') and r.get('changedFromBaseline')]}, indent=2))


if __name__ == "__main__":
    main()

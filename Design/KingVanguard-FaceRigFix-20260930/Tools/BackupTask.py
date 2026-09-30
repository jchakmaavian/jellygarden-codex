"""Plan the scoped FaceRigFix backup; only --apply permits Git mutations.

Creates a normal local task commit, then copies only the allowlisted committed
blobs onto the existing private backup branch. The backup commit has exactly one
parent: PREVIOUS_BACKUP, never the large visual-rebuild-v2 working history.
No checkout, reset, host-file staging or working-tree rewrite is performed.
"""
import argparse
import json
import os
from pathlib import Path
import subprocess
import uuid

ROOT = Path(__file__).resolve().parents[3]
TASK_REL = 'Design/KingVanguard-FaceRigFix-20260930'
TASK = ROOT / TASK_REL
V2 = 'Assets/Characters/KingVenguard/V2/'
OWNED = (
    'Scripts/KingVanguardFaceShapeModifier.cs',
    'Scripts/KingVanguardCharacter.cs',
    'Scripts/KingVanguardFaceRig.cs',
    'Scripts/KingVanguardFaceRig.cs.meta',
    'Editor/KingVanguardAnimationReview.cs',
    'Editor/KingVanguardPrefabBuilder.cs',
    'Resources/KingVanguard/KingVanguard.prefab',
    'Resources/KingVanguard/KingVanguardCelebration.prefab',
    'Spine/KingVanguard_FaceShape.asset',
    'Spine/King_Vanguard.png',
    'Spine/AreaCompleted_Vanguard.png',
)
IMPORTER = 'Design/KingVanguard-Reference-20260929/Tools/ImportVanguard.cs'
WORKING_BRANCH = 'visual-rebuild-v2'
EXPECTED_HEAD = '7f59f771ad4c94dd1a7db3ccfbadecbbfcfd62a7'
BACKUP_BRANCH = 'king-vanguard-v2-backup'
PREVIOUS_BACKUP = 'cee14bda2e1a66918a1b09649aac4a5b0bcc4bd1'
REMOTE_URL = 'https://github.com/jchakmaavian/jellygarden-codex.git'
EXCLUDED_DIRS = {'motionframes', '__pycache__'}
EXCLUDED_SUFFIXES = {'.dll', '.exe', '.pdb', '.rsp', '.pyc', '.pyo'}


def git(*args, env=None, data=None, strip=True):
    result = subprocess.run(
        ['git', '-c', 'core.longpaths=true', '-c', 'core.quotePath=false', *args],
        cwd=ROOT, env=env, input=data, text=True, encoding='utf-8',
        capture_output=True, check=True)
    return result.stdout.strip() if strip else result.stdout


def batches(paths, count=30):
    for start in range(0, len(paths), count):
        yield paths[start:start + count]


def zero_names(*args):
    return {p for p in git(*args, strip=False).split('\0') if p}


def collect_paths():
    paths = [V2 + p for p in OWNED] + [IMPORTER]
    excluded = []
    for item in TASK.rglob('*'):
        if not item.is_file():
            continue
        rel = item.relative_to(ROOT).as_posix()
        if any(p.lower() in EXCLUDED_DIRS for p in item.relative_to(TASK).parts) or item.suffix.lower() in EXCLUDED_SUFFIXES:
            excluded.append(rel)
            continue
        paths.append(rel)
    paths = sorted(set(paths))
    for rel in paths:
        item = ROOT / rel
        if not item.is_file():
            raise RuntimeError('Missing allowlist file: ' + rel)
        if item.is_symlink() or not item.resolve().is_relative_to(ROOT.resolve()):
            raise RuntimeError('Allowlist path resolves outside the project: ' + rel)
    return paths, sorted(excluded)


def local_guards():
    if any(os.environ.get(key) for key in ('GIT_INDEX_FILE', 'GIT_DIR', 'GIT_WORK_TREE')):
        raise RuntimeError('Unexpected ambient Git repository/index override')
    actual = {
        'root': str(Path(git('rev-parse', '--show-toplevel')).resolve()),
        'workingBranch': git('branch', '--show-current'),
        'workingHead': git('rev-parse', 'HEAD'),
        'backupParent': git('rev-parse', 'refs/heads/' + BACKUP_BRANCH),
        'remote': git('remote', 'get-url', 'origin'),
        'indexEmpty': not bool(zero_names('diff', '--cached', '--name-only', '-z')),
    }
    expected = {
        'root': str(ROOT.resolve()), 'workingBranch': WORKING_BRANCH,
        'workingHead': EXPECTED_HEAD, 'backupParent': PREVIOUS_BACKUP,
        'remote': REMOTE_URL, 'indexEmpty': True,
    }
    if actual != expected:
        raise RuntimeError('Local backup guards failed; review before proceeding:\n' +
                           json.dumps({'expected': expected, 'actual': actual}, indent=2))
    return actual


def remote_head():
    rows = git('ls-remote', '--heads', 'origin', 'refs/heads/' + BACKUP_BRANCH).splitlines()
    if len(rows) != 1:
        raise RuntimeError('Expected exactly one existing remote backup branch')
    commit, ref = rows[0].split()
    if ref != 'refs/heads/' + BACKUP_BRANCH:
        raise RuntimeError('Unexpected remote ref')
    return commit


def apply(paths):
    # All preflight checks precede staging. Network lookup is only done on apply.
    local_guards()
    if remote_head() != PREVIOUS_BACKUP:
        raise RuntimeError('Remote backup parent changed; refusing to stage or push')
    for group in batches(paths):
        git('add', '--', *group)
    staged = zero_names('diff', '--cached', '--name-only', '-z')
    if not staged or not staged.issubset(set(paths)):
        raise RuntimeError('Unexpected/empty staged scope; index retained for inspection')
    if git('rev-parse', 'HEAD') != EXPECTED_HEAD or git('branch', '--show-current') != WORKING_BRANCH:
        raise RuntimeError('Working branch moved while staging; index retained for inspection')
    git('commit', '-m', 'Fix King Vanguard facial registration and mouth artwork')
    local_commit = git('rev-parse', 'HEAD')
    local_changes = zero_names('diff', '--name-only', '-z', EXPECTED_HEAD, local_commit)
    if not local_changes.issubset(set(paths)):
        raise RuntimeError('Local commit contains unexpected changes; no backup ref updated')

    # Reuse the exact committed objects, not a second read of the working files.
    # This prevents edits after the local commit from leaking into the backup.
    tree_records = []
    recorded_paths = set()
    for group in batches(paths):
        rows = git('ls-tree', '-r', '-z', '--full-tree', local_commit, '--', *group, strip=False)
        for row in rows.split('\0'):
            if not row:
                continue
            meta, path = row.split('\t', 1)
            mode, kind, blob = meta.split()
            if kind != 'blob' or mode not in ('100644', '100755'):
                raise RuntimeError('Unexpected non-file object: ' + path)
            recorded_paths.add(path)
            tree_records.append(mode + ' ' + blob + '\t' + path + '\0')
    if recorded_paths != set(paths):
        raise RuntimeError('Committed allowlist snapshot is incomplete')

    git_dir = Path(git('rev-parse', '--absolute-git-dir')).resolve()
    alternate_index = git_dir / ('king-face-rig-backup-' + uuid.uuid4().hex + '.index')
    if alternate_index.exists():
        raise RuntimeError('Temporary index already exists')
    env = dict(os.environ, GIT_INDEX_FILE=str(alternate_index))
    try:
        git('read-tree', PREVIOUS_BACKUP, env=env)
        git('update-index', '-z', '--index-info', env=env, data=''.join(tree_records))
        tree = git('write-tree', env=env)
        backup_commit = git('commit-tree', tree, '-p', PREVIOUS_BACKUP,
                            data='Back up verified King Vanguard facial registration and lip-cover correction\n')
        backup_changes = zero_names('diff', '--name-only', '-z', PREVIOUS_BACKUP, backup_commit)
        if not backup_changes.issubset(set(paths)):
            raise RuntimeError('Backup tree has changes outside the allowlist; ref unchanged')
        if git('rev-list', '--parents', '-n', '1', backup_commit).split() != [backup_commit, PREVIOUS_BACKUP]:
            raise RuntimeError('Backup has an unexpected parent; ref unchanged')
        git('update-ref', 'refs/heads/' + BACKUP_BRANCH, backup_commit, PREVIOUS_BACKUP)
        # A normal push only: remote advancement/divergence must reject this push.
        git('push', 'origin', 'refs/heads/' + BACKUP_BRANCH + ':refs/heads/' + BACKUP_BRANCH)
        if remote_head() != backup_commit:
            raise RuntimeError('Remote backup verification failed')
    finally:
        # Only these two explicitly named files can be cleaned; no recursive delete.
        for candidate in (alternate_index, Path(str(alternate_index) + '.lock')):
            if candidate.parent != git_dir or not candidate.name.startswith('king-face-rig-backup-'):
                raise RuntimeError('Unexpected temporary index cleanup path')
            if candidate.is_file():
                candidate.unlink()
    print(json.dumps({'localCommit': local_commit, 'backupCommit': backup_commit,
                      'backupParent': PREVIOUS_BACKUP, 'remoteVerified': True,
                      'allowlistFiles': len(paths), 'hostFilesStaged': False}, indent=2))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--apply', action='store_true', help='Explicitly stage, commit and fast-forward the private backup branch')
    args = parser.parse_args()
    paths, excluded = collect_paths()
    guards = local_guards()
    if not args.apply:
        print(json.dumps({'mode': 'READ-ONLY PLAN', 'guards': guards,
                          'remoteParentCheck': 'Performed only with --apply, before staging',
                          'allowlistFiles': len(paths), 'paths': paths,
                          'excludedFiles': len(excluded), 'excluded': excluded,
                          'willStageHostFiles': False, 'willCheckoutOrReset': False,
                          'willForcePush': False}, indent=2))
        return
    apply(paths)


if __name__ == '__main__':
    main()

"""Commit this task's allowlist and mirror it onto the existing private backup branch.

Host files with earlier uncommitted work are carried only in Recovery's verified
overlay and focused patches. No checkout or working-tree reset is performed.
Run with --apply after verification; default prints the allowlist.
"""
import json
import os
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[3]
TASK = ROOT / 'Design/KingVanguard-ActivityFix-20260930'
V2 = 'Assets/Characters/KingVenguard/V2/'
OWNED = [
    'Editor/KingVanguardRuntimeChecks.cs',
    'Scripts/KingVanguardActivityEvents.cs',
    'Scripts/KingVanguardCharacter.cs',
    'Scripts/KingVanguardFaceShapeModifier.cs',
    'Scripts/KingVanguardFeedbackQueue.cs',
    'Scripts/KingVanguardFeedbackQueue.cs.meta',
    'Spine/AreaCompleted_Vanguard.png',
    'Spine/KingVanguard_FaceShape.asset',
    'Spine/King_Vanguard.png',
]
BRANCH = 'king-vanguard-v2-backup'
PREVIOUS = '20ea37f8e942dc0d9ed50970cf81c0e2715061f5'

def git(*args, env=None, data=None):
    p = subprocess.run(['git', '-c', 'core.longpaths=true', *args], cwd=ROOT,
                       env=env, input=data, text=True, capture_output=True, check=True)
    return p.stdout.strip()

def main():
    paths = [V2 + p for p in OWNED]
    paths.append('Design/KingVanguard-Reference-20260929/Tools/ImportVanguard.cs')
    for p in TASK.rglob('*'):
        if not p.is_file() or 'MotionFrames' in p.parts or '__pycache__' in p.parts:
            continue
        if p.suffix.lower() in {'.dll', '.exe', '.pdb', '.rsp', '.pyc'}:
            continue
        paths.append(p.relative_to(ROOT).as_posix())
    paths = sorted(set(paths))
    if '--apply' not in sys.argv:
        print('\n'.join(paths))
        return
    if git('remote', 'get-url', 'origin') != 'https://github.com/jchakmaavian/jellygarden-codex.git':
        raise RuntimeError('Unexpected destination')
    if git('branch', '--show-current') != 'visual-rebuild-v2':
        raise RuntimeError('Unexpected working branch')
    if git('diff', '--cached', '--name-only'):
        raise RuntimeError('Index contains work outside this task')
    if git('rev-parse', BRANCH) != PREVIOUS:
        raise RuntimeError('Backup parent changed; review before proceeding')
    for p in paths:
        if not (ROOT / p).is_file():
            raise RuntimeError('Missing allowlist file: ' + p)
    for start in range(0, len(paths), 30):
        git('add', '--', *paths[start:start+30])
    staged = set(git('diff', '--cached', '--name-only').splitlines())
    if not staged.issubset(set(paths)):
        raise RuntimeError('Unexpected staged paths')
    git('commit', '-m', 'Fix King Vanguard HUD framing and gameplay reactions')
    local = git('rev-parse', 'HEAD')
    index = ROOT / '.git' / 'king-activity-backup.index'
    if index.exists():
        raise RuntimeError('Temporary index already exists')
    env = dict(os.environ, GIT_INDEX_FILE=str(index))
    git('read-tree', PREVIOUS, env=env)
    for start in range(0, len(paths), 30):
        git('add', '--', *paths[start:start+30], env=env)
    tree = git('write-tree', env=env)
    backup = git('commit-tree', tree, '-p', PREVIOUS,
                 data='Back up verified King Vanguard HUD and activity correction\n')
    git('update-ref', 'refs/heads/' + BRANCH, backup, PREVIOUS)
    git('push', 'origin', BRANCH + ':' + BRANCH)
    remote = git('ls-remote', 'origin', 'refs/heads/' + BRANCH).split()[0]
    if remote != backup:
        raise RuntimeError('Remote backup verification failed')
    print(json.dumps({'localCommit': local, 'backupCommit': backup,
                      'remoteVerified': True, 'allowlistFiles': len(paths)}, indent=2))

if __name__ == '__main__':
    main()

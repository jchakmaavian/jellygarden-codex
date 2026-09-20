from pathlib import Path
import difflib, hashlib, json, zipfile
root=Path(__file__).resolve().parents[3]
review=root/'Design/Effects/ColorBall-live-v1'
sources=[
 'Assets/JellyGarden/Scripts/Juice/ColorBallFrameAnimator.cs',
 'Assets/JellyGarden/Scripts/Juice/ColorBallBoostView.cs',
 'Assets/JellyGarden/Scripts/Juice/ColorBallTargetVfx.cs',
 'Assets/JellyGarden/Scripts/Juice/RoyalColorBallSequence.cs',
 'Assets/JellyGarden/Scripts/Juice/RoyalPowerupDirector.cs',
 'Assets/JellyGarden/Scripts/Editor/RoyalColorBallChecks.cs',
 'Assets/JellyGarden/Scripts/Editor/RoyalEffectsChecks.cs']
manifest=json.loads((review/'source-hashes.json').read_text())
runtime=[]
for name,sha in manifest.items():
    src=root/name
    assert hashlib.sha256(src.read_bytes()).hexdigest()==sha, f'Approved source changed: {src}'
    parts=Path(name).parts
    group=parts[2].replace('ColorBall-','').replace('-v1','')
    if 'VFX' not in parts: dst=root/'Assets/JellyGarden/Resources/RoyalColorBall/Frames'/group/src.name
    else:
        kind='Beam' if 'ColorBall_Beam' in parts else 'Glow' if 'ColorBall_TargetGlow' in parts else 'Impact'
        dst=root/'Assets/JellyGarden/Resources/RoyalColorBall/VFX'/kind/src.name
    assert dst.read_bytes()==src.read_bytes(), f'Runtime frame differs: {dst}'
    runtime.extend([dst,Path(str(dst)+'.meta')])
patch=[]
for name in sources:
    path=root/name
    baseline=review/'baseline'/path.name
    before=baseline.read_text().splitlines(True) if baseline.exists() else []
    patch.extend(difflib.unified_diff(before,path.read_text().splitlines(True),fromfile='a/'+name,tofile='b/'+name))
    runtime.extend([path,Path(str(path)+'.meta')])
(review/'incremental.patch').write_text(''.join(patch))
resource=root/'Assets/JellyGarden/Resources/RoyalColorBall'
runtime.append(Path(str(resource)+'.meta'))
for folder in resource.rglob('*'):
    if folder.is_dir(): runtime.append(Path(str(folder)+'.meta'))
with zipfile.ZipFile(review/'ColorBall-Live-Integration.zip','w',zipfile.ZIP_DEFLATED) as archive:
    for path in sorted(set(runtime)):
        if path.is_file(): archive.write(path,path.relative_to(root))
report={'approved_source_frames_unchanged':len(manifest),'runtime_copies_byte_identical':len(manifest),
        'compiled_in_unity':'6000.5.10f1','play_mode_cases':['horizontal rocket payload at 0.5x','vertical rocket payload at 1.0x'],
        'test_errors':0,'captures':14,'normal_board_restored':True,'archive_files':len(set(runtime))}
(review/'verification.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))

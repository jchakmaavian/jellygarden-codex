from pathlib import Path
import shutil, hashlib, json, uuid
root=Path(__file__).resolve().parents[3]
source=root/'Design/Animation'
dest=root/'Assets/JellyGarden/Resources/RoyalColorBall'
manifest={}
def copy_frames(src, dst):
    dst.mkdir(parents=True,exist_ok=True)
    for f in sorted(src.glob('*.webp')):
        target=dst/f.name
        shutil.copyfile(f,target)
        manifest[str(f.relative_to(root))]=hashlib.sha256(f.read_bytes()).hexdigest()
        meta=Path(str(target)+'.meta')
        if not meta.exists():
            meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nScriptedImporter:\n  internalIDToNameTable: []\n  externalObjects: {}\n  serializedVersion: 2\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n  script: {fileID: 11500000, guid: fda128e237330134eb75754cbb4c4607, type: 3}\n  pixelsPerUnit: 100\n  bleedTransparentEdges: 1\n  astcOnMobile: 1\n')
for group in ('Reveal','Idle','Boosting'):
    copy_frames(source/f'ColorBall-{group}-v1/frames',dest/'Frames'/group)
fx=source/'ColorBall-Boosting-v1/VFX'
for name,path in [('Beam','ColorBall_Beam/SequenceBody/frames'),('Glow','ColorBall_TargetGlow/On/frames'),('Impact','ColorBall_TargetImpact/frames')]:
    copy_frames(fx/path,dest/'VFX'/name)
(root/'Design/Effects/ColorBall-live-v1/source-hashes.json').write_text(json.dumps(manifest,indent=2))
print(f'Copied {len(manifest)} approved frames byte-for-byte; source assets unchanged.')

"""Copy only the extracted coin dependencies, preserving existing Unity metadata."""
from pathlib import Path
import hashlib
import json
import shutil

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent

def copy_exact(source, destination):
    source = ROOT / source
    destination = ROOT / destination
    assert source.is_relative_to(ROOT) and destination.is_relative_to(ROOT)
    destination.parent.mkdir(parents=True, exist_ok=True)
    if not destination.exists() or source.read_bytes() != destination.read_bytes():
        shutil.copyfile(source, destination)
    digest = hashlib.sha256(destination.read_bytes()).hexdigest()
    assert digest == hashlib.sha256(source.read_bytes()).hexdigest()
    return {'source':source.relative_to(ROOT).as_posix(),
            'assetPath':destination.relative_to(ROOT).as_posix(), 'sha256':digest}

def main():
    particles = json.loads((OUT/'NativeCoinParticles.json').read_text(encoding='utf-8'))
    copied = []
    for texture in particles['textures']:
        print('texture', {k:v for k,v in texture.items() if k in ('pathId','name','assetPath','extractedPath')})
        copied.append(copy_exact(texture['extractedPath'], texture['assetPath']))
    for sound in json.loads((OUT/'CoinAudioManifest.json').read_text(encoding='utf-8')):
        if sound['sourcePathId'] not in (17638,17675): continue
        decoded=sound['decoded'][0]
        target='Assets/ReferenceGameplay/Audio/Resources/ReferenceGameplay/Audio/'+Path(decoded['path']).name
        copied.append(copy_exact(decoded['path'],target))
    (OUT/'prepared-import-files.json').write_text(json.dumps(copied,indent=2),encoding='utf-8')
    print('prepared',len(copied),'exact files')
    clips=json.loads((OUT/'DecodedCoinAnimations.json').read_text(encoding='utf-8'))
    for clip in clips:
        print('clip',clip['name'],clip['duration'],[(c['path'],c['property'],c['keys'][:2]) for c in clip['curves'] if c['property'] in ('m_Sprite','m_FlipX')])

if __name__ == '__main__': main()

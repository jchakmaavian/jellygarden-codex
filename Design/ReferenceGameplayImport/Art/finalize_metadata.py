from pathlib import Path
import json,hashlib,math
ROOT=Path(__file__).resolve().parents[3]
ART=ROOT/'Assets/ReferenceGameplay/Art'
def guid(key):return hashlib.md5(('RoyalThrone.ReferenceGameplay.'+key).encode()).hexdigest()
def write_once(path,text):
 if not path.exists():path.write_text(text)
manifest=json.loads((ART/'NativeSpriteCatalog.json').read_text())
for a in manifest['textures']:
 path=ROOT/a['assetPath'];g=guid('native-texture:'+str(a['pathId']));m=2**math.ceil(math.log2(max(a['width'],a['height'])))
 a['guid']=g
 write_once(path.with_suffix('.png.meta'),f'''fileFormatVersion: 2
guid: {g}
TextureImporter:
  serializedVersion: 13
  internalIDToNameTable: []
  externalObjects: {{}}
  mipmaps:
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 0
  textureFormat: 1
  maxTextureSize: {m}
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  alphaUsage: 1
  alphaIsTransparency: 0
  textureType: 0
  textureShape: 1
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: {m}
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 100
    overridden: 0
  userData: Packaged native atlas. Keep exact decoded pixels and dimensions.
  assetBundleName:
  assetBundleVariant:
''')
(ART/'NativeSpriteCatalog.json').write_text(json.dumps(manifest,indent=2))
(Path(__file__).parent/'NativeSpriteCatalog.json').write_text(json.dumps(manifest,indent=2))
for p in [ART,*ART.rglob('*')]:
 if p.suffix=='.meta':continue
 if p.is_dir():write_once(Path(str(p)+'.meta'),f'fileFormatVersion: 2\nguid: {guid(p.relative_to(ROOT).as_posix())}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
 elif p.suffix=='.json':write_once(Path(str(p)+'.meta'),f'fileFormatVersion: 2\nguid: {guid(p.relative_to(ROOT).as_posix())}\nTextScriptImporter:\n  externalObjects: {{}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
p=ROOT/'Assets/ReferenceGameplay/Editor/ReferenceSpriteImporter.cs'
write_once(Path(str(p)+'.meta'),f'fileFormatVersion: 2\nguid: {guid(p.relative_to(ROOT).as_posix())}\nMonoImporter:\n  externalObjects: {{}}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {{instanceID: 0}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
print('Stable folder, atlas, catalog and editor-script identities ready.')

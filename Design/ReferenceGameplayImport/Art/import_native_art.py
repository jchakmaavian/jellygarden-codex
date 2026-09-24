"""Recover packaged gameplay pixels and native Sprite metadata without redesign."""
from pathlib import Path
import json,struct,hashlib,re,shutil,ctypes as C,math
from PIL import Image,ImageDraw
from inspect_art import ROOT,d,by,out,string

HERE=Path(__file__).parent
ART=ROOT/'Assets/ReferenceGameplay/Art'
SPRITES=ART/'Resources/ReferenceGameplay/Sprites'
ATLAS=ART/'Atlases'
ORIGINAL=ROOT/'Design/Inspection-Levels01-50/ReferenceArt'
PACKAGE=ROOT/'Design/Inspection-Levels01-50/ReferencePackage/datapack.unity3d_data'
for folder in (SPRITES,ATLAS):folder.mkdir(parents=True,exist_ok=True)

def guid(key):return hashlib.md5(('RoyalThrone.ReferenceGameplay.'+key).encode()).hexdigest()
def raw(pid):
 r=by[pid];return d[r['byte_offset']:r['byte_offset']+r['size']]

textures={}
def texture(pid):
 if pid in textures:return textures[pid]
 b=raw(pid);name,p=string(b,0);w,h=struct.unpack_from('<II',b,p+8);fmt=struct.unpack_from('<I',b,p+24)[0]
 off,size=struct.unpack_from('<QI',b,p+88);stream,_=string(b,p+100)
 reusable=ORIGINAL/(name+'.png')
 if reusable.exists():im=Image.open(reusable).convert('RGBA')
 else:
  with (PACKAGE/stream).open('rb') as f:f.seek(off);payload=f.read(size)
  if 48<=fmt<=53:
   lib=C.CDLL(r'C:\Program Files\Unity\Hub\Editor\6000.5.10f1\Editor\Data\Tools\astcenc-sse2.dll')
   lib.astcenc_config_init.argtypes=[C.c_int,C.c_uint,C.c_uint,C.c_uint,C.c_float,C.c_uint,C.c_void_p]
   lib.astcenc_context_alloc.argtypes=[C.c_void_p,C.c_uint,C.POINTER(C.c_void_p)]
   lib.astcenc_decompress_image.argtypes=[C.c_void_p,C.c_void_p,C.c_size_t,C.c_void_p,C.c_void_p,C.c_uint]
   lib.astcenc_context_free.argtypes=[C.c_void_p]
   class AI(C.Structure):_fields_=[('x',C.c_uint),('y',C.c_uint),('z',C.c_uint),('type',C.c_int),('data',C.POINTER(C.c_void_p))]
   block=[4,5,6,8,10,12][fmt-48];cfg=C.create_string_buffer(4096)
   assert lib.astcenc_config_init(1,block,block,1,0.,16,cfg)==0
   ctx=C.c_void_p();assert lib.astcenc_context_alloc(cfg,1,C.byref(ctx))==0
   dst=C.create_string_buffer(w*h*4);slices=(C.c_void_p*1)(C.cast(dst,C.c_void_p));img=AI(w,h,1,0,slices);swizzle=(C.c_int*4)(0,1,2,3);src=C.create_string_buffer(payload)
   assert lib.astcenc_decompress_image(ctx,src,len(payload),C.byref(img),swizzle,0)==0
   lib.astcenc_context_free(ctx);payload=dst.raw;fmt=4
  assert fmt in (3,4),(name,fmt)
  im=Image.frombytes('RGB' if fmt==3 else 'RGBA',(w,h),payload).convert('RGBA').transpose(Image.Transpose.FLIP_TOP_BOTTOM)
 assert im.size==(w,h)
 dst=ATLAS/(name+'.png')
 if reusable.exists():shutil.copy2(reusable,dst)
 else:im.save(dst)
 textures[pid]=(im,dict(name=name,pathId=pid,format=fmt,width=w,height=h,stream=stream,streamOffset=off,streamSize=size,assetPath=dst.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(dst.read_bytes()).hexdigest()))
 return textures[pid]

def category(a):
 n=a['name']
 if a['texture_name']=='ColorParticleAtlas':return 'Fragments'
 if a['texture_name']=='AdditiveParticleAtlas':return 'EffectSprites'
 if 'SpecialItems' in n or n.startswith('light_ball'):return 'Boosters'
 if n.startswith('Items-MatchItem'):return 'Pieces'
 if n.startswith('Items-CoinItem') or a['texture_name']=='LevelWinAtlas':return 'Rewards'
 if n.startswith('Border-') or n=='cell_front':return 'Board'
 if 'goal' in n.lower():return 'Goals'
 return 'Blockers'

def sprite_meta(a,path,g):
 hints=dict(pixelsPerUnit=a['ppu'],pivotX=a['pivot'][0],pivotY=a['pivot'][1],borderX=a['border'][0],borderY=a['border'][1],borderZ=a['border'][2],borderW=a['border'][3])
 p=a['pivot'];v=a['border'];maxsize=max(32,2**math.ceil(math.log2(max(a['rect'][2:]))))
 return f'''fileFormatVersion: 2
guid: {g}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 0
  textureFormat: 1
  maxTextureSize: {maxsize}
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  spriteMode: 1
  spriteExtrude: 0
  spriteMeshType: 0
  alignment: 9
  spritePivot: {{x: {p[0]}, y: {p[1]}}}
  spritePixelsToUnits: {a['ppu']}
  spriteBorder: {{x: {v[0]}, y: {v[1]}, z: {v[2]}, w: {v[3]}}}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 0
  textureType: 8
  textureShape: 1
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: {maxsize}
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 100
    overridden: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: {guid('sprite:'+str(a['path_id']))}
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {{}}
  userData: '{json.dumps(hints,separators=(',',':'))}'
  assetBundleName:
  assetBundleVariant:
'''

selected=[a for a in out if a['texture_name'] in ('GameplayAtlas','GameplayExtensionAtlas','ColorParticleAtlas','AdditiveParticleAtlas','IceCubeAtlas','HoneyJarAtlas') or (a['texture_name']=='LevelWinAtlas' and a['name']=='Level_complete_popup_star')]
catalog=[];failed=[]
for a in selected:
 try:
  im,tex=texture(a['texture_path_id']);x,y,w,h=map(round,a['rect'])
  assert 0<=x<x+w<=im.width and 0<=y<y+h<=im.height,(a['name'],a['rect'],im.size)
  filename=re.sub(r'[^\w.-]','_',a['name'])+'__'+str(a['path_id'])+'.png';path=SPRITES/filename
  cut=im.crop((x,im.height-y-h,x+w,im.height-y))
  g=guid('resources.assets:'+str(a['path_id']))
  path.with_suffix('.png.meta').write_text(sprite_meta(a,path,g))
  cut.save(path)
  catalog.append(dict(name=a['name'],sourceFile='resources.assets',sourcePathId=a['path_id'],sourceTexturePathId=a['texture_path_id'],textureName=a['texture_name'],rect=dict(zip(('x','y','width','height'),a['rect'])),pivot=dict(zip(('x','y'),a['pivot'])),offset=dict(zip(('x','y'),a['offset'])),border=dict(zip(('x','y','z','w'),a['border'])),pixelsPerUnit=a['ppu'],assetPath=path.relative_to(ROOT).as_posix(),resourcesPath='ReferenceGameplay/Sprites/'+path.stem,guid=g,category=category(a),width=w,height=h,sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
 except Exception as e:failed.append(dict(name=a['name'],pathId=a['path_id'],reason=str(e)))

manifest=dict(source='Royal_Throne-rf.apk',apkSha256='4493ea05602f18aa3b26ab989acaee33f652aa2a85bd9f3dc3f92360441935a1',nativeCellPitch=140,alpha='Straight color RGBA. Native board renderers use built-in Sprites-Default.',sprites=catalog,textures=[v[1] for v in textures.values()],incomplete=failed)
(ART/'NativeSpriteCatalog.json').write_text(json.dumps(manifest,indent=2))
(HERE/'NativeSpriteCatalog.json').write_text(json.dumps(manifest,indent=2))

lookup={a['name']:a for a in catalog}
keys={
 'ruby':'Items-MatchItem-book','shield':'Items-MatchItem-shield','crown':'Items-MatchItem-crown','leaf':'Items-MatchItem-leaf','lily':'Items-MatchItem-gem','gem':'Items-MatchItem-gem',
 'rocket':'Items-SpecialItems-RocketItem-horizontal_rocket','rocket-vertical':'Items-SpecialItems-RocketItem-vertical_rocket','tnt':'Items-SpecialItems-TntItem-tnt','orb':'Items-SpecialItems-LightballItem-default','propeller-blades-idle':'Items-SpecialItems-PropellerItem-01',
 'propeller-body':'Items-SpecialItems-PropellerItem-body','propeller-pin':'Items-SpecialItems-PropellerItem-pin_01','propeller-rotor':'Items-SpecialItems-PropellerItem-2d_pervane _00000',
 'rocket-up':'Items-SpecialItems-RocketItem-vertical_rocket_part_top','rocket-down':'Items-SpecialItems-RocketItem-vertical_rocket_part_bottom',
 'rocket-left':'Items-SpecialItems-RocketItem-horizontal_rocket_part_left','rocket-right':'Items-SpecialItems-RocketItem-horizontal_rocket_part_right',
 'cell-front-effect':'cell_front','frame-side':'Border-side','frame-inner-corner':'Border-inner_corner','frame-outer-corner':'Border-outer_corner','frame-fill':'Border-fill',
 'crate':'Items-BoxItem-box_1','box-2':'Items-BoxItem-box_2','box-3':'Items-BoxItem-box_3','grass-full':'StaticItems-Grass-light_base01','coin':'Items-CoinItem-coin',
 'hit-flash':'add_hit_blast','sparkle-01':'additive_star_01','clear-ring':'additive_ring_01','glow':'additive_dot_01','beam':'add_lightline','smoke':'add_smoke_soft'
}
mapping=[]
for key,name in keys.items():
 if name in lookup:
  a=lookup[name];mapping.append(dict(key=key,name=name,resourcesPath=a['resourcesPath'],sourcePathId=a['sourcePathId'],width=a['width'],height=a['height'],pixelsPerUnit=a['pixelsPerUnit'],pivot=a['pivot']))
 else:print('KEY UNRESOLVED',key,name)
(HERE/'runtime-key-candidates.json').write_text(json.dumps(dict(entries=mapping,unavailable=['No sixth orange match piece appears in the recovered GameplayAtlas; reference normal set is five pieces.']),indent=2))

tiles=[]
for m in mapping:
 a=lookup[m['name']];im=Image.open(ROOT/a['assetPath']);im.thumbnail((150,150),Image.Resampling.LANCZOS)
 tile=Image.new('RGBA',(210,202),(77,85,117,255));tile.alpha_composite(im,((210-im.width)//2,12+(150-im.height)//2));draw=ImageDraw.Draw(tile);draw.text((8,166),m['key'],fill='white');draw.text((8,182),f"ID {a['sourcePathId']} | {a['width']}x{a['height']} | PPU {a['pixelsPerUnit']:g}",fill='white');tiles.append(tile)
sheet=Image.new('RGBA',(210*6,202*math.ceil(len(tiles)/6)),(25,30,42,255))
for i,t in enumerate(tiles):sheet.alpha_composite(t,((i%6)*210,(i//6)*202))
sheet.convert('RGB').save(HERE/'Native-Gameplay-Sprites-Review.jpg',quality=94)
print('Imported',len(catalog),'sprites,',len(textures),'source atlases, incomplete',failed)

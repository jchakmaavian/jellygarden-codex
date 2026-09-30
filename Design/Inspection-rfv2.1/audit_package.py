from pathlib import Path
import sys,zipfile,json,collections,hashlib
root=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(root/'Design/ReferenceGameplayImport/Media/Tools/Python'))
import UnityPy
out=Path(__file__).parent
z=zipfile.ZipFile(out/'split_UnityDataAssetPack.apk')
present={Path(n).name for n in z.namelist()}
configs={}
for n in z.namelist():
 if n.endswith('_bundle_config.json'):
  conf=json.loads(z.read(n)); names=[b['n'] for g in conf.get('groups',[]) for b in g.get('singleBundleConfigs',[])]
  configs[Path(n).name]={'listed':len(names),'present':[s for s in names if s in present],'not_in_pack':[s for s in names if s not in present]}
report={'apk_sha256':hashlib.sha256((root/'Design/Backup/apk/Royal_Throne_rfv2.1.apk').read_bytes()).hexdigest(),'configs':configs,'bundles':[]}
for apk,entry in [('base.apk','assets/bin/Data/data.unity3d'),('split_UnityDataAssetPack.apk','assets/bin/Data/datapack.unity3d')]:
 data=zipfile.ZipFile(out/apk).read(entry)
 env=UnityPy.load(data)
 counts=collections.Counter(o.type.name for o in env.objects)
 names=[];errors=[]
 for o in env.objects:
  if o.type.name not in ('Material','Shader','Texture2D','Sprite','AnimationClip','AnimatorController','AudioClip','TextAsset'):continue
  try:
   ob=o.read(); name=getattr(ob,'m_Name','');names.append({'type':o.type.name,'name':name})
  except Exception as e:errors.append({'type':o.type.name,'error':str(e)[:160]})
 record={'entry':entry,'objects':sum(counts.values()),'types':dict(counts),'read_errors':errors,'named_assets':names}
 report['bundles'].append(record)
 print(entry,dict(counts),'read_errors',len(errors),flush=True)
(out/'inspection.json').write_text(json.dumps(report,indent=2))
print('CONFIG SUMMARY',json.dumps({k:{'listed':v['listed'],'present':len(v['present']),'not_in_pack':len(v['not_in_pack'])} for k,v in configs.items()}))

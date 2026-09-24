from pathlib import Path
import sys,json
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'Design/ReferenceGameplayImport/Media/Tools'))
from extract_media import tree,pid,objects,traverse
OUT=Path(__file__).parent
GO={'cupboard':52111,'potion':52083,'potion-bottle':33182,'mailbox':52138,'egg':52129,'grass':33165}
all=[]
for key,id in GO.items():
 go=tree(id);root=pid(go['m_Component'][0]['component']);nodes=[]
 print('\nBLOCKER',key,id,'root',root)
 for tid,g,path in traverse(root):
  tr=tree(tid);comps=[]
  print(path,'active',g['m_IsActive'],'position',tr['m_LocalPosition'],'scale',tr['m_LocalScale'])
  for c in g['m_Component']:
   p=pid(c['component']);typ=objects[p].type.name
   if typ=='MonoBehaviour':
    comps.append(dict(pathId=p,type=typ,data={'unavailable':'IL2CPP script fields stripped; raw object retained in original APK'}));continue
   t=tree(p)
   if typ=='Transform':continue
   comps.append(dict(pathId=p,type=typ,data=t))
   if typ in ('SpriteRenderer','MeshRenderer'):print(' ',p,typ,'sprite',t.get('m_Sprite'),'materials',t.get('m_Materials'),'order',t.get('m_SortingOrder'),'enabled',t.get('m_Enabled'))
   if typ=='MonoBehaviour':print(' ',p,typ,'keys',list(t))
  nodes.append(dict(pathId=pid(tr['m_GameObject']),transformId=tid,parentTransformId=pid(tr['m_Father']) if tid!=root else 0,name=g['m_Name'],active=g['m_IsActive'],localPosition=tr['m_LocalPosition'],localRotation=tr['m_LocalRotation'],localScale=tr['m_LocalScale'],components=comps))
 all.append(dict(key=key,nodes=nodes))
(OUT/'NativeBlockers.json').write_text(json.dumps(dict(prefabs=all),indent=2))

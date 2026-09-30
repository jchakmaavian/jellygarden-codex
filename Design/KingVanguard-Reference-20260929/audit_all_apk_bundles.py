from inspect_king_sources import *
sys.path.insert(0,str(ROOT/'Design/ReferenceGameplayImport/Media/Tools/Python'))
import UnityPy

def main():
    bundles=[];known={'King.skel','AreaCompleted.skel','nightmare_popup.skel'}
    with zipfile.ZipFile(ROOT/'Design/Backup/apk/royal_thron_rfv387.apk') as z:
        for entry in z.infolist():
            if not entry.filename.startswith('assets/android/') or entry.filename.endswith('.json'):continue
            data=z.read(entry);rec=dict(entry=entry.filename,bytes=len(data),sha256=digest(data),unityfs=data.startswith(b'UnityFS'),named_king_objects=[],spine_skeletons=[])
            if not rec['unityfs']:bundles.append(rec);continue
            try:env=UnityPy.load(data)
            except Exception as ex:rec['error']=repr(ex);bundles.append(rec);continue
            rec['objects']=len(env.objects)
            for o in env.objects:
                if o.type.name not in ['TextAsset','GameObject','AnimationClip','AnimatorController','Mesh','Texture2D','Sprite']:continue
                try:
                    obj=o.read();name=obj.m_Name
                except Exception as ex:continue
                if re.search(r'(?i)king|robert|kral',name):rec['named_king_objects'].append(dict(path_id=o.path_id,type=o.type.name,name=name))
                if o.type.name!='TextAsset':continue
                payload=obj.m_Script.encode('utf8','surrogateescape') if isinstance(obj.m_Script,str) else obj.m_Script
                if b'3.8.' not in payload[:100]:continue
                s=dict(name=name,path_id=o.path_id,sha256=digest(payload),bytes=len(payload))
                try:
                    with contextlib.redirect_stdout(io.StringIO()):j=Reader(payload).read()
                    s.update(summary(j))
                except Exception as ex:s['error']=repr(ex)
                rec['spine_skeletons'].append(s)
                print('SPINE',entry.filename,name,s.get('bones'),s.get('slots'),[(a['name'],a['duration']) for a in s.get('animations',[])])
            bundles.append(rec);print('BUNDLE',entry.filename,rec['objects'],'king matches',len(rec['named_king_objects']))
    save('all-apk-android-bundles-audit.json',bundles)
    print('TOTAL',len(bundles))

if __name__=='__main__':main()

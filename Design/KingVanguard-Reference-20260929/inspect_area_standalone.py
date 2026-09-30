from pathlib import Path
import hashlib,json
OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[1]
j=json.loads((OUT/'AreaCompleted.skel.spine38.json').read_text(encoding='utf8'))
manifest=json.loads((OUT/'AnimationReview/contact-sheet-manifest.json').read_text())

def active_slots(animation,time):
    result=[]
    for i,s in enumerate(j['slots']):
        name=s['attachment'];color=s['color'];tracks=animation['slots'].get(str(i),{})
        for key in tracks.get('0',[]):
            if key['time']<=time:name=key['attachment']
        for key in tracks.get('1',tracks.get('2',[])):
            if key['time']<=time:color=key['color']
        if name and (color&255)>0:result.append(dict(index=i,slot=s['name'],attachment=name))
    return result

def rect(a):
    c=a['boundsCenter'];s=a['boundsSize']
    return dict(x=(c['x']-s['x']/2)*100,y=(c['y']-s['y']/2)*100,width=s['x']*100,height=s['y']*100)

source=(OUT/'Original-rfv387/AreaCompleted.skel').read_bytes()
runtime=(ROOT/'Assets/Characters/KingVenguard/V2/Spine/AreaCompleted_Vanguard.skel.bytes').read_bytes()
evidence=dict(source_sha256=hashlib.sha256(source).hexdigest(),runtime_sha256=hashlib.sha256(runtime).hexdigest(),binary_identical=source==runtime,stable_source_unit_bounds={a['animation']:rect(a) for a in manifest['animations'] if a['rig']=='AreaCompleted_Vanguard' and a['animation'] in ['king_drill_loop','king_drill_win_clap_loop','king_drill_win_clap_look_loop']},animation_slots={})
for name in ['king_area 1','king_area_2','king_drill_loop','king_drill_win_clap_loop']:
    a=j['animations'][name]
    evidence['animation_slots'][name]=dict(initial_visible_arm_slots=[s for s in active_slots(a,0) if 3<=s['index']<=29 or 76<=s['index']<=96],initial_unkeyed_arm_slots=[s for s in active_slots(a,0) if (18<=s['index']<=29) and str(s['index']) not in a['slots']])
evidence['body_translation_tracks']={name:a['bones'].get('1',{}).get('1',[]) for name,a in j['animations'].items()}
(OUT/'area-standalone-evidence.json').write_text(json.dumps(evidence,ensure_ascii=False,indent=2),encoding='utf8')
print(json.dumps({k:v for k,v in evidence.items() if k not in ['body_translation_tracks','animation_slots']},indent=2))
for name,a in evidence['animation_slots'].items():print(name,'unkeyed active',[(s['index'],s['slot']) for s in a['initial_unkeyed_arm_slots']])

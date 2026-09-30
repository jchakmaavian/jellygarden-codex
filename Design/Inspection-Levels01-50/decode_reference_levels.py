"""Evidence-preserving FlatBuffers inspection of extracted level payloads.

No game assets are written. Semantic labels are added only with supporting evidence.
"""
from pathlib import Path
import struct
import json
import hashlib
import collections
import csv
from read_reference_metadata import Metadata

ROOT = Path(__file__).resolve().parent

class Flat:
    def __init__(self, path):
        self.path = path
        self.b = path.read_bytes()
    def u16(self, p): return struct.unpack_from('<H', self.b, p)[0]
    def i32(self, p): return struct.unpack_from('<i', self.b, p)[0]
    def u32(self, p): return struct.unpack_from('<I', self.b, p)[0]
    def table(self, p):
        vt = p - self.i32(p)
        assert 0 <= vt < len(self.b)-4
        size, objsize = self.u16(vt), self.u16(vt+2)
        assert 4 <= size <= 512 and size % 2 == 0
        assert 4 <= objsize <= len(self.b) and vt+size <= len(self.b)
        fs = [self.u16(vt+4+i*2) for i in range((size-4)//2)]
        assert all(0 <= f < objsize for f in fs)
        return [p+f if f else None for f in fs]
    def field(self, p, idx):
        fs = self.table(p)
        return fs[idx] if idx < len(fs) else None
    def offset(self, p): return p+self.u32(p)
    def vector(self, p):
        q = self.offset(p)
        n = self.u32(q)
        assert 0 <= n <= 10000 and q+4+n*4 <= len(self.b)
        return [q+4+i*4 for i in range(n)]
    def text(self, p):
        q = self.offset(p)
        n = self.u32(q)
        assert n < len(self.b) and q+4+n < len(self.b) and self.b[q+4+n] == 0
        return self.b[q+4:q+4+n].decode('utf-8')
    def describe(self, p):
        out = {'at':p, 'i32':self.i32(p), 'uoffset_destination':self.offset(p)}
        for label, get in [('table',lambda:self.table(self.offset(p))),('string',lambda:self.text(p)),('vector_i32',lambda:[self.i32(v) for v in self.vector(p)])]:
            try:
                v=get()
                if label == 'table':
                    v=[None if a is None else {'at':a,'i32':self.i32(a),'uoffset_destination':self.offset(a)} for a in v]
                if label == 'vector_i32' and len(v)>20: v={'count':len(v),'first20':v[:20]}
                out[label]=v
            except (AssertionError,struct.error,UnicodeDecodeError): pass
        return out

class Decoder:
    def __init__(self):
        self.md=Metadata()
        self.schema={n:self.md.schema(n)['fields'] for n in ['FTiledLevel','FTiledGrid','FTiledCell','FTiledPredefined','FTiledSet','FTiledSetElement','FTiledSetMaxItemCount','FGoalData','FItemCount']}
        self.enums={n:{v['value']:v['name'] for v in self.md.enum(n)['values']} for n in ['TiledId','GoalType','ItemType','CellFillType','MatchType']}
    def table(self,fb,p,name,special=None):
        out={'source_table_offset':p}
        special=special or {}
        for f in self.schema[name]:
            slot=f['slot']; key=f['name'].removesuffix('Offset');at=fb.field(p,slot)
            if slot in special:
                out[key]=special[slot](at) if at is not None else []
            elif f['name'].endswith('Offset'):
                if at:
                    q=fb.offset(at)
                    out[key]={'unparsed_offset':q,'first_i32':fb.i32(q)}
                else:out[key]=None
            else:
                out[key]=(bool(fb.b[at]) if isinstance(f.get('default'),bool) else fb.i32(at)) if at else f.get('default',0)
        return out
    def decode(self,n):
        path=ROOT/'ReferenceLevelsRaw'/f'level{n:02}.bytes';fb=Flat(path);r=fb.u32(0)
        vi=lambda at:[fb.i32(a) for a in fb.vector(at)]
        vt=lambda at,ty,special=None:[self.table(fb,fb.offset(a),ty,special) for a in fb.vector(at)]
        grid=self.table(fb,fb.offset(fb.field(r,3)),'FTiledGrid',{0:vi,1:lambda at:vt(at,'FTiledCell')})
        assert len(grid['items'])==len(grid['cells'])==grid['width']*grid['height'],n
        for i,(code,cell) in enumerate(zip(grid['items'],grid['cells'])):
            assert code in self.enums['TiledId'],(n,code)
            cell.update(storage_index=i,storage_column=i%grid['width'],storage_row=i//grid['width'],item_code=code,item_name=self.enums['TiledId'][code])
        grid['item_rows_in_storage_order']=[grid['items'][a:a+grid['width']] for a in range(0,len(grid['items']),grid['width'])]
        grid['item_name_rows_in_storage_order']=[[self.enums['TiledId'][code] for code in row] for row in grid['item_rows_in_storage_order']]
        extra_special={}
        for f in self.schema['FTiledLevel'][8:]:
            if not f['name'].endswith('Offset'):continue
            slot=f['slot']
            if slot in [17,33,49]:
                def empty_table(at):
                    p=fb.offset(at)
                    assert fb.table(p)==[],(n,p,'Unexpected nonempty optional table')
                    return {'source_table_offset':p,'empty_table':True}
                extra_special[slot]=empty_table
            else:
                def empty_vector(at):
                    assert fb.vector(at)==[],(n,at,'Unexpected nonempty optional vector')
                    return []
                extra_special[slot]=empty_vector
        level=self.table(fb,r,'FTiledLevel',{
            **extra_special,
            0:fb.text,2:lambda at:vt(at,'FGoalData'),3:lambda at:grid,
            4:lambda at:vt(at,'FTiledPredefined',{2:vi}),
            5:lambda at:vt(at,'FTiledSet',{0:fb.text,1:lambda a:vt(a,'FTiledSetElement'),4:vi,7:vi,8:vi,9:lambda a:vt(a,'FTiledSetMaxItemCount')}),
            6:vi,7:lambda at:vt(at,'FItemCount')})
        level.update(level=n,payload_sha256=hashlib.sha256(fb.b).hexdigest(),payload_bytes=len(fb.b))
        for g in level['goals']:
            g['goal_name']=self.enums['GoalType'].get(g['goal'],'UNKNOWN')
            assert g['goal_name']!='UNKNOWN' and g['count']>=0,(n,g)
        for c in level['counts']:
            c['item_type_name']=self.enums['ItemType'].get(c['type'],'UNKNOWN')
            if c['item_type_name']=='UNKNOWN':c['interpretation_status']='Raw signed type preserved; mapping not yet verified.'
        for p in level['predefined']:
            p['item_names']=[self.enums['TiledId'].get(v,'UNKNOWN') for v in p['ids']]
            assert 'UNKNOWN' not in p['item_names'],(n,p)
        for s in level['sets']:
            for e in s['elements']:
                e['item_name']=self.enums['TiledId'].get(e['id'],'UNKNOWN')
                assert e['item_name']!='UNKNOWN',(n,e)
        level['color_names']=[self.enums['MatchType'][c] for c in level['colors']]
        level['item_histogram']={self.enums['TiledId'][c]:v for c,v in sorted(collections.Counter(grid['items']).items())}
        level['cell_layers_histogram']={k:dict(sorted(collections.Counter(str(c[k]) for c in grid['cells']).items())) for k in ['fillType','isPredefined','grass','honey','curtain','chain','roof','jelly','treasuremap','ceramic','mushroomSoil','blinds','waterSoil','forceField','cryptex','iceBorder']}
        level['grid']['item_rows_top_to_bottom']=list(reversed(grid['item_rows_in_storage_order']))
        return level
    def all(self):
        levels=[self.decode(n) for n in range(1,51)]
        inventory=json.loads((ROOT/'reference-levels01-50-inventory.json').read_text())
        for l,src in zip(levels,inventory):assert l['payload_sha256']==src['sha256']
        orientation_checks=[];goal_checks=[]
        for l in levels:
            g=l['grid'];w=g['width'];a=g['items'];hist=collections.Counter(a)
            for tl,tr,bl,br,name in [(41,42,51,52,'Cupboard'),(43,44,53,54,'Potion')]:
                for i,c in enumerate(a):
                    if c!=tl:continue
                    ok=i%w+1<w and i>=w and a[i+1]==tr and a[i-w]==bl and a[i-w+1]==br
                    assert ok,(l['level'],name,i,'Corner placement invalid')
                    orientation_checks.append({'level':l['level'],'group':name,'top_left_storage_index':i,'passed':ok})
            expected={'Box':sum(hist[x] for x in range(21,25)),'Grass':sum(c['grass']!=0 for c in g['cells']),'Cupboard':hist[41]*10,'Egg':hist[26],'Potion':hist[43]*4}
            for goal in l['goals']:
                if goal['isFromSettings'] or goal['goal_name'] not in expected:continue
                ok=goal['count']==expected[goal['goal_name']]
                assert ok,(l['level'],goal,'Static objective does not match layout')
                goal_checks.append({'level':l['level'],'goal':goal['goal_name'],'count':goal['count'],'passed':ok})
        doc={'provenance':'Exact TextAsset payloads from local reference APK; IL2CPP metadata CreateFTiled* parameter order and enum constants provide semantic labels. No game files changed.',
             'layout_orientation':'Row-major, bottom-up. Verified in all 109 authored 2x2 Cupboard/Potion groups: TL at i, TR at i+1, BL at i-width, BR at i-width+1. grid.item_rows_top_to_bottom is ready for visual comparison.',
             'level_count':len(levels),'levels':levels}
        (ROOT/'reference-levels01-50-decoded.json').write_text(json.dumps(doc,indent=2),encoding='utf8')
        validation={'payload_hashes_checked':50,'grid_item_cell_length_checks_passed':50,'orientation_group_checks':orientation_checks,'static_objective_count_checks':goal_checks,
                    'unresolved_semantics':['Coin goal count is authored zero in levels20 and40. Do not treat this as an automatic win; bonus-level runtime behavior remains to be verified.','Predefined above-board sequences are preserved; exact consumption order and spawn fallback are runtime semantics, not established by static data alone.','FItemCount type -2 occurs only with Grass and matches its occupied-cell count; raw code is preserved rather than silently converted.','Set element createRatio defaults to0; set-level ratios are authored. Runtime weighting policy remains to be verified.']}
        (ROOT/'reference-decoding-validation.json').write_text(json.dumps(validation,indent=2),encoding='utf8')
        dest=ROOT/'ReferenceLayouts';dest.mkdir(exist_ok=True)
        summaries=[]
        for l in levels:
            g=l['grid'];goals=', '.join(f"{x['goal_name']}:{x['count']}" for x in l['goals'])
            blockers={k:v for k,v in l['item_histogram'].items() if k not in ['None','Blue','Green','Red','Yellow','Pink','Orange','Fill','Fill1','Fill2','Fill3','Defined','Empty','Set1','Set2','Set3']}
            layers={k:v for k,v in l['cell_layers_histogram'].items() if k not in ['fillType','isPredefined'] and any(x!='0' for x in v)}
            summary={'level':l['level'],'authored_name':l['name'],'width':g['width'],'height':g['height'],'moves':l['move'],'goals':goals,'colors':','.join(l['color_names']),'items':json.dumps(blockers),'layers':json.dumps(layers),'predefined_entries':len(l['predefined'])}
            summaries.append(summary)
            lines=[f"# Reference level {l['level']:02d}",f"Authored name: {l['name']}",f"Grid: {g['width']} x {g['height']}; moves: {l['move']}; goals: {goals}",f"Payload SHA-256: {l['payload_sha256']}",'','Item rows shown top-to-bottom (serialized rows reversed; orientation verified by 2x2 corner codes):','```']
            lines+=[' '.join(f'{v:3d}' for v in row) for row in g['item_rows_top_to_bottom']]
            lines+=['```','','Codes present: '+', '.join(f'{k}={self.enums["TiledId"][k]}' for k in sorted(set(g['items']))),'','Cell layers: '+json.dumps(layers),'','Predefined coordinates and item sequences:','```']
            lines += [f"({p['x']},{p['y']}): {p['ids']} {p['item_names']}" for p in l['predefined']]
            lines+=['```']
            (dest/f'level{l["level"]:02}.md').write_text('\n'.join(lines)+'\n',encoding='utf8')
        with (ROOT/'reference-levels01-50-summary.csv').open('w',newline='',encoding='utf8') as f:
            wr=csv.DictWriter(f,fieldnames=list(summaries[0]));wr.writeheader();wr.writerows(summaries)
        print(f'Verified 50 payload SHA-256s, dimensions/cell lengths, enum codes, scalar defaults, {len(orientation_checks)} orientation groups and {len(goal_checks)} static objective counts.')
        for s in summaries:print(s)
        extra={}
        for l in levels:
            for f in self.schema['FTiledLevel'][8:]:
                name=f['name'].removesuffix('Offset');v=l[name]
                if isinstance(v,dict) and v.get('first_i32',0)!=0:extra.setdefault(name,[]).append((l['level'],v))
        print('NONEMPTY EXTRA FIELDS',extra)

if __name__ == '__main__':
    import sys
    if '--all' in sys.argv:
        Decoder().all()
        sys.exit(0)
    n=int(sys.argv[1]) if len(sys.argv)>1 else 1
    fb=Flat(ROOT/'ReferenceLevelsRaw'/f'level{n:02}.bytes')
    root=fb.u32(0)
    print(json.dumps({'level':n,'root':root,'fields':{str(i):fb.describe(p) if p is not None else None for i,p in enumerate(fb.table(root))}},indent=2))

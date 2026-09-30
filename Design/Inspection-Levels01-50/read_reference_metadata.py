"""Minimal IL2CPP v31 metadata reader; local package evidence only."""
from pathlib import Path
import struct
import json

ROOT=Path(__file__).resolve().parent

class Metadata:
    def __init__(self):
        self.b=(ROOT/'ReferencePackage/global-metadata.dat').read_bytes()
        assert struct.unpack_from('<II',self.b)==(0xfab11baf,31)
        self.strings,self.string_size=self.pair(24)
        self.types_offset,self.types_size=self.pair(160)
        self.methods_offset,_=self.pair(48)
        self.parameters_offset,_=self.pair(88)
        self.fields_offset,_=self.pair(96)
        self.values_offset,_=self.pair(72)
        self.types={}
        for i in range(self.types_size//88):
            a=struct.unpack_from('<16I8H2I',self.b,self.types_offset+i*88)
            name=self.string(a[0]);ns=self.string(a[1])
            self.types[(ns,name)]={'index':i,'name':name,'namespace':ns,'field_start':a[8],'field_count':a[18],'method_start':a[9],'method_count':a[16]}
        self.field_defaults=self.defaults(64)
        self.parameter_defaults=self.defaults(56)
    def pair(self,at):return struct.unpack_from('<II',self.b,at)
    def string(self,index):
        assert index<self.string_size
        p=self.strings+index
        return self.b[p:self.b.index(0,p)].decode('utf8')
    def defaults(self,header):
        p,n=self.pair(header)
        return {k:(t,d) for k,t,d in (struct.unpack_from('<III',self.b,i) for i in range(p,p+n,12))}
    def unsigned_compressed(self,relative):
        p=self.values_offset+relative;b=self.b[p]
        if b<0x80:return b
        if b<0xc0:return ((b&0x3f)<<8)|self.b[p+1]
        if b<0xe0:return ((b&0x1f)<<24)|(self.b[p+1]<<16)|(self.b[p+2]<<8)|self.b[p+3]
        if b==0xf0:return struct.unpack_from('<I',self.b,p+1)[0]
        if b==0xfe:return 0xfffffffe
        if b==0xff:return 0xffffffff
        raise ValueError((p,b))
    def integer_default(self,relative):
        v=self.unsigned_compressed(relative)
        return -(v>>1)-1 if v&1 else v>>1
    def find(self,name):
        matches=[v for (ns,n),v in self.types.items() if n==name and ns.startswith('Royal.')]
        assert len(matches)==1,(name,len(matches))
        return matches[0]
    def enum(self,name):
        t=self.find(name);out=[]
        for i in range(t['field_start'],t['field_start']+t['field_count']):
            s,_,token=struct.unpack_from('<III',self.b,self.fields_offset+i*12)
            if i not in self.field_defaults:continue
            typ,relative=self.field_defaults[i]
            out.append({'name':self.string(s),'value':self.integer_default(relative),'field_index':i,'default_data_index':relative})
        return {'type':t,'values':out}
    def schema(self,name):
        t=self.find(name)
        for i in range(t['method_start'],t['method_start']+t['method_count']):
            a=struct.unpack_from('<7I4H',self.b,self.methods_offset+i*36)
            if self.string(a[0])!='Create'+name:continue
            out=[]
            for pi in range(a[4],a[4]+a[-1]):
                ni,token,ty=struct.unpack_from('<III',self.b,self.parameters_offset+pi*12)
                pname=self.string(ni)
                if pname=='builder':continue
                item={'slot':len(out),'name':pname,'parameter_index':pi,'type_index':ty}
                if pi in self.parameter_defaults:
                    dt,di=self.parameter_defaults[pi]
                    item['default_type_index']=dt
                    item['default_data_index']=di
                    item['default_raw_hex']=self.b[self.values_offset+di:self.values_offset+di+4].hex()
                    if dt==18551:item['default']=self.integer_default(di)
                    elif dt==12850:item['default']=bool(self.b[self.values_offset+di])
                out.append(item)
            return {'type':t,'create_method_index':i,'fields':out}
        raise ValueError(name)

if __name__=='__main__':
    md=Metadata()
    enums={n:md.enum(n) for n in ['TiledId','GoalType','ItemType','CellFillType','MatchType','StaticItemType']}
    schemas={n:md.schema(n) for n in ['FTiledLevel','FTiledGrid','FTiledCell','FTiledPredefined','FTiledSet','FTiledSetElement','FGoalData','FItemCount','FIntIntPair']}
    (ROOT/'reference-schema-evidence.json').write_text(json.dumps({'metadata_version':31,'enums':enums,'schemas':schemas},indent=2),encoding='utf8')
    for n, e in enums.items():
        print(n,[(v['name'],v['value']) for v in e['values']][:115])
    for n, s in schemas.items():
        print(n,[(f['slot'],f['name'],f.get('default')) for f in s['fields']])

from inspect_king_sources import *

def bundle_objects(path):
    d=path.read_bytes();v=struct.unpack_from('>I',d,8)[0];assert v>=19
    start=48 if v>=22 else 20;p=d.index(b'\0',start)+1;uv=d[start:p-1].decode();platform=struct.unpack_from('<i',d,p)[0];p+=4;tree=d[p];p+=1
    n=struct.unpack_from('<I',d,p)[0];p+=4;types=[]
    for i in range(n):
        cls,strip,script=struct.unpack_from('<iBh',d,p);p+=7
        if cls==114:p+=16
        p+=16;types.append(cls)
        if tree:
            nodes,strings=struct.unpack_from('<II',d,p);p+=8+nodes*32+strings
            if v>=21:
                deps=struct.unpack_from('<I',d,p)[0];p+=4+deps*4
    count=struct.unpack_from('<I',d,p)[0];p+=4
    dataoff=struct.unpack_from('>q',d,32)[0] if v>=22 else struct.unpack_from('>I',d,12)[0]
    out=[]
    for i in range(count):
        p=(p+3)&~3
        if v>=22:pid,off,size,typ=struct.unpack_from('<qQIi',d,p);p+=24
        else:pid,off,size,typ=struct.unpack_from('<qIIi',d,p);p+=20
        cls=types[typ];off+=dataoff;name=''
        try:
            noff=off
            if cls==1:comps=struct.unpack_from('<I',d,off)[0];assert comps<1000;noff=off+4+comps*12+4
            if cls in [1,21,28,43,48,49,74,83,89,90,91,95,115,128,137,142,213]:name=string(d,noff)[0]
        except Exception:pass
        out.append(dict(path_id=pid,class_id=cls,byte_offset=off,size=size,name=name))
    return d,out,dict(unity=uv,platform=platform,objects=count,types=types,serialized_version=v,type_tree=bool(tree))

def main():
    assets=[];skeletons=[];bundles=[];dest=OUT/'EmbeddedKingBundles/Recovered';dest.mkdir(exist_ok=True)
    for file in (OUT/'EmbeddedKingBundles').rglob('CAB-*'):
        if '.' in file.name:continue
        d,rows,meta=bundle_objects(file);bundles.append(dict(file=str(file.relative_to(OUT)),**meta));print(file.parent.name,meta)
        for r in rows:
            r=dict(r,file=str(file.relative_to(OUT)));assets.append(r)
            if r['class_id']!=49:continue
            raw=d[r['byte_offset']:r['byte_offset']+r['size']];name,p=string(raw,0);length=struct.unpack_from('<I',raw,p)[0];payload=raw[p+4:p+4+length]
            if name.endswith(('.skel','.atlas')):
                target=dest/file.parent.name;target.mkdir(exist_ok=True);(target/name).write_bytes(payload)
            if b'3.8.' not in payload[:100]:continue
            rec=dict(r,payload_bytes=len(payload),sha256=digest(payload))
            try:
                with contextlib.redirect_stdout(io.StringIO()):j=Reader(payload).read()
                rec['skeleton']=summary(j);(target/(name+'.json')).write_text(json.dumps(j,ensure_ascii=False),encoding='utf8')
            except Exception as ex:rec['parse_error']=repr(ex)
            skeletons.append(rec);print('SPINE',name,rec.get('skeleton',{}).get('bones'),rec.get('skeleton',{}).get('slots'),rec.get('parse_error',''));print('ANIMS',[(n,a['duration']) for n,a in j['animations'].items()])
        print('ART',[(r['class_id'],r['name']) for r in rows if r['class_id'] in [28,49,74,91]])
    save('embedded-king-bundles.json',bundles);save('embedded-king-bundle-assets.json',assets);save('embedded-king-bundle-skeletons.json',skeletons)

if __name__=='__main__':main()

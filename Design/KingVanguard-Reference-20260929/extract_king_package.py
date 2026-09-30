from inspect_king_sources import *
import ctypes as C
import zlib

def texture(data,row,source):
    b=data[row['byte_offset']:row['byte_offset']+row['size']]
    name,p=string(b,0);w,h=struct.unpack_from('<II',b,p+8);fmt=struct.unpack_from('<I',b,p+24)[0]
    off,size=struct.unpack_from('<QI',b,p+88);sname,_=string(b,p+100)
    print('TEXTURE',name,w,h,fmt,off,size,sname)
    with (source.parent/sname).open('rb') as f:f.seek(off);payload=f.read(size)
    if 48<=fmt<=53:
        lib=C.CDLL(r'C:\Program Files\Unity\Hub\Editor\6000.5.10f1\Editor\Data\Tools\astcenc-sse2.dll')
        lib.astcenc_config_init.argtypes=[C.c_int,C.c_uint,C.c_uint,C.c_uint,C.c_float,C.c_uint,C.c_void_p]
        lib.astcenc_context_alloc.argtypes=[C.c_void_p,C.c_uint,C.POINTER(C.c_void_p)]
        lib.astcenc_decompress_image.argtypes=[C.c_void_p,C.c_void_p,C.c_size_t,C.c_void_p,C.c_void_p,C.c_uint]
        lib.astcenc_context_free.argtypes=[C.c_void_p]
        class AI(C.Structure):_fields_=[('x',C.c_uint),('y',C.c_uint),('z',C.c_uint),('type',C.c_int),('data',C.POINTER(C.c_void_p))]
        block=[4,5,6,8,10,12][fmt-48];cfg=C.create_string_buffer(4096);assert lib.astcenc_config_init(1,block,block,1,0.,16,cfg)==0
        context=C.c_void_p();assert lib.astcenc_context_alloc(cfg,1,C.byref(context))==0
        dst=C.create_string_buffer(w*h*4);slices=(C.c_void_p*1)(C.cast(dst,C.c_void_p));img=AI(w,h,1,0,slices);swizzle=(C.c_int*4)(0,1,2,3);src=C.create_string_buffer(payload)
        assert lib.astcenc_decompress_image(context,src,len(payload),C.byref(img),swizzle,0)==0
        lib.astcenc_context_free(context);payload=dst.raw;fmt=4
    assert fmt in (1,4),(name,fmt)
    channels=1 if fmt==1 else 4;stride=w*channels
    scanlines=b''.join(b'\0'+payload[y*stride:(y+1)*stride] for y in reversed(range(h)))
    def chunk(kind,content):return struct.pack('>I',len(content))+kind+content+struct.pack('>I',zlib.crc32(kind+content))
    png=b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',w,h,8,0 if fmt==1 else 6,0,0,0))+chunk(b'IDAT',zlib.compress(scanlines))+chunk(b'IEND',b'')
    return png,dict(name=name,width=w,height=h,format=fmt,stream=sname,stream_offset=off,stream_bytes=size)

def main():
    dest=OUT/'Original-rfv387';dest.mkdir(exist_ok=True)
    allrows=json.loads((OUT/'rfv387-named-assets.json').read_text(encoding='utf8'))
    extracted=[]
    for filename,names in [('sharedassets2.assets',{'King','King.skel','King.atlas'}),('resources.assets',{'AreaCompleted','AreaCompleted.skel','AreaCompleted.atlas'})]:
        src=ROOT/'Design/Inspection-rfv387/datapack.unity3d_data'/filename
        d=src.read_bytes()
        for r in allrows:
            if Path(r['file']).name!=filename or r['name'] not in names:continue
            b=d[r['byte_offset']:r['byte_offset']+r['size']];rec=dict(r)
            if r['class_id']==49:
                name,p=string(b,0);size=struct.unpack_from('<I',b,p)[0];payload=b[p+4:p+4+size]
                (dest/name).write_bytes(payload);rec.update(sha256=digest(payload),output=name)
                if name.endswith('.skel'):
                    with contextlib.redirect_stdout(io.StringIO()):j=Reader(payload).read()
                    save(name+'.spine38.json',j)
            elif r['class_id']==28:
                png,meta=texture(d,r,src);(dest/(r['name']+'.png')).write_bytes(png);rec.update(meta,output=r['name']+'.png')
            else:continue
            extracted.append(rec)
    save('extracted-original-assets.json',extracted)
    # Exact old/new change audit after both complete skeletons have parsed.
    a=json.loads((OLD/'Characters/King/Recovered/King.spine38.json').read_text(encoding='utf8'));b=json.loads((OUT/'King.skel.spine38.json').read_text(encoding='utf8'))
    skin_changes=[]
    for si,skin in enumerate(b['skins']):
        for slot,ats in skin['attachments'].items():
            for name,attachment in ats.items():
                old=a['skins'][si]['attachments'].get(slot,{}).get(name)
                if old!=attachment:skin_changes.append(dict(slot=b['slots'][int(slot)]['name'],attachment=name,old=old,new=attachment))
    diff=dict(unchanged_sections=[k for k in a if a[k]==b.get(k)],changed_sections=[k for k in a if a[k]!=b.get(k)],animations_identical=a['animations']==b['animations'],changed_attachments=skin_changes)
    for name in ['King.atlas','King.png']:
        old=OLD/'Characters/King/Original'/name;new=dest/name
        diff[name]=dict(old_sha256=digest(old.read_bytes()),new_sha256=digest(new.read_bytes()),bytes_identical=old.read_bytes()==new.read_bytes())
    save('old-new-king-comparison.json',diff)
    # Preserve King and King Spine serialized components and expose their stored strings.
    src=ROOT/'Design/Inspection-rfv387/datapack.unity3d_data/level2';d,rs,meta=objects(src);by={r['path_id']:r for r in rs};scene=[]
    for go in [r for r in rs if r['class_id']==1 and r['name'] in ['King','King Spine']]:
        raw=d[go['byte_offset']:go['byte_offset']+go['size']];n=struct.unpack_from('<I',raw)[0];parts=[]
        for i in range(n):
            fid,pid=struct.unpack_from('<iq',raw,4+i*12);r=by[pid];b=d[r['byte_offset']:r['byte_offset']+r['size']]
            filename=f"level2-{pid}-{r['class_id']}.unitybin";(dest/filename).write_bytes(b)
            p=dict(r,output=filename,readable_strings=[s.decode('utf8','replace') for s in re.findall(rb'[\x20-\x7e]{4,}',b)])
            if r['class_id']==114:p['script_reference']=struct.unpack_from('<iq',b,16)
            parts.append(p)
        scene.append(dict(go=go,components=parts))
    save('rfv387-king-scene-components.json',scene)
    # Decode the two integer metadata constants, without claiming method bodies.
    with zipfile.ZipFile(ROOT/'Design/Backup/apk/royal_thron_rfv387.apk') as z:meta=z.read(next(i.filename for i in z.infolist() if i.filename.endswith('/global-metadata.dat')))
    spec=importlib.util.spec_from_file_location('read_reference_metadata',ROOT/'Design/Inspection-Levels01-50/read_reference_metadata.py');module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
    md=module.Metadata.__new__(module.Metadata);md.b=meta;md.strings,md.string_size=md.pair(24);md.types_offset,md.types_size=md.pair(160);md.fields_offset,_=md.pair(96);md.values_offset,_=md.pair(72);md.field_defaults=md.defaults(64)
    constants=[]
    for i in range(md.types_size//88):
        a=struct.unpack_from('<16I8H2I',meta,md.types_offset+i*88);name=md.string(a[0]);ns=md.string(a[1])
        if name not in ['KingUi','KingAnimationStateManager']:continue
        for fi in range(a[8],a[8]+a[18]):
            ni,ty,tok=struct.unpack_from('<III',meta,md.fields_offset+fi*12);fn=md.string(ni)
            if fi in md.field_defaults:
                dt,di=md.field_defaults[fi];constants.append(dict(type=ns+'.'+name,field=fn,default_type_index=dt,raw_hex=meta[md.values_offset+di:md.values_offset+di+4].hex(),compressed_integer=md.integer_default(di)))
    save('rfv387-king-constants.json',constants)
    print('CONSTANTS',constants)
    print('SCENE',json.dumps(scene,ensure_ascii=False))
    print('SKIN CHANGES',[(x['slot'],x['attachment']) for x in skin_changes])

if __name__=='__main__':main()

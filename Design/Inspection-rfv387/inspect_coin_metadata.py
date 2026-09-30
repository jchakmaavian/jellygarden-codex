from pathlib import Path
import re
import json
import zipfile
import struct
import importlib.util

root = Path(__file__).resolve().parents[2]
apk = root / 'Design/Backup/apk/royal_thron_rfv387.apk'
out = Path(__file__).resolve().parent / 'CoinLogic'
out.mkdir(exist_ok=True)
with zipfile.ZipFile(apk) as z:
    matches = [i for i in z.infolist() if 'metadata' in i.filename.lower()]
    print([(i.filename, i.file_size) for i in matches])
    for item in matches:
        if item.filename.endswith('global-metadata.dat'):
            data = z.read(item)
            print('header', struct.unpack_from('<16I', data))
            string_offset, string_count = struct.unpack_from('<II', data, 24)
            strings = [x.decode('utf-8', 'replace') for x in data[string_offset:string_offset+string_count].split(b'\0')]
            ids = [i for i, s in enumerate(strings) if 'coin' in s.lower()]
            selected = [{'index': i, 'value': strings[i], 'context': strings[max(0,i-4):i+5]} for i in ids]
            (out / 'metadata-coin-strings.json').write_text(json.dumps(selected, indent=2))
            spec = importlib.util.spec_from_file_location('read_reference_metadata', root / 'Design/Inspection-Levels01-50/read_reference_metadata.py')
            module = importlib.util.module_from_spec(spec)
            spec.loader.exec_module(module)
            md = module.Metadata.__new__(module.Metadata)
            md.b = data
            md.strings,md.string_size=md.pair(24)
            md.types_offset,md.types_size=md.pair(160)
            md.methods_offset,_=md.pair(48)
            md.parameters_offset,_=md.pair(88)
            md.fields_offset,_=md.pair(96)
            md.values_offset,_=md.pair(72)
            md.field_defaults=md.defaults(64)
            md.parameter_defaults=md.defaults(56)
            wanted = ['CoinCollectAnimation', 'CoinBezierCollect', 'CoinBezierLandscapeCollect', 'CoinCollectHelper', 'CoinCollectData', 'CoinItemView', 'CoinItemAssets', 'RemainingMovesCoinCollectAnimation', 'RemainingMovesManager', 'RemainingMovesSingleCoinPilot', 'RemainingMovesCoinTracer', 'CoinInfoView', 'PlayCoinCollectAnimationAction']
            parsed = []
            for i in range(md.types_size//88):
                a=struct.unpack_from('<16I8H2I',data,md.types_offset+i*88)
                name=md.string(a[0]);ns=md.string(a[1])
                if ns == 'System' and name in ('Single', 'Int32', 'Boolean'): print('primitive',name,a[2])
                if name not in wanted or not ns.startswith('Royal.'): continue
                t = {'name': name, 'namespace': ns, 'index': i, 'fields': [], 'methods': []}
                for fi in range(a[8],a[8]+a[18]):
                    ni,ty,token=struct.unpack_from('<III',data,md.fields_offset+fi*12)
                    f = {'name':md.string(ni), 'type_index':ty}
                    if fi in md.field_defaults:
                        dt,di=md.field_defaults[fi]
                        f.update(default_type_index=dt, default_data_index=di, default_raw_hex=data[md.values_offset+di:md.values_offset+di+4].hex())
                        if dt==23131: f['default_int']=md.integer_default(di)
                        elif dt==16204: f['default_bool']=bool(data[md.values_offset+di])
                        elif dt==31427:f['default_float']=struct.unpack_from('<f', data, md.values_offset+di)[0]
                    t['fields'].append(f)
                for mi in range(a[9],a[9]+a[16]):
                    ma=struct.unpack_from('<7I4H',data,md.methods_offset+mi*36)
                    method={'name':md.string(ma[0]),'parameters':[]}
                    for pi in range(ma[4],ma[4]+ma[-1]):
                        ni,token,ty=struct.unpack_from('<III',data,md.parameters_offset+pi*12)
                        p={'name':md.string(ni),'type_index':ty}
                        if pi in md.parameter_defaults:
                            dt,di=md.parameter_defaults[pi]
                            p.update(default_type_index=dt, default_data_index=di, default_raw_hex=data[md.values_offset+di:md.values_offset+di+4].hex())
                            if dt==23131:p['default_int']=md.integer_default(di)
                            elif dt==16204:p['default_bool']=bool(data[md.values_offset+di])
                            elif dt==31427:p['default_float']=struct.unpack_from('<f', data, md.values_offset+di)[0]
                        method['parameters'].append(p)
                    t['methods'].append(method)
                parsed.append(t)
            (out/'metadata-coin-types.json').write_text(json.dumps(parsed,indent=2))
            for t in parsed:
                print(t['namespace']+'.'+t['name'])
                print('DEFAULTS:', [(f['name'],f.get('default_int',f.get('default_bool',f.get('default_float')))) for f in t['fields'] if 'default_type_index' in f])
            for name in ['CoinCollectAnimation','CoinBezierCollect','RemainingMovesSingleCoinPilot']:
                at=strings.index(name)
                print(name, strings[at:at+90])

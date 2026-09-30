"""Exact APK metadata evidence only; method names do not establish method bodies."""
from pathlib import Path
import importlib.util, json, struct, zipfile

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('metadata_reader', ROOT / 'Design/Inspection-Levels01-50/read_reference_metadata.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
with zipfile.ZipFile(ROOT / 'Design/Backup/apk/royal_thron_rfv387.apk') as archive:
    data = archive.read('assets/bin/Data/Managed/Metadata/global-metadata.dat')
md = module.Metadata.__new__(module.Metadata)
md.b = data
md.strings, md.string_size = md.pair(24)
md.types_offset, md.types_size = md.pair(160)
md.methods_offset, _ = md.pair(48)
md.fields_offset, _ = md.pair(96)
md.values_offset, _ = md.pair(72)
md.field_defaults = md.defaults(64)
types, primitives = [], {}
for index in range(md.types_size // 88):
    row = struct.unpack_from('<16I8H2I', data, md.types_offset + index * 88)
    name, namespace = md.string(row[0]), md.string(row[1])
    if namespace == 'System' and name in ('Single', 'Int32', 'Boolean'):
        primitives[row[2]] = name
    if namespace.startswith('Royal.Scenes.Game.Mechanics.') and ('LightballItem' in namespace or '.Fall' in namespace):
        types.append((index, row, name, namespace))
result = []
for index, row, name, namespace in types:
    entry = dict(name=name, namespace=namespace, index=index, fields=[], methods=[])
    for field_index in range(row[8], row[8] + row[18]):
        ni, type_index, _ = struct.unpack_from('<III', data, md.fields_offset + field_index * 12)
        field = dict(name=md.string(ni), typeIndex=type_index)
        if field_index in md.field_defaults:
            ti, di = md.field_defaults[field_index]
            raw = data[md.values_offset + di:md.values_offset + di + 4]
            field['literal'] = dict(primitive=primitives.get(ti), raw=raw.hex())
            if primitives.get(ti) == 'Single': field['literal']['value'] = struct.unpack('<f', raw)[0]
            elif primitives.get(ti) == 'Int32': field['literal']['value'] = md.integer_default(di)
            elif primitives.get(ti) == 'Boolean': field['literal']['value'] = bool(raw[0])
        entry['fields'].append(field)
    for method_index in range(row[9], row[9] + row[16]):
        method = struct.unpack_from('<7I4H', data, md.methods_offset + method_index * 36)
        entry['methods'].append(md.string(method[0]))
    result.append(entry)
(OUT / 'colorball-fall-metadata.json').write_text(json.dumps(result, indent=2), encoding='utf8')
print('Recovered', len(result), 'scoped types; constants do not establish their runtime units or call order.')

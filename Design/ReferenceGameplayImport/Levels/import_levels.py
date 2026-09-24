"""Reuse the already decoded APK data, verify the runtime projection, and import it.

No APK/device access, normal-save changes, or gameplay rule inference.
"""
from pathlib import Path
import hashlib
import json
import struct

ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / 'Design/Inspection-Levels01-50'
OUTPUT = ROOT / 'Assets/ReferenceGameplay/Levels'
RUNTIME = OUTPUT / 'Resources/ReferenceGameplay/levels01-50.json'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def project(level):
    grid = level['grid']
    width, height = grid['width'], grid['height']
    cells = []
    for row in reversed(range(height)):
        for col in range(width):
            i = row * width + col
            cell = grid['cells'][i]
            cells.append(dict(code=grid['items'][i], grass=cell['grass'],
                              fill=cell['fillType'], predefined=cell['isPredefined']))
    return dict(number=level['level'], name=level['name'], sourceSha256=level['payload_sha256'],
                width=width, height=height, moves=level['move'], colors=level['colors'], cells=cells,
                goals=[dict(name=g['goal_name'], count=g['count'], fromSettings=g['isFromSettings'])
                       for g in level['goals']],
                predefined=[dict(x=p['x'], y=p['y'], ids=p['ids']) for p in level['predefined']],
                sets=[dict(name=s['name'], ratio=s['createRatio'], canFall=s['canFall'],
                           columns=s['targetColumns'], fills=s['targetFills'],
                           elements=[dict(code=e['id'], ratio=e['createRatio']) for e in s['elements']])
                      for s in level['sets']])


def run():
    decoded_path = SOURCE / 'reference-levels01-50-decoded.json'
    schema_path = SOURCE / 'reference-schema-evidence.json'
    previous_path = ROOT / 'Assets/JellyGarden/Resources/RoyalCampaign/levels01-50.json'
    decoded = json.loads(decoded_path.read_text(encoding='utf-8'))['levels']
    previous = json.loads(previous_path.read_text(encoding='utf-8'))['levels']
    inventory = json.loads((SOURCE / 'reference-levels01-50-inventory.json').read_text(encoding='utf-8'))
    native_index = json.loads((ROOT / 'Design/Backup/Reference-UI-Characters/20260923-184326/Shared/serialized-object-index.json').read_text(encoding='utf-8'))
    natives = {row['byte_offset']: row for row in native_index
               if row['file'] == 'resources.assets' and row['class_id'] == 49}
    native_path = SOURCE / 'ReferencePackage/datapack.unity3d_data/resources.assets'
    manifest = []
    assert [level['level'] for level in decoded] == list(range(1, 51))
    with native_path.open('rb') as native:
        for level, old, item in zip(decoded, previous, inventory):
            n = level['level']
            normalized = project(level)
            assert normalized == old, f'Existing runtime projection differs from source: level {n}'
            raw = SOURCE / f'ReferenceLevelsRaw/level{n:02}.bytes'
            assert sha(raw) == item['sha256'] == normalized['sourceSha256']
            row = natives[item['byte_offset']]
            assert row['name'] == str(n)
            native.seek(row['byte_offset'])
            obj = native.read(row['size'])
            name_bytes = struct.unpack_from('<I', obj)[0]
            payload_at = (4 + name_bytes + 3) & ~3
            payload_len = struct.unpack_from('<I', obj, payload_at)[0]
            native_payload = obj[payload_at + 4:payload_at + 4 + payload_len]
            assert native_payload == raw.read_bytes(), f'Original TextAsset mismatch: level {n}'
            ignored_layers = ['honey','curtain','chain','roof','jelly','treasuremap','ceramic',
                              'mushroomSoil','blinds','waterSoil','forceField','cryptex','iceBorder']
            assert all(c[k] == 0 for c in level['grid']['cells'] for k in ignored_layers)
            assert all(s['birdMax'] == s['frogMax'] == -1 and not s['targetFillRatios']
                       and not s['maxItemCounts'] for s in level['sets'])
            manifest.append(dict(number=n, name=level['name'], sourceSha256=sha(raw),
                                 sourcePathId=row['path_id'], sourceByteOffset=row['byte_offset'],
                                 payloadBytes=payload_len, width=normalized['width'], height=normalized['height'],
                                 moves=normalized['moves'], goals=normalized['goals'],
                                 rawPayload=str(raw.relative_to(ROOT)).replace('\\','/')))
    RUNTIME.parent.mkdir(parents=True, exist_ok=True)
    RUNTIME.write_bytes(previous_path.read_bytes())
    evidence = dict(schemaVersion=1,
        apkSha256='4493ea05602f18aa3b26ab989acaee33f652aa2a85bd9f3dc3f92360441935a1',
        runtimeResource='ReferenceGameplay/levels01-50', runtimeSha256=sha(RUNTIME),
        decodedSource=str(decoded_path.relative_to(ROOT)).replace('\\','/'), decodedSourceSha256=sha(decoded_path),
        schemaEvidence=str(schema_path.relative_to(ROOT)).replace('\\','/'), schemaEvidenceSha256=sha(schema_path),
        gridConversion='Source row-major bottom-up converted to Royal Throne row-major top-down; cells, goals, moves, colors and authored feeds/sets unchanged.',
        rulesStatus='Imported layout/configuration; existing Royal Throne execution. Exact reference runtime rules remain incomplete: see RULE-PROVENANCE.md.',
        levels=manifest)
    (OUTPUT / 'Provenance.json').write_text(json.dumps(evidence, indent=2)+'\n', encoding='utf-8')
    report = dict(passed=True, levelCount=50, cellCount=sum(len(l['cells']) for l in previous),
                  originalTextAssetsMatched=50, exactProjectionMatched=50, sourcePayloadHashesMatched=50,
                  runtimeSha256=sha(RUNTIME), apkModified=False, runtimeRuleEquivalenceVerified=False)
    (Path(__file__).parent / 'source-parity.json').write_text(json.dumps(report, indent=2)+'\n', encoding='utf-8')
    print(json.dumps(report))


if __name__ == '__main__':
    run()

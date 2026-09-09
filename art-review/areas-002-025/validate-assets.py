#!/usr/bin/env python3
"""Check saved image files, stage order and generation references without editing art."""
import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image, ImageChops, ImageStat

ROOT = Path(__file__).resolve().parent
REPO = ROOT.parents[1]


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--require-complete', action='store_true')
    args = parser.parse_args()
    inventory = json.loads((ROOT / 'inventory.json').read_text())
    directions = json.loads((ROOT / 'art-direction.json').read_text())
    story = json.loads((REPO / 'Assets/JellyGarden/Resources/Story/arc1.json').read_text())
    live_areas = {area['id']:area for area in story['areas']}
    errors, missing, records, hashes = [], [], [], {}
    for area in inventory['areas']:
        area_id = area['id']
        if area['tasks'] != live_areas[area_id]['tasks']:
            errors.append(f'Area {area_id}: saved objectives differ from the current story')
        if len(directions['areas'][str(area_id)]['repairs']) != len(area['tasks']):
            errors.append(f'Area {area_id}: repair directions do not match the story')
        previous = None
        for state in range(len(area['tasks']) + 1):
            stem = ROOT / 'v1' / f'area-{area_id:03d}' / f'state-{state:02d}'
            path, meta = stem.with_suffix('.png'), stem.with_suffix('.json')
            if not path.exists() or not meta.exists():
                missing.append(f'{area_id}/{state}')
                previous = None
                continue
            record = json.loads(meta.read_text())
            expected_label = area['tasks'][state - 1]['label'] if state else 'Before repairs'
            if (record['area'], record['state'], record['label']) != (area_id, state, expected_label):
                errors.append(f'{path.name}: incorrect area, state or objective')
            reference = Path(record['reference'])
            expected_ref = stem.with_name(f'state-{state - 1:02d}').with_suffix('.png') if state else (ROOT / directions['styleReference']).resolve()
            if reference.resolve() != expected_ref.resolve():
                errors.append(f'{area_id}/{state}: incorrect prior-state reference')
            if not record.get('prompt') or not Path(record['source']).is_file():
                errors.append(f'{area_id}/{state}: prompt or original generation source is missing')
            checksum = digest(path)
            if checksum in hashes:
                errors.append(f'{area_id}/{state}: duplicate of {hashes[checksum]}')
            hashes[checksum] = f'{area_id}/{state}'
            with Image.open(path) as source:
                source.load()
                size = source.size
                if size != (941, 1672):
                    errors.append(f'{area_id}/{state}: unexpected dimensions {size}')
                small = source.convert('RGB').resize((188, 334))
            if previous is not None:
                difference = round(sum(ImageStat.Stat(ImageChops.difference(previous, small)).mean) / 3, 3)
                if difference == 0:
                    errors.append(f'{area_id}/{state}: no visible change from the previous state')
            else:
                difference = None
            previous = small
            records.append({'area':area_id, 'state':state, 'label':expected_label,
                            'path':str(path.relative_to(ROOT)), 'width':size[0], 'height':size[1],
                            'bytes':path.stat().st_size, 'sha256':checksum,
                            'meanPixelDifferenceFromPrevious':difference})
    unchanged = all((REPO / f['path']).is_file() and digest(REPO / f['path']) == f['sha256']
                    for f in inventory['existingFiles'])
    complete = len(records) == inventory['stateCount'] and not missing
    passed = not errors and (complete or not args.require_complete)
    report = {'status':'passed' if passed else 'failed', 'complete':complete,
              'areas':len(inventory['areas']), 'expectedImages':inventory['stateCount'],
              'savedImages':len(records), 'originalRuntimeFilesUnchanged':unchanged,
              'missing':missing, 'errors':errors, 'images':records}
    (ROOT / 'asset-validation.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps({key:value for key,value in report.items() if key not in ('images','missing')}))
    return 0 if passed else 1


if __name__ == '__main__':
    raise SystemExit(main())

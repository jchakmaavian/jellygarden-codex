"""Read-only image validation. Never edits or repairs generated pixels."""
from pathlib import Path
import hashlib
import json
import numpy as np
from PIL import Image

root = Path(__file__).resolve().parent
names = ['Mouth-Peak', 'Moustache-Lifted', 'Eyes-Joy', 'Jaw-Beard']
results = []
for name in names:
    path = root / 'Parts' / (name + '.png')
    result = {'file': str(path.relative_to(root)), 'exists': path.exists()}
    if not path.exists():
        result['pass'] = False
        results.append(result)
        continue
    with Image.open(path) as image:
        image.load()
        result.update(size=list(image.size), mode=image.mode,
                      sha256=hashlib.sha256(path.read_bytes()).hexdigest())
        if image.mode != 'RGBA':
            result['pass'] = False
            results.append(result)
            continue
        alpha = np.asarray(image)[:, :, 3]
        visible = alpha >= 8
        ys, xs = np.where(visible)
        box = [int(xs.min()), int(ys.min()), int(xs.max()+1), int(ys.max()+1)]
        corner = [int(alpha[0,0]), int(alpha[0,-1]), int(alpha[-1,0]), int(alpha[-1,-1])]
        result.update(alpha_range=[int(alpha.min()),int(alpha.max())],
                      transparent_fraction=float((alpha == 0).mean()),
                      near_opaque_fraction=float((alpha >= 240).mean()),
                      faint_alpha_pixels=int(((alpha > 0) & (alpha < 8)).sum()),
                      visible_bbox_alpha8=box, corner_alpha=corner)
        result['pass'] = bool(min(image.size) >= 512 and (alpha == 0).mean() > .1
                              and all(c == 0 for c in corner)
                              and box[0] > 0 and box[1] > 0
                              and box[2] < image.width and box[3] < image.height)
    results.append(result)
report = {'scope':'PNG decode, real alpha, padding and file identity only; not rig or animation QA',
          'pass':all(r['pass'] for r in results),'assets':results}
(root/'art-validation.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8', newline='\n')
print(json.dumps(report,indent=2))
raise SystemExit(0 if report['pass'] else 1)

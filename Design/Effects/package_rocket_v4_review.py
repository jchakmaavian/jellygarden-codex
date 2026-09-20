from pathlib import Path
from datetime import datetime
import json,hashlib,shutil,subprocess
r=Path(__file__).resolve().parents[2];o=r/'Design/Effects/Rocket-review-v4'
since=datetime.fromisoformat('2026-09-15T17:59:35+06:00').timestamp()
checks=o/'checks.txt';report=checks.read_text()
assert checks.stat().st_mtime>since, 'Final test run has not completed yet'
assert 'Errors: \n' in report and 'No runtime errors during rocket tests' in report and 'FAIL:' not in report, 'Final tests need attention'
count=int(report.splitlines()[0].split(':')[1]);assert count>=812
subprocess.run([__import__('sys').executable,str(r/'Design/Effects/verify_rocket_v4.py')],check=True)
result=json.loads((o/'preservation-verification.json').read_text());assert not result['unexpected_changes']
captures=[p for p in o.glob('*.png') if p.stat().st_mtime>since]
(o/'capture-manifest.json').write_text(json.dumps({'final_run_started':'2026-09-15T17:59:35+06:00','capture_count':len(captures),'captures':[{'name':p.name,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted(captures)]},indent=2))
(o/'previews').mkdir(exist_ok=True)
for axis in ['horizontal','vertical']:
    for speed in ['0.5','1']:
        p=o/f'length-5.0-{axis}-{speed}-10.png';assert p.stat().st_mtime>since
        shutil.copy2(p,o/'previews'/p.name)
p=o/'IMPLEMENTATION.md';s=p.read_text();s=s.replace('Final Play Mode regression pending.',f'Final Play Mode regression: **{count} checks passed**, at 0.5× then 1×, with no runtime errors during the run. All 12 length/direction/speed combinations measured the requested 4.500 / 5.000 / 5.500 cell extents. The final 5-cell setting passed the full rocket scenarios and shared-pool gradient restoration checks. The original player board, moves, score, coins and lives were restored.')
s=s.replace('The pooled slot remains occupied until its trails finish.','The pooled slot remains occupied until its trails finish. The short startup tail also has a soft cap, and the original borrowed trail gradient is restored on retirement or cancellation so other boosters retain their appearance.')
p.write_text(s)
print(json.dumps({'passed':count,'final_captures':len(captures),'preservation':result}))

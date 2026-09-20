from pathlib import Path
import hashlib,json,difflib,shutil
r=Path(__file__).resolve().parents[2];out=r/'Design/Effects/Rocket-review-v4'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
base=json.loads((out/'preserved-sha256.json').read_text());changed=[]
for name,before in base.items():
    p=r/name
    if not p.exists() or sha(p)!=before:changed.append(name)
preserve={'baseline_asset_files':len(base),'unexpected_changes':changed}
before=(out/'before/Assets/JellyGarden/Scripts/Juice/RoyalRocketSequence.cs').read_text()
after=(r/'Assets/JellyGarden/Scripts/Juice/RoyalRocketSequence.cs').read_text()
for start,end in [('IEnumerator RocketCell','// QA replays'),('float RocketExitDistance','static Gradient RocketTrailGradient')]:
    preserve[start+' unchanged']=before[before.index(start):before.index(end)]==after[after.index(start):after.index(end)]
(out/'preservation-verification.json').write_text(json.dumps(preserve,indent=2))
patch=[];hashes={}
for p in (out/'before').rglob('*'):
    if not p.is_file():continue
    name=str(p.relative_to(out/'before')).replace('\\','/')
    latest=r/name;hashes[name]=sha(latest)
    patch.extend(difflib.unified_diff(p.read_text().splitlines(True),latest.read_text().splitlines(True),fromfile='a/'+name,tofile='b/'+name))
    dest=out/'verified-source'/name;dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(latest,dest)
    if Path(str(latest)+'.meta').exists():shutil.copy2(Path(str(latest)+'.meta'),Path(str(dest)+'.meta'))
for name in ['Assets/JellyGarden/Resources/RoyalRocketFX/WhiteSmoke.mat','Assets/JellyGarden/Resources/RoyalRocketFX/WhiteSmoke.mat.meta']:
    p=r/name;hashes[name]=sha(p);patch.extend(difflib.unified_diff([],p.read_text().splitlines(True),fromfile='/dev/null',tofile='b/'+name));dest=out/'verified-source'/name;dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest)
(out/'rocket-v4.patch').write_text(''.join(patch));(out/'changed-files-sha256.json').write_text(json.dumps(hashes,indent=2))
print(json.dumps(preserve))

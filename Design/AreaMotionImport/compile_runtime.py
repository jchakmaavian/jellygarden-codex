from pathlib import Path
import re,subprocess
root=Path(__file__).resolve().parents[2];out=root/'Design/AreaMotionImport/Validation';out.mkdir(exist_ok=True)
candidates=list((root/'Library/Bee/artifacts').rglob('Assembly-CSharp.rsp'))
source=max(candidates,key=lambda p:p.stat().st_mtime) if candidates else root/'Design/KingVanguard-ActivityFix-20260930/Validation/Assembly-CSharp.rsp'
s=source.read_text(encoding='utf-8-sig')
s=re.sub(r'^[-/]out:.*$', '-out:"Design/AreaMotionImport/Validation/Assembly-CSharp.dll"',s,flags=re.M)
s=re.sub(r'^[-/]refout:.*$', '-refout:"Design/AreaMotionImport/Validation/Assembly-CSharp.ref.dll"',s,flags=re.M)
for p in (root/'Assets/JellyGarden/Scripts/GUI/AreaMotion').glob('*.cs'):
 rel=p.relative_to(root).as_posix()
 if rel not in s:s+='\n"'+rel+'"\n'
(out/'Assembly-CSharp.rsp').write_text(s,encoding='utf-8')
editor=Path('C:/Program Files/Unity/Hub/Editor/6000.5.10f1/Editor/Data')
cmd=[str(editor/'NetCoreRuntime/dotnet.exe'),str(editor/'DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll'),'@Design/AreaMotionImport/Validation/Assembly-CSharp.rsp']
r=subprocess.run(cmd,cwd=root,capture_output=True,text=True);(out/'runtime-compile.log').write_text(r.stdout+r.stderr);print(r.stdout+r.stderr);raise SystemExit(r.returncode)

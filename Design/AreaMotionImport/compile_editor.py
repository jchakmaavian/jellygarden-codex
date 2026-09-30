from pathlib import Path
import re,subprocess
root=Path(__file__).resolve().parents[2];out=root/'Design/AreaMotionImport/Validation'
source=max((root/'Library/Bee/artifacts').rglob('Assembly-CSharp-Editor.rsp'),key=lambda p:p.stat().st_mtime)
s=source.read_text(encoding='utf-8-sig')
for flag,name in [('out','Assembly-CSharp-Editor.dll'),('refout','Assembly-CSharp-Editor.ref.dll')]:
 s=re.sub(r'^[-/]'+flag+r':.*$', '-'+flag+':"Design/AreaMotionImport/Validation/'+name+'"',s,flags=re.M)
s=re.sub(r'^-r:"[^"]*Assembly-CSharp.ref.dll"$', '-r:"Design/AreaMotionImport/Validation/Assembly-CSharp.ref.dll"',s,flags=re.M)
rel='Assets/JellyGarden/Scripts/Editor/RoyalAreaMotionImport.cs'
if rel not in s:s+='\n"'+rel+'"\n'
(out/'Assembly-CSharp-Editor.rsp').write_text(s,encoding='utf-8')
editor=Path('C:/Program Files/Unity/Hub/Editor/6000.5.10f1/Editor/Data')
r=subprocess.run([str(editor/'NetCoreRuntime/dotnet.exe'),str(editor/'DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll'),'@Design/AreaMotionImport/Validation/Assembly-CSharp-Editor.rsp'],cwd=root,capture_output=True,text=True)
(out/'editor-compile.log').write_text(r.stdout+r.stderr);print(r.stdout+r.stderr);raise SystemExit(r.returncode)

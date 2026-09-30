from pathlib import Path
import shutil,json
p=Path(__file__).resolve().parents[2];v=p.parent/'RoyalArea-Unity-Verification'
def copy(a,b):b.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(a,b)
for folder in ['Assets','Packages','ProjectSettings','Results']:(v/folder).mkdir(parents=True,exist_ok=True)
shutil.copytree(p/'Assets/JellyGarden/Resources/RoyalAreas/NativeMotion',v/'Assets/Resources/RoyalAreas/NativeMotion',dirs_exist_ok=True)
for s in (p/'Assets/JellyGarden/Scripts/GUI/AreaMotion').glob('*.cs'):copy(s,v/'Assets/Runtime'/s.name)
copy(p/'Assets/JellyGarden/Scripts/Editor/RoyalAreaMotionImport.cs',v/'Assets/Editor/RoyalAreaMotionImport.cs')
# Use the exact importer settings in the smaller isolated project's resource location.
f=v/'Assets/Editor/RoyalAreaMotionImport.cs';f.write_text(f.read_text().replace('Assets/JellyGarden/Resources/','Assets/Resources/'))
copy(p/'Design/AreaMotionImport/RoyalAreaMotionVerification.cs',v/'Assets/Editor/RoyalAreaMotionVerification.cs')
copy(p/'ProjectSettings/ProjectVersion.txt',v/'ProjectSettings/ProjectVersion.txt')
settings=(p.parent/'Hammer-Import/ProjectSettings/ProjectSettings.asset').read_text().replace('productName: Hammer-Import','productName: RoyalAreaMotionVerification').replace('companyName: DefaultCompany','companyName: CodexVerification')
(v/'ProjectSettings/ProjectSettings.asset').write_text(settings)
ugui=next((p/'Library/PackageCache').glob('com.unity.ugui@*'))
deps={f'com.unity.modules.{m}':'1.0.0' for m in ['animation','audio','imageconversion','particlesystem','ui','imgui','physics','physics2d','jsonserialize','screencapture']};deps['com.unity.ugui']='file:'+ugui.as_posix()
(v/'Packages/manifest.json').write_text(json.dumps({'dependencies':deps},indent=2))
print(v)

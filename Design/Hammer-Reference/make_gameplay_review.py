from pathlib import Path
import sys,subprocess,json,tarfile,hashlib
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1];OUT=HERE/'Review'
sys.path.insert(0,str(ROOT/'Design/ReferenceGameplayImport/Media/Tools/Python'))
from PIL import Image,ImageDraw,ImageFont
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',20)
sheet=Image.new('RGB',(1440,1350),(12,25,48));draw=ImageDraw.Draw(sheet)
for i,n in enumerate([0,9,17,23,27,33,42,56,67,76,83,110]):
    im=Image.open(OUT/'GameplayFrames'/f'{n:04}.png').resize((240,426),Image.Resampling.LANCZOS)
    x=(i%6)*240;y=(i//6)*450;sheet.paste(im,(x,y));draw.text((x+12,y+425),f'{n/60:.3f} s',font=font,fill='white')
# Two rows: keep the exported artifact compact.
sheet.crop((0,0,1440,900)).save(OUT/'Hammer-Gameplay-contact.jpg',quality=94)
title=Image.new('RGB',(720,48),(12,25,48));d=ImageDraw.Draw(title)
d.text((15,12),'Video reference',font=font,fill='white');d.text((375,12),'Royal Throne implementation',font=font,fill='white');title.save(OUT/'comparison-title.png')
ffmpeg='C:/Program Files/Softdeluxe/Free Download Manager/ffmpeg.exe'
render=[ffmpeg,'-hide_banner','-loglevel','error','-ss','9.18','-i','G:/Unity Projects/Match3-Games_ArtSource/Match-3-JC/00_References/animation-VFX/Hammer.mp4','-i',str(OUT/'Hammer-Gameplay.mp4'),'-loop','1','-framerate','60','-i',str(OUT/'comparison-title.png'),'-filter_complex','[0:v]setpts=PTS-STARTPTS,fps=60,scale=360:800,setsar=1[a];[1:v]setpts=PTS-STARTPTS,scale=360:640,pad=360:800:0:0:color=0x0c1930,setsar=1[b];[a][b]hstack=inputs=2[c];[2:v][c]vstack=inputs=2[v]','-map','[v]','-an','-t','2.5','-r','60','-f','rawvideo','-pix_fmt','rgb24','pipe:1']
decoder=subprocess.Popen(render,stdout=subprocess.PIPE)
encoder=subprocess.Popen(['C:/Program Files/TimeDoctor2/3.18.89/ffmpeg.exe','-hide_banner','-loglevel','error','-y','-f','rawvideo','-pix_fmt','rgb24','-s','720x848','-r','60','-i','pipe:0','-c:v','libx264','-preset','fast','-crf','19','-threads','4','-pix_fmt','yuv420p','-movflags','+faststart',str(OUT/'Hammer-Reference-vs-Gameplay.mp4')],stdin=decoder.stdout)
decoder.stdout.close();assert encoder.wait()==0;assert decoder.wait()==0

package=ROOT.parent/'Hammer-Animation-VFX-SFX.unitypackage'
with tarfile.open(package) as t:
    paths=[t.extractfile(n).read().decode() for n in t.getmembers() if n.name.endswith('/pathname')]
assert all(p.startswith(('Assets/ReferenceGameplay/Tools/Hammer','Assets/ReferenceGameplay/VFX/')) for p in paths),paths
for name in ['HammerExplodeAnimation2.anim','HammerAnimation.prefab','HammerImpactRfv387.prefab','HammerRewardRfv387.prefab','hammer_use_appear.wav','hammer_use_hit.wav','hammer_use_disappear.wav']:
    assert any(p.endswith('/'+name) for p in paths),name
result=dict(ok=True,sha256=hashlib.sha256(package.read_bytes()).hexdigest(),assetCount=len(paths),assetPaths=paths)
(OUT/'full-package-verification.json').write_text(json.dumps(result,indent=2))
print('Review and package verification complete:',len(paths),'package entries')

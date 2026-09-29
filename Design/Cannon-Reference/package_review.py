from pathlib import Path
import subprocess,sys,json
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Design/ReferenceGameplayImport/Media/Tools/Python'))
from PIL import Image,ImageDraw
out=Path(__file__).parent/'Review'
ff='C:/Program Files/Softdeluxe/Free Download Manager/ffmpeg.exe'
start=(out/'Frames/0000.png').stat().st_mtime
count=0
while (out/f'Frames/{count:04d}.png').exists() and (out/f'Frames/{count:04d}.png').stat().st_mtime>=start:count+=1
subprocess.run([ff,'-hide_banner','-loglevel','error','-y','-framerate','60','-i',str(out/'Frames/%04d.png'),'-frames:v',str(count),'-vf','scale=720:1280','-c:v','h264_mf','-b:v','7000k','-pix_fmt','yuv420p','-movflags','+faststart',str(out/'Cannon-Gameplay-v3.mp4')],check=True)
(out/'capture.json').write_text(json.dumps(dict(frames=count,fps=60,source='Unity Game view; fixed simulated timestep',audio='Silent screen capture',performanceBenchmark=False),indent=2))
indices=[0,12,24,42,55,59,61,64,68,73,85,96]
sheet=Image.new('RGB',(6*270,2*504),(20,24,34));draw=ImageDraw.Draw(sheet)
for n,i in enumerate(indices):
 path=out/f'Frames/{i:04d}.png'
 if i>=count or not path.exists():continue
 im=Image.open(path).convert('RGB').resize((270,480),Image.Resampling.LANCZOS)
 x=n%6*270;y=n//6*504;sheet.paste(im,(x,y));draw.text((x+8,y+483),f'{i/60:.2f}s',fill='white')
sheet.save(out/'Cannon-Gameplay-v3-sheet.jpg',quality=92)
print(out/'Cannon-Gameplay-v3.mp4')

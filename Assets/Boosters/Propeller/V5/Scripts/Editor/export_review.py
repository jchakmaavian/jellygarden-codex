from pathlib import Path
import subprocess,json,csv,sys,hashlib
from PIL import Image,ImageDraw,ImageFont
P=Path(__file__).resolve().parents[6];V=P/'Assets/Boosters/Propeller/V5';R=V/'Animation/Review';OUT=V/'Animation/Previews';FF='C:/Program Files/Softdeluxe/Free Download Manager/ffmpeg.exe'
codec=['-c:v','h264_mf','-b:v','5000k','-pix_fmt','yuv420p','-c:a','aac','-b:a','192k','-movflags','+faststart']
def ff(a):
 dest=Path(a[-1]);temp=dest.with_name('.encoding-'+dest.name)
 subprocess.run([FF,'-hide_banner','-loglevel','error','-y']+a[:-1]+['-bsf:v','h264_metadata=colour_primaries=1:transfer_characteristics=1:matrix_coefficients=1',str(temp)],check=True)
 temp.replace(dest)
def rows(p):return list(csv.DictReader(p.open()))
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',18)
def crop(r,root):
 im=Image.open(root/('%04d.jpg'%int(r['frame'])));x=540+float(r['x'])*156.8;y=1004-float(r['y'])*156.8
 return im.crop((int(x-105),int(y-115),int(x+105),int(y+100)))
def sheets(name):
 root=R/'.Capture'/name
 if not (root/'motion.csv').exists():return
 motion=rows(root/'motion.csv')
 for phase in ['Activation','Flight']:
  entries=[r for r in motion if r['phase']==phase][:24];sheet=Image.new('RGB',(1440,1120),'#132238');d=ImageDraw.Draw(sheet)
  for i,r in enumerate(entries):
   left=(i%6)*240;top=(i//6)*280;sheet.paste(crop(r,root),(left+8,top+50));d.text((left+8,top+7),f'{phase} {float(r["age"]):.3f}s',font=font,fill='white');d.text((left+8,top+28),f'Phase {float(r.get("rotorAngle",0)):.1f} / blur {float(r.get("blur",0)):.2f}',font=font,fill='white')
  sheet.save(OUT/(name+'-'+phase+'.jpg'),quality=94)
for n in ['corner-target','rotor-60fps']:sheets(n)
if '--sheets' in sys.argv:sys.exit()
names=['square-match-create','short-flight','medium-flight','long-flight','edge-target','corner-target','3-propellers','5-propellers','propeller-plus-tnt','pooled-reuse','rotor-60fps']
report={}
for name in names:
 f=R/'.Capture'/name;info=json.loads((f/'capture.json').read_text());rate=60 if name=='rotor-60fps' else 30;report[name]=dict(info,fps=rate)
 ff(['-framerate',str(rate),'-i',str(f/'%04d.jpg'),'-i',str(f/'audio.wav'),'-frames:v',str(info['frames']),'-vf','scale=720:1280','-af','apad','-t',str(info['frames']/rate)]+codec+[str(OUT/('60fps-gameplay.mp4' if name=='rotor-60fps' else name+'.mp4'))])
concat=OUT/'gameplay-clips.txt';concat.write_text(''.join("file '"+(OUT/(n+'.mp4')).as_posix()+"'\n" for n in names[:9]))
ff(['-f','concat','-safe','0','-i',str(concat),'-c','copy','-movflags','+faststart',str(OUT/'Propeller-V5-Gameplay.mp4')])
for name,fps in [('corner-target',30),('rotor-60fps',60)]:
 root=R/'.Capture'/name;motion=rows(root/'motion.csv');entries=[r for r in motion if r['phase'] in ['Activation','Flight','Impact']];temp=R/'.Closeup'/name;temp.mkdir(parents=True,exist_ok=True)
 seen=set();unique=[]
 for r in entries:
  if r['frame'] not in seen:unique.append(r);seen.add(r['frame'])
 for i,r in enumerate(unique):
  tile=crop(r,root).resize((504,516));im=Image.new('RGB',(540,590),'#132238');im.paste(tile,(18,62));d=ImageDraw.Draw(im);d.text((18,10),f'{fps} FPS / {r["phase"]} / phase {float(r["rotorAngle"]):.1f}',font=font,fill='white');d.text((18,33),'Real tilted rotor - camera tracks body for review',font=font,fill='white');im.save(temp/f'{i:04d}.jpg',quality=94)
 for slow in [False,True]:
  rate=fps/4 if slow else fps;suffix='-Slow-Review' if slow else ''
  ff(['-framerate',str(rate),'-i',str(temp/'%04d.jpg'),'-an','-c:v','h264_mf','-b:v','4000k','-pix_fmt','yuv420p','-movflags','+faststart',str(OUT/f'Rotor-{fps}FPS{suffix}.mp4')])
motion=rows(R/'.Capture/corner-target/motion.csv');oldroot=P/'Assets/Boosters/Propeller/V4';previous=rows(oldroot/'Animation/Review/.Capture/corner-target/motion.csv')
starts=[int(next(r for r in a if r['phase']=='Activation')['frame'])/30 for a in [previous,motion]]
labels=Image.new('RGB',(1080,40),'#132238');d=ImageDraw.Draw(labels);d.text((20,10),'V4 / 1.0x - crisp blades alias',font=font,fill='white');d.text((555,10),'V5 / 1.0x - progressive phase + swept rotor',font=font,fill='white');labels.save(OUT/'comparison-labels.png')
filters='[0:v]crop=690:760:15:260,scale=540:594,pad=540:634:0:40:black[a];[1:v]crop=690:760:15:260,scale=540:594,pad=540:634:0:40:black[b];[a][b]hstack=inputs=2[both];[both][2:v]overlay=0:0[v]'
ff(['-ss',str(starts[0]),'-i',str(oldroot/'Animation/Previews/corner-target.mp4'),'-ss',str(starts[1]),'-i',str(OUT/'corner-target.mp4'),'-loop','1','-i',str(OUT/'comparison-labels.png'),'-filter_complex',filters,'-map','[v]','-map','1:a','-t','2.6']+codec+[str(OUT/'V4-left-V5-right.mp4')])
(R/'recording-report.json').write_text(json.dumps(report,indent=2))
locked=json.loads((R/'locked-hashes.json').read_text());changed=[name for name,h in locked.items() if not (P/name).exists() or hashlib.sha256((P/name).read_bytes()).hexdigest()!=h]
(R/'locked-verification.json').write_text(json.dumps({'files':len(locked),'changed':changed},indent=2));assert not changed,changed
print('V5 recordings exported; old versions and TNT hashes unchanged.')

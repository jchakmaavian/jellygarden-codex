from pathlib import Path
import subprocess,json,csv
from PIL import Image,ImageDraw,ImageFont
P=Path(__file__).resolve().parents[6];R=P/'Assets/Boosters/Propeller/V4/Animation/Review';OUT=R.parent/'Previews';OUT.mkdir(exist_ok=True)
FF='C:/Program Files/Softdeluxe/Free Download Manager/ffmpeg.exe'
codec=['-c:v','h264_mf','-b:v','5000k','-pix_fmt','yuv420p','-c:a','aac','-b:a','192k','-movflags','+faststart']
def ff(args):subprocess.run([FF,'-hide_banner','-loglevel','error','-y']+args,check=True)
names=['square-match-create','short-flight','medium-flight','long-flight','edge-target','corner-target','3-propellers','5-propellers','propeller-plus-tnt','pooled-reuse','tnt-before','tnt-after']
for name in names:
 f=R/'.Capture'/name;info=json.loads((f/'capture.json').read_text())
 ff(['-framerate','30','-i',str(f/'%04d.jpg'),'-i',str(f/'audio.wav'),'-frames:v',str(info['frames']),'-vf','scale=720:1280','-af','apad','-t',str(info['frames']/30)]+codec+[str(OUT/(name+'.mp4'))])
concat=OUT/'gameplay-clips.txt';concat.write_text(''.join("file '"+(OUT/(n+'.mp4')).as_posix()+"'\n" for n in names[:9]))
ff(['-f','concat','-safe','0','-i',str(concat),'-c','copy','-movflags','+faststart',str(OUT/'Propeller-V4-Gameplay.mp4')])
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',18)
def rows(path):return list(csv.DictReader(path.open()))
motion=rows(R/'.Capture/corner-target/motion.csv')
previous=rows(P/'Assets/Boosters/Propeller/V3/Animation/Review/.Capture/corner-target/motion.csv')
starts=[int(next(r for r in a if r['phase']=='Activation')['frame'])/30 for a in [previous,motion]]
labels=Image.new('RGB',(1080,40),'#132238');d=ImageDraw.Draw(labels);d.text((20,10),'V3 / 1.0x - fixed duration',font=font,fill='white');d.text((555,10),'V4 / 1.0x - distance timing + original SFX',font=font,fill='white');labels.save(OUT/'comparison-labels.png')
filters='[0:v]crop=690:760:15:260,scale=540:594,pad=540:634:0:40:black[a];[1:v]crop=690:760:15:260,scale=540:594,pad=540:634:0:40:black[b];[a][b]hstack=inputs=2[both];[both][2:v]overlay=0:0[v]'
ff(['-ss',str(starts[0]),'-i',str(P/'Assets/Boosters/Propeller/V3/Animation/Previews/corner-target.mp4'),'-ss',str(starts[1]),'-i',str(OUT/'corner-target.mp4'),'-loop','1','-i',str(OUT/'comparison-labels.png'),'-filter_complex',filters,'-map','[v]','-map','1:a','-t','2.6']+codec+[str(OUT/'V3-left-V4-right.mp4')])
# Consecutive takeoff frames: every rendered frame at 30 Hz, no temporal gaps.
entries=[r for r in motion if r['phase']=='Activation']
sheet=Image.new('RGB',(1560,600),'#132238');d=ImageDraw.Draw(sheet)
for i,r in enumerate(entries[:14]):
 im=Image.open(R/'.Capture/corner-target'/('%04d.jpg'%int(r['frame'])));x=540+float(r['x'])*156.8;y=1004-float(r['y'])*156.8
 tile=im.crop((int(x-100),int(y-115),int(x+100),int(y+100)))
 left=(i%7)*222;top=(i//7)*300;sheet.paste(tile,(left+6,top+48));d.text((left+6,top+8),f'{float(r["age"]):.3f}s / {float(r["rotorSpeed"]):.0f} deg/s',font=font,fill='white')
sheet.save(OUT/'Takeoff-Consecutive-Frames.jpg',quality=94)
flight=[r for r in motion if r['phase']=='Flight'];sheet=Image.new('RGB',(1440,580),'#132238');d=ImageDraw.Draw(sheet)
for i,r in enumerate(flight[::2][:12]):
 im=Image.open(R/'.Capture/corner-target'/('%04d.jpg'%int(r['frame'])));x=540+float(r['x'])*156.8;y=1004-float(r['y'])*156.8;tile=im.crop((int(x-105),int(y-115),int(x+105),int(y+100)))
 left=(i%6)*240;top=(i//6)*290;sheet.paste(tile,(left+8,top+48));d.text((left+8,top+8),f'Flight {float(r["age"]):.3f}s',font=font,fill='white')
sheet.save(OUT/'Flight-Frames.jpg',quality=94)
report={}
for name in names:
 f=R/'.Capture'/name;info=json.loads((f/'capture.json').read_text());report[name]=info
(R/'recording-report.json').write_text(json.dumps(report,indent=2))
print('Normal-speed V4 gameplay WITH Unity audio, matched-target V3 comparison and frame sheets exported.')

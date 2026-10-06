"""Encode the Unity recording and prepare unchanged V2 versus live V3 comparison."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import subprocess,csv,json,hashlib
ROOT=Path(__file__).resolve().parents[2]; PROJECT=ROOT.parents[3]
R=ROOT/'Animation/Review';OUT=ROOT/'Animation/Previews';OUT.mkdir(exist_ok=True)
FF=r'C:/Program Files/Softdeluxe/Free Download Manager/ffmpeg.exe'
def ff(args):subprocess.run([FF,'-hide_banner','-loglevel','error','-y']+args,check=True)
codec=['-c:v','h264_mf','-b:v','5000k','-pix_fmt','yuv420p','-movflags','+faststart']
names=['square-match-create','half-speed','normal-speed','corner-target','edge-target','five-simultaneous','propeller-plus-tnt','cancel-and-reuse']
for n in names:
    folder=R/'.Capture'/n;count=len((folder/'timestamps.txt').read_text().splitlines())
    ff(['-framerate','30','-i',str(folder/'%04d.jpg'),'-frames:v',str(count),'-vf','scale=720:1280']+codec+[str(OUT/(n+'.mp4'))])
def rows(name):return list(csv.DictReader((R/'.Capture'/name/'motion.csv').open()))
motion=rows('half-speed');idle=rows('normal-speed')
phase_ranges={}
for phase in ['Reveal','Idle','Activation','Flight','Impact']:
    entries=[r for r in motion if r['phase']==phase]
    start=int(entries[0]['frame'])/30;end=(int(entries[-1]['frame'])+1)/30
    if phase=='Impact':end+=.25
    phase_ranges[phase]=(start,end)
    ff(['-ss',str(start),'-i',str(OUT/'half-speed.mp4'),'-t',str(end-start)]+codec+[str(OUT/(phase+'.mp4'))])
idle_start=int(next(r for r in idle if r['phase']=='Idle')['frame'])/30+.1
ff(['-ss',str(idle_start),'-i',str(OUT/'normal-speed.mp4'),'-t','10']+codec+[str(OUT/'Idle-10-seconds.mp4')])
concat=OUT/'gameplay-clips.txt';concat.write_text(''.join("file '"+(OUT/(n+'.mp4')).as_posix()+"'\n" for n in ['normal-speed','corner-target','edge-target','five-simultaneous','propeller-plus-tnt']))
ff(['-f','concat','-safe','0','-i',str(concat),'-c','copy','-movflags','+faststart',str(OUT/'Propeller-V3-Gameplay.mp4')])
v2=ROOT.parent/'V2/Animation/Previews/half-speed.mp4'
start=phase_ranges['Activation'][0]-.0333
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',18)
labels=Image.new('RGB',(1080,40),'#132238');ld=ImageDraw.Draw(labels)
ld.text((20,10),'V2 - original / 0.5x',fill='white',font=font);ld.text((560,10),'V3 - corrected takeoff / 0.5x',fill='white',font=font)
labels.save(OUT/'comparison-labels.png')
filters='[0:v]crop=690:760:15:260,scale=540:594,pad=540:634:0:40:black[a];[1:v]crop=690:760:15:260,scale=540:594,pad=540:634:0:40:black[b];[a][b]hstack=inputs=2[both];[both][2:v]overlay=0:0'
ff(['-ss','2.7667','-i',str(v2),'-ss',str(start),'-i',str(OUT/'half-speed.mp4'),'-loop','1','-i',str(OUT/'comparison-labels.png'),'-filter_complex',filters,'-t','2.65']+codec+[str(OUT/'V2-left-V3-right-Takeoff.mp4')])
sheet=Image.new('RGB',(1440,690),'#132238');d=ImageDraw.Draw(sheet)
selection=[('Activation',t) for t in [.033,.08,.15,.22,.27,.33,.40,.45]]+[('Flight',t) for t in [.08,.23,.4]]+[('Impact',.06)]
for k,(phase,age) in enumerate(selection):
    row=min((r for r in motion if r['phase']==phase),key=lambda r:abs(float(r['age'])-age))
    frame=int(row['frame']);im=Image.open(R/'.Capture/half-speed'/('%04d.jpg'%frame))
    x=540+float(row['x'])*156.8;y=1004-float(row['y'])*156.8
    crop=im.crop((int(x-115),int(y-130),int(x+115),int(y+100)))
    left=(k%6)*240;top=(k//6)*345;sheet.paste(crop,(left+5,top+60))
    d.text((left+8,top+10),f'{phase} {float(row["age"]):.2f}s',fill='white',font=font)
    d.text((left+8,top+310),f'Rotor {float(row["rotorSpeed"]):.0f} deg/s',fill='#b9cdec',font=font)
sheet.save(OUT/'V3-Motion-Frames.jpg',quality=94)
phases=['Reveal','Idle','Activation','Lift','Flight','Impact-flash']
sheet=Image.new('RGB',(1440,460),'#132238');d=ImageDraw.Draw(sheet)
for k,phase in enumerate(phases):
    im=Image.open(R/f'half-speed-{phase}.png')
    if phase.startswith('Impact'):
        row=next(r for r in motion if r['phase']=='Impact');x=540+float(row['x'])*156.8;y=1004-float(row['y'])*156.8
    elif phase=='Flight':
        row=min((r for r in motion if r['phase']=='Flight'),key=lambda r:abs(float(r['age'])-.11));x=540+float(row['x'])*156.8;y=1004-float(row['y'])*156.8
    else:x,y=347,920 if phase!='Lift' else 872
    crop=im.crop((int(x-110),int(y-130),int(x+110),int(y+90)))
    sheet.paste(crop,(k*240+10,70));d.text((k*240+10,22),phase,fill='white',font=font)
sheet.save(OUT/'Phase-Screenshots.jpg',quality=94)
locked=json.loads((R/'locked-hashes.json').read_text())
changed=[e['path'] for e in locked if hashlib.sha256((PROJECT/e['path']).read_bytes()).hexdigest().upper()!=e['hash'].upper()]
(R/'locked-verification.json').write_text(json.dumps({'checked':len(locked),'changed':changed},indent=2));assert not changed,changed
(OUT/'phase-ranges.json').write_text(json.dumps(phase_ranges,indent=2))
print('Exported V3 recordings and V2-left/V3-right comparison; protected files:',len(locked))

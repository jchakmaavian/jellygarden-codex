"""Export only the focused Unity captures; does not create animation artwork."""
from pathlib import Path
import subprocess, json, hashlib
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
PROJECT = ROOT.parents[3]
REVIEW = ROOT / 'Animation/Review'
OUT = ROOT / 'Animation/Previews'
OUT.mkdir(exist_ok=True)
FFMPEG = r'C:/Program Files/Softdeluxe/Free Download Manager/ffmpeg.exe'

def ff(args):
    subprocess.run([FFMPEG, '-hide_banner', '-loglevel', 'error', '-y'] + args, check=True)

names = ['square-match-create','half-speed','normal-speed','five-simultaneous','propeller-plus-tnt','cancel-and-reuse']
for name in names:
    if (OUT/(name+'.mp4')).exists(): continue
    folder = REVIEW / '.Capture' / name
    count = len((folder/'timestamps.txt').read_text().splitlines())
    # Unity's temporary capture clock advances 1/30 game-unscaled seconds per frame.
    # Wall-clock capture timestamps include the cost of writing screenshots.
    ff(['-framerate','30','-i',str(folder/'%04d.jpg'),'-frames:v',str(count),
        '-vf','scale=720:1280','-c:v','h264_mf','-b:v','5000k','-pix_fmt','yuv420p',
        '-movflags','+faststart',str(OUT/(name+'.mp4'))])

clips = [('Reveal',0,1.1),('Idle',1.12,1.2),('Takeoff',2.766,0.70),
         ('Flight',3.466,0.967),('Impact',4.433,0.62)]
for name,start,length in clips:
    if (OUT/(name+'.mp4')).exists(): continue
    ff(['-ss',str(start),'-i',str(OUT/'half-speed.mp4'),'-t',str(length),
        '-c:v','h264_mf','-b:v','5000k','-pix_fmt','yuv420p','-movflags','+faststart',str(OUT/(name+'.mp4'))])

concat=OUT/'gameplay-clips.txt'
concat.write_text(''.join("file '"+(OUT/(n+'.mp4')).as_posix()+"'\n" for n in ['normal-speed','five-simultaneous','propeller-plus-tnt']))
ff(['-f','concat','-safe','0','-i',str(concat),'-c','copy','-movflags','+faststart',str(OUT/'Propeller-V2-Gameplay.mp4')])

font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',20)
small=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',16)
sheet=Image.new('RGB',(1500,410),'#101c32');d=ImageDraw.Draw(sheet)
phases=[('Reveal',(210,800,480,1070)),('Idle',(210,800,480,1070)),
        ('Activation',(210,730,480,1000)),('Flight',(300,570,570,840)),
        ('Impact',(730,440,1000,710))]
for idx,(phase,box) in enumerate(phases):
    im=Image.open(REVIEW/('half-speed-'+phase+'.png')).crop(box)
    sheet.paste(im,(idx*300+15,65));d.text((idx*300+15,20),phase,fill='white',font=font)
    d.text((idx*300+15,355),'Live Unity capture / 0.5x',fill='#b8c9e3',font=small)
sheet.save(OUT/'Phase-Screenshots.jpg',quality=94)

old=Image.open(PROJECT/'Design/Animation/Propeller-Idle-v1/frames/Propeller_Idle_001.webp').convert('RGBA')
old=old.crop(old.getbbox());old.thumbnail((260,260))
compare=Image.new('RGB',(720,400),'#101c32');d=ImageDraw.Draw(compare)
compare.paste(old,(45+(260-old.width)//2,90+(260-old.height)//2),old)
live=Image.open(REVIEW/'half-speed-Idle.png').crop((282,876,412,1006)).resize((260,260),Image.Resampling.LANCZOS)
compare.paste(live,(405,90))
d.text((30,22),'V1 authored Idle frame',fill='white',font=font)
d.text((390,22),'V2 live mesh rig',fill='white',font=font)
d.text((30,370),'Artwork comparison; separately framed at readable size.',fill='#b8c9e3',font=small)
compare.save(OUT/'V1-vs-V2.jpg',quality=94)

# Sequential takeoff/flight review to expose gaps or pose jumps.
motion=Image.new('RGB',(1400,690),'#101c32');d=ImageDraw.Draw(motion)
for k,frame in enumerate([82,85,88,91,94,97,100,104,108,112]):
    im=Image.open(REVIEW/'.Capture/half-speed'/('%04d.jpg'%frame)).crop((170,440,950,1140))
    im.thumbnail((270,295));x=(k%5)*280;y=(k//5)*345
    motion.paste(im,(x,y+35));d.text((x+8,y+7),'%.2fs'%(frame/30),fill='white',font=small)
motion.save(OUT/'Motion-Review.jpg',quality=94)

locked=json.loads((REVIEW/'locked-hashes.json').read_text(encoding='utf-8-sig'))
changed=[entry['path'] for entry in locked if hashlib.sha256((PROJECT/entry['path']).read_bytes()).hexdigest().upper()!=entry['hash'].upper()]
(REVIEW/'locked-verification.json').write_text(json.dumps({'checked':len(locked),'changed':changed},indent=2))
assert not changed, changed
print('Exported focused previews; protected files unchanged:',len(locked))

from pathlib import Path
import subprocess,json,csv,sys
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[2]
REVIEW=ROOT/'Animation/Review';OUT=ROOT/'Animation/Previews';OUT.mkdir(parents=True,exist_ok=True)
FF=r'C:/Program Files/Softdeluxe/Free Download Manager/ffmpeg.exe'
def run(args):subprocess.run([FF,'-v','error','-y',*map(str,args)],check=True)
def export(inputs,out,extra=[]):
 tmp=out.parent/('.encoding-'+out.name)
 run([*inputs,*extra,'-c:v','h264_mf','-b:v','5000k','-pix_fmt','yuv420p','-bsf:v','h264_metadata=colour_primaries=1:transfer_characteristics=1:matrix_coefficients=1','-c:a','aac','-b:a','192k','-movflags','+faststart',tmp]);tmp.replace(out)
for folder in (REVIEW/'.Capture').iterdir() if '--comparison-only' not in sys.argv else []:
 if not (folder/'capture.json').exists():continue
 info=json.loads((folder/'capture.json').read_text());fps=60 if '60fps' in folder.name else 30;duration=info['frames']/fps
 export(['-framerate',fps,'-i',folder/'%04d.jpg','-i',folder/'audio.wav'],OUT/(folder.name+'.mp4'),['-af','apad','-t',duration])
 print(folder.name,info['frames'],duration,'audio peak',info['peak'])
main=REVIEW/'.Capture/ColorBall-V2-Gameplay';rows=list(csv.DictReader((main/'motion.csv').open()));row=rows[0]
x=540+float(row['x'])*156.8;y=1004-float(row['y'])*156.8;crop=f'crop=480:480:{int(x-240)//2*2}:{int(y-240)//2*2},scale=720:720'
for name,start,duration in [('Activation-closeup',.92,1.55),('Finish-closeup',max(float(r['time']) for r in rows if r['phase']=='Charged')-.12,.7)]:
 export(['-ss',start,'-i',OUT/'ColorBall-V2-Gameplay.mp4'],OUT/(name+'.mp4'),['-t',duration,'-vf',crop])
export(['-ss',.9,'-i',OUT/'reveal-idle-30fps.mp4'],OUT/'Idle-closeup.mp4',['-t',2,'-vf',crop])
export(['-i',OUT/'reveal-idle-30fps.mp4'],OUT/'Reveal-closeup.mp4',['-t',.9,'-vf',crop])
# Consecutive, timestamped rendered frames for the charge/travel/contact/finish review.
for phase in ['Reveal','Charge','Charged','Finish']:
 ids=sorted(set(int(r['frame']) for r in rows if r['phase']==phase and (phase!='Reveal' or float(r['time'])<1)))
 if not ids:continue
 if phase=='Charged':ids=ids[:24]
 sheet=Image.new('RGB',(1200,((len(ids)+5)//6)*220),(17,23,36));draw=ImageDraw.Draw(sheet)
 for j,k in enumerate(ids):
  im=Image.open(main/f'{k:04}.jpg').crop((int(x-240),int(y-240),int(x+240),int(y+240))).resize((200,200))
  xx=j%6*200;yy=j//6*220;sheet.paste(im,(xx,yy));draw.text((xx+3,yy+202),f'{k/30:.3f}s - {phase}',fill='white')
 sheet.save(REVIEW/(phase+'-frames.jpg'))
from PIL import ImageFont
font=ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf',30)
header=Image.new('RGB',(1080,70),(17,23,36));d=ImageDraw.Draw(header)
d.text((22,15),'Previous Color Ball',font=font,fill='white');d.text((562,15),'Color Ball V2 | 1x + audio',font=font,fill='white')
header.save(REVIEW/'Comparison-header.png')
previous=ROOT.parents[3]/'Design/Effects/ColorBall-live-v5/ColorBall-rocket-combo-full.mp4'
export(['-i',previous,'-i',OUT/'ColorBall-V2-Gameplay.mp4','-i',REVIEW/'Comparison-header.png'],OUT/'ColorBall-V1-V2-Comparison.mp4',[
 '-filter_complex','[0:v]scale=540:960,setsar=1,tpad=stop_mode=clone:stop_duration=1[a];[1:v]scale=540:960,setsar=1[b];[a][b]hstack=inputs=2:shortest=1,pad=1080:1030:0:70[p];[p][2:v]overlay=0:0[v]',
 '-map','[v]','-map','1:a','-t',6.6667])
print('Preview exports complete')

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
main=REVIEW/'.Capture/ColorBall-V3-Gameplay';rows=list(csv.DictReader((main/'motion.csv').open()));row=rows[0]
x=540+float(row['x'])*156.8;y=1004-float(row['y'])*156.8;crop=f'crop=480:480:{int(x-240)//2*2}:{int(y-240)//2*2},scale=720:720'
for name,start,duration in [('Activation-closeup',.92,1.55),('Finish-closeup',max(float(r['time']) for r in rows if r['phase']=='Charged')-.12,.7)]:
 export(['-ss',start,'-i',OUT/'ColorBall-V3-Gameplay.mp4'],OUT/(name+'.mp4'),['-t',duration,'-vf',crop])
export(['-ss',.9,'-i',OUT/'reveal-idle-30fps.mp4'],OUT/'Idle-closeup.mp4',['-t',2,'-vf',crop])
export(['-i',OUT/'reveal-idle-30fps.mp4'],OUT/'Reveal-closeup.mp4',['-t',.9,'-vf',crop])
# Consecutive, timestamped rendered frames for the charge/travel/contact/finish review.
for phase in ['Reveal','Charge','Charged','Finish']:
 ids=sorted(set(int(r['frame']) for r in rows if r['phase']==phase and (phase!='Reveal' or float(r['time'])<1)))
 if not ids:continue
 if phase=='Charged':ids=ids[:36]
 sheet=Image.new('RGB',(1200,((len(ids)+5)//6)*220),(17,23,36));draw=ImageDraw.Draw(sheet)
 for j,k in enumerate(ids):
  im=Image.open(main/f'{k:04}.jpg').crop((int(x-240),int(y-240),int(x+240),int(y+240))).resize((200,200))
  xx=j%6*200;yy=j//6*220;sheet.paste(im,(xx,yy));draw.text((xx+3,yy+202),f'{k/30:.3f}s - {phase}',fill='white')
 sheet.save(REVIEW/(phase+'-frames.jpg'))
from PIL import ImageFont
font=ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf',30)
header=Image.new('RGB',(1620,76),(17,23,36));d=ImageDraw.Draw(header)
for i,title in enumerate(['Royal Match reference','Royal Throne V2','Royal Throne V3 | 1x + audio']):d.text((i*540+18,22),title,font=font,fill='white')
header.save(REVIEW/'Comparison-header.png')
reference=Path(r'J:/Unity Projects/Match3-Games_ArtSource/Match-3-JC/00_References/ColorBall/Colorball+finishing up.mp4')
v2=ROOT.parent/'V2/Animation/Previews/ColorBall-V2-Gameplay.mp4'
export(['-ss',5.68,'-i',reference,'-ss',.95,'-i',v2,'-ss',.95,'-i',OUT/'ColorBall-V3-Gameplay.mp4','-i',REVIEW/'Comparison-header.png'],OUT/'RoyalMatch-V2-V3-Comparison.mp4',[
 '-filter_complex','[0:v]scale=540:960:force_original_aspect_ratio=decrease,pad=540:960:(ow-iw)/2:(oh-ih)/2,setsar=1,tpad=stop_mode=clone:stop_duration=3[a];[1:v]scale=540:960,setsar=1,tpad=stop_mode=clone:stop_duration=3[b];[2:v]scale=540:960,setsar=1[c];[a][b][c]hstack=inputs=3:shortest=1,pad=1620:1036:0:76[p];[p][3:v]overlay=0:0[v]',
 '-map','[v]','-map','2:a','-r',30,'-t',6.5])
print('Preview exports complete')


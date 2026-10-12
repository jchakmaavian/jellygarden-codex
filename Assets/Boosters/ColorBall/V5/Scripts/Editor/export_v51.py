from pathlib import Path
import subprocess,json,sys,csv
from PIL import Image,ImageDraw,ImageFont
P=Path(__file__).resolve().parents[6]
V=P/'Assets/Boosters/ColorBall/V5';OUT=V/'Animation/Previews';R=V/'Animation/Review/V5.1'
FF=r'C:/Program Files/Softdeluxe/Free Download Manager/ffmpeg.exe'
REF=Path(r'J:/Unity Projects/Match3-Games_ArtSource/Match-3-JC/00_References/ColorBall/Colorball+finishing up.mp4')
def run(args):subprocess.run([FF,'-v','error','-y',*map(str,args)],check=True)
def export(args,path):
 tmp=path.with_name('.encoding-'+path.name)
 run([*args,'-c:v','h264_mf','-rate_control','quality','-quality','90','-g','1','-b:v','18000k','-pix_fmt','yuv420p','-bsf:v','h264_metadata=colour_primaries=1:transfer_characteristics=1:matrix_coefficients=1','-c:a','aac','-b:a','192k','-movflags','+faststart',tmp]);tmp.replace(path)
full='--full' in sys.argv
name='ColorBall-V5.1-Gameplay' if full else 'Short-proof'
capture=R/'.Capture'/name;info=json.loads((capture/'capture.json').read_text());duration=info['frames']/30
game=OUT/('ColorBall-V5.1-Gameplay.mp4' if full else 'V5.1-Short-proof.mp4')
export(['-framerate',30,'-i',capture/'%04d.jpg','-i',capture/'audio.wav','-af','apad','-t',duration],game)
font=ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf',21)
def chapter(refstart,oldstart,newstart,seconds,label,filename):
 header=Image.new('RGB',(1512,68),(14,20,32));d=ImageDraw.Draw(header)
 for i,title in enumerate(['Royal Match reference','Royal Throne V5','Royal Throne V5.1']):d.text((i*504+16,6),title,font=font,fill='white')
 d.text((16,36),label+' | 1x | equal cell scale | audio: V5.1 game mix',font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',18),fill='#dbd8c1')
 head=R/(filename+'.png');header.save(head)
 export(['-ss',refstart,'-i',REF,'-ss',oldstart,'-i',OUT/'ColorBall-V5-Gameplay.mp4','-ss',newstart,'-i',game,'-i',head,
 '-filter_complex','[0:v]crop=1616:1616:22:542,scale=504:504,setsar=1[a];[1:v]crop=900:900:26:490,scale=504:504,setsar=1[b];[2:v]crop=900:900:26:490,scale=504:504,setsar=1[c];[a][b][c]hstack=inputs=3,pad=1512:572:0:68[p];[p][3:v]overlay=0:0[v]',
 '-map','[v]','-map','2:a','-r',30,'-t',seconds],OUT/(filename+'.mp4'))
if not full:
 chapter(5.90,1.50,1.50,.85,'Charged spin / first discharge / contact','RoyalMatch-V5-V5.1-Short-Comparison')
 images=[]
 for t in [.10,.23,.36,.43,.53,.66]:
  path=R/f'Proof-{t:.2f}.png';run(['-ss',t,'-i',OUT/'RoyalMatch-V5-V5.1-Short-Comparison.mp4','-frames:v',1,path]);images.append(Image.open(path).resize((1260,477)))
 sheet=Image.new('RGB',(1260,477*len(images)))
 for i,im in enumerate(images):sheet.paste(im,(0,i*477))
 sheet.save(R/'Proof-consecutive.jpg')
else:
 def finish(path,label):
  return next(float(r[1]) for r in csv.reader(path.read_text().splitlines()) if r[0]==label and r[2]=='ColorBallSequenceFinished')
 oldend=finish(V/'Animation/Review/events.csv','ColorBall-V5-Gameplay');newend=finish(R/'events.csv',name)
 chapter(5.90,1.50,1.50,1.50,'Phase 1: charged spin / discharges / contacts','V5.1-Comparison-Charge')
 chapter(7.50,oldend,newend,.80,'Phase 2: final discharge / existing Rocket handoff','V5.1-Comparison-Release')
 export(['-i',OUT/'V5.1-Comparison-Charge.mp4','-i',OUT/'V5.1-Comparison-Release.mp4','-filter_complex','[0:v][0:a][1:v][1:a]concat=n=2:v=1:a=1[v][a]','-map','[v]','-map','[a]','-r',30],OUT/'RoyalMatch-V5-V5.1-Comparison.mp4')
 images=[]
 for t in [0,.10,.18,.23,.30,.40,.50,.66]:
  path=R/f'Finish-{t:.2f}.png';run(['-ss',t,'-i',OUT/'V5.1-Comparison-Release.mp4','-frames:v',1,path]);images.append(Image.open(path).resize((1260,477)))
 sheet=Image.new('RGB',(1260,477*len(images)))
 for i,im in enumerate(images):sheet.paste(im,(0,i*477))
 sheet.save(R/'Finish-consecutive.jpg')
print(name,duration,'seconds; captured game audio peak',info['peak'])

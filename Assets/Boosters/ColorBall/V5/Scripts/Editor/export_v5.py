from pathlib import Path
import subprocess,json,sys
from PIL import Image,ImageDraw,ImageFont
P=Path(__file__).resolve().parents[6]
V5=P/'Assets/Boosters/ColorBall/V5';V4=P/'Assets/Boosters/ColorBall/V4';OUT=V5/'Animation/Previews';R=V5/'Animation/Review'
FF=r'C:/Program Files/Softdeluxe/Free Download Manager/ffmpeg.exe'
REF=Path(r'J:/Unity Projects/Match3-Games_ArtSource/Match-3-JC/00_References/ColorBall/Colorball+finishing up.mp4')
def run(args):subprocess.run([FF,'-v','error','-y',*map(str,args)],check=True)
def export(args,path):
 tmp=path.with_name('.encoding-'+path.name)
 run([*args,'-c:v','h264_mf','-b:v','7000k','-pix_fmt','yuv420p','-bsf:v','h264_metadata=colour_primaries=1:transfer_characteristics=1:matrix_coefficients=1','-c:a','aac','-b:a','192k','-movflags','+faststart',tmp]);tmp.replace(path)
name='ColorBall-V5-Gameplay' if '--full' in sys.argv else 'Short-proof'
capture=R/'.Capture'/name;info=json.loads((capture/'capture.json').read_text());duration=info['frames']/30
export(['-framerate',30,'-i',capture/'%04d.jpg','-i',capture/'audio.wav','-af','apad','-t',duration],OUT/(name+'.mp4'))
font=ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf',23)
header=Image.new('RGB',(1512,44),(14,20,32));d=ImageDraw.Draw(header)
for i,label in enumerate(['Royal Match reference','Royal Throne V4 baseline','Royal Throne V5']):d.text((i*504+16,9),label,font=font,fill='white')
header.save(R/'Comparison-header.png')
# Source is centered, with seven measured board cells per panel (72px/cell).
# All clips play at 1x. Phase proof aligns first beam, the full comparison aligns activation.
startref,startgame,length=(5.90,1.50,.85) if '--full' not in sys.argv else (5.78,.9667,5.6)
outname='Reference-V4-V5-Short-Proof.mp4' if '--full' not in sys.argv else 'RoyalMatch-V4-V5-Comparison.mp4'
export(['-ss',startref,'-i',REF,'-ss',startgame,'-i',V4/'Animation/Previews/ColorBall-V4-Gameplay.mp4','-ss',startgame,'-i',OUT/(name+'.mp4'),'-i',R/'Comparison-header.png',
 '-filter_complex','[0:v]crop=1616:1616:22:542,scale=504:504,setsar=1,tpad=stop_mode=clone:stop_duration=4[a];[1:v]crop=900:900:26:490,scale=504:504,setsar=1,tpad=stop_mode=clone:stop_duration=4[b];[2:v]crop=900:900:26:490,scale=504:504,setsar=1,tpad=stop_mode=clone:stop_duration=4[c];[a][b][c]hstack=inputs=3,pad=1512:548:0:44[p];[p][3:v]overlay=0:0[v]',
 '-map','[v]','-map','2:a','-r',30,'-t',length],OUT/outname)
if '--full' not in sys.argv:
 # A tighter equal-cell strip preserves the motion of charged bands for direct review.
 export(['-ss',5.99,'-i',REF,'-ss',1.6,'-i',V4/'Animation/Previews/ColorBall-V4-Gameplay.mp4','-ss',1.6,'-i',OUT/(name+'.mp4'),
 '-filter_complex','[0:v]crop=600:360:530:1170,scale=504:302,setsar=1[a];[1:v]crop=334:200:308:840,scale=504:302,setsar=1[b];[2:v]crop=334:200:308:840,scale=504:302,setsar=1[c];[a][b][c]hstack=inputs=3[v]',
 '-map','[v]','-map','2:a','-r',30,'-t',.65],OUT/'Charged-Bands-Comparison.mp4')
 for t in [.10,.23,.36,.43,.53,.66]:run(['-ss',t,'-i',OUT/outname,'-frames:v',1,R/f'Proof-{t:.2f}.png'])
 images=[Image.open(R/f'Proof-{t:.2f}.png').resize((1008,366)) for t in [.10,.23,.36,.43,.53,.66]]
 sheet=Image.new('RGB',(1008,366*6));
 for k,im in enumerate(images):sheet.paste(im,(0,k*366))
 sheet.save(R/'Proof-consecutive.jpg')
print(outname,duration,'seconds; actual game audio peak',info['peak'])

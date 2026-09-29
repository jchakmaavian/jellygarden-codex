"""Encode actual Unity QA frames; the native cue is a separate review soundtrack."""
from pathlib import Path
import json, subprocess, sys
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Design/ReferenceGameplayImport/Media/Tools/Python'))
from PIL import Image, ImageDraw
OUT=Path(__file__).parent/'Review'
info=json.loads((OUT/'capture.json').read_text())
frames=sorted((OUT/'Frames').glob('*.png'))[:info['frames']]
encoder='C:/Program Files/TimeDoctor2/3.18.89/ffmpeg.exe'
audio=ROOT/'Assets/ReferenceGameplay/Tools/Archer/Resources/ReferenceGameplay/Archer/arrow_use.wav'
duration=len(frames)/info['fps']
args=[encoder,'-hide_banner','-loglevel','error','-y','-f','rawvideo','-pix_fmt','rgb24','-s','540x960','-r',str(info['fps']),'-i','pipe:0','-i',str(audio),'-filter_complex','[1:a]adelay=160|160,apad[a]','-map','0:v','-map','[a]','-c:v','libx264','-preset','fast','-crf','19','-threads','4','-pix_fmt','yuv420p','-c:a','aac','-t',str(duration),'-movflags','+faststart',str(OUT/'Archer-Gameplay.mp4')]
p=subprocess.Popen(args,stdin=subprocess.PIPE)
for file in frames:p.stdin.write(Image.open(file).convert('RGB').resize((540,960),Image.Resampling.LANCZOS).tobytes())
p.stdin.close()
assert p.wait()==0
indices=[0,12,21,27,33,39,51,65]
sheet=Image.new('RGB',(270*4,500*2),(15,25,44));draw=ImageDraw.Draw(sheet)
for n,index in enumerate(indices):
    index=min(index,len(frames)-1)
    im=Image.open(frames[index]).convert('RGB').resize((270,480),Image.Resampling.LANCZOS)
    x=n%4*270;y=n//4*500;sheet.paste(im,(x,y));draw.text((x+8,y+482),f'{index/info["fps"]:.2f}s',fill='white')
sheet.save(OUT/'Archer-Gameplay-contact.jpg',quality=93)
print(f'Encoded {len(frames)} Unity frames; original arrow cue mixed at launch for review.')

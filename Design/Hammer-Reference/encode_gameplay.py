from pathlib import Path
import sys,subprocess,wave
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1];OUT=HERE/'Review'
sys.path.insert(0,str(ROOT/'Design/ReferenceGameplayImport/Media/Tools/Python'))
import numpy as np
from PIL import Image
ffmpeg='C:/Program Files/Softdeluxe/Free Download Manager/ffmpeg.exe'
encoder='C:/Program Files/TimeDoctor2/3.18.89/ffmpeg.exe'
rate=44100;mix=np.zeros(round(2.5*rate))
for name,time in [('appear',0),('hit',.4),('disappear',.9)]:
    path=ROOT/'Assets/ReferenceGameplay/Tools/Hammer/Resources/ReferenceGameplay/Hammer'/f'hammer_use_{name}.wav'
    raw=subprocess.check_output([ffmpeg,'-hide_banner','-loglevel','error','-i',str(path),'-f','f32le','-ar',str(rate),'-ac','1','pipe:1'])
    sound=np.frombuffer(raw,dtype='<f4');start=round(time*rate);mix[start:start+len(sound)]+=sound*.65
wav=OUT/'review-cue-mix.wav'
with wave.open(str(wav),'wb') as w:
    w.setnchannels(1);w.setsampwidth(2);w.setframerate(rate);w.writeframes((np.clip(mix,-1,1)*32767).astype('<i2').tobytes())
# This installed x264 build lacks PNG decoding; feed Pillow-decoded RGB frames.
args=[encoder,'-hide_banner','-loglevel','error','-y','-f','rawvideo','-pix_fmt','rgb24','-s','540x960','-r','60','-i','pipe:0','-i',str(wav),'-c:v','libx264','-preset','fast','-crf','19','-threads','4','-pix_fmt','yuv420p','-c:a','aac','-t','2.5','-movflags','+faststart',str(OUT/'Hammer-Gameplay.mp4')]
p=subprocess.Popen(args,stdin=subprocess.PIPE)
for n in range(150):
    im=Image.open(OUT/'GameplayFrames'/f'{n:04}.png').convert('RGB').resize((540,960),Image.Resampling.LANCZOS)
    p.stdin.write(im.tobytes())
p.stdin.close();assert p.wait()==0
print('Encoded 150 actual Unity frames with a separately mixed review soundtrack')

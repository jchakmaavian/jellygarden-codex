from pathlib import Path
import sys,json,subprocess
HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(HERE.parents[1]/'Design/ReferenceGameplayImport/Media/Tools/Python'))
import numpy as np
import wave
sr=44100
def read(path):
    data=subprocess.check_output(['C:/Program Files/Softdeluxe/Free Download Manager/ffmpeg.exe','-hide_banner','-loglevel','error','-i',str(path),'-f','f32le','-ar',str(sr),'-ac','1','pipe:1'])
    return np.frombuffer(data,dtype='<f4').astype(float)
ref=read(HERE/'ReferenceVideo/audio.wav')
result=[]
for path in (HERE.parents[1]/'Assets/ReferenceGameplay/Tools/Hammer/Resources/ReferenceGameplay/Hammer').glob('*.wav'):
    s=read(path)
    # Normalized matched filter: compare original cue against the reference mix.
    s-=s.mean();size=1<<(len(ref)+len(s)-1).bit_length();conv=np.fft.irfft(np.fft.rfft(ref,size)*np.fft.rfft(s[::-1],size),size)
    corr=conv[len(s)-1:len(ref)];e=np.r_[0,np.cumsum(ref*ref)];energy=e[len(s):]-e[:-len(s)]
    score=corr/np.sqrt(np.maximum(energy,1)*np.dot(s,s));start=round(8*sr);end=round(11*sr)
    ix=start+int(np.argmax(score[start:end]));result.append(dict(cue=path.stem,time=ix/sr,correlation=float(score[ix]),duration=len(s)/sr))
print(json.dumps(result,indent=2));(HERE/'ReferenceVideo/audio-correlation.json').write_text(json.dumps(result,indent=2))
v=json.loads((HERE/'NativeHammerParticles.json').read_text())
for p in v['prefabs']:
    for n in p['nodes']:
        for c in n['components']:
            t=json.loads(c['json'])
            if c['type']=='ParticleSystemRenderer':print(p['name'],n['name'],'renderMode',t['m_RenderMode'],'order',t['m_SortingOrder'])

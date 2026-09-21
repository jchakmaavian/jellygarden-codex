"""Original oscillator/noise synthesis. No recordings or reference audio are used."""
from pathlib import Path
import numpy as np
import wave,json
out=Path(__file__).resolve().parents[2]/'Audio/Resources/PropellerV4';out.mkdir(parents=True,exist_ok=True)
sr=48000;rng=np.random.default_rng(49721);report={}
for name,duration in [('Create',.22),('RotorStart',.40),('Takeoff',.24),('Flight',.5),('NeighborHit',.13),('Impact',.19)]:
 t=np.arange(round(duration*sr))/sr;u=t/duration
 noise=np.convolve(rng.normal(0,1,len(t)),np.ones(13)/13,mode='same')
 if name=='Create':
  x=.48*np.sin(2*np.pi*(820*t+1200*t*t))*np.exp(-t*22)+.24*np.sin(2*np.pi*1640*t)*np.exp(-t*16)+noise*.1*np.exp(-t*40)
 elif name=='RotorStart':
  phase=2*np.pi*(85*t+230*t*t);flutter=.55+.45*np.sin(2*np.pi*(12*t+48*t*t))**2
  x=(.3*np.sin(phase)+.13*np.sin(2*phase)+noise*.45)*flutter*np.sin(np.pi*u)**.5
 elif name=='Takeoff':
  x=noise*.8*np.sin(np.pi*u)**1.6+.32*np.sin(2*np.pi*(260*t-260*t*t))*np.exp(-t*35)
 elif name=='Flight':
  # Integer cycles give a genuinely seamless loop, with a quiet periodic air layer.
  x=(.17*np.sin(2*np.pi*180*t)+.09*np.sin(2*np.pi*360*t))*(.72+.28*np.cos(2*np.pi*42*t))
  for freq in [732,914,1218,1742,2106]:x+=.014*np.sin(2*np.pi*freq*t+rng.uniform(0,6.28))
 elif name=='NeighborHit':
  x=.43*np.sin(2*np.pi*(420*t-700*t*t))*np.exp(-t*35)+noise*.7*np.exp(-t*26)
 else:
  x=.4*np.sin(2*np.pi*(330*t-420*t*t))*np.exp(-t*34)+.26*np.sin(2*np.pi*1880*t)*np.exp(-t*24)+noise*.55*np.exp(-t*35)
 if name!='Flight':x*=np.minimum(1,t/.004)*np.minimum(1,(duration-t)/.016)
 x=x/max(.01,np.max(np.abs(x)))*(.48 if name=='Flight' else .72)
 pcm=np.round(x*32767).astype('<i2')
 with wave.open(str(out/('Propeller_'+name+'.wav')),'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(sr);w.writeframes(pcm.tobytes())
 report[name]={'seconds':duration,'peak':float(np.max(np.abs(x))),'rms':float(np.sqrt(np.mean(x*x)))}
(out.parent.parent/'original-sfx.json').write_text(json.dumps(report,indent=2))
print('Six original Propeller clips synthesized.')

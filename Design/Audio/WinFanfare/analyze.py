import wave,sys,numpy as np
w=wave.open(sys.argv[1]);x=np.frombuffer(w.readframes(w.getnframes()),'<i2').reshape(-1,2).astype(float)/32768
mono=x.mean(1);hop=441
print('dur',len(mono)/44100,'peak',np.abs(x).max(),'dc',mono.mean())
for a,b in ((0,.36),(.60,.95),(1.55,2.0)):
  s=''
  for i in range(int(a*44100),int(b*44100),hop):
    r=np.sqrt((mono[i:i+hop]**2).mean());s+=' .:-=+*#%@'[min(9,max(0,int((20*np.log10(r+1e-9)+40)/3.5)))]
  print(f'{a:.2f}-{b:.2f} 10ms:',s)
for a in (.32,.9,1.2,2.1,2.4):
  seg=mono[int(a*44100):int(a*44100)+4096]*np.hanning(4096);S=np.abs(np.fft.rfft(seg));f=np.fft.rfftfreq(4096,1/44100)
  print(f'centroid@{a}: {(S*f).sum()/S.sum():.0f} Hz')

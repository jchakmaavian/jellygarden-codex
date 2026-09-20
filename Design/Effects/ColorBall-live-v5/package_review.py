from pathlib import Path
import hashlib, json, subprocess, zipfile
root=Path(__file__).resolve().parents[3]
review=Path(__file__).resolve().parent
files=['Assets/JellyGarden/Scripts/Juice/ColorBallTargetVfx.cs',
       'Assets/JellyGarden/Scripts/Editor/RoyalColorBallQaChecks.cs',
       'Assets/JellyGarden/Resources/RoyalColorBall/ColorBallBeam.shader',
       'Assets/JellyGarden/Resources/RoyalColorBall/ColorBallOutline.shader']
with zipfile.ZipFile(review/'ColorBall-Link-Polish.zip','w',zipfile.ZIP_DEFLATED) as z:
    for f in files:
        z.write(root/f,f);z.write(root/(f+'.meta'),f+'.meta')
hashes=json.loads((root/'Design/Effects/ColorBall-live-v1/source-hashes.json').read_text())
assert all(hashlib.sha256((root/p).read_bytes()).hexdigest()==h for p,h in hashes.items())
protected=json.loads((root/'Design/Effects/ColorBall-live-v4/protected-hashes.json').read_text(encoding='utf-8-sig'))
assert all(hashlib.sha256(Path(x['Path']).read_bytes()).hexdigest().upper()==x['Hash'] for x in protected)
print('All approved frames and protected Rocket/TNT files unchanged.')
ffmpeg='C:/Program Files/Softdeluxe/Free Download Manager/ffmpeg.exe'
for label in ('effect-half','rocket-combo-full'):
    folder=root/f'Temp/ColorBall-live-v5-{label}'
    stamps=list(map(float,(folder/'timestamps.txt').read_text().splitlines()))
    entries=[]
    for i,t in enumerate(stamps):
        dt=stamps[i+1]-t if i+1<len(stamps) else 1/30
        entries.extend([f"file '{(folder/f'{i:04d}.jpg').as_posix()}'",f'duration {dt:.5f}'])
    listing=folder/'frames.txt';listing.write_text('\n'.join(entries))
    subprocess.run([ffmpeg,'-hide_banner','-loglevel','error','-y','-f','concat','-safe','0','-i',str(listing),
        '-r','30','-c:v','h264_mf','-b:v','3500k','-pix_fmt','yuv420p','-movflags','+faststart',str(review/f'{label}.mp4')],check=True)
    print(label,len(stamps),'frames',round(stamps[-1]-stamps[0],2),'seconds')

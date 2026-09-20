from pathlib import Path
import difflib,hashlib,json,zipfile,subprocess
root=Path(__file__).resolve().parents[3]
review=Path(__file__).resolve().parent
files=['Assets/JellyGarden/Scripts/RoyalQaService.cs','Assets/JellyGarden/Scripts/Juice/RoyalColorBallSequence.cs','Assets/JellyGarden/Scripts/Juice/RoyalPowerupDirector.cs','Assets/JellyGarden/Scripts/Editor/RoyalColorBallQaChecks.cs']
patch=[]
with zipfile.ZipFile(review/'ColorBall-QA-Flow-Fix.zip','w',zipfile.ZIP_DEFLATED) as z:
    for name in files:
        src=root/name;before=review/'baseline'/src.name
        patch.extend(difflib.unified_diff(before.read_text().splitlines(True) if before.exists() else [],src.read_text().splitlines(True),fromfile='a/'+name,tofile='b/'+name))
        z.write(src,name);z.write(Path(str(src)+'.meta'),name+'.meta')
(review/'incremental.patch').write_text(''.join(patch))
hashes=json.loads((root/'Design/Effects/ColorBall-live-v1/source-hashes.json').read_text())
assert all(hashlib.sha256((root/p).read_bytes()).hexdigest()==h for p,h in hashes.items())
print('Approved 378 source frames remain unchanged.')
ffmpeg='C:/Program Files/Softdeluxe/Free Download Manager/ffmpeg.exe'
for label in ('spawn-activate-full','rocket-combo-full'):
    folder=root/f'Temp/ColorBall-live-v2-{label}'
    if not (folder/'timestamps.txt').exists():continue
    stamps=list(map(float,(folder/'timestamps.txt').read_text().splitlines()))
    entries=[]
    for i,t in enumerate(stamps):
        frame=(folder/f'{i:04d}.jpg').as_posix()
        duration=stamps[i+1]-t if i+1<len(stamps) else 1/15
        entries.extend([f"file '{frame}'",f'duration {duration:.5f}'])
    listing=folder/'frames.txt';listing.write_text('\n'.join(entries))
    subprocess.run([ffmpeg,'-hide_banner','-loglevel','error','-y','-f','concat','-safe','0','-i',str(listing),'-r','30','-c:v','h264_mf','-b:v','5000k','-pix_fmt','yuv420p','-movflags','+faststart',str(review/f'{label}.mp4')],check=True)
    print(label,len(stamps),'captured frames,',round(stamps[-1]-stamps[0],2),'seconds')

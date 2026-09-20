"""Package existing C# rendered animation masters, validate exports, make review media."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import numpy as np
import json, zipfile, subprocess, shutil

BASE=Path(__file__).resolve().parents[2]
REVIEW=BASE/'ColorBall-Reveal-v1/review'
MASTERS=REVIEW/'masters'
FFMPEG='C:/Program Files/Softdeluxe/Free Download Manager/ffmpeg.exe'
SOURCE=Path(__file__).resolve().parent
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',14)
counts={'Reveal':60,'Idle':120,'Boosting':108}
frames={}
report={}
def diff(a,b):
    a=np.asarray(a,dtype=np.float32)/255;b=np.asarray(b,dtype=np.float32)/255
    a[:,:,:3]*=a[:,:,3:];b[:,:,:3]*=b[:,:,3:]
    return float(np.abs(a-b).mean()*255)
def backdrop(im,color,grid=False):
    bg=Image.new('RGBA',(512,512),color)
    if grid:
        dr=ImageDraw.Draw(bg)
        for x in range(88,512,112):dr.line((x,0,x,512),fill='#7e89b8',width=1)
        for y in range(88,512,112):dr.line((0,y,512,y),fill='#7e89b8',width=1)
    return Image.alpha_composite(bg,im).convert('RGB')
def video(images,path):
    images=iter(images);first=next(images)
    cmd=[FFMPEG,'-hide_banner','-loglevel','error','-y','-f','rawvideo','-pix_fmt','rgb24','-s',f'{first.width}x{first.height}','-r','60','-i','-','-an','-c:v','h264_mf','-b:v','7000k','-pix_fmt','yuv420p','-movflags','+faststart',str(path)]
    p=subprocess.Popen(cmd,stdin=subprocess.PIPE)
    p.stdin.write(first.tobytes())
    for im in images:p.stdin.write(im.tobytes())
    p.stdin.close()
    if p.wait():raise RuntimeError('Video encoding failed')
def panel(im,label):
    out=Image.new('RGB',(1024,512))
    out.paste(backdrop(im,'#19263b'),(0,0));out.paste(backdrop(im,'#8b95c8',True),(512,0))
    d=ImageDraw.Draw(out);d.text((14,12),label,fill='white',font=font)
    d.text((528,12),'112 px = one cell | fixed center pivot',fill='#142440',font=font)
    return out

for name,count in counts.items():
    dest=BASE/f'ColorBall-{name}-v1';target=dest/'frames';target.mkdir(exist_ok=True)
    src=sorted((MASTERS/name).glob('*.png'))
    assert len(src)==count,(name,len(src))
    ims=[Image.open(p).convert('RGBA') for p in src];frames[name]=ims
    # Remove only superseded numbered frames in this exact authored group.
    for stale in target.glob(f'ColorBall_{name}_*.webp'):
        if int(stale.stem.rsplit('_',1)[1])>count:stale.unlink()
    border=dirty=0
    for i,im in enumerate(ims):
        assert im.size==(512,512)
        px=np.asarray(im);a=px[:,:,3]
        border=max(border,int(max(a[:2].max(),a[-2:].max(),a[:,:2].max(),a[:,-2:].max())))
        dirty+=int(np.count_nonzero(px[:,:,:3][a==0]))
        path=target/f'ColorBall_{name}_{i+1:03}.webp'
        im.save(path,format='WEBP',lossless=True,exact=True,method=5)
        assert np.array_equal(px,np.asarray(Image.open(path).convert('RGBA'))),str(path)
    assert border==0,(name,'clipped edge',border)
    assert dirty==0,(name,'dirty alpha',dirty)
    durations=[round((i+1)*1000/60)-round(i*1000/60) for i in range(count)]
    ims[0].save(dest/'Preview.webp',save_all=True,append_images=ims[1:],duration=durations,loop=0,lossless=True,exact=True,method=4)
    sheet=Image.new('RGB',(8*160,((count+7)//8)*180),'#283649')
    d=ImageDraw.Draw(sheet)
    for i,im in enumerate(ims):
        x=i%8*160;y=i//8*180
        thumb=backdrop(im,'#8b95c8',True).resize((160,160),Image.Resampling.LANCZOS)
        sheet.paste(thumb,(x,y));d.text((x+5,y+161),f'{i+1:03}',font=font,fill='white')
    sheet.save(dest/'ContactSheet.png')
    if name=='Boosting':order=list(range(18))+list(range(18,78))*3+list(range(78,108))
    else:order=list(range(count))+([count-1]*30 if name=='Reveal' else [])
    video((panel(ims[i],f'{name} | 60 FPS | frame {i+1:03}') for i in order),dest/'Preview.mp4')
    with zipfile.ZipFile(dest/'60FPS_WEBP.zip','w',zipfile.ZIP_DEFLATED) as z:
        for path in sorted(target.glob('*.webp')):z.write(path,'frames/'+path.name)
    report[name]={'frames':count,'size':[512,512],'source_fps':60,'max_border_alpha':border,'hidden_rgb':dirty,'webp_roundtrip':'exact'}

report['handoffs']={'reveal_to_idle':diff(frames['Reveal'][-1],frames['Idle'][0]),'idle_to_boost':diff(frames['Idle'][0],frames['Boosting'][0]),'idle_wrap':diff(frames['Idle'][-1],frames['Idle'][0]),'finish_entry':diff(frames['Boosting'][18],frames['Boosting'][78])}
assert all(v==0 for v in report['handoffs'].values())
loop=frames['Boosting'][18:78]
steps=[diff(a,b) for a,b in zip(loop,loop[1:])]
seam=diff(loop[-1],loop[0])
report['charged_loop']={'first_frame':19,'last_frame':78,'seam_difference':seam,'median_step':float(np.median(steps)),'largest_step':max(steps)}
assert seam<=max(steps)*1.1
assert np.asarray(frames['Boosting'][-1])[:,:,3].max()==0
report['finish_transparent']=True
report['idle_alpha_bbox']=frames['Idle'][0].getchannel('A').point(lambda x:255 if x>127 else 0).getbbox()
chain=frames['Reveal']+frames['Idle'][:60]+frames['Boosting'][:18]+loop*3+frames['Boosting'][78:]
video((panel(im,'Reveal -> Idle -> Spin-up -> 3 charged loops -> Finish') for im in chain),BASE/'ColorBall-Boosting-v1/Chain_Reveal-Idle-Boosting.mp4')
(REVIEW/'quality-checks.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))

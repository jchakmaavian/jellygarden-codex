"""Review composition only: independent beam/target sprites, no gameplay code or baked target positions."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import math,subprocess
BASE=Path(__file__).resolve().parents[2]
ART=Path('J:/Unity Projects/Match3-Games_ArtSource/Match-3-JC/Royal_Throne_Gameplay_Assets')
VFX=BASE/'ColorBall-Boosting-v1/VFX'
def sequence(p):return [Image.open(f).convert('RGBA') for f in sorted(p.glob('*.webp'))]
boost=sequence(BASE/'ColorBall-Boosting-v1/frames')
beam=sequence(VFX/'ColorBall_Beam/SequenceBody/frames')
contact=sequence(VFX/'ColorBall_Beam/Contact/frames')
glow=sequence(VFX/'ColorBall_TargetGlow/On/frames')
hold=sequence(VFX/'ColorBall_TargetGlow/Loop/frames')
rocket=Image.open(ART/'02_powerups/rocket_horizontal.webp').convert('RGBA').resize((96,96),Image.Resampling.LANCZOS)
gempath=next(p for p in (ART/'01_match_pieces').glob('*.webp') if 'crown' in p.name.lower())
gem=Image.open(gempath).convert('RGBA').resize((96,96),Image.Resampling.LANCZOS)
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',22)
out=BASE/'ColorBall-Boosting-v1/Separate-Beams-Handoff-Preview.mp4'
p=subprocess.Popen(['C:/Program Files/Softdeluxe/Free Download Manager/ffmpeg.exe','-hide_banner','-loglevel','error','-y','-f','rawvideo','-pix_fmt','rgb24','-s','1024x768','-r','60','-i','-','-an','-c:v','h264_mf','-b:v','7000k','-pix_fmt','yuv420p','-movflags','+faststart',str(out)],stdin=subprocess.PIPE)
targets=[(170,190),(850,190),(170,585),(850,585)]
for f in range(228):
    img=Image.new('RGBA',(1024,768),'#8895c7');d=ImageDraw.Draw(img)
    for x in range(8,1024,112):d.line((x,60,x,720),fill='#7584b9')
    for y in range(104,720,112):d.line((8,y,1016,y),fill='#7584b9')
    def paste(im,x,y):img.alpha_composite(im,(round(x-im.width/2),round(y-im.height/2)))
    for k,(x,y) in enumerate(targets):
        age=f-(30+k*24);arrived=age>=10
        paste(rocket if arrived else gem,x,y)
        if 0<=age<34:
            dx=x-512;dy=y-384;dist=math.hypot(dx,dy);ux=dx/dist;uy=dy/dist;sx=dist/544;sy=112/192
            transform=(ux/sx,uy/sx,16-(ux*512+uy*384)/sx,-uy/sy,ux/sy,80-(-uy*512+ux*384)/sy)
            img=Image.alpha_composite(img,beam[age].transform(img.size,Image.Transform.AFFINE,transform,Image.Resampling.BILINEAR))
            paste(contact[age].resize((94,94),Image.Resampling.LANCZOS),x,y)
        if arrived and f<168:
            a=age-10;g=glow[a] if a<10 else hold[(a-10)%len(hold)]
            paste(g.resize((149,149),Image.Resampling.LANCZOS),x,y)
    b=f if f<18 else 18+(f-18)%60 if f<138 else 78+(f-138) if f<168 else None
    if b is not None:paste(boost[b],512,384)
    d=ImageDraw.Draw(img)
    d.rectangle((0,0,1024,60),fill='#182944')
    title='Color Ball: separate beams -> target flash -> Rocket conversion' if f<138 else 'Color Ball finish / cleanup' if f<168 else 'HANDOFF: existing Rocket system activates the converted Rockets'
    d.text((20,18),title,font=font,fill='white')
    d.text((20,732),'Review composition only | target positions are NOT baked into the ball animation',font=font,fill='#182944')
    p.stdin.write(img.convert('RGB').tobytes())
p.stdin.close()
assert p.wait()==0
print(out)

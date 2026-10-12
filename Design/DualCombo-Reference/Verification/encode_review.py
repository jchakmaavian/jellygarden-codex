from pathlib import Path
import subprocess,json
from PIL import Image,ImageDraw,ImageFont
root=Path(__file__).resolve().parents[3]
out=Path(__file__).parent
framesRoot=root/'Design/DualCombo-Reference/FullGameFixture/Design/DualCombo-Reference/Verification/Frames'
names=['RocketH-RocketH','RocketH-RocketV','RocketV-RocketV','TNT-TNT','Propeller-Propeller','Colorball-Colorball','RocketH-TNT','RocketV-TNT','Propeller-RocketH','Propeller-RocketV','Propeller-TNT','Colorball-RocketH','Colorball-RocketV','Colorball-TNT','Colorball-Propeller']
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',16)
encoder='C:/Program Files/TimeDoctor2/3.18.89/ffmpeg.exe'
args=[encoder,'-hide_banner','-loglevel','error','-y','-f','rawvideo','-pix_fmt','rgb24','-s','360x680','-r','30','-i','pipe:0','-c:v','libx264','-preset','fast','-crf','20','-threads','4','-pix_fmt','yuv420p','-movflags','+faststart',str(out/'All-Dual-Combos-Unity-Review.mp4')]
p=subprocess.Popen(args,stdin=subprocess.PIPE)
counts={}; sheet=Image.new('RGB',(5*216,3*412),'#101c31')
for idx,name in enumerate(names):
 frames=sorted((framesRoot/name).glob('*.png'));counts[name]=len(frames)
 assert frames,name
 for path in frames:
  image=Image.new('RGB',(360,680),'#101c31');image.paste(Image.open(path).convert('RGB'),(0,40));draw=ImageDraw.Draw(image);draw.text((9,9),name+' | 60 FPS simulation',font=font,fill='white');p.stdin.write(image.tobytes())
 for _ in range(15):p.stdin.write(image.tobytes())
 # Representative early action frame, full sequence available in the video.
 representative=frames[min(len(frames)-1,max(3,int(len(frames)*.25)))]
 tile=Image.open(representative).convert('RGB').resize((216,384));x=(idx%5)*216;y=(idx//5)*412;sheet.paste(tile,(x,y+28));ImageDraw.Draw(sheet).text((x+3,y+5),name,font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',13),fill='white')
p.stdin.close();assert p.wait()==0
sheet.save(out/'All-Dual-Combos-ContactSheet.jpg',quality=94)
(out/'capture-inventory.json').write_text(json.dumps({'simulationFPS':60,'captureFPS':30,'overlayUI':False,'audio':False,'frames':counts},indent=2))
print(json.dumps(counts))

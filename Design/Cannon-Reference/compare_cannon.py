"""Reference/Unity comparison at normal speed; source launch is about 7.30 s."""
from pathlib import Path
import json, subprocess, sys

root = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(root / 'Design/ReferenceGameplayImport/Media/Tools/Python'))
from PIL import Image, ImageDraw, ImageFont

out = Path(__file__).parent / 'Review'
title = Image.new('RGB', (720, 48), (14, 23, 42))
draw = ImageDraw.Draw(title)
font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 18)
draw.text((20, 14), 'Royal Match reference', font=font, fill='white')
draw.text((380, 14), 'Updated Unity cannon', font=font, fill='white')
title.save(out / 'comparison-title.png')
capture = json.loads((out / 'capture.json').read_text())
duration = capture['frames'] / capture['fps']
subprocess.run([
    'C:/Program Files/Softdeluxe/Free Download Manager/ffmpeg.exe',
    '-hide_banner', '-loglevel', 'error', '-y', '-ss', '6.30', '-i',
    'G:/Unity Projects/Match3-Games_ArtSource/Match-3-JC/00_References/animation-VFX/Cannon.mp4',
    '-i', str(out / 'Cannon-Gameplay-v3.mp4'),
    '-loop', '1', '-framerate', '60', '-i', str(out / 'comparison-title.png'),
    '-filter_complex',
    '[0:v]setpts=PTS-STARTPTS,fps=60,scale=360:800,setsar=1[ref];'
    '[1:v]setpts=PTS-STARTPTS,scale=360:640,pad=360:800:0:0:color=0x0e172a,setsar=1[ours];'
    '[ref][ours]hstack=inputs=2[body];[2:v][body]vstack=inputs=2[v]',
    '-map', '[v]', '-an', '-t', str(duration), '-c:v', 'h264_mf',
    '-b:v', '7000k', '-pix_fmt', 'yuv420p', '-movflags', '+faststart',
    str(out / 'Cannon-Reference-vs-v3.mp4')
], check=True)
print(out / 'Cannon-Reference-vs-v3.mp4')

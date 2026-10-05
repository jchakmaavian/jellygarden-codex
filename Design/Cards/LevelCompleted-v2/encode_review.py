from pathlib import Path
import subprocess

root = Path(__file__).resolve().parent
decoder = r'C:/Program Files/Softdeluxe/Free Download Manager/ffmpeg.exe'
encoder = r'C:/Program Files/TimeDoctor2/3.18.89/ffmpeg.exe'
output = root / 'Royal-Throne-Level-Complete-v2.mp4'
decode = subprocess.Popen([decoder, '-hide_banner', '-loglevel', 'error', '-safe', '0',
    '-i', str(root / 'Verification/capture.ffconcat'), '-vf', 'fps=30,scale=720:1280',
    '-f', 'rawvideo', '-pix_fmt', 'rgb24', 'pipe:1'], stdout=subprocess.PIPE, cwd=root / 'Verification')
encode = subprocess.run([encoder, '-hide_banner', '-loglevel', 'error', '-y', '-f', 'rawvideo',
    '-pixel_format', 'rgb24', '-video_size', '720x1280', '-framerate', '30', '-i', 'pipe:0',
    '-an', '-c:v', 'libx264', '-preset', 'fast', '-crf', '19', '-pix_fmt', 'yuv420p',
    '-movflags', '+faststart', str(output)], stdin=decode.stdout)
decode.stdout.close()
decode.wait()
if decode.returncode or encode.returncode:
    raise SystemExit('Video encoding failed')
subprocess.run([decoder, '-hide_banner', '-loglevel', 'error', '-i', str(output), '-f', 'null', '-'], check=True)
print(output)

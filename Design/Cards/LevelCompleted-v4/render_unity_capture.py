"""Encode the actual Unity capture using its measured frame timestamps."""
from pathlib import Path
import subprocess
from render_preview import DECODER, ENCODER

root = Path(__file__).resolve().parent
source = root / 'Verification' / 'capture.ffconcat'
output = root / 'Royal-Throne-Level-Complete-v4-Unity-Preview.mp4'
if not source.is_file():
    raise SystemExit('Run verify_win_card.cs in Unity first.')
decode = [str(DECODER), '-v', 'error', '-safe', '0', '-f', 'concat', '-i', str(source),
          '-vf', 'scale=720:1280,fps=30', '-an', '-f', 'rawvideo', '-pix_fmt', 'rgb24', '-']
encode = [str(ENCODER), '-y', '-v', 'error', '-f', 'rawvideo', '-pix_fmt', 'rgb24',
          '-video_size', '720x1280', '-framerate', '30', '-i', '-', '-an', '-c:v', 'libx264',
          '-preset', 'fast', '-crf', '18', '-pix_fmt', 'yuv420p', '-movflags', '+faststart', str(output)]
decoder = subprocess.Popen(decode, stdout=subprocess.PIPE, cwd=source.parent)
encoder = subprocess.Popen(encode, stdin=decoder.stdout)
decoder.stdout.close()
encode_status = encoder.wait()
decode_status = decoder.wait()
if encode_status or decode_status:
    raise SystemExit(f'FFmpeg failed: decoder={decode_status}, encoder={encode_status}')
print(output)

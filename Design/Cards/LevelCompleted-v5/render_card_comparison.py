"""Compare the supplied reference and verified Unity card at real-time speed, aligned to star onset."""
from pathlib import Path
import json
import subprocess
from render_unity_capture import DECODER, ENCODER, ROOT, REFERENCE, require_finished, ps_quote

source, unity_movie, passes = require_finished('card')
if not unity_movie.is_file():
    raise SystemExit('Encode the actual Unity card capture first with render_unity_capture.py --target card --sheet.')
mapping_path = source.parent / 'Reference-vs-Unity-Star-Entrance.json'
mapping = json.loads(mapping_path.read_text(encoding='utf-8'))
reference_start = float(mapping['reference_onset_seconds'])
unity_start = float(mapping['comparisons'][0]['unity_wall_seconds'])
duration = 3.5
header = source.parent / 'comparison-header.png'
output = ROOT / 'Royal-Throne-v5-Reference-vs-Unity-Card.mp4'

commands = [
    'Add-Type -AssemblyName System.Drawing',
    '$comparisonBitmap = New-Object System.Drawing.Bitmap(720,40)',
    '$comparisonGraphics = [System.Drawing.Graphics]::FromImage($comparisonBitmap)',
    '$comparisonGraphics.Clear([System.Drawing.Color]::FromArgb(13,22,38))',
    "$comparisonFont = New-Object System.Drawing.Font('Arial',16,[System.Drawing.FontStyle]::Bold)",
    "$comparisonGraphics.DrawString('Reference',$comparisonFont,[System.Drawing.Brushes]::White,18,8)",
    "$comparisonGraphics.DrawString('Royal Throne - Unity',$comparisonFont,[System.Drawing.Brushes]::White,378,8)",
    f'$comparisonBitmap.Save({ps_quote(header)},[System.Drawing.Imaging.ImageFormat]::Png)',
    '$comparisonGraphics.Dispose()', '$comparisonBitmap.Dispose()', '$comparisonFont.Dispose()',
]
subprocess.run(['powershell.exe', '-NoProfile', '-NonInteractive', '-Command', '\n'.join(commands)], check=True)

# The first two setpts calls only reset the selected clip origins. Their playback rates are unchanged.
filters = ('[0:v]scale=360:640,setsar=1,fps=30,setpts=PTS-STARTPTS[reference];'
           '[1:v]scale=360:640,setsar=1,fps=30,setpts=PTS-STARTPTS[unity];'
           '[reference][unity]hstack=inputs=2[body];'
           '[2:v]setsar=1,fps=30,setpts=PTS-STARTPTS[header];'
           '[header][body]vstack=inputs=2[out]')
decode_args = [str(DECODER), '-hide_banner', '-v', 'error',
               '-ss', str(reference_start), '-i', str(REFERENCE),
               '-ss', str(unity_start), '-i', str(unity_movie),
               '-loop', '1', '-i', str(header), '-filter_complex', filters,
               '-map', '[out]', '-t', str(duration), '-an', '-f', 'rawvideo', '-pix_fmt', 'rgb24', '-']
encode_args = [str(ENCODER), '-y', '-hide_banner', '-v', 'error', '-f', 'rawvideo',
               '-pix_fmt', 'rgb24', '-video_size', '720x680', '-framerate', '30', '-i', '-',
               '-an', '-c:v', 'libx264', '-preset', 'fast', '-crf', '18', '-pix_fmt', 'yuv420p',
               '-movflags', '+faststart', str(output)]
decoder = subprocess.Popen(decode_args, stdout=subprocess.PIPE)
try:
    encoder = subprocess.Popen(encode_args, stdin=decoder.stdout)
    decoder.stdout.close()
    encode_status = encoder.wait()
    decode_status = decoder.wait()
finally:
    if decoder.poll() is None:
        decoder.kill()
        decoder.wait()
if encode_status or decode_status:
    raise SystemExit(f'FFmpeg comparison failed: decoder={decode_status}, encoder={encode_status}')
subprocess.run([str(DECODER), '-hide_banner', '-v', 'error', '-i', str(output), '-f', 'null', '-'], check=True)
record = {
    'reference_source': str(REFERENCE), 'reference_start_seconds': reference_start,
    'unity_source': str(unity_movie), 'unity_first_hero_frame': mapping['comparisons'][0]['unity_frame'],
    'unity_start_wall_seconds': unity_start, 'duration_seconds': duration, 'output_fps': 30,
    'size': [720, 680], 'audio': False, 'unity_card_passing_checks': passes,
    'timing': 'Both sides play at real-time speed. Only start offsets are aligned; no time stretching or animation-clock retiming.',
    'scope': 'Motion comparison; Royal Throne retains its own artwork, frame and requested larger settled star.',
    'decode_validation': 'passed',
}
output.with_suffix('.json').write_text(json.dumps(record, indent=2) + '\n', encoding='utf-8')
subprocess.run([str(DECODER), '-y', '-hide_banner', '-v', 'error', '-ss', '0.65', '-i', str(output),
                '-frames:v', '1', str(source.parent / 'comparison-preview.png')], check=True)
print(output)

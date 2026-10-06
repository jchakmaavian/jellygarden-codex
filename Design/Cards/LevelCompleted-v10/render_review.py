"""Render verified v10 Unity capture plus a real-time, title-focused reference comparison.

Requires title-buttons-checks.txt to end in a passing SUMMARY and DONE. No Unity commands.
Uses the recorded wall-time cadence for movies, and nearest recorded animation times for the still sheet.
"""
from pathlib import Path
import argparse
import csv
import json
import math
import re
import subprocess

ROOT = Path(__file__).resolve().parent
VERIFY = ROOT / 'Verification'
CAPTURE = VERIFY / 'TitleButtons'
DECODER = Path(r'C:/Program Files/Softdeluxe/Free Download Manager/ffmpeg.exe')
ENCODER = Path(r'C:/Program Files/TimeDoctor2/3.18.89/ffmpeg.exe')
REFERENCE = Path(r'G:/Game/old Backup/JellyGarden-Codex-20261005/Design/RoyalMatchReference/recordings/game completed experience.mp4')
REFERENCE_ONSET = 10.317
REFERENCE_CROP = (140, 530, 800, 235)  # x,y,w,h in the auto-rotated 1080x1920 source.


def run(args):
    subprocess.run([str(arg) for arg in args], check=True)


def ps_quote(value):
    return "'" + str(value).replace("'", "''") + "'"


def ps(commands):
    run(['powershell.exe', '-NoProfile', '-NonInteractive', '-Command', '\n'.join(commands)])


def read_capture():
    checks = VERIFY / 'title-buttons-checks.txt'
    concat = CAPTURE / 'capture.ffconcat'
    timestamps = CAPTURE / 'capture-timestamps.tsv'
    if not all(path.is_file() for path in (checks, concat, timestamps)):
        raise SystemExit('The finalized v10 title/button QA capture is not available yet.')
    log = checks.read_text(encoding='utf-8-sig')
    summary = re.findall(r'SUMMARY passes=(\d+) fails=(\d+)', log)
    if not log.rstrip().endswith('DONE') or not summary or int(summary[-1][1]) != 0:
        raise SystemExit('Refusing to render unfinished or failing title/button QA.')
    if max(concat.stat().st_mtime, timestamps.stat().st_mtime) > checks.stat().st_mtime:
        raise SystemExit('Capture is newer than its final QA log.')
    paths = re.findall(r"^file '(.+)'$", concat.read_text(), re.M)
    rows = list(csv.DictReader(timestamps.open(encoding='utf-8-sig'), delimiter='\t'))
    for row in rows:
        row['frame'] = int(row['frame'])
        for key in ('wall_s', 'title_s', 'heading_x', 'heading_y', 'heading_w', 'heading_h'):
            row[key] = float(row[key])
        row['image'] = CAPTURE / paths[row['frame']]
    if len(rows) < 12 or rows[-1]['wall_s'] < 2.95:
        raise SystemExit('A complete three-second capture is required.')
    # title_s is the RoyalWinCard animation clock, beginning before the .10s title stagger.
    first_moving = next(row for row in rows if row['title_s'] > .001)
    onset_wall = max(0., first_moving['wall_s'] - first_moving['title_s'])
    return concat, rows, onset_wall, int(summary[-1][0])


def pipe_encode(decode_args, output, width, height):
    encode_args = [str(ENCODER), '-y', '-hide_banner', '-v', 'error', '-f', 'rawvideo',
                   '-pix_fmt', 'rgb24', '-video_size', f'{width}x{height}', '-framerate', '30', '-i', '-',
                   '-an', '-c:v', 'libx264', '-preset', 'fast', '-crf', '18', '-pix_fmt', 'yuv420p',
                   '-movflags', '+faststart', str(output)]
    decoder = subprocess.Popen([str(arg) for arg in decode_args], stdout=subprocess.PIPE, cwd=CAPTURE)
    try:
        encoder = subprocess.Popen(encode_args, stdin=decoder.stdout)
        decoder.stdout.close()
        enc = encoder.wait()
        dec = decoder.wait()
    finally:
        if decoder.poll() is None:
            decoder.kill()
            decoder.wait()
    if enc or dec:
        raise SystemExit(f'Encoding failed: decoder={dec}, encoder={enc}')
    run([DECODER, '-hide_banner', '-v', 'error', '-i', output, '-f', 'null', '-'])
    print(output)


def card_movie(concat):
    output = ROOT / 'Royal-Throne-v10-Unity-Card.mp4'
    pipe_encode([DECODER, '-hide_banner', '-v', 'error', '-safe', '0', '-f', 'concat', '-i', concat,
                 '-vf', 'scale=720:1280,fps=30', '-t', '3', '-an', '-f', 'rawvideo', '-pix_fmt', 'rgb24', '-'],
                output, 720, 1280)
    return output


def make_header(path):
    ps(['Add-Type -AssemblyName System.Drawing',
        '$reviewImage = New-Object System.Drawing.Bitmap(1200,44)',
        '$reviewGraphics = [System.Drawing.Graphics]::FromImage($reviewImage)',
        '$reviewGraphics.Clear([System.Drawing.Color]::FromArgb(13,22,38))',
        "$reviewFont = New-Object System.Drawing.Font('Arial',19,[System.Drawing.FontStyle]::Bold)",
        "$reviewGraphics.DrawString('Reference',$reviewFont,[System.Drawing.Brushes]::White,20,8)",
        "$reviewGraphics.DrawString('Royal Throne - Unity',$reviewFont,[System.Drawing.Brushes]::White,620,8)",
        f'$reviewImage.Save({ps_quote(path)},[System.Drawing.Imaging.ImageFormat]::Png)',
        '$reviewGraphics.Dispose()', '$reviewImage.Dispose()', '$reviewFont.Dispose()'])


def crop_filter(rect):
    x, y, w, h = rect
    return f'crop={w}:{h}:{x}:{y}'


def unity_crop(rows):
    # The harness records the title container plus animated headroom in screen pixels.
    row = next(row for row in rows if row['heading_w'] > 0 and row['heading_h'] > 0)
    # Actual captures are 1080x1920 JPEGs; dimensions are part of the harness contract.
    screen_w, screen_h = 1080, 1920
    pad_x = 18
    width = min(screen_w, int(math.ceil(row['heading_w'] + 2 * pad_x)))
    height = int(math.ceil(max(row['heading_h'] + 20, width * 235 / 800)))
    left = max(0, min(screen_w - width, int(math.floor(row['heading_x'] - pad_x))))
    top = max(0, min(screen_h - height, int(math.floor(screen_h - row['heading_y'] - row['heading_h'] - (height - row['heading_h']) * .5))))
    return left, top, width, height


def title_movie(concat, onset_wall, crop):
    output = ROOT / 'Royal-Throne-v10-Reference-vs-Unity-Title.mp4'
    header = CAPTURE / 'title-comparison-header.png'
    make_header(header)
    start_stage = .10
    duration = 1.5
    filters = (f'[0:v]{crop_filter(REFERENCE_CROP)},scale=600:176:force_original_aspect_ratio=decrease,pad=600:176:(ow-iw)/2:(oh-ih)/2:color=0x0d1626,setsar=1,fps=30,setpts=PTS-STARTPTS[ref];'
               f'[1:v]{crop_filter(crop)},scale=600:176:force_original_aspect_ratio=decrease,pad=600:176:(ow-iw)/2:(oh-ih)/2:color=0x0d1626,setsar=1,fps=30,setpts=PTS-STARTPTS[unity];'
               '[ref][unity]hstack=inputs=2[body];'
               '[2:v]setsar=1,fps=30,setpts=PTS-STARTPTS[header];'
               '[header][body]vstack=inputs=2[out]')
    pipe_encode([DECODER, '-hide_banner', '-v', 'error', '-ss', f'{REFERENCE_ONSET + start_stage:.6f}', '-i', REFERENCE,
                 '-ss', f'{onset_wall + start_stage:.6f}', '-safe', '0', '-f', 'concat', '-i', concat,
                 '-loop', '1', '-i', header, '-filter_complex', filters, '-map', '[out]', '-t', str(duration),
                 '-an', '-f', 'rawvideo', '-pix_fmt', 'rgb24', '-'], output, 1200, 220)
    return output, start_stage, duration


def comparison_sheet(rows, crop):
    folder = CAPTURE / 'title-comparison-frames'
    folder.mkdir(exist_ok=True)
    phases = (.20, .30, .45, .60, .75, 1.00)
    mapping = []
    labels = []
    for index, target in enumerate(phases):
        chosen = min(rows, key=lambda row: abs(row['title_s'] - target))
        ref_image, unity_image = folder / f'reference-{index}.png', folder / f'unity-{index}.png'
        run([DECODER, '-y', '-hide_banner', '-v', 'error', '-ss', f'{REFERENCE_ONSET + target:.6f}', '-i', REFERENCE,
             '-frames:v', '1', '-vf', f'{crop_filter(REFERENCE_CROP)},scale=384:114:force_original_aspect_ratio=decrease,pad=384:114:(ow-iw)/2:(oh-ih)/2:color=0x0d1626', ref_image])
        run([DECODER, '-y', '-hide_banner', '-v', 'error', '-i', chosen['image'], '-frames:v', '1',
             '-vf', f'{crop_filter(crop)},scale=384:114:force_original_aspect_ratio=decrease,pad=384:114:(ow-iw)/2:(oh-ih)/2:color=0x0d1626', unity_image])
        labels.append((ref_image, unity_image, target, chosen['title_s']))
        mapping.append({'target_stage_s': target, 'reference_clip_s': REFERENCE_ONSET + target,
                        'unity_frame': chosen['frame'], 'unity_animation_s': chosen['title_s'],
                        'unity_wall_s': chosen['wall_s'], 'unity_image': str(chosen['image'])})
    output = ROOT / 'Royal-Throne-v10-Title-Wave-Comparison.png'
    commands = ['Add-Type -AssemblyName System.Drawing',
                '$reviewImage = New-Object System.Drawing.Bitmap(1152,668)',
                '$reviewGraphics = [System.Drawing.Graphics]::FromImage($reviewImage)',
                '$reviewGraphics.Clear([System.Drawing.Color]::FromArgb(13,22,38))',
                "$reviewTitle = New-Object System.Drawing.Font('Arial',19,[System.Drawing.FontStyle]::Bold)",
                "$reviewFont = New-Object System.Drawing.Font('Arial',11)",
                "$reviewGraphics.DrawString('Well Done! | Reference and actual Unity title wave',$reviewTitle,[System.Drawing.Brushes]::White,18,12)",
                "$reviewGraphics.DrawString('Matched animation stages; Unity labels show nearest captured time. Generated individual letters; reference clip for motion context.',$reviewFont,[System.Drawing.Brushes]::LightSteelBlue,18,44)"]
    for index, (ref_image, unity_image, target, actual) in enumerate(labels):
        x, y = index % 3 * 384, 76 + index // 3 * 290
        commands.append(f'$reviewGraphics.DrawString({ps_quote(f"Reference +{target:.2f}s / clip {REFERENCE_ONSET+target:.3f}s")},$reviewFont,[System.Drawing.Brushes]::White,{x+6},{y})')
        commands.extend([f'$reviewFrame = [System.Drawing.Image]::FromFile({ps_quote(ref_image)})',
                         f'$reviewGraphics.DrawImage($reviewFrame,{x},{y+24},384,114)', '$reviewFrame.Dispose()',
                         f'$reviewGraphics.DrawString({ps_quote(f"Unity +{actual:.3f}s")},$reviewFont,[System.Drawing.Brushes]::White,{x+6},{y+145})',
                         f'$reviewFrame = [System.Drawing.Image]::FromFile({ps_quote(unity_image)})',
                         f'$reviewGraphics.DrawImage($reviewFrame,{x},{y+169},384,114)', '$reviewFrame.Dispose()'])
    commands.extend([f'$reviewImage.Save({ps_quote(output)},[System.Drawing.Imaging.ImageFormat]::Png)',
                     '$reviewGraphics.Dispose()', '$reviewImage.Dispose()', '$reviewTitle.Dispose()', '$reviewFont.Dispose()'])
    ps(commands)
    print(output)
    return output, mapping


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--prepare-only', action='store_true', help='Only validate Python entrypoint; never read incomplete capture.')
    args = parser.parse_args()
    if args.prepare_only:
        print('Renderer prepared; waiting for finalized passing title/button QA.')
        raise SystemExit(0)
    concat, rows, onset_wall, passes = read_capture()
    crop = unity_crop(rows)
    full = card_movie(concat)
    # ffconcat starts its first saved JPEG at zero; its first captured wall timestamp can be nonzero.
    onset_in_capture = max(0., onset_wall - rows[0]['wall_s'])
    comparison, start_stage, duration = title_movie(concat, onset_in_capture, crop)
    sheet, mapping = comparison_sheet(rows, crop)
    report = {'passing_checks': passes, 'capture': str(concat), 'reference': str(REFERENCE),
              'reference_onset_s': REFERENCE_ONSET, 'unity_onset_wall_s': onset_wall,
              'first_saved_frame_wall_s': rows[0]['wall_s'], 'unity_onset_in_capture_s': onset_in_capture,
              'reference_crop_xywh': REFERENCE_CROP, 'unity_crop_xywh': crop,
              'full_card_movie': str(full), 'full_card_duration_s': 3,
              'title_comparison_movie': str(comparison), 'title_comparison_start_stage_s': start_stage,
              'title_comparison_duration_s': duration, 'comparison_sheet': str(sheet), 'stages': mapping,
              'timing': 'Movies retain recorded wall-time cadence. Start offsets only; no speed change. Still sheet uses nearest recorded animation times.',
              'decode_validation': 'all movies passed', 'audio': False}
    (ROOT / 'unity-review.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')

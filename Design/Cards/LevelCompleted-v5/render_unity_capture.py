"""Encode finalized Unity captures at their recorded wall-clock cadence; optionally make a stage sheet.

Examples: --target card --sheet; --target full-flow; --target both
No Unity/editor commands. A passing DONE log is required for each selected capture.
"""
from pathlib import Path
import argparse
import csv
import json
import re
import subprocess

ROOT = Path(__file__).resolve().parent
DECODER = Path(r'C:/Program Files/Softdeluxe/Free Download Manager/ffmpeg.exe')
ENCODER = Path(r'C:/Program Files/TimeDoctor2/3.18.89/ffmpeg.exe')
REFERENCE = Path(r'G:/Game/old Backup/JellyGarden-Codex-20261005/Design/RoyalMatchReference/recordings/game completed experience.mp4')
TARGETS = {
    'card': (ROOT / 'Verification/CardMotion/capture.ffconcat',
             ROOT / 'Verification/CardMotion/checks.txt',
             ROOT / 'Royal-Throne-Level-Complete-v5-Unity-Card.mp4'),
    'full-flow': (ROOT / 'Verification/full-flow-capture.ffconcat',
                  ROOT / 'Verification/reference-experience-checks.txt',
                  ROOT / 'Royal-Throne-Level-Complete-v5-Unity-Full-Flow.mp4'),
}


def require_finished(target):
    source, checks, output = TARGETS[target]
    if not source.is_file() or not checks.is_file():
        raise SystemExit(f'{target}: capture/check log missing; run its Unity QA harness first.')
    log = checks.read_text(encoding='utf-8-sig')
    summary = re.findall(r'SUMMARY passes=(\d+) fails=(\d+)', log)
    if not log.rstrip().endswith('DONE') or not summary or int(summary[-1][1]) != 0:
        raise SystemExit(f'{target}: refusing to encode an unfinished or failing QA run.')
    if source.stat().st_mtime > checks.stat().st_mtime:
        raise SystemExit(f'{target}: capture is newer than the final QA result.')
    return source, output, int(summary[-1][0])


def encode(target):
    source, output, passes = require_finished(target)
    decode = [str(DECODER), '-hide_banner', '-v', 'error', '-safe', '0', '-f', 'concat',
              '-i', str(source), '-vf', 'scale=720:1280,fps=30', '-an', '-f', 'rawvideo',
              '-pix_fmt', 'rgb24', '-']
    encode_args = [str(ENCODER), '-y', '-hide_banner', '-v', 'error', '-f', 'rawvideo',
                   '-pix_fmt', 'rgb24', '-video_size', '720x1280', '-framerate', '30', '-i', '-',
                   '-an', '-c:v', 'libx264', '-preset', 'fast', '-crf', '18', '-pix_fmt', 'yuv420p',
                   '-movflags', '+faststart', str(output)]
    decoder = subprocess.Popen(decode, stdout=subprocess.PIPE, cwd=source.parent)
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
        raise SystemExit(f'{target}: FFmpeg failed: decoder={decode_status}, encoder={encode_status}')
    subprocess.run([str(DECODER), '-hide_banner', '-v', 'error', '-i', str(output), '-f', 'null', '-'], check=True)
    durations = [float(x) for x in re.findall(r'^duration ([\d.]+)$', source.read_text(), re.M)]
    record = {'source': str(source), 'output': str(output), 'passing_checks': passes,
              'recorded_duration_seconds': sum(durations), 'source_frames': len(durations),
              'output_fps': 30, 'size': [720, 1280], 'audio': False,
              'timing': 'Recorded ffconcat wall-time durations resampled at 30 fps; not retimed to the animation clock.',
              'decode_validation': 'passed'}
    output.with_suffix('.json').write_text(json.dumps(record, indent=2) + '\n', encoding='utf-8')
    print(output)


def ps_quote(value):
    return "'" + str(value).replace("'", "''") + "'"


def sheet():
    source, _, _ = require_finished('card')
    if not REFERENCE.is_file():
        raise SystemExit(f'Reference clip unavailable: {REFERENCE}')
    folder = source.parent / 'comparison-frames'
    folder.mkdir(exist_ok=True)
    rows = list(csv.DictReader((source.parent / 'capture-timestamps.tsv').open(encoding='utf-8-sig'), delimiter='\t'))
    paths = re.findall(r"^file '(.+)'$", source.read_text(), re.M)
    first_hero = next(int(row['frame']) for row in rows if float(row['star_scale']) >= 2)
    rows = [row for row in rows if int(row['frame']) >= first_hero]
    labels = []
    mapping = []
    for index, target in enumerate((0, .1, .2, .35, .65, 1.1)):
        selected = min(rows, key=lambda row: abs(float(row['star_s']) - target))
        clip_time = 10.317 + target
        reference_image = folder / f'reference-{index}.png'
        unity_image = folder / f'unity-{index}.png'
        subprocess.run([str(DECODER), '-y', '-hide_banner', '-v', 'error', '-ss', f'{clip_time:.6f}',
                        '-i', str(REFERENCE), '-frames:v', '1', '-vf', 'scale=270:480', str(reference_image)], check=True)
        selected_image = source.parent / paths[int(selected['frame'])]
        subprocess.run([str(DECODER), '-y', '-hide_banner', '-v', 'error', '-i', str(selected_image),
                        '-frames:v', '1', '-vf', 'scale=270:480', str(unity_image)], check=True)
        labels.append((reference_image, f'Reference +{target:.2f}s', f'Clip {clip_time:.3f}s'))
        labels.append((unity_image, f'Unity +{float(selected["star_s"]):.3f}s', f'Wall {float(selected["wall_s"]):.3f}s / frame {selected["frame"]}'))
        mapping.append({'target_stage_seconds': target, 'reference_clip_seconds': clip_time,
                        'unity_frame': int(selected['frame']), 'unity_star_seconds': float(selected['star_s']),
                        'unity_wall_seconds': float(selected['wall_s']), 'unity_source_image': str(selected_image)})
    output = source.parent / 'Reference-vs-Unity-Star-Entrance.png'
    # Windows already supplies System.Drawing; no additional Python image packages are needed.
    commands = [
        'Add-Type -AssemblyName System.Drawing',
        '$sheetBitmap = New-Object System.Drawing.Bitmap(1620,1152)',
        '$sheetGraphics = [System.Drawing.Graphics]::FromImage($sheetBitmap)',
        '$sheetGraphics.Clear([System.Drawing.Color]::FromArgb(13,22,38))',
        '$sheetGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic',
        "$sheetTitleFont = New-Object System.Drawing.Font('Arial',19,[System.Drawing.FontStyle]::Bold)",
        "$sheetLabelFont = New-Object System.Drawing.Font('Arial',13)",
        "$sheetSmallFont = New-Object System.Drawing.Font('Arial',10)",
        "$sheetGraphics.DrawString('Reference and Royal Throne | Star entrance stages',$sheetTitleFont,[System.Drawing.Brushes]::White,20,12)",
        "$sheetGraphics.DrawString('Measured source stages and nearest captured Unity animation times. Different artwork/layout; not a pixel comparison.',$sheetLabelFont,[System.Drawing.Brushes]::LightSteelBlue,20,45)",
    ]
    for index, (path, title, detail) in enumerate(labels):
        col = index // 2
        row = index % 2
        x, y = col * 270, 80 + row * 526
        commands.extend([
            f'$sheetGraphics.DrawString({ps_quote(title)},$sheetLabelFont,[System.Drawing.Brushes]::White,{x+8},{y})',
            f'$sheetGraphics.DrawString({ps_quote(detail)},$sheetSmallFont,[System.Drawing.Brushes]::LightSteelBlue,{x+8},{y+22})',
            f'$sheetFrame = [System.Drawing.Image]::FromFile({ps_quote(path)})',
            f'$sheetGraphics.DrawImage($sheetFrame,{x},{y+42},270,480)',
            '$sheetFrame.Dispose()',
        ])
    commands.extend([
        f'$sheetBitmap.Save({ps_quote(output)},[System.Drawing.Imaging.ImageFormat]::Png)',
        '$sheetGraphics.Dispose()', '$sheetBitmap.Dispose()', '$sheetTitleFont.Dispose()',
        '$sheetLabelFont.Dispose()', '$sheetSmallFont.Dispose()',
    ])
    subprocess.run(['powershell.exe', '-NoProfile', '-NonInteractive', '-Command', '\n'.join(commands)], check=True)
    output.with_suffix('.json').write_text(json.dumps({'reference': str(REFERENCE), 'reference_onset_seconds': 10.317,
                                                      'comparisons': mapping}, indent=2) + '\n', encoding='utf-8')
    print(output)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--target', choices=['card', 'full-flow', 'both'], default='card')
    parser.add_argument('--sheet', action='store_true', help='Also render the six-stage card comparison sheet.')
    args = parser.parse_args()
    for target in ('card', 'full-flow') if args.target == 'both' else (args.target,):
        encode(target)
    if args.sheet:
        sheet()

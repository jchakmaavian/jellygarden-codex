from pathlib import Path
import difflib, hashlib, json, zipfile, subprocess

root = Path(__file__).resolve().parents[3]
review = Path(__file__).resolve().parent
files = ['Assets/JellyGarden/Scripts/Juice/' + name + '.cs' for name in
         ('ColorBallFrameAnimator', 'ColorBallBoostView', 'ColorBallTargetVfx', 'ColorBallPresentation', 'RoyalColorBallSequence')]
files += ['Assets/JellyGarden/Scripts/Editor/RoyalColorBallQaChecks.cs']
files += ['Assets/JellyGarden/Resources/RoyalColorBall/' + name + '.shader' for name in ('ColorBallPresentation', 'ColorBallOutline')]
patch = []
with zipfile.ZipFile(review / 'ColorBall-Polish.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
    for name in files:
        src = root / name
        before = review / 'baseline' / src.name
        patch.extend(difflib.unified_diff(before.read_text().splitlines(True) if before.exists() else [],
                     src.read_text().splitlines(True), fromfile='a/' + name, tofile='b/' + name))
        archive.write(src, name)
        archive.write(Path(str(src) + '.meta'), name + '.meta')
(review / 'incremental.patch').write_text(''.join(patch))
hashes = json.loads((root / 'Design/Effects/ColorBall-live-v1/source-hashes.json').read_text())
assert all(hashlib.sha256((root / p).read_bytes()).hexdigest() == h for p, h in hashes.items())
protected = json.loads((review / 'protected-hashes.json').read_text(encoding='utf-8-sig'))
assert all(hashlib.sha256(Path(row['Path']).read_bytes()).hexdigest().upper() == row['Hash'] for row in protected)
print(f'Unchanged: {len(hashes)} approved frames and {len(protected)} protected Rocket/TNT files.')
ffmpeg = 'C:/Program Files/Softdeluxe/Free Download Manager/ffmpeg.exe'
for label in ('effect-half', 'spawn-activate-full', 'rocket-combo-full'):
    folder = root / f'Temp/ColorBall-live-v4-{label}'
    stamps = list(map(float, (folder / 'timestamps.txt').read_text().splitlines()))
    entries = []
    for i, t in enumerate(stamps):
        duration = stamps[i + 1] - t if i + 1 < len(stamps) else 1 / 30
        entries.extend([f"file '{(folder / f'{i:04d}.jpg').as_posix()}'", f'duration {duration:.5f}'])
    listing = folder / 'frames.txt'
    listing.write_text('\n'.join(entries))
    subprocess.run([ffmpeg, '-hide_banner', '-loglevel', 'error', '-y', '-f', 'concat', '-safe', '0',
                    '-i', str(listing), '-r', '30', '-c:v', 'h264_mf', '-b:v', '5000k', '-pix_fmt',
                    'yuv420p', '-movflags', '+faststart', str(review / f'{label}.mp4')], check=True)
    print(label, len(stamps), 'captured frames;', round(stamps[-1] - stamps[0], 2), 'seconds')

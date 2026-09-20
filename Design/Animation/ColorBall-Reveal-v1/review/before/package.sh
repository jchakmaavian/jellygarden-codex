#!/usr/bin/env bash
# Package one Color Ball sequence: lossless WebP frames, animated WebP preview, MP4 preview, contact sheet, zip, source.
# usage: package.sh <Name: Reveal|Idle|Boosting> <png master dir> <dest dir> <frame count> <mode: once|loop|boost> <rig dir>
set -euo pipefail
NAME=$1; SRC=$2; DEST=$3; COUNT=$4; MODE=$5; RIG=$6
PREFIX="ColorBall_${NAME}"
FONT="fontfile='C\:/Windows/Fonts/arial.ttf'"
DARK=0x1d2230; LAV=0x8a93c8
mkdir -p "$DEST/frames" "$DEST/source"
rm -f "$DEST/frames/"*.webp "$DEST/Preview.webp" "$DEST/Preview.mp4" "$DEST/ContactSheet.png" "$DEST/60FPS_WEBP.zip"

# 1. Frames: lossless WebP (ARGB).
ffmpeg -hide_banner -loglevel error -y -start_number 1 -i "$SRC/${PREFIX}_%03d.png" -frames:v "$COUNT" \
  -c:v libwebp -lossless 1 -compression_level 6 -quality 100 -pix_fmt bgra "$DEST/frames/${PREFIX}_%03d.webp"

# 2. Animated transparent preview (plays the whole sequence, loops forever).
ffmpeg -hide_banner -loglevel error -y -framerate 60 -start_number 1 -i "$SRC/${PREFIX}_%03d.png" -frames:v "$COUNT" \
  -c:v libwebp_anim -lossless 0 -quality 90 -compression_level 6 -pix_fmt yuva420p -loop 0 "$DEST/Preview.webp"

# 3. MP4 preview, 1024x512: left dark slate, right board lavender.
case "$MODE" in
  once)  SEQ="[1]format=rgba,tpad=stop_mode=clone:stop_duration=0.5,loop=loop=2:size=$((COUNT + 30)):start=0[s]" ;;
  loop)  SEQ="[1]format=rgba,loop=loop=2:size=${COUNT}:start=0[s]" ;;
  boost) SEQ="[1]format=rgba,split=3[a][b][c];[a]trim=end_frame=30,setpts=N/60/TB[i];[b]trim=start_frame=30:end_frame=120,setpts=N/60/TB,loop=loop=2:size=90:start=0[l];[c]trim=start_frame=120,setpts=N/60/TB,tpad=stop_duration=0.4:color=black@0[f];[i][l][f]concat=n=3:v=1:a=0,setpts=N/60/TB[s]" ;;
esac
ffmpeg -hide_banner -loglevel error -y -f lavfi -i "color=c=${DARK}:s=512x512:r=60" -framerate 60 -start_number 1 -i "$SRC/${PREFIX}_%03d.png" \
  -f lavfi -i "color=c=${LAV}:s=512x512:r=60" \
  -filter_complex "${SEQ};[s]split[s1][s2];[0][s1]overlay=format=auto:shortest=1[L];[2][s2]overlay=format=auto:shortest=1[R];[L][R]hstack,format=yuv420p" \
  -c:v libx264 -crf 14 -preset slow -r 60 -movflags +faststart "$DEST/Preview.mp4"

# 4. Contact sheet, all frames numbered, on the board lavender.
ROWS=$(( (COUNT + 11) / 12 ))
ffmpeg -hide_banner -loglevel error -y -f lavfi -i "color=c=${LAV}:s=512x512:r=60" -framerate 60 -start_number 1 -i "$SRC/${PREFIX}_%03d.png" \
  -filter_complex "[0][1]overlay=format=auto:shortest=1,scale=192:192:flags=lanczos,drawtext=${FONT}:text='%{eif\:n+1\:d}':x=4:y=4:fontsize=15:fontcolor=black,tile=12x${ROWS}:padding=2:color=0x303442" \
  -frames:v 1 "$DEST/ContactSheet.png"

# 5. Zip of the WebP frames.
WDEST=$(cygpath -w "$DEST")
powershell -NoProfile -ExecutionPolicy Bypass -File "$(cygpath -w "$RIG/zipdir.ps1")" -Root "$WDEST" -Items frames -Out "$WDEST\\60FPS_WEBP.zip"

# 6. Source: the rig, sequences, render and QA scripts.
for f in BoosterFx.cs BallAnalyze.cs BallRig.cs ColorBallSeq.cs ColorBallReveal.cs ColorBallIdleBoost.cs render_seq.ps1 render_all.sh QaCheck.cs qa.ps1 package.sh zipdir.ps1; do cp "$RIG/$f" "$DEST/source/$f"; done
echo "packaged $NAME: $(ls "$DEST/frames" | wc -l) frames"

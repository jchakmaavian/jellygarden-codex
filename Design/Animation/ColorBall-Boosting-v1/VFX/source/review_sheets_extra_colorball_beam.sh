#!/bin/sh
# Extra review images for ColorBall_Beam: 1:1 key frames and Strip ends plain-scaled vs 9-slice. Writes into out/vfx/ColorBall_Beam/review/.
set -e
B="$(cd "$(dirname "$0")/../.." && pwd)"
R="$B/out/vfx/ColorBall_Beam"; O="$R/review"; mkdir -p "$O"; cd "$R"
FONT="fontfile='C\:/Windows/Fonts/arial.ttf'"
ff() { ffmpeg -hide_banner -loglevel error -y "$@"; }
for bgname in dark lavender; do
  if [ $bgname = dark ]; then C=0x20242e; else C=0x8a93c8; fi
  args=""; filt=""; n=0
  for k in 6 10 11 12 20 29 31 33; do
    n=$((n+1)); args="$args -i Sequence/frames/ColorBall_Beam_$(printf %03d $k).png"
    filt="$filt color=c=$C:s=640x160[b$n];[b$n][$((n-1))]overlay=format=auto,drawtext=$FONT:text='f$k':x=4:y=4:fontsize=14:fontcolor=white[v$n];"
  done
  ff $args -filter_complex "${filt}[v1][v2][v3][v4][v5][v6][v7][v8]vstack=inputs=8" -frames:v 1 "$O/rv_keys_1to1_${bgname}.png"
  # Strip target end: plain 0.4x | 9-slice 0.4x | plain 3x | 9-slice 3x, each 4x zoom of the last 60 px
  ff -i preview_strip_sliced_${bgname}.png -filter_complex \
    "[0]split=4[a][b][c][d];[a]crop=60:60:195:320,scale=240:240:flags=neighbor[p1];[b]crop=60:60:195:20,scale=240:240:flags=neighbor[p2];\
[c]crop=60:60:1464:420,scale=240:240:flags=neighbor[p3];[d]crop=60:60:1464:220,scale=240:240:flags=neighbor[p4];[p1][p2][p3][p4]hstack=inputs=4" \
    -frames:v 1 "$O/rv_strip_ends_scaled_${bgname}.png"
done
echo extra sheets ok

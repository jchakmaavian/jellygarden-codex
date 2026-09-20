#!/bin/sh
# Reviewer sheets for ColorBall_Beam. Writes only into out/vfx/ColorBall_Beam/review/.
set -e
B="$(cd "$(dirname "$0")/../.." && pwd)"
R="$B/out/vfx/ColorBall_Beam"
O="$R/review"
mkdir -p "$O"
cd "$R"
FONT="fontfile='C\:/Windows/Fonts/arial.ttf'"
ff() { ffmpeg -hide_banner -loglevel error -y "$@"; }
Q=Sequence/frames/ColorBall_Beam_%03d.png

for bgname in dark lavender; do
  if [ $bgname = dark ]; then C=0x20242e; else C=0x8a93c8; fi
  # full-res tiles: travel+arrival f1-f12, loop f13-f26, fade f27-f34
  ff -f lavfi -i "color=c=$C:s=640x160:r=60" -framerate 60 -start_number 1 -i $Q \
    -filter_complex "[0][1]overlay=format=auto:shortest=1,drawtext=$FONT:text='f%{eif\:n+1\:d}':x=3:y=3:fontsize=14:fontcolor=white:box=1:boxcolor=black@0.5,trim=end_frame=12,tile=2x6:padding=4:color=0x000000" -frames:v 1 "$O/rv_travel_${bgname}.png"
  ff -f lavfi -i "color=c=$C:s=640x160:r=60" -framerate 60 -start_number 13 -i $Q \
    -filter_complex "[0][1]overlay=format=auto:shortest=1,drawtext=$FONT:text='f%{eif\:n+13\:d}':x=3:y=3:fontsize=14:fontcolor=white:box=1:boxcolor=black@0.5,trim=end_frame=14,tile=2x7:padding=4:color=0x000000" -frames:v 1 "$O/rv_loop_${bgname}.png"
  ff -f lavfi -i "color=c=$C:s=640x160:r=60" -framerate 60 -start_number 27 -i $Q \
    -filter_complex "[0][1]overlay=format=auto:shortest=1,drawtext=$FONT:text='f%{eif\:n+27\:d}':x=3:y=3:fontsize=14:fontcolor=white:box=1:boxcolor=black@0.5,trim=end_frame=8,tile=2x4:padding=4:color=0x000000" -frames:v 1 "$O/rv_fade_${bgname}.png"
  # 4x zoom on target end (x 470..630, y 20..140) for f9..f14 and f20, f26
  args=""; filt=""; n=0
  for k in 9 10 11 12 13 20 26 29; do
    n=$((n+1)); args="$args -i Sequence/frames/ColorBall_Beam_$(printf %03d $k).png"
    filt="$filt color=c=$C:s=640x160[b$n];[b$n][$((n-1))]overlay=format=auto,crop=160:120:470:20,scale=480:360:flags=neighbor,drawtext=$FONT:text='f$k':x=4:y=4:fontsize=18:fontcolor=white[v$n];"
  done
  ff $args -filter_complex "${filt}[v1][v2][v3][v4]hstack=4[t];[v5][v6][v7][v8]hstack=4[u];[t][u]vstack=2" -frames:v 1 "$O/rv_target3x_${bgname}.png"
  # 4x zoom on the source end (x 0..120, y 50..110) for f3, f13, f20, f27, f28, f29, f30, f31
  args=""; filt=""; n=0
  for k in 3 13 20 27 28 29 30 31; do
    n=$((n+1)); args="$args -i Sequence/frames/ColorBall_Beam_$(printf %03d $k).png"
    filt="$filt color=c=$C:s=640x160[b$n];[b$n][$((n-1))]overlay=format=auto,crop=120:60:0:50,scale=480:240:flags=neighbor,drawtext=$FONT:text='f$k':x=4:y=4:fontsize=18:fontcolor=white[v$n];"
  done
  ff $args -filter_complex "${filt}[v1][v2][v3][v4]hstack=4[t];[v5][v6][v7][v8]hstack=4[u];[t][u]vstack=2" -frames:v 1 "$O/rv_source4x_${bgname}.png"
  # 4x zoom on travelling head f4, f6, f8 around the tip, and the body mid-section in loop frames
  args=""; filt=""; n=0
  for k in 4 6 8 9; do
    n=$((n+1)); args="$args -i Sequence/frames/ColorBall_Beam_$(printf %03d $k).png"
    case $k in 4) X=240;; 6) X=350;; 8) X=420;; 9) X=440;; esac
    filt="$filt color=c=$C:s=640x160[b$n];[b$n][$((n-1))]overlay=format=auto,crop=140:50:$X:55,scale=560:200:flags=neighbor,drawtext=$FONT:text='f$k':x=4:y=4:fontsize=18:fontcolor=white[v$n];"
  done
  ff $args -filter_complex "${filt}[v1][v2]hstack=2[t];[v3][v4]hstack=2[u];[t][u]vstack=2" -frames:v 1 "$O/rv_head4x_${bgname}.png"
  args=""; filt=""; n=0
  for k in 13 14 15 16 17 18 25 26; do
    n=$((n+1)); args="$args -i Sequence/frames/ColorBall_Beam_$(printf %03d $k).png"
    filt="$filt color=c=$C:s=640x160[b$n];[b$n][$((n-1))]overlay=format=auto,crop=200:40:200:60,scale=800:160:flags=neighbor,drawtext=$FONT:text='f$k':x=4:y=4:fontsize=18:fontcolor=white[v$n];"
  done
  ff $args -filter_complex "${filt}[v1][v2]hstack=2[a];[v3][v4]hstack=2[b];[v5][v6]hstack=2[c];[v7][v8]hstack=2[d];[a][b][c][d]vstack=4" -frames:v 1 "$O/rv_body4x_${bgname}.png"
  # Strip 1:1 + 0.4x + 3x (crop) + 4x ends ; Head 1:1 + 5x
  ff -i Strip/frames/ColorBall_Beam_001.png -filter_complex \
    "[0]split=5[s0][s1][s2][s3][s4];color=c=$C:s=1040x96[b0];[b0][s0]overlay=format=auto[r0];\
[s1]scale=205:96:flags=bilinear[t1];color=c=$C:s=1040x96[b1];[b1][t1]overlay=format=auto[r1];\
[s2]scale=1536:96:flags=bilinear,crop=1040:96:496:0[t2];color=c=$C:s=1040x96[b2];[b2][t2]overlay=format=auto[r2];\
color=c=$C:s=512x96[b3];[b3][s3]overlay=format=auto,crop=130:60:0:18,scale=520:240:flags=neighbor[z1];\
color=c=$C:s=512x96[b4];[b4][s4]overlay=format=auto,crop=130:60:382:18,scale=520:240:flags=neighbor[z2];\
[r0][r1][r2]vstack=3[top];[z1][z2]hstack=2[bot];[top][bot]vstack=2" -frames:v 1 "$O/rv_strip_${bgname}.png"
  ff -i Head/frames/ColorBall_Beam_001.png -filter_complex \
    "[0]split=2[a][b];color=c=$C:s=160x64[b0];[b0][a]overlay=format=auto,pad=800:64:0:0:color=$C[r0];\
color=c=$C:s=160x64[b1];[b1][b]overlay=format=auto,scale=800:320:flags=neighbor,drawbox=x=670:y=0:w=1:h=320:color=red@0.6[r1];[r0][r1]vstack=2" -frames:v 1 "$O/rv_head_${bgname}.png"
done
# loop wrap at 1:1: f25 f26 | f13 f14 on lavender and dark
for bgname in dark lavender; do
  if [ $bgname = dark ]; then C=0x20242e; else C=0x8a93c8; fi
  args=""; filt=""; n=0
  for k in 25 26 13 14; do
    n=$((n+1)); args="$args -i Sequence/frames/ColorBall_Beam_$(printf %03d $k).png"
    filt="$filt color=c=$C:s=640x160[b$n];[b$n][$((n-1))]overlay=format=auto,drawtext=$FONT:text='f$k':x=4:y=4:fontsize=14:fontcolor=white[v$n];"
  done
  ff $args -filter_complex "${filt}[v1][v2][v3][v4]vstack=4" -frames:v 1 "$O/rv_wrap_${bgname}.png"
done
echo sheets ok

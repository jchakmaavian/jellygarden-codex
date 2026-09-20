#!/bin/sh
# Review sheets for ColorBall_Beam. Usage: sheets_colorball_beam.sh <out/vfx/ColorBall_Beam dir> [crop frames, default "5 11 20 30"]
set -e
R="$1"
KEYS="${2:-5 11 20 30}"
FONT="fontfile='C\:/Windows/Fonts/arial.ttf'"
DARK=0x20242e
LAV=0x8a93c8
ff() { ffmpeg -hide_banner -loglevel error -y "$@"; }

# ---------------------------------------------------------------- Sequence
S="$R/Sequence"
for bgname in dark lavender; do
  if [ $bgname = dark ]; then C=$DARK; else C=$LAV; fi
  ff -f lavfi -i "color=c=$C:s=640x160:r=60" -framerate 60 -start_number 1 -i "$S/frames/ColorBall_Beam_%03d.png" \
    -filter_complex "[0][1]overlay=format=auto:shortest=1,scale=400:100:flags=area,drawtext=$FONT:text='%{eif\:n+1\:d}':x=3:y=3:fontsize=14:fontcolor=white:box=1:boxcolor=black@0.4,tile=5x7:padding=2:color=0x000000" \
    -frames:v 1 "$S/sheet_${bgname}.png"
  # 1:1 key frames stacked vertically
  args=""; filt=""; n=0
  for k in $KEYS; do
    n=$((n+1))
    args="$args -i $S/frames/ColorBall_Beam_$(printf %03d $k).png"
    filt="$filt color=c=$C:s=640x160[b$n];[b$n][$((n-1))]overlay=format=auto,drawtext=$FONT:text='f$k':x=4:y=4:fontsize=14:fontcolor=white[v$n];"
  done
  stack=""; i=1; while [ $i -le $n ]; do stack="$stack[v$i]"; i=$((i+1)); done
  ff $args -filter_complex "${filt}${stack}vstack=inputs=$n" -frames:v 1 "$S/crops_1to1_${bgname}.png"
  # 3x zoom on the contact end for the arrival and hold frames, and on the travelling head
  ff -i "$S/frames/ColorBall_Beam_011.png" -i "$S/frames/ColorBall_Beam_020.png" -i "$S/frames/ColorBall_Beam_006.png" -filter_complex \
    "color=c=$C:s=640x160[b1];[b1][0]overlay=format=auto,crop=160:160:480:0,scale=480:480:flags=neighbor[a];\
color=c=$C:s=640x160[b2];[b2][1]overlay=format=auto,crop=160:160:480:0,scale=480:480:flags=neighbor[b];\
color=c=$C:s=640x160[b3];[b3][2]overlay=format=auto,crop=160:160:360:0,scale=480:480:flags=neighbor[c];[a][b][c]hstack=inputs=3" \
    -frames:v 1 "$S/zoom3x_${bgname}.png"
  # 3x zoom on the body mid-section across a crackle re-seed (f19,f20 share a seed; f21 is new), plus loop wrap f26 -> f13
  ff -i "$S/frames/ColorBall_Beam_019.png" -i "$S/frames/ColorBall_Beam_020.png" -i "$S/frames/ColorBall_Beam_021.png" \
     -i "$S/frames/ColorBall_Beam_026.png" -i "$S/frames/ColorBall_Beam_013.png" -filter_complex \
    "color=c=$C:s=640x160[b1];[b1][0]overlay=format=auto,crop=240:60:300:50,scale=720:180:flags=neighbor,drawtext=$FONT:text='f19':x=4:y=4:fontsize=16:fontcolor=white[a];\
color=c=$C:s=640x160[b2];[b2][1]overlay=format=auto,crop=240:60:300:50,scale=720:180:flags=neighbor,drawtext=$FONT:text='f20':x=4:y=4:fontsize=16:fontcolor=white[b];\
color=c=$C:s=640x160[b3];[b3][2]overlay=format=auto,crop=240:60:300:50,scale=720:180:flags=neighbor,drawtext=$FONT:text='f21':x=4:y=4:fontsize=16:fontcolor=white[c];\
color=c=$C:s=640x160[b4];[b4][3]overlay=format=auto,crop=240:60:300:50,scale=720:180:flags=neighbor,drawtext=$FONT:text='f26':x=4:y=4:fontsize=16:fontcolor=white[d];\
color=c=$C:s=640x160[b5];[b5][4]overlay=format=auto,crop=240:60:300:50,scale=720:180:flags=neighbor,drawtext=$FONT:text='f13':x=4:y=4:fontsize=16:fontcolor=white[e];\
[a][b][c][d][e]vstack=inputs=5" -frames:v 1 "$S/zoom3x_body_${bgname}.png"
done
# Animated previews of the whole clip and of the hold loop played 3 times (GIF, dark board)
ff -f lavfi -i "color=c=$DARK:s=640x160:r=60" -framerate 60 -start_number 1 -i "$S/frames/ColorBall_Beam_%03d.png" \
  -filter_complex "[0][1]overlay=format=auto:shortest=1,split[x][y];[x]palettegen=max_colors=128[p];[y][p]paletteuse" "$S/preview_dark.gif"
ff -f lavfi -i "color=c=$LAV:s=640x160:r=60" -stream_loop 2 -framerate 60 -start_number 13 -i "$S/frames/ColorBall_Beam_%03d.png" \
  -filter_complex "[1]trim=end_frame=14,loop=2:14:0[l];[0][l]overlay=format=auto:shortest=1,split[x][y];[x]palettegen=max_colors=128[p];[y][p]paletteuse" "$S/preview_loop_lavender.gif" || true

# ---------------------------------------------------------------- Strip
T="$R/Strip"
for bgname in dark lavender; do
  if [ $bgname = dark ]; then C=$DARK; else C=$LAV; fi
  # 1:1, then X-stretched 0.4x and 1.5x (3x shown in its own file), then 4x zooms of both ends
  ff -i "$T/frames/ColorBall_Beam_001.png" -filter_complex \
    "[0]split=5[s0][s1][s2][s3][s4];\
color=c=$C:s=780x96[b0];[b0][s0]overlay=format=auto[r0];\
[s1]scale=205:96:flags=bilinear[t1];color=c=$C:s=780x96[b1];[b1][t1]overlay=format=auto[r1];\
[s2]scale=768:96:flags=bilinear[t2];color=c=$C:s=780x96[b2];[b2][t2]overlay=format=auto[r2];\
color=c=$C:s=512x96[b3];[b3][s3]overlay=format=auto,crop=96:96:0:0,scale=384:384:flags=neighbor[z1];\
color=c=$C:s=512x96[b4];[b4][s4]overlay=format=auto,crop=96:96:416:0,scale=384:384:flags=neighbor[z2];\
[r0][r1][r2]vstack=inputs=3[top];[z1][z2]hstack=inputs=2,pad=780:384:0:0:color=$C[bot];[top][bot]vstack=inputs=2" \
    -frames:v 1 "$T/sheet_${bgname}.png"
  ff -i "$T/frames/ColorBall_Beam_001.png" -filter_complex \
    "[0]scale=1536:96:flags=bilinear[s];color=c=$C:s=1536x96[b];[b][s]overlay=format=auto" -frames:v 1 "$T/stretch3x_${bgname}.png"
done

# ---------------------------------------------------------------- Head
H="$R/Head"
for bgname in dark lavender; do
  if [ $bgname = dark ]; then C=$DARK; else C=$LAV; fi
  ff -i "$H/frames/ColorBall_Beam_001.png" -filter_complex \
    "[0]split=2[a][b];color=c=$C:s=160x64[b0];[b0][a]overlay=format=auto,pad=640:64:0:0:color=$C[r0];\
color=c=$C:s=160x64[b1];[b1][b]overlay=format=auto,scale=640:256:flags=neighbor[r1];[r0][r1]vstack=inputs=2" \
    -frames:v 1 "$H/sheet_${bgname}.png"
done

# ---------------------------------------------------------------- SequenceBody (same layout as Sequence) and Contact
SB="$R/SequenceBody"
CT="$R/Contact"
for bgname in dark lavender; do
  if [ $bgname = dark ]; then C=$DARK; else C=$LAV; fi
  ff -f lavfi -i "color=c=$C:s=640x160:r=60" -framerate 60 -start_number 1 -i "$SB/frames/ColorBall_Beam_%03d.png" \
    -filter_complex "[0][1]overlay=format=auto:shortest=1,scale=400:100:flags=area,drawtext=$FONT:text='%{eif\:n+1\:d}':x=3:y=3:fontsize=14:fontcolor=white:box=1:boxcolor=black@0.4,tile=5x7:padding=2:color=0x000000" \
    -frames:v 1 "$SB/sheet_${bgname}.png"
  # Contact: all 34 frames at 1:1 (160x160), 9 columns
  ff -f lavfi -i "color=c=$C:s=160x160:r=60" -framerate 60 -start_number 1 -i "$CT/frames/ColorBall_Beam_%03d.png" \
    -filter_complex "[0][1]overlay=format=auto:shortest=1,drawtext=$FONT:text='%{eif\:n+1\:d}':x=3:y=3:fontsize=13:fontcolor=white,tile=9x4:padding=2:color=0x000000" \
    -frames:v 1 "$CT/sheet_${bgname}.png"
  # Contact key frames f11 f12 f13 f20 f30 f32 at 2x
  args=""; filt=""; n=0
  for k in 11 12 13 20 30 32; do
    n=$((n+1)); args="$args -i $CT/frames/ColorBall_Beam_$(printf %03d $k).png"
    filt="$filt color=c=$C:s=160x160[b$n];[b$n][$((n-1))]overlay=format=auto,scale=320:320:flags=neighbor,drawtext=$FONT:text='f$k':x=4:y=4:fontsize=16:fontcolor=white[v$n];"
  done
  ff $args -filter_complex "${filt}[v1][v2][v3][v4][v5][v6]hstack=inputs=6" -frames:v 1 "$CT/zoom2x_keys_${bgname}.png"
done
echo sheets done

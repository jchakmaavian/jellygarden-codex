#!/bin/sh
# Review sheets for ColorBall_TargetImpact: dark + lavender contact sheets, 1:1 key-frame crops (8 frames, 4x2).
# Usage: sheets_colorball_targetimpact.sh [suffix] [key frame numbers...]
V="$(cd "$(dirname "$0")" && pwd)"
B="$(cd "$V/../.." && pwd)"
A="$B/out/vfx/ColorBall_TargetImpact"
cd "$A" || exit 1
SUF="${1:-}"; [ $# -gt 0 ] && shift
KEYS="${*:-1 5 11 13 15 20 29 31}"
FONT="fontfile='C\:/Windows/Fonts/arial.ttf'"
for BG in 20242e:dark 8a93c8:lavender; do
  C="${BG%%:*}"; N="${BG##*:}"
  ffmpeg -hide_banner -loglevel error -y -f lavfi -i "color=c=0x$C:s=384x384:r=60,format=rgba" -framerate 60 -start_number 1 -i frames/ColorBall_TargetImpact_%03d.png \
    -filter_complex "[0][1]overlay=format=rgb:shortest=1,scale=192:192:flags=area,drawtext=$FONT:text='%{eif\:n+1\:d}':x=3:y=3:fontsize=14:fontcolor=white,tile=8x6:padding=2:color=0x101010" \
    -frames:v 1 "sheet_${N}${SUF}.png"
  ARGS=""; CNT=0
  for F in $KEYS; do ARGS="$ARGS -i frames/ColorBall_TargetImpact_$(printf %03d "$F").png"; CNT=$((CNT+1)); done
  FIL="[0]split=$CNT"; j=1; while [ $j -le $CNT ]; do FIL="$FIL[b$j]"; j=$((j+1)); done; FIL="$FIL;"
  j=1; for F in $KEYS; do FIL="$FIL[b$j][$j]overlay=format=rgb,drawtext=$FONT:text='$F':x=4:y=4:fontsize=16:fontcolor=white[o$j];"; j=$((j+1)); done
  STACK=""; j=1; while [ $j -le $CNT ]; do STACK="$STACK[o$j]"; j=$((j+1)); done
  ffmpeg -hide_banner -loglevel error -y -f lavfi -i "color=c=0x$C:s=384x384,format=rgba" $ARGS \
    -filter_complex "${FIL}${STACK}xstack=inputs=$CNT:grid=4x$(( (CNT+3)/4 ))" -frames:v 1 "crops_${N}${SUF}.png"
done
# 1:1 consecutive strips (centre 256x256 crop) for the dissolve and the rim fade, on both backgrounds.
strip() { # name first count bg bgname
  NAME=$1; FIRST=$2; COUNT=$3; C=$4; N=$5
  ffmpeg -hide_banner -loglevel error -y -f lavfi -i "color=c=0x$C:s=384x384:r=60,format=rgba" -framerate 60 -start_number "$FIRST" -i frames/ColorBall_TargetImpact_%03d.png \
    -filter_complex "[0][1]overlay=format=rgb:shortest=1,crop=256:256:64:64,drawtext=$FONT:text='%{eif\:n+$FIRST\:d}':x=3:y=3:fontsize=14:fontcolor=white,tile=${COUNT}x1:padding=2:color=0x101010" \
    -frames:v 1 "seq_${NAME}_${N}${SUF}.png"
}
for BG in 20242e:dark 8a93c8:lavender; do
  C="${BG%%:*}"; N="${BG##*:}"
  strip dissolve 10 6 "$C" "$N"
  strip ring 15 6 "$C" "$N"
  strip fade 25 7 "$C" "$N"
done
# Zooms: 3x crops of the late filaments (dark + lavender), the hollow in the ring phase, and a 3-target board mock at half scale.
zoom() { # out frame cropW cropH cropX cropY scale bg
  OUTN=$1; FR=$2; CW=$3; CH=$4; CX=$5; CY=$6; S=$7; C=$8
  ffmpeg -hide_banner -loglevel error -y -f lavfi -i "color=c=0x$C:s=384x384,format=rgba" -i "frames/ColorBall_TargetImpact_$(printf %03d "$FR").png" \
    -filter_complex "[0][1]overlay=format=rgb,crop=$CW:$CH:$CX:$CY,scale=iw*$S:ih*$S:flags=neighbor,drawtext=$FONT:text='f$FR x$S':x=4:y=4:fontsize=16:fontcolor=white" -frames:v 1 "$OUTN"
}
mkdir -p _z
for FR in 28 29 30 31; do
  zoom "_z/fd_$FR.png" "$FR" 130 100 196 96 3 20242e
  zoom "_z/fl_$FR.png" "$FR" 130 100 196 96 3 8a93c8
  zoom "_z/hd_$FR.png" "$FR" 130 100 196 96 3 20242e
done
for FR in 17 21 25; do zoom "_z/hh_$FR.png" "$FR" 130 100 127 130 3 20242e; done
ffmpeg -hide_banner -loglevel error -y -i _z/fd_28.png -i _z/fd_29.png -i _z/fd_30.png -i _z/fd_31.png -i _z/fl_28.png -i _z/fl_29.png -i _z/fl_30.png -i _z/fl_31.png \
  -filter_complex "xstack=inputs=8:layout=0_0|w0_0|w0+w1_0|w0+w1+w2_0|0_h0|w0_h0|w0+w1_h0|w0+w1+w2_h0" -frames:v 1 "zoom_fade${SUF}.png"
ffmpeg -hide_banner -loglevel error -y -i _z/hh_17.png -i _z/hh_21.png -i _z/hh_25.png -filter_complex "hstack=inputs=3" -frames:v 1 "zoom_hollow${SUF}.png"
rm -rf _z
# Board mock: three targets at 1/2 scale (96 px cells), one rotated 90 deg, over each board colour.
for BG in 20242e:dark 8a93c8:lavender; do
  C="${BG%%:*}"; N="${BG##*:}"; T=""
  for FR in 1 5 12 15 20 26 28 29 30 31 33 38; do
    F3="frames/ColorBall_TargetImpact_$(printf %03d "$FR").png"
    ffmpeg -hide_banner -loglevel error -y -f lavfi -i "color=c=0x$C:s=384x192,format=rgba" -i "$F3" -i "$F3" -i "$F3" \
      -filter_complex "[1]scale=192:192[a];[2]scale=192:192,transpose=1[b];[3]scale=192:192,hflip[c];[0][a]overlay=0:0:format=rgb[o1];[o1][b]overlay=96:0:format=rgb[o2];[o2][c]overlay=192:0:format=rgb,drawtext=$FONT:text='f$FR':x=3:y=3:fontsize=13:fontcolor=white" -frames:v 1 "_bm_$FR.png"
    T="$T -i _bm_$FR.png"
  done
  ffmpeg -hide_banner -loglevel error -y $T -filter_complex "xstack=inputs=12:grid=6x2" -frames:v 1 "boardmock_${N}${SUF}.png"
  rm -f _bm_*.png
done

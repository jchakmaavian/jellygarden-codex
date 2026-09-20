#!/usr/bin/env bash
# Review sheets for ColorBall_StarSpark: tiled sheets on dark + lavender, 1:1 key-frame crops, nearest-neighbour zooms.
# Writes only inside out/vfx/ColorBall_StarSpark/<clip>/ (sheets) and out/vfx/ColorBall_StarSpark/tmp/ (nothing kept).
set -e
OUT="$1"   # out/vfx/ColorBall_StarSpark (posix path)
A=ColorBall_StarSpark
FONT="fontfile='C\:/Windows/Fonts/arial.ttf'"
ff() { ffmpeg -hide_banner -loglevel error -y "$@"; }

sheet() { # clip cols rows bg name
  local clip=$1 cols=$2 rows=$3 bg=$4 name=$5
  ff -f lavfi -i "color=c=$bg:s=128x128:r=60" -framerate 60 -start_number 1 -i "$OUT/$clip/frames/${A}_%03d.png" \
    -filter_complex "[0][1]overlay=format=auto:shortest=1,drawtext=$FONT:text='%{eif\:n+1\:d}':x=3:y=3:fontsize=13:fontcolor=white@0.85,tile=${cols}x${rows}:padding=2:color=0x101010" \
    -frames:v 1 "$OUT/$clip/$name"
}

crops() { # clip bg name frames...
  local clip=$1 bg=$2 name=$3; shift 3
  local inputs=() filt="" k=0 chain=""
  for f in "$@"; do inputs+=(-i "$OUT/$clip/frames/${A}_$(printf %03d $f).png"); done
  for f in "$@"; do
    filt+="color=c=$bg:s=128x128,format=rgba[bg$k];[bg$k][$k]overlay=format=auto,drawtext=$FONT:text='f$f':x=3:y=3:fontsize=12:fontcolor=white@0.8[c$k];"
    chain+="[c$k]"; k=$((k+1))
  done
  filt+="${chain}hstack=inputs=$k"
  ff "${inputs[@]}" -filter_complex "$filt" -frames:v 1 "$OUT/$clip/$name"
}

zoom() { # clip frame bg factor name
  local clip=$1 f=$2 bg=$3 z=$4 name=$5
  ff -f lavfi -i "color=c=$bg:s=128x128" -i "$OUT/$clip/frames/${A}_$(printf %03d $f).png" \
    -filter_complex "[0][1]overlay=format=auto,scale=$((128*z)):$((128*z)):flags=neighbor" -frames:v 1 "$OUT/$clip/$name"
}

# Zoomed end strip: centre 48x48 of frames 18..21, 6x nearest.
endzoom() { # clip bg name
  local clip=$1 bg=$2 name=$3
  local inputs=() filt="" k=0 chain=""
  for f in 18 19 20 21; do inputs+=(-i "$OUT/$clip/frames/${A}_$(printf %03d $f).png"); done
  for f in 18 19 20 21; do
    filt+="color=c=$bg:s=128x128,format=rgba[bg$k];[bg$k][$k]overlay=format=auto,crop=48:48:40:40,scale=288:288:flags=neighbor,drawtext=$FONT:text='f$f':x=4:y=4:fontsize=16:fontcolor=white@0.8[c$k];"
    chain+="[c$k]"; k=$((k+1))
  done
  filt+="${chain}hstack=inputs=$k"
  ff "${inputs[@]}" -filter_complex "$filt" -frames:v 1 "$OUT/$clip/$name"
}

rm -f "$OUT"/White/*.png "$OUT"/Gold/*.png "$OUT"/Static/*.png
for clip in White Gold; do
  sheet $clip 8 3 0x20242e sheet_dark.png
  sheet $clip 8 3 0x8a93c8 sheet_lavender.png
  crops $clip 0x20242e crops_1to1_pop_dark.png 1 2 3 4 5 10
  crops $clip 0x8a93c8 crops_1to1_pop_lavender.png 1 2 3 4 5 10
  crops $clip 0x20242e crops_1to1_end_dark.png 14 16 18 19 20 21
  crops $clip 0x8a93c8 crops_1to1_end_lavender.png 14 16 18 19 20 21
  zoom $clip 3 0x20242e 3 zoom3x_f003_dark.png
  zoom $clip 10 0x20242e 3 zoom3x_f010_dark.png
  zoom $clip 10 0x8a93c8 3 zoom3x_f010_lavender.png
  endzoom $clip 0x20242e zoom6x_end_f018-021_dark.png
  endzoom $clip 0x8a93c8 zoom6x_end_f018-021_lavender.png
done
sheet Static 2 1 0x20242e sheet_dark.png
sheet Static 2 1 0x8a93c8 sheet_lavender.png
zoom Static 1 0x20242e 3 zoom3x_f001_dark.png
zoom Static 1 0x8a93c8 3 zoom3x_f001_lavender.png
zoom Static 2 0x20242e 3 zoom3x_f002_dark.png
zoom Static 2 0x8a93c8 3 zoom3x_f002_lavender.png

# House comparison: Static white | reviewer FatStar ref white | Static gold | FatStar ref gold (refs only if present).
RV="$OUT/review"
if [ -f "$RV/ref_fatstar_white.png" ] && [ -f "$RV/ref_fatstar_gold.png" ]; then
  for bg in 0x20242e 0x8a93c8; do
    ff -i "$OUT/Static/frames/${A}_001.png" -i "$RV/ref_fatstar_white.png" -i "$OUT/Static/frames/${A}_002.png" -i "$RV/ref_fatstar_gold.png" \
      -filter_complex "color=c=$bg:s=128x128,format=rgba,split=4[b0][b1][b2][b3];[b0][0]overlay=format=auto[c0];[b1][1]overlay=format=auto[c1];[b2][2]overlay=format=auto[c2];[b3][3]overlay=format=auto[c3];[c0][c1][c2][c3]hstack=inputs=4,scale=1024:256:flags=lanczos" \
      -frames:v 1 "$OUT/Static/compare_house_$bg.png"
  done
fi
# Board scale: hold frame 10 at 1x, 0.5x and 0.25x on both boards.
for clip in White Gold; do
  for bg in 0x20242e 0x8a93c8; do
    ff -f lavfi -i "color=c=$bg:s=256x128" -i "$OUT/$clip/frames/${A}_010.png" \
      -filter_complex "[1]split[a][b];[a]scale=64:64:flags=area[h];[b]scale=32:32:flags=area[q];[0][h]overlay=16:32:format=auto[t];[t][q]overlay=90:48:format=auto[u];[u][1]overlay=128:0:format=auto" \
      -frames:v 1 "$OUT/$clip/boardscale_$bg.png"
  done
done
echo sheets done

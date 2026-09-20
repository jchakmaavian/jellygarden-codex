#!/usr/bin/env bash
# Reviewer sheets for ColorBall_StarSpark (writes only to out/vfx/ColorBall_StarSpark/review/).
set -e
BASE="$1"   # posix path to booster
OUT="$BASE/out/vfx/ColorBall_StarSpark"
RV="$OUT/review"
mkdir -p "$RV"
A=ColorBall_StarSpark
FONT="fontfile='C\:/Windows/Fonts/arial.ttf'"
ff() { ffmpeg -hide_banner -loglevel error -y "$@"; }

sheet() { # clip cols rows bg name
  local clip=$1 cols=$2 rows=$3 bg=$4 name=$5
  ff -f lavfi -i "color=c=$bg:s=128x128:r=60" -framerate 60 -start_number 1 -i "$OUT/$clip/frames/${A}_%03d.png" \
    -filter_complex "[0][1]overlay=format=auto:shortest=1,drawtext=$FONT:text='%{eif\:n+1\:d}':x=3:y=3:fontsize=12:fontcolor=red,tile=${cols}x${rows}:padding=2:color=0x000000" \
    -frames:v 1 "$RV/$name"
}

crops() { # clip bg name frames...
  local clip=$1 bg=$2 name=$3; shift 3
  local inputs=() filt="" k=0 chain=""
  for f in "$@"; do inputs+=(-i "$OUT/$clip/frames/${A}_$(printf %03d $f).png"); done
  for f in "$@"; do
    filt+="color=c=$bg:s=128x128,format=rgba[bg$k];[bg$k][$k]overlay=format=auto[c$k];"
    chain+="[c$k]"; k=$((k+1))
  done
  filt+="${chain}hstack=inputs=$k"
  ff "${inputs[@]}" -filter_complex "$filt" -frames:v 1 "$RV/$name"
}

zoom() { # clip frame bg factor name
  local clip=$1 f=$2 bg=$3 z=$4 name=$5
  ff -f lavfi -i "color=c=$bg:s=128x128" -i "$OUT/$clip/frames/${A}_$(printf %03d $f).png" \
    -filter_complex "[0][1]overlay=format=auto,scale=$((128*z)):$((128*z)):flags=neighbor" -frames:v 1 "$RV/$name"
}

for clip in White Gold; do
  sheet $clip 8 3 0x20242e ${clip}_sheet_dark.png
  sheet $clip 8 3 0x8a93c8 ${clip}_sheet_lavender.png
  crops $clip 0x20242e ${clip}_crops_pop_dark.png 1 2 3 4 5
  crops $clip 0x8a93c8 ${clip}_crops_pop_lavender.png 1 2 3 4 5
  crops $clip 0x20242e ${clip}_crops_end_dark.png 14 16 18 19 20 21
  crops $clip 0x8a93c8 ${clip}_crops_end_lavender.png 14 16 18 19 20 21
  zoom $clip 20 0x20242e 4 ${clip}_zoom4_f020_dark.png
  zoom $clip 21 0x20242e 4 ${clip}_zoom4_f021_dark.png
  zoom $clip 10 0x8a93c8 3 ${clip}_zoom3_f010_lavender.png
done
zoom Static 1 0x20242e 3 Static_zoom3_f001_dark.png
zoom Static 1 0x8a93c8 3 Static_zoom3_f001_lavender.png
zoom Static 2 0x20242e 3 Static_zoom3_f002_dark.png
zoom Static 2 0x8a93c8 3 Static_zoom3_f002_lavender.png

# Board-scale readability: 0.5x and 0.25x area downscale of hold frame 10 (white/gold) on both backgrounds.
for clip in White Gold; do
  for bg in 0x20242e 0x8a93c8; do
    ff -f lavfi -i "color=c=$bg:s=256x128" -i "$OUT/$clip/frames/${A}_010.png" \
      -filter_complex "[1]split[a][b];[a]scale=64:64:flags=area[h];[b]scale=32:32:flags=area[q];[0][h]overlay=16:32:format=auto[t];[t][q]overlay=112:48:format=auto[u];[u][1]overlay=128:0:format=auto" \
      -frames:v 1 "$RV/${clip}_boardscale_${bg}.png"
  done
done

# House reference: reveal frame 27 star ring, cropped
for bg in 0x20242e 0x8a93c8; do
  ff -f lavfi -i "color=c=$bg:s=512x512" -i "$BASE/out/final/reveal/ColorBall_Reveal_027.png" \
    -filter_complex "[0][1]overlay=format=auto" -frames:v 1 "$RV/ref_reveal027_${bg}.png"
done

# Animated contact at 60fps for temporal check: side by side White|Gold over dark and lavender as a GIF-like strip (mp4)
ff -f lavfi -i "color=c=0x20242e:s=128x128:r=60" -f lavfi -i "color=c=0x8a93c8:s=128x128:r=60" \
  -framerate 60 -start_number 1 -i "$OUT/White/frames/${A}_%03d.png" -framerate 60 -start_number 1 -i "$OUT/Gold/frames/${A}_%03d.png" \
  -filter_complex "[2]split[w1][w2];[3]split[g1][g2];[0]split[d1][d2];[1]split[l1][l2];[d1][w1]overlay=format=auto:shortest=1[a];[d2][g1]overlay=format=auto:shortest=1[b];[l1][w2]overlay=format=auto:shortest=1[c];[l2][g2]overlay=format=auto:shortest=1[d];[a][b][c][d]hstack=inputs=4" \
  -c:v libx264 -pix_fmt yuv420p -crf 12 "$RV/anim_white_gold.mp4"
echo sheets done

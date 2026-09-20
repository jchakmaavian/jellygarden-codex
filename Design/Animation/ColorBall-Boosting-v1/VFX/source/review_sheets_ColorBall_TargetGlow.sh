#!/usr/bin/env bash
# Reviewer sheets for ColorBall_TargetGlow (writes only review/review_*.png).
set -euo pipefail
ROOT="$1"
P=ColorBall_TargetGlow
R=review
FONT="fontfile='C\:/Windows/Fonts/arial.ttf'"
FF="ffmpeg -hide_banner -loglevel error -y"
cd "$ROOT"
LAB="drawtext=$FONT:text='%{eif\:n+1\:d}':x=4:y=4:fontsize=16:fontcolor=white:box=1:boxcolor=black@0.5"

# tile clip bg cols rows out [piecefile]
tile() {
  local clip=$1 bg=$2 cols=$3 rows=$4 out=$5 piece=${6:-}
  if [ -n "$piece" ]; then
    $FF -f lavfi -i "color=c=$bg:s=256x256:r=60" -loop 1 -framerate 60 -i "$piece" -framerate 60 -start_number 1 -i "$clip/frames/${P}_%03d.png" \
      -filter_complex "[0][1]overlay=format=auto:shortest=1[b];[b][2]overlay=format=auto:shortest=1,$LAB,tile=${cols}x${rows}:padding=2:color=0x000000" -frames:v 1 "$out"
  else
    $FF -f lavfi -i "color=c=$bg:s=256x256:r=60" -framerate 60 -start_number 1 -i "$clip/frames/${P}_%03d.png" \
      -filter_complex "[0][1]overlay=format=auto:shortest=1,$LAB,tile=${cols}x${rows}:padding=2:color=0x000000" -frames:v 1 "$out"
  fi
}

tile On 0x20242e 5 2 $R/review_on_dark.png
tile On 0x8a93c8 5 2 $R/review_on_lav.png
tile On 0x8a93c8 5 2 $R/review_on_lav_yellow.png $R/review_piece_yellow.png
tile On 0x8a93c8 5 2 $R/review_on_lav_blue.png $R/review_piece_blue.png
tile Loop 0x20242e 6 5 $R/review_loop_dark.png
tile Loop 0x8a93c8 6 5 $R/review_loop_lav.png
tile Loop 0x8a93c8 6 5 $R/review_loop_lav_red.png $R/review_piece_red.png

# four pieces, loop f1 and f9 and f23, 1:1, lavender
row() { # frame file -> row of 4 pieces
  local f=$1 out=$2
  $FF -f lavfi -i "color=c=0x8a93c8:s=256x256" -i $R/review_piece_red.png -i $R/review_piece_yellow.png -i $R/review_piece_blue.png -i $R/review_piece_green.png -i "$f" \
    -filter_complex "[0][1]overlay[a];[a][5]overlay=format=auto[A];[0][2]overlay[b];[b][5]overlay=format=auto[B];[0][3]overlay[c];[c][5]overlay=format=auto[C];[0][4]overlay[d];[d][5]overlay=format=auto[D];[0][1]overlay[e];[0][2]overlay[g];[e][g][A][B][C][D]hstack=inputs=6" -frames:v 1 "$out"
}
row Loop/frames/${P}_009.png $R/review_pieces_loop9.png
row On/frames/${P}_004.png $R/review_pieces_on4.png

# zooms 3x (nearest) of key regions on dark and lavender
zoom() { # file bg cropW cropH x y out
  local f=$1 bg=$2 w=$3 h=$4 x=$5 y=$6 out=$7
  $FF -f lavfi -i "color=c=$bg:s=256x256" -i "$f" -filter_complex "[0][1]overlay=format=auto,crop=$w:$h:$x:$y,scale=iw*3:ih*3:flags=neighbor" -frames:v 1 "$out"
}
for k in 002 003 004 005; do zoom On/frames/${P}_$k.png 0x20242e 110 110 140 8 $R/review_zoom_star_on$k.png; done
$FF -i $R/review_zoom_star_on002.png -i $R/review_zoom_star_on003.png -i $R/review_zoom_star_on004.png -i $R/review_zoom_star_on005.png -filter_complex "hstack=inputs=4" -frames:v 1 $R/review_zoom_star_strip.png
rm -f $R/review_zoom_star_on00?.png
for k in 001 002 003 009 023; do zoom Loop/frames/${P}_$k.png 0x20242e 128 128 128 128 $R/review_zoom_br_loop$k.png; done
$FF -i $R/review_zoom_br_loop001.png -i $R/review_zoom_br_loop002.png -i $R/review_zoom_br_loop003.png -filter_complex "hstack=inputs=3" -frames:v 1 $R/review_zoom_crackle_strip.png
$FF -i $R/review_zoom_br_loop009.png -i $R/review_zoom_br_loop023.png -filter_complex "hstack=inputs=2" -frames:v 1 $R/review_zoom_breath_9_vs_23.png
rm -f $R/review_zoom_br_loop0??.png
zoom Loop/frames/${P}_023.png 0x8a93c8 96 96 0 0 $R/review_zoom_glintB_lav_f23.png
zoom Loop/frames/${P}_009.png 0x8a93c8 96 96 160 160 $R/review_zoom_glintA_lav_f9.png
# seam strip f28 f29 f30 f1 f2 f3 on dark 1:1
$FF -f lavfi -i "color=c=0x20242e:s=256x256" -i Loop/frames/${P}_028.png -i Loop/frames/${P}_029.png -i Loop/frames/${P}_030.png -i Loop/frames/${P}_001.png -i Loop/frames/${P}_002.png -i Loop/frames/${P}_003.png \
  -filter_complex "[0][1]overlay=format=auto[a];[0][2]overlay=format=auto[b];[0][3]overlay=format=auto[c];[0][4]overlay=format=auto[d];[0][5]overlay=format=auto[e];[0][6]overlay=format=auto[f];[a][b][c][d][e][f]hstack=inputs=6" -frames:v 1 $R/review_seam_dark.png
# alpha channel view of on f1..f5 and loop f1
$FF -framerate 60 -start_number 1 -i On/frames/${P}_%03d.png -filter_complex "alphaextract,tile=5x2:padding=2" -frames:v 1 $R/review_on_alpha.png
echo done

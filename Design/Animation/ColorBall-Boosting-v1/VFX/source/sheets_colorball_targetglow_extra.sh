#!/usr/bin/env bash
# Extra review images for ColorBall_TargetGlow fix pass: sparkle end-of-life zooms, On 1:1 crops over a yellow piece,
# breathing peak/trough 1:1 over lavender + red piece, and an On->Loop x3 preview video.
set -euo pipefail
ROOT="$1"; P=ColorBall_TargetGlow; R=review
FONT="fontfile='C\:/Windows/Fonts/arial.ttf'"
FF="ffmpeg -hide_banner -loglevel error -y"
cd "$ROOT"
rm -f $R/zoom3x_dark.png $R/zoom3x_glints_lav_piece.png $R/review_zoom_leak_f7_f8.png $R/review_zoom_on8_dark.png $R/review_crop_on_1_2_3_8_yellow.png
# Sparkle zooms (3x nearest): top-left quadrant, where sparkles 3 and 4 end their life, On f5..f8 over dark.
ins=(); chain=""; k=0
for f in 005 006 007 008; do
  ins+=(-i On/frames/${P}_$f.png)
  chain+="[0][$((k+1))]overlay=format=auto,crop=128:128:0:0,scale=384:384:flags=neighbor,drawtext=$FONT:text='On f${f#00}':x=4:y=4:fontsize=18:fontcolor=white:box=1:boxcolor=black@0.5[z$k];"
  k=$((k+1))
done
$FF -f lavfi -i "color=c=0x20242e:s=256x256" "${ins[@]}" -filter_complex "${chain}[z0][z1][z2][z3]hstack=inputs=4" -frames:v 1 $R/fix_zoom_sparkles_on5_8_dark.png
# On 1:1 crops f1 f2 f3 f8 over yellow piece on lavender.
ins=(); chain=""; k=0
for f in 001 002 003 008; do
  ins+=(-i On/frames/${P}_$f.png)
  chain+="[0][1]overlay[b$k];[b$k][$((k+2))]overlay=format=auto,drawtext=$FONT:text='On f${f#00}':x=4:y=4:fontsize=14:fontcolor=white:box=1:boxcolor=black@0.5,pad=258:256:0:0[c$k];"
  k=$((k+1))
done
$FF -f lavfi -i "color=c=0x8a93c8:s=256x256" -i $R/review_piece_yellow.png "${ins[@]}" -filter_complex "${chain}[c0][c1][c2][c3]hstack=inputs=4" -frames:v 1 $R/fix_crop_on_1_2_3_8_yellow.png
# Breathing 1:1: Loop f9 (peak) vs f24 (trough), over lavender bare and over red piece.
$FF -f lavfi -i "color=c=0x8a93c8:s=256x256" -i $R/review_piece_red.png -i Loop/frames/${P}_009.png -i Loop/frames/${P}_024.png \
  -filter_complex "[0][2]overlay=format=auto,drawtext=$FONT:text='Loop f9 peak':x=4:y=4:fontsize=14:fontcolor=white:box=1:boxcolor=black@0.5,pad=258:256[a];[0][3]overlay=format=auto,drawtext=$FONT:text='Loop f24 trough':x=4:y=4:fontsize=14:fontcolor=white:box=1:boxcolor=black@0.5,pad=258:256[b];[0][1]overlay[p1];[p1][2]overlay=format=auto,pad=258:256[c];[0][1]overlay[p2];[p2][3]overlay=format=auto[d];[a][b][c][d]hstack=inputs=4" -frames:v 1 $R/fix_breath_peak_trough_lav.png
# 3x zoom of the rim at peak vs trough over lavender (right edge mid).
$FF -f lavfi -i "color=c=0x8a93c8:s=256x256" -i Loop/frames/${P}_009.png -i Loop/frames/${P}_024.png \
  -filter_complex "[0][1]overlay=format=auto,crop=64:64:188:96,scale=192:192:flags=neighbor,pad=194:192[a];[0][2]overlay=format=auto,crop=64:64:188:96,scale=192:192:flags=neighbor[b];[a][b]hstack" -frames:v 1 $R/fix_zoom_rim_peak_trough_lav.png
# Preview: On then Loop x3 over lavender + yellow piece.
$FF -framerate 60 -start_number 1 -i On/frames/${P}_%03d.png -framerate 60 -start_number 1 -i Loop/frames/${P}_%03d.png -f lavfi -i "color=c=0x8a93c8:s=256x256:r=60:d=2" -loop 1 -framerate 60 -i $R/review_piece_yellow.png \
  -filter_complex "[1]loop=loop=2:size=30:start=0[l];[0][l]concat=n=2:v=1[fx];[2][3]overlay=shortest=1[bg];[bg][fx]overlay=format=auto:shortest=1,format=yuv420p" -c:v libx264 -crf 16 $R/preview_on_then_loop_x3.mp4
echo extra done

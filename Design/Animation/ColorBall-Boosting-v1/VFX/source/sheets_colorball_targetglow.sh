#!/usr/bin/env bash
# Review sheets for ColorBall_TargetGlow: dark + lavender tiles, lavender over a mock piece, 1:1 crops, On->Loop handoff strip.
set -euo pipefail
ROOT="$1"   # out/vfx/ColorBall_TargetGlow (POSIX path)
P=ColorBall_TargetGlow
FONT="fontfile='C\:/Windows/Fonts/arial.ttf'"
DARK=0x20242e
LAV=0x8a93c8
PIECE="$ROOT/review/mock_piece.png"
FF="ffmpeg -hide_banner -loglevel error -y"
cd "$ROOT"

tile() { # clip bg cols rows scale out [piece]
  local clip=$1 bg=$2 cols=$3 rows=$4 sc=$5 out=$6 piece=${7:-}
  local lab="drawtext=$FONT:text='%{eif\:n+1\:d}':x=4:y=4:fontsize=16:fontcolor=white:box=1:boxcolor=black@0.45"
  if [ -n "$piece" ]; then
    $FF -f lavfi -i "color=c=$bg:s=256x256:r=60" -loop 1 -framerate 60 -i "$PIECE" -framerate 60 -start_number 1 -i "$clip/frames/${P}_%03d.png" \
      -filter_complex "[0][1]overlay=format=auto:shortest=1[b];[b][2]overlay=format=auto:shortest=1,scale=$sc:$sc:flags=lanczos,$lab,tile=${cols}x${rows}:padding=2" -frames:v 1 "$out"
  else
    $FF -f lavfi -i "color=c=$bg:s=256x256:r=60" -framerate 60 -start_number 1 -i "$clip/frames/${P}_%03d.png" \
      -filter_complex "[0][1]overlay=format=auto:shortest=1,scale=$sc:$sc:flags=lanczos,$lab,tile=${cols}x${rows}:padding=2" -frames:v 1 "$out"
  fi
}

tile On $DARK 5 2 256 On/sheet_dark.png
tile On $LAV 5 2 256 On/sheet_lavender.png
tile On $LAV 5 2 256 On/sheet_lavender_piece.png piece
tile Loop $DARK 6 5 192 Loop/sheet_dark.png
tile Loop $LAV 6 5 192 Loop/sheet_lavender.png
tile Loop $LAV 6 5 192 Loop/sheet_lavender_piece.png piece

crops() { # out bg piece|none label1 file1 label2 file2 ...
  local out=$1 bg=$2 piece=$3; shift 3
  local inputs=() filt="" n=0 idx=0
  while [ $# -gt 0 ]; do
    local lab=$1 f=$2; shift 2
    inputs+=(-i "$f")
    n=$((n+1))
  done
  local args=(-f lavfi -i "color=c=$bg:s=256x256:r=1" )
  if [ "$piece" = piece ]; then args+=(-i "$PIECE"); fi
  local base=1; [ "$piece" = piece ] && base=2
  local k=0 chain="" labels=("${CROP_LABELS[@]}")
  for ((k=0; k<n; k++)); do
    local src=$((base+k))
    if [ "$piece" = piece ]; then
      chain+="[0][1]overlay=format=auto[p$k];[p$k][$src]overlay=format=auto,"
    else
      chain+="[0][$src]overlay=format=auto,"
    fi
    chain+="drawtext=$FONT:text='${labels[$k]}':x=4:y=4:fontsize=14:fontcolor=white:box=1:boxcolor=black@0.45,pad=258:256:0:0:color=0x000000[c$k];"
  done
  local st=""; for ((k=0; k<n; k++)); do st+="[c$k]"; done
  $FF "${args[@]}" "${inputs[@]}" -filter_complex "${chain}${st}hstack=inputs=$n" -frames:v 1 "$out"
}

CROP_LABELS=("On f1" "On f2" "On f4" "On f10")
for bg in dark lav; do
  c=$DARK; [ $bg = lav ] && c=$LAV
  crops On/crop1to1_$bg.png $c none a On/frames/${P}_001.png b On/frames/${P}_002.png c On/frames/${P}_004.png d On/frames/${P}_010.png
done
crops On/crop1to1_lav_piece.png $LAV piece a On/frames/${P}_001.png b On/frames/${P}_002.png c On/frames/${P}_004.png d On/frames/${P}_010.png
CROP_LABELS=("Loop f1" "Loop f8" "Loop f16" "Loop f22")
for bg in dark lav; do
  c=$DARK; [ $bg = lav ] && c=$LAV
  crops Loop/crop1to1_$bg.png $c none a Loop/frames/${P}_001.png b Loop/frames/${P}_008.png c Loop/frames/${P}_016.png d Loop/frames/${P}_022.png
done
crops Loop/crop1to1_lav_piece.png $LAV piece a Loop/frames/${P}_001.png b Loop/frames/${P}_008.png c Loop/frames/${P}_016.png d Loop/frames/${P}_022.png
CROP_LABELS=("On f8" "On f9" "On f10" "Loop f1" "Loop f2")
crops review/handoff_on_to_loop_dark.png $DARK none a On/frames/${P}_008.png b On/frames/${P}_009.png c On/frames/${P}_010.png d Loop/frames/${P}_001.png e Loop/frames/${P}_002.png
CROP_LABELS=("Loop f28" "Loop f29" "Loop f30" "Loop f1" "Loop f2")
crops review/loop_wrap_lav_piece.png $LAV piece a Loop/frames/${P}_028.png b Loop/frames/${P}_029.png c Loop/frames/${P}_030.png d Loop/frames/${P}_001.png e Loop/frames/${P}_002.png
echo sheets done

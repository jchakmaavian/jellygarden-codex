#!/usr/bin/env bash
# Re-render and re-package all three propeller sequences.
# usage: render_all.sh <rig dir> <propeller.png> <png master root> <Design/Animation dir>
set -euo pipefail
RIG=$1; ART=$2; OUT=$3; DEST=$4
WRIG=$(cygpath -w "$RIG"); WART=$(cygpath -w "$ART")

for seq in reveal:Reveal:40:once idle:Idle:120:loop activation:Activation:120:act; do
  IFS=: read -r key name count mode <<< "$seq"
  mkdir -p "$OUT/$key"
  powershell -NoProfile -ExecutionPolicy Bypass -File "$WRIG\\render_seq.ps1" \
    -Dir "$WRIG" -Src "$WART" -Seq "$key" -Out "$(cygpath -w "$OUT/$key")" -Frames "1-$count"
  bash "$RIG/package.sh" "$name" "$OUT/$key" "$DEST/Propeller-$name-v1" "$count" "$mode" "$RIG"
done
echo "all three sequences rendered and packaged"

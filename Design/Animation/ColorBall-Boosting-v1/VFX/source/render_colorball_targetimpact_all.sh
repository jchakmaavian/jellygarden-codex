#!/bin/sh
# Renders all 46 frames of ColorBall_TargetImpact in 3 parallel processes.
V="$(cd "$(dirname "$0")" && pwd)"
B="$(cd "$V/../.." && pwd)"
PS1="$(cygpath -w "$V/render_colorball_targetimpact.ps1")"
DIR="$(cygpath -w "$V")"
OUT="$(cygpath -w "$B/out/vfx/ColorBall_TargetImpact")"
powershell -NoProfile -ExecutionPolicy Bypass -File "$PS1" -Dir "$DIR" -Out "$OUT" -From 0 -To 15 &
powershell -NoProfile -ExecutionPolicy Bypass -File "$PS1" -Dir "$DIR" -Out "$OUT" -From 16 -To 31 &
powershell -NoProfile -ExecutionPolicy Bypass -File "$PS1" -Dir "$DIR" -Out "$OUT" -From 32 -To 45 &
wait

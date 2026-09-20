#!/bin/sh
# Render all ColorBall_Beam clips (Strip, Head, Sequence, SequenceBody, Contact) in 3 parallel processes after deleting stale
# frames, write previews and review sheets, run the builder check. Stops on the first error.
set -e
B="$(cd "$(dirname "$0")/../.." && pwd)"
V="$B/rig/vfx"; O="$B/out/vfx/ColorBall_Beam"
RIG=$(cygpath -w "$V"); OUT=$(cygpath -w "$O"); PS=$(cygpath -w "$V/render_colorball_beam.ps1")
mkdir -p "$O"
rm -rf "$O/Strip/frames" "$O/Head/frames" "$O/Sequence/frames" "$O/SequenceBody/frames" "$O/Contact/frames"
run() { powershell -NoProfile -ExecutionPolicy Bypass -File "$PS" -Dir "$RIG" -Out "$OUT" "$@"; }
run -From 0 -To 11 > "$O/.r1.log" 2>&1 & p1=$!
run -From 12 -To 22 > "$O/.r2.log" 2>&1 & p2=$!
run -From 23 -To 33 > "$O/.r3.log" 2>&1 & p3=$!
wait $p1; wait $p2; wait $p3
cat "$O/.r1.log" "$O/.r2.log" "$O/.r3.log"
grep -q "render ms" "$O/.r1.log" && grep -q "render ms" "$O/.r2.log" && grep -q "render ms" "$O/.r3.log" || exit 1
rm -f "$O/.r1.log" "$O/.r2.log" "$O/.r3.log"
run -Preview "20,hold"
run -ScalePreviews
( cd "$O" && sh "$V/sheets_colorball_beam.sh" . )
sh "$V/review_sheets_extra_colorball_beam.sh"
mkdir -p "$O/review"
powershell -NoProfile -ExecutionPolicy Bypass -File "$(cygpath -w "$V/check_colorball_beam.ps1")" -Dir "$RIG" -Out "$OUT" 2>&1 | tee "$O/review/builder_check_report.txt"

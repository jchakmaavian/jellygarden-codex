#!/usr/bin/env bash
# Render every Color Ball sequence / ball-space asset in parallel processes (Windows PowerShell 5.1 + Git Bash).
# usage: render_all.sh <source dir with render_seq.ps1 and the .cs files> <color_ball.png> <output dir>
# Output: <out>/reveal, idle, boost, coreflare, finishring (PNG masters) and <out>/logs.
set -u
SRC_DIR="${1:?source dir}"; ART="${2:?color_ball.png}"; OUT="${3:?output dir}"
W() { cygpath -w "$1"; }
mkdir -p "$OUT/reveal" "$OUT/idle" "$OUT/boost" "$OUT/coreflare" "$OUT/finishring" "$OUT/logs"
run() { # seq subdir range
  powershell -NoProfile -ExecutionPolicy Bypass -File "$(W "$SRC_DIR/render_seq.ps1")" -Dir "$(W "$SRC_DIR")" -Src "$(W "$ART")" \
    -Seq "$1" -Out "$(W "$OUT/$2")" -Frames "$3" > "$OUT/logs/$1_$3.txt" 2>&1
}
for r in 0-16 17-33 34-50 51-67; do run reveal reveal $r & done
for r in 0-19 20-39 40-59 60-79 80-99 100-119 120-173; do run boost boost $r & done
run idle idle 0-119 &
run coreflare coreflare 0-89 &
run finishring finishring 0-53 &
wait
for s in reveal idle boost coreflare finishring; do echo "$s $(ls "$OUT/$s" | wc -l)"; done
grep -L "render ms" "$OUT"/logs/*.txt | while read f; do echo "FAILED: $f"; cat "$f"; done

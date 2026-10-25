#!/usr/bin/env bash
#
# Record draw lists from fixed cameras and compare two recordings, to show that a shell change
# draws exactly what the build before it drew.
#
# Usage:
#   scripts/compare-draws.sh record DIR [--save FILE] [--verify]
#   scripts/compare-draws.sh diff BASE NEW
#
# Record once on the baseline build and once on the changed build, then diff the two folders.
# Recording uses whatever assemblies scripts/build-shell.sh last built; use --optimized for a
# 1M save. The runs go in parallel and each opens a window, because a draw list needs a display.
#
#   small   25,000 Citizens, one camera, Tick 300
#   verify  25,000 Citizens with BOROUGH_RENDER_VERIFY=1: camera moves, a Street, two draws
#   cams    the save, four cameras from far to near (needs --save)
#   run     the save running at speed 8 until Tick 600 (needs --save)
#
# Traveller and car rows are left out of the comparison because their positions depend on frame
# timing.

set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
godot="${GODOT_BIN:-godot}"

usage() { sed -n '6,7p' "$0" | sed 's/^# *//' >&2; exit 2; }

record() {
  local out save='' verify=0
  out="$(mkdir -p "$1" && cd "$1" && pwd)"; shift
  while [ $# -gt 0 ]; do
    case "$1" in
      --save) save="$(realpath "$2")"; shift 2 ;;
      --verify) verify=1; shift ;;
      *) usage ;;
    esac
  done

  cat > "$out/small.drive" <<EOF
262 focus 120 120 900
300 pause
300 draw $out/small.tsv
300 quit
EOF
  cat > "$out/verify.drive" <<EOF
262 focus 120 120 900
270 focus 120 120 300
280 focus 600 120 300
290 focus 600 400 700
300 hold street
300 click 600 416
300 click 600 448
310 draw $out/verify-a.tsv
320 focus 120 120 300
330 draw $out/verify-b.tsv
330 quit
EOF
  cat > "$out/cams.drive" <<EOF
257 draw $out/cams-a.tsv
262 focus 1875 1875 700
265 draw $out/cams-b.tsv
271 focus 3000 1875 700
274 draw $out/cams-c.tsv
281 focus 1875 1875 300
284 draw $out/cams-d.tsv
284 quit
EOF
  cat > "$out/run.drive" <<EOF
257 focus 1875 1875 700
257 speed 8
600 pause
600 draw $out/run.tsv
600 quit
EOF

  rm -f "$out"/*.tsv "$out"/*.f
  cd "$root"
  local shell=("$godot" --path src/Borough.Godot --)
  timeout 900 "${shell[@]}" --citizens 25000 --drive "$out/small.drive" > "$out/small.log" 2>&1 &
  if [ "$verify" = 1 ]; then
    BOROUGH_RENDER_VERIFY=1 timeout 900 "${shell[@]}" --citizens 25000 --drive "$out/verify.drive" \
      > "$out/verify.log" 2>&1 &
  fi
  if [ -n "$save" ]; then
    timeout 900 "${shell[@]}" --load "$save" --drive "$out/cams.drive" > "$out/cams.log" 2>&1 &
    timeout 900 "${shell[@]}" --load "$save" --drive "$out/run.drive" > "$out/run.log" 2>&1 &
  fi
  wait

  for t in "$out"/*.tsv; do
    [ -e "$t" ] || break
    grep -v -P '^(layer|retained|row)\t(traveller|car)\t' "$t" > "${t%.tsv}.f" || true
  done
  local failed=0
  for log in "$out"/*.log; do
    if grep -q -i exception "$log"; then
      echo "$(basename "$log"): $(grep -c -i exception "$log") exception lines" >&2
      failed=1
    fi
  done
  ls "$out"/*.tsv 2>/dev/null || { echo "no draw lists were written; see $out/*.log" >&2; exit 1; }
  return "$failed"
}

diff_recordings() {
  local base="$1" new="$2" differs=0 f name
  for f in "$base"/*.f; do
    name="$(basename "$f" .f)"
    if [ ! -e "$new/$name.f" ]; then
      echo "$name MISSING"; differs=1
    elif cmp -s "$f" "$new/$name.f"; then
      echo "$name IDENTICAL"
    else
      echo "$name DIFFERS ($(diff "$f" "$new/$name.f" | grep -c '^[<>]') lines)"; differs=1
    fi
  done
  return "$differs"
}

case "${1:-}" in
  record) [ $# -ge 2 ] || usage; shift; record "$@" ;;
  diff)   [ $# -eq 3 ] || usage; diff_recordings "$2" "$3" ;;
  *) usage ;;
esac

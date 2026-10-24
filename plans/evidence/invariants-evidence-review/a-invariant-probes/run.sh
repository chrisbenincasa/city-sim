#!/usr/bin/env bash
# Sweep 03 investigator A probes: copy the probe into a checkout, run it, remove it.
# Usage: run.sh [checkout] [filter]   (checkout defaults to this script's repo root)
set -euo pipefail
here="$(cd "$(dirname "$0")" && pwd)"
root="${1:-$(git -C "$here" rev-parse --show-toplevel)}"
filter="${2:-FullyQualifiedName~Borough.Tests.ReviewProbes.InvariantsProbeA}"
dest="$root/tests/Borough.Tests/ReviewProbes"
mkdir -p "$dest"
cp "$here/InvariantsProbeA.cs" "$dest/InvariantsProbeA.cs"
trap 'rm -f "$dest/InvariantsProbeA.cs"; rmdir "$dest" 2>/dev/null || true' EXIT
cd "$root"
timeout 600 scripts/test.sh --filter "$filter" -- --logger 'console;verbosity=detailed'

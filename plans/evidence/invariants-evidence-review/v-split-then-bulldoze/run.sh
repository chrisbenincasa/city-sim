#!/usr/bin/env bash
# Sweep 03 verifier probe: copy into a checkout, run, remove.
# Usage: run.sh [checkout] [filter]   (checkout defaults to this script's repo root)
set -euo pipefail
here="$(cd "$(dirname "$0")" && pwd)"
root="${1:-$(git -C "$here" rev-parse --show-toplevel)}"
filter="${2:-FullyQualifiedName~Borough.Tests.ReviewProbes.VerifierProbeA.V1}"
dest="$root/tests/Borough.Tests/ReviewProbes"
mkdir -p "$dest"
cp "$here/VerifierProbe.cs" "$dest/VerifierProbe.cs"
trap 'rm -f "$dest/VerifierProbe.cs"; rmdir "$dest" 2>/dev/null || true' EXIT
cd "$root"
timeout 600 scripts/test.sh --filter "$filter" -- --logger 'console;verbosity=detailed'

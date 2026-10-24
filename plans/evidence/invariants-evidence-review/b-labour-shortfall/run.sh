#!/usr/bin/env bash
# Reruns LabourShortfallProbe against a checkout. Usage: run.sh [checkout]  (default: this repo's root)
set -euo pipefail
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
root="${1:-$(git -C "$here" rev-parse --show-toplevel)}"
target="$root/tests/Borough.Tests/ReviewProbes"
mkdir -p "$target"
cp "$here/LabourShortfallProbe.cs" "$target/"
trap 'rm -f "$target/LabourShortfallProbe.cs"; rmdir "$target" 2>/dev/null || true' EXIT
cd "$root"
timeout 600 scripts/test.sh --filter 'FullyQualifiedName~LabourShortfallProbe' -- --logger 'console;verbosity=detailed'

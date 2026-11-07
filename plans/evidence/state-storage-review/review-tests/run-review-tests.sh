#!/usr/bin/env bash
# Run from a repository root. Copies the review tests into tests/Borough.Tests/Review/, runs them
# through scripts/test.sh, then removes them. Extra arguments go to scripts/test.sh after --filter.
# A failing test is the defect reproducing; see plans/state-storage-review.md for expected results.
set -u
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
root="$(pwd)"
[ -f "$root/scripts/test.sh" ] || { echo "run from the repository root"; exit 2; }
dest="$root/tests/Borough.Tests/Review"
mkdir -p "$dest"
cp "$here"/*.cs "$dest"/
trap 'rm -rf "$dest"' EXIT
filter="FullyQualifiedName~Borough.Tests.Review"
[ $# -gt 0 ] && filter="$filter&($1)"
"$root/scripts/test.sh" --filter "$filter"

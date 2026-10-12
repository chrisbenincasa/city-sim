#!/usr/bin/env bash
# Runs the founding log against a varied copy of rulesets/base/.
#
# usage: tune.sh NAME DAYS "key=value ..."
#   Keys are listed in vary.py. Output goes to $TUNING_OUT/NAME (default: ./out/NAME):
#   run.tsv is the probe's daily table and err.txt its diagnostics.
#
# Needs Release builds of Borough.Headless and FoundingProbe:
#   dotnet build -c Release src/Borough.Headless && dotnet build -c Release spikes/FoundingProbe
set -euo pipefail

here=$(cd "$(dirname "$0")" && pwd)
repo=$(cd "$here/../../.." && pwd)
out=${TUNING_OUT:-$here/out}/$1
headless=$repo/src/Borough.Headless/bin/Release/net10.0/Borough.Headless.dll
probe=${PROBE:-$repo/spikes/FoundingProbe/bin/Release/net10.0}/FoundingProbe.dll

rm -rf "$out"
mkdir -p "$out"
cp "$repo"/rulesets/base/*.toml "$out"/
echo "$3" > "$out/vary.txt"
VARY="$3" python3 -I "$here/vary.py" "$out"

key=$(dotnet "$headless" --save-key --ruleset "$out/ruleset.toml" | cut -d' ' -f1)
sed "s/^ruleset 0x.*/ruleset 0x$key/" "$repo/rulesets/base/founding.borough" > "$out/founding.borough"

dotnet "$probe" "$out/ruleset.toml" "$out/founding.borough" "$2" > "$out/run.tsv" 2> "$out/err.txt" || true
tail -1 "$out/err.txt"

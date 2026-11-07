#!/usr/bin/env bash
# Run from a repository root. Copies each ReviewProbe_*.cs into src/Borough.Core/, builds Core,
# reports whether a BOR diagnostic fired, and removes the probe. A gap prints
# "BUILD OK, no BOR diagnostic"; the control must print a BOR id.
set -u
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
root="$(pwd)"
[ -f "$root/src/Borough.Core/Borough.Core.csproj" ] || { echo "run from the repository root"; exit 2; }

dotnet build "$root/src/Borough.Core" -nologo -v q >/dev/null 2>&1 || { echo "baseline build fails; aborting"; exit 2; }

# Optional arguments name a subset of probes (without .cs).
if [ $# -gt 0 ]; then set -- $(for n in "$@"; do echo "$here/$n.cs"; done); else set -- "$here"/ReviewProbe_*.cs; fi
for probe in "$@"; do
  name="$(basename "$probe" .cs)"
  target="$root/src/Borough.Core/$name.cs"
  cp "$probe" "$target"
  out="$(dotnet build "$root/src/Borough.Core" -nologo -v q 2>&1)"
  status=$?
  rm -f "$target"
  ids="$(printf '%s\n' "$out" | grep -oE 'error BOR[0-9]{4}' | awk '{print $2}' | sort -u | paste -sd, -)"
  if [ -n "$ids" ]; then
    echo "$name: fired $ids"
  elif [ $status -eq 0 ]; then
    echo "$name: BUILD OK, no BOR diagnostic"
  else
    echo "$name: BUILD FAILED without BOR (probe does not compile):"
    printf '%s\n' "$out" | grep -E 'error' | sort -u | head -3 | sed 's/^/    /'
  fi
done

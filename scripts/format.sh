#!/usr/bin/env bash
#
# Format the C# in this repository, or check that it is already formatted.
#
# `dotnet format` ships inside the SDK named in global.json, so this adds no
# dependency and needs no adr/0018 exception. It is configured entirely by
# .editorconfig at the repository root.
#
# Usage:
#   scripts/format.sh           # apply the formatting
#   scripts/format.sh --check   # report what would change; exit non-zero if any
#   scripts/format.sh ... -- <args>   # anything after -- goes to `dotnet format`
#
# TWO TARGETS, NOT ONE, and the second is the reason this script exists rather
# than a line in CLAUDE.md. Borough.slnx does not list src/Borough.Godot, so
# `dotnet format Borough.slnx` reaches exactly three of that project's files --
# CitySave.cs, CityPreparation.cs and SimulationThread.cs, which are there only
# because tests/Borough.Tests/Borough.Tests.csproj <Compile Include>s them. The
# other twenty-odd files in the shell are invisible to it. A formatter that
# silently skips a project is worse than no formatter, because the gate goes
# green over code it never read.
#
# The shell restores and formats with NO GODOT INSTALLED: Godot.NET.Sdk comes
# from NuGet like any other SDK. That matters because the whole point of the
# split is that the headless side never requires the editor, and a format gate
# that needed one would be a gate CI could not run.
#
# WHITESPACE AND STYLE, NEVER ANALYZERS. `dotnet format analyzers` fixes the
# CAxxxx/IDExxxx diagnostics, and this repository already fails the BUILD on
# every one of them -- Directory.Build.props sets TreatWarningsAsErrors with
# AnalysisLevel latest-recommended. Running the fixer would be a second, slower
# copy of a gate that already exists and already blocks.

set -uo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

mode=apply
passthrough=()

while [ $# -gt 0 ]; do
  case "$1" in
    --check) mode=check; shift ;;
    --)      shift; passthrough=("$@"); break ;;
    *)       echo "unknown argument: $1" >&2; exit 2 ;;
  esac
done

targets=(
  "Borough.slnx"
  "src/Borough.Godot/Borough.Godot.csproj"
)

args=()
[ "$mode" = check ] && args+=(--verify-no-changes)
args+=("${passthrough[@]+"${passthrough[@]}"}")

echo "mode:    $mode"
echo "targets: ${targets[*]}"
echo

status=0

for target in "${targets[@]}"; do
  for subcommand in whitespace style; do
    echo "── $subcommand $target"
    # `|| status=$?` rather than `set -e`: a check must report EVERY target, not
    # the first one that fails. Reading one unformatted project and then being
    # told nothing about the other is how a second round trip gets paid for.
    ( cd "$root" && dotnet format "$subcommand" "$target" "${args[@]+"${args[@]}"}" ) || status=$?
  done
done

echo
echo "───────────────────────────────────────────────────────────"

if [ "$status" -ne 0 ]; then
  if [ "$mode" = check ]; then
    echo "NOT FORMATTED. Run scripts/format.sh to fix, then commit the result."
  else
    echo "dotnet format exited $status."
  fi
else
  echo "formatted."
fi

exit "$status"

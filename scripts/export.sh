#!/usr/bin/env bash
# Exports the shell for each named preset (Linux, Windows, macOS), or all three, into build/.
# The Rulesets and Style Presets are copied into the Godot project first so the export packs them.
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
project="$root/src/Borough.Godot"
godot="${GODOT:-godot}"
presets=("$@")
[ ${#presets[@]} -eq 0 ] && presets=(Linux Windows macOS)

bundled=(rulesets appearance)
unbundle() { for folder in "${bundled[@]}"; do rm -rf "${project:?}/$folder"; done; }
unbundle
trap unbundle EXIT
for folder in "${bundled[@]}"; do cp -r "$root/$folder" "$project/$folder"; done

"$godot" --headless --path "$project" --import

declare -A outputs=(
    [Linux]=build/linux/Borough.x86_64
    [Windows]=build/windows/Borough.exe
    [macOS]=build/macos/Borough.zip
)

for preset in "${presets[@]}"; do
    output="$root/${outputs[$preset]}"
    rm -rf "$(dirname "$output")"
    mkdir -p "$(dirname "$output")"
    "$godot" --headless --path "$project" --export-release "$preset" "$output"

    # Godot exits 0 when the C# build fails and writes a binary that cannot start.
    if [ "$preset" = macOS ]; then
        grep -q 'Borough.Godot.dll$' <(unzip -Z1 "$output")
    else
        ls "$(dirname "$output")"/data_Borough.Godot_*/Borough.Godot.dll >/dev/null
    fi || { echo "export.sh: $preset has no Borough.Godot.dll, so its C# build failed" >&2; exit 1; }
done

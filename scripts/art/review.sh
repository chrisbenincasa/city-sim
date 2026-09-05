#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/../.."
blender_bin="${BLENDER_BIN:-/Applications/Blender.app/Contents/MacOS/Blender}"
godot_bin="${GODOT_BIN:-godot}"
out="$PWD/artifacts/visual-study"
mkdir -p "$out"
export_module() {
  "$blender_bin" --background --factory-startup --python scripts/art/facade.py -- "$@"
  "$godot_bin" --headless --path src/Borough.Godot --editor --import
}
validate() {
  "$godot_bin" --headless --path src/Borough.Godot res://VisualStudy.tscn -- --validate-study "$@"
}
case "${1:-capture}" in
  rebuild)
    export_module > "$out/rebuild.log" 2>&1
    ;;
  verify)
    dotnet build src/Borough.Godot > "$out/build.log" 2>&1
    # Restore the baseline even when the edited asset fails its contract.
    trap 'export_module > "$out/restore.log" 2>&1' EXIT
    export_module --width 5 --wall 917968 > "$out/edit-export.log" 2>&1
    validate --expected-width 5 --expected-wall 917968 > "$out/edit-check.log" 2>&1
    export_module > "$out/restore.log" 2>&1
    trap - EXIT
    validate > "$out/baseline-check.log" 2>&1
    ;;
  capture)
    "$godot_bin" --path src/Borough.Godot res://VisualStudy.tscn -- --capture-study "$out" > "$out/capture.log" 2>&1
    python3 scripts/art/gallery.py
    ;;
  material-rich)
    export_module --treatment material-rich > "$out/material-rich-export.log" 2>&1
    dotnet build src/Borough.Godot > "$out/material-rich-build.log" 2>&1
    validate --study-treatment material-rich > "$out/material-rich-check.log" 2>&1
    "$godot_bin" --path src/Borough.Godot res://VisualStudy.tscn -- --capture-study "$out/material-rich-baseline" > "$out/material-rich-baseline.log" 2>&1
    "$godot_bin" --path src/Borough.Godot res://VisualStudy.tscn -- --study-treatment material-rich --capture-study "$out/material-rich" > "$out/material-rich-capture.log" 2>&1
    python3 scripts/art/compare.py
    ;;
  kit)
    "$blender_bin" --background --factory-startup --python scripts/art/kit.py > "$out/kit-export.log" 2>&1
    "$godot_bin" --headless --path src/Borough.Godot --editor --import > "$out/kit-import.log" 2>&1
    dotnet build src/Borough.Godot > "$out/kit-build.log" 2>&1
    "$godot_bin" --path src/Borough.Godot res://KitStudy.tscn -- --capture-kit "$out/kit" > "$out/kit-capture.log" 2>&1
    python3 scripts/art/kit-gallery.py
    ;;
  extremes)
    "$blender_bin" --background --factory-startup --python scripts/art/kit.py -- --extremes > "$out/extremes-export.log" 2>&1
    "$godot_bin" --headless --path src/Borough.Godot --editor --import > "$out/extremes-import.log" 2>&1
    dotnet build src/Borough.Godot > "$out/extremes-build.log" 2>&1
    "$godot_bin" --path src/Borough.Godot res://KitStudy.tscn -- --kit-extremes --capture-kit "$out/extremes" > "$out/extremes-capture.log" 2>&1
    python3 scripts/art/extremes-gallery.py
    ;;
  styles)
    "$blender_bin" --background --factory-startup --python scripts/art/kit.py -- --styles > "$out/styles-export.log" 2>&1
    "$godot_bin" --headless --path src/Borough.Godot --editor --import > "$out/styles-import.log" 2>&1
    dotnet build src/Borough.Godot > "$out/styles-build.log" 2>&1
    "$godot_bin" --path src/Borough.Godot res://KitStudy.tscn -- --kit-styles --capture-kit "$out/styles" > "$out/styles-capture.log" 2>&1
    python3 scripts/art/styles-gallery.py
    ;;
  models)
    "$blender_bin" --background --factory-startup --python scripts/art/model-round.py > "$out/models-export.log" 2>&1
    "$godot_bin" --headless --path src/Borough.Godot --editor --import > "$out/models-import.log" 2>&1
    dotnet build src/Borough.Godot --no-restore > "$out/models-build.log" 2>&1
    "$godot_bin" --path src/Borough.Godot res://KitStudy.tscn -- --kit-models --capture-kit "$out/models" > "$out/models-capture.log" 2>&1
    python3 scripts/art/models-gallery.py
    ;;
  palettes)
    dotnet build src/Borough.Godot --no-restore > "$out/palettes-build.log" 2>&1
    "$godot_bin" --path src/Borough.Godot res://KitStudy.tscn -- --kit-palettes --capture-kit "$out/palettes" > "$out/palettes-capture.log" 2>&1
    "$godot_bin" --path src/Borough.Godot res://KitStudy.tscn -- --kit-palettes --palette-tower-city-builder --capture-kit "$out/palettes" > "$out/palettes-tower-capture.log" 2>&1
    python3 scripts/art/palettes-gallery.py
    ;;
  expanded)
    "$blender_bin" --background --factory-startup --python scripts/art/expanded-kit.py > "$out/expanded-export.log" 2>&1
    "$godot_bin" --headless --path src/Borough.Godot --editor --import > "$out/expanded-import.log" 2>&1
    dotnet build src/Borough.Godot --no-restore > "$out/expanded-build.log" 2>&1
    "$godot_bin" --path src/Borough.Godot res://ExpandedStudy.tscn -- --capture-expanded "$out/expanded" > "$out/expanded-capture.log" 2>&1
    python3 scripts/art/expanded-gallery.py
    ;;
  construction)
    "$blender_bin" --background --factory-startup --python scripts/art/construction-study.py > "$out/construction-export.log" 2>&1
    "$godot_bin" --headless --path src/Borough.Godot --editor --import > "$out/construction-import.log" 2>&1
    dotnet build src/Borough.Godot --no-restore > "$out/construction-build.log" 2>&1
    "$godot_bin" --path src/Borough.Godot res://ExpandedStudy.tscn -- --construction --capture-expanded "$out/construction" > "$out/construction-capture.log" 2>&1
    python3 scripts/art/construction-gallery.py
    ;;
  fidelity)
    "$blender_bin" --background --factory-startup --python-exit-code 1 --python scripts/art/fidelity-study.py > "$out/fidelity-export.log" 2>&1
    "$godot_bin" --headless --path src/Borough.Godot --editor --import > "$out/fidelity-import.log" 2>&1
    dotnet build src/Borough.Godot --no-restore > "$out/fidelity-build.log" 2>&1
    "$godot_bin" --path src/Borough.Godot res://ExpandedStudy.tscn -- --fidelity --capture-expanded "$out/fidelity" > "$out/fidelity-capture.log" 2>&1
    python3 scripts/art/fidelity-gallery.py
    ;;
  open-fidelity)
    "$godot_bin" --path src/Borough.Godot res://ExpandedStudy.tscn -- --fidelity
    ;;
  brick-scale)
    dotnet build src/Borough.Godot --no-restore > "$out/brick-scale-build.log" 2>&1
    "$godot_bin" --path src/Borough.Godot res://ExpandedStudy.tscn -- --fidelity --brick-scale .75 --capture-expanded "$out/brick-scale/smaller" > "$out/brick-scale-smaller.log" 2>&1
    "$godot_bin" --path src/Borough.Godot res://ExpandedStudy.tscn -- --fidelity --brick-scale 1.25 --capture-expanded "$out/brick-scale/larger" > "$out/brick-scale-larger.log" 2>&1
    python3 scripts/art/brick-scale-gallery.py
    ;;
  neighbourhood)
    "$blender_bin" --background --factory-startup --python-exit-code 1 --python scripts/art/neighbourhood.py > "$out/neighbourhood-export.log" 2>&1
    "$godot_bin" --headless --path src/Borough.Godot --editor --import > "$out/neighbourhood-import.log" 2>&1
    dotnet build src/Borough.Godot --no-restore > "$out/neighbourhood-build.log" 2>&1
    "$godot_bin" --path src/Borough.Godot res://ExpandedStudy.tscn -- --neighbourhood --capture-expanded "$out/neighbourhood" > "$out/neighbourhood-capture.log" 2>&1
    python3 scripts/art/neighbourhood-gallery.py
    ;;
  open-neighbourhood)
    "$godot_bin" --path src/Borough.Godot res://ExpandedStudy.tscn -- --neighbourhood
    ;;
  open-expanded)
    "$godot_bin" --path src/Borough.Godot res://ExpandedStudy.tscn
    ;;
  open-palettes)
    "$godot_bin" --path src/Borough.Godot res://KitStudy.tscn -- --kit-palettes
    ;;
  open-models)
    "$godot_bin" --path src/Borough.Godot res://KitStudy.tscn -- --kit-models
    ;;
  open-styles)
    "$godot_bin" --path src/Borough.Godot res://KitStudy.tscn -- --kit-styles
    ;;
  open-extremes)
    "$godot_bin" --path src/Borough.Godot res://KitStudy.tscn -- --kit-extremes
    ;;
  open-kit)
    "$godot_bin" --path src/Borough.Godot res://KitStudy.tscn
    ;;
  open-rich)
    "$godot_bin" --path src/Borough.Godot res://VisualStudy.tscn -- --study-treatment material-rich
    ;;
  open)
    "$godot_bin" --path src/Borough.Godot res://VisualStudy.tscn
    ;;
  *) echo 'Usage: scripts/art/review.sh [rebuild|verify|capture|open|material-rich|open-rich|kit|open-kit|extremes|open-extremes|styles|open-styles|models|open-models|palettes|open-palettes|expanded|open-expanded|construction|fidelity|open-fidelity|brick-scale|neighbourhood|open-neighbourhood]' >&2; exit 2 ;;
esac

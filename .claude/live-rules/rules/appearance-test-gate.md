---
description: Gate appearance and shell-drawing commits on the appearance tests, not the full lane
globs: ["src/Borough.Appearance/**", "src/Borough.Godot/**", "scripts/art/**", "art/**", "appearance/**", "tests/Borough.Tests/Appearance/**"]
prompt: ["/\\b(commit|push|gate|test lane)\\b/i"]
priority: 30
---
- This rule overrides the CLAUDE.md gate for one case. If a commit touches only appearance code,
  shell drawing code, Blender scripts, art, appearance presets, their tests and Markdown, gate it
  with the narrow lane below instead of the full `scripts/test.sh`.
- The narrow lane runs the appearance tests plus every test file outside them that names
  `Borough.Appearance` or `Borough.Godot`. List those files with
  `grep -rlE --include='*.cs' 'Borough\.(Appearance|Godot)' tests | grep -v '/Appearance/'`.
  Then run
  `scripts/test.sh --filter 'tier!=instrument&(FullyQualifiedName~Appearance|FullyQualifiedName~<Class>|...)'`,
  with one `FullyQualifiedName~<Class>` for each listed file.
- Add `scripts/test.sh Corpus` when Markdown changed. Add `dotnet build src/Borough.Godot` when
  shell code changed, because the test project does not build the shell.
- Keep the full lane for any change to `Borough.Core`, `Borough.Formats`, `Borough.Headless`,
  Rulesets, Input Logs or golden fixtures. Keep it too for the shell files that
  `tests/Borough.Tests/Borough.Tests.csproj` compiles in: `CityTuning.cs`, `CityPreparation.cs`,
  `SimulationThread.cs` and `CityBudget.cs`. Keep it as well for anything that sends Commands
  or reads the Input Log.

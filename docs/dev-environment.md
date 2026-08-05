# Setting up a development environment

> Vocabulary in [`CONTEXT.md`](../CONTEXT.md). Project layout in
> [`05-technical-architecture.md` §1](05-technical-architecture.md). Build order in
> [`06-roadmap.md`](06-roadmap.md).
>
> Follow this top to bottom on a clean machine and you will end with a solution that builds, a
> test suite that is green, three guard tests holding the architectural boundaries, and a first
> task to start on. Every step ends with something you can check.

---

## What you are setting up, and why it is in two halves

[`adr/0001`](adr/0001-godot-and-csharp.md) puts it plainly: **two toolchains, one repository.**
The .NET solution and the Godot project build separately, and *the headless runner must never
require Godot to be installed.* That is not tidiness — it is the cheapest possible continuous
check that the `Borough.Core` boundary still holds.

So this guide has two tracks, and the split is load-bearing rather than presentational:

| Track | Needs | Covers |
|---|---|---|
| **A — the core toolchain** | .NET SDK, git | `Borough.Core`, `Borough.Tests`, `Borough.Headless`. **All of Phase 1 and most of Phase 2** |
| **B — the shell** | Godot 4.7 .NET build | `Borough.Godot`. Phase 0 spikes S1 and S3, then Phase 3 |

**Track A is the one you need now.** Phase 1 has no graphics in it at all and everything is
verifiable from a terminal. If you only have an hour, do Track A and stop — you will be able to
start real work at the end of it.

Track B matters sooner than the roadmap's Phase 3 suggests, because two of the three Phase 0
spikes are Godot spikes. But that code is explicitly throwaway, so nothing in Track B needs to be
right the first time.

---

## Decisions this guide bakes in

Three things the design corpus leaves open. Each is recorded here with its reasoning so that
changing it later is an informed decision rather than an archaeological dig.

**Target framework: `net10.0`, everywhere.** .NET 8 and .NET 9 both reach end of support on
**10 November 2026**; .NET 10 is LTS through November 2028. This is a multi-year solo project
that must be re-enterable cold after gaps of weeks, so starting on a runtime that is already
inside its final quarter is a false economy. The JIT improvements in 9 and 10 — escape analysis
extended to cover arrays, better struct promotion and devirtualisation — happen to land exactly
on the code shape [`adr/0036`](adr/0036-the-cores-language-is-a-separate-decision-and-it-is-csharp.md)
commits to.

*The risk, stated honestly:* Godot's .NET 10 support is de-facto rather than promised. GodotSharp
targets `net8.0` and Godot's position is that anything backwards-compatible works; community
reports have `net10.0` running from 4.6 onward. There is one known bug where Godot's managed host
mis-probes *shared-framework* assemblies such as `Microsoft.Extensions.*`
([godot#112701](https://github.com/godotengine/godot/issues/112701)). This project is structurally
unlikely to meet it, because [`adr/0003`](adr/0003-deterministic-integer-simulation.md) already
bans dependencies from the core and `Borough.Godot` is a thin shell. And the blast radius is the
shell alone — Core, Tests and Headless never load Godot's managed host at all, which is the whole
point of [`adr/0002`](adr/0002-simulation-is-an-engine-agnostic-library.md). Spike **S1** will
surface any incompatibility at the cheapest possible moment.

Note that [`adr/0003`](adr/0003-deterministic-integer-simulation.md) is what makes a modern
runtime safe here. Floats are banned because *"rounding and intrinsic selection vary with JIT,
target and optimisation level"* — an integer-and-Q16.16 simulation is immune to JIT changes by
construction, so a newer, more aggressive JIT cannot move a result.

**Directory layout: `src/`, `tests/`, `godot/`, `data/`.** Not specified anywhere in the corpus.
Conventional, and it keeps the Godot project — which owns its own `.godot/` cache, `project.godot`,
and import artifacts — from littering the root.

**IDE: JetBrains Rider.** Its Godot support is first-party and it debugs both halves.

---

# Track A — the core toolchain

## A1. Install the .NET 10 SDK

**Linux (Ubuntu/Debian).** The distro feeds lag, so use Microsoft's install script:

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh
/tmp/dotnet-install.sh --channel 10.0
```

Add it to your shell profile if the script installed to `~/.dotnet`:

```bash
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"
```

**macOS:** `brew install --cask dotnet-sdk` — check it gives you 10.x, otherwise use the script
above. **Windows:** `winget install Microsoft.DotNet.SDK.10`.

**Check:**

```bash
dotnet --list-sdks
```

You want a `10.0.x` line. Anything else and the rest of this guide will not build.

## A2. Create the repository skeleton

From the repository root.

```bash
dotnet new sln -n Borough

dotnet new classlib -n Borough.Core     -o src/Borough.Core
dotnet new console  -n Borough.Headless -o src/Borough.Headless
dotnet new xunit    -n Borough.Tests    -o tests/Borough.Tests

dotnet sln add src/Borough.Core src/Borough.Headless tests/Borough.Tests

dotnet add src/Borough.Headless reference src/Borough.Core
dotnet add tests/Borough.Tests  reference src/Borough.Core

mkdir -p data/rulesets
```

**Two things the SDK does that this guide originally did not predict**, both confirmed on
SDK 10.0.110:

- `dotnet new sln` emits **`Borough.slnx`**, the XML solution format, not `Borough.sln`. Harmless,
  and Rider reads it, but the filename appears in the `git add` in A6.
- The project templates **copy `TargetFramework`, `Nullable` and `ImplicitUsings` into every
  `.csproj`**, which is exactly the drift `Directory.Build.props` exists to prevent. Strip them from
  all three so each project carries only what is genuinely its own — `AllowUnsafeBlocks` on `Core`,
  `OutputType` on `Headless`, `IsPackable` and the packages on `Tests`. The build is identical
  either way; the point is that in six months there is one place to change the framework rather
  than four.

The templates also leave a `Class1.cs` and a `UnitTest1.cs` behind. Delete both — an empty project is
a clearer starting point than a placeholder you will wonder about in three weeks.

```bash
rm src/Borough.Core/Class1.cs tests/Borough.Tests/UnitTest1.cs
```

**Note what is deliberately absent: `Borough.Godot` is not in this solution yet, and not in this
track at all.** It is added in Track B. Until then, the constraint that the headless runner never
requires Godot is enforced by there being nothing to require.

**Check:**

```bash
dotnet build
```

## A3. Pin the SDK

A `global.json` at the root stops a future SDK from silently changing the build. Adjust the
version to whatever `dotnet --list-sdks` reported.

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

`latestFeature` accepts patch and feature-band updates within 10.0 but refuses to roll onto 11.0
without you saying so.

## A4. Shared build configuration

`Directory.Build.props` at the root. This is where the target framework and the language settings
live, once, rather than being copied into four `.csproj` files that will drift.

```xml
<Project>

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>

    <!-- Determinism: culture must never reach a number or a parse. adr/0003 -->
    <InvariantGlobalization>true</InvariantGlobalization>

    <!-- These rules fail silently and are violated by accident. docs/05 §4 -->
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest-recommended</AnalysisLevel>

    <Deterministic>true</Deterministic>
  </PropertyGroup>

</Project>
```

`InvariantGlobalization` earns its place specifically. Culture-sensitive parsing and formatting is
a genuine determinism hazard the moment a TOML Ruleset is read on a machine with a comma decimal
separator, and it is far cheaper to switch off globally now than to chase later.

`Borough.Core` needs one thing the others do not — `Span<T>`, `stackalloc` and the pointer work
that [`adr/0036`](adr/0036-the-cores-language-is-a-separate-decision-and-it-is-csharp.md) commits
to. Add to `src/Borough.Core/Borough.Core.csproj`:

```xml
  <PropertyGroup>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>
```

Now an `.editorconfig` at the root. **This one is not optional** — `TreatWarningsAsErrors`
combined with the recommended analysers turns CA1707 ("remove the underscores from member name")
into a build error, and CA1707 is flatly incompatible with the way test names are written. Without
this file the next step does not compile.

```ini
root = true

[*.cs]
indent_style = space
indent_size = 4
end_of_line = lf
insert_final_newline = true
charset = utf-8

[tests/**.cs]
# Test names are sentences. CA1707 governs public API surface, which this is not.
dotnet_diagnostic.CA1707.severity = none
```

**Check:** `dotnet build` still succeeds.

## A5. The guard tests

[`adr/0002`](adr/0002-simulation-is-an-engine-agnostic-library.md) requires the boundary check
*"from the first commit by the CI reference check, not retrofitted once it has already been
violated."* This is that, and it is worth writing before there is anything to catch — a lint that
starts green and stays green is free, and one added after the first violation is a refactor.

`tests/Borough.Tests/BoundaryTests.cs`:

```csharp
using System.Reflection;

namespace Borough.Tests;

/// <summary>
/// The three checks from docs/05 §4 that are enforceable by reflection alone.
/// The remainder are Roslyn analysers in Borough.Analysers, as of slice 3.
/// </summary>
public class BoundaryTests
{
    private static readonly Assembly Core = typeof(Borough.Core.AssemblyMarker).Assembly;

    /// <summary>Lint 1 — no Godot reference from Borough.Core, transitively. adr/0002</summary>
    [Fact]
    public void Core_does_not_reference_Godot()
    {
        var offenders = Core.GetReferencedAssemblies()
            .Where(a => a.Name!.StartsWith("Godot", StringComparison.Ordinal))
            .Select(a => a.Name!)
            .ToList();

        Assert.True(offenders.Count == 0,
            $"Borough.Core references {string.Join(", ", offenders)}. " +
            "adr/0002: the simulation is engine-agnostic and this boundary is the option " +
            "to abandon Godot at all.");
    }

    /// <summary>
    /// Lint 2, state half — no float or double in simulation state. adr/0003.
    /// Arithmetic is not covered here; that needs an analyser.
    /// </summary>
    [Fact]
    public void Core_has_no_floating_point_state()
    {
        var offenders =
            from type in Core.GetTypes()
            from field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic
                                       | BindingFlags.Instance | BindingFlags.Static)
            where field.FieldType == typeof(float)
               || field.FieldType == typeof(double)
               || field.FieldType == typeof(decimal)
            select $"{type.FullName}.{field.Name} ({field.FieldType.Name})";

        var found = offenders.ToList();

        Assert.True(found.Count == 0,
            $"Floating-point state in Borough.Core: {string.Join(", ", found)}. " +
            "adr/0003: integers and Q16.16 fixed-point only. A city sim needs zero " +
            "transcendental functions.");
    }

    /// <summary>
    /// adr/0002's second CI check — Core returns ids and numbers, never display text.
    /// The real leak vector was never `using Godot;`; it is a method that returns a
    /// formatted string because a panel wanted one.
    /// </summary>
    [Fact]
    public void Core_returns_no_human_readable_strings()
    {
        var offenders =
            from type in Core.GetExportedTypes()
            from method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance
                                         | BindingFlags.Static | BindingFlags.DeclaredOnly)
            where method.ReturnType == typeof(string)
            where !method.IsSpecialName            // property getters are checked separately
            select $"{type.Name}.{method.Name}";

        var found = offenders.ToList();

        Assert.True(found.Count == 0,
            $"Public string-returning members on Borough.Core: {string.Join(", ", found)}. " +
            "adr/0002: the shell owns every string a human reads, resolved through the Ruleset.");
    }
}
```

That references a marker type, which is also the only file `Borough.Core` needs to exist right
now. `src/Borough.Core/AssemblyMarker.cs`:

```csharp
namespace Borough.Core;

/// <summary>Anchors reflection over this assembly. Carries no behaviour.</summary>
public static class AssemblyMarker;
```

**Check:**

```bash
dotnet test
```

Three passing tests.

### Prove the boundary guard actually fails

A guard test you have never seen fail is a guard test you have no reason to believe. But the
obvious way to test it does not work, and the reason is worth understanding rather than
discovering:

```bash
dotnet add src/Borough.Core package GodotSharp
dotnet test --filter Godot        # still passes
```

**An unused package reference is elided by the compiler**, so it never reaches the assembly
metadata that `GetReferencedAssemblies()` reads. Add actual usage and it fires:

```bash
cat > src/Borough.Core/Leak.cs <<'EOF'
namespace Borough.Core;
public static class Leak
{
    public static int Thing() => (int)Godot.Mathf.Round(1.5f);
}
EOF
dotnet test --filter Godot        # Failed! Borough.Core references GodotSharp.
```

Then undo both:

```bash
rm src/Borough.Core/Leak.cs
dotnet remove src/Borough.Core package GodotSharp
```

So the guard's real semantics are **fires on use, not on an idle reference** — which is the
correct line, since a reference nothing touches cannot leak an engine type into the simulation.
It is worth knowing the guard has that shape, because "I added the reference and nothing
happened" otherwise reads as a broken test.

## A6. Ignore file and first commit

```bash
dotnet new gitignore
```

Then append the Godot and Rider entries, which the .NET template does not cover:

```gitignore
# Godot
.godot/
godot/.godot/
*.translation

# Rider
.idea/

# Local run artifacts
*.citysave
*.inputlog
```

**Check, and this is the important one:**

```bash
dotnet build && dotnet test
```

Both green, on a machine with no GPU and no Godot installed. That is
[`06-roadmap.md`](06-roadmap.md)'s definition of runnable, and it is an obligation every
milestone from here inherits.

```bash
git add CLAUDE.md CONTEXT.md docs plans src tests \
        Borough.slnx global.json Directory.Build.props .editorconfig .gitignore
git commit -m "Scaffold: four-project layout, boundary guards, net10.0"
```

**Track A is done.** You can now do all of Phase 1.

---

# Track B — the Godot shell

Needed for Phase 0 spikes **S1** and **S3**, and then not again until Phase 3 Milestone 11.

## B1. Install Godot 4.7 — the .NET build

The .NET build is a **separate download** from the standard one. The standard build has no C#
support at all, and the two look identical on the download page.

Get it from [godotengine.org/download](https://godotengine.org/download) — the variant is labelled
**.NET** (it was called "Mono" before 4.0). On Linux it is a zip containing an executable and a
`GodotSharp/` directory; keep them together.

```bash
mkdir -p ~/opt/godot
# unzip the downloaded .NET build here, then:
ln -sf ~/opt/godot/Godot_v4.7-stable_mono_linux.x86_64 ~/.local/bin/godot
```

Do **not** install the Flatpak or Snap build for this. Both sandbox the filesystem, which makes
finding your .NET SDK and your external editor unnecessarily difficult.

**Check:**

```bash
godot --version
```

Should report a version containing `.mono`. That suffix is the part that matters — without it you
have the wrong build and C# will silently not exist.

As a cross-check that you are on the right engine version at all, `GodotSharp` **4.7.1** is
published on NuGet; `dotnet add package GodotSharp` resolving to a 4.7.x is confirmation the
engine line exists and its .NET packages ship.

## B2. Create the Godot project

Godot generates its own `.csproj`, so this is done from Godot rather than from `dotnet new`.

1. Launch `godot`, choose **New Project**, and set the path to `godot/` inside this repository.
2. Renderer: **Forward+**. Low-poly is a permanent commitment
   ([`00-vision.md`](00-vision.md)) so nothing here is demanding, but Forward+ is what the
   `MultiMeshInstance3D` work in [`05 §10`](05-technical-architecture.md) assumes.
3. Create any C# script — `Project → Tools → C# → Create C# solution` if the menu offers it.
   This is what generates the `.csproj`.

Godot writes its own `.sln` next to the project. Delete it and fold the `.csproj` into the real
solution instead:

```bash
rm -f godot/*.sln
dotnet sln add godot/Borough.Godot.csproj
dotnet add godot/Borough.Godot.csproj reference src/Borough.Core
```

Rename the generated `.csproj` to `Borough.Godot.csproj` if Godot named it after the folder, and
update `project.godot`'s `dotnet/project/assembly_name` to match.

## B3. Move it to net10.0

Godot generates `<TargetFramework>net8.0</TargetFramework>`. The root `Directory.Build.props`
sets `net10.0`, but the generated file specifies it locally and therefore wins. Delete the
`TargetFramework` line from `godot/Borough.Godot.csproj` so it inherits.

While there, relax two of the strict settings — Godot's generated and imported code does not
survive `TreatWarningsAsErrors`, and fighting it buys nothing since no simulation logic lives here:

```xml
  <PropertyGroup>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <Nullable>disable</Nullable>
  </PropertyGroup>
```

**Check, and this is the step that validates the whole `net10.0` decision:**

```bash
dotnet build
```

then press **F5** in Godot and confirm the project runs with a C# script attached to a node and
printing from `_Ready()`. If it fails to launch with a `FileNotFoundException` naming a shared
framework assembly, you have hit
[godot#112701](https://github.com/godotengine/godot/issues/112701) — drop
`godot/Borough.Godot.csproj` back to `net8.0` and multi-target `Borough.Core` as
`net8.0;net10.0`. Record that you did, because it changes the reasoning above.

## B4. Confirm the boundary still holds

The single most valuable check in this document, and it takes one command:

```bash
dotnet build src/Borough.Headless
```

`Borough.Headless` must build and run with Godot uninstallable and unreferenced. If adding the
shell to the solution has made the headless runner depend on it, the boundary broke on day one.

---

# Track C — the editor

## C1. Rider

Install via [JetBrains Toolbox](https://www.jetbrains.com/toolbox-app/), which handles updates
and lets you keep versions pinned.

Open `Borough.slnx` — Rider picks up all five projects (the four of `05 §1`, plus `Borough.Analysers`).

**Godot integration:** install the **Godot** plugin from `Settings → Plugins`. It adds a run
configuration that launches the editor and attaches the debugger, which is the only comfortable
way to debug the shell.

**Point Godot back at Rider:** in the Godot editor, `Editor → Editor Settings → Dotnet → Editor`,
select **JetBrains Rider**. Now double-clicking a script in Godot opens it in Rider at the right
line.

**Check:** set a breakpoint in a `_Ready()` method, launch from Rider's Godot configuration, and
confirm it hits.

## C2. Two run configurations worth making now

- **`Headless`** — runs `src/Borough.Headless`. This is the primary interface for the whole of
  Phase 1 and most of Phase 2, so it is worth having on a keystroke.
- **`All tests`** — `dotnet test`. You will run this constantly; determinism work is unusually
  test-driven because the simulation is a pure function and can assert far more about itself than
  most games can.

---

## Verifying the whole thing

Everything below should hold before you consider the environment done.

```bash
dotnet --list-sdks | grep '^10\.'      # .NET 10 SDK present
dotnet build                            # whole solution, including the shell
dotnet test                             # three guard tests green
dotnet build src/Borough.Headless       # builds without touching Godot
godot --version | grep mono             # the .NET build, not the standard one
```

Plus, by hand:

- A breakpoint in the Godot shell is hit from Rider.
- `Core_does_not_reference_Godot` has been *seen to fail* when Godot is actually used from
  `Borough.Core` — see A5.

Everything in Track A above has been executed end to end on Ubuntu 24.04 with SDK 10.0.110:
`dotnet new` templates, `Directory.Build.props`, the three guard tests, and the deliberate
boundary violation. The xunit template resolves to xunit 2.9.3, which supplies `Xunit` as an
implicit using — which is why `BoundaryTests.cs` needs no `using Xunit;`.

---

## What to do first

From [`plans/0002-open-questions.md`](../plans/0002-open-questions.md): **spike S4 has no blockers
and should start now.** It is a six-kernel microbenchmark measuring the machine's response to the
shapes this design makes, at target row counts, with no simulation in it. It needs no Godot, no
engine, and none of the design questions still open — which is exactly why it is unblocked when
almost nothing else is.

Its **K6** kernel is the one that matters most: a ten-minute sustained run looking for a p99.9
pause beyond **15.6 ms**, the 4×-speed Tick budget. That figure is the named revisit trigger for
[`adr/0036`](adr/0036-the-cores-language-is-a-separate-decision-and-it-is-csharp.md) — the
decision to write the core in C# at all. Finding out early is the entire point.

Add `BenchmarkDotNet` to `Borough.Tests` when you get there:

```bash
dotnet add tests/Borough.Tests package BenchmarkDotNet
```

After S4, Phase 0's three Godot-and-routing spikes (S1, S2, S3) and then Milestone 1 — the tick
and determinism harness. [`06-roadmap.md`](06-roadmap.md) has the ordering and the risk each step
retires.

---

## What this guide does not settle

Open items that will need deciding, flagged here so they are not discovered as surprises.

| Item | Needed by | Note |
|---|---|---|
| **TOML parser library** | Milestone 3a | The format is fixed by [`adr/0015`](adr/0015-all-tuning-data-is-hot-reloadable.md) and [`adr/0018`](adr/0018-prefer-off-the-shelf-infrastructure.md); no library is named. `adr/0003` requires any core dependency be argued against it explicitly — a determinism liability needs a written exception |
| ~~**Roslyn analysers for lints 2, 3 and 7**~~ | ~~Milestone 2~~ | **Settled.** Built as slice 3 ([`plans/0006`](../plans/0006-analysers-and-lints.md)): `src/Borough.Analysers`, twelve diagnostics, referenced by `Borough.Core` as an analyser rather than a dependency. The A5 reflection guards below are kept — a transitive reference check is what reflection is good at |
| **CI runner** | Whenever the guards need to run unattended | Everything above is `dotnet test` on a machine with no GPU, so any runner works |
| **Chunk size** | Milestone 10 | Open question 3 in [`05 §11`](05-technical-architecture.md). Cheap now, expensive once save files exist |

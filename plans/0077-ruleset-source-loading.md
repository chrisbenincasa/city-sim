# 0077 — Ruleset source loading and the authoring guide

## Outcome

Implement the agreed [authoring contract](../docs/ruleset-authoring.md): scoped TOML source
files with shared definitions and explicit references, resolved into one validated Ruleset.
Deliver a usable authoring guide with the loader. State: design / unclaimed; the
[board](0000-board.md) owns scheduling. This plan records development work, not a completed loader.

## Decisions to finish

1. Select the entry point and explicit source membership syntax, portable path rules and typed
   definition schemas. Filenames in the guide are illustrative, not a selected module grammar.
2. Define deterministic declaration ordering, stable ids/display labels, source provenance and
   versioned bundle identity. Preserve the existing single-file path and content hashes.
3. Define the resolver output that Core can consume. Coordinate shared behaviour/profile support
   with its runtime implementation; refuse unsupported semantics rather than expand every
   combination silently. Prototype syntax is evidence, not an adopted API.
4. Define bundle retention and compatibility with CitySave/Input Logs, including old-source support
   and explicit upgrades. Coordinate schema/profile migrations with save compatibility work.

## Implementation sequence

1. Add the Formats source-set loader: collect declarations, resolve typed cross-file references,
   reject duplicate/unknown/unresolved definitions with source locations, then validate the whole
   candidate. No file-order override precedence or generic deep merging.
2. Add shared definitions, typed derivations and explicit overrides. Omission means inheritance.
   Emit source-attributed impact reports with before/after values, preserved exceptions, affected
   and unchanged dependants, expanded counts and limits. Keep actual gameplay decisions visible.
3. Integrate the shared host loading path and atomic candidate publication for registered reloads.
   Retain complete content bundles for saves/replays with deterministic membership and byte
   identity. Keep Core free of filenames, strings and parser dependencies.
4. Update this canonical guide with working commands and complete examples. Regenerate applicable
   schema/key-reference output from its source descriptions. Exercise the guide with a designer;
   do not require reading C# or editing generated Rules to perform ordinary content changes.

## Acceptance

- A small multi-file package loads through the real shared host path. Cross-file forward references
  work; duplicate ids name both files; unknown/missing definitions name their source location.
- Enumeration order cannot change resolution, runtime ordering or simulation results. Explicit
  overrides have the same meaning regardless of the containing file's position.
- A shared consumption edit and a storage exception change only their intended dependants. The
  report explains derived capacities, untouched exceptions and unsupported runtime features.
- A failed candidate load leaves the current Ruleset in force. Supported reloads are registered
  and logged; pinned old saves load without the original source directory. Replay and upgraded
  save continuations use the correct complete bundle on a compatible build.
- Existing single-file content and golden identities remain supported. Re-record only deliberate
  behaviour changes under the established golden procedure.
- The authoring walkthrough is reproducible from a clean checkout and is tried by a designer.
  Capture friction without silently completing the author's edits. Report source and expanded
  complexity separately; syntax validity is not a balance claim.

## Boundaries

Core profile selections, capacity resolution, shared behaviour execution and old-state migration
are separate runtime work identified by [0076](0076-ruleset-authoring-experiment.md). Work-dependent
production belongs to the private-production item. Full gameplay balance, a graphical editor,
mod discovery/distribution, a general DSL and arbitrary inheritance are outside this loader slice.
The loader may be developed alongside runtime work, but may not claim support for behaviour the
engine cannot execute. Finish by updating the authoring guide and its implementation-status text.

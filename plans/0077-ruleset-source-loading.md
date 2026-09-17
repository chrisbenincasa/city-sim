# 0077 — Ruleset source loading and the authoring guide

## Outcome

Implement the agreed [authoring contract](../docs/ruleset-authoring.md): scoped TOML source
files with shared definitions and explicit references, resolved into one validated Ruleset.
Deliver a usable authoring guide with the loader. State: design / unclaimed, with the Formats
foundation below implemented; the [board](0000-board.md) owns scheduling. This plan records
development work, not a completed loader.

## Agreed release decisions and remaining design

The canonical [source v1 contract](../docs/ruleset-authoring.md#organising-the-source) owns the
syntax and compatibility contract. The integrated release boundary, deterministic fractional
consumption and deferral of explicit identity/profile migration mappings are agreed. The format
is not implemented. The runtime mechanics are now scoped by
[the runtime factoring plan](ruleset-runtime-factoring.md), which also scopes per-instance
parameter variation out of this release.

1. `[source] version = 1` with explicit relative `members`; no includes, discovery or file-order
   overrides. Capture one immutable candidate. Existing execution sections remain available,
   with typed `id`/`label` and shallow basket, recipe and consumption-reserve references.
2. Allocate dense ids deterministically from typed source ids; explicit `order` controls Rules,
   Policies and Zone Rules. Preserve source spans and separate identity keys from display names.
   Frame source/resolver versions, manifest and sorted members for bundle identity; preserve the
   legacy hash path and its CRLF normalisation. Content identity is not semantic equivalence.
3. Formats owns typed resolution, provenance, previews, retained content and lowering into today's
   numeric Core Ruleset. Report per-kind Rule expansion; never generate kinds for reserve variants.
   Per-instance selections, production-based capacity and work-dependent execution require their
   separate runtime support and are refused meanwhile. No hidden fallback to fixed production or jobs.
4. A Formats bundle codec serves both hosts. CitySave envelope v2 embeds required bundles while
   retaining v1 reading; Input Log encoding and Core save bytes need no packaging-driven change.
   Keep active/checkpoint/replay dependencies and prune unreferenced content. Supported tuning
   uses existing logged migration; general alias/profile migration stays outside 0077.

The foundation below covers manifest capture, declaration collection and legacy compatibility.
Loader and runtime factoring remain separately implementable tasks,
but their integration is required for the first usable authoring release. Demonstrate shared
behaviour and reserve-derived capacities together, without a kind × profile expansion.
Work-dependent production remains separate gameplay work. Intermediate lowering into existing
Rules is development scaffolding, not completion of the authoring release.

Daily consumption must not require exact division across firings. Runtime factoring owns the saved
per-actor/per-Good fractional progress that makes this work; a loader may not round it away.

## Implemented foundation

This is a development slice in `Borough.Formats` and the two hosts. It does not complete step 1
below.

- **Capture.** `RulesetCapture.Read` reads the entry once; `[source]` (as a table, array or root
  key) selects the package reader and malformed or unsupported manifests are refused, never read
  as single files. It enforces version 1, exactly `version`/`members`, the portable path
  vocabulary, self-listing, duplicate and missing members, symbolic links, directories and
  strict UTF-8 with an optional BOM. `FromEntries` applies the same rules to an entry/byte
  collection with exact membership, except that its manifest arrives outside the member map, so a
  member there may carry the entry's own name. An empty member list captures.
- **Identity.** A package's `ContentHash` is the documented framed bundle over retained bytes.
  Single files keep `RulesetFile.HashOfContent`; its CRLF normalisation is now shared, unchanged.
- **Collection and diagnostics.** `RulesetSource.Resolve` collects every member's top-level
  declarations with locations before resolution: required `id` and its grammar, `label`, `order`
  only on Rules/Policies/Zone Rules, `name` refused where `id` replaces it, duplicate
  `(section, id)` naming both locations, one owning member per singleton, table/array conflicts,
  nested tables kept with their owner in one member, and declaration-only members. Diagnostics
  carry path, line, column, code and typed id, sorted by path, line, column and code.
- **Deterministic resolution.** Sections order by name; declarations by id, or `(order, id)` for
  Rules, Policies and Zone Rules. Dense ids follow source ids, identity keys fold the ids, and
  labels reach only `RulesetNames`. Enumeration permutations of the same capture give identical
  hashes, Rulesets and reports; moved declarations and reordered membership give identical
  Rulesets under different identities.
- **Lowering scaffolding.** Ordered declarations are re-emitted for `RulesetLoader` with
  line-preserving `id`→`name` and blanked `label`/`order` edits, so every existing field keeps its
  validation. Reader refusals map to member lines. `terrain` keeps its enum `name`; `hinterland`
  and `lattice` ids are collected but not lowered, and a test keeps that list aligned with the
  reader's key surface. `[[basket]]`, `[[recipe]]`, `[[reserve]]`, Rule `basket`/`recipe` and Bin
  `reserve` selections are refused as unimplemented.
- **Legacy compatibility.** Every shipped Ruleset resolves through `RulesetSource.Load` with its
  existing hash, refusal text and field-for-field Ruleset, and returns the reader's own result.
- **Bundle codec.** `RulesetBundle.Write` and `.Read` carry a capture as an entry-name/byte
  collection, so the host owns the directory or archive: `bundle.json` with envelope version, mode,
  identity, source/resolver versions and sorted members, then `source.toml` with `members/<path>`,
  or `ruleset.toml` alone under its legacy hash. A read recaptures through `FromEntries`, so
  membership, portable paths and UTF-8 are checked as a directory capture checks them; only
  self-listing differs, because the manifest is a separate entry rather than a file among the
  members. It refuses an
  unknown envelope, source or resolver version, a duplicate or undeclared entry, a member list
  disagreeing with the manifest, and content that does not fold to the recorded identity. The JSON
  spelling is not an identity input.
- **Headless loading.** `Session.TryRules` resolves through `RulesetSource`, so every dump command
  accepts a package without a call-site change, and `Session.TryCapture` returns the capture rather
  than a hash, so a package's framed identity reaches the catalogue, Input Log transitions and
  `SaveHeader.RulesetInForce` instead of the manifest's own legacy hash. A command captures once and
  resolves those same bytes, so an edit between two reads cannot record one identity against
  different Rules. `RulesetCheck` still sees
  identities before anything is parsed, so a supplied-Ruleset mismatch is still reported ahead of a
  parse refusal.
- **Shell loading and saving.** `Main` holds the capture rather than one TOML string, so the Godot
  shell boots a package through `RulesetSource` and folds its framed identity into the Input Log and
  the save header. `CitySave` envelope version 2 stores the bundle beside `world.save`; version 1
  saves still read, under their legacy hash. `CityTuning` offers every dial to every member, so a
  package keeps live tuning without knowing which member owns a key: a member that does not state
  the table comes back byte-identical, and a byte order mark and each line's own ending survive, so
  an untuned member cannot move the bundle identity. A tuned Ruleset is written out beside the Input
  Log — one file for a single-file Ruleset, a directory for a package — and the reproduce line
  names it.
- **Loading limits.** A manifest lists at most 256 members and neither a manifest nor a member may
  exceed 4 MiB, refused as `limit` during capture, so the bundle codec inherits the same bounds. An
  oversize member on disk is refused from its length, before its bytes are read.

Remaining work and dependencies, in addition to the sequence below:

1. `Main.Menu.cs` assigns the loaded save's path to `_rulesetPath`, so after a resume the tuner's
   parse label and the readout name the `.borough-city` archive rather than the Ruleset. A content
   catalogue holding several bundles waits on registered in-session reloads, which the shell's
   regenerating tuner is not. The drive channel has no verb for the tuner or for save and load, so
   those two paths are covered by test rather than by a driven run.
2. Reader refusals have no column, some quote the lowered `name` key, and typed reference errors
   come from the single-file reader rather than a typed resolver. Provenance/dependency edges,
   expansion counts, impact previews and old/new id-key collision refusal are not built.
3. Shared baskets, recipe references and reserve derivation (step 2) wait on the saved fractional
   consumption progress scoped in [the runtime factoring plan](ruleset-runtime-factoring.md).
   Integrated execution must replace the lowering before the first usable release.
4. Excluding special files such as FIFOs is deferred to Ruleset sharing and modding, which is where
   a package from outside the player's own checkout first arrives. .NET reports a FIFO as an
   existing regular file of length zero, with the same attributes and Unix mode as a plain file, so
   refusing one needs `stat` through P/Invoke and an `adr/0018` exception; without it a member that
   is a FIFO blocks the read until a writer opens the pipe. Schema/key-reference output does not yet
   describe `id`, `label`, `order` or the manifest, so `.taplo.toml` associates the schema with
   `rulesets/*.toml` only and package members get no editor hints; widen that glob when the schema
   can describe them. The authoring walkthrough and designer handoff remain.

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

- Integrate the separate runtime factoring work before the first usable authoring release. Show
  shared behaviour with reserve-derived capacities, local `days` overrides, and replay/save-load
  equivalence across affected owners, without multiplying kinds by profiles.

- A small multi-file package loads through the real shared host path. Cross-file forward references
  work; duplicate ids name both files; unknown/missing definitions name their source location.
- Enumeration order cannot change resolution, runtime ordering or simulation results. Explicit
  overrides have the same meaning regardless of the containing file's position.
- Reordering manifest membership or moving declarations preserves resolved execution but may
  change bundle identity and consequently State Hash provenance. Test those separately from
  enumeration permutations of the same captured bytes. Preserve legacy declaration ordering.
- A shared consumption edit and a reserve override change only their intended dependants. The
  report explains derived capacities, untouched exceptions and unsupported runtime features.
- A failed candidate load leaves the current Ruleset in force. Supported reloads are registered
  and logged; pinned old saves load without the original source directory. Replay and upgraded
  save continuations use the correct complete bundle on a compatible build.
- Existing single-file content and golden identities remain supported. Re-record only deliberate
  behaviour changes under the established golden procedure.
- Verify portable-path refusals, missing/duplicate members, singleton ownership, typed reference
  errors, integer derivation, id/label separation and reported runtime limits. Verify non-divisible
  daily consumption totals, zero-whole-unit intervals, shortage/recovery without duplicate accrual
  or unbounded debt, and save/load mid-fraction. Bundle read
  refuses unknown versions, missing content and identity mismatch; v1 saves still load. Keep
  over-capacity stock when testing a supported tuning upgrade across all affected Bin owners.
- The authoring walkthrough is reproducible from a clean checkout and is tried by a designer.
  Capture friction without silently completing the author's edits. Report source and expanded
  complexity separately; syntax validity is not a balance claim.

## Boundaries

Saved fractional consumption progress and reserve-derived capacity are separate runtime work,
scoped in [the runtime factoring plan](ruleset-runtime-factoring.md). Per-instance parameter
variation is out of scope there too, so no Ruleset surface here may assume it. Work-dependent
production belongs to the private-production item. Full gameplay balance, a graphical editor,
mod discovery/distribution, a general DSL and arbitrary inheritance are outside this loader slice.
The loader may be developed alongside runtime work, but may not claim support for behaviour the
engine cannot execute. Finish by updating the authoring guide and its implementation-status text.

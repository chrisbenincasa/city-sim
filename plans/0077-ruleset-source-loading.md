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
- **Typed references.** `RulesetSourceReferences` names every place one declaration states
  another's id — twenty-two of them, each with the section it resolves into, and `RulesetSource` matches every one across
  the whole package before lowering. A refusal names the key, the id, the target section and the
  column, so a Bin naming a Good nobody declares reports at the `resource` key rather than at the
  enclosing inline table. Resolution is a third stage and runs only on a package that collected
  cleanly, because a declaration whose own id was refused would otherwise draw a second refusal from
  every member naming it. A section's own table and the nested tables that stayed with it are both
  walked, so `[[hinterland.population]] stage` resolves and reports against its owning
  `[[hinterland]]`. `RulesetSourceReferenceTests` holds the list against `RulesetLoader.KeySurface`;
  a Rule's `fills` resource is held by name instead, because no shipped Ruleset writes it and the
  surface records only what a file asks for. ⚠ **One reference the list cannot express is a
  `[[basket]]`'s `use_per_day`**, whose keys are Resource ids chosen by the author rather than keys
  of the format. `RulesetSourceReference` has no syntax for *every key of this inline table*, so a
  package naming an undeclared Good there gets the reader's line-only refusal instead of a located
  package diagnostic with a column.
- **The section in a refusal.** `RulesetRefusal` carries an optional section and column, so
  `ToLoadResult` no longer drops what the source diagnostic knew and a `[[resource]]` duplicate stops
  reporting as `rule '<id>'`. The reader's own refusals record neither and keep their existing text
  exactly. `RulesetDiagnostic.ToRefusal` is the single conversion, and the headless runner uses it
  for a capture failure too, so the manifest stage and the resolve stage print one format.
- **Lowering scaffolding.** Ordered declarations are re-emitted for `RulesetLoader` with
  line-preserving `id`→`name` and blanked `label`/`order` edits, so every existing field keeps its
  validation. Reader refusals map to member lines. `terrain` keeps its enum `name`; `hinterland`
  and `lattice` ids are collected but not lowered, and a test keeps that list aligned with the
  reader's key surface. The three shared definitions are resolved rather than
  lowered: `[[basket]]`, `[[recipe]]` and `[[reserve]]` become term arrays and Bin ceilings in
  Formats, so nothing about them reaches the reader as a new key.
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
  spelling is not an identity input. The envelope also records the file name the entry was authored
  under, so a host that no longer has a path still has a name; it is the last path component only,
  refused on read if it carries a separator, and is not an identity input either.
- **Naming a resumed Ruleset.** A resumed city has no Ruleset path, so the readout and a driven
  run's `draw` file read `RulesetName()` off the capture rather than `_rulesetPath`, which is now
  the boot argument alone. `Main.Menu.cs` no longer assigns the `.borough-city` path to it.
- **Declaration keys in the generated references.** `RulesetSourceKeys` publishes `id`, `label` and
  `order` with their shapes and their sentences, because the resolver lowers them away before a
  reader sees a member and `RulesetLoader.KeySurface` therefore cannot record them. The schema, the
  key reference and `RulesetSchemaTests` all add them from there, so the two sides still agree by
  construction; `RulesetSourceKeys.Orders` is also the single owner of which sections accept an
  `order`. Each note says the key belongs to a package member, since a single-file Ruleset writes
  `name` and would be refused for writing `id`.
- **The walkthrough.** [The authoring contract](../docs/ruleset-authoring.md) walks
  `rulesets/split/` from either host through membership, identity and order, five refusals quoted
  from the runner, an in-session change, an impact preview, adding a Good and a recipe, sharing a
  basket, sizing a Bin from a reserve, making an exception, and a package city that saves and
  resumes. It states what this build refuses. `ExamplePackageTests` pins the package's members, its
  label and its Rule order; `WalkthroughEditTests` applies each content edit to the shipped package
  and holds the numbers the guide quotes, which are derived and therefore movable without anyone
  touching the guide. The commands themselves are still unheld and the document says so.
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
  an untuned member cannot move the bundle identity. A turn rewrites the value alone; the
  indentation, the spacing either side of the `=` and any trailing comment are the author's and
  survive it, and a dial on a commented line reads back without its comment. A tuned Ruleset is
  written out beside the Input Log — one file for a single-file Ruleset, a directory for a package — and the reproduce line
  names it.
- **Dependency edges.** `RulesetSourceResult.References` carries every reference the resolver
  matched — the declaring section and id, the key path, the target section and id, and the key's
  own location. `ResolveReferences` already walked them and threw them away. They are sorted by
  what they say rather than by where they were found, so a different member enumeration yields the
  same list, and they are empty for a single file and for a package refused before references
  resolve.
- **The impact preview.** `RulesetImpact.Between` compares two resolved candidates and reports what
  replacing one with the other would do. `--against PATH` is the third world-free runner mode and
  needs exactly one `--ruleset`.
  ⚠ **It compares the resolved Rulesets, not the authored text, and that is forced.** A
  `[[basket]]`, `[[recipe]]` and `[[reserve]]` expand inside `RulesetLoader` rather than in the
  package resolver, so two candidates can carry identical source for a Rule and still give it
  different terms. A text comparison would be blind to exactly the case the acceptance check names.
  `RulesetFields` flattens a Ruleset to its stored values by reflection, which is confined to this
  authoring path and never runs during a Tick; the walker `SourcePackage.AssertSameFields` already
  held is now that one implementation. Values are addressed by their owning declaration's typed
  source id, because a dense id moves when a declaration is inserted. Term and Bin pools are re-read
  through their owners and the offsets addressing them are suppressed, for the same reason: a pool
  index is not stable across an edit. Values belonging to no one declaration — a Resource's Need,
  import price and ceiling live in world-level tables rather than on the Resource — are reported
  against their field path rather than dropped.
  A value holding a dense id is reported as the id its author wrote, because a dense id moves when
  a declaration is inserted and `Resource.Raw 2 -> 4` asks the reader to count declarations.
  ⚠ **The old/new id-key collision refusal is written and unexercised.** A 64-bit
  `ContentHash.Of(UTF8(id))` collision is not constructible, so the guard has no test firing it;
  what is tested is that one id in two sections is two declarations and not a collision, which is
  `adr/0048`'s namespace separation.
- **Loading limits.** A manifest lists at most 256 members and neither a manifest nor a member may
  exceed 4 MiB, refused as `limit` during capture, so the bundle codec inherits the same bounds. An
  oversize member on disk is refused from its length, before its bytes are read.

Remaining work and dependencies, in addition to the sequence below:

1. A content catalogue holding several bundles waits on registered in-session reloads, which the
   shell's regenerating tuner is not: `Main.Panels.Regenerate` builds a fresh `Simulation` and a
   fresh `InputLogBuilder`, so a shell session only ever references one bundle and a multi-bundle
   store has no producer. `RulesetBundle` also writes fixed entry names, so a second bundle in one
   save needs a codec change and a CitySave envelope 3. The drive channel has no verb named for the
   tuner or for save and load, but its generic `ui key` verb synthesizes any key the shell binds,
   and the tuner, regenerate and record paths all dispatch from keys — so those paths are drivable
   and are covered by test as well.
2. Refusals the single-file reader raises on the lowered text still carry a line without a column and
   say `name` where a member writes `id`. The reader keeps every value check, shape check and
   `on_fail` cycle; the resolver has taken only the references. The preview reports no migration
   consequence, because it holds no world: stock, occupancy, jobs, governed values and Rule rearming
   are all unreported, and that half waits on a registered in-session reload having somewhere to
   run.
3. Shared baskets, recipe references and reserve derivation (step 2) are implemented under
   [the runtime factoring plan](ruleset-runtime-factoring.md), on the saved fractional consumption
   progress that plan scoped. Integrated execution must still replace the lowering before the first
   usable release.
4. Excluding special files such as FIFOs is deferred to Ruleset sharing and modding, which is where
   a package from outside the player's own checkout first arrives. .NET reports a FIFO as an
   existing regular file of length zero, with the same attributes and Unix mode as a plain file, so
   refusing one needs `stat` through P/Invoke and an `adr/0018` exception; without it a member that
   is a FIFO blocks the read until a writer opens the pipe. `.taplo.toml` now gives package
   members the Ruleset schema through `rulesets/*/*.toml` and a manifest the hand-written
   `rulesets/manifest.schema.json` through `rulesets/*/ruleset.toml`, resolved by Taplo's
   last-match rule. ⚠ A manifest's filename is not reserved, so that association is a convention
   and a package entry file named otherwise is completed as a Ruleset. The schema is authored
   rather than generated, because `[source]` accepts exactly `version` and `members` and a
   generator over two keys re-renders the list rather than deriving it;
   `RulesetManifestSchemaTests` drives each key, the version constant and the member limit through
   the resolver. The authoring walkthrough covers what this build does — loading a package from
   either host, membership, identity and order, the five common refusals, `--reload-at`, the impact
   preview, adding a Good and a recipe, sharing a basket, sizing a Bin from a reserve, making an
   exception, and a package city that saves and resumes. What it still cannot walk through is an
   inhabited city's evolution, which waits on a reload that reports what a transition would do to
   one. The designer handoff remains.

## The last step needs a person

Everything an agent can finish is finished. What is left is acceptance step 4 and its acceptance
check — a designer works the walkthrough from a clean checkout, on a machine that runs the headless
runner and the Godot shell, while someone watches. No agent can stand in for that. The point is to
find where the guide misleads, and an agent that already knows the answer cannot be misled by it.

Rules for that session:

- The designer drives. Do not complete their edit, do not correct their wording, do not answer a
  question the guide should have answered. Note the question and let them keep going.
- Record the friction as it happens: which step they stalled on, what they expected, what the runner
  said instead, and how long each step took.
- A refusal they could not act on is a defect in the message, not in the designer.
- Fix the guide afterwards, not during.

Hold the plan open until that session has run. `WalkthroughEditTests` holds the numbers the guide
quotes, so a stale number fails the suite, but nothing holds the commands or the prose. That is the
gap a person closes.

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

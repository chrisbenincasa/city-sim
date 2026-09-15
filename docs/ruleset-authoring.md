# Authoring the base-game Ruleset

This is the canonical authoring design and guide for the base-game Ruleset. The intended source
is a set of well-scoped TOML files with shared domain definitions and explicit references,
assembled into one complete Ruleset before simulation. Designers author choices and relationships;
the loader resolves references, derives quantities and reports consequences.

**The release decisions below are agreed; runtime mechanics and the saved-selection schema
still need design. The source format is only partly implemented.** `RulesetSource` in Formats
captures manifests, frames bundle identity, collects typed declarations with located diagnostics
and resolves them deterministically by lowering into the single-file reader. `RulesetBundle` writes
and reads the stored bundle described below. The headless runner loads a package through
`RulesetSource`; the Godot shell does not, no host stores a bundle yet, and shared baskets, recipe
references and storage selections are refused as unimplemented. The hosts' loader currently reads a single execution-oriented TOML document. The disposable prototype demonstrates
shared maintenance and impact reports; the factored representation and bakery contract establish
bounded design evidence. They are not production loaders or substitutes for Core mechanics.
[Plan 0077](../plans/0077-ruleset-source-loading.md) captures loader and guide implementation;
the [board](../plans/0000-board.md) owns scheduling and the separate runtime/gameplay work.

## First usable release

The loader and runtime factoring can be developed as separate tasks. Their integration is a
requirement of the first usable authoring release: demonstrate shared behaviour and distinct
saved storage selections together, including sparse exceptions and replay/save-load equivalence,
without manufacturing kinds for profiles. Parsing, diagnostics and bundle retention can start
independently. Work-dependent production remains separate gameplay work. Daily consumption accepts
quantities that do not divide evenly across firings, using deterministic fractional progress.
Explicit membership and portable filenames remain engineering constraints. The first release
supports pinned loading and ordinary tuning; explicit id-rename and old-kind/profile migration
mappings are deferred. Structural conversions must not infer mappings or silently discard state.

## What designers define

| Intent | Author once | Derive or share |
|---|---|---|
| Household consumption | A named basket's Goods and daily quantities | Behaviour wherever that basket is referenced |
| Supply storage | Days of consumption or nominal production | Integer Bin capacities; make the throughput basis explicit |
| A deliberate exception | An explicit local override | Unrelated kinds continue inheriting; omission restores inheritance |
| Production | A recipe's actual input/output edges | Businesses referencing that recipe share its meaning |
| Different premises | The relevant geometry/tenancy and storage choices | Posts derive from floor allocation; a storage choice does not copy the whole kind |
| Employment and funding | Work/pay terms and distinct funding choices | Payroll needs and funding coverage, without forcing them to be equal |
| Economic connectivity | Owners, suppliers, buyers and payment counterparties | Missing-link diagnostics and a dependency/impact report |

A kind's identity, shared behaviour, storage selection and appearance are separate concerns.
Do not manufacture another behavioural kind merely to express a storage or cosmetic difference.
Keep individual Citizen decisions and actual per-entity stock: factoring definitions does not
remove the work needed to simulate actual relationships.

Use shallow named references and sparse explicit exceptions. Omitted overrides mean inheritance;
an author need not write an empty container to state that there are no exceptions. Do not add
arbitrary inheritance chains, executable expressions or a DSL to compensate for missing mechanics.
Equal current values do not merge independently named identities: they may need to diverge later.

## Organising the source

The entry point is a TOML manifest, conventionally `ruleset.toml`:

```toml
[source]
version = 1
members = ["world.toml", "goods.toml", "consumption.toml", "buildings.toml"]
```

The filename is not reserved. Both hosts pass this file through the shared Formats loader;
the presence of `[source]` selects the package reader. An entry without `[source]` follows the
unchanged single-file reader. A malformed or unsupported `[source]` is a refusal, never a
fallback to legacy parsing. The manifest contains only `[source]`, with exactly the keys above.
Members contain declarations, never another manifest. There are no globs, implicit neighbours,
nested includes or external packages. A possible directory is:

```text
base-game/
  ruleset.toml
  world.toml
  goods.toml
  consumption.toml
  recipes.toml
  businesses.toml
  storage.toml
  services.toml
```

Only listed members participate. Paths are relative to the manifest's directory and use `/`.
Require nonempty lower-case ASCII components containing letters, digits, `_`, `-` and `.`;
reject `.`/`..` components, leading `/`, backslashes, drive prefixes, empty components and
Windows reserved device basenames or trailing dots. Members end in `.toml`. Reject duplicate
paths, a manifest listing itself, and symlinks in member paths. Read regular files beneath
the package root; moving the whole directory must not change its meaning. This deliberately
small portable path vocabulary avoids case-folding and Unicode-normalisation differences.
Definition labels may contain Unicode. Membership order has no semantic precedence.

Capture the manifest and all members once as immutable UTF-8 bytes (an optional UTF-8 BOM is
accepted); parsing, hashing, previews and retention use that same capture. Reject invalid UTF-8.
A later disk edit requires another candidate capture. A capture is the candidate's identity;
it does not promise a filesystem transaction across an editor's concurrent writes. A manifest
lists at most 256 members, and neither a manifest nor a member may exceed 4 MiB. These bound what
a load will read; they are not a budget for how much content a game may have.

Start with the connected mechanisms required for a small settlement. Additional Goods, kinds,
recipes and services should add their actual dependencies rather than require every future
system upfront. Defaults must form a coherent starting scenario and remain inspectable. The
shipped demonstration Rulesets are fixtures, not a balanced base-game catalogue; a small file
that parses does not establish a complete founding loop.

## Loader and resolution contract

`Borough.Formats` owns parsing, name resolution, diagnostics and construction of the runtime
Ruleset. The host supplies source/package bytes; Core receives validated ids and numbers and
performs no filesystem discovery or TOML parsing. Both hosts must use the same loader path.

1. **Explicit membership.** Identify the complete source set from an entry point/package; avoid
   ambient working-directory or incidental file-discovery dependencies. Report missing members.
   Use the manifest and portable paths above.
2. **Stable identities.** Identities belong to typed definition namespaces and are separate from
   filenames and display labels. Reject duplicate identities with both source locations. Moving
   a definition between files must not silently create a new entity identity.
3. **References rather than general merging.** Collect declarations, resolve references across
   the complete source set, then validate. Forward references are fine. File order never grants
   override precedence; do not use last-file-wins or a generic deep merge. Explicit domain
   overrides remain local. Reject unknown keys and unresolved references with source locations.
4. **Typed derivation.** Resolve quantities with units, ownership and a declared basis. Show how
   storage follows consumption or nominal throughput. Refuse unsupported/unrepresentable
   semantics rather than substitute a superficially similar mechanism. Validate cycles according
   to their meaning; a legitimate recycling recipe graph is not an inheritance cycle.
5. **Deterministic assembly.** Produce stable definition ordering and runtime references independent
   of filesystem enumeration. Retain a mapping from resulting values/Rules to their authoring
   sources. Measure expanded definitions and actual relationships as well as input file size.
6. **Explain before applying.** Report changed/unchanged dependants, inherited values, explicit
   exceptions, derived values before/after, limits and proposed migration effects. Structural
   validity does not establish balance or semantic safety for occupied content.
7. **Complete replacement.** Read, resolve and validate the whole candidate before publishing it
   for a registered reload. A failed source load leaves the active Ruleset unchanged. A supported
   transition is then recorded through the existing Input Log/Ruleset catalogue boundary.

This contract permits several scoped TOML files to form one Ruleset; it does not imply that the
current loader already accepts the illustrated files or that all resolved behaviour can run in
Core today. Preserve the supported single-file path while introducing the new source format.

### Declaration schemas and references

Source v1 retains existing execution sections and their field types, units and validation from
`RulesetLoader` and the generated reference, with the following explicit additions. It does not
adopt the disposable prototype's plural table grammar. Singleton sections such as `[capacity]`
must have one owning file; even disjoint fragments of the same singleton are refused. Nested
tables stay with their owning declaration in one file; a second file cannot reopen it.

Every top-level array declaration has a required `id` and optional `label`. For declarations
whose existing `name` declares identity, `id` replaces `name`; `label` defaults to `id`. A `name`
that selects a closed enum (for example terrain type) retains that meaning. Anonymous arrays
such as lattice declarations also receive source ids. Nested entries keep their existing
parent-local structure. Ids are nonempty ASCII strings matching `[A-Za-z0-9_][A-Za-z0-9_.-]*`;
comparison is ordinal and case-sensitive. Restricting ids keeps identity independent of locale.
Existing single-file names remain unrestricted under their existing contract.

References use the target's id in the existing typed field: a Resource reference resolves only
in the Resource namespace, a Building reference only in Building kinds, and so on. Collect all
declarations before resolving references. Duplicate `(declaration type, id)` pairs are refused
even when their values agree. Unknown keys, wrong types and unresolved references fail the whole
candidate. Add these shallow shared definitions:

| Declaration | Fields beyond `id` / `label` | Meaning |
|---|---|---|
| `[[basket]]` | `owner`, `use_per_day` | `owner` is `premises`, `occupant` or `business`, matching Bin tenancy (`occupant` means Household stock). `use_per_day` maps Good Resource ids to positive integer units per actor per Day. |
| `[[recipe]]` | `inputs`, `outputs` | Existing typed Rule term arrays, amounts per application; retain scope and ownership/payment validation. Neither field implies free supply or labour. |
| `[[storage]]` | `days` | Positive integer Days of an explicitly selected consumption basis. A flat reusable definition; no parent profiles. |
| `[[rule]]` addition | `basket` or `recipe` | A typed reference. A basket supplies local consumption inputs; a recipe supplies its input/output terms. Keep the Rule's existing kind, rate, apply, failure and emission fields. |
| Building Bin addition | `storage` | An inline table `{ profile = "reserve", basket = "basic", days = 5 }`. `profile` and `basket` are required typed references; `days` is an optional local override. This replaces that Bin's literal `capacity`. |

For a basket Rule, explicit `inputs` and `outputs` are refused; for a recipe Rule, explicit
`inputs`/`outputs` are refused. `basket` and `recipe` are mutually exclusive. A basket Rule uses
fixed apply count one. Designers specify daily consumption without requiring it to divide evenly
across firings. Carry fractional progress deterministically per actor and consumed Good, using
integer quotient/remainder arithmetic with `TicksPerDay` as the denominator. For example, three
units per Day over eight equal successful intervals must consume three units in total, rather
than round each interval or reject the quantity. No floating-point arithmetic or silently changed
cadence is permitted. Checked arithmetic must refuse unrepresentable quantities, not fractions.

Fractional progress is runtime state: save and hash it and preserve replay and thread-count
equivalence. The runtime design must define when progress advances, zero-whole-unit firings,
atomic multi-Good consumption, shortage/wakeup retries and progress migration during tuning.
A retry must not count the same interval twice; starvation must not accumulate unbounded debt.
Keep Bin Rule waiting semantics and individual actors. Demonstrate both uninterrupted totals and
shortage/recovery, including save/load mid-fraction. These mechanics are part of the integrated
authoring release, not something a parser may approximate through rounded generated Rules.
Daily quantities remain nominal under successful execution; they do not guarantee delivered
consumption during shortages.
Recipes retain the explicitly authored apply semantics. No automatic purchasing Rule, price,
wage, grant or work condition is inferred from either reference.

The Bin's Resource must occur in the referenced basket and its owner must match the basket.
Capacity is that Good's `use_per_day × effective days`; effective Days are the local `days`
override when present, otherwise the shared profile's Days. Removing `days` restores inheritance.
All resulting capacities must fit existing Core bounds. Literal capacities remain supported.
Do not silently deduplicate an explicit consumption Rule and a basket Rule: existing conflicts
are refusals and the impact report shows every actual Rule attachment.

For example, these are proposed source v1 declarations, **not runnable with today's loader**:

```toml
[[basket]]
id = "basic"
owner = "occupant"
use_per_day = { food = 32, fuel = 16 }

[[storage]]
id = "reserve"
days = 3
```

An occupant Food Bin using `storage = { profile = "reserve", basket = "basic" }` has capacity
96. Adding `days = 5` there gives that Bin capacity 160; changing the shared profile no longer
changes that Bin. Changing basic Food use to 48 changes these capacities to 144 and 240:
the exception preserves Days, not a hidden absolute capacity.

Nominal production storage and saved instance profile selections require the separate runtime
factoring work; the first usable release must integrate saved selections as specified above.
Their source schema must be completed with that runtime contract before release. Work-dependent
recipes remain separate gameplay work. Until implemented, requests for these semantics must be
refused with the missing capability named; the research bakery's `work` or `selection` tables
must not parse as ignored metadata.

### Ordering, identity and diagnostics

Allocate dense runtime ids by ordinal source id within each declaration type, retaining Core's
reserved zero values and existing width limits. Stable identity is the typed source id, not the
dense slot. Where Core already accepts identity keys, use `ContentHash.Of(UTF8(id))`, matching
the existing name-key fold; namespace separation is supplied by the typed tables. Display labels
go only to Formats' name tables. Refuse distinct ids colliding within a key namespace, including
when comparing an old and replacement candidate; never resolve a collision by picking one.

Order top-level arrays by id, except `rule`, `policy` and `zone_rule`, which accept optional
nonnegative integer `order` (default zero) and sort by `(order, id)`. This exposes priority where
execution order can matter without deriving it from file placement. Keep nested sequences such
as recipe term arrays in authored order: atomic terms and fallback relationships must not be
rewritten as an optimisation. Sort map-shaped basket memberships by Resource id. Moving whole
declarations between members or reordering members leaves resolved execution unchanged. Editing
an id, explicit `order` or an ordered nested sequence may change execution; the preview says so.
The legacy reader retains its existing declaration order without canonical reordering.

Resolved values retain their declaration location, referenced definition locations and local
override location. Diagnostics include member path, one-based line and column, typed id and
reason. Duplicate errors cite both declarations; unresolved references cite the use; derived
overflow errors cite the basis and override. Sort diagnostics by path, line, column and diagnostic
code so a different file enumeration produces the same report. A failure has no usable runtime
candidate, while independent diagnostics can still be collected.

### Resolver output and impact previews

Formats produces an immutable candidate containing the captured source bundle and identity,
typed resolved declarations, provenance/dependency edges, runtime Ruleset, display names and
an expansion report. Parsing and resolution may produce diagnostics without a runtime Ruleset.
Core receives only its existing validated numeric Ruleset and content hash. It receives no
source paths, labels, parser nodes or managed dependency graph. Both hosts use this candidate
instead of separately reading, hashing and parsing files.

During implementation, source sharing may lower into existing Rule and Bin declarations, once per actual kind
attachment. Report that duplication and enforce the existing limits before publication. Do not
create another kind for a storage choice or emit a kind × profile product. The separate runtime
factoring item owns shared execution and saved selections; integration must replace this lowering
before the first usable authoring release. Develop the integrated resolver
behind the same typed source boundary, with explicit format/resolver versioning and equivalence
checks. Preserve each actor, Rule family, cadence, fallback and transaction scope.

Compare old/new candidates by typed id. Report added/removed definitions, label-only changes,
changed and unchanged dependants, effective values before/after, inherited values, preserved
exceptions, ordering changes, authored and expanded counts, limits and unsupported capabilities.
Include nominal daily consumption and each capacity's arithmetic. Without a live world this is
a static comparison, not a claim about stock, solvency or safe migration. With a world, preview
the existing migration consequences across Building, Household and Business owners, including
stock, occupancy/jobs, governed values and Rule rearming. Mark any uncomputed consequence as
unavailable. An unavailable required compatibility check prevents an upgrade, not pinned loading.

The preview is bound to the old and new content identities. Publication uses that exact candidate;
editing files invalidates the preview rather than changing what gets applied. Revalidate world
compatibility at the transition boundary. A refused candidate or transition changes neither the
active Ruleset nor its displayed names and is not recorded as a successful reload.

## Saved cities and evolving content

A content hash identifies exact content, not a promise never to edit the game. Existing CitySave
packages embed their single TOML document. They load pinned content on a compatible build;
changing today's file does not by itself invalidate them. Loading and upgrading are separate.

### Bundle identity and retention

Keep `RulesetFile.HashOfContent` unchanged for legacy content: it removes CR from CRLF pairs
before the existing `ContentHash.Of` fold; other bytes, including comments, remain significant.
Source v1 uses that same fold over a framed bundle, not a generated execution TOML document.
The framing is the ASCII bytes `Borough.RulesetBundle` followed by a zero byte, then unsigned
32-bit little-endian source version (1), resolver version (1) and member count. Next comes a
length-prefixed manifest byte string, then each member in ordinal path order: length-prefixed
UTF-8 relative path and length-prefixed content. Each byte-string length is an unsigned 64-bit
little-endian byte count. Apply only CRLF normalisation to manifest/member content before framing;
retain original bytes in the bundle. Paths have already passed the portable-path checks.
Reject lengths that cannot be represented or exceed the host's documented loading limits.

The entry's basename, absolute root, filesystem timestamps and archive entry order do not enter
the identity. Manifest bytes do, so reordering its member list changes content identity while
preserving resolved execution. Moving a declaration or changing a label also changes bundle
identity; it need not change runtime definitions. Because the content hash enters simulation
provenance, this is not a promise of equal State Hashes across different source bundles.
Enumeration permutations over the *same* captured bundle must preserve both hashes and execution.

The Formats bundle codec writes `bundle.json` with envelope version 1, mode (`legacy` or `source`),
content identity, source/resolver versions and the sorted member path list. Source mode stores
the manifest as `source.toml` and member bytes beneath `members/<relative-path>`. Legacy mode
stores `ruleset.toml`, omits source/resolver versions and members, and retains its original hash
algorithm. The codec works on an entry-name/byte collection; the host supplies a directory or
archive without extracting arbitrary archive paths. Bundle metadata is checked against the
manifest and recomputed identity; its JSON spelling and archive compression are not hash inputs.
Source mode verifies exact membership, duplicates and the framed identity on read. Different
captured content under one identity is refused on catalogue insertion, allowing only the
documented CRLF equivalence. Missing members never fall back to checkout files. A future change
to source interpretation increments the resolver version; a build must implement the recorded
version or refuse it explicitly. Retaining bytes alone is not an interpreter compatibility promise.

### CitySave and replay integration

Introduce CitySave envelope version 2 in the shell, keeping `world.save` owned by Core. Its
`city.json` records seed, active content identity and the content catalogue entries. Store each
Formats bundle under `content/<16-digit-lower-case-hash>/`; the bundle codec defines its internal
layout. Verify metadata, bundle identities and `SaveHeader.RulesetInForce` agree before building
the world. Read version 1 saves through their existing `ruleset.toml` path and hash rules. Write
new saves as version 2, including when the active source is legacy. Do not rewrite old artifacts
or change Core's binary save schema merely to package sources.

A save containing a checkpoint needs the active bundle for continuation. Retain other bundles
referenced by an included Input Log from that checkpoint onward, plus its opening bundle. A
standalone replay artifact pairs the unchanged Formats Input Log codec with the same bundle
catalogue; verify every opening/transition hash resolves before replay starts. A provenance trail
alone is not an Input Log and cannot recreate a session. Loading an old v1 save can continue from
its checkpoint but cannot invent missing earlier Rulesets or inputs for historical replay.

Keep bundles in a host-owned content store while a live session or retained artifact references
them; Core's catalogue still contains only hashes and parsed Rulesets. Deduplicate by identity,
refusing collisions. Release candidates after refusal and prune unreferenced content when sessions
and artifacts release it; do not retain every keystroke forever or evict content a retained replay
needs. Package export must fail if required content is missing. Use temporary-file publication
and atomic replacement as CitySave already does. Bundle codec/validation belongs in Formats so
headless and Godot archives cannot develop different membership or hashing rules.

### Loading, reload and upgrades

Pinned loading always resolves retained sources under their recorded interpreter version and
checks their identity. Today's source directory is consulted only for an explicitly requested
candidate. For source v1's supported same-identity tuning, use the existing logged Ruleset
transition, frozen-world checks and migration path. Register the validated candidate before
recording its transition; refresh names when that transition actually takes effect. Failed
capture/parse/validation leaves the session untouched.

For lowered Bin capacities, preserve stock above the new ceiling and let it drain; no clamp or
Goods destruction. Preserve the existing distinctions for occupancy/job reductions and report
evictions/dismissals and Rule rearming. Source splitting, labels and ordering do not introduce a
new migration algorithm. Identity additions/removals still use existing migration semantics:
report dereliction, dropped Resource Bins and governed-value consequences; reject existing
forbidden changes such as Resource family changes. A static report cannot authorise an unsupported
occupied-world transition.

An id rename is removal plus addition, never an inferred alias. Source v1 introduces no alias
syntax or general upgrade mapping. Mapping renamed identities or old expanded `kind.variant`
content to `(kind, profile)` is the separate save/runtime migration work; refuse requests to
preserve identity through those unsupported conversions. For a legacy-to-source conversion,
ids matching old names retain the existing numeric identity keys where available, but still
require a deliberate transition and all applicable compatibility checks. Names outside source
v1's id grammar need an explicit future migration or a new city; never sanitise them silently.
0077's upgraded-save acceptance covers supported tuning through a logged transition followed by
save/load, not arbitrary content or binary-schema migration.

Exact replay also needs compatible code and every Ruleset referenced by its history. See
[save format and migration](05-technical-architecture.md#7-save-format-and-migration).

## What the investigation established

[0075](../plans/0075-base-game-ruleset-viability.md) separated runtime execution from human assembly.
In [0076](../plans/0076-ruleset-authoring-experiment.md), the user completed three local edits and
reported generally smooth authoring, with the omitted-overrides requirement as the surprise.
That requirement is fixed in the prototype. This is human evidence for those tasks, not a
whole-game usability or balance result.

The initial expansion exceeded Core's 254-kind limit at 83 consumer kinds with three variants.
The [factoring/bakery study](../plans/evidence/ruleset-authoring/scaling-and-bakery.md) preserves
storage differences through separate selections and shares behaviour definitions in research
code. Native Core integration, saved selection state and migration remain development work.
The isolated bakery keeps recipe/work/storage/supply/sales edits local, but a native probe produces
Food with zero workers. Core needs work-dependent production; its `jobs` Readout counts declared
posts and must not stand in for workers present. Existing Pool purchases already exchange Goods
and Money with local sellers.

The implementation boundary is therefore threefold: source loading/resolution and author tools;
Core definition factoring and saved selections; and missing gameplay connections such as labour.
An authoring layer must expose those boundaries rather than hide them behind generated content.

## Design the economic circuit

For each Good, trace its source, seller, payment, transport, buyer's Bin and consumption.
For each employer, trace income, jobs, wage, pay period and failure. For a public service,
add treasury income, grant, staffing, places and attendance. List opening capital separately
from recurring income. A positive treasury or a loaded file does not establish viability.

The current Pool purchase selects a local Business seller. A Hinterland price anchors the
market; it does not create an import supplier. Empty-input production is an explicit source,
not a purchase. Rules can convert Goods and emit pollution, but employing Citizens does not
by itself make that production depend on their work. These distinctions need mechanisms or
an explicit founding scenario decision before they become claims in playable content.

## Current single-file authoring: coupled values

| Authored intent | Values to check together |
|---|---|
| Consumption per Day | Rule input amount, `apply`, `rate` (Ticks), Bin capacity; shopping derives daily use from the net local consumption Rules |
| Days of shopping stock | Consumption above, `low_days`, `target_days`, carry/storage capacity and shop opening times |
| Production throughput | Input/output amounts, `apply`, `rate`, available inputs and output storage; fixed-count production has no automatic staffing factor |
| Paid public jobs | Trade `wage_per_day`, `pay_period_days`, Policy `interval`, grant `amount`, `apply = { derived = "jobs" }` and wage tier/experience settings if enabled |
| Building capacity | Geometry and occupied floor share, `floor_tiles_per_occupant`, `floor_tiles_per_job`, `floor_tiles_per_place`; service places also depend on staffing |
| A Good in a District market | Resource, ownership of declared Bins, supply/consumption Rules, reachable seller and Hinterland price declarations |
| Population turnover | Life Stage successors and children, housing space, Unplaced give-up duration, and the arrival/Outside contract owned by row 31 |

`rate = 256`, consumption `4`, fixed apply `1` gives nominal use of 32 per Day at
2,048 Ticks/Day. A three-Day shopping target is therefore 96; a smaller Bin prevents
holding that target. This arithmetic is a starting estimate: blocking, Trips and availability
still determine the delivered result. Time acceleration belongs to the host.

A grant per declared job and a wage per worker are different policies. Keep both controls:
a designer may deliberately fund vacancies or underfund wages. Doubling pay is one edit;
maintaining the same grant coverage is a second edit when both cadences are one Day.
Do not add a loader rule requiring their equality. Show their relation in the authoring
report, including pay-period cash needs, rather than hiding it in duplicated comments.

## Current single-file authoring: validation and iteration

1. Read the relevant descriptions in [the generated reference](ruleset-reference.md), then
   run the actual loader. The editor schema provides completion and types but deliberately
   permits unknown keys. The loader checks references, retired keys and several cross-table
   constraints. It does not prove a supply chain, solvency or adequate service coverage.
2. Use a fixed seed, command sequence and population. Report purchases/deliveries, payroll
   and shortfall, treasury flows and residual, Need levels and service capacity/attendance.
   Preserve flow sums across every Tick; do not sum a repeated last-event reading.
3. Change one intended balance relation. Retain the control and a precise semantic diff.
   The viability instrument generates variants from one file; it does not maintain copied
   Rulesets. Rerun past the relevant paydays, Life Stages and buffer exhaustion.
4. For an in-session change, register both Rulesets in the catalogue and record the transition
   through Formats' Input Log codec. Verify that the new values actually took effect. The
   headless session runner supports this; specialised dumps must not be assumed to consume
   `--reload-at`. The shell's current tuner **regenerates** the city. A governed Policy override
   survives reload, so inspect its effective value, not just the new default.
5. Run end-of-run invariants and replay/save continuation checks. Watch the shell's trigger,
   response and consequence. Separate tested duration/seed from steady-state or general
   balance claims. Existing Goods-conservation and exclusive-location invariants are still
   unimplemented, so an invariant pass does not certify those properties.

Use a fresh run for changes to frozen world geometry/layer contracts. For supported reloads,
retain every exact Ruleset used: comments and whitespace affect the content hash. Read
[the golden procedure](../tests/Borough.Tests/Golden/README.md) before changing golden inputs.

## Delivering the production authoring guide

Keep this document current as the loader ships. Turn the reviewed source v1 syntax into runnable
examples for a small connected package, then walk through adding a Good/recipe, referencing a
shared basket, choosing storage, introducing/removing an exception and evolving an inhabited
city. Show the impact report and source-located diagnostics for common mistakes. Document which
changes are supported, require migration, or require a new city.

Update schema/completion and the generated key reference with the implementation, using
`RulesetKeyNotes.cs` as the description source. Keep conceptual authoring instructions here,
field-level contracts in the generated reference, and bounded evidence in the investigation.
Documentation and a designer handoff are acceptance requirements of the loader work, not an
optional follow-up after a parser lands. See [0077](../plans/0077-ruleset-source-loading.md).

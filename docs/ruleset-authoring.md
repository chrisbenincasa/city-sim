# Authoring the base-game Ruleset

This is the canonical authoring design and guide for the base-game Ruleset. The intended source
is a set of well-scoped TOML files with shared domain definitions and explicit references,
assembled into one complete Ruleset before simulation. Designers author choices and relationships;
the loader resolves references, derives quantities and reports consequences.

**This is the agreed direction, not an implemented multi-file format.** The production loader
currently reads a single execution-oriented TOML document. The disposable prototype demonstrates
shared maintenance and impact reports; the factored representation and bakery contract establish
bounded design evidence. They are not production loaders or substitutes for Core mechanics.
[Plan 0077](../plans/0077-ruleset-source-loading.md) captures loader and guide implementation;
the [board](../plans/0000-board.md) owns scheduling and the separate runtime/gameplay work.

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

An illustrative organisation is:

```text
base-game/
  goods.toml
  consumption.toml
  recipes.toml
  businesses.toml
  storage.toml
  services.toml
```

These filenames show responsibilities; they are not reserved filenames or working include syntax.
The source entry point and membership syntax still need implementation. Each definition lives in
one owning location and is referenced by stable identity wherever needed. Files organise the
catalogue; shared definitions reduce repetition. Splitting copied Rules across files alone does
not solve maintenance or runtime multiplication.

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
   Define portable relative-path rules and package membership before choosing syntax.
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

## Saved cities and evolving content

A content hash identifies exact content, not a promise never to edit the game. Existing CitySave
packages embed their single TOML document. They load pinned content on a compatible build;
changing today's file does not by itself invalidate them. Loading and upgrading are separate.

The multi-file design must retain the complete source bundle and the version information needed
to resolve it, so a saved city or replay does not depend on files still present in a checkout.
Define a deterministic package identity covering membership and exact source bytes, with explicit
framing/order. Preserve existing single-file hashes and golden fixtures during introduction;
do not silently reinterpret their hashes as a new canonical serialization. A file reorganisation
may change package identity while preserving declaration identity and behaviour.

Stable ids and display labels need an implemented distinction; current migration keys are
name-derived. Upgrades need explicit mappings for renamed/retired identities and old expanded
`kind.variant` content becoming separate kind/profile selections. Check occupied entities and
all Bin owners. Capacity reductions need a stated overflow policy. Refuse or explain unsupported
transitions; successful parsing or save continuation alone does not certify preserved meaning.
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

Keep this document current as the loader ships. Add actual source-entry syntax and runnable
examples for a small connected package, then walk through adding a Good/recipe, referencing a
shared basket, choosing storage, introducing/removing an exception and evolving an inhabited
city. Show the impact report and source-located diagnostics for common mistakes. Document which
changes are supported, require migration, or require a new city.

Update schema/completion and the generated key reference with the implementation, using
`RulesetKeyNotes.cs` as the description source. Keep conceptual authoring instructions here,
field-level contracts in the generated reference, and bounded evidence in the investigation.
Documentation and a designer handoff are acceptance requirements of the loader work, not an
optional follow-up after a parser lands. See [0077](../plans/0077-ruleset-source-loading.md).

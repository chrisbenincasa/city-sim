# Scaling and the bakery: answers after the handoff

## Decision

**Keep shallow shared definitions; replace the variant-to-kind expansion before production use.**
The tested storage difference does not require another behavioural kind. A separate saved
selection can preserve its meaning without copying the consumption behaviour. The richer bakery
can also be described through a small set of shared relationships, but it exposes a genuine
missing Core mechanism: production tied to work. A more powerful authoring language cannot
supply that mechanism honestly.

These are bounded engineering answers. The factored representation and bakery contract execute
in isolated research code. They are not a replacement for Core, an adapter that already runs in
Core, or a claim about a complete game's usability, performance or balance. The real-Core probe
below deliberately tests the gap rather than hiding it behind a mock.

## 1. Removing unnecessary multiplication

The candidate representation keeps these separately:

- A kind's identity and basket reference.
- A shared storage profile, chosen for an instance.
- Explicit kind/profile exceptions; absence means inheritance.
- Shared consumption/replenishment definitions for actual basket/Good relationships.
- Recipes and their actual edges.

An instance references its kind and selected storage profile. Capacity resolves as daily use ×
selected Days, with a sparse exception overriding those Days. Changing storage does not copy the
basket, change the kind identity or invent another Rule definition. Equal values do not collapse
authored identities: two independently named profiles remain independently tunable.

| Authored kinds | Storage profiles | Flat kinds including 8 producers | Factored kinds | Shared Rule templates | Explicit exceptions |
|---:|---:|---:|---:|---:|---:|
| 20 | 3 | 68 | 28 | 20 | 3 |
| 83 | 3 | 257 | 91 | 20 | 13 |
| 1,000 | 3 | 3,008 | 1,008 | 20 | 150 |
| 1,000 | 30 | 30,008 | 1,008 | 20 | 150 |

The factored column counts **proposed definitions**, not Rulesets accepted by today's loader.
There are two baskets with six total Good memberships, giving 12 shared transfer/consumption
Rule templates, plus eight producer recipes. The larger catalogues repeat the same basket mix
and sparse exceptions; they do not establish scaling under a thousand distinct behaviours.
All 30,000 kind/profile capacity combinations in the largest case resolve correctly without a
30,000-entry definition table. A selection still occupies state on each actual instance.

[The checks](factoring-results.json) compare all 60 original resolved variants with the existing
flat compiler, and compare 100 isolated consumption firings, including empty-stock blocking,
against those flat definitions. Research-state serialization preserves kind/profile selections;
retiring a referenced profile refuses resolution. This is **not** full Core Tick/replay/save
compatibility, scheduler equivalence or a timing measurement.

The unavoidable work remains: actual Households need individual stock, decisions and consumption;
a dense basket still has one real relationship per consumed Good. Genuine exceptions still cost
space and explanation. This design removes duplication of definitions, not individual simulation.
And 1,008 genuinely distinct kinds still exceed today's byte-sized kind namespace—the existing
254-kind limit remains a separate capacity decision even after unnecessary variants are removed.

### Concrete Core/save boundary

Current `RuleDefinition.Kind` is a byte; `KindDefinition` owns Bin and Rule slices. `BinDeclaration`
provides capacities through the kind, and `World.FitOccupant` / `FitBusiness` fit owned state from
those declarations. Reusing source text cannot make this representation execute the factored form.

The next implementation should introduce a saved profile selection and typed capacity resolution
first, then share behaviour definitions without rearming/copying them solely for a profile change.
Preserve each Rule Instance's actor and cadence; do not merge individual decisions. Keep the
selection identity deterministic and include it in state hashing. Rebuild derived capacities from
kind, selected profile and current shared definitions. Keep profiles flat, with explicit exceptions.

For existing expanded content, require an explicit mapping from old `kind.variant` identities to
new `(kind, profile)` selections. Do not infer a safe migration from punctuation or equal values.
Verify all affected owners and live stock. Capacity reductions need an explicit overflow policy;
retired profiles need a mapping or refusal. Old saves remain pinned until an upgrade is requested.
No production hash, schema, kind width or CitySave format was changed by this experiment.

## 2. Crossing mechanisms with a bakery

[The authored contract](bakery.toml) has one recipe, one work definition, two storage profiles,
one supply relation, one sales relation, one bakery and two instance/scenario selections. The
large selection changes allocated floor and storage reference; it does not duplicate recipe,
work, supply or sales definitions. The existing floor-per-job relation derives posts rather than
adding a competing `jobs` setting on the Business.

| Relationship | Standard | Large | Where it is authored |
|---|---:|---:|---|
| Allocated Business floor | 6 Tiles | 12 Tiles | Scenario selection; real Core geometry/tenancy must supply this |
| Posts at 3 floor Tiles/job | 2 | 4 | Derived using the existing capacity relation |
| Batches per present worker/Day | 2 | 2 | Shared work definition |
| Produce → Food per batch | 4 → 8 | 4 → 8 | Shared recipe |
| Input/output storage Days | 2 / 1 | 4 / 3 | Standard/reserve storage profiles |
| Derived input/output capacities | 32 / 32 | 128 / 192 | Nominal full-staff throughput × storage Days |
| Wage, input price, sale price | 16 / 1 / 2 | 16 / 1 / 2 | Work, supply and sales respectively |

The storage relation uses **nominal full-staff throughput**, not momentary attendance. Losing a
worker reduces production, not the Bin's capacity. That is an explicit model choice, not an
unspoken author calculation. The quantities are synthetic; no claim is made that they balance
Borough's prices or wages.

[The isolated integer contract](bakery.py) has explicit Business input/output stock and cash,
local seller stock/cash, local buyer stock/cash and worker cash. Buying, producing, selling and
paying have bounded amounts and explicit counterparties. The tested sequence is buy → work →
sell → pay; it is not Core's Tick phase order. Paid supply and sales are transactions, not free
Goods sources. The local buyer's accumulated stock is finite because its cash is finite; this
short contract is not a steady-state economy.

[Checks](bakery-results.json) demonstrate:

- Zero workers produces zero; half staffing halves potential output in the supplied case.
- No input supplier or no purchase cash prevents production from empty stock.
- No buyers eventually fills output storage and blocks production, without overflowing stock.
- Every tested step conserves Money and accounts for recipe input/output conversion.
- Changing the large selection's floor or reserve input Days leaves the standard selection and
  shared recipe untouched. Changing the shared recipe re-derives both storage capacities.
- A research-state roundtrip continues identically. This is not a CitySave migration test.

This is evidence of local authoring and consistent relationships for **this behaviour**, not a
promise that every future service will compose automatically. The kernel is an executable
specification for a missing connection, not a second game engine to ship.

### What the actual Core does

[The native probe](bakery-core-results.txt) constructs a Business through its Building's declared
trade, disables assignment, manually supplies 32 Produce, and runs the fixed-count conversion
for 64 Ticks. It observes **six declared posts, zero workers, 32 Produce consumed and 64 Food
produced**, with end-of-run invariants passing. The input was fixture stock; this probe makes
no paid-supply or balance claim.

The real loader refuses `present_workers` as a production Readout. Its `jobs` Readout counts
posts, and an explicit `[[business]] jobs` setting is retired because jobs derive from floor share.
Therefore lowering the desired bakery to `apply = { derived = "jobs" }` would be wrong even if
it loaded: an unstaffed bakery would still have posts. The authoring prototype must refuse an
unsupported work-dependent contract instead of silently emitting fixed production.

Commerce is not wholly missing: `RuleEngine.Buy` already resolves a Pool input to a local seller
and exchanges Goods for Money atomically. This corrects the earlier prototype wording that
blurred Bin Rule purchases with the disabled ShoppingEngine. Other existing mechanisms provide
owned Bins, conversions, wages, floor-derived posts and seller discovery. The next production
slice should connect actual work availability to recipe execution and test paid input supply,
output purchases, payroll and recovery **in the same Core world**. Define partial-shift/work
accounting and execution order explicitly; do not transplant this daily contract loop into Core.

## What this resolves and what comes next

We can remove the demonstrated **definition** product without discarding storage differences.
We can describe and maintain this richer bakery using shared, finite domain relationships. The
remaining obstacles are concrete runtime representation and labour semantics, not a demonstrated
need for a more expressive language or a general inheritance framework.

Proceed with two bounded implementation slices: (1) profile selection/capacity and shared
behaviour execution, including explicit saved-state migration; (2) labour-dependent production
with a native paid bakery circuit and failure/recovery. The existing authoring and private
production board work own them. Keep the author-facing model shallow while those run. This is
an engineering decision to revise the runtime expansion and connect missing mechanics, not a
whole-game scalability certification.

Verification: research build without warnings; 3,399 working-lane tests pass, no failures/skips
(`/tmp/borough-test-20260913-105449.log`). Representation, isolated contract and native probe
results are retained separately so their different guarantees remain explicit.

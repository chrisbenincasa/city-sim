# A tenant owns what leaves with it and the premises own the capacity

**The economic actor is the **Occupant**, not the Building. A Bin belongs to the Occupant whose
leaving would empty it, and to the premises otherwise; **the premises declare every Bin's capacity and
the tenant holds its level**. Rules follow their Bins. **The Ruleset grows a second kind table** —
`[[building]]` for premises and `[[business]]` for the trade — because the same shopfront hosts a
bakery and then a barber.**
`SOLVE THE ACTUAL PROBLEM` `LEGIBLE CAUSE` `UNIQUE INDIVIDUALS`

**This finishes [`adr/0113`](0113-a-business-is-an-occupant-with-its-own-balance-and-a-building-never-holds-money.md)
and [`adr/0114`](0114-a-balance-a-rule-can-fail-on-is-a-bin-and-a-bins-owner-is-discriminated.md) rather
than deciding anything against them.** `0113` decided that a Business is an Occupant holding its own
balance and that *"a Building never holds money"*; `0114` wrote down the consequence —
***"`World.FindBin` takes an owner rather than a Building slot"*** — and named the blocker in its own
revisit trigger: *"`BinTable.Owner` is a `HandleColumn<Building>`, so no Household, Business or treasury
can own a Bin today."* ***A revisit trigger that has already fired is a decision waiting to be
finished.*** What is new here is the **line** — which Bins move — and the **second kind table**.

**It amends [`CONTEXT.md`](../../CONTEXT.md) → Building**, which draws the line at *logistics versus
decisions* rather than at *premises versus actor*, and which says a Building *"holds Bins, and runs
Rules"*. **It amends `CONTEXT.md` → Business**, which says a Business *"consumes inputs, produces
outputs"* — the two sentences are both authoritative and cannot both be whole.

## Why

**Because the corpus contradicted itself and the shipped content had already picked a side.**

🔴 **`rulesets/minimal.toml` contains the test, and nobody put it there deliberately.** The dwelling
declares two Bins, and its three Rules sort them onto opposite sides of a line that had not been drawn:

| Bin | Rule | `apply` | What that means |
|---|---|---|---|
| `sundries`, capacity 48 | `consume` | `{ derived = "occupancy" }` | **scales with the tenants** |
| `repairs`, capacity 4 | `upkeep` | `{ min = 1, max = 1 }` | **fires once, whatever the occupancy** |

And `condemn_after` demolishes the **premises** off `upkeep`'s failure, which is the same sorting a
third time. ⚠ **The apply count is evidence and not the definition.** The definition is the one a real
city gives: ***does it leave when the tenant leaves?*** Flour goes with the baker; the roof does not.

**The capacity half was found by trying to route around it and failing.** `World.CreateBin`
(`World.cs:2098`) takes its capacity from `DeclaredCapacity(Buildings.Kind[buildingSlot], resource)` —
**at the creation site, not only in `RebuildCapacities`** — and **neither a Household nor a Business
has a kind byte.** The attempt to drive the repair through the Household *"because a Household already
exists"* does not dodge that. ***What it produced instead is the answer: a shop holds what fits in the
shop, and what is in it is the shopkeeper's.*** So `DeclaredCapacity` stays keyed on the **building**
kind, and needs no business kind at all.

**The second kind table is forced by the high street rather than by the loader.** ⚠ **The premises and
the trade are not correlated, and nothing in a single kind table can express that** — it would make
*bakery* a property of the walls, so a tenant leaving would have to **demolish the shop** to stop being
a bakery. ***That is exactly the defect `ZoneRuleEngine.Condemn` has today***, arrived at from the
other end: it takes the longest failure pressure across the Building's whole Rule list and demolishes
the premises, so **one starving tenant condemns the other's shop.**

**The build already knew.** `BusinessTable`'s doc comment predicted the blocker **eight milestones
early**, in the words the milestone eventually hit: *"`local` money must resolve to **an** actor and a
list does not name one."* ⚠ **It is a doc comment, and every mechanical check in the corpus is
document-to-document**, so it was invisible to all of them — filed in
[`plans/0012`](../../plans/0012-corpus-audit.md).

## What changes

| | `[[building]]` — premises | `[[business]]` — the trade |
|---|---|---|
| **Declares** | `footprint`, `parking`, `occupants`, `condemn_after` | `jobs`, shift hours, the wage |
| **Bins** | those the premises keep — `repairs` | those the tenant keeps — stock, the till |
| **Rules** | those that fire once regardless — `upkeep` | those whose work scales with the trade |
| **Owns** | **capacity for every Bin on the Lot** | **the level in the ones it owns** |

⚠ **`occupants` changes meaning and does not change value.** It stops being *how many Households* and
becomes *how many tenants of any kind*, which is what
[`adr/0068`](0068-a-buildings-occupancy-is-declared-by-its-kind-and-an-over-capacity-building-evicts.md)'s
eviction already does — ***an over-capacity Building evicts, and it never asked what the overflow
was.***

🔴 **The Workplace stops being a Building**, and
[`adr/0101`](0101-a-commute-is-two-journeys-and-the-days-shape-is-a-property-of-the-job.md) already said
so in a word nobody had to read closely: a Shift start hour belongs to the **Workplace**, and a
Workplace is where you are **employed**. So shift hours travel with `jobs` to `[[business]]`. ⚠ **The
Citizen still stores nothing**, which is the property that ADR was defending — and it gets *stronger*:
*"changing employer changes their day with no write and nothing to invalidate"* becomes **literally
true**, where today a Citizen changing employer inside one Building changes nothing at all.

**The arming stagger mixes the tenant's monotonic id.** `World.ArmingStagger` (`World.cs:2080`) mixes
the **Building's** id with the `RuleId` *"so that two Rules on one Building do not share an offset"* —
***two Businesses running one Rule reopen that collision on the other axis.*** It is hash-bearing.

**Condemnation ends a tenancy and leaves the premises standing.** ⚠ **The pressure mechanism does not
change**: `Condemn`'s verdict is already an OR and its walk continues only for attribution
(`ZoneRuleEngine.cs:346`). What changes is **what dies**. And `RuleEvidence`
(`BuildingEvidence.cs:61`) **names no subject at all** — the subject is the enclosing
`BuildingEvidence` — so making the answer name a tenant is ***a field, not a redesign***.

## Consequences

**The State Hash moves and every golden baseline re-records**, since `RuleInstance.Building` and
`BinTable.Owner` are saved handles that fold target ids. ⚠ **Not a reason to defer, narrow or split**
([`adr/0100`](0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md)); what is
owed is attribution in the commit subject.

🔴 **`minimal.toml`'s `consume` changes what the city does, and this is a design change rather than an
optimisation.** `sundries` moves to the Household, so `consume` becomes one Rule per Household instead
of one Rule applied `occupancy` times — and `derived = "occupancy"`, **the one declared Readout in the
project**, loses its only caller. ⚠ **The shared larder was a shortcut and its own file says so**:
three Households in one dwelling do not share a kitchen, `minimal.toml`'s first line is *it models no
city*, and under [`adr/0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) a shortcut is
**not evidence**.

**A construction cycle has to be broken.** `BinTable(capacity, Buildings)` is constructed **before**
`BusinessTable(capacity, Buildings, Bins)`, and a Business's `Balance` is a handle **into** `Bins`.
***Pointing Bins at Business closes the loop***, and the current order exists to avoid exactly that.

⚠ **No Rule can read a Business's balance and the readout says so by throwing.** `Readouts.Read`
(`Readouts.cs:206`) throws for `Readout.Balance` because balance is Household-scoped, and
`ReadHousehold` is the only balance readout in the project. ***There is no `ReadBusiness`.***

⚠ **No number is chosen here, so no [`plans/0002`](../../plans/0002-open-questions.md) §D row is owed
by this ADR.** The one number the session could not settle — **what capitalises a Business** — is
[`adr/0142`](0142-an-unpremised-business-emigrates-so-the-sink-is-the-one-households-already-use.md)'s,
and it is owed there.

## Rejected

**All Bins move to the tenant.** It reads cleanly and it is wrong about buildings: a roof, a boiler and
a loading dock are real, and `upkeep`/`repairs` is one of them already shipped. ***It would also have to
answer where a vacant Building's `repairs` live, and the honest answer is that they are the
landlord's.***

**One kind table, with a tenant flag.** Rejected because it makes the trade a property of the walls —
see *Why*. ⚠ **It is also strictly more code**, since every kind lookup would branch on the flag.

**Leave Rules on the premises and move only Bins.** Rejected because a Rule's inputs and outputs are
`BinRef`s, so a Rule whose Bins all belong to a tenant is a tenant's Rule wearing the premises' name —
and `Condemn` would keep demolishing a building because its tenant went hungry, which is
`LEGIBLE CAUSE` failing.

## What would trigger revisiting

- **A Bin that genuinely belongs to neither** — a shared facility with its own consumption, where
  *whose leaving empties it* has no answer. The design would then need a third owner, and this ADR's
  line is the thing that failed.
- **A trade whose premises requirements are inseparable from it** — a blast furnace is not a tenancy.
  If kinds have to be co-declared in practice, the two tables are one table with extra steps.
- **`derived = "occupancy"` acquiring a second caller before milestone 25 runs**, which would make the
  Readout's loss a cost rather than a tidy-up.

# A Bin hangs off its owner, and the polymorphic column stays unbuilt

**A Bin's owner is expressed on the **owner**, not on the Bin. A Household and a Business each gain a
saved `BinHead`/`BinTail`, and `BinTable` gains a saved `OwnerNext` — an intrusive list whose links are
`Handle<Bin>`. `BinTable.Owner` stays a `HandleColumn<Building>` and `OwnerKind` stays the
discriminator. **The polymorphic owner column that [`adr/0114`](0114-a-balance-a-rule-can-fail-on-is-a-bin-and-a-bins-owner-is-discriminated.md)
gestured at is not built**, and this record is where to look for why.**
`SOLVE THE ACTUAL PROBLEM` `LEGIBLE CAUSE`

**This finishes [`adr/0114`](0114-a-balance-a-rule-can-fail-on-is-a-bin-and-a-bins-owner-is-discriminated.md)
on its own terms and declines one reading of it.** That ADR decided *a balance a Rule can fail on is a
Bin, and a Bin's owner is discriminated*, and wrote the consequence as ***"`World.FindBin` takes an
owner rather than a Building slot."*** ⚠ **Both halves survive here.** The owner *is* discriminated —
by `OwnerKind`, which has shipped since milestone 10 — and `FindBin` does take an owner. What is
declined is the inference that **discriminating the owner requires a handle column that addresses
several tables at once.**

It is [`adr/0141`](0141-a-tenant-owns-what-leaves-with-it-and-the-premises-own-the-capacity.md)'s
storage question, and it exists because that ADR made an Occupant hold **more than one** Bin.

## Why

**Because the shape is already in the build twice, and this is the first time its cardinality has
moved.**

🔴 **The existing answer is *the owner holds the handle*, and
[`DistrictPoolTable`](../../src/Borough.Core/Space/DistrictPoolTable.cs) says so in its own remarks**:
*"`adr/0114`'s answer for an owner that does not fit is already in the build twice: a Household and a
Business each hold their Bin's handle on the **owner** row and leave `BinTable.Owner` unset."* ⚠ **That
table became a join for a reason it states**: *"an actor holds one Bin because money is one Resource,
and a District holds one per Good."*

***So the pattern is owner-side ownership, and the join is what it becomes at one cardinality.*** A
Household that owns a balance **and** `sundries` is at a third cardinality — many per owner, one owner
per Bin — and the structure for that is a list, which is what a Building has had all along.

**The polymorphic column's own counter-argument has expired, and its cost has not.** `BinTable.cs:91–99`
refuses it as *"a new column type whose fold dispatches over the kind to four different `Rows`, and
whose dangling check does the same — machinery paid by every Bin in the world so that **one singleton**
need not be spelled separately."* ⚠ **The singleton is the treasury, and `adr/0141` turns it into every
tenant in the city.** ***The premise died; the price did not.*** That price is paid in `Column.Fold`,
`TargetIds` and `SaveHash.TargetsOf` — the code
[`adr/0112`](0112-the-saved-set-is-the-hashed-set-so-a-save-can-compute-its-own-state-hash.md) rests on
and the code lints **5** and **6** use to prove the simulation deterministic. ***The safest place in
this project to not invent machinery is the instrument that detects divergence.***

**And the count is three, not four.** The treasury has no owner row to point at under any shape, and a
District's Pool Bins are addressed by `DistrictPoolTable`, which shipped at milestone 12 task 5. ⚠ **So
the column the comment priced does not exist to be built** — it would address Building, Household and
Business.

## What changes

| | Before | After |
|---|---|---|
| `BinTable.Owner` | `HandleColumn<Building>`, unset unless `OwnerKind` is `Building` | **unchanged** |
| `BinTable.OwnerKind` | the discriminator | **unchanged** |
| `BuildingTable.BinHead`/`BinTail` | `Derived`, rebuilt from `Bins.Owner` | **unchanged** |
| `HouseholdTable` | `Balance`, one `HandleColumn<Bin>` | **`BinHead`/`BinTail`, saved** — the balance is one entry in it |
| `BusinessTable` | `Balance`, one `HandleColumn<Bin>` | **`BinHead`/`BinTail`, saved** |
| `BinTable.BinNext` | `Derived<int>`, the Building list's link | **unchanged** |
| `BinTable.OwnerNext` | — | **new: `SavedHandle<Bin>`, the tenant list's link** |

⚠ **The tenant list is SAVED and the Building list stays DERIVED, and that asymmetry is forced rather
than chosen.** `DistrictPoolTable` states the rule: ***"a derived list is only derivable when the
element names its owner."*** A Building-owned Bin names its Building, so its list rebuilds. A
tenant-owned Bin names nothing, so its list comes out of the save already made or it does not come back
at all.

⚠ **The links are handles and not slot indices, deliberately.** `BinTable.SupplyHead`/`SupplyTail` are
`Saved<int>` — saved slot indices, which fold **identity** where `05 §4` asks for **values**. ***That
precedent is not extended here***: a `HandleColumn<Bin>` folds the target row's monotonic never-reused
id and costs nothing extra to declare. **Whether the wait-list heads should follow is not decided
here** and is filed in [`plans/0012`](../../plans/0012-corpus-audit.md).

## Consequences

🔴 **A Bin cannot name its owner, and that is the whole of what this decision gives up.** `OwnerKind`
answers *what kind of thing owns this*; nothing answers *which one*. ⚠ **`MoneyLedger.Of` already
declares it does not care** — it walks every live Bin *"whoever owns it — and this does not ask"* — and
that sentence is now load-bearing rather than incidental.

**`RebuildCapacities` walks owners rather than Bins.** It resolves `Bins.Owner` today for one purpose —
to fetch `Buildings.Kind` — so it is rewritten to reach each Bin from its owner and take the kind from
the **premises**, which is `adr/0141`'s rule stated as code. ⚠ **Same asymptotic cost, different
traversal**, and its invariant twin `BinCapacitiesMatchTheirDeclarations` moves with it.

**The State Hash moves and every golden baseline re-records.** Not a reason to defer, narrow or split
([`adr/0100`](0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md)); what is
owed is attribution in the commit subject.

**The hot path stays a short walk.** `RuleEngine.Bin` resolves a `local` term per term per evaluation
through `World.FindBin`, so a tenant's Bins must be reachable in the length of that tenant's own Bin
list. ⚠ **This is the axis on which the join table lost** — see *Rejected*.

**No new number, and no [`plans/0002`](../../plans/0002-open-questions.md) §D row is owed.** Nothing
here is tunable and nothing is chosen against a band.

## Rejected

🔴 **A polymorphic `BinTable.Owner`.** ⚠ **This is the strongest rejected option and it may yet be
right** — it is the only shape that answers *Bin → owner* in **O(1)**, it keeps one mechanism for every
owner kind, and `adr/0114` gestures at it. **Refused on where the cost lands rather than on how large
it is**: `Column.Fold`'s contract, `TargetIds` and `SaveHash.TargetsOf` are the machinery that makes a
save able to compute its own State Hash, and ***buying a direction one caller wants by editing the
instrument that proves determinism is the wrong trade at this milestone.*** It also reopens a
construction cycle — `BinTable` is built before `BusinessTable` because a Business's balance is a handle
**into** `Bins` — which would need a two-phase `Rows` bind.

**Saved join tables, one per tenant kind, mirroring `DistrictPoolTable`.** Refused on the **index**.
`plans/0037` task 5 left that table without one deliberately, and said why: *"a dense index is what a
**hot** path wants, and the hot path arrives with the purchase."* ⚠ **A tenant's `local` term is already
that hot path** — `RuleEngine.Bin` per term per evaluation — so a join here would have to ship the index
`DistrictPoolTable` was allowed to defer, which is the whole of the saving gone. ***A single join table
was refused twice over***, because a polymorphic owner is exactly what it would need.

**A saved `Column<int>` link, matching `SupplyHead`/`SupplyTail`.** Refused: it folds a recycled slot
index. It would work and it would spread a defect.

## What would trigger revisiting

- **Anything needing to ask a Bin who owns it.** A diagnostic, an Evidence panel, or an invariant that
  walks Bins rather than owners. ⚠ **`MoneyLedger.Of` is the one to watch** — it walks every live Bin
  and its comment says it does not ask. ***The day it has to ask, this record is the thing that
  failed***, and the answer is the polymorphic column rather than a fourth structure.
- **A fourth owner kind that is not a singleton and not a join.** Three tables is what makes spelling
  them separately cheaper than dispatching over them; the arithmetic reverses somewhere.
- **`RebuildCapacities`'s owner walk becoming hot.** It runs on rebuild today. If a capacity ever has to
  be recomputed per Tick, a traversal chosen for a cold path is being asked to do a warm one's job.
- **A Bin whose owner changes without the Bin being destroyed** — a tenancy transferring stock to its
  successor. Owner-side links make that an unlink and a relink in two lists, where a column would be one
  write. `adr/0142` refused succession for a demolished shop, so nothing needs it yet.

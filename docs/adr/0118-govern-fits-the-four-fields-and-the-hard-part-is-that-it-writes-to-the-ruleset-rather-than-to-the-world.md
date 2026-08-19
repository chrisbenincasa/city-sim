# Govern fits the four fields, and the hard part is that it writes to the Ruleset rather than to the world

**`CommandKind.Govern` fits `Command`'s four fields with room left, so the Input Log format stays at version 1.** A District is named by a **point inside it** and never by an index; *global* is a **bit** and never a sentinel coordinate. **And the examination found the risk is not the fields at all**: Govern is the first verb whose effect is a **standing setting** rather than a one-shot mutation, and its key is a `RuleId` — a dense positional index into a document a hot reload can replace. **Milestone 10 needs no `Govern` command**, so this is a result recorded rather than a blocker cleared. `LEGIBLE CAUSE` `FAST ITERATION` `SOLVE THE ACTUAL PROBLEM`

## Why

`Command.cs` asks for this by name: *"Whether a verb's arrival is free depends entirely on whether its payload fits the four fields below — `Connect`'s was made to fit ([`0077`](0077-a-road-edit-is-one-segment-and-the-player-lays-streets-only.md)), and a verb carrying two coordinate pairs would not have. **Service and Govern have not been examined against that test.**"* The four fields are `Kind` (a `ushort`), `Zone` (a `ushort` payload word), and `East` / `North` (`Tiles`, an `int` each) — twelve bytes.

### The payload fits, and the place fields are what carry the scope

A financial lever is three things: **which lever**, **what value**, and **where it applies**. [`04 §5`](../04-economy-and-goods.md) names the set — *"set tax rates, set service funding levels, and borrow"* — plus [`0035`](0035-infrastructure-is-priced-by-what-it-consumes.md) §3b's rebuild threshold, which is *"a Policy rather than a fifth lever"*.

`CONTEXT.md` supplies the scoping rule and it maps straight onto the fields: *"**Anything attached to a place can be overridden per District**; only instruments acting on the city's single balance sheet — borrowing, essentially — are irreducibly global. **Global is the default level, not a separate category.**"* So `East` / `North` carry the override's place and `Zone`'s sixteen bits carry the lever and its value. Nothing needs a fifth field, and the version stays at 1 — which matters, because a bump *"would cost every log ever written — including the committed golden baseline."*

### A District is named by a point, and that is required rather than convenient

There is **no `DistrictTable` in the build**. A District today is a spatial partition — `RoutingPartition`, at **4 Cells, provisional and unratified** since milestone 5c. So a District "id" is a derived index over a constant nobody has ratified, and a log carrying one is a log **whose meaning changes when that constant is ratified**.

A point does not have that property, and `Connect` already establishes the move: it carries an origin and an axis and *"the far endpoint is derived rather than carried"*, because the world holds the lattice spacing. Naming a District by a Tile inside it is the same trade and the same reason. ***A log names places and never derived indices*** — the sibling of the State Hash's own rule that a handle column folds a monotonic never-reused id rather than a recycled slot.

### Global is a bit, not a sentinel coordinate

The tempting spelling is a reserved coordinate meaning *no place*. It is refused, and by a rule this milestone has now applied three times: **every Tile coordinate is a legitimate place**, so no coordinate can mean *unset* — session F's ***a placeholder whose value sits inside the range of legitimate answers cannot announce itself***, after the money unit ([`0115`](0115-moneys-unit-is-fixed-by-the-smallest-fraction-the-design-multiplies-by.md)) and the opening treasury balance ([`0116`](0116-the-treasury-opens-empty-and-a-founding-balance-is-a-ratio-this-milestone-holds-neither-side-of.md)). The scope is a flag in `Zone`, where the discriminator already lives for `Connect`.

### The finding: every verb before this one wrote to the world

This is what the examination was actually for, and the fields were never the risk.

`Zone`, `Connect`, `Trip` and `Populate` are **one-shot mutations**. Each writes to a **world** entity — a Lot's permission set, the Road Graph, a Trip row, a population — and by the time the next Tick starts the effect is world state keyed on things the world owns.

**A tax rate is not that.** It is a **standing setting**: set at Tick 100, still in force at Tick 100,000. So it is saved, hashed state — and its key is a **Rule**. `RuleId` is `readonly record struct RuleId(ushort Raw)`, a dense index, and its own remark says what it is not: *"what they **mean** is resolved in `Borough.Formats`, which is also the only place a Rule has a name ([`0048`](0048-the-ruleset-is-validated-where-it-is-parsed-and-only-integers-and-strings-cross-into-the-core.md))."* The index is **positional in the Ruleset file in force**.

⚠ ***Every verb before this one wrote to the world; Govern writes to the Ruleset in force*** — and the Ruleset in force is not saved state, it is a hash that can be replaced mid-session under [`0015`](0015-all-tuning-data-is-hot-reloadable.md).

**Replay is safe and the live case is not**, and the two must not be collapsed:

| | Why |
|---|---|
| **Replay** | ✅ safe by construction. `TickInput.RulesetHash` is populated **every Tick** and the reload runs at the top of Phase 0, so a command at Tick T is always resolved against the same file the player was looking at |
| **A live hot reload** | ⚠ open. A designer who inserts a `[[rule]]` shifts every subsequent index, and a standing setting keyed on one now names a different Rule. `RulesetDegradation` already counts *Rules re-armed*, so the world re-derives; **a saved setting cannot** |

***The world can be re-derived on a reload and a saved setting cannot***, so an identifier that is safe inside a table is unsafe as a Policy's key. What that key should be is left open — the two candidates are a hash of the Rule's authored name, which `adr/0048` puts in `Formats`, and an explicit authored `[[policy]]` identity, which is `adr/0015`'s own direction of making the stable thing content. Filed rather than decided, because it belongs with the mechanism.

### Not every player input is a `Command`, and the corpus already proved it

If something ever does not fit, the answer is neither a fifth field nor a version bump. `Simulation`'s reload path says so: *"**There is no new verb, and `Command` was not widened.** A reload is a *transition* rather than an event: `TickInput.RulesetHash` is populated every Tick and was read by nothing until now, and **`Command` is 12 bytes and could not have carried a hash anyway.** The door argument is satisfied rather than weakened — `TickInput` **is** Phase 0's input."*

So the escape hatch exists, is precedented, and costs no version. ***Not every player input is a `Command`***, and the four-field test is a test on the *event-shaped* inputs only.

### Borrowing is the one lever that would not fit, and it is not this milestone's

A borrow amount is **money**, a `long`. Sixteen bits cannot hold it. `East` and `North` are two `int`s — sixty-four bits — and they are **free exactly when the lever is global**, which `CONTEXT.md` says borrowing alone is. The field that says *where* is spare precisely for the one lever that has no *where*.

⚠ **Noted as a fit and deliberately not designed.** What a borrow command carries — an amount, a ceiling, or a choice among offered terms — is stated by no document, which is [`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md)'s **undesigned** class. Designing it here to fill a field would be choosing a mechanism because the bits were available.

⚠ **And [`06`](../06-roadmap.md)'s inventory places borrowing at 10 while milestone 10's own row does not mention it** — bundled into *"Conserved Money, treasury, balance of payments, borrowing"*. **Fourth sighting of a mechanism hidden inside another row**, one row from where [`0117`](0117-upkeep-leaves-milestone-10-and-its-blocker-is-a-rule-with-no-actor.md) found the third, and the same shape: the row is true and the placement is the lie. Milestone 10 does not build borrowing.

### Milestone 10 needs no `Govern` command at all

`plans/0031`'s task 5 is a tax and a transfer authored as `[[rule]]`s in a Ruleset. **A demonstration Ruleset states the rate; nothing has to set it from outside.** So decision 5 was recorded as blocking task 5 and does not: like decisions 2, 3 and 4 before it, the thing the brief expected to need turned out not to be needed here. The examination stands on its own, because `Command.cs` asked for it in writing and an unexamined claim in a doc-comment is exactly what `adr/0093` is about.

## Consequences

- **The Input Log format stays at version 1** and `Command` is not widened. `Command.cs`'s open note is discharged **for Govern**; ⚠ **`Service` is still unexamined** and that half of the sentence stands.
- **A Govern command's shape is settled without being built**: a scope flag and a lever and its value in `Zone`, a point in `East` / `North` for a District override, resolved against the Ruleset the log already records per Tick.
- ⚠ **A Policy's key is an open question**, filed to [`plans/0002`](../../plans/0002-open-questions.md) §C: a standing setting keyed on a `RuleId` is keyed on a file position, and a hot reload can move it. **Replay is unaffected**; do not read the safe half as covering the other.
- **`06`'s inventory row for borrowing is split from *Conserved Money***, so a bundled placement stops reading as a schedule.
- **`Service` inherits the method rather than the answer.** Its payload is a Building and a catchment, which is a place and a kind, so it looks like it fits — but *looks like it fits* is what this examination existed to replace.

## What would trigger revisiting

- **A lever whose value does not fit sixteen bits and which is also place-attached.** That is the one combination the fields cannot take, and it forces either `TickInput` or a version bump. Borrowing escapes it only because it is global.
- **A `DistrictTable` being built**, giving a District a stable saved identity. The point-not-index argument would then rest on preference rather than on necessity, and should be re-argued rather than assumed.
- **`RoutingPartition`'s 4 Cells being ratified or moved.** It is the constant the District partition is derived from, and this ADR cites its provisional status as a reason.
- **A second verb whose effect is a standing setting.** Two of them makes the key question structural rather than Govern's, and it should be answered once for both.

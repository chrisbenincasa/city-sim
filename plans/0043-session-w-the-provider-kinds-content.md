# 0043 — Session W: the Provider kind's content

**[`0002`](0002-open-questions.md) §A's one open row**, and the gate on [`06`](../docs/06-roadmap.md)
milestone **26** — the purchase, where `Scope.Pool` stops throwing.

> ⚠ **THIS DOCUMENT IS A BRIEF AND NOT A CONCLUSION.** It says what the sitting must settle, what the
> corpus already answers for free, and what it contradicts itself about. ***It settles nothing***, and
> the reason is [`0002`](0002-open-questions.md) §A's own: **this is CONTENT rather than mechanism, and
> content may not be settled by whoever happens to be writing the Ruleset.** The sitting runs with the
> user in the room.

> ⚠ **IT IS `0043` AND NOT `0042`, AND THE COLLISION IS WORTH ONE LINE.** This session was scoped as
> `0042` on 2026-08-25 and the number was taken the same afternoon by
> [`0042`](0042-terrain-and-the-land-rows.md), milestone 24's terrain plan, merged from a branch while
> this was being written. ***A next-free number is only next-free against the tree you can see***, which
> is [`adr/0140`](../docs/adr/0140-a-milestone-number-is-an-identity-and-the-roadmaps-order-is-the-sequence.md)'s
> rule meeting a second session.

---

## Status

~~🔴 **NOT RUN.**~~ 🟡 **RAN 2026-08-25 WITH THE USER IN THE ROOM, AND SETTLED ONE OF FOUR.** Written
against the tree at `7e67c46`.

✅ **W-Q1 IS SETTLED** —
[`adr/0163`](../docs/adr/0163-demand-for-a-shop-is-elapsed-unserved-need-in-reach-and-building-claims-it.md),
*demand for a shop is elapsed unserved need in reach, and building claims it.* **Open: W-Q2, W-Q3, W-Q4**
— and ⚠ **W-Q2 shrank to almost nothing as a consequence** (**W12**), while ***W-Q3 is now the
expensive one.***

🔴 **THIS BRIEF'S OWN RECOMMENDATION ON W-Q1 WAS REFUSED, AND THE REFUSAL IS THE DECISION** (**W13**).
The session opened by offering the existing presence test as the principled line, on the grounds that an
impoverished signal cannot smuggle a synthesised one in. ***The user refused it in one sentence —
consolidating to a boolean removes the richness of the data and signal — and was right***, and the
corpus turned out to agree with the user against the brief in two places the brief had not read.

🔴 **The session's own agenda SHRANK before it opened, and by a verified finding rather than an
argument.** §A and [`0003`](0003-build-plan.md) both state milestone 26's gate as **two** things: the
Provider kind's three content decisions, **and a world where a Building genuinely runs out of money.**
✅ **The second is not a gate on this milestone and cannot be** — see **W1** and **W2**. It is an
*obligation inside* 26 rather than a precondition on it, and the Provider is the thing that produces
it. **26's gate is ONE thing, and it is this session.**

---

## Why it opened

**Because milestone 26 is blocked on it and nothing else is.** [`PROCESS.md`](../PROCESS.md) → *The
three tracks* carries the standing rule — ***an argument session runs when something concrete is blocked
on it, and not because it is available*** — and this is the case it describes rather than the case it
warns about.

🔴 **Nobody owns these decisions, and that is why they are in §A rather than in a ledger.** They were
enumerated twice before anybody asked for them — in `rulesets/minimal.toml`'s own header and again in
[`0037`](0037-goods-between-buildings-the-district-pool.md) task 8 — and ***a blocker named inside a task
entry is invisible to the ledger that owns what is next.*** §A records that as its second sighting in two
days; the first was the payer, which sat in `BusinessTable`'s doc comment for eight milestones.

---

## The claim

**A Provider is a second `[[building]]` kind that draws inputs from the District Pool and sells its
output into it**, settled 2026-08-22 with the user in the room by
[`adr/0135`](../docs/adr/0135-a-market-needs-two-sides-so-twelve-ships-a-provider-and-the-price-moves.md).
🔴 **The decisive argument there was that one side is not a weak market but an UNDEFINED one**: with no
seller the Pool holds no stock and consumes nothing, so a price from *Pool level against recent
consumption* is computed from **two zeroes**.

***That the Provider ships is settled. What the Provider IS, as content, is not.*** This session is the
second half only.

---

## What the sitting must settle

**Four things. The first three are `minimal.toml`'s header, quoted verbatim in `adr/0135`'s own
Consequences; the fourth is the file they go in.**

### W-Q1. What raises a Provider — the second `[[zone_rule]]`

⚠ **This is the hard one, and the reason is in the build rather than in the header.** See **W4**: the
create predicate is gated on the **Unplaced Pool**, which is a pool of **Households**. A second Zone Rule
written the obvious way raises shops in proportion to demand for ***homes***.

**What must not happen**: a demand scalar. [`CLAUDE.md`](../CLAUDE.md) → *Things to be careful about* —
***there is no RCI meter; the Unplaced Pool IS the demand signal*** — and a `commercial_demand` float
would be the design's named failure mode arriving as a Ruleset key.

🔴 **The live risk is that this question is VOID AS POSED.** If the honest classification is that
**demand for commercial premises is *unbuilt***, then under
[`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) the answer to *given it
does not exist, should the Zone Rule compensate?* is **build it** — and milestone 26 gets **bigger**
rather than unblocked. ⚠ **`rulesets/crowded.toml`'s header already classifies industrial demand exactly
this way**, so the precedent is in the corpus and points at the expensive answer.

### W-Q2. What declines one — the second decline Rule

*"So that it churns rather than accumulating until the city is all offices."* ⚠ **This is
[`adr/0006`](../docs/adr/0006-no-collection-grows-with-elapsed-time.md) stated as content**: a source with
no sink. **Milestone 27 has the precedent and it is exact** — `0041` **G36**, where a kind-declared shop
waiting in the unpremised pool took Businesses from **121 to 1,095 over 32,768 Ticks on `minimal.toml`**,
and where ***a give-up bound did not close it***, because a bound drains a stock at a rate and that was a
source with no matching sink.

✅ **THE MECHANISM ALREADY EXISTS AND THE QUESTION IS SMALLER THAN IT READS** (**W7**). *A decline Rule*
is not a new Rule family — **it is a Rule the kind can FAIL**, and the whole decline path is already
per-kind: pressure is *continuous starvation of a Rule Instance*, `condemn_after` sits on the **Building
kind** rather than on the Zone Rule, and a Provider kind with no failable Rule is **immortal by
construction** (`condemn_after` absent or `0` returns immediately). ⚠ **So W-Q2 is content in the
strictest sense**: *what does a Provider consume such that failing to get it should kill it?* — and the
answer is a Ruleset authoring decision, not a mechanism.

### W-Q3. The land-use split

How the generator decides which Lots may carry a Provider and which may carry a dwelling.
🔴 **THERE IS NO LAND-USE SPLIT IN THE BUILD AT ALL, AND THIS IS THE QUESTION THAT TURNS OUT TO NEED
CODE** — see **W8**. `SyntheticCity` paints **bit 0 and only bit 0**, at two hard-coded literals, and
there is **no repaint path**: `LotTable.Create` is the sole writer of `Lots.Zone` in the project.
⚠ **So this is not a question about which Lots get which bit. It is a question about a generator that
has no zoning input of any kind**, and the session must decide whether the split is authored, derived,
or drawn by the player.

### W-Q4. Which Ruleset carries it

⚠ **Not `minimal.toml`, and the file says so itself.** `adr/0135`: *"the Provider does not go in
`minimal.toml`; it needs a Ruleset of its own, and that file is the same one `plans/0037`'s Definition of
done already owes."* ⚠ **`bordered.toml` and `crowded.toml` are the precedent** — shipped files that make
content decisions on purpose and carry a header saying what they must not be read as.

---

## What the sitting may not do

**Four rules, and each is an ADR rather than a preference. Read the ADR before leaning on it.**

| Rule | What it forbids here | Read |
|---|---|---|
| A claim a measurement could settle must not be settled by argument | Deciding by discussion whether a candidate demand signal *works*. **Name the number and the machine** | [`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md) |
| A hash-bearing number is chosen with a named ratifier or not at all | Every number this session authors is Ruleset data and hash-bearing. **A ratifier names a machine, a WORLD and a QUANTITY, and a category is not a name** | [`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md) |
| An unbuilt mechanism is not a design constraint | Reasoning from *the simulation has no commercial demand* without classifying it. **Only *refused* is evidence** | [`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) |
| A description of the build is where to look, and never what you found | Taking this document's **W4**–**W6** as the mechanism. ***Open `ZoneRuleEngine` rather than quoting this brief*** | [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md) |

⚠ **And one more, which milestone 27 supplied the worked example for.** `0041` **G39**: task 9's real
content was read out of a doc comment that *predicted* it, the predicted thing was built, and **it loaded
and crashed on the Tick it fired**. ***A comment is right about where to look and is never a claim about
what the work is.***

---

## Findings

**W1 — 🔴 THE BANKRUPT WORLD IS NOT A GATE ON MILESTONE 26, AND THE BUILD SAYS SO IN TWO PLACES.**
§A and [`0003`](0003-build-plan.md) both carry *a world where a Building genuinely runs out of money* as
a second gate. **Bankruptcy is a money Bin coming up short when a Rule needs it**
([`adr/0137`](../docs/adr/0137-the-wait-list-knows-which-bin-and-evidence-does-not-so-bankruptcy-needs-one-field.md):
Pool Bin short is starvation, money Bin short is bankruptcy). **Nothing in the build can produce one
today, and the two reasons are independent:**

1. **The only thing that drains a Business is the levy, and a levy cannot bankrupt anybody** —
   `src/Borough.Core/Rules/Readouts.cs:63`: *"a derived apply count taken off the same Bin the term then
   draws from **can never overdraw** … ***A Rule whose apply count is read off the Bin it spends is
   unfailable by construction.*** So a levy on holdings never joins a wait list and never reports
   bankruptcy."*
2. **Nothing makes a Business spend on a Rule at all.** Milestone 27 task 9 built Rules on a trade and
   **reverted them** (`0041` **G39**) — the column loaded and crashed on the Tick it fired, because
   `RuleEngine.Fire` resolves a Building from the instance.

🔴 **And the world that WILL produce it is the Provider's own, which makes it 26's output rather than
26's precondition** — `src/Borough.Core/Rules/Ruleset.cs:1515`, at `Reprice`: *"The floor is zero and a
glut really can take a price to nothing, which is decision 4's answer and **task 10's material: a
Provider selling into a saturated Pool earns less than it spent, and bankruptcy is the observable that
distinguishes this market from a decorative one.**"* ***So the bankrupt world is downstream of both the
purchase and the Provider, and cannot be shipped ahead of either.*** **Milestone 26's gate is one thing
and not two.**

**W2 — ⚠ THE GATE'S WORDING NAMES AN ENTITY THAT CANNOT DO WHAT IT DESCRIBES.** *A **Building** that
genuinely runs out of money* is refused by
[`adr/0113`](../docs/adr/0113-a-business-is-an-occupant-with-its-own-balance-and-a-building-never-holds-money.md)
in its own title — ***a Building never holds money*** — and by `CONTEXT.md` → Building, which lists money
among the things a Building *"may **never** hold."* **The entity that holds a balance is the Business**,
which is an *Occupant* of a Building rather than the Building. ⚠ **This is loose wording rather than a
wrong decision**, and it is recorded because the sentence has been copied twice — into §A and into
`0003` — and ***the next copy is the one somebody builds against.*** **Owed to
[`0012`](0012-corpus-audit.md).**

**W3 — ✅ `0037` ALREADY ASSIGNS BOTH GATES TO ONE TASK, so they fit together by construction rather than
by judgement.** [`0037`](0037-goods-between-buildings-the-district-pool.md) task 8's entry: *"⚠ **Task
10's world obligation lands here too**, as this list already says: check for *a Building that genuinely
runs out of money* at **this** task rather than at task 10."* ⚠ **The precedent it cites is milestone 11's
F25** — a task specified against a world that did not exist, where the task before it had to ship the
world. **W1 does not contradict this; it sharpens it.** The check belongs at task 8 and **its answer is
already known to be *no*** — milestone 27 task 10 measured **7,165 premisings against ZERO give-ups over
131,072 Ticks** (`0041` **G44**), so nothing drains a Business's money and the world stays unwritten until
the purchase lands.

**W4 — 🔴 THE CREATE PREDICATE READS A POOL OF HOUSEHOLDS, WHICH IS WHY W-Q1 IS HARD.**
`src/Borough.Core/Rules/ZoneRuleEngine.cs:254` — the create predicate is *vacant AND permitted AND
`_world.UnplacedPool.Count != 0`*, and the surrounding comment states it as ***"vacant AND permitted AND
somebody in the Pool would take it"***, with *"no Household in the Unplaced Pool that would accept it"*
listed **beside** *no capital* rather than downstream of it. ⚠ **The Pool is read as *non-empty* and
drained blind** (`adr/0054`). ***So the only demand signal wired into construction today is demand for
dwellings***, and a second Zone Rule inherits it unless the session gives it something else to read.

**W5 — ⚠ THE CHEAP ANSWER TO W-Q1 IS ALREADY REFUSED, so the session may not reach for it.**
`adr/0135`'s *Rejected*: *"**Put the Provider's output on the `dwelling` kind.** The shortcut that would
avoid a second `[[zone_rule]]`. Refused because it makes every dwelling a factory and deletes the
ownership boundary the milestone exists to cross."* ⚠ **`minimal.toml` is doing a version of this today
and says so** — *living above the shop* — and `adr/0148` made it explicit by letting a premises kind
declare a trade. ***That is not the Provider and must not be mistaken for it***: an instantiated shop
opens at a zero balance and sells nothing.

**W6 — ⚠ THERE IS NOW A SECOND POOL, AND IT DID NOT EXIST WHEN THE HEADER WAS WRITTEN.** The unpremised
Business pool shipped with milestone 25 task 5
([`adr/0142`](../docs/adr/0142-an-unpremised-business-emigrates-so-the-sink-is-the-one-households-already-use.md),
[`adr/0144`](../docs/adr/0144-a-tenant-that-loses-its-premises-keeps-only-its-money-and-waits-a-households-wait.md)).
**It is the obvious candidate for W-Q1** and is exactly parallel to *the Unplaced Pool is the demand
signal*. 🔴 **But whether it can carry a signal is MEASURABLE and the one measurement that exists points
the wrong way**: `adr/0147`'s placement re-premises a pooled shop into a standing Building long before
the give-up bound, and milestone 27 task 10 measured **7,165 premisings against ZERO give-ups**
(`0041` **G44**) — ***a pool that drains that fast sits near-empty, and a Zone Rule gated on non-empty
builds almost nothing.*** ⚠ **Under [`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)
this may not be settled in the room**: the refuting number is the pool's *occupancy over a run* and the
machine is the headless `--business` mode, which milestone 27 task 10 shipped.

**W7 — ✅ THE DECLINE PATH IS ALREADY PER-KIND, so W-Q2 needs content and not a mechanism.**
`ZoneRuleEngine.Condemn` (`src/Borough.Core/Rules/ZoneRuleEngine.cs:320-408`), in order: an **undeclared
kind is immortal** and returns (`:334`); `condemn_after` of **0** is immortal and returns (`:339`); the
**premises are judged first** and a hit demolishes (`:349`, `:364`); **only if the premises survive** does
it walk tenancies and end one (`:380-407`). ⚠ **Pressure is continuous starvation of a Rule Instance and
nothing else** — only `Blocking.Supply` starts the clock (`RuleEngine.cs:617`), any other blocking reason
**clears** it (`:615`), and ***one successful firing zeroes it entirely*** (`:550`). ⚠ **`condemn_after`
is a property of the BUILDING KIND, never of the Zone Rule** (`Ruleset.cs:422`), which is why a decline
Rule is a per-kind authoring decision. 🔴 **There is no non-rule decline path for a Building anywhere**:
`DestroyBuilding` has **exactly one production call site**, `ZoneRuleEngine.cs:364` — no age, no vacancy
timer, and **no land-value input to demolition at all**. ***So a Provider kind with no Rule it can fail
never declines, and the city fills with offices exactly as the header predicted.***

**W8 — 🔴 THERE IS NO LAND-USE SPLIT IN THE BUILD, AND AUTHORING ONE IN A RULESET IS A SILENT NO-OP.**
This is the finding that makes W-Q3 the expensive question rather than the obvious one.

- **`SyntheticCity` paints bit 0 and only bit 0**, at two hard-coded literals — `SyntheticCity.cs:663`
  (the no-roads fallback) and `SyntheticCity.cs:693` (the real path), both `zone: 1`, which is the **mask
  for bit index 0**. ***The generator has no zoning parameter, no Ruleset key and no heuristic.***
- **There is no repaint path.** `LotTable.Create` (`LotTable.cs:216-223`) is the **sole writer** of
  `Lots.Zone` in the project; every other reference is a read. ***A running world can never acquire a
  second bit.*** The only other route is `CommandKind.Zone` at carve time, which
  `src/Borough.Headless/EvidenceDump.cs:627` states has **no production call site anywhere**.
- **All eighteen shipped Rulesets carry exactly one `[[zone_rule]]`, every one `zone = 0`.**
- 🔴 ⚠ **AND THE LOADER DOES NOT REFUSE THE BROKEN CASE.** `RulesetLoader.cs:1602-1617` bounds the bit at
  `0..15` and checks nothing else, so a `[[zone_rule]]` with `zone = 1` **loads clean, samples Lots for
  ever, fails the zone-bit term on every one of them, and builds nothing.** ***That is the
  loads-clean-and-does-nothing class this project refuses everywhere else***, and milestone 27 task 7 is
  the precedent for what it costs: `[[building]] jobs` was *"parsed and unread"* until `adr/0148` made it
  a refusal. **A refusal for a zone bit no Lot carries is a candidate output of this sitting.**

**W9 — ⚠ A SECOND `[[zone_rule]]` IS MECHANICALLY FINE, AND THE ASSUMPTION THAT BLOCKS IT IS NOT THE ONE
THE HEADER NAMES.** Nothing in the engine or loader assumes one Zone Rule: the engine loops over all of
them (`ZoneRuleEngine.cs:174`), the loader builds an unbounded list with **no cross-rule uniqueness check
on `kind` or `zone`** (`RulesetLoader.cs:1577-1631`), `ZoneSample.Draw` mixes the rule index into the draw
key so two rules sample different Lots (`ZoneSample.cs:104`), contention is resolved by declaration order,
and `RulesetShape` already compares zone rules count-then-elementwise. ***The real single-zone-rule
assumption is that the only demand term in the create predicate is residential*** — **W4** — **and that is
a content problem wearing a mechanism's clothes.**

**W10 — ⚠ `ZoneRuleDefinition` HAS EXACTLY FOUR FIELDS, which bounds what an answer to W-Q1 may be.**
`Ruleset.cs:694`: `(byte Kind, byte Zone, uint Interval, int RevisitTicks)`, plus two derived members —
`Admits => (ushort)(1 << Zone)` (`:702`) and `SampleFor` (`:737`). **There is no threshold, no capacity, no
priority and no capital field**, and the TOML `name` key is read for error messages and **discarded**
(`RulesetLoader.cs:1580`). ⚠ **So any answer to W-Q1 that reads something new either extends this record
or reads it from the `World`** — and the session should say which, because ***the first is Ruleset content
and the second is a mechanism milestone 26 would then own.***

**W11 — 🔴 TWO DOC COMMENTS ARE STALE IN THE SAME WAY MILESTONE 27 JUST CORRECTED, AND BOTH ARE OWED TO
[`0012`](0012-corpus-audit.md).** `UnpremisedTable.cs:19-25` still says *"IT SHIPS WITH ONE EXIT AND THAT
EXIT IS THE SINK … nothing tenants a Business … `World.CreateBusiness` has no `src/` caller"*, and
`PlacementEngine.cs:645-650` repeats *"nothing tenants a Business"* — ⚠ **four hundred lines above
`PlacementEngine.Tenant`, the method that does it** (`:563-630`, calling `World.Premise` at `:626`).
**`CreateBusiness` now has two production callers**, `World.cs:1335` (the gate) and `World.cs:2856`
(`Fit`). ⚠ **This is `0041` **G1**'s shape a third time** — ***the mechanism moved and the sentences
describing it did not*** — and it is the shape
[`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
predicts: **both are wrong about the trigger and right about where to look.**


---

## What the sitting concluded — 2026-08-25

**One question of four, and it moved the other three.**

**W12 — ✅ W-Q2 IS NEARLY DISCHARGED BY W-Q1'S ANSWER, and it was not obvious in advance.** A trade
raised on *unserved need in reach* is a trade whose premises **sell to reachable customers or do not
sell at all**. A shop with none earns nothing, fails its own Rule, and **failing a Rule is already what
condemns a Building** — `condemn_after` on the kind, pressure from continuous starvation (**W7**).
⚠ **It is not automatic and the sitting must still author one thing**: the trade needs a Rule it *can*
fail. ***A kind with no failable Rule is immortal by construction***, so the decline question is now
*what does a shop consume such that failing to get it should kill it* — one Ruleset decision rather than
a mechanism.

**W13 — 🔴 THE BRIEF MISREAD THE DEMAND-SCALAR RULE, AND SO HAS EVERY COMPRESSION OF IT.**
`CLAUDE.md` → *Things to be careful about* carries *"Don't add a demand scalar. There is no RCI meter.
The Unplaced Pool **is** the demand signal"*, and this brief read that as ***do not model demand***.
[`01 §`](../docs/01-player-experience.md) says the opposite in terms: ***"`CLAUDE.md`'s rule is do not
**add** a demand scalar — this one is not added, it is **counted**"***, and names what is actually
refused — ***"a synthesised scalar with no constituents … it cannot be interrogated when it is
wrong."*** ⚠ **So the test is CONSTITUENTS and not arithmetic**, and a magnitude assembled from named
rows each carrying a reason is not an RCI meter however large it gets.
🔴 **This is [`0012`](0012-corpus-audit.md) Cause 5's shape on a RULE rather than on a number** — the
compressed form in `CLAUDE.md` dropped the word *added*, and ***the clause that made the rule narrow
stayed where it was, doing nothing.*** **Owed to `0012`.**

**W14 — ✅ `02 §5` ALREADY SPECIFIED THE MECHANISM, SO THIS WAS *UNBUILT* RATHER THAN *UNDESIGNED*.**
The six-step UrbanSim loop's **step 3** is ***"Business placement: same shape; commercial seeks unserved
needs in reach"***, and the section closes on this sitting's whole thesis: ***"The residual is the demand
signal — and it is a list of specific frustrated Households with specific reasons, which the commercial
and development logic can read directly."*** ⚠ **`02 §5` marks steps 1, 3, 4 and 6 unbuilt on its own
face**, so [`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md)'s
classification was available for the asking. ***The session spent its first exchange deciding whether
demand could be modelled at all, and the design document had specified how eight months earlier.***

**W15 — 🔴 THE SIGNAL COSTS NOTHING BECAUSE MILESTONE 26 CREATES IT.** `rulesets/minimal.toml`'s
`restock` is **`inputs = []`** — four sundries a firing, drawn from nothing. ***That absence is the
shop.*** When `Scope.Pool` resolves and `restock` draws from a market, a Household with no reachable
seller **fails and starves**, and starvation is already a `Blocking` reason, a wait list and a
`StarvedSince` clock. **No new record, no new column, no new mechanism** — the demand signal is a query
over rows the purchase creates anyway.

**W16 — ⚠ TWO FAILURE MODES WERE FOUND IN THE ANSWER AND BOTH ARE FIXED IN THE ADR.** The **blink**:
starvation clears totally on one successful firing (`RuleEngine.cs:550`), so an intermittently starving
Household is invisible to whichever sample catches it fed — fixed by reading `tick - StarvedSince`, a
magnitude, on a column that already exists and arithmetic `ZoneRuleEngine.Worst` already performs. ⚠
***The user named this one*** — *it is only a snapshot in time.* The **overshoot**: several Lots sampled
in one pass all read the same starving Households and all build, so ***one hungry street gets five
shops, and how many is a property of the cadence rather than of the city*** — fixed by making the claim
subtractive on `02 §5` step 2e's rule, with a build-rate throttle **refused** because it bounds the work
without correcting the reading.

### Still open

| | What | Changed by the sitting? |
|---|---|---|
| **W-Q2** | What declines a Provider | ✅ **Shrank to one authoring decision** — *what does a shop consume such that failing kills it* (**W12**) |
| **W-Q3** | The land-use split | 🔴 **Now the expensive one.** No mechanism under it at all (**W8**) — one zone bit painted, no repaint path, and a Ruleset naming an unpainted bit loads clean and builds nothing |
| **W-Q4** | Which Ruleset carries the Provider | Unchanged, and ⚠ **it is now also a §D2 ratifier**: `adr/0163`'s two numbers name *milestone 26's own demonstration Ruleset* as the world, which is this question |

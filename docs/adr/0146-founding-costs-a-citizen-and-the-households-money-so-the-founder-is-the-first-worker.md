# Founding costs a citizen and the household's money, so the founder is the first worker

**Founding a Business spends **two** things: a **Citizen's labour** and the **Household's money**. The
Citizen is the subject — one Citizen, not a Household and not a group — and the money comes from the
Household because ***a Citizen has no Bin and a Household has one***. **The founder becomes the
Business's first worker**, so nothing records a founder separately: the founder is *the Citizen whose
Workplace is this Business*. ⚠ **No `founder` column is declared, and that is a decision rather than an
omission** — declaring one is easy today and becomes a **constructor cycle** the day
[`adr/0141`](0141-a-tenant-owns-what-leaves-with-it-and-the-premises-own-the-capacity.md)'s trade
owns the jobs and a workplace points at a Business.
`UNIQUE INDIVIDUALS` `SOLVE THE ACTUAL PROBLEM` `BOUNDED KNOWLEDGE`

⚠ **This AMENDS [`adr/0145`](0145-a-business-is-founded-by-a-household-or-arrives-through-a-gate-and-both-land-in-the-pool.md)
on *who founds*, and corrects a claim in it that was false as built.** `0145` says *"a Business founded
by a named Household has a founder the player can inspect"*. 🔴 **It did not, on the day it shipped**:
`BusinessTable` declares `building`, `kind`, `bin_head`, `bin_tail`, `balance`, `building_next` and
`pool_slot`, and **not one of them records where the money came from**. The link was severed the instant
`World.Found` moved the band. ***The argument was written for a build that did not exist and the code
shipped the same day without it*** — filed as [`plans/0012`](../../plans/0012-corpus-audit.md) **Cause 5**
against this corpus's own rule that a description of the build is *where to look* and never what you
found ([`adr/0093`](0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)).
**This ADR makes the sentence true** — by the employment link rather than by the column it implied.

## Why a Citizen and not a Household

**Because `adr/0005` already decided that individuals are Citizens**, and founding a business is the
most individual act in the economy. A Household founding a shop is a **group making one decision**,
which is the Cohort this design refuses by name.

**But the money stays with the Household, and the tables force it.** `CitizenTable` has no `bin_head`,
no `bin_tail` and no `balance`; `HouseholdTable` has all three. ***A Citizen owns no money in this
build, so a Citizen cannot capitalise anything.*** So founding reads:

> **A Citizen founds. Their Household pays.**

⚠ **That is not a compromise, it is the household economics this corpus already asserts.** `docs/04`
and `CONTEXT.md` put the purse at the Household throughout — income, expenses, savings, purchases made
and missed are all Household-scoped ([`adr/0004`](0004-typed-tables-over-ecs.md) cites exactly this
list). **A person starting a business out of the family savings is the ordinary case**, not a modelling
concession.

## Why the founder is recorded by the job and not by a column

**A `founder` column is trivial to add today and would be wrong tomorrow.** `World` builds `Citizens`
before `Businesses`, so `BusinessTable` could take `citizens.Rows` and declare
`Founder = _rows.SavedHandle("founder", citizens.Rows, reference: Reference.Severable)` — one column,
one constructor argument, on `Building`'s own Severable precedent.

⚠ **The problem arrives with milestone 27 task 7.** A job is meant to become a relationship between a
Citizen and a **Business**, which makes `CitizenTable` need `Businesses` while `BusinessTable` needs
`Citizens` — a **constructor cycle**, and the tables are constructed in one ordered pass.

**This corpus already has one answer to that and uses it everywhere**: ***one direction is a handle
column and the other is an intrusive index list on the owner.*** A Household points at its Dwelling and
the Building keeps `DwellingNext`; a Citizen points at its Workplace and the Building keeps `WorkerNext`;
a Business points at its premises and `World` keeps `BuildingBusinesses`. **So exactly one of
{founder-on-Business, workplace-on-Citizen} may be the handle, and the other is a list.**

***Choosing the workplace as the handle makes the founder fall out for free.*** The founder is the
Citizen whose `Workplace` resolves to this Business and whose employment says so. **No column, no cycle,
no second thing to keep consistent** — and a founder who later leaves stops being the founder, which is
the honest reading rather than a bug.

⚠ **This has a hard prerequisite and it is not built**: an unpremised Business has no Building, so a
`Workplace` handle addressing `BuildingTable` **cannot point at it**. ***Founding-as-job is unbuildable
until task 7 repoints the workplace handle***, and that ordering is now load-bearing rather than
incidental.

## What ships at 27 and what does not

**The labour cost ships. The income consequence does not, and it is *unbuilt* rather than deferred.**

The intent this ADR was asked to capture was *"the founder has a job that periodically has no income
until the Business has income."* 🔴 **There is no income in this build for that to be the absence of.**
`Readouts.cs` states it in its own doc comment: *"income is a **flow** that arrives with wages in
milestone 15"*, classified **unbuilt** under
[`adr/0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) — *"nothing is being
approximated; a different thing is being measured."*

⚠ **And the mechanism described is already DESIGNED, which settles how to treat it.**
[`adr/0026`](0026-wages-are-posted-locally-and-never-cleared.md) is *wages are posted locally and never
cleared*: **each Business posts a wage and adjusts it by its own fill rate**, anchored on the Hinterland
wage. ***So "the founder's job pays nothing until the Business earns" is not a gap in the design — it is
`adr/0026` running on a Business with an empty Bin***, and the Business's empty Bin is
precisely what [`adr/0114`](0114-a-balance-a-rule-can-fail-on-is-a-bin-and-a-bins-owner-is-discriminated.md)
built to be blamed and waited on — *"a Bin is the only thing a Rule can **blame** and the only thing a
blocked Rule can **wait on**."* **It is unbuilt, not undesigned**, and the distinction is
`adr/0070`'s own.

**So the founder forgoing a wage is not a thing 27 can build, cheapen or approximate.** Under `adr/0070`
the answer to *given wages do not exist, should founding compensate?* is **build wages** — at milestone
15, where they are already designed and scheduled. ⚠ ***Inventing a 27-shaped proxy for a wage would
put a second, worse answer in front of `adr/0026`***, and the day 15 lands somebody has to find and
delete it. ⚠ **What 27 ships is the half that is real today**: the founder is
**occupied**, so the employment pass will not hire them, and the city is one worker down. ***A labour
cost with no wage attached is still a cost, because a Citizen is a scarce thing.***

## The simplistic trigger, and what makes it a choice

**An unemployed, housed Citizen whose Household can cover the band.** Three predicates, all readable
off columns that exist:

- **Unemployed** — `Workplace` does not resolve. ⚠ **This is what makes it a CHOICE rather than two
  unrelated mechanisms**: the employment pass and the founding pass draw from *the same people*, and
  whichever reaches a Citizen first takes them. **Neither pass knows the other exists**, which is
  [`adr/0017`](0017-agents-satisfice-they-never-optimise.md) — nobody compares the two and nobody
  optimises.
- **Housed** — their Household's `Dwelling` resolves, carried unchanged from `0145`'s amendment and for
  its reason: a Household in the Unplaced Pool must not also be founding.
- **Affordable** — the Household's balance covers the founding band, which is `0145`'s *means and never
  need*, unchanged.

⚠ **The mechanism is reachable in every shipped world, and by construction.** `[[building]] jobs = 8`
sits on the `dwelling` kind and `CLAUDE.md` records the consequence: it *"puts full employment out of
reach by construction."* ***So there are always unemployed Citizens to draw*** — this is
[`plans/0040`](../../plans/0040-the-business-is-the-actor-and-the-building-is-premises.md) **F43**'s
test passing before the code is written rather than after.

**Deliberately simplistic, and named as such.** Nothing here models ambition, risk appetite, skill or
opportunity cost. A Citizen does not *weigh* founding against a job; they are simply available, and one
of two passes reaches them. ***That is the whole model and it is a starting position*** — recorded so
that the day it is replaced, what is being replaced is legible.

## Consequences

- **`World.Found` changes subject** — `Found(Handle<Household>, …)` becomes `Found(Handle<Citizen>, …)`,
  resolving the founder's Household to find the Bin. **The money path is unchanged**; only who is asked
  moves.
- **The founding draw moves from Households to Citizens.** ⚠ **The derived-sample argument
  ([`adr/0059`](0059-a-zone-rules-sample-is-a-revisit-period-so-the-ruleset-states-a-duration.md))
  transfers unmodified, but the population it divides is now the LARGEST in the city.** `plans/0013`
  should expect a bigger row than the Household draw would have produced.
- **`Citizens.Employment` gets its first meaning, and it currently has none.** The column is `Saved`,
  therefore hashed, and the **only writer anywhere in the repository** is `GoldenFixtures.cs:510`,
  `(byte)(i % 3)` — an arbitrary value in a fixture. 🔴 ***A saved, hashed column that no simulation
  code writes or reads is a coverage hole the State Hash cannot see***, filed to
  [`plans/0012`](../../plans/0012-corpus-audit.md). Defining a founder state here is **defining the
  column**, not extending it.
- **A new `purpose_tag` is NOT needed.** `FoundingDraw` (25) and `FoundingTrade` (26) already exist and
  already do this job; what changes is the *population* the draw indexes, not the decision being made.
- **`adr/0145`'s export consequence is unchanged and now costs more.** A founded Business that never
  finds premises still exports its founder's money — and now the founder is **occupied by it**. ⚠ ***So
  found-then-fail leaks a worker as well as a balance***, and the worker comes back when the Business
  departs while the money does not.
- **Task 7 becomes a prerequisite of the founder record**, where before it was parallel work.

## Rejected

**Declaring a `founder` column anyway.** It works today and is one line. Rejected because it makes the
cycle at task 7 a thing to solve rather than a thing avoided, and because ***two records of one
relationship drift***: a founder column and a workplace handle would both claim to say who runs the
shop, and nothing would reconcile them when the founder is replaced.

**Founding as a Household decision, as shipped.** What `0145` built, and it is a Cohort — a group
holding one intention. ⚠ **It also cannot cost labour at all**, because a Household is not a thing that
works, so the version shipped this morning could only ever spend money.

**Letting an employed Citizen found and vacating the job.** More expressive and rejected for now: it
requires a claim about *why* somebody would leave a job, and this build has no wage, no satisfaction and
no ambition to found the claim on. ⚠ ***Under `adr/0070` that is an argument to build one of those, not
to invent a proxy.***

**A founding cost in Citizens greater than one.** A partnership, or a Household putting two members in.
Rejected as unmotivated: nothing downstream reads how many people a Business has yet, so the second
Citizen would be a cost with no consequence. **It is the obvious first extension the day `jobs` reaches
a Business.**

## What would trigger revisiting

- **Milestone 15 landing [`adr/0026`](0026-wages-are-posted-locally-and-never-cleared.md)'s wages.**
  The income half of this decision becomes buildable, and the founder forgoing a wage stops being
  unbuilt. ⚠ **A founder drawing a posted wage from a Business they own is a case `0026` does not
  discuss**, and it is the first thing to check on that day. ***This ADR should be re-read then rather
  than rediscovered.***
- **Task 7 choosing founder-on-Business as the handle instead.** If repointing the workplace turns out
  to be harder than the reverse, the choice inverts and this ADR's *no column* becomes wrong. **The
  reasoning survives; only the direction flips.**
- **Unemployment going to zero in a shipped world.** The trigger's reachability rests on `jobs = 8` on
  the `dwelling` kind. ⚠ **That number is tuning and hot-reloadable**, so a Ruleset that supplies enough
  jobs makes founding unreachable — and then the *unemployed* predicate is the wrong one.
- **Founders proving indistinguishable from hires.** If nothing ever reads *who founded this*, the
  employment link is carrying a distinction nobody consumes, and either the distinction earns a reader
  or the labour cost is the whole mechanism.

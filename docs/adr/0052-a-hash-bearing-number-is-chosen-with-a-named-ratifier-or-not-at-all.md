# A hash-bearing number is chosen with a named ratifier or not at all

**A number that changes the State Hash, or that is frozen at world creation, may not be written down
without two things recorded beside it on the same day: the *named* thing that would ratify it, and
the trigger that would make somebody revisit it.** Not a category — a spike id, a session letter, a
document, a measurement with the quantity written out. If no such thing can be named, that is the
finding, and it is reported rather than worked around.
`SOLVE THE ACTUAL PROBLEM` `HONEST DEGRADATION`

**This does not require ratifying a number before choosing it.** Often that is impossible and forcing
it is worse than the disease — a number ratified early against machinery that does not exist is wrong
*and* load-bearing. The rule governs the record, not the timing.

⚠ **AMENDED 2026-08-15: a named ratifier names a machine *and a world*.** An instrument with an owner,
a date and a written-out quantity still cannot fire if the only city available to point it at is one in
which the number does not vary — and that failure is **worse than an unnamed ratifier**, because the
row reads as discharged the moment the instrument runs. See §*A ratifier names a machine and a world*.

## Why

**These numbers are the expensive class and the corpus already says so.** `05 §4`'s test is that a
change is an optimisation if the State Hash is unchanged and a design change otherwise. So a
hash-bearing number moving means every recorded baseline, every golden trace and every balance
observation taken before the move was taken against a **different city**. The cost of changing one
therefore grows with everything built after it, which is the opposite of an ordinary tuning number and
is why the two cannot share a discipline.

**[`adr/0044`](0044-the-map-layer-diffusion-cadence-is-the-designers-number-not-the-profilers.md) is
the worked example, and it went wrong twice.** The Map Layer cadence was chosen by argument, cited
across three documents as settled, and had to be *measured* back out. The ADR that did the measuring
then got its own second half wrong by argument — filing the cadence as world-creation-fixed while
citing [`adr/0015`](0015-all-tuning-data-is-hot-reloadable.md) without running the membership test
`adr/0015` states. It was withdrawn and recorded rather than amended away, and the finding that
outlived it is **citing an ADR is not applying it**. A named ratifier is the smallest thing that would
have caught either half: the first because nobody could have named one, the second because naming
`adr/0015` would have meant running its test.

**The register had eighteen rows and had never lost one.** [`plans/0002`](../../plans/0002-open-questions.md)
§D, against [`0012`](../../plans/0012-corpus-audit.md)'s 39 struck of 51. That asymmetry was read as
*numbers never get fixed here*, and the triage that followed found something more useful: **five of
the eighteen were not unratified numbers at all** — a convention with no fact of the matter, three
provenance stamps on measurements, and a defective derivation being carried in two places. Of the
thirteen remaining, **seven were unset**, and an **unset number is a gap rather than a debt**: it
blocks work and cannot corrupt it, because nothing accretes on a value that does not exist. The real
debt was six rows.

**So the problem was never the count. It was that the list could not be read.** Rows nobody could act
on sat beside rows rotting quietly, in one undifferentiated table, and the effect was that none of
them looked urgent. This ADR exists to keep that distinction alive at the moment it is cheapest to
record — the day the number is chosen — rather than to be reconstructed by a triage later.

**And the rule is enforceable by a question with a yes-or-no answer**, which is what makes it a rule
rather than an aspiration: *name the thing that would tell us this is wrong.* If the answer is a
category — "a profile", "a spike", "balance testing" — it is not named and the row is not complete.
This is [`adr/0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)'s
test applied to a number instead of a claim: `0043` asks *can you name the number that would refute
this?*, and this asks the same question of the number itself.

## Consequences

**Two extra sentences at the moment of choosing, and nothing else.** This is deliberately the cheapest
discipline that could work. It adds no gate, no review and no approval.

**`0002` §D is the register, and it is now three tables rather than one.** D1 is in use and unratified
— the debt. D2 is unset, each row naming the machinery it waits for, so *not yet* stops reading as
*neglected*. D3 records what was moved out and why, so it is not re-added. A row that cannot be placed
in D1 or D2 is very likely a D3.

**The two numbers being chosen right now are the first test**: the pollution decay rate tau
([`adr/0051`](0051-industrial-pollution-is-a-stock-the-environment-absorbs.md)) and the Rule Instance
arming stagger (slice 7 task 10a). Both are hash-bearing, both are being chosen because the work
cannot proceed without *some* value, and both are exactly the shape this rule was written for — a
number picked by necessity rather than by argument.

**Being unable to name a ratifier is a result, not a blocker.** It means nobody knows how the number
could ever be shown wrong, which is worth surfacing at once. The number may still be chosen; what may
not happen is choosing it quietly.

**The failure mode to watch is the formality.** *"Ratified by: a future spike"* satisfies the letter
and defeats the purpose. A ratifier that names no owner and no quantity is not one, and a row carrying
one should be treated as unfilled.

### Changing a hash-bearing number is **cheap today**, and this rule is a habit being built ahead of its cost

The *Why* above prices the move as *"every recorded baseline, every golden trace and every balance
observation was taken against a different city."* True, and it is worth being precise about which of
those exist:

| Accretion | Cost of a hash-bearing move | Exists today |
|---|---|---|
| Golden traces and baselines | **re-record them.** Mechanical, minutes, already done routinely — slice 10 re-recorded both on a `byte`→`ushort` widening | yes, and cheap |
| **Citations across the corpus** | **the expensive one.** `adr/0044` had to *measure* the Map Layer cadence back out of **three documents** that cited it as settled | yes, and not cheap |
| Balance knowledge — what a good Ruleset looks like | none. `rulesets/minimal.toml` says in its own header that it models no city | **no** |
| **Player saves** | unbounded, and there is no migration story | **no** |

**So the weight this decision will one day carry is not the weight it carries now.** There is no game,
no player and no save; the *only* accretion with real cost today is citation, which is why `adr/0044`
was painful without a single player existing. Read the rule as **a habit rehearsed while it is free**,
not as a reason for caution about picking numbers.

**And it must not be cited to slow anything down.** The board has already recorded one session where
*"the design was generating design"* and the correction was **build**. A rule whose only cost is two
sentences may not become a reason to defer a choice, to open a session, or to treat a number as
dangerous. **`adr/0043`'s companion rule is the one with teeth right now** — do not settle by argument
what a measurement could settle. This one is bookkeeping, and bookkeeping that starts costing more
than it saves has stopped working.

## A ratifier names a machine and a world

*Amendment, 2026-08-15. The rule above is unchanged and this adds a second half to it.*

**The failure mode this ADR already names is the formality** — *"ratified by: a future spike"*, a
ratifier with no owner and no quantity. Milestone 5c produced a different one, and it passes every test
written above.

Four hash-bearing numbers — `[traffic] alpha_percent`, `beta` and `clamp_percent` from
[`0099`](0099-a-legs-cost-is-a-plan-and-a-drive-is-priced-segment-by-segment-as-it-is-met.md), and
`[households] car_ownership_percent` from
[`0098`](0098-a-citizen-travels-in-their-households-mode-and-mode-choice-is-undesigned-rather-than-unbuilt.md)
— were recorded with **5c task 8's long run** as their named ratifier. That is an owner, a date, a
Ruleset (`rulesets/congested.toml`, written for the purpose), and refuting readings stated in both
directions. It is everything this ADR asks for.

**Task 8 ran, and it cannot ratify any of the four.** The load on a Segment came out at **0.0018 /
0.0048 / 0.0110 Vehicles per Segment per Tick** at 4,000 / 16,000 / 64,000 Citizens, growing as roughly
`P^0.66` — so at 1,000,000 it is about **0.07**, against a Segment that holds **1.02** Vehicles at
`congested.toml`'s capacity and **9.2** at the shipped one. BPR is only ever evaluated on the stretch
where it is nearly flat, at every population this project can generate.

**The cause is structural rather than a matter of run length, and it is another decision working as
designed.** [`0090`](0090-the-generator-makes-land-and-the-player-makes-every-road.md) gives the
generator land and the player every road, and `CommandKind.Populate` sizes the paved lattice from the
population it serves — so ***the same number sizes both the demand and the supply***, and `v/c` peaks
at 0.44 whatever the population. Congestion is a property of a network's **shape**, and the only thing
in this build that produces shape is `CommandKind.Connect`. A Ruleset can scale the lattice and cannot
bend it. **No length of run over a generated city varies the quantity, so no length of run can refute
the number.**

### The tell, and why it is worse than an unnamed ratifier

An unnamed ratifier announces itself: the row is visibly incomplete and nobody thinks the number is
settled. **A ratifier that names only the instrument is discharged by running the instrument** — the
spike id gets a tick, the row loses its ⚠, and a number that has never been exposed to a world in
which it could be wrong now reads as measured. That is `plans/0012` **Cause 5**'s shape one level up:
not a figure travelling without its caveat, but a *status* travelling without the condition it was
earned under.

**So the question this ADR enforces gains a second clause.** It asked *name the thing that would tell
us this is wrong*; it now asks:

> *Name the thing that would tell us this is wrong — **and the world you would run it in, in which this
> quantity actually varies.***

If the second answer is a city the project can build today, name it. If it is not, then **the world is
the ratifier's real blocker and the row must say so** — which routes it to
[`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md)'s *build X*, with X being the world
rather than a mechanism. A row in that state is still honestly recorded; what it may not do is name the
instrument alone and let a run discharge it.

### Two corollaries

**A ratifier that has run and could not fire is replaced, never struck.** Striking it leaves the row
bare, and a bare row in D1 reads as one nobody has got to yet rather than one that has already defeated
an attempt. The replacement carries what the attempt found, because *that* is the durable result: the
four rows above now name a world with an under-provisioned network laid by `CommandKind.Connect`, and
they say why the generated one could not serve.

⚠ **[`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md) has the same
gap and inherits this amendment by the same argument.** That rule types a claim by asking *can you name
the number that would refute this, and the machine that would produce it?* — two terms, and the world is
the missing third exactly as it was here. It is not amended in its own file because nothing has yet gone
wrong there; this sentence is the warning rather than the correction, and the first `adr/0043` claim
that turns out to be void for want of a world should carry it over.

## What would trigger revisiting

- **D1 stays empty for a long stretch.** If nothing accumulates, the discipline has done its job and
  is ceremony; retire it rather than perform it.
- **A hash-bearing number turns out to be ratifiable only by players**, after ship. The rule assumes
  a ratifier that exists inside the project's own machinery, and a number whose only honest ratifier
  is a live audience is a case it does not handle.
- **The State Hash stops being the discriminator** — a second hash, a tolerance, or any change to
  `05 §4`'s optimisation-versus-design-change test would move the boundary this rule is drawn on.
- **The rule starts being read as a bar on choosing.** It is not, and if it is being cited to block
  work rather than to record it, the wording has failed and should be fixed here rather than
  reinterpreted in argument.
- **A second ratifier is defeated by its world rather than by its instrument.** One sighting is a
  finding and two is a pattern: if it happens again, the register wants a *world* column rather than a
  clause inside the ratifier cell, so that the rows blocked on a city the project cannot yet build can
  be read off at a glance instead of by reading every cell.

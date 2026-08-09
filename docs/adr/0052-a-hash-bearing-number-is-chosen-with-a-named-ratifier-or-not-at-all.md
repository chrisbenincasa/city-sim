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

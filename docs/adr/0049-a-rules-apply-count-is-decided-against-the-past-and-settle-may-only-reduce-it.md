# A Rule's apply count is decided against the Past, and Settle may only reduce it

**A Bin Rule's apply count is decided in Phase 2 against the Past. Phase 3's re-check may *reduce* that count, or fail the Rule outright, and may never *raise* it.** So Settle serves a greedy Rule **short** rather than enlarging it, and the settle shuffle decides **who goes short when there is not enough** — never **how much anyone takes when there is**. `LEGIBLE CAUSE` `HONEST DEGRADATION`

## Why

### The question does not exist until an apply count can exceed one

Slice 7 task 5 gave Phase 3 a re-check for one reason: Phase 2 evaluates every due Rule against a
single state, and by the time a given intent is reached, earlier intents have moved Bins. While every
count was one, that re-check had exactly two outcomes — the intent still fits, or it does not — so
*which way it may move* was not a question anybody had to answer, and no document answers it.

Task 6's greedy count makes it one. An earlier intent may have **deposited** into a Bin this Rule
draws from, so the Rule can now afford more than it decided on.

### Lowering is forced; raising is merely available

The asymmetry is the whole argument. A Rule cannot spend what the winner of a contested draw already
took — conservation forbids it, and a re-check that could not lower would be a re-check that permits
a negative Bin. Raising is not forced by anything. It falls out for free if Phase 3 re-derives the
count from scratch instead of carrying the decided one down, which is why it arrives unnoticed.

### The worked case, and what it costs

One Building. One flour Bin. A producer **P** making 24 flour, fixed. A greedy consumer **C** eating
6 at a time, `apply = { min = 1, max = 4 }`. The Tick opens with **12** flour.

Phase 2, read-only: P intends to make 24; C sees 12 flour, computes two helpings against a max of
four, and intends **2**.

| Settle order | Free to re-derive | This decision |
|---|---|---|
| C, then P | C eats 12 → Bin **24** | C eats 12 → Bin **24** |
| P, then C | C sees 36, eats 24 → Bin **12** | C eats 12 → Bin **24** |

**Under re-derivation the same city on the same Tick eats either 12 or 24 flour depending on a
hash.** Under this decision the order is unobservable in the total.

### `adr/0037` already names the state a Rule decides against

[`0037`](0037-the-world-is-single-buffered-and-hazards-are-per-table.md) is explicit about what Phase
2 sees:

> **The Past is not a second copy.** It is *the state as of the start of this Tick*, and Phase 2
> observes it because nothing has written yet.

A count re-derived from part-way through Phase 3 is not a count decided against the Past. This ADR is
therefore closer to an application of `0037` than to a new principle — but the observable it governs
is *how much the city consumes*, which under `05 §4` is a different city, so it is recorded where a
reader will find it rather than left implicit.

### The shuffle is a tie-breaker, and a tie-breaker must not become a mechanism

`02 §8` rule 5 introduces the settle shuffle as fairness machinery: a contested outcome is settled by
a counter-based shuffle, never by arrival and never by entity id, because ordering by id is *biased* —
the same Building would win every contested draw for the life of the city. That claim is measured, not
argued: replacing the draw with the slot index makes it **32–0** (`plans/0011`, finding 16).

The order is drawn from `hash(world_seed, instance_id, tick, purpose_tag)`. It is **deliberately
arbitrary**, and that is a virtue in a tie-breaker and a defect in anything else. Order-dependence
under scarcity is irreducible — six flour, two Rules that each want six, and atomicity forbids
splitting one application in half, so somebody eats and somebody waits. Order-dependence in the
*total consumed* is not forced by anything. This decision removes exactly the avoidable half.

### The alternative was a natural order, and it reproduces a defect `02 §5` names

If the settle order were *principled* rather than arbitrary, the objection would weaken. The candidate
is a production order — everything that fills a Bin before everything that drains it. It was not
dismissed cheaply: `04 §1` commits to *two extraction steps, three processing steps, maximum chain
depth of three*, so the graph is a shallow DAG and a topological sort over Bins is feasible.

It fails on four counts.

**"Producer" is not a property of a Rule.** The corpus's own worked example is the bakery — six flour
in, four bread out — a consumer of flour and a producer of bread simultaneously. There is no two-way
partition of the due list to sort; only a topological order over *Bins*.

**It does not remove the shuffle.** Two bakeries drawing the same flour Bin sit at the same
topological level and still need a tie-break. Two mechanisms replace one, and the arbitrariness is
pushed down a level rather than eliminated.

**It reintroduces the unauthored lag.** `02 §5`, on why subscription replaced polling:

> Under polling, each level of an `on_fail` chain carries its own rate and its own phase offset —
> whichever Tick that Building was built on — so an arriving Shipment reaches the thing that needed
> it after a delay determined by construction order. **Deterministic, but unreadable and unauthored.**

Under a production order, whether a chain collapses within a single Tick depends on whether its Rules
are **due together**, which depends on their phase offsets, which are set by when each Building was
built. Two identical cities whose mill and bakery were placed one Tick apart propagate flour at
different speeds. That is the defect verbatim, re-entering through the settle order.

**And it would not settle this question anyway.** Under a production order the consumer *always* sees
the enlarged Bin, so the raise becomes universal rather than a coin flip — and Phase 2 stops deciding
anything at all for greedy Rules.

### Serving short is not a partial application, and all-or-nothing already has a spelling

The stricter reading — Phase 3 honours the decided count or fails the Rule outright — is simpler and
was considered. It is wrong for the case greedy exists to serve: a bakery holding two sacks of flour
with room for four bakings **bakes two**. `02 §4.1`'s atomicity forbids half-applying *one*
application, because that consumes Goods that become nothing; two whole applications out of an
intended four are two whole applications, each atomic, each conserved.

It is also unnecessary as a policy, because the band already expresses it per Rule: a Rule that must
not be served short writes **`min = max`**, and then "afford less than the floor" and "afford less
than I decided" are the same test. Making all-or-nothing global would delete greedy Rules to obtain
something a designer can already ask for one Rule at a time.

### Greedy is not hypothetical

`02 §4` names the load-bearing user, and it is not a bakery's flour:

> Staffing already dissolves into a **labour input Bin** filled by arriving commute Trips, and
> experience folds into it as a **per-worker deposit multiplier** […] **greedy apply then scales
> throughput with experience for free.**

Every Building that employs anyone is greedy on its labour Bin. A fixed count would mean a factory
with twice the workforce produces exactly what one with half produces, and the labour system would
never reach the economy.

## Consequences

- **`RuleEngine.Check` takes a ceiling.** Phase 2 passes no bound and gets the Rule's own `max`;
  Phase 3 passes the count Phase 2 decided. In the source this reads as
  `Check(verdict.Instance, verdict.Applications)` — **a parameter that looks redundant beside a
  re-check that re-derives everything else, and is not.** Anybody reaching to delete it should find
  this ADR.
- **The property is mutation-tested, not asserted.** Re-checking unbounded leaves 12 flour where this
  decision leaves 24, and fails exactly one test by name.
- **A greedy Rule may leave Goods on the table it could have eaten**, until its next due Tick. This
  is deliberate. Waking it early on a deposit would make its cadence depend on deposit traffic rather
  than on its rate, and worse, on how supply is **chunked**: 24 flour arriving as one deposit wakes it
  once, as four deposits of six wakes it four times. Same Goods, different city. The rate is the
  author's statement of how often an actor acts.
- **The lag this leaves is authored.** It is the rate, written in the Ruleset, and legible there —
  which is the property `02 §5` demanded and polling could not give.
- **Reversing this moves the State Hash**, so it is a design change under `05 §4` however it is
  motivated, and not something a later optimisation pass may take.
- **The settle shuffle's remit shrinks to one sentence** and should be quoted as such: it decides who
  goes short when there is not enough, never how much anyone takes when there is.
- **This governs a count decided in Phase 2, and an `on_fail` link first reached in Phase 3 is
  therefore outside it** — not a hole in the rule but a different subject. `02 §4.1` requires a
  Phase 3 atomicity loser to take its fallback, and a link reached that way was never evaluated
  against the Past, so there is no count to reduce. Nor is the harm above in play: what this ADR
  forbids is a *producer's throughput* moving with shuffle position, and a rescue drawn from a
  contended shared Bin is the other case entirely — who gets scarce Pool flour is exactly what the
  shuffle is for. Slice 7 task 8 walks the chain in Phase 3 and serves the link normally.

## What would trigger revisiting

- **Conserved Money.** Money is the one Bin every Rule in the city touches, and `04 §3` moves it by
  wages, purchases, rents and taxes. This decision becomes *more* load-bearing, not less: without it
  the shuffle would set how much money changes hands per Tick. If the arithmetic of a purchase turns
  out to need mid-Tick state, that is the trigger — and see the decision owed in
  [`plans/0011`](../../plans/0011-rule-engine-bins-and-rules.md), that a Rule term is a Ruleset
  constant while `04 §4` makes a price per-District and per-Day.
- **The District Pool.** Today only Rules on a single Building can collide, because `pool` and
  `global` are named holes that throw. When a Pool Bin is written by many Buildings within one Phase
  3, this case stops being narrow and the quantities stop being small.
- **A measured case where a rate's worth of surplus is visibly wrong** — a Building sitting on inputs
  the player can see while a shortage is reported downstream. That is a `LEGIBLE CAUSE` failure and
  would reopen the early-wake option this ADR rejects. Under
  [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md) that claim is
  **measurable** — the number is how long a Building holds usable inputs while a downstream Bin is
  short — and **nothing has measured it**. The rejection above is an argument about mechanism
  (cadence must not follow deposit traffic), not a claim that the surplus is never visible.

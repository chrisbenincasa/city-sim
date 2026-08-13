# A Commute Budget is three rungs and only the last one refuses

**A commute is *fast* to 20 clock minutes, *moderate* to 40, *unsavoury* to 50, and refused beyond it.**
`commute_budget_minutes` stops meaning *the acceptable commute* and starts meaning **the ceiling** — the
one edge that produces a Trip Fate. The two edges below it refuse nothing; they grade a commute that
happens anyway, so a deteriorating city is legible **before** anything fails rather than only after.

Guiding concepts: `LEGIBLE CAUSE`, `NO VERDICT`, `BOUNDED KNOWLEDGE`.

**Arguable** under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md) as
to the shape — whether a graded budget beats a threshold is a question about what a player can read, and
no measurement decides it. **The three values are not arguable** and carry a named ratifier under
[`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md).

## Why

### A single threshold makes a cliff out of the one rule that is supposed to be graded

[`0017`](0017-agents-satisfice-they-never-optimise.md) is the design's rule for how anybody chooses anything:
take the first option that is good enough, not the best one. 5b-bis task 4 built the first real instance
of it — a job seeker draws `candidates` Buildings from a box around home and takes the first with a free
slot it can walk to inside the Budget — and the *good enough* test it was given is a **step function**.
A walk of 19.9 minutes is a job; a walk of 20.1 minutes is unemployment. Nothing in between exists.

That is wrong twice over. It is wrong about people, who take a bad commute rather than no job and resent
it. And it is wrong about **what the player is shown**: the only quantity the threshold produces is a
count of refusals, which has no shape. A city whose commutes are all creeping from 12 minutes to 19 is
deteriorating and reports **zero**, and then reports a cliff.

`01 §4` puts its whole two-hour marker on this mechanism — *"commutes lengthen, and Trips start failing
their **Commute Budget**"*, and the fix is *"not more money — it is geography"*. **A binary budget cannot
teach that lesson**, because the signal arrives at the moment the geography has already gone wrong. Three
rungs make the intervening period observable, and the intervening period is where a spatial fix is still
cheap.

### Three rungs and no more, and the top one is the only one with teeth

| Rung | Clock minutes | What it does |
|---|---|---|
| **fast** | to **20** | nothing. The commute is unremarkable |
| **moderate** | to **40** | nothing mechanically. It is what the player reads |
| **unsavoury** | to **50** | the Trip completes and the Citizen is a candidate for the Departure channel |
| — | beyond 50 | the Trip's Fate is **exceeded commute budget** |

**Only the ceiling refuses**, and that is what keeps this from being three thresholds. Two of the edges
are an **instrument's resolution** — the same argument 5b-bis task 6 made for the Trip cost histogram's
seven bands, where the ladder is deliberately not denominated in the Budget because *a ruler must not
move with the thing it measures*. That decision pays off immediately here: the histogram does not have to
change, and it is the uncensored reading the rungs are read against.

**The *unsavoury* rung is the one with a consequence, and the consequence already exists.** `01 §4`
distinguishes **unhoused** Departures (a capacity failure — build more) from **housed** ones (a quality
failure — fix what you have), and a housed Citizen on a fifty-minute commute is the cleanest quality
failure the design has. So the top rung is where retention pressure comes from, and it needs no new
mechanism to be worth naming.

### It does not open the Trip Fate set, and that is the test `adr/0076` invited

[`0076`](0076-the-trip-fate-set-is-closed-at-four-and-a-fate-names-the-journey.md) closes the Fate set at
four with two clauses, and the second is *"anything that arrives as **time** is scored by the Commute
Budget, which is not a Fate."* Three rungs are three gradations of a cost in time, so all three sit on
the Budget's side of that line and **none of them is a Fate**. A Trip on an unsavoury commute *completes*.

That ADR asked to be refuted by *"a journey ending in a way neither clause covers"*, and this is the
fourth proposal in the corpus that looked like it might and does not. The closure holds.

### The seeker takes the best rung it drew, not the first acceptable one

The mechanical change is one line of intent in the assignment pass. Today it accepts the **first**
candidate that passes; with rungs it accepts the **best rung among its `candidates` draws**, and only
widens to a worse rung when no better one is available. That is satisficing with an ordered preference,
which is what `adr/0017` describes and what a binary test cannot express.

**And it makes `candidates` ratifiable for the first time in the way 5b-bis task 4 predicted.** That task
recorded that its copy of `candidates = 3` was *"ratifiable for the first time here, because placement's
copy scores every candidate identically while this one filters on a real walk"* — the filter was still
binary, so three draws and one draw differed only in hit rate. With rungs the draws differ in **outcome**,
and the number now decides how good a commute the city can find, which is a thing a long run can measure.

**The search box is derived from the ceiling**, not from the fast rung, because a seeker will take an
unsavoury job rather than none and a box that cannot contain one would refuse it before the walk did.
That is 5b-bis task 4's rule unchanged — *looking beyond what could be accepted is looking where nothing
can be found* — with *accepted* now meaning the ceiling.

### What it does to the map, and `adr/0089` survives on the recomputation

[`0089`](0089-the-map-is-sized-by-how-many-commutes-fit-across-it.md) sized the map on **how many Commute
Budgets fit across it**, at **3.7–5.2 for a 30-minute Budget** over 65.5 km — implying a 25–35 km/h
effective road speed. Thirty minutes is a rung that no longer exists, so the ratio has to be restated,
and it does not have one value any more:

| Rung | Reach at 25–35 km/h | Budgets across a 65.5 km map |
|---|---|---|
| fast (20 min) | 8.4–11.8 km | **5.6–7.8** |
| unsavoury (50 min) | 21.0–29.5 km | **2.2–3.1** |

**The rung that decides whether two places are separate towns is the ceiling**, because two places are
one labour market exactly when somebody will take the commute between them — and somebody will, up to 50
minutes. So `adr/0089`'s figure for this design is **2.2–3.1**, below the 3.7–5.2 it quoted and far
above the **0.9** that condemned the old map. `WorldCells = 512` survives: two or three separable
settlements is not the single blob that ADR exists to prevent. **It survives with less headroom than it
was granted**, which is stated rather than smoothed, and *which rung governs separability* is filed as a
measurable question — S2 R1.5 already answered a question of exactly this shape in a column nobody read.

## Consequences

**`[trips] commute_budget_minutes` is the ceiling and two keys join it.** Three hash-bearing numbers
where there was one, all in the Ruleset and all hot-reloadable. The loader refuses a set that is not
strictly increasing, which is the one guard this shape needs and which must ship **with a test** —
`adr/0064` recorded a loader guard that existed for a slice with no test and was therefore invisible to
the reader deciding it did not exist.

**`[jobs]` still cannot load without a Commute Budget**, and the box is now derived from the ceiling. The
box's area grows 6.25× against the old 20-minute derivation, so the candidate draw hits Buildings at the
same density over more ground — which is the direction 5b-bis task 4's *Cell-uniform draw finds nobody*
finding cares about, and it should be re-read against the new box rather than assumed to still hold.

**The Census gains rung counts, not a fifth Fate.** `JobCounter`'s four counters become five or six, and
`TripCounter`'s four Fates are untouched. A Citizen's rung is derivable from its Trip cost and is not a
stored column.

**`01 §4` gains the graded reading** and `01 §7`'s Commute Budget wedge on the sun arc becomes three
shades of one wedge rather than a single edge. That section's claim — *"the budget and the day become one
visual object, so there is no conversion between them to be dishonest about"* — is unaffected and is why
the rungs are drawn there rather than in a panel.

**Nothing here is built.** The rungs are a design decision taken in a session; the implementation is a
later milestone's, and until it lands `[trips]` states one number meaning the old thing.

## What would trigger revisiting

**Playtest, which is the ratifier for all three values.** The refuting observations are named: if the
**unsavoury** rung is never occupied in a long run the ceiling is too low to be reached and the top band
is decoration; if the **fast** rung is never left, the city is too small or the box too tight for the
grading to do anything and the rungs are three names for one state. Either is a number a long run
reports directly.

**A fourth rung being reached for.** Three is a judgement about how many distinctions a player can hold
while reading a map, not a property of commuting. A proposal for a fourth is evidence the rungs are being
used as a scale rather than as a vocabulary, and the right answer then is the histogram, which already
has seven bands and refuses to move with the Budget.

**The ceiling turning out to govern the map.** If `adr/0089`'s ratio is what sizes the world and the
ceiling is what sets the ratio, then a playtest that moves the ceiling moves the map, and those two
decisions should be made together rather than in the order they happened to be taken here.

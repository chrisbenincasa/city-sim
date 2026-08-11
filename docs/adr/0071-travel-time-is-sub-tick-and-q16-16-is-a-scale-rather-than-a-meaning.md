# Travel time is sub-Tick, and Q16.16 is a scale rather than a meaning

**Travel time and speed are carried at the Q16.16 scale, each as its own type.** A traversal cost is
**Q16.16 Ticks**, not whole Ticks; a free-flow speed is **Q16.16 Tiles/Tick**; and a sub-Tile position
keeps the spelling it has. All three are distinct `readonly record struct`s over an `int`, sharing one
implementation in `Borough.Core.Arithmetic.Fixed`, and none of them is assignable to another.
**`05 §121`'s *"Q16.16 is for sub-Tile positions and nothing else"* is amended**: Q16.16 is a
**scale**, the quantities carried at it are enumerated, and the defect that sentence exists to prevent
is a **raw `int` in a Q16.16 role** — which a type prevents and a prohibition does not.

Guiding concepts: `LEGIBLE CAUSE`, `SOLVE THE ACTUAL PROBLEM`.

This is an **arguable** claim under [`adr/0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)
and is settled here rather than routed to a spike, because the two candidate representations are *the
same bits*. No measurement distinguishes them. What differs is what the corpus's own rules say, and
that is a question for a sitting.

## Why

**The cost function must be time.** `02 §5.9` states the constraint and names the game that broke it:
*"the cost function used for routing must be the same quantity used to judge trip failure, and the
same quantity shown to the player. SC4 routed on distance while the player was scored on time, and
the traffic system became unlearnable as a result."* The Commute Budget is drawn as a wedge on the sun
arc (`01 §7`), so the quantity a player sees is time. `02 §2` already commits the core to it:
*"Vehicle speed is stored as Tiles per Tick; a commute is N Ticks."*

**Whole Ticks would reproduce the SC4 failure with better manners.** A Tick is 1/8192 of a Day and
R2a's arithmetic in [`plans/0010`](../../plans/0010-s2-routing.md) puts a vehicle at roughly **one
Segment per Tick**. A 32-Tile Street traversed at 50 km/h is **0.87 Ticks**; so is a 4-Tile stub, and
so is a 60-Tile run, once each is rounded to a whole Tick. A\* over that graph minimises **hop count**
— and does it *while appearing to route on time*, because every column is labelled Ticks and every
panel would say Ticks. That is worse than routing on distance openly: the quantity optimised and the
quantity shown have diverged, and nothing in the code marks the seam. So the cost is sub-Tick, and
only the representation is open.

**The two candidate representations are the same representation.** Q16.16 Ticks, or an integer count
of some fixed fraction of a Tick. `spikes/S2.Routing/Graph/Units.cs` reasons exactly this far, picks
Q16.16 for the benchmark, and stops — writing down that this contradicts `05 §121`, that the
alternative is *"the same representation wearing a different name, so nothing about the measurement
changes either way"*, and that **the decision is for the corpus rather than for a benchmark.** It was
right to stop. The question is not which is more accurate.

**The question is what a raw `int` means, and that is where the actual defect lives.** `Fixed` is a
static class over raw `int` — `FromInt`, `Mul`, `Div`, `Lerp`, `AssertMagnitude` — so a Q16.16
position, a Q16.16 speed and a Q16.16 duration are all spelled `int` and the compiler cannot see the
difference. Nothing stops adding a position to a duration, or passing a speed where a length is
expected. `05 §121` reads as a prohibition on the second quantity, but the property it protects is
stated in its own first clause: **widths are stated once, not chosen per site.** It is a rule against
scale drifting site to site, not against a second dimension existing.

**And the prohibition has already been broken, correctly, and nobody noticed.** `Units.cs` stores
free-flow speed as **Q16.16 Tiles/Tick**. A speed is not a sub-Tile position. `02 §2` *mandates* that
this quantity exist and mandates its unit; `05 §121` *forbids* its representation. **The two documents
have contradicted each other since before the spike ran, and the spike resolved it silently in `02`'s
favour** — which was the right call and the wrong way to make it. Applied as written, `05 §121` would
force speed into whole Tiles per Tick: 36 for a Street, 65 for an Arterial, and **3 for a walk against
a true 3.66**, a 20% error on the mode the pedestrian layer is made of. A rule whose literal
application corrupts a quantity is a rule that was never meant literally.

**`BOR0207` is the evidence for what actually goes wrong.** `05 §178` records it as the one lint added
*because a defect got through*: a Q16.16 quantity is 65,536× its whole value, so `part * 10_000 /
whole` in `int` wraps at **3.3 whole units**, and wraps **negative**, deleting the largest inputs from
a mean. That failure is a **scaled value handled as unscaled** — not a position handled as a duration.
The analyser already covers the first. Types cover the second. Neither is covered by forbidding the
quantity.

**The range and the resolution are ample by three orders, and addition stays exact.** Q16.16 Ticks
spans **±32,768 Ticks — four Days** — against a Commute Budget of order a hundred Ticks; its
resolution is 1/65,536 of a Tick, about **0.16 ms** of in-world time, against segment traversals of
order one Tick. Exactness matters more than either: addition of fixed-point values is exact, so a path
cost is the **exact sum** of its arcs' costs with no accumulation error, which is what A\* compares.

**A finer-grained alternative buys headroom against a bound nothing approaches, and costs the audited
arithmetic.** A count of 1/1024 Ticks in `int` would span ±2,097,152 Ticks (256 Days) at ~10 ms
resolution — more range, ample resolution, and an argument on paper. But it needs its own divide, its
own overflow policy and its own tests, where `Fixed.Div` already widens to 64 bits, floor-divides
through `IntegerMath`, and narrows **`checked`** so an overflow throws rather than wraps — with
`AssertMagnitude` beside it. `adr/0018`'s instinct applies inside the codebase as well as outside it:
do not build a second one of something that works.

## Consequences

**`05 §121` is amended, and `02 §2` wins the contradiction.** The sentence becomes: Q16.16 is a scale;
the quantities carried at it are **sub-Tile position, speed in Tiles/Tick, and travel time in Ticks**;
each is a distinct type; and a bare `int` standing for any of them is the defect. The free-flow speed
column that already violated the old sentence becomes legal rather than remaining an unnoticed
exception. `05 §121`'s other clauses are untouched — fixed×fixed still operates on dimensionless
ratios, and a genuine product of absolutes still widens to Q32.32 per site with a written reason.

**Only dimensionally sound operations are offered.** Tiles ÷ Speed → TravelTime; TravelTime +
TravelTime → TravelTime; TravelTime × an integer → TravelTime. There is no TravelTime + Position,
because the type does not expose one. This is the whole benefit and it is a compile-time one.

**The State Hash does not move.** A `readonly record struct` over an `int` folds the bits the `int`
folded. Every type here is `unmanaged` and satisfies `adr/0036`, so nothing about the table discipline
changes either.

**Existing raw Q16.16 sites are grandfathered, deliberately.** The rule binds quantities as they are
built; it does not commission a sweep of the sub-Tile position sites that predate it. Converting them
is hash-neutral and therefore an optimisation by the corpus's own test — which makes it exactly the
kind of change that must not be smuggled into a slice that has a different job. It is owed, not done,
and `0020` is not the slice that owes it.

**[`plans/0020`](../../plans/0020-the-road-graph.md) task 1 can now type its columns**, and task 4
inherits a debt this ADR does not discharge: the free-flow speeds themselves — 50 km/h for a Street,
90 for an Arterial, 5 for a walk — are **hash-bearing Ruleset numbers** belonging in `[roads]`, and
under [`adr/0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md) each
owes a named ratifier in `0002` §D on the day it is written down. Choosing a *representation* ratifies
no *value*.

**The exchange rates stay outside the simulation.** `Units.cs` computes Q16.16 Tiles/Tick from km/h
through an exact integer factor (48,000), and states metres and seconds in comments that nothing
reads. That arrangement carries over unchanged: `02 §2`'s *"no seconds in the library and no metres"*
survives, because the conversion happens where a human authors a number and never at runtime.

## What would trigger revisiting

**A travel time that overflows the format.** `Fixed.Div`'s `checked` narrow throws rather than
wrapping, so the signal is an exception rather than a wrong answer — which is the point of choosing an
arithmetic that already had one.

**Congestion driving an arc's cost without bound.** This is the live risk, and it is not hypothetical:
`03 §3` gives Segments a volume-delay function, and a VDF at volume far above capacity grows fast. Four
Days of headroom is ample for a free-flow city and is *not* obviously ample for a gridlocked arc whose
cost is meant to read as "effectively impassable". If a jammed arc's cost approaches ±32,768 Ticks,
the answer is a **saturating ceiling with a stated meaning** rather than a wider format — an
impassable arc and a four-Day arc are the same routing decision — but the choice should be made when
the VDF exists, not now.

**A tie-break observed to bias routing.** Two routes whose costs differ by less than 1/65,536 Tick
compare equal, and whatever breaks the tie decides. If route selection is ever found to correlate with
something structural — insertion order, node index — the resolution is the first suspect and this is
the ADR to reopen.

**A fourth quantity wanting the scale but not the range.** The simplification here is *one scale, many
dimensions*. A quantity that needs Q16.16's resolution over a range Q16.16 cannot hold would break it,
and at that point the scale becomes a per-quantity property and this ADR's shape is what has to go.

# Industrial pollution is a stock the environment absorbs

**A Cell's pollution source is a stock: Rules add to it, and it decays toward zero at a rate drawn
from the Ruleset. The ceiling is not authored — it emerges, because a steady emitter settles where
what it adds each cycle equals what the ground absorbs.** No clamp bounds the design, and the
existing bound at the kernel's representation ceiling reverts to being what it always was: an
overflow guard. `EMERGENCE` `SOLVE THE ACTUAL PROBLEM`

## Why

**This was already the design and had simply never been built.** [`02 §2.4`](../02-simulation-model.md)'s
field table has said *"Decays; wind advection is a later addition"* of industrial pollution since it
was written. Slice 6 built the source field, the separable kernel, the staggered schedule and exact
incremental re-diffusion, and did not build the decay. The gap surfaced while planning slice 7 task 10
([`plans/0011`](../../plans/0011-rule-engine-bins-and-rules.md) finding 37): `MapLayers.EmitPollution`
is `+=`, nothing in the repository subtracts, and a Rule with a `map` output therefore grows a
magnitude for as long as the city runs — `adr/0003`'s extension of [`adr/0006`](0006-no-collection-grows-with-elapsed-time.md)
arriving through a Map Layer rather than through a collection.

**The accumulator is right for the reason superposition is right, and wrong for the reason a Rule is
not a Building.** `02 §2.4` makes a Layer *a convolution of a source field*, and a source is a
strength — how hard the thing standing there emits. Twenty factories in one Cell must sum, so `+=` is
correct across *emitters*. A Rule firing twenty thousand times is one emitter, and summing across
*firings* silently converts a strength into an elapsed-time counter. Decay is what reconciles the two:
a quantity per firing against a continuous removal gives a level proportional to the **rate**, which
is the quantity `02 §2.4` says the field holds.

**It is also the physical account, and the physical account is what makes the dials mean something.**
Sources emit, the environment absorbs, and concentration settles where the two balance. That gives
three places a designer or a player can act — how hard a Building emits, how fast an area absorbs, and
what a Policy does to either — and all three read off the same equilibrium. Under any of the rejected
options at least one of the three does nothing observable.

## Considered and rejected

**A cap at the write site.** The obvious fix, with precedent in the same file: `MapLayers.Seal` clamps
to the Cell's Tile count and its docstring calls that *"the `adr/0006` bound, made structural at the
only write site."* Rejected on two counts. It **breaks superposition**, which `02 §2.4` names as the
property everything else rests on — past the clamp, two factories in a Cell read the same as one, and
the field stops being a convolution of anything. And it corrupts what the number means: a plant that
has run a year and one that has run a decade both sit at the cap, so the plume reports *how long this
has stood* rather than *how much it emits*.

There is a real-world cap, and the reason it does not belong here is instructive: exceeding it is
environmental collapse, and this game does not model collapse. Having declined to model the
consequence, we have no use for the threshold.

**Setting the source rather than adding to it.** Closest to `02 §2.4`'s literal reading — a Building
declares its strength and the field holds it. Rejected because it requires the engine to remember each
Building's standing contribution to each Cell so it can be replaced rather than accumulated, which is
new per-Building-per-Layer state on the hot path, and because it makes a `map` term the one term in
the Ruleset that is not a quantity. Every other `amount` in `02 §4.3` is *how much moved this firing*.

**Counter-sources — parks and Policies as negative emitters.** Attractive because convolution is
linear, so a negative source diffuses with no new machinery. Rejected as a *fix*: a second accumulator
of the opposite sign still drifts with elapsed time unless the two rates cancel exactly and for ever,
so it does not bound anything. Rejected as a *mechanism* for a better reason — a park with no factory
near it would produce negative pollution, and a large enough park beside a smokestack would produce a
clean Cell next to the source. Absorption has neither failure: a park with nothing to absorb does
nothing, and a park beside a factory lowers the level without erasing it. Parked in
[`deferred.md`](../deferred.md) as *absorption varies by ground*, which is the same idea in the form
that works.

## Consequences

**Hash-bearing, and it changes every pollution figure the project will ever produce.** Nothing
downstream is recorded yet, so the cost is paid now and only now.

**Tau is a new unratified number**, filed in [`plans/0002`](../../plans/0002-open-questions.md) §D.
`adr/0044` is the standing warning here: the Layer cadence was chosen by argument, cited as settled,
and had to be measured back out. Tau is worse-placed than the cadence was, because it sets an
equilibrium rather than a refresh interval. **It ships as one global value in the Ruleset**;
per-Cell absorption is the end state and is deferred with parks.

**It puts a measurable cost on the one optimisation slice 6 worked hardest for.** Exact incremental
re-diffusion recomputes only Cells whose sources changed plus a halo. A decaying source is a changing
source, so every Cell with industry on it is dirty on every cadence and the incremental set converges
on the occupied set. Whether that matters depends on how much of a real city emits — **unknown, and
routed to a machine** rather than argued, per `adr/0043`. The fallback if it is bad is a coarser decay
cadence than the diffusion cadence, which is a third hash-bearing number and is not chosen here.

**Integer decay stalls, and the tail needs a rule.** `source − source/tau` stops moving once the
source falls below tau, so a demolished factory would leave a permanent stain rather than fading.
Whatever the answer is, it is arithmetic and belongs with the implementation — but it must be written
down, because a small residue that never clears is `adr/0006` returning in miniature.

**The overflow bound stays and stops pretending.** `WorldInvariants.LayerMagnitudesAreBounded` checks
the kernel's representation ceiling. It was never a design bound and, with the design bound now
emergent, it is free to be read as what it is.

## What would trigger revisiting

- **The re-diffusion measurement comes back bad** — if decay pushes the incremental scheme's cost
  close to a full recompute, the cadence question opens and with it whether decay belongs on the
  source field at all rather than on the diffused one.
- **Wind advection arrives.** `02 §2.4` names it as a later addition. Advection moves a stock between
  Cells, which is a second term on the same quantity, and the two need to be designed together rather
  than one bolted onto the other.
- **A Layer other than pollution needs the same treatment and cannot use it.** This ADR is written for
  a field with point sources and a physical removal process. Land value has momentum for entirely
  different reasons and must not be folded in on the strength of the shapes looking alike.
- **Somebody wants a hard ceiling for a game reason** — a pollution level at which something
  categorical happens. That is the collapse model this decision declines, and wanting it back is a
  reason to reopen rather than to clamp quietly.

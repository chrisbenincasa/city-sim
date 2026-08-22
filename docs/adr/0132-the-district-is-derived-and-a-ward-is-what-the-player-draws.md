# The District is derived and a Ward is what the player draws

**A District is a derived pooling region and the player cannot move its boundary.** The other role that
sentence carried — *the scope a Policy may be overridden per* — becomes a separate object, a **Ward**:
a named set of Cells the player draws, renames and redraws freely, which Policy is scoped to and which
has **no logistics consequence whatever**. [`02 §2.1`](../02-simulation-model.md)'s *"player-adjustable as
an advanced action"* is **withdrawn**, because it applied to the pooling boundary and that is the one
boundary it must not apply to.

`PLAYER GOVERNS` `LEGIBLE CAUSE` `SOLVE THE ACTUAL PROBLEM`

## Why

### This is the fourth instance of the welding pattern, and `adr/0047` cut the third off the same noun

[`CONTEXT.md`](../../CONTEXT.md) → District joins two roles with an *and*: *"the boundary within which
Goods pool without physical transport, **and** the scope a Policy may be overridden per."*
[`adr/0047`](0047-routing-never-keys-on-the-district.md) cut a third role — travel-time matrix
granularity — off that same sentence, and named the pattern it belonged to:
[`05 §5`](../05-technical-architecture.md)'s *"a constant welded to two decisions is governed by whichever
of them is louder."* `adr/0040` counted five instances, `adr/0047` was the seventh. **This is the next
one, and it is the second found on this one noun** — which is itself the finding: ***an entity that has
already been split once is the likeliest place to find a second weld, because whatever attached the first
role was a habit rather than an accident.***

### The two roles want opposite things from the same line

**Pooling extent is not a choice.** `CONTEXT.md` bounds it by *"the area within which ignoring transport
is a defensible simplification"*, and [`adr/0022`](0022-land-is-a-stock-the-city-spends.md) warns in its
own words that the design *"silently collapses"* if Shipment cost is ever tuned toward zero. `02 §2.1`
states the consequence as a physical fact: *"the count is **physics rather than a design choice**."*

**Policy scope is nothing but a choice.** `02 §2.1`, four lines further on: redrawing *"is also what makes
District-scoped Policy targetable — **you cannot aim a policy at a boundary you did not choose**."*

Both sentences are in the same section of the same document and nothing reconciles them. **One object
cannot be both the line physics draws and the line the player draws.** Read together they say the player
may redraw a boundary whose whole justification is that its extent is not up to anyone.

### The exploit is real, and `adr/0013` wrote this decision's conclusion before the decision was taken

[`adr/0013`](0013-goods-are-pooled-within-a-district-and-shipped-between.md) → *What would trigger
revisiting*:

> **Players gaming the boundary** — drawing one enormous District to abstract away all freight. If that
> becomes the dominant strategy, Districts need a size ceiling or a cost, because **a boundary the player
> draws to switch off a subsystem is a boundary that is not a gameplay concept.**

That final clause is this ADR's claim, recorded as a *trigger* by the ADR that created the problem. ⚠ **It
was filed as a playtesting observation and it is available as an argument** — the exploit does not need
to be observed, because the mechanism that permits it is legible from the two roles alone. `adr/0041`
supplies the rule in one line: ***"`PLAYER GOVERNS` means the player governs the city, not the physics."***

### Why a second object rather than `adr/0013`'s size ceiling

`adr/0013` offers *"a size ceiling or a cost"*. The cost is
[`adr/0133`](0133-a-pool-draw-pays-for-its-haulage-so-the-boundary-is-a-gradient-and-not-a-cliff.md)'s
business and is orthogonal to this. **The ceiling is a patch**: it bounds the exploit without removing the
category error, and it does so by imposing on the player's *Policy* tool a limit whose reason is a
logistics abstraction the tool has nothing to do with. Explaining that limit means explaining pooling at
the moment the player is doing something unrelated to Goods, which fails `LEGIBLE CAUSE` precisely where
it is most expensive. **Separating the objects is the only one of the three that makes the exploit
unrepresentable rather than bounded.**

### The Ward is cheap because it owns nothing

A Ward is a named set of Cells and a list of Policy overrides. It reads no simulation state, nothing reads
it but Policy evaluation, and it has no extent bound because nothing physical rests on its size. **It is
the small object**, which is the right way round: the expensive, physics-bound one keeps its name and its
role, and the new one carries only the affordance that was homeless.

⚠ **A Ward is hash-bearing, and that is correct rather than a problem.** Redrawing one changes which
Households a transfer reaches, so it changes the city — but that is a deliberate act with a visible
consequence, which is `PLAYER GOVERNS` working rather than failing. The defect this ADR removes is not
*a player act changing the city*; it is *a player act changing the validity of a simplification they
cannot see.*

## Consequences

- **`CONTEXT.md` → District loses its second role**, and a **Ward** entry joins *World and space*. The
  District entry keeps pooling, keeps *"typically hundreds of Cells"*, and drops *"either player-drawn or
  automatically derived"* for **derived**.
- **`02 §2.1`'s *"Settled: both"* blockquote is superseded in half.** Automatic stands; *player-adjustable
  as an advanced action* is withdrawn and moves to the Ward. `plans/0002` item 6 records the same close
  and inherits the same correction.
- **`01 §Govern`'s *"Globally by default, overridden per District"* becomes per Ward.**
- **Milestone 12 is unaffected.** It needs the pooling role only; `Scope.Pool` resolves through the
  District, which keeps that role and that name. [`plans/0037`](../../plans/0037-goods-between-buildings-the-district-pool.md)
  decision 1's last sub-question — *does the player-adjustable arm ship at 12* — is answered by
  removal: **there is no player arm on the District at any milestone.**
- **A Ward is *undesigned* rather than unbuilt** ([`adr/0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md)):
  this ADR names it and settles what it is *for*, and settles nothing about its shape rules, its overlay,
  or how a Policy override resolves where Wards overlap or leave gaps. **Naming it now costs nothing and
  is the point** — the Policy override it carries is itself unbuilt, so the split lands before either
  object exists and nothing has to be unpicked.
- ⚠ **The derivation is now the *only* way a District comes into being**, which raises the stakes on
  `plans/0037` decision 1: there is no longer a player arm to fall back on if the derived shape is
  unsatisfying. That is deliberate. ***A fallback to the player is how the first weld was justified.***

## What would trigger revisiting

- **A Policy that genuinely needs to be scoped to a logistics region.** If one appears — a subsidy on
  carriage, a rationing rule keyed to what a Pool holds — then the two objects have a real coupling and
  this split is the wrong cut. Watch for it when Policy is built, not before.
- **Players drawing Wards that everywhere coincide with Districts.** If the player's natural
  administrative unit turns out to *be* the pooling region, two objects are ceremony and the honest move
  is one object with the player locked out of its extent.
- **Districts becoming individually salient enough that players want to name them.** Naming is a Ward
  affordance here; if it migrates to the District, the District has acquired a player-facing identity and
  the argument above needs re-reading with that in it.

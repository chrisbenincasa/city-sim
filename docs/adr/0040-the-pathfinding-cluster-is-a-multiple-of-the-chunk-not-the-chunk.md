# The pathfinding cluster is a multiple of the Chunk, not the Chunk

**The hierarchical router's cluster is its own size, constrained only to be a whole number of Chunks.
It is not the Chunk.** The Chunk keeps the six roles [`05 §5`](../05-technical-architecture.md) gives
it; the cluster becomes a seventh partition that aligns with the Chunk grid by construction and is
sized independently of it. `FAST ITERATION`

> **Corrected on one point by [`adr/0047`](0047-routing-never-keys-on-the-district.md): the cluster's size is expressed in *Cells*, not in Chunks.** This ADR split the cluster from the Chunk on a **permanence** axis and never ran the **hash** test on the dependency that leaves behind. The Chunk is declared *tuning, hash-preserving*; a cluster sized as *k* Chunks moves when that knob turns, and HPA\*'s routes depend on cluster size — so turning a hash-preserving knob changes the city. ~~**The defect is live today and `adr/0047` did not create it.**~~ The repair keeps everything below intact: the size is a multiple of the frozen Cell, Chunk size is constrained to **divide** it, and the alignment check lands exactly where this ADR already puts it — with the world-creation constants.
>
> ✅ **PAID 2026-08-14, milestone 5c task 1** ([`plans/0026`](../../plans/0026-statistical-resolution-and-the-travel-time-matrix.md)). `Borough.Core.Space.RoutingPartition` takes its edge in **Cells**, refuses at construction anything that is not a power of two, smaller than a Chunk or larger than the map, and `RoutingPartitionTests.The_edge_is_a_whole_number_of_Cells_and_the_Chunk_divides_it` asserts the divisibility this ADR's fourth consequence says *"must be enforced ... it would do so silently"*. **The name is now the *routing partition***, which is `adr/0047`'s term and has a `CONTEXT.md` entry; *pathfinding cluster* survives only here and in that entry's pointer back.
>
> ⚠ **Two things this ADR did not foresee, and both are the same shape as its own defect.** The partition covers only ground a Node stands on, because it is **quadratic** and a tiling of the whole map is a 1.07 GB matrix against 8.3 MB for a million-Citizen city — `adr/0021`, and the consequence table below prices *changing* the size while saying nothing about what the size multiplies. And **a matrix entry's error is a fixed distance and therefore a mode-dependent time**: half a partition diagonal is under a minute by car and several minutes on foot, so a size argued from S2's car-only sweeps is not thereby right for the pedestrian Trips that are the only ones this project builds today. Neither is a correction to what is written below; both are things it is silent about.

## Why

**One of the two is in the save and the other is not, and nobody had noticed the asymmetry.**
`05 §5` makes a save *"a sequence of Chunk records"*, so the Chunk is pinned from milestone 8 onward
and changing it afterwards is a save migration. The hierarchical router's abstract graph is
`(derived AND rebuilt)` under [`adr/0003`](0003-deterministic-integer-simulation.md) — the same class
as the travel-time matrix, which `05 §4` already lists as *rebuilt rather than saved*. Changing the
cluster size costs a recomputation and nothing else, **forever**.

| | Cost of changing it after milestone 8 |
|---|---|
| **Chunk size** | a save migration |
| **Cluster size** | recompute it |

**Unifying them therefore imports permanence onto a structure that never had any.** That is `05 §5`'s
own recorded lesson — *"a constant welded to two decisions is governed by whichever of them is
louder"* — reaching a **sixth** instance, after the Cell, Map Layer diffusion cadence,
[`adr/0034`](0034-fields-are-sorted-by-source-geometry.md)'s Chunk case, the travel-time matrix refresh
cadence and the sun arc's phase widths. It is a *new axis* of the same failure: the Cell was split out
because one of its roles was **hash-bearing**, and this splits because one of its roles is
**permanent** while the other is free.

**It meets `05 §5`'s burden of proof, which is deliberately high.** That section requires a proposal to
split any Chunk role *"to argue why the coupling it removes is worth the five it breaks"*. Four of the
five are not couplings at all — work partition, save serialisation, aggregate caching and render
streaming have nothing to say to a routing abstraction. The fifth, **dirty tracking**, is real:
*"this Chunk is dirty" is a single fact with six consumers*. It survives intact, because the cluster is
constrained to a whole number of Chunks and `05 §5`'s own precedent then applies verbatim — the **Cell**
is a strict *divisor* of the Chunk, giving *"no index conversion that is not a shift, no boundary that
does not align, and no possibility of the two disagreeing about which side of a line something is
on."* The cluster is a strict *multiple*, which is the same argument pointing the other way. A dirty
Chunk maps to exactly one cluster by a shift.

```
Cell     ─┐  strict divisor of the Chunk    (05 §5)
Chunk    ─┤  the permanent one — it is in the save
Cluster  ─┘  strict multiple of the Chunk   (this ADR)
```

**What this corrects.** [`adr/0014`](0014-grid-streets-with-freeform-arterials.md) claims the Road
Graph *"arrives pre-partitioned, because the Chunk grid is already the pathfinding cluster."* The
useful half of that claim is that a **regular tiling already exists**, which is most of what a
hierarchical router wants handed to it. That half is untouched and is why the alignment constraint
costs nothing. The unexamined half is the identity — *the* cluster, rather than *a* cluster — and it
was asserted rather than argued.

## Consequences

- **Chunk size loses the half of its argument that had the strongest claim on it.** `05 §5` says the
  Chunk *"probably wants to be larger than 32×32, that the pathfinding role has the strongest claim,
  and that the render role has a genuine optimum with a bottom to find."* Pathfinding now sizes itself,
  so the Chunk is settled by rendering, saves and work partitioning alone — a two-sided optimum with a
  bottom, rather than a tug-of-war between a permanent decision and a free one.
- **Spike S2's ownership changes.** [`0010`](../../plans/0010-s2-routing.md) R3 sweeps **cluster size**
  and decides it outright, and merely *informs* Chunk size. The plan's own caveat — *"a Chunk size
  chosen from pathfinding alone is a recommendation, not a decision"* — stops being a caveat, because
  S2 no longer needs the render side of a trade it cannot see.
- **Phase 1's provisional pin loses most of its risk.**
  [`0002`](../../plans/0002-open-questions.md) records *"Phase 1 proceeds at Chunk = Cell, provisional,
  pending S2"*. Phase 1 no longer waits on a routing spike to unblock a rendering and save decision.
- **The alignment constraint is load-bearing and must be enforced.** A cluster that is not a whole
  number of Chunks reintroduces every boundary disagreement `05 §5` unified away, and it would do so
  silently. It belongs with the world-creation constants, validated where `TICKS_PER_DAY` and
  `WHEEL_SIZE` are.
- **If S2 measures the best cluster at exactly one Chunk, nothing is lost.** The decoupling was free,
  and the coincidence `adr/0014` assumed becomes a measurement.
- **It does not answer how a Chunk size change is migrated.** Chunk size stays on the *cannot be
  retrofitted* list; this ADR removes a reason it was there, not the list entry. A save migration path
  is unwritten and is recorded as open in [`0002`](../../plans/0002-open-questions.md).

## What would trigger revisiting

- **A cluster size that is not a whole number of Chunks turns out to be materially better.** Then the
  alignment constraint is the thing costing something, and the choice is between paying `05 §5`'s six
  invalidation stories and accepting a worse router. Measure both before choosing.
- **The abstract graph stops being derived.** If preprocessing ever becomes expensive enough that it
  must be saved rather than rebuilt on load, the asymmetry this ADR rests on disappears and the
  unification argument wins on its own terms.
- **A seventh Chunk role appears that genuinely needs the routing partition.** The argument here is that
  four of `05 §5`'s roles are unrelated to routing and the fifth aligns by construction. A new role
  that spans both would have to be weighed rather than assumed away.

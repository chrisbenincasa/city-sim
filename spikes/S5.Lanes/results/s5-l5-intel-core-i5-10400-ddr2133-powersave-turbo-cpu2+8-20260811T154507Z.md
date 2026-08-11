# S5 — the Lane kernel

- **Captured** 2026-08-11 15:45:07 UTC
- **Machine** Intel(R) Core(TM) i5-10400 CPU @ 2.90GHz, 2 logical processors visible
- **OS** Ubuntu 24.04.4 LTS, X64
- **Runtime** .NET 10.0.10
- **Governor** **powersave** — every absolute below is a lower bound on this machine's ability
- **Turbo** enabled
- **Processors allowed** 2,8
- **Build** Release
- **Stopwatch** high resolution, 1000000000 Hz

> `plans/0019-s5-lane-kernel.md`. **S5 does not set the Microscopic Cap.** It measures one side of a ratio — Vehicles affordable in 15.6 ms on one core — whose other side is how many Vehicles a real city stresses at once, and that is milestone 5b's.

## L5 — is the arithmetic's cost a design decision, or a spelling?

L1 measured the IDM as written against a precomputed **approximate** reciprocal and filed the 1.63–1.75× as an attribution rather than a recommendation, because a reciprocal rounds, so it moves the State Hash, so under `CLAUDE.md`'s own test it is *a design change however it was motivated*. **That reasoning is sound and its premise was never checked.** This section checks it: two forms reach most of the same speed and **neither moves a single bit**.

- **Reordered** — `IntegerMath.FloorDiv` spells its correction `(n % d != 0) && ((n < 0) != (d < 0))`, so the **modulo is the first operand and always runs**, and RyuJIT does not fuse it with the division above it. Every `FloorDiv` is therefore **two** 64-bit divisions. Swapping the operands short-circuits the modulo whenever the signs agree. `&&` over two pure conditions is commutative, so this is bit-identical **by construction** — and it is not about the IDM: `Fixed.Div` is the substrate's divide, so it reaches every division site in the simulation.
- **Exact magic** — for a divisor fixed at Ruleset load there is a multiplier and a shift reproducing `floor(n/d)` **at every point in a bounded range** (Granlund & Montgomery 1994). It is not a reciprocal and it does not round. The shift is searched for and **verified at every quotient boundary in range at construction**, and a divisor with no exact form is refused rather than approximated.

### The tables, and the width they need

| | Divisor | Dividend bound | Shift | Multiplier width |
|---|---|---:|---:|---:|
| `v / v0` | per-driver, 20 distinct in this fixture | 2^34 | 50 | **35 bits** — a `ulong` column, 8 bytes against the reciprocal's 4 |
| `v·Δv / 2√(ab)` | 2966, one per Ruleset | — | 46 | 35 bits — **no per-Vehicle column at all** |

**The 128-bit intermediate is required rather than chosen.** The product `n × M` runs to 65–70 bits across a realistic spread of driver speeds, so a 64-bit form would be exact only below a speed cap — and **a correctness property conditional on a tuning number is a worse foundation than the division it replaces**. `UInt128` is one `mulx`, needs no `Math.*`, and trips no lint.

### Bit-identity, on the kernel's own state after 64 Ticks

Four networks built from one seed — 294,912 Vehicles — stepped 64 Ticks, then **every position and every velocity compared against the shipped form**. This is the claim, and a microbenchmark agreeing on sampled operands is not.

| Form | Identical to the shipped kernel? |
|---|---|
| Reordered `FloorDiv` | **BIT-IDENTICAL** |
| Exact magic | **BIT-IDENTICAL** |
| Approximate reciprocal — L1's | **no — the State Hash moves**, and it has drifted by 451,337 Q16.16 units in total |

**The last row is the design change, made visible.** The reciprocal form is not wrong — it is a different city, and after 64 Ticks it is already a measurably different one. The other two are the same city, so under `CLAUDE.md`'s test they are **optimisations and need no ratifier**.

### The price of each spelling

| Form | Divisions/Vehicle | Row | ns/Vehicle | vs shipped | Vehicles in 15.6 ms | Moves the hash? |
|---|---:|---:|---:|---:|---:|---|
| As written | 3 (6 idiv) | 16 B | 40.83 | 1.00× | 382,006 | — |
| **Reordered** | 3 (3–4 idiv) | **16 B** | 26.87 | **1.51×** | 580,400 | **no** |
| **Exact magic** | 1 (1–2 idiv) | 24 B | 27.52 | **1.48×** | 566,798 | **no** |
| Approximate reciprocal | 1 (1–2 idiv) | 20 B | 22.28 | 1.83× | 700,116 | **yes** |

**The exact form captures 71.7% of what the design change buys, and buys it for nothing.** That is the finding. The hash-bearing option is left with a margin of 1.23× over a form that keeps the arithmetic identical, which is not a margin a design decision should be taken for — so `plans/0002` §D2's *how the IDM is spelled* row **retires rather than fills**, which is `adr/0059`'s direction.

**The reordering is the finding that outranks the spike.** It is a three-token change to `IntegerMath.FloorDiv`, it is bit-identical by construction, it needs no ADR and no ratifier — and because `Fixed.Div` is *the* substrate divide, it is worth 1.51× here and something at **every division site in the simulation**, none of which S5 measured or can speak for.

### The headline rung, on the reordered substrate

Two Overlaps per Lane by cursor — the row L4 carries, and the rung `adr/0016`'s tripwire **T1** is scored against. **Measured here rather than inferred** by adding the exchange's cost to the figure above, because that addition is the step this spike has already caught itself wanting to take twice.

| Form | ns/Vehicle | **Vehicles per Tick per core** | Microscopic Segments in 15.6 ms | vs `adr/0016`'s 400,000 |
|---|---:|---:|---:|---|
| As written | 44.41 | **351,224** | 4,878 | **below — T1 fires** |
| **Reordered** | 29.28 | **532,750** | 7,399 | **above — T1 does not fire** |

**T1 un-fires on a change that moves no bit.** `adr/0016`'s transplanted headline is reachable in `adr/0003`'s arithmetic after all, and the reason S5 concluded otherwise is that the substrate was computing a modulo nobody had asked for. **The amendment this spike wrote against that ADR needs its first clause revisited and its second left alone** — the `memcpy` claim is still false by more than an order of magnitude, because a division remains a division.

The `powersave` caveat applies unchanged and now cuts the other way: these are lower bounds, so the canonical `performance` capture can only move the figures **up**, and T1's margin with it.

## L4 — the derived product

**Not emitted.** L4 is a view over L0–L3 and this was a partial run. A product of one measurement and three zeros is worse than no product.

---

**Contention** — 43934 µs of CPU stall accumulated during this run (Linux PSI `cpu total`, end minus start). A run with a quiet window reads near zero.


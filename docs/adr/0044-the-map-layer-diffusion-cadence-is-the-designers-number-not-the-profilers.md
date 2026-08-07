# The Map Layer diffusion cadence is the designer's number, not the profiler's

**The cadence at which a Map Layer is re-diffused is hash-bearing, and it is ordinary hot-reloadable Ruleset data.** A designer may move it; a profiler may not. This supersedes [`05 §9`](../05-technical-architecture.md)'s filing of it as one of three *performance* multipliers, and corrects [`02 §1.2`](../02-simulation-model.md)'s *tuning* row, which was right about the category and silent about the hash.

**The kernel radius is hash-bearing too and lands in a different category — world-creation, baked into the save — and it is recorded unratified.** Industrial pollution's tent kernel reaches **1,024 m — 8 Cells**, the low end of `02 §2.4`'s 1–10 km band. `EMERGENCE` `LEGIBLE CAUSE` `FAST ITERATION`

## Why

**It was measured, not argued.** [`adr/0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md) requires a claim to be typed before it is settled: *can you name the number that would refute this, and the machine that would produce it?* For this claim both existed. The number is a State Hash and the machine is slice 6. `tests/Borough.Tests/Space/LayerScheduleTests.cs` runs two worlds identical in every respect but the diffusion period, driven by identical emissions, and compares their hash traces over 400 Ticks.

**They differ.** Under [`05 §4`](../05-technical-architecture.md) — *a change is an optimisation if the State Hash is unchanged, and a design change otherwise, however it was motivated* — the cadence is therefore a design change. A designer moving it from 64 to 32 builds a different city.

**And the divergence is transient, which makes the case stronger rather than weaker.** Once emissions stop and both cadences have fired, the two fields are bit-identical — because a Layer is a convolution of its sources and not a function of its own history ([`adr/0034 §3`](0034-fields-are-sorted-by-source-geometry.md)). So the cadence does not change what the field *settles to*. It changes **when a source's contribution becomes visible to a Rule that reads the Cell**.

That is the whole claim, and the transience is not an escape from it: **a city is never in the settled state.** Sources change as it builds, which is the entire game, so every Rule that reads these Cells reads the transient. A discrepancy that vanishes once you stop playing has not vanished.

### Where the number was mis-filed, which is the finding

**`05 §9` bullet 3 lists the cadence among *three separate multipliers, all of which are invisible to the player*.** That section is the performance budget; its whole purpose is to name the levers available when the Tick is over budget. A hash-bearing number sitting in that list is a number a profiler will reach for and be *right* to reach for, on this document's own authority.

**This is the same welding failure `adr/0034` found in Chunk size, in the same document, one section earlier.** `05 §4`'s hash rule did not fail either time; nobody had applied it to the number. The rule is only as good as somebody running it against each constant **by name** — and a number embedded in an `if` has no name, which is why the schedule is now a table (`Borough.Core.Space.LayerSchedule`) and not a scatter of `tick % 64` through Phase 5. `02 §1.2`'s row is a lesser offence: it filed the cadence as *tuning*, which is where it belongs, but said nothing about the hash, so a reader had no way to tell the designer's lever from the profiler's.

### Why the Ruleset — and the argument this ADR made first, which was wrong

**This ADR's first draft put the cadence in [`adr/0015`](0015-all-tuning-data-is-hot-reloadable.md)'s world-creation category, and reasoned that the Ruleset is *by definition* the set of numbers a designer may change without changing the city. Both halves are false, and `adr/0015` says so in its own words.** It is recorded rather than quietly rewritten, because the mistake is the same shape as the one being corrected: a number was moved on an argument when a stated test was available.

**The Ruleset is hash-bearing by design.** `adr/0015`: *"The Ruleset's content hash feeds the State Hash"*, and a reload *"is swapped at a phase boundary and recorded in the Input Log, so replays remain exact."* Nearly every number in it changes the city — a production ratio changes it enormously, and nobody proposed freezing that. **Hot reload is not made safe by the numbers being inert; it is made safe by the reload being a logged simulation event.** The machinery for a hash-bearing Ruleset value already exists and this needs none of it built.

**And the world-creation category has a stated membership test**, which is *whether existing simulation state was recorded in units of the constant*. `TICKS_PER_DAY` passes it: every event already on the Event Wheel was scheduled in Ticks against an assumed Day length. **The cadence fails it.** No stored state is denominated in the cadence — a Layer Cell holds a convolution of its sources, and the dirty set holds Cells. Change the period mid-run and nothing already stored is reinterpreted; the next diffusion reads the same sources through the same dirty set and produces the field it would have produced anyway.

`adr/0015` also warns, in the paragraph that states the test, that this is *"a named category with an enumerated membership, not a set of exceptions — the moment it becomes a place to put anything inconvenient, this ADR has been defeated."* A hash-bearing number one did not want in the Ruleset is exactly the inconvenient thing that warning is about.

**So `05 §4` does not sort numbers into Ruleset and world-creation. It sorts them into the designer's and the profiler's.** Hash-bearing means only *not the profiler's to move*. Whether a number is **also** frozen per-world is a second question with a second test, and the two are independent.

### What makes a mid-run change lossless, which is a mechanism and not luck

**The dirty set.** A skipped diffusion loses nothing because the Cells whose sources changed are still marked, so lengthening the period defers work and discards none of it; shortening it makes the next pass cheaper, not different. Slice 6 has this by construction — `plans/0009` task 5 requires incremental re-diffusion to be *exact rather than approximate*, which is the same property viewed from the other side.

**Worth naming because it is load-bearing.** A cadence whose skipped work were unrecoverable — a relaxation, say, where each pass reads the previous field — would fail the world-creation test just as squarely and still need freezing, for a reason `adr/0015`'s test does not capture. `adr/0034 §3`'s refusal of iterative relaxation is therefore what makes this classification available, and the two decisions stand or fall together.

### The kernel radius, which the test sorts the other way

**Stored Cells are denominated in the kernel.** A Layer is stored pre-normalised in kernel units and divided by the kernel's Scale — `(r+1)⁴`, which is 6,561 at r = 8 — at the point of use. Change r and every Cell that is not re-diffused is read at the wrong scale: `adr/0015`'s test, met exactly. So the radius is a **world-creation constant, baked into the save**, and it sits in the Ruleset and is read from it like the rest of that category.

**It is a weaker member than `TICKS_PER_DAY`, and that is recorded rather than smoothed over.** The sources survive a reload — `PollutionSource` is a saved column — so a changed radius is *recoverable* by one full-map re-diffusion, where a changed `TICKS_PER_DAY` has genuinely lost the information needed to reinterpret the Wheel. What `adr/0015` has no vocabulary for is a constant whose change needs a **migration step** rather than a refusal. Filed as a revisit trigger below; not invented here.

`02 §2.4` grounds industrial pollution's range in reality — *real plumes run 1–10 km* — and states no kernel. `02 §2.5` question 2 asks *what is its actionable range in metres, and can you defend the figure from reality?*, so the radius is authored in metres and derived into Cells by rounding up.

**1,024 m is the low end of the band, taken because it is the end a Cell grid can represent.** At 10 km the radius is 79 Cells and the kernel spans 159 Cells per axis against a 128-Cell map — the kernel would be wider than the world.

**The band fails the corpus's own guard rule, and that is recorded rather than resolved.** `02 §2.5` guard rule 1 is *two ranges more than ~5× apart means two fields wearing one name*, and 1–10 km is 10× apart. Either industrial pollution is two fields — a near plume and a regional haze — or the band describes the spread *across* industries rather than the reach of one. **Neither this ADR nor any argument can tell those apart; it wants a source.** Typed *measurable* under `adr/0043` and filed unratified.

### The kernel shape

A separable tent — 1-D weights `r+1−|i|`, gain `(r+1)²`, applied once per axis. Bounded, integer, monotone, and separable, which is what the design already required. A box kernel was rejected for its flat plateau and hard edge: an edge in a pollution field reads to a player as a wall somebody authored, which is `NO VERDICT`'s objection arriving through the art.

**What is not established is that a tent is the right falloff for a plume**, which is an empirical question about dispersion. Also unratified.

### Where the rounding lives, which was forced

`02 §2.4` and `05 §9` both require *integer arithmetic with explicit rounding*. `plans/0009` additionally requires **superposition exact — twenty sources diffused together equal the sum of twenty diffused separately, bit for bit.**

**Those two demands fix where the rounding may go, and it is not inside the passes.** Integer division is not linear: `RoundDiv(41, 81)` is 1 and so is `RoundDiv(82, 81)`, so two sources of 41 in one Cell diffuse to 2 apart and 1 together. Superposition is precisely the statement that the operator *is* linear.

So the passes accumulate exactly, a Layer is **stored pre-normalised in kernel units**, and the single stated division happens at the point of use. This is cheaper as well as exact — one division per read instead of one per Cell per pass — and it is what keeps `adr/0034 §3`'s incremental scheme *exact rather than approximate*. An approximate incremental scheme is a relaxation wearing a convolution's name.

## Consequences

- **`05 §9` bullet 3 loses the cadence as a lever.** Two multipliers remain available to a profiler — coarseness, which is the Cell's and already frozen, and the stagger's *phase*, which is not. The **periods** are the designer's. The section keeps the cadence as an explanation of why the work is affordable, which it is, and stops offering it as a knob.
- **`02 §1.2`'s row keeps `tuning` and gains `hash-bearing`.** The `32–64 Ticks` range goes: a range is an invitation to split the difference, and there is one period per Layer.
- **The staggered offsets are hash-bearing too**, by the same argument and the same measurement. Pollution fires at `tick % 64 == 0` and land value at `tick % 256 == 16`; no Tick carries both, and moving either is a design change a designer may make.
- **`LayerSchedule` and `LayerRates` are a constructor argument of `World`, and that is the finished shape rather than a stopgap.** The first draft owed a `WorldConfiguration` field and a `.borough` format version bump; on the corrected classification **neither is owed**. Slice 8 feeds `LayerRuleset` from the TOML Ruleset alongside everything else, and reload is already a logged event.
- **`adr/0015` gains one world-creation constant, not two.** `adr/0034` added the Cell; this adds the kernel radius. The cadence stays out, and the enumeration stays honest.
- **A save migration path is owed, and the radius sharpens what is missing.** The Chunk carries the same debt. Nothing describes what happens when a world-creation constant should move after saves exist — and the radius shows the answer is not always *refuse*, because here it is one full re-diffusion.
- **A sixth claim in the corpus is measured false, and it is the first outside S2.** The five before it came from the routing spike; this one sat in `05 §9`'s performance budget and `02 §1.2`'s **normative table**, which is the document other documents are told to cite rather than restate. The board's audit note applies with more force than it did: every ADR and design section is in scope for `adr/0043`'s typing, not only the ungrilled ones.
- **And a seventh thing was got wrong here, by argument, one draft before this one.** The correction cost nothing because no code had been built on it; the cost of the *pattern* is that `adr/0015` was cited without its membership test being run. **Citing an ADR is not the same as applying it**, and the difference is whether the test it states was executed against the case at hand.

## What would trigger revisiting

- **A source for the plume range.** If 1–10 km turns out to describe one industry's reach rather than the spread across industries, guard rule 1 splits industrial pollution into two fields and both the radius and this ADR's second half are reopened.
- **A dispersion model that says the falloff is not triangular.** The tent is unratified; a defensible shape replaces it, and that is a re-baseline rather than a tuning change.
- **A profile that says the cadence is the cost.** It does not license the profiler to move it. It licenses a *designer* to be shown the bill and choose — which is a different conversation with a different person in it, and that is the entire point of the classification.
- **A third category, between *reload it freely* and *refuse the reload*: a constant whose change needs a migration.** The kernel radius is the first member and the Chunk may be a second. If a third appears, `adr/0015`'s two-category split is the thing to reopen, not this ADR.
- **Rules that read Map Layer Cells turning out to be insensitive to when a source becomes visible.** That would make the cadence hash-preserving and hand it back to the profiler, and it is measurable: hold the cadence fixed, vary the Rule's read cadence, and see whether the city moves. Nothing can read a Cell until slice 7, so nobody can run it yet.

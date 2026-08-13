# A save is copied at save cadence, not read from a Past that no longer exists

**A save takes a copy of the saved columns at the end of Tick phase 7 and writes it on a background
thread.** `05 §7`'s *"saves are written asynchronously from the Past — the Past is immutable and
known-consistent"* names a structure
[`0037`](0037-the-world-is-single-buffered-and-hazards-are-per-table.md) deleted. **Asynchronous saving
survives the deletion, and its conclusion is unchanged**: `0037`'s cost argument is a **per-Tick**
argument, the copy a save needs is paid **once per autosave**, and dividing the one number by the other
was never done. At one copy per in-world Day the amortised cost is **under a hundredth of a percent of a
Tick**, against the **8–15.6 ms a Tick** — 50–100% of a 4× budget — that got the per-Tick copy deleted.

`FAST ITERATION` `HONEST DEGRADATION`

## Why

### The premise is gone, the replacement was written down, and the section that needed it was not

`05 §3` was rewritten when `0037` shipped and now reads *"one live world, and hazards are per table."*
`05 §7`, four sections later, still says the serialiser walks an immutable Past. There is no Past. Two
tables are double-buffered — Lane dynamics and Map Layer cells, because phases 4 and 5 are parallel and
both read a peer — and everything else is single, written only by serial phases.

**And §3 already states the replacement, in six words.** Its closing paragraph names the three consumers
that genuinely need a complete snapshot and gives each a cadence: *"the **saver** takes one real copy at
save time, the **renderer** reads a published transform history one generation deep, and a **panic**
emits the last checkpoint plus the Input Log."* So this ADR is not discovering the mechanism. It is
recording that **the mechanism was written into one section and the section whose argument depended on
it was left contradicting it in the same edit** — which is [`plans/0012`](../../plans/0012-corpus-audit.md)
*Cause 1* inside a single file, and the reason it survived is that neither sentence is wrong on its own
page.

What is genuinely new here is the **arithmetic**, which nothing has done anywhere: *one real copy at save
time* is a mechanism, not an affordability claim, and the only cost figure in the corpus for that copy is
the per-Tick one `0037` used to delete it. Read carelessly, that number kills the mechanism §3 proposes
and the section would have to fall back to a synchronous write with a hitch. It does not, and the reason
is a denominator.

### The arithmetic `0037` ran, divided by the interval nobody divided by

| | Per Tick, as `0037` found it | Per autosave, at one per in-world Day |
|---|---|---|
| Copy size | ~86 MiB of tables at 1M (S0a) | the **saved** columns only, so less |
| Copy cost | ~10 ms (`0037`: 8–15 ms for 80–150 MB) | ~10 ms |
| Frequency | 8,192 times a Day | **once** |
| Amortised over a 15.6 ms Tick | **50–100% of the budget at 4×** | **0.008%** |

One in-world Day is 8m32s of wall clock at the reference rate, which is a conservative autosave interval
rather than a generous one. Even at an aggressive 64 Ticks — five seconds of play — the amortised cost is
**1% of the Tick budget**, and 64 Ticks is a cadence nobody would choose. There is no interval at which
this is a line item.

So `0037`'s number did not need to shrink. It needed a denominator, and **the structure it deleted and
the structure a save needs are the same bytes at frequencies four orders of magnitude apart.** The
per-Tick copy was doing 150× more work than the Tick itself; the per-autosave copy does about 1/1,600th
of one Tick's work per Tick.

### Why the end of phase 7, and why that is the only free moment

`0037`'s phase table sorts this without any new analysis. Phase 7 (Commit) is **serial**, it is where
schedules and hashes are already written, and by the time it ends both double-buffered tables have
settled — so a copy taken there needs no coordination with either hazard, and no phase is holding a
partially-updated peer. Every other candidate is worse: phases 4 and 5 are parallel, and a copy taken
mid-Tick would capture some tables before Settle and some after, which is a world that never existed and
would load into a hash nothing can reproduce.

This also makes the copy's boundary the same boundary the State Hash is taken at, which matters for
[`0086`](0086-a-save-has-no-schema-of-its-own-and-the-field-declaration-is-the-format.md): the file and
the hash describe the same instant, so the hash in the header is a statement about the bytes below it
rather than about a nearby Tick.

### The blocking part is the copy and not the write, which is the whole point

The shape is:

```
end of phase 7   copy saved columns          ~10 ms, blocking, once per autosave
background       serialise, compress, write  seconds, blocking nothing
```

`05 §7`'s reason for wanting this is untouched by `0037` and is worth restating because it is a product
argument rather than a performance one: *"an autosave that stutters is an autosave the player turns off,
and a save nobody has is a bug report nobody can reproduce."* The file write is I/O and compression over
tens of megabytes and is the part that could take seconds. The copy is a memory-bandwidth operation that
takes about one frame's worth of one Tick. **Moving the unbounded half off the simulation thread is what
async saving buys**, and it buys it for a bounded, measured, amortisable price.

Worth recording alongside: **a hash is not free at this size and a copy is.** S0a measured one State Hash
at **32.47 ms** over the same state — 2.08 Tick budgets, and roughly 3× the copy. If a save is to carry a
verified hash, that hash is computed on the background thread from the copy, never on the simulation
thread as part of taking it.

### The finding, and it points the opposite way to 5b's

Milestone 5b closed on ***a decision that removes a representation defers every decision that reads it***
— `0075` gave a Leg a cost and no path, which made `0041`'s Segment volume unbuildable, and neither ADR
said so. This is the same event with the other outcome: `0037` removed a representation, `05 §7` read it,
and `05 §7`'s conclusion **survives on a different mechanism**. Both cases went unnoticed for the same
reason and only one of them was a defect.

***So the obligation a deletion creates is a re-derivation, not a retraction.*** A consequence drawn from
a deleted structure is not thereby false; it is **unsupported**, and telling the two apart costs the work
of finding out. The failure mode is symmetric and the second half is the one that would have hurt here:
a diligent reader who noticed `05 §7`'s dead premise and struck the paragraph would have deleted a
correct decision and re-introduced the save hitch, with the ADR trail reading as though `0037` had
required it.

## Consequences

- **`05 §7`'s async-save paragraph is rewritten in place**, keeping its conclusion and its product
  argument, replacing its mechanism, and citing `0037`.
- **The autosave interval is a host concern and not a world constant.** It changes no state and enters no
  hash, so it is not `adr/0052`'s business and needs no ratifier — it is the one number in this decision
  that may be a setting. What it must not become is *adaptive*, since a cadence that varies with load
  would make save timing a function of the machine.
- **Milestone 10 gains a structural requirement**: the copy is taken at a phase boundary, so the
  serialiser may never read a live table. A writer that walks `World` directly is a defect even when it
  produces a correct file today, because it is correct only while nothing is parallel around it.
- **The copy is the saved set**, per `0086`, so it is not the 85.98 MiB figure and no size claim above
  should be quoted as the save's. The precise number is available from the declaration and nobody has
  totalled it; that is milestone 10's to report, not this ADR's to guess.
- **This is the first of `05 §3`'s three consumers to be priced.** The other two — the renderer's
  published transform history one generation deep, and a panic's last checkpoint plus Input Log — are
  named with cadences and neither has an arithmetic behind it either. The renderer's is `05 §2`'s and is
  a per-frame claim, which is the one most likely to be carrying the same unexamined multiplication;
  that is session **L**'s, and it is recorded here rather than acted on.

## What would trigger revisiting

- **The copy becoming unaffordable at a *single* occurrence.** Everything here rests on ~10 ms being an
  acceptable one-off. At 4M Citizens, S0a's linear extrapolation puts tables at **343.91 MiB**, so the
  copy is ~40 ms — three Tick budgets in one hitch, which is visible. The repair is an incremental or
  chunked copy, which is real work and should not be attempted before the target moves.
- **A table that cannot be copied cheaply.** Everything in `Core` is `unmanaged` per `0036`, so a copy is
  a `memcpy` today. A cold-path table carrying references — permitted under `ColdPath` — would need its
  own answer, and the answer is probably that cold state is saved synchronously because it is small.
- **Save frequency becoming continuous.** A design that saves every few Ticks — for rollback, or
  networked play — inverts the arithmetic above completely and puts this back where `0037` found it.
  Nothing in the corpus wants that today, and `0020`'s one-world decision is what keeps it away.

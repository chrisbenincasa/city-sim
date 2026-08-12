# 0024 — Session J: the save, the map and the Outside

> **Ran 2026-08-12, one sitting, beside 5b-bis.** Booked on the board as *`05 §7` format half, map size,
> Outside Connection layout — the three things still blocking save/load*, unblocking `06` milestone 10.
> Four ADRs: [`0085`](../docs/adr/0085-nothing-on-this-map-is-far-away-so-a-settlement-is-made-by-a-gap.md),
> [`0086`](../docs/adr/0086-a-save-has-no-schema-of-its-own-and-the-field-declaration-is-the-format.md),
> [`0087`](../docs/adr/0087-a-save-is-copied-at-save-cadence-not-read-from-a-past-that-no-longer-exists.md),
> [`0088`](../docs/adr/0088-the-price-of-a-far-hinterland-is-paid-in-your-own-traffic.md).
> Amendments in place to `05 §7`, `adr/0020`, `CONTEXT.md` → Settlement, Outside Connection and
> Hinterland, `06`'s milestone 10 row, and both of `plans/0002` ledger #1's tables.
>
> **It touched no code**, which is why it could run beside the 5b-bis session.
>
> ⚠ **RE-OPENED THE SAME DAY on its map-size half, with the user in the room, into
> [`0089`](../docs/adr/0089-the-map-is-sized-by-how-many-commutes-fit-across-it.md)** — the map goes to
> **512 Cells, 16384² Tiles, 65.5 km**, and `0085` is superseded on its decision. **The reason the
> session had to be re-opened is the most useful thing in this document and it is recorded in full
> below, under *The process failure*.**

## The process failure, recorded first because it is the finding

**This session was run without the user, and it should not have been.** A session in this project is a
*design* act — `PROCESS.md` says so and the board's whole argument track is built on it — and I treated
it as an execution task: one scoping question at the start, then four ADRs and eleven document
amendments taken alone.

**Two of the five decisions I took unilaterally were wrong, and both were overturned within minutes of
the user seeing them.** Neither needed new information:

| What I decided alone | What the user said | Cost of not asking |
|---|---|---|
| The map stays at 4096², and the job is to find another mechanism that produces separate towns | *"It is up to the player whether they settle on several small towns, fewer larger ones, or one blob"* | **The whole frame was wrong.** The question was never *which mechanism*; it was *does the player have the choice*. They do not, and that is the defect |
| A bigger map is dead on density — 233/km² at 1M is rural | *"We could make the map larger, why not?"* | **`adr/0021` already answers it**: unbuilt ground is a null, so the density to check is the developed one. I cited that ADR twice in the same sitting and did not apply it |

And a third, which the user reached by simply asking *why*: **why is it so short?** Because a Tile is 4 m
and 4096 × 4 m is 16.4 km. I had the number in three tables and never said the sentence.

***The failure mode is specific and worth naming: an agent working alone optimises for a defensible
answer, and a defensible answer to the wrong question is the most expensive thing it can produce.***
`adr/0085` is internally sound, cites correctly, survives its own revisit triggers, and is wrong. It
would have read as settled to every later reader — which is exactly the property the corpus's ADR
discipline is supposed to guarantee, working against it.

The corollary for this corpus: **`adr/0043` types a claim as measurable or arguable, and there is a third
type it does not name — a claim that is the user's to make.** *Whether the late game is one city or
several* is not a measurement and not an argument; it is a preference about what the game is, and no
amount of rigour substitutes for asking. The typing pass below caught two claims I had no right to settle
by argument and missed the one I had no right to settle at all.

## The typing pass, run before anything was argued

Per [`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md),
every claim typed *measurable* or *arguable* before the session was allowed to close any of it. The pass
is recorded because **it is what stopped the sitting from settling two things it had no right to**.

| Claim | Type | Disposition |
|---|---|---|
| The save's contents | **Neither** — already decided by the per-field declaration | Not J's to argue. J's job was noticing |
| The save's size at 1M | **Measurable, and measured** | S0a: 85.98 MiB of tables. Quoted, never argued |
| Header contents, migration shape, slot-exactness | Arguable | `0086` |
| Async save's mechanism after `adr/0037` | Arguable | `0087` |
| 4096² on **density** | Arguable | Survives untouched |
| 4096² on **corner-to-corner travel** | **Measurable, and refuted by re-derivation** | `0085`. Not argued — the number already existed |
| What Commute Budget produces more than one Settlement | **Measurable, and measured** | S2 R1.5. Read, not re-derived |
| The Commute Budget's own value | **Measurable, not yet measured** | Left open. Ratifier is 5b-bis's distribution |
| Outside Connection: how many, where, who places, one abstraction | Arguable, all four | `0088` |
| Its throughput, price and count **values** | Hash-bearing, unset | Three §D2 gaps with a named ratifier |

**Two rows are the point of the table.** *4096² on travel* and *what Budget produces a Settlement* both
look arguable and both had numbers sitting in the corpus already — so a session that had skipped the pass
would have argued its way to a conclusion the evidence contradicts, on the item the board thought was
routine.

## What was decided

| | Question | Answer |
|---|---|---|
| 1 | How big is the map? | ~~4096², on density alone~~ → **`WorldCells = 512`, 16384² Tiles, 65.5 km** (`adr/0089`), sized by *commutes across*. **Not yet flipped** — gated on hash-moving-queue item 6. The 2048² fallback is struck either way |
| 2 | What makes a Settlement, then? | **A missing edge** — unbuilt ground, Severance, a mode nobody has, congestion, **and, once the map moves, distance**. `adr/0085`'s *never a distance* was true only of the small map |
| 3 | What is in a save? | **The field declaration**, in the hash's fold order. Nothing authors a layout |
| 4 | How many version numbers? | **Three** — declaration set, Ruleset content hash, generator. `06`'s debt paid |
| 5 | May a save compact? | **No.** The hash folds every slot and the free list, so compaction is a design change under `05 §4` |
| 6 | Where does an async save read from, now the Past is gone? | **A copy at the end of Phase 7**, paid once per autosave |
| 7 | Is that copy affordable? | **Yes, by four orders of magnitude.** `adr/0037`'s number needed a denominator, not a shrink |
| 8 | What is an Outside Connection? | **An ordinary Building.** 5a-bis and `adr/0031` had already built everything it needs |
| 9 | Road, rail, port — one or three? | **One abstraction, three kinds**, differing in ceiling, price, favoured Goods and mode mask |
| 10 | What does a distant Hinterland cost? | **Congestion on your own streets.** A distance tariff is refused |

## The findings

### 1. The corpus already held the answer to its own question, taken for something else

`plans/0002` ledger #1 justified 4096² partly on *"the three-quarters-of-a-Day corner-to-corner figure…
is what makes far Settlements genuinely separate."* Corrected against
[`adr/0082`](../docs/adr/0082-the-behavioural-clock-is-global-and-car-following-sub-steps-inside-it.md)'s
clock, that crossing is **1.4–2.7% of a Day**, out by 52–73×.

That much is arithmetic. The finding is what happened next: **S2 R1.5 had already measured the
consequence and published it in a table nobody read that column of.** R1.5 swept the Commute Budget to
compare union-find against Tarjan — a question about *algorithms* — and its Settlement-count column
returns **1, holding all 121 Districts, at every rung from 40 Ticks to 120**, against a 30-minute Budget
of **171 Ticks**. The sweep never even reached a realistic Budget, because 120 was already deep into the
region where the answer had stopped moving.

***A measurement answers every question its numbers bear on, not only the one it was run for.*** The
corpus has the inverse of this written down twice — `adr/0043` on claims a measurement could settle, and
S2 R0's *a benchmark cannot refute a claim about a case it never constructs*. This is the third face:
a benchmark **can** refute a claim it was not aimed at, and nothing goes looking. The practical form is
narrow enough to act on — *when a spike sweeps a parameter, the sweep is evidence about every decision
that parameter appears in* — and R1.5's table appears in `spike-results` under a heading about Tarjan.

### 2. Three mechanisms in one sitting kept their conclusions and lost their reasons

Not one of the three items produced a reversal, and all three produced a replaced ground:

| | Conclusion | Ground before | Ground after |
|---|---|---|---|
| Map size | 4096² | density **and** separateness | density alone |
| Async save | asynchronous, no hitch | walking an immutable Past | a copy amortised over the autosave interval |
| Edge choice | a real decision | longer hauls | congestion on your own network |

This has a shape and the shape is `adr/0082`'s, one sitting earlier: *the argument that survived was the
one listed last*. **A decision given several grounds is load-bearing on whichever ones survive, and
nothing recomputes that when one falls.** The corpus's habit of stacking reasons is good — it is what
makes a decision hold up — and this is its cost: a stacked decision *looks* equally well-supported after
a ground is removed, because no document lists which reason is now carrying it.

### 3. A deletion obliges a re-derivation, and half the time the consequence survives

Milestone 5b closed on ***a decision that removes a representation defers every decision that reads it***
— `adr/0075` gave a Leg a cost and no path, and `adr/0041`'s Segment volume became unbuildable with
neither ADR saying so. `05 §7` is the same event with the opposite outcome: `adr/0037` deleted the Past,
§7 read it, and §7's conclusion **survives on a different mechanism**.

So the rule generalises with its sign removed. ***The obligation a deletion creates is a re-derivation,
not a retraction.*** And the dangerous half is the one that would have bitten here: a careful reader
noticing §7's dead premise and striking the paragraph would have **deleted a correct decision** and
re-introduced the save hitch, leaving an ADR trail reading as though `adr/0037` had required it.

⚠ **And the correction was already in the file.** `05 §3` — rewritten when `adr/0037` shipped — says the
saver *"takes one real copy at save time"*. Four sections later §7 still walks the Past. **One document,
two sections, contradicting since the same edit**, and neither sentence wrong on its own page. That is
`plans/0012` *Cause 1* at a granularity the audit has never operated at: it looks for a fact with two
homes across files, and this is a fact with two homes in one.

### 4. The section that was blocking a milestone had nothing in it

`05 §7`'s *format half* has been a 🟡 on the coverage map and an item on the Phase 2 wall list since the
map was written. It turned out to be **almost entirely already decided** — by slice 4's per-field
declaration, which made the save's contents a consequence rather than a choice — and what was actually
sitting there was a **stale transcription** of that decision, wrong in four of its five lines.

***A document that holds a copy of a decision it does not own reads as an open question for exactly as
long as nobody checks which.*** The mark was honest: nobody had argued §7. It was also measuring the
wrong thing, because the argument had been won elsewhere and the section had not been told.

The same shape produced item 3 of this session's scope in one paragraph rather than a sitting: *Outside
Connection layout* was a four-part open fork whose representation had been complete since 5a-bis gave
every Building an Access Point.

### 5. One dangling citation, in two files

`adr/0082` and `CONTEXT.md` both cite **`05 §26`** for the ~4 m Tile. `05` has eleven sections. The
figure is real and lives in that document's budget block; the pointer is not. Filed to `plans/0002`'s
readiness row rather than fixed silently, because it is the second copy that makes it worth recording —
one wrong citation is a typo, and the same wrong citation in two files is a transcription.

## What this session did not close, and must not be read as having closed

- **The Commute Budget.** Still unset, still hash-bearing, still ratified by milestone **5b-bis**'s
  Trip-cost distribution. `0085` uses 30 minutes as a *worked example* and everything it concludes is a
  ratio against whatever the real value turns out to be. At a Budget near 40 Ticks the map fragments and
  `0085` reopens; that is written into its revisit triggers.
- **Whether congestion splits a mature city.** The only Settlement generator left in a contiguous city,
  and every number quoted here is free-flow, under S2's standing frozen-cost-basis caveat. It needs the
  travel-time matrix on a congested basis, which is milestone **5c**.
- **Whether the travel-time matrix is per mode.** `adr/0020` reads a Settlement off one modeless matrix;
  `adr/0072` gives every Arc a mode mask and 5a computes per-mode components. `0085` states the
  requirement on 5c's *specification* and settles nothing structural, because the matrix is unbuilt and
  [`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) forbids the rest.
- **Freight's Stress weighting.** `plans/0002` design fork 14, and `0088`'s replacement friction depends
  on it entirely — a weighting of zero deletes the argument. Not settled here.
- **The three Outside Connection numbers.** §D2 gaps, ratifier milestone 10. Naming a ratifier is not
  choosing a value, per `adr/0052`.
- **`05 §1`, `§2`, `§6`, `§8`, `§10`.** Untouched. §6, threading, still gates milestones 10 and 11 and is
  the largest unargued thing left in that document.

## What is unblocked

**One new blocker, created here.** Hash-moving-queue **item 6** in [`0003`](0003-build-plan.md):
`RoadGenerator` must scope its lattice to developed land before `WorldCells` can move to 512. It is a
defect against `adr/0021` before it is a feature, and it is itself gated on `plans/0002` **ledger #2**,
*open map or progressive land unlock* — which has carried a recommendation and no decision since session
three, and which the full lattice has been silently answering all along. **Do not cap the generator**;
that is the workaround `adr/0073` forbids, and it would remove the only pressure that gets the unlock
rule written.

**Milestone 10.** All three of its named blockers are closed, and the milestone came out **smaller than
`06` described it**: no authored file format, no new Outside Connection subsystem, and a risk that has
moved from *unsaved state* — which `BOR0901` and the declaration make unrepresentable — to *a derived
column that does not rebuild to the value it had*, which is a real class with a sighting already.

`06`'s milestone 10 row and the Phase 2 wall paragraph in `plans/0002` are amended accordingly. **The
argument track's remaining rows are E, G, I and K2**, plus S2's deletion.

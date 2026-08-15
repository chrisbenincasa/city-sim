# A Skill Tier is earned by attendance, and the credential stays a wall

**A city produces its own skilled workers. When a Household's Life Stage advances and a new Young
Household forms, the Skill Tier its adults carry is read off the **Education** Need that Household
accumulated while it had children — so a school place actually taken is what raises a tier, and
`Sorting` becomes the *fast* channel rather than the only one. Experience still carries 1 → 2 and
carries it **slower** for a Citizen who missed schooling; **experience never reaches 3.** The
segments remain **three** and there is no tier 0.**

`LEGIBLE CAUSE` `PLAYER GOVERNS` `EMERGENCE`

Settled with the user in the room on 2026-08-15, grilling `04 §7` — the Jobs section, stale by
construction since [`adr/0026`](0026-wages-are-posted-locally-and-never-cleared.md) rewrote how jobs
work without anyone reading it.

## Why

### `04 §7` denied a mechanism the rest of the corpus had already acquired

The section's last sentence read *"higher-value Businesses require educated Citizens, who arrive by
**Sorting** — attracted by school provision — **rather than being educated in place**."* That was true
when [`adr/0010`](0010-one-clock-and-demographics-by-sorting.md) was written: one clock, no aging, so
composition changed only through who arrived and who left. **It stopped being true and the sentence did
not move.** Four things now contradict it:

| Where | What it says |
|---|---|
| [`adr/0011`](0011-household-life-stages-and-self-generating-population.md) | Life Stages and a **self-generating population** — the city has its own children. `adr/0010`'s own banner concedes Sorting is *"one of two demographic channels rather than the only one"* |
| [`CONTEXT.md`](../../CONTEXT.md) → *Life Stage* | *"a child's **schooling tier** is derived from the Household's stage"* — a stored field feeding school demand and nothing else |
| [`CONTEXT.md`](../../CONTEXT.md) → *Unemployment* | ***"Their children escape** — generational, via education and Life Stage"*, one of five exits from destitution |
| [`adr/0103`](0103-a-need-is-where-a-frequent-private-failure-accumulates.md) | an **Education** Need, refused on **Space** when a school is full — written the same day, with no downstream consumer at all if nobody is educated here |

***A description of the build is where to look and never what you found*** ([`adr/0093`](0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md))
applied to a **design document**: `§7` described the demographic model, and what it described had been
superseded twice by ADRs that never came back to edit it.

### The mechanism introduces no machinery, because every part already exists

1. A Household with children has a Life Stage saying so.
2. It has an **Education** Need, met by getting a child into a school with a free place —
   [`adr/0032`](0032-services-are-delivered-by-trips-not-by-coverage.md) **Attended**, a Provider List
   entry, a Trip, refused on **Space** when the school is full.
3. **That Need is the accumulator** — it falls while unmet and recovers when met
   ([`adr/0102`](0102-a-housed-departure-is-a-comparison-the-household-re-runs-not-a-threshold-it-crosses.md)),
   so a city running its schools full leaves Households sitting at a deficit.
4. **When the stage advances and a new Young Household forms, its adults' Skill Tier is read off that
   accumulated Need.** This step is what did not exist.

**No new duration and no new counter**, because the Need already holds the history — which is the
argument `adr/0102` took the same morning, arriving again on a different mechanism. And **the lag is
structural rather than authored**: the payoff for a school arrives one Life Stage later, which is the
slowest feedback in the game and is the right one to be slowest.

### The cut is legitimate here, and this is not the threshold refused the same day

Step 4 turns a relative scalar into a **category**, which is a threshold — and `adr/0102` spent the
morning refusing one. The two are not in tension, and `CONTEXT.md` → *Skill Tier* says why in its own
words:

> *"not as a balance wall but as a **category boundary** … the one place in the design where *scarcity
> is a gradient, never a wall* correctly does not apply, **because a category is not a quantity**."*

`adr/0102`'s refusal was that a **continuous** outcome — a Household leaving — had been given a cliff.
Here the outcome is genuinely discrete: you hold the credential or you do not. ***A cut is dishonest
over a gradient and honest over a category***, and the corpus had already located the one place where
that applies.

### The wall at 2 → 3 is kept, and it is what makes a school necessary rather than merely efficient

The user's first reading let experience reach every tier given enough time. It was put aside on the
existing rule's own ground: `CONTEXT.md` → *Skill Tier* says the credential boundary *"protects the
education → Office → exports chain from a bypass"*. **If time alone reaches tier 3, a patient player
never builds a school** — the city grows its own analysts eventually, Office staffs itself, and
schooling degrades from a structural requirement to an accelerator.

**What is taken from that reading is the softness at 1 → 2**, where it costs nothing: a Citizen who
missed schooling still climbs to tier 2 by working, and takes longer over it than one who did not. So
schooling **influences** both boundaries and **gates** only the top. Failing your schools costs the
city its top tier and its Office economy, not its workforce.

### The growth loop's damper already exists, and it is spatial

Closing the loop *schools → tier 3 → Office → exports → money → schools* was raised as an undamped
circuit needing the treatment `adr/0026` gave wages. **It does not**, and the objection was withdrawn
in the sitting: `adr/0032` had already written the negative feedback as an aside —

> *"good schools raise land value, which prices out the Families the school was built for."* `NO VERDICT`

A school district that works gentrifies, Families leave, the school empties and the pipeline thins. No
tuning, no cap, and it produces a dilemma to govern rather than a knob to turn. **Third time in one
sitting that a claimed gap was already answered elsewhere in the corpus.**

### There is no tier 0, and the entry that would have gained it argues against it

A fourth segment for the never-schooled was considered and refused. `CONTEXT.md` → *Skill Tier* ends
with a sentence that is load-bearing rather than decorative — *"it does not thin the labour market,
because thinness is a property of the number of segments and the **segments remain three**"* — and
`adr/0026` had to build shrinkage toward the Hinterland wage precisely because thin submarkets produce
signals that *"twitch in a way that reads as the simulation malfunctioning"*. **A fourth segment thins
every submarket, worst in the small cities where the twitch already shows.**

And the state it would name is already expressed: under this ADR, *never schooled* is **stuck at tier 1
and climbing slowly**. The test applied was ***name the mechanism that would read tier 0 differently
from tier 1***, and there is none. The one thing it would buy is a permanent underclass — which is a
**second wall**, in a design that permits exactly one and says so, and which would fight
`CONTEXT.md` → *Unemployment*, built around five **exits** rather than an absorbing floor.

## Consequences

- **`04 §7` is rewritten.** *"Rather than being educated in place"* is struck; Sorting is named as the
  fast channel beside the slow one.
- **`adr/0010`'s Sorting banner is amended.** *"Sorting is the sole channel for the top tier"* is false
  under this ADR: schooling reaches tier 3 and a city can now do its own schooling. One clock, no
  per-Citizen aging and Sorting itself all stand.
- **`CONTEXT.md` → Life Stage's *schooling tier* field acquires its first consumer.** It has been stored
  and read by school demand alone.
- **One hash-bearing number is created**, and under
  [`adr/0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md) it goes to
  [`plans/0002`](../../plans/0002-open-questions.md) §D2 unset: **how well-schooled a childhood must
  have been to reach tier 3**. Only one boundary needs it, because experience owns 1 → 2 outright.
- **Experience gains a second rate.** *Slower for a Citizen who missed schooling* is a second
  hash-bearing number and is filed with the first; it is a **ratio to** the schooled rate rather than a
  free value, so the file states one quantity and the engine derives the other (`adr/0059`'s shape).
- **Milestone 9b inherits it.** Life Stages and self-generation is where step 4 lands, and it needs
  Attended services to exist first — so nothing here is buildable yet, and under
  [`adr/0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) nothing may reason from its
  absence in the meantime.
- **The segments remain three, permanently rather than provisionally**, with the revisit trigger below
  standing in for the tier that was refused.

## What would trigger revisiting

- **A mechanism that must treat *never-schooled* differently from *unskilled*.** A Policy targeting it,
  an employer refusing it, a Departure reason naming it — any of those is when a fourth segment earns
  its cost, and none exists today.
- **A long run producing no tier-3 adults in a city with working schools, or producing them at the same
  rate as one with poor schools.** Both are countable off milestone 9b's run and both refute the cut's
  value directly. This is the named ratifier for the number in §D2.
- **`adr/0032`'s gentrification corollary failing to appear in a run.** The whole damper for this loop
  is that sentence. If a working school district does *not* price out its Families, the circuit is
  undamped and needs the treatment `adr/0026` gave wages after all.
- **Office ceasing to be the tier-3 consumer.** The wall's justification is protecting the education →
  Office → exports chain. If Office's staffing model changes so that chain no longer exists, the wall is
  protecting nothing and the slow path becomes the kinder reading.
</content>

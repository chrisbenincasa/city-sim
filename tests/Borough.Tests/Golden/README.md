# The golden-hash baseline, and how to re-baseline it

Slice 5 task 4 of [`plans/0008`](../../../plans/0008-tick-and-replay.md).

**The point is not that the State Hash never moves. It is that it never moves without somebody
saying so.** Every other determinism test in this repository is a closed loop — it runs a thing
twice and checks the two runs agree, which stays true however far the simulation has drifted from
what it used to compute. The two files here are the only numbers recorded on a previous day, so they
are the only thing that can notice a change nobody was looking for.

## What is committed

| File | What it is | Recorded from |
|---|---|---|
| `session-trace.txt` | Thirty-two State Hash samples from a 256-Tick session | `GoldenFixtures.Session()` |
| `world-hash.txt` | One State Hash over a hand-built city, with its row counts | `GoldenFixtures.Build()` |

**Two artefacts, because one of them cannot see three tables.** The session drives the simulation
through `step()` and is the more valuable of the two — it covers the Input Log, the phase order,
replay and the hash together. But the only verb applied before slice 7 is `Zone`, and a Zone command
creates a Lot; Buildings, Households and Citizens are reachable only through the cold API. Without
`world-hash.txt`, three of the four tables' saved columns would sit under no committed hash at all
and the baseline would be claiming coverage it does not have. When later slices give the player
verbs that build a city, the session absorbs the world fixture's job and this file can go.

The session is a code fixture rather than a `.borough` file because the codec is task 5's
([`adr/0039`](../../../docs/adr/0039-the-text-formats-are-a-fifth-project-not-a-core-exception.md)).
Writing a second reader here to load one today would have created the two implementations that ADR
exists to prevent. When task 5 lands, the session is committed as a file and this fixture becomes
what the codec is checked against.

## Re-baselining

A failure here is a question — *did you mean to do that?* — and the answer is often yes.

1. **Establish which kind of change it is.** A **design change** moved the city; an **optimisation**
   should not have. `05 §4`: *a change is an optimisation if the State Hash is unchanged, and a
   design change otherwise, however it was motivated.* **If you believed you were writing an
   optimisation, this test has just told you that you were not.** That is the whole reason it exists;
   stop and find out what moved before regenerating anything.
2. **Copy the new file out of the failure message.** Both tests print the exact file they would
   commit. There is deliberately no `--update-baselines` switch and no environment variable: a
   baseline that can rewrite itself is one CI misconfiguration away from approving every change it
   sees, which is a baseline that has stopped being one.
3. **If the *fold itself* changed, bump the version byte** in `World.HashSeed`. A change to the fold,
   to the composition order or to `Randomness.Mix` moves every hash in the project at once, and the
   signed seed is what distinguishes that from a regression. A change to the simulation's *behaviour*
   moves the baselines and leaves the seed alone.
4. **Say why, in the commit message**, in the terms this repository already uses: what moved, whether
   it was intended, and which document or ADR authorises it. A re-baseline whose commit message is
   *update golden hashes* is a re-baseline nobody can audit later.
5. **Never regenerate a baseline in the same commit as an unrelated change.** The diff is the record.

### The standing example

**Swapping softmax for Gumbel-max is hash-breaking and distributionally neutral.** The two draw from
the same distribution; the citizens make the same *kinds* of choices; every recorded hash moves
anyway, because the arithmetic that produced them is different. It is entirely safe to do
deliberately, with a re-baseline and a commit message that says so — and entirely unsafe to do
quietly, because from the far side of the change there is no way to tell it from the bug it hides.

That is the shape of every legitimate re-baseline: *the numbers moved, here is the sentence that
authorises it.*

## Editing the fixtures

`GoldenFixtures.cs` is data that happens to be written in C#. Every number in it is load-bearing and
tidying it is a re-baseline. If you need a wider session — a new verb, more Ticks, a different
cadence — that is a deliberate change under the procedure above, not a refactor.

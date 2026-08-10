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
| `session.borough` | The session itself, as the Input Log the runner replays | hand-written; checked against `GoldenFixtures.Session()` |
| `session-trace.txt` | Thirty-two State Hash samples from that 256-Tick session | `Borough.Headless --out` |
| `world-hash.txt` | One State Hash over a hand-built city, with its row counts | `GoldenFixtures.Build()` |

**Two further artefacts are not in this directory and are part of the baseline anyway**:
**[`rulesets/minimal.toml`](../../../rulesets/minimal.toml)**, which the session opens on, and
**[`rulesets/minimal-tuned.toml`](../../../rulesets/minimal-tuned.toml)**, which it **reloads into at
Tick 128** (slice 8 task 10). Both are named by content hash, so editing either moves every sample
here — `The_golden_ruleset_is_the_one_the_session_names` says so for the first with the number to
paste in, and the second's hash lives in `GoldenFixtures.TunedRulesetHash` and in the log's `reload`
line, which carries **both** hashes.

**The second Ruleset is the first with one number changed, and two tests hold it there.**
`The_two_golden_rulesets_differ_in_exactly_one_line` compares them with the comments stripped, so a
copy that drifts fails with the diff in the message. And
`The_committed_reload_moves_the_trace_and_only_after_it` is the one that makes *the golden session
reloads* worth saying: every sample up to Tick 128 is identical to the same session with no
transition, and every sample after it differs. Without that line the baseline could be covering a
reload that changed nothing.

**Two artefacts, because one of them cannot see three tables.** The session drives the simulation
through `step()` and is the more valuable of the two — it covers the Input Log, the phase order,
replay and the hash together. But the only verb applied before slice 7 is `Zone`, and a Zone command
creates a Lot; Buildings, Households and Citizens are reachable only through the cold API. Without
`world-hash.txt`, three of the four tables' saved columns would sit under no committed hash at all
and the baseline would be claiming coverage it does not have. When later slices give the player
verbs that build a city, the session absorbs the world fixture's job and this file can go.

**Slice 7 task 10a moved that boundary without dissolving it.** The session now opens with
`populate`, so it raises 121 Buildings, 360 Households and 1,000 Citizens, gives every Building its
kind's Bins and Rule Instances, and runs the Rule engine for 256 Ticks — the first time the golden
trace has covered any of that. What it does **not** cover is what `GoldenFixtures.Build()` was
written for: a destroyed Household and a destroyed Citizen, so the allocator's free head and its
never-reused id counter are off their initial values. Nothing in a session can destroy a row yet.
`world-hash.txt` goes when slice 10's Zone Rules can demolish a Building, and not before.

**Slice 10 task 7 arrived and the trigger named above turns out not to fire.** The session now
demolishes Buildings, so it frees Building, Bin and Rule Instance rows and hands them back out —
which is new coverage and is exactly what `world-hash.txt` was standing in for. It does **not** cover
what that file was written for. Demolition **evicts** a Household into the Unplaced Pool
([`adr/0054`](../../../docs/adr/0054-a-demolished-buildings-households-are-evicted-into-the-unplaced-pool.md));
it destroys neither a Household nor a Citizen, because destroying one would delete its Money. So the
free head and the id counter of those two tables are still at their initial values in every session
this project can record, and `world-hash.txt` stays. **The correct trigger is a session that can
remove a person** — emigration, or a Departure record — which is Phase 2 and not this slice.

**The session exists twice, and that is the codec's test rather than a duplication.** `session.borough`
is the artefact; `GoldenFixtures.Session()` is the same session built in C#. One test asserts they are
the same log and that the *parsed file* reproduces the committed trace — which is a stronger check
than any round trip the codec can run against its own output, because nothing writes the file first.
A codec checked only against what it just wrote agrees with itself by construction. This one is
checked against a file that was on disk before the run started, which is the situation a bug report
actually creates, and it is the property
[`adr/0039`](../../../docs/adr/0039-the-text-formats-are-a-fifth-project-not-a-core-exception.md)
bought a fifth project to guarantee.

If you change one, change the other. The test will tell you which line you missed.

## Re-baselining

A failure here is a question — *did you mean to do that?* — and the answer is often yes.

1. **Establish which kind of change it is.** A **design change** moved the city; an **optimisation**
   should not have. `05 §4`: *a change is an optimisation if the State Hash is unchanged, and a
   design change otherwise, however it was motivated.* **If you believed you were writing an
   optimisation, this test has just told you that you were not.** That is the whole reason it exists;
   stop and find out what moved before regenerating anything.
2. **Regenerate.** For the session, run the runner and commit the diff:

   ```
   dotnet run --project src/Borough.Headless -- \
     --log tests/Borough.Tests/Golden/session.borough \
     --ruleset rulesets/minimal.toml \
     --ruleset rulesets/minimal-tuned.toml \
     --ticks 256 --hash-every 8 \
     --out tests/Borough.Tests/Golden/session-trace.txt
   ```

   **`--ruleset` is not optional here and the runner will tell you so.** The session records the
   Ruleset's content hash, so a run given none — or given a different one — is refused before it
   starts rather than quietly reproducing a different city. **Both are needed**, because the session
   reloads: a replay resolves each transition's hash out of what it was given, and a transition
   nobody supplied is refused rather than run under the wrong Rules. `--force-ruleset` does not
   waive that one — it waives a mismatch, and Rules nobody has are not a mismatch.

   The runner writes exactly the committed format, so this is a re-record rather than a
   transcription. For `world-hash.txt`, which is a hand-built world rather than a session the runner
   can play, copy the file out of the failure message — the test prints it in full.

   **There is deliberately no `--update-baselines` switch and no environment variable.** A baseline
   that can rewrite itself is one CI misconfiguration away from approving every change it sees, which
   is a baseline that has stopped being one. Regenerating is a command a person runs, and the review
   happens on the diff.
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

`GoldenFixtures.cs` and `session.borough` are data that happen to be written in C# and in the log
format. Every number in either is load-bearing and tidying it is a re-baseline. If you need a wider
session — a new verb, more Ticks, a different cadence — that is a deliberate change under the
procedure above, not a refactor, and the cadence and Tick count are named in three places: the
fixture's constants, the trace header, and the command in step 2.

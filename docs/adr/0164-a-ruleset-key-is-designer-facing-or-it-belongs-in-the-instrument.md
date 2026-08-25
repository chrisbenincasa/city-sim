# A Ruleset key is designer-facing, or it belongs in the instrument

**Before a number becomes a Ruleset key, ask *would a designer ever set this?* If the answer is no, it
is not tuning data — it is a property of whatever fixture generates test worlds, and it belongs there
as ordinary code.** `SOLVE THE ACTUAL PROBLEM` `FAST ITERATION`

⚠ **This is the converse of a rule the corpus already carries and has only ever applied in one
direction.** `CLAUDE.md`: *"No tuning number is a `const` in simulation source. Everything **the
designer would want to change** lives in the TOML Ruleset."* ***The qualifier is load-bearing and has
been read as though it were decoration.*** A number no designer would want to change is not made
correct by living in a file; it is made **misleading**, because every key in a Ruleset is an invitation
to tune it.

---

## Why

### The rule was written against one failure and there are two

`adr/0015` made the Ruleset hot-reloadable so a designer could change the city without a rebuild, and
`CLAUDE.md` states the consequence as *a `const` where a Ruleset value belongs is a defect*. **That
catches the number hidden from the person who needs it.** It says nothing about the opposite error —
***a number exposed to a person who has no use for it*** — and the corpus has been accumulating those
without a name for them.

**The cost is not storage, it is the reader.** A Ruleset is the designer's surface, and every key in it
asserts *somebody should have an opinion about this*. A key that no opinion can improve spends the
designer's attention and, worse, **acquires a `plans/0002` §D row and a ratifier obligation under
[`adr/0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)** — which is
machinery for deciding whether a number describes the city correctly, pointed at a number that
describes no city at all.

### The test is one question and it is checkable

***Would a designer ever set this?*** Not *could* — a designer *could* set anything. **Would**, in the
course of making a city behave differently. Three ways the answer comes back **no**, and each has a
worked example already in the tree:

| Shape | Example | Where it belongs |
|---|---|---|
| **The real mechanism is a player verb** | `[roads] arterial_count = 0`, because ***"an Arterial is a player tool that does not belong in a generator"*** | the generator, neutered or absent |
| **The consumer is an instrument** | the land-use split session W nearly authored — `SyntheticCity` is *"an instrument, not a mechanism"* | the instrument, as ordinary code |
| **The value is derived and stating it is not choosing it** | `rulesets/minimal.toml`'s `revisit_ticks = 2048`, which *is* `TICKS_PER_DAY` | a file is fine, but the comment must say it is derived |

⚠ **The third is not a violation and is listed to keep the test from over-firing.** A derived value
written down so a designer can *find* it is serving the designer; `minimal.toml` already says
***"stating it is not choosing it"*** and that sentence is the disclaimer this test asks for.

### The instrument said so about itself and the session did not read it

`SyntheticCity`'s own summary: ***"This is an instrument, not a mechanism … A real city arrives through
Zone Rules and the Unplaced Pool; nothing about this class is how Citizens are meant to come into
existence, and when slice 10 lands there is a case for deleting it."***

🔴 **Session W proposed a `[lots]` key for the commercial share of a generated world, and got as far as
choosing its ratifier before the question *would a designer set this* was asked** — by the user, not by
the session. **The answer was no**, and it was no for the strongest available reason: **no designer
touches that class at all.** ⚠ **The same paragraph refused the other half of the proposal** — *"It
draws no randomness, deliberately … a fixture is exactly the kind of code somebody reaches for a
convenient random"* — so the proposal was to add both a designer-facing key nobody would set **and** a
`purpose_tag` to a class documenting its own randomlessness as a safety property. ***The instrument had
written down, in advance, both mistakes that were about to be made to it.***

## Rejected

**Judge by consumer instead — a key read only by `SyntheticCity` is scaffolding.** Mechanically
checkable, which is its whole appeal, and **wrong at both ends.** `[roads] block_tiles` is read by the
generator and *is* designer-facing — it sizes the city a designer wants to measure. And a key read deep
in the simulation can still be scaffolding if nobody would ever set it. ***The question is about the
designer, so a test that does not mention the designer answers a different one.***

**Judge by *does it ship in more than one Ruleset*.** Refused, and it would have been actively harmful:
several of this project's most load-bearing numbers are in exactly one file on purpose — `[traffic]`'s
volume-delay function in `congested.toml`, `[market]`'s damping in `twinned.toml` — because a
demonstration is *"a demonstration rather than a city"* and one file is the honest place for a mechanism
only one world exercises. **A count of files says nothing about whether an opinion is worth having.**

**Do nothing, and let §D absorb it.** The status quo. Refused because §D is the ledger of *numbers the
city's behaviour rests on*, and filling it with scaffolding degrades the one ledger `adr/0043` cites as
evidence for what has been examined.

## Consequences

- ✅ **Session W's third number is not written**, and W-Q3's answer moves into the instrument as
  deterministic index arithmetic —
  [`adr/0165`](0165-a-zone-permits-building-kinds-so-the-split-is-exclusive-and-the-instrument-paints-it.md).
  ***The sitting ends having removed a number rather than added one.***
- 🔴 **The existing keys have not been swept and almost certainly contain hits.** Raised by the user on
  2026-08-25 — *"we've defined ones that are only ever useful for demonstrations"* — and **not
  investigated here**, because a sweep is work rather than a decision. **Filed to
  [`plans/0012`](../../plans/0012-corpus-audit.md)** with the test above as its predicate. ⚠ **A hit is
  not automatically a defect**: the third row of the table is a legitimate reason to keep a key, and
  ***the remedy for a borderline case is the disclaiming comment rather than a deletion.***
- ⚠ **This rule constrains authoring and not the loader.** No refusal is added and none should be: a
  loader cannot know what a designer would want, and a mechanical check here would fire on the derived
  case for ever. ***It is a question to ask, and the place it gets asked is a review.***
- ⚠ **It does not license a `const` for anything a designer might plausibly tune.** The default is
  still the Ruleset, and `CLAUDE.md`'s rule is unamended. **This ADR narrows the exception, it does not
  widen it** — the burden is on the person keeping a number out of a file, and the sentence they owe is
  *who would set this, and why would they not*.

## What would trigger revisiting

- **`SyntheticCity` being deleted or promoted.** Its own comment says *"when slice 10 lands there is a
  case for deleting it"*. If a generated starting world becomes a shipped feature rather than an
  instrument, ***every number in it becomes designer-facing at once*** and the second row of the table
  above inverts.
- **Zoning becoming a player verb.** `CommandKind.Zone` exists with no production call site; wiring it
  makes the land-use split the player's, at which point the instrument's pattern is not scaffolding but
  irrelevant.
- **A sweep finding that most keys fail the test.** That would say the rule is too strict rather than
  that the corpus is wrong, and the fork would be a *third* category — designer-facing, instrument, and
  a named middle for numbers that are content but not tuning.
- **Any mechanical check being proposed for this.** It is deliberately not one, and a proposal to make
  it one should reopen the *Rejected* section's first entry rather than route around it.

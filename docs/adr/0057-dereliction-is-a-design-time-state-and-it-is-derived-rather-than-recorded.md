# Dereliction is a design-time state, and it is derived rather than recorded

**A Building is derelict when the Ruleset in force declares no kind for it. That is `Kind == 0` — a
two-compare predicate over a column the save already carries — and there is no `derelict` column, no
flag, no event and no notification.** The only thing that can produce the state is a *Ruleset edit
under a running city*, which happens while a designer is balancing and never while a player is
playing. So dereliction is **development-time state**: it is not decline, not abandonment and not
blight, it must never be given their mechanisms, and a derelict Building stands inert until the player
clears it.
`HONEST DEGRADATION` `SOLVE THE ACTUAL PROBLEM` `PLAYER GOVERNS`

## Why

**Ask when this happens to a player, and the answer is never.** A shipped game ships one Ruleset;
nothing in play removes a Building kind from under a live city. The Ruleset changes mid-city in
exactly two situations, and one of them does not exist yet: a **designer balancing**, which is
[`adr/0015`](0015-all-tuning-data-is-hot-reloadable.md)'s whole reason for being, and a **save made
under one Ruleset loaded under another**, which is `05 §7`'s cross-Ruleset load policy and milestone
10. Both are tooling. Dereliction therefore has no cause inside the city, cannot be reached by play,
and has nothing to say to a player — which decides almost everything below.

**It looks exactly like abandonment from the outside, and it is the opposite kind of thing.** Both are
a Building standing on a Lot doing nothing. But abandonment is a *gameplay outcome* with a cause the
city can name: Failure Pressure as a **duration** ([`adr/0053`](0053-failure-pressure-is-a-duration-not-a-tally.md)),
accumulated from failing Trips, Rules reaching a reporting terminal, or local conditions below the
Occupants' tolerance — and `CONTEXT` → Failure Pressure *requires* it to produce a sentence:
*"abandoned: 74% of work trips exceeded commute budget over 30 days"*. **A derelict Building has no
such sentence, because nothing in the city happened to it.** The only true sentence is *the Rules no
longer describe this*, which is a statement about a file. Giving dereliction abandonment's mechanism
would manufacture a causal story for an editorial act, which is `LEGIBLE CAUSE` failing from the
direction nobody watches: not an effect with no explanation, but an explanation with no cause.

**That is why [`adr/0055`](0055-a-zone-rules-permission-set-scopes-what-it-builds-never-which-lots-it-looks-at.md)'s
second consequence bullet is withdrawn rather than repaired.** It says a derelict Building *"is still
sampled and still dies of its own failures, rather than becoming a permanent monument to a Ruleset
edit"*. **The mechanism it names cannot fire**: a Building with no kind runs no Rules, so no Rule can
reach a terminal, so its pressure is zero for ever and the condemnation walk finds nothing. And the
obvious repair — condemn a derelict Building on sight — is **silent deletion arriving through the Zone
Rule instead of through the reload**, which is precisely what `adr/0015` forbids and the reason the
state is called derelict rather than removed. So `adr/0015` wins, the Building stands, and clearing it
is the player's. **A permanent monument to a Ruleset edit is the correct outcome when a Ruleset edit is
what happened.**

**Derived rather than recorded, because the record would be a cache of a two-compare predicate.**
`Ruleset.Declares(kind)` is `kind != 0 && kind <= KindCount`; `Kind` is saved and hashed already, and
the answer is free at every site that needs it. A `derelict` column would be a second spelling of one
fact, saved and hashed beside it, with the standard consequence: two things to keep in step, one of
which can be wrong. It would also be wrong at a *predictable* moment — the day a kind comes back —
because nothing clears a mark that records history.

> **The undo argument that used to carry this is withdrawn, and it is worth recording why.**
> `plans/0015` argued against the flag from the designer's commonest move — remove `bakery`, watch
> five hundred Ticks, put it back — on the grounds that a mark which never clears leaves a city of
> permanently inert bakeries and no fix but a restart. **That is true of the derived form too.** The
> migration sets `Kind` to 0, so the row stops naming anything and the re-add restores nothing.
> Neither design recovers the undo case. The cache argument is what decides this, and it decides it
> alone; the undo argument was doing no work and reads as though it were.

**What is lost is what the Building *was*, and losing it is the honest choice rather than the cheap
one.** The obvious alternative — leave `Kind` at its old id and treat dereliction as
`!Declares(Kind)` — recovers undo and is **worse**, because kind ids are declaration order. A
re-added declaration lands wherever the file puts it, and the next migration would map that stale id
through the *current* file's key, silently turning every derelict Building into whichever species now
occupies that position. That is the identity defect slice 8 closed, re-entering through the
degradation written after it. The alternative that actually works is a name key per Building row — a
`ulong` on the largest table in the city, bought for a case that already recovers by reloading a save.
Not paid, and stated rather than hidden.

## Consequences

- **There is one read side and one write side.** `Ruleset.Declares(kind)` answers the question; the
  reload's refit is the only thing that produces or clears the state. Nothing else in the core knows
  the word.
- **A derelict Building keeps its Lot, its Bins and its Occupants, and runs nothing.** Invariant
  `DerelictBuildingRunsNoRules` asserts the second half whole-world, because nothing in the shape of a
  re-arm loop makes the exclusion obvious.
- **Recovery is by refit, and it covers the case that actually happens** — a kind that survived the
  edit with different Bins or different Rules is refitted in place, on the same code path that fits a
  new Building. A kind removed and re-added does not recover; reloading a save does.
- **`05 §7`'s cross-Ruleset load must run the migration, not merely check it.** A save records `Kind`
  as a number, and kind ids are positional — so loading a save made under Ruleset A into Ruleset B
  without mapping ids by key is the identity defect on the load path, where it produces Buildings of
  the wrong species rather than derelict ones. Milestone 8 inherits this, and it inherits the map
  rather than a new mechanism.
- **The shell learns of dereliction as a count**, per reload, alongside Bins dropped and Rules
  re-armed. `Core` returns numbers ([`adr/0002`](0002-simulation-is-an-engine-agnostic-library.md)); the
  sentence a designer reads is assembled outside it.
- **`adr/0055`'s second consequence bullet is struck**, and the correction is owed to that document
  rather than to the code.

## What would trigger revisiting

- **A shipped game that reloads Rulesets under a live city** — patching, mods, or a scenario that
  swaps Rules mid-game. Then dereliction stops being development-time and becomes something a player
  can meet, at which point it needs a name in the UI, a readout, and probably a recovery path. This is
  the trigger for the whole ADR and not for one clause of it.
- **A second producer of `Kind == 0`.** Today the value means exactly one thing, which is what makes
  the predicate a diagnosis. If anything else ever writes it — a construction site, a placeholder, a
  ruin the player may restore — the flag argument reopens on its merits, because the predicate would
  no longer be answering the question it is being asked.
- **A non-conforming-use mechanic being wanted deliberately** — a Building whose kind is no longer
  admitted decaying *because* it is non-conforming. That is a fourth Failure Pressure source and
  belongs in `02 §5.9`, added there openly rather than smuggled in by giving dereliction a pressure.
  It is also already `adr/0055`'s own revisit trigger, from the zoning side.
- **A designer asking which kind a derelict Building used to be.** That is the one question the
  derived form cannot answer. The fix is a name key per Building row, and it becomes worth its price
  if remove-and-re-add during balancing turns out to be frequent enough that reloading a save is not
  an answer.

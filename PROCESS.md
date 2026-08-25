# PROCESS.md

How work on this project is organised, and what each word for a unit of work means.

`CONTEXT.md` is the vocabulary of the **city** — Household, Bin, Trip, Segment. This file is the
vocabulary of the **project** — slice, spike, gate, session. Nothing here describes the simulation,
and nothing in `CONTEXT.md` describes how we work. If a term belongs to the city it goes there; if it
belongs to the calendar it goes here.

Written plainly on purpose. The rest of the corpus argues; this file just defines.

---

## Where things live

Four documents carry status or plans, and it matters which is which. A **source** owns a fact. A
**view** restates facts owned elsewhere, so it can be stale and is never authoritative.

| Document | Kind | Owns | Answers |
|---|---|---|---|
| `plans/0000-board.md` | **view** | Nothing. A flat status of everything, for orientation | *what is next* |
| `plans/0003-build-plan.md` | **source** | The order slices are built in, and what gates each one | *what is done* |
| `plans/0002-open-questions.md` | **source** | **Every open question in the corpus**, typed and grouped by what is blocked | *what needs answering* |
| `plans/0012-corpus-audit.md` | **source** | Corrections owed to documents. Deleted when empty | *what a document says wrongly* |
| `docs/adr/` | **source** | Settled decisions, one per file | |
| `docs/spike-results.md` | **source** | Measured numbers, and the decision each produced | |
| `plans/0004`… | **source** | One plan document per slice or spike, holding its task list | |

When the board disagrees with a source, the source is right.

**A question is written in one place.** It goes in `0002`, once, whoever found it and whatever raised
it — a slice plan's *Decisions owed* section, a spike's findings, an ADR's Consequences. The failure
this rule exists to stop has already happened: the board accumulated 63 open items while the file named
*open questions* held none, because it was organised by *which session raised a question* rather than
by *what is open*, and nobody could read it. A correction is **not** a question and goes to `0012`.

---

## Units of work

From largest to smallest.

**Phase** — a stage of the whole project. There are four: Phase 0 is scaffolding, Phase 1 is the
simulation substrate, Phase 2 is the city coming alive, Phase 3 is presentation. Owned by
`docs/06-roadmap.md`. *See the warning about this word below.*

**Milestone** — a numbered goal inside a phase, named by **the risk it retires**. Milestone 3a is the
Rule engine. Owned by `docs/06-roadmap.md`. A milestone names an outcome, never a task list.

**Slice** — the unit you actually sit down and build. Defined in `plans/0003` as *the smallest amount
of work that leaves the build green and retires something*. Sized for one or two sittings. Slices are
numbered from 0 and run in a fixed order. A slice may be smaller than a milestone (milestone 3c was
split across slices 6 and 10) or may not map to one at all.

**Task** — a numbered step inside a slice, listed in that slice's plan document. "Slice 5 task 7" is
a task.

**Spike** — a throwaway experiment that answers a question with a **number** rather than an argument.
Lettered `S0`–`S4`. A spike's code is deleted when it reports; its numbers live forever in
`docs/spike-results.md`. Spikes run on their own track and do not block slices unless a slice's gate
names one.

**Round** — a numbered section of a long spike. `S2 R5` is round 5 of the routing spike. Rounds are
`R0`, `R1`, … and sub-rounds are `R5.4`.

**Session** — a sitting spent *arguing* rather than building: taking a design document that was
written from research and stress-testing it until it either survives or produces an ADR. Lettered
`A`–`Z`. A session is work, not waiting.

---

## Numbering

**One scheme, written down here because it was not.** Session K found `06`'s Phase 2 carrying three
notations at once — numbers that no longer matched the order, a `-bis` suffix that meant *inserted
after*, and a session with a digit in it — and none of the three was defined anywhere. The scheme
below is what the corpus mostly already did; the parts it did not do are corrected rather than
grandfathered.

| Unit | Form | Rule |
|---|---|---|
| **Phase** | `0`–`3` | Fixed. There are four and there will not be a fifth |
| **Milestone** | an integer | ~~**The integer is the position in the sequence.**~~ ⚠ **An IDENTITY, not a position, as of 2026-08-22** ([`adr/0140`](docs/adr/0140-a-milestone-number-is-an-identity-and-the-roadmaps-order-is-the-sequence.md)) — allocated **next-free**, never re-used, and **`06`'s table row order is the sequence**. Unique across the project |
| **Sub-milestone** | integer + lowercase letter | A milestone shipped in independently-runnable parts. Letters run in order: `7a` before `7b` |
| **Slice** | an integer | `plans/0003`'s, from 0, in the order built. A *different axis* from the milestone — slice 6 and milestone 6 are unrelated |
| **Task** | an integer | Inside one slice, spike or milestone plan. *"5c task 8"* |
| **Session** | a capital letter | In the order scheduled. **`I`, `O` and `S` are skipped** — the first two read as `1` and `0`, and `S` is the spike axis |
| **Spike** | `S` + integer, optionally + lowercase letter | Its own axis, unrelated to milestones. `S2`, `S0a` |
| **Round** | `R` + integer, optionally `.integer` | Inside one spike. `S2 R5.4` |
| **Plan document** | `plans/NNNN` | Its own axis. One per slice, spike or session |

**The insertion rule, which is the whole reason the scheme drifted.** ⚠ **SUPERSEDED 2026-08-22 by**
[`adr/0140`](docs/adr/0140-a-milestone-number-is-an-identity-and-the-roadmaps-order-is-the-sequence.md)
— ***a milestone number is an identity and `06`'s table order is the sequence***, so **inserting work
appends a number and moves a row, and renumbers nothing.** 🔴 **The rule below rested on a premise
that was measured on the day it was next needed and had expired**: the unshipped tail carries **276
citations across 73 files**, within 2% of what the shipped head cost when the rule was written to
avoid touching it. ***The tail became expensive because the corpus kept describing work it had not
started***, which is the behaviour the rest of this document rewards. **The shipped-milestone clause
below survives whole; `0140` generalises it to every row.** *The superseded rule follows.*

> A milestone that has **shipped**
> keeps its number for ever: the number has become history, and every citation pointing at it is a
> record of work that happened under that name. A milestone that has **not started** holds a position
> rather than a name, so inserting one **renumbers the unshipped tail and nothing else**. That is
> affordable precisely because it only ever touches rows nobody has built, which are the least cited
> rows in the corpus — session K's renumber moved 126 references where renumbering the shipped head
> would have moved 282.

~~**Whenever a renumber happens, the retired numbers are kept in a table in `06` for ever**~~ — ⚠ **the
table is FROZEN and a third block will never be added** (`adr/0140`), because there will be no third
renumber. **It stays for ever**, so a citation written before 2026-08-22 still resolves, and `06`'s
*read the subject, never the digits* instruction still governs those. **A number is never re-used for
different work**, and next-free allocation is what now guarantees it.

### Retired forms

Kept readable, and not to be used again.

- **`-bis`** (`5a-bis`, `5b-bis`) meant *inserted after a milestone that had already shipped*. It
  existed because there was no insertion rule; under the rule above the correct move is the next free
  number in the unshipped tail. Both rows that carry it are shipped and therefore frozen.
- **Sessions named with a spelled-out number** — **`eight`** and **`nine`**. Both are closed, so both
  are frozen; the form is not to be used again. `nine` is the one that produced
  [`adr/0042`](docs/adr/0042-a-planning-document-cites-and-a-design-document-owns.md), which is why
  it is cited often enough to be worth knowing about.
- **`K1` / `K2`**, a session split with **digits** where the scheme uses lowercase letters. `K1` is
  closed and frozen. `K2` was still open, the split is over, and it takes the parent letter: **`K`**.
  A session that genuinely needs splitting from here takes `Ka`, `Kb` — the same form a milestone's
  sub-parts take, because it is the same idea.

**Three notations were live on the session axis at once** — capitals, digit-suffixed pairs and
spelled-out numbers — and none of the three was defined anywhere. That is what *"we need a more
consistent way of doing this"* was about, and the freeze rule is what makes fixing it affordable:
only `K2` was open, so only `K2` moved.

---

## The three tracks

They do not contend for the same person-hours in the way they look like they do.

| Track | What it is | Blocks |
|---|---|---|
| **code** | Somebody at a keyboard building a slice | The project |
| **argument** | A grilling session against a document | Whatever slice its gate names |
| **spike** | A machine running unattended | Only what a gate names |

There is also a **Godot track** (`S1`, `S3`), which is empty until Phase 3 planning needs it.

**The standing rule:** an argument session runs when something concrete is blocked on it, and not
because it is available. This was learned the hard way — a session run purely because it was
available produced three more sessions' worth of open questions.

---

## Status words

**Gate** — a thing that must be settled before a slice may start. Almost always a design question,
not more code. A slice with an uncleared gate is not started, because a task list written against an
unsettled decision gets rewritten.

**Cleared** — the gate is settled and the slice may begin.

**Owed** — a debt: somewhere the corpus currently says something known to be wrong, or a piece of
work that was deferred deliberately. Listed at the bottom of the board. None of it blocks anything;
all of it is real.

**Ledger** — `plans/0002`'s list of open design questions, grouped by what is blocked on each and
typed *measurable* or *arguable*. Everything below it in that file is an **archive** of the sessions
that raised them, and is not status.

**Unratified** — a number somebody chose because a choice was needed, which nobody has justified. The
project's own finding is that *an unratified number is more dangerous than an open question*, because
it stops looking like a question. Every slice must record the ones it created before it closes.

**Tripwire** — a number stated *in advance* that would refute a claim, together with the machine that
would produce it. Publish the break-even, never a multiple over a guessed denominator.

**Arguable / measurable** — every claim gets typed before it is settled (`adr/0043`). If you can name
the number that would refute it and the machine that would produce it, it is **measurable** and must
go to a spike. Otherwise it is **arguable** and a session may close it. Six claims in this corpus have
been measured false, and two of them were sitting in documents marked as fully argued.

---

## Two words that mean two things

Both are live, both are in the code, and both will bite.

**Slice.**

1. A unit of dev work — *slice 7*, the Rule engine.
2. A subset of rows swept per Tick by the staggered invariant tier — `InvariantRegistry.Slices = 64`,
   and `02 §10`'s *"one slice per Tick"*.

*Rule until this is fixed:* a slice with a number attached (*slice 7*) is always the dev unit. The row
partition is always written as **stagger slice**. Renaming the second one in code is owed — it is one
class and two documentation lines, against twelve plan documents and forty-nine ADRs using the first.

**Phase.**

1. A stage of the project — *Phase 1, slice 6*. Four of them, 0 through 3.
2. A step within one Tick — *Phase 0 is Input, Phase 2 is Decide*. Eight of them, listed in
   `CONTEXT.md` and `02 §1.1`.

These collide directly: "Phase 0" is both the scaffolding stage of the project and the Input step of a
Tick.

*Rule:* always qualify. Write **Tick phase** or **project Phase**, never a bare "Phase 0". In code the
distinction is carried by the `TickPhase` type, which is why the collision has not caused a bug yet.

---

## What a slice must do to be finished

The full list is in `CLAUDE.md` and `plans/0003 §Definition of done`. In short:

- `dotnet build` and `dotnet test` pass on a machine with no GPU and no Godot.
- Every invariant it adds is registered in a frequency tier, never behind a build flag.
- Nothing grows without bound over a long run.
- Any State Hash change was deliberate and re-baselined.
- There is something to look at.
- Every unratified number it chose is written into `plans/0002` before it closes.

---

## Conventions

- ADR filenames are the claim, stated as a sentence:
  `0048-the-ruleset-is-validated-where-it-is-parsed-….md`.
- **An ADR has four sections and the last one is not optional**: the title as a claim, the decision in
  bold up front, `## Why`, `## Consequences`, `## What would trigger revisiting`. *A decision with no
  revisit trigger is a decision nobody can reopen honestly*, which is why the fourth is required rather
  than conventional.
- ADRs are never deleted or silently amended. A wrong one gets a correction block or a banner, so the
  reasoning that was wrong stays readable. **The form is the banner at the top of
  [`docs/adr/0005-two-fidelity-tiers.md`](docs/adr/0005-two-fidelity-tiers.md)** — named here because
  *a convention with no exemplar is a convention every author re-invents*.
- **Every significant decision cites a guiding concept** from `CONTEXT.md`'s tag table. A decision that
  cites none is a decision without a justification.
- Prose is British — modelled, behaviour, optimise, serialisation, sterilise. **The register is dense
  and argumentative: state the claim, then the reasoning that survives objection.** ⚠ **This governs
  documents only.** A reply to the user in the terminal is plain English — lead with the answer, explain
  each term and citation inline as it is used, and debrief at the end of a chunk of work. *The corpus's
  register is a property of the corpus and not of the project*, and carrying it into a chat reply makes
  a status report unreadable.
- **Before a number becomes a Ruleset key, ask *would a designer ever set this?*** If no, it belongs
  in whatever fixture generates test worlds, as ordinary code
  ([`adr/0164`](docs/adr/0164-a-ruleset-key-is-designer-facing-or-it-belongs-in-the-instrument.md)).
  ⚠ **This is the converse of `CLAUDE.md`'s *no tuning number is a `const`*, and the corpus had only
  ever applied that rule in one direction** — the qualifier *the designer would want to change it* is
  load-bearing. A key nobody would set is not made correct by living in a file; it is made misleading,
  because every key in a Ruleset invites somebody to tune it and drags a `plans/0002` §D ratifier
  obligation behind it. ⚠ **It is a question asked in review and deliberately not a mechanical check.**
- Documents cross-reference by section: `02 §4.1`, `05 §9`.

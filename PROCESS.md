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

| Document | Kind | Owns |
|---|---|---|
| `plans/0000-board.md` | **view** | Nothing. A flat status of everything, for orientation |
| `plans/0003-build-plan.md` | **source** | The order slices are built in, and what gates each one |
| `plans/0002-open-questions.md` | **source** | Open design questions and the reasoning behind them |
| `docs/adr/` | **source** | Settled decisions, one per file |
| `docs/spike-results.md` | **source** | Measured numbers, and the decision each produced |
| `plans/0004`… | **source** | One plan document per slice or spike, holding its task list |

When the board disagrees with a source, the source is right.

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
`A`–`M`. A session is work, not waiting.

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

**Ledger** — `plans/0002`'s numbered list of open design questions.

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
- ADRs are never deleted or silently amended. A wrong one gets a correction block or a banner, so the
  reasoning that was wrong stays readable.
- Prose is British — modelled, behaviour, optimise.
- Documents cross-reference by section: `02 §4.1`, `05 §9`.

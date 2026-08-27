# 0045 — The amnesty

**Read this instead of the board. It is the only thing in flight.**

Opened 2026-08-26. Expires **2026-10-07**. One page, and it stays one page.

---

## The situation

**1.17M words of prose against 17,872 lines of executable simulation.** 169 ADRs — one per 106
lines, 30 of them in five days. **236 of 524 commits changed no code.**

Nobody ages, is born or dies. Money flows one way into Businesses because wages are unbuilt.
Every shipped world decays. There is no renderer and no plan for one.

The corpus is a collection that grows with elapsed time and has no sink — `adr/0006`, violated
by the documents that cite it.

---

## Standing orders

Until this expires:

1. **No new ADRs.** `CorpusBudgetTests` fails the build if `docs/adr/` grows past 169.
2. **No new entries in `plans/0002` §A–§F.** The file is frozen at its 2026-08-26 size.
3. **No corpus growth.** `docs/` + `plans/` are capped at their 2026-08-26 word count.
4. **`adr/0043` and `adr/0052` are suspended.** Choose numbers by taste, stamp them `PROVISIONAL`,
   open no §D row, name no ratifier. *Ratification needs a city and the city needs the numbers, so
   those rules now prevent every commitment rather than only premature ones.*
5. **A session that ends without a change under `src/` is not committed.**

To break any of these, delete the test that enforces it in its own commit saying why. The escape
hatch is meant to be visible, not hard.

---

## Definition of done, amended

`CLAUDE.md`'s last clause was *"something to look at"*, and a column of hexadecimal was satisfying it.

> **A milestone is done when you have watched it happen and something surprised you.**

---

## The queue

Ordered. Do not reorder without deleting this line.

| | Work | State |
|---|---|---|
| 1 | `CorpusBudgetTests`, this page, the `CLAUDE.md` pointer | ✅ 2026-08-26 |
| 2 | Write `Citizens.Activity` — was saved, hashed, per-Tick, **no writer** | ✅ 2026-08-26 |
| 3 | `--day` — one Citizen, one Day, off `Evidence.OfCitizen` | ✅ 2026-08-26 |
| 4 | 🔴 **Nobody comes home** — see below | ⬜ |
| 5 | Wages — close the money loop | ⬜ |
| 6 | Ageing, birth, death — write `Citizens.Age` | ⬜ |

Items 2 and 3 cost one day, added no Ruleset key, no number and no ADR, and moved three golden
baselines because a hashed column stopped being all zeros (`adr/0100`).

## What `--day` found on its first run

`minimal.toml`, 2,000 Citizens, one full Day traced from Tick 4,096:

- **at work 415 → 481; walking, in either direction, 0 at both readings.**
- The at-work population **only ever grows**. Over a whole Day it gained 66 and lost nobody.
- The traced Citizen had a home, an employer with premises, a 7-minute planned commute — and
  **made no journey at all.**

⚠ **The zero-walking readings are taken at midnight and prove nothing.** The ratchet is the signal:
people arrive at work and the homeward journey never fires. Diagnose before building item 5 —
a wage paid to somebody who never leaves the building is not a test of anything.

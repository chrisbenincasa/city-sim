# 0128 — The gate ships before the comparison that walks through it

**Guiding concept: a mechanism named inside a decision record is a dependency, and a dependency graph
assembled from milestone rows cannot see it.**

**Status:** accepted, 2026-08-20, with the user in the room. Closes
[`plans/0035`](../../plans/0035-hinterlands-and-arrival-through-the-gate.md) decision **1**.

---

## The decision

**Milestone 11 builds the gate, the Hinterland, the arrival route and the money door. It does not build
the comparison that decides to arrive.** That comparison is `02 §5.4`'s residential choice model with the
Hinterland as an ordinary row, and it stays at milestone **16**, where `06` already puts it.

**`06`'s dependency graph is unchanged.** The edge *Hinterland → the residential choice model* keeps its
direction, and the permutation `06` calls forced stays forced.

---

## The problem this answers

Three documents specify arrival as the choice model.
[`adr/0023`](0023-immigration-arrives-through-the-gate.md) opens with it — *"There is no immigration
rate, no arrival scalar, and no attractiveness meter. **A prospective Household evaluates the city using
the same choice model residents use**"* — `CONTEXT.md` → Hinterland repeats it as *"the identical utility
function residents use"*, and `02 §5.4` says *"a prospective Household compares staying outside against
moving here with the identical utility function."*

**The model those three sentences name is milestone 16.** So the milestone that owns arrival sits five
positions in front of the mechanism its own records say arrival is made of.

⚠ **Nothing in the corpus was wrong, and that is the point.** `06`'s dependency graph is assembled from
**milestone rows**, and this dependency is stated inside an **ADR**. ***A graph built by reading the
rows is a graph of the edges somebody wrote in a row***, and `06`'s note that slots 9 and 11 through 14
*"admit exactly one arrangement — nothing here was a preference"* was derived from four edges without
this one. The arrangement is still the same; the derivation was one edge short of knowing why.

---

## Why not build the choice model here

[`adr/0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) says the choice model is **unbuilt**
rather than refused, and that the answer to *given X does not exist, should Y compensate?* is **build X**.
Taken literally that ends the discussion. It was not taken literally, for one reason: **building X here
inverts an edge rather than satisfying it.** The choice model reads a price surface (13) and money (10)
as well as the Hinterland, so pulling it into 11 pulls two more milestones' outputs in behind it, and
`adr/0070`'s rule is about an absence being used as *evidence* — it does not say every absence must be
filled by the milestone that notices it.

⚠ **The rejected third option is the one worth recording.** A deliberately crude acceptance rule at 11,
marked a hole for 16 to replace, was available and is refused. It is milestone 9's **F13** by
construction: ***a hole that throws is safe because nothing can read it, and one that returns plausible
numbers is a working mechanism that says something false.*** An arrival rule of that kind would produce a
population curve somebody would read, and read as the city.

---

## What this costs, stated rather than discovered

🔴 **At milestone 11 the arrival door has no autonomous caller, and the milestone must not pretend
otherwise.** Its only callers will be a **Command** and its tests — the same shape as
`CommandKind.Populate`, which `World.cs:853` already distinguishes in exactly these terms: ***it is the
founding door and it is not the gate.*** So 11 ships a gate that opens when something tells it to, and
nothing tells it to on its own until 16.

⚠ **That is milestone 9's F17 arriving before the milestone starts rather than in its fourth task.**
There, the producer shipped correct and unobservable because no world exercised it. Here the equivalent
is known on day one, and the consequences are:

- **`06`'s milestone 11 row retires half its stated risk.** *No price in the design has an anchor* is
  retired by authoring the Hinterland. ***Nothing says where Households come from*** is **not** retired
  here — a door is not an answer to *where from*. The row is amended rather than left to read as though
  it were.
- **The acceptance criterion is about the door and never about the population.** A test that asserts a
  city grew would be asserting the Command it just issued.
- **Rejected arrivals with reasons move to 16.** `adr/0023` makes them a required deliverable — *"Without
  this the anchor is felt rather than observed"* — and **nothing declines an offer nobody makes**. The
  obligation is not discharged, it is relocated, and it stays `adr/0023`'s.
- **The Hinterland's authored prices will be read by nothing in the tree on the day they ship.** The
  District Pool is 12 and the price surface is 13. This is
  [`adr/0125`](0125-a-ratifier-that-needs-a-consumer-nobody-built-is-not-reachable-so-the-weights-get-a-floor-and-a-debt.md)'s
  situation exactly — ***a ratifier that needs a consumer nobody built is not reachable*** — and
  `plans/0035`'s §D1 rows must be written knowing it rather than discovering it.

---

## Consequences

- **`06`'s milestone 11 row is amended** to say which half of its risk it retires, and its inventory row
  for *arrival through an Outside Connection* names 16 for the decision half.
- **`plans/0035`'s task list keeps the door and drops the comparison.** The give-up rule
  (`plans/0035` decision 3) does **not** move with it: an inflow driven by a Command is still an inflow,
  and `adr/0006` does not care what called it.
- **`adr/0023` is not superseded and not amended.** Every sentence in it stays true; what changes is
  which milestone builds which half, and that was never in the ADR.
- **Milestone 16 inherits a named obligation** rather than a vague one: the comparison, the rejected
  arrivals and their reasons, and the crossover announcement `adr/0023` describes.

---

## What would reopen this

**16 moving behind another milestone that needs arrivals to be autonomous.** Nothing does today. If a
later milestone needs the city to grow on its own before 16 lands, this decision is what has to be
revisited — and the answer then is to move 16, not to write a rule at 11.

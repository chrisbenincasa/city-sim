# 0129 — The Pool waits at the gate, and an arrival's Trip is the move-in

**Guiding concept: a journey described in prose can name an endpoint the mechanism has to compute.**

**Status:** accepted, 2026-08-20, with the user in the room. Closes
[`plans/0035`](../../plans/0035-hinterlands-and-arrival-through-the-gate.md) decision **2**.

---

## The decision

**A Household arrives at a specific Outside Connection as an entry event and joins the Unplaced Pool
*there*. The Trip happens later, when placement gives it a dwelling: gate → home.**

**The gate is a column on the Pool membership**, not on the Household.

---

## Why the order in `adr/0023` cannot be built as written

[`adr/0023`](0023-immigration-arrives-through-the-gate.md) says arrivals *"arrive as **Trips**
originating at an Outside Connection, **enter the Unplaced Pool**, and house themselves or leave."* Read
in order that is: Trip first, Pool second.

**`TripTable.Start` takes an origin Address and a destination Address** — Segment, offset and side at
both ends. A Household that has not been placed **has no destination**, and the Pool is what will
eventually give it one. So the Trip in that sentence has one end.

***A journey described in prose can name an endpoint the mechanism has to compute***, and the prose is
not wrong — it is describing the arrival from the outside, where "they came here" is one event. The
build has to say *where to*.

**Reordering costs nothing that ADR was buying.** Every property it lists survives:

| `adr/0023` wanted | Still true |
|---|---|
| *"Immigration becomes located"* | The Household arrives **at a named gate** and waits there |
| *"The ceiling stops being a config value"* | Throughput bounds **arrival**, at that gate, on entry |
| *"vehicles entering at a specific gate and contributing real congestion"* | The move-in Trip runs gate → dwelling on the real network |
| *"Departure becomes symmetric"* | A Household that gives up leaves **through a gate** |

---

## Why the gate is a Pool column and not a Household column

**It has to survive the wait.** The gate is known at arrival, because that is where throughput binds;
it is needed again at placement, as the move-in Trip's origin. The interval between them is exactly one
spell in the Pool. **So the Pool row is where it lives, and the placement is forced rather than chosen.**

**A lifetime column on the Household was considered and is wrong.** `CONTEXT.md` → Unplaced Pool gives
the Pool **four** entry routes, and two of them have no gate at all: a Household the city generated
itself when a Mature Family's children left home, and a Household evicted when its Building was
demolished. A column that is meaningless for half its rows is a column describing something else.

⚠ **`adr/0023`'s *"People leave the way they came, through a gate, as a Trip"* needs no amendment.** It
says people enter through a gate and leave through a gate. It does not say the same gate, and a
departing Household is leaving **for a Hinterland**, so the gate it leaves by follows from where it is
going. ***Reading a symmetry of shape as a symmetry of identity invents an obligation the record never
stated*** — and the invented obligation was about to buy a saved column for life.

---

## Consequences

- **`UnplacedTable` gains its second column.** Its doc-comment records that it is *"minimal in
  `adr/0054`'s sense — one column and no opinions"*; this is the first opinion, and it is the arrival
  mechanism's rather than the Pool's.
- **An evicted or self-generated Household waits *in the city*** and the column says so. It is not a
  gate handle with a sentinel; it is *where this Household is waiting*.
- **Placement is unchanged.** It draws from the Pool by position and is blind; nothing about it needs to
  know where a member is waiting.
- **The move-in Trip is the first Trip in the build whose origin is not a Building the traveller
  belongs to.**

## What would reopen this

**A second thing needing the gate after housing.** If a housed Household ever has to know which gate it
entered by — for a readout, for a statistic — the column moves to the Household and this record is why
it did not start there.

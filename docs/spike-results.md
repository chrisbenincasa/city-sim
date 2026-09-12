# Measurement evidence

The original spike report is preserved at [revision be07abc](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/docs/spike-results.md).
The links below retain whole measurement sections, including the machine, world, qualifications,
corrections and conclusions. These are historical readings, not present performance guarantees.
For offline access: `git show be07abc3b90a8102cc77ac131fd8852fbf853596:docs/spike-results.md`.

Current Tick-cost work is in [0013](../plans/0013-tick-budget.md); aged-city and worker measurements
are in [0067](../plans/0067-aged-city-performance.md). Follow their conditions before using a number.
New measurements belong beside the work that needs them, with the command and raw artifact path;
do not copy their narrative into this index. Unfinished work belongs in [the backlog](../plans/0000-board.md).

| Experiment | Evidence |
|---|---|
| S4 — the kernel benchmark | [Capture, conditions and interpretation](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/docs/spike-results.md#L24) |
| S4 task 2 — the row schema and the target row counts | [Capture, conditions and interpretation](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/docs/spike-results.md#L351) |
| K0 — the world's actual footprint | [Capture, conditions and interpretation](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/docs/spike-results.md#L612) |
| K1 — linear scan and update, `checked` and `unchecked` | [Capture, conditions and interpretation](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/docs/spike-results.md#L713) |
| K2 — random gather by generational handle | [Capture, conditions and interpretation](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/docs/spike-results.md#L819) |
| K3 — bulk copy of the K0 footprint | [Capture, conditions and interpretation](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/docs/spike-results.md#L926) |
| K4 — many lookups into small sorted arrays | [Capture, conditions and interpretation](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/docs/spike-results.md#L1057) |
| K5 — wheel bucket drain and reschedule | [Capture, conditions and interpretation](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/docs/spike-results.md#L1177) |
| K6 — the GC tail | [Capture, conditions and interpretation](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/docs/spike-results.md#L1340) |
| S2 — the routing ceiling | [Capture, conditions and interpretation](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/docs/spike-results.md#L1637) |
| S2 R1 — the travel-time matrix | [Capture, conditions and interpretation](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/docs/spike-results.md#L2033) |
| S2 R2 — the path source, the crossover, and the attribution lag | [Capture, conditions and interpretation](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/docs/spike-results.md#L2479) |
| S2 R3 — HPA\*, the cluster it owns, and the reduction that decides it | [Capture, conditions and interpretation](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/docs/spike-results.md#L2720) |
| S2 R4 — distance-vector, and the table that has to stay current | [Capture, conditions and interpretation](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/docs/spike-results.md#L3136) |
| S2 R5 — the edit storm, the gesture, and the Epoch that has to carry a location | [Capture, conditions and interpretation](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/docs/spike-results.md#L3398) |
| S2 R8 — the congestion loop, and the finding that outranks it | [Capture, conditions and interpretation](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/docs/spike-results.md#L3974) |
| S2 R6 — the two caches | [Capture, conditions and interpretation](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/docs/spike-results.md#L4366) |
| S2 R6.4 — what a per-Citizen Habit Route costs, and the column the trilemma had no cell for | [Capture, conditions and interpretation](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/docs/spike-results.md#L4864) |
| S2 R5.6 — the Parking Shed, and the rung it disagrees with | [Capture, conditions and interpretation](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/docs/spike-results.md#L5223) |
| S2 R7 — the report: the re-capture, the conclusion that moved, and the tripwire scored | [Capture, conditions and interpretation](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/docs/spike-results.md#L5338) |
| S0a — the world at target size | [Capture, conditions and interpretation](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/docs/spike-results.md#L6334) |
| S0b — the Tick with work in it | [Capture, conditions and interpretation](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/docs/spike-results.md#L6459) |
| S5 — the Lane kernel | [Capture, conditions and interpretation](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/docs/spike-results.md#L6647) |

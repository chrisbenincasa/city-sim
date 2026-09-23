# Starter prompt — after the reboot

Paste the block below into a fresh session.

```text
We are resuming after a reboot. Two jobs, in order.

1. Merge the outstanding PRs into main.
   - Open PRs at hand-off (2026-09-23), all MERGEABLE on GitHub:
     #16 private-production-and-labour (46 files, includes re-recorded goldens)
     #17 board-sweep (docs and plans/0000-board.md)
     #18 ci-full-suite (tests only)
   - Re-check `gh pr list` first; the list may have changed.
   - Merge #18, then #17, then #16. #16 and #17 both edit plans/0000-board.md,
     so expect to resolve the board by hand after the first of them lands.
   - Run `scripts/test.sh` on main after each merge. Stop and report on a failure.
   - Two branches hold commits not on main and have no open PR. Ask me before
     touching either: `worktree-row-31-attracts-people` (1 commit, formatter check)
     and `catchment-fold` in ../city-sim-terrain (4 commits, no upstream).
   - Preserve every worktree. Do not delete branches without asking.

2. Start the procedural Building generator.
   - Read research/procedural-buildings/SESSION.md (the ten decisions),
     REPORT.md (the research) and
     docs/adr/0173-a-building-is-drawn-realistically-at-every-distance-from-its-own-facts.md.
   - The board row "Procedural Building generator" owns the work. Its first step
     is the four measurements in REPORT.md §3, on this machine (i5-10400,
     GTX 1080 8 GB). Confirm `nvidia-smi` works before measuring; it failed
     before the reboot with a driver/library version mismatch.
   - Record machine, world, thread count and capture conditions with each number.
   - Two sibling rows are ready too: "Camera never enters a Building" (shell only)
     and "Building construction phase" (simulation, changes the State Hash).
```

# Working in city-sim

Start with Git status, recent local/remote commits and `git worktree list`, then
[the backlog](plans/0000-board.md) and the selected item's plan. Read [PROCESS.md](PROCESS.md)
for the workflow. The amnesty and separate status/question/audit ledgers are retired. Process
instructions there supersede older record-keeping requirements; do not restore them from an ADR.
Preserve other worktrees and uncommitted changes. Do not start work already owned by another tree.

## The project and where to look

Borough is a city-builder with individual Citizens, moving Goods and explainable consequences.
Godot hosts the shell; the C# simulation is engine-independent.

| Path | Purpose |
|---|---|
| `CONTEXT.md` | Domain vocabulary; use its terms and avoid its explicitly rejected concepts |
| `PROCESS.md` | How to choose, document and finish work |
| `plans/0000-board.md` | The single backlog: active work, prerequisites and deferred follow-ups |
| `docs/00-vision.md`, `docs/01-player-experience.md` | Intent and player interactions |
| `docs/02-simulation-model.md`, `docs/03-agent-architecture.md`, `docs/04-economy-and-goods.md` | Simulation, movement and economy design |
| `docs/05-technical-architecture.md` | Technical boundaries and invariants |
| `docs/06-roadmap.md` | Longer-term capabilities; not a second work queue |
| `docs/07-the-drawing.md` | Appearance and asset authoring |
| `docs/adr/` | Existing decisions; consult the ones relevant to the change |
| `docs/deferred.md` | Deliberate deferrals and their revisit conditions |
| `plans/0013-tick-budget.md`, `docs/spike-results.md` | Measurement evidence, with its conditions |
| `docs/dev-environment.md` | Machine setup |

Use code and tests to establish current behaviour. A description of missing code can be stale.
Update durable design text in place; put completed work in commits and PRs. Comments retain
contracts, units, invariants and non-obvious reasons, not session narratives. Documentation-only
changes are legitimate. No ratio, ADR quota or central number-ratification ledger gates them.

## Building assets and watching the shell

Author new Building models and visual variants in Blender first, including debugging models.
Follow [the authoring procedure](docs/07-the-drawing.md#building-authoring-procedure).
Godot imports and assembles the assets; simulation geometry owns footprint, floor-area and access
constraints. Use the drive skill when changing the shell or anything it draws, and watch the result.

## Architecture invariants

- `Borough.Core` has no Godot dependency, transitively. The headless build requires neither Godot nor a GPU.
- Simulation state and arithmetic use integers and Q16.16, never `float`/`double` or `Math.*`.
  Use `Borough.Core.Arithmetic` for division and shifts; do not bypass the analysers.
- Do not enumerate `Dictionary` or `HashSet` in simulation code. Do not use `System.Random`, wall
  clocks, GUID generation or unstable object hashes. Randomness is a counter hash of world seed,
  entity id, Tick and a distinct `purpose_tag` for each use.
- Simulation state is unmanaged unless explicitly marked `[ColdPath("reason")]`. Hot paths hold
  no references. Variable-length per-entity collections use intrusive index lists in flat arrays.
- Each table field is allocated through `Rows.Saved`, `Rows.Derived` or `Rows.SavedHandle`.
  Saved state is hashed; derived state must actually be rebuilt. Handles hash the target's
  monotonic id, not its recyclable slot. Hash order is declaration order, then array index order.
- Preserve replay, save/reload and thread-count equivalence. Do not accumulate shared state from
  parallel loops. Register invariants at the appropriate frequency, not behind build flags.
- Every collection needs a sink and quantities must remain bounded in steady state.
- Designer-facing tuning belongs in the TOML Ruleset, not simulation constants. Fixture sizing is
  ordinary fixture code. A State Hash change is a behaviour change and needs deliberate golden
  re-recording; hash movement alone is no reason to defer useful work.
- Core returns ids and numbers; the shell owns human-readable strings. Input Logs are encoded by
  `Borough.Formats`, never independently parsed by either shell.
- Keep individual Citizen decisions. Do not introduce an RCI meter, Cohorts, an ECS or camera-driven
  simulation fidelity. Bin Rules and Sweep Rules have different behaviour; switching families is
  not a performance-only change.

The build-time analysers enforce numerical, collection and state restrictions. New diagnostics
need a test that demonstrates the violation. `DerivedRebuildAuditTests` checks rebuild coverage;
`FactorioTests`, replay/golden tests and `RouteWorkerTests` cover the corresponding equivalences.

## Build and test

```sh
dotnet build
scripts/test.sh                 # working lane and pre-commit gate
scripts/test.sh Policy          # focused iteration
scripts/test.sh --filter 'EXPR' # explicit filter
scripts/test.sh --all           # milestone/full suite, including instruments
dotnet run --project src/Borough.Headless -- --help
```

Read the log printed by `scripts/test.sh`; do not repeat a run just to recover its results.
The default lane is `tier!=instrument`: an unannotated test is an assertion. Instruments opt out
with `[Trait(Tier.Key, Tier.Instrument)]`. Keep the assertion budget enforced by `TierBudgetTests`.
`Simulation.VerifyDecideWritesNothing` is opt-in for tests; headless runs default it on.

At a milestone run the full suite, invariants and relevant long-run checks. A visible capability
also needs a driven demonstration showing its trigger, response and consequence. Record what
observation exposed in its plan or PR. CI supplies failure reports; performance claims require
Release on a named machine, world, thread count and capture conditions. A busy run can verify
behaviour but cannot establish a quiet-machine timing.

## Rulesets and generated references

Read each Ruleset's header: demonstrations are fixtures, not balanced cities. `minimal.toml`
explains shared keys; other files explain their variations. Before editing a golden fixture,
read [the re-record procedure](tests/Borough.Tests/Golden/README.md); comments also affect file
hashes. Use that procedure rather than copying hash values by hand.

`docs/ruleset-reference.md` is generated. Author key descriptions in
`src/Borough.Formats/RulesetKeyNotes.cs`; regenerate with headless `--key-reference`.
Regenerate the schema with `--schema` and check TOML with `npx @taplo/cli lint 'rulesets/*.toml'`
when its contract changes. `Options.Usage` owns the full list of runner modes and flags.

# Agent Rules <!-- tessl-managed -->

@.tessl/RULES.md follow the [instructions](.tessl/RULES.md)

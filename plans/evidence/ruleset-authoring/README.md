# Ruleset authoring maintenance experiment

[Plan 0076](../../0076-ruleset-authoring-experiment.md) owns the experiment and failure conditions.
This is a disposable prototype. Its synthetic catalogue is deliberately repetitive and unbalanced.
The user completed the initial handoff and reported generally smooth editing, with omitted
overrides unexpectedly rejected. Omission now means inheritance; the user's edits are preserved.

## Use the prototype

Edit **catalogue.toml**. Keep **baseline.toml** unchanged for comparison. The one-time fixture
script refuses to overwrite either file; do not regenerate the source to perform a task.

```sh
python3 plans/evidence/ruleset-authoring/author.py \
  plans/evidence/ruleset-authoring/catalogue.toml \
  --against plans/evidence/ruleset-authoring/baseline.toml
```

Open `generated/preview/report.md`. It lists authored edits, effective values before/after,
inherited values and exceptions, affected kinds, retired identities and expanded counts.
`generated/preview/resolved.json` contains every consumer definition, including unchanged ones.
Generated files are disposable; do not edit them. Reports describe static expectations and do
not modify or migrate a saved city. The preview is intentionally plain text before any editor work.

The source has five sections:

| Section | Meaning |
|---|---|
| `goods` | Stable id, display label and price |
| `baskets` | Good consumption in units per Day; kinds refer to one basket |
| `variants` | Shared storage in Days of that basket's consumption |
| `kinds` | Stable id, label, basket reference and optional storage Days keyed by variant |
| `recipes` | Actual input/output Good amounts per firing; an empty input is an explicit fixture source |

Each kind gets all three storage variants in this experiment. Storage precedence is explicit:
a kind's override for a variant replaces that variant's shared Days; removing the override
restores inheritance. Omitting `overrides` entirely is equivalent to `overrides = {}`.
There is no inheritance chain. A separate basket expresses a different
consumption pattern; per-Good kind overrides are not implemented.

Ids become runtime names and must remain stable across saves. Labels appear in authoring data
and reports only; the production shell is not integrated with these labels. Changing a label
leaves runtime bytes unchanged. Changing an id is removal plus addition, not an alias.

`scenario.toml` carries the fixed research world settings. The compiler supplies fixed 256-Tick
consumption, 8-Tick production/replenishment, wage/shift and producer storage conventions.
Consumption must be divisible by eight; the compiler refuses unrepresentable rates rather than
rounding. Storage capacities derive exactly from daily use × Days. These fixed conventions are
limits of the experiment, not proposed universal game defaults. Pool inputs use Core's paid local-market purchase; ShoppingEngine is disabled here; production is not labour-gated. Solvency and gameplay balance are not measured here.

## Your handoff tasks

Try these from the written requirements before reading the agent's exercise implementation.
Use separate copies of `catalogue.toml` if you want independent comparisons; pass their paths
in the command above. There is no time limit. Record where the representation or report leaves
you uncertain; ask rather than editing generated output to make something work.

1. Increase Food use in the basic basket by 50%. Keep every home's Days of storage intact,
   including exceptions. Explain which homes changed and which retained their previous use.
2. Make the shared reserve variant hold ten Days. Preserve deliberate kind-specific reserve
   choices. Explain why some reserve homes do not change.
3. Remove home_00's special reserve storage choice so it inherits the shared reserve setting.
   Explain the resulting capacity and whether you expect any sibling variant to change.

The observation is about your ability to make and explain these edits. The agent should not
silently perform them in `catalogue.toml`. The working catalogue contains the user's completed combined edits; `baseline.toml` retains the original.

## Agent reproduction and real loader

```sh
python3 plans/evidence/ruleset-authoring/exercise.py
dotnet build plans/evidence/ruleset-viability/Viability.csproj --no-restore -m:1 -nr:false
dotnet plans/evidence/ruleset-viability/bin/Debug/net10.0/Viability.dll \
  plans/evidence/ruleset-authoring --authoring
# Validate your compiled output separately:
dotnet plans/evidence/ruleset-viability/bin/Debug/net10.0/Viability.dll \
  plans/evidence/ruleset-authoring/generated/preview/ruleset.toml --validate
```

The exercise creates task variants and checks locality, preserved exceptions, declaration-order
independence and author-error diagnostics. It never changes the user's catalogue. `checks.json`
retains the results; `generated/*.md` explains each controlled change.

The C# probe loads every task output with Formats, seeds an inhabited city, saves through the
actual CitySave implementation and tries changes on disposable copies. `persistence.txt` records
actual reload degradation and save/256-Tick continuation checks, separately from static previews.
Packages live in `/tmp/borough-authoring-experiment`; this command does not upgrade a user's save.
The short horizon checks persistence, not balance. Fixture setup uses World APIs to move three
Households into a storage exception, an inherited reserve kind and the other basket, and to
create the eight producers. This is constructed test state, not a replay of player founding.
It exercises those 12 standing kinds, not every catalogue kind or every stock owner. Runtime hash agreement is not proof
that a Good rename/removal preserves stock semantics. Those transitions remain unsupported here.

## Scaling finding

With the same two baskets and eight recipes, 20, 40 and 80 authored consumer kinds expand to
68, 128 and 248 runtime kinds and 368, 728 and 1,448 Rules. At 83 consumer kinds the product
is 257 runtime kinds; the existing loader allows 254. The preview exposes this budget and the
CLI exits unsuccessfully if it is exceeded. The retained native-loader probe also checks the refusal.

This is an observed failure of the current variant-to-kind expansion at that size. Human editing
can still be evaluated in the handoff, but this compiler is not a scalable production model yet.
Revising variant representation or runtime factoring needs a separate decision; silently widening
an id would not establish that all expansion costs are acceptable.

## Factoring and richer behaviour

[The follow-up findings](scaling-and-bakery.md) answer the two questions raised after the handoff.
`factoring.py` checks a proposed factored representation against flat resolved definitions;
`bakery.toml` and `bakery.py` state/exercise a finite cross-mechanism contract. Neither is a
production runtime adapter. `BakeryProbe.cs` separately checks the current Core boundary.

```sh
python3 plans/evidence/ruleset-authoring/factoring.py
python3 plans/evidence/ruleset-authoring/bakery.py
python3 plans/evidence/ruleset-authoring/bakery-core-input.py
dotnet build plans/evidence/ruleset-viability/Viability.csproj --no-restore -m:1 -nr:false
dotnet plans/evidence/ruleset-viability/bin/Debug/net10.0/Viability.dll \
  plans/evidence/ruleset-authoring --bakery
```

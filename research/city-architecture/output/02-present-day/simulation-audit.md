# Current repository check and fixed game sample

**The new architectural direction does not change the measured game sample.** `audit.py` independently reconstructs the archived `d9addc3` draw, checks its repeated capture byte-for-byte, and records checkpoint/working-file hashes. It produces [52 rows](evidence/game-sample.csv) and a [manifest](evidence/reproduction.json). This is a fresh calculation on an archived capture, **not a fresh game run**.

Run from the repository root:

```sh
python3 research/city-architecture/output/02-present-day/audit.py
```

HEAD checked: `7916db4022066a741531087aa1ed6325a448303f`. Checkpoint: `d9addc3e04d600f58990b2a8036d03cf852445b7`. Draw SHA-256: `918824632344f4cadf63bab85017be95b15b26f3f128d142511e45705f21aa32`. Fixture remains shopping, Tick 600,400 Citizens. These are demonstration conditions, not citywide distributions. The [first audit](../simulation-audit.md) retains full camera, mesh and owner interpretation.

| ID | East–west × south–north m | Wall m / floors | Counted floor m² | Reconstructed tenancy ceiling | Jobs per premised Business |
|---|---:|---:|---:|---:|---:|
| G001 | 36 × 20 | 7 / 2 | 1,440 | 3 | 10 |
| G002 | 32 × 24 | 10.5 / 3 | 2,304 | 5 | 9 |
| G003 | 24 × 16 | 10.5 / 3 | 1,152 | 2 | 12 |
| G004 | 52 × 24 | 7 / 2 | 2,496 | 6 | 8 |
| G008 | 28 × 60 | 7 / 2 | 3,360 | 8 | 8 |

Selection stays first four draw indices plus first maximum-area component. All 52 body rows have distinct Building IDs in this sample. Wings/podiums elsewhere can share IDs; never generalise the one-body relationship.

`LotTable`/`BuildingPlan` own ground and floor; `Main.Massing` scales walls and chooses roofs; `RoofMeshes` owns roof topology. `World.TryDeclaredOccupancy`, `TryDeclaredJobs` and `CapacityRuleset.Holds` interpret content rates. One Tile is 4 m and one square Tile 16 m². In this fixture 25floorTiles/tenancy means 400 m² counted floor per tenancy; threefloorTiles/job means 48 m² within the Business share. Capacities are not observations of population or net dwelling area.

Fresh comparison confirms `RoofMeshes`, `BuildingPlan` and `shopping.toml` are byte-identical to the checkpoint. `Main.Massing` changes only Health-overlay colouring. `World.cs` and `Ruleset.cs` changed; the manifest does not pretend otherwise. Inspection of the named capacity functions confirms the rates used for this fixture reconstruction; changes elsewhere prevent claiming a current-run inventory match.

## Architectural comparison

- **US03:** its front/rear plan pieces total a stepped envelope. The overall including-stoop schedule cannot be treated as a solid floor plate. The new scale sheet draws the labelled body rather than its largest box.
- **NL01:** the 5.1 m beukmaat and 9.4 m depth are a structural/module control, not an externally surveyed footprint. It is displayed with a dashed boundary. Its GBO cannot be compared directly to game counted floor as though both were gross area.
- **DE04:** approximately 13 m is a clear structural span, not the whole building's outside width. Its purpose is to explain construction choice, not justify copying dimensions onto G003.
- **N3:** retains G003's exact wall body and counted floor but tests twelve architectural dwellings. That differs from the two-tenancy content ceiling. More real-world references do not authorize quietly changing that mapping.

## Responsibility split

The same five game bodies should remain in all regional comparison sets. Candidate interpretations below are **proposed tests**, not assertions that a source building exactly fits. These deliberately overlap between regions: use and access constrain the family more strongly than nationality.

| Fixed game body | US-informed candidate | Dutch/German-informed candidate | Fictional candidate / critical reservation |
|---|---|---|---|
| G001,36 × 20,two floors | Small commercial block with offices above | Commercial/workplace block | Same two-floor body; front entrances and rear delivery zone, supported floor and planned core |
| G002,32 × 24,three floors | Deep mixed commercial/office building | Mixed workplace building | Residential use remains conditional on daylight/access planning; no court carved from counted floor |
| G003,24 × 16,three floors | Small block apartments | Stair/corridor-access apartments | N3 controlled comparison; same body and proposed circulation, content mismatch retained |
| G004,52 × 24,two floors | Workshop/storage with full upper floor | Light-production/workplace range | A clear single-storey portal hall is not a substitute for two counted floors; frame and floor loads unresolved |
| G008,28 × 60,two floors | Larger commercial/production building | Larger commercial/production building | Repeated structural bays and multiple service points; no enormous domestic roof |

The atlas does not dimensionally validate every candidate. Only N3 is developed into an exact-body replacement brief. The others define bounded questions for a later nonresidential pass, rather than falsely treating all five bodies as residential models.

| Appearance/model work | Simulation/content question | Evidence/preference still open |
|---|---|---|
| Whole roof assemblies, drains, parapets, human-scale doors, planned bays, readable shared stairs | Whether architectural dwellings correspond to tenancies/Buildings; kind/use allocation | Perceptual benefit at actual camera distances |
| Contemporary palette and construction modules; selective retrofit skins | Covered ground for attachments, balconies, courts, parking and side paths | Climate package; regional mixture judged in motion |
| Regional variants on the same valid footprint and access topology | Different setbacks, plot subdivision and changes to footprint/floor area | Empirical age/family frequencies, not established by selected plans |
| Stable condition variants | Which occupied openings may reflect simulated use | No inferred occupancy from windows |

Do not carve courts out of solid simulated ground, shrink walls away from allocated footprint, or draw additional occupied homes to make capacity appear plausible. Separate model-family selection from regional treatment. N1 and N2 are new proposed research grounds; only N3 is an exact-body substitution for the selected sample.

No Godot, Debug build or gameplay tests were required or run for these read-only calculations. Production visual verification remains follow-on work under the drive skill.

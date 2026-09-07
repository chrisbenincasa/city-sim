# Archived reconstruction and current owners

**Fresh calculation of archived evidence; no fresh simulation run.** Run `python3 research/city-architecture/output/03-context-and-construction/audit.py` from the repository root. The script writes only this pass's sample and manifest. Previous passes are preserved.

Checkpoint `d9addc3e04d600f58990b2a8036d03cf852445b7`; current HEAD checked `7916db4022066a741531087aa1ed6325a448303f`. The archived near and near-repeat TSV captures are byte-identical. Their SHA-256 is `918824632344f4cadf63bab85017be95b15b26f3f128d142511e45705f21aa32`. Fixture: shopping, Tick 600, 400 Citizens. This capture contains 52 building components with 52 distinct Building IDs; components need not be unique Buildings in other fixtures.

The [manifest](evidence/reproduction.json) records checkpoint/current hashes. `RoofMeshes.cs`, `BuildingPlan.cs` and `shopping.toml` match the checkpoint. `Main.Massing.cs` differs only in Health-overlay colouring. `World.cs` and `Ruleset.cs` have changed; their present capacity functions were read directly. This establishes the selected reconstruction's arithmetic and relevant owners, not equality of a new current city inventory.

| Sample | Width×depth m | Floors / walls m | Counted floor m² | Reconstructed tenancy ceiling | Jobs per premised Business |
|---|---:|---:|---:|---:|---:|
| G001 | 36×20 | 2 / 7 | 1,440 | 3 | 10 |
| G002 | 32×24 | 3 / 10.5 | 2,304 | 5 | 9 |
| G003 | 24×16 | 3 / 10.5 | 1,152 | 2 | 12 |
| G004 | 52×24 | 2 / 7 | 2,496 | 6 | 8 |
| G008 | 28×60 | 2 / 7 | 3,360 | 8 | 8 |

Selection remains first four draw indices and first maximum-area component. [All rows](evidence/game-sample.csv) retain actual Building IDs and draw indices. G labels are research identifiers, not simulation row IDs.

## Arithmetic and meaning

One Tile is 4 m; one square Tile is 16 m². In this fixture the rate is 25 floor Tiles per tenancy and 3 floor Tiles per job. For G003, 1,152/16=72 floor Tiles; floor(72/25)=2 tenancies. Per Business, floor(floor(72/2)/3)=12 jobs. These are ceilings, not observed inhabitants, room counts or employee attendance.

`World.TryDeclaredOccupancy` permits tenancies when the kind houses people or provides premises. A Business can claim a tenancy. Thus even “two tenancies” is not a promise of two architectural homes. `TryDeclaredJobs` divides a Business's share of floor, not the whole building's window count. `CapacityRuleset.Holds` supplies minimum/zero handling; reconstruction applies it only to these positive floors/rates.

| Owner | Responsibility verified in source | Architectural consequence |
|---|---|---|
| `LotTable` / `BuildingPlan.FloorTiles` | Ground, storeys, hollow/podium forms and counted floor | Model must follow actual shape, not only bounding box |
| `BuildingPlan.Hollow` | Court eligibility and hole dimensions; shared by floor and shell paths | Existing courtyard support is real, but cannot be invoked for arbitrary solid samples |
| `Main.Massing` | Metres, 3.5 m wall height per storey, appearance and roofs | Housing comparison's 3.2 m floor height is a separate proposal; not silently an exact game substitute |
| `RoofMeshes` | Roof topology | Needs source-constrained assembly interpretation, not capacity ownership |
| `World.TryDeclaredOccupancy` / `TryDeclaredJobs` | Floor-rate capacities | Renderer must not create additional simulated dwellings/jobs |
| `shopping.toml` `[capacity]` | Fixture content rates | Not a real-world area standard; unchanged |

## Selected-body interpretation, without hiding conflicts

| Body | Plausible proposed architecture / appearance change | Content, ground or capacity issue | Missing evidence |
|---|---|---|---|
| G001 | Two-floor commercial/workplace building; full supported upper floor, two cores, receiving and parapet roof; detailed W1 plate | 3 shared tenancies are not three equal businesses by architectural proof; surrounding service yard separately allocated | Engineering floor loads, occupant/egress plan, actual site |
| G002 | Deep office/service building with perimeter occupied rooms and central support space | Three full floors remain; residential conversion needs daylight/circulation solution, not an uncounted court | Documented ordinary deep-plan example, service uses |
| G003 | Apartment-scale envelope with shared circulation; two very large tenancies possible as an interpretation | Twelve-flat A1 is a content experiment, not a legal appearance replacement; retain 10.5 m game walls if testing exact body | Tenancy-to-home policy; actual dwelling distribution |
| G004 | Two-floor light production or commercial range, multiple supported bays | A portal hall plus partial mezzanine loses counted floor; reclassification cannot be assumed | Full-floor structural/circulation plan |
| G008 | Long two-floor workplace with repeated bays and multiple service thresholds | Do not turn it into many houses from facade rhythm; length does not grant extra tenancies | Fire compartments, travel distances, site servicing |

W1 is the exact-body drawing. Other rows remain bounded interpretations, not completed architectural validation. Residential A1 and A2 compare equal **study gross envelopes**, not measured usable area and not certified game-capacity matches.

## Responsibility split

Appearance: wall/roof construction cues, material scale, gutters, non-occupied facade articulation, repairs and compatible shading.

Content/simulation: footprint subdivision, covered ground, real courts, terraces/undercrofts, use allocation, tenancies versus homes, capacity rates, parking, service circulation ownership and changes in floor area.

Research/preference: desirable family mix, climate, level of detail, recognition in motion, measured local plan evidence. No gameplay, Ruleset, ADR, corpus-check or production-asset changes were made. Godot was not launched; no Debug build was required. Simulation tests were not run for read-only reconstruction.

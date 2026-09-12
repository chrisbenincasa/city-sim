# Backlog

This is the only maintained list of unfinished work. [PROCESS.md](../PROCESS.md) defines the workflow.
Check Git and worktrees before choosing an item. Remove a completed entry; its commit or PR keeps
history. This includes gameplay and all work needed to develop, present, test and deliver the game.
Scoping and design are work. Entries below capture candidates without promising every candidate
will ship; **scoping**, **design**, **ready**, **active** and **deferred** have the meanings in PROCESS.
Unclaimed entries are available for their stated next step. Grouped entries split when their parts
can be scheduled independently. Plans hold implementation decisions and acceptance checks, not a second priority queue.

## Active and next

| Work | Owner / state | Next step and prerequisite |
|---|---|---|
| **31 — The city attracts people** | `worktree-row-31-attracts-people`; [0073](0073-the-city-attracts-people.md) | Continue the prospect, admission, Outside population account and autonomous flow in that worktree. Its plan owns the remaining tasks; do not infer progress from an old task count. Include choice stickiness from [0068 D5](0068-the-choice-model.md#decisions). |
| **32 — The city spends** | `worktree-row-32-city-spends`; [0070](0070-the-city-spends.md) | Continue opening treasury funds, service placement and running costs, funding controls and school closure. Coordinate shared Money and admission code with row 31. |
| **30 — Diagnose decline, intervene and observe recovery** | Available for scoping; [0064](0064-the-information-interface.md) and [0047](0047-decline-demolish-and-cleared-land.md) | Write its short plan. Preserve sustained Trip-failure and below-tolerance scope; connect a cause, consequence, Evidence, player action and recovery or persistent failure. Check expanded dependencies before implementation; milestone 17's inherited gate was clear. |

Row 30 should reuse the existing supply diagnosis interface. Failure durations must measure elapsed
failure, not event tallies, and must not borrow the Building condemnation window. Include at least
one additional failure source with an intervention; a counter or explanation label alone is insufficient.
Check deterministic hover aiming in the current drive channel before its observation work.

## Scope and design — city systems

These entries bring the roadmap and every candidate in [0074](0074-the-systems-nobody-named.md)
onto the board. Check current code before treating a definition's attachment or absence claim as
current. Existing active rows keep their scope; related candidates do not silently expand them.

| Work | Owner / state | Next step and prerequisite |
|---|---|---|
| Development, land ownership and prices | Unclaimed / scoping; [roadmap](../docs/06-roadmap.md), [land banking](0074-the-systems-nobody-named.md#land-banking) | Establish private capital, construction Materials, pricing and placement behaviour; decide whether a landowner exists before pricing private construction or withholding land. |
| Office and agglomeration | Unclaimed / scoping; [roadmap](../docs/06-roadmap.md) | Define the export, staffing and location consequences and a playable acceptance world. |
| Freight and Outside trade | Unclaimed / scoping; [roadmap](../docs/06-roadmap.md) | Trace actual Shipments, Vehicles, payment and gate capacity; scope the missing transport loop. Coordinate gate changes with row 31. |
| Driver response and detailed traffic | Unclaimed / design; [roadmap](../docs/06-roadmap.md) | Scope Habit, Sight, Temperament, diversion and queues; then define promotion/demotion and audit tolerances. Detailed traffic is a prerequisite for implementing fidelity transfer. |
| Mode choice, Transit and cycling | Unclaimed / design; [cycling](0074-the-systems-nobody-named.md#cycling) | Establish existing travel modes and decide how an individual chooses a mode; scope Transit and cycling against that decision. |
| Road pricing | Unclaimed / design; [0074](0074-the-systems-nobody-named.md#road-pricing) | Decide charge timing, payer and routing response; demonstrate who pays and how traffic changes. |
| Terrain, extraction, hazards and Terraform | `catchment-fold` tree exists / scoping; [roadmap](../docs/06-roadmap.md) | Establish that tree's remaining scope and ownership before starting overlapping terrain work. Separate static ground, extraction and regeneration, Terraform costs, and Shocks/Intensity Dial outcomes. |
| Health, dispatch and Service variants | Unclaimed / scoping; [roadmap](../docs/06-roadmap.md) | Check routine care and inpatient behaviour, then define Incident response and what successful containment changes. |
| Childcare | Unclaimed / design; [0074](0074-the-systems-nobody-named.md#childcare) | Decide the consequence of unavailable childcare for adult labour and the parent drop-off Trip. |
| Elder care | Unclaimed / design; [0074](0074-the-systems-nobody-named.md#elder-care) | Decide care at home versus relocation, then connect attendance, housing and labour consequences. |
| Death care | Unclaimed / design; [0074](0074-the-systems-nobody-named.md#death-care) | Decide dissolution's Service and land consequence, including a bounded cemetery stock and its sink. |
| Segregation and policy incidence | Unclaimed / design; [0074](0074-the-systems-nobody-named.md#reading-the-city) | Choose what sorting the player can inspect and whether public opinion needs a response mechanism or an incidence readout. Preserve individual explanations. |
| Homelessness on the map | Unclaimed / design; [0074](0074-the-systems-nobody-named.md#homelessness-as-a-place) | Decide whether unhoused Households have locations and what the player can inspect or do there; coordinate with row 31's Pool. |
| Local Waste disposal | Unclaimed / design; [0074](0074-the-systems-nobody-named.md#waste-disposal-on-the-map) | Scope landfill/incineration, finite capacity, exhaustion or reclamation, emissions and hauling consequences. |
| Utility scope: district heating and telecommunications | Unclaimed / design; [0074](0074-the-systems-nobody-named.md#district-heating) | Test whether either creates a spatial choice; decide the Utility abstraction's boundary and admit, defer or decline each candidate. |
| Libraries, culture and evening destinations | Unclaimed / design; [0074](0074-the-systems-nobody-named.md#destinations) | Define reasons and hours for Trips and Amenity effects. Separate local museums from visitor-dependent content. |
| Tourism and hotels | Unclaimed / design; [0074](0074-the-systems-nobody-named.md#tourism) | Decide visitor identity, admission, spending, stays and departure before hotel occupancy. Coordinate with row 31; tourism design need not wait for its completion. |
| Whether closing a service empties the city | Unclaimed / ready; [0070](0070-the-city-spends.md) finding F19 | Row 32's demonstration saw population fall from 2,211 to 1,778 over six Days while four schools folded, with only 23 funded posts city-wide to account for it. Run the same Ruleset, seed and 2,000 Citizens with the funding left in place and compare the Day-48 population. Do not write the causal claim into any document until that run exists. Coordinate with row 31, which owns departure. |

## Scope and design — making and delivering the game

These are first-class outcomes. A scoping entry identifies what to investigate, not a claim that
all listed facilities are missing. Select and prioritise them alongside city systems.

| Work | Owner / state | Next step and prerequisite |
|---|---|---|
| City-scale simulation performance | Unclaimed / scoping; [0013](0013-tick-budget.md), [0067](0067-aged-city-performance.md) | Choose representative worlds and target budgets; attribute staffing/care, purchases, routing and kernel costs before choosing optimisations. Preserve the original measurement conditions. |
| Snapshot, thread and table ownership | Unclaimed / scoping; [technical architecture](../docs/05-technical-architecture.md#6-threading-policy), [0067](0067-aged-city-performance.md) | Check live snapshot/marshalling and mutation boundaries; identify a concrete correctness or cost gap before extraction or concurrency changes. Parallel decisions, a scheduler and lock-free structures remain conditional on measured need. |
| Rendering at city scale | Unclaimed / scoping; [technical architecture](../docs/05-technical-architecture.md#10-rendering) | Measure frames, uploads, instance variation, spatial partition and zoom behaviour on a representative developed city; set acceptance conditions and scope bottlenecks. |
| Building kit and readable city drawing | Unclaimed / scoping; [drawing](../docs/07-the-drawing.md#6-what-the-picture-does-not-say-yet) | Inspect the current kit and shell; scope Building-kind identity, walkers/drivers, parking, Districts, regional overlays and night/street lighting against what the simulation knows. Author Building assets in Blender and observe changes through drive. |
| Information tools beyond decline | Unclaimed / scoping; [0064](0064-the-information-interface.md) | Inventory Trajectories, Pins, overlays and empty-Lot explanations; select observable outcomes beyond row 30 without duplicating its diagnosis scope. |
| Usability, accessibility and onboarding | Unclaimed / scoping; [player experience](../docs/01-player-experience.md) | Review the existing menu, settings and controls through a first-city session. Scope input rebinding, text/UI scaling, colour-independent readings, discovery and tutorial needs; define supported access and input targets. |
| Sound and music | Unclaimed / scoping; [player experience](../docs/01-player-experience.md) | Inventory existing audio; decide feedback, city ambience, music and volume controls, then define an authoring and playback slice. |
| Save compatibility and recovery | Unclaimed / scoping; [technical architecture](../docs/05-technical-architecture.md#7-save-format-and-migration) | Inspect CitySave and Formats; define supported version transitions, unknown Rulesets, corrupt/interrupted saves and recovery behaviour before choosing migrations or autosave changes. |
| Crash reproduction and diagnostics | Unclaimed / scoping; [technical architecture](../docs/05-technical-architecture.md#8-crash-forensics) | Check checkpoint/Input Log capture and failure handling; scope a replayable failure artifact and useful player-facing recovery. |
| Ruleset authoring, sharing and modding | Unclaimed / design; [0074](0074-the-systems-nobody-named.md#modding) | Define supported content changes, packaging/discovery, validation feedback and unknown-hash behaviour with save compatibility. Review existing TOML/schema/hot reload before extending them. |
| Development and asset iteration tools | Unclaimed / scoping; [developer setup](../docs/dev-environment.md), [drawing procedure](../docs/07-the-drawing.md#building-authoring-procedure) | Exercise clean setup, headless, drive, Blender import and asset review; select reproducible friction to remove and document the supported workflow. |
| Driven-run tooling reports success it did not have | Unclaimed / ready; [0070](0070-the-city-spends.md) findings F15 and F16 | Two silent failures found taking row 32's demonstration. `--reload-at` is accepted by every runner mode and consumed only by the Input Log builder, so a `--school` dump reads and hashes the second Ruleset, never switches to it and reports it anyway; refuse the combination where `--reload-at`'s other refusals live. `Main.Channels.cs`'s `Serve` returns out of both loops when a write finds the client gone, so one dead driver kills `--listen` for the rest of the run and later commands are accepted and dropped; continue the accept loop instead. |
| Two commands in one Tick can crash the shell | Unclaimed / design; [0070](0070-the-city-spends.md) finding F17 | The shell asks `Simulation.Refuses` once per click and drains the whole queue into one `Step`, so a command applied earlier in the batch can invalidate a later one's answer and `ApplyService` throws on the re-ask. Reproduced with two `service` clicks at one Tile, no money involved; the session stops mid-Tick and the Input Log already holds a command that cannot replay. Worst while paused, where nothing steps and the queue holds every click. Choose between the shell carrying a running cost across the batch, `ApplyInput` reporting rather than throwing, and sending at most one command a Tick. |
| Test reliability and CI feedback | Unclaimed / scoping; [test runner](../scripts/test.sh) | Review working/full lanes, artifact retention and post-submit failure ownership; reproduce relevant historical flakes before filing fixes. Set useful long-run coverage without restoring corpus-shape gates. |
| Packaging, platforms and release | Unclaimed / scoping; [technical architecture](../docs/05-technical-architecture.md) | Choose initial supported platforms and distribution target; inspect export/build automation and scope install, launch, update, versioning and asset licensing/credits checks for a distributable build. |
| Localisation readiness | Unclaimed / scoping; [player experience](../docs/01-player-experience.md) | Decide language scope; inventory shell strings, fonts and layouts, then scope translation and formatting support while keeping human-readable strings outside Core. |
| Playtesting and content calibration | Unclaimed / scoping; [player experience](../docs/01-player-experience.md), [Rulesets](../rulesets/minimal.toml) | Choose playable phases and non-fixture worlds, observe player choices and natural playing speeds, and tune values in their owning content. |
| Current design and developer documentation | Unclaimed / scoping; [technical architecture](../docs/05-technical-architecture.md), [earlier reports](#earlier-reports--verify-on-touch) | Select a document whose stale claims obstruct current work; verify against code and update in place. Include old deferred entries claiming already-built economics or schooling is absent; do not recreate a correction ledger. |

## Deferred — revisit on subsystem touch

| Work | Owner / trigger |
|---|---|
| Make a private education path observable in a viable world | Schooling content; [0071 F1](0071-education-changes-a-working-life.md). The demonstrated poverty path did not exercise private tuition; retain the unsuccessful path as evidence. |
| Pollution charges: inability to pay and inability to reduce emissions | Economy and emissions; [0072](0072-city-income.md). Consider partial collection and a real emissions response when extending the mechanism. |
| Household formation: pairing versus separate adults | Life Stages; [0046](0046-life-stages-and-a-self-generating-population.md). Revisit when a mechanism distinguishes one adult from two in a Household. |
| Whether beauty becomes an explicit vision pillar | Drawing direction; [07 §5](../docs/07-the-drawing.md#5-the-fifth-pillar-which-is-an-open-question-and-not-a-decision). Revisit when selecting further photographic/detail work; the existing camera and lighting do not decide it. |
| Ground markings that imply a simulation claim | Drawing; [0049](0049-visuals.md), findings F46–F52. Revisit when adding such markings. |
| What a founder loses in bankruptcy | Business founding; [0065](0065-business-insolvency.md). Revisit when founding is common enough to observe. |
| Keep routing and kernel spike artifacts while still needed | `spikes/S2.Routing/` and `spikes/S4.Kernels/`; [0010](0010-s2-routing.md), [0004](0004-s4-kernel-benchmark.md). Do not delete merely because a former gate passed. Check current users first. |

## Deferred — longer-term candidates

All entries here are unclaimed and deferred. [Deferred design notes](../docs/deferred.md) hold
the rationale and retrofit costs; the triggers below govern reconsideration. Verify old claims
when a trigger fires. The economics and schooling remnants there belong to the documentation
scoping entry above, not a new implementation commitment.

| Work | Revisit trigger |
|---|---|
| Gravity-fed sewage | Utility plant siting proves to be pure budgeting with no spatial choice. |
| Water depth, stratification, tides and directional flow | Wind advection is built (directional flow), or distinct Resources require stratification. Depth already represented by capacity is not new work. |
| Ground-dependent pollution absorption | Greenspace enters scope, or playtests show the global decay model hides meaningful differences in land use. |
| Pollution crossing the map edge | Upstream siting is systematically preferred or free pollution export becomes a visible loophole. |
| Crime types; prisons and courts | A playable Incident/response loop reveals distinct problems needing distinct consequences or detention. |
| Intersection diagnosis and traffic-management tools | Traffic-management intent changes or observed congestion needs turn fidelity; scope actionable tools separately from internal fidelity. |
| Archipelago worlds | Playtests favour starting separate tiles over Settlement merging/splitting. |
| Free-roam Citizen following | Release polish or players trying to follow Citizens beyond current inspection controls. |
| Illegal parking | Parking pressure is too quiet under the existing response; design enforcement or another meaningful consequence. |
| Returning departed Households | Playtests show a need to remember departures; any remembered population must be bounded. |
| Ruleset DSL | Measured TOML authoring friction slows content iteration; part of authoring tools if triggered. |
| Fourth labour tier | Workforce progression stalls, Office is too easy to staff, or larger cities support a distinct top-tier market. |
| Named diseases | Generic illness, urgent care and inpatient capacity form a playable care cycle. |
| Ferries and cable cars | Playable water or relief barriers create a transport problem these modes solve. |
| Wildlife and ecology | A habitat would change a concrete player decision. |
| Historic preservation | A demonstration loses valued Buildings to redevelopment with no player intervention. |
| Architectural era | Building age is recorded and can support meaningful variant selection. |
| Achievements and campaign | A world is interesting to inherit; scope authored situations and recognition around that artifact. |
| Snow clearing | Another capability introduces seasons; scope accumulation, dispatch and traffic consequences together. |
| Birth-rate and immigration Policies | Population binds in a demonstrated case that spatial action cannot relieve. |

## Earlier reports — verify on touch

These are preserved leads from the retired ledgers, **not confirmed present defects, priorities or
new gates**. Several reports predate shipped repairs. When related work starts, read the linked
original conditions, inspect current code, and either fix the issue, put an actionable task above,
or remove the lead. No separate audit session or ratification programme is required.

| Area | Reports to check | Original evidence |
|---|---|---|
| Decline and parking | Balance → failure → recovery demonstrations; Business tenancy failure duration; pressure on parking supply; congestion ladder direction | [Question ledger A](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/plans/0002-open-questions.md#L41). Row 30 owns the decline investigation. |
| Traffic and routing | Vehicles actually moving, shopping Trip frequency, parking departure walk, route memory/sharing, cache hit rate, walk-search cost, matrix cadence and resolution, peak load and commuter fraction | [Measurements B](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/plans/0002-open-questions.md#L92), [traffic questions](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/plans/0002-open-questions.md#L348), [routing cluster](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/plans/0002-open-questions.md#L644) |
| Traffic fidelity | Lane distribution, freight Stress, Habit/Sight/Temperament, promotion/demotion, virtual queues and microscopic capacity | [Traffic questions](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/plans/0002-open-questions.md#L348), [B](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/plans/0002-open-questions.md#L92). Scope the driver mechanism before choosing its parameters. |
| Land and world generation | Height/Terraform, land-value staggering and rush-hour response, pollution decay/plume, road density, playability floors, Hazard maps, extraction hysteresis and fertility explanation | [Land questions](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/plans/0002-open-questions.md#L152), [generation obligations](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/plans/0002-open-questions.md#L1444) |
| Economy and services | Private capital, labour as an input, Materials during construction, changing Outside prices, policy identity, unaffordable commands, departing balances, Building Money validation, health, recreation and Service variants | [Economy](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/plans/0002-open-questions.md#L484), [unowned reports](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/plans/0002-open-questions.md#L1006). Check rows 31–33's changes before assuming absence. |
| Choice and ownership | Multiplicative utility, endogenous car ownership, counters for premises considered and aggregate preference behaviour | [Choice](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/plans/0002-open-questions.md#L330), [B](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/plans/0002-open-questions.md#L92), [ownership](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/plans/0002-open-questions.md#L1017) |
| Architecture and rendering | Snapshot/thread ownership and marshalling, mobile spatial partition, Chunk size, mesh instancing versus variation, save-shape migrations, raw table mutation and Tick ownership | [Architecture](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/plans/0002-open-questions.md#L544), [B](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/plans/0002-open-questions.md#L92), [table doors](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/plans/0002-open-questions.md#L1024). Building assets are authored in Blender first. |
| Test reliability and performance | Allocation measurement instability, post-submit results ownership, accidental decide guard cost and long-run coverage | [Allocation evidence](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/plans/0002-open-questions.md#L110), [CI ownership](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/plans/0002-open-questions.md#L1082), [test cost](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/plans/0002-open-questions.md#L1378) |
| Runtime defects previously filed | Resource insertion during reload (15); fixed temporary path collisions (18); corrupt save handles (19); Pool terms without Districts (20); missing chain-depth guard (24); long post-submit regression (25); duplicate removal commands (26); footpath rendering width (27); intermittent tests (13) | [Original queue entries and reproductions](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/plans/0003-build-plan.md#L819). Numbers here are historical identifiers, not new priorities. |
| Documentation mismatches | Descriptions of land area, Lots, layers, Rule timing, tenancy, money issuance, testing costs, procedural ground and other code/document disagreements | [Filed corrections](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/plans/0012-corpus-audit.md#L1906), [later reports](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/plans/0012-corpus-audit.md#L3843). Correct current text when verified; do not preserve correction narratives. |
| Calibration | Previously provisional values and proposed experiments | [D1/D2 and original qualifications](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/plans/0002-open-questions.md#L1106). The central ratifier obligation is retired; tune and demonstrate in the capability that uses the value. |

Capability context is in [the roadmap](../docs/06-roadmap.md), with definitions in
[0074](0074-the-systems-nobody-named.md) and deliberate deferrals in [deferred.md](../docs/deferred.md).
Their candidates are indexed above and below; context files do not own a separate queue. The old coverage/grilling programme is retired.
Historical evidence is also available offline with `git show be07abc3b90a8102cc77ac131fd8852fbf853596:PATH`.

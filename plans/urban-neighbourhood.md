# A playable urban-fabric neighbourhood

Show the payoff of the existing individual housing decisions before adding intensity caps.
Owner: `urban-permissions`.
Status: implementation and automated/driven validation delivered; **user playtest outstanding
and deferred**. This design is not complete until that playtest and its acceptance review finish.

## Required user playtest — deferred

The user currently accesses the session remotely over a shell and cannot playtest. Revisit when
they have access to the graphical game and can play the [walkthrough](../examples/UrbanNeighbourhood/README.md).
This deferral blocks completion of the urban-fabric design, not progress on other work.
Automated tests and the agent-driven demonstration remain valid evidence but do not replace this
user acceptance step.

- [ ] The user plays the neighbourhood, granting permission before and after the waiting period.
- [ ] Confirm they can understand why the Households wait, what the zoning action changes, and
  why construction stops once both are housed; assess whether the interaction justifies its complexity.
- [ ] Record the user's verdict and resolve any acceptance-blocking findings before marking the
  design complete. Select further intensity/form work in light of that verdict.

## Demonstration scope

Deliver a reproducible saved neighbourhood with Citizens, affordable but unattractive vacancies,
two seeking Households and unpermitted frontage. Use the existing zoning gesture to permit an
addition. City Evidence links to each seeker and the Household inspector explains the saved
preference episode. Keep inspection read-only and retain normal placement and construction.

Acceptance: no construction without permission; elapsed observations before mismatch qualifies;
one suitable addition houses both seekers; existing vacancies remain; further permitted ground
stays empty once demand is met. Check save/reload continuation and invariants, run the working
lane, and watch the intervention in Godot. Record what the observation reveals. The fixture
isolates housing; it is not a balanced economy or a first-playable founding loop.

## Delivered demonstration

[Export and play instructions](../examples/UrbanNeighbourhood/README.md) use an ordinary city save
with its embedded Ruleset. Six Citizens begin at Tick 512: two resident Households, two arriving
Households. Existing homes cost 50 per Day, Outside 25, and the new kind zero; all seekers carry
100. These prices isolate preference from affordability, with no claim to economic balance.

City Evidence lists seeking Households in pages of six, read at the panel's stated snapshot Tick.
Following a link opens that individual even without a Building on the map. Its live inspector
reads the current comparison and saved episode; it neither observes nor accrues evidence.
The same selection follows the Household into its home. Reading a page bounds complete housing
comparisons to those six rows. This exposes housing need, not a complete explanation of every
rejected construction proposal.

## Driven observation

Godot 4.7.2 Mono, Debug, Vulkan on Intel UHD 630, 1400×900, seed 62002. This was a busy development
machine with tests running; frame rates are not performance evidence. The shell's new JSON housing
fields confirmed the final assembly was loaded. Both screenshots and draw rows were inspected.
[Retained observations](../artifacts/visual-study/urban-neighbourhood/observation.json) include
captions, State Hashes, search readings and layer counts; no populated layer reached capacity.

| Tick | Player action / observation |
|---|---|
| 512 | Loaded the save and followed the actual Household 3 button in Evidence. Two seeking Households, six Citizens, two drawn homes plus the gate. |
| 650 | Paused. Household 3 had 136 of 256 elapsed observation Ticks. No addition. |
| 902 | Paused with zoning withheld. Its episode spanned 376 Ticks, with the latest observation at 888; it qualified, but no addition appeared. |
| 902–905 | Painted the three vacant parcels with the normal housing tool at Tiles (20,10), (32,10), (44,10). Paused edits each advanced their command Tick. One addition appeared; before move-in, the new vacancy already counted as an option. |
| 952 | Both seekers were housed. The retained Household 3 inspector read Home: Building 4 and still listed Citizens 3 and 4. |
| 1402 | Still four Buildings including the undrawn gate, two vacant Lots and six Citizens. Evidence read zero seekers. Back from the Household opened its new Building. No further addition. |

[Waiting](../artifacts/visual-study/urban-neighbourhood/waiting.png),
[qualified without permission](../artifacts/visual-study/urban-neighbourhood/qualified.png),
[housed](../artifacts/visual-study/urban-neighbourhood/housed.png).

Watching exposed two presentation gaps that are fixed here: unplaced Households could not be
inspected, and “Nothing in the city is stopped” ignored their housing search. The headline now
states Building activity separately. The fixture gate has no visible geometry; its population
admissions are real, but it is not presented as a finished Outside Connection scene. The example
omits jobs, supplies and rent collection. The deferred user playtest above will assess whether
more intensity controls would improve the interaction.

## Validation

`UrbanNeighbourhoodTests` covers early and late permission, elapsed observation eligibility,
read-only inspection, the two existing vacancies remaining unused, one addition housing both
seekers, no further construction, save/reload continuation with one versus two route workers,
and end-of-run invariants. The exported save also loaded in Godot and passed its load checks.

The working lane passed **3,853 tests**. The Godot Debug build, repository format check and
Taplo lint of all 54 Rulesets passed. No simulation behaviour or saved-state declaration changed.

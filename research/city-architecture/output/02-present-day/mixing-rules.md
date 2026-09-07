# Regional coherence and a fictional mixture

These are **proposed design rules inferred from the atlas**, not measured frequencies or approved game mechanics. The user requested both regional and fictional possibilities, with a possible slider. Keep the initial slider explicitly tied to two selected reference sets: Pacific Northwest infill and contemporary Dutch housing. Additional American and European contexts would need their own catalogues.

## What can mix, and at what scale?

| Feature | Within one building | Across a street | Across neighbourhoods |
|---|---|---|---|
| Brick/wood/metal and muted colours | Yes, with explained wall build-up, joints and water shedding | Shared palette can connect different buildings | Wider palette change can mark a different development period |
| Windows, shading, entrance canopy | Fit room/core positions and structural openings | Different patterns can coexist with a common frontage line | Complete opening/access families can vary |
| Pitched versus low-slope roof | Choose a complete assembly; additions need abutment details | Both are evidenced within American and Dutch examples | A coherent package may favour either without claiming national exclusivity |
| Timber versus concrete/steel structure | Hybrid only with explicit load transfer and floor build-up | Different structures can be neighbours | Select by type, era and span; do not expose structure solely by region |
| Porch/setback versus street-wall frontage | Needs allocated front ground and a usable threshold | Transition at ends, corners or setbacks; do not interpenetrate neighbours | Different block/plot layouts can support each |
| Stair, gallery or corridor access | Discrete plan choice with connected paths | Preserve side/rear access and privacy | Different building families can carry different access patterns |
| Rear servicing, parking, garages | Must fit movement and required floor/ground reservations | Share a lane only where it connects | Can differ by district; never use parking as a proxy for nationality |
| Construction age, repair and retrofit | Separate original form and later intervention | Selective survivors and replacements create history | Independent art/content controls; no automatic heritage at the European end |

Sources behind these distinctions: [South Bend plans](https://southbendin.gov/wp-content/uploads/2023/06/SBBT_Catalog_23-0506-lowres.pdf#page=22), [Hogekwartier variants](https://www.amersfoort.nl/ro-online/NL.IMRO.0307.BP00141-0301/b_NL.IMRO.0307.BP00141-0301_rb3.pdf#page=15), [Seattle roof/access drawings](https://www.seattle.gov/dpd/AppDocs/GroupMeetings/DRProposal3012121AgendaID4348.pdf#page=16), [Wohnregal construction](https://www.f-a-r.net/projects/en_projects/119_wohnregal/).

## Candidate selection rule

For a new Building or a deliberately re-reviewed replacement, choose eligible models first. Eligibility includes footprint and permitted attachments, storeys, architectural use, shared versus individual access, party-wall conditions, local ground/streets, service space and a compatible climate/construction package. These predicates are research concepts; some need capabilities the current game does not expose.

For an eligible model `i`, a candidate weight could be:

`weight(i,b) = eligibility(i) × ((1−b) × US_weight(i) + b × NL_weight(i))`, with `b` from 0 to 1.

Normalize only after filtering. If the total is zero, use a separately authored fallback with valid access or leave the site unresolved; never stretch an ineligible model. Shared models can have weight at both ends. The weights are design choices. A 50 setting is not guaranteed to produce half of all Buildings from each set, especially where many candidates fail eligibility. It also says nothing about population, land area or floor-area shares.

For coherence, test a block/street preference with selective infill against fully independent per-building selection. Keep a stable selection seed when changing the comparison setting; otherwise random changes obscure the effect. Do not silently regenerate existing buildings when a player moves a preference: decide separately whether it affects future construction, a preview or an explicit reskin. Appearance-only reskinning must preserve footprint, capacity and meaningful access; broader transformations require content decisions.

## What the interactive study actually does

[blend-study.html](blend-study.html) contains eight fixed **illustrative design slots**, grouped into four adjacent pairs. Each slot has two geometry-compatible proposed treatment packages, US-informed and Dutch-informed. Thresholds switch pairs in a fixed order at 12.5,37.5,62.5,87.5. The midpoint therefore displays four of each. This simple deterministic example explains whole-package selection; it does **not implement** the probability formula, simulate actual eligibility, or estimate citywide shares. Equal counts happen here because the slots were deliberately made compatible and equally counted.

The displayed forms, palettes and module layout are proposed visual notation. Streets, plots, floor counts, uses, heritage frequency and future features are held fixed. Labels remain visible for accessibility and to prevent a weak visual distinction from being mistaken for proof. In production, a valid plot may permit only one family; this demo does not solve that case.

## Decision after the prototype

Keep a simple slider if players understand it as influence over compatible designs and the midpoint produces coherent streets. Prefer named presets plus optional blend if the endpoints are too broad or repeated exceptions dominate. Use separate urban-layout choices if the desired distinction primarily involves plots, density, access or parking. There is no research basis for making the UI more complicated before this visual comparison.

Counterexamples to test: a modern flat-roof American row; a Dutch pitched row; a German postwar apartment; industrially framed European live/work; a modern US brick block; an older survivor in each context. A treatment that requires changing all these to fit a national stereotype fails the research brief.

# A contemporary city with an architectural past

Research for city-sim's designer and building-model authors · 6 September 2026

**Start with a fictional, temperate city combining contemporary Pacific Northwest and Dutch housing influences. Give it ordinary postwar buildings, selective older survivors and visible recent adaptations.** Keep coherent regional alternatives available. The evidence supports blending complete building designs within compatible streets; it does not support morphing a national roof or multiplying every dimension by an “American” factor.

This follows the user's present-day/near-future direction and replaces the first pass's provisional northern English recommendation. Rare heritage architecture is an art choice. Construction era, renovation date and condition should remain distinct. “Near future” means a modest next generation of buildings and interventions, not a dated forecast.

Read the [illustrated atlas](atlas/index.html), [scaled comparison](scale-comparison.svg), [blend experiment](blend-study.html), [model briefs](model-briefs.md) and [simulation audit](simulation-audit.md). The [dimension table](dimensions.csv) retains original units alongside metric conversions; [sources](sources.csv) record exact pages, inspection and reuse standing.

## 1. What should the everyday stock contain?

Use a family set broader than detached houses: narrow houses and duplexes; attached homes; small stair/corridor-access apartments; mixed-use and live/work buildings; larger apartment ranges and courtyard infill; ordinary retail, offices and workshops. Make the last group a distinct construction/access family rather than another residential body with different windows.

The new atlas supplies 15 records, not 15 independent measured buildings. Municipal proposals provide labelled dimensions; built profiles provide corroborating street relationships. German energy typologies provide postwar examples without requiring heritage facades. The selection is deliberately useful for modelling, not statistically representative. [South Bend catalogue](https://southbendin.gov/wp-content/uploads/2023/06/SBBT_Catalog_23-0506-lowres.pdf#page=22), [IWU typology](https://www.iwu.de/fileadmin/publikationen/gebaeudebestand/episcope/2015_IWU_LogaEtAl_Deutsche-Wohngeb%C3%A4udetypologie.pdf#page=172)

Keep substantial postwar fabric even while older ornamental buildings are scarce. The Commission reports 85% of EU buildings were constructed before 2000; that is neither a heritage share nor a distribution to copy into the game. US age proportions were not verified: the attempted Census endpoint returned a missing-key page. We therefore recommend no supposedly empirical age percentages. [European Commission, stock context](https://energy.ec.europa.eu/topics/energy-efficiency/energy-performance-buildings/energy-performance-buildings-directive_en)

## 2. Which settings are defensible starting points?

| Candidate | Evidence-based vocabulary | Proposed use in the game |
|---|---|---|
| Pacific Northwest US infill | Paired homes, brick block apartments, contemporary live/work; legible entrances, parking/service arrangements | American endpoint for the first comparison. Midwest narrow-house/duplex plans are dimensional controls, not proof of Portland prevalence |
| Contemporary Dutch housing | Coordinated narrow-frontage rows, small groups around shared green ground, both flat and pitched roofs | European endpoint for the first comparison; distinguish structural bay width from outside wall width |
| German postwar and contemporary infill | Apartment ranges, restrained terraces, new courtyard buildings, prefabricated live/work frames | Additional European families; prevents the Dutch endpoint from becoming a definition of Europe |
| Fictional mixture | Families above selected under common street, climate and access constraints | Recommended first prototype; shared warm masonry/timber and grey roof/metal palette is an authored choice |

Portland's 2008 toolkit explicitly says its prototype pitched roofs were a presentation choice, not a rejection of flat roofs. This is a useful warning against copying the bias of a reference collection. [Portland, A-2](https://www.portland.gov/sites/default/files/2020-01/toolkit1208-optimized_bkmrks.pdf#page=80)

Dutch evidence also defeats a simple national roof rule: Hogekwartier's 4A and 4B share a stated bay/depth pairing but have flat and pitched variants. Their usable areas differ. Roof and floor configuration belong together. [Amersfoort, appendix drawing p 14](https://www.amersfoort.nl/ro-online/NL.IMRO.0307.BP00141-0301/b_NL.IMRO.0307.BP00141-0301_rb3.pdf#page=15)

## 3. What makes the dimensions and construction credible?

Select access and structure before facade repetition. The South Bend sixplex has a stepped plan and central circulation, not a rectangular house enlarged until six homes fit. Seattle's live/work proposal resolves different floor datums, independent stairs and occupied/service roof areas. Both are design documents, not verified as-built surveys. [South Bend, p 21](https://southbendin.gov/wp-content/uploads/2023/06/SBBT_Catalog_23-0506-lowres.pdf#page=22), [Seattle, sheets 12–16](https://www.seattle.gov/dpd/AppDocs/GroupMeetings/DRProposal3012121AgendaID4348.pdf#page=13)

Width can grow through repeated dwelling bays; depth needs rooms, circulation, light and structural support. Berlin's Wohnregal instead employs approximately 13 m clear spans using precast industrial components. That is evidence for a different structural family, not a universal residential span. [Frohn/Rojas, ARQ p 95](https://publikationen.bibliothek.kit.edu/1000125224/90508743#page=2)

A shallow industrial roof is credible when its frame and cladding explain it. Portal-frame guidance separates rafters, haunches, purlins and longitudinal bracing. Product-specific timber tables likewise distinguish roof spans from residential floor spans. A roof span cannot certify the floor beneath it. [Steel Construction Info](https://steelconstruction.info/topics/design/portal-frames), [Metsä Wood, p 25](https://www.metsagroup.com/globalassets/metsa-wood/attachments/others/metsa-kerto-ripa-prefabricated-building-elements.pdf.pdf#page=25)

The briefs translate these relationships into explicitly proposed parameters. Their dimensions are buildable research-model instructions, not structural or regulatory certification.

## 4. What makes the street coherent?

Hold a few things consistent: public frontage, threshold depth, party-wall versus side-access condition, shared height bands, and an intelligible rear/service network. Permit different eras and materials within those constraints. This is a design inference from the plans, not a measured law of urban beauty.

Contemporary infill can preserve a block relationship without copying an old facade. At cb 19, street fronts and courtyard balconies have different roles; the latter also participate in the project's escape arrangement. Do not import its balcony treatment without reserving the access it serves. [zanderroth, cb 19](https://www.zanderroth.de/de/projekte/cb19/340)

Commercial depth and loading need their own ground. Separate customer/home entrances from deliveries and refuse routes; a shutter placed against a garden does not establish vehicle access. [WBDG, loading-dock relationships](https://stg.wbdg.org/space-types/loading-dock)

## 5. What should the slider control?

**Recommend a contextual blend of eligible designs, with regional presets—not a universal geometry slider.** Start with two named reference sets: Pacific Northwest infill and Dutch contemporary housing. Keep density, age, condition and near-future interventions independent of the blend.

For each available plot, first filter designs by footprint, use, access, party walls, height, climate package and service needs. Blend the remaining selection weights. A midpoint mixes complete designs; it does not make half a stair, halfway change a structural span or turn a pitched roof into a flat roof. If one side has no eligible design, report or tolerate that limitation instead of forcing a fit.

The [interactive study](blend-study.html) demonstrates that limited behaviour at 0, 50 and 100. Its slots and weights are authored examples; it is not a simulator or evidence of visual preference. Cluster choices at the street/block level to test coherence, then allow selective infill. Compare against an independently shuffled version before deciding whether clustering helps. The detailed [compatibility rules](mixing-rules.md) specify what the experiment does and leaves unresolved.

## 6. How should near-future change appear?

Make adaptation a first-class source of visual variety: deeper reveals after insulation, altered windows, roof equipment, solar layouts, shading, renewed cladding and carefully located additions. Nottingham demonstrates built facade/roof retrofit packages. US panel-block work is still described as development/demonstration; its target cost and performance are not established outcomes. [Energiesprong Nottingham](https://www.energiesprong.uk/projects/nottingham), [DOE/Fraunhofer](https://www.energy.gov/cmei/buildings/single-family-deep-energy-retrofits-prefabricated-panel-block-wall-insulation)

Grand Parc's documented 3.8 m winter-garden/balcony additions demonstrate how much a retrofit can change a facade and its ground relationship while retaining a building. It is an exceptional large project, so borrow the principle selectively, not its scale. The architect's page contains 2016/2017 dating and incompatible existing-area figures; the table avoids those area totals. [Grand Parc project](https://www.lacatonvassal.com/index.php?idp=80)

PV or a planted roof needs access, drainage and support reservations. An exposed timber facade is not proof of a timber structure. None of these features should be automatically assigned by nationality or vacancy state.

## 7. What should we build next?

Build the briefed **four-home contemporary row, corner mixed-use building and small apartment block**, with a low workshop/service mass used only as a secondary context test. Compare US-informed, Dutch-informed and mixed variants on matched ground and camera paths. Then add one selective older survivor and one retrofit variant to test accumulated age.

G003 remains the decisive control: **24 ×16 m, 10.5 m walls, 1,152 m² counted floor, two reconstructed tenancy places**. N3 retains that body while proposing apartment access and a roof assembly. Its twelve-dwelling architectural hypothesis remains a content question. A style blend must not silently alter the number of simulated homes or Businesses. [Reproduced audit](simulation-audit.md)

The main remaining evidence gaps are ordinary modern workshops and standalone retail plans, as-built roof drainage sections, measured court widths, American postwar stock geometry and southern/eastern European or Scandinavian coverage. Climate-specific suitability and the visual success of a mixture require the next prototype. The dimensional controls and counterexamples are sufficient to start that bounded comparison; further broad searching would not settle the user's aesthetic preference.

All work is research-only and uncommitted. No Godot run, gameplay/content edit, ADR or corpus-check change occurred. [Verification and limitations](VALIDATION.md) distinguish completed artifact checks from unperformed simulation and visual-prototype checks.

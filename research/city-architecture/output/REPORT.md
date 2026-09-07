# City architecture research

**Provisional recommendation: a northern English industrial town, with nineteenth-century terraces and workshops, interwar additions and contemporary infill.** No response to the early preference question was received before this bundle was prepared. This is a starting context for testing, not player approval or a universal European style.

Open the [illustrated atlas](atlas/index.html), then the [dimension-derived comparison](scale-comparison.svg). The [three briefs](model-briefs.md) specify the next models. [Dimensions](dimensions.csv), [sources and reuse standing](sources.csv), and the [simulation audit](simulation-audit.md) provide the evidence behind them.

## What the evidence changes

The next improvement should be **building type and access, alongside roof construction**. A terrace is a repeated arrangement of houses and party walls; a small apartment building needs stairs and a circulation plan; a workshop can retain a domestic frontage while acquiring narrower ranges behind it. Those distinctions survive at neighbourhood distance. More elaborate windows cannot supply them. The inspected house plans and factory roof photograph are particularly useful controls: [HTA LTH201/207, S01](https://data.parliament.uk/DepositedPapers/Files/DEP2012-1192/Designoflifetimehomes_final.pdf#page=9), [Evans, S13, printed p 63 Fig 4](https://www.aschb.org.uk/wp-content/uploads/2022/01/Vol-34.pdf#page=65).

The checkpoint's corrected roofs are an improvement, not an architectural specification. Its sampled Building G003 is **24 × 16 m, with 10.5 m walls and a 4.125 m roof rise**. It has room for a materially different residential type from a detached house. The scale sheet compares it with a **24 × 7.8 m, six-flat proposal**, whose reported eaves/ridge heights are 6.5/8.5 m. The latter is a documented 2018 design, not a measured surviving building, and its three-storey description must not be divided into equal floor heights. [S03, p 92 §4.4](https://democracy.epsom-ewell.gov.uk/documents/g569/Public%20reports%20pack%2013th-Dec-2018%2019.30%20Planning%20Committee.pdf?T=10)

There are two separate experiments:

- Give G003 a plausible apartment layout **without changing its footprint**. M3 tests this and includes a genuinely low-slope roof assembly.
- Build a smaller house and repeat it as a terrace. M1 tests this on an independent research street; applying it to existing simulation ground would need a subdivision/content decision.

A broad Building is not necessarily wrong. Replacing every broad Building with little houses would change both the intended type and its ground use.

## Why this context

| Direction | Useful fit | What would need its own investigation |
|---|---|---|
| Northern England, chiefly 1850–1930 plus later infill | Warm masonry/slate; attached houses, modest shops and working rear courts can share a coherent street vocabulary | Metric surveys of ordinary northern houses and shops; regional stone/brick differences |
| Dutch/Flemish towns, earlier cores plus later expansion | Narrow plot identities and working back streets make fronts and service sides meaningful | Amsterdam is only the researched Dutch example; Flemish evidence is insufficient. Gables, party-wall construction and canal logistics need a separate kit |
| Northeastern US, chiefly 1850–1930 plus infill | Timber apartment houses, stoops and side gaps offer colour and readable entrances | Different framing, porch, roof-edge and block relationships; do not transplant English terrace details |

The atlas illustrates these as original **concept diagrams**, not surveyed regional buildings. Amsterdam's archive supplies the service-street relationship; Boston's city report supplies the timber/stoop typology. Neither supports dimensions for our English kit. [S22](https://www.amsterdam.nl/stadsarchief/canon/windows/12/), [S23](https://www.boston.gov/sites/default/files/imce-uploads/2017-01/retrofitting_report_10.7.2016.pdf)

## Construction findings

1. **Choose access and structural organisation before facade repetition.** A shared stair can explain a long building with few external doors; a terrace needs individual entrances. Make that distinction visible.
2. **Width and depth are different parameters.** Grow a terrace by adding dwellings, not widening every room. Grow a deep workshop through an explicit range, frame or rooflight system. Grow a residential block only with an explicit daylight/circulation solution.
3. **Roofs need assemblies and junctions.** A pair of ridges creates a valley requiring drainage. A hip needs ridges and hips whose geometry follows the spans. A parapet hides a roof; it does not prove that the roof is horizontal. Historic shop ranges and interwar apartments provide different edge conditions. [S07](https://historicengland.org.uk/listing/the-list/list-entry/1133525), [S09](https://historicengland.org.uk/listing/the-list/list-entry/1379280)
4. **Covered modules govern material scale.** Modern brick dimensions and double-lapped tile dimensions are useful controls, not measurements of historic buildings. Enlarging the apparent brick must remain a labelled art experiment. [S17](https://www.wienerberger.co.uk/products/brick/standard-format-bricks.html), [S18, 800CPT35](https://www.marley.co.uk/-/media/066d3695d9a94cb7a8019b1db5175f1f.pdf?rev=27071efb996241869e2faffc511005fe)
5. **Wear follows construction.** Proposed weathering masks should concentrate around runoff, exposed edges, joints and repairs. Vacancy is not evidence of a collapsed roof or structural distress. Preserve the same underlying building through occupied and abandoned appearances.

## Limits and next experiment

The atlas has **17 records**, including related proposals and cross-listed examples, not 17 independent measured surveys. There are two workshop examples; the mixed-use family includes useful controls that are not verified corner shops with homes above. Southern English dimensional controls cannot establish northern regional ranges. Floor-to-floor heights, window dimensions, historic roof pitches, courtyard widths and most plot boundaries remain missing. No occupancy has been inferred from windows or floor area. No generated image is used as evidence.

The next small prototype is specified in the briefs: **five terrace houses, one corner mixed-use building, and one apartment block**, with rear access and a low workshop attachment. Review untextured silhouette, entrances and backs first; then matching material/condition variants. The primary question is whether we can recognise the type and count its intended entrances before reading a label.

Work is confined to this output directory and left uncommitted. No gameplay, Rulesets, ADRs, corpus checks or production assets changed. Godot was not launched. The audit reproduces archived geometry calculations, not a fresh simulation; source comparison establishes where its geometry still applies. Research-file validation and the remaining verification limits are recorded in [VALIDATION.md](VALIDATION.md).

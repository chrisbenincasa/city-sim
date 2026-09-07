# Validation and evidence limits

Research date: 2026-09-06. The user explicitly authorised research-only outputs during the amnesty. No compensating implementation changes were made.

## Reproduction checks

Run from repository root:

```sh
python3 research/city-architecture/output/validate_bundle.py
```

Checks cover required files, unique source IDs, dimension status and units, missing-value handling, duplicate measurement keys, local HTML/Markdown links, SVG XML, script syntax, the 52-ID sample and arithmetic controls. Regeneration must leave the generated tables, atlas, scale sheet and evidence files byte-identical. This is artifact verification, not a gameplay regression suite.

The checkpoint draw and its repeated draw are byte-identical; SHA-256 inputs and inspected HEAD are in [reproduction.json](evidence/reproduction.json). Dimensions and capacities were recalculated from stored geometry and read code. The archived screenshot was visually inspected and copied unchanged into this bundle. No new simulation run is claimed.

The scale SVG was rendered with macOS Quick Look and visually reviewed. Its numerical scale is 10 SVG units per metre for buildings; material samples are explicitly enlarged 30 ×. G003 and M3 share the same footprint. The paired-roof counterfactual is explicitly a calculation, not a predecessor screenshot. No browser automation or moving-camera review was performed.

## Source inspection and access

- HTA's Parliament-hosted PDF downloaded successfully. Ground/first plans LTH201 and LTH207 were rendered and visually inspected. The Cambridge-hosted copy refused downloads; it was readable through web extraction. The report's numerical schedules, not pixel scaling, supply the dimensions.
- Epsom's committee pack downloaded; printed/PDF p 92 was rendered and its dimension schedule visually checked. These are proposal dimensions. No as-built completion, current dwelling count or full interior/roof section is verified.
- ASCHB's Evans conservation account downloaded; printed p 63 Fig 4 was visually inspected for street, rear and roof relationships. The image remains a source link because redistribution rights were not established. No perspective-derived metric dimensions were extracted.
- Goldsmith Street's published street photograph was downloaded temporarily and visually inspected. Marmalade Lane's Type D image was also inspected: it is an unscaled axonometric rendering, not a floor plan. Neither is redistributed here. Marmalade's brochure returned 404.
- Historic England listings and the Lincoln character extract supplied construction/count/access descriptions. Their photo links are not represented as complete measured surveys. DP045426's caption was inspected; the photograph itself was not independently inspected at full resolution.
- HABS MA-802 for Narbonne House was located but the survey download failed. It supplies no dimensional evidence. Commons image-rights pages also failed to load, so no Commons images were redistributed or treated as inspected evidence.
- Bauder's general falls advice and the indexed 2025 update differ in prescription. The update's full page redirects to login. The brief's fall is a labelled prototype choice, not a current compliance claim.

Temporary downloads and rendered source pages were kept outside the repository. No third-party source PDF or photograph was copied into the bundle. Original analytical diagrams are not copies of the source drawings.

## Repository checks not performed

Godot was neither run nor changed; no drive workflow or Debug build was necessary. No fresh screenshot, State Hash, save/reload, gameplay test, corpus test or performance result was produced. Source/corpus checks were not weakened. The corpus budget test's covered Markdown directories were inspected; they are`docs` and`plans`. This fact does not replace the user's explicit research authorisation.

Only `research/city-architecture/output/` was created/modified. The work remains uncommitted. Production asset authoring, live occupancy mapping and a small prototype street are follow-on work.

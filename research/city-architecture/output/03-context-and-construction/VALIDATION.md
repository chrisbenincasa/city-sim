# Validation record

Review edition 7 September 2026. [Machine checks](evidence/validation.json), [source-file hashes](evidence/download-hashes.json) and [external availability results](evidence/external-links.json) retain the detail.

## Verified artifacts and arithmetic

- 13 full SVG research plates plus 20 derived atlas thumbnails parse correctly and carry accessible titles. HTML review, atlas and document companions are generated locally.
- All local HTML links/anchors and image paths resolve; exact link count is in the machine record.
- 231 dimension-table rows pass unit conversion checks. Most rows are explicitly **proposed coordinates/dimensions**, not 231 real-world measurements. Source IDs resolve to 24 external and two internal records.
- Both apartment proposals have 1,152 m² gross study envelope. Contextual body rectangles do not overlap and leave the six-metre lane clear. G001's contextual body retains 36×20 m, two floors and 7 m walls. These checks do not establish vehicle turning, legal access or room layout.
- Archived near/repeat captures are byte-identical; all 52 sample rows recompute floor/capacity arithmetic. Checkpoint/current owner hashes are recorded. The retained checkpoint PNG is also byte-identical to the git artifact. **This is reconstruction, not a new game run.**
- Cached South Bend and Amersfoort PDFs match the prior pass's download hashes, and consequential sheets were re-inspected.

## Evidence and links

Consequential drawings inspected directly: South Bend sixplex, Amersfoort programme, Sacramento courtyard illustration, Tingbjerg street sections/photos, Spanish roof section/example table, Lorain roof/detail sheets, steel structural diagrams and Danish typehouse sketches. Records distinguish coordinator inspection, researcher inspection, text-only access and retained previous verification. No uninspected full-building dimension is promoted to a measurement.

All 24 external source URLs received an availability check. Twenty-one returned HTTP200 to HEAD. Lorain returned404 to HEAD but the coordinator's GET succeeded and was byte-identical to the inspected PDF. Carlisle's final HEAD failed and web retrieval returned502; current availability remains unverified. Bauder's general page returned403 to HEAD despite earlier readable web-tool access. These outcomes are not silently reported as “all external links green”.

No source photographs or PDF pages were redistributed. They remain at the exact cited URLs/pages. Local inspection copies were hashed; public access was not treated as an image licence.

## Sampled visual inspection

Headless Chrome rendered the full plates and sampled HTML at desktop width; the review entry was also checked at 390 px width. DOM checks reported no broken images, document horizontal overflow or full-SVG text outside the viewBox in captured views. Rendering records are `render-checks.json`, `render-checks-final.json` and `render-checks-corrections.json`.

Visual inspection covered the controlled apartment comparison, contextual streets including the fictional material variant, workplace, roof construction, low housing, retail/history and evidence controls. HTML inspection sampled the opening review/atlas, closing review links, dense source cards and simulation tables, plus the mobile opening. It was **not an exhaustive inspection of every HTML scroll position or atlas thumbnail**. The inserted archived PNG was inspected separately; its figure uses the previously checked responsive image layout.

Observed defects were corrected and relevant outputs rendered again: ambiguous stair elevations, crowded dimension labels, reversed axonometric visibility, facade inconsistency between views, workplace height/access reservation, unsupported-looking PV mounts, missing rear roof/gutter cues and a cricket placed on the wrong side of roof plant. The final viewport checks passed. Architectural diagrams remain schematic; review does not certify detailed geometry for construction.

## Unperformed checks and remaining gaps

No full room-layout, engineering, egress, daylight, drainage-capacity or vehicle swept-path check. No Godot launch, Debug build, simulation test suite, fresh simulation run, production mesh, performance reading, moving-camera review or player recognition test.

The package does not establish regional stock frequencies, fully measured European commercial types or a surveyed València block. Every contextual street is proposed ground. Tenancy-to-home mapping and appropriate content for large game bodies remain unresolved. These limitations are kept alongside the completed comparisons and briefs, not substituted for them.

All work is isolated in this pass and uncommitted. Previous passes, gameplay, Rulesets, ADRs and corpus checks were preserved.

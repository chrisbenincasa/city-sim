# Context and construction — research pass03

Open [index.html](index.html) for the illustrated review. The package includes 13 full vector plates, a source atlas, evidence/dimension tables, an archived simulation reconstruction and the next prototype briefs. Existing passes remain intact.

Rebuild from this directory with Python3 standard library, in order:

1. `python3 audit.py` — reconstruct the archived git capture.
2. `python3 draw.py` — author the proposed geometric comparisons.
3. `python3 data.py` — write evidence/dimension tables; reuses pass02 source metadata.
4. `python3 build.py` — build HTML review and companions.
5. `python3 validate.py` — local link, arithmetic, source-hash and recorded-render checks.

`render-check.cjs` uses Node22 and locally installed macOS Chrome to render the corrected plate sample; it writes QA screenshots to `/private/tmp` and viewport results here. `check-links.py` checks external URLs with HEAD and requires network access; GET follow-up results in the retained record were added after actual downloads. A later HEAD rerun alone cannot reproduce those GET confirmations.

`report-source.md` is the canonical decision text; `REPORT.md` is its review copy. Third-party inspection PDFs/screenshots are not bundled; download hashes and exact source references are retained. `validate.py` checks cached source hashes when the local inspection files exist and does not claim to download missing ones.

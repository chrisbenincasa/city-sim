---
description: Run the formatter check before every commit
priority: 50
---
- `scripts/test.sh` does not run the formatter. Run `scripts/format.sh --check` before every commit,
  as a separate gate; it exits 2 on a violation and 0 when clean.
- CI's `assertions` job runs Formatting **before** any test, so a whitespace violation fails the lane
  with zero tests executed. The job name says assertions and no assertion ran; read the step, not the
  job title.
- Fix with `scripts/format.sh`, then re-check. Confirm with `git diff` that it changed only layout.
- Where the formatter's own output is awkward — it splits a `with` expression from its brace — write
  the ordinary block form by hand and re-run the check, rather than committing mangled output.

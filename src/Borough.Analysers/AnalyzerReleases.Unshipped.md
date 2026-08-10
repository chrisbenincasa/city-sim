; Every diagnostic this assembly can report, tracked by Roslyn's own release-tracking analyser
; (RS2008). The ledger is not ceremony: a diagnostic id is a promise to whoever suppressed it, and
; this file is what makes silently renumbering one impossible. Entries move to
; AnalyzerReleases.Shipped.md when the project first ships something a suppression could outlive.
;
; Naming: BOR0<r><nn>, where <r> is the CI lint number in docs/05 §4. BOR08xx is the purpose_tag row
; of that section's banned-construct table, which is stated as a build-time check but is not one of
; the seven numbered lints. BOR09xx is adr/0003's per-field declaration, which 05 §4 does not
; enumerate at all — it is a rule the ADR states and the table layer implements, and it needs a
; build-time check for the same reason the purpose_tag row does: nothing at runtime can catch it.

### New Rules

Rule ID | Category                | Severity | Notes
--------|-------------------------|----------|--------------------------------------------------
BOR0201 | Borough.Determinism     | Error    | Lint 2 — float, double or decimal in Borough.Core, in state or in arithmetic
BOR0202 | Borough.Determinism     | Error    | Lint 2 — a System.Math or System.MathF member; use the tabulated fixed-point Transcendental
BOR0203 | Borough.Determinism     | Error    | Lint 2 — raw integer `/`; state the rounding through IntegerMath or Fixed
BOR0204 | Borough.Determinism     | Error    | Lint 2 — shift by a non-constant count; the count is silently masked
BOR0205 | Borough.Determinism     | Error    | Lint 2 — wall-clock time; the library has no clock
BOR0206 | Borough.Determinism     | Error    | Lint 2 — Guid.NewGuid or the default object/ValueType GetHashCode
BOR0207 | Borough.Determinism     | Error    | Lint 2 — a ratio pre-scaled by a large constant and divided in 32 bits; a Q16.16 quantity wraps almost at once
BOR0301 | Borough.Determinism     | Error    | Lint 3 — enumerating a hash-ordered map or set; building and looking up stays legal
BOR0302 | Borough.Determinism     | Error    | Lint 3 — System.Random; use Randomness.Draw
BOR0701 | Borough.SimulationState | Error    | Lint 7 — a struct that does not satisfy `unmanaged`, without a [ColdPath] argument
BOR0801 | Borough.Determinism     | Error    | Two PurposeTag members sharing one value
BOR0802 | Borough.Determinism     | Error    | A PurposeTag member claiming 0, which is reserved for None
BOR0803 | Borough.Determinism     | Error    | PurposeTag not backed by ulong
BOR0901 | Borough.SimulationState | Error    | Storage in a [Table] type that is not a declared Column or the table's own Rows

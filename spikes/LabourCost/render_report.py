"""Fill the report's CPU table after all thirteen workloads have finished."""
from pathlib import Path
import json

root = Path(__file__).resolve().parents[2]
out = root / 'plans/evidence/private-production-and-labour'
rows = json.loads((out / 'summary.json').read_text())
assert len(rows) == 13, f'Expected 13 completed workloads, got {len(rows)}'
assert all(r['maxAllocatedBytes'] == 0 and r['collections'] == [0, 0, 0] for r in rows)
lines = ['| World / roster | On-duty workers | Baseline ms | Deposit only: extra ms | Deposit + expiry: extra ms (range) | Extra / 15.6 ms target |',
         '|---|---:|---:|---:|---:|---:|']
for r in rows:
    if r['roster'] == 'observed' or (r['capture'].startswith('generated') and r['onDuty']):
        e = r['modes']['deposit-expiry']; d = r['modes']['deposit']
        low, high = e['pairedDeltaMinMaxMs']
        lines.append(f"| {r['capture']} / {r['roster']} | {r['onDuty']:,} | {r['baselineMedianMs']:.3f} | "
                     f"{d['pairedDeltaMedianMs']:.3f} | {e['pairedDeltaMedianMs']:.3f} ({low:.3f}–{high:.3f}) | {e['budgetPercent']:.1f}% |")
lines += ['', 'All **468 timed batches** reported **zero allocated bytes and zero GC collections**. The',
          'zero-worker controls are near zero marginal cost. The complete clone-control overheads,',
          'paired samples and all thirteen workloads are retained in `summary.json`.', '']
for r in rows:
    if r['capture'] == 'generated-1000000' and r['onDuty']:
        e = r['modes']['clone-control']
        lines.append(f"At {r['onDuty']:,} workers, the no-op copy adds {e['pairedDeltaMedianMs']:.3f} ms median "
                     f"(range {e['pairedDeltaMinMaxMs'][0]:.3f}–{e['pairedDeltaMinMaxMs'][1]:.3f}).")
lines += ['', 'The observed small-world expiry increment is about 43 ns per on-duty worker; the first',
          'million-Citizen staffed case is about 180 ns. Working-set growth matters: multiplying the',
          'small-world figure by a guessed million-city workforce would understate the measured cost.']
p = out / 'README.md'
s = p.read_text()
start = s.index('## CPU results\n') + len('## CPU results\n')
end = s.index('## Storage results\n', start)
s = s[:start] + '\n' + '\n'.join(lines) + '\n\n' + s[end:]
s = s.replace('RECOMMENDATION', '''Do not classify the proposed per-worker deposit path as affordable against the million-Citizen
Tick target on the strength of the existing scan. Even 64,673 depositing workers add 11.65 ms;
129,427 add 20.80 ms in this prototype. Background load and the simplified integration prevent
precise capacity certification, but every paired result in those cases is a substantial cost.
The whole wage pass already exceeds the target at these constructed workloads; these results
must not be presented as a new complete-Tick budget or added to old incomparable timings.

**Next CPU experiment:** keep individual fractional accrual and per-Tick attendance response,
but combine deposits per Business within the Tick and measure the flush, indexing and wake-up
cost. Preserve when waiting Rules become eligible; aggregation must not silently change their
ordering or throughput. Also exercise the intended authored deposit rate, since this run deliberately
makes every on-duty worker deposit whole units every Tick. Switching to one deposit per Day would
change gameplay and is not warranted by these measurements alone.

**Storage:** reject the premise that unconditional buckets are noise. Compare a compact store for
expiring Bins and sensible initial Bin sizing before choosing the layout. Four buckets are a
measured candidate, not a selected precision requirement. The memory result is exact for these
counts; the content mix and future layout remain choices.

The requested measurements are complete. The implementation strategy still needs those choices;
the report does not claim production, shelf life or their gameplay acceptance checks are built.''')
p.write_text(s)

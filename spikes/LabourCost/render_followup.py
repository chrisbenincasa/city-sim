"""Render the complete paired results and actual storage-accounting table."""
from pathlib import Path
import json

root = Path(__file__).resolve().parents[2]
out = root / 'plans/evidence/private-production-and-labour/combined'
rows = json.loads((out / 'summary.json').read_text())
assert len(rows) == 5, f'Expected five completed captures, got {len(rows)}'
assert all(r['maxAllocatedBytes'] == 0 and r['collections'] == [0, 0, 0] for r in rows)
lines = ['Each increment below is the median paired difference from that capture’s wage-only baseline.',
         'Medians of different paired differences need not add or subtract exactly.', '',
         '| Capture | On-duty workers | Wage-only ms | Individual+dense extra ms | Combined+dense extra ms | Individual+compact extra ms | Combined+compact extra ms |',
         '|---|---:|---:|---:|---:|---:|---:|']
for r in rows:
    m = r['modes']
    lines.append(f"| {r['capture']} | {r['workload']['onDuty']:,} | {m['baseline']['medianMs']:.3f} | " +
                 ' | '.join(f"{m[k]['vsBaseline']['medianMs']:.3f}" for k in
                            ('direct-dense', 'combined-dense', 'direct-sparse', 'combined-sparse')) + ' |')
lines += ['', 'The combined+compact candidate compared directly with individual+dense:', '',
          '| Capture | Paired change, ms | Full paired range, ms |', '|---|---:|---:|']
for r in rows:
    d = r['modes']['combined-sparse']['vsDirectDense']
    lines.append(f"| {r['capture']} | {d['medianMs']:+.3f} | {d['rangeMs'][0]:+.3f} to {d['rangeMs'][1]:+.3f} |")
lines += ['', 'Negative is faster. These ranges describe seven observed pairs, not confidence intervals.',
          'All **175 timed batches** reported **zero allocations and zero GC collections**.', '',
          'Measured compact-table payload, after adding one labour Bin per Business:', '',
          '| Capture | Dense expiry MiB | Compact expiry + index MiB | Combining scratch MiB |', '|---|---:|---:|---:|']
for r in rows:
    if r['capture'].endswith('low'): continue
    m = r['memory']
    lines.append(f"| {r['capture']} | {m['denseBytes']/2**20:.3f} | {m['sparseBytes']/2**20:.3f} | {m['combinedScratchBytes']/2**20:.3f} |")
seed0 = next(r for r in rows if r['capture'] == '1000000-seed0-high')
lines += ['', 'For seed 0 at one million, compact expiry uses 131,072 allocated rows for 84,670 live',
          'labour Bins: 8 MiB of row payload plus 6.87 MiB for the Bin index. Dense expiry costs',
          '68.66 MiB over 1,800,000 allocated Bin slots. That is **53.80 MiB less expiry storage**',
          '(78.35%); combining needs a separate **1.72 MiB** of scratch.',
          'Business-to-Bin handles and waste counters add 1.14 MiB each in either prototype.',
          'The 71.24 MiB of existing Bin-column growth and 7.63 MiB of Citizen remainders from the',
          'original scenario remain. This is not a 78% reduction in total city memory.', '',
          'Calculated content sensitivity for that same seed and allocation policy:', '',
          '| Existing Goods Bins also expiring | Expiring Bins including labour | Compact capacity | Compact expiry + index MiB | Dense expiry MiB |',
          '|---|---:|---:|---:|---:|']
for r in seed0['storageSensitivity']:
    lines.append(f"| {r['goodsExpiringPercent']}% | {r['expiringBins']:,} | {r['sparseCapacity']:,} | {r['sparseBytes']/2**20:.3f} | {r['denseBytes']/2**20:.3f} |")
lines += ['', 'These content mixes are calculations, not new observed worlds. At 100%, the compact table',
          'crosses another capacity boundary and costs slightly more than dense storage. A compact',
          'representation is justified for sparse expiry, not automatically for every future Goods mix.', '',
          'Actual potential deposit calls over each 32-pass capture, calculated outside timing from',
          'the same roster, rates and initial fractions (all timed proxy queues are empty):', '',
          '| Capture | Individual calls | Combined calls |', '|---|---:|---:|']
for r in rows:
    d = r['deposit-counts']; lines.append(f"| {r['capture']} | {d['directCalls']:,} | {d['combinedCalls']:,} |")
p = out / 'README.md'; s = p.read_text()
a = s.index('## Results\n') + len('## Results\n'); b = s.index('## Interpretation and recommended next step', a)
s = s[:a] + '\n' + '\n'.join(lines) + '\n\n' + s[b:]
p.write_text(s)

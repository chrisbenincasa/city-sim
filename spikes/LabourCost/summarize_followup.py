"""Summarize paired current-build comparisons; no subtraction across capture dates."""
from pathlib import Path
from collections import defaultdict
import json
import statistics

root = Path(__file__).resolve().parents[2]
out = root / 'plans/evidence/private-production-and-labour/combined'
summary = []
for path in sorted(out.glob('*.jsonl')):
    if path.name.endswith('-host.jsonl'):
        continue
    rows = [json.loads(s) for s in path.read_text().splitlines()]
    pairs = defaultdict(dict)
    for r in rows:
        if r['type'] == 'timing': pairs[r['pair']][r['mode']] = r
    if len(pairs) != 7 or any(len(p) != 5 for p in pairs.values()): continue
    item = {'capture': path.stem}
    for kind in ('conditions', 'workload', 'memory', 'deposit-counts'):
        item[kind] = next(r for r in rows if r['type'] == kind)
    item['storageSensitivity'] = [r for r in rows if r['type'] == 'storage-sensitivity']
    item['modes'] = {}
    for mode in ('baseline', 'direct-dense', 'combined-dense', 'direct-sparse', 'combined-sparse'):
        times = [p[mode]['msPerPass'] for p in pairs.values()]
        def comparison(reference):
            deltas = [p[mode]['msPerPass'] - p[reference]['msPerPass'] for p in pairs.values()]
            return {'medianMs': statistics.median(deltas), 'rangeMs': [min(deltas), max(deltas)], 'pairedMs': deltas}
        item['modes'][mode] = {'medianMs': statistics.median(times), 'rangeMs': [min(times), max(times)],
                               'vsBaseline': comparison('baseline'), 'vsDirectDense': comparison('direct-dense')}
    for group, direct in (('combined-dense', 'direct-dense'), ('combined-sparse', 'direct-sparse')):
        deltas = [p[group]['msPerPass'] - p[direct]['msPerPass'] for p in pairs.values()]
        item['modes'][group]['vsMatchingDirect'] = {'medianMs': statistics.median(deltas),
                                                  'rangeMs': [min(deltas), max(deltas)], 'pairedMs': deltas}
    item['maxAllocatedBytes'] = max(r['allocated'] for p in pairs.values() for r in p.values())
    item['collections'] = [sum(r['collections'][g] for p in pairs.values() for r in p.values()) for g in range(3)]
    summary.append(item)
(out / 'summary.json').write_text(json.dumps(summary, indent=2) + '\n')
for s in summary:
    print(s['capture'], 'workers', s['workload']['onDuty'], 'businesses', s['workload']['workingBusinesses'])
    for mode, m in s['modes'].items():
        print(f"  {mode:16} {m['medianMs']:.4f}ms  extra {m['vsBaseline']['medianMs']:.4f}ms  "
              f"vs direct dense {m['vsDirectDense']['medianMs']:.4f}ms {m['vsDirectDense']['rangeMs']}")

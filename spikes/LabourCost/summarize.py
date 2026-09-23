"""Summarize completed captures; retain paired deltas, not differences of pooled means."""
from pathlib import Path
from collections import defaultdict
import json
import statistics

root = Path(__file__).resolve().parents[2]
out = root / 'plans/evidence/private-production-and-labour'
summary = []
for path in sorted(out.glob('*.jsonl')):
    if path.name.endswith('-host.jsonl') or path.name.startswith('age-'):
        continue
    rows = [json.loads(s) for s in path.read_text().splitlines()]
    groups = defaultdict(dict)
    workloads = {r['roster']: r for r in rows if r['type'] == 'workload'}
    for row in rows:
        if row['type'] == 'timing':
            groups[row['roster']].setdefault(row['pair'], {})[row['mode']] = row
    for roster, pairs in groups.items():
        if len(pairs) != 9 or any(len(p) != 4 for p in pairs.values()):
            continue
        item = {'capture': path.stem, **workloads[roster]}
        del item['type']
        baseline = [p['baseline']['msPerPass'] for p in pairs.values()]
        item['baselineMedianMs'] = statistics.median(baseline)
        item['baselineMinMaxMs'] = [min(baseline), max(baseline)]
        item['modes'] = {}
        for mode in ['clone-control', 'deposit', 'deposit-expiry']:
            deltas = [p[mode]['msPerPass'] - p['baseline']['msPerPass'] for p in pairs.values()]
            item['modes'][mode] = {
                'medianMs': statistics.median(p[mode]['msPerPass'] for p in pairs.values()),
                'pairedDeltaMedianMs': statistics.median(deltas),
                'pairedDeltaMinMaxMs': [min(deltas), max(deltas)],
                'pairedDeltaMs': deltas,
                'budgetPercent': statistics.median(deltas) / 15.6 * 100,
                'nsPerOnDutyWorker': statistics.median(deltas) * 1e6 / item['onDuty'] if item['onDuty'] else None}
        item['maxAllocatedBytes'] = max(r['allocated'] for p in pairs.values() for r in p.values())
        item['collections'] = [sum(r['collections'][g] for p in pairs.values() for r in p.values()) for g in range(3)]
        summary.append(item)
(out / 'summary.json').write_text(json.dumps(summary, indent=2) + '\n')
for s in summary:
    e = s['modes']['deposit-expiry']
    print(f"{s['capture']:20} {s['roster']:15} {s['onDuty']:8} workers baseline={s['baselineMedianMs']:.4f}ms "
          f"expiry delta={e['pairedDeltaMedianMs']:.4f}ms range={e['pairedDeltaMinMaxMs']} "
          f"control={s['modes']['clone-control']['pairedDeltaMedianMs']:.4f}ms")

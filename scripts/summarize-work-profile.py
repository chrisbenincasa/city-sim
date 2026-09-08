#!/usr/bin/env python3
"""Summarize --profile-work JSONL; correlations are diagnostic, not causal attribution."""
import argparse
import json
import math
from pathlib import Path
from statistics import mean


def flatten(value, prefix=''):
    result = {}
    for key, item in value.items():
        name = f'{prefix}.{key}' if prefix else key
        if isinstance(item, dict):
            result.update(flatten(item, name))
        elif isinstance(item, (int, float)):
            result[name] = item
    return result


def correlation(xs, ys):
    mx, my = mean(xs), mean(ys)
    numerator = sum((x - mx) * (y - my) for x, y in zip(xs, ys))
    denominator = math.sqrt(sum((x - mx)**2 for x in xs) * sum((y - my)**2 for y in ys))
    return numerator / denominator if denominator else None


def summarize(path):
    records = [json.loads(line) for line in path.read_text().splitlines()]
    rows = [flatten(row) for row in records if row.get('type') == 'work']
    if not rows:
        raise ValueError('No work records; capture with --profile-work.')
    if records[-1].get('type') != 'end' or records[-1].get('invariants') != 'passed':
        raise ValueError('Capture did not complete its end invariants.')
    start = records[0]['WarmupTicks']
    if len(rows) != records[0]['Ticks'] or any(row['tick'] != start + i for i, row in enumerate(rows)):
        raise ValueError('Work records must cover every measured Tick exactly once.')
    tail = sorted(rows, key=lambda row: row['ms'], reverse=True)[:math.ceil(len(rows) * .01)]
    timings = [row['ms'] for row in rows]
    counters = {}
    for key in rows[0]:
        if key in ('tick', 'ms'):
            continue
        values = [row[key] for row in rows]
        counters[key] = dict(total=sum(values), mean=mean(values), tailMean=mean(row[key] for row in tail),
                             correlationWithMs=correlation(values, timings))
    per_request = {}
    for prefix in ('shoppingWork.Estimates', 'shoppingRoutes', 'otherRoutes'):
        searches = counters[prefix + '.Searches']['total']
        requests = counters[prefix + '.Requests']['total']
        per_request[prefix] = dict(requests=requests, searches=searches,
            settledPerSearch=counters[prefix + '.Settled']['total'] / searches if searches else None,
            popsPerSearch=counters[prefix + '.Pops']['total'] / searches if searches else None)
    selection = 'shoppingWork.Selection.'
    calls = counters[selection + 'CandidateCalls']['total']
    per_request['selection'] = dict(calls=calls,
        cellsPerCall=counters[selection + 'CandidateCells']['total'] / calls if calls else None,
        linksPerCall=counters[selection + 'CandidateLinks']['total'] / calls if calls else None,
        queryReadsPerCandidate=sum(counters.get(selection + key, {'total': 0})['total']
            for key in ('CountCells', 'CandidateCells', 'PrefixReads', 'RebuiltCells')) / calls if calls else None)
    growth = [dict(tick=record['tick'], stepAllocatedBytes=record['allocated'], **change)
              for record in records if record.get('type') == 'work'
              for change in record.get('tableGrowth', [])]
    return dict(conditions=records[0], perRequest=per_request, tableGrowth=growth,
                samples=[r for r in records if 'MeanMs' in r],
                end=records[-1], ticks=len(rows), counters=counters, slowest=tail)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('capture', type=Path)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    result = summarize(args.capture)
    args.output.write_text(json.dumps(result, indent=2) + '\n')
    print('All-Tick mean | slowest 1% mean | Pearson r with instrumented Step ms | counter')
    for name, row in result['counters'].items():
        r = row['correlationWithMs']
        print(f"{row['mean']:12.2f} | {row['tailMean']:16.2f} | {r if r is not None else 'constant':>8} | {name}")

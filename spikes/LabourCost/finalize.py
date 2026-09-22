"""After successful captures, summarize host conditions and compress large evidence files."""
from pathlib import Path
import gzip
import hashlib
import json
import statistics

root = Path(__file__).resolve().parents[2]
out = root / 'plans/evidence/private-production-and-labour'
summary = [] if list(out.glob('*-host.jsonl')) else json.loads((out / 'host-summary.json').read_text())
for path in sorted(out.glob('*-host.jsonl')):
    rows = [json.loads(s) for s in path.read_text().splitlines()]
    if rows[-1].get('exitCode') != 0:
        raise SystemExit(f'Incomplete or failed capture: {path}')
    samples = [r for r in rows if 'cpu' in r]
    sibling, other, target = [], [], []
    for a, b in zip(samples, samples[1:]):
        def cpus(sample):
            return {line.split()[0]: list(map(int, line.split()[1:9])) for line in sample['cpu']}
        before, after = cpus(a), cpus(b)
        busy = []
        for cpu in range(12):
            delta = [y - x for x, y in zip(before[f'cpu{cpu}'], after[f'cpu{cpu}'])]
            ticks = sum(delta)
            busy.append(100 * (ticks - delta[3] - delta[4]) / ticks if ticks else 0)
        target.append(busy[2]); sibling.append(busy[8])
        other.append(statistics.mean(busy[i] for i in range(12) if i not in (2, 8)))
    freq = [int(r['cpu2_khz']) for r in samples]
    summary.append({'capture': path.stem, 'command': rows[0]['command'],
                    'startUtc': samples[0]['utc'], 'endUtc': samples[-1]['utc'],
                    'cpu2MeanBusyPercent': statistics.mean(target),
                    'sibling8MeanBusyPercent': statistics.mean(sibling),
                    'otherCpuMeanBusyPercent': statistics.mean(other),
                    'sampledFrequencyKhzMinMax': [min(freq), max(freq)],
                    'scope': 'entire process including setup; unweighted mean of roughly one-second intervals'})
    path.with_suffix('.jsonl.gz').write_bytes(gzip.compress(path.read_bytes(), mtime=0))
    path.unlink()
(out / 'host-summary.json').write_text(json.dumps(summary, indent=2) + '\n')
checkpoint = Path('/tmp/labour-aged.borough')
if checkpoint.exists():
    (out / 'aged-10000.borough.gz').write_bytes(gzip.compress(checkpoint.read_bytes(), mtime=0))
for path in out.glob('*.stderr.txt'):
    if not path.stat().st_size:
        path.unlink()
files = sorted(p for p in out.iterdir() if p.is_file() and p.name != 'SHA256SUMS')
(out / 'SHA256SUMS').write_text(''.join(f'{hashlib.sha256(p.read_bytes()).hexdigest()}  {p.name}\n' for p in files))
print(json.dumps(summary, indent=2))

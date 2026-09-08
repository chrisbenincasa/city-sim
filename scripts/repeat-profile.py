#!/usr/bin/env python3
"""Repeat a frozen Linux profile command with affinity and host-load evidence."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import statistics
import subprocess
import time

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('command', type=Path, help='JSON containing command and cwd')
parser.add_argument('--output', type=Path, required=True)
parser.add_argument('--cpu', type=int, required=True)
parser.add_argument('--repeats', type=int, default=4)
args = parser.parse_args()
if args.repeats < 2 or args.cpu not in os.sched_getaffinity(0):
    parser.error('Require at least two repeats and an allowed CPU.')
args.output.mkdir(parents=True, exist_ok=True)
if any(args.output.iterdir()):
    parser.error('Output must be empty.')
spec = json.loads(args.command.read_text())
command = ['taskset', '-c', str(args.cpu), *spec['command']]
cpu = Path(f'/sys/devices/system/cpu/cpu{args.cpu}')


def read(path):
    try:
        return Path(path).read_text().strip()
    except OSError:
        return None


def sample():
    return {'time': time.time(), 'stat': read('/proc/stat'),
            'loadavg': read('/proc/loadavg'),
            'frequency_khz': read(cpu / 'cpufreq/scaling_cur_freq')}


fingerprints = {}
for token in spec['command']:
    path = Path(spec['cwd']) / token
    if path.is_file():
        fingerprints[str(path)] = hashlib.sha256(path.read_bytes()).hexdigest()
        if path.suffix == '.dll':
            for assembly in path.parent.glob('Borough.*.dll'):
                fingerprints[str(assembly)] = hashlib.sha256(assembly.read_bytes()).hexdigest()
metadata = {'command': command, 'cwd': spec['cwd'], 'sha256': fingerprints,
            'siblings': read(cpu / 'topology/thread_siblings_list'),
            'policy': {key: read(cpu / 'cpufreq' / key) for key in
                       ('scaling_driver', 'scaling_governor', 'scaling_min_freq', 'scaling_max_freq')},
            'turbo_disabled': read('/sys/devices/system/cpu/intel_pstate/no_turbo'),
            'exception': 'Frequency policy is observed, not changed; host workloads are not isolated.'}
(args.output / 'conditions.json').write_text(json.dumps(metadata, indent=2))
rows = []
for index in range(args.repeats):
    stem = args.output / f'run-{index + 1}'
    with stem.with_suffix('.jsonl').open('w') as out, stem.with_suffix('.err').open('w') as err, \
            stem.with_suffix('.host.jsonl').open('w') as host:
        process = subprocess.Popen(command, cwd=spec['cwd'], stdout=out, stderr=err)
        try:
            while True:
                host.write(json.dumps(sample()) + '\n')
                host.flush()
                if process.poll() is not None:
                    break
                time.sleep(1)
        finally:
            if process.poll() is None:
                process.terminate()
                process.wait()
        if process.returncode:
            raise RuntimeError(f'Capture failed: {stem}')
    records = [json.loads(line) for line in stem.with_suffix('.jsonl').read_text().splitlines()]
    daily = [row for row in records if 'MeanMs' in row]
    end = next(row for row in records if row.get('type') == 'end')
    if len(daily) != 1 or end['invariants'] != 'passed':
        raise ValueError('Require one measured Day and passing invariants.')
    rows.append({'daily': daily[0], 'end': end})
    print(json.dumps({'run': index + 1, **daily[0], 'hash': end['hash']}), flush=True)
timing = ('MeanMs', 'P95Ms', 'P99Ms', 'MaxMs')
allocation = ('AllocatedBytes', 'Gen0Collections', 'Gen1Collections', 'Gen2Collections')
variable = {*timing, *allocation, 'MaxTick', 'ManagedHeapBytes', 'WorkingSetBytes'}
control = {key: value for key, value in rows[0]['daily'].items() if key not in variable}
equivalent = all(row['end'] == rows[0]['end'] and
                 {k: v for k, v in row['daily'].items() if k not in variable} == control for row in rows)
summary = {'equivalent_activity_hash': equivalent,
           'equal_allocations_collections': all(
               all(row['daily'][key] == rows[0]['daily'][key] for key in allocation) for row in rows),
           'runs': rows,
           'variation': {key: {'min': min(r['daily'][key] for r in rows),
                               'max': max(r['daily'][key] for r in rows),
                               'median': statistics.median(r['daily'][key] for r in rows)} for key in (*timing, *allocation)}}
(args.output / 'summary.json').write_text(json.dumps(summary, indent=2))
if not equivalent:
    raise ValueError('Activity or hash mismatch; inspect summary.json.')

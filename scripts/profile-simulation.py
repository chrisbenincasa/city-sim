#!/usr/bin/env python3
"""Attach dotnet-trace after ageing; build Borough.Headless in Release first."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import time

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--trace-tool', default='dotnet-trace')
parser.add_argument('--citizens', type=int, default=100000)
parser.add_argument('--seed', type=int, default=0)
parser.add_argument('--warmup-ticks', type=int, default=14336)
parser.add_argument('--ticks', type=int, default=2048)
parser.add_argument('--ruleset', default='rulesets/stress-shopping.toml')
parser.add_argument('--output', type=Path)
parser.add_argument('--binary', type=Path, help='Frozen profiling host; defaults to the local Release build')
parser.add_argument('--profile-load', type=Path, help='Resume an aged save; warmup-ticks remains absolute')
args = parser.parse_args()
if args.citizens < 1 or args.seed < 0 or args.warmup_ticks < 0 or args.ticks < 1:
    parser.error('Use positive population/Ticks and nonnegative seed/ageing.')
version = subprocess.check_output([args.trace_tool, '--version'], text=True).strip()
root = Path(__file__).resolve().parents[1]
output = (args.output or Path(tempfile.mkdtemp(prefix='borough-attribution-'))).resolve()
output.mkdir(parents=True, exist_ok=True)
if any(output.iterdir()):
    parser.error('Use an empty output directory to preserve previous captures.')
binary = (args.binary or root / 'src/Borough.Headless/bin/Release/net10.0/Borough.Headless.dll').resolve()
assemblies = binary.parent
fingerprints = {name: hashlib.sha256((assemblies / name).read_bytes()).hexdigest()
                for name in ['Borough.Headless.dll', 'Borough.Core.dll', 'Borough.Formats.dll']}
command = ['dotnet', str(binary),
           '--profile', '--ruleset', args.ruleset, '--citizens', str(args.citizens), '--seed', str(args.seed),
           '--warmup-ticks', str(args.warmup_ticks), '--ticks', str(args.ticks),
           '--no-decide-guard', '--profile-wait']
if args.profile_load:
    command.extend(['--profile-load', str(args.profile_load.resolve())])
trace_path = output / 'simulation.nettrace'
tracer = None
with (output / 'run.jsonl').open('w') as run_log, (output / 'errors.log').open('w') as errors, (output / 'trace.log').open('w') as trace_log:
    process = subprocess.Popen(command, cwd=root, stdin=subprocess.PIPE, stdout=run_log, stderr=errors, text=True)
    try:
        deadline = time.monotonic() + 3600
        last = None
        while True:
            lines = (output / 'run.jsonl').read_text().splitlines()
            records = [json.loads(line) for line in lines if line.endswith('}')]
            if records and records[-1] != last:
                last = records[-1]
                print(json.dumps(last), flush=True)
            if last and last.get('type') == 'ready':
                assert last['pid'] == process.pid and last['tick'] == args.warmup_ticks
                break
            if process.poll() is not None or time.monotonic() > deadline:
                raise RuntimeError(f'Run did not reach capture boundary; see {output}')
            time.sleep(.25)
        trace_command = [args.trace_tool, 'collect', '--process-id', str(process.pid),
                         '--profile', 'dotnet-sampled-thread-time,dotnet-common', '--output', str(trace_path)]
        (output / 'capture.json').write_text(json.dumps({'run': command, 'trace': trace_command,
            'tool_version': version, 'TMPDIR': os.environ.get('TMPDIR'), 'assembly_sha256': fingerprints,
            'measurement': 'managed stack attribution, not untraced Tick timing'}, indent=2))
        tracer = subprocess.Popen(trace_command, stdin=subprocess.PIPE, stdout=trace_log, stderr=subprocess.STDOUT, text=True)
        deadline = time.monotonic() + 30
        # Printed after StartTraceSessionAsync, including with redirected output.
        while f'Output File    : {trace_path}' not in (output / 'trace.log').read_text():
            if tracer.poll() is not None or time.monotonic() > deadline:
                raise RuntimeError(f'Trace did not attach; see {output / "trace.log"}')
            time.sleep(.1)
        process.stdin.write('go\n'); process.stdin.flush()
        print('Profiler attached; measuring the aged city.', flush=True)
        assert process.wait(timeout=3600) == 0, 'Simulation failed; inspect errors.log'
        assert tracer.wait(timeout=120) == 0, 'Trace collection failed; inspect trace.log'
    finally:
        for child in (process, tracer):
            if child is not None and child.poll() is None:
                child.terminate()
                try:
                    child.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    child.kill(); child.wait()
subprocess.run([args.trace_tool, 'convert', str(trace_path), '--format', 'Speedscope',
                '--output', str(output / 'simulation')], check=True)
for name, extra in [('self', []), ('inclusive', ['--inclusive'])]:
    with (output / (name + '.txt')).open('w') as report:
        subprocess.run([args.trace_tool, 'report', str(trace_path), 'topN', '-n', '40', *extra], stdout=report, check=True)
with (output / 'step.txt').open('w') as report:
    subprocess.run([sys.executable, str(root / 'scripts/summarize-simulation-profile.py'),
                    str(output / 'simulation.speedscope.json'), '--output', str(output / 'step.json')],
                   stdout=report, check=True)
print(f'Capture complete: {output}', flush=True)

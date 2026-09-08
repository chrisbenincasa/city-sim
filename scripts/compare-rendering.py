#!/usr/bin/env python3
"""Compare uncapped rendering throughput at a fixed Tick and camera; GPU time is separate."""
import argparse
import csv
import hashlib
import json
import os
from pathlib import Path
import socket
import statistics
import subprocess
import tempfile
import time

CASES = ['baseline', 'scale85', 'scale75', 'scale50', 'shadow4096', 'shadow-low',
         'shadows-off', 'ssao-off', 'foliage-off', 'foliage4000', 'foliage3000', 'foliage-shadows-off']
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--output', type=Path)
parser.add_argument('--chunk-metres', choices=[256, 512, 1024], type=int, default=256)
parser.add_argument('--cases', choices=CASES, nargs='+', default=['scale75', 'shadow4096', 'ssao-off', 'foliage3000'])
parser.add_argument('--seconds', type=int, default=8)
parser.add_argument('--citizens', type=int, default=1000)
args = parser.parse_args()
if args.seconds < 3 or args.citizens < 1:
    parser.error('Use at least 3 seconds and a positive population.')
root = Path(__file__).resolve().parents[1]
output = (args.output or Path(tempfile.mkdtemp(prefix='borough-render-comparison-'))).resolve()
output.mkdir(parents=True, exist_ok=True)
print(output, flush=True)
boot = output / 'boot.drive'
boot.write_text('422 pause\n')
results = []
with tempfile.TemporaryDirectory(prefix='borough-render-socket-') as folder:
    address = str(Path(folder) / 'control.sock')
    command = [os.environ.get('GODOT_BIN', 'godot'), '--path', str(root / 'src/Borough.Godot'),
               '--disable-vsync', '--max-fps', '0', '--', '--ruleset', 'rulesets/minimal.toml',
               '--citizens', str(args.citizens), '--start-at', '422', '--drive', str(boot), '--listen', address]
    env = dict(os.environ, BOROUGH_RENDER_PROFILE='1', BOROUGH_RENDER_CHUNK_METRES=str(args.chunk_metres),
               BOROUGH_PERFORMANCE_LOG=str(output / 'samples.csv'))
    assembly = root / 'src/Borough.Godot/.godot/mono/temp/bin/Debug/Borough.Godot.dll'
    manifest = {'command': command, 'chunk_metres': args.chunk_metres,
                'assembly_sha256': hashlib.sha256(assembly.read_bytes()).hexdigest(),
                'warmup_seconds': 4, 'sample_seconds': args.seconds, 'measurement': 'uncapped frame throughput'}
    with (output / 'game.log').open('w') as log:
        process = subprocess.Popen(command, cwd=root, env=env, stdout=log, stderr=subprocess.STDOUT)
        try:
            deadline = time.monotonic() + 60
            while not Path(address).exists():
                if process.poll() is not None or time.monotonic() > deadline:
                    raise RuntimeError(f'Game did not start; see {output / "game.log"}')
                time.sleep(.1)
            with socket.socket(socket.AF_UNIX) as connection:
                connection.settimeout(30)
                connection.connect(address)
                with connection.makefile('rwb', buffering=0) as wire:
                    def send(text):
                        wire.write((text + '\n').encode())
                        reply = wire.readline().decode()
                        if not reply.startswith('ok\t'):
                            raise RuntimeError(reply)

                    def samples():
                        with (output / 'samples.csv').open() as data:
                            return list(csv.DictReader(data))

                    send(f'ui read {output / "initial.json"}')
                    initial = json.loads((output / 'initial.json').read_text())
                    manifest['rendering'] = initial['Rendering']
                    assert initial['Rendering']['FrameLimit'] == 0
                    assert initial['Rendering']['Vsync'] == 'Disabled'
                    send('ui debug off')
                    send('ui point 64 64')
                    send('focus 136 104 608')
                    send('tilt 35')
                    sequence = ['baseline']
                    for case in args.cases:
                        sequence += [case, 'baseline']
                    manifest['sequence'] = sequence
                    (output / 'run.json').write_text(json.dumps(manifest, indent=2))
                    first = None
                    for index, case in enumerate(sequence):
                        send('ui render-probe ' + case)
                        time.sleep(4)
                        start = len(samples())
                        time.sleep(args.seconds)
                        rows = samples()[start + 1:]
                        assert rows and all(int(row['ticks']) == 0 and row['probe'] == case for row in rows)
                        name = f'{index:02}-{case}'
                        send(f'ui read {output / (name + ".json")}')
                        state = json.loads((output / (name + '.json')).read_text())
                        identity = [state[key] for key in ['Hash', 'Tick', 'Camera', 'Viewport']]
                        if first is None:
                            first = identity
                        assert identity == first, 'World, camera or viewport changed during comparison'
                        result = {'case': case, 'fps_median': statistics.median(float(row['fps']) for row in rows),
                                  'shell_ms_per_frame': statistics.median(float(row['shell_ms']) / int(row['frames']) for row in rows),
                                  'samples': rows}
                        results.append(result)
                        (output / 'results.json').write_text(json.dumps(results, indent=2))
                        print(f'{name}: {result["fps_median"]:.1f} FPS', flush=True)
                        send(f'shoot {output / (name + ".png")}')
                        time.sleep(.3)
                    send('ui render-probe baseline')
                    send('ui debug ' + ('on' if initial['Debug'] else 'off'))
                    send('quit')
            try:
                process.wait(timeout=10)
                assert process.returncode == 0, 'Godot exited with an error'
            except subprocess.TimeoutExpired:
                (output / 'shutdown-warning.txt').write_text('Godot acknowledged quit but stalled on exit; terminated after capture.\n')
        finally:
            if process.poll() is None:
                process.terminate()
                try:
                    process.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    process.kill()
                    process.wait()
assert 'ERROR:' not in (output / 'game.log').read_text(), 'Godot reported an error'
print('PASS: fixed world, camera and viewport. See run.json, results.json and captures.')

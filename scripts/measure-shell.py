#!/usr/bin/env python3
"""Capture running and paused shell counters; build Godot's loaded assembly first."""
import argparse
import csv
import json
import os
from pathlib import Path
import socket
import subprocess
import tempfile
import time

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--max-fps', type=int, help='Override the project limit; 0 removes the limit.')
parser.add_argument('--seconds', type=int, default=10, help='Seconds per running/paused phase, minimum 3.')
parser.add_argument('--output', type=Path)
parser.add_argument('--citizens', type=int, default=1000)
parser.add_argument('--ruleset', default='rulesets/minimal.toml')
parser.add_argument('--start-at', type=int, help='Age the city before opening the window; otherwise use the game default.')
parser.add_argument('--startup-timeout', type=int, default=60, help='Seconds allowed for ageing and opening the first frame.')
parser.add_argument('--focus', nargs=3, type=int, metavar=('EAST', 'NORTH', 'DISTANCE'), help='Fix the camera on this Tile at this distance.')
parser.add_argument('--headless', action='store_true', help='Use Godot without a display or GPU.')
args = parser.parse_args()
if (args.seconds < 3 or args.citizens < 1 or args.startup_timeout < 1
        or args.start_at is not None and args.start_at < 0
        or args.max_fps is not None and args.max_fps < 0):
    parser.error('Use at least 3 seconds, positive Citizens, and nonnegative start Tick and frame limit.')
root = Path(__file__).resolve().parents[1]
output = (args.output or Path(tempfile.mkdtemp(prefix='borough-performance-'))).resolve()
output.mkdir(parents=True, exist_ok=True)
print(output, flush=True)
with tempfile.TemporaryDirectory(prefix='borough-perf-socket-') as socket_dir:
    address = str(Path(socket_dir) / 'drive.sock')
    command = [os.environ.get('GODOT_BIN', 'godot'), '--path', str(root / 'src/Borough.Godot')]
    if args.headless:
        command += ['--headless']
    if args.max_fps is not None:
        command += ['--max-fps', str(args.max_fps)]
    command += ['--', '--ruleset', args.ruleset, '--citizens', str(args.citizens), '--listen', address]
    if args.start_at is not None:
        command += ['--start-at', str(args.start_at)]
    (output / 'run.json').write_text(json.dumps({'command': command, 'seconds_per_phase': args.seconds}, indent=2))
    env = dict(os.environ, BOROUGH_PERFORMANCE_LOG=str(output / 'samples.csv'), BOROUGH_RENDER_PROFILE='1')
    with (output / 'game.log').open('w') as log:
        process = subprocess.Popen(command, cwd=root, env=env, stdout=log, stderr=subprocess.STDOUT)
        try:
            deadline = time.monotonic() + args.startup_timeout
            while not Path(address).exists():
                if process.poll() is not None or time.monotonic() > deadline:
                    raise RuntimeError(f'Game did not start; see {output / "game.log"}')
                time.sleep(.1)
            with socket.socket(socket.AF_UNIX) as connection:
                connection.settimeout(args.startup_timeout)
                connection.connect(address)
                with connection.makefile('rwb', buffering=0) as wire:
                    def send(text):
                        wire.write((text + '\n').encode())
                        reply = wire.readline().decode()
                        if not reply.startswith('ok\t'):
                            raise RuntimeError(reply)
                    send('ui point 64 64')
                    connection.settimeout(60)
                    if args.focus is not None:
                        send('focus ' + ' '.join(map(str, args.focus)))
                    time.sleep(5)  # Exclude startup and shader compilation from the comparison.
                    time.sleep(args.seconds)
                    send('pause')
                    send(f'draw {output / "paused-before.tsv"}')
                    time.sleep(args.seconds)
                    send(f'draw {output / "paused-after.tsv"}')
                    send(f'ui read {output / "ui.json"}')
                    if not args.headless:
                        send(f'shoot {output / "screen.png"}')
                    time.sleep(.5)
                    send('quit')
            try:
                process.wait(timeout=20)
                if process.returncode != 0:
                    raise RuntimeError(f'Godot exited with {process.returncode}; see game.log')
            except subprocess.TimeoutExpired:
                warning = 'Godot acknowledged quit but stalled on exit; terminating the capture process.\n'
                (output / 'shutdown-warning.txt').write_text(warning)
                print(warning, end='', flush=True)
        finally:
            if process.poll() is None:
                process.terminate()
                try:
                    process.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    process.kill()
                    process.wait()

assert (output / 'paused-before.tsv').read_bytes() == (output / 'paused-after.tsv').read_bytes(), 'Paused drawing changed'
def uploads(name):
    return [line for line in (output / name).read_text().splitlines() if line.startswith('render\t')]
assert uploads('paused-before.tsv.profile.tsv') == uploads('paused-after.tsv.profile.tsv'), 'Paused scene uploaded geometry'
state = json.loads((output / 'ui.json').read_text())
assert any(label['Text'].endswith(' FPS') for label in state['Fonts']), 'FPS indicator missing'
with (output / 'samples.csv').open() as data:
    samples = list(csv.DictReader(data))
assert any(int(row['ticks']) > 0 for row in samples), 'No running samples'
assert all(int(row['ticks']) == 0 and float(row['step_ms']) == 0 for row in samples[-2:]), 'Simulation did work while paused'
assert all(float(row['process_cpu_ms']) >= 0 for row in samples), 'Invalid process CPU counter'
assert 'ERROR:' not in (output / 'game.log').read_text(), 'Godot reported an error'
print('PASS: FPS visible, paused simulation idle, drawing and uploads unchanged. Timings are in samples.csv.')

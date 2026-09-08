#!/usr/bin/env python3
"""Observe threaded shell ownership, paused edits, and replay; build Godot Debug first."""
import argparse
import csv
import json
import os
from pathlib import Path
import re
import socket
import subprocess
import tempfile
import time

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--output', type=Path, required=True)
parser.add_argument('--headless', action='store_true')
parser.add_argument('--main-thread-sim', action='store_true')
parser.add_argument('--route-workers', type=int, default=8, choices=range(1, 9))
parser.add_argument('--citizens', type=int, default=4000)
parser.add_argument('--start-at', type=int, default=512)
parser.add_argument('--seconds', type=int, default=8)
parser.add_argument('--require-busy-frames', action='store_true',
                    help='Use on a workload with Steps longer than a frame interval.')
args = parser.parse_args()
root = Path(__file__).resolve().parents[1]
out = args.output.resolve()
out.mkdir(parents=True, exist_ok=True)
boot = out / 'boot.drive'
boot.write_text(f'{args.start_at} pause\n')
with tempfile.TemporaryDirectory(prefix='borough-thread-') as temporary:
    address = str(Path(temporary) / 'shell.sock')
    command = ['godot', '--path', str(root / 'src/Borough.Godot')]
    if args.headless:
        command.append('--headless')
    command += ['--', '--ruleset', 'rulesets/stress-shopping.toml', '--citizens', str(args.citizens),
                '--start-at', str(args.start_at), '--route-workers', str(args.route_workers),
                '--drive', str(boot), '--listen', address, '--record', str(out / 'commands.drive')]
    if args.main_thread_sim:
        command.append('--main-thread-sim')
    (out / 'run.json').write_text(json.dumps({'command': command, 'seconds': args.seconds,
                                           'build': 'Godot Debug; functional observation, not a reference timing'}, indent=2))
    env = dict(os.environ, BOROUGH_LOG='1', BOROUGH_PERFORMANCE_LOG=str(out / 'frames.csv'), BOROUGH_SHOT='1')
    with (out / 'game.log').open('w') as log:
        process = subprocess.Popen(command, cwd=root, env=env, stdout=log, stderr=subprocess.STDOUT)
        try:
            deadline = time.monotonic() + 600
            while not Path(address).exists():
                if process.poll() is not None or time.monotonic() > deadline:
                    raise RuntimeError(f'Failed to start: {out / "game.log"}')
                time.sleep(.1)
            with socket.socket(socket.AF_UNIX) as connection:
                connection.settimeout(600)
                connection.connect(address)
                with connection.makefile('rwb', buffering=0) as wire:
                    def send(text):
                        wire.write((text + '\n').encode())
                        response = wire.readline().decode()
                        assert response.startswith('ok\t'), response
                        return int(response.split('\t')[1])

                    assert send('') == args.start_at
                    connection.settimeout(30)
                    send('focus 48 32 300')
                    send('hold look')
                    send('click 48 32')
                    send('hold street')
                    send('click 48 32 shift')
                    # A paused edit buys one Tick, with no resume and no subsequent Tick drift.
                    deadline = time.monotonic() + 20
                    while send('') == args.start_at:
                        assert time.monotonic() < deadline, 'Paused edit never applied'
                        time.sleep(.02)
                    time.sleep(.2)
                    assert send('') == args.start_at + 1
                    send('click 48 32')
                    deadline = time.monotonic() + 20
                    while send('') == args.start_at + 1:
                        assert time.monotonic() < deadline, 'Replacement never applied'
                        time.sleep(.02)
                    time.sleep(.2)
                    assert send('') == args.start_at + 2
                    send('hold look')
                    send('ui help on')
                    send('ui help off')
                    send('speed 8')
                    send('ui key E')
                    send('ui key L')
                    send('ui key L')
                    time.sleep(args.seconds)
                    tick = send('pause')
                    send('turn right')
                    send('zoom in 1')
                    send('click 48 32')
                    if args.headless:
                        send('ui help on')  # No physical pointer: hold edge scrolling during count checks.
                    time.sleep(.2)
                    send(f'draw {out / "paused-before.tsv"}')
                    time.sleep(1.2)
                    assert send('') == tick
                    send(f'draw {out / "paused-after.tsv"}')
                    send(f'ui read {out / "ui.json"}')
                    if not args.headless:
                        send(f'shoot {out / "screen.png"}')
                        time.sleep(.3)
                    send('quit')
            try:
                process.wait(timeout=30)
            except subprocess.TimeoutExpired:
                subprocess.run(['sample', str(process.pid), '1', '-file', str(out / 'exit-sample.txt')],
                               stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=10)
                raise
            assert process.returncode == 0, process.returncode
            assert not Path(address).exists(), 'Listener socket survived shutdown'
        finally:
            if process.poll() is None:
                process.terminate()
                try:
                    process.wait(timeout=10)
                except subprocess.TimeoutExpired:
                    process.kill()
                    process.wait()
log = (out / 'game.log').read_text()
assert 'ERROR' not in log and 'Exception' not in log, log
assert (out / 'paused-before.tsv').read_bytes() == (out / 'paused-after.tsv').read_bytes(), 'Paused geometry changed'
state_hash = re.search(r'State Hash 0x([0-9A-F]+)', log).group(1)
source = root / 'src/Borough.Godot' / f'session-{tick}.borough'
replay_log = out / 'session.borough'
replay_log.write_bytes(source.read_bytes())
replay = subprocess.run(['dotnet', str(root / 'src/Borough.Headless/bin/Release/net10.0/Borough.Headless.dll'),
                         '--log', str(replay_log), '--ruleset', 'rulesets/stress-shopping.toml',
                         '--ticks', str(tick), '--hash-every', str(tick), '--no-decide-guard'], cwd=root, text=True,
                        stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=600)
(out / 'replay.txt').write_text(replay.stdout)
assert replay.returncode == 0 and state_hash in replay.stdout, replay.stdout
with (out / 'frames.csv').open() as stream:
    frames = list(csv.DictReader(stream))
busy = sum(int(frame['busy_frames']) for frame in frames)
assert all(frame['threaded'] == str(not args.main_thread_sim) for frame in frames)
assert sum(int(frame['ticks']) for frame in frames) > 0
if args.main_thread_sim:
    assert busy == 0
if args.require_busy_frames:
    assert busy > 0, 'No frame observed while Step was still running'
summary = {'tick': tick, 'hash': state_hash, 'paused_edits': 2,
           'busy_frames': busy, 'replay': 'equal', 'paused_drawing': 'equal', 'clean_shutdown': True}
(out / 'checks.json').write_text(json.dumps(summary, indent=2))
print(json.dumps(summary))

#!/usr/bin/env python3
"""Check paused and running shell performance on a displayed, populated city.

Launch Godot with --listen SOCKET and BOROUGH_PERFORMANCE_LOG=CSV. Supply
local regression ceilings explicitly; these are not simulation budget readings.
Run without competing builds/tests. Repeat with small and large populations.
"""
import argparse
import csv
import json
from pathlib import Path
import socket
import statistics
import tempfile
import time

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('socket')
parser.add_argument('csv', type=Path)
parser.add_argument('--max-shell-ms', type=float, required=True)
parser.add_argument('--min-fps', type=float, required=True)
parser.add_argument('--sample-seconds', type=float, default=20)
args = parser.parse_args()
if args.sample_seconds < 5:
    parser.error('--sample-seconds must be at least 5')

with socket.socket(socket.AF_UNIX) as connection, tempfile.TemporaryDirectory() as temporary:
    connection.settimeout(30)
    connection.connect(args.socket)
    wire = connection.makefile('rwb', buffering=0)

    def command(text):
        wire.write((text + '\n').encode())
        reply = wire.readline()
        assert reply.startswith(b'ok\t'), reply
        assert b'REFUSED' not in reply, reply

    def read():
        path = Path(temporary) / 'state.json'
        command('ui read ' + str(path))
        return json.loads(path.read_text())

    def samples():
        with args.csv.open() as source:
            return list(csv.DictReader(source))

    def measure(label):
        # Exclude the partially accumulated sample and UI snapshot costs.
        start = len(samples()) + 1
        time.sleep(args.sample_seconds)
        measured = samples()[start:]
        assert len(measured) >= 3, 'Not enough complete performance samples.'
        shell = [float(row['shell_ms']) / int(row['frames']) for row in measured]
        fps = [float(row['fps']) for row in measured]
        print(f'{label}: shell median {statistics.median(shell):.2f} ms/frame; '
              f'FPS median {statistics.median(fps):.1f}, minimum {min(fps):.1f}', flush=True)
        assert max(shell) < args.max_shell_ms, (label, shell)
        assert min(fps) >= args.min_fps, (label, fps)
        return measured

    command('pause')
    command('ui close')
    command('hold look')
    command('ui text-size 100')
    command('ui pointer move 720 480')
    time.sleep(1)
    initial = read()
    measure('Paused')
    paused = read()
    assert paused['Hash'] == initial['Hash']
    assert 'SupplyMarks' not in paused
    assert all(button['Icon'] != 'trouble' for button in paused['Buttons'])
    command('speed 5')
    running = measure('Running')
    assert sum(int(row['ticks']) for row in running) > 0
    assert read()['Tick'] > initial['Tick']
    print('PASS: no floating supply buttons, unchanged paused city, and both frame ceilings.')

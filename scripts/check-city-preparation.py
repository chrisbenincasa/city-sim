#!/usr/bin/env python3
"""Capture progressing startup and cancel it through the Godot socket; build Godot Debug first."""
import argparse
from pathlib import Path
import socket
import subprocess
import tempfile
import time

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--output', type=Path, required=True)
parser.add_argument('--headless', action='store_true')
args = parser.parse_args()
root = Path(__file__).resolve().parents[1]
out = args.output.resolve()
out.mkdir(parents=True, exist_ok=True)
with tempfile.TemporaryDirectory(prefix='borough-prepare-') as folder:
    address = str(Path(folder) / 'shell.sock')
    command = ['godot', '--path', str(root / 'src/Borough.Godot')]
    if args.headless:
        command.append('--headless')
    command += ['--', '--ruleset', 'rulesets/stress-shopping.toml', '--citizens', '16000',
                '--start-at', '1000000', '--route-workers', '8', '--listen', address]
    (out / 'command.txt').write_text(repr(command))
    with (out / 'game.log').open('w') as log:
        process = subprocess.Popen(command, cwd=root, stdout=log, stderr=subprocess.STDOUT)
        try:
            deadline = time.monotonic() + 30
            while not Path(address).exists():
                assert process.poll() is None and time.monotonic() < deadline
                time.sleep(.05)
            with socket.socket(socket.AF_UNIX) as client:
                client.settimeout(15)
                client.connect(address)
                with client.makefile('rwb', buffering=0) as wire:
                    def send(line):
                        wire.write((line + '\n').encode())
                        reply = wire.readline().decode()
                        assert reply.startswith('ok\t0\tpreparing'), reply
                        return reply
                    first = send(f'shoot {out / "loading-first.png"}')
                    time.sleep(2)
                    second = send(f'shoot {out / "loading-later.png"}')
                    assert first != second, 'Loading progress did not change'
                    send('quit')
            assert process.wait(timeout=30) == 0
            assert not Path(address).exists()
        finally:
            if process.poll() is None:
                process.terminate()
                process.wait(timeout=30)
    assert 'ERROR' not in (out / 'game.log').read_text()
    (out / 'checks.txt').write_text('Progress changed while Godot answered captures; cancellation exited normally.\n' + first + second)
    print((out / 'checks.txt').read_text())

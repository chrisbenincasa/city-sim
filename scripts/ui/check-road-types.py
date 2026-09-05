#!/usr/bin/env python3
"""Check a paused severance fixture (2,000 Citizens) through its --listen socket."""
import json
from pathlib import Path
import socket
import sys
import time

output = Path(__file__).resolve().parents[2] / 'artifacts/hud-live'
connection = socket.socket(socket.AF_UNIX)
connection.settimeout(20)
connection.connect(sys.argv[1])
wire = connection.makefile('rwb', buffering=0)


def command(text):
    wire.write((text + '\n').encode())
    response = wire.readline().decode()
    assert response.startswith('ok\t'), response
    time.sleep(.15)


def read(name):
    path = output / (name + '.json')
    command('ui read ' + str(path))
    return json.loads(path.read_text())


command('pause')
command('ui close')
initial = read('road-types-initial')
for kind in ['Foot Path', 'Arterial']:
    target = next(t for t in initial['MapTargets'] if t['Name'] == kind)
    command('ui road ' + str(target['Id']))
    state = read('road-type-' + kind.replace(' ', '-'))
    if kind == 'Foot Path':
        assert 'Walking only' in state['Text'] and 'Walking:' in state['Text'] and 'Driving:' not in state['Text']
    else:
        assert 'Driving only' in state['Text'] and 'Driving:' in state['Text'] and 'Walking:' not in state['Text']
    assert state['Hash'] == initial['Hash']
print('PASS: Foot Path and Arterial permissions and travel conditions, unchanged paused State Hash.')
wire.close()
connection.close()

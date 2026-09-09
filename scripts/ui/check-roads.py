#!/usr/bin/env python3
"""Road inspection in the paused diagnosed fixture (256 Citizens, Tick 512)."""
import json
from pathlib import Path
import socket
import sys
import time

output = Path(__file__).resolve().parents[2] / 'artifacts/hud-live'
output.mkdir(parents=True, exist_ok=True)
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


def press(state, label):
    r = next(b['Rect'] for b in state['Buttons'] if b['Text'] == label)
    command(f"ui press {round(r['X']+r['Width']/2)} {round(r['Y']+r['Height']/2)}")


for cmd in ['pause', 'hold look', 'ui close', 'ui size 1440 960', 'ui debug off', 'focus 80 32 250', 'click 80 32', 'ui point 80 32']:
    command(cmd)
road = read('road-dark')
assert road['Road'] == 6 and road['Selected'] == 0 and 'road Segment' in road['Synopsis']
assert '0 Vehicles present' in road['Text'] and '9.2 s on next entry' in road['Text']
assert 'Walking:' in road['Text'] and '3 frontage Lots · 0 vacant' in road['Text']
assert 'A · Tile 64, 32' in road['Text'] and 'B · Tile 96, 32' in road['Text']
initial_hash = road['Hash']
command('shoot artifacts/hud-live/road-dark.png')
# Actual viewport clicks use the perspective ray, rather than the driven Tile path.
for kind, identity, selected in [('road', 6, 'Road'), ('building', 1, 'Selected')]:
    command('ui close')
    state = read('road-map-projection')
    target = next(t for t in state['MapTargets'] if t['Kind'] == kind and t['Id'] == identity)
    command(f"ui map-press {round(target['X'])} {round(target['Y'])}")
    hit = read('road-screen-' + kind)
    assert hit[selected] == identity, (target, hit['Selected'], hit['Road'])
# Empty ground no longer selects a nearby Address in the same Cell.
command('click 80 50')
empty = read('road-ground')
assert empty['Road'] == 0 and empty['Selected'] == 0 and 'Open ground' in empty['Text']
command('click 80 32')
command('ui section travel off')
command('ui section frontage on')
parent = read('road-frontage')
press(parent, 'Building 1 · dwelling')
building = read('road-building')
assert building['Selected'] == 1 and building['Road'] == 0
command('ui section households on')
press(read('road-households'), 'Household 2 →')
assert read('road-household')['Household'] == 2
press(read('road-household-back'), '‹ Building 1')
press(read('road-building-back'), '‹ Road Segment 6')
back = read('road-back')
assert back['Road'] == 6 and back['Scroll'] == parent['Scroll']
assert all(back['Expanded'].get(k) == v for k, v in parent['Expanded'].items())
for width, height in [(1280, 800), (1440, 960), (1920, 1080)]:
    command(f'ui size {width} {height}')
    for theme in ['light', 'dark']:
        command('ui theme ' + theme)
        command('ui debug on')
        state = read(f'road-{theme}-{width}')
        assert state['Hash'] == initial_hash and state['Road'] == 6
        for key in ['Inspector', 'Hover', 'DebugPanel']:
            r = state[key]
            assert r['X'] >= 0 and r['Y'] >= 0 and r['X'] + r['Width'] <= width + 1 and r['Y'] + r['Height'] <= height + 1, (key, r)
        command(f'shoot artifacts/hud-live/road-{theme}-{width}.png')
command('ui size 1440 960')
command('ui section frontage off')
command('ui section connections on')
press(read('road-connections'), 'Street 5 · Walking and driving')
assert read('road-connected')['Road'] == 5
press(read('road-before-close'), '×')
closed = read('road-closed')
assert not closed['InspectorVisible'] and closed['Road'] == 0 and closed['Hash'] == initial_hash
print('PASS: road and Building screen picking, empty ground, live travel/frontage facts, nested breadcrumbs, connection links, both themes, narrow layouts, close, unchanged paused State Hash.')
wire.close()
connection.close()

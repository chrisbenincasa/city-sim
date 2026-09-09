#!/usr/bin/env python3
"""Exercise a paused --listen shell running the diagnosed 256-Citizen fixture at Tick 512."""
import json
from pathlib import Path
import socket
import sys
import time

root = Path(__file__).resolve().parents[2]
output = root / 'artifacts/hud-live'
output.mkdir(parents=True, exist_ok=True)
connection = socket.socket(socket.AF_UNIX)
connection.settimeout(15)
connection.connect(sys.argv[1] if len(sys.argv) > 1 else '/private/tmp/borough-hud.sock')
wire = connection.makefile('rwb', buffering=0)


def command(text):
    wire.write((text + '\n').encode())
    response = wire.readline().decode()
    assert response.startswith('ok\t'), response
    time.sleep(.10)  # Let Godot's deferred container layout finish before reading rectangles.
    return response


def read(name):
    path = output / (name + '.json')
    command('ui read ' + str(path))
    return json.loads(path.read_text())


def press(state, label):
    button = next(b for b in state['Buttons'] if b['Text'] == label)
    r = button['Rect']
    command(f"ui press {round(r['X'] + r['Width']/2)} {round(r['Y'] + r['Height']/2)}")


def bounded(state):
    width, height = state['Viewport']['Width'], state['Viewport']['Height']
    for name in ['Inspector', 'Hover'] + (['DebugPanel'] if state['Debug'] else []):
        r = state[name]
        assert r['X'] >= 0 and r['Y'] >= 0, (name, r)
        assert r['X'] + r['Width'] <= width + 1, (name, r, width)
        assert r['Y'] + r['Height'] <= height + 1, (name, r, height)
    a, b = state['Inspector'], state['Hover']
    assert (a['X'] + a['Width'] <= b['X'] or b['X'] + b['Width'] <= a['X']
            or a['Y'] + a['Height'] <= b['Y'] or b['Y'] + b['Height'] <= a['Y']), (a, b)
    close = next(b['Rect'] for b in state['Buttons'] if b['Text'] == '×')
    assert close['Y'] + close['Height'] <= height and close['Width'] >= 36


command('pause')
command('ui size 1440 960')
command('ui theme dark')
command('ui debug off')
command('hold look')
command('click 68 36')
command('ui point 90 36')
building = read('checked-building-dark')
assert building['Selected'] == 1 and building['Household'] == 0
assert 'Waiting for repairs' in building['Text']
assert 'Waiting for space for sundries' in building['Text']
assert 'Household 2' in building['Text']
assert '1 Household · 1 Business' in building['Synopsis']  # Hover is a different Building.
initial_hash = building['Hash']
bounded(building)
command('shoot artifacts/hud-live/checked-building-dark.png')
command('ui section households on')
command('ui scroll 80')
parent = read('parent-position')
press(parent, 'Household 2 →')
command('ui section stocks on')
command('ui section activities on')
command('ui section finances on')
command('ui scroll 75')
household = read('household-position')
assert household['Household'] == 2 and '48 / 48 units' in household['Text']
assert household['Scroll'] > 0, 'Scroll preservation must exercise a scrolled panel'
command('ui theme light')
theme = read('checked-household-light')
assert theme['Scroll'] == household['Scroll'] and theme['Expanded'] == household['Expanded']
command('shoot artifacts/hud-live/checked-household-light.png')
for width, height in [(1280, 800), (1920, 1080)]:
    command(f'ui size {width} {height}')
    command('ui debug on')
    state = read(f'checked-{width}-{height}')
    bounded(state)
    assert state['Household'] == 2 and state['Expanded'] == household['Expanded']
    assert state['Hash'] == initial_hash and state['Tick'] == building['Tick']
    command(f'shoot artifacts/hud-live/checked-{width}-{height}.png')
    command('ui theme dark')
    dark = read(f'checked-dark-{width}-{height}')
    assert dark['Inspector'] == state['Inspector'] and dark['Hover'] == state['Hover']
    assert dark['Scroll'] == state['Scroll'] and dark['Hash'] == initial_hash
    command(f'shoot artifacts/hud-live/checked-dark-{width}-{height}.png')
    command('ui theme light')
command('ui size 1440 960')
command('ui debug off')
state = read('before-back')
press(state, '‹ Building 1')
back = read('after-back')
assert back['Household'] == 0 and back['Scroll'] == parent['Scroll']
assert all(back['Expanded'].get(k) == v for k, v in parent['Expanded'].items())
# A panel click is tested while a destructive tool is held: UI input must be consumed.
command('hold demolish')
press(back, '×')
closed = read('after-mouse-close')
assert not closed['InspectorVisible'] and closed['Selected'] == 0
assert closed['Hash'] == initial_hash and closed['Tick'] == building['Tick']
assert closed['Synopsis'] == building['Synopsis']
command('hold look')
command('click 68 36')
command('ui point 90 36')
command('ui theme dark')
command('ui size 1440 960')
print('PASS: live Evidence, independent hover, theme/resize preservation, bounds at 4 sizes, breadcrumb restoration, mouse close, and unchanged paused State Hash.')
wire.close()
connection.close()

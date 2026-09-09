#!/usr/bin/env python3
"""Circular camera controls and the cached, clickable road minimap in a paused city."""
import json
from pathlib import Path
import socket
import sys
import time

out = Path(__file__).resolve().parents[2] / 'artifacts/hud-live'
out.mkdir(parents=True, exist_ok=True)
s = socket.socket(socket.AF_UNIX)
s.settimeout(30)
s.connect(sys.argv[1])
f = s.makefile('rwb', buffering=0)

def command(text):
    f.write((text + '\n').encode())
    reply = f.readline()
    assert reply.startswith(b'ok\t'), reply
    time.sleep(.15)

def read():
    path = out / 'navigation-state.json'
    command('ui read ' + str(path))
    return json.loads(path.read_text())

def press(label):
    r = next(b['Rect'] for b in read()['Buttons'] if b['Text'] == label)
    command(f"ui press {round(r['X'] + r['Width']/2)} {round(r['Y'] + r['Height']/2)}")

def identity(state):
    return tuple(state[k] for k in ['Hash', 'Tool', 'Layer', 'Selected', 'Household', 'Business'])

for cmd in ['pause', 'ui text-size 100', 'ui size 1440 960', 'ui tools off', 'ui layers off',
            'ui building 2', 'hold zone 0', 'overlay value', 'focus 48 48 650', 'ui pointer move 720 480']:
    command(cmd)
state = read()
original = identity(state)
builds = state['MiniMap']['Rebuilds']
assert state['MiniMap']['RoadCount'] > 0
for theme in ['dark', 'light']:
    command('ui theme ' + theme)
    for scale in [100, 150]:
        command('ui text-size ' + str(scale))
        for label, field, sign in [('↶', 'Yaw', -1), ('↷', 'Yaw', 1), ('↑', 'Pitch', 1),
                                   ('↓', 'Pitch', -1), ('+', 'Distance', -1), ('−', 'Distance', 1)]:
            before = read()['Camera'][field]
            press(label)
            state = read()
            assert (state['Camera'][field] - before) * sign > 0, label
            assert identity(state) == original
            assert state['MiniMap']['Rebuilds'] == builds
        state = read()
        mini = state['MiniMap']; r = mini['Rect']
        # Use a rounded, actual viewport pixel and derive its expected Tile from the displayed bounds.
        x, y = round(r['X'] + r['Width'] * .7), round(r['Y'] + r['Height'] * .3)
        east = max(0, round(mini['East'] + (x-r['X']) / r['Width'] * mini['Width']))
        north = max(0, round(mini['North'] + (1-(y-r['Y']) / r['Height']) * mini['Height']))
        distance = state['Camera']['Distance']
        command(f'ui press {x} {y}')
        state = read()
        assert abs(state['Camera']['Focus']['X'] - east * 4) <= 4
        assert abs(state['Camera']['Focus']['Z'] + north * 4) <= 4
        assert state['Camera']['Distance'] == distance and identity(state) == original
        assert not state['Zoning']['Dragging']
        assert state['MiniMap']['Rebuilds'] == builds
        command('focus 48 48 650')
        command(f'shoot {out}/navigation-{theme}-{scale}.png')
# A road edit must invalidate the cached drawing, including when a recycled slot is involved.
command('ui close'); command('ui text-size 100'); command('hold street')
state = read()
road = next(p for p in state['MapTargets'] if p['Kind'] == 'road')
count = state['MiniMap']['RoadCount']
command(f"click {road['East']} {road['North']} shift")
command('speed 8'); time.sleep(.5); command('pause')
state = read()
assert state['MiniMap']['RoadCount'] < count, state['Refused']
assert state['MiniMap']['Rebuilds'] > builds
command('hold look')
print('PASS: all circular controls, themes/scales, north-up minimap click-to-pan, tool/inspection preservation, cached drawing and road-edit invalidation.')

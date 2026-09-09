#!/usr/bin/env python3
"""Study 03 acceptance, on diagnosed.toml with 256 Citizens through --listen."""
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

def read(name='foundation-state'):
    path = out / (name + '.json')
    previous = None
    for _ in range(20):
        command('ui read ' + str(path))
        state = json.loads(path.read_text())
        layout = [(b['Text'], b['Rect']) for b in state['Buttons']]
        if layout == previous:
            return state
        previous = layout
    raise AssertionError('Layout did not settle')

def press(label):
    state = read()
    b = next(b for b in state['Buttons'] if b['Text'] == label)
    assert not b['Disabled'], label
    r = b['Rect']
    command(f"ui press {round(r['X'] + r['Width']/2)} {round(r['Y'] + r['Height']/2)}")

def clear(a, b):
    return (a['X'] + a['Width'] <= b['X'] + 1 or b['X'] + b['Width'] <= a['X'] + 1
            or a['Y'] + a['Height'] <= b['Y'] + 1 or b['Y'] + b['Height'] <= a['Y'] + 1)

def bounded(r, w, h):
    assert r['X'] >= 0 and r['Y'] >= 0, r
    assert r['X'] + r['Width'] <= w + 1 and r['Y'] + r['Height'] <= h + 1, (r, w, h)

def identity(state):
    return tuple(state[k] for k in ['Selected', 'Household', 'Business', 'Road', 'Tool', 'Layer', 'Hash'])

for cmd in ['pause', 'ui text-size 100', 'ui size 1440 960', 'ui debug off', 'ui tools off',
            'ui layers off', 'hold look', 'ui building 2']:
    command(cmd)
press('Zoning'); press('Housing')
state = read()
assert state['Tool'] == 'Zone' and not state['ToolsVisible']
press('Whole blocks')
assert read()['Zoning']['Unit'] == 'blocks'
press('Land value ↗')
state = read()
assert state['Layer'] == 'Value' and state['LegendVisible'] and not state['LayersShown']
held = identity(state)
press('Zoning'); press('Close')
assert identity(read()) == held
press('Maps'); press('Close Maps')
assert identity(read()) == held and read()['LegendVisible']
press('Government')
state = read()
assert state['PoliciesVisible'] and identity(state) == held
press('Close Government')
assert identity(read()) == held
press('Maps'); command('ui key Escape')
assert not read()['LayersShown'] and identity(read()) == held
state = read(); distance = state['Camera']['Distance']
press('+'); assert read()['Camera']['Distance'] < distance
assert read()['CameraShown']
assert not any(b['Text'] == 'Camera' for b in read()['Buttons'])
for theme in ['light', 'dark']:
    command('ui theme ' + theme)
    for width, height in [(1280, 800), (1440, 960), (1920, 1080)]:
        for scale in [100, 150]:
            command(f'ui size {width} {height}'); command(f'ui text-size {scale}')
            command(f'ui pointer move {width//2} {height//2}')
            command('focus 48 48 650')
            command('ui tools on'); command('ui layers on')
            state = read(f'foundation-{theme}-{width}-{scale}')
            for name in ['PlacementRail', 'Tools', 'Layers', 'LegendPanel', 'Inspector', 'Console', 'CameraPanel', 'Ruler']:
                bounded(state[name], width, height)
            for a, b in [('Tools', 'Layers'), ('Tools', 'Inspector'), ('Layers', 'Inspector'),
                         ('Layers', 'LegendPanel'), ('Console', 'Inspector'), ('Console', 'LegendPanel'), ('CameraPanel', 'Console'),
                         ('CameraPanel', 'Ruler'), ('CameraPanel', 'LegendPanel'), ('Ruler', 'LegendPanel')]:
                assert clear(state[a], state[b]), (a, b, state[a], state[b])
            assert state['CameraPanel']['X'] == state['Console']['X']
            assert state['CameraPanel']['Y'] < state['Console']['Y']
            assert state['Ruler']['X'] >= state['CameraPanel']['X'] + state['CameraPanel']['Width']
            assert state['ConsoleScroll'] == 0
            assert state['Console']['Height'] <= 240, state['Console']
            assert identity(state) == held
            press('Government'); state = read()
            assert state['PoliciesVisible'] and identity(state) == held
            bounded(state['Policies'], width, height)
            assert clear(state['Policies'], state['Console'])
            assert clear(state['Policies'], state['Inspector'])
            press('Close Government')
            command('shoot ' + str(out / f'foundation-{theme}-{width}-{scale}.png'))
            press('Close Maps'); press('Close')
            press('Cancel'); assert read()['Tool'] == 'Look'
            press('Zoning'); press('Housing')
            assert read()['Tool'] == 'Zone' and not read()['ToolsVisible']
command('ui text-size 100'); command('ui size 1440 960')
command('ui close'); command('ui key Escape')
assert read()['Tool'] == 'Look'
print('PASS: placement, contextual data, independent Government/Maps/inspection, persistent legend, camera, cancellation and desktop matrix.')

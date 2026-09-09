#!/usr/bin/env python3
"""Exercise row 8 through viewport input, retaining captures for visual review."""
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
    time.sleep(.18)


def read(name='discovery-state'):
    path = output / (name + '.json')
    command('ui read ' + str(path))
    return json.loads(path.read_text())


def click(rect):
    command(f"ui press {round(rect['X'] + rect['Width']/2)} {round(rect['Y'] + rect['Height']/2)}")


def press(state, label):
    click(next(b['Rect'] for b in state['Buttons'] if b['Text'] == label))


def bounded(rect, width, height):
    assert rect['X'] >= 0 and rect['Y'] >= 0, rect
    assert rect['X'] + rect['Width'] <= width + 1, rect
    assert rect['Y'] + rect['Height'] <= height + 1, rect


if len(sys.argv) == 3:
    assert read()['TextPercent'] == int(sys.argv[2]), read()['TextPercent']
    command('ui text-size 100')
    print('PASS: text-size preference survives restarting Godot.')
    sys.exit(0)

command('pause')
command('focus 80 48 350')
command('ui debug off')
command('ui layers off')
command('ui tools off')
command('ui help off')
command('ui text-size 100')
command('ui size 1440 960')
command('hold look')
command('click 68 36')
state = read()
selected, original_hash = state['Selected'], state['Hash']
assert selected
for label, field, direction in [('↶', 'Yaw', -1), ('↷', 'Yaw', 1),
                                 ('↑', 'Pitch', 1), ('↓', 'Pitch', -1),
                                 ('+', 'Distance', -1), ('−', 'Distance', 1)]:
    before = state['Camera'][field]
    press(state, label)
    state = read()
    assert (state['Camera'][field] - before) * direction > 0, (label, before, state['Camera'])
    assert state['Selected'] == selected and state['Hash'] == original_hash

for theme in ['light', 'dark']:
    command('ui theme ' + theme)
    for width, height in [(1440, 960), (1280, 800)]:
        command(f'ui size {width} {height}')
        command('ui text-size 100')
        state = read()
        press(state, 'Settings')
        state = read()
        assert state['SettingsVisible'] and state['Pace'] == 'paused'
        before = next(f['Size'] for f in state['Fonts'] if f['Text'] == 'Settings')
        press(state, 'A+')
        state = read()
        assert state['TextPercent'] == 110
        assert next(f['Size'] for f in state['Fonts'] if f['Text'] == 'Settings') > before
        command('ui text-size 150')
        state = read(f'discovery-help-{theme}-{width}')
        bounded(state['Settings'], width, height)
        for label in ['A−', 'A+', 'Reset size']:
            bounded(next(b['Rect'] for b in state['Buttons'] if b['Text'] == label), width, height)
        press(state, 'Help & shortcuts')
        state = read()
        assert state['HelpVisible']
        bounded(state['Help'], width, height)
        command(f'shoot {output}/discovery-help-{theme}-{width}.png')
        area = state['HelpContent']
        before_scroll = state['HelpScroll']
        command(f"ui wheel {round(area['X'] + area['Width']/2)} {round(area['Y'] + area['Height']/2)} 8")
        state = read()
        assert state['HelpScroll'] > before_scroll, state['HelpScroll']
        command(f"ui wheel {round(area['X'] + area['Width']/2)} {round(area['Y'] + area['Height']/2)} -20")
        # A map click outside Help is swallowed even with a destructive tool selected.
        command('hold demolish')
        command('ui press 1 1')
        assert read()['Selected'] == selected
        command('ui key Escape')
        state = read()
        assert not state['HelpVisible'] and state['InspectorVisible'] and state['Tool'] == 'Demolish'
        assert state['Hash'] == original_hash
        command('ui key Slash shift')
        assert read()['HelpVisible']
        press(read(), 'Close Help ×')
        command('hold look')
        state = read(f'discovery-console-{theme}-{width}')
        bounded(state['Console'], width, height)
        assert state['Inspector']['Y'] + state['Inspector']['Height'] <= state['Console']['Y'] + 1
        command(f'shoot {output}/discovery-console-{theme}-{width}.png')

# Every control stays reachable in the smallest design frame at maximum type size, with both
# panels open and a wash on. The console used to be scrolled to reach Cancel here; at 1280 x 800 it
# fits, so what is asserted is that nothing needs scrolling rather than that scrolling works.
command('ui size 1280 800')
command('ui text-size 150')
command('ui tools on')
command('ui layers on')
command('overlay pollution')
command('hold demolish')
state = read('discovery-expanded-console')
bounded(state['Console'], 1280, 800)
bounded(state['Tools'], 1280, 800)
bounded(state['Layers'], 1280, 800)
assert state['Inspector']['Y'] + state['Inspector']['Height'] <= state['Console']['Y'] + 1
assert state['Layers']['Y'] >= state['Tools']['Y'] + state['Tools']['Height'] - 1, (state['Tools'], state['Layers'])
assert state['CameraPanel']['X'] == state['Console']['X']
assert state['CameraPanel']['Y'] < state['Console']['Y']
assert state['Ruler']['X'] >= state['CameraPanel']['X'] + state['CameraPanel']['Width']
assert state['ConsoleScroll'] == 0, state['ConsoleScroll']
area = state['ConsoleContent']
cancel = next(b['Rect'] for b in state['Buttons'] if b['Text'] == 'Cancel')
assert cancel['Y'] >= area['Y'] and cancel['Y'] + cancel['Height'] <= area['Y'] + area['Height'], (cancel, area)
press(state, 'Cancel')
assert read()['Tool'] == 'Look'
command('ui tools off')
command('ui layers off')
command('overlay off')

# Focused editable text must not rotate the camera, arm a tool or open Help.
command('ui text-size 100')
command('ui key Tab')
state = read()
assert state['TunerVisible'] and state['Inputs']
click(state['Inputs'][0]['Rect'])
before = read()
for key in ['Q', 'B', 'Slash shift']:
    command('ui key ' + key)
state = read()
assert state['Camera'] == before['Camera'] and state['Tool'] == before['Tool']
assert not state['HelpVisible'] and any(f['Focused'] for f in state['Inputs'])
command('ui key Escape')
state = read()
assert not state['TunerVisible'] and state['InspectorVisible']
command('ui key Escape')
assert not read()['InspectorVisible']
command('hold demolish')
command('ui key Escape')
assert read()['Tool'] == 'Look'
command('ui key Question')
assert read()['HelpVisible']
press(read(), 'Close Help ×')
assert read()['Hash'] == original_hash
command('resume')
state = read()
pace, tick = state['Pace'], state['Tick']
press(state, 'Settings')
press(read(), 'Help & shortcuts')
state = read()
assert state['HelpVisible'] and state['Pace'] == pace and state['Tick'] > tick
press(state, 'Close Help ×')
assert read()['Pace'] == pace
command('pause')
command('ui text-size 110')
print('PASS: camera buttons, shared sizing, both themes and widths, Help mouse shielding, keyboard discovery, focus and Escape order; paused city unchanged.')

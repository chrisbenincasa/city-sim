#!/usr/bin/env python3
"""Check the everyday console through a --listen socket: pace, layers, tools, refusals.

plans/0064 rows 4, 5 and 6 share one acceptance check -- pause, change speed, choose a map layer,
inspect a subject and cancel an editing tool ENTIRELY WITH THE MOUSE, in both themes and at narrow
sizes. Every assertion here presses a real button rectangle through `ui press`, so a control that is
drawn but unreachable fails exactly as a control that is missing does.
"""
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


def press(state, label):
    button = next(b for b in state['Buttons'] if b['Text'] == label)
    r = button['Rect']
    command(f"ui press {round(r['X'] + r['Width']/2)} {round(r['Y'] + r['Height']/2)}")


def clear(a, b):
    """Two rectangles that do not overlap."""
    return (a['X'] + a['Width'] <= b['X'] or b['X'] + b['Width'] <= a['X']
            or a['Y'] + a['Height'] <= b['Y'] or b['Y'] + b['Height'] <= a['Y'])


command('pause')
command('ui size 1440 960')
command('ui theme light')
command('ui debug off')
command('ui close')
command('hold look')

# ---- row 4: the pace changes entirely with the mouse, and the console says what the city is doing.
state = read('console-paused')
assert state['Pace'] == 'paused', state['Pace']
assert state['DayLength'] == '', state['DayLength']
assert state['SpeedLabel'] == '1x' and state['PauseHighlighted']
assert not any(f['Text'] in ['paused', 'UNDER POINTER'] for f in state['Fonts'])
buttons = {b['Text']: b for b in state['Buttons']}
assert buttons['◀◀']['Rect']['X'] < buttons['▶']['Rect']['X'] < buttons['▶▶']['Rect']['X']
press(state, '▶')                                         # resume, which is not the ▶▶ beside it
state = read('console-running')
assert state['Pace'] == '1x', state['Pace']
assert not state['PauseHighlighted']
assert 'a Day in' in state['DayLength'], state['DayLength']
faster = state['Pace']
press(state, '▶▶')
state = read('console-faster')
assert state['Pace'] != faster, (faster, state['Pace'])
press(state, '◀◀')
assert read('console-slower')['Pace'] == faster
press(state, '⏸')
state = read('console-repaused')
assert state['Pace'] == 'paused'
assert 'Day ' in state['Day'] and ':' in state['Day'], state['Day']

# ---- row 5: a layer is chosen and cleared with the mouse, and its legend states its axis.
assert state['Layer'] == 'None' and state['Legend'] == ''
press(state, 'Layers  ▾')
state = read('console-layers-open')
assert state['LayersShown']
press(state, 'Pollution')
state = read('console-layer-pollution')
assert state['Layer'] == 'Pollution', state['Layer']
assert 'POLLUTION' in state['Legend'] and 'Cells read' in state['Legend'], state['Legend']
assert 'Rung' not in [b['Text'] for b in state['Buttons']]   # a debug wash, and debug is off
press(state, 'Off')
state = read('console-layer-off')
assert state['Layer'] == 'None' and state['Legend'] == ''

# ---- row 6: a tool is selected and cancelled with the mouse, and a refusal reads without hiding.
command('click 68 36')
state = read('console-selected')
assert state['InspectorVisible'] and state['Selected'] != 0
press(state, 'DEMOLISH  (b)')
state = read('console-armed')
assert state['Tool'] == 'Demolish', state['Tool']
command('click 68 36')                                     # occupied ground, which is refused
state = read('console-refused')
assert 'still lives there' in state['Refused'], state['Refused']
assert clear(state['Refusal'], state['Inspector']), (state['Refusal'], state['Inspector'])
assert state['InspectorVisible'] and state['Selected'] != 0
press(state, 'Cancel  ✕')
state = read('console-cancelled')
assert state['Tool'] == 'Look', state['Tool']
assert 'Cancel  ✕' not in [b['Text'] for b in state['Buttons']]

# ---- both themes and every asserted size: the console stays inside the window and clear of the
# inspector, which is the one thing rows 4-6 may not cost row 1.
hash_at_rest = state['Hash']
for theme in ['light', 'dark']:
    command('ui theme ' + theme)
    for width, height in [(1440, 960), (1024, 640), (640, 720), (480, 640)]:
        command(f'ui size {width} {height}')
        state = read(f'console-{theme}-{width}x{height}')
        console = state['Console']
        assert console['X'] >= 0 and console['Y'] >= 0, console
        assert console['X'] + console['Width'] <= width + 1, (console, width)
        assert console['Y'] + console['Height'] <= height + 1, (console, height)
        assert clear(console, state['Inspector']), (console, state['Inspector'])
        assert state['Hash'] == hash_at_rest

print('PASS: mouse-only pace, layer choice and legend, tool arm/cancel, a readable refusal that '
      'leaves the inspector alone, and a bounded console in both themes at four sizes.')

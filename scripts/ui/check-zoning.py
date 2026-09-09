#!/usr/bin/env python3
"""Exercise block Zoning and the left tool column through viewport input."""
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
    response = f.readline().decode()
    assert response.startswith('ok\t'), response
    time.sleep(.2)

def read(name='zoning-state'):
    path = out / (name + '.json')
    command('ui read ' + str(path))
    return json.loads(path.read_text())

def press(text):
    for _ in range(25):
        state = read()
        b = next(b for b in state['Buttons'] if b['Text'] == text)
        r = b['Rect']
        tray = state['ToolContent']
        if not state['ToolsVisible'] or text in ['Tools', 'Close', 'Cancel'] or (r['Y'] >= tray['Y'] - 1 and r['Y'] + r['Height'] <= tray['Y'] + tray['Height'] + 1):
            break
        direction = -1 if r['Y'] < tray['Y'] else 1
        command(f"ui wheel {round(tray['X'] + tray['Width']/2)} {round(tray['Y'] + tray['Height']/2)} {direction}")
    else:
        raise AssertionError('Tool button could not be reached: ' + text)
    command(f"ui press {round(r['X'] + r['Width']/2)} {round(r['Y'] + r['Height']/2)}")

def clear(a,b):
    return a['X']+a['Width'] <= b['X']+1 or b['X']+b['Width'] <= a['X']+1 or a['Y']+a['Height'] <= b['Y']+1 or b['Y']+b['Height'] <= a['Y']+1

def pointer(verb,p):
    command(f"ui pointer {verb} {p['X']:.1f} {p['Y']:.1f}")

command('pause')
command('ui size 1440 960')
command('ui text-size 100')
command('ui tools off')
command('ui close')
command('ui debug off')
command('focus 48 48 650')
press('Zoning')
state = read()
permission = next(b['Text'] for b in state['Buttons'] if b['Text'] == 'Housing')
press(permission)
assert read()['Tool'] == 'Zone'
press('Whole blocks')
command('ui zone-begin 4 4'); command('ui zone-end 132 132')
state = read()
assert not state['ToolsVisible']
# Use the projected road ground points, safely away from the console.
points = [p for p in state['MapTargets'] if p['Kind'] == 'road' and p['East'] > 10 and p['North'] > 10 and 60 < p['X'] < 1350 and 60 < p['Y'] < state['Console']['Y']-30]
assert len(points) > 2
p, q = points[0], points[-1]
initial = state['Hash']
pointer('down', p)
assert read()['Zoning']['Dragging']
pointer('move', q)
state = read('zoning-preview')
assert state['Hash'] == initial and state['Zoning']['Blocks'] > 1, state['Zoning']
command('shoot ' + str(out / 'zoning-rectangle.png'))
command('ui key Escape')
pointer('up', q)
assert not read()['Zoning']['Dragging'] and read()['Hash'] == initial
# Releasing over a control discards the gesture.
pointer('down', p)
r = read()['Console']
pointer('up', {'X':r['X']+8, 'Y':r['Y']+8})
assert not read()['Zoning']['Dragging']
# Erase and repaint identical semantic bounds; the release queues, the next Tick applies.
press('Zoning'); press('Erase zoning')
pointer('down', p); pointer('move', q); pointer('up', q)
assert not read()['Zoning']['Dragging'] and 'queued' in read()['Zoning']['Feedback']
command('ui zone-begin 4 4'); command('ui zone-point 68 68')
assert read()['Zoning']['Blocks'] == 9
command('ui zone-end 68 68')
assert not read()['Zoning']['Dragging']
assert any(word in read()['Zoning']['Feedback'] for word in ['queued', 'nothing changed'])
command('resume'); time.sleep(.5); command('pause')
command('ui zone-begin 4 4'); command('ui zone-end 68 68')
assert 'nothing changed' in read()['Zoning']['Feedback']
press('Zoning'); press(permission)
command('ui zone-begin 68 68'); command('ui zone-end 4 4')
assert 'queued' in read()['Zoning']['Feedback']
command('resume'); time.sleep(.5); command('pause')
# Browser and inspector remain independently usable across themes and sizes.
command('hold look'); command('click 68 36')
for theme in ['light','dark']:
    command('ui theme '+theme)
    for width,height,scale in [(1440,960,100),(1280,800,100),(1280,800,150)]:
        command(f'ui size {width} {height}'); command(f'ui text-size {scale}')
        command('ui tools on')
        state = read(f'zoning-{theme}-{width}-{scale}')
        tray = state['Tools']
        assert tray['X'] >= 0 and tray['Y'] >= 0
        assert tray['X'] > state['PlacementRail']['X'] and tray['Width'] < width / 2
        assert not any('Rectangle · snaps' in label['Text'] for label in state['Fonts'])
        assert tray['X']+tray['Width'] <= width+1 and tray['Y']+tray['Height'] <= height+1
        assert clear(tray,state['Console'])
        if state['InspectorVisible']:
            inspector = state['Inspector']
            assert clear(tray, inspector), (tray, inspector)
            assert inspector['X'] + inspector['Width'] <= width + 1, inspector
            close = next(b['Rect'] for b in state['Buttons'] if b['Text'] == '×')
            assert close['X'] + close['Width'] <= width, close
        command('shoot '+str(out / f'zoning-{theme}-{width}-{scale}.png'))
        camera = state['Camera']
        press(permission); assert read()['Tool'] == 'Zone'
        assert read()['Camera'] == camera, 'Scrolling tools moved the camera'
        assert not read()['ToolsVisible']
command('ui size 1440 960'); command('ui text-size 100')
press('Demolish')
assert read()['Tool'] == 'Demolish'
press('Cancel'); assert read()['Tool'] == 'Look'
print('PASS: real mouse rectangle preview, cancellation, erase/repaint, both themes and desktop layout.')

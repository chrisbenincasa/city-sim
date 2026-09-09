#!/usr/bin/env python3
"""Saved edge scrolling, content-sized console, and minimap dragging through --listen.
Usage: DISPLAY=:1 python3 scripts/ui/check-navigation-options.py SOCKET X11_WINDOW_ID
Uses xdotool for edge scrolling: synthetic Godot events do not move the desktop pointer.
"""
import json
from pathlib import Path
import socket
import sys
import subprocess
import time

out = Path(__file__).resolve().parents[2] / 'artifacts/hud-live'
out.mkdir(parents=True, exist_ok=True)
s = socket.socket(socket.AF_UNIX)
s.settimeout(30)
s.connect(sys.argv[1])
f = s.makefile('rwb', buffering=0)

def command(text):
    f.write((text + '\n').encode())
    assert f.readline().startswith(b'ok\t'), text
    time.sleep(.2)

def read():
    path = out / 'navigation-options.json'
    command('ui read ' + str(path))
    return json.loads(path.read_text())

def press(label):
    r = next(b['Rect'] for b in read()['Buttons'] if b['Text'] == label)
    command(f"ui press {round(r['X'] + r['Width']/2)} {round(r['Y'] + r['Height']/2)}")

def identity(state):
    return tuple(state[k] for k in ['Hash', 'Tool', 'Layer', 'Selected', 'Household', 'Business'])

for c in ['pause', 'ui size 1920 1080', 'ui text-size 100', 'hold look', 'ui settings on', 'ui edge-scroll on']:
    command(c)
press('Edge scrolling')
assert not read()['EdgeScrolling']
command('ui settings off')
command('focus 48 48 600')
before = read()['Camera']
subprocess.run(['xdotool', 'mousemove', '--window', sys.argv[2], '1', '540'], check=True)
time.sleep(.5)
assert read()['Camera'] == before
command('ui settings on')
press('Edge scrolling')
assert read()['EdgeScrolling']
command('ui settings off')
subprocess.run(['xdotool', 'mousemove', '--window', sys.argv[2], '1', '540'], check=True)
time.sleep(.5)
assert read()['Camera']['Focus'] != before['Focus']
command('ui edge-scroll off')
subprocess.run(['xdotool', 'mousemove', '--window', sys.argv[2], '960', '540'], check=True)
assert read()['Console']['Width'] < 1200

for c in ['hold zone 0', 'overlay value', 'ui building 2']:
    command(c)
for theme in ['dark', 'light']:
    command('ui theme ' + theme)
    for scale in [100, 150]:
        command('ui text-size ' + str(scale))
        state = read()
        original = identity(state)
        r = state['MiniMap']['Rect']
        x, y = round(r['X'] + r['Width']*.4), round(r['Y'] + r['Height']*.5)
        command(f'ui pointer down {x} {y}')
        start = read()
        assert start['MiniMapDragging']
        x = round(r['X'] + r['Width']*.75)
        command(f'ui pointer move {x} {y}')
        moved = read()
        assert moved['Camera']['Focus'] != start['Camera']['Focus']
        assert identity(moved) == original and not moved['Zoning']['Dragging']
        assert moved['MiniMap']['Rebuilds'] == state['MiniMap']['Rebuilds']
        command('ui pointer move 960 540')
        command('ui pointer up 960 540')
        released = read()
        assert not released['MiniMapDragging']
        command('ui pointer move 1000 550')
        assert read()['Camera'] == released['Camera']
        command(f'ui pointer down {x} {y}')
        command('ui key Escape')
        cancelled = read()
        assert not cancelled['MiniMapDragging'] and identity(cancelled) == original
        command('ui pointer move 960 540')
        command('ui pointer up 960 540')
        assert read()['Camera'] == cancelled['Camera']
        command('shoot ' + str(out / f'navigation-options-{theme}-{scale}.png'))
command('ui text-size 100')
command('hold look')
command('ui settings on')
command('shoot ' + str(out / 'navigation-options-settings.png'))
print('PASS: edge toggle, compact console, minimap drag/release/Escape; preference left off for restart check')

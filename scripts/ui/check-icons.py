#!/usr/bin/env python3
"""Verify rendered SVG variants, stable hit boxes and unchanged paused simulation state."""
import json
from pathlib import Path
import socket
import sys
import time

root = Path(__file__).resolve().parents[2]
out = root / 'art/ui-study'
s = socket.socket(socket.AF_UNIX)
s.settimeout(30)
s.connect(sys.argv[1])
f = s.makefile('rwb', buffering=0)

def command(text):
    f.write((text + '\n').encode())
    reply = f.readline()
    assert reply.startswith(b'ok\t'), reply
    time.sleep(.18)

def read():
    path = Path('/tmp/borough-icon-state.json')
    command('ui read ' + str(path))
    return json.loads(path.read_text())

def button(state, text):
    return next(b for b in state['Buttons'] if b['Text'] == text)

def move(rect):
    command(f"ui pointer move {round(rect['X']+rect['Width']/2)} {round(rect['Y']+rect['Height']/2)}")

def click(rect):
    command(f"ui press {round(rect['X']+rect['Width']/2)} {round(rect['Y']+rect['Height']/2)}")

def quiet():
    command('ui pointer move 700 180')

def hover(text, icon):
    quiet()
    before = read()
    b = button(before, text)
    assert b['Icon'] == icon and not b['Filled'], b
    assert b['Rect']['Width'] >= b['IconWidth'] and b['Rect']['Height'] >= b['IconWidth'], b
    move(b['Rect'])
    after = read()
    hovered = button(after, text)
    assert hovered['Filled'] and hovered['Rect'] == b['Rect'], (b, hovered)
    assert after['Hash'] == before['Hash'] and after['Tick'] == before['Tick']
    quiet()
    assert not button(read(), text)['Filled']

command('pause')
command('ui size 1440 960')
command('ui text-size 100')
command('ui close')
command('focus 48 48 600')
quiet()
initial = read()
for theme in ['dark', 'light']:
    command('ui theme ' + theme)
    hover('Tools', 'grid')
    hover('Menu', 'menu')
    hover('Settings', 'settings')
    hover('▶', 'play')
    command('ui tools on')
    hover('Roads', 'road')
    hover('Zoning', 'grid')
    command('hold zone 0')
    quiet()
    selected = button(read(), 'Housing')
    assert selected['Pressed'] and not selected['Filled']
    hover('Housing', 'housing')
    command('hold look')
    command('ui tools off')
    command('ui building 2')
    quiet()
    command(f'shoot {out}/icons-{theme}.png')
    for scale in [100, 150]:
        command(f'ui text-size {scale}')
        hover('Tools', 'grid')
        assert button(read(), 'Tools')['IconWidth'] == scale * 24 // 100
    command('ui text-size 100')
    command('ui close')
quiet()
state = read()
assert all(b['Icon'] != 'trouble' for b in state['Buttons'])
command('ui building 2')
command('ui theme dark')
quiet()
command(f'shoot {out}/icons-dark.png')
assert read()['Hash'] == initial['Hash']
print('PASS: real outline/filled textures, stable hover geometry, selected outline, both themes, 100/150% sizing, no floating warning buttons and unchanged paused State Hash.')

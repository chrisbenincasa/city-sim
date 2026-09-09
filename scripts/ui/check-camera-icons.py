#!/usr/bin/env python3
"""Measure visible icon bounds against circular button centers in real rendered frames."""
import json
from pathlib import Path
import socket
import sys
import time
from PIL import Image

out = Path(__file__).resolve().parents[2] / 'artifacts/hud-live'
out.mkdir(parents=True, exist_ok=True)
s = socket.socket(socket.AF_UNIX); s.settimeout(30); s.connect(sys.argv[1])
f = s.makefile('rwb', buffering=0)
labels = ['↶', '↷', '↑', '↓', '+', '−']

def command(text):
    f.write((text + '\n').encode())
    assert f.readline().startswith(b'ok\t')
    time.sleep(.2)

def read():
    path = out / 'camera-icon-state.json'
    command('ui read ' + str(path))
    return json.loads(path.read_text())

command('pause'); command('ui tools off'); command('ui layers off'); command('ui close')
command('ui size 1440 960')
failures = []
for theme, ink in [('dark', (130,178,255)), ('light', (36,88,163))]:
    command('ui theme ' + theme)
    for scale in [100,150]:
        command('ui text-size ' + str(scale))
        for hovered in [None] + labels:
            command('ui pointer move 720 480')
            state = read()
            buttons = {b['Text']: b for b in state['Buttons'] if b['Text'] in labels}
            if hovered:
                r = buttons[hovered]['Rect']
                command(f"ui pointer move {r['X']+r['Width']/2} {r['Y']+r['Height']/2}")
            path = out / f'camera-icons-{theme}-{scale}-{labels.index(hovered) if hovered else "outline"}.png'
            command('shoot ' + str(path))
            with Image.open(path).convert('RGB') as image:
                for label, button in buttons.items():
                    r = button['Rect']
                    points = [(x+.5,y+.5) for y in range(round(r['Y']),round(r['Y']+r['Height']))
                              for x in range(round(r['X']),round(r['X']+r['Width']))
                              if max(abs(a-b) for a,b in zip(image.getpixel((x,y)),ink)) < 28]
                    assert points, (label,theme,scale)
                    cx = (min(p[0] for p in points)+max(p[0] for p in points))/2
                    cy = (min(p[1] for p in points)+max(p[1] for p in points))/2
                    dx,dy = cx-r['X']-r['Width']/2, cy-r['Y']-r['Height']/2
                    if abs(dx)>1 or abs(dy)>1:
                        failures.append((theme,scale,hovered,label,round(dx,2),round(dy,2)))
    if failures: break
assert not failures, failures
print('PASS: visible icon bounds centered within one pixel in both axes, both themes, 100/150% text, outline and filled hover states.')

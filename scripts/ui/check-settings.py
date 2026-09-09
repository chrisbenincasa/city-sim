#!/usr/bin/env python3
"""Settings navigation, UI response latency, and zoning drawing through the live shell."""
import json
from pathlib import Path
import socket
import sys
import time

out = Path(__file__).resolve().parents[2] / 'artifacts/hud-live'
out.mkdir(parents=True, exist_ok=True)
s = socket.socket(socket.AF_UNIX)
s.settimeout(20)
s.connect(sys.argv[1])
f = s.makefile('rwb', buffering=0)

def send(text):
    f.write((text + '\n').encode())
    reply = f.readline()
    assert reply.startswith(b'ok\t'), reply

def command(text):
    send(text)
    time.sleep(.2)

def read():
    command('ui read ' + str(out / 'settings-state.json'))
    return json.loads((out / 'settings-state.json').read_text())

def press(label):
    for _ in range(12):
        state = read()
        rect = next(b['Rect'] for b in state['Buttons'] if b['Text'] == label)
        if label != 'Settings' or rect['Y'] + rect['Height'] <= state['ConsoleContent']['Y'] + state['ConsoleContent']['Height']:
            break
        area = state['ConsoleContent']
        command(f"ui wheel {round(area['X']+area['Width']/2)} {round(area['Y']+area['Height']/2)} 3")
    command(f"ui press {round(rect['X']+rect['Width']/2)} {round(rect['Y']+rect['Height']/2)}")

def bounded(rect, w, h):
    assert 0 <= rect['X'] <= rect['X']+rect['Width'] <= w+1, rect
    assert 0 <= rect['Y'] <= rect['Y']+rect['Height'] <= h+1, rect

command('pause')
command('ui size 1440 960')
command('ui settings off')
command('ui help off')
command('ui tools off')
command('ui text-size 100')
initial = read()
latencies = []
for action in ['ui theme light', 'ui theme dark', 'ui debug on', 'ui debug off'] * 2:
    start = time.monotonic()
    send(action)
    send('')  # Wait for the next frame too: the old stall followed the action acknowledgement.
    elapsed = time.monotonic() - start
    latencies.append(elapsed)
    assert elapsed < .75, (action, elapsed)
    time.sleep(.2)
for theme in ['light', 'dark']:
    command('ui theme ' + theme)
    for w,h,scale in [(1440,960,100),(480,640,150)]:
        command(f'ui size {w} {h}')
        command(f'ui text-size {scale}')
        press('Settings')
        state = read()
        assert state['SettingsVisible']
        bounded(state['Settings'],w,h)
        for label in ['Light theme','Debug readout','Help & shortcuts','A−','A+','Reset size']:
            rect = next(b['Rect'] for b in state['Buttons'] if b['Text']==label)
            bounded(rect,w,h)
            assert rect['Width'] >= 30, (label,rect)
        command(f'shoot {out}/settings-{theme}-{w}.png')
        press('Reset size')
        assert read()['TextPercent'] == 100
        press('Help & shortcuts')
        assert read()['HelpVisible'] and not read()['SettingsVisible']
        command('ui key Escape')
        press('Tools')
        assert read()['ToolsVisible']
        press('Close')
        opener = next(b['Rect'] for b in read()['Buttons'] if b['Text']=='Tools')
        assert opener['X'] <= 24 and opener['Y'] <= 24
assert read()['Hash'] == initial['Hash']
command('ui size 1440 960')
command('hold zone 1')
command('ui close')
command('focus 80 48 350')
command('ui tools on')
command('ui zone-point 80 48')
command('draw ' + str(out / 'zoning-surfaces.tsv'))
rows = [line.split('\t') for line in (out / 'zoning-surfaces.tsv').read_text().splitlines() if line.startswith('row\t')]
zones = [r for r in rows if r[1]=='zone']
assert zones, 'No persistent block fill'
for r in [r for r in rows if r[1] in ['zone','cursor','plot']]:
    assert float(r[5])+float(r[8])/2 < .05, r  # Below street and footpath surfaces.
assert any(float(r[12])>float(r[11]) and float(r[12])>float(r[13]) for r in zones), 'Housing fill is not green'
command(f'shoot {out}/zoning-filled-blocks.png')
command('hold look')
command('ui tools off')
print(f'PASS: Settings, default reset, left Tools opener, unchanged city, colored fill below roads; max action + next frame {max(latencies):.3f}s.')

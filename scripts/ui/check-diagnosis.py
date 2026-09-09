#!/usr/bin/env python3
"""Exercise diagnosis-fixture.py's city at Tick 512 through visible controls."""
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

def read(name='diagnosis-state'):
    path = out / (name + '.json')
    command('ui read ' + str(path))
    return json.loads(path.read_text())

def click(rect):
    command(f"ui press {round(rect['X']+rect['Width']/2)} {round(rect['Y']+rect['Height']/2)}")

def press(label, last=False):
    buttons = [b for b in read()['Buttons'] if b['Text'] == label]
    assert buttons, label
    click(buttons[-1 if last else 0]['Rect'])

def link(label):
    state = read()
    button = next(b for b in state['Buttons'] if b['Text'] == label)
    command('ui scroll ' + str(max(0, round(state['Scroll'] + button['Rect']['Y'] - state['Inspector']['Y'] - 200))))
    before = read()
    press(label)
    return before

command('pause')
command('ui text-size 100')
command('ui pointer move 720 480')
command('focus 48 48 600')
command('ui close')
initial = read('diagnosis-unselected')
assert initial['Tick'] == 512 and not initial['InspectorVisible']
assert 'SupplyMarks' not in initial
command('hold look')
target = next(t for t in initial['MapTargets'] if t['Kind'] == 'building' and t['Id'] == 2)
command(f"ui map-press {round(target['X'])} {round(target['Y'])}")
command('ui section attention on')
selected = read()
assert selected['Selected'] == 2 and selected['Hash'] == initial['Hash']
assert 'Waiting for money' in selected['Text'] and '+3 other shortfalls' in selected['Text']
command('hold look')
for theme in ['dark', 'light']:
    command('ui theme ' + theme)
    command('ui pointer move 720 480')
    read('diagnosis-' + theme)
    command(f'shoot {out}/diagnosis-{theme}.png')
link('Inspect Household 3 →')
household = read()
assert household['Household'] == 3 and 'Available:' in household['Text']
link('Inspect finances →')
assert read()['Scroll'] > 0
workplace_parent = link('Workplace: Business 19 →')
assert read()['Business'] == 19 and 'Business stocks' in read()['Text']
press('‹ Household 3')
assert read()['Business'] == 0 and read()['Household'] == 3
assert read()['Scroll'] == workplace_parent['Scroll']
press('‹ Building 2')
assert read()['Household'] == 0
def rebate(amount):
    command('ui tools on')
    press('Policies')
    state = read()
    assert state['PoliciesVisible']
    click(state['Inputs'][-1]['Rect'])
    for key in ['Home', 'End shift', 'Backspace'] + ['Key' + digit for digit in str(amount)]:
        command('ui key ' + key)
    assert read()['Inputs'][-1]['Text'] == str(amount)
    press('Set', last=True)
    press('×', last=True)
    command('ui tools off')
    command('ui pointer move 720 480')
    command('focus 48 48 600')

rebate(1000)
command('speed 8')
for _ in range(20):
    state = read()
    if '+2 other shortfalls' in state['Text']:
        command('pause')
        partial = read('diagnosis-partial')
        command(f'shoot {out}/diagnosis-partial.png')
        break
else:
    raise AssertionError(state['Text'])
rebate(100)
command('speed 8')
resolved = None
for _ in range(20):
    time.sleep(.2)
    state = read()
    if 'No supply shortfalls reported.' in state['Text']:
        command('pause')
        resolved = read('diagnosis-resolved')
        break
assert resolved is not None, state['Text']
assert 'Waiting for money' not in resolved['Text']
assert resolved['Hash'] != initial['Hash']
command(f'shoot {out}/diagnosis-resolved.png')
print('PASS: map selection; concurrent causes; inspection; Household, finances and Workplace links; both desktop themes; governed rebate clears the explanation.')

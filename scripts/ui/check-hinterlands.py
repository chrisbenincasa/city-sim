#!/usr/bin/env python3
"""Row 31 task 7: the Outside panel -- four edges, the queue, and each door's own quota.

Run against a shell listening on a socket over a Ruleset with a counted Outside:

    godot --path src/Borough.Godot -- --ruleset rulesets/attracted.toml \
        --citizens 1000 --start-at 6000 --listen /tmp/borough.sock &
    python3 scripts/ui/check-hinterlands.py /tmp/borough.sock
"""
import json
from pathlib import Path
import socket
import sys
import time

out = Path(__file__).resolve().parents[2] / 'artifacts/hud-live'
out.mkdir(parents=True, exist_ok=True)
s = socket.socket(socket.AF_UNIX)
s.settimeout(120)
s.connect(sys.argv[1])
f = s.makefile('rwb', buffering=0)


def command(text):
    f.write((text + '\n').encode())
    reply = f.readline()
    assert reply.startswith(b'ok\t'), (text, reply)
    time.sleep(.15)


def read(name='outside-state'):
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


def button(state, label, exact=False, last=False):
    hits = [b for b in state['Buttons']
            if b['Text'] == label if exact] or ([] if exact else
           [b for b in state['Buttons'] if b['Text'].startswith(label)])
    if not hits:
        return None
    return hits[-1] if last else hits[0]


def press(label, exact=False, last=False):
    state = read()
    b = button(state, label, exact, last)
    assert b is not None, f'no button {label!r}'
    assert not b['Disabled'], label
    r = b['Rect']
    command(f"ui press {round(r['X'] + r['Width']/2)} {round(r['Y'] + r['Height']/2)}")
    assert not read()['Refused'], read()['Refused']


def bounded(r, w, h):
    assert r['X'] >= 0 and r['Y'] >= 0, r
    assert r['X'] + r['Width'] <= w + 1 and r['Y'] + r['Height'] <= h + 1, (r, w, h)


def texts(state):
    return ([label['Text'] for label in state['Fonts']]
            + [b['Text'] for b in state['Buttons']])


for cmd in ['pause', 'ui text-size 100', 'ui size 1440 960', 'ui theme dark',
            'ui debug off', 'ui tools off', 'ui layers off', 'hold look',
            'ui pointer move 720 480', 'focus 48 48 600']:
    command(cmd)

start = read()
paused = start['Hash']
assert not start['OutsideShown']
assert button(start, 'Outside', exact=True) is not None, 'no Outside launcher'
assert not button(start, 'Outside', exact=True)['Disabled']

# The panel opens off the console's own Outside button.
press('Outside', exact=True)
opened = read('outside-open')
assert opened['OutsideShown']
bounded(opened['OutsidePanel'], 1440, 960)
outside = opened['Outside']
assert outside is not None, 'this Ruleset states no [immigration] and cannot answer'

# Four edge rows, whatever the city has done to them, each one selectable.
assert len(outside['Edges']) == 4, outside['Edges']
for name in ['West', 'East', 'South', 'North']:
    assert button(opened, name, exact=True) is not None, name

# A fresh occasion has exactly one of four outcomes, and a queue retry is not one of them.
for edge in outside['Edges']:
    for when in ['Today', 'Yesterday']:
        flows = edge[when]
        assert flows['Occasions'] == (flows['NoConnection'] + flows['NoSample']
                                      + flows['StayedOutside'] + flows['Willing']), (edge, when)

# Waiting outside a full door and looking for a home inside the city are different counts, and
# the panel states them under different headings rather than calling both Unplaced.
account = outside['Account']
assert account['Residual'] == 0 and account['HouseholdResidual'] == 0, account
shown = texts(opened)
assert 'Waiting outside' in shown and 'Admitted, looking' in shown
waiting = sum(edge['QueueHouseholds'] for edge in outside['Edges'])
assert f'{waiting:,} Households' in shown
assert f"{account['PoolHouseholds']:,} Households" in shown

# Each door has its own quota; an edge's is its doors added up and never one door's own.
for edge in outside['Edges']:
    doors = [door for door in outside['Doors'] if door['Edge'] == edge['Edge']]
    assert len(doors) == edge['Gates'], (edge['Edge'], len(doors), edge['Gates'])
    assert sum(door['AdmittedToday'] for door in doors) == edge['AdmittedToday']
    assert sum(door['Ceiling'] - door['AdmittedToday'] for door in doors) == edge['RemainingToday']

# 🔴 Reading the Outside moves no state. Every figure above came out of the world without
# consuming a draw, resetting a meter or writing a row.
assert read()['Hash'] == paused, 'the panel moved the State Hash'

# Selecting an edge shows its compositions, its doors and both Days of its flows.
press('West', exact=True)
west = read('outside-west')
assert west['Outside']['OpenEdge'] == 'West'
detail = texts(west)
assert any(line.startswith('Who is out there · ') for line in detail), detail
assert any(line.startswith('Doors · ') for line in detail), detail
for heading in ['Who considered coming', 'What became of them', "The Outside's own arithmetic"]:
    assert heading in detail, heading
assert read()['Hash'] == paused

# Inspecting a door states that door's quota and links to the edge whose stock it draws on.
door = outside['Doors'][0]
command(f"ui building {door['Building']}")
inspected = read('outside-gate')
assert inspected['Selected'] == door['Building'], (inspected['Selected'], door['Building'])
assert any('Outside Connection · ' in line for line in texts(inspected)), texts(inspected)
assert any('Admitted today · ' in line for line in texts(inspected)), 'no quota on the door'
assert any('Outside →' in line for line in texts(inspected)), 'no link to the edge'

# The left anchor holds one panel at a time, as Government and the Evidence list already agree.
press('Government')
governed = read()
assert governed['PoliciesVisible'] and not governed['OutsideShown']
press('Outside', exact=True)
back = read()
assert back['OutsideShown'] and not back['PoliciesVisible']
press('Evidence')
evicted = read()
assert evicted['CityEvidenceShown'] and not evicted['OutsideShown']
press('Outside', exact=True)
assert read()['OutsideShown']

# The desktop matrix: both themes, two sizes, 100% and 150% text.
for width, height in [(1280, 800), (1440, 960)]:
    for theme in ['dark', 'light']:
        for percent in [100, 150]:
            command(f'ui size {width} {height}')
            command('ui theme ' + theme)
            command(f'ui text-size {percent}')
            sized = read(f'outside-{width}-{theme}-{percent}')
            assert sized['OutsideShown']
            panel = sized['OutsidePanel']
            bounded(panel, width, height)

            # ⚠ Only the panel and its fixed controls. A row inside the body may sit below the
            # visible scroll area, which is a thing to scroll to rather than one drawn off frame.
            r = button(sized, 'Close Outside')['Rect']
            bounded(r, width, height)
            assert r['X'] >= panel['X'] - 1 and r['Y'] >= panel['Y'] - 1, (r, panel)
            assert r['X'] + r['Width'] <= panel['X'] + panel['Width'] + 1, (r, panel)

command('ui size 1440 960')
command('ui text-size 100')
command('ui theme light')
command(f'shoot {out}/outside-light.png')
command('ui theme dark')
command(f'shoot {out}/outside-dark.png')

print('PASS: the Outside panel opens off its own launcher, shows four edges whatever stands on '
      'them, keeps waiting-outside apart from admitted-and-looking, adds each edge up out of its '
      'own doors, states every fresh outcome as one of four, moves no State Hash, opens from a '
      'door as well as from the console, takes turns with Government and the Evidence list, and '
      'stays bounded in both themes at two sizes and two text scales.')

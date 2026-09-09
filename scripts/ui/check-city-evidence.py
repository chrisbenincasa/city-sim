#!/usr/bin/env python3
"""Rows 7 and 14: the city Evidence list and the Trouble wash, on diagnosis-fixture.py's city."""
import json
from pathlib import Path
import socket
import sys
import time

out = Path(__file__).resolve().parents[2] / 'artifacts/hud-live'
out.mkdir(parents=True, exist_ok=True)
s = socket.socket(socket.AF_UNIX)
s.settimeout(60)
s.connect(sys.argv[1])
f = s.makefile('rwb', buffering=0)


def command(text):
    f.write((text + '\n').encode())
    reply = f.readline()
    assert reply.startswith(b'ok\t'), (text, reply)
    time.sleep(.15)


def read(name='city-evidence-state'):
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


def member_links(state):
    return [b['Text'] for b in state['Buttons']
            if ' \u00b7 Building ' in b['Text'] and b['Text'].endswith('\u2192')]


def bounded(r, w, h):
    assert r['X'] >= 0 and r['Y'] >= 0, r
    assert r['X'] + r['Width'] <= w + 1 and r['Y'] + r['Height'] <= h + 1, (r, w, h)


def trouble(state):
    return [g for g in state['CityEvidence']['Groups'] if g['Blocked'] != 'Space']


def routine(state):
    return [g for g in state['CityEvidence']['Groups'] if g['Blocked'] == 'Space']


for cmd in ['pause', 'ui text-size 100', 'ui size 1440 960', 'ui theme dark',
            'ui debug off', 'ui tools off', 'ui layers off', 'hold look',
            'ui pointer move 720 480', 'focus 48 48 600']:
    command(cmd)

start = read()
paused = start['Hash']
assert not start['CityEvidenceShown']
assert button(start, 'Evidence') is not None and not button(start, 'Evidence')['Disabled']
assert button(start, 'City')['Disabled'] and button(start, 'Pins')['Disabled']

# The list opens off the console's own Evidence button and reads the city once.
press('Evidence')
opened = read('city-evidence-open')
assert opened['CityEvidenceShown']
reading = opened['CityEvidence']
assert reading['Read'] and reading['ReadAt'] == opened['Tick']
assert reading['BuildingsRead'] > 0
assert trouble(opened), reading['Groups']
bounded(opened['CityEvidencePanel'], 1440, 960)

# Every group's count reconciles with the subjects listed under it.
assert sum(g['Subjects'] for g in reading['Groups']) == reading['Subjects']

# A routine wait is its own group and stays out of the wash.
assert routine(opened), 'the fixture no longer produces a routine wait'
assert reading['WashSubjects'] == sum(g['Subjects'] for g in trouble(opened))

# Expanding a group lists its members, six at a time, and the paging says so.
first = trouble(opened)[0]
press(str(first['Subjects']))
expanded = read('city-evidence-expanded')
links = member_links(expanded)
assert len(links) == min(6, first['Subjects']), links
assert expanded['CityEvidence']['OpenGroup'] >= 0
if first['Subjects'] > 6:
    assert button(expanded, 'More') is not None and button(expanded, 'Earlier')['Disabled']
    press('More')
    later = read()
    assert later['CityEvidence']['From'] == 6
    assert not button(later, 'Earlier')['Disabled']
    press('Earlier')
    assert read()['CityEvidence']['From'] == 0

# A member opens its own subject, through the inspection path the map click uses.
member = member_links(read())[0]
subject = int(member.split()[1])
building = int(member.split('Building ')[1].split()[0])
press(member)
navigated = read('city-evidence-subject')
assert navigated['Selected'] == building, (navigated['Selected'], building)
assert navigated['Household'] == subject or navigated['Business'] == subject
assert navigated['InspectorVisible']
assert navigated['Hash'] == paused

# The wash draws the same reading, narrowed to the group the list has open.
command('overlay trouble')
washed = read('city-evidence-wash')
assert washed['Layer'] == 'Trouble'
assert 'TROUBLE' in washed['Legend']
assert washed['CityEvidence']['WashSubjects'] == first['Subjects']
assert washed['CityEvidence']['WashBuildings'] > 0
assert f"{first['Subjects']:,} subjects" in washed['Legend'], washed['Legend']
command(f'shoot {out}/city-evidence-wash-dark.png')

# Closing the filter widens it back to every cause but routine waiting.
press(str(first['Subjects']))
wide = read()
assert wide['CityEvidence']['OpenGroup'] == -1
assert wide['CityEvidence']['WashSubjects'] == sum(g['Subjects'] for g in trouble(wide))
assert 'routine waiting' in wide['Legend']

# Government and the list share one slot, so opening either folds the other away, and the layer and
# the inspection stand through both.
press('Government')
governed = read()
assert governed['PoliciesVisible'] and not governed['CityEvidenceShown']
assert governed['Layer'] == 'Trouble', 'opening Government dropped the layer'
assert governed['Selected'] == building
press('Close Government')
press('Evidence')

# With the wash off nothing drives the cadence, so the list holds the Tick it was read at.
command('overlay off')
stale = read('city-evidence-stale')
assert stale['CityEvidence']['ReadAt'] == reading['ReadAt'], 'the list re-read itself with no wash on'

command('speed 8')
for _ in range(40):
    now = read()
    if now['Tick'] > stale['Tick'] + 256:
        break
command('pause')

held = read()
assert held['CityEvidence']['ReadAt'] == reading['ReadAt'], 'the held list moved'

# ⚠ WHAT A DRIVEN RUN CANNOT SHOW HERE, and why it is not a gap in the panel. Row 7 wants a resolved
# condition to leave the list. No shipped Ruleset can express balance -> unbalance -> balance
# (adr/0168), so a Household this fixture rebates is short again within its next upkeep; and Demolish
# refuses occupied ground, so nothing here can remove a listed subject either. EvidenceTests
# .A_repaired_cause_leaves_the_city_list_and_the_other_stays holds that requirement exactly, in Core,
# by repairing the Bin the Rule is asleep on. What this run holds is that a Refresh moves the reading
# and the list re-reconciles against it.
press('Refresh Evidence')
after = read('city-evidence-refreshed')
assert after['CityEvidence']['ReadAt'] > held['CityEvidence']['ReadAt']
assert sum(g['Subjects'] for g in after['CityEvidence']['Groups']) == after['CityEvidence']['Subjects']
assert after['CityEvidence']['WashSubjects'] == sum(g['Subjects'] for g in trouble(after))

press(str(trouble(after)[0]['Subjects']))
listed = read('city-evidence-members')
assert len(listed['CityEvidence']['OpenMembers']) == trouble(listed)[0]['Subjects']
assert len(set(listed['CityEvidence']['OpenMembers'])) == len(listed['CityEvidence']['OpenMembers'])
assert len(member_links(listed)) == min(6, trouble(listed)[0]['Subjects'])

# With the wash on, the reading follows its cadence and the head says so.
command('overlay trouble')
command('speed 8')
following = read('city-evidence-following')
for _ in range(40):
    following = read()
    if following['CityEvidence']['ReadAt'] > held['CityEvidence']['ReadAt']:
        break
else:
    raise AssertionError('the wash did not carry the reading forward')
command('pause')

# Escape folds the list away and leaves the layer and the inspection standing.
command('ui key Escape')
closed = read()
assert not closed['CityEvidenceShown']
assert closed['Layer'] == 'Trouble' and closed['Selected'] == building

# The desktop matrix: both themes, three sizes, 100% and 150% text.
command('ui city on')
for width, height in [(1280, 800), (1440, 960), (1920, 1080)]:
    for theme in ['dark', 'light']:
        for percent in [100, 150]:
            command(f'ui size {width} {height}')
            command('ui theme ' + theme)
            command(f'ui text-size {percent}')
            sized = read(f'city-evidence-{width}-{theme}-{percent}')
            assert sized['CityEvidenceShown']
            assert sized['CityEvidence']['Read']
            panel = sized['CityEvidencePanel']
            bounded(panel, width, height)

            # ⚠ ONLY THE PANEL AND ITS FIXED CONTROLS. A row inside the list may sit below the
            # visible scroll area, exactly as an inspector link may -- which is a thing to scroll to
            # rather than a control drawn off the frame.
            for label in ['Refresh Evidence', 'Close Evidence']:
                r = button(sized, label)['Rect']
                bounded(r, width, height)
                assert r['X'] >= panel['X'] - 1 and r['Y'] >= panel['Y'] - 1, (label, r, panel)
                assert r['X'] + r['Width'] <= panel['X'] + panel['Width'] + 1, (label, r, panel)
command('ui size 1440 960')
command('ui text-size 100')
command('ui theme light')
command(f'shoot {out}/city-evidence-light.png')
command('ui theme dark')
command(f'shoot {out}/city-evidence-dark.png')

print('PASS: city Evidence list opens, groups reconcile with their members, routine waits stay out '
      'of the wash, members navigate, the wash follows the filter, the reading holds still without '
      'the wash and follows it with, Government and the list take turns, and the desktop matrix is '
      'bounded in both themes at three sizes and two text scales.')

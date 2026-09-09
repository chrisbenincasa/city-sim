#!/usr/bin/env python3
"""Save/load and menu lifecycle through real button rectangles in the driven shell."""
import json
from pathlib import Path
import socket
import sys
import time
import tempfile

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

def read():
    command('ui read ' + str(out / 'menu-state.json'))
    return json.loads((out / 'menu-state.json').read_text())

def press(label):
    state = read()
    matches = [b for b in state['Buttons'] if b['Text'] == label]
    assert matches, (label, [b['Text'] for b in state['Buttons']])
    rect = matches[-1]['Rect']
    command(f"ui press {round(rect['X']+rect['Width']/2)} {round(rect['Y']+rect['Height']/2)}")

def choose(path, label):
    assert read()['FilePickerVisible']
    command('ui file-path ' + str(path))
    if label == 'Open':
        item = next(i for i in read()['FileItems'] if i['Text'] == path.name)
        r = item['Rect']
        command(f"ui press {round(r['X']+r['Width']/2)} {round(r['Y']+r['Height']/2)}")
    press(label)
    assert not read()['FilePickerVisible'], read()['MenuMessage']

command('pause')
command('ui menu off')
command('ui size 1440 960')
command('ui text-size 100')
command('ui help off')
command('ui settings off')
command('ui tools off')
with tempfile.TemporaryDirectory(prefix='borough-menu-') as folder:
    save = Path(folder) / 'My city.borough-city'
    bad = Path(folder) / 'Broken city.borough-city'
    bad.write_text('not a city')
    for theme in ['dark', 'light']:
        command('ui theme ' + theme)
        press('Menu')
        assert read()['MenuVisible']
        before = read()
        command('ui key Space')
        command('ui map-press 800 400')
        assert read()['Hash'] == before['Hash']
        command(f'shoot {out}/menu-{theme}.png')
        press('Settings')
        assert read()['SettingsVisible']
        press('×')
        press('Help & shortcuts')
        assert read()['HelpVisible']
        press('Close Help ×')
        press('Credits')
        assert read()['MenuPage'] == 'credits'
        command(f'shoot {out}/menu-credits-{theme}.png')
        press('Back to menu')
        press('Save city…')
        command(f'shoot {out}/menu-picker-{theme}.png')
        press('Cancel')
        press('Quit game')
        assert read()['MenuPage'] == 'confirm'
        press('Cancel')
        press('Resume city')
        assert read()['PauseHighlighted'] and not read()['MenuVisible']
    press('Menu')
    press('Load city…')
    assert read()['MenuPage'] == 'confirm'
    press('Save and continue')
    press('Cancel')  # Cancelling the picker must cancel the pending load too.
    assert read()['MenuPage'] == 'main' and not read()['FilePickerVisible']
    press('Save city…')
    choose(save, 'Save')
    saved = read()
    assert not saved['Unsaved'] and save.exists(), saved['MenuMessage']
    press('Load city…')
    choose(bad, 'Open')
    failed = read()
    assert 'Could not load' in failed['MenuMessage'], failed['MenuMessage']
    assert failed['Hash'] == saved['Hash'] and failed['Tick'] == saved['Tick']
    press('Resume city')
    command('speed 8')
    time.sleep(.6)
    press('Menu')
    advanced = read()
    assert advanced['Tick'] > saved['Tick'] and advanced['Unsaved']
    press('Resume city')
    assert read()['Pace'] == '4x', read()['Pace']
    press('Menu')
    press('Load city…')
    press('Discard and continue')
    choose(save, 'Open')
    loaded = read()
    assert loaded['Hash'] == saved['Hash'] and loaded['Tick'] == saved['Tick'], loaded['MenuMessage']
    assert not loaded['InspectorVisible'] and not loaded['Unsaved']
    command(f'shoot {out}/menu-loaded.png')
    press('Resume city')
    assert read()['Pace'] == '4x'
    command('pause')
    assert read()['Tick'] > saved['Tick']
    press('Menu')
    press('Load city…')
    press('Save and continue')
    checkpoint = Path(folder) / 'Checkpoint.borough-city'
    command('ui file-path ' + str(checkpoint))
    press('Save')
    assert checkpoint.exists() and read()['FilePickerVisible'] and not read()['Unsaved']
    press('Cancel')  # Save succeeded; cancelling the subsequent load keeps this city.
    press('Resume city')
    command('speed 8')
    time.sleep(.3)
    command('pause')
    command('ui text-size 150')
    # Saving from Quit must finish writing before the process exits.
    press('Menu')
    command(f'shoot {out}/menu-large-text.png')
    press('Quit game')
    press('Save and continue')
    final = Path(folder) / 'Final city.borough-city'
    command('ui file-path ' + str(final))
    press('Save')
    assert final.exists()
print('PASS: menu modality, both themes, Settings/Help/Credits, pause restoration, save/load, failed-load preservation, cancellation and protected Quit.')

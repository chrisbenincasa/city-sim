#!/usr/bin/env python3
"""Run the three undo drives on a display and assert what their readouts and draw lists show.

scripts/ui/check-undo.py
"""
from pathlib import Path
import shutil
import subprocess

root = Path(__file__).resolve().parents[2]
out = root / 'artifacts/undo'
shutil.rmtree(out, ignore_errors=True)
out.mkdir(parents=True)
for drive in ['undo', 'undo-refusals', 'undo-gate']:
    subprocess.run(['godot', '--path', str(root / 'src/Borough.Godot'), '--', '--empty',
                    '--seed', '0x135283C', '--citizens', '2000',
                    '--drive', str(root / f'scripts/ui/{drive}.drive')],
                   check=True, timeout=600, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

def layer(name, kind):
    for line in (out / f'{name}.tsv').read_text().splitlines():
        cells = line.split('\t')
        if cells[:2] == ['layer', kind]:
            return int(cells[2])
    raise AssertionError(f'No {kind} layer in {name}.tsv')

def roads(name): return layer(name, 'road')
def zones(name): return layer(name, 'zone')

def text(name): return (out / f'{name}.txt').read_text()

def hover(name):
    readout = text(name)
    return readout[readout.index('\nhover'):]

def said(name, words):
    assert words in text(name), f'{name}: expected "{words}"'

def hovered(name, words):
    assert words in hover(name), f'{name}: expected "{words}" in the hover'

UNDONE_STREET = 'UNDO — removed 1 Street'
REPAINTED = 'UNDO — repainted 1 patch of ground'
SPLIT = 'REFUSED — that gesture cut a Street that was already there in two'
MIXED = 'REFUSED — that stroke painted over ground holding more than one permission'
CLEARED = 'REFUSED — that act cleared what stood there'
PLACED = 'REFUSED — that act placed a Building'
WIDE = 'REFUSED — that gesture covered more ground than the shell remembers'

# undo.drive
assert roads('02-street-laid') == roads('01-founded') + 1
assert roads('03-street-undone') == roads('01-founded')
said('03-street-undone', UNDONE_STREET)
hovered('04-zone-painted', 'block (0, 3) — repaints to housing')
hovered('05-zone-undone', 'block (0, 3) — already housing')
said('05-zone-undone', REPAINTED)
said('06-refused-split', SPLIT)
said('07-refused-mixed', MIXED)
said('08-refused-placement', PLACED)
assert layer('08-refused-placement', 'building') == layer('01-founded', 'building') + 1

# undo-refusals.drive
assert roads('10-held-over-undone') == roads('09-held-over') - 1
said('10-held-over-undone', UNDONE_STREET)
hovered('12-same-moment-undone', 'block (0, 0) — repaints to trade')
said('12-same-moment-undone', REPAINTED)
said('13-refused-part', MIXED)
said('14-refused-reshaped', CLEARED)
said('16-refused-side', CLEARED)
assert zones('16-refused-side') == zones('15-side-painted')
said('18-refused-wide', WIDE)
assert (roads('18-refused-wide'), zones('18-refused-wide')) == (roads('17-wide-painted'), zones('17-wide-painted'))

# undo-gate.drive
hovered('20-waited-at-first', 'shift-click removes Street 1 ')
hovered('20-waited-at-second', 'shift-click removes Street 2 ')
hovered('21-done-at-first', 'shift-click removes Street 1 ')
assert 'shift-click removes Street ' not in hover('21-done-at-second'), 'The waited undo removed the wrong Street'
assert roads('21-waited-done') == roads('20-waited') - 1
said('23-refused-batch-cross', SPLIT)
assert roads('23-refused-batch-cross') == roads('22-crossed') == 5
said('25-refused-grid', SPLIT)
said('26-refused-grid-again', SPLIT)
assert roads('26-refused-grid-again') == roads('24-grid') == 12
assert roads('28-removed') == roads('27-two-streets') - 1
said('29-refused-removal', CLEARED)
assert roads('29-refused-removal') == roads('28-removed')
assert (zones('30-parcel-painted'), zones('31-parcel-undone')) == (1, 0)
said('31-parcel-undone', REPAINTED)
assert (zones('32-block-painted'), zones('33-block-undone')) == (1, 0)
said('33-block-undone', REPAINTED)

print('PASS: undo reverses Streets and paint, waits for a queued gesture, and refuses splits, '
      'mixed paint, barriers and oversized strokes.')

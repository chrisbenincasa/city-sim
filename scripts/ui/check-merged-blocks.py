#!/usr/bin/env python3
"""Assert the shell reads and draws a merged 128 m block as one block.

godot --path src/Borough.Godot -- --empty --seed 0x135283C --citizens 2000 \
  --ruleset rulesets/base/ruleset.toml --listen /tmp/merged-blocks.sock &
scripts/ui/check-merged-blocks.py /tmp/merged-blocks.sock

Streets every 32 Tiles on base's 16-Tile lattice enclose 2x2 merged blocks.
"""
import atexit
import json
from pathlib import Path
import socket
import sys

METRES_PER_TILE = 4
out = Path(__file__).resolve().parents[2] / 'artifacts/merged-blocks'
out.mkdir(parents=True, exist_ok=True)
s = socket.socket(socket.AF_UNIX)
s.settimeout(60)
s.connect(sys.argv[1])
f = s.makefile('rwb', buffering=0)


@atexit.register
def quit_shell():
    try:
        f.write(b'quit\n')
    except OSError:
        pass


def command(text):
    f.write((text + '\n').encode())
    response = f.readline().decode()
    assert response.startswith('ok\t'), (text, response)
    return int(response.split('\t')[1])


def tick():
    start = command('resume')
    while command('') <= start:
        pass
    command('pause')


def zoning():
    path = out / 'state.json'
    command('ui read ' + str(path))
    return json.loads(path.read_text())['Zoning']


def hover():
    path = out / 'readout.txt'
    command('readout ' + str(path))
    text = path.read_text()
    return text[text.index('\nhover'):]


def rows(layer, name):
    path = out / f'{name}.tsv'
    command('draw ' + str(path))
    found = []
    for line in path.read_text().splitlines():
        cells = line.split('\t')
        if cells[0] == 'row' and cells[1] == layer:
            found.append((float(cells[4]), -float(cells[6]), float(cells[7]), float(cells[9])))
    return found


def inside(row, west, south, east, north):
    x, z, wide, deep = row
    return west <= x - wide / 2 + .01 and x + wide / 2 - .01 <= east and south <= z - deep / 2 + .01 and z + deep / 2 - .01 <= north


command('pause')
command('ui tools off')
command('focus 64 64 700')
command('hold street')
for edge in range(0, 129, 32):
    command(f'click 0 {edge}'); command(f'click 128 {edge}')
    command(f'click {edge} 0'); command(f'click {edge} 128')
tick()

command('ui zone-size blocks')
command('hold zone 0')

# The north-east square of the merged block at (32, 32)-(64, 64) Tiles.
command('ui zone-begin 52 52')
command('ui zone-point 52 52')
one = zoning()
said = hover()
assert one['Blocks'] == 1, one
assert one['Lots'] > 0, one
assert f"{one['Lots']} Lot{'s' if one['Lots'] != 1 else ''} to create across 1 block" in said, said
assert 'block (2, 2)' in said, said
assert '4 of 4 faces still to subdivide' in said, said

command('ui zone-point 92 52')
two = zoning()
assert two['Blocks'] == 2, two
assert 'across 2 blocks' in hover(), hover()
command('ui zone-point 52 52')

before = len(rows('plot', 'before'))
command('ui zone-end 52 52')
tick()
plots = rows('plot', 'after')
assert len(plots) - before == one['Lots'], (before, len(plots), one['Lots'])

west, south, east, north = (32 * METRES_PER_TILE, 32 * METRES_PER_TILE, 64 * METRES_PER_TILE, 64 * METRES_PER_TILE)
middle = 48 * METRES_PER_TILE
block = [p for p in plots if inside(p, west, south, east, north)]
assert len(block) == one['Lots'], (len(block), one['Lots'])
quarters = {(p[0] > middle, p[1] > middle) for p in block}
assert quarters == {(False, False), (False, True), (True, False), (True, True)}, quarters
centre = [p for p in block if abs(p[0] - middle) < p[2] / 2 and abs(p[1] - middle) < p[3] / 2]
assert not centre, centre

assert 'a click changes nothing' in hover(), hover()

print(f'PASS: merged block hovers as 1 block of {one["Lots"]} Lots, two blocks as 2, '
      f'and its {len(block)} plots fill all four quarters.')

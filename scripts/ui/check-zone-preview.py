#!/usr/bin/env python3
"""Assert the whole-block Zoning hover's proposed Lot count.

godot --path src/Borough.Godot -- --empty --seed 0x135283C --citizens 2000 \
  --ruleset rulesets/base/ruleset.toml --listen /tmp/zone-preview.sock &
scripts/ui/check-zone-preview.py /tmp/zone-preview.sock
"""
import json
from pathlib import Path
import socket
import sys

out = Path(__file__).resolve().parents[2] / 'artifacts/zone-preview'
out.mkdir(parents=True, exist_ok=True)
s = socket.socket(socket.AF_UNIX)
s.settimeout(60)
s.connect(sys.argv[1])
f = s.makefile('rwb', buffering=0)

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

def proposed(east, north):
    command(f'ui zone-point {east} {north}')
    path = out / 'state.json'
    command('ui read ' + str(path))
    return json.loads(path.read_text())['Zoning']['Lots']

def plots():
    path = out / 'draw.tsv'
    command('draw ' + str(path))
    for line in path.read_text().splitlines():
        cells = line.split('\t')
        if cells[:2] == ['layer', 'plot']:
            return int(cells[2])
    raise AssertionError('No plot layer in ' + str(path))

def stroke(west, south, east, north):
    command(f'ui zone-begin {west} {south}')
    command(f'ui zone-end {east} {north}')
    tick()

command('pause')
command('ui tools off')
command('hold street')
for edge in range(0, 129, 32):
    command(f'click 0 {edge}'); command(f'click 128 {edge}')
    command(f'click {edge} 0'); command(f'click {edge} 128')
tick()

command('ui zone-size blocks')
command('hold zone 0')
fresh = proposed(4, 4)
assert fresh > 0, fresh
stroke(4, 4, 4, 4)
assert proposed(4, 4) == 0, 'Standing Lots counted as new'

partly = proposed(34, 34)
command('ui zone-size parcels')
stroke(34, 34, 34, 34)
command('ui zone-size blocks')
remaining = proposed(34, 34)
assert 0 < remaining < partly, (partly, remaining)

command('hold erase')
assert proposed(36, 36) == 0, 'Erase proposes Lots'

command('hold zone 0')
command('ui zone-begin 68 68')
expected = proposed(100, 100)
before = plots()
command('ui zone-end 100 100')
tick()
assert plots() - before == expected, (before, plots(), expected)

command('quit')
print(f'PASS: fresh {fresh}, standing 0, partly zoned {partly} -> {remaining}, erase 0, 2x2 created {expected}.')

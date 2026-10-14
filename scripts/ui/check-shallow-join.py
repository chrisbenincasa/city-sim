#!/usr/bin/env python3
"""Run the shallow-join drive on a display and assert both Streets are laid.

scripts/ui/check-shallow-join.py
"""
from pathlib import Path
import shutil
import subprocess

root = Path(__file__).resolve().parents[2]
out = root / 'artifacts/shallow-join'
shutil.rmtree(out, ignore_errors=True)
out.mkdir(parents=True)
subprocess.run(['godot', '--path', str(root / 'src/Borough.Godot'), '--', '--empty', '--citizens', '1000',
                '--drive', str(root / 'scripts/ui/shallow-join.drive')],
               check=True, timeout=600, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

roads = next(int(cells[2]) for cells in (line.split('\t') for line in (out / 'laid.tsv').read_text().splitlines())
             if cells[:2] == ['layer', 'road'])
said = (out / 'laid.txt').read_text()
assert 'wider angle' not in said, said
assert roads == 2, f'expected 2 road Segments, found {roads}'
print('ok: both Streets laid')

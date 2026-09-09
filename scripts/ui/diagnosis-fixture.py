#!/usr/bin/env python3
"""Prepare a money-shortfall interaction fixture; all changed tuning is provisional."""
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[2]
out = Path(sys.argv[1])
out.mkdir(parents=True, exist_ok=True)
text = (root / 'rulesets/taxed.toml').read_text()
changes = {
    '{ resource = "repairs",  capacity = 4 },': '{ resource = "repairs",  capacity = 4 },\n    { resource = "money", owner = "occupant" },',
    'inputs  = [ { scope = "local", resource = "repairs", amount = 4 } ]\noutputs = []':
        'inputs  = [ { scope = "local", resource = "money", amount = 100 } ]\noutputs = [ { scope = "global", resource = "money", amount = 100 } ]',
    'rate    = 16': 'rate    = 256',
    'interval = 2048': 'interval = 32',
    'apply = { min = 100, max = 100 }': 'apply = { min = 1, max = 1 }',
}
for old, new in changes.items():
    assert old in text, old
    text = text.replace(old, new)
(out / 'diagnosis.toml').write_text('# UI fixture derived from taxed.toml; not a balance demonstration.\n' + text)
(out / 'diagnosis.drive').write_text('''512 pause
512 ui size 1440 960
512 ui text-size 100
512 ui theme dark
512 ui pointer move 720 480
512 focus 48 48 600
512 ui point 48 48
''')
print(out / 'diagnosis.toml')

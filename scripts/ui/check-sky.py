#!/usr/bin/env python3
"""Read the drawn day/night marker at fixed civil times in fresh Godot runs."""
import json
from pathlib import Path
import subprocess

root = Path(__file__).resolve().parents[2]
output = root / 'artifacts/hud-live'
output.mkdir(parents=True, exist_ok=True)
for minute, name, daytime in [(300, '0500', False), (360, '0600', True),
                               (720, '1200', True), (1080, '1800', False), (0, '0000', False)]:
    tick = max(1, (((minute - 300) % 1440) * 2048 + 1439) // 1440)
    state_path = output / f'sky-{name}.json'
    drive = output / f'sky-{name}.drive'
    drive.write_text(f'''1 pause
1 ui size 1440 960
1 ui text-size 118
1 ui debug off
1 ui theme dark
1 focus 80 48 350
1 ui read {state_path}
1 shoot {output}/sky-{name}.png
1 quit
''')
    with (output / f'sky-{name}.log').open('w') as log:
        subprocess.run(['godot', '--path', str(root / 'src/Borough.Godot'), '--',
                        '--ruleset', 'rulesets/diagnosed.toml', '--citizens', '256',
                        '--start-at', str(tick), '--drive', str(drive)],
                       cwd=root, stdout=log, stderr=subprocess.STDOUT, check=True, timeout=45)
    state = json.loads(state_path.read_text())
    sky = state['Sky']
    assert sky['Minute'] == minute and sky['Daytime'] == daytime, sky
    if name in ['0500', '0600']:
        assert sky['X'] < .2, sky
    if name == '0500':
        assert sky['Y'] > .5, sky
    if name == '1200':
        assert .45 < sky['X'] < .55 and sky['Y'] < .25, sky
    if name == '1800':
        assert sky['X'] > .8 and abs(sky['Y'] - .5) < .02, sky
    if name == '0000':
        assert .45 < sky['X'] < .55 and sky['Y'] > .75, sky
    print(f'PASS: {name} {"day" if daytime else "night"}, marker {sky["X"]:.3f}, {sky["Y"]:.3f}')

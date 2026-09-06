#!/usr/bin/env python3
"""Capture the live facade shader and a fixed shopping city; build Godot in Debug first."""
import json
import os
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else ROOT / 'artifacts/visual-study/live-facades'
GODOT = os.environ.get('GODOT_BIN', 'godot')
OUTPUT.mkdir(parents=True, exist_ok=True)


def run(name, args, profile=False):
    environment = dict(os.environ)
    if profile:
        environment.update(BOROUGH_RENDER_VERIFY='1', BOROUGH_RENDER_PROFILE='1')
    log = OUTPUT / f'{name}.log'
    with log.open('w') as stream:
        subprocess.run([GODOT, '--path', str(ROOT / 'src/Borough.Godot'), *args],
                       cwd=ROOT, env=environment, stdout=stream, stderr=subprocess.STDOUT,
                       check=True, timeout=120)
    assert 'ERROR:' not in log.read_text(), log


run('matrix', ['res://FacadeStudy.tscn', '--', '--facade-capture', str(OUTPUT)])
for name, distance, overlay in [('near', 160, 'off'), ('rung', 160, 'rung'),
                                 ('age', 160, 'age'), ('city', 900, 'off')]:
    script = OUTPUT / f'{name}.drive'
    # Each run has one shoot: multiple shoots in a single Tick coalesce in the shell.
    script.write_text('\n'.join(['600 pause', f'600 focus 88 92 {distance}', '600 tilt 32',
        f'600 overlay {overlay}', f'600 draw {OUTPUT / (name + ".tsv")}',
        f'600 draw {OUTPUT / (name + "-repeat.tsv")}',
        f'600 shoot {OUTPUT / (name + ".png")}', '600 quit']) + '\n')
    run(name, ['--', '--ruleset', 'rulesets/shopping.toml', '--citizens', '400',
               '--start-at', '600', '--drive', str(script)], profile=True)
    assert (OUTPUT / f'{name}.tsv').read_bytes() == (OUTPUT / f'{name}-repeat.tsv').read_bytes()
    def uploads(suffix):
        return [line for line in (OUTPUT / f'{name}{suffix}.tsv.profile.tsv').read_text().splitlines()
                if line.startswith('render\t')]
    assert uploads('') == uploads('-repeat'), 'unchanged paused draw uploaded geometry'

images = [f'facade-{i:02}.png' for i in range(12)] + ['near.png', 'rung.png', 'age.png', 'city.png']
for name in images:
    assert (OUTPUT / name).is_file(), name
(OUTPUT / 'manifest.json').write_text(json.dumps({
    'fixture': 'FacadeStudy: brick/plaster × residential/commercial × day/night/debug wash',
    'columns': ['vacant', 'half occupied', 'fully occupied', 'abandoned'],
    'live': {'ruleset': 'shopping.toml', 'citizens': 400, 'seed': 0, 'tick': 600, 'focus': [88, 92]},
    'images': images, 'performance_claim': False,
}, indent=2) + '\n')
(OUTPUT / 'index.html').write_text('<!doctype html><meta charset="utf-8"><title>Live facade review</title>'
    '<style>body{background:#202624;color:#ece9df;font:18px system-ui;margin:24px}img{width:100%}figure{margin:24px 0}</style>'
    '<h1>Live facade review</h1><p>Controlled shader samples followed by the running city. '
    'Appearance is provisional; these are not performance measurements.</p>'
    + ''.join(f'<figure><figcaption>{name}</figcaption><a href="{name}"><img src="{name}"></a></figure>' for name in images))
print(f'PASS: facade captures and unchanged paused uploads. Review: {OUTPUT / "index.html"}', flush=True)

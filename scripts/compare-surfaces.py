#!/usr/bin/env python3
"""Controlled material and live-overlay comparisons. Build the Godot Debug project first."""
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
OUT = Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else ROOT / 'artifacts/visual-study/surfaces-2'
OUT.mkdir(parents=True, exist_ok=True)
GODOT = os.environ.get('GODOT_BIN', 'godot')


def run(name, args, style=2):
    with (OUT / f'{name}.log').open('w') as log:
        subprocess.run([GODOT, '--path', str(ROOT / 'src/Borough.Godot'), *args], cwd=ROOT,
            env=dict(os.environ, BOROUGH_OVERLAY_STYLE=str(style), BOROUGH_RENDER_VERIFY='1', BOROUGH_RENDER_PROFILE='1'),
            stdout=log, stderr=subprocess.STDOUT, check=True, timeout=120)
    assert 'ERROR:' not in (OUT / f'{name}.log').read_text(), name


run('materials', ['res://FacadeStudy.tscn', '--', '--material-capture', str(OUT)])
shots = [f'material-{i:02}.png' for i in range(6)]
for style, label in enumerate(['flat', 'faces', 'edges']):
    for scale, distance in [('near', 160), ('city', 900)]:
        name = f'{label}-{scale}'
        script = OUT / f'{name}.drive'
        script.write_text('\n'.join(['600 pause', f'600 focus 88 92 {distance}', '600 tilt 32',
            '600 overlay age', f'600 draw {OUT / (name + ".tsv")}',
            f'600 draw {OUT / (name + "-repeat.tsv")}', f'600 shoot {OUT / (name + ".png")}', '600 quit']) + '\n')
        run(name, ['--', '--ruleset', 'rulesets/shopping.toml', '--citizens', '400', '--start-at', '600', '--drive', str(script)], style)
        assert (OUT / f'{name}.tsv').read_bytes() == (OUT / f'{name}-repeat.tsv').read_bytes()
        def uploads(suffix):
            return [line for line in (OUT / f'{name}{suffix}.tsv.profile.tsv').read_text().splitlines() if line.startswith('render\t')]
        assert uploads('') == uploads('-repeat'), 'unchanged paused scene uploaded geometry'
        shots.append(name + '.png')

for name in shots:
    assert (OUT / name).is_file(), name
sources = [
    ('Cities: Skylines II — electricity infoview', 'https://www.paradoxinteractive.com/games/cities-skylines-ii/features/electricity-water'),
    ('SimCity — Data Maps, manual pp. 8–9', 'https://akamai.cdn.ea.com/eadownloads/u/f/manuals/GAME-SIMCITY/SimCity_2013.pdf'),
    ('Painted Plaster Wall', 'https://polyhaven.com/a/painted_plaster_wall'),
    ('Worn Plaster Wall', 'https://polyhaven.com/a/worn_plaster_wall'),
    ('Damaged Plaster — used as a wear mask', 'https://polyhaven.com/a/damaged_plaster'),
    ('Poly Haven CC0 licence', 'https://polyhaven.com/license'),
]
files = ['src/Borough.Godot/' + name for name in ['buildings.gdshader', 'overlay-buildings.gdshader',
    'FacadeMaterials.cs', 'FacadeStudy.cs', 'Main.Ground.cs', 'Main.Massing.cs', 'Main.Rendering.cs']]
(OUT / 'manifest.json').write_text(json.dumps(dict(
    status='Comparison only. No texture, wear strength, scale or overlay style approved.',
    live=dict(ruleset='shopping.toml', seed=0, citizens=400, tick=600, focus=[88, 92], tilt=32),
    material_columns=['brick occupied', 'brick abandoned', 'plaster occupied', 'plaster abandoned'],
    material_variants=['untreated', 'painted plaster', 'worn plaster'],
    hashes={f: hashlib.sha256((ROOT / f).read_bytes()).hexdigest() for f in files},
    sources=sources, images=shots, performance_claim=False), indent=2) + '\n')
html = '''<!doctype html><meta charset="utf-8"><title>Surfaces and overlays — comparison 2</title>
<style>body{background:#202624;color:#eee8df;font:17px system-ui;margin:24px auto;max-width:1500px;padding:0 20px}a{color:#bbdac6}img{width:100%}select,button{font:inherit;padding:8px;margin:8px 12px 8px 0}section{margin:40px 0}h1{font-size:30px}p{max-width:900px;line-height:1.55}</style>
<h1>Surfaces and overlays</h1><p>Comparison studies, not final art. The base paint and geometry stay fixed.
The worn-plaster candidate is deliberately more weathered, including on occupied Buildings. Wear has no simulated age or progression yet.</p>
<section><h2>Material and wear</h2><label>Surface <select id="material"><option value="0">Untreated reference</option><option value="1" selected>Painted plaster + layered wear</option><option value="2">Worn plaster + layered wear</option></select></label>
<label>Distance <select id="distance"><option value="0">Close</option><option value="3">Street</option></select></label>
<p>Left to right: brick occupied, brick abandoned, plaster occupied, plaster abandoned.</p><a id="materialLink"><img id="materialImage"></a></section>
<section><h2>Overlay shape and context</h2><label>Treatment <select id="style"><option value="flat">Flat reference</option><option value="faces">Fixed face shading</option><option value="edges" selected>Face shading + edges</option></select></label>
<label>Distance <select id="scale"><option value="near">Near</option><option value="city">City</option></select></label>
<p>Same city, Tick, camera and Age values. Face shading uses a fixed diagram light, independent of the Day. Neutral surroundings retain shape. Edges follow unit-mesh bounds; this is not a complete screen-space silhouette pass.</p><a id="overlayLink"><img id="overlayImage"></a></section>
<section><h2>Reference starting points</h2><p>The Cities: Skylines II publisher screenshot retains shaded geometry inside categorical colour and a muted, visible surrounding city. Our interpretation is to preserve shape and context; its rendering implementation was not inspected. SimCity's manual documents task-specific Data Maps and paired system information, not a shader recipe.</p><ul>'''
html += ''.join(f'<li><a href="{url}">{title}</a></li>' for title, url in sources)
html += '''</ul></section><script>
function refresh(){let n=Number(document.getElementById('material').value)+Number(document.getElementById('distance').value);let m=`material-${String(n).padStart(2,'0')}.png`;let o=`${document.getElementById('style').value}-${document.getElementById('scale').value}.png`;for(let [id,src] of [['material',m],['overlay',o]]){document.getElementById(id+'Image').src=src;document.getElementById(id+'Image').alt=src;document.getElementById(id+'Link').href=src;}}
document.querySelectorAll('select').forEach(s=>s.addEventListener('change',refresh));refresh();</script>'''
(OUT / 'index.html').write_text(html)
print(f'PASS: comparisons captured and paused uploads unchanged. {OUT / "index.html"}', flush=True)

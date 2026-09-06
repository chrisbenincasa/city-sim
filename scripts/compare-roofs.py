#!/usr/bin/env python3
"""Capture production roof meshes, surfaces and live overlays after a Godot Debug build."""
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
OUT = Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else ROOT / 'artifacts/visual-study/roof-proportions'
OUT.mkdir(parents=True, exist_ok=True)

def run(name, args):
    with (OUT / f'{name}.log').open('w') as log:
        subprocess.run([os.environ.get('GODOT_BIN', 'godot'), '--path', str(ROOT / 'src/Borough.Godot'), *args],
            cwd=ROOT, env=dict(os.environ, BOROUGH_RENDER_VERIFY='1', BOROUGH_RENDER_PROFILE='1', BOROUGH_OVERLAY_STYLE='2'),
            stdout=log, stderr=subprocess.STDOUT, check=True, timeout=120)
    assert 'ERROR:' not in (OUT / f'{name}.log').read_text(), name

run('specimens', ['res://RoofStudy.tscn', '--', str(OUT / 'after')])
for name, wash, distance in [('age-near','age',160), ('age-city','age',900), ('daylight-near','none',160), ('rung-near','rung',160)]:
    script = OUT / f'{name}.drive'
    script.write_text('\n'.join(['600 pause', f'600 focus 88 92 {distance}', '600 tilt 32',
        f'600 overlay {wash}', f'600 draw {OUT / (name + ".tsv")}',
        f'600 draw {OUT / (name + "-repeat.tsv")}', f'600 shoot {OUT / (name + ".png")}', '600 quit']) + '\n')
    run(name, ['--', '--ruleset','rulesets/shopping.toml','--citizens','400','--start-at','600','--drive',str(script)])
    assert (OUT / f'{name}.tsv').read_bytes() == (OUT / f'{name}-repeat.tsv').read_bytes()
    def uploads(suffix):
        return [s for s in (OUT / f'{name}{suffix}.tsv.profile.tsv').read_text().splitlines() if s.startswith('render\t')]
    assert uploads('') == uploads('-repeat'), name
    assert (OUT / f'{name}.png').is_file(), name

files = ['RoofMeshes.cs','RoofMaterials.cs','RoofStudy.cs','overlay-buildings.gdshader','surfaces.gdshader','Main.cs','Main.Assets.cs','Main.Massing.cs','Main.Rendering.cs','assets/city/material-study/roof-sources.json']
(OUT / 'manifest.json').write_text(json.dumps({
    'sources': {f: hashlib.sha256((ROOT / 'src/Borough.Godot' / f).read_bytes()).hexdigest() for f in files},
    'standing':'Smaller roof spans, wall-coloured gable ends and photographed slate at source scale; provisional art.'
}, indent=2)+'\n')
(OUT / 'index.html').write_text('''<!doctype html><meta charset="utf-8"><title>Roof proportions and scale</title>
<style>body{background:#202528;color:#e5e4df;font:17px system-ui;margin:28px}button,select{font:inherit;padding:8px;margin:5px}img{display:block;width:100%;max-width:1500px}p{max-width:1050px}a{color:#bdcfdf}</style>
<h1>Roof proportions and scale — comparison</h1>
<p>The live fixture contains large blocks: its median drawn width and depth are 28 m and 22 m; walls are 7–10.5 m high. Broad footprints now have two narrower roof spans. The truncated pyramid is replaced; hips have a short ridge; gable ends continue the walls. The city footprints stay identical so the shape change can be judged against the original.</p>
<p>The material is <a href="https://polyhaven.com/a/roof_slates_02">Rob Tuytel’s Roof Slates 02 (CC0)</a>, at the source’s documented 3 m width. The specimen view includes a deliberately doubled texture scale and 1.75 m figures. Surface colour follows the existing roof palette; this remains a material candidate. The previous procedural tiles were 32 × 24 cm.</p>
<select id="choice">
<option value="daylight-near.png">New roof forms + photographed surface — live city</option>
<option value="../roofs/daylight-near.png">Previous roof forms + procedural surface — same city/camera</option>
<option value="after/roof-4.png">New forms — photographed surface at source scale</option>
<option value="after/roof-5.png">New forms — photographed surface enlarged 2×</option>
<option value="after/roof-3.png">New forms — previous procedural courses</option>
<option value="after/roof-2.png">New forms — original grid surface</option>
<option value="after/roof-1.png">New forms — crease outlines</option>
<option value="age-near.png">Live city — Age overlay</option>
<option value="age-city.png">Live city — Age overlay at city distance</option>
<option value="rung-near.png">Live city — Rung overlay</option>
</select><img id="shot" src="daylight-near.png">
<p>These changes address oversized roof volumes and surface noise. Continuous gutters, built ridge fittings, roof junctions and variety still need art review. The large Building footprints are a separate question from tile scale.</p>
<p><a href="../surfaces-2/index.html">Plaster and wear comparison</a> · <a href="manifest.json">Capture source hashes</a></p>
<script>choice.onchange=()=>shot.src=choice.value;</script>''')
print(OUT / 'index.html')

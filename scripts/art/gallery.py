"""Assemble the locally rendered blockout review and record its source hashes."""
import hashlib
import json
from pathlib import Path

root = Path(__file__).resolve().parents[2]
out = root / 'artifacts/visual-study'
views = [('block', 'The specimen', 'Six homes, two shops and a school around open ground. Two residential frontage widths; two and three storeys.'),
         ('street', 'Street and corner', 'Shop glazing and canopies establish use. The school entrance and court remain visible through the gaps.'),
         ('facade', 'The imported module', 'Blender-authored openings, sill and mullion, assembled in Godot. This is a pipeline proof, not a finished treatment.'),
         ('neighbourhood', 'Repetition screen', 'Nine identical copies deliberately reveal the repeated stamp. No population or performance claim.')]
source_paths = ['scripts/art/facade.py', 'src/Borough.Godot/VisualStudy.cs',
                'art/visual-study/specimen.json', 'art/visual-study/facade.json',
                'src/Borough.Godot/assets/visual-study/facade.glb']
manifest = {'sources': {p: hashlib.sha256((root / p).read_bytes()).hexdigest() for p in source_paths},
            'captures': [json.loads((out / (view + '.json')).read_text()) for view, _, _ in views]}
(out / 'run.json').write_text(json.dumps(manifest, indent=2) + '\n')
figures = '\n'.join(f'<section id="{v}"><h2>{title}</h2><p>{desc}</p><a href="{v}.png"><img src="{v}.png" alt="Godot noon render: {title}"></a></section>' for v, title, desc in views)
(out / 'index.html').write_text('''<!doctype html><html lang="en"><meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1"><title>0063 · Blockout review</title>
<style>body{margin:0;background:#182025;color:#e9ecea;font:17px/1.6 system-ui}main{max-width:1200px;margin:auto;padding:40px 24px}h1{font-size:40px;line-height:1.15}h2{margin-bottom:0}p{max-width:850px;color:#c2cdcf}a{color:#b5d6d0}nav{display:flex;gap:24px;flex-wrap:wrap}img{width:100%;display:block;border-radius:8px}section{margin:48px 0}small{color:#9fb4b7}</style>
<main><small>BOROUGH / 0063 / SPECIMEN v1</small><h1>One block, before the treatments</h1>
<p>Plain Godot blockout in fixed noon light. Review the layout, building mix, proportions and cameras. Material-rich, sculpted and middle treatments follow layout agreement.</p>
<p>This is an isolated art fixture with provisional authored dimensions, not live Lots. No simulation, occupancy, abandonment or material finish is being demonstrated.</p>
<nav><a href="#block">Block</a><a href="#street">Street</a><a href="#facade">Facade</a><a href="#neighbourhood">Neighbourhood</a><a href="run.json">Run manifest</a></nav>
''' + figures + '<p>Review: is this a representative specimen for the three treatments? Name any layout or building-family changes before it is frozen.</p></main></html>')

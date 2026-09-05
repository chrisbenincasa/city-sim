"""Validate matched captures and assemble the first architectural treatment test."""
import hashlib
import json
from pathlib import Path
import struct

root = Path(__file__).resolve().parents[2]
out = root / 'artifacts/visual-study'
views = ['block', 'street', 'facade', 'neighbourhood']
captures = {}
for view in views:
    base = json.loads((out / 'material-rich-baseline' / (view + '.json')).read_text())
    rich = json.loads((out / 'material-rich' / (view + '.json')).read_text())
    assert base['treatment'] == 'blockout' and rich['treatment'] == 'material-rich'
    for key in base:
        if key != 'treatment':
            assert base[key] == rich[key], (view, key, base[key], rich[key])
    captures[view] = {'blockout': base, 'material-rich': rich}
    for directory in ['material-rich-baseline', 'material-rich']:
        assert (out / directory / (view + '.png')).read_bytes()[:8] == b'\x89PNG\r\n\x1a\n'

sources = ['scripts/art/facade.py', 'src/Borough.Godot/VisualStudy.cs',
           'src/Borough.Godot/VisualStudy.Materials.cs', 'art/visual-study/specimen.json',
           'art/visual-study/facade-material-rich.json']
geometry = {}
for stem in ['facade', 'facade-material-rich']:
    path = root / 'src/Borough.Godot/assets/visual-study' / (stem + '.glb')
    sources.append(str(path.relative_to(root)))
    data = path.read_bytes()
    magic, version, size, json_size, chunk_type = struct.unpack_from('<5I', data)
    assert magic == 0x46546c67 and version == 2 and size == len(data) and chunk_type == 0x4e4f534a
    asset = json.loads(data[20:20+json_size])
    primitives = [p for m in asset['meshes'] for p in m['primitives']]
    assert all(p.get('mode', 4) == 4 for p in primitives)
    geometry[stem] = {
        'triangles': sum(asset['accessors'][p['indices']]['count'] // 3 for p in primitives),
        'mesh_primitives': len(primitives), 'materials': len(asset['materials']),
        'embedded_images': len(asset.get('images', [])), 'file_bytes': len(data)}
manifest = {'sources': {p: hashlib.sha256((root / p).read_bytes()).hexdigest() for p in sources},
            'captures': captures, 'single_module_geometry': geometry,
            'limitations': 'First architectural test only. Trees, car and walker retain blockout shapes. No motion, state response or performance acceptance.'}
(out / 'material-rich-run.json').write_text(json.dumps(manifest, indent=2) + '\n')
figures = ''.join(f'''<section><h2>{view.title()}</h2><div class="pair"><figure><figcaption>Plain blockout</figcaption><a href="material-rich-baseline/{view}.png"><img src="material-rich-baseline/{view}.png" alt="Plain {view}"></a></figure><figure><figcaption>Material-rich · first test</figcaption><a href="material-rich/{view}.png"><img src="material-rich/{view}.png" alt="Material-rich {view}"></a></figure></div></section>''' for view in views)
rows = ''.join(f'<tr><th>{key.replace("_", " ")}</th><td>{geometry["facade"][key]:,}</td><td>{geometry["facade-material-rich"][key]:,}</td></tr>' for key in geometry['facade'])
(out / 'material-rich.html').write_text('''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>0063 · First material-rich test</title>
<style>body{margin:0;background:#182025;color:#e9ecea;font:17px/1.6 system-ui}main{max-width:1600px;padding:32px 24px;margin:auto}h1{font-size:38px;line-height:1.2}p{max-width:900px;color:#c2cdcf}a{color:#b5d6d0}.pair{display:grid;grid-template-columns:1fr 1fr;gap:16px}figure{margin:0}img{width:100%;display:block}figcaption{padding:8px 0}section{margin:36px 0}td,th{text-align:left;padding:8px 28px 8px 0;border-bottom:1px solid #39454a}@media(max-width:800px){.pair{grid-template-columns:1fr}}button{font:inherit;background:#b5d6d0;border:0;padding:8px 16px;cursor:pointer;border-radius:4px}</style>
<main><small>BOROUGH / 0063 / PROVISIONAL SPECIMEN</small><h1>The first material-rich test</h1>
<p>Matched noon views: same building layout, widths, storeys, palette, camera and exposure. The first architectural pass adds window frames, sill drips, shallow masonry relief, roughness variation, door joinery and roof edges.</p>
<p>The specimen is still awaiting layout review. Trees, car and walker remain blockouts. This is an early test, not the complete treatment or a live city.</p>
<p><a href="material-rich-run.json">Capture and geometry manifest</a> · <a href="index.html">Original blockout review</a></p>
''' + figures + '<h2>One reusable facade module</h2><p>Static asset counts only. These are not frame-time or target-population measurements.</p><table><tr><th>Quantity</th><th>Blockout</th><th>First test</th></tr>' + rows + '</table></main></html>')
print('MATCHED_CAPTURE_OK: four view pairs; dimensions, camera and capture settings agree.')
print(json.dumps(geometry))

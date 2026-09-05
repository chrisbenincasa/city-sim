"""Validate matched palette-only captures and publish their review gallery."""
import hashlib
import json
import struct
from pathlib import Path

root = Path(__file__).resolve().parents[2]
out = root / 'artifacts/visual-study/palettes'
palettes = ['original', 'warm-slate', 'earth-terracotta']
subjects = [('detached-house', 'realistic'), ('residential-tower', 'realistic'),
            ('residential-tower', 'city-builder'), ('car', 'realistic')]
original_run = json.loads((out.parent / 'models/run.json').read_text())
controls = original_run['controls_checked'] + ['triangles', 'surfaces', 'bounds', 'treatment']
records = []
sections = []
sources = ['art/visual-study/palettes.json', 'art/visual-study/preferences.json',
           'src/Borough.Godot/KitStudy.cs', 'src/Borough.Godot/KitStudy.Styles.cs',
           'src/Borough.Godot/KitStudy.Palettes.cs', 'scripts/art/palettes-gallery.py',
           'scripts/art/review.sh']
for specimen, treatment in subjects:
    asset = f'src/Borough.Godot/assets/visual-study/kit/models/{specimen}-{treatment}.glb'
    sources.append(asset)
    assert hashlib.sha256((root / asset).read_bytes()).hexdigest() == original_run['sources'][asset], asset
    sections.append(f'<section><h2>{specimen.replace("-", " ").title()} · {treatment}</h2>')
    for view in ['whole', 'detail']:
        if view == 'detail':
            sections.append('<details><summary>Matched close views</summary>')
        sections.append('<div class="row">')
        baseline = next(c for c in original_run['captures'] if c['specimen'] == specimen and c['treatment'] == treatment and c['view'] == view)
        for palette in palettes:
            stem = f'{specimen}-{treatment}-{palette}-{view}'
            record = json.loads((out / (stem + '.json')).read_text())
            assert record['palette'] == palette and record['comparison'] == 'palette-round-1'
            assert all(record[k] == baseline[k] for k in controls), stem
            png = (out / (stem + '.png')).read_bytes()
            assert png[:8] == b'\x89PNG\r\n\x1a\n'
            assert struct.unpack('>II', png[16:24]) == (1440, 960)
            records.append(record)
            sections.append(f'<figure data-palette="{palette}"><figcaption>{palette.replace("-", " ").title()}</figcaption><a href="{stem}.png"><img src="{stem}.png" alt="{specimen}, {treatment}, {palette}, {view}" loading="lazy"></a></figure>')
        sections.append('</div>')
        if view == 'detail':
            sections.append('</details>')
    sections.append('</section>')
manifest = {'comparison': 'palette-round-1', 'varied': ['material albedo colours only'],
            'controls_checked': controls, 'captures': records,
            'sources': {p: hashlib.sha256((root / p).read_bytes()).hexdigest() for p in sources}}
(out / 'run.json').write_text(json.dumps(manifest, indent=2) + '\n')
(out / 'index.html').write_text('''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Borough · Grounded palette samples</title>
<style>body{margin:0;background:#202626;color:#eeeae2;font:17px/1.6 system-ui}main{max-width:1800px;margin:auto;padding:32px 24px}h1{font-size:40px;line-height:1.2}p{max-width:1050px;color:#c9ccc6}a{color:#c1d2c8}.row{display:grid;grid-template-columns:repeat(3,1fr);gap:16px}figure{margin:0}figure[hidden]{display:none}img{width:100%;display:block}figcaption{padding:10px 0;font-size:20px}section{margin:48px 0}summary{padding:16px 0;cursor:pointer}.switch{position:sticky;top:0;background:#202626f5;padding:14px 0;display:flex;gap:12px;flex-wrap:wrap;z-index:1}button{font:inherit;padding:8px 14px;color:inherit;background:#34403b;border:1px solid #7a867e;border-radius:4px;cursor:pointer}button[aria-pressed=true]{background:#c1d2c8;color:#202626}body.single .row{grid-template-columns:1fr;max-width:1200px}@media(max-width:900px){.row{grid-template-columns:1fr}}</style>
<main><small>BOROUGH / 0063 / PALETTE ROUND 1</small><h1>Warmth with more weight</h1>
<p>The realistic house, tower and car you liked, plus the city-builder tower. Each appears in the original palette and two more grounded alternatives. The model files, surface textures, roughness, camera, exposure and noon lighting are held fixed; only material colours change.</p>
<p><b>Original:</b> cream, lavender and mint. <b>Warm slate:</b> warm masonry, dark slate roofs, muted brick and deep green car paint. <b>Earth terracotta:</b> sandy plaster, clay roofs, subdued stone accents and oxide-red car paint. Both alternatives darken the glazing and reduce the bright cream trim.</p>
<p>This tests the palette concern on preferred geometry. It is not a new geometry comparison or a final selection. Glass remains opaque; geometry is still an initial procedural study. These isolated fixtures do not demonstrate live simulation or rendering performance.</p>
<nav><a href="../models/index.html">First model round</a> · <a href="../kit/index.html">Original kit</a> · <a href="../extremes/index.html">Extremes</a> · <a href="../styles/index.html">Earlier styles</a> · <a href="run.json">Run manifest</a></nav>
<div class="switch"><button data-palette="all" aria-pressed="true">All three</button><button data-palette="original" aria-pressed="false">Original</button><button data-palette="warm-slate" aria-pressed="false">Warm slate</button><button data-palette="earth-terracotta" aria-pressed="false">Earth terracotta</button></div>
''' + '\n'.join(sections) + '''</main><script>document.querySelectorAll('button[data-palette]').forEach(button=>button.addEventListener('click',()=>{const p=button.dataset.palette;document.body.classList.toggle('single',p!=='all');document.querySelectorAll('figure').forEach(f=>f.hidden=p!=='all'&&f.dataset.palette!==p);document.querySelectorAll('button').forEach(b=>b.setAttribute('aria-pressed',String(b===button)));}));</script></html>''')
print('PALETTE_COMPARISON_OK: 4 unchanged GLBs; 24 PNGs at 1440×960; geometry and camera/light controls match the first model round.')

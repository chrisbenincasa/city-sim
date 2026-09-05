"""Compare texture scale against the preserved, positively reviewed fidelity pilot."""
import json,hashlib,struct
from pathlib import Path
root=Path(__file__).resolve().parents[2];out=root/'artifacts/visual-study/brick-scale'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
baseline=root/'artifacts/visual-study/fidelity';prior=json.loads((baseline/'run.json').read_text())
asset='src/Borough.Godot/assets/visual-study/fidelity/attached-townhouse.glb'
assert sha(root/asset)==prior['source_sha256'][asset]
controls=['subject','palette','view','instances','camera','target','fov','viewport','sunRotation','sunEnergy','sunAngularDistance','ambientEnergy','ssao','taa','exposure','light','godot','renderer','tick','population']
records=[];images={};rows=[]
for palette in ['warm-slate','earth-terracotta']:
 for view in ['whole','close']:
  stem=f'attached-townhouse-{palette}-{view}';reference=json.loads((baseline/(stem+'.json')).read_text())
  rows.append(f'<h2>{palette.replace("-"," ").title()} · {view}</h2><div class="row">')
  for folder,scale,label in [('smaller',.75,'Smaller · 0.75×'),('baseline',1,'Liked pilot · 1×'),('larger',1.25,'Larger · 1.25×')]:
   directory=baseline if folder=='baseline' else out/folder
   record=json.loads((directory/(stem+'.json')).read_text())
   assert all(record[k]==reference[k] for k in controls),(folder,stem)
   assert record.get('brickScale',1)==scale
   png=directory/(stem+'.png');b=png.read_bytes()
   assert b[:8]==b'\x89PNG\r\n\x1a\n' and struct.unpack('>II',b[16:24])==(1440,960)
   if folder=='baseline':assert sha(png)==prior['capture_sha256'][str(png.relative_to(root))]
   records.append(record);images[str(png.relative_to(root))]=sha(png)
   url=('../fidelity/' if folder=='baseline' else folder+'/')+stem+'.png'
   rows.append(f'<figure><figcaption>{label}</figcaption><a href="{url}"><img src="{url}" alt="{label}, {palette}, {view}" loading="lazy"></a></figure>')
  rows.append('</div>')
paths=[asset,'src/Borough.Godot/ExpandedStudy.cs','scripts/art/brick-scale-gallery.py','scripts/art/review.sh','art/visual-study/preferences.json','art/visual-study/fidelity-1/research.json']
(out/'run.json').write_text(json.dumps(dict(comparison='brick-scale-1',controls_checked=controls,captures=records,capture_sha256=images,source_sha256={p:sha(root/p) for p in paths},geometry='Unchanged GLB, verified against positively reviewed fidelity pilot.',texture_scaling='Diffuse, normal and roughness use the same uniform UV scale. Brick and mortar scale together. Approximate apparent brick lengths: 18, 24, 30 cm; inferred from image, not measured masonry.'),indent=2)+'\n')
(out/'index.html').write_text('''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Borough · Brick sizes</title><style>body{background:#202626;color:#eeeae2;font:17px/1.6 system-ui;margin:0}main{max-width:1900px;margin:auto;padding:32px 24px}h1{font-size:40px}p{max-width:1000px;color:#c9ccc6}a{color:#c1d2c8}.row{display:grid;grid-template-columns:repeat(3,1fr);gap:14px}figure{margin:0}figcaption{padding:10px 0}img{width:100%;display:block}h2{margin-top:42px}@media(max-width:1000px){.row{grid-template-columns:1fr}}</style><main><small>BOROUGH / 0063 / MATERIAL SCALE</small><h1>One texture, three brick sizes</h1><p>The fidelity pilot is the positively reviewed direction. This comparison varies brick scale while keeping its model, texture files, palette, camera and lighting fixed. The centre images are the preserved pilot captures.</p><p>Approximate apparent brick lengths: 18, 24 and 30 cm. These are visual estimates from the texture. Mortar and weathering scale with the bricks; changing brick proportions or mortar thickness independently would need a different mapping or material treatment.</p><p><a href="../fidelity/index.html">Fidelity pilot and research</a> · <a href="run.json">Validation manifest</a> · <a href="https://polyhaven.com/a/brick_wall_001">Brick material: Poly Haven, CC0</a></p>'''+''.join(rows)+'</main></html>')
print('BRICK_SCALE_OK: unchanged GLB and texture files; 8 new matched captures; 4 preserved pilot captures.')

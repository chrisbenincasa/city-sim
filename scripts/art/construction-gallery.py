"""Publish the construction balance pilot against preserved first-pass captures."""
import json,hashlib,struct
from pathlib import Path
root=Path(__file__).resolve().parents[2];out=root/'artifacts/visual-study/construction';baseline=out.parent/'expanded'
source=json.loads((root/'art/visual-study/construction-study-1/manifest.json').read_text())
assert source['source_sha256']==hashlib.sha256((root/source['authority']).read_bytes()).hexdigest()
controls=['camera','target','fov','viewport','sunRotation','sunEnergy','sunAngularDistance','ambientEnergy','ssao','taa','exposure','light','godot','renderer','tick','population']
sections=[];records=[];captures={}
for asset in source['assets']:
 subject=asset['specimen'];sections.append(f'<section><h2>{subject.replace("-"," ").title()}</h2>')
 for palette in ['warm-slate','earth-terracotta']:
  sections.append(f'<h3>{palette.replace("-"," ").title()}</h3>')
  for view in ['whole','close']:
   if view=='close':sections.append('<details><summary>Matched close views</summary>')
   stem=f'{subject}-{palette}-{view}';old=json.loads((baseline/(stem+'.json')).read_text());new=json.loads((out/(stem+'.json')).read_text())
   assert all(old[k]==new[k] for k in controls),stem
   assert new['comparison']=='construction-study-1' and new['subject']==subject and new['palette']==palette
   old_asset=next(a for a in json.loads((root/'art/visual-study/expanded-kit-1/manifest.json').read_text())['assets'] if a['specimen']==subject)
   assert old_asset['geometry_sha256']!=asset['geometry_sha256'],subject
   size=[asset['bounds_blender'][1][i]-asset['bounds_blender'][0][i] for i in [0,2,1]]
   assert all(abs(new['instances'][0]['boundsSize'][i]-size[i])<.01 for i in range(3))
   records.append(new)
   for path in [baseline/(stem+'.png'),out/(stem+'.png')]:
    b=path.read_bytes();assert b[:8]==b'\x89PNG\r\n\x1a\n' and struct.unpack('>II',b[16:24])==(1440,960)
    captures[str(path.relative_to(root))]=hashlib.sha256(b).hexdigest()
   sections.append(f'<div class="pair"><figure><figcaption>First expanded pass</figcaption><a href="../expanded/{stem}.png"><img src="../expanded/{stem}.png" alt="First pass {subject} {palette} {view}" loading="lazy"></a></figure><figure><figcaption>Construction balance</figcaption><a href="{stem}.png"><img src="{stem}.png" alt="Construction balance {subject} {palette} {view}" loading="lazy"></a></figure></div>')
   if view=='close':sections.append('</details>')
 sections.append('</section>')
paths=['scripts/art/construction-study.py','scripts/art/expanded-kit.py','scripts/art/model-round.py','scripts/art/construction-gallery.py','scripts/art/review.sh','src/Borough.Godot/ExpandedStudy.cs','src/Borough.Godot/ExpandedStudy.tscn','art/visual-study/construction-study-1/manifest.json','art/visual-study/palettes.json','art/visual-study/preferences.json']
paths += ['src/Borough.Godot/assets/visual-study/construction/'+a['asset'] for a in source['assets']]
(out/'run.json').write_text(json.dumps(dict(comparison='construction-study-1',controls_checked=controls,captures=records,source_sha256={p:hashlib.sha256((root/p).read_bytes()).hexdigest() for p in paths},capture_sha256=captures),indent=2)+'\n')
(out/'index.html').write_text('''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Borough · Finding the realism balance</title><style>body{margin:0;background:#202626;color:#eeeae2;font:17px/1.6 system-ui}main{max-width:1600px;margin:auto;padding:36px 24px}h1{font-size:40px;line-height:1.2}p{max-width:1100px;color:#c9ccc6}a{color:#c1d2c8}.pair{display:grid;grid-template-columns:1fr 1fr;gap:18px}figure{margin:0}img{width:100%;display:block}figcaption{padding:10px 0;font-size:20px}section{margin:48px 0}summary{padding:16px 0;cursor:pointer}@media(max-width:850px){.pair{grid-template-columns:1fr}}</style><main><small>BOROUGH / 0063 / CONSTRUCTION BALANCE</small><h1>A little more believable</h1><p>A focused response to the cartoonish feel of the first expanded kit. Warm slate and earth remain; camera and noon lighting match the first pass. Geometry, joinery and material response change. This is a balance test, not a final direction.</p><p><b>Townhouse:</b> a continuous terrace roof, finer inset windows, smaller doors, shared chimney stacks, slate/tile courses and drainage. <b>Shop:</b> a recessed storefront with slender framing, a small display, thinner fascia and canopy. <b>Factory:</b> thinner rooflight structure, inset loading shutters, brick piers and smaller office openings.</p><p>Wall openings are real apertures. Glazing is partially transparent with shallow window pockets and occasional blinds; masonry has restrained normal/roughness maps. These are not furnished interiors or simulated occupancy. The study remains procedural and the narrow pilot has not yet been applied to the whole kit.</p><nav><a href="../expanded/index.html">Full expanded kit and street study</a> · <a href="../palettes/index.html">Earlier palette samples</a> · <a href="run.json">Comparison manifest</a></nav>'''+'\n'.join(sections)+'</main></html>')
print('CONSTRUCTION_COMPARISON_OK: 3 changed geometries; 12 new captures matched to 12 preserved first-pass views; shared cameras and noon light.')

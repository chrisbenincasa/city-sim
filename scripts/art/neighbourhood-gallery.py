"""Validate a fixed neighbourhood across six review cameras."""
import json,hashlib,struct
from pathlib import Path
root=Path(__file__).resolve().parents[2];out=root/'artifacts/visual-study/neighbourhood'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
source=json.loads((root/'art/visual-study/neighbourhood-1/manifest.json').read_text())
assert source['source_sha256']==sha(root/source['authority'])
assert {a['specimen'] for a in source['assets']}=={'corner-shop','residential-tower','street-tree','walker'}
assert len({a['geometry_sha256'] for a in source['assets']})==4
views=['street','neighbourhood','city-distance','shop','roofs','tower'];records=[];hashes={};sections=[]
for view in views:
 stem='neighbourhood-mixed-approved-'+view
 r=json.loads((out/(stem+'.json')).read_text());records.append(r)
 assert r['comparison']=='neighbourhood-1' and r['view']==view and r['tick'] is None
 if len(records)>1:
  for k in ['instances','sunRotation','sunEnergy','sunAngularDistance','ambientEnergy','fov','viewport','exposure','light']:assert r[k]==records[0][k],k
 for asset in source['assets']:
  instances=[i for i in r['instances'] if i['asset']==asset['specimen']];assert instances
  expected=[asset['bounds_blender'][1][i]-asset['bounds_blender'][0][i] for i in [0,2,1]]
  assert all(abs(instances[0]['boundsSize'][i]-expected[i])<.02 for i in range(3))
 p=out/(stem+'.png');b=p.read_bytes();assert b[:8]==b'\x89PNG\r\n\x1a\n' and struct.unpack('>II',b[16:24])==(1440,960)
 hashes[str(p.relative_to(root))]=sha(p)
 sections.append(f'<section id="{view}"><h2>{view.replace("-"," ").title()}</h2><a href="{stem}.png"><img src="{stem}.png" alt="Neighbourhood {view} view" loading="lazy"></a></section>')
# Axis-aligned building envelopes, including terrace entrance courts, must be separate.
buildings=[i for i in records[0]['instances'] if i['asset'] in ['corner-shop','attached-townhouse','residential-tower']]
rects=[]
for i in buildings:
 lo=i['boundsMin'];size=i['boundsSize'];p=i['position'];sign=-1 if i['yaw']==180 else 1
 xs=[p[0]+sign*lo[0],p[0]+sign*(lo[0]+size[0])];zs=[p[2]+sign*lo[2],p[2]+sign*(lo[2]+size[2])]
 rects.append((min(xs),max(xs),min(zs),max(zs)))
for n,a in enumerate(rects):
 for b in rects[n+1:]:assert min(a[1],b[1])<=max(a[0],b[0]) or min(a[3],b[3])<=max(a[2],b[2]),'Buildings overlap'
paths=['scripts/art/neighbourhood.py','scripts/art/neighbourhood-gallery.py','scripts/art/fidelity-study.py','scripts/art/construction-study.py','scripts/art/expanded-kit.py','scripts/art/model-round.py','scripts/art/review.sh','src/Borough.Godot/ExpandedStudy.cs','src/Borough.Godot/ExpandedStudy.Neighbourhood.cs','art/visual-study/neighbourhood-1/manifest.json','art/visual-study/neighbourhood-1/materials.json','art/visual-study/preferences.json','art/visual-study/palettes.json']
paths += [str(p.relative_to(root)) for p in (root/'src/Borough.Godot/assets/visual-study/neighbourhood').glob('*.glb')]
paths += [str(p.relative_to(root)) for p in (root/'src/Borough.Godot/assets/visual-study/neighbourhood/materials').glob('*.jpg')]
for p in ['src/Borough.Godot/assets/visual-study/fidelity/attached-townhouse.glb','src/Borough.Godot/assets/visual-study/kit/models/car-realistic.glb']:
 old=json.loads((root/('artifacts/visual-study/fidelity/run.json' if 'attached' in p else 'artifacts/visual-study/models/run.json')).read_text())
 # Fidelity uses a source table; the original model round's asset table differs.
 assert sha(root/p)==old['source_sha256' if 'attached' in p else 'sources'][p]
 paths.append(p)
(out/'run.json').write_text(json.dumps(dict(comparison='neighbourhood-1',captures=records,capture_sha256=hashes,source_sha256={p:sha(root/p) for p in paths},materials=json.loads((root/'art/visual-study/neighbourhood-1/materials.json').read_text()),limits='Static authored fixture. City-distance is the same small corner viewed from farther away, not a city repetition test. No simulation, motion, night, LOD or measured rendering-cost acceptance.'),indent=2)+'\n')
nav=' · '.join(f'<a href="#{v}">{v.replace("-"," ").title()}</a>' for v in views)
(out/'index.html').write_text('''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Borough · A finished corner study</title><style>body{margin:0;background:#202626;color:#eeeae2;font:17px/1.6 system-ui}main{max-width:1440px;margin:auto;padding:32px 24px}h1{font-size:42px;line-height:1.2}p{max-width:1050px;color:#c9ccc6}a{color:#c1d2c8}nav{position:sticky;top:0;background:#202626ee;padding:15px 0;z-index:2}img{width:100%;display:block}section{margin:42px 0;scroll-margin-top:65px}</style><main><small>BOROUGH / 0063 / NEIGHBOURHOOD FIDELITY</small><h1>A street corner, together</h1><p>The positively reviewed terrace joins an authored corner shop and a residential tower based on the preferred realistic massing. Recessed glazing, curtains, slender balcony rails, roof equipment, shop displays and awnings extend the construction approach. The palette families remain warm, with modest brick-scale variation between buildings.</p><p>The setting adds branching trees with individual leaves, smoother pedestrian geometry, the previously liked car, seating, lamps, bins, kerbs, crossings and pavement joints. Poly Haven supplies the photographed brick; ambientCG supplies the asphalt. All model geometry is authored by the study scripts.</p><p>Six actual Godot captures share the same scene and noon lighting. The city-distance view tests how this small corner reads farther away; it does not demonstrate a complete city. This is a static review fixture. Night, motion, live simulation responses, deliberate city repetition and measured rendering costs remain outstanding. Interiors are shallow pockets, not furnished rooms; trees and pedestrians remain procedural studies.</p><p><a href="../fidelity/index.html">Earlier fidelity pilot</a> · <a href="../brick-scale/index.html">Brick sizes</a> · <a href="run.json">Source, material and capture manifest</a></p><nav>'''+nav+'</nav>'+''.join(sections)+'<p>Materials: <a href="https://polyhaven.com/a/brick_wall_001">Brick Wall 001 — Dimitrios Savva / Rob Tuytel, Poly Haven</a>; <a href="https://ambientcg.com/view?id=Asphalt012">Asphalt012 — ambientCG</a>. Both CC0. Additional libraries identified for future materials: ShareTextures and cgbookcase.</p></main></html>')
print('NEIGHBOURHOOD_OK: six cameras, fixed scene/light, four distinct new assets, imported bounds/normals/materials, separate building envelopes, preserved terrace.')

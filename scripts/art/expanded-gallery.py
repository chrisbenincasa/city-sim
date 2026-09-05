"""Check the roster, unchanged anchors, matched specimen views and staged layout."""
import hashlib,json,struct
from pathlib import Path
root=Path(__file__).resolve().parents[2]
out=root/'artifacts/visual-study/expanded'
source=json.loads((root/'art/visual-study/expanded-kit-1/manifest.json').read_text())
roster=json.loads((root/'art/visual-study/comparison-kit.json').read_text())
expected=roster['remaining']+['round-tree','conical-tree']
assert [a['specimen'] for a in source['assets']]==expected
assert len({a['geometry_sha256'] for a in source['assets']})==13
assert source['source_sha256']==hashlib.sha256((root/source['authority']).read_bytes()).hexdigest()
assert source['helper_sha256']==hashlib.sha256((root/'scripts/art/model-round.py').read_bytes()).hexdigest()
old=json.loads((root/'artifacts/visual-study/models/run.json').read_text())
controls=['camera','target','fov','viewport','sunRotation','sunEnergy','sunAngularDistance','ambientEnergy','ssao','taa','exposure','light','triangles','surfaces','godot','renderer','tick','population']
records=[];sections=[];capture_hashes={}
def read(stem):
 c=json.loads((out/(stem+'.json')).read_text());b=(out/(stem+'.png')).read_bytes()
 assert b[:8]==b'\x89PNG\r\n\x1a\n' and struct.unpack('>II',b[16:24])==(1440,960),stem
 assert c['comparison']=='expanded-kit-1' and c['tick'] is None and c['population'] is None
 records.append(c);capture_hashes[stem+'.png']=hashlib.sha256(b).hexdigest();return c
# Context first: do the parts work together?
sections.append('<section id="street"><h2>A street, then repetition</h2><p>Hand-arranged art fixtures at model scale, with both earlier residential towers and varied car colours. Street and courtyard paving are decorative context. No simulated city state is represented.</p>')
for subject,view in [('street','overview'),('street','street'),('street','courtyard'),('repetition','overview'),('repetition','street')]:
 stem=f'{subject}-mixed-approved-{view}';c=read(stem)
 sections.append(f'<figure class="context"><figcaption>{subject.title()} · {view}</figcaption><a href="{stem}.png"><img src="{stem}.png" alt="{subject} {view}"></a></figure>')
 # Conservative axis-aligned check: buildings must not intersect one another.
 buildings=[]
 for item in c['instances']:
  if item['asset'] in ['car-realistic','bus','walker','round-tree','conical-tree']:continue
  x,y,z=item['position'];lo=item['boundsMin'];size=item['boundsSize']
  assert item['yaw'] in [0,180]
  if item['yaw']==0:rect=(x+lo[0],z+lo[2],x+lo[0]+size[0],z+lo[2]+size[2])
  else:rect=(x-lo[0]-size[0],z-lo[2]-size[2],x-lo[0],z-lo[2])
  for name,other in buildings:
   overlap=min(rect[2],other[2])-max(rect[0],other[0]),min(rect[3],other[3])-max(rect[1],other[1])
   assert not (overlap[0]>.03 and overlap[1]>.03),(subject,item['asset'],name,overlap)
  buildings.append((item['asset'],rect))
sections.append('</section>')
for a in source['assets']:
 s=a['specimen'];lo,hi=a['bounds_blender'];assert abs(lo[2])<.025 and a['polygons']>0
 sizes=[hi[i]-lo[i] for i in range(3)]
 assert all(0<v<60 for v in sizes),(s,sizes)
 if s=='walker':assert 1.5<sizes[2]<2
 if s=='bus':assert 10<sizes[1]<14 and 2.3<sizes[0]<3 and 2.8<sizes[2]<3.8
 sections.append(f'<section id="{s}"><h2>{s.replace("-"," ").title()}</h2>')
 for view in ['whole','close']:
  if view=='close':sections.append('<details><summary>Close construction views</summary>')
  sections.append('<div class="pair">');pair=[]
  for palette in ['warm-slate','earth-terracotta']:
   stem=f'{s}-{palette}-{view}';c=read(stem);pair.append(c)
   assert c['subject']==s and c['palette']==palette and c['view']==view and len(c['instances'])==1
   item=c['instances'][0];assert item['asset']==s
   assert all(abs(item['boundsSize'][i]-sizes[j])<.01 for i,j in [(0,0),(1,2),(2,1)]),(s,item['boundsSize'],sizes)
   sections.append(f'<figure><figcaption>{palette.replace("-"," ").title()}</figcaption><a href="{stem}.png"><img loading="lazy" src="{stem}.png" alt="{s} {palette} {view}"></a></figure>')
  assert all(pair[0][k]==pair[1][k] for k in controls),s
  assert pair[0]['instances'][0]['boundsMin']==pair[1]['instances'][0]['boundsMin']
  sections.append('</div>')
  if view=='close':sections.append('</details>')
 sections.append('</section>')
paths=['scripts/art/expanded-kit.py','scripts/art/model-round.py','scripts/art/expanded-gallery.py','scripts/art/review.sh','src/Borough.Godot/ExpandedStudy.cs','src/Borough.Godot/ExpandedStudy.tscn','art/visual-study/expanded-kit-1/manifest.json','art/visual-study/palettes.json','art/visual-study/preferences.json','art/visual-study/comparison-kit.json','art/visual-study/expanded-layout.json']
paths += ['src/Borough.Godot/assets/visual-study/expanded/'+a['asset'] for a in source['assets']]
for name in ['detached-house-realistic','residential-tower-realistic','residential-tower-city-builder','car-realistic']:
 p=f'src/Borough.Godot/assets/visual-study/kit/models/{name}.glb'
 assert hashlib.sha256((root/p).read_bytes()).hexdigest()==old['sources'][p],p
 paths.append(p)
(out/'run.json').write_text(json.dumps(dict(comparison='expanded-kit-1',captures=records,controls_checked=controls,sources={p:hashlib.sha256((root/p).read_bytes()).hexdigest() for p in paths},capture_sha256=capture_hashes),indent=2)+'\n')
nav=' · '.join(f'<a href="#{s}">{s.replace("-"," ")}</a>' for s in expected)
(out/'index.html').write_text('''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Borough · Expanded kit and street study</title><style>body{margin:0;background:#202626;color:#eeeae2;font:17px/1.6 system-ui}main{max-width:1500px;margin:auto;padding:36px 24px}h1{font-size:42px;line-height:1.2}p{max-width:1100px;color:#c9ccc6}a{color:#c1d2c8}nav{line-height:2}.pair{display:grid;grid-template-columns:1fr 1fr;gap:20px}figure{margin:0 0 22px}img{width:100%;display:block}figcaption{padding:10px 0;font-size:20px}section{margin:48px 0}summary{padding:16px 0;cursor:pointer}.context{max-width:1440px}@media(max-width:800px){.pair{grid-template-columns:1fr}}</style><main><small>BOROUGH / 0063 / EXPANDED KIT 1</small><h1>From specimens to a street</h1><p>Eleven new roster specimens and two tree forms, built around the construction and palettes you liked. Narrow terraces, walk-ups, an open courtyard, office and mixed-use towers, two shops, a factory, a school, a bus and a walker. Each new specimen has matched slate/earth whole and close views. Tree foliage, bark, skin and trousers retain their own colours.</p><p>The street keeps the approved realistic house and car, and both residential tower models. The repetition view deliberately reuses housing models: this is a check on how obvious those repeats remain, not a claim of finished neighbourhood variety.</p><p>Initial procedural constructions. Glass remains opaque; fine textures, interiors, motion, night, live state responses and measured rendering costs remain outstanding. Mesh counts are descriptive only. The new models have not yet received player feedback.</p><nav><a href="#street">Street and repetition</a> · <a href="../palettes/index.html">Approved palette samples</a> · <a href="../models/index.html">First model round</a> · <a href="run.json">Run manifest</a></nav><nav>''' + nav + '</nav>' + '\n'.join(sections) + '</main></html>')
print('EXPANDED_COMPARISON_OK: 13 distinct new assets; 4 unchanged anchors; 57 captures; matched specimen controls; no building overlap in layouts.')

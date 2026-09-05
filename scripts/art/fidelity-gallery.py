"""Validate and publish the reference-led fidelity pilot; retain earlier captures."""
import json,hashlib,struct,html
from pathlib import Path
root=Path(__file__).resolve().parents[2];out=root/'artifacts/visual-study/fidelity';baseline=out.parent/'construction'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
source=json.loads((root/'art/visual-study/fidelity-1/manifest.json').read_text())
assert source['source_sha256']==sha(root/source['authority'])
asset=source['assets'][0]
assert len(source['assets'])==1 and asset['specimen']=='attached-townhouse'
old_asset=json.loads((root/'art/visual-study/construction-study-1/manifest.json').read_text())['assets'][0]
assert asset['geometry_sha256']!=old_asset['geometry_sha256']
# A texture can import successfully while sampling one UV point everywhere.
glb=(root/'src/Borough.Godot/assets/visual-study/fidelity/attached-townhouse.glb').read_bytes()
json_size=struct.unpack_from('<I',glb,12)[0];gltf=json.loads(glb[20:20+json_size]);binary=glb[28+json_size:]
brick=next(i for i,mat in enumerate(gltf['materials']) if mat['name']=='brick')
texture=gltf['materials'][brick]['pbrMetallicRoughness']['baseColorTexture']
for primitive in [p for mesh in gltf['meshes'] for p in mesh['primitives'] if p['material']==brick]:
 accessor=gltf['accessors'][primitive['attributes']['TEXCOORD_'+str(texture.get('texCoord',0))]]
 assert accessor['componentType']==5126 and accessor['type']=='VEC2'
 buffer=gltf['bufferViews'][accessor['bufferView']];offset=buffer.get('byteOffset',0)+accessor.get('byteOffset',0);stride=buffer.get('byteStride',8)
 uv=[struct.unpack_from('<ff',binary,offset+i*stride) for i in range(accessor['count'])]
 assert all(max(v[axis] for v in uv)-min(v[axis] for v in uv)>1 for axis in [0,1]),'Brick UVs collapsed'
controls=['camera','target','fov','viewport','sunRotation','sunEnergy','sunAngularDistance','ambientEnergy','ssao','taa','exposure','light','godot','renderer','tick','population']
records=[];captures={};sections=[]
for palette in ['warm-slate','earth-terracotta']:
 sections.append(f'<section><h2>{palette.replace("-"," ").title()}</h2>')
 for view in ['whole','close']:
  stem=f'attached-townhouse-{palette}-{view}'
  old=json.loads((baseline/(stem+'.json')).read_text());new=json.loads((out/(stem+'.json')).read_text())
  assert all(old[k]==new[k] for k in controls),stem
  assert new['comparison']=='fidelity-1' and new['palette']==palette and new['view']==view
  size=[asset['bounds_blender'][1][i]-asset['bounds_blender'][0][i] for i in [0,2,1]]
  assert all(abs(new['instances'][0]['boundsSize'][i]-size[i])<.01 for i in range(3))
  records.append(new)
  for p in [baseline/(stem+'.png'),out/(stem+'.png')]:
   b=p.read_bytes();assert b[:8]==b'\x89PNG\r\n\x1a\n' and struct.unpack('>II',b[16:24])==(1440,960)
   captures[str(p.relative_to(root))]=sha(p)
  sections.append(f'<h3>{view.title()} view</h3><div class="pair"><figure><figcaption>Construction pilot</figcaption><a href="../construction/{stem}.png"><img src="../construction/{stem}.png" loading="lazy" alt="Previous terrace {palette} {view}"></a></figure><figure><figcaption>Fidelity pilot</figcaption><a href="{stem}.png"><img src="{stem}.png" loading="lazy" alt="New terrace {palette} {view}"></a></figure></div>')
 sections.append('</section>')
research=json.loads((root/'art/visual-study/fidelity-1/research.json').read_text())
paths=[root/p for p in ['scripts/art/fidelity-study.py','scripts/art/fidelity-gallery.py','scripts/art/construction-study.py','scripts/art/expanded-kit.py','scripts/art/model-round.py','scripts/art/review.sh','src/Borough.Godot/ExpandedStudy.cs','art/visual-study/fidelity-1/manifest.json','art/visual-study/fidelity-1/research.json','art/visual-study/preferences.json','art/visual-study/palettes.json','src/Borough.Godot/assets/visual-study/fidelity/attached-townhouse.glb']]
paths+=list((root/'art/visual-study/fidelity-1/textures').glob('*.jpg'))
(out/'run.json').write_text(json.dumps(dict(comparison='fidelity-1',controls_checked=controls,captures=records,source_sha256={str(p.relative_to(root)):sha(p) for p in paths},capture_sha256=captures,research=research),indent=2)+'\n')
refs=''.join(f'<li><a href="{html.escape(s["url"])}">{html.escape(s["title"])}</a> — {html.escape(s["finding"])}</li>' for s in research['sources'])
(out/'index.html').write_text('''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Borough · Fidelity pilot</title><style>body{margin:0;background:#202626;color:#eeeae2;font:17px/1.6 system-ui}main{max-width:1700px;margin:auto;padding:36px 24px}h1{font-size:42px;line-height:1.2}p,li{max-width:1100px;color:#c9ccc6}a{color:#c1d2c8}.pair{display:grid;grid-template-columns:1fr 1fr;gap:18px}figure{margin:0}img{width:100%;display:block}figcaption{padding:10px 0;font-size:20px}section{margin:48px 0}@media(max-width:850px){.pair{grid-template-columns:1fr}}</style><main><small>BOROUGH / 0063 / REFERENCE-LED FIDELITY</small><h1>From building form to a finished surface</h1><p>The current kit is a first cut, not the intended end state. This terrace tests a more complete workflow: photographed brick, fine sash joinery, folded curtains and room pockets, cornice mouldings, door hardware, individual shrub leaves, entrance paving and railings. All geometry is authored by the study scripts; the brick maps are third-party CC0 material.</p><p>The two approved palette families remain. Brick has its own photographed colour variation with a restrained palette tint; glazing is lighter and more transparent. Cameras, exposure and noon lighting match the preserved construction pilot. Model and material changes are combined here, so this does not measure their separate contributions.</p><p>These are actual Godot captures. This is still a procedural pilot: rendered walls and end elevations need more surface work; the rooms are shallow static pockets; planting, distant appearance, night and city repetition remain to be judged. Geometry counts are descriptive, not measured rendering costs. No final direction is selected.</p><nav><a href="../expanded/index.html">Whole kit and street</a> · <a href="../construction/index.html">Construction pilot</a> · <a href="run.json">Manifest and attribution</a></nav>'''+''.join(sections)+'<section><h2>What the references changed</h2><div class="pair"><figure><figcaption>SimCity 2013 — supplied player screenshot</figcaption><a href="https://www.reddit.com/r/SimCity/comments/wf98pe/some_random_simcity_2013_screenshots/">Open the source screenshot</a></figure><figure><figcaption>Cities: Skylines II — supplied Steam screenshot post</figcaption><a href="https://www.reddit.com/r/CitiesSkylines2/comments/1470y1t/new_screenshots_on_cities_skylines_2_steam_page/">Open the source screenshot</a></figure></div><p>Reference imagery belongs to its respective creators and publishers; shown here for private comparison, separate from Borough assets.</p><p>My reading of the screenshots: believable proportions need layers of material, construction and surrounding detail. SimCity retains strong colour and readable silhouettes; Cities: Skylines II provides a useful closer-view reference for glazing, facade joints and surface variation. Neither is evidence that our current assets have reached that standard.</p><ul>'+refs+'</ul><p>Brick material: Dimitrios Savva (photography), Rob Tuytel (processing), Poly Haven, CC0. Original texture files are preserved with download URLs and hashes in the manifest. Reference screenshots are research only and are not runtime assets.</p></section></main></html>')
print('FIDELITY_COMPARISON_OK: changed geometry; 4 matched new captures; imported texture maps; 4 preserved comparison captures.')

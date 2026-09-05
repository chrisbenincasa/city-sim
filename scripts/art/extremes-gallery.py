"""Build the preference-anchored extremes comparison, checking all shared controls."""
import hashlib
import json
from pathlib import Path

root=Path(__file__).resolve().parents[2]
out=root/'artifacts/visual-study/extremes'
base=json.loads((root/'art/visual-study/kit/manifest.json').read_text())
source=json.loads((root/'art/visual-study/kit/extremes/manifest.json').read_text())
assert source['source_sha256']==hashlib.sha256((root/source['authority']).read_bytes()).hexdigest()
controls=['specimen','camera','target','fov','viewport','light','exposure','view','tick','population','godot','renderer']
records=[]
sections=''
paths=['scripts/art/kit.py','src/Borough.Godot/KitStudy.cs','src/Borough.Godot/kit-sculpted.gdshader',
       'art/visual-study/preferences.json','art/visual-study/kit/extremes/manifest.json']
for specimen in ['detached-house','residential-tower','car']:
    preferred='middle' if specimen=='car' else 'material-rich'
    rows=[a for a in source['assets'] if a['specimen']==specimen]
    original=next(a for a in base['assets'] if a['specimen']==specimen and a['treatment']==preferred)
    assert all(a['core_sha256']==original['core_sha256'] for a in rows),specimen
    paths.append('src/Borough.Godot/assets/visual-study/kit/'+original['asset'])
    paths.extend('src/Borough.Godot/assets/visual-study/kit/extremes/'+a['asset'] for a in rows)
    sections+=f'<section><h2>{specimen.replace("-"," ").title()}</h2>'
    for view in ['whole','detail']:
        sections+=f'<h3>{view.title()}</h3><div class="row">'
        captures=[]
        for treatment,label in [(preferred,'Your current favourite'),('detailed-extreme','Detail pushed further'),('sculpted-extreme','Bold sculpted / stepped shading')]:
            stem=f'{specimen}-{treatment}-{view}'
            capture=json.loads((out/(stem+'.json')).read_text())
            captures.append(capture)
            records.append(capture)
            assert (out/(stem+'.png')).read_bytes()[:8]==b'\x89PNG\r\n\x1a\n'
            sections+=f'<figure><figcaption>{label}</figcaption><a href="{stem}.png"><img src="{stem}.png" alt="{specimen} {treatment} {view}"></a><small>{capture["triangles"]:,} triangles · {capture["surfaces"]} surfaces</small></figure>'
        for c in captures:
            assert all(c[k]==captures[0][k] for k in controls),(specimen,view)
        sections+='</div>'
    sections+='</section>'
manifest={'captures':records,'controls_checked':controls,
'sources':{p:hashlib.sha256((root/p).read_bytes()).hexdigest() for p in paths},
'varied':'detail geometry, masonry colour variation, surface response; bold sculpted also changes diffuse shading model'}
(out/'run.json').write_text(json.dumps(manifest,indent=2)+'\n')
(out/'index.html').write_text('''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Borough · Exploring the extremes</title><style>
body{margin:0;background:#182025;color:#e9ecea;font:17px/1.6 system-ui}main{max-width:1900px;margin:auto;padding:36px 24px}h1{font-size:40px;line-height:1.2}p{max-width:950px;color:#c2cdcf}a{color:#b5d6d0}.row{display:grid;grid-template-columns:repeat(3,1fr);gap:14px}figure{margin:0}img{width:100%;display:block}figcaption{font-size:19px;padding:8px 0}section{margin:48px 0}small{color:#a4b7b7}@media(max-width:1000px){.row{grid-template-columns:1fr}}</style>
<main><small>BOROUGH / 0063 / EXPLORATION</small><h1>How far should the treatments go?</h1>
<p>Working preferences: material-rich house and tower; middle car. Each appears at left, unchanged, beside two stronger alternatives.</p>
<p>The detailed extreme adds pronounced masonry variation and smaller construction details. The bold sculpted extreme enlarges joinery and uses three-band diffuse shading with no specular highlights. These are separately labelled style experiments: the shading model and local material variation change; core composition, source palette, camera, light placement and exposure remain controlled.</p>
<p>Both whole-object and close views are provided. Click any image for full size. <a href="../kit/index.html">Original trio</a> · <a href="run.json">Comparison manifest</a></p>
''' + sections + '<p>No style is locked in. Geometry counts are not performance measurements. These are isolated art specimens with no simulated population or state response.</p></main></html>')
print('EXTREMES_COMPARISON_OK: 18 captures; core composition agrees with original kit; camera and lighting controls match.')

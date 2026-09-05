"""Verify distinct model interpretations with shared role, approximate scale and cameras."""
import hashlib
import json
from pathlib import Path

root=Path(__file__).resolve().parents[2]
out=root/'artifacts/visual-study/models'
source=json.loads((root/'art/visual-study/model-round-1/manifest.json').read_text())

assert source['source_sha256']==hashlib.sha256((root/source['authority']).read_bytes()).hexdigest()
styles=['realistic','city-builder','voxel','low-poly']
specimens=['detached-house','residential-tower','car']
controls=['specimen','camera','target','fov','viewport','taa','light','exposure','sunRotation','sunEnergy','ambientEnergy','sunAngularDistance','ssao','view','tick','population','godot','renderer']
paths=['scripts/art/model-round.py','src/Borough.Godot/KitStudy.cs','src/Borough.Godot/KitStudy.Styles.cs',
       'art/visual-study/preferences.json','art/visual-study/model-round-1/manifest.json']

paths.extend('src/Borough.Godot/assets/visual-study/kit/models/'+a['asset'] for a in source['assets'])
records=[]
sections=''
for specimen in specimens:
    rows=[a for a in source['assets'] if a['specimen']==specimen]
    assert {a['treatment'] for a in rows}==set(styles)
    assert len({a['geometry_sha256'] for a in rows})==4,specimen
    for a in rows:
        lo,hi=a['bounds_blender'];size=[hi[i]-lo[i] for i in range(3)]
        limits={'detached-house':(12,15,10),'residential-tower':(26,24,47),'car':(2.5,5,2)}[specimen]
        assert all(0<size[i]<=limits[i] for i in range(3)),(specimen,a['treatment'],size)
        assert abs(lo[2])<.025
    sections+=f'<section id="{specimen}"><h2>{specimen.replace("-"," ").title()}</h2>'
    for view in ['whole','detail']:
        if view=='detail':sections+='<details><summary>Close views</summary>'
        sections+='<div class="row">'
        captures=[]
        for style in styles:
            stem=f'{specimen}-{style}-{view}'
            capture=json.loads((out/(stem+'.json')).read_text())
            assert capture['treatment']==style and capture['view']==view
            captures.append(capture)
            records.append(capture)
            assert (out/(stem+'.png')).read_bytes()[:8]==b'\x89PNG\r\n\x1a\n'
            sections+=f'<figure data-style="{style}"><figcaption>{style.title()}</figcaption><a href="{stem}.png"><img src="{stem}.png" alt="{specimen}, {style}, {view}" loading="lazy"></a><small>{capture["triangles"]:,} imported triangles · {capture["surfaces"]} mesh surfaces</small></figure>'
        assert all(all(c[k]==captures[0][k] for k in controls) for c in captures),(specimen,view)
        sections+='</div>'
        if view=='detail':sections+='</details>'
    sections+='</section>'
(out/'run.json').write_text(json.dumps({'captures':records,'controls_checked':controls,
'varied':['silhouette','proportions','massing','construction details','vehicle profile','edge treatment','paint roughness'],
'sources':{p:hashlib.sha256((root/p).read_bytes()).hexdigest() for p in paths}},indent=2)+'\n')
(out/'index.html').write_text('''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Borough · First model round</title><style>
body{margin:0;background:#182025;color:#e9ecea;font:17px/1.6 system-ui}main{max-width:1560px;margin:auto;padding:36px 24px}h1{font-size:42px;line-height:1.2}p{max-width:1000px;color:#c2cdcf}a{color:#b5d6d0}.row{display:grid;grid-template-columns:repeat(2,1fr);gap:18px}figure{margin:0}figure[hidden]{display:none}img{width:100%;display:block}figcaption{font-size:21px;padding:8px 0}section{margin:48px 0}small{color:#a4b7b7}nav,.switch{display:flex;gap:14px;flex-wrap:wrap}button{font:inherit;border:1px solid #677b81;background:#25343a;color:#e9ecea;border-radius:5px;padding:7px 16px;cursor:pointer}button[aria-pressed=true]{background:#b5d6d0;color:#152326}.switch{position:sticky;top:0;background:#182025f5;padding:12px 0;z-index:2}body.single .row{grid-template-columns:1fr;max-width:1100px}summary{cursor:pointer;padding:16px 0;font-size:20px}td,th{padding:10px;text-align:left;border-bottom:1px solid #39454b}@media(max-width:800px){.row{grid-template-columns:1fr}main{padding:24px 12px}}</style>
<main><small>BOROUGH / 0063 / MODEL ROUND 1</small><h1>Four model languages</h1>
<p>Twelve purpose-built models: a detached house, residential tower and passenger car in four directions. Geometry varies deliberately; warm material colours, cameras, light and scale category are shared. Use the buttons to compare a direction, or show all four. Open close views beneath each group.</p>
<table><tr><th>Direction</th><th>Model language</th></tr>
<tr><td>Realistic</td><td>Articulated house wings, finer joinery, individual tower balconies and setbacks, shaped sedan with wheel arches. A first procedural approximation of realistic construction.</td></tr>
<tr><td>City-builder</td><td>SC2013-inspired proportions: pronounced gables, larger windows, expressive tower crown and a taller, chunkier car cabin.</td></tr>
<tr><td>Voxel</td><td>Crisp cuboid construction, stepped roofs and tower setbacks, grid glazing, block-built car and square wheels.</td></tr>
<tr><td>Low-poly</td><td>Asymmetric folded house roof, octagonal tower, angular car profile and polygonal wheels.</td></tr></table>
<p>These are model interpretations, not shader swaps. Exact architectural composition is allowed to change in this round. No interiors or live simulation; this is a direction study, not finished production art.</p>
<nav><a href="#detached-house">House</a><a href="#residential-tower">Tower</a><a href="#car">Car</a><a href="../extremes/index.html">Earlier favourites and extremes</a><a href="run.json">Run manifest</a></nav>
<div class="switch" aria-label="Display style"><button data-style="all" aria-pressed="true">All four</button><button data-style="realistic" aria-pressed="false">Realistic</button><button data-style="city-builder" aria-pressed="false">City-builder</button><button data-style="voxel" aria-pressed="false">Voxel</button><button data-style="low-poly" aria-pressed="false">Low-poly</button></div>
''' + sections + '''<p>Initial style executions, not a final selection. These are isolated art specimens with no simulation state. Counts describe imported meshes, not measured rendering costs. Motion, city-scale repetition and rendering costs remain to be tested.</p></main>
<script>document.querySelectorAll('button[data-style]').forEach(button=>button.addEventListener('click',()=>{const style=button.dataset.style;document.body.classList.toggle('single',style!=='all');document.querySelectorAll('figure[data-style]').forEach(figure=>figure.hidden=style!=='all'&&figure.dataset.style!==style);document.querySelectorAll('button[data-style]').forEach(b=>b.setAttribute('aria-pressed',String(b===button)));}));</script></html>''')
print('MODEL_ROUND_COMPARISON_OK: 12 distinct meshes; 24 captures; scale envelopes and shared camera/light controls agree.')

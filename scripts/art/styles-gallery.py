"""Verify and present the four art languages on identical specimens and cameras."""
import hashlib
import json
from pathlib import Path

root=Path(__file__).resolve().parents[2]
out=root/'artifacts/visual-study/styles'
source=json.loads((root/'art/visual-study/kit/styles/manifest.json').read_text())
original=json.loads((root/'art/visual-study/kit/manifest.json').read_text())
assert source['source_sha256']==hashlib.sha256((root/source['authority']).read_bytes()).hexdigest()
styles=['painterly','graphic','miniature','naturalistic']
specimens=['detached-house','residential-tower','car']
controls=['specimen','camera','target','fov','viewport','taa','light','exposure','sunRotation','sunEnergy','ambientEnergy','view','tick','population','godot','renderer']
paths=['scripts/art/kit.py','src/Borough.Godot/KitStudy.cs','src/Borough.Godot/KitStudy.Styles.cs',
       'art/visual-study/preferences.json','art/visual-study/kit/styles/manifest.json']
paths.extend('src/Borough.Godot/'+p for p in ['kit-painterly.gdshader','kit-graphic.gdshader','kit-outline.gdshader','kit-miniature.gdshader','kit-natural-wall.gdshader'])
paths.extend('src/Borough.Godot/assets/visual-study/kit/styles/'+a['asset'] for a in source['assets'])
records=[]
sections=''
for specimen in specimens:
    baseline=next(a for a in original['assets'] if a['specimen']==specimen)
    rows=[a for a in source['assets'] if a['specimen']==specimen]
    assert {a['treatment'] for a in rows}==set(styles)
    assert all(a['core_sha256']==baseline['core_sha256'] for a in rows),specimen
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
'varied':['material palette','shading model','small geometry details','miniature bevels','sun angular size','ambient occlusion'],
'sources':{p:hashlib.sha256((root/p).read_bytes()).hexdigest() for p in paths}},indent=2)+'\n')
(out/'index.html').write_text('''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Borough · Four art styles</title><style>
body{margin:0;background:#182025;color:#e9ecea;font:17px/1.6 system-ui}main{max-width:1560px;margin:auto;padding:36px 24px}h1{font-size:42px;line-height:1.2}p{max-width:1000px;color:#c2cdcf}a{color:#b5d6d0}.row{display:grid;grid-template-columns:repeat(2,1fr);gap:18px}figure{margin:0}figure[hidden]{display:none}img{width:100%;display:block}figcaption{font-size:21px;padding:8px 0}section{margin:48px 0}small{color:#a4b7b7}nav,.switch{display:flex;gap:14px;flex-wrap:wrap}button{font:inherit;border:1px solid #677b81;background:#25343a;color:#e9ecea;border-radius:5px;padding:7px 16px;cursor:pointer}button[aria-pressed=true]{background:#b5d6d0;color:#152326}.switch{position:sticky;top:0;background:#182025f5;padding:12px 0;z-index:2}body.single .row{grid-template-columns:1fr;max-width:1100px}summary{cursor:pointer;padding:16px 0;font-size:20px}td,th{padding:10px;text-align:left;border-bottom:1px solid #39454b}@media(max-width:800px){.row{grid-template-columns:1fr}main{padding:24px 12px}}</style>
<main><small>BOROUGH / 0063 / ART LANGUAGE STUDY</small><h1>Four distinct directions</h1>
<p>The same house, residential tower and car in four treatments. Whole-object cameras and close cameras are matched across styles. Use the buttons to compare one style at a time without changing the view, or show all four.</p>
<table><tr><th>Style</th><th>What changes</th></tr>
<tr><td>Painterly</td><td>Warm/cool colour washes, directional brush texture, matte shading, simplified small details. Procedural brush marks, not painted source textures.</td></tr>
<tr><td>Graphic</td><td>Ochre/coral/navy palette, flat colour regions, hard shadow bands, selective ink outlines.</td></tr>
<tr><td>Miniature</td><td>Rounded edges, pastel materials, matte wrapped shading, softer sun shadows and ambient occlusion. Temporal antialiasing is shared by all four styles; no photographic blur.</td></tr>
<tr><td>Naturalistic</td><td>Neutral masonry variation, normal/roughness maps, smoother metal and paint, sky reflections and ambient occlusion. Glazing remains opaque; no interiors.</td></tr></table>
<p>This comparison deliberately varies palette and material response. Light direction, energy, exposure and core composition stay fixed. Sun angular size is 4° for miniature and 0.5° elsewhere. It tests art direction, not just the amount of detail.</p>
<nav><a href="#detached-house">House</a><a href="#residential-tower">Tower</a><a href="#car">Car</a><a href="../extremes/index.html">Earlier favourites and extremes</a><a href="run.json">Run manifest</a></nav>
<div class="switch" aria-label="Display style"><button data-style="all" aria-pressed="true">All four</button><button data-style="painterly" aria-pressed="false">Painterly</button><button data-style="graphic" aria-pressed="false">Graphic</button><button data-style="miniature" aria-pressed="false">Miniature</button><button data-style="naturalistic" aria-pressed="false">Naturalistic</button></div>
''' + sections + '''<p>Initial style executions, not a final selection. These are isolated art specimens with no simulation state. Counts describe imported meshes; graphic outlines add a rendering pass. Motion, city-scale repetition and rendering costs remain to be tested.</p></main>
<script>document.querySelectorAll('button[data-style]').forEach(button=>button.addEventListener('click',()=>{const style=button.dataset.style;document.body.classList.toggle('single',style!=='all');document.querySelectorAll('figure[data-style]').forEach(figure=>figure.hidden=style!=='all'&&figure.dataset.style!==style);document.querySelectorAll('button[data-style]').forEach(b=>b.setAttribute('aria-pressed',String(b===button)));}));</script></html>''')
print('ART_STYLES_COMPARISON_OK: 24 captures; original composition and shared camera/light controls agree.')

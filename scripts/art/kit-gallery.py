"""Verify comparison controls, then assemble the kit gallery."""
import hashlib
import json
from pathlib import Path

root=Path(__file__).resolve().parents[2]
out=root/'artifacts/visual-study/kit'
source=json.loads((root/'art/visual-study/kit/manifest.json').read_text())
assert source['source_sha256']==hashlib.sha256((root/source['authority']).read_bytes()).hexdigest()
specimens=['detached-house','residential-tower','car']
treatments=['material-rich','sculpted','middle']
controls=['specimen','camera','target','fov','viewport','light','exposure','tick','population','godot','renderer']
records=[]
for specimen in specimens:
    rows=[a for a in source['assets'] if a['specimen']==specimen]
    assert {a['treatment'] for a in rows}==set(treatments)
    assert len({a['core_sha256'] for a in rows})==1
    captures=[json.loads((out/f'{specimen}-{treatment}.json').read_text()) for treatment in treatments]
    for capture in captures:
        assert all(capture[k]==captures[0][k] for k in controls), specimen
        assert capture['triangles']>0 and capture['surfaces']>0
        stem=f"{specimen}-{capture['treatment']}"
        assert (out/(stem+'.png')).read_bytes()[:8]==b'\x89PNG\r\n\x1a\n'
        records.append(capture)
paths=['scripts/art/kit.py','src/Borough.Godot/KitStudy.cs','art/visual-study/comparison-kit.json','art/visual-study/kit/manifest.json']
paths += ['src/Borough.Godot/assets/visual-study/kit/'+a['asset'] for a in source['assets']]
manifest={'captures':records,'sources':{p:hashlib.sha256((root/p).read_bytes()).hexdigest() for p in paths},'controls_checked':controls}
(out/'run.json').write_text(json.dumps(manifest,indent=2)+'\n')
sections=''
for specimen in specimens:
    sections+=f'<section id="{specimen}"><h2>{specimen.replace("-"," ").title()}</h2><div class="row">'
    for treatment in treatments:
        stem=f'{specimen}-{treatment}'
        r=next(r for r in records if r['specimen']==specimen and r['treatment']==treatment)
        sections+=f'<figure><figcaption>{treatment.replace("-"," ").title()}</figcaption><a href="{stem}.png"><img src="{stem}.png" alt="{specimen}, {treatment}"></a><small>{r["triangles"]:,} triangles · {r["surfaces"]:,} mesh surfaces</small></figure>'
    sections+='</div></section>'
(out/'index.html').write_text('''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Borough · First comparison trio</title><style>
body{margin:0;background:#182025;color:#e9ecea;font:17px/1.6 system-ui}main{max-width:1900px;margin:auto;padding:36px 24px}h1{font-size:40px;line-height:1.2}p{max-width:950px;color:#c2cdcf}a{color:#b5d6d0}.row{display:grid;grid-template-columns:repeat(3,1fr);gap:14px}figure{margin:0}img{width:100%;display:block}figcaption{font-size:20px;padding:8px 0}section{margin:42px 0}small{color:#a4b7b7}@media(max-width:1000px){.row{grid-template-columns:1fr}}nav{display:flex;gap:24px;flex-wrap:wrap}</style>
<main><small>BOROUGH / 0063 / COMPARISON KIT v1</small><h1>One specimen, three treatments</h1>
<p>First trio: detached house, residential tower and car. Each keeps its composition, dimensions, openings, palette, camera and noon lighting across treatments. Click any image for full size.</p>
<p>Initial executions for review—not a chosen style or the completed kit. Material-rich adds small joinery and surface relief; sculpted concentrates on broad frames and masses; middle keeps selective depth and quieter surfaces.</p>
<nav><a href="#detached-house">House</a><a href="#residential-tower">Tower</a><a href="#car">Car</a><a href="run.json">Run manifest</a></nav>
''' + sections + '<p>These isolated art specimens have no simulated occupancy or population. Counts describe imported geometry, not rendering time. Street context, close views, motion, night lighting and live-state checks remain further work.</p></main></html>')
print('KIT_COMPARISON_OK: 9 captures; composition and capture controls match within each specimen')

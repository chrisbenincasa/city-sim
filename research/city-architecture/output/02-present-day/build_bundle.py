#!/usr/bin/env python3
from pathlib import Path
import csv, html, json, math, shutil, subprocess, textwrap
from research_data import SOURCES, EXAMPLES, DIMENSIONS
O=Path(__file__).resolve().parent
S={s['source_id']:s for s in SOURCES}
def writecsv(name,rows):
    with (O/name).open('w',newline='') as f:
        w=csv.DictWriter(f,list(rows[0]));w.writeheader();w.writerows(rows)
subprocess.run(['python3',str(O/'audit.py')],check=True)
writecsv('sources.csv',SOURCES);writecsv('dimensions.csv',DIMENSIONS)
(O/'evidence/catalogue.json').write_text(json.dumps(EXAMPLES,indent=2,ensure_ascii=False)+'\n')
shutil.copyfile(O/'report-source.md',O/'REPORT.md')
esc=html.escape

class SVG:
    def __init__(self,w,h,title):
        self.w=w;self.h=h;self.parts=[f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}" role="img" aria-label="{esc(title)}"><title>{esc(title)}</title><rect width="{w}" height="{h}" fill="#f6f2e9"/>', '<style>text{font-family:Arial,sans-serif;fill:#273e43;font-size:14px}.title{font-size:26px;font-weight:bold}.heading{font-size:19px;font-weight:bold}.small{font-size:12px}</style>']
    def rect(self,x,y,w,h,fill='#b88569',stroke='#364c52',dash=''):
        self.parts.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="{fill}" stroke="{stroke}" stroke-width="1.2"'+(f' stroke-dasharray="{dash}"' if dash else '')+'/>')
    def path(self,d,fill='none',stroke='#364c52',dash=''):
        self.parts.append(f'<path d="{d}" fill="{fill}" stroke="{stroke}" stroke-width="1.3"'+(f' stroke-dasharray="{dash}"' if dash else '')+'/>')
    def text(self,x,y,t,cl=''):
        self.parts.append(f'<text x="{x}" y="{y}" class="{cl}">{esc(t)}</text>')
    def para(self,x,y,t,chars=48,cl='small',leading=18):
        for i,line in enumerate(textwrap.wrap(t,chars)):self.text(x,y+i*leading,line,cl)
    def person(self,x,base,scale=8):
        self.parts.append(f'<circle cx="{x}" cy="{base-1.55*scale}" r="{.12*scale}" fill="#273e43"/>')
        self.path(f'M{x} {base-1.4*scale}v{.75*scale}m0 {-.65*scale}l{-.25*scale} {.45*scale}m{.25*scale} {-.45*scale}l{.25*scale} {.45*scale}m{-.25*scale} {.20*scale}l{-.22*scale} {.65*scale}m{.22*scale} {-.65*scale}l{.22*scale} {.65*scale}')
    def finish(self):return ''.join(self.parts)+'</svg>'

# Reference and game geometry: all building views use exactly 8 units per metre.
v=SVG(1440,1600,'Architecture at the same scale: evidence, game and proposed models')
v.text(40,44,'Same ground. Different building logic.','title')
v.text(40,72,'Blue = documented control · orange = game · green = proposed section. Model treatments below are PROPOSED; 8 units/metre.')
v.text(40,95,'Evidence envelopes are plans, not inferred floor areas. Unknown heights remain unknown. No image-derived measurements.','small')
cols=[40,395,750,1090]
for x,name,sub in zip(cols,['G003 game body','US03 sixplex plan','US01 narrow house','NL01 bay/depth control'],['d9addc3 / 3 storeys','South Bend proposal / 2 storeys','South Bend proposal / 2 storeys','Hogekwartier design / 4A and 4B']):
    v.text(x,136,name,'heading');v.text(x,158,sub,'small')
v.rect(40,180,24*8,16*8)
# Put the stepped rear toward the top; source lower front rectangle is 40×40 ft.
w=40*.3048*8;rear=34*.3048*8;rd=26*.3048*8;fd=40*.3048*8
v.path(f'M395 180h{rear}v{rd}h{w-rear}v{fd}H395Z','#b8cdd5')
v.rect(750,180,6.096*8,8.5344*8,'#b8cdd5')
v.rect(1090,180,5.1*8,9.4*8,'none','#537c8d','5 3')
notes=[('24×16 m exterior; 384 m² ground.','1,152 m² counted floor; two tenancies.'),('12.192 m maximum body width;','20.1168 m body depth. Stoop omitted.'),('6.096×8.5344 m main body.','Porch is outside this rectangle.'),('5.1 m beukmaat ×9.4 m depth.','Dashed: not surveyed exterior walls.')]
for x,n in zip(cols,notes):
    for i,t in enumerate(n):v.text(x,372+i*20,t,'small')
v.text(40,430,'The roof problem: cross-sections with unchanged wall bodies','heading')
for x,label in [(40,'G003: recorded gable'),(395,'N3: proposed apartment roof'),(750,'G001: recorded paired spans')]:v.text(x,458,label)
base=625
v.rect(40,base-10.5*8,16*8,10.5*8)
v.path(f'M{40-.35*8} {base-10.5*8}L{40+8*8} {base-(10.5+4.125)*8}L{40+16.35*8} {base-10.5*8}Z','#657785')
v.rect(395,base-10.5*8,16*8,10.5*8,'#adc4b5')
v.rect(395,base-10.95*8,16*8,.45*8,'#657785')
v.path(f'M395 {base-10.5*8}L459 {base-10.3*8}L523 {base-10.5*8}')
v.rect(750,base-7*8,20*8,7*8)
x=750-.35*8;y=base-7*8;span=10.35*8
v.path(f'M{x} {y}L{x+span/2} {y-2.676*8}L{x+span} {y}L{x+1.5*span} {y-2.676*8}L{x+2*span} {y}Z','#657785')
for x in [200,555,935]:v.person(x,base)
for x,t in [(40,'Wall 10.5 m + roof rise 4.125 m.'),(395,'Wall 10.5 m + parapet 0.45 m.'),(750,'Wall 7 m; two 10.35 m outer spans.')]:v.text(x,651,t,'small')
v.text(750,674,'Rise 2.676 m each; a valley needs drainage.','small')
v.para(1090,500,'No national roof rule: Dutch designs include flat and pitched caps. Seattle proposes occupied roof decks. Select assemblies by type and access.',43,'small',20)
v.text(395,674,'Midspan cut; headhouse is outside section.','small')
v.path('M40 710H1400')
v.text(40,744,'Three model briefs — original proposed elevations, selected openings','heading')
def model(svg,x,base,kind,palette='mix',scale=8):
    w,d,h={'N1':(24,12,6),'N2':(16,12,10.5),'N3':(24,16,10.5)}[kind]
    wall={'us':'#b87961','nl':'#cbb791','mix':'#bda58a'}[palette]
    svg.rect(x,base-h*scale,w*scale,h*scale,wall)
    if kind=='N1':
        rise=6*math.tan(math.radians(25));svg.rect(x,base-(h+rise)*scale,w*scale,rise*scale,'#617381')
        for i in range(4):
            xx=x+i*6*scale;svg.path(f'M{xx} {base}v{-6*scale}')
            svg.rect(xx+scale,base-2.2*scale,scale,2.2*scale,'#456b73')
            for b in [1.5,4.5]:svg.rect(xx+(b-.6)*scale,base-(3+.85+1.5)*scale,1.2*scale,1.5*scale,'#cfe0dc')
            svg.rect(xx+3.75*scale,base-2.35*scale,1.5*scale,1.5*scale,'#cfe0dc')
    else:
        svg.rect(x,base-(h+.45)*scale,w*scale,.45*scale,'#617381')
        floors=[4.2,7.35] if kind=='N2' else [0,3.5,7]
        n=4 if kind=='N2' else 8;bay=w/n
        for floor in floors:
            for i in range(n):
                if kind=='N3' and floor==0 and i in [3,4]:continue
                xx=x+((i+.5)*bay-.6)*scale
                svg.rect(xx,base-(floor+.85+1.5)*scale,1.2*scale,1.5*scale,'#cfe0dc')
                if palette=='us':svg.path(f'M{xx-.08*scale} {base-(floor+.85)*scale}h{1.36*scale}')
        if kind=='N2':
            for i in range(4):svg.rect(x+(i*4+.3)*scale,base-3.05*scale,3.4*scale,2.6*scale,'#cfe0dc')
            for i in [0,2]:svg.rect(x+(i*4+.5)*scale,base-2.2*scale,1.2*scale,2.2*scale,'#456b73')
        else:
            svg.rect(x+(w/2-.6)*scale,base-2.2*scale,1.2*scale,2.2*scale,'#456b73')
            svg.rect(x+(w/2-1.2)*scale,base-2.45*scale,2.4*scale,.15*scale,'#617381')
            svg.rect(x+20*scale,base-(h+2.4)*scale,2*scale,2.4*scale,'#617381')
    svg.person(x+w*scale+14,base,scale)
for x,k,t in [(40,'N1','24×12 m; four homes; wall 6 m'),(500,'N2','16×12 m; wall 10.5 m'),(940,'N3','G003 ground retained; wall 10.5 m')]:
    model(v,x,910,k);v.text(x,943,k+' · '+{'N1':'attached row','N2':'corner mixed use','N3':'apartment block'}[k],'heading');v.text(x,966,t,'small')
v.text(40,1000,'N1 ridge +2.798 m; contemporary metal roof. N2 and N3 use complete low-slope assemblies. No illustrated occupancy is simulated.','small')
v.path('M40 1025H1400')
v.text(40,1059,'A structural span is not a building style','heading')
# This is a span bar, not a fabricated building envelope.
for x,metres,title,subtitle in [(40,13,'Wohnregal: approx. 13 m clear span','Documented built structure; outer depth unknown.'),(500,5.25,'Kerto-Ripa first-row floor limit: 5.25 m','Product calculation with stated assumptions; not N3 approval.'),(1000,16,'G003 outer wall depth: 16 m','Game body; not a clear span or structural design.')]:
    v.path(f'M{x} 1100h{metres*8}m{-metres*8} -6v12m{metres*8} -12v12')
    v.text(x,1135,title,'small');v.para(x,1158,subtitle,49,'small')
v.path('M40 1205H1400')
v.text(40,1240,'Material controls — 30× larger than the building views (240 units/metre)','heading')
v.rect(40,1270,.215*240,.065*240)
v.text(40,1325,'Brick face 215×65 mm','small')
v.rect(380,1270,.165*240,.265*240,'#617381')
v.path('M380 1294h39.6');v.text(445,1290,'Tile 265×165 mm; 65 mm headlap','small');v.text(445,1312,'Calculated double-lap gauge: 100 mm','small')
for i in range(3):v.path(f'M{940+i*.6096*240} 1270v80')
v.text(940,1380,'Seattle schedule: 24 in =609.6 mm seam module','small')
v.text(40,1420,'Surface modules do not establish building proportions. Modern product dimensions are not measured historic courses.','small')
v.path('M40 1470h80M40 1464v12M80 1464v12M120 1464v12')
v.text(40,1500,'0 / 5 / 10 m building scale','small');v.person(315,1490);v.text(340,1490,'1.75 m person at building scale','small')
v.para(40,1534,'Sources: US1 plans; NL1 schedule; DE3 span; CON2 product table; MAT1/MAT2 modules; US3 seam schedule; R1 archive. Proposed N1–N3: P1. Exact pages and uncertainty in dimensions.csv.',180,'small',20)
(O/'scale-comparison.svg').write_text(v.finish())

STYLE='''body{margin:0;background:#f6f2e9;color:#273e43;font:16px/1.6 system-ui,sans-serif}header,main,footer{max-width:1260px;margin:auto;padding:28px}h1{font-size:clamp(32px,5vw,56px);line-height:1.08;max-width:950px}h2{margin-top:38px}h3{margin:0}a{color:#176a78}nav{display:flex;flex-wrap:wrap;gap:18px}.intro{max-width:950px}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(320px,1fr));gap:22px}article,.panel{background:#fffdf8;border:1px solid #d8ccba;padding:22px;border-radius:6px}.tag{font-size:12px;text-transform:uppercase;letter-spacing:.06em;color:#756344}.muted{color:#596b6c}.gap{border-left:3px solid #b97857;padding-left:12px;font-size:14px}svg{max-width:100%;height:auto}table{border-collapse:collapse;width:100%;font-size:13px}td,th{padding:8px;text-align:left;border-bottom:1px solid #ded5c8;vertical-align:top}summary{cursor:pointer}select,button,input{font:inherit}select,button{padding:8px}input[type=range]{width:min(700px,90%)}label{display:block}#slots{display:grid;grid-template-columns:repeat(4,1fr);gap:14px}.slot{padding:12px;border:1px solid #d0c2ac;background:#fffdf8}.slot p{margin:4px;font-size:13px}.slot svg{width:100%}.legend{padding:18px;background:#e1e8e1}.tablewrap{overflow-x:auto}@media(max-width:700px){#slots{grid-template-columns:repeat(2,1fr)}}@media print{nav,select,input,button{display:none}article{break-inside:avoid}}'''
def page(title,body):return f'<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>{esc(title)}</title><style>{STYLE}</style></head><body>{body}</body></html>'
def source_url(s,prefix='../'):
    u=S[s]['url'];return u if u.startswith('https:') else prefix+u
def card_diagram(e):
    a=SVG(360,230,e['name']+' — original analytical diagram')
    if e['shape']:
        z=e['shape'];scale=7;x=25;y=28
        if 'rear_w' in z:
            rd=z['d']-z['front_d'];a.path(f'M{x} {y}h{z["rear_w"]*scale}v{rd*scale}h{(z["w"]-z["rear_w"])*scale}v{z["front_d"]*scale}H{x}Z','#b8cdd5')
        else:a.rect(x,y,z['w']*scale,z['d']*scale,'none' if z.get('module') else '#b8cdd5','#537c8d','4 3' if z.get('module') else '')
        a.text(200,48,f'{z["w"]:g} m','heading');a.text(200,70,f'× {z["d"]:g} m','heading')
        a.para(200,100,'Bay/depth module; not exterior walls.' if z.get('module') else 'Documented plan limits. No height inferred.',19)
        a.text(20,205,'PLAN · 7 units/m · original dimensional diagram','small')
    else:
        # Deliberately abstract access relationships, not a made-up measured elevation.
        a.rect(25,60,90,50,'#e2ded1');a.rect(205,60,125,50,'#adc4b5')
        a.path('M115 85H205M195 80L205 85L195 90')
        a.text(35,90,'Street','small');a.text(215,90,'Building / entry','small')
        if e['family'] in ('Courtyard / infill','Retrofit'):
            a.rect(205,140,125,35,'#dae3ce');a.path('M267 110v30');a.text(215,162,'Rear / addition','small')
        a.text(20,205,'ACCESS RELATION · not to scale or a reconstruction','small')
    return a.finish()
body='<header><div class="tag">Research pass 02 · present day / near future</div><h1>A city can have history<br>without looking historic.</h1><p class="intro">American and European references for ordinary contemporary buildings, retained postwar fabric and selective adaptation. The fictional mixture is provisional. All illustrated models are original analytical diagrams; source drawings and photographs remain linked.</p><nav><a href="../REPORT.md">Report</a><a href="../scale-comparison.svg">Scaled comparison</a><a href="../blend-study.html">Try the blend</a><a href="../model-briefs.md">Model briefs</a><a href="../dimensions.csv">Dimensions</a><a href="../sources.csv">Sources / rights</a></nav></header><main>'
body+='<div class="legend">15 records include related proposals and typology examples. They are not 15 independent measured surveys. Blue plans use documented dimensions; dashed plans are structural modules. Missing dimensions remain missing. IEE Projects TABULA + EPISCOPE (<a href="https://episcope.eu">www.episcope.eu</a>) supplies the German typology evidence.</div>'
body+='<h2>Three contexts to test</h2><div class="grid">'
for title,text,pal in [('US-informed','Pacific Northwest infill, with Midwest house plans as additional controls. No claim to represent every American city.','us'),('Dutch-informed','Contemporary rows and apartment families. German and French evidence broadens the European catalogue.','nl'),('Fictional mixture','Compatible complete designs share street, access and climate assumptions. Age and density stay independent.','mix')]:
    a=SVG(360,190,title+' proposed apartment treatment');model(a,25,145,'N3',pal,10)
    body+=f'<article><h3>{title}</h3>{a.finish()}<p>{text}</p><p class="tag">Proposed treatment · same N3 body</p></article>'
body+='</div><h2>Evidence atlas</h2><label for="family">Filter family</label><select id="family"><option value="all">All families</option>'
families=list(dict.fromkeys(e['family'] for e in EXAMPLES))
for f in families:body+=f'<option value="{esc(f)}">{esc(f)}</option>'
body+='</select>'
for f in families:
    body+=f'<section data-family="{esc(f)}"><h2>{esc(f)}</h2><div class="grid">'
    for e in [r for r in EXAMPLES if r['family']==f]:
        body+=f'<article id="{e["example_id"]}"><div class="tag">{e["example_id"]} · {esc(e["status"])}</div><h3>{esc(e["name"])}</h3><p class="muted">{esc(e["place"])} · {esc(e["era"])}</p>{card_diagram(e)}<p>{esc(e["findings"])}</p><p class="gap">Gap / limit: {esc(e["gaps"])}</p><p>'
        body+=' · '.join(f'<a href="{esc(source_url(s))}">{s}: source plans / photographs</a>' for s in e['source_ids'])
        body+=f'</p><p class="tag">{esc(e["sheet_page"])}</p>'
        dims=[d for d in DIMENSIONS if d['example_id']==e['example_id']]
        if dims:
            body+='<details><summary>Dimension ledger and units</summary><div class="tablewrap"><table><tr><th>Quantity</th><th>Published</th><th>Metric</th></tr>'
            for d in dims:body+=f'<tr><td>{esc(d["quantity"].replace("_"," "))}</td><td>{d["value"]} {esc(d["units"])}</td><td>{d["metric_value"]} {esc(d["metric_units"])}</td></tr>'
            body+='</table></div><p class="gap">See CSV for method and caveat on every row. Converted digits do not increase measurement precision.</p></details>'
        body+='</article>'
    body+='</div></section>'
body+='<h2>Workshop and commercial gap</h2><p>No new measured ordinary standalone workshop or retail-only plan was secured. Live/work is not evidence for industrial workshops. Use <a href="https://steelconstruction.info/topics/design/portal-frames">portal-frame construction guidance</a> and <a href="https://stg.wbdg.org/space-types/loading-dock">service-access relationships</a> as clearly labelled controls. Keep the first pass’s historical workshops as selective older examples.</p><h2>Rights and visual standing</h2><p>Municipal publication is not image-reuse permission. Source PDFs were downloaded to temporary storage for inspection; they are not redistributed here. These original plan envelopes and relationship diagrams are not traced facades, as-built reconstructions or production assets. See the source table for which photos, plans and sections were actually inspected.</p></main><footer><a href="../VALIDATION.md">Validation and remaining limits</a> · <a href="../../REPORT.md">First pass, historical recommendation</a></footer><script>document.querySelector("#family").addEventListener("change",e=>document.querySelectorAll("section[data-family]").forEach(s=>s.hidden=e.target.value!=="all"&&s.dataset.family!==e.target.value));</script>'
(O/'atlas/index.html').write_text(page('Present-day city architecture atlas',body))

# Interactive explanatory artifact. Each switch selects a complete proposed package.
forms={}
for k in ['N1','N2','N3']:
    for pal in ['us','nl']:
        a=SVG(280,175,k+' '+pal+' proposed treatment');model(a,15,145,k,pal,8);forms[k+'_'+pal]=a.finish()
slots=[{'id':i,'kind':['N1','N2','N3','N3'][i//2],'threshold':[12.5,37.5,62.5,87.5][i//2]} for i in range(8)]
(O/'evidence/blend-slots.json').write_text(json.dumps(slots,indent=2)+'\n')
body='<header><div class="tag">Proposed selection experiment · not gameplay</div><h1>Blend designs.<br>Keep their logic intact.</h1><p class="intro">Eight fixed illustrative slots, grouped in adjacent pairs. The control selects whole geometry-compatible treatments. It does not change footprints, storeys, uses, age or roof pitch. This is a deliberately simple demonstration, not an empirical distribution.</p><nav><a href="REPORT.md">Report</a><a href="atlas/index.html">Atlas</a><a href="mixing-rules.md">Selection rules and limits</a><a href="scale-comparison.svg">Scale sheet</a></nav></header><main><div class="panel"><label for="blend">US-informed 0 ← <strong id="value">50</strong> → 100 Dutch-informed</label><input type="range" id="blend" min="0" max="100" value="50" step="1"><p><button data-b="0">US endpoint</button> <button data-b="50">Mixture</button> <button data-b="100">Dutch endpoint</button></p><p id="count" aria-live="polite">4 US-informed + 4 Dutch-informed slots. Footprints and uses unchanged.</p></div><h2>Same slots, different selections</h2><div id="slots"></div><noscript>The static midpoint is shown. Enable JavaScript to switch packages, or read mixing-rules.md for the selection explanation.</noscript><h2>What this does not establish</h2><p>These are proposed treatments on shared architectural bodies. The small two-dimensional drawings cannot establish preference at game camera distances. Real eligibility can be asymmetric: some plots fit only one model family. Adding side galleries, garages, balconies or different setbacks would require a separately valid design. A midpoint does not halve a stair or structural span.</p><p>The count below the slider is a count of these eight slots, not people, dwellings, land area or a predicted city share. Thresholds are authored at 12.5, 37.5, 62.5 and 87.5; each switches one pair. Roof families occur on both sides.</p></main>'
initial=''.join('<div class="slot"><p><strong>Slot '+str(s['id']+1)+' · '+s['kind']+'</strong></p>'+forms[s['kind']+('_nl' if 50>=s['threshold'] else '_us')]+'<p>'+('Dutch-informed' if 50>=s['threshold'] else 'US-informed')+' proposed package</p></div>' for s in slots)
body=body.replace('<div id="slots"></div>','<div id="slots">'+initial+'</div>')
body+='<script>const FORMS='+json.dumps(forms)+';const SLOTS='+json.dumps(slots)+''';
function selected(b){return SLOTS.map(s=>({...s,package:b>=s.threshold?'nl':'us'}));}
function render(){const b=Number(document.querySelector('#blend').value),chosen=selected(b);document.querySelector('#value').textContent=b;document.querySelector('#slots').innerHTML=chosen.map(s=>`<div class="slot"><p><strong>Slot ${s.id+1} · ${s.kind}</strong></p>${FORMS[s.kind+'_'+s.package]}<p>${s.package==='us'?'US-informed':'Dutch-informed'} proposed package</p></div>`).join('');const nl=chosen.filter(s=>s.package==='nl').length;document.querySelector('#count').textContent=`${8-nl} US-informed + ${nl} Dutch-informed slots. All footprints and architectural uses unchanged.`;}
document.querySelector('#blend').addEventListener('input',render);document.querySelectorAll('[data-b]').forEach(b=>b.addEventListener('click',()=>{document.querySelector('#blend').value=b.dataset.b;render()}));render();
</script>'''
(O/'blend-study.html').write_text(page('Architectural influence blend study',body))
print(f'Wrote 7 required outputs plus blend study: {len(EXAMPLES)} atlas records, {len(SOURCES)} sources, {len(DIMENSIONS)} dimension rows.')

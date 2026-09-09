#!/usr/bin/env python3
from pathlib import Path
import html

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'art/ui-study'
# Each pair shares a metaphor and geometry; the second changes its visual weight.
ICONS = [
 ('Controls', 'inspect', 'Inspect', '<circle cx="10" cy="10" r="6"/><path d="m14.5 14.5 6 6"/>', '<path d="M10 2a8 8 0 1 0 4.9 14.3l5.4 5.4 2-2-5.5-5.3A8 8 0 0 0 10 2Zm0 3a5 5 0 1 1 0 10 5 5 0 0 1 0-10Z"/>'),
 ('Controls', 'play', 'Resume', '<path d="m8 4 12 8-12 8Z"/>', '<path d="m7 3 14 9-14 9Z"/>'),
 ('Controls', 'layers', 'Map layers', '<path d="m2 8 10-5 10 5-10 5Zm0 5 10 5 10-5M2 18l10 5 10-5"/>', '<path d="m1 7 11-5 11 5-11 5Zm0 6 3-1.5 8 3.7 8-3.7 3 1.5-11 5Zm0 6 3-1.5 8 3.7 8-3.7 3 1.5-11 5Z"/>'),
 ('Controls', 'policies', 'Policies', '<path d="M3 7h4m6 0h8M3 17h10m6 0h2"/><circle cx="10" cy="7" r="3"/><circle cx="16" cy="17" r="3"/>', '<path d="M2 6h5v2H2Zm11 0h9v2h-9ZM2 16h11v2H2Zm17 0h3v2h-3Z"/><circle cx="10" cy="7" r="4"/><circle cx="16" cy="17" r="4"/>'),
 ('Goods', 'produce', 'Produce', '<path d="M12 7C7 3 3 7 4 13s5 9 8 6c3 3 7 0 8-6s-3-10-8-6Zm0 0V4m0 0c1-3 5-3 6-2-1 3-4 4-6 2Z"/>', '<path d="M12 6C6 2 2 7 3 13c1 7 6 10 9 7 3 3 8 0 9-7 1-6-3-11-9-7Zm-1-1V2h2v3Z M13 4c0-3 4-4 7-3-1 3-4 4-7 3Z"/>'),
 ('Goods', 'food', 'Food', '<path d="M3 11c0-5 4-8 9-8s9 3 9 8v8H3Zm4-5 3 4m2-6 3 4m2-3 2 3M3 15h18"/>', '<path d="M12 2C6 2 2 6 2 11v10h20V11c0-5-4-9-10-9ZM6 7l2-1 3 4-2 1Zm6-2 2-1 3 4-2 1Zm-7 10h14v2H5Z"/>'),
 ('Goods', 'timber', 'Timber', '<path d="M5 6h14M5 18h14"/><ellipse cx="5" cy="12" rx="3" ry="6"/><path d="M19 6c4 0 4 12 0 12M9 9h8m-7 6h7"/>', '<path d="M6 5h12c6 0 6 14 0 14H6c-7 0-7-14 0-14Zm0 3c-3 0-3 8 0 8s3-8 0-8Zm5 0v2h7V8Zm0 6v2h7v-2Z"/>'),
 ('Goods', 'materials', 'Materials', '<path d="M2 5h20v14H2Zm0 7h20M9 5v7m6 0v7"/>', '<path d="M2 4h6v7H2Zm8 0h12v7H10ZM2 13h12v7H2Zm14 0h6v7h-6Z"/>'),
 ('Goods', 'consumer-goods', 'Consumer Goods', '<path d="M5 8h14l2 13H3Zm4 0V5a3 3 0 0 1 6 0v3"/>', '<path d="M7 7V5a5 5 0 0 1 10 0v2h3l2 15H2L4 7Zm3 0h4V5a2 2 0 0 0-4 0Z"/>'),
 ('Subjects / zones', 'housing', 'Housing / Building', '<path d="m2 11 10-8 10 8M5 9v12h14V9M10 21v-7h4v7"/>', '<path d="m1 11 11-9 11 9-2 2-2-1.6V22h-5v-8h-4v8H5V11.4L3 13Z"/>'),
 ('Subjects / zones', 'trade', 'Trade / Business', '<path d="M3 10h18l-2-6H5Zm2 0v11h14V10M9 21v-6h6v6M8 4l-1 6m9-6 1 6"/>', '<path d="M4 3h16l3 8H1Zm0 10h16v9h-6v-7h-4v7H4Z"/>'),
 ('Subjects / zones', 'mixed', 'Mixed permission', '<path d="m2 10 6-5 6 5M4 9v12h8V9m3 12V3h6v18m-3-14h0m0 5h0m0 5h0"/>', '<path d="m1 10 7-6 7 6-2 2-1-1v11H9v-6H7v6H3V11l-1 1ZM15 2h8v20h-8Zm3 4v3h2V6Zm0 6v3h2v-3Zm0 6v2h2v-2Z"/>'),
 ('Subjects / zones', 'household', 'Household', '<circle cx="8" cy="7" r="3"/><circle cx="17" cy="9" r="2.5"/><path d="M2 21v-4a6 6 0 0 1 12 0v4Zm13-7a5 5 0 0 1 7 5v2h-5"/>', '<circle cx="8" cy="7" r="4"/><circle cx="18" cy="9" r="3"/><path d="M1 22v-5a7 7 0 0 1 14 0v5Zm16 0v-5c0-1-.2-2-.5-3 4-1 6.5 2 6.5 5v3Z"/>'),
 ('Subjects / zones', 'school', 'School', '<path d="m2 8 10-5 10 5-10 5Zm4 3v6l6 3 6-3v-6m4-3v9"/>', '<path d="m0 8 12-6 12 6-12 6Zm5 6 7 3 7-3v5l-7 3-7-3ZM21 12h2v8h-2Z"/>'),
 ('Subjects / zones', 'clinic', 'Clinic', '<path d="M9 3h6v6h6v6h-6v6H9v-6H3V9h6Z"/>', '<path d="M8 2h8v6h6v8h-6v6H8v-6H2V8h6Z"/>'),
 ('Status', 'trouble', 'Needs attention', '<path d="m12 3 10 18H2ZM12 9v5m0 3h0"/>', '<path d="m12 1 12 22H0Zm-1 8v6h2V9Zm0 8v2h2v-2Z"/>'),
 ('Status', 'unavailable', 'Unavailable', '<path d="m12 1 11 11-11 11L1 12Z M9 9a3 3 0 0 1 6 0c0 2-3 2-3 5m0 3h0"/>', '<path d="m12 0 12 12-12 12L0 12Zm-4 9h2a2 2 0 0 1 4 0c0 2-3 1-3 5h2c0-2 3-2 3-5a4 4 0 0 0-8 0Zm3 7v3h2v-3Z"/>'),
 ('Status', 'waiting', 'Routine waiting', '<circle cx="12" cy="12" r="9"/><path d="M12 6v6l4 3"/>', '<path d="M12 1a11 11 0 1 0 0 22 11 11 0 0 0 0-22Zm-1 4h2v6.5l4 3-1.2 1.6-4.8-3.6Z"/>'),
 ('Status', 'clear', 'No reported issue', '<circle cx="12" cy="12" r="9"/><path d="m7 12 3 3 7-7"/>', '<path d="M12 1a11 11 0 1 0 0 22 11 11 0 0 0 0-22ZM6 12l2-2 3 3 6-6 2 2-8 8Z"/>'),
]

def shape(body, solid):
    return f'<g fill="currentColor" fill-rule="evenodd">{body}</g>' if solid else f'<g fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round">{body}</g>'

def icon(name, solid=False, size=24):
    return f'<svg width="{size}" height="{size}" viewBox="0 0 24 24" aria-hidden="true"><use href="#{name}-{int(solid)}"/></svg>'

def use(name, solid, x, y, size, ink):
    return f'<use href="#{name}-{int(solid)}" x="{x}" y="{y}" width="{size}" height="{size}" color="{ink}"/>'

OUT.mkdir(parents=True, exist_ok=True)
defs = ''
for group, name, label, outline, solid in ICONS:
    for filled, body in [(False, outline), (True, solid)]:
        content = shape(body, filled)
        defs += f'<symbol id="{name}-{int(filled)}" viewBox="0 0 24 24">{content}</symbol>'
        target = OUT / 'icons' / ('solid' if filled else 'outline')
        target.mkdir(parents=True, exist_ok=True)
        (target / f'{name}.svg').write_text(f'<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24">{content}</svg>\n')

palettes = [
 ('LIGHT', '#f1f5fa', '#fff', '#23354a', '#64748b', '#d5dfeb', '#a65a05'),
 ('DARK', '#1e2a39', '#263448', '#e8eff8', '#a9b9cc', '#405169', '#ffd18a'),
 ('LIGHT · GREYSCALE', '#f3f3f3', '#fff', '#303030', '#6a6a6a', '#d8d8d8', '#555'),
 ('DARK · GREYSCALE', '#252525', '#303030', '#eee', '#b2b2b2', '#515151', '#ccc'),
]
W, H = 1600, 1520
parts = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}"><defs>{defs}</defs><rect width="1600" height="1520" fill="#e7ebef"/>']
def text(x,y,t,size=14,fill='#23354a',weight=400):
    parts.append(f'<text x="{x}" y="{y}" font-family="Arial, sans-serif" font-size="{size}" font-weight="{weight}" fill="{fill}">{html.escape(t)}</text>')
def rect(x,y,w,h,fill,r=0):
    parts.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{r}" fill="{fill}"/>')
text(40,48,'BOROUGH / ICON STUDY 01',14,weight=700)
text(40,92,'One vocabulary. Two visual weights.',36,weight=700)
text(40,126,'Custom SVG candidates · 24-unit grid · outline 1.75-unit stroke · displayed at 24 px and 32 px',17)
text(40,153,'Read across: the same symbol, theme and size. Labels stay. No interface changes have been applied.',15,fill='#52677b')
for col,(title,bg,surface,ink,muted,line,accent) in enumerate(palettes):
    x=40+col*390
    rect(x,184,350,1108,bg,14)
    text(x+20,215,title,13,ink,700)
    text(x+154,246,'OUTLINE',11,muted,700);text(x+252,246,'SOLID',11,muted,700)
    for dx,t in [(154,'24'),(196,'32'),(252,'24'),(294,'32')]:text(x+dx,266,t,11,muted)
    y=279;last=None
    for group,name,label,outline,solid in ICONS:
        if group!=last:
            rect(x+16,y,318,25,surface,4);text(x+24,y+17,group.upper(),10,muted,700)
            y+=33;last=group
        text(x+20,y+26,label,12,ink)
        tone=accent if group=='Status' and name=='trouble' else ink
        for dx,filled,size in [(151,False,24),(192,False,32),(249,True,24),(290,True,32)]:
            parts.append(use(name,filled,x+dx,y+(44-size)/2,size,tone))
        y+=44
    rect(x+16,y+6,318,1,line)
    text(x+20,y+32,'Compare at 100% zoom for actual sizes.',11,muted)
text(40,1320,'Suggested division of work',21,weight=700)
text(40,1351,'Controls + labelled readings',16,weight=700)
text(40,1377,'Outline: quiet enough to repeat beside text.',15)
text(555,1351,'Marks over the city',16,weight=700)
text(555,1377,'Solid: stronger silhouette over detailed ground.',15)
text(1080,1351,'Meaning without colour',16,weight=700)
text(1080,1377,'Triangle ! · diamond ? · clock · check.',15)
text(40,1435,'Decisions to review',13,weight=700)
text(40,1462,'Are the Goods distinct? Does the family feel right for Borough? Are outline controls + solid world marks the right pairing?',16)
text(40,1492,'Study assets created for Borough · provisional symbols · map patterns and selection outlines need their own in-context check.',12,fill='#52677b')
parts.append('</svg>')
(OUT/'icon-comparison.svg').write_text(''.join(parts))

rows=''
for group,name,label,outline,solid in ICONS:
    rows+=f'<tr data-group="{group}"><th><small>{group}</small>{label}</th><td>{icon(name)}{icon(name,False,32)}</td><td>{icon(name,True)}{icon(name,True,32)}</td></tr>'
panels=''
for n,(title,bg,surface,ink,muted,line,accent) in enumerate(palettes):
    panels+=f'<section class="panel" data-mode="{n}" style="--bg:{bg};--surface:{surface};--ink:{ink};--muted:{muted};--line:{line};--accent:{accent}"><h2>{title}</h2><table><thead><tr><th>Symbol</th><th>Outline <small>24 / 32 px</small></th><th>Solid <small>24 / 32 px</small></th></tr></thead><tbody>{rows}</tbody></table></section>'
page='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Borough · Icon comparison</title>
<style>
*{box-sizing:border-box}body{margin:0;background:#e7ebef;color:#23354a;font-family:Arial,sans-serif}header,footer{padding:32px 40px}h1{font-size:36px;margin:12px 0}p{line-height:1.5;margin:8px 0}.eyebrow{font-size:13px;font-weight:bold;letter-spacing:.1em}.controls{display:flex;gap:10px;align-items:center;flex-wrap:wrap;margin-top:20px}button,a{color:inherit}button{padding:10px 15px;border:1px solid #9aabbd;border-radius:7px;background:white;font:inherit;cursor:pointer}button[aria-pressed=true]{background:#23354a;color:white}main{display:grid;grid-template-columns:repeat(4,minmax(310px,1fr));gap:20px;padding:0 40px}.panel{background:var(--bg);color:var(--ink);padding:20px;border-radius:12px}h2{font-size:13px;letter-spacing:.08em;margin:3px 0 22px}table{border-collapse:collapse;width:100%;table-layout:fixed}th{text-align:left;font-weight:400}thead th{font-size:12px;font-weight:bold;color:var(--muted);padding-bottom:14px}thead th:first-child{width:43%}small{display:block;font-size:10px;color:var(--muted);font-weight:normal;margin:4px 0}tbody th{font-size:12px}td,tbody th{border-top:1px solid var(--line);height:58px}td svg{vertical-align:middle;margin-right:9px}tr[data-group=Status] td{color:var(--ink)}tr:nth-last-child(4) td{color:var(--accent)}.notes{display:grid;grid-template-columns:repeat(3,1fr);gap:40px}footer h3{font-size:16px;margin:0 0 8px}.links{display:flex;gap:24px;margin-top:24px}.solo main{grid-template-columns:minmax(310px,600px);justify-content:center}@media(max-width:1390px){main{grid-template-columns:repeat(2,minmax(310px,1fr))}}@media(max-width:720px){main{grid-template-columns:1fr;padding:0 20px}header,footer{padding:24px 20px}.notes{grid-template-columns:1fr;gap:16px}}
</style><body><svg width="0" height="0" style="position:absolute" aria-hidden="true"><defs>DEFS</defs></svg>
<header><div class="eyebrow">BOROUGH / ICON STUDY 01</div><h1>One vocabulary. Two visual weights.</h1><p>Original SVG candidates on a 24-unit grid. Each pair is shown at 24 and 32 pixels. View at 100% browser zoom.</p><p>Keep labels. Compare recognisability and visual weight before choosing a family.</p><nav class="controls" aria-label="Comparison view"><button data-view="all" aria-pressed="true">All treatments</button><button data-view="0" aria-pressed="false">Light</button><button data-view="1" aria-pressed="false">Dark</button><button data-view="2" aria-pressed="false">Light greyscale</button><button data-view="3" aria-pressed="false">Dark greyscale</button></nav></header><main>PANELS</main>
<footer><div class="notes"><div><h3>Controls and labelled readings</h3><p>Outline is the quieter candidate for repeated icons beside text.</p></div><div><h3>Marks over the city</h3><p>Solid is the stronger candidate over detailed ground. Check it against the city before adopting it.</p></div><div><h3>Meaning without colour</h3><p>Attention uses a triangle, unavailable a diamond, routine waiting a clock. A check means only “no reported issue”.</p></div></div><p class="links"><a href="icon-comparison.svg">Standalone SVG sheet</a><a href="icon-comparison.png">PNG sheet</a></p><p>Study only. No interface changes applied. Map patterns and selection outlines require a separate in-context check.</p></footer>
<script>document.querySelectorAll('[data-view]').forEach(button=>button.onclick=()=>{const view=button.dataset.view;document.body.classList.toggle('solo',view!=='all');document.querySelectorAll('[data-view]').forEach(b=>b.setAttribute('aria-pressed',String(b===button)));document.querySelectorAll('[data-mode]').forEach(p=>p.hidden=view!=='all'&&p.dataset.mode!==view);});</script></body></html>'''
(OUT/'icon-comparison.html').write_text(page.replace('DEFS',defs).replace('PANELS',panels))
print(OUT/'icon-comparison.svg')

#!/usr/bin/env python3
from pathlib import Path
import csv,json,re,html,xml.etree.ElementTree as ET
O=Path(__file__).resolve().parent
E=html.escape
CSS='''*{box-sizing:border-box}body{margin:0;background:#f5f3ed;color:#263237;font:17px/1.6 system-ui,sans-serif}main{max-width:1200px;margin:auto;padding:38px 30px 80px}h1{font-size:44px;line-height:1.13;max-width:930px;margin:20px 0}h2{font-size:28px;line-height:1.25;margin-top:45px}h3{font-size:21px;line-height:1.3}p{max-width:950px}a{color:#246479;text-underline-offset:3px}nav{display:flex;gap:10px 25px;flex-wrap:wrap;border-bottom:1px solid #ccd0ca;padding:0 0 20px;font-size:15px}.eyebrow{letter-spacing:.12em;text-transform:uppercase;font-size:13px;color:#7c5a3b}.lead{font-size:22px;max-width:970px}.grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:24px}.card{background:#fffefa;border:1px solid #d8dbd2;border-radius:6px;padding:22px;min-width:0;scroll-margin-top:20px}.card p{font-size:15px}.card img{width:100%;height:260px;object-fit:contain;background:#faf9f5}.tag{font-size:12px;display:inline-block;background:#e6eae3;padding:4px 8px;margin-right:5px;border-radius:3px}.warning{border-left:4px solid #ae7947;background:#ede7dc;padding:14px 20px}.figure{margin:28px 0;background:#fffefa;border:1px solid #d8dbd2;padding:16px}.figure img{width:100%;height:auto;display:block}.figure figcaption{font-size:14px;margin-top:12px}.small{font-size:14px;color:#536066}table{width:100%;border-collapse:collapse;background:#fffefa;font-size:14px;margin:24px 0;display:block;overflow-x:auto}th,td{padding:12px;text-align:left;vertical-align:top;border-bottom:1px solid #d8dbd2;min-width:110px}th{background:#e8ebe5}code{background:#e9ebe5;padding:2px 4px;font-size:.9em;overflow-wrap:anywhere}pre{padding:18px;background:#e9ebe5;overflow:auto}li{margin:8px 0}footer{margin-top:50px;border-top:1px solid #ccd0ca;padding-top:20px;font-size:14px}.wide{grid-column:1/-1}@media(max-width:750px){main{padding:24px 16px}h1{font-size:32px}.grid{grid-template-columns:1fr}.lead{font-size:19px}}@media print{body{background:white}main{padding:0}nav{display:none}.card,.figure{break-inside:avoid}a{color:#263237}}'''
(O/'style.css').write_text(CSS)
def page(title,body,rel=''):
 return '<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>'+E(title)+'</title><link rel="stylesheet" href="'+rel+'style.css"><main><nav><a href="'+rel+'index.html">Review</a><a href="'+rel+'REPORT.html">Decisions</a><a href="'+rel+'atlas/index.html">Evidence atlas</a><a href="'+rel+'coverage-and-audit.html">Coverage + prior audit</a><a href="'+rel+'model-briefs.html">Prototype briefs</a><a href="'+rel+'VALIDATION.html">Validation</a></nav>'+body+'<footer>Pass 03 · original research proposals and traceable evidence · no gameplay or content changes · uncommitted</footer></main></html>'
def inline(s):
 s=E(s)
 s=re.sub(r'!\[([^\]]*)\]\(([^)]+)\)',lambda m:'<img style="max-width:100%" alt="'+m[1]+'" src="'+m[2]+'">',s)
 def link(m):
  url=m[2]
  if url.endswith('.md') and (O/url).is_file():url=url[:-3]+'.html'
  return '<a href="'+url+'">'+m[1]+'</a>'
 s=re.sub(r'\[([^\]]*)\]\(([^)]+)\)',link,s)
 s=re.sub(r'\*\*([^*]+)\*\*',r'<strong>\1</strong>',s)
 s=re.sub(r'`([^`]+)`',r'<code>\1</code>',s)
 return s
def md(text):
 lines=text.splitlines();out=[];i=0
 while i<len(lines):
  l=lines[i]
  if not l.strip():i+=1;continue
  if l.startswith('|'):
   out.append('<table>');n=0
   while i<len(lines) and lines[i].startswith('|'):
    if not re.match(r'^\|[\s:|\-]+$',lines[i]):
     tag='th' if n==0 else 'td';out.append('<tr>'+''.join('<'+tag+'>'+inline(x.strip())+'</'+tag+'>' for x in lines[i].strip('|').split('|'))+'</tr>');n+=1
    i+=1
   out.append('</table>');continue
  if l.startswith('#'):
   level=len(l)-len(l.lstrip('#'));out.append(f'<h{level}>'+inline(l[level:].strip())+f'</h{level}>');i+=1;continue
  if re.match(r'^\d+\. ',l) or l.startswith('- '):
   out.append('<ul>')
   while i<len(lines) and (re.match(r'^\d+\. ',lines[i]) or lines[i].startswith('- ')):
    out.append('<li>'+inline(re.sub(r'^(\d+\.|-) ','',lines[i]))+'</li>');i+=1
   out.append('</ul>');continue
  para=[]
  while i<len(lines) and lines[i].strip() and not lines[i].startswith(('#','|')):para.append(lines[i]);i+=1
  out.append('<p>'+inline(' '.join(para))+'</p>')
 return ''.join(out)
(O/'REPORT.md').write_text((O/'report-source.md').read_text())
for p in O.glob('*.md'):
 if p.name in ['report-source.md','RESEARCH-PLAN.md']:continue
 (O/(p.stem+'.html')).write_text(page(p.stem,md(p.read_text())))
sources=list(csv.DictReader((O/'sources.csv').open()))
# Each thumbnail is a crop of an original analytical drawing, never a third-party image.
thumbs={
'CA1':('low-housing.svg','0 80 370 630'),'CA2':('evidence-controls.svg','760 80 380 250'),
'CA3':('retail-history.svg','0 80 560 445'),'CA4':('workplace.svg','20 90 535 335'),
'CA5':('workplace.svg','570 440 540 295'),'CA6':('construction.svg','20 385 1110 220'),

'US1':('evidence-controls.svg','20 80 350 410'),'NL1':('evidence-controls.svg','380 80 380 255'),
'PNW1':('fictional.svg','0 590 1130 200'),'NL2':('low-housing.svg','390 80 360 630'),
'DK1':('controlled-housing.svg','590 85 550 590'),'DK2':('low-housing.svg','0 80 370 630'),
'DK3':('controlled-housing.svg','590 145 520 245'),'ES1':('evidence-controls.svg','380 330 380 160'),
'ES2':('valencia.svg','0 80 550 500'),'C1':('construction.svg','760 80 390 285'),
'C2':('construction.svg','20 385 1110 220'),'MAT1':('construction.svg','575 625 580 245'),
'RFT1':('retail-history.svg','0 540 1140 225'),'RFT2':('retail-history.svg','0 540 1140 225')}
source_evidence={'CA2','US1','NL1','ES1','MAT1'}
body='<p class="eyebrow">Evidence atlas / traceable constraints</p><h1>Look at the source, then the proposed consequence.</h1><p class="lead">24 external source records and two internal records are not 24 measured buildings. This atlas combines existing examples, design proposals, construction typologies and technical controls. Their standing stays attached to every claim.</p><p class="warning">Thumbnails are original numerical abstractions or modelling proposals. Third-party photographs and drawings remain at their source links; no generated imagery is evidence. Local inspection copies are not redistributed.</p><p><a href="../drawings/evidence-controls.svg">Open the dimensional evidence plate</a> · <a href="../dimensions.csv">Dimension CSV</a> · <a href="../sources.csv">Full provenance CSV</a></p><div class="grid">'
for r in sources:
 id=r['source_id'];body+='<article class="card" id="'+id+'"><span class="tag">'+id+'</span><span class="tag">'+E(r['standing'])+'</span><h2>'+E(r['title'])+'</h2>'
 if id in thumbs:
  name,box=thumbs[id];txt=(O/'drawings'/name).read_text();txt=re.sub(r'viewBox="[^"]+"',f'viewBox="{box}"',txt,count=1)
  # The thumbnail may crop the full source title. Its own alternative text remains precise.
  (O/'drawings'/f'thumb-{id}.svg').write_text(txt)
  body+='<img src="../drawings/thumb-'+id+'.svg" alt="'+E(('Original dimension abstraction for ' if id in source_evidence else 'Proposed modelling consequence associated with ')+id)+'"><p class="small">'+('Numerical abstraction; unknown boundaries stay unresolved.' if id in source_evidence else 'PROPOSAL thumbnail: illustrates a possible modelling consequence, not this source building.')+'</p>'
 body+='<p>'+E(r['claim'])+'</p><p><strong>Limit:</strong> '+E(r['uncertainty'])+'</p><p class="small">'+E(r['author'])+' · '+E(r['date'])+' · '+E(r['pages'])+'</p><p><a href="'+E(r['url'] if r['url'].startswith('https:') else '../'+r['url'],quote=True)+'">Open primary source / drawings and photographs</a></p><p class="small"><strong>Inspection:</strong> '+E(r['inspection'])+'</p><p class="small"><strong>Reuse:</strong> '+E(r['reuse'])+'</p></article>'
body+='</div><h2>What is still missing</h2><p>Measured ordinary European retail, office and workshop plans; verified whole-block València dimensions; current local code compliance; consistent floor-to-floor surveys. The contextual drawings supply concrete proposed alternatives without pretending these gaps are filled.</p>'
(O/'atlas/index.html').write_text(page('Evidence atlas — city architecture pass03',body,'../'))
def fig(name,caption):return '<figure class="figure"><a href="drawings/'+name+'.svg"><img src="drawings/'+name+'.svg" alt="'+E(caption)+'"></a><figcaption>'+E(caption)+' · <a href="drawings/'+name+'.svg">Open full-size SVG</a></figcaption></figure>'
body='<p class="eyebrow">City architecture / pass 03 / review edition 7 September 2026</p><h1>Different buildings. Credible streets. A fictional city.</h1><p class="lead">The provisional starting point is a temperate fictional mixture. The choice is now tested through plans, access, roofs and service ground—before colour. Five specific contexts provide alternatives, with unequal evidence strength made explicit.</p><p class="warning"><strong>Research proposals, not game changes.</strong> All contextual street dimensions are authored. The archived game sample is reconstructed, not rerun. Source-derived dimensions are separately identified.</p>'
body+='<h2>Start here: a ten-minute review</h2><ol><li>Compare the two apartment plans and entrances below. Does the difference remain visible without colour?</li><li>Inspect the five contexts, then the fictional assembly. Decide which street relationships feel right.</li><li>Open the workplace sheet: follow the upper floor, receiving route and roof drainage.</li><li>Only then compare the material pass. Judge warmth and detail separately from architecture.</li></ol><p><a href="review-guide.html">Review questions</a> · <a href="REPORT.html">Concise decision report</a> · <a href="coverage-and-audit.html">What the previous pass overclaimed</a></p>'
body+=fig('controlled-housing','A / Equal residential use, gross envelope and height. Different depth, length and stair organisation. Both are proposals.')
body+='<h2 id="contexts">B / Five coherent contexts, then a designed mixture</h2><p>Each has an 80 m study frontage at a common plan scale. Plots, setbacks and access deliberately differ. Shared comparison size is a drawing device, not evidence that the actual blocks are equal.</p>'
for slug,title in [('sacramento','Sacramento: low postwar bodies, forecourt apartments and commercial service ground'),('south-bend','South Bend: incremental infill, stoops and rear lane'),('amersfoort','Amersfoort: attached modules, gardens and a connected rear path'),('tingbjerg','Tingbjerg: apartment ranges, shared open ground and selective infill'),('valencia','València application: compact mixed street; regional block geometry remains provisional'),('fictional','Fictional start: attached homes, apartment, corner commerce and rear workplaces')]:body+=fig(slug,title)
body+='<h2>Archived visual checkpoint</h2><figure class="figure"><img src="../evidence/checkpoint.png" alt="Archived d9addc3 game capture with broad bodies and paired roofs"><figcaption>Retained d9addc3 daylight-near capture, byte-verified against git. Shopping fixture, Tick 600, 400 Citizens. Not a new run or a matched-camera comparison with the research diagrams.</figcaption></figure>'
body+='<h2>C / Workplaces and construction</h2><p>G001 retains its archived footprint, walls and two complete floors. Industrial-looking roof geometry does not justify dropping the upper floor.</p>'+fig('workplace','W1 / 36 ×20 m, two full floors, 7 m walls. Appearance and use interpretation proposed; capacity remains a separate question.')
body+=fig('construction','Roof spans, rise and drainage are explicit. Exploded layers have a labelled scale exception.')
body+='<h2>D / Low housing and history</h2>'+fig('low-housing','Detached, semi-detached and attached proposals: fronts, plans and garden elevations.')+fig('retail-history','Within-region retail layout difference and proposed repair/retrofit sequence.')
body+='<h2>E / Materials follow geometry</h2>'+fig('fictional-materials','Same fictional geometry with a separate warm/slate treatment. This is not a regional endpoint.')
body+='<h2>Evidence and next experiment</h2>'+fig('evidence-controls','Original dimensional abstractions preserve area-definition conflicts and unknown boundaries.')
body+='<div class="grid">'
for title,file,desc in [('Decision report','REPORT.html','Recommendations, confidence and answers to the nine questions.'),('Evidence atlas','atlas/index.html','Source provenance, inspected sheets, limitations and reuse standing.'),('Coverage + audit','coverage-and-audit.html','What is supported, proposed, untested or left for judgment.'),('Construction rules','construction-rules.html','Independent variation, coupled changes and invalid combinations.'),('Mixing rules','mixing-rules.html','Building, street and neighbourhood compatibility; slider decision.'),('Simulation audit','simulation-audit.html','Reproducible checkpoint sample, current owners and capacity conflicts.'),('Prototype briefs','model-briefs.html','Five-body street plus alternative range, exact dimensions and review tests.'),('Validation','VALIDATION.html','What was verified, sampled, corrected and not performed.')]:body+='<div class="card"><h3><a href="'+file+'">'+title+'</a></h3><p>'+desc+'</p></div>'
body+='</div><p><a href="dimensions.csv">Dimension table CSV</a> · <a href="sources.csv">Source table CSV</a> · <a href="evidence/game-sample.csv">Archived sample CSV</a></p>'
(O/'index.html').write_text(page('City architecture — illustrated review pass03',body))
print('Built review, atlas and document HTML companions.')

#!/usr/bin/env python3
from pathlib import Path
from html.parser import HTMLParser
from urllib.parse import urlsplit,unquote
import csv,json,hashlib,math,xml.etree.ElementTree as ET,subprocess
O=Path(__file__).resolve().parent
class Links(HTMLParser):
 def __init__(self):super().__init__();self.links=[];self.ids=[]
 def handle_starttag(self,tag,attrs):
  a=dict(attrs)
  for k in ('href','src'):
   if k in a:self.links.append(a[k])
  if 'id' in a:self.ids.append(a['id'])
checks={};broken=[];count=0
for p in O.rglob('*.html'):
 h=Links();h.feed(p.read_text());assert len(h.ids)==len(set(h.ids)),p
 for link in h.links:
  u=urlsplit(link)
  if u.scheme or not u.path:continue
  target=(p.parent/unquote(u.path)).resolve();count+=1
  if not target.is_file():broken.append([str(p.relative_to(O)),link])
  elif u.fragment and target.suffix=='.html':
   dest=Links();dest.feed(target.read_text())
   if u.fragment not in dest.ids:broken.append([str(p.relative_to(O)),link,'missing anchor'])
checks['local_links_checked']=count;checks['broken_local_links']=broken
assert not broken,broken
svgs=list((O/'drawings').glob('*.svg'))
for p in svgs:
 t=ET.parse(p).getroot();assert t.find('{http://www.w3.org/2000/svg}title') is not None,p
checks['svg_xml_and_titles']=len(svgs)
dims=list(csv.DictReader((O/'dimensions.csv').open()));sources=list(csv.DictReader((O/'sources.csv').open()));ids={r['source_id'] for r in sources}
for r in dims:
 assert r['source_id'] in ids,r
 v=float(r['original_value']);m=float(r['metric_value']);unit=r['original_unit'];expected=v
 if unit=='ft':expected=v*.3048
 elif unit=='sq ft':expected=v*.09290304
 elif unit=='mm':expected=v/1000
 elif unit=='cm':expected=v/100
 elif unit=='in/ft':expected=v/12*100
 assert math.isclose(m,expected,rel_tol=1e-9,abs_tol=1e-9),r
checks['dimension_rows_verified']=len(dims);checks['source_records']=len(sources)
streets=json.loads((O/'evidence/street-proposals.json').read_text());area={}
for slug,title,sub,bs,note in streets:
 area[slug]=sum(b['w']*b['d']*b['n'] for b in bs)
 for i,a in enumerate(bs):
  assert 0<=a['x'] and 0<=a['y'] and a['x']+a['w']<=80 and a['y']+a['d']<=64
  assert a['y']+a['d']<=33 or a['y']>=39,(slug,'lane obstruction',a)
  for b in bs[i+1:]:assert not (a['x']<b['x']+b['w'] and a['x']+a['w']>b['x'] and a['y']<b['y']+b['d'] and a['y']+a['d']>b['y']),(slug,a,b)
  if a['name']=='G001 workplace':assert (a['w'],a['d'],a['n'],a['height'])==(36,20,2,7)
checks['street_body_nonoverlap_and_lane_clear']=len(streets);checks['proposed_street_gross_envelopes_m2']=area
assert 24*16*3==32*12*3==1152
checks['controlled_floor_envelope_equal']=True
raw=list(csv.DictReader((O/'evidence/game-sample.csv').open()));assert len(raw)==52
for r in raw:
 floor=float(r['width_m'])*float(r['depth_m'])*int(r['storeys']);assert floor==int(r['floor_m2']);assert max(1,int(floor/16)//25)==int(r['tenancies'])
checks['archived_rows_arithmetic']=len(raw)
# preserve hashes of source files inspected locally without redistributing them
files={r['source_id']:r['local'] for r in sources if r['local']}
files.update(US1='/private/tmp/city-architecture-research/southbend.pdf',NL1='/private/tmp/hogekwartier.pdf',DK2='/private/tmp/architecture3-typehouse.pdf')
hashes={};matched=[]
prior=json.loads((O.parent/'02-present-day/evidence/download-hashes.json').read_text())
for id,name in files.items():
 p=Path(name)
 if p.is_file():
  b=p.read_bytes();hashes[id]=dict(sha256=hashlib.sha256(b).hexdigest(),bytes=len(b),redistributed=False)
  if id in prior:
   assert hashes[id]['sha256']==prior[id]['sha256'],(id,'prior cached source changed')
   matched.append(id)
(O/'evidence/download-hashes.json').write_text(json.dumps(hashes,indent=2))
checks['retained_source_hashes_match']=matched
checks['no_fresh_simulation_run']=True
checks['render_records']=[p.name for p in sorted((O/'evidence').glob('render-checks*.json'))]
for name in checks['render_records']:
 for r in json.loads((O/'evidence'/name).read_text()):
  assert not r['brokenImages'] and not r['svgOverflow'],r
  assert r['scrollWidth']<=r['width'],r
checks['rendered_viewport_checks']=True
checks['visual_inspection']='Sampled; listed in VALIDATION.md, not claimed exhaustive.'
(O/'evidence/validation.json').write_text(json.dumps(checks,indent=2)+'\n')
print(json.dumps(checks,indent=2))

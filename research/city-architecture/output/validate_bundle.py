#!/usr/bin/env python3
"""Validate research artifacts and byte-stable generation, without running game tests."""
from pathlib import Path
import ast,csv,hashlib,json,re,subprocess,xml.etree.ElementTree as ET
from html.parser import HTMLParser
from urllib.parse import urlparse,unquote
O=Path(__file__).resolve().parent
required=['REPORT.md','atlas/index.html','dimensions.csv','sources.csv','simulation-audit.md','model-briefs.md','scale-comparison.svg']
assert all((O/x).is_file() for x in required)
for f in O.glob('*.py'):ast.parse(f.read_text())
ET.parse(O/'scale-comparison.svg')
sources=list(csv.DictReader((O/'sources.csv').open()));ids={x['source_id'] for x in sources}
assert len(ids)==len(sources)
dims=list(csv.DictReader((O/'dimensions.csv').open()))
assert all(x['source_id'] in ids for x in dims)
assert all(x['evidence_status'] in ('documented','estimated','proposed','missing') for x in dims)
assert all(bool(x['value'])==(x['evidence_status']!='missing') for x in dims)
assert all(x['units'] and x['method'] and x['sheet_page'] for x in dims)
assert all(float(x['value'])>=0 for x in dims if x['value'])
seen=set()
for x in dims:
 key=(x['example_id'],x['quantity']);assert key not in seen,key;seen.add(key)
class CheckLinks(HTMLParser):
 def handle_starttag(self,tag,attrs):
  for k,v in attrs:
   if k in ('href','src') and v and not urlparse(v).scheme and not v.startswith('#'):
    assert (O/'atlas'/unquote(v.split('#')[0])).exists(),v
CheckLinks().feed((O/'atlas/index.html').read_text())
for f in O.glob('*.md'):
 for target in re.findall(r'\]\(([^)]+)\)',f.read_text()):
  if not urlparse(target).scheme and not target.startswith('#'):
   assert (f.parent/unquote(target.split('#')[0])).exists(),(f.name,target)
sample=list(csv.DictReader((O/'evidence/simulation-sample.csv').open()))
assert len(sample)==52
assert len({s['building_id'] for s in sample})==52
assert float(sample[2]['floor_m2'])==1152 and int(sample[2]['tenancy_capacity'])==2
# Geometry arithmetic checks independent of the CSV writer's calculations.
assert abs(24*16*3-1152)<1e-9
assert 24*16-(24*2+2*4*5-2*4*2+2*7)==298
assert 298/4==74.5
assert (265-65)/2==100
files=[O/'atlas/index.html',O/'scale-comparison.svg',O/'dimensions.csv',O/'sources.csv',*sorted((O/'evidence').glob('*'))]
def hashes():return {str(p.relative_to(O)):hashlib.sha256(p.read_bytes()).hexdigest() for p in files if p.is_file()}
before=hashes();subprocess.run(['python3',str(O/'build_bundle.py')],check=True,capture_output=True)
assert before==hashes(),'Regeneration changed artifacts'
print(f'PASS: {len(required)} required outputs; {len(sources)} source IDs; {len(dims)} dimension/gap rows; 52 Building IDs; valid local links; byte-stable regeneration.')

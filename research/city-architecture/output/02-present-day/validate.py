#!/usr/bin/env python3
"""Artifact and arithmetic checks only; does not execute the simulation."""
from pathlib import Path
import ast,csv,hashlib,json,re,subprocess,xml.etree.ElementTree as ET
from html.parser import HTMLParser
from urllib.parse import urlparse,unquote
O=Path(__file__).resolve().parent
required=['REPORT.md','atlas/index.html','dimensions.csv','sources.csv','simulation-audit.md','model-briefs.md','scale-comparison.svg']
assert all((O/x).is_file() and (O/x).stat().st_size>0 for x in required)
for p in O.glob('*.py'):ast.parse(p.read_text())
src=list(csv.DictReader((O/'sources.csv').open()));ids={r['source_id'] for r in src}
assert len(ids)==len(src)
claims=list(csv.DictReader((O/'evidence/claims.csv').open()))
assert all(set(c['source_ids'].split())<=ids for c in claims)
dims=list(csv.DictReader((O/'dimensions.csv').open()));seen=set()
factors={'ft':.3048,'ft2':.09290304,'in':.0254,'mm':.001}
for d in dims:
    assert d['source_id'] in ids and d['sheet_page'] and d['method']
    assert d['evidence_status'] in ('documented','proposed','estimated','missing')
    key=(d['example_id'],d['quantity']);assert key not in seen,key;seen.add(key)
    assert abs(float(d['metric_value'])-float(d['value'])*factors.get(d['units'],1))<1e-7,d
assert (O/'REPORT.md').read_bytes()==(O/'report-source.md').read_bytes()
def local(base,target):
    if target and not urlparse(target).scheme and not target.startswith('#'):
        assert (base/unquote(target.split('#')[0])).exists(),(base,target)
class Links(HTMLParser):
    def __init__(self,base):super().__init__();self.base=base
    def handle_starttag(self,tag,attrs):
        for k,v in attrs:
            if k in ('href','src'):local(self.base,v)
for p in O.rglob('*.html'):Links(p.parent).feed(p.read_text())
for p in O.rglob('*.md'):
    for target in re.findall(r'\]\(([^)]+)\)',p.read_text()):local(p.parent,target)
for s in src:local(O,s['url'])
tree=ET.parse(O/'scale-comparison.svg');root=tree.getroot()
w,h=map(float,[root.attrib['width'],root.attrib['height']])
for t in root.iter('{http://www.w3.org/2000/svg}text'):
    assert 0<=float(t.attrib['x'])<w and 0<=float(t.attrib['y'])<h
sample=list(csv.DictReader((O/'evidence/game-sample.csv').open()))
assert len(sample)==len({r['id'] for r in sample})==52
g3=next(r for r in sample if r['id']=='3')
assert (int(g3['floor_m2']),int(g3['tenancies']))==(1152,2)
assert 24*16*3==1152
assert 24*2+2*4*5-2*4*2+2*7==86
assert (24*16-86)/4==74.5
assert (265-65)/2==100
# Execute the actual interactive script against a small DOM harness.
script=re.findall(r'<script>([\s\S]*?)</script>',(O/'blend-study.html').read_text())[-1]
assert (O/'blend-study.html').read_text().split('<script>')[0].count('class="slot"')==8
harness='''const vm=require('node:vm'),assert=require('node:assert/strict');
const nodes={};for(const id of ['#blend','#value','#slots','#count'])nodes[id]={value:'50',textContent:'',innerHTML:'',addEventListener(){}};
const buttons=[0,50,100].map(x=>({dataset:{b:String(x)},addEventListener(k,fn){this.click=fn;}}));
const document={querySelector:s=>nodes[s],querySelectorAll:()=>buttons};
const context=vm.createContext({document});vm.runInContext(SCRIPT,context);
let previous=0;for(let b=0;b<=100;b++){nodes['#blend'].value=String(b);vm.runInContext('render()',context);const chosen=vm.runInContext('selected('+b+')',context),nl=chosen.filter(s=>s.package==='nl').length;assert(nl>=previous);previous=nl;assert.equal(chosen.length,8);assert.equal((nodes['#slots'].innerHTML.match(/class="slot"/g)||[]).length,8);assert.deepEqual(Array.from(chosen,s=>s.kind),['N1','N1','N2','N2','N3','N3','N3','N3']);if(b===0)assert.equal(nl,0);if(b===50)assert.equal(nl,4);if(b===100)assert.equal(nl,8);}
for(const b of buttons){b.click();assert.equal(nodes['#blend'].value,b.dataset.b);}
console.log('PASS: actual blend script, 101 settings, endpoint buttons, stable slot types.');
'''.replace('SCRIPT',json.dumps(script))
r=subprocess.run(['node','-e',harness],text=True,capture_output=True)
assert r.returncode==0,r.stderr
print(r.stdout.strip())
outputs=[O/p for p in required]+[O/'blend-study.html',*sorted((O/'evidence').glob('*.json')),*sorted((O/'evidence').glob('*.csv'))]
def hashes():return {str(p.relative_to(O)):hashlib.sha256(p.read_bytes()).hexdigest() for p in outputs}
before=hashes();subprocess.run(['python3',str(O/'build_bundle.py')],check=True,capture_output=True)
assert before==hashes(),'Generated artifacts changed on regeneration'
print(f'PASS: 7 required outputs; {len(src)} source IDs; {len(dims)} converted dimension rows; local links; SVG structure; 52-ID audit; byte-stable generation.')

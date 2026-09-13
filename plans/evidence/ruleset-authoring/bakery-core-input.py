"""Prepare honest current-Core capability probes; never substitutes jobs for present workers."""
import copy
from pathlib import Path
import tomllib
from author import compile_model,read,inline
root=Path(__file__).parent
text,_,_=compile_model(read(root/'baseline.toml'))
d=tomllib.loads(text)
d.pop('jobs') # Keep assignment disabled so declared posts cannot masquerade as present staff.
d['rule']=[r for r in d['rule'] if r['name']=='producer.food.produce']
d['rule'][0]['inputs'][0]['scope']='local'
d['rule'][0]['apply']={'min':1,'max':1}
for b in d['building']:
    if b['name']=='producer.food': b['bins'].append(dict(resource='produce',owner='business',capacity=32))
def dump(d):
    lines=['# Current-Core probe: manually supplied input, no assignment, no buyers; not a labour-aware bakery.']
    for name,value in d.items():
        entries=value if isinstance(value,list) else [value]
        for fields in entries:
            lines.append(f'[[{name}]]' if isinstance(value,list) else f'[{name}]')
            lines.extend(f'{k} = {inline(v)}' for k,v in fields.items())
    return '\n'.join(lines)+'\n'
out=root/'generated';out.mkdir(exist_ok=True)
(out/'bakery-core.toml').write_text(dump(d))
present=copy.deepcopy(d);present['rule'][0]['apply']={'derived':'present_workers'}
(out/'bakery-present-workers.toml').write_text(dump(present))
slots=copy.deepcopy(d)
next(b for b in slots['business'] if b['name']=='producer.food')['jobs']=2
(out/'bakery-worker-slots.toml').write_text(dump(slots))

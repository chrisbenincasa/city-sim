"""Agent checks and task variants. Does not edit catalogue.toml or complete user handoff tasks."""
import copy
import json
from pathlib import Path
from author import read, report, compile_model, source_text
root=Path(__file__).parent
out=root/'generated'; out.mkdir(exist_ok=True)
base=read(root/'baseline.toml')
original, resolved, counts=compile_model(base)
variants={'baseline':base}
def case(name):
    m=copy.deepcopy(base); variants[name]=m; return m
case('shared')['baskets']['basic']['use']['food']=48
case('variant')['variants']['reserve']['days']=10
m=case('add-good'); m['goods']['spice']=dict(label='Spice',price=100)
m['recipes']['spice']=dict(inputs=dict(produce=4),outputs=dict(spice=8))
m['baskets']['varied']['use']['spice']=8
case('remove-override')['kinds']['home_00']['overrides'].pop('reserve')
case('label')['kinds']['home_00']['label']='Courtyard house'
case('retire')['kinds'].pop('home_00')
# Identity changes are delete/add, not aliases.
m=case('change-id'); m['kinds']['courtyard']=m['kinds'].pop('home_00')
omitted=copy.deepcopy(base)
omitted['kinds']['home_01'].pop('overrides')
assert compile_model(omitted)[0]==original
results=[]
scale=[]
for count in [20,40,80,83]:
    model=copy.deepcopy(base)
    model['kinds']={f'home_{i:02}':copy.deepcopy(base['kinds'][f'home_{i%20:02}']) for i in range(count)}
    text, _, size=compile_model(model)
    (out/f'scale{count}.toml').write_text(text)
    scale.append(dict(authored_kinds=count,counts=size))
for name, model in variants.items():
    text, explanation, current, size=report(model,base)
    (out/(name+'.toml')).write_text(text)
    (out/(name+'.source.toml')).write_text(source_text(model))
    (out/(name+'.md')).write_text(explanation)
    changed={k for k in current.keys() & resolved.keys() if current[k]!=resolved[k]}
    if name=='shared':
        assert len(changed)==30
        assert all(current[k]['capacities']['food']==48*current[k]['days'] for k in changed)
        assert current['home_00.reserve']['days']==8
    if name=='variant':
        assert len(changed)==17
        assert all(k.endswith('.reserve') for k in changed)
        assert current['home_10.reserve']==resolved['home_10.reserve']
    if name=='add-good':
        assert len(changed)==30 and all(int(k[5:7])>=10 for k in changed)
    if name=='remove-override':
        assert changed=={'home_00.reserve'} and current['home_00.reserve']['days']==6
    if name=='label': assert text==original
    # Identical model with reversed declaration order must emit identical runtime bytes.
    reversed_model={group:dict(reversed(list(entries.items()))) for group,entries in model.items()}
    assert compile_model(reversed_model)[0]==text
    results.append(dict(task=name,changed_existing_consumers=len(changed),counts=size))
# Negative checks cover author mistakes that would otherwise silently break intended locality.
errors=[]
for name, mutate in [
    ('unknown-key',lambda m:m['variants']['reserve'].update(day=10)),
    ('unknown-basket',lambda m:m['kinds']['home_00'].update(basket='basci')),
    ('unknown-variant',lambda m:m['kinds']['home_00']['overrides'].update(resreve=10)),
    ('unrepresentable-use',lambda m:m['baskets']['basic']['use'].update(food=33)),
    ('unknown-recipe-good',lambda m:m['recipes']['food']['inputs'].update(typo=4)),
    ('nonpositive-storage',lambda m:m['variants']['reserve'].update(days=0)),
]:
    m=copy.deepcopy(base);mutate(m)
    try: compile_model(m)
    except ValueError as error: errors.append(dict(case=name,diagnostic=str(error)))
    else: raise AssertionError(name)
(root/'checks.json').write_text(json.dumps(dict(tasks=results,scaling=scale,diagnostics=errors),indent=2)+'\n')
print('Locality, exception preservation, order-independent expansion and six diagnostic checks passed.')

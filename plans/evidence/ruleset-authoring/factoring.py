"""Representation experiment, not a Core Ruleset or save format. No variant Cartesian table."""
import copy
import json
from pathlib import Path
from author import read, validate, compile_model

ROOT=Path(__file__).parent


def factor(model):
    validate(model)
    # References preserve authored identity, even when two definitions currently have equal values.
    return dict(baskets=copy.deepcopy(model['baskets']), storage=copy.deepcopy(model['variants']),
                kinds={key:dict(basket=k['basket'],overrides=copy.deepcopy(k.get('overrides',{})))
                       for key,k in model['kinds'].items()}, recipes=copy.deepcopy(model['recipes']))


def resolve(program, kind, storage):
    k=program['kinds'][kind]
    inherited=program['storage'][storage]['days']
    days=k['overrides'].get(storage,inherited)
    use=program['baskets'][k['basket']]['use']
    return dict(days=days,use=use,capacities={g:a*days for g,a in use.items()})


def consume(program, row):
    """One isolated consumption firing; compare exact amounts and blocking, not a whole Tick."""
    r=resolve(program,row['kind'],row['storage'])
    for good,daily in sorted(r['use'].items()):
        amount=daily//8
        if row['stock'].get(good,0)>=amount: row['stock'][good]-=amount


def check():
    base=read(ROOT/'baseline.toml'); measurements=[]
    for count,profiles in [(20,3),(83,3),(1000,3),(1000,30)]:
        m=copy.deepcopy(base)
        m['kinds']={f'home_{i:04}':copy.deepcopy(base['kinds'][f'home_{i%20:02}']) for i in range(count)}
        if profiles>3: m['variants'].update({f'extra_{i}':dict(days=i+1) for i in range(profiles-3)})
        p=factor(m)
        # No (kind,storage) definition array exists in the representation.
        assert len(p['kinds'])==count and len(p['storage'])==profiles
        pairs=0
        for kind,k in m['kinds'].items():
            for variant,v in m['variants'].items():
                expected_days=k.get('overrides',{}).get(variant,v['days'])
                r=resolve(p,kind,variant)
                assert r['capacities']=={g:a*expected_days for g,a in m['baskets'][k['basket']]['use'].items()}
                pairs+=1
        measurements.append(dict(kinds=count,storage_profiles=profiles,actual_pairs_checked=pairs,
            flat_kinds=count*profiles+len(m['recipes']),factored_kinds=count+len(m['recipes']),
            shared_rule_templates=2*sum(len(b['use']) for b in m['baskets'].values())+len(m['recipes']),
            exceptions=sum(len(k['overrides']) for k in p['kinds'].values())))
    # Compare against the existing expanded compiler, not only the factor's resolver.
    _,expanded,_=compile_model(base); p=factor(base)
    rows=[]; references=[]
    for identity,r in sorted(expanded.items()):
        kind,storage=identity.split('.')
        resolved=resolve(p,kind,storage)
        assert all(resolved[k]==r[k] for k in ['days','use','capacities'])
        row=dict(kind=kind,storage=storage,stock=dict(resolved['capacities']))
        rows.append(row);references.append(copy.deepcopy(row))
    # Saved selections remain separate from kinds. This JSON is research state, not CitySave.
    restored=json.loads(json.dumps(rows,sort_keys=True))
    for firing in range(100):
        for row,reference,loaded in zip(rows,references,restored):
            consume(p,row);consume(p,loaded)
            declared=expanded[reference['kind']+'.'+reference['storage']]
            for good,daily in declared['use'].items():
                amount=daily//8
                if reference['stock'][good]>=amount: reference['stock'][good]-=amount
        assert rows==references==restored
    # Shared content edits preserve selections and explicit exceptions; retired profiles refuse lookup.
    tuned=copy.deepcopy(p);tuned['storage']['reserve']['days']=10
    assert resolve(tuned,'home_00','reserve')['days']==8
    assert resolve(tuned,'home_01','reserve')['days']==10
    del tuned['storage']['reserve']
    try: resolve(tuned,'home_01','reserve')
    except KeyError: pass
    else: raise AssertionError('Retired live selection silently remapped')
    result=dict(scope='Definition lookup, isolated consumption and research-state roundtrip; not Core event/replay equivalence.',
                scaling=measurements,expanded_comparison_rows=len(rows),consumption_firings=100,
                checks=['all profile/kind capacity pairs agree','isolated consumption agrees with flat definitions',
                        'saved selections survive research roundtrip','override preserved','retired selection refuses'])
    (ROOT/'factoring-results.json').write_text(json.dumps(result,indent=2)+'\n')
    return result

if __name__=='__main__': print(json.dumps(check(),indent=2))

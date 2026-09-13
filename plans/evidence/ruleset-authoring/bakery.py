"""Finite bakery contract and conservation probe. Not a simulation engine or Core adapter."""
import copy
import json
from pathlib import Path
import tomllib
ROOT=Path(__file__).parent


def resolve(model,selection):
    allowed={'recipe':{'inputs','outputs'},'work':{'batches_per_present_worker_day','wage_per_worker_day'},
             'storage':{'input_days','output_days'},'supply':{'resource','unit_price','counterparty'},
             'sales':{'resource','unit_price','counterparty'},'business':{'recipe','work','storage','supply','sales'},
             'selection':{'business_floor_tiles','storage'}}
    if set(model)!=set(allowed)|{'capacity'} or set(model['capacity'])!={'floor_tiles_per_job'}:
        raise ValueError('Unexpected bakery contract sections/capacity fields')
    for section,entries in model.items():
        if section=='capacity': continue
        for name,fields in entries.items():
            required=allowed[section]-({'storage'} if section=='selection' else set())
            if not required <= fields.keys() <= allowed[section]: raise ValueError(f'{section}.{name}: unknown or missing field')
    business=model['business']['bakery']; selected=model['selection'][selection]
    recipe=model['recipe'][business['recipe']]; work=model['work'][business['work']]
    storage=model['storage'][selected.get('storage',business['storage'])]
    if type(model['capacity']['floor_tiles_per_job']) is not int or model['capacity']['floor_tiles_per_job']<=0:
        raise ValueError('floor_tiles_per_job must be a positive integer')
    slots=selected['business_floor_tiles']//model['capacity']['floor_tiles_per_job']
    batches=slots*work['batches_per_present_worker_day']
    supply=model['supply'][business['supply']]; sales=model['sales'][business['sales']]
    if supply['counterparty']!='local_seller' or sales['counterparty']!='local_household':
        raise ValueError('This contract models local counterparties only; Outside trade is not implemented here')
    if set(recipe['inputs'])!={supply['resource']} or set(recipe['outputs'])!={sales['resource']}:
        raise ValueError('This bounded bakery needs one purchased input and one sold output; no silent extra chain.')
    input_amount=next(iter(recipe['inputs'].values())); output_amount=next(iter(recipe['outputs'].values()))
    for value in [model['capacity']['floor_tiles_per_job'],selected['business_floor_tiles'],
                  work['batches_per_present_worker_day'],work['wage_per_worker_day'],
                  storage['input_days'],storage['output_days'],supply['unit_price'],sales['unit_price'],
                  input_amount,output_amount]:
        if type(value) is not int or value<=0: raise ValueError('Contract quantities must be positive integers')
    return dict(slots=slots,batches=batches,input_per_batch=input_amount,output_per_batch=output_amount,
                input_capacity=batches*input_amount*storage['input_days'],
                output_capacity=batches*output_amount*storage['output_days'],
                batches_per_worker=work['batches_per_present_worker_day'],wage=work['wage_per_worker_day'],
                input_price=supply['unit_price'],output_price=sales['unit_price'])


def day(config,state,present,demand):
    """A specified sequence for this contract only: buy, work, sell, pay. Stock has explicit owners."""
    if not 0<=present<=config['slots']: raise ValueError('Present workers exceed slots')
    before_money=sum(state[k] for k in ['cash','seller_cash','buyer_cash','worker_cash'])
    before_input=state['input']+state['seller_stock']
    before_output=state['output']+state['buyer_stock']
    bought=min(config['input_capacity']-state['input'],state['seller_stock'],state['cash']//config['input_price'])
    state['input']+=bought;state['seller_stock']-=bought
    state['cash']-=bought*config['input_price'];state['seller_cash']+=bought*config['input_price']
    batches=min(present*config['batches_per_worker'],state['input']//config['input_per_batch'],
                (config['output_capacity']-state['output'])//config['output_per_batch'])
    state['input']-=batches*config['input_per_batch'];state['output']+=batches*config['output_per_batch']
    sold=min(demand,state['output'],state['buyer_cash']//config['output_price'])
    state['output']-=sold;state['buyer_stock']+=sold
    state['cash']+=sold*config['output_price'];state['buyer_cash']-=sold*config['output_price']
    due=present*config['wage'];paid=min(due,state['cash'])
    state['cash']-=paid;state['worker_cash']+=paid
    assert before_money==sum(state[k] for k in ['cash','seller_cash','buyer_cash','worker_cash'])
    assert before_input==state['input']+state['seller_stock']+batches*config['input_per_batch']
    assert before_output+batches*config['output_per_batch']==state['output']+state['buyer_stock']
    assert 0<=state['input']<=config['input_capacity'] and 0<=state['output']<=config['output_capacity']
    return dict(bought=bought,batches=batches,produced=batches*config['output_per_batch'],sold=sold,paid=paid,shortfall=due-paid)


def check():
    m=tomllib.loads((ROOT/'bakery.toml').read_text())
    normal=resolve(m,'standard');large=resolve(m,'large')
    assert normal['input_capacity']==32 and normal['output_capacity']==32
    assert large['input_capacity']==128 and large['output_capacity']==192
    results=[]
    initial=dict(input=0,output=0,cash=512,seller_stock=10000,seller_cash=0,buyer_cash=10000,buyer_stock=0,worker_cash=0)
    for variant,config in [('standard',normal),('large',large)]:
        for event,workers,demand in [('staffed',config['slots'],64),('no_workers',0,64),('half_workers',config['slots']//2,64),('no_buyers',config['slots'],0)]:
            state=copy.deepcopy(initial); r=day(config,state,workers,demand)
            if event=='no_workers': assert r['produced']==0
            if event=='half_workers': assert r['batches']==config['batches']//2
            if event=='no_buyers':
                for _ in range(4): r=day(config,state,workers,0)
                assert r['produced']==0 and state['output']==config['output_capacity']
            results.append(dict(variant=variant,event=event,result=r))
    starved=copy.deepcopy(initial);starved['seller_stock']=0
    assert day(normal,starved,normal['slots'],64)['produced']==0
    poor=copy.deepcopy(initial);poor['cash']=0
    assert day(normal,poor,normal['slots'],64)['produced']==0
    # Combined maintenance is local: variant slots/storage choices change, recipe/work/supply/sales remain shared.
    changed=copy.deepcopy(m);changed['selection']['large']['business_floor_tiles']=18
    assert resolve(changed,'standard')==normal and changed['recipe']==m['recipe']
    assert resolve(changed,'large')['input_capacity']==192
    storage=copy.deepcopy(m);storage['storage']['reserve']['input_days']=5
    assert resolve(storage,'standard')==normal and resolve(storage,'large')['input_capacity']==160
    shared=copy.deepcopy(m);shared['recipe']['bread']['inputs']['produce']=6
    assert resolve(shared,'standard')['input_capacity']==48 and resolve(shared,'large')['input_capacity']==192
    # Research state roundtrip; current CitySave is intentionally not claimed to support this representation.
    a=copy.deepcopy(initial)
    day(normal,a,2,16);b=json.loads(json.dumps(a))
    for _ in range(10): assert day(normal,a,2,16)==day(normal,b,2,16) and a==b
    result=dict(scope='Isolated integer contract, not Core staffing/trips/events or a balance run.',
                standard=normal,large=large,cases=results,
                maintenance=['large staffing edit leaves standard and shared recipe unchanged',
                    'reserve storage edit leaves standard unchanged','shared recipe edit re-derives both capacities'],
                boundaries=['present workers unavailable as a production Readout',
                    'allocated business floor is a scenario input here; Core must supply actual geometry/tenancy',
                    'storage selection is not currently saved separately from kind',
                    'purchase/work/sales/pay sequence here is not Core phase order'])
    (ROOT/'bakery-results.json').write_text(json.dumps(result,indent=2)+'\n')
    return result

if __name__=='__main__': print(json.dumps(check(),indent=2))

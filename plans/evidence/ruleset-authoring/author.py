"""Disposable authoring experiment. Standard-library Python; no runtime schema changes."""
import argparse
import json
from pathlib import Path
import re
import tomllib

ROOT = Path(__file__).parent


def inline(value):
    if isinstance(value, dict):
        return '{ ' + ', '.join(f'{k if re.fullmatch(r"[A-Za-z0-9_-]+", k) else json.dumps(k)} = {inline(v)}' for k, v in sorted(value.items())) + ' }'
    if isinstance(value, list):
        return '[' + ', '.join(map(inline, value)) + ']'
    return json.dumps(value, ensure_ascii=False)


def source_text(model):
    lines = ['# Synthetic maintenance catalogue, not balanced gameplay. Edit this source, not generated Rules.']
    for group, entries in model.items():
        for key, fields in entries.items():
            lines += ['', f'[{group}.{key}]']
            lines += [f'{field} = {inline(value)}' for field, value in fields.items()]
    return '\n'.join(lines) + '\n'


def read(path):
    return tomllib.loads(Path(path).read_text())


def validate(m):
    schema = {'goods': {'label', 'price'}, 'baskets': {'use'}, 'variants': {'days'},
              'kinds': {'label', 'basket', 'overrides'}, 'recipes': {'inputs', 'outputs'}}
    if set(m) != set(schema):
        raise ValueError(f'Root requires exactly {sorted(schema)}')
    for group, entries in m.items():
        if not isinstance(entries, dict) or not entries:
            raise ValueError(f'{group}: requires named definitions')
        for key, fields in entries.items():
            location = f'{group}.{key}'
            if not re.fullmatch('[a-z][a-z0-9_]*', key):
                raise ValueError(f'{location}: ids use lowercase letters, digits and underscores')
            if not isinstance(fields, dict) or set(fields) != schema[group]:
                raise ValueError(f'{location}: expected keys {sorted(schema[group])}')
            if 'label' in fields and not isinstance(fields['label'], str):
                raise ValueError(f'{location}.label: expected text')
    def positive(value, path):
        if type(value) is not int or value <= 0:
            raise ValueError(f'{path}: expected positive integer')
    for key, good in m['goods'].items(): positive(good['price'], f'goods.{key}.price')
    for key, variant in m['variants'].items(): positive(variant['days'], f'variants.{key}.days')
    for key, basket in m['baskets'].items():
        if not isinstance(basket['use'], dict) or not basket['use']:
            raise ValueError(f'baskets.{key}.use: requires Good/daily-use pairs')
        for good, daily in basket['use'].items():
            if good not in m['goods']: raise ValueError(f'baskets.{key}.use.{good}: unknown Good')
            positive(daily, f'baskets.{key}.use.{good}')
            if daily % 8: raise ValueError(f'baskets.{key}.use.{good}: daily use must be divisible by 8 at the fixture cadence of 256 Ticks')
    for key, kind in m['kinds'].items():
        if kind['basket'] not in m['baskets']: raise ValueError(f'kinds.{key}.basket: unknown basket {kind["basket"]}')
        if not isinstance(kind['overrides'], dict): raise ValueError(f'kinds.{key}.overrides: expected variant/days pairs')
        for variant, days in kind['overrides'].items():
            if variant not in m['variants']: raise ValueError(f'kinds.{key}.overrides.{variant}: unknown variant')
            positive(days, f'kinds.{key}.overrides.{variant}')
    for key, recipe in m['recipes'].items():
        if not recipe['outputs']: raise ValueError(f'recipes.{key}.outputs: requires a sink for production')
        for side, terms in recipe.items():
            if not isinstance(terms, dict): raise ValueError(f'recipes.{key}.{side}: expected Good/amount pairs')
            for good, amount in terms.items():
                if good not in m['goods']: raise ValueError(f'recipes.{key}.{side}.{good}: unknown Good')
                positive(amount, f'recipes.{key}.{side}.{good}')


def compile_model(m):
    validate(m)
    shared = (ROOT / 'scenario.toml').read_text()
    lines = ['# Deterministic expanded research content; edit catalogue.toml.', shared,
             '[[resource]]\nname="money"\nfamily="money"']
    def add(table, fields):
        lines.append(f'[[{table}]]\n' + '\n'.join(f'{k} = {inline(v)}' for k, v in fields.items()))
    for good in sorted(m['goods']): add('resource', {'name': good, 'family': 'good'})
    resolved = {}
    for kind, fields in sorted(m['kinds'].items()):
        for variant, settings in sorted(m['variants'].items()):
            identity = kind + '.' + variant
            override = variant in fields['overrides']
            days = fields['overrides'].get(variant, settings['days'])
            basket = fields['basket']
            use = m['baskets'][basket]['use']
            bins = [dict(resource=g, owner='occupant', capacity=daily*days) for g, daily in sorted(use.items())]
            add('building', dict(name=identity, houses=True, bins=bins))
            resolved[identity] = dict(label=fields['label'], basket=basket, days=days,
                                     storage_source=f'kinds.{kind}.overrides.{variant}' if override else f'variants.{variant}.days',
                                     use_source=f'baskets.{basket}.use', use=use,
                                     capacities={g: daily*days for g, daily in use.items()}, override=override)
            for good, daily in sorted(use.items()):
                local = dict(scope='local', resource=good, amount=daily//8)
                add('rule', dict(name=identity+'.buy.'+good, kind=identity, rate=8,
                    apply=dict(min=1,max=4), inputs=[dict(local,scope='pool')], outputs=[local]))
                add('rule', dict(name=identity+'.consume.'+good, kind=identity, rate=256,
                    apply=dict(min=1,max=1), inputs=[local], outputs=[]))
    for recipe, fields in sorted(m['recipes'].items()):
        identity = 'producer.'+recipe
        add('business', dict(name=identity, wage_per_day=2048, pay_period_days=7,
                            shift_start_earliest_hour=8, shift_start_latest_hour=8))
        add('building', dict(name=identity, premises=True, business=identity,
                            bins=[dict(resource=g,owner='business',capacity=1024) for g in sorted(fields['outputs'])]+[dict(resource='money',owner='business')]))
        add('rule', dict(name=identity+'.produce',kind=identity,rate=8,apply=dict(min=1,max=4),
                        inputs=[dict(scope='pool',resource=g,amount=a) for g,a in sorted(fields['inputs'].items())],
                        outputs=[dict(scope='local',resource=g,amount=a) for g,a in sorted(fields['outputs'].items())]))
    for name, kind, zone in [('housing', next(iter(resolved)), 0), ('trade', 'producer.'+sorted(m['recipes'])[0], 1)]:
        add('zone_rule', dict(name=name,kind=kind,zone=zone,interval=32,revisit_ticks=2048))
    add('life_stage', dict(name='adult',duration_days=64,spread_days=0))
    for east,north in [(0,0)]: add('lattice',dict(origin_east_tiles=east,origin_north_tiles=north))
    add('hinterland', dict(edge='north',emigrant_balance_min=0,emigrant_balance_max=0,
                          prices=[dict(resource=g,price=f['price']) for g,f in sorted(m['goods'].items())]))
    text = '\n\n'.join(lines)+'\n'
    parsed = tomllib.loads(text)
    def references(value):
        if isinstance(value, dict):
            return sum((1 if key in {'resource', 'kind', 'business'} else 0) + references(item) for key,item in value.items())
        if isinstance(value, list): return sum(map(references, value))
        return 0
    counts = dict(consumer_kinds=len(resolved), goods=len(m['goods']), recipes=len(m['recipes']),
                  expanded_kinds=len(parsed['building']), expanded_rules=len(parsed['rule']),
                  explicit_overrides=sum(len(k['overrides']) for k in m['kinds'].values()),
                  basket_memberships=sum(len(b['use']) for b in m['baskets'].values()),
                  recipe_edges=sum(len(r['inputs'])+len(r['outputs']) for r in m['recipes'].values()),
                  runtime_references=references(parsed))
    return text, resolved, counts


def report(model, previous=None):
    text, resolved, counts = compile_model(model)
    oldtext, old, oldcounts = compile_model(previous) if previous is not None else ('',{}, {})
    changed = [key for key in sorted(resolved) if resolved[key] != old.get(key)]
    removed = sorted(old.keys()-resolved.keys())
    removed_producers = sorted(set(previous['recipes']) - set(model['recipes'])) if previous else []
    def quantities(values): return ', '.join(f'{k}={v}' for k,v in sorted(values.items()))
    def describe(r):
        if r is None: return 'absent'
        return f"use {quantities(r['use'])}/Day; {r['days']} Days; capacity {quantities(r['capacities'])}"
    lines = ['# Authoring preview', '', 'Static expectations; no saved city has been modified.', '',
             f'Counts: {counts}',
             f'Runtime kind budget: {counts["expanded_kinds"]}/254. '+('CANNOT LOAD: each variant expands to a kind; reduce the product or revise the runtime model.' if counts['expanded_kinds']>254 else 'Within the current loader limit; this is not a performance verdict.'), f'Previous counts: {oldcounts}',
             f'Changed/new consumer definitions: {len(changed)}; unchanged: {len(resolved)-len(changed)}.',
             f'Runtime bytes changed: {text != oldtext}. Labels do not define runtime identity.', '',
             'Retired consumer ids: '+(', '.join(removed) or 'none')+'. Occupied retired kinds become derelict under current migration.',
             'Removed Good ids: '+((', '.join(sorted(set(previous["goods"])-set(model["goods"]))) or 'none') if previous else 'none')+'. Stock migration requires explicit review.', '',
             'Retired producer ids: '+(', '.join('producer.'+k for k in removed_producers) or 'none')+'. Standing producers lose their kind.', '',
             'Recipe changes: '+(', '.join(k for k in sorted(model['recipes']) if previous is None or model['recipes'][k] != previous['recipes'].get(k)) or 'none'), '',
             'Fixed scenario: 256-Tick consumption; 8-Tick replenishment/production; fixed wages/geometry from the fixture.',
             'Pool replenishment here is a synthetic source path, not paid shopping. No balance claim.', '',
             '| Kind | Basket / daily use | Storage | Source of storage | Exception |',
             '|---|---|---|---|---|']
    for key in changed:
        r=resolved[key]
        lines.append(f'| {key} | {r["use_source"]}: {quantities(r["use"])} | {r["days"]} Days: {quantities(r["capacities"])} | {r["storage_source"]} | {"yes" if r["override"] else "no"} |')
    if previous is not None:
        def leaves(value, path=''):
            if isinstance(value, dict):
                return {p: v for k, item in value.items() for p, v in leaves(item, path+'.'+k).items()}
            return {path.lstrip('.'): value}
        before, after = leaves(previous), leaves(model)
        lines += ['', '## Authored changes', '', '| Source path | Before | After |', '|---|---|---|']
        for path in sorted(before.keys() | after.keys()):
            if before.get(path) != after.get(path):
                lines.append(f'| {path} | {before.get(path, "absent")} | {after.get(path, "absent")} |')
        lines += ['', '## Effective changes', '', '| Kind | Before | After |', '|---|---|---|']
        for key in changed:
            lines.append(f'| {key} | {describe(old.get(key))} | {describe(resolved[key])} |')
    return text, '\n'.join(lines)+'\n', resolved, counts


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('source', type=Path); p.add_argument('--against',type=Path)
    p.add_argument('--out',type=Path,default=ROOT/'generated'/'preview')
    args=p.parse_args()
    try: text, explanation, resolved, counts = report(read(args.source), read(args.against) if args.against else None)
    except (ValueError, TypeError, KeyError, tomllib.TOMLDecodeError) as error: p.exit(2, f'{args.source}: {error}\nNo preview written; any existing output belongs to a previous successful run.\n')
    args.out.mkdir(parents=True,exist_ok=True)
    (args.out/'ruleset.toml').write_text(text)
    (args.out/'report.md').write_text(explanation)
    (args.out/'resolved.json').write_text(json.dumps(resolved,indent=2,sort_keys=True)+'\n')
    print(f'{args.out}/report.md\n{counts}')
    if counts['expanded_kinds'] > 254:
        p.exit(2, 'Expanded content exceeds the runtime kind limit; see report.md.\n')

if __name__ == '__main__': main()

"""One-time fixture creation. Refuses to overwrite the author's working copy."""
from pathlib import Path
from author import source_text
root=Path(__file__).parent
names=['food','produce','clothes','fuel','tools','soap','furniture','books']
m=dict(goods={g:dict(label=g.title(),price=100) for g in names},
       baskets=dict(basic=dict(use=dict(food=32,fuel=16)), varied=dict(use=dict(food=32,clothes=8,soap=8,books=8))),
       variants=dict(compact=dict(days=2),standard=dict(days=4),reserve=dict(days=6)),
       kinds={f'home_{i:02}':dict(label=f'Home {i:02}',basket='basic' if i<10 else 'varied',overrides={'reserve':8} if i in [0,10,19] else {}) for i in range(20)},
       recipes={g:dict(inputs={} if g!='food' else dict(produce=4),outputs={g:8}) for g in names})
for filename in ['baseline.toml','catalogue.toml']:
    path=root/filename
    if path.exists(): raise SystemExit(f'Refusing to overwrite {path}')
    path.write_text(source_text(m))

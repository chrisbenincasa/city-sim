"""Contemporary and mid-century forms for one house on a narrow detached Lot, 8-11 m wide. The forms are
the wide house forms from wide-house-forms.py drawn as a single house; the traditional forms come from
house-forms.py, at its fixed 8 x 12 m.

blender --background --factory-startup --python-exit-code 1 --python scripts/art/narrow-house-periods.py -- REPO OUT [job...]

Jobs: sheet-<form> for each contemporary and mid-century form, street-now (the shipped mix, all
traditional) and street-mixed (the target period shares).
"""
import importlib.util
import math
import random
import sys
from pathlib import Path

ARGS = sys.argv[sys.argv.index('--') + 1:]
REPO, OUT, ONLY = Path(ARGS[0]), Path(ARGS[1]), ARGS[2:]

spec = importlib.util.spec_from_file_location('whf', REPO / 'scripts/art/wide-house-forms.py')
whf = importlib.util.module_from_spec(spec)
_argv = sys.argv
sys.argv = _argv[:_argv.index('--') + 3] + ['(none)']
spec.loader.exec_module(whf)
sys.argv = _argv
hfm, hf, ts = whf.hfm, whf.hf, whf.ts

NEW = whf.NEW
PERIOD = whf.PERIOD
TRADITIONAL = hfm.ROOFS + hfm.SHAPES

# Preset weights of the shipped narrow forms, house-detached-<form> in appearance/test-street/preset.toml.
SHIPPED = {'side-gable': 3, 'front-gable': 3, 'hipped': 2, 'half-hip': 1, 'cross-gable': 2, 'saltbox': 1,
           'gambrel': 1, 'l-plan': 1, 'rear-outshot': 1, 'side-wing': 1}


def narrow(form, seed, W, D, storeys, cx, cy, turned):
    s = whf.look(seed, W, D, storeys, form)
    whf.place(s, cx, cy, turned)
    return s


def traditional(name, build, seed, cx, cy, turned, storeys=2):
    s = hfm.look(seed, storeys)
    start = len(ts.parts)
    kw = {'pitch': random.Random(seed + 9).uniform(*hfm.RANGES[name])} if name in hfm.RANGES else {}
    if name == 'side-wing':
        kw['side'] = random.Random(seed).choice([-1, 1])
    hfm.finish(s, build(s, **kw))
    for part in ts.parts[start:]:
        if turned:
            part.rotation_euler[2] = math.pi
        part.location = (cx, cy, 0)


def sheet(form, seed=501):
    """The form at 8 x 12 m and two storeys, 10 x 14 m and two, 11 x 16 m and three."""
    hf.fresh()
    camera = hf.stage(road_y=(-205.0, -195.0))
    for k, (W, D, st, cx) in enumerate(((8, 12, 2, -16), (10, 14, 2, 0), (11, 16, 3, 17))):
        narrow(form, seed + k, W, D, st, cx, D / 2 - 6, False)
    whf.finish(camera, [(f'sheet-{form}', (0, -62, 27), (1, 2, 4))])


def street(name, pick, seed):
    """Seven houses to a side, each on its own 14 m plot."""
    hf.fresh()
    camera = hf.stage()
    r = random.Random(seed)
    last = None
    for side, turned in ((1, False), (-1, True)):
        for i in range(7):
            cx = (i - 3) * 14.0
            x = cx if side > 0 else -cx
            form = pick(r, last)
            last = form
            if form in NEW:
                W, D = r.choice([8, 9, 10, 11]), r.choice([10, 12, 14])
                narrow(form, r.randrange(1 << 30), W, D, r.choice([2, 2, 3]), x, side * (10 + D / 2), turned)
            else:
                build = dict(TRADITIONAL)[form]
                traditional(form, build, r.randrange(1 << 30), x, side * (10 + 6), turned)
            print('STREET', name, side, i, form, PERIOD.get(form, 'traditional'))
    whf.finish(camera, [(f'{name}-game', (0, -95, 80), (0, 6, 0)),
                        (f'{name}-level', (-50, -2, 4), (-10, 14, 6))])


def shipped(r, last):
    while True:
        form = r.choices(list(SHIPPED), weights=list(SHIPPED.values()))[0]
        if form != last:
            return form


def mixed(r, last):
    pools = [(.55, [f for f in NEW if PERIOD[f] == 'contemporary']),
             (.25, [f for f in NEW if PERIOD[f] == 'mid-century']),
             (.20, list(SHIPPED))]
    while True:
        form = r.choice(r.choices([p for _, p in pools], weights=[w for w, _ in pools])[0])
        if form != last:
            return form


JOBS = {f'sheet-{f}': (lambda f=f: sheet(f)) for f in NEW}
JOBS['street-now'] = lambda: street('street-now', shipped, 83)
JOBS['street-mixed'] = lambda: street('street-mixed', mixed, 83)

for name, job in JOBS.items():
    if not ONLY or name in ONLY:
        job()
        print('DONE', name)

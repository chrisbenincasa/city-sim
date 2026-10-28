"""Wide house forms: a detached Building of several houses on a 12-84 m Lot, varied by roof and plan.
The walls, openings and dressing come from house-families.py and the roof helpers from house-forms.py.
Pitch ranges stop at 45 degrees. The deep plan is a double-pile row with rear outriggers, for Lots
32-36 m deep, and draws within the 20% floor allowance (docs/07-the-drawing.md).

src/Borough.Appearance/FamilyBodyBuilder.Wide.cs is the reference for the contemporary and mid-century
forms; this script drew their options. The traditional forms here are candidates that did not ship.

blender --background --factory-startup --python-exit-code 1 --python scripts/art/wide-house-forms.py -- REPO OUT [job...]

Jobs: sheet-<form> for each form, deep-<roof> for each deep-plan roof, street, and mixed, a street drawn at the
target period shares.
"""
import importlib.util
import math
import random
import sys
from pathlib import Path

ARGS = sys.argv[sys.argv.index('--') + 1:]
REPO, OUT, ONLY = Path(ARGS[0]), Path(ARGS[1]), ARGS[2:]

# house-forms.py runs the jobs named on the command line when imported, so it is handed none of its own.
spec = importlib.util.spec_from_file_location('hfm', REPO / 'scripts/art/house-forms.py')
hfm = importlib.util.module_from_spec(spec)
_argv = sys.argv
sys.argv = _argv[:_argv.index('--') + 3] + ['(none)']
spec.loader.exec_module(hfm)
sys.argv = _argv
hf, ts, S = hfm.hf, hfm.ts, hfm.S
poly, hipgable = hfm.poly, hfm.hipgable
import bpy

PITCH = {'hipped-pair': (24, 34), 'double-fronted': (28, 40), 'gabled-wings': (32, 42), 'gambrel': (0, 0),
         'mansard': (0, 0), 'cross-gables': (30, 40), 'catslide': (30, 38), 'terrace': (22, 40), 'gables': (30, 45),
         'villa': (24, 34), 'deep-m-gable': (30, 40), 'deep-hipped': (24, 34), 'deep-gabled-front': (34, 45), 'deep-cross-gables': (30, 38)}


def look(seed, W, D, storeys, form):
    if form in PERIOD:
        return new_look(seed, W, D, storeys, form)
    s = hf.draw(seed, W, D, storeys, 'villa' if form == 'villa' else 'terrace')
    low, high = PITCH[form]
    s['pitch'] = random.Random(seed + 9).uniform(low, high) if high else 0
    s['form'] = form
    hf.bind(s)
    return s


def module(s, i, w, mirror=False, bay=False):
    m = s['mods'][i % len(s['mods'])]
    return dict(m, w=w, mirror=mirror, bay=bay)


def row(s, mods, x0, y0, y1, storeys, front=True, back=True):
    """The street and garden walls of a row of houses, left to right from x0. Fronts face -Y."""
    H, keep = storeys * S, s['storeys']
    s['storeys'] = storeys
    x, doors = x0, []
    for m in mods:
        w = m['w']
        ts.materials['door'] = ts.materials[m['door']]
        if front:
            face = ts.Face((x, y0, 0), (1, 0, 0), (0, -1, 0))
            openings = hf.house_front(m, s)
            hf.elevation('front', face, w, H, openings, s, m['wall'])
            door = openings[0]
            hf.canopy(face, door[0], door[2], s, door[1] + door[3])
            doors.append(x + door[0] + .5)
            if m['bay']:
                u = w * .62 - 1.3 if not m['mirror'] else w - (w * .62 + 1.3)
                hf.bay_window(face, u, s, s['ground'] or m['wall'])
        if back:
            hf.elevation('back', ts.Face((x + w, y1, 0), (-1, 0, 0), (0, 1, 0)), w, H, hf.house_back(m, s), s, m['wall'])
        x += w
    s['storeys'] = keep
    return doors


def end_walls(s, x0, x1, y0, y1, storeys, sides=(-1, 1)):
    d, H, keep = y1 - y0, storeys * S, s['storeys']
    s['storeys'] = storeys
    windows = hf.ends(d, s) if d >= 3.5 else []
    if -1 in sides:
        hf.elevation('left end', ts.Face((x0, y1, 0), (0, -1, 0), (-1, 0, 0)), d, H, windows, s, s['end'])
    if 1 in sides:
        hf.elevation('right end', ts.Face((x1, y0, 0), (0, 1, 0), (1, 0, 0)), d, H, windows, s, s['end'])
    s['storeys'] = keep


def stack(s, x, y, low, top):
    ts.box('chimney', (x - .45, y - .45, low), (x + .45, y + .45, top + 1.1), s['ground'] or s['end'])
    hf.boxes('pots', [((x + dx - .1, y - .1, top + 1.1), (x + dx + .1, y + .1, top + 1.45)) for dx in (-.2, .2)], hf.flat('9a5a3a'))


def dormer(s, cx, y, z, wall):
    """A gabled dormer whose face stands at y on a roof slope rising toward +Y."""
    ts.box('dormer', (cx - .85, y, z), (cx + .85, y + 2.6, z + 1.75), wall)
    ts.box('dormer glass', (cx - .5, y - .03, z + .4), (cx + .5, y, z + 1.45), 'glass')
    ts.box('dormer bar', (cx - .03, y - .05, z + .4), (cx + .03, y, z + 1.45), s['frame'])
    hf.prism_y('dormer roof', [(cx - 1.05, z + 1.65), (cx, z + 2.35), (cx + 1.05, z + 1.65),
                               (cx + 1.05, z + 1.8), (cx, z + 2.5), (cx - 1.05, z + 1.8)], y - .25, y + 2.6, s['roof'])


def houses(W, width):
    n = max(2, round(W / width)) if W >= 12 else 1
    return n, W / n


# ---------------------------------------------------------------------------------------------
# The forms. Each takes a look and returns the x of its street doors.

def hipped_pair(s):
    """Semi-detached pairs, mirrored about a shared stack, each pair under its own hipped roof."""
    W, D, st = s['W'], s['D'], s['storeys']
    H = st * S
    n, w = houses(W, random.Random(s['seed']).uniform(6.0, 8.0))
    mods = [module(s, i, w, mirror=i % 2 == 1, bay=s['mods'][i % len(s['mods'])]['bay']) for i in range(n)]
    doors = row(s, mods, -W / 2, -D / 2, D / 2, st)
    end_walls(s, -W / 2, W / 2, -D / 2, D / 2, st)
    ts.plinth(W, D)
    groups, i = [], 0
    while i < n:
        size = 3 if n - i == 3 else min(2, n - i)
        groups.append((i, size))
        i += size
    for first, size in groups:
        x0 = -W / 2 + first * w
        gw = size * w
        top = hipgable(s, 'x', x0 + gw / 2, 0, D / 2, gw / 2, H, s['pitch'], k=1)
        for k in range(1, size):
            stack(s, x0 + k * w, 0, top - 1.5, top)
    return doors


def double_fronted(s):
    """Wide symmetrical houses with a central door, a stack at each party wall, under one side gable."""
    W, D, st = s['W'], s['D'], s['storeys']
    H, wh = st * S, hf.window_height()
    n = max(1, round(W / random.Random(s['seed']).uniform(10.0, 13.0)))
    w = W / n
    doors = []
    for i in range(n):
        x = -W / 2 + i * w
        m = module(s, i, w)
        ts.materials['door'] = ts.materials[m['door']]
        ww = s['win_w'] * 1.15
        openings = [(w / 2 - .55, .3, 1.1, min(2.75, S - .75), 'door')]
        openings += [(w * c - ww / 2, .75, ww, wh, 'window') for c in (.2, .8)]
        openings += [(w * c - ww / 2, k * S + .75, ww, wh, 'window') for k in range(1, st) for c in (.2, .5, .8)]
        face = ts.Face((x, -D / 2, 0), (1, 0, 0), (0, -1, 0))
        hf.elevation('front', face, w, H, openings, s, m['wall'])
        hf.canopy(face, w / 2 - .55, 1.1, s, .3 + min(2.75, S - .75))
        doors.append(x + w / 2)
        back = [(w * c - s['win_w'] / 2, k * S + .75, s['win_w'], wh, 'window') for k in range(st) for c in (.2, .5, .8)]
        hf.elevation('back', ts.Face((x + w, D / 2, 0), (-1, 0, 0), (0, 1, 0)), w, H, back, s, m['wall'])
    end_walls(s, -W / 2, W / 2, -D / 2, D / 2, st)
    ts.plinth(W, D)
    top = hipgable(s, 'x', 0, 0, D / 2, W / 2, H, s['pitch'])
    for i in range(n + 1):
        x = -W / 2 + i * w
        x = min(max(x, -W / 2 + .5), W / 2 - .5)
        stack(s, x, 0, top - 1.5, top)
    return doors


def gabled_wings(s):
    """A side-gabled range set back between full-height front-gabled wings at its ends."""
    W, D, st = s['W'], s['D'], s['storeys']
    H = st * S
    r = random.Random(s['seed'] + 5)
    wing, proj = min(6.0, max(4.0, W * .2)), 1.8
    sides = [-1, 1] if W >= 24 else [r.choice([-1, 1])]
    y_main = -D / 2 + proj
    left = -W / 2 + (wing if -1 in sides else 0)
    right = W / 2 - (wing if 1 in sides else 0)
    n, w = houses(right - left, 7.0)
    if right - left < 12:
        n, w = 1, right - left
    doors = row(s, [module(s, i, w, mirror=i % 2 == 1) for i in range(n)], left, y_main, D / 2, st, back=False)
    bn, bw = houses(W, 7.0)
    row(s, [module(s, i, bw) for i in range(bn)], -W / 2, y_main, D / 2, st, front=False)
    end_walls(s, -W / 2, W / 2, y_main, D / 2, st)
    wing_depth = min(D * .55, proj + 5.0)
    pw = min(45.0, s['pitch'] + 6)
    for side in sides:
        x0, x1 = (-W / 2, -W / 2 + wing) if side < 0 else (W / 2 - wing, W / 2)
        doors += hfm.block(s, x0, x1, -D / 2, y_main + .01, st, back=False)
        hipgable(s, 'y', (x0 + x1) / 2, -D / 2 + wing_depth / 2, wing / 2, wing_depth / 2, H, pw, walls=(-1,))
        ts.box('wing glass', ((x0 + x1) / 2 - .45, -D / 2 - .03, H + .5), ((x0 + x1) / 2 + .45, -D / 2, H + .5 + min(1.3, wing / 2 * math.tan(math.radians(pw)) * .4)), 'glass')
    ts.plinth(W, D)
    A = (D / 2 - y_main) / 2
    top = hipgable(s, 'x', 0, y_main + A, A, W / 2, H, s['pitch'])
    t = math.tan(math.radians(s['pitch']))
    for i in range(n):
        cx = left + (i + .5) * w
        if i % 2 == 0 and right - left >= 8:
            dormer(s, cx, y_main + 1.4, H + (1.4 - .35) * t - .2, s['mods'][i % len(s['mods'])]['wall'])
    for i in range(1, bn):
        stack(s, -W / 2 + i * bw, y_main + A, top - 1.5, top)
    return doors


def gambrel(s):
    """A barn roof along the street over one storey fewer of wall, lit by a long shed dormer."""
    W, D, st = s['W'], s['D'], s['storeys']
    ws = max(1, st - 1)
    Hw = ws * S
    n, w = houses(W, 7.0)
    mods = [module(s, i, w, mirror=i % 2 == 1) for i in range(n)]
    doors = row(s, mods, -W / 2, -D / 2, D / 2, ws)
    end_walls(s, -W / 2, W / 2, -D / 2, D / 2, ws)
    ts.plinth(W, D)
    X, A = W / 2, D / 2
    lower, upper = math.tan(math.radians(66)), math.tan(math.radians(24))
    run, e = 1.6, .45
    ak = A - run
    zk = Hw + run * lower
    top = zk + ak * upper
    low = Hw - e * .55
    Le = X + .25
    for side in (-1, 1):
        poly('gambrel lower', [(-Le, side * (A + e), low), (Le, side * (A + e), low), (Le, side * ak, zk), (-Le, side * ak, zk)], 'roof')
        poly('gambrel upper', [(-Le, side * ak, zk), (Le, side * ak, zk), (Le, 0, top), (-Le, 0, top)], 'roof')
    for f in (-1, 1):
        poly('gambrel end', [(f * X, -A, Hw), (f * X, A, Hw), (f * X, ak, zk), (f * X, 0, top), (f * X, -ak, zk)], 'wall')
    dx, front = X - 1.2, -(A - .35)
    dz = zk + .2
    ts.box('shed dormer', (-dx, front, Hw), (dx, -1.2, dz), s['end'])
    poly('dormer roof', [(-dx - .25, front - .4, dz + .05), (dx + .25, front - .4, dz + .05), (dx + .25, -1.0, top - .9), (-dx - .25, -1.0, top - .9)], 'roof')
    for i in range(n):
        for c in ((.3, .7) if w >= 6.5 else (.5,)):
            wx = -W / 2 + i * w + c * w
            if abs(wx) < dx - .8:
                ts.box('dormer frame', (wx - .72, front - .06, Hw + .78), (wx + .72, front, Hw + 2.42), s['trim'])
                ts.box('dormer window', (wx - .6, front - .09, Hw + .9), (wx + .6, front - .06, Hw + 2.3), 'glass')
    for i in range(1, n, 2):
        stack(s, -W / 2 + i * w, ak * .3, top - 1.5, top)
    return doors


def mansard(s):
    """A slate mansard on all four sides over one storey fewer of wall, a dormer to each bay."""
    W, D, st = s['W'], s['D'], s['storeys']
    ws = max(1, st - 1)
    Hw = ws * S
    n, w = houses(W, 7.0)
    mods = [module(s, i, w, mirror=i % 2 == 1) for i in range(n)]
    doors = row(s, mods, -W / 2, -D / 2, D / 2, ws)
    end_walls(s, -W / 2, W / 2, -D / 2, D / 2, ws)
    ts.plinth(W, D)
    X, Y, e, inset, rise = W / 2, D / 2, .25, 1.1, 3.1
    zk = Hw + rise
    outer = [(-X - e, -Y - e), (X + e, -Y - e), (X + e, Y + e), (-X - e, Y + e)]
    inner = [(-X + inset, -Y + inset), (X - inset, -Y + inset), (X - inset, Y - inset), (-X + inset, Y - inset)]
    for i in range(4):
        j = (i + 1) % 4
        poly('mansard', [(*outer[i], Hw - .1), (*outer[j], Hw - .1), (*inner[j], zk), (*inner[i], zk)], 'roof')
    ts.box('cornice', (-X - e - .1, -Y - e - .1, Hw - .3), (X + e + .1, Y + e + .1, Hw), s['trim'])
    hipgable(s, 'x', 0, 0, Y - inset, X - inset, zk, 14, k=1, eaves=0)
    lean = inset / rise
    for i in range(n):
        for c in ((.3, .7) if w >= 6.5 else (.5,)):
            cx = -W / 2 + (i + c) * w
            y = -Y + lean * .4 - .25
            ts.box('dormer', (cx - .7, y, Hw + .4), (cx + .7, y + 1.4, Hw + 2.5), s['end'])
            ts.box('dormer glass', (cx - .45, y - .03, Hw + .7), (cx + .45, y, Hw + 2.1), 'glass')
            hf.prism_y('dormer head', [(cx - .85, Hw + 2.45), (cx, Hw + 2.85), (cx + .85, Hw + 2.45), (cx + .85, Hw + 2.6),
                                       (cx, Hw + 3.0), (cx - .85, Hw + 2.6)], y - .15, y + 1.4, s['trim'])
    for i in range(1, n, 2):
        stack(s, -W / 2 + i * w, 0, zk - .5, zk + .9)
    return doors


def cross_gables(s):
    """A side-gabled row with a front gable over every other house."""
    W, D, st = s['W'], s['D'], s['storeys']
    H = st * S
    n, w = houses(W, 7.0)
    mods = [module(s, i, w, mirror=i % 2 == 1) for i in range(n)]
    doors = row(s, mods, -W / 2, -D / 2, D / 2, st)
    end_walls(s, -W / 2, W / 2, -D / 2, D / 2, st)
    ts.plinth(W, D)
    top = hipgable(s, 'x', 0, 0, D / 2, W / 2, H, s['pitch'])
    pg = min(45.0, s['pitch'] + 8)
    start = random.Random(s['seed'] + 7).choice([0, 1]) if n > 2 else 0
    for i in range(start, n, 2):
        cx = -W / 2 + (i + .5) * w
        a = min(w * .38, 3.2)
        hipgable(s, 'y', cx, -D / 4, a, D / 4, H, pg, walls=(-1,))
        ts.box('gable glass', (cx - .45, -D / 2 - .03, H + .5), (cx + .45, -D / 2, H + .5 + min(1.4, a * math.tan(math.radians(pg)) * .4)), 'glass')
    for i in range(1, n):
        stack(s, -W / 2 + i * w, 0, top - 1.5, top)
    return doors


def catslide(s):
    """Full height to the street, the rear slope sweeping down to one storey, a dormer to each house."""
    W, D, st = s['W'], s['D'], s['storeys']
    H = st * S
    n, w = houses(W, 7.0)
    mods = [module(s, i, w, mirror=i % 2 == 1) for i in range(n)]
    doors = row(s, mods, -W / 2, -D / 2, D / 2, st, back=False)
    row(s, mods, -W / 2, -D / 2, D / 2, 1, front=False)
    X, Y, E = W / 2, D / 2, hfm.E
    t = math.tan(math.radians(s['pitch']))
    ar = -.25 * Y
    top = H + (Y + ar) * t
    zr = S + .5
    rear = (top - zr) / (Y - ar)
    Le = X + .3
    poly('front slope', [(-Le, -Y - E, H - E * t), (Le, -Y - E, H - E * t), (Le, ar, top), (-Le, ar, top)], 'roof')
    poly('rear slope', [(-Le, ar, top), (Le, ar, top), (Le, Y + E, zr - E * rear), (-Le, Y + E, zr - E * rear)], 'roof')
    for f in (-1, 1):
        poly('catslide end', [(f * X, -Y, 0), (f * X, Y, 0), (f * X, Y, zr), (f * X, ar, top), (f * X, -Y, H)], 'wall')
    ts.plinth(W, D)
    for i in range(n):
        cx = -W / 2 + (i + .5) * w
        dormer(s, cx, -Y + 1.2, H + 1.2 * t - .2, mods[i]['wall'])
    for i in range(1, n):
        stack(s, -W / 2 + i * w, ar, top - 1.5, top)
    return doors


def deep(s, roof):
    """A double-pile row: a front and a rear range under parallel roofs, with paired two-storey
    outriggers running back to the end of the Lot."""
    W, D, st = s['W'], s['D'], s['storeys']
    H = st * S
    n, w = houses(W, 6.5)
    df = min(13.0, max(8.0, D * .37))
    yb = -D / 2 + 2 * df
    mods = [module(s, i, w, mirror=i % 2 == 1) for i in range(n)]
    doors = row(s, mods, -W / 2, -D / 2, yb, st)
    end_walls(s, -W / 2, W / 2, -D / 2, yb, st)
    ts.box('plinth', (-W / 2 - .05, -D / 2 - .05, 0), (W / 2 + .05, yb + .05, .3), 'plinth')
    os_ = max(1, st - 1)
    Ho = os_ * S
    covered = W * 2 * df
    if D / 2 - yb > 1.5:
        wings = [(k * w - w / 2, k * w + w / 2) for k in range(1, n, 2)]
        if n % 2:
            wings.append((n * w - w / 2, n * w))
        for a, b in wings:
            x0, x1 = -W / 2 + a, -W / 2 + b
            hfm.block(s, x0, x1, yb - .01, D / 2, os_, front=False)
            hipgable(s, 'y', (x0 + x1) / 2, (yb + D / 2) / 2, (x1 - x0) / 2, (D / 2 - yb) / 2, Ho, 38, walls=(1,))
            covered += (x1 - x0) * (D / 2 - yb) * os_ / st
    cf, cr = -D / 2 + df / 2, -D / 2 + 1.5 * df
    if roof == 'deep-m-gable':
        top = hipgable(s, 'x', 0, cf, df / 2, W / 2, H, s['pitch'])
        hipgable(s, 'x', 0, cr, df / 2, W / 2, H, s['pitch'])
    elif roof == 'deep-hipped':
        top = hipgable(s, 'x', 0, cf, df / 2, W / 2, H, s['pitch'], k=1)
        hipgable(s, 'x', 0, cr, df / 2, W / 2, H, s['pitch'], k=1)
    elif roof == 'deep-cross-gables':
        top = hipgable(s, 'x', 0, cf, df / 2, W / 2, H, s['pitch'])
        hipgable(s, 'x', 0, cr, df / 2, W / 2, H, s['pitch'])
        pg = min(45.0, s['pitch'] + 8)
        for i in range(0, n, 2):
            cx = -W / 2 + (i + .5) * w
            a = min(w * .38, 3.2)
            hipgable(s, 'y', cx, -D / 2 + df / 4, a, df / 4, H, pg, walls=(-1,))
            ts.box('gable glass', (cx - .45, -D / 2 - .03, H + .5), (cx + .45, -D / 2, H + .5 + min(1.4, a * math.tan(math.radians(pg)) * .4)), 'glass')
    else:
        top = hipgable(s, 'x', 0, cr, df / 2, W / 2, H, s['pitch'] - 8)
        for i in range(n):
            cx = -W / 2 + (i + .5) * w
            hipgable(s, 'y', cx, cf, w / 2, df / 2, H, s['pitch'], walls=(-1,))
            ts.box('gable glass', (cx - .45, -D / 2 - .03, H + .5), (cx + .45, -D / 2, H + 1.8), 'glass')
    for i in range(1, n, 2):
        stack(s, -W / 2 + i * w, cf, top - 1.5, top)
    print('FLOOR', roof, W, D, st, f'{covered / (W * D):.2f}')
    return doors


# ---------------------------------------------------------------------------------------------
# Contemporary and mid-century forms. Flat colors stand in for standing-seam metal and fiber-cement panels.

for _name, _metres, _paint in (('sheet-metal-004', [2.0, 2.0], True), ('wood-siding-011', [1.4, 1.4], False),
                               ('hinoki-planks', [1.89, 1.89], False), ('wood-siding-005', [2.4, 2.4], False),
                               ('bitumen', [20, 20], False)):
    ts.SURFACES[_name] = {'metres': _metres, 'mean': .7 if _paint else None,
                          'maps': {k: hf.LIB / f'{_name}-{k}.jpg' for k in ('albedo', 'normal', 'roughness')
                                   if (hf.LIB / f'{_name}-{k}.jpg').exists()}}

PERIOD = {'staggered-gables': 'contemporary', 'flat-setback': 'contemporary', 'sawtooth': 'contemporary', 'pavilions': 'contemporary',
          'span': 'mid-century', 'butterfly': 'mid-century', 'monopitch': 'mid-century', 'low-hip': 'mid-century',
          'deep-flat': 'contemporary', 'deep-gables': 'contemporary', 'deep-sawtooth': 'contemporary', 'deep-span': 'mid-century'}
PITCH.update({'staggered-gables': (38, 45), 'flat-setback': (0, 0), 'sawtooth': (14, 22), 'pavilions': (38, 45),
              'span': (12, 16), 'butterfly': (8, 14), 'monopitch': (8, 12), 'low-hip': (18, 24),
              'deep-flat': (0, 0), 'deep-gables': (38, 45), 'deep-sawtooth': (14, 22), 'deep-span': (12, 16)})
METAL = ['34383b', '4a4f53', '6e7377']
PANEL = ['3a3e41', '5b6064', '8a8d86', 'c8c3b8']
WHITE = ['d8d6d0', 'd6d2c8', 'cfcac0']
C_DOORS = ['c9a227', '2f4a5c', 'b4472f', '2b2e31', '6b8f71', 'd9d4c8']
M_DOORS = ['c45a3b', 'd8c27a', '2f4a5c', '6f8f7a', 'eeeeea', '2b2e31']
M_PANELS = ['a9c3cf', 'd8c27a', 'c45a3b', 'eeeeea', '6f8f7a']


def new_look(seed, W, D, storeys, form):
    r = random.Random(seed)
    s = {'seed': seed, 'W': W, 'D': D, 'storeys': storeys, 'H': storeys * S, 'form': form, 'period': PERIOD[form]}
    low, high = PITCH[form]
    s['pitch'] = random.Random(seed + 9).uniform(low, high) if high else 0
    brick = hf.textured(r.choice(['bricks-051', 'bricks-066', 'bricks-085']))
    if s['period'] == 'contemporary':
        s['roof'] = hf.flat(r.choice(METAL), .45)
        timber = hf.textured(r.choice(['wood-siding-011', 'hinoki-planks']))
        render = hf.textured('painted-plaster-wall', r.choice(WHITE))
        panel = hf.flat(r.choice(PANEL), .7)
        ground, upper = r.choice([(brick, brick), (render, render), (timber, timber), (brick, timber), (render, panel), (brick, panel)])
        s['frame'] = hf.flat(r.choice(['2b2e31', '2b2e31', 'eeeeea', '6b5a45']), .5)
        s['trim'] = hf.flat('3a3d40', .5)
        s['doors'] = [hf.flat(c, .5) for c in r.sample(C_DOORS, 3)]
        s['eaves'] = .05
        s['accent'] = timber if upper != timber else panel
        s['panels'] = False
    else:
        s['roof'] = hf.textured(r.choice(['roofing-tiles-007', 'roofing-tiles-008']))
        render = hf.textured('painted-plaster-wall', 'd8d6d0')
        ground = upper = brick if r.random() < .75 else render
        s['accent'] = r.choice([hf.textured('clay-roof-tiles-02'), hf.textured('wood-siding-005'), hf.flat(r.choice(M_PANELS), .7)])
        s['frame'] = hf.flat(r.choice(['eeeeea', 'eeeeea', '2b2e31']), .5)
        s['trim'] = hf.flat('eeeeea')
        s['doors'] = [hf.flat(c, .6) for c in r.sample(M_DOORS, 3)]
        s['eaves'] = .45
        s['panels'] = True
    s['fin'] = brick
    s['ground'], s['upper'], s['end'] = ground, upper, upper
    s['flat'] = hf.textured('bitumen')
    s['variant'] = r.choice(['wide', 'pair', 'full'])
    s['boundary'] = r.choice(['hedge', 'brick', 'open'])
    s['canopy'] = 'none'
    bind(s)
    return s


def bind(s):
    for key in ('trim', 'frame', 'roof'):
        ts.materials[key] = ts.materials[s[key]]
    ts.materials['wall'] = ts.materials['wall-end'] = ts.materials[s['end']]
    ts.materials['plinth'] = ts.materials[hf.flat('6f6d68')]


def mface(name, face, w, H, openings, s):
    g, u = s['ground'], s['upper']
    if u != g and H > S:
        ts.facade(f'{name} ground', face, w, S, [o for o in openings if o[1] < S], g)
        top = ts.Face(tuple(face.point(0, S)), face.along, face.outward)
        ts.facade(f'{name} upper', top, w, H - S, [(a, v - S, b, h, k) for a, v, b, h, k in openings if v >= S], u)
    else:
        ts.facade(name, face, w, H, openings, g)
    d = ts.REVEAL
    for a, v, b, h, kind in openings:
        if kind == 'window':
            for u0, u1, v0, v1 in ((a, a + b, v, v + .07), (a, a + b, v + h - .07, v + h), (a, a + .07, v, v + h), (a + b - .07, a + b, v, v + h)):
                face.slab('frame', u0, u1, v0, v1, d - .06, d, 'frame')
    face.slab('base', -.02, w + .02, 0, .15, -.03, 0, 'plinth')


def c_front(w, st, var, mirror, door=True):
    f = (lambda a, b: w - a - b) if mirror else (lambda a, b: a)
    out, x = [], .5
    if door:
        out += [(f(x, 1.1), .15, 1.1, 2.4, 'door'), (f(x + 1.2, .45), .15, .45, 2.4, 'window')]
        x += 1.9
    gw = w - x - .5
    if gw >= 1.0:
        out.append((f(x, gw), .45, gw, 2.1, 'window'))
    for k in range(1, st):
        v = k * S
        if var == 'full' and w >= 5:
            b = min(w - 2.0, 3.2)
            out.append((f((w - b) / 2, b), v + .15, b, 2.9, 'window'))
        elif var == 'pair' and w >= 5.5:
            out += [(f(w * .2 - .45, .9), v + .2, .9, 2.8, 'window'), (f(w * .62 - 1.1, 2.2), v + .7, 2.2, 1.6, 'window')]
        else:
            b = min(w - 1.6, 4.2)
            out.append((f((w - b) / 2, b), v + .6, b, 1.8, 'window'))
    return out


def m_front(w, st, mirror, door=True):
    f = (lambda a, b: w - a - b) if mirror else (lambda a, b: a)
    out, x = [], .5
    if door:
        out.append((f(x, 1.0), .15, 1.0, 2.3, 'door'))
        x += 1.3
    gw = w - x - .5
    if gw >= 1.0:
        out.append((f(x, gw), .75, gw, 1.55, 'window'))
    out += [(f(.5, w - 1.0), k * S + .95, w - 1.0, 1.35, 'window') for k in range(1, st)]
    return out


def m_back(w, st, s):
    b, b2 = min(w - 1.4, 4.0), min(w - 2.0, 3.0)
    tall = s['period'] == 'contemporary'
    out = [((w - b) / 2, .15, b, 2.4, 'window')] if tall else [(.5, .15, 1.0, 2.3, 'door'), (1.8, .75, w - 2.3, 1.55, 'window')]
    return out + [((w - b2) / 2, k * S + .7, b2, 1.6, 'window') for k in range(1, st)]


def mrow(s, n, w, x0, y0, y1, st, front=True, back=True, ends=(-1, 1), door=True, z0=0.0, first=0):
    H, doors = st * S, []
    for i in range(n):
        x, k = x0 + i * w, first + i
        ts.materials['door'] = ts.materials[s['doors'][k % len(s['doors'])]]
        if front:
            face = ts.Face((x, y0, z0), (1, 0, 0), (0, -1, 0))
            contemporary = s['period'] == 'contemporary'
            ops = c_front(w, st, s['variant'], k % 2 == 1, door) if contemporary else m_front(w, st, k % 2 == 1, door)
            mface('front', face, w, H, ops, s)
            for a, v, b, h, kind in ops:
                if kind == 'door':
                    doors.append(x + a + b / 2)
                    if contemporary:
                        lo, hi = (a - .3, a + 1.95) if k % 2 == 0 else (a - .85, a + b + .3)
                        face.slab('canopy', lo, hi, 2.62, 2.72, -.9, 0, 'trim')
                    else:
                        face.slab('canopy', a - .4, a + b + .4, 2.5, 2.62, -1.0, 0, 'trim')
                elif contemporary and v > S and h > 2.5:
                    face.slab('rail', a, a + b, v + .95, v + 1.0, -.08, 0, 'frame')
            if s['panels']:
                for f in range(1, st):
                    face.slab('panel', .45, w - .45, f * S - .95, f * S + .95, -.03, 0, s['accent'])
        if back:
            mface('back', ts.Face((x + w, y1, z0), (-1, 0, 0), (0, 1, 0)), w, H, m_back(w, st, s), s)
    d = y1 - y0
    windows = [(d * .5 - .45, f * S + .5, .9, 2.0, 'window') for f in range(st)] if d >= 4 and s['period'] == 'contemporary' else []
    if -1 in ends:
        mface('left end', ts.Face((x0, y1, z0), (0, -1, 0), (-1, 0, 0)), d, H, windows, s)
    if 1 in ends:
        mface('right end', ts.Face((x0 + n * w, y0, z0), (0, 1, 0), (1, 0, 0)), d, H, windows, s)
    return doors


def parapet(s, x0, x1, y0, y1, z, height=.45, material=None):
    material = material or s['upper']
    ts.box('deck', (x0, y0, z - .05), (x1, y1, z + .02), s['flat'])
    for a, b in (((x0, y0), (x1, y0 + .25)), ((x0, y1 - .25), (x1, y1)), ((x0, y0), (x0 + .25, y1)), ((x1 - .25, y0), (x1, y1))):
        ts.box('parapet', (*a, z), (*b, z + height), material)
    for a, b in (((x0 - .03, y0 - .03), (x1 + .03, y0 + .28)), ((x0 - .03, y1 - .28), (x1 + .03, y1 + .03)),
                 ((x0 - .03, y0 - .03), (x0 + .28, y1 + .03)), ((x1 - .28, y0 - .03), (x1 + .03, y1 + .03))):
        ts.box('coping', (*a, z + height), (*b, z + height + .06), 'trim')


def gable_glass(s, cx, y, H, half, pitch):
    t = math.tan(math.radians(pitch))
    top = H + (half - .75) * t - .35
    if top > H + 1.0:
        ts.box('gable glass', (cx - .75, y - .03, H + .3), (cx + .75, y, top), 'glass')
        for x0, x1, z0, z1 in ((cx - .75, cx + .75, H + .3, H + .37), (cx - .75, cx + .75, top - .07, top),
                               (cx - .75, cx - .68, H + .3, top), (cx + .68, cx + .75, H + .3, top)):
            ts.box('gable frame', (x0, y - .06, z0), (x1, y - .03, z1), 'frame')


def staggered_gables(s):
    """One steep front gable to each house, no eaves, every other house set back from the street."""
    W, D, st = s['W'], s['D'], s['storeys']
    H, r = st * S, random.Random(s['seed'] + 5)
    n, w = houses(W, r.uniform(5.5, 7.0))
    step, first = r.uniform(1.2, 2.0), r.choice([0, 1])
    doors = []
    for i in range(n):
        x0 = -W / 2 + i * w
        back = n > 1 and i % 2 != first
        y0 = -D / 2 + (step if back else 0)
        sides = tuple(sd for sd in (-1, 1) if not back or (sd < 0 and i == 0) or (sd > 0 and i == n - 1))
        doors += mrow(s, 1, w, x0, y0, D / 2, st, ends=sides, first=i)
        hipgable(s, 'y', x0 + w / 2, (y0 + D / 2) / 2, w / 2, (D / 2 - y0) / 2, H, s['pitch'], eaves=s['eaves'])
        gable_glass(s, x0 + w / 2, y0, H, w / 2, s['pitch'])
    return doors


def flat_setback(s):
    """Flat roofed. At three storeys the top storey stands back behind a roof terrace; at two, framed boxes
    stand out round every other upper window."""
    W, D, st = s['W'], s['D'], s['storeys']
    r = random.Random(s['seed'] + 5)
    n, w = houses(W, r.uniform(6.0, 8.0))
    low = st - 1 if st >= 3 else st
    Hl = low * S
    doors = mrow(s, n, w, -W / 2, -D / 2, D / 2, low)
    parapet(s, -W / 2, W / 2, -D / 2, D / 2, Hl)
    if low < st:
        sb = r.uniform(1.8, 2.6)
        keep = s['ground'], s['upper'], s['variant']
        s['ground'] = s['upper'] = s['accent']
        s['variant'] = 'full'
        mrow(s, n, w, -W / 2, -D / 2 + sb, D / 2, 1, door=False, z0=Hl)
        s['ground'], s['upper'], s['variant'] = keep
        ts.box('roof', (-W / 2 - .3, -D / 2 + sb - .4, Hl + S), (W / 2 + .3, D / 2 + .3, Hl + S + .25), 'trim')
        ts.box('terrace glass', (-W / 2 + .3, -D / 2 + .1, Hl + .5), (W / 2 - .3, -D / 2 + .14, Hl + 1.05), 'glass')
        for i in range(1, n):
            ts.box('terrace screen', (-W / 2 + i * w - .04, -D / 2 + .25, Hl), (-W / 2 + i * w + .04, -D / 2 + sb, Hl + 1.8), s['accent'])
    else:
        for i in range(0, n, 2):
            face = ts.Face((-W / 2 + i * w, -D / 2, 0), (1, 0, 0), (0, -1, 0))
            for a, v, b, h, kind in c_front(w, st, s['variant'], i % 2 == 1):
                if v > S:
                    for u0, u1, v0, v1 in ((a - .2, a + b + .2, v - .2, v), (a - .2, a + b + .2, v + h, v + h + .2),
                                           (a - .2, a, v, v + h), (a + b, a + b + .2, v, v + h)):
                        face.slab('box window', u0, u1, v0, v1, -.5, 0, s['accent'])
                    break
    return doors


def sawtooth(s):
    """A mono-pitch to each house, all rising the same way, a clerestory in each step."""
    W, D, st = s['W'], s['D'], s['storeys']
    H, r = st * S, random.Random(s['seed'] + 5)
    n, w = houses(W, r.uniform(6.0, 7.5))
    d = r.choice([-1, 1])
    rise, e = w * math.tan(math.radians(s['pitch'])), s['eaves']
    doors = mrow(s, n, w, -W / 2, -D / 2, D / 2, st)
    for i in range(n):
        a, b = -W / 2 + i * w, -W / 2 + (i + 1) * w
        lo, hi = (a, b) if d > 0 else (b, a)
        poly('mono', [(lo, -D / 2 - e, H), (hi, -D / 2 - e, H + rise), (hi, D / 2 + e, H + rise), (lo, D / 2 + e, H)], 'roof')
        for y in (-D / 2, D / 2):
            poly('mono end', [(lo, y, H), (hi, y, H), (hi, y, H + rise)], 'wall')
        poly('step', [(hi, -D / 2, H), (hi, D / 2, H), (hi, D / 2, H + rise), (hi, -D / 2, H + rise)], 'wall')
        if 0 < (i + 1 if d > 0 else i) < n and rise > 1.2:
            x = hi + d * .02
            ts.box('clerestory', (min(x, x + d * .03), -D / 2 + .6, H + .3), (max(x, x + d * .03), D / 2 - .6, H + rise - .3), 'glass')
    return doors


def pavilions(s):
    """Front-gabled pavilions joined by flat-roofed links set back from the street. The doors are in the links."""
    W, D, st = s['W'], s['D'], s['storeys']
    H, r = st * S, random.Random(s['seed'] + 5)
    lw, rec = r.uniform(2.8, 3.4), r.uniform(1.6, 2.4)
    count = max(1, round((W + lw) / (r.uniform(7.0, 8.5) + lw)))
    pw = (W - (count - 1) * lw) / count if count > 1 else W - lw
    layout = []
    for i in range(count):
        layout.append(('pavilion', pw))
        if i < count - 1 or count == 1:
            layout.append(('link', lw))
    ls = st if st <= 2 else st - 1
    x, doors = -W / 2, []
    for k, (kind, width) in enumerate(layout):
        if kind == 'pavilion':
            doors += mrow(s, 1, width, x, -D / 2, D / 2, st, door=k == 0 and count > 1, first=k)
            hipgable(s, 'y', x + width / 2, 0, width / 2, D / 2, H, s['pitch'], eaves=s['eaves'])
            gable_glass(s, x + width / 2, -D / 2, H, width / 2, s['pitch'])
        else:
            keep = s['ground'], s['upper'], s['variant']
            s['ground'] = s['upper'] = s['accent']
            s['variant'] = 'wide'
            doors += mrow(s, 1, width, x, -D / 2 + rec, D / 2, ls, ends=(), first=k)
            parapet(s, x, x + width, -D / 2 + rec, D / 2, ls * S, material=s['accent'])
            s['ground'], s['upper'], s['variant'] = keep
        x += width
    return doors


def span(s):
    """A shallow side gable over narrow houses, brick cross walls standing forward between them, panels under
    ribbon windows."""
    W, D, st = s['W'], s['D'], s['storeys']
    H, r = st * S, random.Random(s['seed'] + 5)
    n, w = houses(W, r.uniform(5.2, 6.4))
    doors = mrow(s, n, w, -W / 2, -D / 2, D / 2, st)
    e, t = s['eaves'], math.tan(math.radians(s['pitch']))
    hipgable(s, 'x', 0, 0, D / 2, W / 2, H, s['pitch'], eaves=e)
    for y, f in ((-D / 2, -1), (D / 2, 1)):
        ts.box('fascia', (-W / 2 - e, y + f * e - .03, H - e * t - .28), (W / 2 + e, y + f * e + .03, H - e * t + .02), 'trim')
        for i in range(n + 1):
            x = -W / 2 + i * w
            ts.box('cross wall', (x - .13, min(y, y + f * .4), 0), (x + .13, max(y, y + f * .4), H - .15), s['fin'])
    return doors


def butterfly(s):
    """An inverted roof to each house, falling to a valley on the house's center line."""
    W, D, st = s['W'], s['D'], s['storeys']
    H, r = st * S, random.Random(s['seed'] + 5)
    n, w = houses(W, r.uniform(6.5, 8.0))
    t, e = math.tan(math.radians(s['pitch'])), .45
    rise = w / 2 * t
    doors = mrow(s, n, w, -W / 2, -D / 2, D / 2, st)
    for i in range(n):
        a, b = -W / 2 + i * w, -W / 2 + (i + 1) * w
        c = (a + b) / 2
        ea, eb = (e if i == 0 else 0), (e if i == n - 1 else 0)
        poly('butterfly', [(a - ea, -D / 2 - e, H + rise + ea * t), (c, -D / 2 - e, H), (c, D / 2 + e, H), (a - ea, D / 2 + e, H + rise + ea * t)], 'roof')
        poly('butterfly', [(c, -D / 2 - e, H), (b + eb, -D / 2 - e, H + rise + eb * t), (b + eb, D / 2 + e, H + rise + eb * t), (c, D / 2 + e, H)], 'roof')
        for y in (-D / 2, D / 2):
            poly('butterfly wall', [(a, y, H), (c, y, H), (a, y, H + rise)], 'wall')
            poly('butterfly wall', [(c, y, H), (b, y, H), (b, y, H + rise)], 'wall')
        ts.box('fascia', (a - ea, -D / 2 - e - .04, H - .2), (b + eb, -D / 2 - e, H + .02), 'trim')
    for x in (-W / 2, W / 2):
        poly('end', [(x, -D / 2, H), (x, D / 2, H), (x, D / 2, H + rise), (x, -D / 2, H + rise)], 'wall')
    return doors


def monopitch(s):
    """One low mono-pitch rising toward the street, a clerestory band of timber and glass under its high edge."""
    W, D, st = s['W'], s['D'], s['storeys']
    H, r = st * S, random.Random(s['seed'] + 5)
    n, w = houses(W, r.uniform(5.5, 7.0))
    t = min(math.tan(math.radians(s['pitch'])), 3.0 / D)
    rise = D * t
    doors = mrow(s, n, w, -W / 2, -D / 2, D / 2, st)
    for i in range(n):
        face = ts.Face((-W / 2 + i * w, -D / 2, H), (1, 0, 0), (0, -1, 0))
        ops = [(.4, .3, w - .8, rise - .6, 'window')] if rise > 1.2 else []
        ts.facade('clerestory', face, w, rise, ops, s['accent'])
    for x in (-W / 2, W / 2):
        poly('end', [(x, -D / 2, H), (x, D / 2, H), (x, -D / 2, H + rise)], 'wall')
    ef, eb = .7, .35
    poly('mono', [(-W / 2 - .3, -D / 2 - ef, H + rise + ef * t), (W / 2 + .3, -D / 2 - ef, H + rise + ef * t),
                  (W / 2 + .3, D / 2 + eb, H - eb * t), (-W / 2 - .3, D / 2 + eb, H - eb * t)], 'roof')
    ts.box('fascia', (-W / 2 - .3, -D / 2 - ef - .04, H + rise + ef * t - .3), (W / 2 + .3, -D / 2 - ef, H + rise + ef * t + .02), 'trim')
    for i in range(n + 1):
        x = -W / 2 + i * w
        ts.box('cross wall', (x - .13, -D / 2 - .4, 0), (x + .13, -D / 2, H - .1), s['fin'])
    return doors


def low_hip(s):
    """A low hipped roof with deep eaves over the whole row, the upper storey hung with tiles or boards and a
    canopy along the ground floor."""
    W, D, st = s['W'], s['D'], s['storeys']
    H, r = st * S, random.Random(s['seed'] + 5)
    n, w = houses(W, r.uniform(6.0, 7.5))
    keep = s['upper'], s['panels']
    s['upper'], s['panels'] = s['accent'], False
    doors = mrow(s, n, w, -W / 2, -D / 2, D / 2, st)
    s['upper'], s['panels'] = keep
    hipgable(s, 'x', 0, 0, D / 2, W / 2, H, s['pitch'], k=1, eaves=.75)
    ts.box('canopy', (-W / 2 + .2, -D / 2 - 1.0, 2.55), (W / 2 - .2, -D / 2, 2.67), 'trim')
    return doors


NEW = {'staggered-gables': staggered_gables, 'flat-setback': flat_setback, 'sawtooth': sawtooth, 'pavilions': pavilions,
       'span': span, 'butterfly': butterfly, 'monopitch': monopitch, 'low-hip': low_hip}
DEEP_NEW = {'deep-flat': flat_setback, 'deep-gables': staggered_gables, 'deep-sawtooth': sawtooth, 'deep-span': span}


def new_deep(s, roof):
    """The street form over a double-pile front, flat-roofed outriggers running back to the end of the Lot."""
    W, D, st = s['W'], s['D'], s['storeys']
    df = min(13.0, max(8.0, D * .37))
    pile = 2 * df
    start = len(ts.parts)
    s['D'] = pile
    doors = DEEP_NEW[roof](s)
    s['D'] = D
    for part in ts.parts[start:]:
        for v in part.data.vertices:
            v.co.y += -D / 2 + pile / 2
    yb = -D / 2 + pile
    os_ = max(1, st - 1)
    covered = W * pile
    n, w = houses(W, 6.5)
    if D / 2 - yb > 1.5:
        wings = [(k * w - w / 2, k * w + w / 2) for k in range(1, n, 2)]
        if n % 2:
            wings.append((n * w - w / 2, n * w))
        for a, b in wings:
            x0, x1 = -W / 2 + a, -W / 2 + b
            mrow(s, 1, x1 - x0, x0, yb - .01, D / 2, os_, front=False)
            parapet(s, x0, x1, yb - .01, D / 2, os_ * S)
            covered += (x1 - x0) * (D / 2 - yb) * os_ / st
    print('FLOOR', roof, W, D, st, f'{covered / (W * D):.2f}')
    return doors


def new_street(s, doors):
    if s['boundary'] != 'open':
        hf.street_details(s, doors)
        return
    ts.box('forecourt', (-s['W'] / 2, -s['D'] / 2 - 2.2, 0), (s['W'] / 2, -s['D'] / 2, .04), hf.flat('a8a49b'))


BUILD = {'hipped-pair': hipped_pair, 'double-fronted': double_fronted, 'gabled-wings': gabled_wings, 'gambrel': gambrel,
         'mansard': mansard, 'cross-gables': cross_gables, 'catslide': catslide,
         'terrace': None, 'gables': None, 'villa': None,
         'deep-m-gable': None, 'deep-hipped': None, 'deep-gabled-front': None, 'deep-cross-gables': None}


def place(s, cx, cy, turned):
    start = len(ts.parts)
    form = s['form']
    if form in ('terrace', 'gables', 'villa'):
        hf.BUILD[form](s)
    elif form in NEW or form in DEEP_NEW:
        new_street(s, (NEW[form](s) if form in NEW else new_deep(s, form)))
    else:
        doors = deep(s, form) if form.startswith('deep') else BUILD[form](s)
        hf.street_details(s, doors)
    for part in ts.parts[start:]:
        if turned:
            part.rotation_euler[2] = math.pi
        part.location = (cx, cy, 0)


def finish(camera, shots):
    for part in ts.parts:
        ts.project_uvs(part)
    for name, eye, target in shots:
        hf.shoot(camera, name, eye, target)


def sheet(form, seed=301):
    """The form at the small and the large band, two storeys and three."""
    hf.fresh()
    camera = hf.stage(road_y=(-205.0, -195.0))
    small = 24 if form == 'villa' else 16
    place(look(seed, small, 12, 2, form), -28, 0, False)
    place(look(seed + 1, 44, 20, 3, form), 12, 4, False)
    finish(camera, [(f'sheet-{form}', (-12, -90, 40), (-1, 2, 4))])


def deep_sheet(roof, seed=401):
    hf.fresh()
    camera = hf.stage(road_y=(-205.0, -195.0))
    place(look(seed, 24, 32, 2, roof), -24, 0, False)
    place(look(seed + 1, 40, 36, 3, roof), 16, 2, False)
    finish(camera, [(f'{roof}-front', (-8, -110, 52), (-2, 2, 4)),
                    (f'{roof}-rear', (40, 75, 45), (0, 4, 4))])


def street(seed=55):
    """Fourteen Buildings to a side, mixed forms and widths."""
    hf.fresh()
    camera = hf.stage()
    r = random.Random(seed)
    forms = [f for f in BUILD if not f.startswith('deep')]
    for side, turned in ((1, False), (-1, True)):
        x = -150.0
        last = None
        for _ in range(9):
            W = r.choice([16, 20, 24, 28, 32, 40, 48])
            D = r.choice([12, 16, 20])
            form = r.choice([f for f in forms if f != last and (f != 'villa' or W >= 24)])
            last = form
            s = look(r.randrange(1 << 30), W, D, r.choice([2, 3]), form)
            cx = x + W / 2
            place(s, cx if side > 0 else -cx, side * (10 + D / 2), turned)
            print('STREET', side, cx, form, W, D, s['storeys'], round(s['pitch']))
            x += W + r.uniform(4, 8)
    finish(camera, [('street-game', (-70, -175, 150), (-70, -8, 0)),
                    ('street-level', (-140, -4, 5), (-90, 16, 7))])


JOBS = {f'sheet-{f}': (lambda f=f: sheet(f)) for f in BUILD if not f.startswith('deep')}
JOBS.update({r: (lambda r=r: deep_sheet(r)) for r in ('deep-m-gable', 'deep-hipped', 'deep-gabled-front', 'deep-cross-gables')})
JOBS['street'] = street


def mixed_street(seed=61):
    """Nine Buildings to a side drawn at the target period shares, no two neighbors sharing a form."""
    hf.fresh()
    camera = hf.stage()
    r = random.Random(seed)
    pools = [('contemporary', .55, [f for f in NEW if PERIOD[f] == 'contemporary']),
             ('mid-century', .25, [f for f in NEW if PERIOD[f] == 'mid-century']),
             ('traditional', .20, ['terrace', 'gables', 'villa'])]
    for side, turned in ((1, False), (-1, True)):
        x, last = -150.0, None
        for _ in range(9):
            W = r.choice([16, 20, 24, 28, 32, 40, 48])
            D = r.choice([12, 16, 20])
            while True:
                pool = r.choices(pools, weights=[p[1] for p in pools])[0][2]
                form = r.choice(pool)
                if form != last and (form != 'villa' or W >= 24):
                    break
            last = form
            s = look(r.randrange(1 << 30), W, D, r.choice([2, 3]), form)
            cx = x + W / 2
            place(s, cx if side > 0 else -cx, side * (10 + D / 2), turned)
            print('STREET', side, cx, form, W, D, s['storeys'], round(s['pitch']))
            x += W + r.uniform(4, 8)
    finish(camera, [('mixed-game', (-70, -175, 150), (-70, -8, 0)),
                    ('mixed-level', (-140, -4, 5), (-90, 16, 7))])


JOBS.update({f'sheet-{f}': (lambda f=f: sheet(f)) for f in NEW})
JOBS.update({r: (lambda r=r: deep_sheet(r)) for r in DEEP_NEW})
JOBS['mixed'] = mixed_street

for name, job in JOBS.items():
    if not ONLY or name in ONLY:
        job()
        print('DONE', name)

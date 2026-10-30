"""Detached shop forms: a shopfront Building standing alone on its Lot, one store on the ground storey
and flats above, 2-3 storeys on 20-60 m by 16-28 m. The materials, parapets, gables and roof trim come
from wide-house-forms.py.

blender --background --factory-startup --python-exit-code 1 --python scripts/art/detached-shop-forms.py -- REPO OUT [job...]

Jobs: sheet-<form> for each form, sheet-units (the glass base split into several shops), and street, the
founding run's shop Lots drawn at the target period shares.
"""
import importlib.util
import math
import random
import sys
from pathlib import Path

ARGS = sys.argv[sys.argv.index('--') + 1:]
REPO, OUT, ONLY = Path(ARGS[0]), Path(ARGS[1]), ARGS[2:]

# wide-house-forms.py runs the jobs named on the command line when imported, so it is handed none of its own.
spec = importlib.util.spec_from_file_location('wh', REPO / 'scripts/art/wide-house-forms.py')
wh = importlib.util.module_from_spec(spec)
_argv = sys.argv
sys.argv = _argv[:_argv.index('--') + 3] + ['(none)']
spec.loader.exec_module(wh)
sys.argv = _argv
hf, hfm, ts, S = wh.hf, wh.hfm, wh.ts, wh.S
poly = wh.poly

FORMS = {
    'glass-base': {'period': 'contemporary', 'pitch': (0, 0)},
    'gabled-hall': {'period': 'contemporary', 'pitch': (38, 45)},
    'colonnade': {'period': 'contemporary', 'pitch': (0, 0)},
    'mono-pitch': {'period': 'contemporary', 'pitch': (8, 12)},
    'canopy': {'period': 'mid-century', 'pitch': (0, 0)},
    'folded-plate': {'period': 'mid-century', 'pitch': (14, 20)},
    'main-street': {'period': 'traditional', 'pitch': (0, 0)},
}

FRAMES = ['1f2124', '2b2e31', '3a3d40', 'b9bdbf']
BRANDS = ['2f6b4f', 'b4472f', '1f4e79', 'c9a227', '6b2f4a', '2b2e31', 'd9d4c8']
TIMBER_PAINTS = ['2f4a3c', '6a2f2a', '24303a', '1f2124', '2e5a5a', '7a3b52']
CREAMS = ['e3dccb', 'd8d2c4', 'ddd6c4']


def look(seed, W, D, storeys, form, units=1):
    period = FORMS[form]['period']
    wh.PERIOD[form] = 'contemporary' if period == 'contemporary' else 'mid-century'
    wh.PITCH[form] = FORMS[form]['pitch']
    s = wh.new_look(seed, W, D, storeys, form)
    s['period'] = period
    r = random.Random(seed + 21)
    s['units'] = units
    s['entrance'] = r.choice(['left', 'centre', 'right'])
    s['wrap'] = r.choice([-1, 1])
    s['brand'] = r.choice(BRANDS)
    if period == 'traditional':
        brick = hf.textured(r.choice(['bricks-051', 'bricks-066', 'bricks-085']))
        s['ground'] = s['upper'] = s['end'] = brick
        s['trim'] = hf.flat(r.choice(CREAMS))
        s['frame'] = hf.flat('ece8dc', .5)
        s['store'] = hf.flat(r.choice(TIMBER_PAINTS), .6)
        wh.bind(s)
    elif period == 'mid-century':
        s['store'] = hf.flat(r.choice(['b9bdbf', 'c8ccce', '2b2e31']), .4)
    else:
        s['store'] = hf.flat(r.choice(FRAMES), .45)
    ts.materials['storefront'] = ts.materials[s['store']]
    ts.materials['sign'] = ts.materials[hf.flat(s['brand'], .5)]
    ts.materials['metal'] = ts.materials[hf.flat('55595c', .5)]
    return s


def faces(x0, x1, y0, y1, z=0.0):
    """Front, right, back and left walls of a rectangle, each with its length. Fronts face -Y."""
    W, D = x1 - x0, y1 - y0
    return {'front': (ts.Face((x0, y0, z), (1, 0, 0), (0, -1, 0)), W),
            'right': (ts.Face((x1, y0, z), (0, 1, 0), (1, 0, 0)), D),
            'back': (ts.Face((x1, y1, z), (-1, 0, 0), (0, 1, 0)), W),
            'left': (ts.Face((x0, y1, z), (0, -1, 0), (-1, 0, 0)), D)}


def framed(face, ops):
    d = ts.REVEAL
    for a, v, b, h, kind in ops:
        if kind == 'window':
            for u0, u1, v0, v1 in ((a, a + b, v, v + .07), (a, a + b, v + h - .07, v + h), (a, a + .07, v, v + h), (a + b - .07, a + b, v, v + h)):
                face.slab('frame', u0, u1, v0, v1, d - .06, d, 'frame')


def door_glass(face, u, w, top):
    face.slab('door glass', u + .12, u + w - .12, .3, top - .12, ts.REVEAL + .03, ts.REVEAL + .04, 'glass')


def store_bays(w, r):
    n = max(1, round(w / r.uniform(3.6, 4.4)))
    return n, w / n


def store_front(s, face, w):
    """The store's street front: glazed bays between piers and one entrance, or one entrance per shop."""
    r = random.Random(s['seed'] + 31)
    period = s['period']
    top = {'contemporary': 2.8, 'mid-century': 2.85, 'traditional': 2.9}[period]
    riser = {'contemporary': .2, 'mid-century': .45, 'traditional': .7}[period]
    pier = {'contemporary': .22, 'mid-century': .3, 'traditional': .35}[period]
    units = [(w * i / s['units'], w * (i + 1) / s['units']) for i in range(s['units'])]
    ops, doors = [], []
    for k, (a, b) in enumerate(units):
        n, bw = store_bays(b - a, r)
        if s['units'] > 1:
            door = 0 if k % 2 == 0 else n - 1
        else:
            door = {'left': 0, 'centre': n // 2, 'right': n - 1}[s['entrance']]
        for i in range(n):
            u0 = a + i * bw
            if i == door:
                dw = min(2.0, bw - 2 * pier - .3)
                ops.append((u0 + (bw - dw) / 2, .15, dw, top - .15, 'door'))
                doors.append((u0 + (bw - dw) / 2, dw))
            else:
                ops.append((u0 + pier, riser, bw - 2 * pier, top - riser, 'shop'))
    ts.materials['door'] = ts.materials[s['store']]
    ts.facade('store', face, w, S, ops, 'storefront' if period == 'contemporary' else 'wall')
    for u, dw in doors:
        door_glass(face, u, dw, top - .15 + .15)
    face.slab('base', -.02, w + .02, 0, .15, -.03, 0, 'plinth')
    if period == 'contemporary':
        face.slab('fascia', 0, w, top + .05, S - .02, -.06, 0, 'storefront')
    elif period == 'traditional':
        n, bw = store_bays(w, random.Random(s['seed'] + 31))
        for i in range(n + 1):
            a = min(max(i * bw - pier / 2, 0), w - pier)
            face.slab('pilaster', a, a + pier, 0, 3.0, -.12, 0, 'storefront')
        face.slab('fascia', 0, w, 3.0, 3.4, -.2, 0, 'storefront')
        face.slab('cornice', -.1, w + .1, 3.4, 3.52, -.35, 0, 'trim')
    for k, (a, b) in enumerate(units):
        ts.materials['sign'] = ts.materials[hf.flat(s['brand'] if k == 0 else random.Random(s['seed'] + k).choice(BRANDS), .5)]
        sw = min(12.0, (b - a) * .5)
        c = (a + b) / 2
        if period == 'contemporary':
            face.slab('sign', c - sw / 2, c + sw / 2, top + .12, S - .1, -.1, -.06, 'sign')
        elif period == 'traditional':
            face.slab('sign', c - sw / 2, c + sw / 2, 3.07, 3.33, -.22, -.2, 'sign')
    return doors


def store_side(s, face, d, front_first):
    """A free side wall's ground storey: the store's glass turning the corner, then the flats' door."""
    def at(u, span):
        return u if front_first else d - u - span
    wrap = min(6.0, d * .35) if s['period'] != 'traditional' else min(3.2, d * .25)
    ops = [(at(.4, wrap), .45 if s['period'] != 'contemporary' else .2, wrap, 2.4 if s['period'] != 'contemporary' else 2.8, 'shop')]
    if d - wrap > 6.0:
        ops.append((at(d - 3.6, 1.1), .15, 1.1, 2.4, 'door'))
        ops.append((at(d - 2.1, 1.0), 1.0, 1.0, 1.4, 'window'))
    ts.materials['door'] = ts.materials[s['doors'][0]]
    ts.facade('store side', face, d, S, ops, 'wall' if s['period'] != 'contemporary' else s['ground'])
    framed(face, ops)
    face.slab('base', -.02, d + .02, 0, .15, -.03, 0, 'plinth')


def store_back(s, face, w):
    ops = [(1.2, .15, 3.2, 3.0, 'roller'), (5.2, .15, 1.0, 2.3, 'door')]
    if w > 14:
        ops.append((w - 3.0, .9, 1.6, 1.4, 'window'))
    ts.materials['door'] = ts.materials[hf.flat('55595c', .5)]
    ts.facade('store back', face, w, S, ops, s['ground'])
    framed(face, ops)
    face.slab('base', -.02, w + .02, 0, .15, -.03, 0, 'plinth')


def upper_ops(s, w, rows, side=False):
    period = s['period']
    out = []
    if period == 'mid-century' and not side:
        return [(.5, k * S + .95, w - 1.0, 1.35, 'window') for k in range(rows)] if w > 2 else []
    if period == 'traditional':
        n = max(1, round(w / 2.9))
        return [(w * (i + .5) / n - .55, k * S + .75, 1.1, 1.9, 'window') for k in range(rows) for i in range(n)]
    n = max(1, round(w / (3.6 if not side else 4.5)))
    for k in range(rows):
        for i in range(n):
            c = w * (i + .5) / n
            if period == 'contemporary' and not side and (i + k) % 3 == 1:
                out.append((c - .8, k * S + .15, 1.6, 2.6, 'window'))
            else:
                out.append((c - .8, k * S + .7, 1.6, 1.8 if period == 'contemporary' else 1.5, 'window'))
    return out


def uppers(s, x0, x1, y0, y1, z0, rows, material=None, which=('front', 'right', 'back', 'left')):
    """The flats' storeys from z0, windows in the period's pattern on every free wall."""
    if rows <= 0:
        return
    material = material or s['upper']
    for name, (face, n) in faces(x0, x1, y0, y1, z0).items():
        if name not in which:
            continue
        side = name in ('left', 'right')
        ops = upper_ops(s, n, rows, side)
        ts.facade(f'upper {name}', face, n, rows * S, ops, material)
        framed(face, ops)
        for a, v, b, h, kind in ops:
            if s['period'] == 'contemporary' and h > 2.5:
                face.slab('rail', a, a + b, v + .95, v + 1.0, -.08, 0, 'frame')
            elif s['period'] == 'traditional':
                face.slab('lintel', a - .12, a + b + .12, v + h, v + h + .22, -.04, 0, 'trim')
                face.slab('sash rail', a, a + b, v + h * .5 - .03, v + h * .5 + .03, ts.REVEAL - .07, ts.REVEAL - .02, 'frame')
        if s['period'] == 'mid-century' and not side and n > 2:
            for k in range(rows):
                face.slab('panel', .5, n - .5, k * S + .1, k * S + .95, -.03, 0, s['accent'])


def ground(s, x0, x1, y0, y1):
    f = faces(x0, x1, y0, y1)
    doors = store_front(s, *f['front'])
    store_back(s, *f['back'])
    store_side(s, f['right'][0], f['right'][1], front_first=True)
    store_side(s, f['left'][0], f['left'][1], front_first=False)
    return doors


def block(s, x0, x1, y0, y1, storeys):
    doors = ground(s, x0, x1, y0, y1)
    uppers(s, x0, x1, y0, y1, S, storeys - 1)
    return doors


def glass_base(s):
    """A fully glazed store under flats in brick, timber or panels. At three storeys the top storey stands
    back behind a roof terrace."""
    W, D, st = s['W'], s['D'], s['storeys']
    r = random.Random(s['seed'] + 5)
    x0, x1, y0, y1 = -W / 2, W / 2, -D / 2, D / 2
    low = st - 1 if st >= 3 else st
    block(s, x0, x1, y0, y1, low)
    wh.parapet(s, x0, x1, y0, y1, low * S)
    floor = W * D * low
    if low < st:
        sb = r.uniform(1.8, 2.6)
        uppers(s, x0 + .4, x1 - .4, y0 + sb, y1 - .4, low * S, 1, material=s['accent'])
        ts.box('roof', (x0, y0 + sb - .4, st * S), (x1, y1, st * S + .25), 'trim')
        ts.box('terrace glass', (x0 + .3, y0 + .1, low * S + .5), (x1 - .3, y0 + .14, low * S + 1.05), 'glass')
        floor += (W - .8) * (D - sb - .4)
    print('FLOOR', s['form'], W, D, st, f'{floor / (W * D * st):.2f}')


def gabled_hall(s):
    """Steep plain gables across the frontage, no eaves, metal, glass in every gable end."""
    W, D, st = s['W'], s['D'], s['storeys']
    H, r = st * S, random.Random(s['seed'] + 5)
    n = max(2, round(W / r.uniform(9.0, 12.0)))
    gw = W / n
    block(s, -W / 2, W / 2, -D / 2, D / 2, st)
    for i in range(n):
        cx = -W / 2 + (i + .5) * gw
        hfm.hipgable(s, 'y', cx, 0, gw / 2, D / 2, H, s['pitch'], eaves=s['eaves'])
        wh.gable_edges(s, 'y', cx, 0, gw / 2, D / 2, H, s['pitch'], s['eaves'])
        wh.gable_glass(s, cx, -D / 2, H, gw / 2, s['pitch'])


def colonnade(s):
    """The store set back behind square piers, the flats flush with the street above them."""
    W, D, st = s['W'], s['D'], s['storeys']
    r = random.Random(s['seed'] + 5)
    x0, x1, y0, y1 = -W / 2, W / 2, -D / 2, D / 2
    cd = r.uniform(2.4, 3.0)
    ground(s, x0, x1, y0 + cd, y1)
    uppers(s, x0, x1, y0, y1, S, st - 1)
    wh.parapet(s, x0, x1, y0, y1, st * S, height=.6)
    n = max(2, round(W / r.uniform(3.6, 4.4)))
    for i in range(n + 1):
        x = x0 + W * i / n
        a, b = max(x0, x - .25), min(x1, x + .25)
        ts.box('pier', (a, y0, 0), (b, y0 + .5, S), s['upper'])
    ts.box('soffit', (x0, y0, S - .2), (x1, y0 + cd, S), 'trim')
    ts.box('colonnade floor', (x0, y0, 0), (x1, y0 + cd, .14), hf.flat('a8a49b'))
    print('FLOOR', s['form'], W, D, st, f'{1 - cd / (D * st):.2f}')


def mono_pitch(s):
    """One low mono-pitch over the whole Building rising toward the street, a clerestory under its high edge."""
    W, D, st = s['W'], s['D'], s['storeys']
    H = st * S
    t = min(math.tan(math.radians(s['pitch'])), 3.5 / D)
    rise = D * t
    x0, x1, y0, y1 = -W / 2, W / 2, -D / 2, D / 2
    block(s, x0, x1, y0, y1, st)
    face = ts.Face((x0, y0, H), (1, 0, 0), (0, -1, 0))
    n = max(1, round(W / 4.0))
    ops = [(W * (i + .5) / n - 1.5, .3, 3.0, rise - .6, 'window') for i in range(n)] if rise > 1.2 else []
    ts.facade('clerestory', face, W, rise, ops, s['accent'])
    for x in (x0, x1):
        poly('end', [(x, y0, H), (x, y1, H), (x, y0, H + rise)], s['accent'])
    ef, eb = .9, .35
    a, b = (x0 - .3, y1 + eb, H - eb * t), (x1 + .3, y1 + eb, H - eb * t)
    c, d = (x0 - .3, y0 - ef, H + rise + ef * t), (x1 + .3, y0 - ef, H + rise + ef * t)
    poly('mono', [c, d, b, a], 'roof')
    wh.slope_edges(s, a, b, c, d, ridge=False)
    ts.box('fascia', (x0 - .3, y0 - ef - .04, c[2] - .3), (x1 + .3, y0 - ef, c[2] + .02), 'trim')


def canopy(s):
    """A thin flat roof, ribbon windows over coloured panels, and a deep canopy along the store that turns both
    corners."""
    W, D, st = s['W'], s['D'], s['storeys']
    H, r = st * S, random.Random(s['seed'] + 5)
    x0, x1, y0, y1 = -W / 2, W / 2, -D / 2, D / 2
    block(s, x0, x1, y0, y1, st)
    ts.box('deck', (x0, y0, H - .05), (x1, y1, H + .02), s['flat'])
    ts.box('roof edge', (x0 - .5, y0 - .5, H - .35), (x1 + .5, y1 + .5, H - .05), 'trim')
    ts.box('roof edge', (x0 - .5, y0 - .5, H - .05), (x1 + .5, y0 - .3, H + .25), 'trim')
    ts.box('roof edge', (x0 - .5, y1 + .3, H - .05), (x1 + .5, y1 + .5, H + .25), 'trim')
    deep, turn = r.uniform(2.2, 2.8), min(6.5, D * .4)
    ts.box('canopy', (x0 - deep, y0 - deep, 3.05), (x1 + deep, y0, 3.3), 'trim')
    for x, sign in ((x0, -1), (x1, 1)):
        ts.box('canopy', (min(x, x + sign * deep), y0, 3.05), (max(x, x + sign * deep), y0 + turn, 3.3), 'trim')
        ts.box('end fin', (min(x, x + sign * .3), y0 + turn, 0), (max(x, x + sign * .3), y1, H - .35), s['fin'])
    ts.materials['sign'] = ts.materials[hf.flat(s['brand'], .5)]
    sw = min(10.0, W * .4)
    ts.box('sign', (-sw / 2, y0 - deep - .05, 3.3), (sw / 2, y0 - deep + .1, 3.95), 'sign')


def folded_plate(s):
    """A folded-plate roof: shallow folds across the frontage, glazed triangles in the folds at the front."""
    W, D, st = s['W'], s['D'], s['storeys']
    H, r = st * S, random.Random(s['seed'] + 5)
    x0, x1, y0, y1 = -W / 2, W / 2, -D / 2, D / 2
    block(s, x0, x1, y0, y1, st)
    n = max(2, round(W / r.uniform(4.0, 5.5)))
    f = W / n
    rise = f / 2 * math.tan(math.radians(s['pitch']))
    e = .6
    for i in range(n):
        a, c = x0 + i * f, x0 + (i + .5) * f
        b = a + f
        for lo, hi in ((a, c), (b, c)):
            pa, pb = (lo, y0 - e, H), (lo, y1 + e, H)
            qa, qb = (hi, y0 - e, H + rise), (hi, y1 + e, H + rise)
            poly('fold', [pa, qa, qb, pb] if lo < hi else [pb, qb, qa, pa], 'roof')
            ts.box('fold edge', (min(lo, hi), y0 - e - .06, H - .25), (max(lo, hi), y0 - e, H - .05), 'trim')
        poly('fold glass', [(a, y0, H), (b, y0, H), (c, y0, H + rise)], 'glass')
        poly('fold end', [(b, y1, H), (a, y1, H), (c, y1, H + rise)], 'wall')
        for p, q in (((a, y0 - e - .05, H), (c, y0 - e - .05, H + rise)), ((c, y0 - e - .05, H + rise), (b, y0 - e - .05, H))):
            wh.beam('fold fascia', p, q, (0, .05, 0), (0, 0, .14), 'trim')
    for x in (x0, x1):
        poly('end', [(x, y0, H), (x, y1, H), (x, y1, H + .01)], 'wall')
    ts.box('canopy', (x0 + .2, y0 - 1.6, 2.95), (x1 - .2, y0, 3.1), 'trim')


def main_street(s):
    """A two-part commercial block: one timber shopfront under a fascia, flats in brick behind sash windows,
    a corbelled cornice and a name panel on the parapet."""
    W, D, st = s['W'], s['D'], s['storeys']
    H = st * S
    x0, x1, y0, y1 = -W / 2, W / 2, -D / 2, D / 2
    block(s, x0, x1, y0, y1, st)
    wh.parapet(s, x0, x1, y0, y1, H, height=.9, material=s['upper'])
    for z0, z1, out in ((H - .55, H - .4, .1), (H - .4, H - .25, .2), (H - .25, H - .1, .32), (H - .1, H + .05, .42)):
        ts.box('corbel', (x0 - out, y0 - out, z0), (x1 + out, y0, z1), 'trim')
        for x, sign in ((x0, -1), (x1, 1)):
            ts.box('corbel', (min(x, x + sign * out), y0, z0), (max(x, x + sign * out), y1, z1), 'trim')
    pw = min(5.0, W * .3)
    ts.box('name parapet', (-pw, y0, H + .9), (pw, y0 + .3, H + 1.9), s['upper'])
    ts.box('name panel', (-pw + .4, y0 - .04, H + 1.05), (pw - .4, y0, H + 1.7), 'trim')
    ts.box('name coping', (-pw - .05, y0 - .05, H + 1.9), (pw + .05, y0 + .35, H + 2.0), 'trim')
    ts.box('string course', (x0 - .05, y0 - .06, S - .02), (x1 + .05, y0, S + .2), 'trim')


BUILD = {'glass-base': glass_base, 'gabled-hall': gabled_hall, 'colonnade': colonnade, 'mono-pitch': mono_pitch,
         'canopy': canopy, 'folded-plate': folded_plate, 'main-street': main_street}

ROAD = (-29.0, -19.0)
PAVEMENT = 3.0
FORECOURT = 2.0


def place(s, cx, turned=False):
    """A Building behind its forecourt, facing the road from the north side, or from the south when turned."""
    start = len(ts.parts)
    BUILD[s['form']](s)
    ts.box('forecourt', (-s['W'] / 2, -s['D'] / 2 - FORECOURT, 0), (s['W'] / 2, -s['D'] / 2, .13), hf.flat('a8a49b'))
    reach = FORECOURT + s['D'] / 2
    cy = ROAD[0] - PAVEMENT - reach if turned else ROAD[1] + PAVEMENT + reach
    for part in ts.parts[start:]:
        if turned:
            part.rotation_euler[2] = math.pi
        part.location = (cx, cy, 0)


GAME_PITCH, GAME_FOV = 50, 60


def game(target, distance):
    """The shell's camera: 50 degrees down, a 60 degree vertical field, looking north at the target."""
    p = math.radians(GAME_PITCH)
    eye = (target[0], target[1] - math.cos(p) * distance, target[2] + math.sin(p) * distance)
    return eye, target, True


def finish(camera, shots):
    for part in ts.parts:
        ts.project_uvs(part)
    for name, eye, target, *shell in shots:
        if shell:
            camera.data.sensor_fit, camera.data.angle = 'VERTICAL', math.radians(GAME_FOV)
        else:
            camera.data.sensor_fit, camera.data.lens = 'AUTO', 40
        hf.shoot(camera, name, eye, target)


def stage():
    return hf.stage(road_y=ROAD, pavement=PAVEMENT, extent=1500)


def sheet(form, seed=None, units=1, tag=None):
    """The form at a founding run's small and large shop Lots: 24 x 16 m at 2 storeys, 52 x 24 m at 3."""
    tag = tag or form
    seed = seed if seed is not None else 301 + 97 * list(FORMS).index(form)
    hf.fresh()
    camera = stage()
    place(look(seed, 24, 16, 2, form, units), -34)
    place(look(seed + 1, 52, 24, 3, form, units), 14)
    finish(camera, [(f'{tag}-game', *game((-3, -4, 0), 150)),
                    (f'{tag}-near', (-3, -122, 48), (-3, 0, 4)),
                    (f'{tag}-street', (-62, -27, 1.7), (8, -4, 7.5))])


FOUNDING = [(20, 20, 2), (20, 24, 2), (44, 20, 2), (52, 24, 2), (56, 20, 2), (20, 24, 3), (20, 28, 3), (24, 24, 3),
            (32, 28, 3), (44, 24, 3), (52, 16, 3), (60, 16, 3)]
SHARES = [('contemporary', .55), ('mid-century', .25), ('traditional', .20)]


def street(seed=61):
    """Twelve of a founding run's shop Lots, six to a side, drawn at the target period shares with no two
    neighbors sharing a form."""
    hf.fresh()
    camera = stage()
    r = random.Random(seed)
    lots = FOUNDING[:]
    r.shuffle(lots)
    pools = {p: [f for f in FORMS if FORMS[f]['period'] == p] for p, _ in SHARES}
    for side in (0, 1):
        x, last = -140.0, None
        for W, D, st in lots[side * 6:(side + 1) * 6]:
            while True:
                period = r.choices([p for p, _ in SHARES], weights=[w for _, w in SHARES])[0]
                form = r.choice(pools[period])
                if form != last:
                    break
            last = form
            cx = x + W / 2
            place(look(r.randrange(1 << 30), W, D, st, form), cx if side == 0 else -cx, turned=side == 1)
            print('STREET', side, cx, form, W, D, st)
            x += W + r.uniform(8, 16)
    finish(camera, [('street-game', *game((0, -24, 0), 300)),
                    ('street-far', *game((0, -24, 0), 700)),
                    ('street-near', (-60, -120, 55), (-40, -10, 4)),
                    ('street-level', (-150, -27, 1.7), (-80, -8, 8))])


JOBS = {f'sheet-{f}': (lambda f=f: sheet(f)) for f in FORMS}
JOBS['sheet-units'] = lambda: sheet('glass-base', units=3, tag='units')
JOBS['street'] = street

for name, job in JOBS.items():
    if not ONLY or name in ONLY:
        job()
        print('DONE', name)

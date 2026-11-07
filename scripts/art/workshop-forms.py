"""Workshop forms: a run of three workshops at 28, 36 and 32 m frontage, 16 m deep and two storeys,
the first free on its left and the last free on its right by a side street. Each form is one period's
roof and plan. The run draws its walls, roof and trim once; each Building draws its own doors, their
paint and its signs.

These were the source for the dressed workshop forms. src/Borough.Appearance/FamilyBodyBuilder.Workshop.cs is
the reference now, and a change to a form belongs there first.

blender --background --factory-startup --python-exit-code 1 --python scripts/art/workshop-forms.py -- REPO OUT [job...]

Jobs: one per form, named as in FORMS. Each writes <form>.png from the street and <form>-roof.png from above.
"""
import importlib.util
import json
import math
import random
import sys
from pathlib import Path

ARGS = sys.argv[sys.argv.index('--') + 1:]
REPO, OUT, ONLY = Path(ARGS[0]), Path(ARGS[1]), ARGS[2:]

# apartment-forms.py runs the jobs named on the command line when imported, so it is handed none of its own.
spec = importlib.util.spec_from_file_location('af', REPO / 'scripts/art/apartment-forms.py')
af = importlib.util.module_from_spec(spec)
_argv = sys.argv
sys.argv = _argv[:_argv.index('--') + 3] + ['(none)']
spec.loader.exec_module(af)
sys.argv = _argv
hf, ts, S = af.hf, af.ts, af.S
poly = af.poly

LIBRARY = json.loads((REPO / 'src/Borough.Godot/assets/city/library/materials.json').read_text())
for _name in ('brick-wall-001', 'bricks-051', 'bricks-066', 'bricks-085', 'bricks-088', 'painted-plaster-wall',
              'sheet-metal-004', 'roofing-tiles-003', 'clay-roof-tiles-02', 'bitumen', 'corrugated-iron-02',
              'rough-block-wall', 'wood-siding-005', 'wood-siding-012', 'concrete-034', 'patterned-brick-wall-03'):
    _spec = LIBRARY['textures'][_name]
    ts.SURFACES[_name] = {'metres': _spec['tile_metres'],
                          'mean': LIBRARY['paint_mean_linear_luminance'] if _spec['paint'] else None,
                          'maps': {k: REPO / f['file'] for k, f in _spec['files'].items()}}

RUN = [28.0, 36.0, 32.0]
DEPTH = 16.0
H = 2 * S
R = ts.REVEAL
PLINTH = .3
ROOF_DEPTH = 10.0

DOORS = ['2f4a3c', '6a2f2a', '24303a', '2e5a5a', '8a7a3a', '4f5253', '7a3b52', '3a4a6a', '5a3a32']
ROLLERS = ['8d9194', '5d6a6e', '2f4a5c', '6a2f2a', 'b9b6ad', '3a4a3c', 'a8923e', '4f5b5f']
SIGNS = ['24303a', '6a2f2a', '2f4a3c', 'd9d0b8', '1f2124', '2e5a5a']

FORMS = {
    'builders-yard': {
        'note': "Victorian builder's yard, 1860-1910: a hipped brick yard house beside a boarded cart shed on posts, pantiled",
        'walls': ['bricks-085', 'brick-wall-001'], 'trim': ['e3dccb', 'd8d2c4'],
        'roof': 'clay-roof-tiles-02', 'pitch': 26,
    },
    'monitor': {
        'note': 'Engineering works, 1900-1940: side gable with a glazed clerestory monitor along the ridge, steel windows, a works chimney',
        'walls': ['bricks-051', 'bricks-085', 'brick-wall-001'], 'trim': ['d8d2c4', 'c9c6bd'],
        'roof': 'roofing-tiles-003', 'pitch': 18,
    },
    'interwar-garage': {
        'note': 'Interwar motor works, 1920-1939: Belfast-truss bow roof behind a stepped rendered parapet and name panel',
        'walls': ['painted-plaster-wall#d6d0c0', 'painted-plaster-wall#d3d6c8', 'painted-plaster-wall#d9cfb4'], 'trim': ['3a4a3c', '6a2f2a', '24303a'],
        'roof': 'corrugated-iron-02#4f5b5f', 'rise': .18,
    },
    'parapet-roller': {
        'note': 'Postwar trade workshops, 1950-1975: brown brick under a flat parapet, roller doors, high strip windows, concrete band',
        'walls': ['bricks-066', 'bricks-051', 'bricks-088#8a6a52'], 'trim': ['c9c6bd', 'b9b6ad'],
        'roof': None,
    },
    'portal-shed': {
        'note': 'Steel portal units, 1975-2005: low-pitch gable fronts in profiled sheet over a block dado, sectional doors, trade counter',
        'walls': ['sheet-metal-004#6f7f86', 'sheet-metal-004#8a9599', 'sheet-metal-004#5c6b5a', 'sheet-metal-004#b8b8b0'],
        'trim': ['2f4a5c', '6a2f2a', '3a4a3c'], 'roof': 'sheet-metal-004#5d6266', 'pitch': 8,
    },
}


def mat(name, spec):
    texture, _, paint = spec.partition('#')
    ts.materials[name] = ts.materials[hf.textured(texture, paint or None)]


def flat(name, code, rough=.85):
    ts.materials[name] = ts.materials[hf.flat(code, rough)]


def face_of(p, q, z=0.0):
    dx, dy = q[0] - p[0], q[1] - p[1]
    n = math.hypot(dx, dy)
    return ts.Face((p[0], p[1], z), (dx / n, dy / n, 0), (dy / n, -dx / n, 0)), n


def sides(b, z=0.0):
    """The four walls, anticlockwise from above, as role: (face, length)."""
    x0, x1, y0, y1 = b['x0'], b['x1'], b['y0'], b['y1']
    return {'front': face_of((x0, y0), (x1, y0), z), 'right': face_of((x1, y0), (x1, y1), z),
            'back': face_of((x1, y1), (x0, y1), z), 'left': face_of((x0, y1), (x0, y0), z)}


def beam(name, p, q, w, h, material):
    """A box of width w and height h along the segment p to q."""
    d = [q[i] - p[i] for i in range(3)]
    s = (d[1], -d[0], 0) if abs(d[0]) + abs(d[1]) > 1e-6 else (1, 0, 0)
    n = math.hypot(s[0], s[1])
    s = (s[0] / n * w / 2, s[1] / n * w / 2, 0)
    u = (s[1] * d[2] - 0, -s[0] * d[2], s[0] * d[1] - s[1] * d[0])
    m = math.sqrt(sum(c * c for c in u)) or 1
    u = tuple(c / m * h for c in u)
    if u[2] < 0:
        u = tuple(-c for c in u)
    pts = []
    for base in (p, q):
        for a, b in ((-1, 0), (1, 0), (1, 1), (-1, 1)):
            pts.append(tuple(base[i] + a * s[i] + b * u[i] for i in range(3)))
    ts.mesh(name, pts, [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)], material)


def bars(face, u, v, w, h, cols, rows, material='frame'):
    """Glazing bars across an opening."""
    for i in range(1, cols):
        x = u + w * i / cols
        face.slab('bar', x - .03, x + .03, v, v + h, R - .07, R - .02, material)
    for j in range(1, rows):
        y = v + h * j / rows
        face.slab('bar', u, u + w, y - .03, y + .03, R - .07, R - .02, material)


def arch(face, u, v, w, h, rise, ring=.28, material='trim'):
    """An arched head over a rectangular opening: wall spandrels in its top corners and a ring of voussoirs."""
    spring = v + h - rise
    arc = [(u + w / 2 - w / 2 * math.cos(a), spring + rise * math.sin(a)) for a in [k * math.pi / 16 for k in range(17)]]
    left, right = arc[:9], arc[8:]
    poly('spandrel', [face.point(a, c) for a, c in left] + [face.point(u, v + h)], 'wall')
    poly('spandrel', [face.point(a, c) for a, c in right] + [face.point(u + w, v + h)], 'wall')
    outer = [(u + w / 2 - (w / 2 + ring) * math.cos(a), spring + (rise + ring) * math.sin(a)) for a in [k * math.pi / 16 for k in range(17)]]
    for (a0, c0), (a1, c1), (b0, d0), (b1, d1) in zip(arc, arc[1:], outer, outer[1:]):
        poly('voussoir', [face.point(a0, c0, -.04), face.point(b0, d0, -.04), face.point(b1, d1, -.04), face.point(a1, c1, -.04)], material)


def spread(width, items, margin=None):
    """Lays item widths along a wall with equal gaps, returning each item's u."""
    gap = (width - sum(items)) / (len(items) + 1)
    u, out = gap, []
    for w in items:
        out.append(u)
        u += w + gap
    return out


def drop(start, prefixes):
    """Removes the parts made since start whose names begin with one of prefixes."""
    keep = ts.parts[:start]
    for part in ts.parts[start:]:
        if part.name.startswith(prefixes):
            hf.bpy.data.objects.remove(part, do_unlink=True)
        else:
            keep.append(part)
    ts.parts[:] = keep


def blank(face, n, height, name='party'):
    ts.facade(name, face, n, height, [], 'wall')


def windows_along(face, n, v, w, h, step, material='wall', cols=3, rows=4, head=None, skip=()):
    count = max(1, round(n / step))
    ops = []
    for i in range(count):
        c = n * (i + .5) / count
        if any(a <= c <= b for a, b in skip):
            continue
        if c - w / 2 > .2 and c + w / 2 < n - .2:
            ops.append((c - w / 2, v, w, h, 'window'))
    return ops


def dress(face, ops, cols=3, rows=4, head=None):
    for u, v, w, h, kind in ops:
        if kind == 'window':
            bars(face, u, v, w, h, cols, rows)
            if head == 'arch':
                arch(face, u, v, w, h, min(.45, w * .25), ring=.22)
            elif head == 'lintel':
                face.slab('lintel', u - .15, u + w + .15, v + h, v + h + .25, -.04, 0, 'trim')


def plinth(b):
    for role, (face, n) in sides(b).items():
        if role in ('left', 'right') and b[role]:
            continue
        face.slab('plinth', 0, n, 0, PLINTH, -.04, .1, 'plinth')


def rear_deck(b, yb, top, parapet=.5):
    x0, x1, y1 = b['x0'], b['x1'], b['y1']
    if yb >= y1:
        return
    ts.box('rear deck', (x0, yb, top - .2), (x1, y1, top + .05), 'membrane')
    ts.box('rear parapet', (x0, y1 - .25, top), (x1, y1, top + parapet), 'wall')
    if not b['left']:
        ts.box('rear parapet', (x0, yb, top), (x0 + .25, y1 - .25, top + parapet), 'wall')
    if not b['right']:
        ts.box('rear parapet', (x1 - .25, yb, top), (x1, y1 - .25, top + parapet), 'wall')


def ordinary_walls(b, front, back, side, height=H, head=None, cols=3, rows=4):
    """Front and back with the given openings; a free side with its own, a shared side blank."""
    F = sides(b)
    for role, ops in (('front', front), ('back', back)):
        face, n = F[role]
        ts.facade(role, face, n, height, ops, 'wall')
        dress(face, ops, cols, rows, head)
    for role in ('left', 'right'):
        face, n = F[role]
        if b[role]:
            blank(face, n, height)
        else:
            ops = side(n)
            ts.facade(role, face, n, height, ops, 'wall')
            dress(face, ops, cols, rows, head)
    return F


# --- Forms ------------------------------------------------------------------------------------





def builders_yard(b, look, rng):
    x0, x1, y0, y1 = b['x0'], b['x1'], b['y0'], b['y1']
    w = x1 - x0
    ow = 9.0
    house_left = rng.random() < .5
    hx0, hx1 = (x0, x0 + ow) if house_left else (x1 - ow, x1)
    sx0, sx1 = (x0 + ow, x1) if house_left else (x0, x1 - ow)
    HH = H + 2.0
    hb = {'x0': hx0, 'x1': hx1, 'y0': y0, 'y1': y1, 'left': b['left'] if house_left else False,
          'right': False if house_left else b['right']}
    F = sides(hb)
    for role, (f, m) in F.items():
        if role in ('left', 'right') and hb[role]:
            blank(f, m, HH)
            continue
        if role == 'front':
            ops = [(m / 2 - .55, PLINTH, 1.1, 2.6, 'door')] + [(c - .6, 1.0, 1.2, 2.0, 'window') for c in (1.8, m - 1.8)]
            ops += [(c - .6, S + 1.3, 1.2, 2.0, 'window') for c in (1.8, m / 2, m - 1.8)]
        else:
            ops = windows_along(None, m, 1.0, 1.2, 2.0, 3.2) + windows_along(None, m, S + 1.3, 1.2, 2.0, 3.2)
        ts.facade('house', f, m, HH, ops, 'wall')
        dress(f, ops, 2, 3, 'lintel')
        f.slab('plinth', 0, m, 0, PLINTH, -.04, .1, 'plinth')
        if role == 'front':
            f.slab('door hood', m / 2 - 1.0, m / 2 + 1.0, 3.0, 3.18, -.7, 0, 'trim')
            f.slab('name board', 1.0, m - 1.0, S + .3, S + 1.0, -.08, 0, 'sign')
    af.pitched(dict(hb, left=False, right=False, y1=y0 + 10.0), y0, HH, 34, hip=True)
    rear_deck(hb, y0 + 10.0, HH)
    recess = 2.4
    sw = sx1 - sx0
    sb = {'x0': sx0, 'x1': sx1, 'y0': y0, 'y1': y1,
          'left': True if house_left else b['left'], 'right': b['right'] if house_left else True}
    gf, gm = face_of((sx0, y0 + recess), (sx1, y0 + recess))
    nbay = max(2, round(sw / 4.0))
    pb = sw / nbay
    store = rng.sample(range(nbay), max(1, nbay // 3))
    ops = [(pb * (i + .5) - 1.2, PLINTH, 2.4, 2.8, 'door') for i in store]
    ts.facade('cart shed back', gf, gm, S, ops, 'board')
    for i in range(nbay + 1):
        x = sx0 + pb * i
        x = min(max(x, sx0 + .15), sx1 - .15)
        ts.box('post', (x - .14, y0 + .05, 0), (x + .14, y0 + .33, S - .3), 'timber')
    ts.box('bressumer', (sx0, y0, S - .3), (sx1, y0 + .35, S), 'timber')
    ts.box('shed soffit', (sx0, y0, S - .32), (sx1, y0 + recess, S - .28), 'board')
    for i in range(nbay):
        if i in store:
            continue
        xa = sx0 + pb * i + .5
        for k in range(rng.randint(2, 5)):
            ts.box('stacked timber', (xa, y0 + .8, .1 + k * .32), (xa + pb - 1.0, y0 + recess - .1, .1 + k * .32 + .28), 'planks')
    uf, um = face_of((sx0, y0), (sx1, y0), S)
    loft = rng.randrange(nbay)
    uops = [(pb * (loft + .5) - .7, .2, 1.4, 2.4, 'door')]
    uops += [(pb * (i + .5) - .6, 1.1, 1.2, 1.2, 'window') for i in range(nbay) if i != loft]
    ts.facade('cart shed loft', uf, um, H - S, uops, 'board')
    ts.box('hoist beam', (sx0 + pb * (loft + .5) - .09, y0 - .9, H - .45), (sx0 + pb * (loft + .5) + .09, y0 + .1, H - .27), 'timber')
    bf, bm = face_of((sx1, y1), (sx0, y1))
    ts.facade('cart shed back', bf, bm, H, windows_along(None, bm, S + .9, 1.2, 1.2, 4.0), 'board')
    for role in ('left', 'right'):
        if not sb[role]:
            x = sx0 if role == 'left' else sx1
            p0, p1 = ((x, y1), (x, y0 + recess)) if role == 'left' else ((x, y0 + recess), (x, y1))
            f, m = face_of(p0, p1)
            ts.facade('shed end', f, m, S, [], 'board')
            p0, p1 = ((x, y1), (x, y0)) if role == 'left' else ((x, y0), (x, y1))
            f, m = face_of(p0, p1, S)
            ts.facade('shed end', f, m, H - S, [], 'board')
    yb = y0 + 8.0
    af.E = .4
    start = len(ts.parts)
    af.pitched(dict(sb, y1=yb), y0, H, look['pitch'], hip=False)
    drop(start, ('chimney', 'pots'))
    af.E = .45
    rear_deck(sb, yb, H)


def monitor(b, look, rng):
    x0, x1, y0, y1 = b['x0'], b['x1'], b['y0'], b['y1']
    w, D = x1 - x0, y1 - y0
    t = math.tan(math.radians(look['pitch']))
    m, mh, E = 4.0, 1.4, .4
    yc = (y0 + y1) / 2
    zm = H + (D / 2 - m / 2) * t
    top = zm + mh + (m / 2) * t
    nb = max(3, round(w / 4.0))
    pb = w / nb
    doors = rng.sample(range(1, nb - 1), 1)
    front = []
    for i in range(nb):
        c = pb * (i + .5)
        if i in doors:
            front.append((c - 1.7, PLINTH, 3.4, 4.0, 'door'))
        else:
            front.append((c - 1.3, 1.0, 2.6, 2.6, 'window'))
        front.append((c - 1.3, S + 1.0, 2.6, 2.0, 'window'))
    side = lambda d: windows_along(None, d, 1.0, 2.6, 2.6, 4.0) + windows_along(None, d, S + 1.0, 2.6, 2.0, 4.0)
    back = windows_along(None, w, 1.0, 2.6, 2.6, 4.0) + windows_along(None, w, S + 1.0, 2.6, 2.0, 4.0)
    F = ordinary_walls(b, front, back, side, head='lintel', cols=4, rows=5)
    for role in ('front', 'back'):
        face, n = F[role]
        for i in range(nb + 1):
            u = min(max(pb * i, .3), n - .3)
            face.slab('pier', u - .3, u + .3, 0, H, -.15, 0, 'wall')
        face.slab('eaves band', 0, n, H - .5, H - .2, -.18, 0, 'trim')
    face, n = F['front']
    face.slab('name stone', n / 2 - 2.5, n / 2 + 2.5, S + .2, S + .8, -.1, 0, 'trim')
    plinth(b)
    xa, xb = x0 - (0 if b['left'] else .3), x1 + (0 if b['right'] else .3)
    ya, yb = y0 - E, y1 + E
    low = H - E * t
    poly('roof front', [(xa, ya, low), (xb, ya, low), (xb, yc - m / 2, zm), (xa, yc - m / 2, zm)], 'roof')
    poly('roof back', [(xb, yb, low), (xa, yb, low), (xa, yc + m / 2, zm), (xb, yc + m / 2, zm)], 'roof')
    for yy, out in ((yc - m / 2, (0, -1, 0)), (yc + m / 2, (0, 1, 0))):
        along = (1, 0, 0) if out[1] < 0 else (-1, 0, 0)
        f = ts.Face((x0 if out[1] < 0 else x1, yy, zm), along, out)
        ts.facade('monitor', f, w, mh, [(.3, .15, w - .6, mh - .3, 'window')], 'wall-end')
        bars(f, .3, .15, w - .6, mh - .3, max(2, round(w / 1.2)), 1)
    mE = .3
    poly('monitor roof', [(xa, yc - m / 2 - mE, zm + mh - mE * t), (xb, yc - m / 2 - mE, zm + mh - mE * t), (xb, yc, top), (xa, yc, top)], 'roof')
    poly('monitor roof', [(xb, yc + m / 2 + mE, zm + mh - mE * t), (xa, yc + m / 2 + mE, zm + mh - mE * t), (xa, yc, top), (xb, yc, top)], 'roof')
    for role, x, s in (('left', x0, -1), ('right', x1, 1)):
        pts = [(x, y0, H), (x, y1, H), (x, yc + m / 2, zm), (x, yc - m / 2, zm)]
        mon = [(x, yc - m / 2, zm), (x, yc + m / 2, zm), (x, yc + m / 2, zm + mh), (x, yc, top), (x, yc - m / 2, zm + mh)]
        if s < 0:
            pts, mon = pts[::-1], mon[::-1]
        poly('gable', pts, 'wall')
        poly('monitor end', mon, 'wall')
        if b[role]:
            ts.box('party upstand', (x - .12, y0, H - .2), (x + .12, y1, H + .3), 'wall')
    af.gutters(xa, xb, ya, yb, low)
    if rng.random() < .4:
        cx = x0 + w * rng.choice([.2, .8])
        ts.box('works chimney', (cx - .8, y1 + .2, 0), (cx + .8, y1 + 1.8, top + 9.0), 'wall')
        ts.box('chimney cap', (cx - 1.0, y1, top + 8.6), (cx + 1.0, y1 + 2.0, top + 9.0), 'trim')


def interwar_garage(b, look, rng):
    x0, x1, y0, y1 = b['x0'], b['x1'], b['y0'], b['y1']
    w, D = x1 - x0, y1 - y0
    rise = D * look['rise']
    centre = w * rng.choice([.5, .5, .3, .7])
    gw = rng.choice([5.0, 5.6, 6.2])
    front = [(centre - gw / 2, PLINTH, gw, 4.2, 'roller')]
    for c in (centre - gw / 2 - 3.2, centre + gw / 2 + 3.2):
        if 2.2 < c < w - 2.2:
            front.append((c - 1.9, .6, 3.8, 2.6, 'shop'))
    front += windows_along(None, w, S + 1.0, 2.4, 1.6, 4.0, skip=[(centre - gw / 2 - 1.5, centre + gw / 2 + 1.5)])
    side = lambda d: windows_along(None, d, 1.2, 2.4, 1.6, 4.0) + windows_along(None, d, S + 1.0, 2.4, 1.6, 4.0)
    back = windows_along(None, w, S + 1.0, 2.4, 1.6, 4.0) + [(w / 2 - 2.2, PLINTH, 4.4, 4.0, 'roller')]
    F = ordinary_walls(b, front, back, side, cols=4, rows=3)
    face, n = F['front']
    plinth(b)
    for i in range(3):
        z = H - .9 + i * .25
        face.slab('speed line', 0, n, z, z + .08, -.06, 0, 'trim')
    face.slab('parapet', 0, n, H, H + 1.3, -.02, .3, 'wall')
    face.slab('coping', 0, n, H + 1.3, H + 1.4, -.06, .32, 'trim')
    sw = min(12.0, n * .38)
    face.slab('step', centre - sw / 2, centre + sw / 2, H + 1.3, H + 2.4, -.08, .3, 'wall')
    face.slab('step', centre - sw / 4, centre + sw / 4, H + 2.4, H + 3.1, -.1, .3, 'wall')
    face.slab('name panel', centre - sw / 2 + .5, centre + sw / 2 - .5, H + 1.45, H + 2.25, -.12, 0, 'sign')
    for u in (centre - sw / 2, centre + sw / 2 - .4, 0, n - .4):
        face.slab('fin', u, u + .4, 0, H + (2.8 if abs(u + .2 - centre) < sw else 1.8), -.2, 0, 'wall')
    face.slab('canopy', centre - gw / 2 - .4, centre + gw / 2 + .4, 4.6, 4.75, -1.6, 0, 'trim')
    N = 14
    arc = [(y0 + D * k / N, H + rise * math.sin(math.pi * k / N)) for k in range(N + 1)]
    xa, xb = x0 - (0 if b['left'] else .25), x1 + (0 if b['right'] else .25)
    for (ya, za), (yb, zb) in zip(arc, arc[1:]):
        poly('bow roof', [(xa, ya, za), (xb, ya, za), (xb, yb, zb), (xa, yb, zb)], 'roof')
    for role, x, s in (('left', x0, -1), ('right', x1, 1)):
        if not b[role]:
            pts = [(x, y, z) for y, z in arc]
            poly('bow end', pts if s < 0 else pts[::-1], 'wall')
    for k in range(1, N, 3):
        ya, za = arc[k]
        ts.box('rooflight', (x0 + 2, ya - .4, za + .02), (x1 - 2, ya + .4, za + .08), 'glass')
    ts.box('back upstand', (x0, y1 - .2, H), (x1, y1, H + .3), 'wall')


def parapet_roller(b, look, rng):
    x0, x1, y0, y1 = b['x0'], b['x1'], b['y0'], b['y1']
    w = x1 - x0
    k = rng.choice([1, 2, 2, 3]) if w > 30 else rng.choice([1, 2])
    items = [('roller', rng.choice([4.0, 4.5]))] * k + [('office', 5.0)]
    rng.shuffle(items)
    us = spread(w, [iw for _, iw in items])
    front = []
    canopy = None
    for (kind, iw), u in zip(items, us):
        if kind == 'roller':
            front.append((u, PLINTH, iw, 4.4, 'roller'))
        else:
            front.append((u, PLINTH, 1.0, 2.4, 'door'))
            front.append((u + 1.5, .9, 3.4, 1.5, 'window'))
            canopy = u
    front += [(u, 5.2, w - 2 * u, .9, 'window') for u in (.8,)]
    side = lambda d: [(.8, 5.2, d - 1.6, .9, 'window'), (d / 2 - .5, PLINTH, 1.0, 2.4, 'door')]
    back = [(.8, 5.2, w - 1.6, .9, 'window'), (w * .3, PLINTH, 4.0, 4.4, 'roller')]
    F = ordinary_walls(b, front, back, side, cols=1, rows=1)
    for role, (face, n) in F.items():
        if role in ('left', 'right') and b[role]:
            continue
        face.slab('band', 0, n, 4.75, 5.0, -.08, 0, 'band')
        for o in (front if role == 'front' else []):
            if o[3] < 1 and o[2] > 3:
                for i in range(1, round(o[2] / 1.8)):
                    x = o[0] + o[2] * i / round(o[2] / 1.8)
                    face.slab('mullion', x - .04, x + .04, o[1], o[1] + o[3], R - .06, R, 'frame')
        face.slab('parapet', 0, n, H, H + .9, -.02, .25, 'wall')
        face.slab('coping', 0, n, H + .9, H + 1.0, -.08, .3, 'band')
    face, n = F['front']
    if canopy is not None:
        face.slab('canopy', canopy - .4, canopy + 1.4, 2.8, 2.95, -1.2, 0, 'band')
    sign_u = rng.uniform(1.0, max(1.0, w - 9.0))
    face.slab('sign', sign_u, sign_u + 8.0, 6.3, 6.85, -.06, 0, 'sign')
    plinth(b)
    ts.box('roof', (x0, y0, H - .2), (x1, y1, H + .02), 'membrane')
    for i in range(int(w // 8)):
        x = x0 + 4 + i * 8
        if x + 1.3 < x1:
            ts.box('rooflight', (x - 1.2, y0 + 4, H), (x + 1.2, y0 + 7, H + .35), 'glass')
            ts.box('rooflight', (x - 1.2, y1 - 7, H), (x + 1.2, y1 - 4, H + .35), 'glass')
    ts.box('roof vent', (x0 + w * .6 - .3, y0 + 7.7, H), (x0 + w * .6 + .3, y0 + 8.3, H + .9), 'metal')


def dado(face, width, ops, top):
    """A block dado along a wall, broken by every opening that rises through it."""
    cuts = sorted((o[0], o[0] + o[2]) for o in ops if o[1] < top)
    u = 0
    for a, c in cuts + [(width, width)]:
        if a - u > .05:
            face.slab('dado', u, a, 0, top, -.1, 0, 'dado')
        u = max(u, c)


def portal_shed(b, look, rng):
    x0, x1, y0, y1 = b['x0'], b['x1'], b['y0'], b['y1']
    w = x1 - x0
    t = math.tan(math.radians(look['pitch']))
    xc, top = (x0 + x1) / 2, H + w / 2 * t
    k = 2 if w > 30 and rng.random() < .6 else 1
    items = [('sectional', 5.0)] * k + [('counter', 6.0)]
    if rng.random() < .5:
        items.reverse()
    us = spread(w, [iw for _, iw in items])
    front = []
    for (kind, iw), u in zip(items, us):
        if kind == 'sectional':
            front.append((u, PLINTH, iw, 5.0, 'roller'))
        else:
            front.append((u + .2, PLINTH, 2.6, 2.7, 'shop'))
            front.append((u + 3.2, .9, 2.6, 1.6, 'window'))
            front.append((u + .2, 3.9, 5.6, 1.2, 'window'))
            counter = u
    side = lambda d: [(d / 2 - .5, PLINTH, 1.0, 2.3, 'door')]
    back = [(w / 2 - 2.5, PLINTH, 5.0, 5.0, 'roller')]
    F = ordinary_walls(b, front, back, side, cols=1, rows=1)
    for role, (face, n) in F.items():
        if role in ('left', 'right') and b[role]:
            continue
        dado(face, n, front if role == 'front' else back if role == 'back' else side(n), 2.2)
        face.slab('dado cap', 0, n, 2.2, 2.3, -.13, 0, 'trim')
    face, n = F['front']
    face.slab('canopy', counter - .3, counter + 6.3, 3.05, 3.25, -1.5, 0, 'trim')
    face.slab('sign', counter, counter + 6.0, 5.4, 6.4, -.1, 0, 'sign')
    for u, v, ow, oh, kind in front:
        if kind == 'roller':
            for i in range(1, 6):
                face.slab('panel joint', u, u + ow, v + oh * i / 6 - .02, v + oh * i / 6 + .02, R + .02, R + .05, 'metal')
            face.slab('door frame', u - .15, u + ow + .15, v + oh, v + oh + .2, -.06, 0, 'trim')
    for y, s in ((y0, -1), (y1, 1)):
        poly('gable', [(x0, y, H), (x1, y, H), (xc, y, top)][::(-s)], 'wall')
        beam('barge', (x0 - .1, y + s * .1, H - .05), (xc, y + s * .1, top + .02), .3, .25, 'trim')
        beam('barge', (xc, y + s * .1, top + .02), (x1 + .1, y + s * .1, H - .05), .3, .25, 'trim')
    ya, yb = y0 - .25, y1 + .25
    xa, xb = x0 - (0 if b['left'] else .3), x1 + (0 if b['right'] else .3)
    la, lb = H - (0 if b['left'] else .3 * t), H - (0 if b['right'] else .3 * t)
    poly('roof', [(xa, yb, la), (xa, ya, la), (xc, ya, top), (xc, yb, top)], 'roof')
    poly('roof', [(xc, yb, top), (xc, ya, top), (xb, ya, lb), (xb, yb, lb)], 'roof')
    for f0, f1 in ((.3, .38), (.62, .7)):
        for side_x, sx in ((x0, -1), (x1, 1)):
            za, zb = H + (1 - f1) * (w / 2) * t, H + (1 - f0) * (w / 2) * t
            xa_, xb_ = xc + sx * f0 * (w / 2), xc + sx * f1 * (w / 2)
            pts = [(xa_, y0 + 1.5, zb + .03), (xb_, y0 + 1.5, za + .03), (xb_, y1 - 1.5, za + .03), (xa_, y1 - 1.5, zb + .03)]
            poly('rooflight', pts if sx > 0 else pts[::-1], 'rooflight')
    ts.box('ridge cap', (xc - .25, ya, top - .05), (xc + .25, yb, top + .08), 'trim')
    for role in ('left', 'right'):
        x = x0 if role == 'left' else x1
        if b[role]:
            ts.box('valley gutter', (x - .3, ya, H - .2), (x + .3, yb, H + .02), 'metal')
        else:
            s = -1 if role == 'left' else 1
            ts.box('gutter', (min(x, x + s * .5), ya, H - .4 - .3 * t), (max(x, x + s * .5), yb, H - .15 - .3 * t), 'trim')
    ts.box('yard apron', (x0, y0 - 3.0, .1), (x1, y0, .14), 'apron')


BUILDERS = {'builders-yard': builders_yard, 'monitor': monitor, 'interwar-garage': interwar_garage,
            'parapet-roller': parapet_roller, 'portal-shed': portal_shed}


def run_look(form, run):
    """The run's draw: walls, roof and trim."""
    mat('wall', run.choice(form['walls']))
    ts.materials['wall-end'] = ts.materials['wall']
    if form.get('roof'):
        mat('roof', form['roof'])
    flat('trim', run.choice(form['trim']))
    flat('frame', '33373a', .5)
    flat('plinth', '6f6a62')
    flat('metal', '55595c', .5)
    flat('band', run.choice(['c9c6bd', 'bdb9ae']))
    flat('setts', '5f5c57', .9)
    flat('timber', '4a3a2c', .8)
    flat('planks', 'a08660', .8)
    flat('rooflight', 'a9bcc6', .15)
    flat('apron', '8f8c86', .9)
    mat('board', run.choice(['wood-siding-005', 'wood-siding-012']))
    mat('dado', 'rough-block-wall#9a958c')
    mat('membrane', 'bitumen')
    return {'pitch': form.get('pitch'), 'rise': form.get('rise')}


def building_look(form, rng):
    flat('door', rng.choice(ROLLERS if form in ('parapet-roller', 'portal-shed', 'interwar-garage') else DOORS), .6)
    flat('sign', rng.choice(SIGNS), .5)


def run(name):
    form = FORMS[name]
    hf.fresh()
    camera = hf.stage(road_y=(-14.0, -6.0))
    scene = hf.bpy.context.scene
    scene.render.resolution_x, scene.render.resolution_y = 1600, 900
    seed = sum(map(ord, name))
    look = run_look(form, random.Random(seed))
    total = sum(RUN)
    x = -total / 2
    for i, w in enumerate(RUN):
        b = {'x0': x, 'x1': x + w, 'y0': -3.0, 'y1': -3.0 + DEPTH, 'left': i > 0, 'right': i < len(RUN) - 1}
        rng = random.Random(seed * 31 + i)
        building_look(name, rng)
        BUILDERS[name](b, look, rng)
        x += w
    xe = x
    road = hf.flat('1d1e21', .9)
    pave = hf.flat('8f8c86', .9)
    ts.box('side pavement', (xe, -6.0, -.02), (xe + 3.0, 80, .12), pave)
    ts.box('side road', (xe + 3.0, -14.0, -.02), (xe + 11.0, 80, .125), road)
    ts.box('side pavement', (xe + 11.0, -6.0, -.02), (xe + 14.0, 80, .12), pave)
    for part in ts.parts:
        ts.project_uvs(part)
    hf.shoot(camera, name, (xe + 14, -50, 17), (xe - 46, 6, 1))
    hf.shoot(camera, f'{name}-roof', (-total / 2 - 30, -70, 62), (6, 6, 2))


JOBS = {f: (lambda f=f: run(f)) for f in FORMS}

for job_name, job in JOBS.items():
    if not ONLY or job_name in ONLY:
        job()
        print('DONE', job_name)

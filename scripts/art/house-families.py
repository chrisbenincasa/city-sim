"""House families: three base forms, every proportion, material and detail drawn per Building from
its seed. FamilyBodyBuilder.House.cs builds the forms, walls, doors, windows, roofs, upstands,
chimneys, bargeboards, villa bays and attic glass from this reference. Glazing bars, lintels, bands,
shutters, bay windows, door hoods, boundaries, gutters, downpipes, dormers and chimney pots are drawn
here and not yet in the game. The game draws from its own seeded generator, so seed N here is not
seed N there.

blender --background --factory-startup --python-exit-code 1 --python scripts/art/house-families.py -- REPO OUT [job...]

Jobs: street-a, street-b, sheet-terrace, sheet-gables, sheet-villa.
"""
import importlib.util
import math
import random
import sys
from pathlib import Path

import bpy
from mathutils import Vector

ARGS = sys.argv[sys.argv.index('--') + 1:]
REPO, OUT, ONLY = Path(ARGS[0]), Path(ARGS[1]), ARGS[2:]
OUT.mkdir(parents=True, exist_ok=True)
_spec = importlib.util.spec_from_file_location('ts', REPO / 'scripts/art/test-street.py')
ts = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(ts)

S = 3.5
LIB = REPO / 'src/Borough.Godot/assets/city/library'
TEXTURES = {
    'bricks-088': ([1.8, 1.8], True), 'bricks-051': ([1.25, 1.25], False), 'bricks-066': ([2.4, 2.4], False),
    'bricks-085': ([2.4, 1.2], False), 'painted-plaster-wall': ([2.0, 2.0], True), 'plaster-001': ([2.0, 2.0], True),
    'clay-roof-tiles-02': ([2.5, 2.5], False), 'roofing-tiles-007': ([3.2, 3.2], False),
    'roofing-tiles-008': ([3.2, 3.2], False),
}
for _name, (_metres, _paint) in TEXTURES.items():
    ts.SURFACES[_name] = {'metres': _metres, 'mean': .7 if _paint else None,
                          'maps': {k: LIB / f'{_name}-{k}.jpg' for k in ('albedo', 'normal', 'roughness')}}

WALLS = ['d6cfbf', 'c9b79c', 'b9ad97', '8e9b86', 'a7aeb0', 'c4a48a', 'd3c6a0', '9aa7b4', 'b48f7a', 'cfd3c6']
SHINGLES = ['353e44', '4d463f', '5a3a32', '3d4a52']
TRIMS = ['d6cfbf', 'eeeeea', '4a4f52', 'c9bfa8']
FRAMES = ['f0efe9', '2f4a3c', '33373a', 'e6dcc4', '6a2f2a']
DOORS = ['2f4a5c', '6a2f2a', '2f4a3c', '4f5253', '7a5a2c', '24303a', '8a7a3a']
FORMS = ('terrace', 'gables', 'villa')

CACHE = {}


def textured(key, paint=None):
    name = f'{key}/{paint}'
    if name not in CACHE:
        CACHE[name] = ts.surface(name, key, paint)
        ts.materials[name] = CACHE[name]
    return name


def flat(code, roughness=.85):
    name = f'flat/{code}/{roughness}'
    if name not in CACHE:
        material = bpy.data.materials.new(name)
        material.use_nodes = True
        shader = material.node_tree.nodes['Principled BSDF']
        shader.inputs['Base Color'].default_value = ts.rgb(code) + (1,)
        shader.inputs['Roughness'].default_value = roughness
        material.diffuse_color = ts.rgb(code) + (1,)
        CACHE[name] = material
        ts.materials[name] = material
    return name


def boxes(name, items, material):
    """Many boxes as one mesh."""
    vertices, faces = [], []
    for (x0, y0, z0), (x1, y1, z1) in items:
        base = len(vertices)
        vertices += [(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0),
                     (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)]
        faces += [tuple(base + i for i in f) for f in ts.BOX_FACES]
    if items:
        ts.mesh(name, vertices, faces, material)


def prism_y(name, profile, y0, y1, material):
    n = len(profile)
    vertices = [(x, y0, z) for x, z in profile] + [(x, y1, z) for x, z in profile]
    faces = [tuple(range(n)), tuple(reversed(range(n, 2 * n)))]
    faces += [(i, n + i, n + (i + 1) % n, (i + 1) % n) for i in range(n)]
    ts.mesh(name, vertices, faces, material)


# ---------------------------------------------------------------------------------------------
# The draw. Everything a Building looks like comes from its seed and its Lot.

def draw(seed, W, D, storeys, form=None):
    r = random.Random(seed)
    eligible = [f for f in FORMS if f != 'villa' or W >= 24]
    s = {'seed': seed, 'W': W, 'D': D, 'storeys': storeys, 'H': storeys * S, 'form': form or r.choice(eligible)}
    s['roof'] = r.choice([textured('clay-roof-tiles-02'), textured('roofing-tiles-007'), textured('roofing-tiles-008'),
                          textured('shingles', r.choice(SHINGLES))])
    scheme = r.choice(['painted brick', 'brick', 'render', 'brick and render'])
    natural = r.choice(['bricks-051', 'bricks-066', 'bricks-085'])
    s['scheme'] = scheme
    s['ground'] = textured(natural) if scheme == 'brick and render' else None
    per_house = r.random() < .65 and s['form'] != 'villa'
    shared = r.choice(WALLS)

    def wall():
        paint = r.choice(WALLS) if per_house else shared
        if scheme == 'painted brick':
            return textured('bricks-088', paint)
        if scheme == 'brick':
            return textured(natural)
        return textured(r.choice(['painted-plaster-wall', 'plaster-001']) if scheme == 'render' else 'painted-plaster-wall', paint)

    s['trim'] = flat(r.choice(TRIMS))
    s['frame'] = flat(r.choice(FRAMES), .5)
    s['panes'] = r.choice(['2x2', '1x2', '3x2', '2x3'])
    s['win_w'] = r.uniform(1.0, 1.3)
    s['bands'] = r.random() < .7
    s['shutters'] = flat(r.choice(DOORS)) if r.random() < .25 else None
    s['boundary'] = r.choice(['brick', 'hedge', 'railing', 'picket'])
    s['canopy'] = r.choice(['flat', 'gable', 'porch', 'none'])
    s['end'] = wall()
    if s['form'] == 'villa':
        s['pitch'] = r.uniform(24, 34)
        s['wall'] = s['end']
        s['bay_wall'] = wall() if r.random() < .5 else textured('bricks-088', r.choice(WALLS)) if scheme != 'brick' else s['end']
        s['bays'] = 1 if W < 36 else r.choice([2, 2, 3]) if W >= 48 else 2
        s['bay_w'] = r.uniform(5.5, 7.5)
        s['spacing'] = r.uniform(3.2, 4.0)
        s['bay_pitch'] = r.uniform(35, 50)
        s['door'] = flat(r.choice(DOORS))
        s['dormers'] = r.choice(['none', 'some'])
    else:
        s['pitch'] = r.uniform(25, 40) if s['form'] == 'terrace' else r.uniform(38, 52)
        target = r.uniform(6.0, 8.5)
        n = max(2, round(W / target))
        pattern = r.choice(['same', 'pairs'])
        bay_odds = r.choice([0, .3, .7])
        s['mods'] = [{'w': W / n, 'wall': wall(), 'door': flat(r.choice(DOORS)),
                      'mirror': (i % 2 == 1) if pattern == 'pairs' else False, 'bay': r.random() < bay_odds}
                     for i in range(n)]
        s['dormers'] = r.choice(['none', 'each', 'alternate']) if s['form'] == 'terrace' else None
        s['attic'] = r.choice(['single', 'pair'])
    return s


# ---------------------------------------------------------------------------------------------
# Elevations and their dressing.

def window_height():
    return min(1.9, S - 1.3)


def elevation(name, face, width, H, openings, s, material):
    """A wall with its openings, split at the first floor where the ground storey is brick."""
    if s['ground'] and H > S:
        ts.facade(f'{name} ground', face, width, S, [o for o in openings if o[1] < S], s['ground'])
        upper = ts.Face(tuple(face.point(0, S)), face.along, face.outward)
        ts.facade(f'{name} upper', upper, width, H - S, [(u, v - S, w, h, k) for u, v, w, h, k in openings if v >= S], material)
    else:
        ts.facade(name, face, width, H, openings, material)
    dress(face, width, H, openings, s)


def dress(face, width, H, openings, s):
    for u, v, w, h, kind in openings:
        if kind == 'window':
            face.slab('lintel', u - .12, u + w + .12, v + h, v + h + .24, -.05, .02, s['trim'])
            columns, rows = (int(c) for c in s['panes'].split('x'))
            bars = [((u + w * i / columns - .03, v), (u + w * i / columns + .03, v + h)) for i in range(1, columns)]
            bars += [((u, v + h * i / rows - .03), (u + w, v + h * i / rows + .03)) for i in range(1, rows)]
            for (u0, v0), (u1, v1) in bars:
                face.slab('bar', u0, u1, v0, v1, .1, .18, s['frame'])
            if s['shutters'] and v > 1.5:
                for a in (u - .6, u + w + .05):
                    face.slab('shutter', a, a + .55, v, v + h, -.06, 0, s['shutters'])
        if kind == 'door':
            face.slab('lintel', u - .15, u + w + .15, v + h, v + h + .26, -.05, .02, s['trim'])
            face.slab('fanlight', u + .06, u + w - .06, v + h - .5, v + h - .06, .19, .21, 'glass')
            face.slab('transom', u, u + w, v + h - .56, v + h - .5, .17, .22, s['frame'])
            face.slab('lamp', u + w + .25, u + w + .4, 2.0, 2.35, -.18, 0, flat('2a2c2e', .4))
            face.slab('number', u - .4, u - .15, 1.7, 1.9, -.03, 0, flat('eeeeea'))
    if s['bands']:
        for k in range(1, round(H / S)):
            face.slab('string course', -.02, width + .02, k * S - .08, k * S + .1, -.06, 0, s['trim'])
    doors = sorted((u - .05, u + w + .05) for u, v, w, h, kind in openings if kind == 'door' and v < 1)
    start = -.02
    for a, b in doors + [(width + .02, width + .02)]:
        if a - start > .05:
            face.slab('plinth band', start, a, 0, .7, -.04, 0, 'plinth')
        start = b
    face.slab('eaves band', -.02, width + .02, H - .3, H, -.07, 0, s['trim'])


def canopy(face, u, w, s, top):
    """A door hood on a street face looking down -Y."""
    style = s['canopy']
    if style == 'flat':
        face.slab('canopy', u - .45, u + w + .45, top + .32, top + .44, -1.0, 0, s['trim'])
        for x in (u - .35, u + w + .25):
            face.slab('bracket', x, x + .1, top - .05, top + .32, -.8, 0, s['trim'])
    elif style in ('gable', 'porch'):
        depth = 1.0 if style == 'gable' else 1.8
        x0, x1 = face.point(u - .5, 0)[0], face.point(u + w + .5, 0)[0]
        y = face.point(0, 0)[1]
        base = top + .35 if style == 'gable' else top + .5
        rise = (x1 - x0) / 2 * .8
        cx = (x0 + x1) / 2
        prism_y('hood', [(x0 - .1, base), (cx, base + rise), (x1 + .1, base), (x1 + .1, base + .14), (cx, base + rise + .14), (x0 - .1, base + .14)],
                y - depth, y, s['roof'])
        ts.mesh('hood gable', [(x0, y - depth + .05, base), (x1, y - depth + .05, base), (cx, y - depth + .05, base + rise)], [(0, 1, 2)], s['trim'])
        if style == 'porch':
            boxes('porch posts', [((x - .08, y - depth + .1, .3), (x + .08, y - depth + .26, base)) for x in (x0 + .1, x1 - .1)], s['trim'])
            ts.box('porch floor', (x0, y - depth, 0), (x1, y, .3), 'plinth')
        else:
            boxes('hood brackets', [((x - .05, y - .7, top - .1), (x + .05, y, base)) for x in (x0 + .15, x1 - .15)], s['trim'])


def bay_window(face, u, s, wall_material):
    """A ground-floor bay standing 0.75 m forward of a street face."""
    face.slab('bay', u, u + 2.6, .3, 3.0, -.75, 0, wall_material)
    face.slab('bay glass', u + .25, u + 2.35, .9, 2.6, -.78, -.75, 'glass')
    for x in (u + .25 + 2.1 / 3, u + .25 + 4.2 / 3):
        face.slab('bay mullion', x - .04, x + .04, .9, 2.6, -.8, -.75, s['frame'])
    face.slab('bay transom', u + .25, u + 2.35, 2.1, 2.16, -.8, -.75, s['frame'])
    face.slab('bay roof', u - .1, u + 2.7, 3.0, 3.18, -.92, 0, s['roof'])


def house_front(m, s):
    w, wh = m['w'], window_height()
    flip = (lambda u, width: w - u - width) if m['mirror'] else (lambda u, width: u)
    out = [(flip(.7, 1.0), .3, 1.0, min(2.75, S - .75), 'door')]
    if not m['bay']:
        gw = min(s['win_w'] * 1.5, w * .3)
        out.append((flip(w * .62 - gw / 2, gw), .75, gw, wh, 'window'))
    centres = [w * .28, w * .72] if w >= 6.5 else [w * .6]
    for k in range(1, s['storeys']):
        out += [(flip(c - s['win_w'] / 2, s['win_w']), k * S + .75, s['win_w'], wh, 'window') for c in centres]
    return out


def house_back(m, s):
    w, wh = m['w'], window_height()
    out = [(w * .25 - .5, .3, 1.0, 2.5, 'door'), (w * .7 - s['win_w'] / 2, .75, s['win_w'], wh, 'window')]
    for k in range(1, s['storeys']):
        out += [(w * c - s['win_w'] / 2, k * S + .75, s['win_w'], wh, 'window') for c in (.3, .7)]
    return out


def ends(depth, s):
    return [(depth * c - .5, k * S + .75, 1.0, window_height(), 'window') for k in range(s['storeys']) for c in (.3, .7)]


def street_details(s, door_xs):
    W, D = s['W'], s['D']
    y = -D / 2 - 1.8
    edges, start = [], -W / 2
    for g in sorted(door_xs) + [W / 2 + .8]:
        if g - .8 - start > .3:
            edges.append((start, g - .8))
        start = g + .8
    kind = s['boundary']
    for a, b in edges:
        if kind == 'brick':
            ts.box('front wall', (a, y - .15, 0), (b, y + .15, .75), s['ground'] or s['end'])
            ts.box('coping', (a - .03, y - .2, .75), (b + .03, y + .2, .83), s['trim'])
        elif kind == 'hedge':
            ts.box('hedge', (a, y - .4, 0), (b, y + .4, r_height(s, a)), flat('2f4a26', .95))
        elif kind == 'railing':
            ts.box('railing wall', (a, y - .15, 0), (b, y + .15, .45), s['ground'] or s['end'])
            count = max(2, int((b - a) / .14))
            boxes('railing bars', [((a + (b - a) * i / count - .015, y - .015, .45), (a + (b - a) * i / count + .015, y + .015, 1.25))
                                   for i in range(count + 1)], flat('1f2224', .4))
            ts.box('railing rail', (a, y - .03, 1.15), (b, y + .03, 1.2), flat('1f2224', .4))
        else:
            count = max(2, int((b - a) / .16))
            boxes('pickets', [((a + (b - a) * i / count - .04, y - .02, 0), (a + (b - a) * i / count + .04, y + .02, .95))
                              for i in range(count + 1)], flat('eeeeea'))
            boxes('picket rails', [((a, y + .02, z), (b, y + .05, z + .08)) for z in (.25, .7)], flat('eeeeea'))
    for x in door_xs:
        ts.box('path', (x - .6, y - .2, 0), (x + .6, -D / 2, .04), flat('b8b2a6'))


def r_height(s, a):
    return 1.0 + (hash((s['seed'], round(a))) % 5) * .08


def gutter(x0, x1, y0, y1, z):
    ts.box('gutter', (x0, y0, z - .18), (x1, y1, z), flat('3b3f42', .5))


def downpipe(x, y, H):
    ts.box('downpipe', (x - .07, y - .14, .3), (x + .07, y, H - .1), flat('3b3f42', .5))


def bind(s):
    for key, value in (('trim', s['trim']), ('frame', s['frame']), ('roof', s['roof']), ('wall', s['end']), ('wall-end', s['end'])):
        ts.materials[key] = ts.materials[value]
    ts.materials['plinth'] = ts.materials[flat('9d988e')]


# ---------------------------------------------------------------------------------------------
# The three forms.

def terrace(s):
    W, D, H = s['W'], s['D'], s['H']
    x, doors = -W / 2, []
    for m in s['mods']:
        w = m['w']
        ts.materials['door'] = ts.materials[m['door']]
        front = ts.Face((x, -D / 2, 0), (1, 0, 0), (0, -1, 0))
        openings = house_front(m, s)
        elevation('front', front, w, H, openings, s, m['wall'])
        elevation('back', ts.Face((x + w, D / 2, 0), (-1, 0, 0), (0, 1, 0)), w, H, house_back(m, s), s, m['wall'])
        door = openings[0]
        canopy(front, door[0], door[2], s, door[1] + door[3])
        doors.append(x + door[0] + .5)
        if m['bay']:
            u = w * .62 - 1.3 if not m['mirror'] else w - (w * .62 + 1.3)
            bay_window(front, u, s, s['ground'] or m['wall'])
        x += w
    elevation('left end', ts.Face((-W / 2, D / 2, 0), (0, -1, 0), (-1, 0, 0)), D, H, ends(D, s), s, s['end'])
    elevation('right end', ts.Face((W / 2, -D / 2, 0), (0, 1, 0), (1, 0, 0)), D, H, ends(D, s), s, s['end'])
    ts.plinth(W, D)
    roof = ts.gable(W, D, H, s['pitch'])
    slope = math.tan(math.radians(s['pitch']))
    low = H - .4 * slope
    edge = D / 2 + .15
    x = -W / 2
    for i, m in enumerate(s['mods']):
        if i:
            profile = [(-edge, H - .3), (edge, H - .3), (edge, roof(edge) + .25), (0, roof(0) + .25), (-edge, roof(edge) + .25)]
            ts.prism('party wall upstand', profile, x - .15, x + .15, s['ground'] or s['end'])
            ts.box('chimney', (x - .45, .4, roof(.9) - .6), (x + .45, 1.3, roof(0) + 1.1), s['ground'] or s['end'])
            boxes('pots', [((x + dx - .1, .75, roof(0) + 1.1), (x + dx + .1, .95, roof(0) + 1.45)) for dx in (-.2, .2)], flat('9a5a3a'))
            downpipe(x, -D / 2 - .06, H)
        dormer = s['dormers'] == 'each' or (s['dormers'] == 'alternate' and i % 2 == 0)
        if dormer:
            cx = x + m['w'] * .5
            yf = -D / 2 + 2.2
            zb = roof(D / 2 - 2.2) - .15
            ts.box('dormer', (cx - .85, yf, zb), (cx + .85, yf + 3.0, zb + 1.75), m['wall'])
            ts.box('dormer glass', (cx - .5, yf - .03, zb + .4), (cx + .5, yf, zb + 1.45), 'glass')
            ts.box('dormer bar', (cx - .03, yf - .05, zb + .4), (cx + .03, yf, zb + 1.45), s['frame'])
            prism_y('dormer roof', [(cx - 1.05, zb + 1.65), (cx, zb + 2.35), (cx + 1.05, zb + 1.65),
                                    (cx + 1.05, zb + 1.8), (cx, zb + 2.5), (cx - 1.05, zb + 1.8)], yf - .25, yf + 3.0, s['roof'])
        x += m['w']
    for y0, y1 in ((-D / 2 - .62, -D / 2 - .4), (D / 2 + .4, D / 2 + .62)):
        gutter(-W / 2 - .25, W / 2 + .25, y0, y1, low)
    street_details(s, doors)


def gables(s):
    W, D, H = s['W'], s['D'], s['H']
    pitch = math.radians(s['pitch'])
    x, doors = -W / 2, []
    for i, m in enumerate(s['mods']):
        w = m['w']
        ts.materials['door'] = ts.materials[m['door']]
        front = ts.Face((x, -D / 2, 0), (1, 0, 0), (0, -1, 0))
        openings = house_front(m, s)
        elevation('front', front, w, H, openings, s, m['wall'])
        elevation('back', ts.Face((x + w, D / 2, 0), (-1, 0, 0), (0, 1, 0)), w, H, house_back(m, s), s, m['wall'])
        door = openings[0]
        canopy(front, door[0], door[2], s, door[1] + door[3])
        doors.append(x + door[0] + .5)
        if m['bay']:
            u = w * .62 - 1.3 if not m['mirror'] else w - (w * .62 + 1.3)
            bay_window(front, u, s, s['ground'] or m['wall'])
        rise = w / 2 * math.tan(pitch)
        cx = x + w / 2
        for y in (-D / 2, D / 2):
            ts.mesh('gable', [(x, y, H), (x + w, y, H), (cx, y, H + rise)], [(0, 1, 2)], m['wall'])
        e, t = .35, .2
        low = H - e * math.tan(pitch)
        prism_y('pitched roof', [(x - e, low), (cx, H + rise), (x + w + e, low), (x + w + e, low + t), (cx, H + rise + t), (x - e, low + t)],
                -D / 2 - .4, D / 2 + .4, s['roof'])
        prism_y('bargeboard', [(x - e - .02, low - .12), (cx, H + rise - .12), (x + w + e + .02, low - .12), (x + w + e + .02, low + .02),
                               (cx, H + rise + .02), (x - e - .02, low + .02)], -D / 2 - .48, -D / 2 - .38, s['trim'])
        wh = min(1.3, rise * .45)
        lights = [cx - .45] if s['attic'] == 'single' else [cx - .95, cx + .15]
        for a in lights:
            ts.box('attic glass', (a, -D / 2 - .03, H + .45), (a + .8 if s['attic'] == 'pair' else a + .9, -D / 2, H + .45 + wh), 'glass')
            ts.box('attic sill', (a - .08, -D / 2 - .1, H + .38), ((a + .8 if s['attic'] == 'pair' else a + .9) + .08, -D / 2, H + .45), s['trim'])
        for gx in (x - e + .05, x + w + e - .05):
            gutter(gx - .11, gx + .11, -D / 2 - .4, D / 2 + .4, low)
        ts.box('chimney', (cx + w * .18 - .4, D * .15, H + rise * .4), (cx + w * .18 + .4, D * .15 + .8, H + rise + 1.0), s['ground'] or s['end'])
        if i:
            downpipe(x, -D / 2 - .06, H)
        x += w
    elevation('left end', ts.Face((-W / 2, D / 2, 0), (0, -1, 0), (-1, 0, 0)), D, H, ends(D, s), s, s['end'])
    elevation('right end', ts.Face((W / 2, -D / 2, 0), (0, 1, 0), (1, 0, 0)), D, H, ends(D, s), s, s['end'])
    ts.plinth(W, D)
    street_details(s, doors)


def villa(s):
    W, D, H = s['W'], s['D'], s['H']
    back_off, wh = 1.5, window_height()
    d = D - back_off
    y_front = -D / 2 + back_off
    ts.materials['door'] = ts.materials[s['door']]
    n = max(1, round(W / s['spacing']))
    columns = [-W / 2 + W * (i + .5) / n for i in range(n)]
    bay_w = s['bay_w']
    centres = {1: [0.0], 2: [-W / 4, W / 4], 3: [-W / 3, 0.0, W / 3]}[s['bays']]
    door_x = (centres[0] + centres[1]) / 2 if len(centres) > 1 else -W / 4
    if len(centres) == 3:
        door_x = (centres[0] + centres[1]) / 2
    spans = sorted([(c - bay_w / 2, c + bay_w / 2) for c in centres])
    gaps, start = [], -W / 2
    for a, b in spans + [(W / 2, W / 2)]:
        if a - start > 1.0:
            gaps.append((start, a))
        start = b
    for a, b in gaps:
        openings = []
        for c in columns:
            if a + .9 < c < b - .9 and abs(c - door_x) > 1.3:
                openings += [(c - a - s['win_w'] / 2, k * S + .75, s['win_w'], wh, 'window') for k in range(s['storeys'])]
            elif a + .9 < c < b - .9:
                openings += [(c - a - s['win_w'] / 2, k * S + .75, s['win_w'], wh, 'window') for k in range(1, s['storeys'])]
        if a < door_x < b:
            openings.append((door_x - a - .55, .3, 1.1, min(2.75, S - .75), 'door'))
        face = ts.Face((a, y_front, 0), (1, 0, 0), (0, -1, 0))
        elevation('front', face, b - a, H, openings, s, s['wall'])
        if a < door_x < b:
            canopy(face, door_x - a - .55, 1.1, s, .3 + min(2.75, S - .75))
    back = [(W / 2 - c - s['win_w'] / 2, k * S + .75, s['win_w'], wh, 'window') for c in columns for k in range(s['storeys'])]
    elevation('back', ts.Face((W / 2, D / 2, 0), (-1, 0, 0), (0, 1, 0)), W, H, back, s, s['wall'])
    elevation('left end', ts.Face((-W / 2, D / 2, 0), (0, -1, 0), (-1, 0, 0)), d, H, ends(d, s), s, s['wall'])
    elevation('right end', ts.Face((W / 2, y_front, 0), (0, 1, 0), (1, 0, 0)), d, H, ends(d, s), s, s['wall'])
    e = .45
    tan = math.tan(math.radians(s['pitch']))
    y0, y1 = y_front - e, D / 2 + e
    cy, half = (y0 + y1) / 2, (y1 - y0) / 2
    x0, x1 = -W / 2 - e, W / 2 + e
    low = H - e * tan
    ridge = low + half * tan
    r0, r1 = (min(x0 + half, 0), cy, ridge), (max(x1 - half, 0), cy, ridge)
    ts.mesh('hip roof', [(x0, y0, low), (x1, y0, low), (x1, y1, low), (x0, y1, low), r0, r1],
            [(0, 1, 5, 4), (2, 3, 4, 5), (3, 0, 4), (1, 2, 5), (3, 2, 1, 0)], s['roof'])
    for c in centres:
        bx0 = c - bay_w / 2
        bay_openings = [(bay_w / 2 - 1.4, k * S + .75, 2.8, wh, 'window') for k in range(s['storeys'])]
        elevation('bay front', ts.Face((bx0, -D / 2, 0), (1, 0, 0), (0, -1, 0)), bay_w, H, bay_openings, s, s['bay_wall'])
        ts.box('bay side', (bx0 - .1, -D / 2, 0), (bx0 + .1, y_front, H), s['bay_wall'])
        ts.box('bay side', (bx0 + bay_w - .1, -D / 2, 0), (bx0 + bay_w + .1, y_front, H), s['bay_wall'])
        bt = math.tan(math.radians(s['bay_pitch']))
        rise = bay_w / 2 * bt
        ts.mesh('bay gable', [(bx0, -D / 2, H), (bx0 + bay_w, -D / 2, H), (c, -D / 2, H + rise)], [(0, 1, 2)], s['bay_wall'])
        ee = .3
        lo = H - ee * bt
        depth = min(D / 2 + y_front + (ridge - H) / tan + 1.0, D * .6)
        prism_y('bay roof', [(bx0 - ee, lo), (c, H + rise), (bx0 + bay_w + ee, lo), (bx0 + bay_w + ee, lo + .2), (c, H + rise + .2), (bx0 - ee, lo + .2)],
                -D / 2 - .35, -D / 2 + depth, s['roof'])
        prism_y('bargeboard', [(bx0 - ee - .02, lo - .12), (c, H + rise - .12), (bx0 + bay_w + ee + .02, lo - .12), (bx0 + bay_w + ee + .02, lo + .02),
                               (c, H + rise + .02), (bx0 - ee - .02, lo + .02)], -D / 2 - .43, -D / 2 - .33, s['trim'])
        ts.box('attic glass', (c - .5, -D / 2 - .03, H + .5), (c + .5, -D / 2, H + .5 + min(1.4, rise * .5)), 'glass')
    if s['dormers'] == 'some':
        for c in columns[1::3]:
            if all(abs(c - k) > bay_w / 2 + 1.2 for k in centres) and r0[0] - 1 < c < r1[0] + 1:
                yf = y_front + 1.6
                zb = low + (yf - y0) * tan - .15
                ts.box('dormer', (c - .8, yf, zb), (c + .8, yf + 2.6, zb + 1.6), s['wall'])
                ts.box('dormer glass', (c - .5, yf - .03, zb + .35), (c + .5, yf, zb + 1.3), 'glass')
                ts.box('dormer roof', (c - .95, yf - .25, zb + 1.6), (c + .95, yf + 2.6, zb + 1.78), s['roof'])
    gutter(x0, x1, y0 - .22, y0, low)
    gutter(x0, x1, y1, y1 + .22, low)
    for x in (-W / 2 + .3, W / 2 - .3):
        downpipe(x, y_front - .06, H)
    for x in (-W / 2 + W * .28, W / 2 - W * .28):
        ts.box('chimney', (x - .45, cy - .45, ridge - 1.2), (x + .45, cy + .45, ridge + 1.1), s['ground'] or s['end'])
    ts.plinth(W, D)
    street_details(s, [door_x])


BUILD = {'terrace': terrace, 'gables': gables, 'villa': villa}


def place(s, cx, cy, turned):
    start = len(ts.parts)
    bind(s)
    BUILD[s['form']](s)
    for part in ts.parts[start:]:
        if turned:
            part.rotation_euler[2] = math.pi
        part.location = (cx, cy, 0)


# ---------------------------------------------------------------------------------------------
# Scenes.

def stage(road_y=(-5.0, 5.0), pavement=3.0, extent=400):
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_EEVEE'
    scene.render.resolution_x, scene.render.resolution_y = 1800, 1100
    scene.view_settings.view_transform = 'AgX'
    world = bpy.data.worlds.new('sky')
    world.use_nodes = True
    world.node_tree.nodes['Background'].inputs['Color'].default_value = (.55, .66, .82, 1)
    world.node_tree.nodes['Background'].inputs['Strength'].default_value = .6
    scene.world = world
    sun = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN'))
    sun.data.energy = 4.0
    sun.data.angle = .06
    sun.rotation_euler = Vector((-.5, .7, -.75)).to_track_quat('-Z', 'Y').to_euler()
    scene.collection.objects.link(sun)
    a, b = road_y
    ts.box('grass', (-extent, -extent, -.05), (extent, extent, -.02), flat('2c3a1f', .95))
    ts.box('road', (-extent, a, -.02), (extent, b, 0), flat('1d1e21', .9))
    ts.box('pavement', (-extent, a - pavement, -.02), (extent, a, .12), flat('8f8c86', .9))
    ts.box('pavement', (-extent, b, -.02), (extent, b + pavement, .12), flat('8f8c86', .9))
    camera = bpy.data.objects.new('camera', bpy.data.cameras.new('camera'))
    camera.data.lens = 40
    camera.data.clip_end = 3000
    scene.collection.objects.link(camera)
    scene.camera = camera
    return camera


def shoot(camera, name, eye, target):
    camera.location = eye
    camera.rotation_euler = (Vector(target) - Vector(eye)).to_track_quat('-Z', 'Y').to_euler()
    bpy.context.scene.render.filepath = str(OUT / f'{name}.png')
    bpy.ops.render.render(write_still=True)


def fresh():
    ts.reset({})
    CACHE.clear()


BUCKETS = [((16, 19), 4), ((20, 27), 20), ((28, 35), 18), ((36, 47), 20), ((48, 64), 35)]


def lot(r):
    (lo, hi), = r.choices([b for b, _ in BUCKETS], weights=[w for _, w in BUCKETS])
    return r.randint(lo, hi) // 4 * 4 or lo, r.choice([20, 24, 24, 28]), r.choice([2, 3])


def street(tag, seed, length=300.0):
    fresh()
    r = random.Random(seed)
    camera = stage()
    log = []
    for side, turned in ((1, False), (-1, True)):
        x = -length / 2
        while x < length / 2 - 16:
            W, D, storeys = lot(r)
            W = min(W, length / 2 - x)
            if W < 16:
                break
            s = draw(r.randrange(1 << 30), W, D, storeys)
            cx = x + W / 2
            place(s, cx if side > 0 else -cx, side * (10 + D / 2), turned)
            log.append((side, cx, s['form'], W, D, storeys, s['scheme']))
            x += W + r.uniform(3, 7)
    for part in ts.parts:
        ts.project_uvs(part)
    shoot(camera, f'{tag}-overview', (-40, -300, 260), (0, 0, 0))
    shoot(camera, f'{tag}-game-west', (-75, -125, 105), (-75, 8, 0))
    shoot(camera, f'{tag}-game-east', (75, -125, 105), (75, 8, 0))
    shoot(camera, f'{tag}-street-north', (-110, -3, 5), (-60, 16, 7))
    shoot(camera, f'{tag}-street-south', (110, 3, 5), (60, -16, 7))
    for line in log:
        print('STREET', tag, *line)


def sheet(form, seeds, W=40, D=24):
    fresh()
    camera = stage(road_y=(-205.0, -195.0))
    for i, seed in enumerate(seeds):
        col, row = i % 3, i // 3
        s = draw(seed, W, D, 2 + row % 2, form)
        place(s, (col - 1) * (W + 10), row * (D + 14), False)
    for part in ts.parts:
        ts.project_uvs(part)
    shoot(camera, f'sheet-{form}', (-30, -125, 85), (0, 20, 4))


JOBS = [('street-a', lambda: street('street-a', 11)), ('street-b', lambda: street('street-b', 23))] + \
       [(f'sheet-{f}', (lambda f=f: sheet(f, [101 + k for k in range(6)]))) for f in FORMS]

for name, job in JOBS:
    if not ONLY or name in ONLY:
        job()
        print('DONE', name, len(ts.parts))

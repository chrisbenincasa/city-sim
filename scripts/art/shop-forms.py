"""Shop-house forms: a parade of shops with flats above, ending at a street corner. Each form is one
period's roof and plan. The parade draws its walls and trim once; each shop unit draws its own
shopfront style, paint, awning and sign. src/Borough.Appearance/FamilyBodyBuilder.Shop.cs builds the
same forms for the shell.

blender --background --factory-startup --python-exit-code 1 --python scripts/art/shop-forms.py -- REPO OUT [job...]

Jobs: one per form, named as in FORMS.
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
              'sheet-metal-004', 'roofing-tiles-003', 'clay-roof-tiles-02', 'bitumen'):
    _spec = LIBRARY['textures'][_name]
    ts.SURFACES[_name] = {'metres': _spec['tile_metres'],
                          'mean': LIBRARY['paint_mean_linear_luminance'] if _spec['paint'] else None,
                          'maps': {k: REPO / f['file'] for k, f in _spec['files'].items()}}

PARADE = [12.0, 4.0, 8.0, 16.0, 8.0]
DEPTH = 14.0
STOREYS = 3
UNIT = 6.0
FASCIA = (2.95, 3.45)
ROOF_DEPTH = 10.0
SETBACK = 2.4
PLINTH = .3

SHOP_PAINTS = ['2f4a3c', '6a2f2a', '24303a', '1f2124', 'e6dcc4', '2e5a5a', '8a7a3a', '4f5253', '7a3b52', 'b8b0a0']
AWNINGS = ['7e3a2f', '2f4a3c', '3a4a6a', 'c9b79c', '5a3a32', '8a7a3a']
FLAT_DOORS = ['2f4a5c', '6a2f2a', '2f4a3c', '4f5253', '7a5a2c', '24303a']
PANELS = ['8fb3c4', 'd8c36a', 'b9c4a0', 'c78a6a']
SHUTTERS = ['3e5a44', '5c7a8a', '8a4a3a']
ZINCS = ['6b7175', '4f5b5f', '5d5a55']

# Nine draws: timber three, aluminium two, glass two, iron one, deco one.
FRONT_STYLES = ['timber'] * 3 + ['alu'] * 2 + ['glass'] * 2 + ['iron', 'deco']

# walls: the parade draws one, as a library texture with an optional paint after '#'.
# corner: how a free side on a block corner cuts the plan, and how far.
FORMS = {
    'victorian-gable': {
        'note': 'Victorian terrace shops: slate side gable, splayed corner shop door, ghost sign on the side street',
        'walls': ['brick-wall-001', 'bricks-085', 'bricks-088#c2ae82', 'bricks-088#b9a47c'], 'bands': ['e3dccb', 'd8d2c4'],
        'roof': 'gable', 'tiles': 'roofing-tiles-003', 'pitch': 38, 'upper': 'sash', 'corner': ('ground-splay', 1.6),
        'ghost': True,
    },
    'gabled-oriel': {
        'note': 'Late Victorian parade: a front gable and a first-floor oriel over each shop, clay tiles',
        'walls': ['bricks-085', 'brick-wall-001', 'bricks-088#b48f7a'], 'bands': ['e3dccb', 'd6cdb8'],
        'roof': 'gable-fronts', 'tiles': 'clay-roof-tiles-02', 'pitch': 40, 'upper': 'oriel', 'corner': ('square', 0),
    },
    'turret-corner': {
        'note': 'Edwardian corner: hipped slate, splayed corner rising into an octagonal turret',
        'walls': ['bricks-085', 'brick-wall-001', 'bricks-051'], 'bands': ['e3dccb', 'd9cfb6'],
        'roof': 'hip', 'tiles': 'roofing-tiles-003', 'pitch': 34, 'upper': 'sash', 'corner': ('splay', 2.6),
        'turret': True, 'courses': True,
    },
    'main-street': {
        'note': 'Main-street commercial block: flat roof, corbelled cornice and name panel, corner column',
        'walls': ['bricks-051', 'bricks-085', 'brick-wall-001', 'bricks-088#b9a47c'], 'bands': ['ddd6c4', 'c9bfa8'],
        'roof': 'cornice', 'upper': 'arched', 'corner': ('ground-splay', 2.2),
    },
    'mansard': {
        'note': 'Stucco parade under a slate mansard with dormers',
        'walls': ['painted-plaster-wall#d6d0c0', 'painted-plaster-wall#cfc6b0', 'painted-plaster-wall#c9c2b4',
                  'painted-plaster-wall#d3c8ae', 'painted-plaster-wall#bfc4bd'], 'bands': ['f0ece2'],
        'roof': 'mansard', 'tiles': 'roofing-tiles-003', 'upper': 'sash', 'corner': ('square', 0), 'courses': True,
    },
    'interwar-curve': {
        'note': 'Interwar parade: flat roof, rounded corner, render bands, steel windows wrapping the curve',
        'walls': ['bricks-088#b9a48a', 'bricks-088#a58f74', 'bricks-085'], 'bands': ['ece6d6', 'e2dcc8'],
        'roof': 'parapet', 'upper': 'steel', 'corner': ('round', 4.0), 'courses': True,
    },
    'sixties-canopy': {
        'note': '1960s parade: thin flat roof, a concrete canopy along every shop, ribbon windows and panels',
        'walls': ['bricks-088#c9b48a', 'bricks-088#b8ad96', 'bricks-051'], 'bands': ['c9c6bd', 'b9b6ad'],
        'roof': 'thin', 'upper': 'ribbon', 'corner': ('square', 0),
    },
    'setback-modern': {
        'note': 'Contemporary infill: brick base, top storey set back in zinc',
        'walls': ['bricks-051', 'brick-wall-001', 'bricks-088#a7aeb0', 'bricks-088#6f6a62'], 'bands': ['4a4f52', '33373a'],
        'roof': 'setback', 'upper': 'tall', 'corner': ('square', 0),
    },
    'pantile-hipped': {
        'note': 'Rendered parade under a deep-eaved hipped pantile roof, shuttered windows, iron balconies',
        'walls': ['painted-plaster-wall#d6bf96', 'painted-plaster-wall#d9c9a8', 'painted-plaster-wall#cfa98a',
                  'painted-plaster-wall#c9b79c', 'painted-plaster-wall#d4cdbd'], 'bands': ['ece6d6'],
        'roof': 'hip', 'tiles': 'clay-roof-tiles-02', 'pitch': 24, 'upper': 'shutter', 'corner': ('square', 0),
        'eaves': 1.0,
    },
}


def mat(name, key, paint=None):
    ts.materials[name] = ts.materials[hf.textured(key, paint) if key else hf.flat(paint)]


def flat(name, code, rough=.85):
    ts.materials[name] = ts.materials[hf.flat(code, rough)]


def face_of(p, q, z=0.0):
    dx, dy = q[0] - p[0], q[1] - p[1]
    n = math.hypot(dx, dy)
    return ts.Face((p[0], p[1], z), (dx / n, dy / n, 0), (dy / n, -dx / n, 0)), n


def sub(face, u):
    return ts.Face(face.point(u, 0), face.along, face.outward)


def units(w):
    n = max(1, round(w / UNIT))
    return [(w * i / n, w * (i + 1) / n) for i in range(n)]


def corner_left(b):
    return b['corner'] and not b['left']


def corner_right(b):
    return b['corner'] and not b['right']


def shared(b, role):
    return role in ('left', 'right') and b[role]


def cut_metres(b, form):
    return min(form['corner'][1], (b['x1'] - b['x0']) / 2, (b['y1'] - b['y0']) / 2)


def plan(b, form, ground):
    """The plan, anticlockwise from above, as (point, role of the edge leaving it)."""
    x0, x1, y0, y1 = b['x0'], b['x1'], b['y0'], b['y1']
    cut = form['corner'][0]
    if cut == 'ground-splay':
        cut = 'splay' if ground else 'square'
    c = cut_metres(b, form)
    out = []
    if corner_left(b) and cut == 'splay':
        out += [((x0, y0 + c), 'corner'), ((x0 + c, y0), 'front')]
    elif corner_left(b) and cut == 'round':
        out += [((x0 + c + c * math.cos(a), y0 + c + c * math.sin(a)), 'corner') for a in [math.pi + k * math.pi / 8 for k in range(4)]]
        out.append(((x0 + c, y0), 'front'))
    else:
        out.append(((x0, y0), 'front'))
    if corner_right(b) and cut == 'splay':
        out += [((x1 - c, y0), 'corner'), ((x1, y0 + c), 'right')]
    elif corner_right(b) and cut == 'round':
        out += [((x1 - c + c * math.cos(a), y0 + c + c * math.sin(a)), 'corner') for a in [1.5 * math.pi + k * math.pi / 8 for k in range(4)]]
        out.append(((x1, y0 + c), 'right'))
    else:
        out.append(((x1, y0), 'right'))
    out += [((x1, y1), 'back'), ((x0, y1), 'left')]
    return out


def edges(pl):
    return [(pl[i][0], pl[(i + 1) % len(pl)][0], pl[i][1]) for i in range(len(pl))]


def top_of(b, form):
    """The storeys the walls rise to: one short of the Building under a mansard or set-back top."""
    return (b['storeys'] - (1 if form['roof'] in ('mansard', 'setback') else 0)) * S


def upper_size(form):
    return {'arched': (1.1, 2.0, .7), 'steel': (2.2, 1.5, .95), 'ribbon': (0, 1.45, .95), 'tall': (1.5, 2.6, .15),
            'shutter': (1.1, 1.8, .8)}.get(form['upper'], (1.1, 1.9, .75))


def dress_window(face, u, v, w, h, form):
    """Heads, hoods, rails and shutters around one upper window."""
    kind = form['upper']
    if kind in ('sash', 'oriel'):
        face.slab('lintel', u - .12, u + w + .12, v + h, v + h + .22, -.04, 0, 'trim')
        face.slab('sash rail', u, u + w, v + h * .5 - .03, v + h * .5 + .03, ts.REVEAL - .07, ts.REVEAL - .02, 'frame')
    elif kind == 'arched':
        face.slab('hood', u - .15, u + w + .15, v + h, v + h + .3, -.08, 0, 'trim')
        face.slab('keystone', u + w / 2 - .14, u + w / 2 + .14, v + h - .05, v + h + .42, -.1, 0, 'trim')
        face.slab('sash rail', u, u + w, v + h * .55 - .03, v + h * .55 + .03, ts.REVEAL - .07, ts.REVEAL - .02, 'frame')
    elif kind == 'shutter':
        for a in (u - .55, u + w):
            face.slab('shutter', a + .03, a + .52, v - .02, v + h + .02, -.06, -.02, 'shutter')
        face.slab('rail', u - .15, u + w + .15, v + .95, v + 1.0, -.45, -.4, 'metal')
        face.slab('balconette', u - .2, u + w + .2, v - .12, v, -.5, 0, 'trim')
    elif kind == 'tall':
        face.slab('rail', u + .05, u + w - .05, v + .95, v + 1.0, ts.REVEAL - .06, ts.REVEAL - .02, 'metal')


def upper_wall(name, face, width, height, form, n=None):
    """An upper wall from the first floor to the eaves, each window dressed in the form's period."""
    rows = max(1, round(height / S))
    w, h, sill = upper_size(form)
    if form['upper'] == 'ribbon':
        ops = [(.4, k * S + sill, width - .8, h, 'window') for k in range(rows)] if width > 1.6 else []
    else:
        n = n if n is not None else max(1, round(width / 2.9))
        w = min(w, width / n - .6)
        ops = [(width * (i + .5) / n - w / 2, k * S + sill, w, h, 'window') for k in range(rows) for i in range(n)] if w > .5 else []
    ts.facade(name, face, width, height, ops, 'wall')
    for u, v, ow, oh, _ in ops:
        dress_window(face, u, v, ow, oh, form)
    if form['upper'] == 'ribbon':
        for k in range(rows):
            face.slab('spandrel', .4, width - .4, k * S + .1, k * S + sill, -.02, 0, 'panel')


def shop_unit(face, width, form, rng, door_left, state):
    """One shopfront across a ground-storey stretch, in a style and paint of its own."""
    style = rng.choice(FRONT_STYLES)
    paint = rng.choice(SHOP_PAINTS)
    state['paint'] = paint
    flat('door', paint, .6)
    flat('frame', paint if style == 'timber' else 'c8ccce' if style == 'alu' else '33373a', .5)
    flat('shopfront', paint, .6)
    flat('fascia', '141618' if style == 'deco' else paint, .4 if style == 'deco' else .6)
    p = .3 if style in ('timber', 'iron') else .15
    dw = min(1.1, max(.9, width * .22))
    riser = {'timber': .7, 'iron': .45, 'deco': .55, 'alu': .5, 'glass': .3}[style]
    top = {'timber': 2.9, 'iron': 3.15, 'deco': 2.85, 'alu': 2.75, 'glass': 3.15}[style]
    du = p + .05 if door_left else width - p - .05 - dw
    su0, su1 = (du + dw + .15, width - p - .05) if door_left else (p + .05, du - .15)
    window = su1 - su0 > .6
    ops = [(du, PLINTH, dw, top - PLINTH - .05, 'door')]
    if window:
        ops.append((su0, riser, su1 - su0, top - riser, 'shop'))
    ts.facade('shop', face, width, S, ops, 'wall')
    face.slab('door glass', du + .15, du + dw - .15, 1.2, top - .5, ts.REVEAL + .03, ts.REVEAL + .04, 'glass')
    face.slab('fanlight', du, du + dw, top - .4, top - .35, ts.REVEAL, ts.REVEAL + .05, 'frame')
    if window:
        face.slab('riser', su0 - .02, su1 + .02, PLINTH, riser + .02, -.05, ts.REVEAL, 'fascia' if style == 'deco' else 'shopfront')
        face.slab('transom', su0, su1, top - .45, top - .38, ts.REVEAL - .08, ts.REVEAL, 'frame')
    if style == 'timber':
        for a in (0, width - p):
            face.slab('pilaster', a, a + p, PLINTH, FASCIA[0], -.12, 0, 'shopfront')
            face.slab('console', a + .02, a + p - .02, FASCIA[0] - .2, FASCIA[1] + .1, -.32, 0, 'shopfront')
        post = du + dw if door_left else du - .15
        face.slab('post', post, post + .15, PLINTH, top, -.06, 0, 'shopfront')
        face.slab('fascia', p, width - p, FASCIA[0], FASCIA[1], -.2, 0, 'fascia')
        face.slab('cornice', 0, width, FASCIA[1], FASCIA[1] + .12, -.35, 0, 'shopfront')
    elif style == 'iron':
        for a in (0, width - .22):
            face.slab('iron column', a, a + .22, PLINTH, 3.35, -.1, 0, 'frame')
        face.slab('iron beam', 0, width, 3.2, 3.4, -.12, 0, 'frame')
        if form['roof'] != 'thin':
            face.slab('sign board', .5, width - .5, S + .05, S + .6, -.08, 0, 'fascia')
    elif style == 'deco':
        face.slab('fascia', 0, width, 2.9, 3.4, -.12, 0, 'fascia')
        face.slab('chrome', 0, width, 2.86, 2.9, -.13, 0, 'chrome')
    elif style == 'alu':
        face.slab('fascia', 0, width, 2.8, 3.05, -.08, 0, 'fascia')
    else:
        face.slab('fascia', .15, width - .15, 3.2, 3.4, -.06, 0, 'fascia')
    if form['roof'] != 'thin' and style != 'deco' and rng.random() < .5:
        flat('awning', rng.choice(AWNINGS), .9)
        a0, a1 = .25, width - .25
        z = FASCIA[0] - .05 if style == 'timber' else 2.75
        poly('awning', [face.point(a0, z), face.point(a1, z), face.point(a1, z - .55, -1.5), face.point(a0, z - .55, -1.5)], 'awning')
        face.slab('valance', a0, a1, z - .8, z - .55, -1.52, -1.48, 'awning')
    if form['roof'] != 'thin' and rng.random() < .35:
        a = width - .1 if door_left else .1
        face.slab('sign bracket', a - .03, a + .03, 3.9, 3.95, -.8, 0, 'metal')
        face.slab('hanging sign', a - .03, a + .03, 3.3, 3.85, -.75, -.25, 'fascia')


def side_ground(face, width, wrap, front_first, state):
    """A free side wall's ground storey: a shop window turning a corner, then the flats' door."""
    def at(u, span):
        return u if front_first else width - u - span
    shop = min(3.2, width * .3)
    door = width - 3.2 > (shop + .9 if wrap else .3)
    ops = []
    if wrap:
        ops.append((at(.5, shop), .7, shop, 2.2, 'shop'))
    if door:
        ops.append((at(width - 3.2, 1.0), PLINTH, 1.0, 2.3, 'door'))
        ops.append((at(width - 1.8, .9), 1.0, .9, 1.4, 'window'))
    ts.facade('side shop', face, width, S, ops, 'wall')
    if door:
        u = at(width - 3.5, 1.6)
        face.slab('flat door hood', u, u + 1.6, 2.75, 2.9, -.6, 0, 'trim')
    if wrap:
        flat('shopfront', state['paint'], .6)
        u = at(.2, shop + .6)
        face.slab('fascia', u, u + shop + .6, FASCIA[0], FASCIA[1], -.2, 0, 'shopfront')
        u = at(0, shop + 1.0)
        face.slab('cornice', u, u + shop + 1.0, FASCIA[1], FASCIA[1] + .12, -.35, 0, 'shopfront')


def corner_ground(face, width, state):
    """A splayed or rounded corner's ground storey: a door where it is wide enough, display glass where not."""
    flat('shopfront', state['paint'], .6)
    if width > 1.8:
        flat('door', state['paint'], .6)
        dw = min(1.2, width - .7)
        ts.facade('corner door', face, width, S, [((width - dw) / 2, PLINTH, dw, 2.55, 'door')], 'wall')
        face.slab('door glass', (width - dw) / 2 + .15, (width + dw) / 2 - .15, 1.2, 2.45, ts.REVEAL + .03, ts.REVEAL + .04, 'glass')
    else:
        flat('frame', state['paint'], .5)
        ts.facade('corner glass', face, width, S, [(.1, .7, width - .2, 2.1, 'shop')], 'wall')
    face.slab('fascia', 0, width, FASCIA[0], FASCIA[1], -.2, 0, 'shopfront')


def ground_splay_corners(b, form):
    """A soffit and a column, or a corbel, under the upper storeys where a splay cuts only the ground storey."""
    c = cut_metres(b, form)
    y0 = b['y0']
    for here, ex, sign in ((corner_left(b), b['x0'], -1), (corner_right(b), b['x1'], 1)):
        if not here:
            continue
        ix = ex - sign * c
        poly('soffit', [(ix, y0, S - .02), (ex, y0, S - .02), (ex, y0 + c, S - .02)], 'trim')
        if form['roof'] == 'cornice':
            a, bb = ex - sign * .45, ex - sign * .05
            ts.box('corner column', (min(a, bb), y0 + .05, PLINTH), (max(a, bb), y0 + .45, S - .02), 'frame')
            a, bb = ex - sign * .55, ex + sign * .05
            ts.box('column base', (min(a, bb), y0 - .05, 0), (max(a, bb), y0 + .55, .45), 'trim')
        else:
            a = ex - sign * .35
            ts.box('corbel', (min(a, ex), y0, S - .7), (max(a, ex), y0 + .35, S - .02), 'trim')


def parade_parts(look):
    flat('door', look['door'], .6)
    flat('frame', 'ece8dc', .5)


def walls(b, form, rng, look):
    """Every wall of one Building: shopfronts on the street, flats above, party walls where attached."""
    top = top_of(b, form)
    state = {'paint': SHOP_PAINTS[0]}
    unit = 0
    for p, q, role in edges(plan(b, form, ground=True)):
        face, n = face_of(p, q)
        if role == 'front':
            for u0, u1 in units(n):
                shop_unit(sub(face, u0), u1 - u0, form, rng, unit % 2 == 0, state)
                unit += 1
        elif role == 'corner':
            corner_ground(face, n, state)
        elif role in ('left', 'right') and not b[role]:
            parade_parts(look)
            side_ground(face, n, corner_left(b) if role == 'left' else corner_right(b), role == 'right', state)
        elif role == 'back':
            parade_parts(look)
            ts.facade('back ground', face, n, S, [(1.0, PLINTH, 1.0, 2.3, 'door'), (n - 2.6, .95, 1.4, 1.5, 'window')], 'wall')
        else:
            ts.facade('party ground', face, n, S, [], 'wall')
        if not shared(b, role):
            face.slab('plinth', 0, n, 0, PLINTH, -.04, .1, 'plinth')
    parade_parts(look)
    if form['corner'][0] == 'ground-splay':
        ground_splay_corners(b, form)
    high = top - S
    if high <= 0:
        return
    upper = plan(b, form, ground=False)
    for p, q, role in edges(upper):
        face, n = face_of(p, q, S)
        if role == 'front' and form['upper'] == 'oriel':
            for u0, u1 in units(n):
                oriel(b, sub(face, u0), u1 - u0, high, form)
        elif role == 'front':
            for u0, u1 in units(n):
                upper_wall('upper front', sub(face, u0), u1 - u0, high, form)
        elif role == 'corner':
            upper_wall('upper corner', face, n, high, form, n=1)
        elif role in ('left', 'right') and not b[role]:
            upper_wall('upper side', face, n, high, form, n=max(1, round(n / 4.5)))
        elif role == 'back':
            upper_wall('upper back', face, n, high, form)
        else:
            ts.facade('party upper', face, n, high, [], 'wall')
    if form.get('courses'):
        for z in (S - .02, top - .35):
            for p, q, role in edges(upper):
                if not shared(b, role):
                    face, n = face_of(p, q)
                    face.slab('string course', 0, n, z, z + .25, -.06, 0, 'trim')
    if form.get('ghost'):
        if corner_right(b):
            ts.box('ghost sign', (b['x1'], b['y0'] + 4.0, S + .6), (b['x1'] + .02, b['y1'] - 2.0, top - .5), 'ghost')
        if corner_left(b):
            ts.box('ghost sign', (b['x0'] - .02, b['y0'] + 4.0, S + .6), (b['x0'], b['y1'] - 2.0, top - .5), 'ghost')


def oriel(b, face, width, height, form):
    """A canted oriel over a shop on the first floor, with sashes on the storeys above."""
    c = face.origin[0] + width / 2
    bw = min(3.0, width - .8)
    if bw >= 1.6:
        af.canted(c, bw, b['y0'], b['y0'] - .65, S + .2, 2 * S - .3, [(S + .75, S + 2.75)])
        ts.box('oriel corbel', (c - bw / 2 + .2, b['y0'] - .5, S - .15), (c + bw / 2 - .2, b['y0'], S + .2), 'trim')
    if height > S + .1:
        ts.facade('oriel wall', face, width, S, [], 'wall')
        upper_wall('upper front', ts.Face((face.origin[0], face.origin[1], face.origin[2] + S), face.along, face.outward),
                   width, height - S, form)
    else:
        ts.facade('oriel wall', face, width, height, [], 'wall')


def roof_back(b):
    """Where a pitched roof's back slope ends. Behind it a deep Building's rear range has a flat roof."""
    return min(b['y1'], b['y0'] + ROOF_DEPTH)


def rear_deck(b, H):
    yb = roof_back(b)
    if yb >= b['y1']:
        return
    x0, x1, y1 = b['x0'], b['x1'], b['y1']
    ts.box('rear deck', (x0, yb, H - .2), (x1, y1, H + .05), 'membrane')
    ts.box('rear parapet', (x0, y1 - .25, H), (x1, y1, H + .5), 'wall')
    if not b['left']:
        ts.box('rear parapet', (x0, yb, H), (x0 + .25, y1 - .25, H + .5), 'wall')
    if not b['right']:
        ts.box('rear parapet', (x1 - .25, yb, H), (x1, y1 - .25, H + .5), 'wall')


def hips(b):
    """A hip fits a Building free at both ends, or one wide enough that its hips meet behind the party wall."""
    return (not b['left'] and not b['right']) or (b['x1'] - b['x0']) >= roof_back(b) - b['y0']


def flat_roof(b, form, H):
    """A membrane deck behind a parapet, a corbelled cornice or a thin fascia, with a roof hatch."""
    pl = plan(b, form, ground=False)
    ts.mesh('roof deck', [(x, y, H + .02) for (x, y), _ in pl], [tuple(range(len(pl)))], 'membrane')
    kind = form['roof']
    if kind == 'thin':
        for p, q, role in edges(pl):
            face, n = face_of(p, q)
            face.slab('roof edge', 0, n, H - .35, H + .25, 0 if shared(b, role) else -.5, 0, 'trim')
            if role == 'front' or (role in ('left', 'right') and not b[role]):
                face.slab('canopy', 0, n, 3.05, 3.3, -2.0, 0, 'trim')
        if corner_right(b):
            ts.box('canopy corner', (b['x1'], b['y0'] - 2.0, 3.05), (b['x1'] + 2.0, b['y0'], 3.3), 'trim')
        if corner_left(b):
            ts.box('canopy corner', (b['x0'] - 2.0, b['y0'] - 2.0, 3.05), (b['x0'], b['y0'], 3.3), 'trim')
    else:
        P = .9 if kind == 'cornice' else .7
        for p, q, role in edges(pl):
            face, n = face_of(p, q)
            face.slab('parapet', 0, n, H, H + P, 0, .25, 'wall')
            face.slab('coping', 0, n, H + P, H + P + .08, 0 if shared(b, role) else -.05, .3, 'trim')
            street = role in ('front', 'corner') or (role in ('left', 'right') and not b[role])
            if kind == 'cornice' and street:
                for z0, z1, out, m in ((H - .55, H - .4, .1, 'wall'), (H - .4, H - .25, .2, 'wall'),
                                       (H - .25, H - .1, .32, 'wall'), (H - .1, H + .05, .42, 'trim')):
                    face.slab('corbel', 0, n, z0, z1, -out, 0, m)
            if kind == 'parapet' and role == 'corner':
                face.slab('corner parapet', 0, n, H + P, H + P + 1.4, 0, .25, 'wall')
                face.slab('corner coping', 0, n, H + P + 1.4, H + P + 1.5, -.05, .3, 'trim')
        w = b['x1'] - b['x0']
        if kind == 'cornice' and w >= 8:
            c, pw, y0, t = (b['x0'] + b['x1']) / 2, min(4.0, w * .35), b['y0'], H + P
            ts.box('name parapet', (c - pw, y0, t), (c + pw, y0 + .3, t + .9), 'wall')
            ts.box('name panel', (c - pw + .4, y0 - .04, t + .15), (c + pw - .4, y0, t + .75), 'trim')
            ts.box('name coping', (c - pw - .05, y0 - .05, t + .9), (c + pw + .05, y0 + .35, t + 1.0), 'trim')
        if kind == 'parapet':
            c = cut_metres(b, form)
            for here, ex, sign in ((corner_left(b), b['x0'], -1), (corner_right(b), b['x1'], 1)):
                if here:
                    px, py = ex - sign * c * .4, b['y0'] + c * .4
                    ts.box('flagpole', (px - .04, py - .04, H + P), (px + .04, py + .04, H + P + 5.0), 'metal')
    cx, cy = (b['x0'] + b['x1']) / 2, (b['y0'] + b['y1']) / 2
    ts.box('roof hatch', (cx - .8, cy + 1.5, H), (cx + .8, cy + 2.5, H + .5), 'metal')


def mansard(b, eaves):
    """The top storey inside a slate mansard, a dormer over each upper window front and back."""
    x0, x1, y0, y1 = b['x0'], b['x1'], b['y0'], b['y1']
    crown, k = eaves + S, 1.1
    ka = 0 if b['left'] else k
    kb = 0 if b['right'] else k
    xa, xb = x0 - (0 if b['left'] else .08), x1 + (0 if b['right'] else .08)
    poly('mansard front', [(xa, y0 - .08, eaves), (xb, y0 - .08, eaves), (x1 - kb, y0 + k, crown), (x0 + ka, y0 + k, crown)], 'roof')
    poly('mansard back', [(xb, y1 + .08, eaves), (xa, y1 + .08, eaves), (x0 + ka, y1 - k, crown), (x1 - kb, y1 - k, crown)], 'roof')
    for attached, x, kk, out in ((b['left'], x0, ka, -1), (b['right'], x1, kb, 1)):
        if attached:
            poly('mansard end', [(x, y0, eaves), (x, y1, eaves), (x, y1 - k, crown), (x, y0 + k, crown)], 'wall-end')
            af.upstand(x, out, [(y0 - .1, eaves - .3), (y1 + .1, eaves - .3), (y1 + .1, eaves + .25),
                                (y1 - k + .05, crown + .3), (y0 + k - .05, crown + .3), (y0 - .1, eaves + .25)])
        else:
            xs = x + out * .08
            pts = [(xs, y1 + .08, eaves), (xs, y0 - .08, eaves), (x - out * kk, y0 + k, crown), (x - out * kk, y1 - k, crown)]
            poly('mansard side', pts if out < 0 else pts[::-1], 'roof')
    ts.box('mansard deck', (x0 + ka, y0 + k, crown - .1), (x1 - kb, y1 - k, crown + .1), 'membrane')
    ca = x0 if b['left'] else x0 + ka - .05
    cb = x1 if b['right'] else x1 - kb + .05
    ts.box('crown coping', (ca, y0 + k - .05, crown + .1), (cb, y0 + k + .2, crown + .22), 'trim')
    ts.box('crown coping', (ca, y1 - k - .2, crown + .1), (cb, y1 - k + .05, crown + .22), 'trim')
    ts.box('cornice', (x0, y0 - .45, eaves - .35), (x1, y0, eaves), 'trim')
    ts.box('back cornice', (x0, y1, eaves - .25), (x1, y1 + .3, eaves), 'trim')
    centres = []
    for u0, u1 in units(x1 - x0):
        n = max(1, round((u1 - u0) / 2.9))
        centres += [x0 + u0 + (u1 - u0) * (i + .5) / n for i in range(n)]
    dw = .6
    for face_y, sign in ((y0, 1), (y1, -1)):
        for c in centres:
            glass, behind = face_y + sign * .35, face_y + sign * (k + .3)
            lo, hi = sorted((glass, behind))
            pane = glass - sign * .03
            ts.box('dormer cheeks', (c - dw, lo, eaves + .25), (c + dw, hi, eaves + 2.5), 'trim')
            ts.box('dormer glass', (c - dw + .15, pane - .01, eaves + .55), (c + dw - .15, pane + .01, eaves + 2.2), 'glass')
            ts.box('dormer hood', (c - dw - .15, lo - .12, eaves + 2.45), (c + dw + .15, hi, eaves + 2.65), 'roof')
    for kx in (1, 3):
        af.chimney(x0 + (x1 - x0) * kx / 4, (y0 + y1) / 2, crown - .2, crown + 1.4)


def setback_top(b, eaves):
    """The top storey 2.4 m behind a glazed terrace, clad in zinc."""
    x0, x1, y0, y1 = b['x0'], b['x1'], b['y0'], b['y1']
    w, back = x1 - x0, y0 + SETBACK
    ts.box('terrace deck', (x0 + .1, y0 + .1, eaves - .2), (x1 - .1, back, eaves + .05), 'membrane')
    ts.box('terrace edge', (x0, y0 - .2, eaves - .3), (x1, y0 + .15, eaves + .12), 'trim')
    ts.box('terrace glass', (x0 + .1, y0 + .02, eaves + .12), (x1 - .1, y0 + .06, eaves + 1.1), 'glass')
    n = max(1, round(w / 2.6))
    ops = [(w * (i + .5) / n - .8, .15, 1.6, 2.6, 'window') for i in range(n)]
    ts.facade('top front', ts.Face((x0, back, eaves), (1, 0, 0), (0, -1, 0)), w, S, ops, 'zinc')
    ts.facade('top back', ts.Face((x1, y1, eaves), (-1, 0, 0), (0, 1, 0)), w, S, [], 'zinc')
    ts.facade('top side', ts.Face((x0, y1, eaves), (0, -1, 0), (-1, 0, 0)), y1 - back, S, [], 'zinc')
    ts.facade('top side', ts.Face((x1, back, eaves), (0, 1, 0), (1, 0, 0)), y1 - back, S, [], 'zinc')
    roof = eaves + S
    ts.box('top deck', (x0, back, roof - .1), (x1, y1, roof + .02), 'membrane')
    la = x0 if b['left'] else x0 - .15
    lb = x1 if b['right'] else x1 + .15
    ts.box('top coping', (la, back - .15, roof - .1), (lb, y1 + .15, roof + .2), 'zinc')


def turret(b, H, right):
    """An octagonal turret over a splayed corner, a storey above the eaves, under a slate spire."""
    c = cut_metres(b, FORMS['turret-corner'])
    sign = 1 if right else -1
    ex = b['x1'] if right else b['x0']
    cx, cy = ex - sign * (c / 2 - .25), b['y0'] + c / 2 - .25
    r = c / 2 + .35
    z0, z1, z2 = H - .3, H + 3.0, H + 7.2
    ring = [(cx + r * math.cos(math.pi / 8 + k * math.pi / 4), cy + r * math.sin(math.pi / 8 + k * math.pi / 4)) for k in range(8)]
    for p, q in zip(ring, ring[1:] + ring[:1]):
        face, n = face_of(p, q, z0)
        ts.facade('turret wall', face, n, z1 - z0, [(.25, .9, n - .5, 1.7, 'window')], 'wall')
        face.slab('turret band', 0, n, z1 - z0 - .3, z1 - z0, -.08, 0, 'trim')
        ep, eq = [(cx + (x - cx) * 1.12, cy + (y - cy) * 1.12) for x, y in (p, q)]
        poly('spire', [(ep[0], ep[1], z1), (eq[0], eq[1], z1), (cx, cy, z2)], 'roof')
    ts.box('finial', (cx - .06, cy - .06, z2), (cx + .06, cy + .06, z2 + 1.0), 'metal')


def building(b, form, rng, look):
    H = b['storeys'] * S
    walls(b, form, rng, look)
    kind = form['roof']
    if kind in ('gable', 'gable-fronts', 'hip'):
        rear_deck(b, H)
        roofed = dict(b, y1=roof_back(b))
        af.E = form.get('eaves', .45)
        af.pitched(roofed, b['y0'], H, form['pitch'], hip=kind == 'hip' and hips(b))
        if kind == 'gable-fronts':
            t = math.tan(math.radians(form['pitch']))
            for u0, u1 in units(b['x1'] - b['x0']):
                af.front_gable((b['x0'] + (u0 + u1) / 2, (u1 - u0) / 2 - af.E - .1, b['y0']), b['y0'], H, H - af.E * t, t)
        af.E = .45
        if form.get('turret'):
            if corner_right(b):
                turret(b, H, right=True)
            if corner_left(b):
                turret(b, H, right=False)
    elif kind in ('parapet', 'cornice', 'thin'):
        flat_roof(b, form, H)
    elif kind == 'mansard':
        mansard(b, top_of(b, form))
    elif kind == 'setback':
        setback_top(b, top_of(b, form))


def parade_look(form, run):
    """The parade's draw: its walls, trim, flats' doors, panels, shutters and zinc."""
    wall = run.choice(form['walls'])
    texture, _, paint = wall.partition('#')
    mat('wall', texture, paint or None)
    ts.materials['wall-end'] = ts.materials['wall']
    mat('roof', form.get('tiles', 'roofing-tiles-003'))
    flat('trim', run.choice(form['bands']))
    look = {'door': run.choice(FLAT_DOORS)}
    flat('panel', run.choice(PANELS), .6)
    flat('shutter', run.choice(SHUTTERS), .7)
    mat('zinc', 'sheet-metal-004', run.choice(ZINCS))
    flat('plinth', '8f8c86')
    flat('metal', '55595c', .5)
    flat('chrome', 'c9ccce', .2)
    flat('ghost', 'c9b994', .95)
    mat('membrane', 'bitumen')
    return look


def parade(name):
    form = FORMS[name]
    hf.fresh()
    camera = hf.stage(road_y=(-14.0, -6.0))
    scene = hf.bpy.context.scene
    scene.render.resolution_x, scene.render.resolution_y = 1600, 900
    seed = sum(map(ord, name))
    look = parade_look(form, random.Random(seed))
    total = sum(PARADE)
    x = -total / 2
    for i, w in enumerate(PARADE):
        last = i == len(PARADE) - 1
        b = {'x0': x, 'x1': x + w, 'y0': -3.0, 'y1': -3.0 + DEPTH, 'storeys': STOREYS,
             'left': i > 0, 'right': not last, 'corner': last}
        building(b, form, random.Random(seed * 31 + i), look)
        x += w
    xe = x
    road = hf.flat('1d1e21', .9)
    pave = hf.flat('8f8c86', .9)
    ts.box('side pavement', (xe, -6.0, -.02), (xe + 3.0, 80, .12), pave)
    ts.box('side road', (xe + 3.0, -14.0, -.02), (xe + 11.0, 80, .125), road)
    ts.box('side pavement', (xe + 11.0, -6.0, -.02), (xe + 14.0, 80, .12), pave)
    for part in ts.parts:
        ts.project_uvs(part)
    hf.shoot(camera, name, (xe + 16, -40, 13), (xe - 16, 2, 5.5))


JOBS = {f: (lambda f=f: parade(f)) for f in FORMS}

for job_name, job in JOBS.items():
    if not ONLY or job_name in ONLY:
        job()
        print('DONE', job_name)

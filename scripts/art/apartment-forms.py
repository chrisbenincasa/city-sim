"""Apartment forms: the corridor, two-unit and walk-up apartments, varied by roof and plan. Each form
draws a Building's whole frontage. A side shared with a neighbor is blank, and a roof stops at it
behind a party upstand. Walls lay each family's bay rules from appearance/test-street/preset.toml.

FamilyBodyBuilder.Apartment.cs is the reference; this script follows it. The parapet form is the
base body, which this script approximates.

blender --background --factory-startup --python-exit-code 1 --python scripts/art/apartment-forms.py -- REPO OUT [job...]

Jobs: tile-<form> and detached-<form> for each form.
"""
import importlib.util
import math
import sys
from pathlib import Path

ARGS = sys.argv[sys.argv.index('--') + 1:]
REPO, OUT, ONLY = Path(ARGS[0]), Path(ARGS[1]), ARGS[2:]

# house-families.py runs the jobs named on the command line when imported, so it is handed none of its own.
spec = importlib.util.spec_from_file_location('hf', REPO / 'scripts/art/house-families.py')
hf = importlib.util.module_from_spec(spec)
_argv = sys.argv
sys.argv = _argv[:_argv.index('--') + 3] + ['(none)']
spec.loader.exec_module(hf)
sys.argv = _argv
ts, S = hf.ts, hf.S

ts.SURFACES['roofing-tiles-003'] = {'metres': [2.4, 2.4], 'mean': None,
                                    'maps': {k: hf.LIB / f'roofing-tiles-003-{k}.jpg' for k in ('albedo', 'normal', 'roughness')}}

E = .45
FREE = .3
PARAPET = .7

FAMILIES = {
    'corridor': {
        'bay': 3.0, 'wall': ('render', 'cdc6b6'), 'end': ('render', 'cdc6b6'), 'door': '4f5253',
        'sizes': {'window': (1.6, 1.7, .95), 'door': (2.0, 2.4, .3), 'stair': (2.0, 2.6, 1.2)},
        'street': (['window*', 'hall*', 'window*'], ['window*']),
        'back': (['window*', 'hall*', 'window*'], ['window*']),
        'side': (['blank*', 'door', 'blank*'], ['blank*', 'stair', 'blank*']),
        'hatch': True, 'vents': 2,
    },
    'two-unit': {
        'bay': 3.0, 'wall': ('painted-plaster-wall', 'd4d0c6'), 'end': ('painted-plaster-wall', 'bebbb2'), 'door': '2f4a5c',
        'sizes': {'window': (1.6, 1.7, .95), 'door': (2.0, 2.5, .3), 'stair': (2.0, 2.6, 1.2)},
        'street': (['door', 'window*', 'door'], ['stair', 'window*', 'stair']),
        'back': (['window', 'door', 'window*', 'door', 'window'], ['window*']),
        'side': (['blank*', 'window', 'blank', 'window', 'blank*'], ['blank*', 'window', 'blank', 'window', 'blank*']),
        'party': True, 'hatch': True, 'vents': 2,
    },
    'walkup': {
        'bay': 4.0, 'wall': ('bricks-085', None), 'end': ('bricks-085', None), 'door': '6a2f2a',
        'sizes': {'window': (1.4, 1.7, .95), 'door': (2.0, 2.4, .3), 'stair': (2.0, 2.6, 1.2), 'balcony': (2.4, 2.3, None)},
        'street': (['window*', 'door', 'window*', 'door', 'window*'], ['window*', 'stair', 'window*', 'stair', 'window*']),
        'back': (['balcony+window*'], ['balcony+window*']),
        'side': (['window', 'blank*', 'window'], ['window', 'blank*', 'window']),
        'hatch': False, 'vents': 0,
    },
}


def poly(name, pts, material):
    clean = []
    for p in pts:
        if not clean or max(abs(a - b) for a, b in zip(p, clean[-1])) > 1e-4:
            clean.append(p)
    if len(clean) > 2 and max(abs(a - b) for a, b in zip(clean[0], clean[-1])) < 1e-4:
        clean.pop()
    if len(clean) >= 3:
        ts.mesh(name, clean, [tuple(range(len(clean)))], material)


def expand(tokens, n):
    """A wall rule over n bays. Fixed tokens take their bays; starred ones share the rest, outermost first."""
    groups = [(t.rstrip('*').split('+'), t.endswith('*')) for t in tokens]
    fixed = sum(len(g) for g, star in groups if not star)
    stars = [i for i, (g, star) in enumerate(groups) if star]
    share = {i: 0 for i in stars}
    free = max(0, n - fixed)
    if stars:
        for i in stars:
            share[i] = free // len(stars)
        centre = (len(groups) - 1) / 2
        outer = sorted(stars, key=lambda i: -abs(i - centre))
        for k in range(free - sum(share.values())):
            share[outer[k % len(outer)]] += 1
    out = []
    for i, (g, star) in enumerate(groups):
        if star:
            out += [g[k % len(g)] for k in range(share[i])]
        else:
            out += g
    return out[:n]


def lay(fam, width, storeys, H, rule, street):
    """Openings over a wall: (u, v, w, h, kind). Also the stair centres and balcony doors it found."""
    n = max(1, round(width / fam['bay']))
    bay = width / n
    ground, upper = expand(rule[0], n), expand(rule[1], n)
    sizes = fam['sizes']
    out, extra = [], {'stairs': [], 'balconies': [], 'doors': [], 'halls': [], 'upper': upper, 'bay': bay}
    for k in range(storeys):
        row = ground if k == 0 else upper
        floor = k * S
        b = 0
        while b < n:
            kind = row[b]
            if k == 0 and kind == 'hall':
                last = b
                while last + 1 < n and row[last + 1] == 'hall':
                    last += 1
                c = (b + last + 1) * bay / 2
                out.append((c - 1.2, .3, 2.4, 2.5, 'door'))
                extra['halls'].append(c)
                b = last + 1
                continue
            kind = 'window' if kind == 'hall' else kind
            if kind in sizes:
                w, h, sill = sizes[kind]
                if sill is None:
                    sill = .3 if k == 0 else .15
                w = min(w, bay - .8)
                foot = floor + sill
                h = min(h, H - .3 - foot)
                drawn = 'door' if kind in ('door', 'balcony') else kind
                out.append((b * bay + bay / 2 - w / 2, foot, w, h, drawn))
                if kind == 'stair' and k == 1:
                    extra['stairs'].append(b * bay + bay / 2)
                if kind == 'balcony' and k > 0:
                    extra['balconies'].append((out[-1], floor))
                if kind == 'door' and k == 0:
                    extra['doors'].append((out[-1], upper[b] == 'stair'))
            b += 1
    return out, extra


def look(fam):
    f = FAMILIES[fam]
    ts.materials['wall'] = ts.materials[hf.textured(*f['wall'])]
    ts.materials['wall-end'] = ts.materials[hf.textured(*f['end'])]
    ts.materials['door'] = ts.materials[hf.flat(f['door'])]
    ts.materials['trim'] = ts.materials[hf.flat('ebe6d8')]
    ts.materials['frame'] = ts.materials[hf.flat('eeeeea')]
    ts.materials['plinth'] = ts.materials[hf.flat('a3a29b')]
    ts.materials['metal'] = ts.materials[hf.flat('8d9194', .5)]
    ts.materials['roof'] = ts.materials[hf.textured('clay-roof-tiles-02')]
    ts.materials['membrane'] = ts.materials[hf.textured('membrane') if 'membrane' in ts.SURFACES else hf.flat('6f7274')]
    return f


def canted(c, width, wall_y, face_y, z0, z1, floors):
    """A bay of three walls from wall_y out to face_y, canted at its sides, glazed on each floor and lidded."""
    fw = width / 2 - (wall_y - face_y) * .55
    plan = [(c - width / 2, wall_y), (c - fw, face_y), (c + fw, face_y), (c + width / 2, wall_y)]
    for (ax, ay), (bx, by) in zip(plan, plan[1:]):
        poly('bay wall', [(ax, ay, z0), (bx, by, z0), (bx, by, z1), (ax, ay, z1)], 'wall')
        dx, dy = bx - ax, by - ay
        ln = math.hypot(dx, dy)
        nx, ny = dy / ln * .03, -dx / ln * .03
        inset = min(.15, ln * .12)
        ux, uy = dx / ln * inset, dy / ln * inset
        for low, high in floors:
            low, high = max(low, z0 + .1), min(high, z1 - .1)
            if high > low:
                poly('bay glass', [(ax + ux + nx, ay + uy + ny, low), (bx - ux + nx, by - uy + ny, low),
                                   (bx - ux + nx, by - uy + ny, high), (ax + ux + nx, ay + uy + ny, high)], 'glass')
    ts.box('bay lid', (c - width / 2 - .1, face_y - .12, z1), (c + width / 2 + .1, wall_y, z1 + .25), 'trim')


def inside_stretch(u, ow, u0, u1, w):
    """An opening within 0.2 m of a corner the street wall turns is left out."""
    return u > (u0 + .2 if u0 > 0 else 0) and u + ow < (u1 - .2 if u1 < w else w)


def walls(b, segs, storeys, top=None):
    """The walls of a plan. segs are (u0, u1, setback) along the street from the left end. The street
    and side walls follow each segment's setback; the back wall runs the whole frontage."""
    f = FAMILIES[b['fam']]
    x0, x1, y0, y1 = b['x0'], b['x1'], b['y0'], b['y1']
    w, H = x1 - x0, storeys * S
    roof = top if top is not None else H
    front, ext = lay(f, w, storeys, roof, f['street'], True)
    for u0, u1, sb in segs:
        face = ts.Face((x0 + u0, y0 + sb, 0), (1, 0, 0), (0, -1, 0))
        inside = [(u - u0, v, ow, oh, k) for u, v, ow, oh, k in front if inside_stretch(u, ow, u0, u1, w)]
        ts.facade('front', face, u1 - u0, H, inside, 'wall')
        ts.box('plinth', (x0 + u0, y0 + sb - .05, 0), (x0 + u1, y1 + .05, .3), 'plinth')
        for (u, v, ow, oh, k), stair in ext['doors']:
            if stair and inside_stretch(u, ow, u0, u1, w):
                face.slab('door hood', u - u0 - .4, u - u0 + ow + .4, v + oh + .1, v + oh + .25, -1.1, 0, 'trim')
        for c in ext['halls']:
            if inside_stretch(c - 1.2, 2.4, u0, u1, w):
                face.slab('entrance canopy', c - u0 - 1.8, c - u0 + 1.8, 2.9, 3.05, -1.2, 0, 'trim')
    for (a, sa), (b_, sb) in zip(((s[1], s[2]) for s in segs), ((s[0], s[2]) for s in segs[1:])):
        if abs(sa - sb) < 1e-3:
            continue
        x = x0 + a
        lo, hi = sorted((sa, sb))
        face = ts.Face((x, y0 + lo, 0), (0, 1, 0), (1, 0, 0)) if sa < sb else ts.Face((x, y0 + hi, 0), (0, -1, 0), (-1, 0, 0))
        ts.facade('return', face, hi - lo, H, [], 'wall')
    back, bext = lay(f, w, storeys, roof, f['back'], False)
    bface = ts.Face((x1, y1, 0), (-1, 0, 0), (0, 1, 0))
    ts.facade('back', bface, w, H, back, 'wall')
    for (u, v, ow, oh, k), floor in bext['balconies']:
        u0_, u1_ = u - .3, u + ow + .3
        bface.slab('balcony slab', u0_, u1_, floor - .05, floor + .15, -1.4, 0, 'trim')
        bface.slab('balcony rail', u0_, u1_, floor + .15, floor + 1.1, -1.4, -1.35, 'metal')
        for e in (u0_, u1_ - .05):
            bface.slab('balcony side', e, e + .05, floor + .15, floor + 1.1, -1.35, 0, 'metal')
    for attached, x, fy, outward, along in ((b['left'], x0, segs[0][2], -1, -1), (b['right'], x1, segs[-1][2], 1, 1)):
        d = y1 - (y0 + fy)
        side = [] if attached else lay(f, d, storeys, roof, f['side'], False)[0]
        face = ts.Face((x, y1, 0), (0, -1, 0), (-1, 0, 0)) if outward < 0 else ts.Face((x, y0 + fy, 0), (0, 1, 0), (1, 0, 0))
        ts.facade('side', face, d, H, side, 'wall')
    if f.get('party'):
        cx = (x0 + x1) / 2
        for sb in [s[2] for s in segs if s[0] < w / 2 < s[1]]:
            ts.box('party line', (cx - .15, y0 + sb - .12, .3), (cx + .15, y0 + sb, H), 'wall-end')
        ts.box('party line back', (cx - .15, y1, .3), (cx + .15, y1 + .12, H), 'wall-end')
    return ext


def flat_roof(b, segs, H, ext):
    """A deck behind a coped parapet that follows the street wall's set-backs. The coping overhangs a
    free end and stops at a shared one. The roof kit stands behind the rearmost set-back."""
    x0, x1, y0, y1 = b['x0'], b['x1'], b['y0'], b['y1']
    w = x1 - x0
    top = H + PARAPET
    left = x0 if b['left'] else x0 - .02
    right = x1 if b['right'] else x1 + .02
    for u0, u1, sb in segs:
        ts.box('roof deck', (x0 + u0 + .1, y0 + sb + .1, H - .2), (x0 + u1 - .1, y1 - .1, H + .02), 'membrane')
        ts.box('parapet front', (x0 + u0, y0 + sb, H), (x0 + u1, y0 + sb + .25, top), 'wall')
        ts.box('coping front', (x0 + u0 - .02 if u0 > 0 else left, y0 + sb - .05, top),
               (x0 + u1 + .02 if u1 < w else right, y0 + sb + .3, top + .08), 'trim')
    for (a, sa), (_, sb) in zip(((s[1], s[2]) for s in segs), ((s[0], s[2]) for s in segs[1:])):
        if abs(sa - sb) > 1e-3:
            lo, hi = sorted((sa, sb))
            ts.box('parapet return', (x0 + a - .125, y0 + lo, H), (x0 + a + .125, y0 + hi + .25, top), 'wall')
            ts.box('coping return', (x0 + a - .15, y0 + lo, top), (x0 + a + .15, y0 + hi + .3, top + .08), 'trim')
    ts.box('parapet back', (x0, y1 - .25, H), (x1, y1, top), 'wall')
    ts.box('coping back', (left, y1 - .3, top), (right, y1 + .05, top + .08), 'trim')
    ts.box('parapet side', (x0, y0 + segs[0][2], H), (x0 + .25, y1, top), 'wall')
    ts.box('coping side', (left, y0 + segs[0][2], top), (x0 + .27, y1, top + .08), 'trim')
    ts.box('parapet side', (x1 - .25, y0 + segs[-1][2], H), (x1, y1, top), 'wall')
    ts.box('coping side', (x1 - .27, y0 + segs[-1][2], top), (right, y1, top + .08), 'trim')
    roof_kit(b, y0 + max(s[2] for s in segs), H, ext)


def roof_kit(b, front, H, ext):
    """A hatch over each street stair, or one behind the middle where the street wall has none; the
    family's vents; and the party line's upstand across the roof."""
    f = FAMILIES[b['fam']]
    x0, x1, y1 = b['x0'], b['x1'], b['y1']
    cy = (b['y0'] + y1) / 2
    if f['hatch'] and not ext['stairs']:
        ts.box('roof hatch', ((x0 + x1) / 2 - 1.0, cy + 1.0, H), ((x0 + x1) / 2 + 1.0, cy + 2.2, H + .5), 'metal')
    if f['hatch']:
        for s in ext['stairs']:
            ts.box('roof hatch', (x0 + s - 1.0, front + 2.5, H), (x0 + s + 1.0, front + 3.7, H + .5), 'metal')
    for k in range(f['vents']):
        vx = x0 + (x1 - x0) * (k + 1) / (f['vents'] + 1)
        ts.box('rooftop vent', (vx - .4, cy + 3.0, H), (vx + .4, cy + 3.8, H + .9), 'metal')
    if f.get('party'):
        cx = (x0 + x1) / 2
        ts.box('party upstand', (cx - .15, front + .25, H), (cx + .15, y1 - .25, H + PARAPET), 'wall-end')


def upstand(x, side, profile):
    """Half a party wall's thickness above the roof, on this Building's side of the line."""
    lo, hi = (x, x + .15) if side < 0 else (x - .15, x)
    ts.prism('party upstand', profile, lo, hi, 'wall-end')


def chimney(x, y, z0, z1):
    ts.box('chimney', (x - .5, y - .45, z0), (x + .5, y + .45, z1), 'wall-end')
    hf.boxes('pots', [((x + dx - .1, y - .1, z1), (x + dx + .1, y + .1, z1 + .4)) for dx in (-.25, 0, .25)], 'wall-end')


def pitched(b, yf, H, pitch, hip, gables=()):
    """A ridge along the street over the main range from yf back. A free end hips or shows a gable;
    an end against a neighbor stops at the party wall. gables are (centre x, half width, front y) of
    cross gables whose ridges run back into the front slope."""
    x0, x1, y1 = b['x0'], b['x1'], b['y1']
    t = math.tan(math.radians(pitch))
    half = (y1 - yf) / 2
    if hip and not b['left'] and not b['right'] and half > (x1 - x0) / 2:
        turned_hip(b, yf, H, t, gables)
        return
    cy, top, low = (yf + y1) / 2, H + half * t, H - E * t
    ya, yb = yf - E, y1 + E
    ends = []
    for attached, x, out in ((b['left'], x0, -1), (b['right'], x1, 1)):
        if attached:
            ends.append((x, x))
        elif hip:
            ends.append((x + out * E, x + out * E - out * (half + E)))
        else:
            ends.append((x + out * FREE, x + out * FREE))
    (xa, ra), (xb, rb) = ends
    if ra > rb:
        ra = rb = (ra + rb) / 2
    if gables:
        (gx, gh, gy), = gables
        apex = (gx, yf + gh, H + gh * t)
        poly('roof front', [(xa, ya, low), (gx - gh - E, ya, low), apex, (gx, cy, top), (ra, cy, top)], 'roof')
        poly('roof front', [(gx + gh + E, ya, low), (xb, ya, low), (rb, cy, top), (gx, cy, top), apex], 'roof')
    else:
        poly('roof front', [(xa, ya, low), (xb, ya, low), (rb, cy, top), (ra, cy, top)], 'roof')
    poly('roof back', [(xb, yb, low), (xa, yb, low), (ra, cy, top), (rb, cy, top)], 'roof')
    for (xe, r), (attached, x, out) in zip(ends, ((b['left'], x0, -1), (b['right'], x1, 1))):
        if hip and not attached:
            poly('hip', [(xe, yb, low), (xe, ya, low), (r, cy, top)] if out < 0 else [(xe, ya, low), (xe, yb, low), (r, cy, top)], 'roof')
        else:
            poly('gable end', [(x, yf, H), (x, y1, H), (x, cy, top)], 'wall-end')
        if attached:
            edge = half + .15
            upstand(x, out, [(cy - edge, H - .3), (cy + edge, H - .3), (cy + edge, top - edge * t + .25), (cy, top + .25), (cy - edge, top - edge * t + .25)])
        elif not hip:
            poly('verge front', [(xe, ya, low + .2), (xe, cy, top + .2), (xe, cy, top), (xe, ya, low)], 'trim')
            poly('verge back', [(xe, cy, top + .2), (xe, yb, low + .2), (xe, yb, low), (xe, cy, top)], 'trim')
    for g in gables:
        front_gable(g, yf, H, low, t)
    gutters(xa, xb, ya, yb, low)
    for k in (1, 3):
        cx = x0 + (x1 - x0) * k / 4
        chimney(cx, cy + .6, top - 1.2, top + 1.1)


def turned_hip(b, yf, H, t, gables):
    """A hipped roof whose ridge runs back from the street, over a Building free at both ends and
    deeper than it is wide."""
    x0, x1, y1 = b['x0'], b['x1'], b['y1']
    cx, hw = (x0 + x1) / 2, (x1 - x0) / 2
    top, low = H + hw * t, H - E * t
    xa, xb, ya, yb = x0 - E, x1 + E, yf - E, y1 + E
    r0, r1 = yf + hw, y1 - hw
    poly('roof side', [(xa, yb, low), (xa, ya, low), (cx, r0, top), (cx, r1, top)], 'roof')
    poly('roof side', [(xb, ya, low), (xb, yb, low), (cx, r1, top), (cx, r0, top)], 'roof')
    poly('hip back', [(xb, yb, low), (xa, yb, low), (cx, r1, top)], 'roof')
    if gables:
        (gx, gh, gy), = gables
        apex = (gx, yf + gh, H + gh * t)
        poly('hip front', [(xa, ya, low), (gx - gh - E, ya, low), apex, (cx, r0, top)], 'roof')
        poly('hip front', [(gx + gh + E, ya, low), (xb, ya, low), (cx, r0, top), apex], 'roof')
        front_gable(gables[0], yf, H, low, t)
    else:
        poly('hip front', [(xa, ya, low), (xb, ya, low), (cx, r0, top)], 'roof')
    gutters(xa, xb, ya, yb, low)
    for k in (1, 3):
        chimney(cx + .6, yf + (y1 - yf) * k / 4, top - 1.2, top + 1.1)


def front_gable(g, yf, H, low, t):
    """A front gable whose ridge runs back into the main roof's front slope, with an attic window
    where it is tall enough."""
    gx, gh, gy = g
    gt = H + gh * t
    xe0, xe1 = gx - gh - E, gx + gh + E
    ye, ya = gy - E, yf - E
    poly('cross roof', [(xe0, ye, low), (xe0, ya, low), (gx, yf + gh, gt), (gx, ye, gt)], 'roof')
    poly('cross roof', [(xe1, ya, low), (xe1, ye, low), (gx, ye, gt), (gx, yf + gh, gt)], 'roof')
    poly('cross gable', [(gx - gh, gy, H), (gx + gh, gy, H), (gx, gy, gt)], 'wall')
    for xe in (xe0, xe1):
        poly('cross verge', [(xe, ye - .02, low), (gx, ye - .02, gt), (gx, ye - .02, gt + .2), (xe, ye - .02, low + .2)], 'trim')
    if gt - H > 2.2:
        gw = min(1.2, gh * .5)
        ts.box('attic frame', (gx - gw / 2 - .1, gy - .07, H + .45), (gx + gw / 2 + .1, gy, H + 1.85), 'trim')
        ts.box('attic glass', (gx - gw / 2, gy - .09, H + .55), (gx + gw / 2, gy - .06, H + 1.75), 'glass')


def gutters(xa, xb, ya, yb, low):
    ts.box('gutter', (xa, ya, low - .18), (xb, ya + .22, low), 'metal')
    ts.box('gutter', (xa, yb - .22, low - .18), (xb, yb, low), 'metal')


def form_parapet(b):
    segs = [(0, b['x1'] - b['x0'], 0)]
    flat_roof(b, segs, b['storeys'] * S, walls(b, segs, b['storeys']))


def form_hipped(b, pitch=26):
    walls(b, [(0, b['x1'] - b['x0'], 0)], b['storeys'])
    pitched(b, b['y0'], b['storeys'] * S, pitch, hip=True)


def form_side_gable(b, pitch=34):
    walls(b, [(0, b['x1'] - b['x0'], 0)], b['storeys'])
    pitched(b, b['y0'], b['storeys'] * S, pitch, hip=False)


def form_mansard(b):
    """The top storey inside a slate mansard lit by a dormer a bay, the walls a storey lower."""
    x0, x1, y0, y1 = b['x0'], b['x1'], b['y0'], b['y1']
    f = FAMILIES[b['fam']]
    ts.materials['roof'] = ts.materials[hf.textured('roofing-tiles-003')]
    H2 = (b['storeys'] - 1) * S
    Ht, k = H2 + S, 1.1
    ext = walls(b, [(0, x1 - x0, 0)], b['storeys'] - 1)
    ka = 0 if b['left'] else k
    kb = 0 if b['right'] else k
    xa, xb = x0 - (0 if b['left'] else .08), x1 + (0 if b['right'] else .08)
    poly('mansard front', [(xa, y0 - .08, H2), (xb, y0 - .08, H2), (x1 - kb, y0 + k, Ht), (x0 + ka, y0 + k, Ht)], 'roof')
    poly('mansard back', [(xb, y1 + .08, H2), (xa, y1 + .08, H2), (x0 + ka, y1 - k, Ht), (x1 - kb, y1 - k, Ht)], 'roof')
    for attached, x, kk, out in ((b['left'], x0, ka, -1), (b['right'], x1, kb, 1)):
        xs = x + out * .08
        if attached:
            poly('mansard end', [(x, y0, H2), (x, y1, H2), (x, y1 - k, Ht), (x, y0 + k, Ht)], 'wall-end')
            upstand(x, out, [(y0 - .1, H2 - .3), (y1 + .1, H2 - .3), (y1 + .1, H2 + .25), (y1 - k + .05, Ht + .3), (y0 + k - .05, Ht + .3), (y0 - .1, H2 + .25)])
        else:
            pts = [(xs, y1 + .08, H2), (xs, y0 - .08, H2), (x - out * kk, y0 + k, Ht), (x - out * kk, y1 - k, Ht)]
            poly('mansard side', pts if out < 0 else pts[::-1], 'roof')
    ts.box('mansard deck', (x0 + ka, y0 + k, Ht - .1), (x1 - kb, y1 - k, Ht + .1), 'membrane')
    ca = x0 if b['left'] else x0 + ka - .05
    cb = x1 if b['right'] else x1 - kb + .05
    ts.box('crown coping', (ca, y0 + k - .05, Ht + .1), (cb, y0 + k + .2, Ht + .22), 'trim')
    ts.box('crown coping', (ca, y1 - k - .2, Ht + .1), (cb, y1 - k + .05, Ht + .22), 'trim')
    ts.box('cornice', (x0, y0 - .45, H2 - .35), (x1, y0, H2), 'trim')
    ts.box('back cornice', (x0, y1, H2 - .25), (x1, y1 + .3, H2), 'trim')
    w = x1 - x0
    n = max(1, round(w / f['bay']))
    bay = w / n
    upper = ext['upper']
    for face_y, sign in ((y0, 1), (y1, -1)):
        for i in range(n):
            if face_y == y0 and upper[i] == 'blank':
                continue
            c = x0 + (i + .5) * bay
            dw = min(.75, bay / 2 - .45)
            yf = face_y + sign * .35
            yr = face_y + sign * (k + .3)
            lo, hi = sorted((yf, yr))
            ts.box('dormer cheeks', (c - dw, lo, H2 + .25), (c + dw, hi, H2 + 2.5), 'trim')
            ts.box('dormer glass', (c - dw + .15, yf - sign * .03 - .01, H2 + .55), (c + dw - .15, yf - sign * .03 + .01, H2 + 2.2), 'glass')
            ts.box('dormer hood', (c - dw - .15, lo - .12, H2 + 2.45), (c + dw + .15, hi, H2 + 2.65), 'roof')
    for kx in (1, 3):
        chimney(x0 + w * kx / 4, (y0 + y1) / 2, Ht - .2, Ht + 1.4)


def form_centre_gable(b, pitch=26):
    """A hipped range whose middle third steps forward 1.2 m under a front gable."""
    w = b['x1'] - b['x0']
    f = FAMILIES[b['fam']]
    bay = w / max(1, round(w / f['bay']))
    n = max(1, round(w / f['bay']))
    P = 1.2
    room = b['y1'] - b['y0'] - P - 1.6
    widest = max(1, int(room / bay))
    mid = min(max(2, round(n / 3)), widest)
    if (n - mid) % 2:
        mid += -1 if mid > 2 or mid + 1 > widest else 1
    if not (1 <= mid < n and mid * bay <= room):
        form_hipped(b, pitch)
        return
    u0 = (n - mid) / 2 * bay
    u1 = u0 + mid * bay
    walls(b, [(0, u0, P), (u0, u1, 0), (u1, w, P)], b['storeys'])
    H = b['storeys'] * S
    pitched(b, b['y0'] + P, H, pitch, hip=True, gables=[(b['x0'] + (u0 + u1) / 2, (u1 - u0) / 2, b['y0'])])


def form_bay_front(b):
    """A parapet range set back 0.9 m behind full-height canted bays, one either side of the middle."""
    x0, w = b['x0'], b['x1'] - b['x0']
    f = FAMILIES[b['fam']]
    n = max(1, round(w / f['bay']))
    bay = w / n
    P = .9
    ext = walls(b, [(0, w, P)], b['storeys'])
    H = b['storeys'] * S
    picks = [] if n < 3 else [1, n - 2] if n < 10 else [1, n // 2 - 1, n // 2 + (0 if n % 2 == 0 else 1), n - 2]
    # A canted bay wider than its bay moves one bay inward where it would overhang a street door outside it.
    if min(3.2, bay + .6) > bay:
        ground = expand(f['street'][0], n)
        picks = [i + (1 if i < n / 2 else -1) if ground[i - (1 if i < n / 2 else -1)] == 'door' else i for i in picks]
    for i in sorted({i for i in picks if 0 < i < n - 1}):
        c = x0 + (i + .5) * bay
        bw = min(3.2, bay + .6)
        canted(c, bw, b['y0'] + P, b['y0'], .3, H, [(k * S + .95, k * S + 2.65) for k in range(b['storeys'])])
        ts.box('bay base', (c - bw / 2, b['y0'], 0), (c + bw / 2, b['y0'] + P, .3), 'plinth')
    flat_roof(b, [(0, w, P)], H, ext)


def form_forecourt(b):
    """Two end wings on the street line and the middle set back 3 m around a forecourt, under parapets."""
    w = b['x1'] - b['x0']
    f = FAMILIES[b['fam']]
    n = max(1, round(w / f['bay']))
    bay = w / n
    wing = max(2, round(n / 4)) * bay
    segs = [(0, wing, 0), (wing, w - wing, 3.0), (w - wing, w, 0)]
    flat_roof(b, segs, b['storeys'] * S, walls(b, segs, b['storeys']))
    ts.box('forecourt paving', (b['x0'] + wing, b['y0'], .0), (b['x1'] - wing, b['y0'] + 3.0, .14), 'plinth')


def form_setback(b):
    """The top storey set 2.4 m back from the street behind a railed roof terrace."""
    x0, x1, y0, y1 = b['x0'], b['x1'], b['y0'], b['y1']
    w = x1 - x0
    H2 = (b['storeys'] - 1) * S
    SB = 2.4
    ext = walls(b, [(0, w, 0)], b['storeys'] - 1)
    ts.box('terrace deck', (x0 + .1, y0 + .1, H2 - .2), (x1 - .1, y0 + SB, H2 + .05), 'membrane')
    ts.box('terrace edge', (x0, y0 - .3, H2 - .3), (x1, y0 + .15, H2 + .12), 'trim')
    ts.box('terrace rail', (x0 + .05, y0 + .02, H2 + 1.05), (x1 - .05, y0 + .1, H2 + 1.12), 'metal')
    n = max(2, round(w / 1.5))
    hf.boxes('rail posts', [((x0 + w * i / n - .03, y0 + .02, H2 + .05), (x0 + w * i / n + .03, y0 + .08, H2 + 1.05)) for i in range(1, n)], 'metal')
    top = dict(b, y0=y0 + SB)
    f = FAMILIES[b['fam']]
    upper = (f['street'][1], f['street'][1])
    saved = f['street']
    f['street'] = upper
    face = ts.Face((x0, y0 + SB, H2), (1, 0, 0), (0, -1, 0))
    ops, _ = lay(f, w, 1, S, upper, True)
    ops = [(u, v, ow, oh, 'door' if k == 'window' and i % 2 == 0 else k) for i, (u, v, ow, oh, k) in enumerate(ops)]
    ops = [(u, .15 if k == 'door' else v, ow, (2.3 if k == 'door' else oh), k) for u, v, ow, oh, k in ops]
    ts.facade('top front', face, w, S, ops, 'wall-end')
    f['street'] = saved
    back = ts.Face((x1, y1, H2), (-1, 0, 0), (0, 1, 0))
    ts.facade('top back', back, w, S, lay(f, w, 1, S, (f['back'][1], f['back'][1]), False)[0], 'wall-end')
    for attached, x, out in ((b['left'], x0, -1), (b['right'], x1, 1)):
        face = ts.Face((x, y1, H2), (0, -1, 0), (-1, 0, 0)) if out < 0 else ts.Face((x, y0 + SB, H2), (0, 1, 0), (1, 0, 0))
        ts.facade('top side', face, y1 - y0 - SB, S, [], 'wall-end')
    flat_roof(top, [(0, w, 0)], b['storeys'] * S, ext)


FORMS = {
    'parapet': form_parapet,
    'hipped': form_hipped,
    'side-gable': form_side_gable,
    'mansard': form_mansard,
    'centre-gable': form_centre_gable,
    'bay-front': form_bay_front,
    'forecourt': form_forecourt,
    'setback': form_setback,
}


def row(form, items, y0):
    """items: (family, width, depth, storeys, gap after). A zero gap joins a Building to the next."""
    total = sum(w + g for _, w, _, _, g in items) - items[-1][4]
    x = -total / 2
    for i, (fam, w, d, storeys, gap) in enumerate(items):
        look(fam)
        b = {'fam': fam, 'x0': x, 'x1': x + w, 'y0': y0, 'y1': y0 + d, 'storeys': storeys,
             'left': i > 0 and items[i - 1][4] == 0, 'right': gap == 0 and i + 1 < len(items)}
        FORMS[form](b)
        x += w + gap
    return total


def tile(form, detached=False):
    hf.fresh()
    camera = hf.stage(road_y=(-14.0, -6.0))
    scene = hf.bpy.context.scene
    scene.render.resolution_x, scene.render.resolution_y = 1600, 820
    if detached:
        items = [('two-unit', 16.0, 20.0, 4, 10.0), ('corridor', 40.0, 16.0, 5, 10.0), ('walkup', 56.0, 12.0, 5, 0.0)]
    else:
        items = [('corridor', 20.0, 16.0, 3, 0.0), ('two-unit', 24.0, 16.0, 3, 8.0), ('corridor', 28.0, 16.0, 3, 8.0),
                 ('walkup', 36.0, 12.0, 3, 0.0)]
    total = row(form, items, -3.0)
    for part in ts.parts:
        ts.project_uvs(part)
    name = f'{"detached" if detached else "tile"}-{form}'
    hf.shoot(camera, name, (-total * .06, -total * .98 - 12, total * .48 + 12), (0, 5, 6))


JOBS = {f'tile-{f}': (lambda f=f: tile(f)) for f in FORMS}
JOBS.update({f'detached-{f}': (lambda f=f: tile(f, True)) for f in FORMS})

for name, job in JOBS.items():
    if not ONLY or name in ONLY:
        job()
        print('DONE', name)

"""Rowhouse forms: attached houses on perimeter Lots, varied by roof and plan. Each form draws one
house between its two side walls, so a house closes against a neighbor of any form. It draws its own
roof ends over a shared wall, blank walls on its shared sides and a party upstand where its roof meets
one. Walls, openings and dressing come from house-families.py, so the walls here are a stand-in: the
shell lays them by each family's bay rules (appearance/test-street/preset.toml). Roofs and plans
follow FamilyBodyBuilder.Terrace.cs, and a change to one belongs in the other.

blender --background --factory-startup --python-exit-code 1 --python scripts/art/rowhouse-forms.py -- REPO OUT [job...]

Jobs: tile-<form> for each form, street-terraces and street-houses.
"""
import importlib.util
import math
import random
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
import bpy

E = .35
FREE = .3
BAY = 3.5


def poly(name, pts, material):
    clean = []
    for p in pts:
        if not clean or max(abs(a - b) for a, b in zip(p, clean[-1])) > 1e-4:
            clean.append(p)
    if len(clean) > 2 and max(abs(a - b) for a, b in zip(clean[0], clean[-1])) < 1e-4:
        clean.pop()
    if len(clean) >= 3:
        ts.mesh(name, clean, [tuple(range(len(clean)))], material)


def terrace_look(seed, w, d, storeys):
    """What a terrace shares: roof, trim, frames, pitch draw and wall scheme."""
    s = hf.draw(seed, w, d, storeys, 'terrace')
    s['canopy'] = random.Random(seed).choice(['flat', 'gable', 'none'])
    return s


def house_look(terrace, seed, w):
    """A house of a terrace: its own wall paint and door."""
    own = hf.draw(seed, w, terrace['D'], terrace['storeys'], 'terrace')
    s = dict(terrace)
    m = dict(own['mods'][0], w=w, mirror=False, bay=False)
    if terrace['scheme'] == 'brick':
        m['wall'] = terrace['end']
    s['mods'] = [m]
    s['end'] = m['wall'] if terrace['scheme'] != 'brick and render' else terrace['end']
    hf.bind(s)
    ts.materials['door'] = ts.materials[m['door']]
    return s


def front_openings(w, storeys, mirror, door=True, skip=None):
    """A door in the first bay and windows in the rest, then a window a bay on each storey above."""
    n = max(1, round(w / BAY))
    b, wh, dh = w / n, hf.window_height(), min(2.75, S - .75)
    out = []

    def put(u, v, ww, hh, kind):
        u = w - u - ww if mirror else u
        if skip and v < 2 * S and skip[0] < u + ww and u < skip[1]:
            return
        out.append((u, v, ww, hh, kind))

    if door:
        put(.45 if n == 1 else b / 2 - .5, .3, 1.0, dh, 'door')
    if n == 1:
        gw = min(1.4, w - 2.35)
        if gw >= .7:
            put(w - .45 - gw, .75, gw, wh, 'window')
    else:
        for i in range(1, n):
            gw = min(1.6, b - .9)
            put(i * b + b / 2 - gw / 2, .75, gw, wh, 'window')
    for k in range(1, storeys):
        for i in range(n):
            uw = min(1.3, b - .9)
            put(i * b + b / 2 - uw / 2, k * S + .75, uw, wh, 'window')
    return out


def back_openings(w, storeys):
    n = max(1, round(w / BAY))
    b, wh = w / n, hf.window_height()
    out = [(w - b / 2 - .5, .3, 1.0, 2.5, 'door')]
    out += [(i * b + b / 2 - .6, .75, 1.2, wh, 'window') for i in range(n - 1)]
    out += [(i * b + b / 2 - .6, k * S + .75, 1.2, wh, 'window') for k in range(1, storeys) for i in range(n)]
    return out


def walls(h, x0=None, x1=None, y0=None, y1=None, storeys=None, front=True, back=True, sides=(True, True),
          door=True, skip=None):
    """One rectangular volume. A side wall shared with a neighbor is blank."""
    s = h['s']
    x0, x1 = h['x0'] if x0 is None else x0, h['x1'] if x1 is None else x1
    y0, y1 = h['y0'] if y0 is None else y0, h['y1'] if y1 is None else y1
    storeys = storeys or h['storeys']
    w, d, H = x1 - x0, y1 - y0, storeys * S
    wall = s['mods'][0]['wall']
    doors = []
    if front:
        face = ts.Face((x0, y0, 0), (1, 0, 0), (0, -1, 0))
        openings = front_openings(w, storeys, h['mirror'], door, skip)
        hf.elevation('front', face, w, H, openings, s, wall)
        for u, v, ow, oh, kind in openings:
            if kind == 'door':
                hf.canopy(face, u, ow, s, v + oh)
                face.slab('step', u - .25, u + ow + .25, 0, .3, -1.0, 0, 'plinth')
                doors.append(x0 + u + ow / 2)
    if back:
        hf.elevation('back', ts.Face((x1, y1, 0), (-1, 0, 0), (0, 1, 0)), w, H, back_openings(w, storeys), s, wall)
    if sides[0]:
        ts.facade('left side', ts.Face((x0, y1, 0), (0, -1, 0), (-1, 0, 0)), d, H, [], s['end'])
    if sides[1]:
        ts.facade('right side', ts.Face((x1, y0, 0), (0, 1, 0), (1, 0, 0)), d, H, [], s['end'])
    ts.box('plinth', (x0, y0 - .05, 0), (x1, y1 + .05, .3), 'plinth')
    return doors


def upstand(h, x, profile):
    """Half a party wall's thickness above the roof, on this house's side of the line."""
    lo, hi = (x, x + .15) if x == h['x0'] else (x - .15, x)
    ts.prism('party upstand', profile, lo, hi, h['s']['end'])


def chimney(h, y, z0, z1):
    """A stack on the right party wall, so one house of each pair carries it."""
    if not h['right']:
        return
    x = h['x1']
    ts.box('chimney', (x - .4, y, z0), (x + .4, y + .9, z1), h['s']['ground'] or h['s']['end'])
    hf.boxes('pots', [((x + dx - .1, y + .35, z1), (x + dx + .1, y + .55, z1 + .35)) for dx in (-.2, .2)], hf.flat('9a5a3a'))


def gable_along(h, y0, y1, H, pitch):
    """A ridge along the street. It overhangs a free end and stops at a shared one."""
    x0, x1 = h['x0'], h['x1']
    t = math.tan(math.radians(pitch))
    A, cy = (y1 - y0) / 2, (y0 + y1) / 2
    top, low = H + A * t, H - E * t
    xa, xb = x0 - (0 if h['left'] else FREE), x1 + (0 if h['right'] else FREE)
    for side in (-1, 1):
        ye = cy + side * (A + E)
        poly('roof slope', [(xa, ye, low), (xb, ye, low), (xb, cy, top), (xa, cy, top)], 'roof')
    for x in (x0, x1):
        poly('gable end', [(x, y0, H), (x, y1, H), (x, cy, top)], h['s']['end'])
    edge = A + .15

    def roof(dist):
        return top - abs(dist) * t

    profile = [(cy - edge, H - .3), (cy + edge, H - .3), (cy + edge, roof(edge) + .25), (cy, top + .25), (cy - edge, roof(edge) + .25)]
    for attached, x in ((h['left'], x0), (h['right'], x1)):
        if attached:
            upstand(h, x, profile)
    chimney(h, cy + .3, roof(1.2) - .6, top + 1.1)
    for ye in (cy - A - E, cy + A + E - .22):
        hf.gutter(xa, xb, ye, ye + .22, low)


def gable_across(h, y0, y1, H, pitch, half=0):
    """A ridge running back from the street, so each house shows a gable. Half 1 draws the left half of
    a gable whose ridge stands on the house's right wall, half -1 the right half."""
    x0, x1 = h['x0'], h['x1']
    t = math.tan(math.radians(pitch))
    if half == 0:
        cx, outer = (x0 + x1) / 2, [(x0, -1, h['left']), (x1, 1, h['right'])]
    elif half == 1:
        cx, outer = x1, [(x0, -1, h['left'])]
    else:
        cx, outer = x0, [(x1, 1, h['right'])]
    A = abs(outer[0][0] - cx)
    top = H + A * t
    ya, yb = y0 - E, y1 + E
    s = h['s']
    wall = s['mods'][0]['wall']
    for xo, side, attached in outer:
        over = 0 if attached else FREE
        xe, low = xo + side * over, H - over * t
        poly('roof slope', [(xe, ya, low), (xe, yb, low), (cx, yb, top), (cx, ya, top)], 'roof')
        for y, d in ((ya, -.03), (yb, .03)):
            poly('verge', [(xe, y + d, low - .05), (cx, y + d, top - .05), (cx, y + d, top + .15), (xe, y + d, low + .15)], s['trim'])
    for y in (y0, y1):
        poly('gable', [(x0, y, H), (x1, y, H), (cx, y, top)], wall)
    # The attic window keeps 0.1 m of gable wall beside its frame's top corner.
    off = 0 if half == 0 else .7
    glazed = (top - H) - (off + .55 + .1) * t - .52
    if glazed >= .6:
        gx = cx - half * off
        gh = min(1.3, glazed)
        ts.box('attic frame', (gx - .55, y0 - .07, H + .4), (gx + .55, y0, H + .52 + gh), s['trim'])
        ts.box('attic glass', (gx - .43, y0 - .09, H + .52), (gx + .43, y0 - .06, H + .4 + gh), 'glass')
    if half == 1:
        ts.box('chimney', (cx - .4, y0 + 3, top - .8), (cx + .4, y0 + 3.9, top + .9), s['ground'] or s['end'])


def side_gable(h):
    doors = walls(h)
    gable_along(h, h['y0'], h['y1'], h['storeys'] * S, h['pitch'])
    return doors


def front_gable(h):
    doors = walls(h)
    gable_across(h, h['y0'], h['y1'], h['storeys'] * S, h['pitch'])
    return doors


def paired_gable(h):
    doors = walls(h)
    gable_across(h, h['y0'], h['y1'], h['storeys'] * S, h['pitch'], half=h['pair'])
    return doors


def parapet(h):
    """A flat roof behind a parapet, with a bracketed cornice along the street."""
    x0, x1, y0, y1 = h['x0'], h['x1'], h['y0'], h['y1']
    H = h['storeys'] * S
    top = H + .9
    s = h['s']
    wall = s['mods'][0]['wall']
    doors = walls(h)
    ts.box('parapet', (x0, y0, H), (x1, y0 + .25, top), wall)
    ts.box('back parapet', (x0, y1 - .25, H), (x1, y1, H + .5), s['end'])
    ts.box('roof deck', (x0, y0, H - .2), (x1, y1, H + .05), hf.flat('4f4d4a'))
    for attached, x, inward in ((h['left'], x0, 1), (h['right'], x1, -1)):
        a, b = sorted((x, x + inward * (.15 if attached else .25)))
        ts.box('side parapet', (a, y0, H), (b, y1, H + (.6 if attached else .5)), s['end'])
    ts.box('cornice', (x0, y0 - .45, H - .45), (x1, y0, H - .2), s['trim'])
    ts.box('coping', (x0, y0 - .06, top), (x1, y0 + .32, top + .1), s['trim'])
    count = max(2, round((x1 - x0) / .9))
    hf.boxes('brackets', [((x0 + (x1 - x0) * (i + .5) / count - .06, y0 - .4, H - .95), (x0 + (x1 - x0) * (i + .5) / count + .06, y0, H - .45))
                          for i in range(count)], s['trim'])
    chimney(h, y0 + 3, H, top + 1.0)
    ts.box('hatch', (x0 + (x1 - x0) * .5 - .45, y1 - 3.0, H), (x0 + (x1 - x0) * .5 + .45, y1 - 2.1, H + .45), hf.flat('3b3f42', .5))
    return doors


def mansard(h):
    """Two storeys of wall and the third inside a mansard, lit by a dormer a bay."""
    x0, x1, y0, y1 = h['x0'], h['x1'], h['y0'], h['y1']
    s = h['s']
    H2 = (h['storeys'] - 1) * S
    Ht, k, cy = H2 + S, 1.1, (y0 + y1) / 2
    crown = Ht + .35
    doors = walls(h, storeys=h['storeys'] - 1)
    xa, xb = x0 - (0 if h['left'] else .1), x1 + (0 if h['right'] else .1)
    poly('mansard front', [(xa, y0 - .05, H2), (xb, y0 - .05, H2), (xb, y0 + k, Ht), (xa, y0 + k, Ht)], 'roof')
    poly('mansard back', [(xb, y1 + .05, H2), (xa, y1 + .05, H2), (xa, y1 - k, Ht), (xb, y1 - k, Ht)], 'roof')
    poly('crown front', [(xa, y0 + k, Ht), (xb, y0 + k, Ht), (xb, cy, crown), (xa, cy, crown)], 'roof')
    poly('crown back', [(xb, y1 - k, Ht), (xa, y1 - k, Ht), (xa, cy, crown), (xb, cy, crown)], 'roof')
    for x in (x0, x1):
        poly('mansard end', [(x, y0, H2), (x, y1, H2), (x, y1 - k, Ht), (x, cy, crown), (x, y0 + k, Ht)], s['end'])
    ts.box('cornice', (x0, y0 - .4, H2 - .3), (x1, y0, H2), s['trim'])
    ts.box('back cornice', (x0, y1, H2 - .2), (x1, y1 + .25, H2), s['trim'])
    n = max(1, round((x1 - x0) / BAY))
    b = (x1 - x0) / n
    for i in range(n):
        c = x0 + (i + .5) * b
        yf = y0 + .3
        ts.box('dormer cheeks', (c - .7, yf, H2 + .25), (c + .7, y0 + k + .4, H2 + 2.45), s['trim'])
        ts.box('dormer glass', (c - .48, yf - .03, H2 + .55), (c + .48, yf, H2 + 2.15), 'glass')
        ts.box('dormer bar', (c - .03, yf - .05, H2 + .55), (c + .03, yf, H2 + 2.15), s['frame'])
        ts.box('dormer hood', (c - .85, yf - .15, H2 + 2.4), (c + .85, y0 + k + .4, H2 + 2.6), 'roof')
    profile = [(y0 - .1, H2 - .3), (y1 + .1, H2 - .3), (y1 + .1, H2 + .25), (y1 - k + .05, Ht + .25), (cy, crown + .25),
               (y0 + k - .05, Ht + .25), (y0 - .1, H2 + .25)]
    for attached, x in ((h['left'], x0), (h['right'], x1)):
        if attached:
            upstand(h, x, profile)
    chimney(h, cy + .3, Ht - .5, crown + 1.1)
    return doors


def canted(c, width, wall_y, face_y, z0, z1, glazing, wall, s):
    """A bay of three walls from wall_y out to face_y, canted at its sides, glazed and lidded."""
    fw = width / 2 - (wall_y - face_y) * .55
    plan = [(c - width / 2, wall_y), (c - fw, face_y), (c + fw, face_y), (c + width / 2, wall_y)]
    for (ax, ay), (bx, by) in zip(plan, plan[1:]):
        poly('bay wall', [(ax, ay, z0), (bx, by, z0), (bx, by, z1), (ax, ay, z1)], wall)
        dx, dy = bx - ax, by - ay
        ln = math.hypot(dx, dy)
        nx, ny = dy / ln * .03, -dx / ln * .03
        inset = min(.15, ln * .12)
        ux, uy = dx / ln * inset, dy / ln * inset
        for low, high in glazing:
            v0, v1 = max(low, z0 + .1), min(high, z1 - .1)
            if v1 > v0:
                poly('bay glass', [(ax + ux + nx, ay + uy + ny, v0), (bx - ux + nx, by - uy + ny, v0),
                                   (bx - ux + nx, by - uy + ny, v1), (ax + ux + nx, ay + uy + ny, v1)], 'glass')
    ts.box('bay lid', (c - width / 2 - .1, face_y - .12, z1), (c + width / 2 + .1, wall_y, z1 + .25), s['trim'])


def bay_front(h):
    """A side gable over a wall set back 0.9 m behind a two-storey canted bay. A house of one bay has
    no room beside its door, so it carries a first-floor oriel over the door and its wall stays on the line."""
    x0, x1, y0, y1 = h['x0'], h['x1'], h['y0'], h['y1']
    s = h['s']
    wall = s['mods'][0]['wall']
    w, H = x1 - x0, h['storeys'] * S
    n = max(1, round(w / BAY))
    b = w / n
    if n == 1:
        doors = walls(h)
        c, ow = (x0 + x1) / 2, min(2.4, w - .8)
        z0, z1 = S + .2, min(2 * S, H) - .2
        canted(c, ow, y0, y0 - .6, z0, z1, [(S + .5, z1 - .35)], wall, s)
        ts.box('oriel base', (c - ow / 2 - .1, y0 - .72, z0 - .25), (c + ow / 2 + .1, y0, z0), s['trim'])
        gable_along(h, y0, y1, H, h['pitch'])
        return doors
    yf = y0 + .9
    doors = walls(h, y0=yf)
    for x, inward in ((x0, 1), (x1, -1)):
        a, c = sorted((x, x + inward * .25))
        ts.box('pier', (a, y0, 0), (c, yf, H), wall)
    at = n - 2 if h['mirror'] else 1
    bc, bw = x0 + (at + .5) * b, min(3.4, b - .4)
    canted(bc, bw, yf, y0, .3, min(2 * S, H), [(.95, 2.65), (S + .75, S + 2.6)], wall, s)
    ts.box('bay base', (bc - bw / 2, y0, 0), (bc + bw / 2, yf, .3), 'plinth')
    gable_along(h, yf, y1, H, h['pitch'])
    return doors


def back_building(h):
    """A side-gabled front range and a two-storey back building on one side of the yard. Pairs of
    houses put their back buildings against their shared wall."""
    x0, x1, y0, y1 = h['x0'], h['x1'], h['y0'], h['y1']
    s = h['s']
    d, w, H = y1 - y0, x1 - x0, h['storeys'] * S
    ym = y0 + max(6.0, round(.7 * d * 2) / 2)
    ww = min(w - 1.5, max(2.4, .55 * w))
    side = h['wing']
    wx0, wx1 = (x1 - ww, x1) if side > 0 else (x0, x0 + ww)
    doors = walls(h, y1=ym, back=False)
    hf.elevation('back', ts.Face((x1, ym, 0), (-1, 0, 0), (0, 1, 0)), w, H,
                 [o for o in back_openings(w, h['storeys']) if not (x1 - o[0] - o[2] < wx1 + .1 and x1 - o[0] > wx0 - .1 and o[1] < 2 * S)],
                 s, s['mods'][0]['wall'])
    inner_left = side > 0
    hw = {'s': s, 'x0': wx0, 'x1': wx1, 'y0': ym, 'y1': y1, 'storeys': 2, 'mirror': False}
    walls(hw, front=False, sides=(True, True))
    inner = ts.Face((wx0, y1, 0), (0, -1, 0), (-1, 0, 0)) if inner_left else ts.Face((wx1, ym, 0), (0, 1, 0), (1, 0, 0))
    ln = y1 - ym
    wins = [(ln * c - .55, k * S + .75, 1.1, hf.window_height(), 'window') for k in range(2) for c in (.35, .75) if ln >= 3]
    hf.elevation('wing inner', inner, ln, 2 * S, wins, s, s['mods'][0]['wall'])
    hi, lo = 2 * S + 1.1, 2 * S
    xi, xo = (wx0, wx1) if inner_left else (wx1, wx0)
    poly('wing roof', [(xi - (.25 if inner_left else -.25), ym, lo - .1), (xo, ym, hi), (xo, y1 + .3, hi), (xi - (.25 if inner_left else -.25), y1 + .3, lo - .1)], 'roof')
    poly('wing end', [(xi, y1, lo), (xo, y1, lo), (xo, y1, hi)], s['mods'][0]['wall'])
    gable_along(h, y0, ym, H, h['pitch'])
    return doors


FORMS = {
    'side-gable': (side_gable, (20, 40)),
    'parapet': (parapet, None),
    'mansard': (mansard, None),
    'front-gable': (front_gable, (30, 45)),
    'paired-gable': (paired_gable, (25, 35)),
    'bay-front': (bay_front, (20, 40)),
    'back-building': (back_building, (20, 40)),
}
NARROW_ONLY = {'paired-gable'}


def pitch_range(form, w):
    return FORMS[form][1]


def build_row(houses, origin_x, d, storeys, y_front):
    """houses: list of (form, width, terrace look, house seed, pitch). Adjacent houses are attached."""
    x = origin_x
    pair_index = {}
    for i, (form, w, look, seed, pitch) in enumerate(houses):
        run_start = i
        while run_start > 0 and houses[run_start - 1][2] is look and houses[run_start - 1][0] == form:
            run_start -= 1
        pos = i - run_start
        pair = 0
        if form == 'paired-gable':
            run_end = i
            while run_end + 1 < len(houses) and houses[run_end + 1][2] is look and houses[run_end + 1][0] == form:
                run_end += 1
            if pos % 2 == 0 and i < run_end:
                pair = 1
            elif pos % 2 == 1:
                pair = -1
        s = house_look(look, seed, w)
        h = {'s': s, 'x0': x, 'x1': x + w, 'y0': y_front, 'y1': y_front + d, 'storeys': storeys,
             'left': i > 0, 'right': i + 1 < len(houses), 'mirror': pos % 2 == 1, 'pitch': pitch,
             'pair': pair, 'wing': 1 if pos % 2 == 0 else -1}
        fn = FORMS[form][0] if not (form == 'paired-gable' and pair == 0) else front_gable
        fn(h)
        x += w
    return x


def terrace_of(form, n, w, d, storeys, r):
    look = terrace_look(r.randrange(1 << 30), w, d, storeys)
    rng = pitch_range(form, w)
    pitch = r.uniform(*rng) if rng else 0
    return [(form, w, look, r.randrange(1 << 30), pitch) for _ in range(n)]


def tile(form, seed=404):
    hf.fresh()
    camera = hf.stage(road_y=(-14.0, -6.0))
    scene = bpy.context.scene
    scene.render.resolution_x, scene.render.resolution_y = 1400, 760
    r = random.Random(seed)
    x = -34.0
    for w in (4.0, 8.0, 12.0):
        n = 4 if w == 4 else 2 if w == 12 else 3
        if w == 12 and form in NARROW_ONLY:
            x += 2 * w + 6
            continue
        x = build_row(terrace_of(form, n, w, 12.0, 3, r), x, 12.0, 3, -3.0) + 6
    for part in ts.parts:
        ts.project_uvs(part)
    hf.shoot(camera, f'tile-{form}', (-14, -70, 40), (-2, 3, 4))
    if form == 'back-building':
        hf.shoot(camera, f'tile-{form}-rear', (14, 80, 40), (2, 3, 4))


def street(name, seed, mode):
    """Fourteen 8 m houses a side. Terraces: runs of 3 to 6 houses share a form, and neighboring runs
    differ. Houses: each house takes its own form, and neighbors differ."""
    hf.fresh()
    camera = hf.stage(road_y=(-5.0, 5.0))
    r = random.Random(seed)
    names = list(FORMS)
    w, d = 8.0, 12.0
    for turned, y in ((False, 8.0), (True, -8.0)):
        houses, last = [], None
        while len(houses) < 14:
            form = r.choice([f for f in names if f != last])
            if mode == 'terraces':
                n = min(r.randint(3, 6), 14 - len(houses))
                houses += terrace_of(form, n, w, d, 3, r)
            else:
                n = 2 if form == 'paired-gable' and len(houses) <= 12 else 1
                houses += terrace_of(form, n, w, d, 3, r)
            last = form
        start = len(ts.parts)
        build_row(houses, -56.0, d, 3, 0.0)
        for part in ts.parts[start:]:
            if turned:
                part.rotation_euler[2] = math.pi
                part.location = (0, y, 0)
            else:
                part.location = (0, y, 0)
    for part in ts.parts:
        ts.project_uvs(part)
    hf.shoot(camera, f'{name}-game', (0, -110, 95), (0, 8, 0))
    hf.shoot(camera, f'{name}-street', (-62, -3, 5), (-20, 12, 8))


JOBS = {f'tile-{f}': (lambda f=f: tile(f)) for f in FORMS}
JOBS['street-terraces'] = lambda: street('street-terraces', 31, 'terraces')
JOBS['street-houses'] = lambda: street('street-houses', 31, 'houses')

for name, job in JOBS.items():
    if not ONLY or name in ONLY:
        job()
        print('DONE', name)

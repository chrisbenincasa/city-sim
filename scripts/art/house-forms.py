"""Detached house forms: one house on a narrow Lot, varied by roof and plan. FamilyBodyBuilder.Detached.cs
builds the same forms; the walls, openings and dressing come from house-families.py. Pitch ranges match
the builder and stop at 45 degrees, 48 for the half-hip, until a form needs a steeper roof. The L-plan
and rear outshot draw less floor than the Building holds and the side wing draws more
(docs/07-the-drawing.md).

blender --background --factory-startup --python-exit-code 1 --python scripts/art/house-forms.py -- REPO OUT [job...]

Jobs: tile-<form> for each form, street-mixed and street-two.
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

W, D = 8.0, 12.0
E = .35


def look(seed, storeys=2):
    s = hf.draw(seed, W, D, storeys, 'terrace')
    m = s['mods'][0]
    m['w'], m['mirror'], m['bay'] = W, False, False
    s['mods'] = [m]
    s['canopy'] = random.Random(seed).choice(['flat', 'gable', 'porch', 'none'])
    hf.bind(s)
    ts.materials['door'] = ts.materials[m['door']]
    return s


def poly(name, pts, material):
    clean = []
    for p in pts:
        if not clean or max(abs(a - b) for a, b in zip(p, clean[-1])) > 1e-4:
            clean.append(p)
    if len(clean) > 2 and max(abs(a - b) for a, b in zip(clean[0], clean[-1])) < 1e-4:
        clean.pop()
    if len(clean) >= 3:
        ts.mesh(name, clean, [tuple(range(len(clean)))], material)


def block(s, x0, x1, y0, y1, storeys, front=True, back=True, ends=True, end_storeys=None):
    """Walls of one rectangular volume. Front faces -Y."""
    w, d, H = x1 - x0, y1 - y0, storeys * S
    m = dict(s['mods'][0], w=w)
    keep = s['storeys']
    s['storeys'] = storeys
    doors = []
    if front:
        face = ts.Face((x0, y0, 0), (1, 0, 0), (0, -1, 0))
        openings = hf.house_front(m, s)
        hf.elevation('front', face, w, H, openings, s, m['wall'])
        door = openings[0]
        hf.canopy(face, door[0], door[2], s, door[1] + door[3])
        doors.append(x0 + door[0] + .5)
    if back:
        hf.elevation('back', ts.Face((x1, y1, 0), (-1, 0, 0), (0, 1, 0)), w, H, hf.house_back(m, s), s, m['wall'])
    if ends:
        es = end_storeys or storeys
        s['storeys'] = es
        windows = hf.ends(d, s) if d >= 3.5 else []
        hf.elevation('left end', ts.Face((x0, y1, 0), (0, -1, 0), (-1, 0, 0)), d, es * S, windows, s, s['end'])
        hf.elevation('right end', ts.Face((x1, y0, 0), (0, 1, 0), (1, 0, 0)), d, es * S, windows, s, s['end'])
    s['storeys'] = keep
    ts.box('plinth', (x0 - .05, y0 - .05, 0), (x1 + .05, y1 + .05, .3), 'plinth')
    return doors


def mapper(axis, cx, cy):
    if axis == 'y':
        return lambda a, l, z: (cx + a, cy + l, z)
    return lambda a, l, z: (cx + l, cy + a, z)


def hipgable(s, axis, cx, cy, A, L, H, pitch, k=0.0, walls=(-1, 1), eaves=E):
    """Ridge along `axis`. k=0 is a gable, k=1 a full hip, between is a half-hip."""
    P = mapper(axis, cx, cy)
    t = math.tan(math.radians(pitch))
    top, low = H + A * t, H - eaves * t
    Ae, Le = A + eaves, L + eaves
    zk = top - k * (top - low)
    ak = (top - zk) / t
    ry = max(0.0, Le - ak)
    for side in (-1, 1):
        pts = [P(side * Ae, -Le, low), P(side * ak, -Le, zk), P(0, -ry, top), P(0, ry, top), P(side * ak, Le, zk), P(side * Ae, Le, low)]
        poly('roof slope', pts, 'roof')
    for f in (-1, 1):
        if ak > 1e-3:
            poly('hip', [P(-ak, f * Le, zk), P(ak, f * Le, zk), P(0, f * ry, top)], 'roof')
    for f in walls:
        if zk <= H + 1e-3:
            continue
        aw = min(A, (top - zk) / t)
        poly('gable wall', [P(-A, f * L, H), P(A, f * L, H), P(aw, f * L, zk), P(-aw, f * L, zk)], 'wall')
    return top


def lean_to(x0, x1, y0, y1, z_high, z_low, fall='y'):
    """A mono-pitch roof falling from y0 (or x0) at z_high to y1 (or x1) at z_low, with end walls."""
    if fall == 'y':
        poly('lean-to', [(x0 - .2, y0, z_high), (x1 + .2, y0, z_high), (x1 + .2, y1 + .3, z_low - .15), (x0 - .2, y1 + .3, z_low - .15)], 'roof')
        for x in (x0, x1):
            poly('lean-to end', [(x, y0, S), (x, y1, S), (x, y1, z_low), (x, y0, z_high)], 'wall')
    else:
        poly('lean-to', [(x0, y0 - .2, z_high), (x0, y1 + .2, z_high), (x1 + .3, y1 + .2, z_low - .15), (x1 + .3, y0 - .2, z_low - .15)], 'roof')
        for y in (y0, y1):
            poly('lean-to end', [(x0, y, S), (x1, y, S), (x1, y, z_low), (x0, y, z_high)], 'wall')


X, Y, H = W / 2, D / 2, 2 * S


def side_gable(s, pitch=35):
    doors = block(s, -X, X, -Y, Y, 2)
    hipgable(s, 'x', 0, 0, Y, X, H, pitch)
    return doors


def front_gable(s, pitch=45):
    doors = block(s, -X, X, -Y, Y, 2)
    hipgable(s, 'y', 0, 0, X, Y, H, pitch)
    return doors


def hipped(s, pitch=30):
    doors = block(s, -X, X, -Y, Y, 2)
    hipgable(s, 'y', 0, 0, X, Y, H, pitch, k=1)
    return doors


def half_hip(s, pitch=45):
    doors = block(s, -X, X, -Y, Y, 2)
    hipgable(s, 'y', 0, 0, X, Y, H, pitch, k=.35)
    return doors


def cross_gable(s, pitch=35):
    doors = block(s, -X, X, -Y, Y, 2)
    hipgable(s, 'x', 0, 0, Y, X, H, pitch)
    side = random.Random(s['seed']).choice([-1, 1])
    hipgable(s, 'y', side * (X - 2.4), -Y / 2, 2.4, Y / 2, H, 42, walls=(-1,))
    return doors


def saltbox(s, pitch=38):
    doors = block(s, -X, X, -Y, Y, 2, back=False, ends=False)
    block(s, -X, X, -Y, Y, 1, front=False, back=True, ends=True)
    t = math.tan(math.radians(pitch))
    ar = -.3 * Y
    top = H + (Y + ar) * t
    zr = S + .5
    P = mapper('x', 0, 0)
    Le = X + .3
    rear = (top - zr) / (Y - ar)
    poly('front slope', [P(-Y - E, -Le, H - E * t), P(ar, -Le, top), P(ar, Le, top), P(-Y - E, Le, H - E * t)], 'roof')
    poly('rear slope', [P(ar, -Le, top), P(Y + E, -Le, zr - E * rear), P(Y + E, Le, zr - E * rear), P(ar, Le, top)], 'roof')
    for f in (-1, 1):
        poly('saltbox end', [P(-Y, f * X, S), P(Y, f * X, S), P(Y, f * X, zr), P(ar, f * X, top), P(-Y, f * X, H)], 'wall')
    return doors


def gambrel_house(s, dormer=None):
    """One storey of wall; the upper floor lives inside the gambrel, lit by end windows and a shed dormer."""
    doors = block(s, -X, X, -Y, Y, 1)
    lower, upper = math.tan(math.radians(66)), math.tan(math.radians(24))
    A, run, e = Y, 1.6, .45
    ak = A - run
    zk = S + run * lower
    top = zk + ak * upper
    low = S - e * .55
    Le = X + .25
    for side in (-1, 1):
        poly('gambrel lower', [(-Le, side * (A + e), low), (Le, side * (A + e), low), (Le, side * ak, zk), (-Le, side * ak, zk)], 'roof')
        poly('gambrel upper', [(-Le, side * ak, zk), (Le, side * ak, zk), (Le, 0, top), (-Le, 0, top)], 'roof')
    for f in (-1, 1):
        x = f * X
        poly('gambrel end', [(x, -A, S), (x, A, S), (x, ak, zk), (x, 0, top), (x, -ak, zk)], 'wall')
        for c in (-.3, .3):
            y = c * A
            o = x + f * .06
            ts.box('end frame', (min(x, o + f * .04), y - .62, S + .78), (max(x, o + f * .04), y + .62, S + 2.62), s['trim'])
            ts.box('end window', (min(x, o + f * .07), y - .5, S + .9), (max(x, o + f * .07), y + .5, S + 2.5), 'glass')
        poly('verge', [(x + f * .02, -A - e, low - .05), (x + f * .02, -ak, zk - .05), (x + f * .02, 0, top - .05),
                       (x + f * .02, 0, top + .12), (x + f * .02, -ak, zk + .12), (x + f * .02, -A - e, low + .12)], s['trim'])
        poly('verge', [(x + f * .02, A + e, low - .05), (x + f * .02, ak, zk - .05), (x + f * .02, 0, top - .05),
                       (x + f * .02, 0, top + .12), (x + f * .02, ak, zk + .12), (x + f * .02, A + e, low + .12)], s['trim'])
    if dormer if dormer is not None else random.Random(s['seed'] + 3).random() < .6:
        dx, front = X - 1.0, -(A - .35)
        dz = zk + .2
        ts.box('shed dormer', (-dx, front, S), (dx, -1.2, dz), s['end'])
        poly('dormer roof', [(-dx - .25, front - .4, dz + .05), (dx + .25, front - .4, dz + .05), (dx + .25, -1.0, top - .9), (-dx - .25, -1.0, top - .9)], 'roof')
        for c in (-.5, .5):
            wx = c * dx
            ts.box('dormer frame', (wx - .72, front - .06, S + .78), (wx + .72, front, S + 2.42), s['trim'])
            ts.box('dormer window', (wx - .6, front - .09, S + .9), (wx + .6, front - .06, S + 2.3), 'glass')
    return doors


def l_plan(s, pitch=38):
    """The front recessed 1.6 m beside a full-height gabled wing, all inside the 8 x 12 m footprint."""
    side = random.Random(s['seed'] + 1).choice([-1, 1])
    wing = 4.4
    r = -Y + 1.6
    wx0, wx1 = (-X, -X + wing) if side < 0 else (X - wing, X)
    doors = block(s, -X, X, r, Y, 2)
    block(s, wx0, wx1, -Y, r + .01, 2, back=False)
    hipgable(s, 'x', 0, (r + Y) / 2, (Y - r) / 2, X, H, pitch)
    hipgable(s, 'y', (wx0 + wx1) / 2, -Y / 2, wing / 2, Y / 2, H, 42, walls=(-1,))
    return doors


def rear_outshot(s, pitch=40):
    """Two storeys at the front, a one-storey kitchen range behind, inside the footprint."""
    split = Y - 4.0
    doors = block(s, -X, X, -Y, split, 2)
    block(s, -X, X, split, Y, 1, front=False)
    hipgable(s, 'x', 0, (-Y + split) / 2, (split + Y) / 2, X, H, pitch)
    lean_to(-X, X, split, Y, S + 1.3, S)
    return doors


def side_wing(s, pitch=40, side=None):
    """The full house plus a one-storey wing 3 m into the side garden: outside the footprint."""
    doors = block(s, -X, X, -Y, Y, 2)
    hipgable(s, 'x', 0, 0, Y, X, H, pitch)
    side = side or random.Random(s['seed'] + 2).choice([-1, 1])
    x0, x1 = (X, X + 3.0) if side > 0 else (-X - 3.0, -X)
    block(s, x0, x1, -Y + 2.5, Y - 1.0, 1, front=True, back=True, ends=False)
    if side > 0:
        lean_to(x0, x1, -Y + 2.5, Y - 1.0, S + 1.2, S, fall='x')
    else:
        poly('lean-to', [(x1, -Y + 2.3, S + 1.2), (x1, Y - .8, S + 1.2), (x0 - .3, Y - .8, S - .15), (x0 - .3, -Y + 2.3, S - .15)], 'roof')
        for y in (-Y + 2.5, Y - 1.0):
            poly('lean-to end', [(x0, y, S), (x1, y, S), (x1, y, S + 1.2), (x0, y, S)], 'wall')
    return doors


ROOFS = [('side-gable', side_gable), ('front-gable', front_gable), ('hipped', hipped), ('half-hip', half_hip),
         ('cross-gable', cross_gable), ('saltbox', saltbox), ('gambrel', gambrel_house)]
SHAPES = [('l-plan', l_plan), ('rear-outshot', rear_outshot), ('side-wing', side_wing)]


def finish(s, doors):
    hf.street_details(s, doors)


def tile(name, build, seed=404, **kw):
    hf.fresh()
    camera = hf.stage(road_y=(-205.0, -195.0))
    scene = bpy.context.scene
    scene.render.resolution_x, scene.render.resolution_y = 900, 700
    s = look(seed)
    finish(s, build(s, **kw))
    for part in ts.parts:
        ts.project_uvs(part)
    hf.shoot(camera, name, (-17, -25, 15), (0, 0, 4.2))


def street(name, seed, forms, pitch_ranges):
    hf.fresh()
    camera = hf.stage()
    r = random.Random(seed)
    for row, turned, y in ((0, False, 8 + 4 + Y), (1, True, -(8 + 4 + Y))):
        for i in range(7):
            cx = (i - 3) * 16.0
            fname, build = r.choice(forms)
            s = look(r.randrange(1 << 30))
            start = len(ts.parts)
            kw = {}
            if fname in pitch_ranges:
                kw['pitch'] = r.uniform(*pitch_ranges[fname])
            finish(s, build(s, **kw))
            for part in ts.parts[start:]:
                if turned:
                    part.rotation_euler[2] = math.pi
                part.location = (cx if not turned else -cx, y, 0)
    for part in ts.parts:
        ts.project_uvs(part)
    hf.shoot(camera, f'{name}-game', (0, -95, 80), (0, 6, 0))
    hf.shoot(camera, f'{name}-street', (-50, -2, 4), (-10, 14, 6))


JOBS = {}
for n, b in ROOFS + SHAPES:
    JOBS[f'tile-{n}'] = (lambda n=n, b=b: tile(f'tile-{n}', b, **({'side': -1} if n == 'side-wing' else {})))
JOBS['tile-gambrel-plain'] = lambda: tile('tile-gambrel-plain', gambrel_house, seed=405, dormer=False)
JOBS['tile-gambrel-dormer'] = lambda: tile('tile-gambrel-dormer', gambrel_house, seed=406, dormer=True)
RANGES = {'side-gable': (22, 40), 'front-gable': (30, 45), 'hipped': (22, 35), 'half-hip': (38, 48),
          'cross-gable': (28, 40), 'saltbox': (32, 42), 'l-plan': (30, 40), 'rear-outshot': (30, 40), 'side-wing': (30, 40)}
JOBS['street-mixed'] = lambda: street('street-mixed', 77, ROOFS + SHAPES, RANGES)
JOBS['street-two'] = lambda: street('street-two', 77, ROOFS[:2], {})

for name, job in JOBS.items():
    if not ONLY or name in ONLY:
        job()
        print('DONE', name)

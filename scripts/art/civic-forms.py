"""School and Gate forms: the mid-rise bodies a School and a Gate (kind "port") draw. A school stands on a
Lot the player picks, 3 storeys on 48 x 20 m in the founding run. A Gate stands on an edge Lot, 3 storeys on
28 x 24 m in the founding run and 2 storeys on 60 x 20 m in the fixtures.

blender --background --factory-startup --python-exit-code 1 --python scripts/art/civic-forms.py -- OUT [job...]

Jobs: export, sheet-<form> for each form in FORMS, lineup-school and lineup-gate.

Schools draw roof-play (contemporary), ribbon (mid-century) and board (traditional). Gates draw interchange
(contemporary), hall (contemporary) and station (traditional).

export is the shell's reference. It builds each form's reference sites near and far and writes
art/civic-forms/bodies.json with each site's faces and corners per part and its bounds. CivicBodyTests holds
FamilyBodyBuilder.BuildMidrise to it. It also writes art/civic-forms/lineup.blend and
assets/midrise-families/civic.glb, which carries every part's swatch named part first, as the shell matches
them.

Each form draws its walls face by face with the mid-rise roles and open stretches of midrise-families.py, so
attached flanks and courtyard rings follow the shell's builder. The signature pieces stand on the first
wing's street face. The shell gives a civic Lot a 1-2 m front yard, so nothing reaches more than 2.5 m past
the footprint.

Far mode draws every wall as one far-facade quad per storey with the near bay count, and keeps the roofs,
towers and canopies that make the silhouette. Its cells are 0 party, 1 ground, 2 typical and 4 end, the same
ids as the mansion block's.
"""
import importlib.util
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

ARGS = sys.argv[sys.argv.index('--') + 1:]
OUT, ONLY = Path(ARGS[0]), ARGS[1:]
ROOT = Path(__file__).resolve().parents[2]

_spec = importlib.util.spec_from_file_location('midrise', ROOT / 'scripts/art/midrise-families.py')
mr = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(mr)
tall, street = mr.tall, mr.street
Geometry, Side, touching = mr.Geometry, mr.Side, mr.touching
STOREY, WALL, SIDES = mr.STOREY, mr.WALL, mr.SIDES
wall_with_openings, bays, centred = mr.wall_with_openings, mr.bays, mr.centred

for texture in ('bricks-051', 'bricks-066', 'bricks-085', 'painted-plaster-wall', 'wood-siding-011', 'hinoki-planks',
                'preconcrete-wall-001', 'concrete-tile-facade', 'standing-seam', 'roofing-tiles-003',
                'roofing-tiles-007', 'bitumen', 'concrete-panels'):
    spec = tall.LIBRARY['textures'][texture]
    street.SURFACES[texture] = {'metres': spec['tile_metres'],
                                'mean': tall.LIBRARY['paint_mean_linear_luminance'] if spec['paint'] else None,
                                'maps': {key: ROOT / f['file'] for key, f in spec['files'].items()}}

S = STOREY
FAR_CELLS = {'party': 0, 'ground': 1, 'typical': 2, 'end': 4}

# Each form's proportions in metres. bay: the window module the far facade repeats.
LOOKS = {
    'roof-play': {'bay': 3.4, 'window': [2.4, 2.1, .8], 'end_bay': 5.0, 'end_window': 1.6, 'reveal': .3,
                  'parapet': .5, 'entrance': 8.0, 'entrance_share': .3, 'recess': 2.0},
    'ribbon': {'bay': 2.4, 'ribbon': [.95, 2.9], 'spandrel': [.12, .9], 'fascia': [.1, .45, .4], 'hall': 15.0,
               'hall_share': .32, 'hall_least': 30.0, 'hall_rise': 2.5, 'stack': 8.0},
    'board': {'bay': 3.0, 'window': [1.3, 2.5, .6], 'group': 15.0, 'step': 2.0, 'pitch': 45.0, 'gable': 9.0,
              'reveal': .2, 'plinth': .45, 'cornice': .15},
    'interchange': {'bay': 3.0, 'recess': 4.0, 'recess_share': .2, 'fin': 1.2, 'ribbon': [.5, 3.0],
                    'canopy': 2.5, 'pylon': 1.8, 'parapet': .6, 'column': 7.2},
    'hall': {'bay': 4.0, 'pitch': 35.0, 'mullion': 1.6, 'portal': [9.0, 4.8], 'ribbon': [1.0, 2.6], 'span': 40.0},
    'station': {'bay': 3.0, 'window': [1.3, 2.3, .7], 'pitch': 35.0, 'pavilion': [12.0, .42], 'forward': 1.0,
                'rise': 1.5, 'tower': 3.4, 'canopy': 2.0, 'reveal': .2},
}

# kind, period, base parts and paint schemes. A part is (texture or None, sRGB). A scheme overrides parts.
COMMON = {'glass': (None, '2f3a42'), 'door': (None, '2b2e31'), 'membrane': ('bitumen', None),
          'metal': (None, '2f3437'), 'lobby': (None, 'f2efe6'), 'far-facade': (None, 'a0948a')}
FORMS = {
    'roof-play': {'kind': 'school', 'period': 'contemporary',
                  'parts': {'wall': ('bricks-085', None), 'wall-end': ('bricks-085', None), 'trim': (None, '3a3d40'),
                            'frame': (None, '2b2e31'), 'band': (None, 'eeeeea'), 'paving': ('concrete-panels', 'b9b6ad'),
                            'court': (None, '3f7d4f'), 'canopy': (None, 'eeeeea'), 'accent': (None, 'e0a526')},
                  'schemes': [{}, {'wall': ('painted-plaster-wall', 'd8d6d0'), 'wall-end': ('painted-plaster-wall', 'd8d6d0'),
                               'accent': (None, '2f8f8a'), 'court': (None, 'b5553c')},
                              {'wall': ('bricks-066', None), 'wall-end': ('bricks-066', None), 'accent': (None, 'd0603a'),
                               'court': (None, '3b6e8f')}]},
    'ribbon': {'kind': 'school', 'period': 'mid-century',
               'parts': {'wall': ('preconcrete-wall-001', 'd6d2c8'), 'wall-end': ('bricks-051', None), 'trim': (None, 'eeeeea'),
                         'spandrel': (None, 'a9c3cf'), 'panel': (None, 'd8c27a'), 'canopy': (None, 'eeeeea')},
               'schemes': [{}, {'spandrel': (None, 'c45a3b'), 'panel': (None, 'e3dccb')},
                           {'spandrel': (None, '6f8f7a'), 'panel': (None, 'd8c27a'), 'wall-end': ('bricks-066', None)}]},
    'board': {'kind': 'school', 'period': 'traditional',
              'parts': {'wall': ('bricks-066', None), 'band': (None, 'e3dccb'), 'trim': (None, 'ece8dc'),
                        'frame': (None, 'ece8dc'), 'roof': ('roofing-tiles-003', None)},
              'schemes': [{}, {'wall': ('bricks-051', None), 'band': (None, 'd8d2c4')},
                          {'wall': ('bricks-085', None), 'roof': ('roofing-tiles-007', None)}]},
    'interchange': {'kind': 'gate', 'period': 'contemporary',
                    'parts': {'wall': ('concrete-tile-facade', 'c8c3b8'), 'wall-end': ('concrete-tile-facade', '8a8d86'),
                              'trim': (None, 'eeeeea'), 'frame': (None, '3a3d40'), 'band': (None, 'eeeeea'),
                              'canopy': (None, 'eeeeea'), 'accent': (None, 'e0a526')},
                    'schemes': [{}, {'accent': (None, '2f8f8a'), 'wall': ('concrete-tile-facade', 'd6d2c8')},
                                {'accent': (None, 'd0603a'), 'wall': ('concrete-tile-facade', '8a8d86')}]},
    'hall': {'kind': 'gate', 'period': 'contemporary',
             'parts': {'wall': ('wood-siding-011', None), 'frame': (None, 'eeeeea'), 'trim': (None, '3a3d40'),
                       'roof': ('standing-seam', '4a4f53'), 'accent': (None, 'c4693f')},
             'schemes': [{}, {'wall': ('hinoki-planks', None), 'frame': (None, 'c9a477'), 'accent': (None, '3f6fb0')},
                         {'roof': ('standing-seam', '6e7377'), 'accent': (None, '7aa33c')}]},
    'station': {'kind': 'gate', 'period': 'traditional',
                'parts': {'wall': ('bricks-066', None), 'band': (None, 'e3dccb'), 'roof': ('roofing-tiles-003', None),
                          'canopy': (None, '2f4a3c')},
                'schemes': [{}, {'wall': ('bricks-051', None), 'canopy': (None, '6a2f2a')},
                            {'wall': ('bricks-085', None), 'band': (None, 'd8d2c4'), 'canopy': (None, '24303a')}]},
}

# Each kind's two Lots: frontage, depth, storeys. The first is the founding Lot.
LOTS = {'school': [(48, 20, 3), (28, 20, 3)], 'gate': [(28, 24, 3), (60, 20, 2)]}


# ---------------------------------------------------------------------------------------------
# Shared pieces

def stretches(rects, bounds, ring, attached):
    """Every open stretch of every face, with its wing index, side name and role."""
    for i, rect in enumerate(rects):
        for name in SIDES:
            side = Side(rect, name)
            role = mr.role_of(name, rect, bounds, ring, attached)
            cuts = sorted({0.0, side.length} | {e for o in rects if o is not rect
                                                 and (t := touching(side, rect, o)) for e in t})
            for a, b in zip(cuts, cuts[1:]):
                if any(o is not rect and (t := touching(side, rect, o)) and t[0] <= a + 1e-4 and b - 1e-4 <= t[1]
                       for o in rects):
                    continue
                yield i, rect, name, side, role, a, b


def far_wall(g, side, a, b, storey, role, bay, depth=0.0):
    """One plain wall quad per storey with the near stretch's bay count."""
    if role == 'party':
        cell = FAR_CELLS['party']
    else:
        cell = FAR_CELLS['end' if role == 'end' and storey else 'ground' if storey == 0 else 'typical']
    side.grid_plane(g, a, b, storey * S, (storey + 1) * S, depth, 0, len(bays(b - a, bay)), cell)


def far_walls(g, side, a, b, storeys, role, bay):
    for storey in range(storeys):
        far_wall(g, side, a, b, storey, role, bay)


def frame(g, side, o, d):
    u0, u1, z0, z1 = o[:4]
    side.slab(g, 'frame', u0, u1, z0, z0 + .06, d - .08, d)
    side.slab(g, 'frame', u0, u1, z1 - .06, z1, d - .08, d)
    side.slab(g, 'frame', u0, u0 + .06, z0, z1, d - .08, d)
    side.slab(g, 'frame', u1 - .06, u1, z0, z1, d - .08, d)


def parapet(g, side, a, b, top, height, part='wall'):
    side.slab(g, part, a, b, top, top + height, 0, WALL)
    side.slab(g, 'trim', a, b, top + height, top + height + .08, -.04, WALL + .04)


def flat_roofs(g, rects, top):
    for x0, y0, x1, y1 in rects:
        g.box('membrane', (x0 + .1, y0 + .1, top - .2), (x1 - .1, y1 - .1, top + .05))


def face_polygon(g, side, part, points, d):
    """A polygon in a face's plane from (u, z) points, anticlockwise seen from outside."""
    g.polygon(part, [side.point(u, z, d) for u, z in points])


def disc(g, side, part, u, z, radius, d, n=16):
    face_polygon(g, side, part, [(u + radius * math.cos(2 * math.pi * j / n), z + radius * math.sin(2 * math.pi * j / n))
                                 for j in range(n)], d)


def clock(g, side, u, z, radius, d):
    """A clock face with a dark rim and hands at ten past ten."""
    disc(g, side, 'metal', u, z, radius + .12, d)
    disc(g, side, 'lobby', u, z, radius, d - .01)
    for angle, length in ((math.pi / 3, radius * .55), (math.pi * 5 / 6, radius * .8)):
        a, b = math.cos(angle), math.sin(angle)
        face_polygon(g, side, 'metal', [(u - .05 * b, z + .05 * a), (u + .05 * b, z - .05 * a),
                                        (u + .05 * b + length * a, z - .05 * a + length * b),
                                        (u - .05 * b + length * a, z + .05 * a + length * b)], d - .02)


def pyramid(g, part, cx, cy, half, z, height):
    apex = (cx, cy, z + height)
    c = [(cx - half, cy - half, z), (cx + half, cy - half, z), (cx + half, cy + half, z), (cx - half, cy + half, z)]
    for k in range(4):
        g.polygon(part, [c[k], c[(k + 1) % 4], apex])


def hip(g, rect, z, rise, gable_w=False, gable_e=False):
    """A hipped roof over rect from eaves height z, rising rise metres per metre in. A gable_w or gable_e
    end stands upright against a party wall. Returns the ridge height."""
    x0, y0, x1, y1 = rect
    w, d = x1 - x0, y1 - y0
    if w >= d - 1e-3:
        h, cy = d / 2 * rise, (y0 + y1) / 2
        ra = x0 if gable_w else x0 + d / 2
        rb = x1 if gable_e else x1 - d / 2
        if rb - ra > 1e-3:
            g.polygon('roof', [(x0, y0, z), (x1, y0, z), (rb, cy, z + h), (ra, cy, z + h)])
            g.polygon('roof', [(x1, y1, z), (x0, y1, z), (ra, cy, z + h), (rb, cy, z + h)])
        else:
            g.polygon('roof', [(x0, y0, z), (x1, y0, z), (ra, cy, z + h)])
            g.polygon('roof', [(x1, y1, z), (x0, y1, z), (ra, cy, z + h)])
        g.polygon('wall' if gable_w else 'roof', [(x0, y1, z), (x0, y0, z), (ra, cy, z + h)])
        g.polygon('wall' if gable_e else 'roof', [(x1, y0, z), (x1, y1, z), (rb, cy, z + h)])
    else:
        h, cx = w / 2 * rise, (x0 + x1) / 2
        ra, rb = y0 + w / 2, y1 - w / 2
        g.polygon('roof', [(x0, y1, z), (x0, y0, z), (cx, ra, z + h), (cx, rb, z + h)])
        g.polygon('roof', [(x1, y0, z), (x1, y1, z), (cx, rb, z + h), (cx, ra, z + h)])
        g.polygon('roof', [(x0, y0, z), (x1, y0, z), (cx, ra, z + h)])
        g.polygon('roof', [(x1, y1, z), (x0, y1, z), (cx, rb, z + h)])
    return z + h


def grown(rect, by):
    x0, y0, x1, y1 = rect
    return (x0 - by, y0 - by, x1 + by, y1 + by)


def party_ends(rect, bounds, ring, attached):
    return (mr.role_of('w', rect, bounds, ring, attached) == 'party',
            mr.role_of('e', rect, bounds, ring, attached) == 'party')


def quoins(g, side, u, top, left):
    """Alternating long and short stone blocks up a corner, from u inward."""
    for k in range(int(math.floor((top - .4) / .7))):
        w = .6 if k % 2 == 0 else .4
        a, b = (u, u + w) if left else (u - w, u)
        side.slab(g, 'band', a, b, k * .7, k * .7 + .6, -.05, 0)


# ---------------------------------------------------------------------------------------------
# School: roof-play

def playground(g, rect, z, far):
    """A fenced play deck on the roof: a court with hoops, a shade canopy and a stair overrun."""
    x0, y0, x1, y1 = rect
    if x1 - x0 < 16 or y1 - y0 < 12:
        return
    g.box('paving', (x0 + .3, y0 + .3, z + .05), (x1 - .3, y1 - .3, z + .1))
    zc = z + .1
    cw, cd, cy = min(28.0, x1 - x0 - 12), min(15.0, y1 - y0 - 3), (y0 + y1) / 2
    cx0 = x0 + 1.5
    cx1 = cx0 + cw
    if cw >= 8:
        g.box('court', (cx0, cy - cd / 2, zc), (cx1, cy + cd / 2, zc + .03))
        if not far:
            mid = (cx0 + cx1) / 2
            lines = [((cx0, cy - cd / 2), (cx1, cy - cd / 2 + .08)), ((cx0, cy + cd / 2 - .08), (cx1, cy + cd / 2)),
                     ((cx0, cy - cd / 2), (cx0 + .08, cy + cd / 2)), ((cx1 - .08, cy - cd / 2), (cx1, cy + cd / 2)),
                     ((mid - .04, cy - cd / 2), (mid + .04, cy + cd / 2))]
            for (ax, ay), (bx, by) in lines:
                g.box('band', (ax, ay, zc + .03), (bx, by, zc + .045))
            for post, board in ((cx0 + .6, cx0 + .7), (cx1 - .6, cx1 - .7)):
                g.box('metal', (post - .08, cy - .08, zc), (post + .08, cy + .08, zc + 2.6))
                g.box('band', (board - .03, cy - .9, zc + 2.4), (board + .03, cy + .9, zc + 3.25))
    if not far:
        corners = [(x0 + .3, y0 + .3), (x1 - .3, y0 + .3), (x1 - .3, y1 - .3), (x0 + .3, y1 - .3)]
        for k in range(4):
            (ax, ay), (bx, by) = corners[k], corners[(k + 1) % 4]
            n = max(1, round(math.hypot(bx - ax, by - ay) / 3))
            for j in range(n):
                px, py = ax + (bx - ax) * j / n, ay + (by - ay) * j / n
                g.box('metal', (px - .05, py - .05, zc), (px + .05, py + .05, zc + 3.2))
            for h in (1.1, 2.2, 3.14):
                g.box('metal', (min(ax, bx) - .02, min(ay, by) - .02, zc + h),
                      (max(ax, bx) + .02, max(ay, by) + .02, zc + h + .06))
    sx = cx1 + 1 if cw >= 8 else x0 + 1.5
    sw = min(9.0, x1 - 5.5 - sx)
    if sw >= 3:
        if not far:
            for px in (sx, sx + sw):
                for py in (y0 + 1.5, y1 - 1.5):
                    g.box('metal', (px - .08, py - .08, zc), (px + .08, py + .08, zc + 2.8))
        g.box('canopy', (sx - .4, y0 + 1.1, zc + 2.8), (sx + sw + .4, y1 - 1.1, zc + 2.95))
    g.box('accent', (x1 - 4.5, y1 - 6, zc), (x1 - .5, y1 - .5, zc + 3.1))
    g.box('metal', (x1 - 4.6, y1 - 6.1, zc + 3.1), (x1 - .4, y1 - .4, zc + 3.25))


def roof_play(g, rects, bounds, ring, attached, storeys, far):
    """An urban school: a double-height entrance cut into the frontage with a colored lining, classroom
    windows in deep reveals, and a fenced playground on the roof."""
    look = LOOKS['roof-play']
    top = storeys * S
    d = look['reveal'] + WALL
    ww, wh, sill = look['window']
    for i, rect, name, side, role, a, b in stretches(rects, bounds, ring, attached):
        if far:
            far_walls(g, side, a, b, storeys, role, look['bay'])
            if role != 'party':
                parapet(g, side, a, b, top, look['parapet'], 'far-facade')
            continue
        if role == 'party':
            side.slab(g, 'wall', a, b, 0, top, 0, WALL)
            continue
        e0 = e1 = None
        if i == 0 and name == 's' and b - a >= 16:
            ew = min(look['entrance'], (b - a) * look['entrance_share'])
            e0 = (a + b - ew) / 2
            e1 = e0 + ew
        lifted = min(2, storeys)
        bay, width = (look['end_bay'], look['end_window']) if role == 'end' else (look['bay'], ww)
        for k in range(storeys):
            z = k * S
            entry = e0 is not None and k < lifted
            holes = []
            for u0, u1 in bays(b - a, bay):
                o = centred(a + u0, a + u1, width, z + sill, wh)
                if entry and o[1] > e0 - .3 and o[0] < e1 + .3:
                    continue
                holes.append(o)
            cut = [(e0, e1, z, z + S, None)] if entry else []
            wall_with_openings(g, side, 'wall', a, b, z, z + S, holes + cut, 0, d)
            for o in holes:
                frame(g, side, o, d)
        if e0 is not None:
            eh, r, c = lifted * S, look['recess'], (e0 + e1) / 2
            side.plane(g, 'glass', e0, e1, 0, eh, r)
            side.slab(g, 'accent', e0, e0 + .1, 0, eh, 0, r)
            side.slab(g, 'accent', e1 - .1, e1, 0, eh, 0, r)
            side.slab(g, 'accent', e0, e1, eh - .1, eh, 0, r)
            side.plane(g, 'door', c - 1.2, c + 1.2, 0, 2.6, r - .02)
            if eh + 1 < top:
                side.slab(g, 'band', e0 + .4, e1 - .4, eh + .25, eh + .85, -.06, 0)
        parapet(g, side, a, b, top, look['parapet'])
    flat_roofs(g, rects, top)
    playground(g, rects[0], top, far)


# ---------------------------------------------------------------------------------------------
# School: ribbon

def hall_span(i, name, length, hw):
    """The ribbon school's hall on the first wing's faces, as a u interval, or None."""
    if i != 0 or not hw:
        return None
    return {'s': (length - hw, length), 'n': (0.0, hw), 'e': (0.0, length)}.get(name)


def ribbon_wall(g, side, a, b, storeys, role, look, front):
    top = storeys * S
    lo, hi = look['ribbon']
    s0, s1 = look['spandrel']
    if role == 'end':
        side.slab(g, 'wall-end', a, b, 0, top, 0, WALL)
        return
    modules = bays(b - a, look['bay'])
    for k in range(storeys):
        z = k * S
        side.slab(g, 'trim', a, b, z, z + s0, -.03, WALL)
        for j, (u0, u1) in enumerate(modules):
            side.slab(g, 'spandrel' if (j + k) % 2 == 0 else 'panel', a + u0, a + u1, z + s0, z + s1, -.03, WALL)
        side.slab(g, 'trim', a, b, z + s1, z + lo, -.05, WALL)
        side.plane(g, 'glass', a, b, z + lo, z + hi, .12)
        side.slab(g, 'wall', a, b, z + hi, z + S, 0, WALL)
    count = len(modules)
    for j in range(count + 1):
        u = min(max(a + (b - a) * j / count - .09, a), b - .18)
        side.slab(g, 'trim', u, u + .18, 0, top, -.08, 0)
    if front and b - a >= 8:
        c = (a + b) / 2
        side.plane(g, 'door', c - 1.2, c + 1.2, 0, 2.6, -.06)
        side.slab(g, 'canopy', c - 3.3, c + 3.3, 2.95, 3.15, -3, 0)
        for u in (c - 3, c + 3):
            side.slab(g, 'metal', u - .06, u + .06, 0, 2.95, -2.8, -2.68)


def ribbon(g, rects, bounds, ring, attached, storeys, far):
    """A light-frame school: ribbon windows over colored spandrel panels between slim white columns, a deep
    white roof edge, a taller hall with a clerestory, and a brick boiler stack."""
    look = LOOKS['ribbon']
    top = storeys * S
    x0, y0, x1, y1 = rects[0]
    hw = min(look['hall'], (x1 - x0) * look['hall_share']) if not ring and x1 - x0 >= look['hall_least'] else 0.0
    hh = top + look['hall_rise']
    f0, f1, out = look['fascia']
    for i, rect, name, side, role, a, b in stretches(rects, bounds, ring, attached):
        span = hall_span(i, name, side.length, hw)
        pieces = [(a, b, False)]
        if span:
            pieces = [(p, q, h) for p, q, h in ((a, min(b, span[0]), False), (max(a, span[0]), min(b, span[1]), True),
                                                (max(a, span[1]), b, False)) if q - p > 1e-4]
        for p, q, in_hall in pieces:
            if role == 'party':
                if far and not in_hall:
                    far_walls(g, side, p, q, storeys, role, look['bay'])
                else:
                    side.slab(g, 'wall', p, q, 0, hh if in_hall else top, 0, WALL)
                continue
            if far:
                far_walls(g, side, p, q, storeys, role, look['bay'])
                if in_hall:
                    side.slab(g, 'wall-end', p, q, top, hh, 0, WALL)
            elif in_hall:
                holes = [(p + .5, q - .5, hh - 2.3, hh - .7)] if q - p > 2 else []
                wall_with_openings(g, side, 'wall-end', p, q, 0, hh, holes, 0, WALL)
            else:
                ribbon_wall(g, side, p, q, storeys, role, look, i == 0 and name == 's')
            if in_hall:
                ext = .3 if role == 'end' else 0.0
                side.slab(g, 'trim', p - ext, q + ext, hh - .1, hh + .35, -.3, .3)
            else:
                ext = out if role == 'end' else 0.0
                side.slab(g, 'trim', p - ext, q + ext, top - f0, top + f1, -out, out)
    flat_roofs(g, rects, top)
    if hw:
        hall = (x1 - hw, y0, x1, y1)
        side = Side(hall, 'w')
        side.slab(g, 'wall-end', 0, side.length, top, hh, 0, WALL)
        side.slab(g, 'trim', 0, side.length, hh - .1, hh + .35, -.3, .3)
        g.box('membrane', (hall[0] + .1, y0 + .1, hh - .2), (x1 - .1, y1 - .1, hh + .05))
    sx = x1 - hw / 2 if hw else x1 - 3
    g.box('wall-end', (sx - .75, y1 - 1.7, top - .2), (sx + .75, y1 - .2, top + look['stack']))
    g.box('trim', (sx - .85, y1 - 1.8, top + look['stack']), (sx + .85, y1 - .1, top + look['stack'] + .3))


# ---------------------------------------------------------------------------------------------
# School: board

def board(g, rects, bounds, ring, attached, storeys, far):
    """A board school: tall brick storeys, a steep hipped roof, a gable over each group of tall windows,
    stone bands, two entrances, and a bell cupola on the ridge."""
    look = LOOKS['board']
    top = storeys * S
    rise = math.tan(math.radians(look['pitch']))
    d = look['reveal'] + WALL
    ww, wh, sill = look['window']
    x0, y0, x1, y1 = rects[0]
    groups = max(1, round((x1 - x0) / look['group']))
    gw = min(look['gable'], (x1 - x0) / groups - 3)
    c0 = look['cornice']
    for i, rect, name, side, role, a, b in stretches(rects, bounds, ring, attached):
        if role == 'party':
            if far:
                far_walls(g, side, a, b, storeys, role, look['bay'])
            else:
                side.slab(g, 'wall', a, b, 0, top, 0, WALL)
            continue
        ext = c0 if role == 'end' else 0.0
        side.slab(g, 'band', a - ext, b + ext, top - .3, top, -c0, 0)
        if far:
            far_walls(g, side, a, b, storeys, role, look['bay'])
            continue
        front = i == 0 and name == 's'
        doors = []
        centres = []
        if front:
            centres = [a + (b - a) * (k + .5) / groups for k in range(groups)]
            doors = [a + 1.6] + ([b - 3.4] if b - a > 20 else [])
        for k in range(storeys):
            z = k * S
            if front:
                windows = [(c + j * look['step'] - ww / 2, c + j * look['step'] + ww / 2, z + sill, z + sill + wh)
                           for c in centres for j in (-1, 0, 1)]
            else:
                windows = [centred(a + u0, a + u1, ww, z + sill, wh) for u0, u1 in bays(b - a, look['bay'])]
            entries = []
            if k == 0:
                windows = [o for o in windows if not any(o[1] > q - .3 and o[0] < q + 2.1 for q in doors)]
                entries = [(q, q + 1.8, look['plinth'], 3.0, 'door') for q in doors]
            wall_with_openings(g, side, 'wall', a, b, z, z + S, windows + entries, 0, d)
            for u0, u1, z0, z1 in windows:
                side.slab(g, 'band', u0 - .12, u1 + .12, z1, z1 + .3, -.05, 0)
                side.slab(g, 'frame', u0, u1, z0 + wh * .7 - .04, z0 + wh * .7 + .04, d - .07, d - .02)
        for q in doors:
            side.slab(g, 'band', q - .3, q, look['plinth'], 3.45, -.12, 0)
            side.slab(g, 'band', q + 1.8, q + 2.1, look['plinth'], 3.45, -.12, 0)
            side.slab(g, 'band', q - .3, q + 2.1, 3.0, 3.45, -.12, 0)
            side.slab(g, 'band', q - .5, q + 2.3, 3.45, 3.7, -.4, 0)
        for k in range(1, storeys):
            side.slab(g, 'band', a, b, k * S - .05, k * S + .2, -.06, 0)
        side.slab(g, 'band', a, b, 0, look['plinth'], -.08, 0)
    ridge = top
    for k, rect in enumerate(rects):
        west, east = party_ends(rect, bounds, ring, attached)
        z = hip(g, grown(rect, c0), top, rise, west, east)
        if k == 0:
            ridge = z
    rx0, ry0, rx1, ry1 = grown(rects[0], c0)
    if rx1 - rx0 < ry1 - ry0:
        return
    if gw >= 4:
        front = Side(rects[0], 's')
        peak, run = gw / 2 * rise, gw / 2 + .3
        for k in range(groups):
            c = x0 + (x1 - x0) * (k + .5) / groups
            g.polygon('roof', [(c - gw / 2, ry0, top), (c, ry0, top + peak), (c, ry0 + run, top + peak),
                               (c - gw / 2, ry0 + run, top)])
            g.polygon('roof', [(c, ry0, top + peak), (c + gw / 2, ry0, top), (c + gw / 2, ry0 + run, top),
                               (c, ry0 + run, top + peak)])
            g.polygon('wall', [(c - gw / 2, ry0, top), (c + gw / 2, ry0, top), (c, ry0, top + peak)])
            if far:
                continue
            u = c - x0
            for j in (-1, 0, 1):
                height = min(peak * .55, 3.2) - (.5 if j else 0)
                if height > .8:
                    front.slab(g, 'glass', u + j * 1.3 - .45, u + j * 1.3 + .45, top + .5, top + .5 + height, -c0 - .02, -c0)
                    front.slab(g, 'band', u + j * 1.3 - .55, u + j * 1.3 + .55, top + .5 + height, top + .7 + height,
                               -c0 - .05, -c0)
            front.slab(g, 'band', u - gw / 2, u + gw / 2, top, top + .2, -c0 - .06, -c0)
    bx, by = (x0 + x1) / 2, (y0 + y1) / 2
    zr = ridge - .4
    g.box('trim', (bx - 1.1, by - 1.1, zr - .6), (bx + 1.1, by + 1.1, zr + .1))
    g.box('trim', (bx - .9, by - .9, zr), (bx + .9, by + .9, zr + 2.4))
    if not far:
        g.box('metal', (bx - .55, by - .95, zr + .5), (bx + .55, by - .9, zr + 2.0))
    g.box('trim', (bx - 1.05, by - 1.05, zr + 2.4), (bx + 1.05, by + 1.05, zr + 2.6))
    pyramid(g, 'roof', bx, by, 1.05, zr + 2.6, 1.6)
    g.box('metal', (bx - .04, by - .04, zr + 4.2), (bx + .04, by + .04, zr + 5.0))
    reach = ((rx1 - rx0) - (ry1 - ry0)) / 2
    if reach > 2:
        for sx in (bx - min((x1 - x0) * .3, reach - 1), bx + min((x1 - x0) * .3, reach - 1)):
            g.box('wall', (sx - .6, by - .5, ridge - 1.5), (sx + .6, by + .5, ridge + 1.6))
            g.box('band', (sx - .7, by - .6, ridge + 1.6), (sx + .7, by + .6, ridge + 1.8))


# ---------------------------------------------------------------------------------------------
# Gate: interchange

def interchange(g, rects, bounds, ring, attached, storeys, far):
    """A transit interchange: a glazed concourse set back under the upper storeys, a thin canopy, fins over
    ribbon windows, and a clock pylon at one corner."""
    look = LOOKS['interchange']
    top = storeys * S
    x0, y0, x1, y1 = rects[0]
    r = min(look['recess'], (y1 - y0) * look['recess_share'])
    lo, hi = look['ribbon']
    for i, rect, name, side, role, a, b in stretches(rects, bounds, ring, attached):
        if role == 'party':
            if far:
                far_walls(g, side, a, b, storeys, role, look['bay'])
            else:
                side.slab(g, 'wall', a, b, 0, top, 0, WALL)
            continue
        front = i == 0 and name == 's'
        ga, gb = a, b
        if i == 0 and name == 'e':
            ga = max(a, r)
        if i == 0 and name == 'w':
            gb = min(b, side.length - r)
        if far:
            if front:
                far_wall(g, side, a, b, 0, role, look['bay'], r)
            elif gb - ga > 1e-3:
                far_wall(g, side, ga, gb, 0, role, look['bay'])
            for k in range(1, storeys):
                far_wall(g, side, a, b, k, role, look['bay'])
        else:
            if front:
                side.slab(g, 'metal', a, b, 0, .3, r, r + WALL)
                side.plane(g, 'glass', a, b, .3, S - .3, r + .1)
                for u0, _ in bays(b - a, look['bay'])[1:]:
                    side.slab(g, 'frame', a + u0 - .04, a + u0 + .04, .3, S - .3, r, r + .1)
                side.slab(g, 'metal', a, b, S - .3, S, r, r + WALL)
                side.slab(g, 'trim', a, b, S - .2, S, 0, r)
                spans = bays(b - a, look['column'])
                for u in [a + u0 for u0, _ in spans[1:]] + [b - .3]:
                    side.slab(g, 'metal', u - .15, u + .15, 0, S - .2, .2, .5)
                m0, m1 = spans[len(spans) // 2]
                c = a + (m0 + m1) / 2
                side.plane(g, 'door', c - 1.6, c + 1.6, 0, 2.7, r - .02)
            elif gb - ga > 1e-3:
                holes = [centred(ga + u0, ga + u1, 2.4, .6, 2.2) for u0, u1 in bays(gb - ga, 4.0)]
                wall_with_openings(g, side, 'wall-end', ga, gb, 0, S, holes, 0, .1 + WALL)
            for k in range(1, storeys):
                z = k * S
                wall_with_openings(g, side, 'wall', a, b, z, z + S, [(a + .4, b - .4, z + lo, z + hi)], 0, .15 + WALL)
            if role != 'end' and storeys > 1:
                n = max(1, round((b - a) / look['fin']))
                for j in range(n + 1):
                    u = a + (b - a) * j / n
                    side.slab(g, 'metal', max(a, u - .06), min(b, u + .06), S, top, -.5, 0)
        parapet(g, side, a, b, top, look['parapet'], 'far-facade' if far else 'wall')
        if front:
            side.slab(g, 'canopy', a, b, S - .05, S + .3, -look['canopy'], 0)
            side.slab(g, 'metal', a, b, S + .05, S + .4, -look['canopy'] - .05, -look['canopy'] + .1)
            p0, p1 = a + .3, a + .3 + look['pylon']
            side.slab(g, 'accent', p0, p1, 0, top + 7, -.6, 1.2)
            if not far:
                clock(g, side, (p0 + p1) / 2, top + 5.4, .7, -.61)
                side.slab(g, 'band', p0 + .2, p1 - .2, top + 2.2, top + 4.2, -.64, -.6)
    flat_roofs(g, rects, top)


# ---------------------------------------------------------------------------------------------
# Gate: hall

def gable_spans(length, most):
    n = 2 if length > most else 1
    return [(length * k / n, length * (k + 1) / n) for k in range(n)]


def hall(g, rects, bounds, ring, attached, storeys, far):
    """A great gabled hall: one or two steep gables to the street, glazed from the concourse floor to the
    ridge, timber sides with ribbon windows, and a deep portal around the doors."""
    look = LOOKS['hall']
    top = storeys * S
    rise = math.tan(math.radians(look['pitch']))
    lo, hi = look['ribbon']
    d = .1 + WALL
    for i, rect, name, side, role, a, b in stretches(rects, bounds, ring, attached):
        if role == 'party':
            if far:
                far_walls(g, side, a, b, storeys, role, look['bay'])
            else:
                side.slab(g, 'wall', a, b, 0, top, 0, WALL)
            continue
        if role == 'end':
            side.slab(g, 'trim', a, b, top - .25, top, -.25, 0)
        if far:
            far_walls(g, side, a, b, storeys, role, look['bay'])
            continue
        if i == 0 and name == 's':
            spans = [(a + p, a + q) for p, q in gable_spans(b - a, look['span'])]
            wall_with_openings(g, side, 'wall', a, b, 0, top, [(p + .5, q - .5, .3, top - .6) for p, q in spans], 0, d)
            for p, q in spans:
                gw, c = q - p, (p + q) / 2
                m = max(2, int(gw / look['mullion']))
                for j in range(1, m):
                    u = p + gw * j / m
                    side.slab(g, 'frame', u - .07, u + .07, .3, top + max(0.0, (gw / 2 - abs(u - c)) * rise - .4), -.14, -.02)
                for z in [k * S for k in range(1, storeys)] + [top - .1]:
                    side.slab(g, 'frame', p + .4, q - .4, z, z + .14, -.14, -.02)
                peak = gw / 2 * rise
                clock(g, side, c, top + peak * .55, min(1.1, peak * .18), -.15)
            pw, ph = look['portal']
            pw = min(pw, (spans[0][1] - spans[0][0]) * .5)
            pc = (a + b) / 2
            side.slab(g, 'accent', pc - pw / 2, pc - pw / 2 + .5, 0, ph - .2, -1.2, 0)
            side.slab(g, 'accent', pc + pw / 2 - .5, pc + pw / 2, 0, ph - .2, -1.2, 0)
            side.slab(g, 'accent', pc - pw / 2, pc + pw / 2, ph - .6, ph, -1.2, 0)
            side.plane(g, 'door', pc - 1.6, pc + 1.6, 0, 2.8, -.03)
        elif role == 'end':
            for k in range(storeys):
                z = k * S
                holes = [(a + .6, b - .6, z + lo, z + hi)] if b - a > 2 else []
                wall_with_openings(g, side, 'wall', a, b, z, z + S, holes, 0, d)
        else:
            for k in range(storeys):
                z = k * S
                holes = [centred(a + u0, a + u1, 2.0, z + .9, 1.8) for u0, u1 in bays(b - a, look['bay'])]
                wall_with_openings(g, side, 'wall', a, b, z, z + S, holes, 0, d)
    for k, (x0, y0, x1, y1) in enumerate(rects):
        along_x = not ring or x1 - x0 >= y1 - y0
        start, end = (x0, x1) if along_x else (y0, y1)
        for p, q in gable_spans(end - start, look['span']):
            p, q = start + p, start + q
            c, h = (p + q) / 2, top + (q - p) / 2 * rise
            if along_x:
                g.polygon('roof', [(p, y0, top), (c, y0, h), (c, y1, h), (p, y1, top)])
                g.polygon('roof', [(c, y0, h), (q, y0, top), (q, y1, top), (c, y1, h)])
                g.polygon('wall', [(p, y0, top), (q, y0, top), (c, y0, h)])
                g.polygon('wall', [(q, y1, top), (p, y1, top), (c, y1, h)])
                if k == 0 and not far:
                    g.polygon('glass', [(p + .4, y0 - .01, top), (q - .4, y0 - .01, top), (c, y0 - .01, h - .4)])
            else:
                g.polygon('roof', [(x0, p, top), (x1, p, top), (x1, c, h), (x0, c, h)])
                g.polygon('roof', [(x1, q, top), (x0, q, top), (x0, c, h), (x1, c, h)])
                g.polygon('wall', [(x0, q, top), (x0, p, top), (x0, c, h)])
                g.polygon('wall', [(x1, p, top), (x1, q, top), (x1, c, h)])


# ---------------------------------------------------------------------------------------------
# Gate: station

def station_wall(g, side, p, q, storeys, look, quoin_left, quoin_right):
    top = storeys * S
    d = look['reveal'] + WALL
    ww, wh, sill = look['window']
    for k in range(storeys):
        z = k * S
        windows = [centred(p + u0, p + u1, ww, z + sill, wh) for u0, u1 in bays(q - p, look['bay'])]
        wall_with_openings(g, side, 'wall', p, q, z, z + S, windows, 0, d)
        for u0, u1, z0, z1 in windows:
            side.slab(g, 'band', u0 - .15, u1 + .15, z1, z1 + .32, -.05, 0)
            side.slab(g, 'band', (u0 + u1) / 2 - .14, (u0 + u1) / 2 + .14, z1 - .05, z1 + .42, -.09, 0)
    if quoin_left:
        quoins(g, side, p, top, True)
    if quoin_right:
        quoins(g, side, q, top, False)


def station(g, rects, bounds, ring, attached, storeys, far):
    """A station head-house: brick wings under a hipped roof, a centre pavilion with a tall arched entrance,
    a clock tower with a pyramid roof, stone quoins and dressings, and glazed iron canopies on columns."""
    look = LOOKS['station']
    top = storeys * S
    rise = math.tan(math.radians(look['pitch']))
    forward = 0.0 if ring else look['forward']
    x0, y0, x1, y1 = rects[0]
    walls = rects if ring else [(x0, y0 + forward, x1, y1)]
    cx = (x0 + x1) / 2
    cw = min(look['pavilion'][0], (x1 - x0) * look['pavilion'][1])
    deep = min(6.0, walls[0][3] - walls[0][1] - 2)
    pavilion = (cx - cw / 2, y0, cx + cw / 2, walls[0][1] + deep)
    ph = top + look['rise']
    span = (pavilion[0] - x0, pavilion[2] - x0)
    for i, rect, name, side, role, a, b in stretches(walls, bounds, ring, attached):
        if role == 'party':
            if far:
                far_walls(g, side, a, b, storeys, role, look['bay'])
            else:
                side.slab(g, 'wall', a, b, 0, top, 0, WALL)
            continue
        ext = .15 if role == 'end' else 0.0
        side.slab(g, 'band', a - ext, b + ext, top - .3, top, -.15, 0)
        if far:
            far_walls(g, side, a, b, storeys, role, look['bay'])
            continue
        if i == 0 and name == 's':
            for p, q, left, right in ((a, min(b, span[0]), True, False), (max(a, span[1]), b, False, True)):
                if q - p < 1e-3:
                    continue
                station_wall(g, side, p, q, storeys, look, left, right)
                ua, ub = p + .4, q - .4
                if ub - ua >= 3:
                    c0 = look['canopy']
                    g.polygon('glass', [side.point(ua, 3.5, -c0), side.point(ub, 3.5, -c0), side.point(ub, 4.1, 0),
                                        side.point(ua, 4.1, 0)])
                    side.slab(g, 'canopy', ua, ub, 3.15, 3.55, -c0 - .05, -c0 + .05)
                    cols = max(1, round((ub - ua) / 4))
                    for j in range(cols + 1):
                        u = ua + .2 + (ub - ua - .4) * j / cols
                        side.slab(g, 'canopy', u - .1, u + .1, 0, 3.5, -c0 + .1, -c0 + .3)
        else:
            station_wall(g, side, a, b, storeys, look, True, True)
    ridge = top
    for k, rect in enumerate(walls):
        west, east = party_ends(rect, bounds, ring, attached)
        z = hip(g, grown(rect, .15), top, rise, west, east)
        if k == 0:
            ridge = z
    px0, py0, px1, py1 = pavilion
    if far:
        g.box('wall', (px0, py0, 0), (px1, py1, ph))
    else:
        front = Side(pavilion, 's')
        rad = min(2.6, cw * .22)
        spring = min(2 * S, ph - rad - 1.2)
        c = cw / 2
        d = look['reveal'] + WALL
        holes = [(c - rad, c + rad, 0, spring + rad, None)]
        uppers = [centred(1.0, 2.2, 1.2, spring + .8, 2.2), centred(cw - 2.2, cw - 1.0, 1.2, spring + .8, 2.2)] if storeys > 2 else []
        wall_with_openings(g, front, 'wall', 0, cw, 0, ph, holes + uppers, 0, d)
        for u0, u1, z0, z1 in uppers:
            front.slab(g, 'band', u0 - .15, u1 + .15, z1, z1 + .32, -.05, 0)
        n = 12
        arc = [(math.cos(math.pi * j / n), math.sin(math.pi * j / n)) for j in range(n + 1)]
        face_polygon(g, front, 'wall', [(c - rad, spring + rad), (c - rad, spring)]
                     + [(c + rad * x, spring + rad * y) for x, y in reversed(arc[n // 2 + 1:n])] + [(c, spring + rad)], 0)
        face_polygon(g, front, 'wall', [(c + rad, spring + rad), (c, spring + rad)]
                     + [(c + rad * x, spring + rad * y) for x, y in reversed(arc[1:n // 2])] + [(c + rad, spring)], 0)
        front.plane(g, 'glass', c - rad, c + rad, 0, spring, d)
        face_polygon(g, front, 'glass', [(c + rad * x, spring + rad * y) for x, y in arc], d)
        outer = rad + .45
        for j in range(n):
            (xa, ya), (xb, yb) = arc[j], arc[j + 1]
            face_polygon(g, front, 'band', [(c + rad * xa, spring + rad * ya), (c + outer * xa, spring + outer * ya),
                                            (c + outer * xb, spring + outer * yb), (c + rad * xb, spring + rad * yb)], -.06)
        front.plane(g, 'door', c - 1.4, c + 1.4, 0, 3.0, d - .02)
        front.slab(g, 'band', -.2, cw + .2, ph - .4, ph, -.25, 0)
        quoins(g, front, 0, ph, True)
        quoins(g, front, cw, ph, False)
        east, west = Side(pavilion, 'e'), Side(pavilion, 'w')
        reach = py1 - py0
        if forward > 0:
            east.slab(g, 'wall', 0, forward, 0, ph, 0, WALL)
            west.slab(g, 'wall', reach - forward, reach, 0, ph, 0, WALL)
        east.slab(g, 'wall', forward, reach, top, ph, 0, WALL)
        west.slab(g, 'wall', 0, reach - forward, top, ph, 0, WALL)
        east.slab(g, 'band', 0, reach, ph - .4, ph, -.25, 0)
        west.slab(g, 'band', 0, reach, ph - .4, ph, -.25, 0)
        Side(pavilion, 'n').slab(g, 'wall', 0, cw, top, ph, 0, WALL)
    g.box('roof', (px0 - .1, py0 - .1, ph - .05), (px1 + .1, py1, ph + .25))
    tw = look['tower']
    tower = (cx - tw / 2, py0 + .4, cx + tw / 2, py0 + .4 + tw)
    th = max(ridge + 3, ph + 6.5)
    ty = (tower[1] + tower[3]) / 2
    g.box('wall', (tower[0], tower[1], ph), (tower[2], tower[3], th))
    if not far:
        clock(g, Side(tower, 's'), tw / 2, th - 1.9, 1.1, -.01)
    g.box('band', (tower[0] - .25, tower[1] - .25, th), (tower[2] + .25, tower[3] + .25, th + .35))
    pyramid(g, 'roof', cx, ty, tw / 2 + .25, th + .35, 3.2)
    g.box('metal', (cx - .05, ty - .05, th + 3.5), (cx + .05, ty + .05, th + 4.6))


BUILD = {'roof-play': roof_play, 'ribbon': ribbon, 'board': board, 'interchange': interchange, 'hall': hall,
         'station': station}


def body(g, form, frontage, depth, storeys, attached, far=False):
    rects = mr.wings(frontage, depth)
    ring = len(rects) > 1
    bounds = (-frontage / 2, -depth / 2, frontage / 2, depth / 2)
    BUILD[form](g, rects, bounds, ring, tuple(attached), storeys, far)
    return {'form': form, 'frontage': frontage, 'depth': depth, 'storeys': storeys, 'ring': ring,
            'attached': list(attached)}


# ---------------------------------------------------------------------------------------------
# Renders

MADE = {}


def material(name, texture, colour):
    key = (name, texture, colour)
    if key in MADE:
        return MADE[key]
    if texture:
        made = street.surface(f'{name}-{texture}-{colour}', texture, colour)
    else:
        made = bpy.data.materials.new(f'{name}-{colour}')
        made.use_nodes = True
        shader = made.node_tree.nodes['Principled BSDF']
        shader.inputs['Base Color'].default_value = street.rgb(colour) + (1,)
        shader.inputs['Roughness'].default_value = .12 if name == 'glass' else .8
        shader.inputs['Metallic'].default_value = .5 if name in ('glass', 'metal') else 0
        if name == 'lobby':
            shader.inputs['Emission Color'].default_value = street.rgb(colour) + (1,)
            shader.inputs['Emission Strength'].default_value = .6
    MADE[key] = made
    return made


def materials(form, scheme):
    parts = {**COMMON, **FORMS[form]['parts'], **FORMS[form]['schemes'][scheme]}
    return {part: material(part, *spec) for part, spec in parts.items()}


def reset():
    for thing in list(bpy.data.objects):
        bpy.data.objects.remove(thing)
    for data in list(bpy.data.meshes):
        bpy.data.meshes.remove(data)
    for made in list(bpy.data.materials):
        bpy.data.materials.remove(made)
    MADE.clear()


ROAD = (-29.0, -19.0)
PAVEMENT = 3.0
YARD = 2.0


def stage():
    reset()
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
    ground = Geometry()
    ground.box('grass', (-600, -600, -.05), (600, 600, -.02))
    ground.box('lots', (-400, ROAD[1] + PAVEMENT, -.02), (400, ROAD[1] + PAVEMENT + 60, .01))
    ground.box('road', (-400, ROAD[0], -.02), (400, ROAD[1], .02))
    ground.box('pavement', (-400, ROAD[1], -.02), (400, ROAD[1] + PAVEMENT, .14))
    tall.build('ground', ground, {'grass': material('grass', None, '3a4a2a'), 'lots': material('lots', None, '6b7a4c'),
                                  'road': material('road', None, '1d1e21'),
                                  'pavement': material('pavement', None, '8f8c86')})
    camera = bpy.data.objects.new('camera', bpy.data.cameras.new('camera'))
    camera.data.clip_end = 3000
    scene.collection.objects.link(camera)
    scene.camera = camera
    return camera


def place(form, frontage, depth, storeys, cx, scheme=0, attached=()):
    """A Building facing the road from the north side, its front YARD back from the pavement."""
    g = Geometry()
    body(g, form, frontage, depth, storeys, attached)
    thing = tall.build(f'{form}-{cx}', g, materials(form, scheme))
    thing.location = (cx, ROAD[1] + PAVEMENT + YARD + depth / 2, 0)


def neighbours(x0, x1, depth, storeys):
    """Plain massing for the Perimeter Lots either side, so the founding Lot reads as part of a block face."""
    y0 = ROAD[1] + PAVEMENT + YARD
    g = Geometry()
    for a, b in ((x0 - 18.0, x0), (x1, x1 + 18.0)):
        g.box('neighbour', (a, y0, 0), (b, y0 + depth, storeys * S))
    tall.build('neighbours', g, {'neighbour': material('neighbour', None, 'b8b5ae')})


def game(target, distance):
    p = math.radians(50)
    return (target[0], target[1] - math.cos(p) * distance, target[2] + math.sin(p) * distance), target, True


def shoot(camera, name, eye, target, shell=False):
    if shell:
        camera.data.sensor_fit, camera.data.angle = 'VERTICAL', math.radians(60)
    else:
        camera.data.sensor_fit, camera.data.lens = 'AUTO', 40
    camera.location = eye
    camera.rotation_euler = (Vector(target) - Vector(eye)).to_track_quat('-Z', 'Y').to_euler()
    bpy.context.scene.render.filepath = str(OUT / f'{name}.png')
    bpy.ops.render.render(write_still=True)


def sheet(form):
    """The form at its kind's two Lots: the founding Lot between attached neighbours, the other alone."""
    (w1, d1, st1), (w2, d2, st2) = LOTS[FORMS[form]['kind']]
    camera = stage()
    c1 = -10 - 18 - w1 / 2
    neighbours(c1 - w1 / 2, c1 + w1 / 2, d1, st1)
    place(form, w1, d1, st1, c1, 0, ('w', 'e'))
    c2 = 14 + w2 / 2
    place(form, w2, d2, st2, c2, 1)
    mid = ((c1 - w1 / 2 - 18) + (14 + w2)) / 2
    for shot, eye, target, *shell in [('game', *game((mid, 0, 0), 125)), ('far', *game((mid, 0, 0), 480)),
                                      ('near', (mid - 10, -190, 62), (mid, 4, 4)),
                                      ('street', (c1 - 34, -26, 1.7), (c1 + 6, -4, 6.5)),
                                      ('alone', (c2 + 30, -60, 14), (c2, 0, 5))]:
        shoot(camera, f'{form}-{shot}', eye, target, bool(shell))


def lineup(kind):
    """Every form of a kind at its founding Lot, left to right in the order of FORMS."""
    w, d, st = LOTS[kind][0]
    camera = stage()
    forms = [f for f in FORMS if FORMS[f]['kind'] == kind]
    gap = 14.0
    total = len(forms) * w + (len(forms) - 1) * gap
    for i, form in enumerate(forms):
        place(form, w, d, st, -total / 2 + w / 2 + i * (w + gap), i % 3)
    for shot, eye, target, *shell in [('game', *game((0, 0, 0), 150)), ('near', (-30, -235, 70), (0, 5, 4))]:
        shoot(camera, f'lineup-{kind}-{shot}', eye, target, bool(shell))


# ---------------------------------------------------------------------------------------------
# The shell's reference

SOURCES = ROOT / 'art/civic-forms'
EXPORT = ROOT / 'src/Borough.Godot/assets/midrise-families'

# Every part's swatch colour. A family's materials and paint scheme override any of them.
SWATCHES = {
    'wall': 'b0a89c', 'wall-end': '8a8780', 'trim': 'cfccc4', 'roof': '3a3d40', 'membrane': '2b2c2e',
    'glass': '2f3a42', 'door': '2b2e31', 'frame': '3a3d40', 'metal': '2f3437', 'spandrel': '3f7f7a',
    'panel': 'd2c9b4', 'paving': 'b9b6ad', 'band': 'e3dccb', 'accent': 'c4693f', 'canopy': 'eeeeea',
    'lobby': 'f2efe6', 'court': '3f7d4f', 'far-facade': 'a0948a',
}

# name, form, frontage, depth, storeys, attached sides. The founding Lots, the fixture Gate, an attached
# Lot and a courtyard ring for each form.
LINEUP = [
    *[(f'{form}-{tag}', form, w, d, st, att) for form in ('roof-play', 'ribbon', 'board')
      for tag, w, d, st, att in (('founding', 48, 20, 3, ()), ('small', 28, 20, 3, ('w',)),
                                 ('attached', 40, 24, 4, ('e',)), ('ring', 56, 52, 3, ()))],
    *[(f'{form}-{tag}', form, w, d, st, att) for form in ('interchange', 'hall', 'station')
      for tag, w, d, st, att in (('founding', 28, 24, 3, ()), ('fixture', 60, 20, 2, ()),
                                 ('attached', 36, 20, 3, ('e',)), ('ring', 52, 48, 3, ()))],
]


def swatch(part, colour):
    made = bpy.data.materials.new(f'{part}-civic')
    made.use_nodes = True
    rgba = street.rgb(colour) + (1,)
    shader = made.node_tree.nodes['Principled BSDF']
    shader.inputs['Base Color'].default_value = rgba
    shader.inputs['Roughness'].default_value = .15 if part == 'glass' else .8
    shader.inputs['Metallic'].default_value = .6 if part in ('glass', 'metal') else 0
    made.diffuse_color = rgba
    return made


def census(g):
    """Faces and corners per part and the site's bounds, which the shell's builder is tested against."""
    points = [v for vertices, _ in g.parts.values() for v in vertices]
    return {'faces': {part: len(faces) for part, (_, faces) in sorted(g.parts.items())},
            'corners': {part: sum(map(len, faces)) for part, (_, faces) in sorted(g.parts.items())},
            'bounds': [[round(min(p[i] for p in points), 3) for i in range(3)],
                       [round(max(p[i] for p in points), 3) for i in range(3)]]}


def export():
    SOURCES.mkdir(parents=True, exist_ok=True)
    EXPORT.mkdir(parents=True, exist_ok=True)
    bpy.context.preferences.filepaths.save_version = 0
    reset()
    made = {f'{part}-civic': swatch(part, colour) for part, colour in SWATCHES.items()}
    made['far-facade'] = made.pop('far-facade-civic')
    reports = []
    x = 0.0
    for name, form, frontage, depth, storeys, attached in LINEUP:
        near, far = Geometry(), Geometry()
        report = body(near, form, frontage, depth, storeys, attached)
        body(far, form, frontage, depth, storeys, attached, far=True)
        seen, distant = census(near), census(far)
        reports.append({'name': name, **report, **seen, 'far_faces': distant['faces'],
                        'far_corners': distant['corners'], 'far_bounds': distant['bounds'],
                        'far_cell_counts': tall.cell_histogram(far)})
        tall.build(f'{name}-near', mr.family_geometry('civic', near), made, (x + frontage / 2, 0))
        tall.build(f'{name}-far', mr.family_geometry('civic', far), made, (x + frontage / 2, 120))
        x += frontage + 30
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCES / 'lineup.blend'))

    reset()
    parts = [part for part in SWATCHES if part != 'far-facade']
    made = {f'{part}-civic': swatch(part, SWATCHES[part]) for part in parts}
    swatches = Geometry()
    for i, part in enumerate(parts):
        swatches.box(f'{part}-civic', (i * 2, 0, 0), (i * 2 + 1, 1, 1))
    library = tall.build('civic', swatches, made)
    for other in bpy.data.objects:
        other.select_set(False)
    library.select_set(True)
    bpy.context.view_layer.objects.active = library
    bpy.ops.export_scene.gltf(filepath=str(EXPORT / 'civic.glb'), export_format='GLB', use_selection=True,
                              export_apply=True, export_yup=True)

    (SOURCES / 'bodies.json').write_text(json.dumps({'storey_metres': STOREY, 'wing_metres': mr.WING,
                                                     'looks': LOOKS, 'far_cells': FAR_CELLS, 'sites': reports},
                                                    indent=2) + '\n')
    for report in reports:
        print('CIVIC', report['name'], report['faces'])


JOBS = {f'sheet-{f}': (lambda f=f: sheet(f)) for f in FORMS}
JOBS['lineup-school'] = lambda: lineup('school')
JOBS['lineup-gate'] = lambda: lineup('gate')
JOBS['export'] = export
OUT.mkdir(parents=True, exist_ok=True)
for name, job in JOBS.items():
    if not ONLY or name in ONLY:
        job()
        print('DONE', name)

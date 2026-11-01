"""Perimeter forms: contemporary and mid-century mid-rise bodies for Perimeter Lots at 3 storeys and up.

blender --background --factory-startup --python-exit-code 1 --python scripts/art/perimeter-forms.py -- OUT [job...]

Jobs: export, sheet-<form> for each form in FORMS, sheet-mansion for the shipped mansion block, and mix,
which draws the forms at the target period shares.

export is the shell's reference. It builds each form's reference sites near and far, and writes
art/perimeter-forms/bodies.json with each site's faces and corners per part and its bounds, the looks and
the far cell table. PerimeterBodyTests holds FamilyBodyBuilder.BuildMidrise to it. It also writes
art/perimeter-forms/lineup.blend and assets/midrise-families/perimeter.glb, which carries the untextured
part swatches named part first, as the shell matches them. The shell's front setback stands the body
back from its street, so a reference site fills its whole footprint.

Every sheet stands two blocks of a founding run's Lots, taken from the base Ruleset's founding log at
Day 45, with their footprints, fronts and storeys. Streets are 8 m wide on the block lines, as the shell
draws them. A sheet's body draws its street front 2-4 m back inside its footprint, and the strip in front
becomes a garden with a hedge and paths. Wings, face roles and openings come from midrise-families.py,
so rings follow the shell's builder.

Far mode draws every wall as one far-facade quad per storey with the near bay count. Its cells are
0 party, 1 ground, 2 typical, 3 attic and 4 end, the same ids as the mansion block's.
"""
import importlib.util
import json
import math
import random
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
wall_with_openings, bays, entries, centred, railing = (mr.wall_with_openings, mr.bays, mr.entries,
                                                       mr.centred, mr.railing)

for texture in ('bricks-088', 'brick-wall-001', 'wood-siding-011', 'wood-siding-012', 'wood-siding-013',
                'plaster-001', 'standing-seam', 'concrete-wall-001', 'white-planks-clean', 'concrete-panels',
                'bitumen', 'aerial-grass-rock', 'gravelly-sand'):
    spec = tall.LIBRARY['textures'][texture]
    street.SURFACES[texture] = {'metres': spec['tile_metres'],
                                'mean': tall.LIBRARY['paint_mean_linear_luminance'] if spec['paint'] else None,
                                'maps': {key: ROOT / f['file'] for key, f in spec['files'].items()}}

COMMON = {'glass': (None, '2f3a42'), 'door': (None, '2b2e31'), 'membrane': ('bitumen', None),
          'paving': ('concrete-panels', 'b9b6ad'), 'metal': (None, '2f3437'), 'solar': (None, '1c2430'),
          'sedum': (None, '6f7a3e'), 'plant': (None, '9a9d9b'), 'canopy': (None, '3a3d40'),
          'lobby': (None, 'e8d9a8')}

# period, look, base parts and paint schemes for each candidate. bay: window module in metres.
# window: (width, height, sill). A scheme overrides parts; each Building draws one.
FORMS = {
    'brick-loggia': {
        'period': 'contemporary',
        'look': {'bay': 3.6, 'window': (1.8, 2.5, .4), 'reveal': .45, 'loggia_every': 3, 'loggia_depth': 1.6,
                 'attic': True, 'attic_setback': 2.2, 'parapet': .6, 'band': True, 'yard_balconies': True},
        'parts': {'attic': ('standing-seam', '5b5f62'), 'frame': (None, '3b3530'), 'reveal': (None, '8a7f74'),
                  'band': (None, 'c8c2b6')},
        'schemes': [
            {'wall': ('bricks-088', '5e4a3c'), 'wall-end': ('bricks-088', '5e4a3c'), 'trim': (None, '4a4744')},
            {'wall': ('bricks-088', '7b4a36'), 'wall-end': ('bricks-088', '7b4a36'), 'trim': (None, '3b3530')},
            {'wall': ('bricks-088', '4f4d4a'), 'wall-end': ('bricks-088', '4f4d4a'), 'trim': (None, '8a7f74'),
             'frame': (None, 'b08d57')},
        ],
    },
    'timber-frame': {
        'period': 'contemporary',
        'look': {'bay': 3.0, 'window': (1.4, 2.6, .1), 'reveal': .3, 'box_every': 4, 'attic': True,
                 'attic_setback': 2.0, 'parapet': .5, 'yard_balconies': True},
        'parts': {'attic': ('standing-seam', '3c4043'), 'reveal': (None, '6b5a48')},
        'schemes': [
            {'wall': ('wood-siding-011', None), 'wall-end': ('concrete-wall-001', 'a9a8a3'), 'frame': (None, '26292b'),
             'trim': (None, '2f3235')},
            {'wall': ('wood-siding-012', None), 'wall-end': ('concrete-wall-001', '5c5f60'), 'frame': (None, 'c9c6be'),
             'trim': (None, 'c9c6be')},
            {'wall': ('wood-siding-013', None), 'wall-end': ('concrete-wall-001', '8c8a84'), 'frame': (None, '3d4a3f'),
             'trim': (None, '3d4a3f')},
        ],
    },
    'white-render': {
        'period': 'contemporary',
        'look': {'bay': 3.2, 'widths': (1.1, 2.3, 1.1, 1.6), 'window_height': 1.7, 'sill': .8, 'reveal': .12,
                 'corner_balconies': True, 'loggia_depth': 1.8, 'attic': False, 'parapet': .4,
                 'slab_balconies': True, 'accent': True},
        'parts': {'frame': (None, '3a3d40'), 'reveal': (None, 'c9c5bc')},
        'schemes': [
            {'wall': ('plaster-001', 'd9d6cf'), 'wall-end': ('concrete-wall-001', '4d4f51'), 'trim': (None, 'cfccc4'),
             'accent': (None, 'c4693f')},
            {'wall': ('plaster-001', 'd6ccb6'), 'wall-end': ('concrete-wall-001', '5a5246'), 'trim': (None, 'cbbfa6'),
             'accent': (None, '3f6f8a')},
            {'wall': ('plaster-001', 'bcc7c2'), 'wall-end': ('concrete-wall-001', '3f4544'), 'trim': (None, 'b1bcb7'),
             'accent': (None, 'd2a23c')},
        ],
    },
    'gable-front': {
        'period': 'contemporary',
        'look': {'bay': 4.0, 'window': (1.0, 2.1, .6), 'reveal': .1, 'module': 8.0, 'deep': 16.0, 'pile': 12.0, 'pitch': 45, 'attic': False,
                 'gable_window': True},
        'parts': {'frame': (None, '2b2e31'), 'reveal': (None, '7d6e62')},
        'schemes': [
            {'wall': ('brick-wall-001', None), 'wall-end': ('brick-wall-001', None), 'roof': ('standing-seam', '3a3d40'),
             'trim': (None, '3a3d40')},
            {'wall': ('bricks-088', 'b3ada3'), 'wall-end': ('bricks-088', 'b3ada3'), 'roof': ('standing-seam', '56605a'),
             'trim': (None, '56605a')},
            {'wall': ('bricks-088', '6e4434'), 'wall-end': ('bricks-088', '6e4434'), 'roof': ('standing-seam', '7c7f80'),
             'trim': (None, '7c7f80')},
        ],
    },
    'ribbon': {
        'period': 'mid-century',
        'look': {'bay': 3.0, 'band': (.9, 2.3), 'mullion': 1.5, 'ground_recess': 1.6, 'canopy': 2.2,
                 'fascia': (.45, .7), 'attic': False, 'stair_tower': True},
        'parts': {'wall': ('plaster-001', 'd2cfc6'), 'wall-end': ('concrete-wall-001', '6b6d6e'),
                  'frame': (None, 'cfd2d0'), 'trim': (None, 'e2e0da'), 'reveal': (None, '5a5c5e')},
        'schemes': [{'spandrel': (None, '3f7f7a')}, {'spandrel': (None, 'c9a227')}, {'spandrel': (None, 'b4472f')}],
    },
    'crosswall': {
        'period': 'mid-century',
        'look': {'bay': 3.0, 'pier_every': 2, 'pier': (.6, .55), 'window': (2.0, 1.6, .9), 'reveal': .15,
                 'fascia': (.35, .9), 'attic': False},
        'parts': {'wall': ('bricks-088', '7a4a36'), 'wall-end': ('bricks-088', '7a4a36'),
                  'frame': (None, 'd8d5cd'), 'trim': (None, 'd8d5cd'), 'reveal': (None, '6d625a')},
        'schemes': [{'panel': ('white-planks-clean', 'd2c9b4')}, {'panel': ('white-planks-clean', '8fb3a8')},
                    {'panel': ('white-planks-clean', 'd9b56c')}],
    },
}
PERIOD_SHARES = [('contemporary', .55), ('mid-century', .25), ('traditional', .20)]
FAR_CELLS = {'party': 0, 'ground': 1, 'typical': 2, 'attic': 3, 'end': 4}

# Two blocks of the founding run at Day 45, in tiles: Lot, footprint x, y, width, height, front, storeys.
# Fronts are 's', 'n', 'w' or 'e'. Blocks are 32 tiles square and streets run on their edges.
TILE = 4.0
LOTS = [
    (0, 1, 2, 7, 6, 's', 3), (1, 11, 1, 7, 5, 's', 3), (2, 22, 1, 8, 7, 's', 3), (3, 2, 25, 12, 5, 'n', 3),
    (4, 16, 25, 15, 5, 'n', 4), (5, 2, 8, 12, 8, 'w', 4), (6, 1, 17, 15, 6, 'w', 3), (7, 17, 10, 13, 13, 'e', 4),
    (32, 34, 2, 9, 5, 's', 3), (33, 45, 2, 5, 4, 's', 3), (34, 53, 1, 10, 7, 's', 4), (35, 33, 25, 15, 6, 'n', 4),
    (36, 49, 26, 13, 5, 'n', 4), (37, 33, 9, 15, 7, 'w', 4), (38, 34, 17, 12, 6, 'w', 3), (39, 50, 8, 12, 14, 'e', 3),
]
STREETS_X, STREETS_Y = (0, 32, 64), (0, 32)
STREET, FOOTWAY = 8.0, 1.5
TURN = {'s': 0.0, 'e': math.pi / 2, 'n': math.pi, 'w': -math.pi / 2}


# ---------------------------------------------------------------------------------------------
# Faces

def entrance(g, side, u, look, d):
    """A door recessed 1.2 m under a projecting canopy, with a lit lobby slot beside it."""
    side.slab(g, 'reveal', u - .95, u - .9, 0, 2.9, d, d + 1.2)
    side.slab(g, 'reveal', u + .9, u + .95, 0, 2.9, d, d + 1.2)
    side.plane(g, 'door', u - .9, u + .9, 0, 2.6, d + 1.2)
    side.plane(g, 'lobby', u - .9, u + .9, 2.6, 2.9, d + 1.2)
    side.slab(g, 'canopy', u - 1.6, u + 1.6, 2.95, 3.12, -1.3, d)


def ground(g, side, a, b, role, look):
    doors = [a + e for e in entries(b - a, 14.0)] if role == 'street' else []
    d = look.get('reveal', WALL) + WALL
    if 'ground_recess' in look and role == 'street':
        r = look['ground_recess']
        side.slab(g, 'wall-end', a, b, 0, .3, -.05, r + WALL)
        holes = [(u0 + .15, u1 - .15, .3, 3.0) for u0, u1 in ((a + x, a + y) for x, y in bays(b - a, look['bay']))]
        wall_with_openings(g, side, 'wall-end', a, b, .3, STOREY, holes, r, r + WALL)
        for u in [a + .3] + [a + x for x, _ in bays(b - a, 6.0)][1:] + [b - .3]:
            side.slab(g, 'trim', u - .2, u + .2, 0, STOREY, .2, .6)
        side.slab(g, 'trim', a, b, STOREY - .25, STOREY, -look['canopy'], r)
        return
    openings = []
    for u0, u1 in bays(b - a, look['bay']):
        u0, u1 = a + u0, a + u1
        if any(u0 <= q < u1 for q in doors):
            continue
        if role == 'street':
            openings.append((u0 + .35, u1 - .35, .6, 3.0))
        elif role != 'end':
            openings.append(centred(u0, u1, 1.4, .9, 1.8))
    door_holes = [(q - .9, q + .9, 0, 2.9, None) for q in doors]
    wall_with_openings(g, side, 'wall-end', a, b, 0, STOREY, openings + door_holes, 0, d)
    for o in openings:
        frame(g, side, o, look)
    for q in doors:
        entrance(g, side, q, look, d)
    if look.get('band') is True:
        side.slab(g, 'band', a, b, STOREY - .35, STOREY, -.06, d)


def frame(g, side, o, look):
    u0, u1, z0, z1 = o[:4]
    d = look.get('reveal', WALL) + WALL
    side.slab(g, 'frame', u0, u1, z0, z0 + .06, d - .08, d)
    side.slab(g, 'frame', u0, u1, z1 - .06, z1, d - .08, d)
    side.slab(g, 'frame', u0, u0 + .06, z0, z1, d - .08, d)
    side.slab(g, 'frame', u1 - .06, u1, z0, z1, d - .08, d)


def upper_openings(form, look, a, b, storey, role, stretch):
    z = storey * STOREY
    openings, loggias, boxes = [], [], []
    for i, (u0, u1) in enumerate(stretch):
        u0, u1 = a + u0, a + u1
        if role == 'end' and i % 2:
            continue
        if form == 'white-render':
            corner = look['corner_balconies'] and role == 'street' and i in (0, len(stretch) - 1)
            if corner:
                loggias.append((u0, u1, z, z + STOREY - .35, None))
                continue
            w = look['widths'][(i + storey) % len(look['widths'])]
            openings.append(centred(u0, u1, w, z + look['sill'], look['window_height']))
        elif form == 'brick-loggia' and role == 'street' and i % look['loggia_every'] == 1 \
                and 0 < i < len(stretch) - 1:
            loggias.append((u0 + .3, u1 - .3, z, z + 2.9, None))
        else:
            w, h, sill = look['window']
            openings.append(centred(u0, u1, w, z + sill, h))
            if form == 'timber-frame' and role == 'street' and i % look['box_every'] == 2:
                boxes.append(openings[-1])
    return openings, loggias, boxes


def balcony(g, side, u0, u1, z, depth, part='trim'):
    side.slab(g, part, u0, u1, z - .18, z, -depth, 0)
    side.slab(g, 'glass', u0, u1, z, z + 1.0, -depth, -depth + .03)
    side.slab(g, part, u0, u1, z + 1.0, z + 1.06, -depth - .02, -depth + .05)


def cap(g, side, a, b, top, role, form, look, wall='wall'):
    """The top of a face: a fascia, a gable-front's eave, or a parapet with a coping."""
    if 'fascia' in look:
        h, out = look['fascia']
        side.slab(g, 'trim', a - (out if role == 'end' else 0), b, top - .05, top + h, -out, WALL)
    elif form == 'gable-front':
        side.slab(g, 'trim', a, b, top - .15, top, -.05, WALL)
    else:
        p = look.get('parapet', .5)
        side.slab(g, wall, a, b, top, top + p, 0, WALL)
        side.slab(g, 'trim', a, b, top + p, top + p + .08, -.04, WALL + .04)


def full_storeys(storeys, look):
    """The storeys below the set-back attic, which a form with an attic draws from 4 storeys."""
    return storeys - 1 if look['attic'] and storeys >= 4 else storeys


def stair_towers(g, side, a, b, top, role, look, far=False):
    if not (look.get('stair_tower') and role == 'street' and b - a > 20):
        return
    for q in [a + e for e in entries(b - a, 14.0)]:
        side.slab(g, 'wall', q - 1.6, q + 1.6, STOREY, top + 1.2, -1.0, 0)
        if not far:
            side.slab(g, 'glass', q - .5, q + .5, STOREY + .4, top - .4, -1.03, -1.0)


def face(g, side, a, b, storeys, role, form, look):
    if role == 'party':
        side.slab(g, 'wall', a, b, 0, storeys * STOREY, 0, WALL)
        return
    full = full_storeys(storeys, look)
    top = full * STOREY
    stretch = bays(b - a, look['bay'])
    ground(g, side, a, b, role, look)

    if form == 'ribbon':
        lo, hi = look['band']
        for storey in range(1, full):
            z = storey * STOREY
            side.slab(g, 'spandrel', a + .3, b - .3, z, z + lo, -.03, WALL)
            side.slab(g, 'wall', a, a + .3, z, z + STOREY, 0, WALL)
            side.slab(g, 'wall', b - .3, b, z, z + STOREY, 0, WALL)
            side.slab(g, 'spandrel', a + .3, b - .3, z + hi, z + STOREY, -.03, WALL)
            side.plane(g, 'glass', a + .3, b - .3, z + lo, z + hi, .12)
            n = max(1, round((b - a - .6) / look['mullion']))
            for k in range(n + 1):
                u = a + .3 + (b - a - .6) * k / n
                side.slab(g, 'frame', u - .04, u + .04, z + lo, z + hi, .05, .14)
        stair_towers(g, side, a, b, top, role, look)
        if role == 'yard':
            for storey in range(1, full):
                for i, (u0, u1) in enumerate(stretch):
                    if i % 2 == 0:
                        balcony(g, side, a + u0 + .2, a + u1 - .2, storey * STOREY, 1.3)
    elif form == 'crosswall':
        pw, out = look['pier']
        piers = [a] + [a + x for j, (x, _) in enumerate(stretch) if j and j % look['pier_every'] == 0] + [b]
        for storey in range(1, full):
            z = storey * STOREY
            w, h, sill = look['window']
            openings = [centred(a + u0, a + u1, w, z + sill, h) for u0, u1 in stretch]
            wall_with_openings(g, side, 'panel', a, b, z, z + STOREY, openings, .1, .1 + WALL)
            for o in openings:
                side.slab(g, 'frame', o[0], o[1], o[2] - .08, o[2], 0, .15)
            side.slab(g, 'trim', a, b, z - .12, z + .12, -.05, WALL)
            if role == 'yard':
                for p0, p1 in zip(piers, piers[1:]):
                    side.slab(g, 'trim', p0 + pw, p1 - pw, z - .15, z, -1.4, 0)
                    railing(g, side, p0 + pw, p1 - pw, z, -1.35)
        for u in piers:
            u0, u1 = max(a, u - pw / 2), min(b, u + pw / 2)
            side.slab(g, 'wall', u0, u1, STOREY, top, -out, WALL)
    else:
        for storey in range(1, full):
            z = storey * STOREY
            openings, loggias, boxes = upper_openings(form, look, a, b, storey, role, stretch)
            d = look['reveal'] + WALL
            wall_with_openings(g, side, 'wall', a, b, z, z + STOREY, openings + loggias, 0, d)
            for o in openings:
                frame(g, side, o, look)
            for u0, u1, za, zb, _ in loggias:
                ld = look['loggia_depth']
                wall_with_openings(g, side, 'reveal', u0, u1, za, zb,
                                   [centred(u0, u1, min(2.4, u1 - u0 - .4), za, 2.5) + ('glass',)], ld, ld + .1)
                side.slab(g, 'reveal', u0 - .05, u0, za, zb, d, ld)
                side.slab(g, 'reveal', u1, u1 + .05, za, zb, d, ld)
                side.slab(g, 'reveal', u0, u1, zb, zb + .05, d, ld)
                side.slab(g, 'trim', u0, u1, za, za + .1, 0, ld)
                railing(g, side, u0, u1, za + .1, .1)
            for u0, u1, za, zb in boxes:
                side.slab(g, 'trim', u0 - .25, u0, za - .25, zb + .25, -.5, 0)
                side.slab(g, 'trim', u1, u1 + .25, za - .25, zb + .25, -.5, 0)
                side.slab(g, 'trim', u0, u1, zb, zb + .25, -.5, 0)
                side.slab(g, 'trim', u0, u1, za - .25, za, -.5, 0)
            if form == 'timber-frame':
                for o in openings:
                    if o[:4] not in boxes:
                        railing(g, side, o[0], o[1], o[2], .05)
            if role == 'yard' and look.get('yard_balconies'):
                for i, (u0, u1) in enumerate(stretch):
                    if i % 3 == 1:
                        balcony(g, side, a + u0 - .4, a + u1 + .4, z, 1.6)
            if form == 'white-render' and look.get('slab_balconies') and role == 'street':
                for i, (u0, u1) in enumerate(stretch):
                    if 0 < i < len(stretch) - 1 and (i + storey) % 3 == 0:
                        balcony(g, side, a + u0 - .3, a + u1 + .3, z, 1.2, 'trim')
        if form == 'white-render' and look.get('accent') and role == 'street':
            for q in [a + e for e in entries(b - a, 14.0)]:
                side.slab(g, 'accent', q - 1.1, q + 1.1, STOREY, top, -.02, .02)
                for storey in range(1, full):
                    z = storey * STOREY
                    side.slab(g, 'glass', q - .35, q + .35, z + .6, z + 2.9, -.04, -.02)

    cap(g, side, a, b, top, role, form, look)


def far_wall(g, side, a, b, storey, role, look, cell=None):
    """One plain wall quad per storey with the near stretch's bay count."""
    if role == 'party':
        cell = FAR_CELLS['party']
    elif cell is None:
        cell = FAR_CELLS['end' if role == 'end' and storey else 'ground' if storey == 0 else 'typical']
    side.grid_plane(g, a, b, storey * STOREY, (storey + 1) * STOREY, 0, 0, len(bays(b - a, look['bay'])), cell)


def face_far(g, side, a, b, storeys, role, form, look):
    if role == 'party':
        for storey in range(storeys):
            far_wall(g, side, a, b, storey, role, look)
        return
    full = full_storeys(storeys, look)
    top = full * STOREY
    for storey in range(full):
        far_wall(g, side, a, b, storey, role, look)
    if form == 'ribbon':
        stair_towers(g, side, a, b, top, role, look, far=True)
    cap(g, side, a, b, top, role, form, look, 'far-facade')


def attic_face(g, side, a, b, z, look):
    openings = [centred(a + u0, a + u1, min(2.6, u1 - u0 - .5), z + .2, 2.6) for u0, u1 in bays(b - a, look['bay'])]
    wall_with_openings(g, side, 'attic', a, b, z, z + STOREY - .2, openings, 0, WALL)
    side.slab(g, 'trim', a, b, z + STOREY - .2, z + STOREY, -.1, WALL)


ROOF_KITS = ('solar', 'sedum', 'plant')


def roof_kit_of(frontage, depth, storeys):
    """A Building's roof detail, fixed by its size so the shell can share one mesh between equal bodies."""
    return ROOF_KITS[(int(frontage) + 3 * int(depth) + 7 * storeys) % 3]


def roof_kit(g, rect, z, kind):
    """Roof detail on a flat roof: solar rows, a sedum mat, or a plant room behind a screen."""
    x0, y0, x1, y1 = rect
    if x1 - x0 < 8 or y1 - y0 < 8:
        return
    if kind == 'sedum':
        g.box('sedum', (x0 + .7, y0 + .7, z + .05), (x1 - .7, y1 - .7, z + .15))
        return
    if kind == 'solar':
        for k in range(math.floor((y1 - y0 - 1.6) / 2.6 + 1e-3)):
            y = y0 + 1.5 + 2.6 * k
            g.polygon('solar', [(x0 + 1.5, y, z + .3), (x1 - 1.5, y, z + .3), (x1 - 1.5, y + 1.2, z + .85),
                                (x0 + 1.5, y + 1.2, z + .85)])
            g.box('metal', (x0 + 1.5, y + 1.1, z), (x1 - 1.5, y + 1.2, z + .85))
        return
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    g.box('plant', (cx - 3, cy - 2, z), (cx + 3, cy + 2, z + 2.2))
    g.box('metal', (cx - 3.4, cy - 2.4, z), (cx + 3.4, cy - 2.3, z + 2.4))
    g.box('metal', (cx - 3.4, cy + 2.3, z), (cx + 3.4, cy + 2.4, z + 2.4))


def gables(g, rects, ring, top, look, far=False):
    """Steep standing-seam gables. A shallow body turns a gable to the street every module; a deep solid
    body runs its ridges along the street, one pile at most 12 m deep after another."""
    rise = math.tan(math.radians(look['pitch']))
    for rect in rects:
        x0, y0, x1, y1 = rect
        deep = not ring and y1 - y0 > look['deep']
        along_x = not deep and (not ring or (x1 - x0) >= (y1 - y0))
        length = (x1 - x0) if along_x else (y1 - y0)
        n = math.ceil(length / look['pile']) if deep else max(1, round(length / look['module']))
        m = length / n
        for k in range(n):
            if along_x:
                a, b = x0 + k * m, x0 + (k + 1) * m
                c, h = (a + b) / 2, top + m / 2 * rise
                g.polygon('roof', [(a, y0, top), (c, y0, h), (c, y1, h), (a, y1, top)])
                g.polygon('roof', [(c, y0, h), (b, y0, top), (b, y1, top), (c, y1, h)])
                g.polygon('wall', [(a, y0, top), (b, y0, top), (c, y0, h)])
                g.polygon('wall', [(b, y1, top), (a, y1, top), (c, y1, h)])
                if look.get('gable_window') and not far:
                    y = y0 - .02
                    g.polygon('glass', [(c - .5, y, top + .4), (c + .5, y, top + .4),
                                        (c + .5, y, top + 2.4), (c - .5, y, top + 2.4)])
            else:
                a, b = y0 + k * m, y0 + (k + 1) * m
                c, h = (a + b) / 2, top + m / 2 * rise
                g.polygon('roof', [(x0, a, top), (x1, a, top), (x1, c, h), (x0, c, h)])
                g.polygon('roof', [(x1, b, top), (x0, b, top), (x0, c, h), (x1, c, h)])
                g.polygon('wall', [(x0, b, top), (x0, a, top), (x0, c, h)])
                g.polygon('wall', [(x1, a, top), (x1, b, top), (x1, c, h)])


def roofs(g, rects, bounds, ring, attached, storeys, form, look, kit, far=False):
    if look['attic'] and storeys >= 4:
        full_top = (storeys - 1) * STOREY
        s = look['attic_setback']
        attics = []
        for rect in rects:
            x0, y0, x1, y1 = rect
            st = [n for n in SIDES if mr.role_of(n, rect, bounds, ring, attached) in ('street', 'end')]
            attics.append((x0 + (s if 'w' in st else 0), y0 + (s if 's' in st else 0),
                           x1 - (s if 'e' in st else 0), y1 - (s if 'n' in st else 0)))
            g.box('paving', (x0 + .1, y0 + .1, full_top - .2), (x1 - .1, y1 - .1, full_top + .05))
            if far:
                continue
            for name in SIDES:
                if mr.role_of(name, rect, bounds, ring, attached) == 'street':
                    side = Side(rect, name)
                    side.slab(g, 'glass', .2, side.length - .2, full_top + .05, full_top + 1.05, .05, .08)
        for attic, rect in zip(attics, rects):
            for name in SIDES:
                side = Side(attic, name)
                cuts = sorted({0.0, side.length} | {e for o in attics if o is not attic
                                                     and (t := touching(side, attic, o)) for e in t})
                role = mr.role_of(name, rect, bounds, ring, attached)
                for a, b in zip(cuts, cuts[1:]):
                    if any(o is not attic and (t := touching(side, attic, o)) and t[0] <= a + 1e-4
                           and b - 1e-4 <= t[1] for o in attics):
                        continue
                    if far:
                        far_wall(g, side, a, b, storeys - 1, role, look, FAR_CELLS['attic'])
                        if role != 'party':
                            side.slab(g, 'trim', a, b, full_top + STOREY - .2, full_top + STOREY, -.1, WALL)
                    elif role == 'party':
                        side.slab(g, 'wall', a, b, full_top, full_top + STOREY, 0, WALL)
                    else:
                        attic_face(g, side, a, b, full_top, look)
            ax0, ay0, ax1, ay1 = attic
            g.box('membrane', (ax0 + .1, ay0 + .1, full_top + STOREY - .2), (ax1 - .1, ay1 - .1, full_top + STOREY))
            roof_kit(g, attic, full_top + STOREY, kit)
        return
    top = storeys * STOREY
    if form == 'gable-front':
        gables(g, rects, ring, top, look, far)
        return
    for rect in rects:
        x0, y0, x1, y1 = rect
        g.box('membrane', (x0 + .1, y0 + .1, top - .2), (x1 - .1, y1 - .1, top + .05))
        roof_kit(g, rect, top + .05, kit)


def body(g, form, frontage, depth, storeys, attached, far=False):
    look = {**FORMS[form]['look']}
    rects = mr.wings(frontage, depth)
    ring = len(rects) > 1
    bounds = (-frontage / 2, -depth / 2, frontage / 2, depth / 2)
    for rect in rects:
        for name in SIDES:
            side = Side(rect, name)
            cuts = sorted({0.0, side.length} | {e for o in rects if o is not rect
                                                 and (t := touching(side, rect, o)) for e in t})
            role = mr.role_of(name, rect, bounds, ring, attached)
            for a, b in zip(cuts, cuts[1:]):
                if any(o is not rect and (t := touching(side, rect, o)) and t[0] <= a + 1e-4 and b - 1e-4 <= t[1]
                       for o in rects):
                    continue
                (face_far if far else face)(g, side, a, b, storeys, role, form, look)
    roofs(g, rects, bounds, ring, attached, storeys, form, look, roof_kit_of(frontage, depth, storeys), far)
    return {'form': form, 'frontage': frontage, 'depth': depth, 'storeys': storeys, 'ring': ring,
            'attached': list(attached)}


def recess_for(frontage, depth, r):
    """How far a body's street front draws back: 2-4 m, at most a fifth of its depth, and never so far
    that a ring would draw as a solid body."""
    recess = min(r.uniform(2.0, 4.0), depth * .2)
    if frontage >= mr.RING_MINIMUM and depth >= mr.RING_MINIMUM:
        recess = min(recess, depth - mr.RING_MINIMUM)
    return max(0.0, round(recess * 2) / 2)


def garden(g, frontage, depth, recess, r):
    """The strip in front of a drawn-back body: lawn, a hedge on the street edge with gaps at the doors,
    paths to the doors, and trees where the strip is deep enough."""
    y0, y1 = -depth / 2, -depth / 2 + recess
    if recess <= 0:
        return
    g.box('lawn', (-frontage / 2, y0, .0), (frontage / 2, y1, .06))
    doors = [-frontage / 2 + e for e in entries(frontage, 14.0)]
    edges = sorted([-frontage / 2, frontage / 2] + [u for q in doors for u in (q - 1.2, q + 1.2)])
    hedge = r.random() < .6
    for a, b in zip(edges[0::2], edges[1::2]):
        if hedge:
            g.box('hedge', (a + .2, y0 + .2, 0), (b - .2, y0 + .9, 1.0))
        else:
            g.box('kerbwall', (a + .2, y0 + .2, 0), (b - .2, y0 + .5, .6))
    for q in doors:
        g.box('path', (q - 1.1, y0, 0), (q + 1.1, y1 + .1, .08))
    if recess >= 3:
        for a, b in zip(doors[:-1], doors[1:]):
            x = (a + b) / 2 + r.uniform(-1.5, 1.5)
            g.box('trunk', (x - .15, y0 + recess / 2 - .15, 0), (x + .15, y0 + recess / 2 + .15, 2.2))
            g.icosphere('leaf', (x, y0 + recess / 2, 3.6), 1.6)


# ---------------------------------------------------------------------------------------------
# Scene

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
        shader.inputs['Roughness'].default_value = .12 if name in ('glass', 'solar') else .8
        shader.inputs['Metallic'].default_value = .5 if name in ('glass', 'metal', 'solar') else 0
        if name == 'lobby':
            shader.inputs['Emission Color'].default_value = street.rgb(colour) + (1,)
            shader.inputs['Emission Strength'].default_value = 1.5
    MADE[key] = made
    return made


def materials(parts):
    return {part: material(part, *spec) for part, spec in parts.items()}


GROUNDS = {'lawn': (None, '5d7a3a'), 'hedge': (None, '2f4a25'), 'kerbwall': (None, 'a19a8c'),
           'path': ('concrete-panels', 'b9b6ad'), 'trunk': (None, '4a3a2c'), 'leaf': (None, '3f5f2c')}


def stage():
    for thing in list(bpy.data.objects):
        bpy.data.objects.remove(thing)
    for data in list(bpy.data.meshes):
        bpy.data.meshes.remove(data)
    for made in list(bpy.data.materials):
        bpy.data.materials.remove(made)
    MADE.clear()
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
    ground.box('grass', (-400, -400, -.05), (700, 500, -.02))
    span_x, span_y = STREETS_X[-1] * TILE, STREETS_Y[-1] * TILE
    ground.box('yard', (0, 0, -.02), (span_x, span_y, .01))
    half, foot = STREET / 2, FOOTWAY
    def gaps(lines, lo, hi):
        cuts = [lo] + [c for line in lines for c in (line * TILE - half, line * TILE + half)] + [hi]
        return list(zip(cuts[0::2], cuts[1::2]))

    for x in STREETS_X:
        x *= TILE
        ground.box('road', (x - half + foot, -60, -.02), (x + half - foot, span_y + 60, .02))
        for y0, y1 in gaps(STREETS_Y, -60, span_y + 60):
            ground.box('pavement', (x - half, y0, -.02), (x - half + foot, y1, .14))
            ground.box('pavement', (x + half - foot, y0, -.02), (x + half, y1, .14))
    for y in STREETS_Y:
        y *= TILE
        ground.box('road', (-60, y - half, -.02), (span_x + 60, y + half, .021))
        for x0, x1 in gaps(STREETS_X, -60, span_x + 60):
            ground.box('pavement', (x0, y - half, -.02), (x1, y - half + foot, .141))
            ground.box('pavement', (x0, y + half - foot, -.02), (x1, y + half, .141))
    tall.build('ground', ground, {'grass': material('grass', None, '3a4a2a'), 'yard': material('yard', None, '556b3a'),
                                  'road': material('road', None, '1d1e21'),
                                  'pavement': material('pavement', None, '8f8c86')})
    camera = bpy.data.objects.new('camera', bpy.data.cameras.new('camera'))
    camera.data.clip_end = 3000
    scene.collection.objects.link(camera)
    scene.camera = camera
    return camera


def place(name, geometry, made, centre, front, shift=(0, 0)):
    thing = tall.build(name, geometry, made, shift)
    thing.rotation_euler[2] = TURN[front]
    thing.location = (centre[0], centre[1], 0)
    return thing


def lot_frame(lot):
    """A Lot's centre in metres, and its frontage and depth relative to its front."""
    _, x, y, w, h, front, storeys = lot
    centre = ((x + w / 2) * TILE, (y + h / 2) * TILE)
    frontage, depth = (w * TILE, h * TILE) if front in 's n' else (h * TILE, w * TILE)
    return centre, frontage, depth, front, storeys


def stand(lot, form, seed):
    r = random.Random(seed)
    centre, frontage, depth, front, storeys = lot_frame(lot)
    name = f'lot-{lot[0]}'
    if form == 'mansion':
        g = Geometry()
        mr.site(g, 'mansion', frontage, depth, storeys)
        place(name, mr.family_geometry('mansion', g), mr_made(), centre, front)
        return
    recess = recess_for(frontage, depth, r)
    spec = FORMS[form]
    scheme = r.choice(spec['schemes'])
    made = materials({**COMMON, **spec['parts'], **scheme})
    g = Geometry()
    body(g, form, frontage, depth - recess, storeys, ())
    place(name, g, made, centre, front, (0, recess / 2))
    yard = Geometry()
    garden(yard, frontage, depth, recess, r)
    if yard.parts:
        place(f'{name}-garden', yard, materials(GROUNDS), centre, front)


MR_MADE = {}


def mr_made():
    if 'made' not in MR_MADE or not MR_MADE['made']['wall-mansion'].name:
        MR_MADE['made'] = mr.materials()
    return MR_MADE['made']


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


SHOTS = [
    ('game', *game((128, 58, 0), 165)),
    ('far', *game((128, 58, 0), 420)),
    ('near', (20, -70, 48), (90, 40, 4)),
    ('street', (129.6, -14, 1.7), (126, 70, 7)),
    ('frontage', (-6, -1.2, 1.7), (60, 9, 6)),
    ('ring', (178, 14, 46), (96, 66, 4)),
]


def sheet(form, tag=None):
    camera = stage()
    MR_MADE.clear()
    for i, lot in enumerate(LOTS):
        stand(lot, form, 1009 * (lot[0] + 1) + 31 * len(form))
    for shot, eye, target, *shell in SHOTS:
        shoot(camera, f'{tag or form}-{shot}', eye, target, bool(shell))


def mix(seed=7):
    """The forms at the target period shares: 9 contemporary, 4 mid-century and 3 traditional of 16 Lots.
    Mansion blocks admit 4 storeys and up, so traditional takes only 4-storey Lots."""
    camera = stage()
    MR_MADE.clear()
    r = random.Random(seed)
    lots = LOTS[:]
    r.shuffle(lots)
    tall_lots = [lot for lot in lots if lot[6] >= 4][:3]
    rest = [lot for lot in lots if lot not in tall_lots]
    cycles = {p: r.sample([f for f in FORMS if FORMS[f]['period'] == p],
                          k=sum(FORMS[f]['period'] == p for f in FORMS)) for p in ('contemporary', 'mid-century')}
    picks = {lot[0]: 'mansion' for lot in tall_lots}
    for i, lot in enumerate(rest):
        period = 'mid-century' if i < 4 else 'contemporary'
        k = i if i < 4 else i - 4
        picks[lot[0]] = cycles[period][k % len(cycles[period])]
    for lot in LOTS:
        print('MIX', lot[0], picks[lot[0]])
        stand(lot, picks[lot[0]], r.randrange(1 << 30))
    for shot, eye, target, *shell in SHOTS:
        shoot(camera, f'mix-{shot}', eye, target, bool(shell))


# ---------------------------------------------------------------------------------------------
# The shell's reference

SOURCES = ROOT / 'art/perimeter-forms'
EXPORT = ROOT / 'src/Borough.Godot/assets/midrise-families'

# The untextured parts' swatch colours. A family's paint scheme overrides any of them.
SWATCHES = {
    'trim': 'cfccc4', 'frame': '3a3d40', 'reveal': '8a7f74', 'band': 'c8c2b6', 'spandrel': '3f7f7a',
    'accent': 'c4693f', 'glass': '2f3a42', 'door': '2b2e31', 'metal': '2f3437', 'solar': '1c2430',
    'sedum': '6f7a3e', 'plant': '9a9d9b', 'canopy': '3a3d40', 'lobby': 'e8d9a8',
}
TEXTURED = {'wall': 'b0a89c', 'wall-end': '8a8780', 'attic': '5b5f62', 'roof': '3a3d40', 'panel': 'd2c9b4',
            'membrane': '2b2c2e', 'paving': 'b9b6ad', 'far-facade': 'a0948a'}

# name, form, frontage, depth, storeys, attached sides. Sizes are founding Lots and platted rings.
LINEUP = [
    ('brick-loggia-solid', 'brick-loggia', 48, 24, 4, ()),
    ('brick-loggia-low', 'brick-loggia', 28, 20, 3, ('w',)),
    ('brick-loggia-ring', 'brick-loggia', 56, 52, 5, ()),
    ('timber-frame-solid', 'timber-frame', 40, 20, 4, ('e',)),
    ('timber-frame-low', 'timber-frame', 32, 16, 3, ()),
    ('timber-frame-ring', 'timber-frame', 52, 48, 6, ()),
    ('white-render-solid', 'white-render', 44, 22, 4, ()),
    ('white-render-low', 'white-render', 24, 18, 3, ('w', 'e')),
    ('white-render-ring', 'white-render', 60, 52, 5, ()),
    ('gable-front-shallow', 'gable-front', 40, 16, 3, ()),
    ('gable-front-deep', 'gable-front', 36, 26, 4, ('w',)),
    ('gable-front-ring', 'gable-front', 56, 48, 4, ()),
    ('ribbon-solid', 'ribbon', 52, 22, 4, ()),
    ('ribbon-low', 'ribbon', 20, 16, 3, ('e',)),
    ('ribbon-ring', 'ribbon', 56, 56, 5, ()),
    ('crosswall-solid', 'crosswall', 44, 20, 4, ()),
    ('crosswall-low', 'crosswall', 28, 24, 3, ('w',)),
    ('crosswall-ring', 'crosswall', 52, 52, 4, ()),
]


def swatch(part, colour):
    made = bpy.data.materials.new(f'{part}-perimeter')
    made.use_nodes = True
    rgba = street.rgb(colour) + (1,)
    shader = made.node_tree.nodes['Principled BSDF']
    shader.inputs['Base Color'].default_value = rgba
    shader.inputs['Roughness'].default_value = .15 if part in ('glass', 'solar') else .8
    shader.inputs['Metallic'].default_value = .6 if part in ('glass', 'metal', 'solar') else 0
    made.diffuse_color = rgba
    return made


def census(g):
    """Faces and corners per part and the site's bounds, which the shell's builder is tested against."""
    points = [v for vertices, _ in g.parts.values() for v in vertices]
    return {'faces': {part: len(faces) for part, (_, faces) in sorted(g.parts.items())},
            'corners': {part: sum(map(len, faces)) for part, (_, faces) in sorted(g.parts.items())},
            'bounds': [[round(min(p[i] for p in points), 3) for i in range(3)],
                       [round(max(p[i] for p in points), 3) for i in range(3)]]}


def clear():
    for thing in list(bpy.data.objects):
        bpy.data.objects.remove(thing)
    for data in list(bpy.data.meshes):
        bpy.data.meshes.remove(data)
    for made in list(bpy.data.materials):
        bpy.data.materials.remove(made)
    MADE.clear()


def export():
    SOURCES.mkdir(parents=True, exist_ok=True)
    EXPORT.mkdir(parents=True, exist_ok=True)
    bpy.context.preferences.filepaths.save_version = 0
    clear()
    made = {f'{part}-perimeter': swatch(part, colour) for part, colour in {**SWATCHES, **TEXTURED}.items()}
    made['far-facade'] = made.pop('far-facade-perimeter')
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
        tall.build(f'{name}-near', mr.family_geometry('perimeter', near), made, (x + frontage / 2, 0))
        tall.build(f'{name}-far', mr.family_geometry('perimeter', far), made, (x + frontage / 2, 120))
        x += frontage + 30
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCES / 'lineup.blend'))

    clear()
    made = {f'{part}-perimeter': swatch(part, colour) for part, colour in SWATCHES.items()}
    swatches = Geometry()
    for i, part in enumerate(SWATCHES):
        swatches.box(f'{part}-perimeter', (i * 2, 0, 0), (i * 2 + 1, 1, 1))
    library = tall.build('perimeter', swatches, made)
    for other in bpy.data.objects:
        other.select_set(False)
    library.select_set(True)
    bpy.context.view_layer.objects.active = library
    bpy.ops.export_scene.gltf(filepath=str(EXPORT / 'perimeter.glb'), export_format='GLB', use_selection=True,
                              export_apply=True, export_yup=True)

    looks = {form: spec['look'] for form, spec in FORMS.items()}
    (SOURCES / 'bodies.json').write_text(json.dumps({'storey_metres': STOREY, 'wing_metres': mr.WING,
                                                     'looks': looks, 'far_cells': FAR_CELLS, 'sites': reports},
                                                    indent=2) + '\n')
    for report in reports:
        print('PERIMETER', report['name'], report['faces'])


JOBS = {f'sheet-{f}': (lambda f=f: sheet(f)) for f in [*FORMS, 'mansion']}
JOBS['mix'] = mix
JOBS['export'] = export
OUT.mkdir(parents=True, exist_ok=True)
for name, job in JOBS.items():
    if not ONLY or name in ONLY:
        job()
        print('DONE', name)

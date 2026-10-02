"""The mid-rise Building families: a brick mansion block and a precast panel slab, as solid bodies and rings.

Run with Blender in the background:
  blender --background --factory-startup --python-exit-code 1 --python scripts/art/midrise-families.py

platted.toml raises these above the test street's three storeys:

  perimeter      5 to 8 storeys    mansion block
  back-to-back   8 to 11 storeys   mansion block
  courtyard      12 to 15 storeys  panel slab
  slab           14 to 17 storeys  panel slab

A footprint at least 48 m each way is a ring (BuildingPlan.Hollow): south and north wings run the
full width, west and east wings stand between them, and every wing is 16 m thick. Any smaller
footprint is one solid body. Storeys are 3.5 m and are the simulation's; nothing here re-solves them.

Every face of a wing takes one role:
  street   on the site's edge, the frontage side of a solid body or any outer face of a ring
  yard     a ring's courtyard face, or a solid body's back
  end      a solid body's flank with no neighbour
  party    a flank against a neighbour, drawn blank

The mansion block is brick over a rendered ground storey, with punched windows, loggias on the
street, a cornice and a rendered attic set back behind a terrace. Its yard is the same brick
with projecting balconies. The panel slab is a precast grid on a recessed ground storey with pilotis,
loggia columns on the street, access galleries on the yard, and plant rooms on the roof.

The variants are saved as .blend references with a lineup for review. The only exports are
mansion.glb and slab.glb, which carry each family's untextured part materials, named part first so
the shell matches them by part; textured parts name their library texture in the Style Preset. Each body's origin is its site centre at ground level, and the street face
looks down -Y, which Godot receives as +Z.

Far mode uses one `far-facade` part for every wall. Its UVMap counts the near facade's bays in u and
storeys from ground level in v; uv2.x carries one cell id per face. Roof membranes, paving, plant
rooms and outline trim keep their near part names. The far cell table is:

  0  mansion-party/slab-party    1  mansion-ground      2  mansion-typical
  3  mansion-attic               4  mansion-end         5  slab-ground
  6  slab-typical                7  slab-end
"""
import bpy
import importlib.util
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
_spec = importlib.util.spec_from_file_location('tall_families', ROOT / 'scripts/art/tall-families.py')
tall = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(tall)
street = tall.street
Geometry, Side, touching, subtract = tall.Geometry, tall.Side, tall.touching, tall.subtract
cell_histogram = tall.cell_histogram

SOURCES = ROOT / 'art/midrise-families'
EXPORT = ROOT / 'src/Borough.Godot/assets/midrise-families'
STOREY = 3.5
WING = 16.0
RING_MINIMUM = 48.0
WALL = .25
SIDES = ('s', 'e', 'n', 'w')

FAR_CELLS = {
    'mansion-party': 0,
    'slab-party': 0,
    'mansion-ground': 1,
    'mansion-typical': 2,
    'mansion-attic': 3,
    'mansion-end': 4,
    'slab-ground': 5,
    'slab-typical': 6,
    'slab-end': 7,
}

for texture in ('bricks-088', 'painted-plaster-wall', 'preconcrete-wall-001', 'concrete-wall-004'):
    spec = tall.LIBRARY['textures'][texture]
    street.SURFACES[texture] = {'metres': spec['tile_metres'],
                                'mean': tall.LIBRARY['paint_mean_linear_luminance'] if spec['paint'] else None,
                                'maps': {key: ROOT / f['file'] for key, f in spec['files'].items()}}

# Part name: (library texture or None, colour) for each family.
PARTS = {
    'mansion': {
        'wall': ('bricks-088', '9a5b45'), 'wall-end': ('painted-plaster-wall', 'd8cfbf'), 'trim': (None, 'e9e4d8'),
        'glass': (None, '35414a'), 'frame': (None, 'ece9e2'), 'door': (None, '3b3f3c'), 'metal': (None, '2f3437'),
        'membrane': ('bitumen', None), 'plinth': (None, '7d7a73'), 'paving': ('concrete-panels', 'b9b6ad'),
        'reveal': (None, 'c9c0b0'),
    },
    'slab': {
        'wall': ('preconcrete-wall-001', 'cfcbc0'), 'wall-end': ('concrete-wall-004', 'b8b4aa'),
        'spandrel': (None, 'a9a49a'), 'trim': (None, 'deddd6'), 'glass': (None, '33404a'), 'frame': (None, '8c9295'),
        'door': (None, '3a3d3f'), 'metal': (None, '5d6366'), 'membrane': ('bitumen', None), 'plinth': (None, '6f6e69'),
        'reveal': (None, '4a4f52'),
    },
}

# Lengths in metres. bay: the window module along a face. window, door: (width, height, sill).
LOOKS = {
    'mansion': {
        'bay': 3.3, 'window': (1.3, 1.9, .9), 'ground_window': (1.7, 2.1, .7), 'door': (1.6, 2.7, .3),
        'entry_every': 16.0, 'loggia_every': 3, 'loggia_depth': 1.4, 'balcony_every': 2, 'balcony_depth': 1.4,
        'attic_setback': 1.8, 'attic_window': (2.4, 2.3, .5), 'cornice': (.45, .35), 'string_course': (.25, .12), 'parapet': .9,
    },
    'slab': {
        'bay': 3.6, 'window': (2.1, 1.5, .9), 'door': (1.2, 2.3, 0), 'gallery_door': (1.0, 2.2, 0),
        'gallery_window': (1.2, 1.0, 1.2), 'entry_every': 30.0, 'loggia_every': 4, 'loggia_depth': 1.2,
        'gallery_depth': 1.8, 'balustrade': 1.1, 'ground_recess': 1.5, 'piloti_every': 7.2, 'piloti': .5,
        'joint': (.06, .02), 'plant_every': 30.0, 'plant': (6.0, 4.0, 3.0), 'parapet': .6,
    },
}

# The settings a Style Preset family may change, with each family's default. street_openings is
# 'loggias', 'balconies' or 'windows'; shops glazes the street ground storey; attic is the mansion's
# set-back top storey; galleries are the slab's yard access decks.
SETTINGS = {
    'mansion': {'bay': 3.3, 'street_openings': 'loggias', 'shops': False, 'attic': True},
    'slab': {'bay': 3.6, 'street_openings': 'loggias', 'shops': False, 'galleries': True},
}


def wall_with_openings(g, side, part, u0, u1, z0, z1, openings, d0=0.0, d1=WALL):
    """A wall slab from u0 to u1 and z0 to z1 with rectangular openings cut through it. Each
    opening gets a glass plane at the back of the slab."""
    us = sorted({u0, u1} | {u for o in openings for u in o[:2]})
    zs = sorted({z0, z1} | {z for o in openings for z in o[2:4]})
    for za, zb in zip(zs, zs[1:]):
        start = None
        for ua, ub in zip(us, us[1:]):
            hole = any(o[0] <= ua + 1e-4 and ub - 1e-4 <= o[1] and o[2] <= za + 1e-4 and zb - 1e-4 <= o[3]
                       for o in openings)
            if not hole and start is None:
                start = ua
            if hole and start is not None:
                side.slab(g, part, start, ua, za, zb, d0, d1)
                start = None
        if start is not None:
            side.slab(g, part, start, us[-1], za, zb, d0, d1)
    for o in openings:
        kind = o[4] if len(o) > 4 else 'glass'
        if kind:
            side.plane(g, kind, o[0], o[1], o[2], o[3], d1)


def bays(length, bay):
    count = max(1, round(length / bay))
    width = length / count
    return [(i * width, (i + 1) * width) for i in range(count)]


def far_wall(g, side, a, b, storey, family, role, look, cell=None):
    """One plain wall quad with the same bay split as the corresponding near stretch."""
    count = len(bays(b - a, look['bay']))
    if role == 'party':
        cell = FAR_CELLS[f'{family}-party']
    elif cell is None and role == 'end' and storey != 0:
        cell = FAR_CELLS[f'{family}-end']
    elif cell is None:
        band = 'ground' if storey == 0 else 'typical'
        cell = FAR_CELLS[f'{family}-{band}']
    side.grid_plane(g, a, b, storey * STOREY, (storey + 1) * STOREY, 0, 0, count, cell)


def centred(a, b, width, z, height):
    middle = (a + b) / 2
    return (middle - width / 2, middle + width / 2, z, z + height)


def entries(length, every):
    """Bay-centre positions of entrances, spread evenly and never at a corner."""
    count = max(1, round(length / every))
    return [length * (i + .5) / count for i in range(count)]


def railing(g, side, u0, u1, z, depth):
    side.slab(g, 'metal', u0, u1, z + 1.0, z + 1.08, depth - .04, depth + .04)
    for u in [u0 + .05] + [u0 + (u1 - u0) * i / 8 for i in range(1, 8)] + [u1 - .05]:
        side.slab(g, 'metal', u - .02, u + .02, z, z + 1.0, depth - .02, depth + .02)


# ---------------------------------------------------------------------------------------------
# Mansion block

def mansion_face(g, side, a, b, storeys, role, look):
    """One stretch of a mansion face, a to b along it, below the attic storey."""
    if role == 'party':
        side.slab(g, 'wall', a, b, 0, storeys * STOREY, 0, WALL)
        return
    full = storeys - 1 if look['attic'] else storeys
    top = full * STOREY
    stretch = bays(b - a, look['bay'])
    doors = [a + e for e in entries(b - a, look['entry_every'])] if role == 'street' else []
    shops = look['shops'] and role == 'street'

    side.slab(g, 'plinth', a, b, 0, .4, -.05, WALL)
    ground = []
    for i, (u0, u1) in enumerate(stretch):
        u0, u1 = a + u0, a + u1
        if any(u0 <= d < u1 for d in doors):
            w, h, sill = look['door']
            ground.append(centred(u0, u1, w, sill, h) + ('door',))
        elif shops:
            ground.append((u0 + .3, u1 - .3, .4, 2.9))
        elif role != 'end' or i % 2 == 0:
            w, h, sill = look['ground_window']
            ground.append(centred(u0, u1, w, sill, h))
    wall_with_openings(g, side, 'wall-end', a, b, .4, STOREY, [o for o in ground if o[2] >= .4]
                       + [(o[0], o[1], .4, o[3]) + o[4:] for o in ground if o[2] < .4])
    for u in doors:
        side.slab(g, 'trim', u - 1.3, u + 1.3, 3.05, 3.2, -.9, 0)
    if shops:
        side.slab(g, 'trim', a, b, 2.9, 3.25, -.15, WALL)
    h, out = look['string_course']
    side.slab(g, 'trim', a, b, STOREY - h, STOREY, -out, WALL)

    for storey in range(1, full):
        z = storey * STOREY
        openings = []
        for i, (u0, u1) in enumerate(stretch):
            u0, u1 = a + u0, a + u1
            w, hgt, sill = look['window']
            street_loggia = role == 'street' and look['street_openings'] == 'loggias'
            if street_loggia and i % look['loggia_every'] == 1 and 0 < i < len(stretch) - 1:
                openings.append((u0 + .35, u1 - .35, z, z + 2.9, None))
            elif role != 'end' or i % 2 == 0:
                openings.append(centred(u0, u1, w, z + sill, hgt))
            street_balcony = role == 'street' and look['street_openings'] == 'balconies'
            if (role == 'yard' or street_balcony) and i % look['balcony_every'] == 1:
                d = look['balcony_depth']
                side.slab(g, 'trim', u0 + .2, u1 - .2, z - .15, z, -d, 0)
                railing(g, side, u0 + .2, u1 - .2, z, -d + .05)
        loggias = [o for o in openings if o[4:] == (None,)]
        wall_with_openings(g, side, 'wall', a, b, z, z + STOREY, openings)
        for u0, u1, za, zb, _ in loggias:
            d = look['loggia_depth']
            wall_with_openings(g, side, 'reveal', u0, u1, za, zb, [
                centred(u0, u0 + (u1 - u0) * .45, 1.0, za, 2.3) + ('glass',),
                centred(u0 + (u1 - u0) * .55, u1, 1.2, za + .9, 1.5)], d, d + .1)
            side.slab(g, 'wall', u0 - .1, u0, za, zb, WALL, d)
            side.slab(g, 'wall', u1, u1 + .1, za, zb, WALL, d)
            side.slab(g, 'trim', u0, u1, za, za + .12, -.05, d)
            railing(g, side, u0, u1, za + .12, .05)

    ch, cout = look['cornice'] if role == 'street' else (.2, .08)
    side.slab(g, 'trim', a, b, top - .1, top + ch - .1, -cout, WALL)
    if not look['attic']:
        side.slab(g, 'wall', a, b, top + ch - .1, top + look['parapet'], 0, WALL)
        side.slab(g, 'trim', a, b, top + look['parapet'], top + look['parapet'] + .1, -.05, WALL + .05)


def mansion_face_far(g, side, a, b, storeys, role, look):
    """A mansion stretch with openings, projections and string courses removed."""
    if role == 'party':
        for storey in range(storeys):
            far_wall(g, side, a, b, storey, 'mansion', role, look)
        return
    full = storeys - 1 if look['attic'] else storeys
    side.slab(g, 'plinth', a, b, 0, .4, -.05, WALL)
    for storey in range(full):
        far_wall(g, side, a, b, storey, 'mansion', role, look)
    top = full * STOREY
    ch, cout = look['cornice'] if role == 'street' else (.2, .08)
    side.slab(g, 'trim', a, b, top - .1, top + ch - .1, -cout, WALL)
    if not look['attic']:
        side.slab(g, 'far-facade', a, b, top + ch - .1, top + look['parapet'], 0, WALL)
        side.slab(g, 'trim', a, b, top + look['parapet'], top + look['parapet'] + .1, -.05, WALL + .05)


def mansion_attic(g, side, a, b, z, look):
    """The set-back attic storey: render, a window in every bay, a thin eave."""
    openings = [centred(a + u0, a + u1, look['attic_window'][0], z + look['attic_window'][2], look['attic_window'][1]) for u0, u1 in bays(b - a, look['bay'])]
    wall_with_openings(g, side, 'wall-end', a, b, z, z + STOREY, openings)
    side.slab(g, 'trim', a, b, z + STOREY, z + STOREY + .25, -.5, WALL)


# ---------------------------------------------------------------------------------------------
# Panel slab

def slab_face(g, side, a, b, storeys, role, look):
    if role == 'party':
        side.slab(g, 'wall', a, b, 0, storeys * STOREY, 0, WALL)
        return
    top = storeys * STOREY
    stretch = bays(b - a, look['bay'])
    recess = look['ground_recess'] if role in ('street', 'yard') else 0.0
    doors = [a + e for e in entries(b - a, look['entry_every'])] if role in ('street', 'yard') else []

    side.slab(g, 'plinth', a, b, 0, .3, -.05, recess + WALL)
    ground = []
    for u0, u1 in stretch:
        u0, u1 = a + u0, a + u1
        if any(u0 <= d < u1 for d in doors):
            ground.append(centred(u0, u1, 2.4, .3, 2.5) + ('door',))
        elif look['shops'] and role == 'street':
            ground.append((u0 + .2, u1 - .2, .3, 2.8))
        elif role != 'end':
            ground.append(centred(u0, u1, 2.8, .6, 2.2))
    wall_with_openings(g, side, 'wall-end', a, b, .3, STOREY, ground, recess, recess + WALL)
    if recess:
        side.slab(g, 'spandrel', a, b, STOREY - .4, STOREY, -.02, recess)
        n = max(1, round((b - a) / look['piloti_every']))
        for i in range(n + 1):
            u = min(max(a + (b - a) * i / n, a + look['piloti'] / 2), b - look['piloti'] / 2)
            side.slab(g, 'wall', u - look['piloti'] / 2, u + look['piloti'] / 2, .3, STOREY - .4, 0, look['piloti'])
        for d in doors:
            side.slab(g, 'trim', d - 2.0, d + 2.0, 2.9, 3.05, -1.2, recess)

    galleries = role == 'yard' and look['galleries']
    for storey in range(1, storeys):
        z = storey * STOREY
        openings, loggias = [], []
        for i, (u0, u1) in enumerate(stretch):
            u0, u1 = a + u0, a + u1
            w, h, sill = look['window']
            if role == 'street' and look['street_openings'] == 'loggias' and i % look['loggia_every'] == 2:
                loggias.append((u0, u1))
                openings.append((u0, u1, z, z + STOREY, None))
            elif galleries:
                dw, dh, _ = look['gallery_door']
                ww, wh, ws = look['gallery_window']
                third = (u1 - u0) / 3
                openings.append(centred(u0, u0 + third * 1.4, dw, z, dh) + ('door',))
                openings.append(centred(u0 + third * 1.4, u1, ww, z + ws, wh))
            elif role != 'end' or i % 4 == 1:
                openings.append(centred(u0, u1, w if role != 'end' else .9, z + sill, h))
            if role == 'street' and look['street_openings'] == 'balconies' and i % 2 == 1:
                d = look['loggia_depth']
                side.slab(g, 'trim', u0 + .15, u1 - .15, z - .2, z, -d, 0)
                side.slab(g, 'spandrel', u0 + .15, u1 - .15, z, z + look['balustrade'], -d, -d + .12)
        wall_with_openings(g, side, 'wall', a, b, z, z + STOREY, openings)
        for u0, u1 in loggias:
            d = look['loggia_depth']
            wall_with_openings(g, side, 'reveal', u0, u1, z, z + STOREY,
                               [centred(u0, u1, 2.6, z, 2.5) + ('glass',)], d, d + .1)
            side.slab(g, 'trim', u0, u1, z + STOREY - .2, z + STOREY, WALL, d)
            side.slab(g, 'spandrel', u0, u1, z, z + look['balustrade'], 0, .15)
        if galleries:
            d = look['gallery_depth']
            side.slab(g, 'trim', a, b, z - .2, z, -d, 0)
            side.slab(g, 'spandrel', a, b, z, z + look['balustrade'], -d, -d + .12)
        jw, jout = look['joint']
        side.slab(g, 'trim', a, b, z - jw / 2, z + jw / 2, -jout, 0)
        if role != 'end':
            for u0, _ in stretch[1:]:
                side.slab(g, 'trim', a + u0 - jw / 2, a + u0 + jw / 2, z, z + STOREY, -jout, 0)
    side.slab(g, 'wall', a, b, top, top + look['parapet'], 0, WALL)
    side.slab(g, 'trim', a, b, top + look['parapet'], top + look['parapet'] + .1, -.05, WALL + .05)


def slab_face_far(g, side, a, b, storeys, role, look):
    """A panel slab stretch with its openings, joints, pilotis and access decks removed."""
    if role == 'party':
        for storey in range(storeys):
            far_wall(g, side, a, b, storey, 'slab', role, look)
        return
    side.slab(g, 'plinth', a, b, 0, .3, -.05, WALL)
    for storey in range(storeys):
        far_wall(g, side, a, b, storey, 'slab', role, look)
    top = storeys * STOREY
    side.slab(g, 'far-facade', a, b, top, top + look['parapet'], 0, WALL)
    side.slab(g, 'trim', a, b, top + look['parapet'], top + look['parapet'] + .1, -.05, WALL + .05)


def plant_rooms(g, rect, z, look):
    x0, y0, x1, y1 = rect
    w, d, h = look['plant']
    long_x = x1 - x0 >= y1 - y0
    length = (x1 - x0) if long_x else (y1 - y0)
    for c in entries(length, look['plant_every']):
        if long_x:
            cx, cy = x0 + c, (y0 + y1) / 2
            low, high = (cx - w / 2, cy - d / 2, z), (cx + w / 2, cy + d / 2, z + h)
        else:
            cx, cy = (x0 + x1) / 2, y0 + c
            low, high = (cx - d / 2, cy - w / 2, z), (cx + d / 2, cy + w / 2, z + h)
        g.box('wall-end', low, high)
        g.box('trim', (low[0] - .1, low[1] - .1, high[2]), (high[0] + .1, high[1] + .1, high[2] + .15))


# ---------------------------------------------------------------------------------------------
# Sites

def wings(frontage, depth):
    """The simulation's plan: one rectangle, or a ring of four 16 m wings (Main.Massing Wings)."""
    x0, y0, x1, y1 = -frontage / 2, -depth / 2, frontage / 2, depth / 2
    if frontage < RING_MINIMUM or depth < RING_MINIMUM:
        return [(x0, y0, x1, y1)]
    return [(x0, y0, x1, y0 + WING), (x0, y1 - WING, x1, y1),
            (x0, y0 + WING, x0 + WING, y1 - WING), (x1 - WING, y0 + WING, x1, y1 - WING)]


def role_of(side_name, rect, site, ring, attached):
    """A face's role from where it lies on the site."""
    x0, y0, x1, y1 = site
    on_edge = {'s': abs(rect[1] - y0) < 1e-3, 'n': abs(rect[3] - y1) < 1e-3,
               'w': abs(rect[0] - x0) < 1e-3, 'e': abs(rect[2] - x1) < 1e-3}[side_name]
    if side_name in attached and on_edge:
        return 'party'
    if ring:
        return 'street' if on_edge else 'yard'
    return {'s': 'street', 'n': 'yard'}.get(side_name, 'end')


def site(g, family, frontage, depth, storeys, attached=(), settings=None, far=False):
    chosen = {**SETTINGS[family], **(settings or {})}
    look = {**LOOKS[family], **chosen}
    rects = wings(frontage, depth)
    ring = len(rects) > 1
    bounds = (-frontage / 2, -depth / 2, frontage / 2, depth / 2)
    for rect in rects:
        for name in SIDES:
            side = Side(rect, name)
            cuts = sorted({0.0, side.length} | {e for o in rects if o is not rect
                                                 and (t := touching(side, rect, o)) for e in t})
            role = role_of(name, rect, bounds, ring, attached)
            for a, b in zip(cuts, cuts[1:]):
                if any(o is not rect and (t := touching(side, rect, o)) and t[0] <= a + 1e-4 and b - 1e-4 <= t[1]
                       for o in rects):
                    continue
                if family == 'mansion':
                    (mansion_face_far if far else mansion_face)(g, side, a, b, storeys, role, look)
                else:
                    (slab_face_far if far else slab_face)(g, side, a, b, storeys, role, look)

    if family == 'mansion' and look['attic']:
        (mansion_roofs_far if far else mansion_roofs)(g, rects, bounds, ring, attached, storeys, look)
    else:
        top = storeys * STOREY
        for rect in rects:
            x0, y0, x1, y1 = rect
            g.box('membrane', (x0 + .1, y0 + .1, top - .2), (x1 - .1, y1 - .1, top + .05))
            if family == 'slab' and (not ring or rect[2] - rect[0] > rect[3] - rect[1]):
                plant_rooms(g, (x0 + 4, y0 + 4, x1 - 4, y1 - 4), top, look)
    floor = sum((r[2] - r[0]) * (r[3] - r[1]) for r in rects) * storeys
    return {'family': family, 'frontage': frontage, 'depth': depth, 'storeys': storeys, 'ring': ring,
            'attached': list(attached), 'settings': chosen, 'floor_square_metres': round(floor)}


def mansion_roofs(g, rects, bounds, ring, attached, storeys, look):
    """The attic over each wing, set back from its street faces, with a paved terrace in front."""
    full_top = (storeys - 1) * STOREY
    s = look['attic_setback']
    attics = []
    for rect in rects:
        x0, y0, x1, y1 = rect
        street_sides = [n for n in SIDES if role_of(n, rect, bounds, ring, attached) == 'street']
        attics.append((x0 + (s if 'w' in street_sides else 0), y0 + (s if 's' in street_sides else 0),
                       x1 - (s if 'e' in street_sides else 0), y1 - (s if 'n' in street_sides else 0)))
        g.box('paving', (x0 + .1, y0 + .1, full_top - .2), (x1 - .1, y1 - .1, full_top + .05))
    for attic, rect in zip(attics, rects):
        for name in SIDES:
            side = Side(attic, name)
            cuts = sorted({0.0, side.length} | {e for o in attics if o is not attic
                                                 and (t := touching(side, attic, o)) for e in t})
            role = role_of(name, rect, bounds, ring, attached)
            for a, b in zip(cuts, cuts[1:]):
                if any(o is not attic and (t := touching(side, attic, o)) and t[0] <= a + 1e-4 and b - 1e-4 <= t[1]
                       for o in attics):
                    continue
                if role == 'party':
                    side.slab(g, 'wall', a, b, full_top, full_top + STOREY, 0, WALL)
                else:
                    mansion_attic(g, side, a, b, full_top, look)
        for name in SIDES:
            if role_of(name, rect, bounds, ring, attached) == 'street':
                side = Side(rect, name)
                railing(g, side, .2, side.length - .2, full_top + .05, .1)
        ax0, ay0, ax1, ay1 = attic
        g.box('membrane', (ax0 + .1, ay0 + .1, full_top + STOREY - .2), (ax1 - .1, ay1 - .1, full_top + STOREY + .05))


def mansion_roofs_far(g, rects, bounds, ring, attached, storeys, look):
    """The near attic footprints and roofs with windows and terrace railings removed."""
    full_top = (storeys - 1) * STOREY
    s = look['attic_setback']
    attics = []
    for rect in rects:
        x0, y0, x1, y1 = rect
        street_sides = [name for name in SIDES if role_of(name, rect, bounds, ring, attached) == 'street']
        attics.append((x0 + (s if 'w' in street_sides else 0), y0 + (s if 's' in street_sides else 0),
                       x1 - (s if 'e' in street_sides else 0), y1 - (s if 'n' in street_sides else 0)))
        g.box('paving', (x0 + .1, y0 + .1, full_top - .2), (x1 - .1, y1 - .1, full_top + .05))
    for attic, rect in zip(attics, rects):
        for name in SIDES:
            side = Side(attic, name)
            cuts = sorted({0.0, side.length} | {edge for other in attics if other is not attic
                                                 and (touch := touching(side, attic, other)) for edge in touch})
            role = role_of(name, rect, bounds, ring, attached)
            for a, b in zip(cuts, cuts[1:]):
                if any(other is not attic and (touch := touching(side, attic, other))
                       and touch[0] <= a + 1e-4 and b - 1e-4 <= touch[1] for other in attics):
                    continue
                far_wall(g, side, a, b, storeys - 1, 'mansion', role, look,
                         FAR_CELLS['mansion-attic'])
                if role != 'party':
                    side.slab(g, 'trim', a, b, full_top + STOREY, full_top + STOREY + .25, -.5, WALL)
        ax0, ay0, ax1, ay1 = attic
        g.box('membrane', (ax0 + .1, ay0 + .1, full_top + STOREY - .2),
              (ax1 - .1, ay1 - .1, full_top + STOREY + .05))


# Sizes and storeys the simulation raises on platted.toml at 40,000 Citizens.
LINEUP = {
    'mansion': [
        ('perimeter-terrace-a', 40, 16, 6, ('e',)),
        ('perimeter-terrace-b', 32, 20, 7, ('w', 'e')),
        ('perimeter-terrace-c', 48, 16, 5, ('w',)),
        ('back-to-back-deep', 36, 44, 10, ()),
        ('perimeter-ring', 56, 56, 7, ()),
        ('back-to-back-ring', 60, 52, 10, ()),
        ('balconies-no-attic', 40, 16, 6, (), {'street_openings': 'balconies', 'attic': False, 'bay': 3.0}),
        ('shops-wide-bay', 48, 20, 5, (), {'shops': True, 'street_openings': 'windows', 'bay': 3.9}),
        ('shops-ring', 52, 48, 8, (), {'shops': True, 'attic': False}),
    ],
    'slab': [
        ('courtyard-face', 116, 30, 14, ()),
        ('courtyard-flank', 40, 30, 13, ()),
        ('slab-ring', 116, 54, 16, ()),
        ('windows-no-galleries', 60, 30, 13, (), {'street_openings': 'windows', 'galleries': False, 'bay': 3.0}),
        ('balconies-shops', 60, 30, 14, (), {'street_openings': 'balconies', 'shops': True, 'bay': 4.2}),
    ],
}


def materials():
    made = {}
    for family, parts in PARTS.items():
        for part, (texture, colour) in parts.items():
            name = f'{part}-{family}'
            if texture:
                made[name] = street.surface(f'{name}-{texture}', texture, colour)
                continue
            material = bpy.data.materials.new(name)
            material.use_nodes = True
            rgba = street.rgb(colour) + (1,)
            shader = material.node_tree.nodes['Principled BSDF']
            shader.inputs['Base Color'].default_value = rgba
            shader.inputs['Roughness'].default_value = .15 if part == 'glass' else .8
            shader.inputs['Metallic'].default_value = .6 if part in ('glass', 'metal') else 0
            material.diffuse_color = rgba
            made[name] = material
    far = bpy.data.materials.new('far-facade')
    far.use_nodes = True
    far.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (.63, .58, .52, 1)
    far.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = .8
    made['far-facade'] = far
    return made


def census(g):
    """Faces per part and the site's bounds, which the shell's builder is tested against."""
    points = [v for vertices, _ in g.parts.values() for v in vertices]
    return {'faces': {part: len(faces) for part, (_, faces) in sorted(g.parts.items())},
            'bounds': [[round(min(p[i] for p in points), 3) for i in range(3)],
                       [round(max(p[i] for p in points), 3) for i in range(3)]]}


def family_geometry(family, g):
    """Renames each near part to its family's material and keeps the shared far facade part."""
    out = Geometry()
    names = {part: part if part == 'far-facade' else f'{part}-{family}' for part in g.parts}
    out.parts = {names[part]: data for part, data in g.parts.items()}
    out.grid_uvs = {names[part]: data for part, data in g.grid_uvs.items()}
    return out


def lineup_offsets():
    """West to east in two rows: mansion blocks in front, slabs behind. The terrace stands as one
    row of three attached bodies."""
    offsets = {}
    x = 0.0
    for name, frontage, depth, *_ in LINEUP['mansion']:
        offsets[name] = (x + frontage / 2, 0)
        gap = 0 if name.startswith('perimeter-terrace') and name != 'perimeter-terrace-c' else 40
        x += frontage + gap
    x = 0.0
    for name, frontage, depth, *_ in LINEUP['slab']:
        offsets[name] = (x + frontage / 2, 140)
        x += frontage + 50
    return offsets


def main():
    SOURCES.mkdir(parents=True, exist_ok=True)
    EXPORT.mkdir(parents=True, exist_ok=True)
    bpy.context.preferences.filepaths.save_version = 0
    reports = []
    for family, sites in LINEUP.items():
        for thing in list(bpy.data.objects):
            bpy.data.objects.remove(thing)
        for data in list(bpy.data.meshes):
            bpy.data.meshes.remove(data)
        for material in list(bpy.data.materials):
            bpy.data.materials.remove(material)
        made = materials()
        offsets = lineup_offsets()
        for name, frontage, depth, storeys, attached, *settings in sites:
            setting = settings[0] if settings else None
            near = Geometry()
            report = site(near, family, frontage, depth, storeys, attached, setting)
            near_census = census(near)
            far = Geometry()
            far_report = site(far, family, frontage, depth, storeys, attached, setting, far=True)
            assert far_report == report, (report, far_report)
            far_census = census(far)
            reports.append({'name': name, **report, **near_census,
                            'far_faces': far_census['faces'], 'far_bounds': far_census['bounds'],
                            'far_cell_counts': cell_histogram(far)})
            tall.build(f'{name}-near', family_geometry(family, near), made, offsets[name])
            tall.build(f'{name}-far', family_geometry(family, far), made,
                       (offsets[name][0], offsets[name][1] + 260))
        bpy.ops.wm.save_as_mainfile(filepath=str(SOURCES / f'{family}.blend'))

    for thing in list(bpy.data.objects):
        bpy.data.objects.remove(thing)
    for data in list(bpy.data.meshes):
        bpy.data.meshes.remove(data)
    for material in list(bpy.data.materials):
        bpy.data.materials.remove(material)
    made = materials()
    offsets = lineup_offsets()
    for family, sites in LINEUP.items():
        for name, frontage, depth, storeys, attached, *settings in sites:
            setting = settings[0] if settings else None
            near = Geometry()
            far = Geometry()
            site(near, family, frontage, depth, storeys, attached, setting)
            site(far, family, frontage, depth, storeys, attached, setting, far=True)
            tall.build(f'{name}-near', family_geometry(family, near), made, offsets[name])
            tall.build(f'{name}-far', family_geometry(family, far), made,
                       (offsets[name][0], offsets[name][1] + 260))
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCES / 'lineup.blend'))

    for family, parts in PARTS.items():
        swatches = Geometry()
        i = 0
        for part, (texture, _) in parts.items():
            if not texture:
                swatches.box(f'{part}-{family}', (i * 2, 0, 0), (i * 2 + 1, 1, 1))
                i += 1
        library = tall.build(family, swatches, made)
        for other in bpy.data.objects:
            other.select_set(False)
        library.select_set(True)
        bpy.context.view_layer.objects.active = library
        bpy.ops.export_scene.gltf(filepath=str(EXPORT / f'{family}.glb'), export_format='GLB', use_selection=True,
                                  export_apply=True, export_yup=True)

    (SOURCES / 'bodies.json').write_text(json.dumps({'storey_metres': STOREY, 'wing_metres': WING,
                                                     'ring_minimum_metres': RING_MINIMUM, 'looks': LOOKS,
                                                     'far_cells': FAR_CELLS, 'sites': reports}, indent=2) + '\n')
    for report in reports:
        print('MIDRISE', report)


if __name__ == '__main__':
    main()

"""The tall-Building tower families: four bodies for the simulation's Tower pattern, with facades.

Run with Blender in the background:
  blender --background --factory-startup --python-exit-code 1 --python scripts/art/tall-families.py

A tower site is the simulation's Tower pattern on a 116 m block. The simulation counts a podium
over the whole site and a centred shaft half the site's width each way (BuildingPlan.Tower). Each
body replaces that one shaft with wings that hold the same floor, to within one percent:

  point          four 29 m towers of equal height, banded residential facade
  stepped-point  four 29 m towers, each a spine and a lower front block with a planted terrace;
                 the step turns a quarter per tower, and a recessed reveal storey marks it
  l              a 96 m street arm and a taller 76 m back arm, 20 m deep, precast ribbon facade
  h              two 80 m bars and a 40 m link, 20 m deep, curtain wall

A wing is a rectangle, a base height, a storey count and a facade. A facade is recessed glass
behind a spandrel band on every storey, crossed by vertical fins. Its parameters are what the shell's
body builder reproduces at any podium height.

The variants are saved as .blend references. The only export is library.glb, which carries the
untextured part materials; textured parts name their library texture in the Style Preset.

Each body's origin is its site centre at ground level. The street face looks down -Y, which Godot
receives as +Z. Faces are wound counter-clockwise seen from outside.

Far mode uses one `far-facade` part for every wall. Its UVMap counts facade bays in u and storeys
from ground level in v; uv2.x carries one cell id per face. Roof membranes, paving and outline trim
keep their near part names. The far cell table is:

  0  blank                     1  point-podium-ground       2  point-podium-typical
  3  point-shaft               4  stepped-point-podium-ground
  5  stepped-point-podium-typical                         6  stepped-point-shaft
  7  l-podium-ground           8  l-podium-typical         9  l-shaft
 10  h-podium-ground          11  h-podium-typical        12  h-shaft
 13  crown
"""
import bmesh
import bpy
import importlib.util
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
_spec = importlib.util.spec_from_file_location('test_street', ROOT / 'scripts/art/test-street.py')
street = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(street)

SOURCES = ROOT / 'art/tall-families'
EXPORT = ROOT / 'src/Borough.Godot/assets/tall-families'
STOREY = 3.5
SITE = 116
TOTAL_STOREYS = 62
PODIUM_STOREYS = (2, 5, 8)
EXPORTED_PODIUM = 5
RECESS = .3

LIBRARY = json.loads((ROOT / 'src/Borough.Godot/assets/city/library/materials.json').read_text())
for texture in ('preconcrete-wall-001', 'brick-wall-001', 'bitumen', 'concrete-panels'):
    spec = LIBRARY['textures'][texture]
    street.SURFACES[texture] = {'metres': spec['tile_metres'],
                                'mean': LIBRARY['paint_mean_linear_luminance'] if spec['paint'] else None,
                                'maps': {key: ROOT / f['file'] for key, f in spec['files'].items()}}

# Part name: (library texture or None, colour). Textured parts carry their tile size for the UVs.
PARTS = {
    'wall': ('preconcrete-wall-001', 'd3cfc4'), 'wall-end': ('brick-wall-001', None), 'trim': (None, 'eceae3'),
    'glass': (None, '33414a'), 'spandrel': (None, '3d4349'), 'frame': (None, '9ba1a4'),
    'metal': (None, '7d8387'), 'membrane': ('bitumen', None), 'plinth': (None, '8f8e88'),
    'door': (None, '3a3d3f'), 'reveal': (None, '262b2f'), 'paving': ('concrete-panels', 'b9b6ad'),
    'planter': (None, '8e8b83'), 'tree': (None, '6f7f63'),
}

# spandrel: band height from each floor; out: how far the band stands proud of the face line.
# fin_every: spacing of vertical fins along the face; fin: (width, front depth, back depth), where a
# negative depth stands outside the face line and the glass sits at RECESS inside it.
FACADES = {
    'banded': {'spandrel': .9, 'spandrel_part': 'trim', 'out': .35, 'fin_every': 7.25,
               'fin': (.25, -.35, RECESS), 'fin_part': 'trim'},
    'curtain': {'spandrel': 1.2, 'spandrel_part': 'spandrel', 'out': .05, 'fin_every': 1.5,
                'fin': (.08, -.25, RECESS), 'fin_part': 'frame'},
    'ribbon': {'spandrel': 1.4, 'spandrel_part': 'wall', 'out': 0, 'fin_every': 1.75,
               'fin': (.08, RECESS - .1, RECESS), 'fin_part': 'frame'},
    'spine': {'spandrel': .6, 'spandrel_part': 'wall', 'out': 0, 'fin_every': 2.9,
              'fin': (.5, -.6, RECESS), 'fin_part': 'wall'},
    'podium': {'spandrel': 1.3, 'spandrel_part': 'wall-end', 'out': 0, 'fin_every': 6.0,
               'fin': (.6, 0, RECESS), 'fin_part': 'wall-end'},
}
SIDES = ('s', 'e', 'n', 'w')

FAR_CELLS = {
    'blank': 0,
    'point-podium-ground': 1,
    'point-podium-typical': 2,
    'point-shaft': 3,
    'stepped-point-podium-ground': 4,
    'stepped-point-podium-typical': 5,
    'stepped-point-shaft': 6,
    'l-podium-ground': 7,
    'l-podium-typical': 8,
    'l-shaft': 9,
    'h-podium-ground': 10,
    'h-podium-typical': 11,
    'h-shaft': 12,
    'crown': 13,
}


class Geometry:
    """Vertices and faces gathered per part, so a whole site becomes one object."""

    def __init__(self):
        self.parts = {}
        self.grid_uvs = {}

    def polygon(self, part, points, grid_uv=None, cell=0):
        vertices, faces = self.parts.setdefault(part, ([], []))
        base = len(vertices)
        vertices.extend(points)
        faces.append(tuple(range(base, base + len(points))))
        self.grid_uvs.setdefault(part, []).append((grid_uv, cell) if grid_uv else None)

    def box(self, part, low, high):
        (x0, y0, z0), (x1, y1, z1) = low, high
        if x1 - x0 < 1e-4 or y1 - y0 < 1e-4 or z1 - z0 < 1e-4:
            return
        corners = [(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0),
                   (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)]
        vertices, faces = self.parts.setdefault(part, ([], []))
        base = len(vertices)
        vertices.extend(corners)
        faces.extend(tuple(base + i for i in face) for face in street.BOX_FACES)
        self.grid_uvs.setdefault(part, []).extend([None] * len(street.BOX_FACES))

    def icosphere(self, part, centre, radius):
        bm = bmesh.new()
        bmesh.ops.create_icosphere(bm, subdivisions=1, radius=radius)
        for face in bm.faces:
            self.polygon(part, [tuple(centre[i] + v.co[i] for i in range(3)) for v in face.verts])
        bm.free()


class Side:
    """One face of a rectangle. u runs along it, to the right of someone outside facing it; depth
    runs inward from the face line."""

    def __init__(self, rect, side):
        x0, y0, x1, y1 = rect
        self.side = side
        self.origin, self.along, self.outward, self.length = {
            's': ((x0, y0), (1, 0), (0, -1), x1 - x0), 'e': ((x1, y0), (0, 1), (1, 0), y1 - y0),
            'n': ((x1, y1), (-1, 0), (0, 1), x1 - x0), 'w': ((x0, y1), (0, -1), (-1, 0), y1 - y0)}[side]

    def point(self, u, z, depth=0.0):
        return (self.origin[0] + self.along[0] * u - self.outward[0] * depth,
                self.origin[1] + self.along[1] * u - self.outward[1] * depth, z)

    def slab(self, geometry, part, u0, u1, z0, z1, d0, d1):
        a, b = self.point(u0, z0, d0), self.point(u1, z1, d1)
        geometry.box(part, tuple(map(min, a, b)), tuple(map(max, a, b)))

    def plane(self, geometry, part, u0, u1, z0, z1, depth):
        geometry.polygon(part, [self.point(u0, z0, depth), self.point(u1, z0, depth),
                                self.point(u1, z1, depth), self.point(u0, z1, depth)])

    def grid_plane(self, geometry, u0, u1, z0, z1, depth, grid_u0, grid_u1, cell):
        geometry.polygon('far-facade', [self.point(u0, z0, depth), self.point(u1, z0, depth),
                                        self.point(u1, z1, depth), self.point(u0, z1, depth)],
                         [(grid_u0, z0 / STOREY), (grid_u1, z0 / STOREY),
                          (grid_u1, z1 / STOREY), (grid_u0, z1 / STOREY)], cell)


def facade(geometry, side, u0, u1, z0, z1, floor0, style):
    """A facade piece from u0 to u1 and z0 to z1. Storeys count up from floor0."""
    look = FACADES[style]
    side.plane(geometry, 'glass', u0, u1, z0, z1, RECESS)
    first = round((z0 - floor0) / STOREY)
    last = round((z1 - floor0) / STOREY)
    for storey in range(first, last):
        z = floor0 + storey * STOREY
        side.slab(geometry, look['spandrel_part'], u0, u1, z, z + look['spandrel'], -look['out'], RECESS + .05)
    width, front, back = look['fin']
    step = look['fin_every']
    count = round(side.length / step)
    for i in range(1, count):
        u = side.length * i / count
        if u0 < u - width / 2 and u + width / 2 < u1:
            side.slab(geometry, look['fin_part'], u - width / 2, u + width / 2, z0, z1, front, back)


def far_facade(geometry, side, u0, u1, z0, z1, bay, cell, depth=0.0):
    """Plain storey quads whose u split matches the near facade's full-side bay split."""
    count = max(1, round(side.length / bay))
    grid_u0 = u0 * count / side.length
    grid_u1 = u1 * count / side.length
    first = round(z0 / STOREY)
    last = round(z1 / STOREY)
    for storey in range(first, last):
        za, zb = storey * STOREY, (storey + 1) * STOREY
        side.grid_plane(geometry, u0, u1, za, zb, depth, grid_u0, grid_u1, cell)


def subtract(span, covers):
    """What remains of the interval span after removing each interval in covers."""
    pieces = [span]
    for c0, c1 in covers:
        pieces = [p for a, b in pieces for p in ((a, min(b, c0)), (max(a, c1), b)) if p[1] - p[0] > 1e-4]
    return pieces


def touching(side, rect, other):
    """The along-face interval where other's rectangle abuts this side, or None."""
    x0, y0, x1, y1 = rect
    ox0, oy0, ox1, oy1 = other
    line = {'s': (oy1, y0), 'n': (oy0, y1), 'e': (ox0, x1), 'w': (ox1, x0)}[side.side]
    if abs(line[0] - line[1]) > 1e-3:
        return None
    if side.side in ('s', 'n'):
        lo, hi = max(x0, ox0), min(x1, ox1)
        span = (lo - x0, hi - x0) if side.side == 's' else (x1 - hi, x1 - lo)
    else:
        lo, hi = max(y0, oy0), min(y1, oy1)
        span = (lo - y0, hi - y0) if side.side == 'e' else (y1 - hi, y1 - lo)
    return span if span[1] - span[0] > 1e-3 else None


def wing(name, rect, storeys, style, crown=False, inset=0.0, soffit=False):
    return {'name': name, 'rect': rect, 'storeys': storeys, 'style': style, 'crown': crown, 'inset': inset,
            'soffit': soffit, 'z0': None, 'capped': True}


def stack(wings, base):
    """Sets each wing's base: a wing sits on the last earlier wing whose rectangle contains it."""
    for i, w in enumerate(wings):
        w['z0'] = base
        for below in reversed(wings[:i]):
            bx0, by0, bx1, by1 = below['rect']
            x0, y0, x1, y1 = w['rect']
            if bx0 - 1e-3 <= x0 and x1 <= bx1 + 1e-3 and by0 - 1e-3 <= y0 and y1 <= by1 + 1e-3:
                w['z0'] = below['z0'] + below['storeys'] * STOREY
                below['capped'] = False
                break
    return wings


def draw_wing(geometry, w, wings):
    x0, y0, x1, y1 = w['rect']
    z0, z1 = w['z0'], w['z0'] + w['storeys'] * STOREY
    if w['style'] == 'reveal':
        d = w['inset']
        geometry.box('reveal', (x0 + d, y0 + d, z0), (x1 - d, y1 - d, z1))
        return
    for name in SIDES:
        side = Side(w['rect'], name)
        cuts = sorted({0.0, side.length} | {e for o in wings if o is not w and (t := touching(side, w['rect'], o['rect']))
                                            for e in t})
        for a, b in zip(cuts, cuts[1:]):
            covers = [(o['z0'], o['z0'] + o['storeys'] * STOREY) for o in wings if o is not w and o['style'] != 'reveal'
                      and (t := touching(side, w['rect'], o['rect'])) and t[0] <= a + 1e-4 and b - 1e-4 <= t[1]]
            for za, zb in subtract((z0, z1), covers):
                facade(geometry, side, a, b, za, zb, z0, w['style'])
                if w['capped'] and abs(zb - z1) < 1e-4:
                    side.slab(geometry, FACADES[w['style']]['spandrel_part'], a, b, z1, z1 + 1.2, -FACADES[w['style']]['out'], .3)
                    side.slab(geometry, 'trim', a, b, z1 + 1.2, z1 + 1.3, -FACADES[w['style']]['out'] - .05, .35)
    if w['capped']:
        geometry.box('membrane', (x0 + .1, y0 + .1, z1 - .2), (x1 - .1, y1 - .1, z1 + .05))
    if w['soffit']:
        geometry.box('trim', (x0 - .05, y0 - .05, z0), (x1 + .05, y1 + .05, z0 + .3))
    if w['crown']:
        crown(geometry, w['rect'], z1)


def draw_wing_far(geometry, w, wings, variant):
    """The same wing stack as near mode, with one plain facade quad per open storey band."""
    x0, y0, x1, y1 = w['rect']
    z0, z1 = w['z0'], w['z0'] + w['storeys'] * STOREY
    cell = FAR_CELLS[f'{variant}-shaft']
    if w['style'] == 'reveal':
        d = w['inset']
        rect = (x0 + d, y0 + d, x1 - d, y1 - d)
        for name in SIDES:
            side = Side(rect, name)
            side.grid_plane(geometry, 0, side.length, z0, z1, 0, 0, 1, cell)
        return
    for name in SIDES:
        side = Side(w['rect'], name)
        cuts = sorted({0.0, side.length} | {e for o in wings if o is not w and (t := touching(side, w['rect'], o['rect']))
                                            for e in t})
        for a, b in zip(cuts, cuts[1:]):
            covers = [(o['z0'], o['z0'] + o['storeys'] * STOREY) for o in wings if o is not w and o['style'] != 'reveal'
                      and (t := touching(side, w['rect'], o['rect'])) and t[0] <= a + 1e-4 and b - 1e-4 <= t[1]]
            for za, zb in subtract((z0, z1), covers):
                far_facade(geometry, side, a, b, za, zb, FACADES[w['style']]['fin_every'], cell)
                if w['capped'] and abs(zb - z1) < 1e-4:
                    side.slab(geometry, 'far-facade', a, b, z1, z1 + 1.2, -FACADES[w['style']]['out'], .3)
                    side.slab(geometry, 'trim', a, b, z1 + 1.2, z1 + 1.3,
                              -FACADES[w['style']]['out'] - .05, .35)
    if w['capped']:
        geometry.box('membrane', (x0 + .1, y0 + .1, z1 - .2), (x1 - .1, y1 - .1, z1 + .05))
    if w['soffit']:
        geometry.box('trim', (x0 - .05, y0 - .05, z0), (x1 + .05, y1 + .05, z0 + .3))
    if w['crown']:
        far_crown(geometry, w['rect'], z1)


def crown(geometry, rect, z):
    """A louvred plant screen set back from the parapet, with plant behind it."""
    x0, y0, x1, y1 = rect
    s, h = 2.5, 4.0
    screen = (x0 + s, y0 + s, x1 - s, y1 - s)
    geometry.box('spandrel', (screen[0] + .3, screen[1] + .3, z), (screen[2] - .3, screen[3] - .3, z + h - .3))
    for name in SIDES:
        side = Side(screen, name)
        for blade in range(7):
            v = z + .4 + blade * .5
            side.slab(geometry, 'metal', 0, side.length, v, v + .12, -.1, .3)
        for u in (0, side.length):
            side.slab(geometry, 'metal', max(u - .15, 0), min(u + .15, side.length), z, z + h, -.1, .3)
        side.slab(geometry, 'metal', 0, side.length, z + h - .3, z + h, -.15, .3)


def far_crown(geometry, rect, z):
    """A plain screen box that keeps the crown's outline and landmark height."""
    x0, y0, x1, y1 = rect
    s, h = 2.5, 4.0
    screen = (x0 + s, y0 + s, x1 - s, y1 - s)
    for name in SIDES:
        side = Side(screen, name)
        side.grid_plane(geometry, 0, side.length, z, z + h, 0, 0, 1, FAR_CELLS['crown'])
    geometry.box('membrane', (screen[0] + .1, screen[1] + .1, z + h - .1),
                 (screen[2] - .1, screen[3] - .1, z + h))


def terrace(geometry, rect, z):
    """A planted roof inside the parapet: paving, planters along two edges and three trees."""
    x0, y0, x1, y1 = (rect[0] + .4, rect[1] + .4, rect[2] - .4, rect[3] - .4)
    geometry.box('paving', (x0, y0, z), (x1, y1, z + .2))
    long_x = x1 - x0 >= y1 - y0
    for edge in (0, 1):
        if long_x:
            y = y0 + .6 if edge == 0 else y1 - 1.8
            geometry.box('planter', (x0 + .6, y, z + .2), (x1 - .6, y + 1.2, z + .9))
        else:
            x = x0 + .6 if edge == 0 else x1 - 1.8
            geometry.box('planter', (x, y0 + .6, z + .2), (x + 1.2, y1 - .6, z + .9))
    for t in (.2, .5, .8):
        at = (x0 + (x1 - x0) * t, y0 + 1.2, z + 3.1) if long_x else (x0 + 1.2, y0 + (y1 - y0) * t, z + 3.1)
        geometry.icosphere('tree', at, 1.6)


def podium(geometry, storeys, wings):
    """The podium over the whole site: shopfronts on the ground storey, brick ribbon above, a
    parapet, and a planted roof round the towers."""
    half = SITE / 2
    rect = (-half, -half, half, half)
    top = storeys * STOREY
    geometry.box('plinth', (-half - .05, -half - .05, 0), (half + .05, half + .05, .3))
    for name in SIDES:
        side = Side(rect, name)
        side.plane(geometry, 'glass', 0, side.length, .3, 3.0, RECESS + .1)
        for i in range(1, round(side.length / 3)):
            u = side.length * i / round(side.length / 3)
            side.slab(geometry, 'frame', u - .05, u + .05, .3, 3.0, RECESS, RECESS + .1)
        for i in range(0, round(side.length / 12) + 1):
            u = min(max(side.length * i / round(side.length / 12), .4), side.length - .4)
            side.slab(geometry, 'wall-end', u - .4, u + .4, .3, 3.0, 0, RECESS + .1)
        side.slab(geometry, 'trim', 0, side.length, 3.0, STOREY, -.15, RECESS + .1)
        if storeys > 1:
            facade(geometry, side, 0, side.length, STOREY, top, STOREY, 'podium')
        side.slab(geometry, 'wall-end', 0, side.length, top, top + 1.1, 0, .3)
        side.slab(geometry, 'trim', 0, side.length, top + 1.1, top + 1.2, -.05, .35)
        for u in (side.length * .25, side.length * .75):
            side.slab(geometry, 'trim', u - 3, u + 3, 3.1, 3.25, -2.2, 0)
            side.slab(geometry, 'door', u - 1.2, u + 1.2, .3, 2.6, RECESS - .05, RECESS + .05)
    geometry.box('membrane', (-half + .3, -half + .3, top - .2), (half - .3, half - .3, top))
    geometry.box('paving', (-half + .3, -half + .3, top), (half - .3, half - .3, top + .15))
    for name in SIDES:
        side = Side((-half + 2, -half + 2, half - 2, half - 2), name)
        side.slab(geometry, 'planter', 2, side.length - 2, top + .15, top + .85, 0, 1.4)
        for i in range(1, 12):
            u = side.length * i / 12
            p = side.point(u, top + 3.0, .7)
            if not any(o['rect'][0] - 3 < p[0] < o['rect'][2] + 3 and o['rect'][1] - 3 < p[1] < o['rect'][3] + 3
                       for o in wings):
                geometry.icosphere('tree', p, 1.8)


def far_podium(geometry, storeys, variant):
    """The podium mass and roof with openings, canopies, mullions and landscaping removed."""
    half = SITE / 2
    rect = (-half, -half, half, half)
    top = storeys * STOREY
    geometry.box('plinth', (-half - .05, -half - .05, 0), (half + .05, half + .05, .3))
    for name in SIDES:
        side = Side(rect, name)
        far_facade(geometry, side, 0, side.length, 0, STOREY, 3.0,
                   FAR_CELLS[f'{variant}-podium-ground'])
        if storeys > 1:
            far_facade(geometry, side, 0, side.length, STOREY, top,
                       FACADES['podium']['fin_every'], FAR_CELLS[f'{variant}-podium-typical'])
        side.slab(geometry, 'far-facade', 0, side.length, top, top + 1.1, 0, .3)
        side.slab(geometry, 'trim', 0, side.length, top + 1.1, top + 1.2, -.05, .35)
    geometry.box('membrane', (-half + .3, -half + .3, top - .2), (half - .3, half - .3, top))
    geometry.box('paving', (-half + .3, -half + .3, top), (half - .3, half - .3, top + .15))


def spine_and_front(name, x0, y0, spine_side, share, shaft):
    """One 29 m stepped tower: a full-height spine on spine_side and a lower front block."""
    tower, spine = 29, 17
    x1, y1 = x0 + tower, y0 + tower
    spine_rect, front_rect = {
        'w': ((x0, y0, x0 + spine, y1), (x0 + spine, y0, x1, y1)),
        'e': ((x1 - spine, y0, x1, y1), (x0, y0, x1 - spine, y1)),
        's': ((x0, y0, x1, y0 + spine), (x0, y0 + spine, x1, y1)),
        'n': ((x0, y1 - spine, x1, y1), (x0, y0, x1, y1 - spine))}[spine_side]
    spine_storeys = round(shaft * share * 1.2)
    target = tower * tower * shaft * share
    front_area = tower * (tower - spine)
    front_storeys = max(1, round((target - tower * spine * spine_storeys) / front_area))
    high = spine_storeys - front_storeys - 1
    return [wing(f'{name}-front', front_rect, front_storeys, 'curtain'),
            wing(f'{name}-spine-low', spine_rect, front_storeys, 'spine'),
            wing(f'{name}-reveal', spine_rect, 1, 'reveal', inset=.8),
            wing(f'{name}-spine-high', spine_rect, high, 'spine', crown=True, soffit=True)]


def variant_wings(variant, shaft):
    """The wings of a variant, sized so their floor matches a shaft of the given storeys."""
    target = (SITE // 2) ** 2 * shaft
    near, far = -44, 15
    if variant == 'point':
        return [wing(f't{i}', (x, y, x + 29, y + 29), shaft, 'banded', crown=True)
                for i, (x, y) in enumerate(((near, near), (far, near), (far, far), (near, far)))]
    if variant == 'stepped-point':
        layout = ((near, near, 'w'), (far, near, 's'), (far, far, 'e'), (near, far, 'n'))
        shares = (1.15, .85, 1.05, .95)
        return [w for i, ((x, y, s), share) in enumerate(zip(layout, shares))
                for w in spine_and_front(f't{i}', x, y, s, share, shaft)]
    if variant == 'l':
        street_rect, back_rect = (-48, -48, 48, -28), (-48, -28, -28, 48)
        street_storeys = round(shaft * .75)
        back_storeys = round((target - 96 * 20 * street_storeys) / (20 * 76))
        return [wing('street-arm', street_rect, street_storeys, 'ribbon', crown=True),
                wing('back-arm', back_rect, back_storeys, 'ribbon', crown=True)]
    if variant == 'h':
        storeys = round(target / (2 * 20 * 80 + 40 * 20))
        return [wing('west-bar', (-40, -40, -20, 40), storeys, 'curtain', crown=True),
                wing('east-bar', (20, -40, 40, 40), storeys, 'curtain', crown=True),
                wing('link', (-20, -10, 20, 10), storeys, 'curtain')]
    raise ValueError(variant)


def site(geometry, variant, podium_storeys, far=False):
    shaft = TOTAL_STOREYS - podium_storeys
    wings = stack(variant_wings(variant, shaft), podium_storeys * STOREY)
    if far:
        far_podium(geometry, podium_storeys, variant)
    else:
        podium(geometry, podium_storeys, wings)
    for w in wings:
        if far:
            draw_wing_far(geometry, w, wings, variant)
        else:
            draw_wing(geometry, w, wings)
        if w['name'].endswith('-front'):
            if far:
                x0, y0, x1, y1 = (w['rect'][0] + .4, w['rect'][1] + .4,
                                  w['rect'][2] - .4, w['rect'][3] - .4)
                geometry.box('paving', (x0, y0, w['z0'] + w['storeys'] * STOREY),
                             (x1, y1, w['z0'] + w['storeys'] * STOREY + .2))
            else:
                terrace(geometry, w['rect'], w['z0'] + w['storeys'] * STOREY)
    drawn = sum((w['rect'][2] - w['rect'][0]) * (w['rect'][3] - w['rect'][1]) * w['storeys'] for w in wings)
    simulated = (SITE // 2) ** 2 * shaft
    top = max(w['z0'] + w['storeys'] * STOREY for w in wings)
    return {'variant': variant, 'podium_storeys': podium_storeys, 'shaft_floor': simulated, 'drawn_floor': drawn,
            'difference_percent': round(100 * (drawn - simulated) / simulated, 2), 'top_metres': top,
            'wings': [(w['name'], w['storeys']) for w in wings if w['style'] != 'reveal']}


def materials():
    made = {}
    for part, (texture, colour) in PARTS.items():
        if texture:
            made[part] = street.surface(f'{part}-{texture}', texture, colour)
            continue
        material = bpy.data.materials.new(part)
        material.use_nodes = True
        rgba = street.rgb(colour) + (1,)
        shader = material.node_tree.nodes['Principled BSDF']
        shader.inputs['Base Color'].default_value = rgba
        shader.inputs['Roughness'].default_value = .15 if part == 'glass' else .8
        shader.inputs['Metallic'].default_value = .6 if part in ('glass', 'metal', 'frame') else 0
        material.diffuse_color = rgba
        made[part] = material
    far = bpy.data.materials.new('far-facade')
    far.use_nodes = True
    far.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (.55, .58, .6, 1)
    far.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = .8
    made['far-facade'] = far
    return made


def build(name, geometry, made, offset=(0, 0)):
    vertices, faces, indices, grids, used = [], [], [], [], []
    for part, (vs, fs) in geometry.parts.items():
        base = len(vertices)
        vertices.extend((x + offset[0], y + offset[1], z) for x, y, z in vs)
        faces.extend(tuple(base + i for i in f) for f in fs)
        indices.extend([len(used)] * len(fs))
        part_grids = geometry.grid_uvs.get(part, [None] * len(fs))
        assert len(part_grids) == len(fs), (name, part, len(part_grids), len(fs))
        grids.extend(part_grids)
        used.append(made[part])
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    for material in used:
        data.materials.append(material)
    for polygon, index in zip(data.polygons, indices):
        polygon.material_index = index
    data.update()
    body = bpy.data.objects.new(name, data)
    body['site_offset_x'] = offset[0]
    body['site_offset_y'] = offset[1]
    bpy.context.scene.collection.objects.link(body)
    degenerate = sum(1 for p in data.polygons if p.area <= 1e-8)
    assert not degenerate, (name, degenerate)
    street.project_uvs(body)
    if any(grids):
        uv = data.uv_layers['UVMap']
        uv2 = data.uv_layers.new(name='uv2')
        for coordinate in uv2.data:
            coordinate.uv = (0, 0)
        for polygon, grid in zip(data.polygons, grids):
            if not grid:
                continue
            coordinates, cell = grid
            assert len(coordinates) == len(polygon.loop_indices), (name, polygon.index)
            for loop, coordinate in zip(polygon.loop_indices, coordinates):
                uv.data[loop].uv = coordinate
                uv2.data[loop].uv = (cell, 0)
    return body


def census(geometry):
    """Faces per part and bounds for a generated site."""
    points = [vertex for vertices, _ in geometry.parts.values() for vertex in vertices]
    return {'faces': {part: len(faces) for part, (_, faces) in sorted(geometry.parts.items())},
            'bounds': [[round(min(point[i] for point in points), 3) for i in range(3)],
                       [round(max(point[i] for point in points), 3) for i in range(3)]]}


def reset():
    for thing in list(bpy.data.objects):
        bpy.data.objects.remove(thing)
    for data in list(bpy.data.meshes):
        bpy.data.meshes.remove(data)
    for material in list(bpy.data.materials):
        bpy.data.materials.remove(material)
    return materials()


VARIANTS = ('point', 'stepped-point', 'l', 'h')


def main():
    SOURCES.mkdir(parents=True, exist_ok=True)
    EXPORT.mkdir(parents=True, exist_ok=True)
    bpy.context.preferences.filepaths.save_version = 0
    reports = []
    for variant in VARIANTS:
        for podium_storeys in PODIUM_STOREYS:
            near = Geometry()
            report = site(near, variant, podium_storeys)
            assert abs(report['difference_percent']) < 1, report
            far = Geometry()
            far_report = site(far, variant, podium_storeys, far=True)
            assert far_report == report, (report, far_report)
            far_census = census(far)
            report.update({'far_faces': far_census['faces'], 'far_bounds': far_census['bounds']})
            reports.append(report)
            print('TALL BOUNDS', variant, podium_storeys, census(near)['bounds'], far_census['bounds'])
        made = reset()
        near = Geometry()
        far = Geometry()
        site(near, variant, EXPORTED_PODIUM)
        site(far, variant, EXPORTED_PODIUM, far=True)
        spacing = SITE + 30
        build(f'{variant}-near', near, made, (-spacing / 2, 0))
        build(f'{variant}-far', far, made, (spacing / 2, 0))
        bpy.ops.wm.save_as_mainfile(filepath=str(SOURCES / f'{variant}.blend'))

    made = reset()
    swatches = Geometry()
    for i, part in enumerate(p for p, (texture, _) in PARTS.items() if not texture):
        swatches.box(part, (i * 2, 0, 0), (i * 2 + 1, 1, 1))
    library = build('library', swatches, made)
    bpy.ops.object.select_all(action='DESELECT')
    library.select_set(True)
    bpy.ops.export_scene.gltf(filepath=str(EXPORT / 'library.glb'), export_format='GLB', use_selection=True,
                              export_apply=True, export_yup=True)

    made = reset()
    pair_spacing = 2 * SITE + 100
    for i, variant in enumerate(VARIANTS):
        center = (i - 1.5) * pair_spacing
        near = Geometry()
        far = Geometry()
        site(near, variant, EXPORTED_PODIUM)
        site(far, variant, EXPORTED_PODIUM, far=True)
        build(f'{variant}-near', near, made, (center - (SITE + 30) / 2, 0))
        build(f'{variant}-far', far, made, (center + (SITE + 30) / 2, 0))
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCES / 'lineup.blend'))

    (SOURCES / 'bodies.json').write_text(json.dumps({'storey_metres': STOREY, 'total_storeys': TOTAL_STOREYS,
                                                     'facades': FACADES, 'far_cells': FAR_CELLS,
                                                     'sites': reports}, indent=2) + '\n')
    for report in reports:
        print('TALL', report)


if __name__ == "__main__":
    main()

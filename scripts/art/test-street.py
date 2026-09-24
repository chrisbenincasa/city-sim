"""The first procedural-buildings test street: pass 03's five bodies and the A2 alternative, with surface materials.

Run with Blender in the background:
  blender --background --factory-startup --python-exit-code 1 --python scripts/art/test-street.py

Dimensions follow research/city-architecture/output/03-context-and-construction/model-briefs.md,
except that every storey is 3.5 m, the simulation's storey height, where the briefs propose 3.2 m.
Each body's origin is its footprint centre at ground level. Its street or lane face looks down -Y,
which Godot receives as +Z. Faces are wound counter-clockwise seen from outside.
"""
import bpy
import bmesh
import hashlib
import json
import math
from pathlib import Path

from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
SOURCES = ROOT / 'art/test-street'
EXPORT = ROOT / 'src/Borough.Godot/assets/test-street'
STOREY = 3.5
REVEAL = .18

COLOURS = {
    'wall': 'd9d8d2', 'wall-end': 'c9c8c1', 'trim': 'f0efe9', 'roof': '85888a', 'membrane': '6f7274',
    'glass': '39454d', 'door': '4f5253', 'frame': 'eeeeea', 'metal': '8d9194', 'plinth': 'a3a29b',
    'roof-new': '5a524a', 'storefront': '3a342e', 'sign': '2f5446', 'awning': '7e3a2f', 'solar': '1d2733',
}

FETCHED = json.loads((ROOT / 'art/materials/test-street/materials.json').read_text())
SURFACES = {name: {'metres': s['tile_metres'], 'mean': FETCHED['paint_mean_linear_luminance'] if s['paint'] else None,
                   'maps': {key: ROOT / f['file'] for key, f in s['files'].items()}}
            for name, s in FETCHED['surfaces'].items()}
CITY = ROOT / 'src/Borough.Godot/assets/city'
SURFACES['brick'] = {'metres': [1.125, 1.125], 'mean': None,
                     'maps': {'albedo': CITY / 'brick-wall-diffuse.jpg', 'normal': CITY / 'brick-wall-normal.jpg'}}
SURFACES['shingles'] = {'metres': [4.0, 4.0],
                        'mean': json.loads((CITY / 'roofing/asphalt-shingles.json').read_text())['albedo_linear_mean_luminance'],
                        'maps': {key: CITY / f'roofing/asphalt-shingles-{key}.png' for key in ('albedo', 'normal', 'roughness')}}
TEXTURE_PIXELS = 1024

# A body's finish replaces a blockout material with a surface and a paint colour. A painted surface
# divides the colour by its albedo's mean luminance; None keeps the photograph's own colour.
FINISHES = {
    'h1-attached-range': {'wall': ('siding', 'b9ad97'), 'wall-end': ('siding', 'a39985'), 'roof': ('shingles', '353e44')},
    'a1-apartment': {'wall': ('render', 'cdc6b6'), 'membrane': ('membrane', None)},
    'a2-stair-range': {'wall': ('render', 'cdc6b6'), 'membrane': ('membrane', None)},
    'm1-corner': {'wall': ('brick', None), 'membrane': ('membrane', None)},
    'w1-workplace': {'wall': ('block', 'c3bcaa'), 'wall-end': ('block', 'ada691'), 'membrane': ('membrane', None)},
    'w2-workshop': {'wall': ('sheet', '5d6a6e'), 'wall-end': (None, '4f5b5f'), 'membrane': ('membrane', None)},
    'g003-two-tenancy': {'wall': ('render', 'cdc6b6'), 'wall-end': ('render', 'b8b1a1'), 'membrane': ('membrane', None)},
}
FINISHES['h1-reroofed'] = FINISHES['h1-attached-range'] | {'roof-new': ('shingles', '4d463f')}
FINISHES['m1-repaired'] = FINISHES['m1-corner']
FINISHES['w1-solar'] = FINISHES['w1-workplace']
BOX_FACES = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]

parts = []
materials = {}


def linear(channel):
    return channel / 12.92 if channel <= .04045 else ((channel + .055) / 1.055) ** 2.4


def rgb(code):
    return tuple(linear(int(code[i:i + 2], 16) / 255) for i in (0, 2, 4))


def image(path, colour):
    loaded = bpy.data.images.load(str(path), check_existing=True)
    if tuple(loaded.size) != (TEXTURE_PIXELS, TEXTURE_PIXELS) and loaded.size[0] > TEXTURE_PIXELS:
        loaded.scale(TEXTURE_PIXELS, TEXTURE_PIXELS * loaded.size[1] // loaded.size[0])
    if not colour:
        loaded.colorspace_settings.name = 'Non-Color'
    return loaded


def surface(name, key, code):
    """A textured material. Its tile size rides along for project_uvs."""
    spec = SURFACES[key]
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    material['tile_metres'] = spec['metres']
    nodes, links = material.node_tree.nodes, material.node_tree.links
    shader = nodes['Principled BSDF']
    albedo = nodes.new('ShaderNodeTexImage')
    albedo.image = image(spec['maps']['albedo'], True)
    factor = (1, 1, 1)
    if code:
        factor = tuple(c / spec['mean'] for c in rgb(code))
        assert max(factor) <= 1, (name, factor)
        tint = nodes.new('ShaderNodeMix')
        tint.data_type, tint.blend_type = 'RGBA', 'MULTIPLY'
        tint.inputs['Factor'].default_value = 1
        links.new(albedo.outputs['Color'], tint.inputs['A'])
        tint.inputs['B'].default_value = factor + (1,)
        links.new(tint.outputs['Result'], shader.inputs['Base Color'])
    else:
        links.new(albedo.outputs['Color'], shader.inputs['Base Color'])
    normal = nodes.new('ShaderNodeTexImage')
    normal.image = image(spec['maps']['normal'], False)
    bump = nodes.new('ShaderNodeNormalMap')
    links.new(normal.outputs['Color'], bump.inputs['Color'])
    links.new(bump.outputs['Normal'], shader.inputs['Normal'])
    if 'roughness' in spec['maps']:
        roughness = nodes.new('ShaderNodeTexImage')
        roughness.image = image(spec['maps']['roughness'], False)
        links.new(roughness.outputs['Color'], shader.inputs['Roughness'])
    else:
        shader.inputs['Roughness'].default_value = .85
    material.diffuse_color = tuple(min(1.0, f * .5) for f in factor) + (1,)
    return material


def reset(finish):
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for data in list(bpy.data.meshes):
        bpy.data.meshes.remove(data)
    for material in list(bpy.data.materials):
        bpy.data.materials.remove(material)
    parts.clear()
    materials.clear()
    for name, code in COLOURS.items():
        key, paint = finish.get(name, (None, None))
        if key:
            materials[name] = surface(f'{name}-{key}', key, paint)
            continue
        material = bpy.data.materials.new(name)
        material.use_nodes = True
        colour = rgb(paint or code) + (1,)
        shader = material.node_tree.nodes['Principled BSDF']
        shader.inputs['Base Color'].default_value = colour
        shader.inputs['Roughness'].default_value = .25 if name == 'glass' else .85
        material.diffuse_color = colour
        materials[name] = material


def project_uvs(body):
    """World-scale UVs: u runs level along each face and v runs up it, or up the slope of a roof."""
    bm = bmesh.new()
    bm.from_mesh(body.data)
    layer = bm.loops.layers.uv.new('UVMap')
    for face in bm.faces:
        width, height = body.data.materials[face.material_index].get('tile_metres', (1.0, 1.0))
        n = face.normal
        along = Vector((-n.y, n.x, 0)).normalized() if abs(n.z) < .95 else Vector((1, 0, 0))
        up = n.cross(along)
        for loop in face.loops:
            loop[layer].uv = (loop.vert.co.dot(along) / width, loop.vert.co.dot(up) / height)
    bm.to_mesh(body.data)
    bm.free()


def mesh(name, vertices, faces, material):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.materials.append(materials[material])
    data.update()
    thing = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(thing)
    parts.append(thing)


def box(name, low, high, material):
    (x0, y0, z0), (x1, y1, z1) = low, high
    mesh(name, [(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0),
                (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)], BOX_FACES, material)


def prism(name, profile, x0, x1, material):
    """A solid from x0 to x1 whose cross-section is a convex (y, z) profile, anticlockwise seen from +X."""
    n = len(profile)
    vertices = [(x0, y, z) for y, z in profile] + [(x1, y, z) for y, z in profile]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2 * n))]
    faces += [(i, (i + 1) % n, n + (i + 1) % n, n + i) for i in range(n)]
    mesh(name, vertices, faces, material)


class Face:
    """A wall plane. u runs along the wall, to the right of someone outside facing it; v runs up;
    depth runs inward. (u, depth, v) is a right-handed frame on every face."""

    def __init__(self, origin, along, outward):
        self.origin, self.along, self.outward = origin, along, outward

    def point(self, u, v, depth=0.0):
        return tuple(self.origin[i] + self.along[i] * u - self.outward[i] * depth + (v if i == 2 else 0)
                     for i in range(3))

    def slab(self, name, u0, u1, v0, v1, d0, d1, material):
        corners = [(u0, d0, v0), (u1, d0, v0), (u1, d1, v0), (u0, d1, v0),
                   (u0, d0, v1), (u1, d0, v1), (u1, d1, v1), (u0, d1, v1)]
        mesh(name, [self.point(u, v, d) for u, d, v in corners], BOX_FACES, material)


def facade(name, face, width, height, openings, material='wall'):
    """A wall with openings cut through it. An opening is (u, v, width, height, kind)."""
    us = sorted({round(u, 4) for u in (0, width, *[o[0] for o in openings], *[o[0] + o[2] for o in openings])})
    vs = sorted({round(v, 4) for v in (0, height, *[o[1] for o in openings], *[o[1] + o[3] for o in openings])})
    for o in openings:
        assert 0 < o[0] and o[0] + o[2] < width and 0 < o[1] and o[1] + o[3] < height, (name, o)
    vertices, faces = [], []
    for a, b in zip(us, us[1:]):
        for c, d in zip(vs, vs[1:]):
            u, v = (a + b) / 2, (c + d) / 2
            if any(o[0] < u < o[0] + o[2] and o[1] < v < o[1] + o[3] for o in openings):
                continue
            base = len(vertices)
            vertices += [face.point(a, c), face.point(b, c), face.point(b, d), face.point(a, d)]
            faces.append((base, base + 1, base + 2, base + 3))
    mesh(name, vertices, faces, material)
    for u, v, w, h, kind in openings:
        depth = REVEAL + (.05 if kind in ('door', 'roller') else 0)
        corners = [(u, v), (u + w, v), (u + w, v + h), (u, v + h)]
        quads = [[face.point(p, q), face.point(r, s), face.point(r, s, depth), face.point(p, q, depth)]
                 for (p, q), (r, s) in zip(corners, corners[1:] + corners[:1])]
        mesh(f'{name} reveal', [c for quad in quads for c in quad],
             [(i * 4, i * 4 + 1, i * 4 + 2, i * 4 + 3) for i in range(4)], 'trim')
        mesh(f'{name} {kind}', [face.point(p, q, depth) for p, q in corners], [(0, 1, 2, 3)],
             'door' if kind in ('door', 'roller') else 'glass')
        if kind == 'window':
            face.slab(f'{name} sill', u - .06, u + w + .06, v - .07, v, -.06, .02, 'trim')
        if kind in ('shop', 'shop-new', 'stair', 'window') and w > 1.6:
            count = round(w / 1.5) - 1
            frame = 'storefront' if kind == 'shop-new' else 'frame'
            for i in range(1, count + 1):
                x = u + w * i / (count + 1)
                face.slab(f'{name} mullion', x - .035, x + .035, v, v + h, depth - .08, depth, frame)
        if kind == 'shop-new':
            face.slab(f'{name} transom', u, u + w, v + h - .45, v + h - .38, depth - .08, depth, 'storefront')
            face.slab(f'{name} kickplate', u, u + w, v, v + .35, depth - .1, depth - .02, 'storefront')
        if kind == 'roller':
            for i in range(1, int(h / .5)):
                face.slab(f'{name} slat', u, u + w, v + i * .5 - .015, v + i * .5 + .015, depth - .03, depth, 'metal')


def walls(width, depth, height, front, back, left, right, end_material='wall'):
    """Four walls around the footprint centre, returned as faces for further detail."""
    x, y = width / 2, depth / 2
    faces = {'front': Face((-x, -y, 0), (1, 0, 0), (0, -1, 0)), 'back': Face((x, y, 0), (-1, 0, 0), (0, 1, 0)),
             'left': Face((-x, y, 0), (0, -1, 0), (-1, 0, 0)), 'right': Face((x, -y, 0), (0, 1, 0), (1, 0, 0))}
    facade('front', faces['front'], width, height, front)
    facade('back', faces['back'], width, height, back)
    facade('left end', faces['left'], depth, height, left, end_material)
    facade('right end', faces['right'], depth, height, right, end_material)
    return faces


def bays(centres, v, w, h, kind='window'):
    return [(c - w / 2, v, w, h, kind) for c in centres]


def upper(count, centres, w=1.4, h=1.7, sill=.95):
    return [o for s in range(1, count) for o in bays(centres, s * STOREY + sill, w, h)]


def plinth(width, depth):
    box('plinth', (-width / 2 - .05, -depth / 2 - .05, 0), (width / 2 + .05, depth / 2 + .05, .3), 'plinth')


def flat_roof(width, depth, height, parapet=.7):
    x, y, top = width / 2, depth / 2, height + parapet
    box('roof deck', (-x + .25, -y + .25, height - .2), (x - .25, y - .25, height), 'membrane')
    box('parapet front', (-x, -y, height), (x, -y + .25, top), 'wall')
    box('parapet back', (-x, y - .25, height), (x, y, top), 'wall')
    box('parapet left', (-x, -y + .25, height), (-x + .25, y - .25, top), 'wall')
    box('parapet right', (x - .25, -y + .25, height), (x, y - .25, top), 'wall')
    box('coping front', (-x - .05, -y - .05, top), (x + .05, -y + .3, top + .08), 'trim')
    box('coping back', (-x - .05, y - .3, top), (x + .05, y + .05, top + .08), 'trim')
    box('coping left', (-x - .05, -y + .3, top), (-x + .3, y - .3, top + .08), 'trim')
    box('coping right', (x - .3, -y + .3, top), (x + .05, y - .3, top + .08), 'trim')


def gable(width, depth, height, pitch, eaves=.4, segments=None):
    """A pitched roof whose rafters span the depth, so the ridge runs along the street.
    Returns a function giving the roof's top surface height at a distance from the ridge."""
    slope = math.tan(math.radians(pitch))
    x, y, t = width / 2 + eaves * .5, depth / 2 + eaves, .2
    low, top = height - eaves * slope, height + depth / 2 * slope
    profile = [(-y, low), (-y, low + t), (0, top + t), (y, low + t), (y, low), (0, top)][::-1]
    for x0, x1, material in segments or [(-x, x, 'roof')]:
        prism('pitched roof', profile, max(x0, -x), min(x1, x), material)
    for sign in (-1, 1):
        gx = sign * width / 2
        vertices = [(gx, -depth / 2, height), (gx, depth / 2, height), (gx, 0, top)]
        mesh('gable end', vertices, [(0, 1, 2) if sign > 0 else (2, 1, 0)], 'wall')
    return lambda distance: top + t - abs(distance) * slope


def h1(reroofed=False):
    """Attached range: four 6 x 12 m house modules, two storeys, 18 degree roof across the depth.
    Reroofed, the third house carries newer shingles in another shade, split at the party walls."""
    width, depth, height = 24.0, 12.0, 2 * STOREY
    front, back = [], []
    for left in (0, 6, 12, 18):
        front += [(left + .9, .3, 1.0, 2.3, 'door'), (left + 3.0, 1.0, 2.2, 1.6, 'window'),
                  (left + .9, STOREY + .9, 1.0, 1.4, 'window'), (left + 3.0, STOREY + .9, 2.2, 1.6, 'window')]
        back += [(left + .8, .3, 2.6, 2.3, 'door'), (left + 4.2, 1.0, 1.2, 1.6, 'window'),
                 (left + .8, STOREY + .9, 2.2, 1.7, 'window'), (left + 4.0, STOREY + .9, 1.4, 1.7, 'window')]
    end = [(3.0, 1.0, .9, 1.4, 'window'), (8.0, STOREY + 1.0, .9, 1.4, 'window')]
    walls(width, depth, height, front, back, end, end, 'wall-end')
    plinth(width, depth)
    segments = [(-99, 0, 'roof'), (0, 6, 'roof-new'), (6, 99, 'roof')] if reroofed else None
    roof = gable(width, depth, height, 18, segments=segments)
    edge = depth / 2 + .15
    for x in (-6.0, 0.0, 6.0):
        profile = [(-edge, height - .3), (edge, height - .3), (edge, roof(edge) + .25), (0, roof(0) + .25),
                   (-edge, roof(edge) + .25)]
        prism('party wall upstand', profile, x - .15, x + .15, 'wall')
    for left in (0, 6, 12, 18):
        x = -width / 2 + left + 1.4
        box('front step', (x - .75, -depth / 2 - 1.0, 0), (x + .75, -depth / 2, .3), 'plinth')
        box('chimney', (x + 3.0, .8, roof(1.4) - .6), (x + 3.6, 1.4, roof(0) + .9), 'wall-end')


def a1():
    """Corridor apartment: 24 x 16 m, three storeys, one central entrance, stairs at both ends."""
    width, depth, height = 24.0, 16.0, 3 * STOREY
    centres = [1.5 + 3 * i for i in range(8)]
    ground = [c for i, c in enumerate(centres) if i not in (3, 4)]
    front = bays(ground, 1.0, 1.6, 1.7) + [(10.8, .3, 2.4, 2.5, 'door')] + upper(3, centres, w=1.6)
    back = bays(ground, 1.0, 1.6, 1.7) + [(10.8, .3, 2.4, 2.4, 'door')] + upper(3, centres, w=1.6)
    end = [(7.0, .3, 2.0, 2.4, 'door'), (7.0, STOREY + 1.2, 2.0, 2.6, 'stair'), (7.0, 2 * STOREY + 1.2, 2.0, 2.0, 'stair')]
    walls(width, depth, height, front, back, end, end)
    plinth(width, depth)
    flat_roof(width, depth, height)
    box('entrance canopy', (-1.8, -depth / 2 - 1.2, 2.9), (1.8, -depth / 2, 3.05), 'trim')
    for x in (-8.0, 8.0):
        box('end door canopy', (x * 1.5 - .5, -1.3, 2.8), (x * 1.5 + .5, 1.3, 2.95), 'trim')
    box('roof hatch', (-1.0, 1.0, height), (1.0, 2.2, height + .5), 'metal')
    for x in (-8.0, 8.0):
        box('rooftop vent', (x - .4, 3.0, height), (x + .4, 3.8, height + .9), 'metal')


def a2():
    """Stair-access range: 32 x 12 m, three storeys, two stair stacks each serving two flats a floor,
    no corridor, parapeted membrane roof, balconies to the garden."""
    width, depth, height = 32.0, 12.0, 3 * STOREY
    front = []
    for stair in (6.0, 22.0):
        front += [(stair + 1.0, .3, 2.0, 2.4, 'door'), (stair + 1.0, STOREY + 1.2, 2.0, 2.6, 'stair'),
                  (stair + 1.0, 2 * STOREY + 1.2, 2.0, 2.0, 'stair')]
    flats = [1.8, 4.3, 11.7, 14.3, 17.7, 20.3, 27.7, 30.2]
    front += bays(flats, .95, 1.4, 1.7) + upper(3, flats)
    back = []
    for flat in (4.0, 12.0, 20.0, 28.0):
        back += [(flat - 2.7, .3, 2.4, 2.3, 'door'), (flat + 1.5, .95, 1.4, 1.7, 'window')]
        back += [(flat - 2.7, s * STOREY + .15, 2.4, 2.3, 'door') for s in (1, 2)]
        back += [(flat + 1.5, s * STOREY + .95, 1.4, 1.7, 'window') for s in (1, 2)]
    end = [(x, s * STOREY + .95, 1.2, 1.5, 'window') for s in range(3) for x in (3.4, 7.4)]
    faces = walls(width, depth, height, front, back, end, end)
    plinth(width, depth)
    flat_roof(width, depth, height)
    for stair in (6.0, 22.0):
        x = -width / 2 + stair + 2.0
        box('entrance canopy', (x - 1.4, -depth / 2 - 1.1, 2.85), (x + 1.4, -depth / 2, 3.0), 'trim')
    garden = faces['back']
    for flat in (4.0, 12.0, 20.0, 28.0):
        u0, u1 = flat - 3.0, flat
        for s in (1, 2):
            v = s * STOREY
            garden.slab('balcony slab', u0, u1, v - .05, v + .15, -1.4, 0, 'trim')
            garden.slab('balcony rail', u0, u1, v + .15, v + 1.1, -1.4, -1.35, 'metal')
            for u in (u0, u1 - .05):
                garden.slab('balcony side', u, u + .05, v + .15, v + 1.1, -1.35, 0, 'metal')


def m1(repaired=False):
    """Corner mixed use: 24 x 16 m, three storeys, shopfront on the street, residential door and core
    on the side street (the +X end), receiving at the back, roof falling to the service side.
    Repaired, the west shop has a new bronze storefront, a sign board and a fabric awning."""
    width, depth, height = 24.0, 16.0, 3 * STOREY
    centres = [1.5 + 3 * i for i in range(8)]
    shop = lambda i: 'shop-new' if repaired and i < 4 else 'shop'
    front = [(i * 3 + .35, .3, 2.3, 2.6, 'door') if i in (1, 5) else (i * 3 + .35, .6, 2.3, 3.0, shop(i))
             for i in range(8)]
    front += upper(3, centres, w=1.5, h=1.8)
    back = [(2.0, .3, 3.2, 3.0, 'roller'), (7.0, .3, 1.0, 2.2, 'door')] + upper(3, centres, w=1.3, h=1.6)
    side = [(1.0, .6, 2.4, 3.0, 'shop'), (4.2, .6, 2.4, 3.0, 'shop'), (12.0, .3, 1.2, 2.4, 'door'),
            (13.5, .6, 1.2, 2.1, 'stair'), (12.2, STOREY + 1.2, 2.0, 2.6, 'stair'), (12.2, 2 * STOREY + 1.2, 2.0, 2.0, 'stair')]
    side += [(x, s * STOREY + .95, 1.5, 1.8, 'window') for s in (1, 2) for x in (1.5, 4.8, 8.1)]
    party = [(x, s * STOREY + .95, 1.3, 1.6, 'window') for s in (1, 2) for x in (4.0, 10.0)]
    walls(width, depth, height, front, back, party, side)
    plinth(width, depth)
    flat_roof(width, depth, height, parapet=.9)
    box('shop fascia', (-width / 2, -depth / 2 - .25, 3.75), (width / 2 + .25, -depth / 2, 4.3), 'trim')
    box('side fascia', (width / 2, -depth / 2, 3.75), (width / 2 + .25, 0, 4.3), 'trim')
    for sign in (-1, 1):
        if repaired and sign < 0:
            y = -depth / 2
            prism('fabric awning', [(y - 1.3, 3.4), (y, 3.83), (y, 3.95), (y - 1.3, 3.52)], -11.2, -.8, 'awning')
            box('shop sign', (-11.0, y - .32, 3.86), (-1.0, y - .25, 4.2), 'sign')
            continue
        box('shop awning', (sign * 6 - 5.2, -depth / 2 - 1.3, 3.62), (sign * 6 + 5.2, -depth / 2, 3.74), 'metal')
    box('residential canopy', (width / 2, 3.8, 2.85), (width / 2 + 1.0, 5.6, 3.0), 'trim')
    box('plant curb', (-6.0, 1.0, height), (2.0, 5.0, height + .35), 'metal')
    box('plant unit', (-5.4, 1.5, height + .35), (-2.4, 4.5, height + 1.6), 'metal')
    box('plant unit', (-1.6, 1.5, height + .35), (1.4, 4.5, height + 1.6), 'metal')
    box('stair overrun', (width / 2 - 5.0, 3.0, height), (width / 2 - .25, 7.75, height + 2.4), 'wall')
    for x in (-9.0, -3.0, 3.0):
        box('scupper', (x - .2, depth / 2, height + .05), (x + .2, depth / 2 + .3, height + .3), 'metal')
        box('downpipe', (x - .08, depth / 2, .3), (x + .08, depth / 2 + .16, height + .05), 'metal')


def w1(solar=False):
    """Workplace, archived G001: 36 x 20 m, two full floors, six 6 m bays, receiving at the back.
    With solar, rows of 10 degree panels cover the roof clear of the plant and crickets."""
    width, depth, height = 36.0, 20.0, 2 * STOREY
    front = [(12.8, .3, 2.4, 2.6, 'door'), (15.6, 1.0, 1.6, 1.9, 'shop')]
    front += [(bay * 6 + .8, 1.0, 4.4, 1.9, 'shop') for bay in (0, 1, 3, 4, 5)]
    front += [(bay * 6 + .8, STOREY + 1.0, 4.4, 1.9, 'shop') for bay in range(6)]
    back = [(.8, .3, 4.4, 4.0, 'roller'), (6.8, .3, 4.4, 4.0, 'roller'), (13.0, .3, 1.0, 2.3, 'door')]
    back += [(bay * 6 + .8, STOREY + 1.0, 4.4, 1.9, 'shop') for bay in range(6)]
    end = [(8.0, .3, 4.0, 2.5, 'stair'), (8.0, STOREY + 1.0, 4.0, 1.9, 'stair'), (15.0, .3, 1.0, 2.3, 'door')]
    walls(width, depth, height, front, back, end, end)
    plinth(width, depth)
    flat_roof(width, depth, height, parapet=.8)
    for x in [-width / 2 + bay * 6 for bay in range(7)]:
        box('pilaster', (x - .2, -depth / 2 - .2, .3), (x + .2, -depth / 2, height + .8), 'wall-end')
        box('pilaster', (x - .2, depth / 2, .3), (x + .2, depth / 2 + .2, height + .8), 'wall-end')
    box('entrance canopy', (-6.0, -depth / 2 - 1.4, 3.1), (-2.0, -depth / 2 - .2, 3.25), 'trim')
    box('receiving canopy', (6.5, depth / 2 + .2, 4.5), (17.8, depth / 2 + 2.2, 4.65), 'metal')
    for x in (-10.0, 8.0):
        box('plant curb', (x - 2.5, -2.0, height), (x + 2.5, 2.0, height + .35), 'metal')
        box('plant unit', (x - 2.0, -1.5, height + .35), (x + 2.0, 1.5, height + 1.5), 'metal')
        mesh('cricket', [(x - 2.5, 2.0, height), (x + 2.5, 2.0, height), (x, 3.5, height + .12)], [(0, 1, 2)], 'membrane')
    for x in (-15.0, -9.0, 3.0):
        box('scupper', (x - .2, depth / 2, height + .05), (x + .2, depth / 2 + .3, height + .3), 'metal')
        box('overflow', (x + 1.0, depth / 2, height + .3), (x + 1.3, depth / 2 + .2, height + .5), 'metal')
        box('downpipe', (x - .08, depth / 2, .3), (x + .08, depth / 2 + .16, height + .05), 'metal')
    if solar:
        rise = .95 * math.tan(math.radians(10))
        for y in (-8.6, -7.0, -5.4, -3.8, 4.4, 6.0, 7.6):
            low = height + .35
            for module in range(19):
                x = -16.5 + module * 1.74
                prism('solar module', [(y, low), (y + .95, low + rise), (y + .95, low + rise + .04), (y, low + .04)],
                      x, x + 1.7, 'solar')
            box('solar rail', (-16.5, y + .8, height), (16.5, y + .9, low + rise), 'metal')


def w2():
    """Workshop: 32 x 16 m, two full floors on an 8 m grid, a 3.5 m receiving door, parapeted membrane roof."""
    width, depth, height = 32.0, 16.0, 2 * STOREY
    front = [(2.2, .3, 3.5, 3.0, 'roller'), (9.5, .3, 1.0, 2.3, 'door'), (11.0, 1.0, 4.0, 1.4, 'window')]
    front += [(bay * 8 + 1.5, 1.2, 5.0, 1.2, 'window') for bay in (2, 3)]
    front += [(bay * 8 + 1.5, STOREY + 1.1, 5.0, 1.2, 'window') for bay in range(4)]
    back = [(bay * 8 + 1.5, v, 5.0, 1.2, 'window') for bay in range(4) for v in (1.2, STOREY + 1.1)]
    end = [(6.0, .3, 1.0, 2.3, 'door'), (9.5, STOREY + 1.1, 3.0, 1.2, 'window')]
    faces = walls(width, depth, height, front, back, end, end)
    plinth(width, depth)
    flat_roof(width, depth, height, parapet=.8)
    for bay in range(4):
        x = -width / 2 + bay * 8 + 4
        for y in (-3.5, 3.5):
            box('rooflight curb', (x - 1.3, y - 1.6, height), (x + 1.3, y + 1.6, height + .3), 'metal')
            box('rooflight', (x - 1.2, y - 1.5, height + .3), (x + 1.2, y + 1.5, height + .4), 'glass')
        box('roof vent', (x - .25, -.25, height), (x + .25, .25, height + .8), 'metal')
    for name in ('front', 'back'):
        for u in range(0, 33, 8):
            faces[name].slab('panel joint', max(u - .06, 0), min(u + .06, width), .3, height, -.06, 0, 'wall-end')
        faces[name].slab('floor band', 0, width, STOREY - .1, STOREY + .1, -.06, 0, 'wall-end')


def g003():
    """Exact game body G003: 24 x 16 m, three storeys, 10.5 m walls. The simulation's ceiling is two
    tenancies, so the body reads as two halves split at mid-frontage, each with its own street door,
    stair and garden door."""
    width, depth, height = 24.0, 16.0, 3 * STOREY
    front, back = [], []
    for half, stair in ((0.0, 1.2), (12.0, 8.8)):
        front += [(half + stair, .3, 2.0, 2.5, 'door'), (half + stair, STOREY + 1.2, 2.0, 2.6, 'stair'),
                  (half + stair, 2 * STOREY + 1.2, 2.0, 2.0, 'stair')]
        rooms = [half + c for c in ((5.0, 8.0, 11.0) if stair < 5 else (1.0, 4.0, 7.0))]
        front += bays(rooms, 1.0, 1.6, 1.7) + upper(3, rooms, w=1.6)
        back += [(half + 4.8, .3, 2.4, 2.4, 'door')]
        back += bays([half + 2.0, half + 9.5], 1.0, 1.6, 1.7) + upper(3, [half + 2.0, half + 6.0, half + 9.5], w=1.6)
    end = [(x, s * STOREY + .95, 1.4, 1.7, 'window') for s in range(3) for x in (4.0, 10.6)]
    faces = walls(width, depth, height, front, back, end, end, 'wall-end')
    plinth(width, depth)
    flat_roof(width, depth, height)
    for name in ('front', 'back'):
        faces[name].slab('party line', 11.85, 12.15, .3, height, -.12, 0, 'wall-end')
    box('party upstand', (-.15, -depth / 2 + .25, height), (.15, depth / 2 - .25, height + .7), 'wall-end')
    for sign in (-1, 1):
        x = sign * 9.8
        box('entrance canopy', (x - 1.4, -depth / 2 - 1.1, 2.9), (x + 1.4, -depth / 2, 3.05), 'trim')
        box('roof hatch', (x - 1.0, -5.5, height), (x + 1.0, -4.3, height + .5), 'metal')
        box('rooftop vent', (sign * 5.0 - .4, 3.0, height), (sign * 5.0 + .4, 3.8, height + .9), 'metal')


BODIES = [('h1-attached-range', h1), ('a1-apartment', a1), ('a2-stair-range', a2), ('m1-corner', m1), ('w1-workplace', w1), ('w2-workshop', w2), ('g003-two-tenancy', g003),
          ('h1-reroofed', lambda: h1(reroofed=True)), ('m1-repaired', lambda: m1(repaired=True)), ('w1-solar', lambda: w1(solar=True))]


def export(name, build):
    reset(FINISHES[name])
    build()
    degenerate = [part.name for part in parts if any(p.area <= 1e-8 for p in part.data.polygons)]
    assert not degenerate, (name, degenerate)
    bpy.ops.object.select_all(action='DESELECT')
    for part in parts:
        part.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    body = bpy.context.object
    body.name = name
    used = []
    for slot in body.data.materials:
        if slot not in used:
            used.append(slot)
    indices = [used.index(body.data.materials[p.material_index]) for p in body.data.polygons]
    body.data.materials.clear()
    for material in used:
        body.data.materials.append(material)
    for polygon, index in zip(body.data.polygons, indices):
        polygon.material_index = index
    assert all(p.area > 1e-8 for p in body.data.polygons), name
    project_uvs(body)
    low = [min(v.co[i] for v in body.data.vertices) for i in range(3)]
    high = [max(v.co[i] for v in body.data.vertices) for i in range(3)]
    assert abs(low[2]) < .025, (name, low)
    geometry = hashlib.sha256(repr(([tuple(v.co) for v in body.data.vertices],
                                    [tuple(p.vertices) for p in body.data.polygons])).encode()).hexdigest()
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCES / f'{name}.blend'))
    bpy.ops.export_scene.gltf(filepath=str(EXPORT / f'{name}.glb'), export_format='GLB', use_selection=True,
                              export_apply=True, export_yup=True, export_tangents=True,
                              export_image_format='JPEG', export_jpeg_quality=88)
    return {'body': name, 'vertices': len(body.data.vertices), 'faces': len(body.data.polygons),
            'bounds_min': [round(v, 3) for v in low], 'bounds_max': [round(v, 3) for v in high],
            'materials': [m.name for m in used], 'geometry_sha256': geometry}


SOURCES.mkdir(parents=True, exist_ok=True)
EXPORT.mkdir(parents=True, exist_ok=True)
records = [export(name, build) for name, build in BODIES]
(SOURCES / 'bodies.json').write_text(json.dumps({'storey_metres': STOREY, 'bodies': records}, indent=2) + '\n')
for record in records:
    print('TEST_STREET_BODY', record['body'], record['vertices'], record['bounds_min'], record['bounds_max'])

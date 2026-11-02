"""Street furniture for shop forecourts: café sets, parasols, planters, benches, bollards, bike stands,
bicycles, A-boards, tree grates, café screens and potted topiaries.

Run with Blender in the background:
  "$BLENDER_BIN" --background --factory-startup --python-exit-code 1 --python scripts/art/forecourt.py -- export
  "$BLENDER_BIN" --background --factory-startup --python-exit-code 1 --python scripts/art/forecourt.py -- render OUTDIR

The shell paves each shop's forecourt and places these modules whole on it. Each module's origin is
its footprint centre at ground level, and its street side looks down -Y, which Godot receives as +Z.
Materials are flat colours, which the shell reads as vertex colours. The materials in PAINTED export
white, so the shell's instance colour becomes their paint; it multiplies every other material too, so
the shell keeps instance colours near white on modules whose fixed colours matter.
"""
import bpy
import bmesh
import hashlib
import json
import math
import sys
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
SOURCES = ROOT / 'art/forecourt'
EXPORT = ROOT / 'src/Borough.Godot/assets/forecourt'

COLOURS = {
    'iron': '23272a', 'steel': '9a9ea1', 'marble': 'e6e2da', 'rattan': 'b08a5c', 'timber': '9a7450',
    'timber-dark': '5e4632', 'soil': '4a3628', 'leaf': '4f7a3a', 'leaf-dark': '35512b', 'leaf-light': '74a04a',
    'chalk': '2a2f2f', 'chalk-text': 'd8d4c8', 'rubber': '1b1b1c', 'reflect': 'dfe2e4', 'pot': '3b3f42',
    'canvas': 'ffffff', 'planter': 'ffffff', 'bloom': 'ffffff', 'paint': 'ffffff',
}
PAINTED = ('canvas', 'planter', 'bloom', 'paint')

parts = []
materials = {}


def linear(channel):
    return channel / 12.92 if channel <= .04045 else ((channel + .055) / 1.055) ** 2.4


def rgb(code):
    return tuple(linear(int(code[i:i + 2], 16) / 255) for i in (0, 2, 4))


def reset():
    for thing in list(bpy.data.objects):
        bpy.data.objects.remove(thing)
    for data in list(bpy.data.meshes):
        bpy.data.meshes.remove(data)
    for material in list(bpy.data.materials):
        bpy.data.materials.remove(material)
    parts.clear()
    materials.clear()
    for name, code in COLOURS.items():
        material = bpy.data.materials.new(name)
        colour = rgb(code) + (1,)
        material.use_nodes = True
        shader = material.node_tree.nodes['Principled BSDF']
        shader.inputs['Base Color'].default_value = colour
        shader.inputs['Roughness'].default_value = .3 if name in ('marble', 'reflect', 'steel') else .85
        shader.inputs['Metallic'].default_value = .5 if name in ('steel', 'iron') else 0
        material.diffuse_color = colour
        materials[name] = material


def finish(name, data, material):
    data.materials.append(materials[material])
    thing = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(thing)
    parts.append(thing)


def solid(name, vertices, faces, material):
    """A closed part, its faces turned outward whatever order they were listed in."""
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    made = bmesh.new()
    made.from_mesh(data)
    bmesh.ops.recalc_face_normals(made, faces=made.faces)
    made.to_mesh(data)
    made.free()
    finish(name, data, material)


BOX_FACES = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]


def box(name, low, high, material):
    (x0, y0, z0), (x1, y1, z1) = low, high
    solid(name, [(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0),
                 (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)], BOX_FACES, material)


def lathe(name, profile, sides, material, centre=(0, 0)):
    """A solid of revolution about an upright axis, from (radius, height) pairs listed bottom to top."""
    cx, cy = centre
    vertices, faces = [], []
    for r, z in profile:
        for k in range(sides):
            a = 2 * math.pi * k / sides
            vertices.append((cx + r * math.cos(a), cy + r * math.sin(a), z))
    for i in range(len(profile) - 1):
        for k in range(sides):
            a, b = i * sides + k, i * sides + (k + 1) % sides
            faces.append((a, b, b + sides, a + sides))
    faces.append(tuple(reversed(range(sides))))
    last = (len(profile) - 1) * sides
    faces.append(tuple(range(last, last + sides)))
    solid(name, vertices, faces, material)


def frame_of(direction):
    d = direction.normalized()
    helper = Vector((0, 0, 1)) if abs(d.z) < .9 else Vector((1, 0, 0))
    u = d.cross(helper).normalized()
    return u, d.cross(u).normalized()


def tube(name, a, b, radius, material, sides=6):
    a, b = Vector(a), Vector(b)
    u, v = frame_of(b - a)
    ring = [u * math.cos(2 * math.pi * k / sides) * radius + v * math.sin(2 * math.pi * k / sides) * radius
            for k in range(sides)]
    vertices = [tuple(a + p) for p in ring] + [tuple(b + p) for p in ring]
    faces = [tuple(range(sides)), tuple(range(sides, 2 * sides))]
    faces += [(k, (k + 1) % sides, sides + (k + 1) % sides, sides + k) for k in range(sides)]
    solid(name, vertices, faces, material)


def path(name, points, radius, material, sides=6):
    for k, (a, b) in enumerate(zip(points, points[1:])):
        tube(f'{name}-{k}', a, b, radius, material, sides)


def blob(name, centre, scale, material, subdivisions=1):
    made = bmesh.new()
    bmesh.ops.create_icosphere(made, subdivisions=subdivisions, radius=1)
    for vertex in made.verts:
        vertex.co = Vector((vertex.co.x * scale[0] + centre[0], vertex.co.y * scale[1] + centre[1],
                            vertex.co.z * scale[2] + centre[2]))
    data = bpy.data.meshes.new(name)
    made.to_mesh(data)
    made.free()
    finish(name, data, material)


def torus_y(name, centre, major, minor, material, segments=16, sides=5):
    """A ring standing in the XZ plane, for wheels."""
    cx, cy, cz = centre
    vertices, faces = [], []
    for i in range(segments):
        a = 2 * math.pi * i / segments
        for k in range(sides):
            b = 2 * math.pi * k / sides
            r = major + minor * math.cos(b)
            vertices.append((cx + r * math.cos(a), cy + minor * math.sin(b), cz + r * math.sin(a)))
    for i in range(segments):
        for k in range(sides):
            p, q = i * sides + k, ((i + 1) % segments) * sides + k
            faces.append((p, q, ((i + 1) % segments) * sides + (k + 1) % sides, i * sides + (k + 1) % sides))
    solid(name, vertices, faces, material)


def slab_x(name, profile, x0, x1, material):
    """A prism along X from a convex (y, z) outline."""
    n = len(profile)
    vertices = [(x0, y, z) for y, z in profile] + [(x1, y, z) for y, z in profile]
    faces = [tuple(range(n)), tuple(range(n, 2 * n))]
    faces += [(k, (k + 1) % n, n + (k + 1) % n, n + k) for k in range(n)]
    solid(name, vertices, faces, material)


# ---------------------------------------------------------------------------------------------
# Modules


def chair(name, cx, facing):
    """A bistro chair whose seat centre is at cx and whose back is away from the table."""
    prism_sides = 12
    lathe(f'{name}-seat', [(.2, .44), (.2, .47)], prism_sides, 'rattan', centre=(cx, 0))
    for k in range(4):
        a = math.pi / 4 + k * math.pi / 2
        top = (cx + .14 * math.cos(a), .14 * math.sin(a), .44)
        foot = (cx + .19 * math.cos(a), .19 * math.sin(a), 0)
        tube(f'{name}-leg-{k}', foot, top, .011, 'iron', 5)
    back = -facing
    hoop = [(cx + back * .19 * math.cos(t), .19 * math.sin(t), 0) for t in
            [math.radians(d) for d in (-60, -30, 0, 30, 60)]]
    for k, (x, y, _) in enumerate(hoop):
        if k in (0, 4):
            tube(f'{name}-upright-{k}', (x, y, .47), (x, y, .86), .011, 'iron', 5)
    path(f'{name}-rail', [(x, y, .86) for x, y, _ in hoop], .011, 'iron', 5)
    for k, ((xa, ya, _), (xb, yb, _)) in enumerate(zip(hoop, hoop[1:])):
        out = Vector((xb - xa, yb - ya, 0)).normalized().cross(Vector((0, 0, 1))) * .012
        solid(f'{name}-weave-{k}', [(xa - out.x, ya - out.y, .62), (xb - out.x, yb - out.y, .62),
                                    (xb + out.x, yb + out.y, .62), (xa + out.x, ya + out.y, .62),
                                    (xa - out.x, ya - out.y, .8), (xb - out.x, yb - out.y, .8),
                                    (xb + out.x, yb + out.y, .8), (xa + out.x, ya + out.y, .8)],
              BOX_FACES, 'rattan')


def cafe_table():
    """A round marble bistro table, 0.7 m across, with a chair either side along X."""
    lathe('base', [(.22, 0), (.22, .02), (.08, .05), (.03, .07)], 16, 'iron')
    tube('column', (0, 0, .06), (0, 0, .72), .028, 'iron', 8)
    lathe('top', [(.35, .72), (.35, .75)], 20, 'marble')
    lathe('rim', [(.355, .715), (.355, .725)], 20, 'iron')
    for side in (-1, 1):
        chair(f'chair-{side}', side * .6, -side)


def umbrella():
    """A square market parasol, 2.4 m across, with a vented peak, a valance and ribs on a weighted base."""
    box('base', (-.25, -.25, 0), (.25, .25, .08), 'iron')
    lathe('collar', [(.06, .08), (.04, .18)], 8, 'iron')
    tube('pole', (0, 0, .08), (0, 0, 2.66), .024, 'iron', 8)
    half, eave, mid, peak = 1.2, 2.2, 2.47, 2.6
    corners = [(-half, -half), (half, -half), (half, half), (-half, half)]
    upper = [(x, y, eave) for x, y in corners] + [(x * .5, y * .5, mid) for x, y in corners] + [(0, 0, peak)]
    lower = [(x, y, z - .03) for x, y, z in upper]
    vertices = upper + lower
    faces = []
    for k in range(4):
        n = (k + 1) % 4
        faces += [(k, n, 4 + n, 4 + k), (4 + k, 4 + n, 8)]
        faces += [(9 + k, 9 + 4 + k, 9 + 4 + n, 9 + n), (9 + 4 + k, 9 + 8, 9 + 4 + n)]
        faces += [(k, 9 + k, 9 + n, n)]
    solid('canopy', vertices, faces, 'canvas')
    vent = [(x * .16, y * .16, peak + .02) for x, y in corners] + [(x * .2, y * .2, peak - .02) for x, y in corners]
    vent += [(0, 0, peak + .1)]
    solid('vent', vent, [(4, 5, 6, 7), (4, 5, 1, 0), (5, 6, 2, 1), (6, 7, 3, 2), (7, 4, 0, 3),
                         (0, 1, 8), (1, 2, 8), (2, 3, 8), (3, 0, 8)], 'canvas')
    for k, ((xa, ya), (xb, yb)) in enumerate(zip(corners, corners[1:] + corners[:1])):
        dx, dy = (yb - ya) * .004, (xa - xb) * .004
        solid(f'valance-{k}', [(xa, ya, eave - .2), (xb, yb, eave - .2), (xb + dx, yb + dy, eave - .2),
                               (xa + dx, ya + dy, eave - .2), (xa, ya, eave - .02), (xb, yb, eave - .02),
                               (xb + dx, yb + dy, eave - .02), (xa + dx, ya + dy, eave - .02)], BOX_FACES, 'canvas')
    for k, (x, y) in enumerate(corners):
        tube(f'rib-{k}', (0, 0, 1.78), (x * .96, y * .96, eave - .04), .011, 'iron', 4)


def planter():
    """A trough planter, 1.2 m by 0.5 m, on a recessed plinth under a soil top."""
    box('plinth', (-.56, -.21, 0), (.56, .21, .05), 'iron')
    box('body', (-.6, -.25, .05), (.6, .25, .5), 'planter')
    box('rim', (-.63, -.28, .5), (.63, .28, .55), 'planter')
    box('soil', (-.58, -.23, .55), (.58, .23, .57), 'soil')


CLUMPS = [(-.46, .06, .17, 'leaf-dark'), (-.3, -.07, .2, 'leaf'), (-.1, .07, .22, 'leaf-light'),
          (.08, -.06, .19, 'leaf-dark'), (.26, .06, .21, 'leaf'), (.44, -.05, .18, 'leaf-light'),
          (-.2, .1, .14, 'leaf-dark'), (.36, .11, .13, 'leaf-dark')]


def planter_green():
    """The planting in a trough: overlapping shrub clumps in three greens."""
    for k, (x, y, r, tone) in enumerate(CLUMPS):
        blob(f'clump-{k}', (x, y, .58 + r * .6), (r * 1.1, min(.16, r), r * .9), tone, 2)


def planter_bloom():
    """Flower heads over a trough's planting."""
    heads = [(-.5, -.08, .9), (-.36, .1, .86), (-.24, -.14, .97), (-.08, .12, .92), (.06, -.1, .93),
             (.2, .1, .98), (.32, -.12, 1.0), (.44, .08, .9), (.54, -.06, .84)]
    for k, (x, y, z) in enumerate(heads):
        blob(f'head-{k}', (x, y, z), (.065, .065, .045), 'bloom')


def bench():
    """A slatted street bench, 1.8 m, on cast iron ends, its seat facing the street."""
    for k in range(5):
        y = -.22 + k * .095
        box(f'seat-{k}', (-.9, y, .43), (.9, y + .075, .465), 'timber')
    for k, t in enumerate((.2, .5, .8)):
        y, z = .2 + t * .1, .5 + t * .38
        slab_x(f'back-{k}', [(y - .015, z - .04), (y + .01, z - .045), (y + .025, z + .04), (y, z + .045)],
               -.9, .9, 'timber')
    for x in (-.78, .78):
        tube(f'leg-front-{x}', (x, -.23, 0), (x, -.2, .43), .022, 'iron')
        tube(f'leg-back-{x}', (x, .22, 0), (x, .19, .43), .022, 'iron')
        tube(f'upright-{x}', (x, .19, .43), (x, .3, .92), .022, 'iron')
        tube(f'rail-{x}', (x, -.2, .43), (x, .19, .43), .022, 'iron')
        tube(f'arm-post-{x}', (x, -.2, .43), (x, -.18, .64), .02, 'iron')
        tube(f'arm-{x}', (x, -.25, .65), (x, .22, .66), .028, 'iron')


def bollard():
    """A cast iron bollard, 0.9 m, with a domed head and a reflective band."""
    lathe('body', [(.11, 0), (.11, .03), (.085, .05), (.08, .8), (.075, .86), (.05, .9), (.02, .92), (.001, .925)],
          14, 'iron')
    lathe('band', [(.086, .7), (.086, .75)], 14, 'reflect')


STAND_EVERY = 1.8


def bike_rack():
    """Two Sheffield stands in line along the street, 1.8 m apart, so bicycles park parallel to the kerb."""
    for k, cx in enumerate((-STAND_EVERY / 2, STAND_EVERY / 2)):
        rise = [(cx - .35 + .08 - .08 * math.cos(a), 0, .67 + .08 * math.sin(a)) for a in
                [math.radians(d) for d in (0, 30, 60, 90)]]
        fall = [(cx + .35 - .08 + .08 * math.cos(a), 0, .67 + .08 * math.sin(a)) for a in
                [math.radians(d) for d in (90, 60, 30, 0)]]
        path(f'stand-{k}', [(cx - .35, 0, 0)] + rise + fall + [(cx + .35, 0, 0)], .024, 'steel')


def bicycle():
    """A city bicycle standing along X, its painted frame on two wheels and a basket at the front."""
    def at(along, z, across=0):
        return (along, across, z)
    rear, front = at(.52, .34), at(-.52, .34)
    for name, hub in (('rear', rear), ('front', front)):
        torus_y(f'{name}-tyre', hub, .32, .022, 'rubber')
        torus_y(f'{name}-rim', hub, .29, .01, 'steel', sides=4)
        tube(f'{name}-hub', (hub[0], -.04, hub[2]), (hub[0], .04, hub[2]), .02, 'steel')
    crank, seat, head, low = at(.04, .3), at(.2, .8), at(-.36, .82), at(-.4, .66)
    for a, b in ((crank, seat), (seat, head), (crank, low), (crank, rear), (seat, rear), (low, front), (low, head)):
        tube('frame', a, b, .017, 'paint')
    tube('stem', head, at(-.33, .98), .014, 'steel')
    tube('bar', at(-.31, .98, -.27), at(-.31, .98, .27), .012, 'steel')
    for side in (-1, 1):
        tube(f'grip-{side}', at(-.31, .98, side * .2), at(-.3, .98, side * .28), .018, 'rubber')
    tube('post', seat, at(.23, .88), .012, 'steel')
    box('saddle', (.14, -.07, .88), (.34, .07, .93), 'rubber')
    box('crank-arm', (0, -.06, .28), (.08, .06, .32), 'steel')
    box('basket', (-.58, -.12, .8), (-.38, .12, .95), 'rattan')
    tube('basket-strut', at(-.48, .8), at(-.5, .4), .008, 'steel', 4)
    box('rack', (.3, -.06, .66), (.74, .06, .68), 'steel')
    tube('rack-strut', at(.72, .66), rear, .008, 'steel', 4)


def a_board():
    """A sandwich board, 0.6 m wide and 0.9 m tall, its chalk face to the street."""
    for side in (-1, 1):
        foot, top = side * .3, side * .025
        slab_x(f'panel-{side}', [(foot, 0), (foot + side * .025, 0), (top + side * .025, .92), (top, .92)],
               -.3, .3, 'paint')
        f0, t0 = foot + side * .025, top + side * .025
        def at(t):
            return (f0 + (t0 - f0) * t, .92 * t)
        (y0, z0), (y1, z1) = at(.1), at(.82)
        slab_x(f'chalk-{side}', [(y0, z0), (y0 + side * .006, z0), (y1 + side * .006, z1), (y1, z1)],
               -.25, .25, 'chalk')
        for k, (t, w) in enumerate(((.68, .17), (.52, .2), (.4, .14), (.28, .18))):
            (ya, za), (yb, zb) = at(t), at(t + .035)
            slab_x(f'text-{side}-{k}', [(ya + side * .006, za), (ya + side * .009, za), (yb + side * .009, zb),
                                        (yb + side * .006, zb)], -w, w, 'chalk-text')
    tube('hinge', (-.3, 0, .9), (.3, 0, .9), .015, 'iron')


def tree_grate():
    """A cast iron tree grate, 1.2 m square, under a four-post guard."""
    for k, (low, high) in enumerate((((-.6, -.6, 0), (.6, -.52, .04)), ((-.6, .52, 0), (.6, .6, .04)),
                                     ((-.6, -.52, 0), (-.52, .52, .04)), ((.52, -.52, 0), (.6, .52, .04)))):
        box(f'frame-{k}', low, high, 'iron')
    for k, r in enumerate((.24, .34, .44)):
        vertices, faces, n = [], [], 20
        for i in range(n):
            a = 2 * math.pi * i / n
            for rr, z in ((r - .025, 0), (r + .025, 0), (r + .025, .035), (r - .025, .035)):
                vertices.append((rr * math.cos(a), rr * math.sin(a), z))
        for i in range(n):
            j = (i + 1) % n
            for q in range(4):
                faces.append((i * 4 + q, i * 4 + (q + 1) % 4, j * 4 + (q + 1) % 4, j * 4 + q))
        solid(f'ring-{k}', vertices, faces, 'iron')
    for k in range(8):
        a = math.pi * k / 4
        tube(f'spoke-{k}', (.2 * math.cos(a), .2 * math.sin(a), .018), (.55 * math.cos(a), .55 * math.sin(a), .018),
             .014, 'iron', 4)
    lathe('soil', [(.2, 0), (.2, .02)], 12, 'soil')
    posts = [(-.32, -.32), (.32, -.32), (.32, .32), (-.32, .32)]
    for k, (x, y) in enumerate(posts):
        tube(f'post-{k}', (x, y, 0), (x, y, 1.5), .018, 'iron')
    for z in (.85, 1.45):
        path(f'hoop-{z}', [(x, y, z) for x, y in posts + posts[:1]], .014, 'iron', 5)


def cafe_screen():
    """A café windbreak, 1.8 m along X: a canvas panel between steel posts on splayed feet."""
    for x in (-.88, .88):
        tube(f'post-{x}', (x, 0, 0), (x, 0, 1.0), .02, 'steel')
        tube(f'foot-{x}', (x, -.2, .01), (x, .2, .01), .015, 'steel', 4)
    box('panel', (-.86, -.012, .16), (.86, .012, .96), 'canvas')
    tube('rail', (-.88, 0, 1.0), (.88, 0, 1.0), .018, 'steel')


def topiary():
    """A clipped bay ball on a stem in a tapered pot, for a shop door."""
    lathe('pot', [(.19, 0), (.25, .46), (.28, .48), (.28, .52), (.001, .52)], 16, 'pot')
    tube('stem', (0, 0, .5), (0, 0, 1.0), .025, 'timber-dark')
    blob('ball', (0, 0, 1.2), (.32, .32, .3), 'leaf-dark', 2)


MODULES = {
    'cafe-table': cafe_table,
    'umbrella': umbrella,
    'planter': planter,
    'planter-green': planter_green,
    'planter-bloom': planter_bloom,
    'bench': bench,
    'bollard': bollard,
    'bike-rack': bike_rack,
    'bicycle': bicycle,
    'a-board': a_board,
    'tree-grate': tree_grate,
    'cafe-screen': cafe_screen,
    'topiary': topiary,
}


def joined(name):
    bpy.ops.object.select_all(action='DESELECT')
    for part in parts:
        part.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    body = bpy.context.object
    body.name = name
    return body


def export(name, build):
    reset()
    build()
    assert all(p.area > 1e-8 for part in parts for p in part.data.polygons), name
    body = joined(name)
    low = [min(v.co[i] for v in body.data.vertices) for i in range(3)]
    high = [max(v.co[i] for v in body.data.vertices) for i in range(3)]
    geometry = hashlib.sha256(repr(([tuple(round(c, 5) for c in v.co) for v in body.data.vertices],
                                    [tuple(p.vertices) for p in body.data.polygons])).encode()).hexdigest()
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCES / f'{name}.blend'))
    bpy.ops.export_scene.gltf(filepath=str(EXPORT / f'{name}.glb'), export_format='GLB', use_selection=True,
                              export_apply=True, export_yup=True)
    return {'module': name, 'vertices': len(body.data.vertices), 'faces': len(body.data.polygons),
            'triangles': sum(len(p.vertices) - 2 for p in body.data.polygons),
            'bounds_min': [round(v, 3) for v in low], 'bounds_max': [round(v, 3) for v in high],
            'materials': sorted({m.name for m in body.data.materials}), 'geometry_sha256': geometry}


# ---------------------------------------------------------------------------------------------
# Review renders: one forecourt vignette, with a painted tone per module where the shell paints one.

VIGNETTE = [
    # module, x, y, turn in degrees, paint
    ('cafe-table', -3.2, -.9, 0, None), ('cafe-table', -1.2, -.9, 0, None),
    ('umbrella', -3.2, -.9, 0, '2f5d50'), ('umbrella', -1.2, -.9, 0, '2f5d50'),
    ('cafe-screen', -3.2, -2.0, 0, 'e8e2d0'), ('cafe-screen', -1.3, -2.0, 0, 'e8e2d0'),
    ('topiary', .2, .6, 0, None), ('topiary', 2.2, .6, 0, None),
    ('a-board', 1.2, -.4, 15, '2f5d50'),
    ('planter', 3.6, -1.8, 0, 'a65a3a'), ('planter-green', 3.6, -1.8, 0, None), ('planter-bloom', 3.6, -1.8, 0, 'c8506a'),
    ('bench', 5.4, -1.7, 0, None),
    ('tree-grate', 7.4, -1.6, 0, None),
    ('bike-rack', 10, -2.0, 0, None), ('bicycle', 9.1, -1.86, 0, '2d4a6b'), ('bicycle', 10.9, -2.14, 180, '9a3b32'),
    ('bollard', 11.6, -1.9, 0, None), ('bollard', 12.4, -1.9, 0, None),
]


def paint_copy(code):
    made = bpy.data.materials.new(f'paint-{code}')
    made.use_nodes = True
    made.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = rgb(code) + (1,)
    made.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = .8
    return made


def render(out):
    out.mkdir(parents=True, exist_ok=True)
    reset()
    built = {}
    for name, build in MODULES.items():
        parts.clear()
        build()
        body = joined(name)
        body.hide_render = True
        built[name] = body
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_EEVEE'
    scene.render.resolution_x, scene.render.resolution_y = 1800, 1000
    scene.view_settings.view_transform = 'AgX'
    world = bpy.data.worlds.new('sky')
    world.use_nodes = True
    world.node_tree.nodes['Background'].inputs['Color'].default_value = (.55, .66, .82, 1)
    world.node_tree.nodes['Background'].inputs['Strength'].default_value = .7
    scene.world = world
    sun = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN'))
    sun.data.energy = 4.0
    sun.data.angle = .06
    sun.rotation_euler = Vector((-.5, .7, -.75)).to_track_quat('-Z', 'Y').to_euler()
    scene.collection.objects.link(sun)
    parts.clear()
    box('paving', (-5, -2.6, -.1), (14, 1.2, 0), 'steel')
    box('kerb', (-5, -2.85, -.1), (14, -2.6, .02), 'marble')
    box('road', (-5, -9, -.2), (14, -2.85, -.08), 'chalk')
    box('shopfront', (-5, 1.2, 0), (14, 1.5, 4.2), 'rattan')
    box('window', (-4.4, 1.18, .5), (13.4, 1.2, 3.2), 'chalk')
    for name, x, y, turn, paint in VIGNETTE:
        copy = built[name].copy()
        copy.data = built[name].data.copy()
        copy.hide_render = False
        copy.location = (x, y, 0)
        copy.rotation_euler = (0, 0, math.radians(turn))
        if paint:
            tone = paint_copy(paint)
            for slot in copy.material_slots:
                if slot.material and slot.material.name in PAINTED:
                    slot.link = 'OBJECT'
                    slot.material = tone
        scene.collection.objects.link(copy)
    camera = bpy.data.objects.new('camera', bpy.data.cameras.new('camera'))
    scene.collection.objects.link(camera)
    scene.camera = camera
    camera.data.lens = 35
    for shot, eye, target in (('kit-oblique', (4, -10, 4.2), (4, -.4, .6)),
                              ('kit-cafe', (-1.2, -5.2, 1.6), (-1.8, -.6, .9)),
                              ('kit-edge', (9.5, -5, 1.7), (8.5, -1.2, .6))):
        camera.location = eye
        camera.rotation_euler = (Vector(target) - Vector(eye)).to_track_quat('-Z', 'Y').to_euler()
        scene.render.filepath = str(out / f'{shot}.png')
        bpy.ops.render.render(write_still=True)


if __name__ == '__main__':
    arguments = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else ['export']
    if arguments[0] == 'render':
        render(Path(arguments[1]))
    else:
        SOURCES.mkdir(parents=True, exist_ok=True)
        EXPORT.mkdir(parents=True, exist_ok=True)
        records = [export(name, build) for name, build in MODULES.items()]
        (SOURCES / 'modules.json').write_text(json.dumps({'painted': list(PAINTED), 'modules': records}, indent=2) + '\n')

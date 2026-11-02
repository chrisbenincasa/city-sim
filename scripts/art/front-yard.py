"""Front-garden details for dwellings: hedges, low walls, railings, picket fences, gate piers, gates,
gate paths, doorsteps and planted borders.

Run with Blender in the background:
  "$BLENDER_BIN" --background --factory-startup --python-exit-code 1 --python scripts/art/front-yard.py -- export
  "$BLENDER_BIN" --background --factory-startup --python-exit-code 1 --python scripts/art/front-yard.py -- render OUTDIR

The shell lays each dwelling's lawn and places these modules whole on it. Each module's origin is its
footprint centre at ground level, and its street side looks down -Y, which Godot receives as +Z.
Boundary and border modules are 2 m bays along X, and path modules are 1 m bays along Y. The shell
repeats bays and stretches each by at most a quarter so a run fits its length. Materials are flat
colours, which the shell reads as vertex colours. The materials in PAINTED export white, so the shell's
instance colour becomes their paint; it multiplies every other material too.

The bodies draw 3.5 m storeys, about 1.25 times a real terrace's, so modules in GROWN are scaled by GROW
after they are built. Bay modules grow in height and depth and keep their 2 m bay; gates and piers grow
on every axis. Paths and doorsteps keep their real size, because they sit on the ground and meet the
door. Each docstring gives the real size before growing.
"""
import hashlib
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import forecourt as kit  # noqa: E402
from forecourt import BOX_FACES, blob, box, lathe, rgb, solid, tube  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
SOURCES = ROOT / 'art/front-yard'
EXPORT = ROOT / 'src/Borough.Godot/assets/front-yard'

BAY = 2.0
PATH_BAY = 1.0
GROW = 1.2
GROWN = {
    'hedge': (1, GROW, GROW), 'wall': (1, GROW, GROW), 'railing': (1, GROW, GROW), 'picket': (1, GROW, GROW),
    'border': (1, GROW, GROW), 'pier': (GROW,) * 3, 'gate-iron': (GROW,) * 3, 'gate-timber': (GROW,) * 3,
}

COLOURS = {
    'hedge': '30492a', 'hedge-light': '45663a', 'leaf': '4f7a3a', 'leaf-dark': '35512b', 'leaf-light': '74a04a',
    'coping': 'cfc8b8', 'iron': '1e2123', 'mortar': '8a857c', 'edging': '7a4a36', 'soil': '4a3628',
    'pot': '3b3f42', 'timber-dark': '5e4632',
    'tile-red': 'a4513a', 'tile-cream': 'ddd2bc', 'tile-black': '2a2a2a', 'step': 'b9b3a6',
    'brick': 'ffffff', 'paint': 'ffffff', 'flag': 'ffffff', 'bloom': 'ffffff',
}
PAINTED = ('brick', 'paint', 'flag', 'bloom')


def reset():
    for thing in list(bpy.data.objects):
        bpy.data.objects.remove(thing)
    for data in list(bpy.data.meshes):
        bpy.data.meshes.remove(data)
    for material in list(bpy.data.materials):
        bpy.data.materials.remove(material)
    kit.parts.clear()
    kit.materials.clear()
    for name, code in COLOURS.items():
        material = bpy.data.materials.new(name)
        colour = rgb(code) + (1,)
        material.use_nodes = True
        shader = material.node_tree.nodes['Principled BSDF']
        shader.inputs['Base Color'].default_value = colour
        shader.inputs['Roughness'].default_value = .4 if name in ('iron', 'tile-black', 'tile-red', 'tile-cream') else .9
        shader.inputs['Metallic'].default_value = .4 if name == 'iron' else 0
        material.diffuse_color = colour
        kit.materials[name] = material


def tops(name, quads, z, material):
    """Flat upward faces at height z, one per (x0, y0, x1, y1) rectangle, for paving laid on a bed."""
    vertices, faces = [], []
    for x0, y0, x1, y1 in quads:
        n = len(vertices)
        vertices += [(x0, y0, z), (x1, y0, z), (x1, y1, z), (x0, y1, z)]
        faces.append((n, n + 1, n + 2, n + 3))
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    kit.finish(name, data, material)


# ---------------------------------------------------------------------------------------------
# Boundaries: 2 m bays along X, centred on the boundary line.


def hedge():
    """A clipped privet hedge, 1.1 m tall and 0.7 m through, its top and faces softened by leafy mounds."""
    box('core', (-BAY / 2, -.3, 0), (BAY / 2, .3, .98), 'hedge')
    for k, x in enumerate((-.75, -.25, .25, .75)):
        lean = .03 if k % 2 else -.02
        blob(f'top-{k}', (x, lean, .98), (.29, .33, .13), 'hedge-light', 2)
        for side in (-1, 1):
            blob(f'face-{k}-{side}', (x + .08 * side, side * .29, .55 + .1 * (k % 2)), (.27, .07, .36), 'hedge', 1)


def wall():
    """A low brick garden wall, 0.68 m to the top of a stone coping that overhangs both faces."""
    box('plinth', (-BAY / 2, -.17, 0), (BAY / 2, .17, .15), 'brick')
    box('body', (-BAY / 2, -.14, .15), (BAY / 2, .14, .6), 'brick')
    box('coping', (-BAY / 2, -.18, .6), (BAY / 2, .18, .66), 'coping')
    solid('weathering', [(-BAY / 2, -.18, .66), (BAY / 2, -.18, .66), (BAY / 2, .18, .66), (-BAY / 2, .18, .66),
                         (-BAY / 2, 0, .7), (BAY / 2, 0, .7)],
          [(0, 1, 2, 3), (0, 4, 5, 1), (3, 2, 5, 4), (0, 3, 4), (1, 5, 2)], 'coping')


def railing():
    """A dwarf brick wall under cast iron railings with spear heads, 1.15 m in all."""
    box('plinth', (-BAY / 2, -.17, 0), (BAY / 2, .17, .12), 'brick')
    box('body', (-BAY / 2, -.14, .12), (BAY / 2, .14, .42), 'brick')
    box('coping', (-BAY / 2, -.18, .42), (BAY / 2, .18, .5), 'coping')
    box('rail-low', (-BAY / 2, -.02, .56), (BAY / 2, .02, .6), 'iron')
    box('rail-high', (-BAY / 2, -.02, 1.0), (BAY / 2, .02, 1.04), 'iron')
    for k in range(16):
        x = -BAY / 2 + .0625 + k * .125
        tube(f'bar-{k}', (x, 0, .5), (x, 0, 1.08), .009, 'iron', 4)
        solid(f'spear-{k}', [(x - .025, -.012, 1.08), (x + .025, -.012, 1.08), (x + .025, .012, 1.08),
                             (x - .025, .012, 1.08), (x, 0, 1.16)],
              [(0, 3, 2, 1), (0, 1, 4), (1, 2, 4), (2, 3, 4), (3, 0, 4)], 'iron')


def picket():
    """A painted timber picket fence, 0.95 m, with pointed pickets on two back rails between posts."""
    for x in (-BAY / 2, BAY / 2):
        box(f'post-{x}', (x - .045, -.045, 0), (x + .045, .045, 1.0), 'paint')
        box(f'cap-{x}', (x - .06, -.06, 1.0), (x + .06, .06, 1.03), 'paint')
    box('rail-low', (-BAY / 2, .012, .2), (BAY / 2, .052, .28), 'paint')
    box('rail-high', (-BAY / 2, .012, .68), (BAY / 2, .052, .76), 'paint')
    for k in range(13):
        x = -BAY / 2 + .15 + k * .1417
        solid(f'picket-{k}', [(x - .035, -.01, .05), (x + .035, -.01, .05), (x + .035, .012, .05), (x - .035, .012, .05),
                              (x - .035, -.01, .86), (x + .035, -.01, .86), (x + .035, .012, .86), (x - .035, .012, .86),
                              (x, -.01, .95), (x, .012, .95)],
              [(0, 3, 2, 1), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7),
               (4, 5, 8), (6, 7, 9), (5, 6, 9, 8), (7, 4, 8, 9)], 'paint')


def pier():
    """A brick gate pier, 0.45 m square and 1.0 m tall, under a stone cap with a shallow pyramid top."""
    box('plinth', (-.25, -.25, 0), (.25, .25, .15), 'brick')
    box('shaft', (-.225, -.225, .15), (.225, .225, .96), 'brick')
    box('cap', (-.27, -.27, .96), (.27, .27, 1.04), 'coping')
    solid('pyramid', [(-.24, -.24, 1.04), (.24, -.24, 1.04), (.24, .24, 1.04), (-.24, .24, 1.04), (0, 0, 1.16)],
          [(0, 3, 2, 1), (0, 1, 4), (1, 2, 4), (2, 3, 4), (3, 0, 4)], 'coping')


# ---------------------------------------------------------------------------------------------
# Gates: one leaf, 1 m across X, closed.


def gate_iron():
    """A cast iron gate leaf, 1 m, whose bars rise to an arched top rail."""
    box('rail-low', (-.48, -.02, .1), (.48, .02, .15), 'paint')
    box('rail-mid', (-.48, -.02, .78), (.48, .02, .82), 'paint')
    for side in (-1, 1):
        box(f'stile-{side}', (side * .48 - .025, -.025, .02), (side * .48 + .025, .025, .98), 'paint')
    arch = [(-.48 + .96 * k / 8, .96 + .1 * math.sin(math.pi * k / 8)) for k in range(9)]
    kit.path('arch', [(x, 0, z) for x, z in arch], .016, 'paint', 4)
    for k in range(1, 8):
        x, top = arch[k]
        tube(f'bar-{k}', (x, 0, .12), (x, 0, top), .009, 'paint', 4)
        blob(f'knob-{k}', (x, 0, .45), (.022, .022, .03), 'paint', 1)


def gate_timber():
    """A painted picket gate leaf, 1 m, braced on a Z of rails behind its pickets."""
    box('rail-low', (-.47, .012, .2), (.47, .052, .29), 'paint')
    box('rail-high', (-.47, .012, .66), (.47, .052, .75), 'paint')
    a, b = (-.42, .032, .26), (.42, .032, .7)
    tube('brace', a, b, .035, 'paint', 4)
    for k in range(7):
        x = -.42 + k * .14
        solid(f'picket-{k}', [(x - .035, -.01, .06), (x + .035, -.01, .06), (x + .035, .012, .06), (x - .035, .012, .06),
                              (x - .035, -.01, .82), (x + .035, -.01, .82), (x + .035, .012, .82), (x - .035, .012, .82),
                              (x, -.01, .9), (x, .012, .9)],
              [(0, 3, 2, 1), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7),
               (4, 5, 8), (6, 7, 9), (5, 6, 9, 8), (7, 4, 8, 9)], 'paint')


# ---------------------------------------------------------------------------------------------
# Paths: 1 m bays along Y, 1.1 m wide, laid on a mortar bed whose top is 0.1 m.


def path_flags():
    """Square stone flags, two across and two along, between brick-on-edge kerbs."""
    box('bed', (-.5, -PATH_BAY / 2, 0), (.5, PATH_BAY / 2, .09), 'mortar')
    flags = [(x0 + .015, y0 + .015, x0 + .485, y0 + .485) for x0 in (-.5, 0) for y0 in (-.5, 0)]
    tops('flags', flags, .1, 'flag')
    for side in (-1, 1):
        x0, x1 = (.5, .56) if side > 0 else (-.56, -.5)
        box(f'kerb-{side}', (x0, -PATH_BAY / 2, 0), (x1, PATH_BAY / 2, .13), 'edging')


def path_tiled():
    """Victorian geometric tiles: a red and cream chequer in a black border, between rope-top kerbs."""
    box('bed', (-.5, -PATH_BAY / 2, 0), (.5, PATH_BAY / 2, .09), 'mortar')
    n, w = 6, .8 / 6
    red = [(-.4 + i * w, -.5 + j * (1 / n), -.4 + (i + 1) * w, -.5 + (j + 1) * (1 / n))
           for i in range(n) for j in range(n) if (i + j) % 2 == 0]
    cream = [(-.4 + i * w, -.5 + j * (1 / n), -.4 + (i + 1) * w, -.5 + (j + 1) * (1 / n))
             for i in range(n) for j in range(n) if (i + j) % 2 == 1]
    tops('red', red, .1, 'tile-red')
    tops('cream', cream, .1, 'tile-cream')
    tops('border', [(-.5, -.5, -.4, .5), (.4, -.5, .5, .5)], .1, 'tile-black')
    for side in (-1, 1):
        x0, x1 = (.5, .56) if side > 0 else (-.56, -.5)
        box(f'kerb-{side}', (x0, -PATH_BAY / 2, 0), (x1, PATH_BAY / 2, .12), 'edging')
        for k in range(5):
            y = -.4 + k * .2
            blob(f'rope-{side}-{k}', ((x0 + x1) / 2, y, .125), (.032, .1, .02), 'edging', 1)


def doorstep():
    """Two stone steps up to a 0.3 m sill, 1.4 m wide; the upper tread meets the wall on +Y."""
    box('lower', (-.7, -.3, 0), (.7, .3, .15), 'step')
    box('upper', (-.6, 0, .15), (.6, .3, .3), 'step')


def border():
    """A planted border, 2 m by 0.6 m: low shrub mounds on soil with flower heads among them."""
    box('soil', (-BAY / 2, -.3, 0), (BAY / 2, .3, .1), 'soil')
    mounds = [(-.8, .05, .2, 'leaf-dark'), (-.45, -.08, .17, 'leaf'), (-.12, .07, .21, 'leaf-light'),
              (.2, -.05, .18, 'leaf-dark'), (.52, .06, .2, 'leaf'), (.82, -.04, .16, 'leaf-light')]
    for k, (x, y, r, tone) in enumerate(mounds):
        blob(f'mound-{k}', (x, y, .1 + r * .45), (r * 1.15, min(.2, r), r * .8), tone, 2)
    heads = [(-.66, -.12, .36), (-.3, .1, .33), (0, -.14, .38), (.34, .12, .34), (.66, -.1, .35), (.92, .1, .3)]
    for k, (x, y, z) in enumerate(heads):
        blob(f'head-{k}', (x, y, z), (.06, .06, .04), 'bloom')


MODULES = {
    'hedge': hedge,
    'wall': wall,
    'railing': railing,
    'picket': picket,
    'pier': pier,
    'gate-iron': gate_iron,
    'gate-timber': gate_timber,
    'path-flags': path_flags,
    'path-tiled': path_tiled,
    'doorstep': doorstep,
    'border': border,
}


def grown(name):
    body = kit.joined(name)
    sx, sy, sz = GROWN.get(name, (1, 1, 1))
    for v in body.data.vertices:
        v.co = (v.co.x * sx, v.co.y * sy, v.co.z * sz)
    return body


def export(name, build):
    reset()
    build()
    assert all(p.area > 1e-8 for part in kit.parts for p in part.data.polygons), name
    body = grown(name)
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
# Review renders: four front gardens before one house front, a door in each, with the shell's paints.

FRONT = 4.0
STREET = -4.6

# Each garden: west and east x, door x, boundary, gate, path, and the paints for brick, gate and boundary.
GARDENS = [
    (-13, -7, -10, 'railing', 'gate-iron', 'path-tiled', {'brick': '9a4f38', 'gate-iron': '1e2123'}),
    (-7, -1, -4, 'railing', 'gate-iron', 'path-tiled', {'brick': '9a4f38', 'gate-iron': '1e2123'}),
    (-1, 7, 3, 'hedge', 'gate-timber', 'path-flags', {'gate-timber': 'e8e4da', 'flag': 'b8b2a6'}),
    (7, 15, 11, 'wall', 'gate-iron', 'path-flags', {'brick': 'c9a46a', 'gate-iron': '2f4a3c', 'flag': 'a8a196'}),
    (15, 23, 19, 'picket', 'gate-timber', 'path-flags', {'picket': 'e8e4da', 'gate-timber': 'e8e4da', 'flag': 'c4bdb0'}),
]


def paint_copy(code):
    made = bpy.data.materials.new(f'paint-{code}')
    made.use_nodes = True
    made.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = rgb(code) + (1,)
    made.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = .85
    return made


def place(built, scene, name, x, y, turn=0, paint=None, stretch=(1, 1, 1)):
    copy = built[name].copy()
    copy.data = built[name].data.copy()
    copy.hide_render = False
    copy.location = (x, y, 0)
    copy.rotation_euler = (0, 0, math.radians(turn))
    copy.scale = stretch
    if paint:
        tone = paint_copy(paint)
        for slot in copy.material_slots:
            if slot.material and slot.material.name in PAINTED:
                slot.link = 'OBJECT'
                slot.material = tone
    scene.collection.objects.link(copy)


def run(built, scene, name, x0, x1, y, paint=None, turn=0):
    """Bays along a boundary from x0 to x1, stretched to fit as the shell stretches them."""
    length = x1 - x0
    if length < .3:
        return
    bays = max(1, round(length / BAY))
    for k in range(bays):
        place(built, scene, name, x0 + (k + .5) * length / bays, y, turn, paint, (length / bays / BAY, 1, 1))


def render(out):
    out.mkdir(parents=True, exist_ok=True)
    reset()
    built = {}
    for name, build in MODULES.items():
        kit.parts.clear()
        build()
        body = grown(name)
        body.hide_render = True
        built[name] = body
    kit.parts.clear()
    kit.topiary()
    built['topiary'] = kit.joined('topiary')
    built['topiary'].hide_render = True
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
    kit.parts.clear()
    box('lawn', (-14, STREET, -.1), (24, FRONT, .06), 'leaf')
    box('footway', (-14, STREET - 2, -.1), (24, STREET, .08), 'step')
    box('road', (-14, -14, -.2), (24, STREET - 2, -.05), 'tile-black')
    box('front', (-14, FRONT, 0), (24, FRONT + .4, 6), 'edging')
    for west, east, door, *_ in GARDENS:
        box(f'door-{door}', (door - .5, FRONT - .02, .3), (door + .5, FRONT, 2.6), 'iron')
    for west, east, door, boundary, gate, path, paints in GARDENS:
        line = STREET + .35
        brick = paints.get('brick')
        bpaint = paints.get(boundary, brick if boundary in ('wall', 'railing') else None)
        piered = boundary in ('wall', 'railing')
        reach = 1.16 if piered else .66
        run(built, scene, boundary, west + (.3 if piered else 0), door - reach, line, bpaint)
        run(built, scene, boundary, door + reach, east - (.3 if piered else 0), line, bpaint)
        if piered:
            for x in (door - .88, door + .88):
                place(built, scene, 'pier', x, line, paint=brick)
        place(built, scene, gate, door, line, paint=paints.get(gate))
        start, end = line + .35, FRONT - .6
        bays = max(1, round(end - start))
        for k in range(bays):
            place(built, scene, path, door, start + (k + .5) * (end - start) / bays,
                  paint=paints.get('flag'), stretch=(1, (end - start) / bays, 1))
        place(built, scene, 'doorstep', door, FRONT - .3)
        if boundary == 'hedge':
            run(built, scene, 'border', west + .2, door - .8, FRONT - .6, paint='c8506a')
            for side in (-1, 1):
                place(built, scene, 'topiary', door + side * 1.0, FRONT - .45, stretch=(GROW,) * 3)
    divider, length = (STREET + .35 + .2, FRONT - .2), FRONT - .2 - (STREET + .55)
    bays = max(1, round(length / BAY))
    for k in range(bays):
        place(built, scene, 'railing', -7, divider[0] + (k + .5) * length / bays, 90, '9a4f38', (length / bays / BAY, 1, 1))
    camera = bpy.data.objects.new('camera', bpy.data.cameras.new('camera'))
    scene.collection.objects.link(camera)
    scene.camera = camera
    camera.data.lens = 35
    for shot, eye, target in (('yard-oblique', (5, -18, 7), (5, 0, .6)),
                              ('yard-terrace', (-4, -9, 2.2), (-7, 0, .7)),
                              ('yard-detached', (8, -8.5, 2), (8, 0, .6))):
        camera.location = eye
        camera.rotation_euler = (Vector(target) - Vector(eye)).to_track_quat('-Z', 'Y').to_euler()
        scene.render.filepath = str(out / f'{shot}.png')
        bpy.ops.render.render(write_still=True)


arguments = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else ['export']
if arguments[0] == 'render':
    render(Path(arguments[1]))
else:
    SOURCES.mkdir(parents=True, exist_ok=True)
    EXPORT.mkdir(parents=True, exist_ok=True)
    records = [export(name, build) for name, build in MODULES.items()]
    (SOURCES / 'modules.json').write_text(json.dumps({'painted': list(PAINTED), 'bay_metres': BAY,
                                                      'path_bay_metres': PATH_BAY, 'modules': records}, indent=2) + '\n')

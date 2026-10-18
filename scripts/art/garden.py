"""Garden furniture for block interiors: sheds, greenhouses, beds, seating, play equipment and boundaries.

Run with Blender in the background:
  "$BLENDER_BIN" --background --factory-startup --python-exit-code 1 --python scripts/art/garden.py

The shell lays lawns, paving, hedges and walls as tinted boxes, and places these modules whole on
them. Each module's origin is its footprint centre at ground level. Its house side looks down -Y,
which Godot receives as +Z, and the shell turns each module to face its garden's house. Fence and
railing panels run 2 m along X and are repeated, never stretched. Faces are wound counter-clockwise
seen from outside. Materials are flat colours, which the shell reads as vertex colours.
"""
import bpy
import hashlib
import json
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCES = ROOT / 'art/garden'
EXPORT = ROOT / 'src/Borough.Godot/assets/garden'

PANEL = 2.0

COLOURS = {
    'boards': '8a6a48', 'post': '6b4f36', 'felt': '3a3836', 'trim': 'd8d2c4', 'door': '5d7a5a',
    'glass': '9fb8b4', 'frame': 'e8e8e2', 'plinth': '9b5a3c', 'soil': '5a4030', 'edging': '8d6e4c',
    'leaf': '4f7a3a', 'leaf-dark': '35512b', 'bloom': 'c7607a', 'bloom-white': 'ece6d6',
    'cream': 'd6cfbf', 'seat': '6b4f36', 'iron': '1f2224', 'red': 'c43a2f', 'blue': '2f6fb0',
    'yellow': 'e0b030', 'steel': '8d9194',
}
BOX_FACES = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]

parts = []
materials = {}


def linear(channel):
    return channel / 12.92 if channel <= .04045 else ((channel + .055) / 1.055) ** 2.4


def rgb(code):
    return tuple(linear(int(code[i:i + 2], 16) / 255) for i in (0, 2, 4))


def reset():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
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
        shader.inputs['Roughness'].default_value = .2 if name == 'glass' else .85
        material.diffuse_color = colour
        materials[name] = material


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


def ridge(name, x0, x1, y0, y1, eave, top, material):
    """A roof pitched along X: two slopes meeting over the middle of Y, with gable ends."""
    middle = (y0 + y1) / 2
    mesh(name, [(x0, y0, eave), (x1, y0, eave), (x1, middle, top), (x0, middle, top),
                (x0, y1, eave), (x1, y1, eave)],
         [(0, 1, 2, 3), (3, 2, 5, 4), (0, 3, 4), (1, 5, 2), (0, 4, 5, 1)], material)


def prism(name, cx, cy, radius, z0, z1, sides, material):
    """An upright polygonal prism, for planted beds and round tables."""
    ring = [(cx + radius * math.cos(2 * math.pi * k / sides), cy + radius * math.sin(2 * math.pi * k / sides))
            for k in range(sides)]
    vertices = [(x, y, z0) for x, y in ring] + [(x, y, z1) for x, y in ring]
    faces = [tuple(reversed(range(sides))), tuple(range(sides, 2 * sides))]
    faces += [(k, (k + 1) % sides, sides + (k + 1) % sides, sides + k) for k in range(sides)]
    mesh(name, vertices, faces, material)


def shed():
    """A timber shed, 2.4 m by 1.8 m, its door toward the house under a dark felt roof."""
    box('walls', (-1.2, -.9, 0), (1.2, .9, 1.9), 'boards')
    ridge('roof', -1.32, 1.32, -1.02, 1.02, 1.9, 2.45, 'felt')
    box('door', (-.4, -.94, 0), (.4, -.9, 1.75), 'door')
    box('window', (.55, -.94, 1.0), (1.0, -.9, 1.5), 'glass')
    box('fascia-front', (-1.32, -1.04, 1.84), (1.32, -1.0, 1.92), 'trim')
    box('fascia-back', (-1.32, 1.0, 1.84), (1.32, 1.04, 1.92), 'trim')


def greenhouse():
    """A glasshouse, 2.6 m by 2.0 m, on a brick plinth."""
    box('plinth', (-1.3, -1.0, 0), (1.3, 1.0, .5), 'plinth')
    box('glazing', (-1.25, -.95, .5), (1.25, .95, 1.8), 'glass')
    ridge('roof', -1.3, 1.3, -1.0, 1.0, 1.8, 2.4, 'glass')
    for x in (-1.3, 0, 1.3):
        box(f'mullion-{x}', (x - .04, -1.02, .5), (x + .04, 1.02, 1.82), 'frame')
    box('ridge-bar', (-1.32, -.04, 2.36), (1.32, .04, 2.44), 'frame')
    box('door', (-.35, -1.04, .02), (.35, -1.0, 1.8), 'frame')


def veg_bed():
    """A raised vegetable bed, 3.5 m by 1.0 m, with rows of crops."""
    box('edging', (-1.75, -.5, 0), (1.75, .5, .28), 'edging')
    box('soil', (-1.65, -.4, .28), (1.65, .4, .32), 'soil')
    for k, y in enumerate((-.22, .02, .26)):
        box(f'crop-{k}', (-1.5, y - .08, .32), (1.5, y + .08, .55 if k != 1 else .7), 'leaf' if k != 1 else 'leaf-dark')


def table():
    """A round garden table with two chairs."""
    prism('top', 0, 0, .5, .7, .76, 8, 'cream')
    box('leg', (-.05, -.05, 0), (.05, .05, .7), 'steel')
    for side in (-1, 1):
        box(f'seat-{side}', (side * .85 - .22, -.22, .42), (side * .85 + .22, .22, .47), 'cream')
        box(f'back-{side}', (side * 1.05 - .03, -.22, .47), (side * 1.05 + .03, .22, .9), 'cream')
        box(f'legs-{side}', (side * .85 - .18, -.18, 0), (side * .85 + .18, .18, .42), 'steel')


def bench():
    """A park bench, 1.8 m, its seat facing the house side."""
    box('seat', (-.9, -.25, .42), (.9, .25, .48), 'seat')
    box('back', (-.9, .2, .5), (.9, .26, .9), 'seat')
    for x in (-.8, .8):
        box(f'end-{x}', (x - .04, -.25, 0), (x + .04, .26, .5), 'iron')


def play():
    """A play set: swing frame, slide tower and a small climbing frame, 8 m by 5 m."""
    for x in (-3.4, -1.0):
        for y in (-.9, .9):
            box(f'swing-leg-{x}-{y}', (x - .06, y - .06, 0), (x + .06, y + .06, 2.2), 'red')
    box('swing-bar', (-3.5, -.06, 2.2), (-.9, .06, 2.32), 'red')
    for x in (-2.8, -1.6):
        box(f'swing-seat-{x}', (x - .25, -.12, .45), (x + .25, .12, .5), 'iron')
    box('tower', (.6, -.6, 0), (1.8, .6, 1.5), 'blue')
    box('tower-roof', (.5, -.7, 2.4), (1.9, .7, 2.5), 'yellow')
    for x in (.6, 1.8):
        box(f'tower-post-{x}', (x - .05, -.05, 1.5), (x + .05, .05, 2.4), 'blue')
    mesh('slide', [(1.8, -.35, 1.42), (3.9, -.35, .12), (3.9, .35, .12), (1.8, .35, 1.42),
                   (1.8, -.35, 1.5), (3.9, -.35, .2), (3.9, .35, .2), (1.8, .35, 1.5)], BOX_FACES, 'yellow')
    box('climber', (-.4, 1.4, 0), (.4, 2.2, 1.2), 'yellow')


def flowerbed():
    """The round planted bed at the centre of a shared green, 4.8 m across."""
    prism('kerb', 0, 0, 2.4, 0, .25, 12, 'trim')
    prism('soil', 0, 0, 2.2, .25, .3, 12, 'soil')
    prism('planting', 0, 0, 1.9, .3, .6, 12, 'leaf')
    prism('bloom', 0, 0, 1.1, .6, .8, 10, 'bloom')
    prism('centre', 0, 0, .4, .8, 1.0, 8, 'bloom-white')


def fence_panel():
    """One 2 m bay of close-boarded timber fence, 1.6 m high, posts at both ends."""
    box('boards', (-PANEL / 2, -.02, .05), (PANEL / 2, .02, 1.55), 'boards')
    for x in (-PANEL / 2, PANEL / 2):
        box(f'post-{x}', (x - .05, -.05, 0), (x + .05, .05, 1.65), 'post')
    box('rail', (-PANEL / 2, .02, .3), (PANEL / 2, .06, .38), 'post')
    box('rail-top', (-PANEL / 2, .02, 1.2), (PANEL / 2, .06, 1.28), 'post')


def railing_panel():
    """One 2 m bay of painted iron railing, 1.1 m high."""
    box('top', (-PANEL / 2, -.03, 1.02), (PANEL / 2, .03, 1.1), 'iron')
    box('bottom', (-PANEL / 2, -.03, .1), (PANEL / 2, .03, .16), 'iron')
    for k in range(10):
        x = -PANEL / 2 + .1 + k * .2
        box(f'bar-{k}', (x - .015, -.015, 0), (x + .015, .015, 1.16), 'iron')


MODULES = {
    'shed': shed,
    'greenhouse': greenhouse,
    'veg-bed': veg_bed,
    'table': table,
    'bench': bench,
    'play': play,
    'flowerbed': flowerbed,
    'fence-panel': fence_panel,
    'railing-panel': railing_panel,
}


def export(name, build):
    reset()
    build()
    assert all(p.area > 1e-8 for part in parts for p in part.data.polygons), name
    bpy.ops.object.select_all(action='DESELECT')
    for part in parts:
        part.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    body = bpy.context.object
    body.name = name
    low = [min(v.co[i] for v in body.data.vertices) for i in range(3)]
    high = [max(v.co[i] for v in body.data.vertices) for i in range(3)]
    geometry = hashlib.sha256(repr(([tuple(v.co) for v in body.data.vertices],
                                    [tuple(p.vertices) for p in body.data.polygons])).encode()).hexdigest()
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCES / f'{name}.blend'))
    bpy.ops.export_scene.gltf(filepath=str(EXPORT / f'{name}.glb'), export_format='GLB', use_selection=True,
                              export_apply=True, export_yup=True)
    return {'module': name, 'vertices': len(body.data.vertices), 'faces': len(body.data.polygons),
            'bounds_min': [round(v, 3) for v in low], 'bounds_max': [round(v, 3) for v in high],
            'materials': sorted({m.name for m in body.data.materials}), 'geometry_sha256': geometry}


SOURCES.mkdir(parents=True, exist_ok=True)
EXPORT.mkdir(parents=True, exist_ok=True)
records = [export(name, build) for name, build in MODULES.items()]
(SOURCES / 'modules.json').write_text(json.dumps({'panel_metres': PANEL, 'modules': records}, indent=2) + '\n')

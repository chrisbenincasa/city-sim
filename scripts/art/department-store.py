"""The high-street block's department store modules: a body cell, a roof tile, a parapet, four facade
bays and the corner tower.

Run with Blender in the background:
  "$BLENDER_BIN" --background --factory-startup --python-exit-code 1 --python scripts/art/department-store.py

The shell places each module whole at positions the simulation supplies, so every module is sized
to the 4 m Tile grid and none is stretched to fit. A storey is 3.5 m, the simulation's storey height.

The body cell and roof tile have their origin at the Tile's centre, at the storey's floor or the roof.
A facade bay or parapet has its origin at the wall line, at the middle of the bay, and its outer face
looks down -Y, which Godot receives as +Z (south). The shell turns them to face the other streets.
The corner tower's origin is the centre of its 8 m base on the roof. Faces are wound
counter-clockwise seen from outside. Materials are flat colours, which the shell reads as vertex
colours.
"""
import bpy
import hashlib
import json
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCES = ROOT / 'art/department-store'
EXPORT = ROOT / 'src/Borough.Godot/assets/department-store'

TILE = 4.0
STOREY = 3.5
ROOF = .15
PARAPET = .9
TOWER = 2 * TILE

COLOURS = {
    'stone': 'd9cdb4', 'string': 'eee6d3', 'membrane': '6b6e70', 'glass': '334049', 'frame': '2c3033',
    'fascia': '6e1f2a', 'corner-fascia': '1f3f5e', 'sign': 'f2e9cf', 'canopy': '3b3f42', 'door': '222629',
    'copper': '5f8f7c',
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
        shader.inputs['Roughness'].default_value = .25 if name == 'glass' else .85
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


def frustum(name, sides, bottom, top, low, high, material):
    """A closed prism with a regular polygon section, radius `bottom` at `low` and `top` at `high`."""
    ring = [(math.cos(math.tau * (i + .5) / sides), math.sin(math.tau * (i + .5) / sides)) for i in range(sides)]
    vertices = [(x * bottom, y * bottom, low) for x, y in ring] + [(x * top, y * top, high) for x, y in ring]
    faces = [tuple(reversed(range(sides))), tuple(range(sides, 2 * sides))]
    faces += [(i, (i + 1) % sides, sides + (i + 1) % sides, sides + i) for i in range(sides)]
    mesh(name, vertices, faces, material)


def body():
    """One Tile of the store for one storey, walled on every side."""
    half = TILE / 2
    box('body', (-half, -half, 0), (half, half, STOREY), 'stone')


def roof():
    """One Tile of flat roof."""
    half = TILE / 2
    box('roof', (-half, -half, 0), (half, half, ROOF), 'membrane')


def parapet():
    """One Tile of parapet along a roof edge, its coping overhanging the wall line."""
    half = TILE / 2
    box('parapet', (-half, 0, 0), (half, .35, PARAPET), 'stone')
    box('coping', (-half, -.1, PARAPET), (half, .45, PARAPET + .12), 'string')


def window_bay():
    """An upper-storey bay: a tall framed window, with a string course at the floor line."""
    half = TILE / 2
    box('string', (-half, -.12, 0), (half, 0, .25), 'string')
    box('frame', (-1.1, -.08, .75), (1.1, 0, 3.05), 'frame')
    box('glass', (-.95, -.1, .85), (.95, -.08, 2.95), 'glass')
    box('sill', (-1.2, -.2, .65), (1.2, 0, .75), 'string')


def shop_bay(fascia):
    """A ground-storey display window under a fascia band."""
    half = TILE / 2
    box('stallriser', (-half, -.1, 0), (half, 0, .45), 'stone')
    box('glass', (-half + .15, -.1, .45), (half - .15, -.05, 2.75), 'glass')
    box('fascia', (-half, -.25, 2.75), (half, 0, STOREY), fascia)


def anchor_bay():
    """One bay of the anchor store's ground storey."""
    shop_bay('fascia')


def entrance():
    """The anchor store's entrance bay: doors, a canopy over them and a name sign above the fascia."""
    half = TILE / 2
    shop_bay('fascia')
    box('door', (-1.3, -.12, 0), (1.3, -.1, 2.5), 'door')
    box('canopy', (-half, -2.4, 2.6), (half, -.25, 2.75), 'canopy')
    box('sign', (-1.7, -.35, 2.8), (1.7, -.25, 3.45), 'sign')


def corner_bay():
    """One bay of a corner Unit's ground storey, with its own fascia colour."""
    shop_bay('corner-fascia')


def corner_tower():
    """The corner feature: an octagonal drum and a copper dome raised over a corner Unit."""
    radius = TOWER / 2 * .92
    frustum('plinth', 8, radius, radius, 0, .4, 'string')
    frustum('drum', 8, radius * .88, radius * .88, .4, 3.4, 'stone')
    frustum('cornice', 8, radius * .96, radius * .96, 3.4, 3.7, 'string')
    frustum('dome-low', 8, radius * .86, radius * .7, 3.7, 5.2, 'copper')
    frustum('dome-high', 8, radius * .7, radius * .3, 5.2, 6.4, 'copper')
    frustum('finial', 8, .25, .05, 6.4, 7.6, 'string')


MODULES = {
    'body': body,
    'roof': roof,
    'parapet': parapet,
    'window-bay': window_bay,
    'anchor-bay': anchor_bay,
    'entrance': entrance,
    'corner-bay': corner_bay,
    'corner-tower': corner_tower,
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
    thing = bpy.context.object
    thing.name = name
    low = [min(v.co[i] for v in thing.data.vertices) for i in range(3)]
    high = [max(v.co[i] for v in thing.data.vertices) for i in range(3)]
    geometry = hashlib.sha256(repr(([tuple(v.co) for v in thing.data.vertices],
                                    [tuple(p.vertices) for p in thing.data.polygons])).encode()).hexdigest()
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCES / f'{name}.blend'))
    bpy.ops.export_scene.gltf(filepath=str(EXPORT / f'{name}.glb'), export_format='GLB', use_selection=True,
                              export_apply=True, export_yup=True)
    return {'module': name, 'vertices': len(thing.data.vertices), 'faces': len(thing.data.polygons),
            'bounds_min': [round(v, 3) for v in low], 'bounds_max': [round(v, 3) for v in high],
            'materials': sorted({m.name for m in thing.data.materials}), 'geometry_sha256': geometry}


SOURCES.mkdir(parents=True, exist_ok=True)
EXPORT.mkdir(parents=True, exist_ok=True)
records = [export(name, build) for name, build in MODULES.items()]
(SOURCES / 'modules.json').write_text(json.dumps({'tile_metres': TILE, 'storey_metres': STOREY,
                                                  'modules': records}, indent=2) + '\n')

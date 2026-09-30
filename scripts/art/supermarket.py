"""The town supermarket's modules: store bays, a car-park surface tile, a stall marking, a deck slab
and a deck edge.

Run with Blender in the background:
  "$BLENDER_BIN" --background --factory-startup --python-exit-code 1 --python scripts/art/supermarket.py

The shell places each module whole at positions the simulation supplies, so every module is sized
to the 4 m Tile grid and none is stretched to fit. The store is one storey of 3.5 m, the
simulation's storey height, and 40 m deep, its anchor row depth. A deck level is 3 m floor to floor.

Each module's origin is its footprint centre at ground level, except the deck slab, whose origin is
its top surface. The street face looks down -Y, which Godot receives as +Z. Faces are wound
counter-clockwise seen from outside. Materials are flat colours, which the shell reads as vertex
colours.
"""
import bpy
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCES = ROOT / 'art/supermarket'
EXPORT = ROOT / 'src/Borough.Godot/assets/supermarket'

TILE = 4.0
STOREY = 3.5
STORE_DEEP = 40.0
PARAPET = .8
DECK_LEVEL = 3.0
SLAB = .3
STALL_LONG = 5.0
STALL_WIDE = 2.5
LINE = .12

COLOURS = {
    'wall': 'd6d3c9', 'trim': 'eeede7', 'membrane': '6f7274', 'glass': '39454d', 'fascia': '2f6b46',
    'sign': 'f2efe4', 'canopy': 'c9c7bf', 'door': '2b2f31', 'asphalt': '333335', 'paint': 'dcdcd2',
    'concrete': 'a9a8a2', 'barrier': '8d9194',
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


def box(name, low, high, material):
    (x0, y0, z0), (x1, y1, z1) = low, high
    data = bpy.data.meshes.new(name)
    data.from_pydata([(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0),
                      (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)], [], BOX_FACES)
    data.materials.append(materials[material])
    data.update()
    thing = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(thing)
    parts.append(thing)


def store_bay():
    """One 4 m bay of the store: shed wall, flat roof, a glazed front under a fascia band."""
    half, deep = TILE / 2, STORE_DEEP / 2
    box('shed', (-half, -deep, 0), (half, deep, STOREY), 'wall')
    box('roof', (-half, -deep, STOREY), (half, deep, STOREY + .1), 'membrane')
    box('parapet-front', (-half, -deep, STOREY), (half, -deep + .3, STOREY + PARAPET), 'trim')
    box('parapet-back', (-half, deep - .3, STOREY), (half, deep, STOREY + PARAPET), 'trim')
    box('glazing', (-half + .1, -deep - .05, .3), (half - .1, -deep, STOREY - .7), 'glass')
    box('fascia', (-half, -deep - .15, STOREY - .7), (half, -deep, STOREY + PARAPET), 'fascia')


def store_entrance():
    """The entrance bay: a store bay with a door, a canopy over it and a sign above the fascia."""
    store_bay()
    deep = STORE_DEEP / 2
    box('door', (-1.2, -deep - .1, 0), (1.2, -deep - .05, 2.4), 'door')
    box('canopy', (-TILE / 2, -deep - 3.0, 2.8), (TILE / 2, -deep - .15, 3.0), 'canopy')
    box('sign', (-1.6, -deep - .3, STOREY + .1), (1.6, -deep - .15, STOREY + PARAPET + .6), 'sign')


def parking_surface():
    """One Tile of car-park asphalt."""
    half = TILE / 2
    box('asphalt', (-half, -half, 0), (half, half, .05), 'asphalt')


def stall():
    """The painted line along a stall's north edge, its long axis east-west, on top of a surface."""
    box('line', (-STALL_LONG / 2, STALL_WIDE / 2 - LINE / 2, .05), (STALL_LONG / 2, STALL_WIDE / 2 + LINE / 2, .06),
        'paint')


def deck_slab():
    """One Tile of deck floor, its top at the origin, with a column at its south-west corner that
    stands on the level below."""
    half = TILE / 2
    box('slab', (-half, -half, -SLAB), (half, half, 0), 'concrete')
    box('column', (-half, -half, -DECK_LEVEL + .05), (-half + .5, -half + .5, -SLAB), 'concrete')


def deck_edge():
    """One Tile of barrier along a raised deck's edge, running east-west."""
    box('barrier', (-TILE / 2, -.1, 0), (TILE / 2, .1, 1.1), 'barrier')


MODULES = {
    'store-bay': store_bay,
    'store-entrance': store_entrance,
    'parking-surface': parking_surface,
    'stall': stall,
    'deck-slab': deck_slab,
    'deck-edge': deck_edge,
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
(SOURCES / 'modules.json').write_text(json.dumps({'tile_metres': TILE, 'storey_metres': STOREY,
                                                  'deck_level_metres': DECK_LEVEL, 'modules': records},
                                                 indent=2) + '\n')

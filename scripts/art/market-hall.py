"""The market hall's modules: three families of hall shell, two stall states and the market square.

Run with Blender in the background:
  "$BLENDER_BIN" --background --factory-startup --python-exit-code 1 --python scripts/art/market-hall.py

The shell places each module whole at positions the simulation supplies, so every module is sized to
the 4 m Tile grid and none is stretched to fit. A hall is built a Tile at a time: a wall or an
entrance on each edge Tile, a roof over every Tile (over aisle Tiles only in the stall shed), and one
stall on each stall Unit's Tile.

Each module's origin is its Tile's centre at ground level. A wall's or a stall's front looks down -Y,
which Godot receives as +Z, and the shell turns it outward or to face its aisle. Roof and square
pieces are placed unturned. Faces are wound counter-clockwise seen from outside. Materials are flat
colours, which the shell reads as vertex colours.
"""
import bpy
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCES = ROOT / 'art/market-hall'
EXPORT = ROOT / 'src/Borough.Godot/assets/market-hall'

TILE = 4.0
HALF = TILE / 2
FRONT = -HALF

IRON_EAVES = 7.0
CONCRETE_EAVES = 5.5
SHED_EAVES = 4.2

COLOURS = {
    'brick': '8e4b32', 'stone': 'd9cdb0', 'iron': '24413a', 'roof-glass': 'a9c3c2', 'glass': '39454d',
    'door': '2b2f31', 'concrete': 'b0aca1', 'concrete-dark': '8a867c', 'rooflight': 'd8e2e0',
    'sheet': '7d8a8c', 'steel': '5b6164', 'sign': 'c9412e', 'counter': '8a6a47', 'awning': 'c23b32',
    'awning-stripe': 'f1ece0', 'crate': 'b48a52', 'fruit': 'd9922b', 'greens': '5d8f3a',
    'shutter': '9ea3a4', 'flags': 'c7bda8', 'joint': '9c937f', 'trunk': '5a4432', 'leaf': '4f7a3a',
    'bench': '6b4a2f',
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
        shader.inputs['Roughness'].default_value = .25 if 'glass' in name else .85
        material.diffuse_color = colour
        materials[name] = material


def link(name, vertices, faces, material):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.materials.append(materials[material])
    data.update()
    thing = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(thing)
    parts.append(thing)


def box(name, low, high, material):
    (x0, y0, z0), (x1, y1, z1) = low, high
    link(name, [(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0),
                (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)], BOX_FACES, material)


def ridge_along_y(name, half_x, y0, y1, base, top, material):
    """A roof pitched across X, its ridge running along Y over x = 0, eaves at z = base."""
    link(name, [(-half_x, y0, base), (0, y0, base + top), (half_x, y0, base),
                (-half_x, y1, base), (0, y1, base + top), (half_x, y1, base)],
         [(0, 1, 4, 3), (1, 2, 5, 4), (0, 2, 1), (3, 4, 5), (0, 3, 5, 2)], material)


def slope_across_y(name, y0, y1, low, high, material, thick=.12):
    """A flat sheet over the whole Tile, falling from the back (+Y) to the front (-Y)."""
    link(name, [(-HALF, y0, low), (HALF, y0, low), (HALF, y1, high), (-HALF, y1, high),
                (-HALF, y0, low + thick), (HALF, y0, low + thick), (HALF, y1, high + thick),
                (-HALF, y1, high + thick)], BOX_FACES, material)


# Victorian iron-and-glass hall: brick plinth, glazed iron screens, ridge-and-furrow glass roof.

def iron_wall():
    """One Tile of outer wall: a brick plinth, a glazed screen in an iron frame, a stone cornice."""
    box('plinth', (-HALF, FRONT, 0), (HALF, FRONT + .5, 1.2), 'brick')
    box('screen', (-HALF, FRONT + .15, 1.2), (HALF, FRONT + .3, IRON_EAVES - .6), 'glass')
    box('pier', (-HALF, FRONT - .05, 0), (-HALF + .35, FRONT + .5, IRON_EAVES - .6), 'brick')
    box('transom', (-HALF, FRONT + .1, 3.8), (HALF, FRONT + .35, 3.95), 'iron')
    box('cornice', (-HALF, FRONT - .2, IRON_EAVES - .6), (HALF, FRONT + .5, IRON_EAVES), 'stone')


def iron_entrance():
    """One Tile of outer wall opened to an aisle, under an arched iron fanlight."""
    box('pier', (-HALF, FRONT - .05, 0), (-HALF + .35, FRONT + .5, IRON_EAVES - .6), 'brick')
    box('opening', (-HALF + .35, FRONT + .35, 0), (HALF, FRONT + .4, 3.6), 'door')
    box('fanlight', (-HALF + .35, FRONT + .15, 3.6), (HALF, FRONT + .3, IRON_EAVES - .6), 'roof-glass')
    box('arch', (-HALF + .35, FRONT - .1, 3.6), (HALF, FRONT + .3, 3.9), 'iron')
    box('cornice', (-HALF, FRONT - .2, IRON_EAVES - .6), (HALF, FRONT + .5, IRON_EAVES), 'stone')


def iron_roof():
    """One Tile of the roof's ironwork: a rib across the hall and a ridge bar running north-south."""
    box('rib', (-HALF, -.07, IRON_EAVES - .25), (HALF, .07, IRON_EAVES), 'iron')
    box('ridge', (-.1, -HALF, IRON_EAVES + 1.0), (.1, HALF, IRON_EAVES + 1.15), 'iron')


def iron_glass():
    """One Tile of ridge-and-furrow glazing over the ironwork. The shell draws it see-through."""
    ridge_along_y('glass', HALF, -HALF, HALF, IRON_EAVES, 1.1, 'roof-glass')


# 1960s concrete hall: ribbed board-marked walls, a flat slab roof with a rooflight to each Tile.

def concrete_wall():
    """One Tile of ribbed concrete wall with a clerestory strip under the roof."""
    box('wall', (-HALF, FRONT, 0), (HALF, FRONT + .4, CONCRETE_EAVES - 1.0), 'concrete')
    box('rib', (-.2, FRONT - .25, 0), (.2, FRONT, CONCRETE_EAVES), 'concrete-dark')
    box('clerestory', (-HALF, FRONT + .1, CONCRETE_EAVES - 1.0), (HALF, FRONT + .3, CONCRETE_EAVES), 'glass')


def concrete_entrance():
    """One Tile of wall opened to an aisle, under a cantilevered canopy."""
    box('opening', (-HALF, FRONT + .3, 0), (HALF, FRONT + .35, 3.0), 'door')
    box('lintel', (-HALF, FRONT, 3.0), (HALF, FRONT + .4, CONCRETE_EAVES - 1.0), 'concrete')
    box('canopy', (-HALF, FRONT - 1.6, 3.0), (HALF, FRONT, 3.25), 'concrete-dark')
    box('clerestory', (-HALF, FRONT + .1, CONCRETE_EAVES - 1.0), (HALF, FRONT + .3, CONCRETE_EAVES), 'glass')


def concrete_roof():
    """One Tile of flat slab roof with a square rooflight at its centre."""
    box('slab', (-HALF, -HALF, CONCRETE_EAVES), (HALF, HALF, CONCRETE_EAVES + .35), 'concrete')
    box('upstand', (-1.0, -1.0, CONCRETE_EAVES + .35), (1.0, 1.0, CONCRETE_EAVES + .6), 'concrete-dark')
    box('rooflight', (-.85, -.85, CONCRETE_EAVES + .6), (.85, .85, CONCRETE_EAVES + .75), 'rooflight')


# Open stall shed: a steel frame and a sheet roof, open on every side.

def shed_wall():
    """One Tile of open edge: a steel post at the Tile's west corner and an eaves beam."""
    box('post', (-HALF, FRONT, 0), (-HALF + .2, FRONT + .2, SHED_EAVES), 'steel')
    box('beam', (-HALF, FRONT, SHED_EAVES - .35), (HALF, FRONT + .2, SHED_EAVES), 'steel')


def shed_entrance():
    """An open edge with the market's sign hung from the eaves beam."""
    shed_wall()
    box('sign', (-HALF + .3, FRONT - .05, SHED_EAVES - 1.1), (HALF - .3, FRONT + .05, SHED_EAVES - .35), 'sign')


def shed_roof():
    """One Tile of aisle roof, falling from a ridge at its back to the eaves at its front.

    The shell roofs only the aisles, turning each aisle's two Tiles to fall away from the ridge
    between them, so the stall pairs stand open to the sky.
    """
    slope_across_y('sheet', -HALF, HALF, SHED_EAVES, SHED_EAVES + .5, 'sheet')
    box('purlin', (-HALF, -.06, SHED_EAVES + .1), (HALF, .06, SHED_EAVES + .25), 'steel')


# A stall: one Tile, its counter on the aisle side.

def stall_frame():
    box('back', (-HALF + .1, HALF - .25, 0), (HALF - .1, HALF - .1, 2.3), 'counter')
    box('post-west', (-HALF + .1, FRONT + .1, 0), (-HALF + .25, FRONT + .25, 2.3), 'counter')
    box('post-east', (HALF - .25, FRONT + .1, 0), (HALF - .1, FRONT + .25, 2.3), 'counter')


def stall_open():
    """A trading stall: a counter of produce under a striped awning."""
    stall_frame()
    box('counter', (-HALF + .1, FRONT + .1, 0), (HALF - .1, FRONT + .9, .9), 'counter')
    box('crates', (-HALF + .2, FRONT + .15, .9), (HALF - .2, FRONT + .85, 1.05), 'crate')
    box('fruit', (-HALF + .3, FRONT + .2, 1.05), (-.1, FRONT + .8, 1.25), 'fruit')
    box('greens', (.1, FRONT + .2, 1.05), (HALF - .3, FRONT + .8, 1.25), 'greens')
    slope_across_y('awning', FRONT - .4, HALF - .25, 2.0, 2.5, 'awning', .06)
    box('stripe', (-.25, FRONT - .4, 2.07), (.25, FRONT + .1, 2.14), 'awning-stripe')


def stall_shut():
    """A vacant stall: the counter hidden behind a closed roller shutter."""
    stall_frame()
    box('shutter', (-HALF + .1, FRONT + .1, 0), (HALF - .1, FRONT + .25, 2.3), 'shutter')
    box('box', (-HALF + .1, FRONT + .1, 2.3), (HALF - .1, FRONT + .4, 2.55), 'shutter')


# The market square.

def square_paving():
    """One Tile of square: large flags with a joint across both ways."""
    box('flags', (-HALF, -HALF, 0), (HALF, HALF, .06), 'flags')
    box('joint-x', (-HALF, -.04, .06), (HALF, .04, .065), 'joint')
    box('joint-y', (-.04, -HALF, .06), (.04, HALF, .065), 'joint')


def square_tree():
    """A plane tree in a paved pit."""
    box('pit', (-.8, -.8, .06), (.8, .8, .1), 'joint')
    box('trunk', (-.2, -.2, 0), (.2, .2, 3.2), 'trunk')
    box('crown', (-1.9, -1.9, 3.0), (1.9, 1.9, 6.4), 'leaf')
    box('top', (-1.2, -1.2, 6.4), (1.2, 1.2, 7.4), 'leaf')


def square_bench():
    """A timber bench facing south."""
    box('seat', (-1.2, -.25, .42), (1.2, .25, .5), 'bench')
    box('back', (-1.2, .2, .5), (1.2, .28, .95), 'bench')
    box('leg-west', (-1.1, -.2, 0), (-.95, .2, .42), 'steel')
    box('leg-east', (.95, -.2, 0), (1.1, .2, .42), 'steel')


MODULES = {
    'iron-wall': iron_wall,
    'iron-entrance': iron_entrance,
    'iron-roof': iron_roof,
    'concrete-wall': concrete_wall,
    'concrete-entrance': concrete_entrance,
    'concrete-roof': concrete_roof,
    'shed-wall': shed_wall,
    'shed-entrance': shed_entrance,
    'shed-roof': shed_roof,
    'stall-open': stall_open,
    'stall-shut': stall_shut,
    'square-paving': square_paving,
    'square-tree': square_tree,
    'square-bench': square_bench,
    'iron-glass': iron_glass,
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
(SOURCES / 'modules.json').write_text(json.dumps({'tile_metres': TILE, 'modules': records}, indent=2) + '\n')

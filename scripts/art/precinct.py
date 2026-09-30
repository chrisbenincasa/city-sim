"""The precinct's modules, three families of shop tiles, a walkway piece and a gallery.

Run with Blender in the background:
  "$BLENDER_BIN" --background --factory-startup --python-exit-code 1 --python scripts/art/precinct.py

The shell places each module whole at positions the simulation supplies, so every module is sized to
the 4 m Tile grid and none is stretched to fit. A shop row is built a Tile at a time: a front tile
facing the walkway, with a door on the first Tile of each Unit, and body tiles behind it. Each
storey repeats the ground's tiles. The deck is the supermarket's modules.

Each module's origin is its footprint centre at ground level, except a walkway roof and a gallery,
whose origin is the storey line they hang from. A shop tile's front looks down -Y, which Godot
receives as +Z, and the shell turns it to face the walkway. A walkway piece spans the 2-Tile walkway
across X and one Tile along Y, and is placed unturned. Faces are wound counter-clockwise seen from
outside. Materials are flat colours, which the shell reads as vertex colours.
"""
import bpy
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCES = ROOT / 'art/precinct'
EXPORT = ROOT / 'src/Borough.Godot/assets/precinct'

TILE = 4.0
STOREY = 3.5
WALKWAY = 8.0

COLOURS = {
    'glass': '39454d', 'door': '2b2f31', 'membrane': '6f7274', 'paving': 'b5aea0', 'joint': '8f887a',
    'brick': '9b5a3c', 'stone': 'e2d8bf', 'timber': '2f5a45', 'iron': '1f3a30', 'roof-glass': 'a9c3c2',
    'concrete': 'a8a49a', 'tile': '7a5a3c', 'fascia': 'd9772b', 'soil': '4e3b2a', 'leaf': '4f7a3a',
    'panel': 'eceae4', 'steel': 'b7babd', 'accent': '3a6ea5',
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


def ridge_along_y(name, half_x, y0, y1, top, material):
    """A roof pitched across X, its ridge running along Y over x = 0, eaves at z = 0."""
    link(name, [(-half_x, y0, 0), (0, y0, top), (half_x, y0, 0), (-half_x, y1, 0), (0, y1, top), (half_x, y1, 0)],
         [(0, 1, 4, 3), (1, 2, 5, 4), (0, 2, 1), (3, 4, 5), (0, 3, 5, 2)], material)


HALF = TILE / 2
FRONT = -HALF


def shop_tile(wall, roof, top):
    box('mass', (-HALF, -HALF, 0), (HALF, HALF, STOREY), wall)
    box('roof', (-HALF, -HALF, STOREY), (HALF, HALF, STOREY + top), roof)


# Victorian arcade: brick and stone, timber shopfronts, a glazed ridge roof on iron ribs.

def arcade_front():
    """One Tile of shopfront: a timber frame round a tall window, under a stone cornice."""
    shop_tile('brick', 'stone', .3)
    box('frame', (-HALF, FRONT - .12, 0), (HALF, FRONT, STOREY - .5), 'timber')
    box('window', (-HALF + .35, FRONT - .16, .6), (HALF - .35, FRONT - .12, STOREY - .8), 'glass')
    box('cornice', (-HALF, FRONT - .3, STOREY - .5), (HALF, FRONT, STOREY), 'stone')


def arcade_door():
    """The first Tile of a shop: a half-glazed door beside a narrow window."""
    arcade_front()
    box('door', (-1.6, FRONT - .2, 0), (-.4, FRONT - .16, 2.5), 'door')


def arcade_body():
    """One Tile of shop behind its front."""
    shop_tile('brick', 'membrane', .15)


def arcade_cover():
    """One Tile of the passage roof: glass pitched between the rows, on an iron rib."""
    ridge_along_y('glass', WALKWAY / 2, -HALF, HALF, 2.4, 'roof-glass')
    box('rib', (-WALKWAY / 2, -.08, 0), (WALKWAY / 2, .08, .2), 'iron')
    box('ridge', (-.12, -HALF, 2.3), (.12, HALF, 2.5), 'iron')


def arcade_gallery():
    """An upper walkway along the front, on iron brackets, with a railing."""
    box('deck', (-HALF, FRONT - 1.5, -.15), (HALF, FRONT, 0), 'timber')
    box('rail', (-HALF, FRONT - 1.5, .95), (HALF, FRONT - 1.4, 1.05), 'iron')
    box('bar', (-HALF, FRONT - 1.48, .1), (HALF, FRONT - 1.42, .15), 'iron')
    box('bracket', (-.08, FRONT - 1.4, -.9), (.08, FRONT, -.15), 'iron')


# 1960s open precinct: concrete frame, tiled panels, orange fascias, planters in the open walkway.

def open_front():
    """One Tile of shopfront: full-width glazing under a deep orange fascia."""
    shop_tile('concrete', 'concrete', .4)
    box('window', (-HALF + .1, FRONT - .05, .3), (HALF - .1, FRONT, STOREY - 1.0), 'glass')
    box('fascia', (-HALF, FRONT - .6, STOREY - 1.0), (HALF, FRONT, STOREY - .3), 'fascia')
    box('soffit', (-HALF, FRONT - 1.6, STOREY - 1.0), (HALF, FRONT, STOREY - .85), 'concrete')


def open_door():
    """The first Tile of a shop: a glazed door in the window wall."""
    open_front()
    box('door', (-.9, FRONT - .1, 0), (.9, FRONT - .05, 2.3), 'door')


def open_body():
    """One Tile of shop behind its front, clad in tiled panels."""
    shop_tile('tile', 'membrane', .15)


def open_cover():
    """A raised planter with a bench along it, standing on the walkway's paving."""
    box('planter', (-1.6, -.9, 0), (1.6, .9, .7), 'concrete')
    box('soil', (-1.45, -.75, .7), (1.45, .75, .75), 'soil')
    box('shrubs', (-1.3, -.6, .75), (1.3, .6, 1.4), 'leaf')
    box('bench', (-1.6, -1.4, .4), (1.6, -.9, .5), 'timber')


def open_gallery():
    """An upper deck along the front, in concrete with a solid parapet."""
    box('deck', (-HALF, FRONT - 2.0, -.3), (HALF, FRONT, 0), 'concrete')
    box('parapet', (-HALF, FRONT - 2.0, 0), (HALF, FRONT - 1.8, 1.0), 'concrete')
    box('column', (-.2, FRONT - 2.0, -STOREY), (.2, FRONT - 1.6, -.3), 'concrete')


# Modern galleria: white panels, silver frames, a flat glazed roof over the walkway.

def galleria_front():
    """One Tile of shopfront: frameless glazing between silver mullions."""
    shop_tile('panel', 'panel', .3)
    box('window', (-HALF, FRONT - .05, 0), (HALF, FRONT, STOREY - .6), 'glass')
    box('mullion', (-HALF, FRONT - .1, 0), (-HALF + .1, FRONT, STOREY - .6), 'steel')
    box('band', (-HALF, FRONT - .15, STOREY - .6), (HALF, FRONT, STOREY), 'accent')


def galleria_door():
    """The first Tile of a shop: a wide glazed opening framed in silver."""
    galleria_front()
    box('opening', (-1.4, FRONT - .12, 0), (1.4, FRONT - .05, 2.8), 'door')
    box('head', (-1.5, FRONT - .15, 2.8), (1.5, FRONT - .05, 2.95), 'steel')


def galleria_body():
    """One Tile of shop behind its front."""
    shop_tile('panel', 'membrane', .15)


def galleria_cover():
    """One Tile of the walkway roof: a flat glazed panel on a silver beam, raised on an upstand."""
    box('upstand', (-WALKWAY / 2, -HALF, 0), (-WALKWAY / 2 + .2, HALF, 1.2), 'steel')
    box('upstand-east', (WALKWAY / 2 - .2, -HALF, 0), (WALKWAY / 2, HALF, 1.2), 'steel')
    box('glass', (-WALKWAY / 2, -HALF, 1.2), (WALKWAY / 2, HALF, 1.3), 'roof-glass')
    box('beam', (-WALKWAY / 2, -.1, 1.0), (WALKWAY / 2, .1, 1.2), 'steel')


def galleria_gallery():
    """An upper walkway along the front with a glass balustrade."""
    box('deck', (-HALF, FRONT - 1.8, -.2), (HALF, FRONT, 0), 'panel')
    box('balustrade', (-HALF, FRONT - 1.8, 0), (HALF, FRONT - 1.75, 1.0), 'glass')
    box('handrail', (-HALF, FRONT - 1.82, 1.0), (HALF, FRONT - 1.72, 1.06), 'steel')


def paving():
    """One Tile of walkway floor: flags with a joint across the middle."""
    box('flags', (-HALF, -HALF, 0), (HALF, HALF, .06), 'paving')
    box('joint', (-HALF, -.04, .06), (HALF, .04, .065), 'joint')


MODULES = {
    'arcade-front': arcade_front,
    'arcade-door': arcade_door,
    'arcade-body': arcade_body,
    'arcade-cover': arcade_cover,
    'arcade-gallery': arcade_gallery,
    'open-front': open_front,
    'open-door': open_door,
    'open-body': open_body,
    'open-cover': open_cover,
    'open-gallery': open_gallery,
    'galleria-front': galleria_front,
    'galleria-door': galleria_door,
    'galleria-body': galleria_body,
    'galleria-cover': galleria_cover,
    'galleria-gallery': galleria_gallery,
    'paving': paving,
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
                                                  'walkway_metres': WALKWAY, 'modules': records},
                                                 indent=2) + '\n')

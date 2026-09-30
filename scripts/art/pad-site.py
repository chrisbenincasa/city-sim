"""The pad site's modules, three families of a building bay, a door bay and forecourt pieces.

Run with Blender in the background:
  "$BLENDER_BIN" --background --factory-startup --python-exit-code 1 --python scripts/art/pad-site.py

The shell places each module whole at positions the simulation supplies, so every module is sized to
the 4 m Tile grid and none is stretched to fit. A bay is one Tile along the pad's street and 16 m
deep, the pad's footprint depth. Forecourt pieces are one Tile square. The car park's asphalt and
stall markings are the supermarket's modules.

Each module's origin is its footprint centre at ground level. The street face looks down -Y, which
Godot receives as +Z, and the shell turns each pad to face its street. Faces are wound
counter-clockwise seen from outside. Materials are flat colours, which the shell reads as vertex
colours.
"""
import bpy
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCES = ROOT / 'art/pad-site'
EXPORT = ROOT / 'src/Borough.Godot/assets/pad-site'

TILE = 4.0
STOREY = 3.5
BAY_DEEP = 16.0
CANOPY = 4.8
LINE = .12

COLOURS = {
    'render': 'e4e1d8', 'membrane': '6f7274', 'glass': '39454d', 'door': '2b2f31', 'sign': 'f2efe4',
    'petrol': 'b3302a', 'canopy': 'efefea', 'kerb': 'b9b8b1', 'pump': 'd9d9d4', 'post': '8d9194',
    'brick': '9b5a3c', 'roof': '4a3a33', 'burger': 'e0a526',
    'stone': 'cfc6b0', 'cornice': 'b8ae96', 'bank': '1f3f6b',
    'asphalt': '333335', 'paint': 'dcdcd2',
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


HALF = TILE / 2
FRONT = -BAY_DEEP / 2
BACK = BAY_DEEP / 2


def asphalt():
    box('asphalt', (-HALF, -HALF, 0), (HALF, HALF, .05), 'asphalt')


# Petrol forecourt: a flat-roofed kiosk, a canopy over the pumps.

def kiosk_bay():
    """One bay of the kiosk: rendered shed, flat roof, a glazed front under a red fascia."""
    box('shed', (-HALF, FRONT, 0), (HALF, BACK, STOREY), 'render')
    box('roof', (-HALF, FRONT, STOREY), (HALF, BACK, STOREY + .1), 'membrane')
    box('glazing', (-HALF + .1, FRONT - .05, .3), (HALF - .1, FRONT, STOREY - .7), 'glass')
    box('fascia', (-HALF, FRONT - .15, STOREY - .7), (HALF, FRONT, STOREY + .5), 'petrol')


def kiosk_door():
    """The kiosk's entrance bay: a door and a sign above the fascia."""
    kiosk_bay()
    box('door', (-1.0, FRONT - .1, 0), (1.0, FRONT - .05, 2.3), 'door')
    box('sign', (-1.4, FRONT - .3, STOREY + .5), (1.4, FRONT - .15, STOREY + 1.3), 'sign')


def canopy():
    """One Tile of forecourt canopy: a white soffit under a red band."""
    box('soffit', (-HALF, -HALF, CANOPY), (HALF, HALF, CANOPY + .3), 'canopy')
    box('band', (-HALF, -HALF, CANOPY + .3), (HALF, HALF, CANOPY + .9), 'petrol')


def pump_island():
    """A kerbed island running back from the street, with two pumps and a canopy column."""
    box('island', (-.6, -1.6, 0), (.6, 1.6, .15), 'kerb')
    box('pump-front', (-.35, -1.3, .15), (.35, -.7, 1.8), 'pump')
    box('pump-back', (-.35, .7, .15), (.35, 1.3, 1.8), 'pump')
    box('column', (-.15, -.15, .15), (.15, .15, CANOPY), 'post')


# Fast-food pavilion: a brick box under a pitched band, a drive lane past a menu board.

def pavilion_bay():
    """One bay of the pavilion: brick walls, a dark roof band and a glazed front."""
    box('walls', (-HALF, FRONT, 0), (HALF, BACK, STOREY), 'brick')
    box('roof-band', (-HALF, FRONT - .6, STOREY), (HALF, BACK + .6, STOREY + .9), 'roof')
    box('roof', (-HALF, FRONT + .4, STOREY + .9), (HALF, BACK - .4, STOREY + 1.2), 'roof')
    box('glazing', (-HALF + .3, FRONT - .05, .6), (HALF - .3, FRONT, STOREY - .4), 'glass')


def pavilion_door():
    """The pavilion's entrance bay: a door, a hatch for the drive lane and a sign on the roof."""
    pavilion_bay()
    box('door', (-1.0, FRONT - .1, 0), (1.0, FRONT - .05, 2.3), 'door')
    box('hatch', (1.2, FRONT - .12, 1.0), (1.8, FRONT - .05, 1.6), 'burger')
    box('sign', (-1.5, -.1, STOREY + 1.2), (1.5, .1, STOREY + 2.6), 'burger')


def drive_lane():
    """One Tile of drive lane along the building front, with a painted arrow along the lane."""
    asphalt()
    box('edge-front', (-HALF, -HALF + .2, .05), (HALF, -HALF + .2 + LINE, .06), 'paint')
    box('edge-back', (-HALF, HALF - .2 - LINE, .05), (HALF, HALF - .2, .06), 'paint')
    box('arrow-shaft', (-1.2, -LINE / 2, .05), (.6, LINE / 2, .06), 'paint')
    box('arrow-head', (.6, -.4, .05), (1.0, .4, .06), 'paint')


def menu_board():
    """A menu board on two posts, its face toward the lane at +Y."""
    box('post-west', (-1.0, -.1, 0), (-.8, .1, 1.0), 'post')
    box('post-east', (.8, -.1, 0), (1.0, .1, 1.0), 'post')
    box('board', (-1.2, -.1, 1.0), (1.2, .1, 2.4), 'roof')
    box('face', (-1.1, .1, 1.1), (1.1, .15, 2.3), 'burger')


# Bank drive-up: a stone box with a portico, a covered lane past a teller island.

def bank_bay():
    """One bay of the bank: stone walls, a cornice and a tall narrow window."""
    box('walls', (-HALF, FRONT, 0), (HALF, BACK, STOREY + .6), 'stone')
    box('cornice', (-HALF, FRONT - .25, STOREY + .6), (HALF, BACK + .25, STOREY + 1.0), 'cornice')
    box('window', (-.6, FRONT - .05, .9), (.6, FRONT, STOREY - .2), 'glass')


def bank_door():
    """The bank's entrance bay: a door between two columns, and its name over the cornice."""
    bank_bay()
    box('door', (-.9, FRONT - .1, 0), (.9, FRONT - .05, 2.6), 'door')
    box('column-west', (-1.7, FRONT - 1.0, 0), (-1.3, FRONT - .6, STOREY + .6), 'cornice')
    box('column-east', (1.3, FRONT - 1.0, 0), (1.7, FRONT - .6, STOREY + .6), 'cornice')
    box('pediment', (-1.9, FRONT - 1.1, STOREY + .6), (1.9, FRONT, STOREY + 1.0), 'cornice')
    box('name', (-1.5, FRONT - 1.15, STOREY + .65), (1.5, FRONT - 1.1, STOREY + .95), 'bank')


def drive_up_lane():
    """One Tile of covered lane along the building front."""
    asphalt()
    box('edge-front', (-HALF, -HALF + .2, .05), (HALF, -HALF + .2 + LINE, .06), 'paint')
    box('roof', (-HALF, -HALF, 3.6), (HALF, HALF, 3.9), 'stone')
    box('fascia', (-HALF, -HALF - .05, 3.5), (HALF, -HALF + .1, 4.0), 'bank')


def teller():
    """A kerbed island with a teller pedestal facing the lane at +Y, and a column under the roof."""
    box('island', (-1.5, -.5, 0), (1.5, .5, .15), 'kerb')
    box('pedestal', (-.4, -.1, .15), (.4, .4, 1.4), 'stone')
    box('screen', (-.3, .4, .8), (.3, .45, 1.2), 'glass')
    box('column', (1.1, -.15, .15), (1.4, .15, 3.6), 'post')


MODULES = {
    'kiosk-bay': kiosk_bay,
    'kiosk-door': kiosk_door,
    'canopy': canopy,
    'pump-island': pump_island,
    'pavilion-bay': pavilion_bay,
    'pavilion-door': pavilion_door,
    'drive-lane': drive_lane,
    'menu-board': menu_board,
    'bank-bay': bank_bay,
    'bank-door': bank_door,
    'drive-up-lane': drive_up_lane,
    'teller': teller,
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
                                                  'bay_deep_metres': BAY_DEEP, 'modules': records},
                                                 indent=2) + '\n')

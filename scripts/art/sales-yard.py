"""The sales yard's modules, three families of a shed bay, a door bay, a yard tile and a yard feature.

Run with Blender in the background:
  "$BLENDER_BIN" --background --factory-startup --python-exit-code 1 --python scripts/art/sales-yard.py

The shell places each module whole at positions the simulation supplies, so every module is sized to
the 4 m Tile grid and none is stretched to fit. A bay is one Tile along the yard's street and 40 m
deep, the shed's footprint depth. Yard tiles and features are one Tile square. The stall band's
asphalt and stall markings are the supermarket's modules.

Each module's origin is its footprint centre at ground level. The street face looks down -Y, which
Godot receives as +Z, and the shell turns each Lot to face its street. Faces are wound
counter-clockwise seen from outside. Materials are flat colours, which the shell reads as vertex
colours.
"""
import bpy
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCES = ROOT / 'art/sales-yard'
EXPORT = ROOT / 'src/Borough.Godot/assets/sales-yard'

TILE = 4.0
SHED_DEEP = 40.0
LINE = .12

COLOURS = {
    'cladding': 'c9ccce', 'membrane': '6f7274', 'glass': '39454d', 'door': '2b2f31', 'sign': 'f2efe4',
    'dealer': '1d4f91', 'asphalt': '333335', 'paint': 'dcdcd2', 'post': '8d9194',
    'car-red': 'a8302c', 'car-silver': 'b7babd', 'car-blue': '2f4d7a', 'car-black': '222426',
    'plinth': 'b9b8b1', 'frame': 'e8e8e2', 'pane': '9fb8b4', 'gravel': 'a39a86', 'bench': '6b5139',
    'leaf': '4f7a3a', 'bloom': 'c7607a', 'pot': '9b5a3c',
    'steel': '5d6e62', 'roof': '4a4f52', 'shutter': '9ea3a6', 'merchant': 'e0a526', 'concrete': '9a9994',
    'timber': 'c09a66', 'brick': '9b5a3c', 'pallet': '8a6f4a',
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
        shader.inputs['Roughness'].default_value = .25 if name in ('glass', 'pane') else .85
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


def ridge(name, x0, x1, y0, y1, eave, top, material):
    """A roof pitched along X: two slopes meeting over the middle of Y, with gable ends."""
    middle = (y0 + y1) / 2
    data = bpy.data.meshes.new(name)
    data.from_pydata([(x0, y0, eave), (x1, y0, eave), (x1, middle, top), (x0, middle, top),
                      (x0, y1, eave), (x1, y1, eave)], [],
                     [(0, 1, 2, 3), (3, 2, 5, 4), (0, 3, 4), (1, 5, 2), (0, 4, 5, 1)])
    data.materials.append(materials[material])
    data.update()
    thing = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(thing)
    parts.append(thing)


HALF = TILE / 2
FRONT = -SHED_DEEP / 2
BACK = SHED_DEEP / 2


def car(name, x, y, material):
    """A parked car on a Tile, 1.7 m wide and 3.8 m long along Y."""
    box(f'{name}-body', (x - .85, y - 1.9, .2), (x + .85, y + 1.9, .95), material)
    box(f'{name}-cabin', (x - .75, y - .9, .95), (x + .75, y + .8, 1.45), 'glass')


# Car showroom: a glazed front under a dealer's fascia, a lot of cars for sale, a totem sign.

SHOWROOM = 7.0


def showroom_bay():
    """One bay of the showroom: glazed full height for the first 12 m, clad behind."""
    box('hall', (-HALF, FRONT + 12, 0), (HALF, BACK, SHOWROOM), 'cladding')
    box('front', (-HALF, FRONT, 0), (HALF, FRONT + 12, SHOWROOM), 'glass')
    box('roof', (-HALF, FRONT, SHOWROOM), (HALF, BACK, SHOWROOM + .15), 'membrane')
    box('fascia', (-HALF, FRONT - .2, SHOWROOM - 1.2), (HALF, FRONT, SHOWROOM + .4), 'dealer')


def showroom_door():
    """The showroom's entrance bay: a door and the dealer's name over the fascia."""
    showroom_bay()
    box('door', (-1.2, FRONT - .1, 0), (1.2, FRONT - .02, 2.6), 'door')
    box('sign', (-1.8, FRONT - .35, SHOWROOM + .4), (1.8, FRONT - .2, SHOWROOM + 1.4), 'sign')


def car_lot():
    """One Tile of the sales lot: asphalt with two cars parked nose to the street."""
    box('asphalt', (-HALF, -HALF, 0), (HALF, HALF, .05), 'asphalt')
    box('line', (-LINE / 2, -HALF, .05), (LINE / 2, HALF, .06), 'paint')
    car('west', -1.0, 0, 'car-red')
    car('east', 1.0, 0, 'car-silver')


def totem():
    """The dealer's totem sign at the lot's street corner, with a car on its plinth."""
    box('asphalt', (-HALF, -HALF, 0), (HALF, HALF, .05), 'asphalt')
    box('plinth', (-1.2, -1.9, .05), (.8, 1.9, .35), 'plinth')
    car('show', -.2, 0, 'car-blue')
    box('pole', (1.2, -.3, 0), (1.8, .3, 7.5), 'post')
    box('panel', (1.1, -.9, 5.0), (1.9, .9, 7.5), 'dealer')


# Garden centre: a glasshouse on a low wall, benches of plants, potted trees.

GLASSHOUSE = 4.2


def glasshouse_bay():
    """One bay of the glasshouse: a brick plinth, glazed walls and a glazed ridge roof."""
    box('plinth', (-HALF, FRONT, 0), (HALF, BACK, .6), 'pot')
    box('walls', (-HALF, FRONT, .6), (HALF, BACK, GLASSHOUSE), 'pane')
    box('mullion', (-HALF, FRONT - .05, .6), (-HALF + .12, FRONT, GLASSHOUSE), 'frame')
    ridge('roof', -HALF, HALF, FRONT, BACK, GLASSHOUSE, GLASSHOUSE + 2.2, 'pane')
    box('eave-front', (-HALF, FRONT - .1, GLASSHOUSE - .1), (HALF, FRONT + .1, GLASSHOUSE + .1), 'frame')
    box('eave-back', (-HALF, BACK - .1, GLASSHOUSE - .1), (HALF, BACK + .1, GLASSHOUSE + .1), 'frame')


def glasshouse_door():
    """The glasshouse's entrance bay: double doors under a gabled porch with a green sign."""
    glasshouse_bay()
    box('door', (-1.1, FRONT - .1, 0), (1.1, FRONT - .02, 2.5), 'frame')
    box('porch', (-1.6, FRONT - 2.0, 2.8), (1.6, FRONT, 3.1), 'frame')
    box('sign', (-1.5, FRONT - 2.05, 3.1), (1.5, FRONT - 1.9, 3.9), 'leaf')


def plant_bench():
    """One Tile of the plant yard: gravel and a slatted bench of potted plants."""
    box('gravel', (-HALF, -HALF, 0), (HALF, HALF, .05), 'gravel')
    box('bench', (-1.6, -.6, .5), (1.6, .6, .6), 'bench')
    box('leg-west', (-1.5, -.1, .05), (-1.3, .1, .5), 'bench')
    box('leg-east', (1.3, -.1, .05), (1.5, .1, .5), 'bench')
    box('plants', (-1.5, -.5, .6), (1.5, .5, .95), 'leaf')
    box('flowers', (-1.2, -.3, .95), (-.2, .3, 1.1), 'bloom')
    box('flowers-east', (.4, -.3, .95), (1.3, .3, 1.1), 'bloom')


def tree_pot():
    """A potted tree on gravel, for the yard's street corner."""
    box('gravel', (-HALF, -HALF, 0), (HALF, HALF, .05), 'gravel')
    box('pot', (-.6, -.6, .05), (.6, .6, .8), 'pot')
    box('trunk', (-.1, -.1, .8), (.1, .1, 2.2), 'bench')
    box('crown', (-1.1, -1.1, 2.0), (1.1, 1.1, 3.8), 'leaf')


# Builders' merchant: a steel warehouse with a roller shutter, timber racks, brick pallets.

WAREHOUSE = 8.0


def warehouse_bay():
    """One bay of the warehouse: profiled steel walls under a shallow pitched roof."""
    box('walls', (-HALF, FRONT, 0), (HALF, BACK, WAREHOUSE), 'steel')
    ridge('roof', -HALF, HALF, FRONT, BACK, WAREHOUSE, WAREHOUSE + 1.6, 'roof')
    box('band', (-HALF, FRONT - .05, WAREHOUSE - 1.0), (HALF, FRONT, WAREHOUSE - .6), 'merchant')


def warehouse_door():
    """The warehouse's trade entrance: a roller shutter and the merchant's name above it."""
    warehouse_bay()
    box('shutter', (-1.6, FRONT - .1, 0), (1.6, FRONT - .02, 4.5), 'shutter')
    box('sign', (-1.8, FRONT - .2, 5.2), (1.8, FRONT - .05, 6.6), 'merchant')


def timber_rack():
    """One Tile of the timber yard: concrete, a steel rack and stacked lengths of timber."""
    box('concrete', (-HALF, -HALF, 0), (HALF, HALF, .05), 'concrete')
    box('upright-west', (-1.8, -.1, .05), (-1.6, .1, 3.2), 'post')
    box('upright-east', (1.6, -.1, .05), (1.8, .1, 3.2), 'post')
    box('stack-low', (-1.9, -.7, .3), (1.9, .7, 1.1), 'timber')
    box('stack-high', (-1.9, -.6, 1.6), (1.9, .6, 2.3), 'timber')
    box('arm', (-1.9, -.8, 1.4), (1.9, .8, 1.5), 'post')


def brick_pallets():
    """Two wrapped pallets of bricks on concrete, for the yard's street corner."""
    box('concrete', (-HALF, -HALF, 0), (HALF, HALF, .05), 'concrete')
    box('pallet-west', (-1.7, -.6, .05), (-.5, .6, .2), 'pallet')
    box('bricks-west', (-1.6, -.5, .2), (-.6, .5, 1.1), 'brick')
    box('pallet-east', (.5, -.6, .05), (1.7, .6, .2), 'pallet')
    box('bricks-east', (.6, -.5, .2), (1.6, .5, 1.1), 'brick')


MODULES = {
    'showroom-bay': showroom_bay,
    'showroom-door': showroom_door,
    'car-lot': car_lot,
    'totem': totem,
    'glasshouse-bay': glasshouse_bay,
    'glasshouse-door': glasshouse_door,
    'plant-bench': plant_bench,
    'tree-pot': tree_pot,
    'warehouse-bay': warehouse_bay,
    'warehouse-door': warehouse_door,
    'timber-rack': timber_rack,
    'brick-pallets': brick_pallets,
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
(SOURCES / 'modules.json').write_text(json.dumps({'tile_metres': TILE, 'shed_deep_metres': SHED_DEEP,
                                                  'modules': records}, indent=2) + '\n')

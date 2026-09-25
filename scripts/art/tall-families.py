"""Monochrome blockouts of the stepped tower family: four point towers on a podium.

Run with Blender in the background:
  blender --background --factory-startup --python-exit-code 1 --python scripts/art/tall-families.py

A tower site is the simulation's Tower pattern on a 116 m block. The simulation counts a podium
over the whole site and a centred shaft half the site's width each way (BuildingPlan.Tower). The
body replaces that one shaft with four 29 m towers holding the same floor, so the four towers'
footprints sum to the shaft's. Each tower is a spine and a front block, interlocked in plan. The
front block stops lower, carries a planted terrace, and a recessed reveal storey marks the step
on the spine.

Each body's origin is its site centre at ground level. The street face looks down -Y, which Godot
receives as +Z. Faces are wound counter-clockwise seen from outside.
"""
import bpy
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCES = ROOT / 'art/tall-families'
EXPORT = ROOT / 'src/Borough.Godot/assets/tall-families'
STOREY = 3.5
SITE = 116
TOWER = 29
SPINE = 17
TOTAL_STOREYS = 62
PODIUM_STOREYS = (2, 5, 8)

# Each tower's share of the shaft's storeys. The shares average 1, so the four towers hold the
# shaft's floor, and differ so the group steps.
SHARES = (1.15, 0.85, 1.05, 0.95)
SPINE_RISE = 1.2

COLOURS = {'mass': 'd6d5cf', 'podium': 'c8c6be', 'reveal': '4a5055', 'base': '8f8e88',
           'terrace': 'b3b1a9', 'planter': '9a9890', 'tree': '7d8a72', 'screen': 'b9b8b1'}

materials = {}
parts = []


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
    materials.clear()
    for name, code in COLOURS.items():
        material = bpy.data.materials.new(name)
        material.use_nodes = True
        colour = rgb(code) + (1,)
        shader = material.node_tree.nodes['Principled BSDF']
        shader.inputs['Base Color'].default_value = colour
        shader.inputs['Roughness'].default_value = .3 if name == 'reveal' else .85
        material.diffuse_color = colour
        materials[name] = material


def mesh(name, vertices, faces, material, collection):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.materials.append(materials[material])
    data.update()
    thing = bpy.data.objects.new(name, data)
    collection.objects.link(thing)
    parts.append(thing)
    return thing


def outline(x0, y0, x1, y1, radius=0.0, rounded=(True, True, True, True), segments=4):
    """An anticlockwise rectangle seen from above. rounded names the corners SW, SE, NE, NW."""
    corners = [((x0, y0), (1, 1), 180), ((x1, y0), (-1, 1), 270),
               ((x1, y1), (-1, -1), 0), ((x0, y1), (1, -1), 90)]
    points = []
    for ((cx, cy), (sx, sy), start), round_it in zip(corners, rounded):
        if not round_it or radius <= 0:
            points.append((cx, cy))
            continue
        ox, oy = cx + sx * radius, cy + sy * radius
        for step in range(segments + 1):
            angle = math.radians(start + 90 * step / segments)
            points.append((ox + radius * math.cos(angle), oy + radius * math.sin(angle)))
    return points


def prism(name, points, z0, z1, material, collection):
    n = len(points)
    vertices = [(x, y, z0) for x, y in points] + [(x, y, z1) for x, y in points]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2 * n))]
    faces += [(i, (i + 1) % n, n + (i + 1) % n, n + i) for i in range(n)]
    return mesh(name, vertices, faces, material, collection)


def box(name, x0, y0, x1, y1, z0, z1, material, collection):
    return prism(name, outline(x0, y0, x1, y1), z0, z1, material, collection)


def ring(name, x0, y0, x1, y1, z0, z1, thick, material, collection):
    """Four thin walls round a rectangle, for parapets and plant screens."""
    box(f'{name}-s', x0, y0, x1, y0 + thick, z0, z1, material, collection)
    box(f'{name}-n', x0, y1 - thick, x1, y1, z0, z1, material, collection)
    box(f'{name}-w', x0, y0 + thick, x0 + thick, y1 - thick, z0, z1, material, collection)
    box(f'{name}-e', x1 - thick, y0 + thick, x1, y1 - thick, z0, z1, material, collection)


def tree(name, x, y, z, collection):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=1.6, location=(x, y, z + 2.2))
    crown = bpy.context.object
    crown.name = name
    crown.data.materials.append(materials['tree'])
    for owner in list(crown.users_collection):
        owner.objects.unlink(crown)
    collection.objects.link(crown)
    parts.append(crown)


def terrace(name, x0, y0, x1, y1, z, collection):
    """A planted roof: paving, a glass-height balustrade, planters along two edges and three trees."""
    box(f'{name}-paving', x0, y0, x1, y1, z, z + .2, 'terrace', collection)
    ring(f'{name}-rail', x0, y0, x1, y1, z + .2, z + 1.3, .12, 'screen', collection)
    box(f'{name}-planter-a', x0 + .6, y0 + .6, x1 - .6, y0 + 1.8, z + .2, z + .9, 'planter', collection)
    box(f'{name}-planter-b', x0 + .6, y1 - 1.8, x1 - .6, y1 - .6, z + .2, z + .9, 'planter', collection)
    for i, t in enumerate((.2, .5, .8)):
        tree(f'{name}-tree-{i}', x0 + (x1 - x0) * t, y0 + 1.2, z + .9, collection)


def storeys(target_floor, area):
    return max(1, round(target_floor / area))


def tower(name, x0, y0, spine_side, share, shaft_storeys, podium_top, collection):
    """One 29 m tower: a full-height spine on spine_side ('w', 'e', 's' or 'n') and a front block."""
    x1, y1 = x0 + TOWER, y0 + TOWER
    tower_floor = TOWER * TOWER * shaft_storeys * share
    spine_storeys = round(shaft_storeys * share * SPINE_RISE)
    spine_area = TOWER * SPINE
    front_area = TOWER * (TOWER - SPINE)
    front_storeys = storeys(tower_floor - spine_area * spine_storeys, front_area)

    # Spine and front rectangles, with the corners each rounds: only corners on the tower's outline.
    if spine_side == 'w':
        spine, front = (x0, y0, x0 + SPINE, y1), (x0 + SPINE, y0, x1, y1)
        spine_round, front_round = (True, False, False, True), (False, True, True, False)
    elif spine_side == 'e':
        spine, front = (x1 - SPINE, y0, x1, y1), (x0, y0, x1 - SPINE, y1)
        spine_round, front_round = (False, True, True, False), (True, False, False, True)
    elif spine_side == 's':
        spine, front = (x0, y0, x1, y0 + SPINE), (x0, y0 + SPINE, x1, y1)
        spine_round, front_round = (True, True, False, False), (False, False, True, True)
    else:
        spine, front = (x0, y1 - SPINE, x1, y1), (x0, y0, x1, y1 - SPINE)
        spine_round, front_round = (False, False, True, True), (True, True, False, False)

    radius = 2.5
    front_top = podium_top + front_storeys * STOREY
    spine_top = podium_top + spine_storeys * STOREY
    prism(f'{name}-front', outline(*front, radius, front_round), podium_top, front_top, 'mass', collection)
    prism(f'{name}-spine-low', outline(*spine, radius, spine_round), podium_top, front_top, 'mass', collection)
    inset = .8
    sx0, sy0, sx1, sy1 = spine
    prism(f'{name}-reveal', outline(sx0 + inset, sy0 + inset, sx1 - inset, sy1 - inset, max(0, radius - inset), spine_round),
          front_top, front_top + STOREY, 'reveal', collection)
    prism(f'{name}-spine-high', outline(*spine, radius, spine_round), front_top + STOREY, spine_top, 'mass', collection)
    ring(f'{name}-screen', sx0 + 1.5, sy0 + 1.5, sx1 - 1.5, sy1 - 1.5, spine_top, spine_top + 3.5, .3, 'screen', collection)
    fx0, fy0, fx1, fy1 = front
    terrace(f'{name}-terrace', fx0 + .4, fy0 + .4, fx1 - .4, fy1 - .4, front_top, collection)

    floor = spine_area * spine_storeys + front_area * front_storeys
    return {'spine_storeys': spine_storeys, 'front_storeys': front_storeys, 'floor': floor,
            'top_metres': spine_top + 3.5}


def site(podium_storeys, offset_x, collection):
    """The whole tower site, centred on (offset_x, 0)."""
    half = SITE / 2
    podium_top = podium_storeys * STOREY
    shaft_storeys = TOTAL_STOREYS - podium_storeys
    name = f'p{podium_storeys}'
    ox = offset_x

    prism(f'{name}-ground', outline(ox - half + 1.2, -half + 1.2, ox + half - 1.2, half - 1.2, 1), 0, STOREY,
          'base', collection)
    prism(f'{name}-podium', outline(ox - half, -half, ox + half, half, 2), STOREY, podium_top, 'podium', collection)
    ring(f'{name}-parapet', ox - half + 1, -half + 1, ox + half - 1, half - 1, podium_top, podium_top + 1.1, .3,
         'podium', collection)
    box(f'{name}-podium-roof', ox - half + 1.3, -half + 1.3, ox + half - 1.3, half - 1.3, podium_top, podium_top + .2,
        'terrace', collection)

    near, far = -half + 14, half - 14 - TOWER
    layout = [(near, near, 'n'), (far, near, 'e'), (far, far, 'n'), (near, far, 'w')]
    towers = []
    for i, ((tx, ty, side), share) in enumerate(zip(layout, SHARES)):
        towers.append(tower(f'{name}-t{i}', ox + tx, ty, side, share, shaft_storeys, podium_top, collection))
        planter_y = ty - 3 if ty < 0 else ty + TOWER + 1
        box(f'{name}-t{i}-podium-planter', ox + tx, planter_y, ox + tx + TOWER, planter_y + 2,
            podium_top + .2, podium_top + .9, 'planter', collection)

    simulated = SITE * SITE * podium_storeys + (SITE // 2) ** 2 * shaft_storeys
    drawn = SITE * SITE * podium_storeys + sum(t['floor'] for t in towers)
    return {'podium_storeys': podium_storeys, 'towers': towers, 'simulated_floor': simulated,
            'drawn_floor': drawn, 'difference_percent': round(100 * (drawn - simulated) / simulated, 2)}


def main():
    SOURCES.mkdir(parents=True, exist_ok=True)
    EXPORT.mkdir(parents=True, exist_ok=True)
    reports = []
    for podium_storeys in PODIUM_STOREYS:
        reset()
        parts.clear()
        collection = bpy.context.scene.collection
        reports.append(site(podium_storeys, 0, collection))
        name = f'stepped-towers-p{podium_storeys}'
        bpy.ops.wm.save_as_mainfile(filepath=str(SOURCES / f'{name}.blend'))
        bpy.ops.export_scene.gltf(filepath=str(EXPORT / f'{name}.glb'), export_format='GLB')

    reset()
    parts.clear()
    collection = bpy.context.scene.collection
    for i, podium_storeys in enumerate(PODIUM_STOREYS):
        site(podium_storeys, (i - 1) * (SITE + 40), collection)
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCES / 'stepped-towers-lineup.blend'))

    for report in reports:
        print('TALL', report)


main()

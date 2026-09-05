"""Run with Blender --background --factory-startup --python scripts/art/facade.py."""
import argparse
import hashlib
import json
import math
from pathlib import Path
import sys
import bpy

root = Path(__file__).resolve().parents[2]
p = argparse.ArgumentParser()
p.add_argument('--width', type=float, default=4.0)
p.add_argument('--wall', default='b9afa0')
p.add_argument('--treatment', choices=['blockout', 'material-rich'], default='blockout')
a = p.parse_args(sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else [])
if not 2.0 <= a.width <= 6.0 or len(a.wall) != 6:
    raise ValueError('width must be 2–6 metres; wall must be six hex digits')
rgb = [int(a.wall[i:i+2], 16) / 255 for i in (0, 2, 4)]
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
bpy.context.scene.unit_settings.system = 'METRIC'
bpy.context.scene.unit_settings.scale_length = 1

def material(name, color, roughness):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    linear = [v / 12.92 if v <= .04045 else ((v + .055) / 1.055) ** 2.4 for v in color]
    bsdf.inputs['Base Color'].default_value = (*linear, 1)
    bsdf.inputs['Roughness'].default_value = roughness
    return m

rich = a.treatment == 'material-rich'
wall = material('wall', rgb, .85)
trim = material('trim', (.75, .72, .65), .7)
glass = material('glazing', (.075, .13, .16), .25)
metal = material('metal', (.25, .28, .29), .32) if rich else None
if rich:
    metal.node_tree.nodes.get('Principled BSDF').inputs['Metallic'].default_value = .65
    # Two metres per repeat: 8 bricks across, 24 courses, running bond.
    n = 512
    heights, roughness = [], []
    for y in range(n):
        course = y * 24 / n
        row = int(course)
        for x in range(n):
            brick = x * 8 / n + (row % 2) * .5
            u, v = brick % 1, course % 1
            edge = min(u * .25, (1-u) * .25, v / 12, (1-v) / 12)
            grain = ((x * 73856093 ^ y * 19349663) % 997) / 997
            heights.append(min(1, max(0, (edge - .002) / .005)) * .0018 + (grain-.5)*.0002)
            roughness.append(.8 + grain * .1 if edge > .004 else .96)
    normal_pixels, rough_pixels = [], []
    for y in range(n):
        for x in range(n):
            dx = (heights[y*n+(x+1)%n] - heights[y*n+(x-1)%n]) * n / 4
            dy = (heights[((y+1)%n)*n+x] - heights[((y-1)%n)*n+x]) * n / 4
            length = math.sqrt(dx*dx+dy*dy+1)
            normal_pixels.extend((.5-dx/length*.5, .5-dy/length*.5, .5+1/length*.5, 1))
            r = roughness[y*n+x]
            rough_pixels.extend((r, r, r, 1))
    nodes, links = wall.node_tree.nodes, wall.node_tree.links
    bsdf = nodes.get('Principled BSDF')
    for label, pixels in [('masonry-normal', normal_pixels), ('masonry-roughness', rough_pixels)]:
        image = bpy.data.images.new(label, n, n, alpha=False)
        image.colorspace_settings.name = 'Non-Color'
        image.pixels.foreach_set(pixels)
        image.filepath_raw = str(root / 'art/visual-study' / (label + '.png'))
        image.file_format = 'PNG'
        image.save()
        image.pack()
        texture = nodes.new('ShaderNodeTexImage')
        texture.image = image
        if label.endswith('normal'):
            normal = nodes.new('ShaderNodeNormalMap')
            links.new(texture.outputs['Color'], normal.inputs['Color'])
            links.new(normal.outputs['Normal'], bsdf.inputs['Normal'])
        else:
            links.new(texture.outputs['Color'], bsdf.inputs['Roughness'])

def box(name, center, size, mat):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    ob = bpy.context.object
    ob.name = name
    ob.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    ob.data.materials.append(mat)
    if rich:
        uv = ob.data.uv_layers.active.data
        for poly in ob.data.polygons:
            axis = max(range(3), key=lambda i: abs(poly.normal[i]))
            for loop in poly.loop_indices:
                point = ob.matrix_world @ ob.data.vertices[ob.data.loops[loop].vertex_index].co
                u, v = ((point.x, point.z) if axis == 1 else
                        (point.y, point.z) if axis == 0 else (point.x, point.y))
                uv[loop].uv = (u / 2, v / 2)
        if mat != wall:
            bevel = ob.modifiers.new('edge_highlights', 'BEVEL')
            bevel.width = .008
            bevel.segments = 2
            bpy.ops.object.modifier_apply(modifier=bevel.name)
    return ob

# Front faces Blender -Y; exported front is Godot +Z. Bottom origin, 3.5 m storey.
w = a.width
box('left_pier', (-(w+1.4)/4, 0, 1.75), ((w-1.4)/2, .3, 3.5), wall)
box('right_pier', ((w+1.4)/4, 0, 1.75), ((w-1.4)/2, .3, 3.5), wall)
box('below_window', (0, 0, .5), (1.4, .3, 1), wall)
box('above_window', (0, 0, 3.1), (1.4, .3, .8), wall)
box('window', (0, .02, 1.85), (1.4, .06, 1.7), glass)
box('sill', (0, -.1, .98), (1.6, .48, .12), trim)
box('mullion', (0, -.06, 1.85), (.07, .14, 1.7), trim)
box('head', (0, -.08, 2.7), (1.6, .4, .12), trim)
if rich:
    for x in (-.67, .67):
        box('frame_jamb', (x, -.065, 1.85), (.065, .15, 1.7), trim)
    for z in (1.04, 2.66):
        box('frame_rail', (0, -.065, z), (1.34, .15, .065), trim)
    box('transom', (0, -.09, 2.24), (1.34, .1, .045), trim)
    box('sill_drip', (0, -.32, .96), (1.5, .035, .022), metal)
    box('head_flashing', (0, -.24, 2.775), (1.6, .035, .018), metal)
for ob in bpy.context.scene.objects:
    assert ob.type == 'MESH'
    assert all(poly.area > 0 for poly in ob.data.polygons)
    assert all(abs(poly.normal.length - 1) < 1e-5 for poly in ob.data.polygons)
stem = 'facade-material-rich' if rich else 'facade'
source = root / 'art/visual-study' / (stem + '.blend')
export = root / 'src/Borough.Godot/assets/visual-study' / (stem + '.glb')
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(source))
bpy.ops.export_scene.gltf(filepath=str(export), export_format='GLB', export_yup=True,
                          export_apply=True, export_materials='EXPORT', export_tangents=rich, use_selection=False)
manifest = dict(authority='scripts/art/facade.py', blender=bpy.app.version_string,
    script_sha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
    units='metres', blender_up='+Z', godot_up='+Y', godot_front='+Z', origin='bottom centre',
    width=w, height=3.5, wall_rgb=rgb, treatment=a.treatment,
    material_slots=['wall', 'trim', 'glazing'] + (['metal'] if rich else []),
    textures=['masonry-normal.png', 'masonry-roughness.png'] if rich else [],
    uv_repeat_metres=2 if rich else None,
    export=dict(format='GLB', yup=True, apply=True, materials='EXPORT'),
    source=str(source.relative_to(root)), asset=str(export.relative_to(root)))
(root / 'art/visual-study' / (stem + '.json')).write_text(json.dumps(manifest, indent=2)+'\n')
print('FACADE_EXPORT_OK')

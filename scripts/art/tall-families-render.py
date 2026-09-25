"""Renders the tower family lineup for review. Run with Blender in the background:
  blender --background art/tall-families/lineup.blend --python scripts/art/tall-families-render.py -- OUT_DIR

The lineup stands point, stepped-point, l and h west to east, 176 m apart, each on a 5-storey podium.
"""
import bpy
import sys
from pathlib import Path
from mathutils import Vector

out = Path(sys.argv[sys.argv.index('--') + 1])
out.mkdir(parents=True, exist_ok=True)
scene = bpy.context.scene
scene.render.engine = 'BLENDER_EEVEE'
scene.eevee.taa_render_samples = 16
scene.view_settings.view_transform = 'AgX'

world = bpy.data.worlds.new('sky')
world.use_nodes = True
world.node_tree.nodes['Background'].inputs['Color'].default_value = (.55, .65, .8, 1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value = .45
scene.world = world
sun = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN'))
sun.data.energy = 5.5
sun.data.angle = .02
sun.rotation_euler = (Vector((-.45, .55, -.7)).to_track_quat('-Z', 'Y')).to_euler()
scene.collection.objects.link(sun)

bpy.ops.mesh.primitive_plane_add(size=4000, location=(0, 0, -.02))
ground = bpy.data.materials.new('ground')
ground.use_nodes = True
ground.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (.12, .14, .11, 1)
bpy.context.object.data.materials.append(ground)

cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam'))
scene.collection.objects.link(cam)
scene.camera = cam

views = {
    'aerial': ((-380, -760, 460), (0, 0, 110), 2000, 1200, 30),
    'elevation': ((0, -1200, 150), (0, 0, 150), 2000, 1000, 0),
    'street': ((-70, -80, 1.7), (-88, 0, 70), 1200, 1500, 16),
    'podium': ((-40, -130, 30), (-88, -40, 12), 1600, 1000, 35),
    'crowns': ((-60, -230, 330), (-176, 0, 225), 1600, 1000, 30),
}
for name, (location, target, width, height, lens) in views.items():
    cam.location = Vector(location)
    cam.rotation_euler = (Vector(target) - cam.location).to_track_quat('-Z', 'Y').to_euler()
    if lens:
        cam.data.type, cam.data.lens = 'PERSP', lens
    else:
        cam.data.type, cam.data.ortho_scale = 'ORTHO', 760
    cam.data.clip_end = 6000
    scene.render.resolution_x, scene.render.resolution_y = width, height
    scene.render.filepath = str(out / f'{name}.png')
    bpy.ops.render.render(write_still=True)

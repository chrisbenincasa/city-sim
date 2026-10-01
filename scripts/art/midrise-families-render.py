"""Renders the mid-rise family lineup for review, dressed and in monochrome. Run with Blender in the background:
  blender --background art/midrise-families/lineup.blend --python scripts/art/midrise-families-render.py -- OUT_DIR

The front row stands the mansion blocks west to east: a terrace of three attached bodies, a deep
back-to-back body and two rings. The back row, 140 m north, stands the courtyard face, the
courtyard flank and the slab ring.
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

grey = bpy.data.materials.new('monochrome')
grey.use_nodes = True
grey.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (.6, .6, .6, 1)
grey.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = .8

cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam'))
scene.collection.objects.link(cam)
scene.camera = cam

views = {
    'aerial': ((-120, -330, 260), (190, 80, 0), 2000, 1200, 30),
    'terrace': ((70, -42, 1.7), (64, 0, 14), 1600, 1000, 16),
    'rings': ((330, -110, 150), (325, 10, 0), 1600, 1000, 30),
    'slab-street': ((80, 55, 8), (80, 140, 28), 1600, 1000, 16),
    'slab-yard': ((314, 95, 110), (314, 140, 18), 1600, 1000, 28),
    'mansion-variants': ((540, -120, 50), (540, 0, 10), 1600, 1000, 22),
    'slab-variants': ((505, 30, 40), (505, 140, 25), 1600, 1000, 22),
}
for monochrome in (False, True):
    scene.view_layers[0].material_override = grey if monochrome else None
    for name, (location, target, width, height, lens) in views.items():
        cam.location = Vector(location)
        cam.rotation_euler = (Vector(target) - cam.location).to_track_quat('-Z', 'Y').to_euler()
        cam.data.type, cam.data.lens = 'PERSP', lens
        cam.data.clip_end = 6000
        scene.render.resolution_x, scene.render.resolution_y = width, height
        scene.render.filepath = str(out / f'{name}{"-mono" if monochrome else ""}.png')
        bpy.ops.render.render(write_still=True)

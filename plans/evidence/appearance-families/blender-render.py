"""Renders an authored test-street body for a side-by-side. Run with Blender in the background:
  blender --background art/test-street/rowhouses.blend --python plans/evidence/appearance-families/blender-render.py -- OUT.png
"""
import bpy, math, sys
from mathutils import Vector
out = sys.argv[sys.argv.index('--') + 1]
scene = bpy.context.scene
scene.render.engine = 'BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items] else 'BLENDER_EEVEE'
scene.render.resolution_x, scene.render.resolution_y = 1600, 900
cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam'))
scene.collection.objects.link(cam)
cam.location = Vector((-26, -30, 16))
cam.rotation_euler = (Vector((0, 0, 3.5)) - cam.location).to_track_quat('-Z', 'Y').to_euler()
cam.data.lens = 40
scene.camera = cam
sun = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN'))
sun.data.energy = 4
sun.rotation_euler = (math.radians(50), 0, math.radians(-30))
scene.collection.objects.link(sun)
world = bpy.data.worlds.new('w'); scene.world = world; world.use_nodes = True
world.node_tree.nodes['Background'].inputs['Color'].default_value = (.6, .7, .8, 1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value = .8
bpy.ops.mesh.primitive_plane_add(size=200, location=(0, 0, -.01))
ground = bpy.data.materials.new('ground'); ground.use_nodes = True
ground.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (.12, .25, .08, 1)
bpy.context.object.data.materials.append(ground)
scene.render.filepath = out
bpy.ops.render.render(write_still=True)

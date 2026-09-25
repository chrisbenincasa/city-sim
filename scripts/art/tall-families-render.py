"""Renders the stepped tower lineup for review. Run with Blender in the background:
  blender --background art/tall-families/stepped-towers-lineup.blend --python scripts/art/tall-families-render.py -- OUT_DIR
"""
import bpy
import math
import sys
from pathlib import Path
from mathutils import Vector

out = Path(sys.argv[sys.argv.index('--') + 1])
out.mkdir(parents=True, exist_ok=True)
scene = bpy.context.scene
scene.render.engine = 'BLENDER_WORKBENCH'
shading = scene.display.shading
shading.light = 'STUDIO'
shading.color_type = 'MATERIAL'
shading.show_shadows = True
shading.show_cavity = True
shading.cavity_type = 'WORLD'
scene.display.light_direction = (0.45, -0.55, 0.7)
scene.display.shadow_shift = 0.1
bpy.ops.mesh.primitive_plane_add(size=2000, location=(0, 0, -.02))
ground = bpy.data.materials.new('ground')
ground.diffuse_color = (.32, .33, .31, 1)
bpy.context.object.data.materials.append(ground)

cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam'))
scene.collection.objects.link(cam)
scene.camera = cam

views = {
    'aerial': ((-420, -760, 420), (0, 0, 130), 1800, 1200, 35),
    'street': ((-10, -95, 1.7), (0, 0, 120), 1000, 1400, 16),
    'elevation': ((0, -900, 165), (0, 0, 165), 1800, 1400, 0),
}
for name, (location, target, width, height, lens) in views.items():
    cam.location = Vector(location)
    cam.rotation_euler = (Vector(target) - cam.location).to_track_quat('-Z', 'Y').to_euler()
    if lens:
        cam.data.type, cam.data.lens = 'PERSP', lens
    else:
        cam.data.type, cam.data.ortho_scale = 'ORTHO', 470
    cam.data.clip_end = 5000
    scene.render.resolution_x, scene.render.resolution_y = width, height
    scene.render.filepath = str(out / f'{name}.png')
    bpy.ops.render.render(write_still=True)

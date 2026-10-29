"""Snap-lock standing-seam metal roofing, modeled at product dimensions and baked to a tiling texture.

Run with Blender in the background:
  blender --background --factory-startup --python-exit-code 1 --python scripts/art/standing-seam.py

The tile is 4.8768 m (192 in) square and holds twelve 16 in (406.4 mm) pans. Each seam stands 1.5 in
(38.1 mm) tall and 1 in (25.4 mm) wide with a rounded cap. Two 3 mm stiffening ribs run down each pan,
and each pan bows up to 3.5 mm between its seams. Seams run along Blender Y, which is up-slope and the
image's vertical. Each pan takes its own tone and gloss, as sheets from different coils do.

The maps go to art/materials/, and scripts/art/materials.py adds them to the texture library.
"""
import math
from pathlib import Path

import bmesh
import bpy
import numpy as np

OUT = Path(__file__).resolve().parents[2] / 'art/materials'
INCH = .0254
PAN = 16 * INCH
PANS = 12
TILE = PAN * PANS
SEAM_H = 1.5 * INCH
SEAM_W = 1.0 * INCH
RIB_H = .003
RIB_W = .02
PIXELS = 1024
SAMPLES = 64
PAINT_MEAN = .7
PAN_TONE = (1.0, .96, 1.04, .985, 1.02, .955, 1.03, .99, .965, 1.045, 1.0, .975)
PAN_GLOSS = (.36, .44, .32, .40, .47, .34, .42, .38, .45, .33, .41, .37)
CAN = .0035
STEP = .04


def profile():
    """(x, z) points across one pan, seam centered on x = 0 and x = PAN."""
    pts = []
    h = SEAM_W / 2
    for i in range(9):
        a = math.pi / 2 * (1 - i / 8)
        pts.append((h * math.cos(a) * .9, SEAM_H - h * .4 * (1 - math.sin(a))))
    pts += [(h, SEAM_H * .55), (h + .004, .002), (h + .012, 0)]
    for c in (PAN / 3, 2 * PAN / 3):
        for k in range(7):
            t = k / 6
            pts.append((c - RIB_W / 2 + t * RIB_W, RIB_H * math.sin(math.pi * t)))
    tail = [(PAN - x, z) for x, z in reversed(pts[:12])]
    return pts + tail


def build():
    across = []
    for i in range(PANS + 2):
        for x, z in profile():
            across.append(((i - 1) * PAN + x, z))
    bm = bmesh.new()
    n = int(round(TILE / STEP))
    ys = [k * TILE / n for k in range(-2, n + 3)]

    def can(x, y):
        i = int(math.floor(x / PAN))
        t = x / PAN - i
        edge = SEAM_W / 2 + .015
        if t * PAN < edge or (1 - t) * PAN < edge:
            return 0.0
        k = i % PANS
        phase = k * 2.399
        waves = 2 + k % 3
        bulge = math.sin(math.pi * (t * PAN - edge) / (PAN - 2 * edge))
        return CAN * (.55 + .45 * math.sin(2 * math.pi * waves * y / TILE + phase)) * bulge * (1 if k % 2 else -.7)

    rows = [[bm.verts.new((x, y, z + can(x, y))) for x, z in across] for y in ys]
    for r in range(len(ys) - 1):
        for j in range(len(across) - 1):
            bm.faces.new((rows[r][j], rows[r][j + 1], rows[r + 1][j + 1], rows[r + 1][j]))
    mesh = bpy.data.meshes.new('seam-high')
    bm.to_mesh(mesh)
    return bpy.data.objects.new('seam-high', mesh)


def target():
    mesh = bpy.data.meshes.new('seam-bake')
    mesh.from_pydata([(0, 0, 0), (TILE, 0, 0), (TILE, TILE, 0), (0, TILE, 0)], [], [(0, 1, 2, 3)])
    uv = mesh.uv_layers.new(name='UVMap')
    for loop, (u, v) in zip(uv.data, ((0, 0), (1, 0), (1, 1), (0, 1))):
        loop.uv = (u, v)
    return bpy.data.objects.new('seam-bake', mesh)


def material():
    mat = bpy.data.materials.new('painted-steel')
    tree = mat.node_tree
    bsdf = tree.nodes['Principled BSDF']
    coords = tree.nodes.new('ShaderNodeTexCoord')
    noise = tree.nodes.new('ShaderNodeTexNoise')
    noise.inputs['Scale'].default_value = 60
    noise.inputs['Detail'].default_value = 6
    tree.links.new(coords.outputs['Object'], noise.inputs['Vector'])
    ramp = tree.nodes.new('ShaderNodeMapRange')
    ramp.inputs['To Min'].default_value = .32
    ramp.inputs['To Max'].default_value = .46
    tree.links.new(noise.outputs['Fac'], ramp.inputs['Value'])
    tree.links.new(ramp.outputs['Result'], bsdf.inputs['Roughness'])
    bsdf.inputs['Base Color'].default_value = (.7, .7, .7, 1)
    return mat


def bake(kind, low, **extra):
    image = bpy.data.images.new(kind, PIXELS, PIXELS, alpha=False, float_buffer=True)
    tree = low.data.materials[0].node_tree
    tex = tree.nodes.get('bake-image') or tree.nodes.new('ShaderNodeTexImage')
    tex.name = 'bake-image'
    tex.image = image
    tree.nodes.active = tex
    bpy.ops.object.bake(type=kind, use_selected_to_active=True, cage_extrusion=.06, max_ray_distance=.12,
                        margin=0, use_clear=True, **extra)
    px = np.empty(PIXELS * PIXELS * 4, dtype=np.float32)
    image.pixels.foreach_get(px)
    return px.reshape(PIXELS, PIXELS, 4)


def write(path, arr):
    image = bpy.data.images.new(path.stem, PIXELS, PIXELS, alpha=False, float_buffer=False)
    image.colorspace_settings.name = 'Non-Color'
    rgba = np.ones((PIXELS, PIXELS, 4), dtype=np.float32)
    rgba[..., :3] = arr if arr.ndim == 3 else arr[..., None]
    image.pixels.foreach_set(rgba.ravel())
    s = bpy.context.scene.render.image_settings
    s.file_format, s.color_mode, s.color_depth = 'PNG', 'RGB' if arr.ndim == 3 else 'BW', '8'
    image.save_render(str(path), scene=bpy.context.scene)


def srgb(linear):
    linear = np.clip(linear, 0, 1)
    return np.where(linear <= .0031308, linear * 12.92, 1.055 * np.power(linear, 1 / 2.4) - .055)


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.device = 'CPU'
    scene.cycles.samples = SAMPLES
    scene.world = bpy.data.worlds.new('bake')
    scene.world.light_settings.distance = .05
    scene.view_settings.view_transform = 'Standard'
    high, low = build(), target()
    for o in (high, low):
        scene.collection.objects.link(o)
    high.data.materials.append(material())
    holder = bpy.data.materials.new('bake-target')
    low.data.materials.append(holder)
    bpy.ops.object.select_all(action='DESELECT')
    high.select_set(True)
    low.select_set(True)
    bpy.context.view_layer.objects.active = low

    normal = bake('NORMAL', low, normal_space='TANGENT')[..., :3]
    rough = bake('ROUGHNESS', low)[..., 0]
    ao = bake('AO', low)[..., 0]

    x = (np.arange(PIXELS) + .5) / PIXELS * TILE
    pan = np.clip((x / PAN).astype(int), 0, PANS - 1)
    tone = np.array(PAN_TONE)[pan][None, :]
    albedo = (.30 + .70 * ao ** 1.5) * tone
    albedo = albedo * PAINT_MEAN / albedo.mean()
    OUT.mkdir(parents=True, exist_ok=True)
    write(OUT / 'standing-seam-albedo.png', srgb(np.repeat(albedo[..., None], 3, axis=2)))
    write(OUT / 'standing-seam-normal.png', normal)
    gloss = np.array(PAN_GLOSS)[pan][None, :]
    rough = np.clip(gloss + (rough - rough.mean()) * 1.5 + (1 - ao) * .25, 0, 1)
    write(OUT / 'standing-seam-roughness.png', rough)
    print('SEAM', TILE, float(albedo.mean()), float(srgb(albedo).std()))


main()

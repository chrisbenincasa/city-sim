"""Laminated asphalt roof shingles, modelled at product dimensions and baked to a tiling texture.

Run with Blender in the background:
  blender --background --factory-startup --python-exit-code 1 --python scripts/art/asphalt-shingles.py

The tile is 4 m square and holds 28 courses at the 5 5/8 in (142.9 mm) exposure of US laminated
("architectural") shingles, four 1 m shingles per course. Each course sits on the one below, so the
roof surface is a sawtooth whose step is one shingle's thickness. Blender Y is up-slope and each
course's butt edge faces -Y.

The script measures the baked height map and exits 1 if the course pitch misses the specification.
"""
import bpy
import bmesh
import hashlib
import json
import math
import random
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'art/materials/asphalt-shingles.blend'
EXPORT = ROOT / 'src/Borough.Godot/assets/city/roofing'
NAME = 'asphalt-shingles'

INCH = .0254
EXPOSURE_SPEC = 5.625 * INCH
TILE = 4.0
COURSES = 28
EXPOSURE = TILE / COURSES
LENGTH = 1.0
PIXELS = 2048
SEED = 5625

BACKER = .0032
LAMINATE = .0028
THICKNESS = BACKER + LAMINATE
END_GAP = .003
TAB_WIDTH = (.09, .26)
CUTOUT_WIDTH = (.025, .085)
CUTOUT_REACH = (.50, .78)
LEAST_STAGGER = .15

BUTT_BEVEL = .003
SIDE_BEVEL = .0015

HEIGHT_RANGE = .016
SAMPLES = 24

assert abs(EXPOSURE - EXPOSURE_SPEC) / EXPOSURE_SPEC < .001, 'tile does not hold whole courses at spec'


def layout(rng):
    offsets = [rng.uniform(0, LENGTH)]
    while len(offsets) < COURSES:
        o = (offsets[-1] + rng.uniform(LEAST_STAGGER, LENGTH - LEAST_STAGGER)) % LENGTH
        wrap = abs(((offsets[0] - o) + LENGTH / 2) % LENGTH - LENGTH / 2)
        if len(offsets) < COURSES - 1 or wrap >= LEAST_STAGGER:
            offsets.append(o)
    shingles = []
    for _ in range(COURSES):
        row = []
        for _ in range(round(TILE / LENGTH)):
            x, pieces = END_GAP / 2, []
            while x < LENGTH - END_GAP / 2:
                tab = min(rng.uniform(*TAB_WIDTH), LENGTH - END_GAP / 2 - x)
                pieces.append(('tab', x, x + tab, rng.random()))
                x += tab
                if x >= LENGTH - END_GAP / 2 - CUTOUT_WIDTH[0]:
                    break
                cut = rng.uniform(*CUTOUT_WIDTH)
                pieces.append(('cut', x, x + cut, rng.uniform(*CUTOUT_REACH)))
                x += cut
            row.append((pieces, rng.random()))
        shingles.append(row)
    return offsets, shingles


def build(offsets, shingles):
    mesh = bpy.data.meshes.new('shingles-high')
    bm = bmesh.new()
    tone = bm.faces.layers.float.new('tone')
    layer = bm.faces.layers.float.new('layer')

    def under(v):
        return THICKNESS * (1 - v)

    def prism(x0, x1, y0, v0, v1, lift, t, which):
        ya, yb = y0 + v0 * EXPOSURE, y0 + v1 * EXPOSURE
        za, zb = under(v0) + lift, under(v1) + lift
        ti, tx0, tx1 = ya + BUTT_BEVEL, x0 + SIDE_BEVEL, x1 - SIDE_BEVEL
        zi = za + (zb - za) * BUTT_BEVEL / (yb - ya)
        top = [bm.verts.new(p) for p in ((tx0, ti, zi), (tx1, ti, zi), (tx1, yb, zb), (tx0, yb, zb))]
        low = [bm.verts.new(p) for p in ((x0, ya, za - lift - .002), (x1, ya, za - lift - .002),
                                         (x1, yb, zb - lift - .002), (x0, yb, zb - lift - .002))]
        faces = [bm.faces.new(top), bm.faces.new((low[0], low[1], top[1], top[0])),
                 bm.faces.new((low[3], low[0], top[0], top[3])), bm.faces.new((low[1], low[2], top[2], top[1]))]
        for f in faces:
            f[tone], f[layer] = t, which

    for k in range(-1, COURSES + 1):
        course, y0 = k % COURSES, k * EXPOSURE
        first = offsets[course] - LENGTH * math.ceil((offsets[course] + LENGTH) / LENGTH)
        j = 0
        while first + j * LENGTH < TILE + LENGTH:
            sx = first + j * LENGTH
            index = round((sx - offsets[course]) / LENGTH) % len(shingles[course])
            pieces, backer_tone = shingles[course][index]
            prism(sx + END_GAP / 2, sx + LENGTH - END_GAP / 2, y0, 0, 1, BACKER, backer_tone, 0)
            for kind, a, b, value in pieces:
                if kind == 'tab':
                    prism(sx + a, sx + b, y0, 0, 1, THICKNESS, value, 1)
                else:
                    prism(sx + a, sx + b, y0, value, 1, THICKNESS, backer_tone, 1)
            j += 1

    bm.to_mesh(mesh)
    bm.free()
    return bpy.data.objects.new('shingles-high', mesh)


def node(tree, kind, location, **inputs):
    n = tree.nodes.new(kind)
    n.location = location
    for key, value in inputs.items():
        n.inputs[key].default_value = value
    return n


def torus(tree):
    """Maps object XY onto a flat 4D torus so every noise tiles with the 4 m tile."""
    coords = node(tree, 'ShaderNodeTexCoord', (-1400, 0))
    split = node(tree, 'ShaderNodeSeparateXYZ', (-1200, 0))
    tree.links.new(coords.outputs['Object'], split.inputs[0])
    radius = TILE / (2 * math.pi)
    parts = []
    for axis in ('X', 'Y'):
        angle = node(tree, 'ShaderNodeMath', (-1000, 0))
        angle.operation = 'MULTIPLY'
        angle.inputs[1].default_value = 2 * math.pi / TILE
        tree.links.new(split.outputs[axis], angle.inputs[0])
        for fn in ('COSINE', 'SINE'):
            trig = node(tree, 'ShaderNodeMath', (-800, 0))
            trig.operation = fn
            tree.links.new(angle.outputs[0], trig.inputs[0])
            scaled = node(tree, 'ShaderNodeMath', (-600, 0))
            scaled.operation = 'MULTIPLY'
            scaled.inputs[1].default_value = radius
            tree.links.new(trig.outputs[0], scaled.inputs[0])
            parts.append(scaled.outputs[0])
    vector = node(tree, 'ShaderNodeCombineXYZ', (-400, 0))
    for socket, part in zip(vector.inputs, parts[:3]):
        tree.links.new(part, socket)
    return vector.outputs[0], parts[3]


def material():
    mat = bpy.data.materials.new('asphalt-shingles')
    tree = mat.node_tree
    tree.nodes.clear()
    vector, w = torus(tree)

    def noise4(kind, scale, location, **settings):
        n = node(tree, kind, location)
        if kind == 'ShaderNodeTexVoronoi':
            n.voronoi_dimensions = '4D'
        else:
            n.noise_dimensions = '4D'
        for key, value in settings.items():
            n.inputs[key].default_value = value
        n.inputs['Scale'].default_value = scale
        tree.links.new(vector, n.inputs['Vector'])
        tree.links.new(w, n.inputs['W'])
        return n

    granule = noise4('ShaderNodeTexVoronoi', 1 / .0016, (-200, 300))
    mottle = noise4('ShaderNodeTexNoise', 1 / .03, (-200, 0), Detail=3.0)
    tone = node(tree, 'ShaderNodeAttribute', (-200, -300))
    tone.attribute_type, tone.attribute_name = 'GEOMETRY', 'tone'
    which = node(tree, 'ShaderNodeAttribute', (-200, -450))
    which.attribute_type, which.attribute_name = 'GEOMETRY', 'layer'

    grain = node(tree, 'ShaderNodeSeparateColor', (0, 300))
    tree.links.new(granule.outputs['Color'], grain.inputs[0])

    script = [
        ('tab', 'MULTIPLY_ADD', tone.outputs['Fac'], .44, .78),
        ('granule', 'MULTIPLY_ADD', grain.outputs['Red'], .50, .75),
        ('mottle', 'MULTIPLY_ADD', mottle.outputs['Fac'], .20, .90),
        ('layer', 'MULTIPLY_ADD', which.outputs['Fac'], .50, .50),
    ]
    factors = []
    for i, (_, op, socket, a, b) in enumerate(script):
        m = node(tree, 'ShaderNodeMath', (200, 300 - i * 150))
        m.operation = op
        m.inputs[1].default_value, m.inputs[2].default_value = a, b
        tree.links.new(socket, m.inputs[0])
        factors.append(m.outputs[0])
    value = factors[0]
    for i, f in enumerate(factors[1:]):
        m = node(tree, 'ShaderNodeMath', (400 + i * 150, 200))
        m.operation = 'MULTIPLY'
        tree.links.new(value, m.inputs[0])
        tree.links.new(f, m.inputs[1])
        value = m.outputs[0]
    albedo = node(tree, 'ShaderNodeMath', (900, 200))
    albedo.operation = 'MULTIPLY'
    albedo.inputs[1].default_value = .20
    tree.links.new(value, albedo.inputs[0])

    rough = node(tree, 'ShaderNodeMath', (900, -100))
    rough.operation = 'MULTIPLY_ADD'
    rough.inputs[1].default_value, rough.inputs[2].default_value = .10, .84
    tree.links.new(grain.outputs['Green'], rough.inputs[0])

    bump = node(tree, 'ShaderNodeBump', (900, -300), Strength=.55, Distance=.0006)
    tree.links.new(granule.outputs['Distance'], bump.inputs['Height'])

    bsdf = node(tree, 'ShaderNodeBsdfPrincipled', (1150, 0))
    tree.links.new(albedo.outputs[0], bsdf.inputs['Base Color'])
    tree.links.new(rough.outputs[0], bsdf.inputs['Roughness'])
    tree.links.new(bump.outputs['Normal'], bsdf.inputs['Normal'])

    height = node(tree, 'ShaderNodeNewGeometry', (900, -550))
    zsplit = node(tree, 'ShaderNodeSeparateXYZ', (1050, -550))
    tree.links.new(height.outputs['Position'], zsplit.inputs[0])
    zscale = node(tree, 'ShaderNodeMath', (1200, -550))
    zscale.operation = 'MULTIPLY'
    zscale.inputs[1].default_value = 1 / HEIGHT_RANGE
    tree.links.new(zsplit.outputs['Z'], zscale.inputs[0])
    emit = node(tree, 'ShaderNodeEmission', (1350, -550))
    tree.links.new(zscale.outputs[0], emit.inputs['Color'])

    out = node(tree, 'ShaderNodeOutputMaterial', (1550, 0))
    tree.links.new(bsdf.outputs[0], out.inputs['Surface'])
    return mat, bsdf, emit, out


def target():
    mesh = bpy.data.meshes.new('shingles-bake')
    mesh.from_pydata([(0, 0, 0), (TILE, 0, 0), (TILE, TILE, 0), (0, TILE, 0)], [], [(0, 1, 2, 3)])
    uv = mesh.uv_layers.new(name='UVMap')
    for loop, (u, v) in zip(uv.data, ((0, 0), (1, 0), (1, 1), (0, 1))):
        loop.uv = (u, v)
    return bpy.data.objects.new('shingles-bake', mesh)


def bake(kind, image, bake_target, **extra):
    tree = bake_target.data.materials[0].node_tree
    tex = tree.nodes.get('bake-image') or tree.nodes.new('ShaderNodeTexImage')
    tex.name = 'bake-image'
    tex.image = image
    tree.nodes.active = tex
    bpy.ops.object.bake(type=kind, use_selected_to_active=True, cage_extrusion=.05,
                        max_ray_distance=.1, margin=0, use_clear=True, **extra)
    pixels = np.empty(PIXELS * PIXELS * 4, dtype=np.float32)
    image.pixels.foreach_get(pixels)
    return pixels.reshape(PIXELS, PIXELS, 4)


def blank(name):
    return bpy.data.images.new(name, PIXELS, PIXELS, alpha=False, float_buffer=True)


def save(name, rgb, depth='8', directory=EXPORT):
    path = directory / f'{NAME}-{name}.png'
    image = bpy.data.images.new(f'out-{name}', PIXELS, PIXELS, alpha=False, float_buffer=depth == '16')
    image.colorspace_settings.name = 'Non-Color'
    rgba = np.ones((PIXELS, PIXELS, 4), dtype=np.float32)
    rgba[..., :3] = rgb if rgb.ndim == 3 else rgb[..., None]
    image.pixels.foreach_set(rgba.ravel())
    settings = bpy.context.scene.render.image_settings
    settings.file_format, settings.color_mode, settings.color_depth = 'PNG', 'RGB', depth
    image.save_render(str(path), scene=bpy.context.scene)
    return path


def srgb(linear):
    linear = np.clip(linear, 0, 1)
    return np.where(linear <= .0031308, linear * 12.92, 1.055 * np.power(linear, 1 / 2.4) - .055)


def measure(height):
    profile = height.mean(axis=1)
    spectrum = np.abs(np.fft.rfft(profile - profile.mean()))
    courses = int(np.argmax(spectrum[1:PIXELS // 8]) + 1)
    rise = np.diff(np.concatenate([profile, profile[:1]]))
    steep = np.flatnonzero(rise > .3 * THICKNESS / HEIGHT_RANGE)
    groups = np.split(steep, np.flatnonzero(np.diff(steep) > 1) + 1) if len(steep) else []
    butts = np.array([g.mean() for g in groups])
    spacing = np.diff(np.concatenate([butts, butts[:1] + PIXELS])) * TILE / PIXELS
    return {
        'courses_by_spectrum': courses,
        'butt_edges_found': int(len(butts)),
        'course_pitch_mm_mean': round(float(spacing.mean()) * 1000, 2) if len(spacing) else None,
        'course_pitch_mm_range': [round(float(spacing.min()) * 1000, 2), round(float(spacing.max()) * 1000, 2)]
        if len(spacing) else None,
    }


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.device = 'CPU'
    scene.cycles.samples = SAMPLES
    scene.world = bpy.data.worlds.new('bake')
    scene.world.light_settings.distance = .012
    scene.view_settings.view_transform = 'Raw'
    scene.view_settings.look = 'None'

    rng = random.Random(SEED)
    offsets, shingles = layout(rng)
    high = build(offsets, shingles)
    low = target()
    for obj in (high, low):
        scene.collection.objects.link(obj)
    mat, bsdf, emit, out = material()
    high.data.materials.append(mat)
    holder = bpy.data.materials.new('bake-target')
    low.data.materials.append(holder)
    holder.node_tree.nodes.clear()

    bpy.ops.object.select_all(action='DESELECT')
    high.select_set(True)
    low.select_set(True)
    bpy.context.view_layer.objects.active = low

    diffuse = bake('DIFFUSE', blank('diffuse'), low, pass_filter={'COLOR'})[..., :3]
    normal = bake('NORMAL', blank('normal'), low, normal_space='TANGENT')[..., :3]
    rough = bake('ROUGHNESS', blank('roughness'), low)[..., 0]
    ao = bake('AO', blank('ao'), low)[..., 0]
    mat.node_tree.links.new(emit.outputs[0], out.inputs['Surface'])
    height = bake('EMIT', blank('height'), low)[..., 0]
    mat.node_tree.links.new(bsdf.outputs[0], out.inputs['Surface'])

    albedo = diffuse * (.45 + .55 * ao)[..., None]
    luminance = albedo @ np.array([.2126, .7152, .0722], dtype=np.float32)

    EXPORT.mkdir(parents=True, exist_ok=True)
    SOURCE.parent.mkdir(parents=True, exist_ok=True)
    files = {
        'albedo': save('albedo', srgb(albedo)),
        'normal': save('normal', normal),
        'roughness': save('roughness', rough),
        'height': save('height', np.clip(height, 0, 1), depth='16', directory=SOURCE.parent),
    }
    measured = measure(height)
    manifest = {
        'material': 'Laminated asphalt roof shingles',
        'authority': 'scripts/art/asphalt-shingles.py',
        'blender': bpy.app.version_string,
        'licence': 'Authored for Borough; no third-party images',
        'specification': {
            'product': 'US laminated (architectural) asphalt shingle',
            'exposure_mm': round(EXPOSURE_SPEC * 1000, 2),
            'exposure_source': '5 5/8 in exposure, as published for GAF Timberline HDZ and CertainTeed Landmark',
            'shingle_length_mm': LENGTH * 1000,
            'thickness_mm': round(THICKNESS * 1000, 2),
            'bevel_mm': {'butt': BUTT_BEVEL * 1000, 'tab_side': SIDE_BEVEL * 1000},
            'least_stagger_mm': LEAST_STAGGER * 1000,
        },
        'tile': {
            'metres': TILE, 'pixels': PIXELS, 'mm_per_pixel': round(TILE / PIXELS * 1000, 3),
            'courses': COURSES, 'course_pitch_mm': round(EXPOSURE * 1000, 3),
            'shingles_per_course': round(TILE / LENGTH), 'up_slope': '+V in Blender, image top',
            'normal_map': 'tangent space, OpenGL (+Y up-slope)',
            'height_map_metres_at_white': HEIGHT_RANGE,
        },
        'albedo_linear_mean_luminance': round(float(luminance.mean()), 6),
        'measured': measured,
        'seed': SEED,
        'files': {k: {'file': str(p.relative_to(ROOT)), 'sha256': hashlib.sha256(p.read_bytes()).hexdigest()}
                  for k, p in files.items()},
    }
    (EXPORT / f'{NAME}.json').write_text(json.dumps(manifest, indent=2) + '\n')
    bpy.data.images.remove(bpy.data.images['out-albedo'])
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE), compress=True)
    print(json.dumps(measured))

    pitch = measured['course_pitch_mm_mean']
    if measured['courses_by_spectrum'] != COURSES or measured['butt_edges_found'] != COURSES \
            or pitch is None or abs(pitch - EXPOSURE * 1000) > 1.0:
        raise SystemExit(f'course measurement failed: {measured}')


main()

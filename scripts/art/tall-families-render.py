"""Renders near/far tower pairs and UV-grid overlays for review.

Run with Blender in the background:
  blender --background art/tall-families/lineup.blend --python scripts/art/tall-families-render.py -- OUT_DIR

Each far facade gets a temporary checker material from UVMap. The overlay material reads the same
UVs and leaves only integer bay and storey boundaries visible over the near body.
"""
import bpy
import sys
from pathlib import Path
from mathutils import Vector


class ReviewRenderer:
    def __init__(self, out):
        self.out = Path(out)
        self.out.mkdir(parents=True, exist_ok=True)
        self.scene = bpy.context.scene
        self.bodies = {obj.name: obj for obj in self.scene.objects
                       if obj.type == 'MESH' and obj.name.endswith(('-near', '-far'))}
        self.original_locations = {name: obj.location.copy() for name, obj in self.bodies.items()}
        self.opaque_grid = self.grid_material('far-grid-checker', False)
        self.overlay_grid = self.grid_material('far-grid-overlay', True)
        self.setup_scene()

    def setup_scene(self):
        scene = self.scene
        scene.render.engine = 'BLENDER_EEVEE'
        scene.render.resolution_x = 1600
        scene.render.resolution_y = 1000
        scene.render.resolution_percentage = 100
        scene.render.image_settings.file_format = 'PNG'
        scene.render.film_transparent = False
        scene.view_settings.view_transform = 'AgX'

        world = bpy.data.worlds.new('review-sky')
        world.use_nodes = True
        world.node_tree.nodes['Background'].inputs['Color'].default_value = (.55, .65, .8, 1)
        world.node_tree.nodes['Background'].inputs['Strength'].default_value = .45
        scene.world = world

        sun = bpy.data.objects.new('review-sun', bpy.data.lights.new('review-sun', 'SUN'))
        sun.data.energy = 4.0
        sun.data.angle = .08
        sun.rotation_euler = Vector((-.45, .55, -.7)).to_track_quat('-Z', 'Y').to_euler()
        scene.collection.objects.link(sun)

        bpy.ops.mesh.primitive_plane_add(size=4000, location=(0, 0, -.03))
        self.ground = bpy.context.object
        self.ground.name = 'review-ground'
        ground = bpy.data.materials.new('review-ground')
        ground.use_nodes = True
        ground.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (.12, .14, .11, 1)
        ground.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = .9
        self.ground.data.materials.append(ground)

        self.camera = bpy.data.objects.new('review-camera', bpy.data.cameras.new('review-camera'))
        scene.collection.objects.link(self.camera)
        scene.camera = self.camera
        self.camera.data.type = 'ORTHO'
        self.camera.data.clip_end = 5000

    @staticmethod
    def grid_material(name, transparent):
        material = bpy.data.materials.new(name)
        material.use_nodes = True
        nodes = material.node_tree.nodes
        links = material.node_tree.links
        nodes.clear()

        output = nodes.new('ShaderNodeOutputMaterial')
        uv = nodes.new('ShaderNodeUVMap')
        uv.uv_map = 'UVMap'
        split = nodes.new('ShaderNodeSeparateXYZ')
        links.new(uv.outputs['UV'], split.inputs['Vector'])

        edges = []
        for component in ('X', 'Y'):
            fract = nodes.new('ShaderNodeMath')
            fract.operation = 'FRACT'
            links.new(split.outputs[component], fract.inputs[0])
            low = nodes.new('ShaderNodeMath')
            low.operation = 'LESS_THAN'
            low.inputs[1].default_value = .055
            links.new(fract.outputs[0], low.inputs[0])
            high = nodes.new('ShaderNodeMath')
            high.operation = 'GREATER_THAN'
            high.inputs[1].default_value = .945
            links.new(fract.outputs[0], high.inputs[0])
            edge = nodes.new('ShaderNodeMath')
            edge.operation = 'MAXIMUM'
            links.new(low.outputs[0], edge.inputs[0])
            links.new(high.outputs[0], edge.inputs[1])
            edges.append(edge.outputs[0])
        line = nodes.new('ShaderNodeMath')
        line.operation = 'MAXIMUM'
        links.new(edges[0], line.inputs[0])
        links.new(edges[1], line.inputs[1])

        if transparent:
            clear = nodes.new('ShaderNodeBsdfTransparent')
            glow = nodes.new('ShaderNodeEmission')
            glow.inputs['Color'].default_value = (1.0, .01, .12, 1)
            glow.inputs['Strength'].default_value = 3.0
            mix = nodes.new('ShaderNodeMixShader')
            links.new(line.outputs[0], mix.inputs[0])
            links.new(clear.outputs[0], mix.inputs[1])
            links.new(glow.outputs[0], mix.inputs[2])
            links.new(mix.outputs[0], output.inputs['Surface'])
            if hasattr(material, 'surface_render_method'):
                material.surface_render_method = 'DITHERED'
            elif hasattr(material, 'blend_method'):
                material.blend_method = 'BLEND'
            material.show_transparent_back = False
            return material

        checker = nodes.new('ShaderNodeTexChecker')
        checker.inputs['Color1'].default_value = (.18, .38, .58, 1)
        checker.inputs['Color2'].default_value = (.68, .82, .92, 1)
        checker.inputs['Scale'].default_value = 1.0
        links.new(uv.outputs['UV'], checker.inputs['Vector'])

        cell_uv = nodes.new('ShaderNodeUVMap')
        cell_uv.uv_map = 'uv2'
        cell_split = nodes.new('ShaderNodeSeparateXYZ')
        links.new(cell_uv.outputs['UV'], cell_split.inputs['Vector'])
        hue_scale = nodes.new('ShaderNodeMath')
        hue_scale.operation = 'MULTIPLY_ADD'
        hue_scale.inputs[1].default_value = .045
        hue_scale.inputs[2].default_value = .42
        links.new(cell_split.outputs['X'], hue_scale.inputs[0])
        tint = nodes.new('ShaderNodeHueSaturation')
        tint.inputs['Saturation'].default_value = .8
        links.new(hue_scale.outputs[0], tint.inputs['Hue'])
        links.new(checker.outputs['Color'], tint.inputs['Color'])

        color = nodes.new('ShaderNodeMixRGB')
        color.blend_type = 'MIX'
        color.inputs['Color2'].default_value = (.015, .02, .025, 1)
        links.new(line.outputs[0], color.inputs[0])
        links.new(tint.outputs['Color'], color.inputs[1])
        shader = nodes.new('ShaderNodeBsdfPrincipled')
        shader.inputs['Roughness'].default_value = .78
        links.new(color.outputs['Color'], shader.inputs['Base Color'])
        links.new(shader.outputs[0], output.inputs['Surface'])
        return material

    @staticmethod
    def bounds(obj):
        return [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]

    def restore(self):
        for name, obj in self.bodies.items():
            obj.location = self.original_locations[name]
            obj.hide_render = True
        bpy.context.view_layer.update()

    def center(self, obj, x, y):
        if 'site_offset_x' in obj and 'site_offset_y' in obj:
            original = self.original_locations[obj.name]
            obj.location.x = original.x + x - float(obj['site_offset_x'])
            obj.location.y = original.y + y - float(obj['site_offset_y'])
        else:
            points = self.bounds(obj)
            middle = Vector(((min(p.x for p in points) + max(p.x for p in points)) / 2,
                             (min(p.y for p in points) + max(p.y for p in points)) / 2, 0))
            obj.location.x += x - middle.x
            obj.location.y += y - middle.y
        bpy.context.view_layer.update()

    @staticmethod
    def use_material(obj, material):
        for index, current in enumerate(obj.data.materials):
            if current and current.name.startswith('far-facade'):
                obj.data.materials[index] = material
                return
        raise RuntimeError(f'{obj.name} has no far-facade material')

    def frame(self, objects, filename, direction=Vector((.32, -1, .52))):
        points = [point for obj in objects for point in self.bounds(obj)]
        center = Vector(((min(p.x for p in points) + max(p.x for p in points)) / 2,
                         (min(p.y for p in points) + max(p.y for p in points)) / 2,
                         (min(p.z for p in points) + max(p.z for p in points)) / 2))
        direction = direction.normalized()
        self.camera.location = center + direction * 2000
        self.camera.rotation_euler = (center - self.camera.location).to_track_quat('-Z', 'Y').to_euler()
        bpy.context.view_layer.update()
        rotation = self.camera.matrix_world.to_quaternion()
        right = rotation @ Vector((1, 0, 0))
        up = rotation @ Vector((0, 1, 0))
        horizontal = [point.dot(right) for point in points]
        vertical = [point.dot(up) for point in points]
        width = max(horizontal) - min(horizontal)
        height = max(vertical) - min(vertical)
        aspect = self.scene.render.resolution_x / self.scene.render.resolution_y
        self.camera.data.ortho_scale = max(width, height * aspect, 10) * 1.18
        self.scene.render.filepath = str(self.out / filename)
        bpy.ops.render.render(write_still=True)

    def pair(self, base, filename):
        self.restore()
        near = self.bodies[f'{base}-near']
        far = self.bodies[f'{base}-far']
        near_width = max(p.x for p in self.bounds(near)) - min(p.x for p in self.bounds(near))
        far_width = max(p.x for p in self.bounds(far)) - min(p.x for p in self.bounds(far))
        separation = max(near_width, far_width) + max(12, max(near_width, far_width) * .18)
        self.center(near, -separation / 2, 0)
        self.center(far, separation / 2, 0)
        self.use_material(far, self.opaque_grid)
        near.hide_render = False
        far.hide_render = False
        self.frame([near, far], filename)

    def facade_copy(self, source):
        mesh = source.data
        facade_indices = {index for index, material in enumerate(mesh.materials)
                          if material and material.name.startswith(('far-facade', 'far-grid'))}
        uv = mesh.uv_layers.get('UVMap')
        uv2 = mesh.uv_layers.get('uv2')
        if not facade_indices or uv is None or uv2 is None:
            raise RuntimeError(f'{source.name} lacks far facade UV layers')
        vertices, faces, grid, cells = [], [], [], []
        for polygon in mesh.polygons:
            if polygon.material_index not in facade_indices:
                continue
            face = []
            face_uv, face_cells = [], []
            offset = polygon.normal.normalized() * .035
            for loop_index in polygon.loop_indices:
                face.append(len(vertices))
                vertex = mesh.vertices[mesh.loops[loop_index].vertex_index].co
                vertices.append(tuple(vertex + offset))
                face_uv.append(tuple(uv.data[loop_index].uv))
                face_cells.append(tuple(uv2.data[loop_index].uv))
            faces.append(tuple(face))
            grid.append(face_uv)
            cells.append(face_cells)
        data = bpy.data.meshes.new(f'{source.name}-overlay')
        data.from_pydata(vertices, [], faces)
        data.materials.append(self.overlay_grid)
        grid_layer = data.uv_layers.new(name='UVMap')
        cell_layer = data.uv_layers.new(name='uv2')
        for polygon, face_uv, face_cells in zip(data.polygons, grid, cells):
            for loop_index, coordinate, cell in zip(polygon.loop_indices, face_uv, face_cells):
                grid_layer.data[loop_index].uv = coordinate
                cell_layer.data[loop_index].uv = cell
        overlay = bpy.data.objects.new(f'{source.name}-overlay', data)
        overlay.matrix_world = source.matrix_world.copy()
        self.scene.collection.objects.link(overlay)
        return overlay

    def overlay(self, base, filename):
        self.restore()
        near = self.bodies[f'{base}-near']
        far = self.bodies[f'{base}-far']
        self.center(near, 0, 0)
        self.center(far, 0, 0)
        near.hide_render = False
        overlay = self.facade_copy(far)
        self.frame([near, overlay], filename, Vector((.18, -1, .25)))
        data = overlay.data
        bpy.data.objects.remove(overlay, do_unlink=True)
        bpy.data.meshes.remove(data)


def main():
    out = Path(sys.argv[sys.argv.index('--') + 1])
    renderer = ReviewRenderer(out)
    for variant in ('point', 'stepped-point', 'l', 'h'):
        renderer.pair(variant, f'tower-{variant}-near-far.png')
        renderer.overlay(variant, f'tower-{variant}-overlay.png')


if __name__ == '__main__':
    main()

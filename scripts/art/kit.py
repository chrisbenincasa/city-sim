"""Blender source of truth for the first three comparison-kit specimens."""
import argparse
import sys
import hashlib
import json
import math
from pathlib import Path
import bpy

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'src/Borough.Godot/assets/visual-study/kit'
SOURCE = ROOT / 'art/visual-study/kit'
parser=argparse.ArgumentParser()
parser.add_argument('--extremes',action='store_true')
parser.add_argument('--styles',action='store_true')
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
if args.styles and args.extremes:parser.error('choose styles or extremes')
if args.styles:
    OUT = OUT / 'styles'
    SOURCE = SOURCE / 'styles'
if args.extremes:
    OUT = OUT / 'extremes'
    SOURCE = SOURCE / 'extremes'
OUT.mkdir(parents=True, exist_ok=True)
SOURCE.mkdir(parents=True, exist_ok=True)
TREATMENTS = ['painterly','graphic','miniature','naturalistic'] if args.styles else ['detailed-extreme', 'sculpted-extreme'] if args.extremes else ['material-rich', 'sculpted', 'middle']
PALETTE = {'wall': 'b9afa0', 'stone': 'c4beb0', 'roof': '666d72', 'glass': '354c59',
           'frame': '777f80', 'paint': '77949b', 'rubber': '303639', 'lamp': 'dad5bd'}
records = []

def material(name, treatment):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    values = [int(PALETTE[name][i:i+2], 16) / 255 for i in (0, 2, 4)]
    linear = [v/12.92 if v <= .04045 else ((v+.055)/1.055)**2.4 for v in values]
    bsdf.inputs['Base Color'].default_value = (*linear, 1)
    bsdf.inputs['Roughness'].default_value = {'glass': .22, 'paint': .35, 'frame': .4}.get(name, .82)
    if name in ('roof', 'frame'): bsdf.inputs['Metallic'].default_value = .35
    if name == 'wall' and treatment in ('material-rich', 'detailed-extreme'):
        for label in ('normal', 'roughness'):
            image = bpy.data.images.load(str(ROOT / 'art/visual-study' / f'masonry-{label}.png'), check_existing=True)
            image.colorspace_settings.name = 'Non-Color'
            image.pack()
            tex = m.node_tree.nodes.new('ShaderNodeTexImage')
            tex.image = image
            if label == 'normal':
                normal = m.node_tree.nodes.new('ShaderNodeNormalMap')
                m.node_tree.links.new(tex.outputs['Color'], normal.inputs['Color'])
                m.node_tree.links.new(normal.outputs['Normal'], bsdf.inputs['Normal'])
            else: m.node_tree.links.new(tex.outputs['Color'], bsdf.inputs['Roughness'])
    return m

def finish(ob, name, mat, bevel=0):
    ob.name = name
    ob.data.materials.append(mats[mat])
    if ob.type == 'MESH' and ob.data.uv_layers.active:
        uv = ob.data.uv_layers.active.data
        for poly in ob.data.polygons:
            axis = max(range(3), key=lambda i: abs(poly.normal[i]))
            for loop in poly.loop_indices:
                p = ob.matrix_world @ ob.data.vertices[ob.data.loops[loop].vertex_index].co
                uv[loop].uv = ((p.x if axis != 0 else p.y)/2, (p.z if axis != 2 else p.y)/2)
    if style == 'miniature':
        if name in ('house','tower','podium','pitched_roof'): bevel = .18
        elif bevel: bevel = min(bevel*1.8,.2)
    if bevel:
        mod = ob.modifiers.new('edge', 'BEVEL')
        mod.width = bevel
        mod.segments = 5 if style in ('miniature','naturalistic') else 3 if treatment in ('material-rich', 'detailed-extreme') else 2
        bpy.ops.object.modifier_apply(modifier=mod.name)
        if style in ('miniature','naturalistic'):
            for face in ob.data.polygons:face.use_smooth=True
            weighted=ob.modifiers.new('weighted_normals','WEIGHTED_NORMAL')
            weighted.keep_sharp=True
            bpy.ops.object.modifier_apply(modifier=weighted.name)
    return ob

def box(name, pos, size, mat, bevel=0, core=False):
    if core: core_parts.append([name, pos, size, mat])
    bpy.ops.mesh.primitive_cube_add(size=1, location=pos)
    ob = bpy.context.object
    ob.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(ob, name, mat, bevel)

def mesh(name, vertices, faces, mat, core=False, bevel=0):
    if core: core_parts.append([name, vertices, faces, mat])
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    ob = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(ob)
    bpy.ops.object.select_all(action='DESELECT')
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.object.mode_set(mode='OBJECT')
    return finish(ob, name, mat, bevel)

def window(x, front, z, width=1.4, height=1.65):
    # Opening and glazing extents remain common; treatment changes joinery.
    box('opening', (x, front, z), (width+.22, .28, height+.22), 'roof', core=True)
    box('glass', (x, front-.16, z), (width, .035, height), 'glass', core=True)
    thick = {'material-rich': .055, 'sculpted': .14, 'middle': .085, 'detailed-extreme': .045, 'sculpted-extreme': .28}[treatment]
    depth = {'material-rich': .18, 'sculpted': .28, 'middle': .22, 'detailed-extreme': .24, 'sculpted-extreme': .6}[treatment]
    for side in (-1,1):
        box('jamb', (x+side*(width/2+.06), front-.22, z), (.28 if treatment == 'sculpted-extreme' else .12, depth, height+.28), 'stone', .04 if treatment == 'sculpted-extreme' else .015)
        box('rail', (x, front-.22, z+side*(height/2+.06)), (width+.24, depth, .28 if treatment == 'sculpted-extreme' else .12), 'stone', .04 if treatment == 'sculpted-extreme' else .015)
    if treatment not in ('sculpted', 'sculpted-extreme'):
        box('mullion', (x, front-.24, z), (thick, .08, height), 'frame', .008)
    if treatment in ('material-rich', 'detailed-extreme'):
        box('crossbar', (x, front-.24, z+.34), (width, .08, .045), 'frame', .006)
        box('drip', (x, front-.37, z-height/2-.075), (width+.28, .055, .035), 'frame', .006)
    box('sill', (x, front-.26, z-height/2-.16), (width+.38, .48, .13), 'stone', .02)

def house():
    box('house', (0,0,3), (8,10,6), 'wall', core=True)
    mesh('pitched_roof', [(-4.35,-5.3,6),(4.35,-5.3,6),(0,-5.3,8.2),(-4.35,5.3,6),(4.35,5.3,6),(0,5.3,8.2)],
         [(0,2,1),(3,4,5),(0,3,5,2),(2,5,4,1),(0,1,4,3)], 'roof', core=True)
    for x in (-2.3,2.3):
        for z in (1.8,4.7): window(x,-5.035,z)
    for y in (-2.7,2.7):
        for z in (1.8,4.7):
            before = set(bpy.context.scene.objects)
            window(y,-4.035,z)
            for ob in set(bpy.context.scene.objects)-before:
                x0,y0=ob.location.x,ob.location.y
                ob.location.x,ob.location.y=-y0,x0
                ob.rotation_euler.z += math.pi/2
    core_parts.append(['side_windows_rotation',90])
    box('door', (0,-5.07,1.2), (1.15,.16,2.4), 'glass', core=True)
    box('porch', (0,-5.55,2.65), (2.1,1.3,.18), 'stone', .03, core=True)
    box('step', (0,-5.5,.08), (1.8,1,.16), 'stone', .015, core=True)
    box('base', (0,-5.05,.23), (8.05,.18,.46), 'stone', .02)
    if treatment not in ('sculpted', 'sculpted-extreme'):
        for x in (-4.28,4.28): box('gutter', (x,0,6.04), (.12,10.7,.14), 'frame', .015)
        box('handle', (.37,-5.18,1.1), (.025,.035,.2), 'frame', .006)
    if treatment in ('material-rich', 'detailed-extreme'):
        for y in range(-5,6):
            for side in (-1,1):
                seam=box('roof_seam', (side*2.17,y,7.12), (4.86,.025,.025), 'frame')
                seam.rotation_euler.y = side*math.atan2(2.2,4.35)
        for x in (-3.85,3.85): box('downpipe', (x,-5.16,3), (.09,.09,5.9), 'frame', .01)

def tower():
    box('podium', (0,0,2.1), (24,20,4.2), 'stone', core=True)
    box('tower', (0,1,24.1), (18,16,39.8), 'wall', core=True)
    box('crown', (0,1,44.2), (18.5,16.5,.4), 'roof', core=True)
    for floor in range(12):
        z=5.8+floor*3.2
        for x in (-6,-2,2,6): window(x,-7.04,z,2.3,2.15)
        box('balcony_slab', (0,-8.2,z-1.3), (17,2.6,.18), 'stone', .025, core=True)
        box('balcony_rail', (0,-9.43,z-.65), (17,.09,1.1), 'glass', core=True)
        for x in (-8.5,8.5): box('balcony_return', (x,-8.25,z-.65), (.09,2.45,1.1), 'glass', core=True)
        if treatment not in ('sculpted', 'sculpted-extreme'):
            box('handrail', (0,-9.46,z-.08), (17,.07,.055), 'frame', .008)
        if treatment in ('material-rich', 'detailed-extreme'):
            for x in range(-8,9,2): box('rail_post', (x,-9.46,z-.65), (.045,.05,1.1), 'frame', .005)
    for x in range(-10,11,4): window(x,-10.04,2.1,2.8,3.1)
    box('entry_canopy', (0,-10.9,3.6), (7,2.5,.25), 'roof', .025, core=True)
    for side in (-1,1):
        for floor in range(12):
            for y in (-3,2,7):
                box('side_opening', (side*9.035,y,5.8+floor*3.2), (.14,2.1,2.15), 'glass', core=True)
    if treatment in ('sculpted', 'sculpted-extreme'):
        for x in (-8.9,8.9): box('corner_band', (x,-7.1,24), (.25,.28,39.5), 'stone', .03)

def car():
    # X width, Y length, Z up. Front is -Y, exported Godot +Z.
    box('body', (0,0,.67), (1.82,4.4,.72), 'paint', .19, core=True)
    vertices=[(-.84,-1.15,.93),(.84,-1.15,.93),(-.67,-.75,1.48),(.67,-.75,1.48),
              (-.84,1.45,.93),(.84,1.45,.93),(-.67,1.05,1.48),(.67,1.05,1.48)]
    mesh('cabin',vertices,[(0,1,3,2),(4,6,7,5),(0,2,6,4),(1,5,7,3),(2,3,7,6),(0,4,5,1)],'glass',core=True,bevel=.04)
    box('roof', (0,.15,1.485), (1.34,1.8,.055), 'paint', .035, core=True)
    for x in (-.89,.89):
        for y in (-1.35,1.35):
            core_parts.append(['wheel',x,y,.34,.34])
            bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=.34, depth=.19, location=(x,y,.34), rotation=(0,math.pi/2,0))
            ob=bpy.context.object
            bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
            finish(ob,'tyre','rubber',.035)
            bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=.2, depth=.195, location=(x,y,.34), rotation=(0,math.pi/2,0))
            ob=bpy.context.object
            bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
            finish(ob,'hub','frame',.015)
    for x in (-.62,.62): box('headlamp',(x,-2.18,.78),(.38,.06,.15),'lamp',.025,core=True)
    box('grille',(0,-2.21,.57),(.83,.055,.18),'rubber',.025,core=True)
    for side in (-1,1):
        box('pillar',(side*.78,.2,1.21),(.085,.13,.53),'paint',.02)
        box('mirror',(side*1.0,-.8,1.02),(.2,.24,.13),'paint',.035,core=True)
        if treatment not in ('sculpted', 'sculpted-extreme'):
            for y in (-.3,.8): box('handle',(side*.919,y,.85),(.025,.17,.025),'frame',.008)
            box('sill',(side*.915,0,.35),(.06,2.05,.08),'frame',.015)
        if treatment in ('material-rich', 'detailed-extreme'):
            for y in (-.7,.55,1.2): box('door_joint',(side*.918,y,.64),(.008,.015,.48),'roof')
            for y in (-1.35,1.35):
                for angle in range(0,360,60):
                    spoke=box('spoke',(side*.992,y,.34),(.018,.33,.025),'stone',.006)
                    spoke.rotation_euler.x=math.radians(angle)

def extreme_details(specimen):
    # Deliberately stronger masonry colour and construction detail for a boundary test.
    for i in range(5):
        name=f'brick_{i}'
        mat=mats['wall'].copy()
        mat.name=name
        bsdf=mat.node_tree.nodes.get('Principled BSDF')
        for socket in ('Normal','Roughness'):
            for link in list(bsdf.inputs[socket].links):mat.node_tree.links.remove(link)
        color=list(bsdf.inputs['Base Color'].default_value)
        bsdf.inputs['Base Color'].default_value=tuple(c*(.86+i*.06) if j<3 else c for j,c in enumerate(color))
        mats[name]=mat
    def brick_face(width,height,front,z0=0,side=False):
        batches=[([],[]) for _ in range(5)]
        for row in range(int(height/.22)):
            z=z0+row*.22+.11
            start=-width/2-(.25 if row%2 else 0)
            for col in range(int(width/.5)+2):
                left=max(-width/2,start+col*.5)
                right=min(width/2,start+(col+1)*.5)
                if right-left<.035: continue
                center=(left+right)/2
                size=(right-left-.016,.012,.204)
                pos=(center,front,z)
                if side: pos=(-front,center,z);size=(.012,right-left-.016,.204)
                vertices,faces=batches[(row*17+col*31)%5]
                first_vertex=len(vertices)
                corners=[(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]
                vertices.extend(tuple(pos[i]+corner[i]*size[i]/2 for i in range(3)) for corner in corners)
                faces.extend(tuple(first_vertex+i for i in face) for face in [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)])
        for index,(vertices,faces) in enumerate(batches):
            mesh('masonry',vertices,faces,f'brick_{index}')
    if specimen=='detached-house':
        brick_face(8,6,-5.008)
        brick_face(10,6,-4.008,side=True)
    elif specimen=='residential-tower':
        brick_face(18,39.8,-7.008,4.2)
        # Tower side is centred at Y=1; restore that offset after generating its masonry.
        before=set(bpy.context.scene.objects)
        brick_face(16,39.8,-9.008,4.2,side=True)
        for ob in set(bpy.context.scene.objects)-before: ob.location.y+=1
    else:
        mats['paint'].node_tree.nodes.get('Principled BSDF').inputs['Metallic'].default_value=.55
        mats['paint'].node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.18
        for y in (-1.35,1.35):
            for side in (-1,1):
                for angle in range(0,360,20):
                    rad=math.radians(angle)
                    box('tyre_rib',(side*.991,y+math.cos(rad)*.28,.34+math.sin(rad)*.28),(.016,.023,.023),'roof')
        for x in (-.62,.62):
            for z in (.745,.79,.835):box('lamp_louvre',(x,-2.217,z),(.34,.009,.009),'frame')
        for x in (-.25,.25):
            wiper=box('wiper',(x,-1.048,1.09),(.36,.025,.02),'rubber',.004)
            wiper.rotation_euler.y=.15

for specimen, build in [('detached-house',house),('residential-tower',tower),('car',car)]:
    for requested in TREATMENTS:
        style=requested if args.styles else None
        treatment={'painterly':'middle','graphic':'sculpted','miniature':'middle','naturalistic':'material-rich'}.get(requested,requested)
        bpy.ops.object.select_all(action='SELECT')
        bpy.ops.object.delete(use_global=False)
        for material_data in list(bpy.data.materials): bpy.data.materials.remove(material_data)
        mats={name:material(name,treatment) for name in PALETTE}
        core_parts=[]
        bpy.context.scene.unit_settings.system='METRIC'
        bpy.context.scene.unit_settings.scale_length=1
        build()
        if treatment == "detailed-extreme": extreme_details(specimen)
        for ob in bpy.context.scene.objects:
            assert all(p.area>0 for p in ob.data.polygons), ob.name
        # Export one shared surface per material instead of one draw surface per trim piece.
        for name, mat in mats.items():
            group=[ob for ob in bpy.context.scene.objects if ob.data.materials[0] == mat]
            if not group: continue
            bpy.ops.object.select_all(action='DESELECT')
            for ob in group: ob.select_set(True)
            bpy.context.view_layer.objects.active=group[0]
            bpy.ops.object.join()
            bpy.context.object.name=name
        stem=f'{specimen}-{requested}'
        bpy.context.preferences.filepaths.save_version=0
        bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/(stem+'.blend')))
        bpy.ops.export_scene.gltf(filepath=str(OUT/(stem+'.glb')),export_format='GLB',export_yup=True,export_apply=True,export_tangents=True,export_materials='EXPORT')
        records.append(dict(specimen=specimen,treatment=requested,core_sha256=hashlib.sha256(json.dumps(core_parts,sort_keys=True).encode()).hexdigest(),asset=stem+'.glb'))
for specimen in ('detached-house','residential-tower','car'):
    assert len({r['core_sha256'] for r in records if r['specimen']==specimen})==1
manifest=dict(version='comparison-kit-v1',authority='scripts/art/kit.py',blender=bpy.app.version_string,
              units='metres',godot_up='+Y',godot_front='+Z',origin='ground centre',palette=PALETTE,
              source_sha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),assets=records)
(SOURCE/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
print('KIT_EXPORT_OK: common composition for each specimen across all requested treatments')

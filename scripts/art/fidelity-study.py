"""Reference-led terrace: authored construction and CC0 photographed brick material."""
import bpy, math, random, importlib.util
from pathlib import Path
from mathutils import Vector
spec=importlib.util.spec_from_file_location('construction',Path(__file__).with_name('construction-study.py'))
c=importlib.util.module_from_spec(spec);spec.loader.exec_module(c)
e=c.e;m=c.m;box=m.box;mesh=m.mesh
rng=random.Random(63)
old_frame=c.frame

def frame(cx,z,w,h,door=False):
    old_frame(cx,z,w,h,door)
    if door:
        for zz in [z-.6,z-.12]:
            box('raised door panel',(cx,-.052,zz),(w-.27,.026,.32),'roof',.012)
        box('letter plate',(cx,.003,z+.11),(.24,.028,.045),'metal',.005)
        for dx in [-w/2-.09,w/2+.09]:box('door architrave',(cx+dx,.023,z+.04),(.12,.12,h+.08),'stone',.007)
        return
    for sign in [-1,1]:
        box('slender sash lining',(cx+sign*(w/2-.055),-.02,z),(.035,.055,h-.045),'blind')
    box('sash meeting rail',(cx,-.015,z+.09),(w-.07,.055,.048),'blind',.003)
    # Curtains are folded mesh, set inside the opening, varied by window.
    opening=rng.uniform(.25,.68)
    for sign in [-1,1]:
        cw=w*(1-opening)/2
        x0=cx-w/2+.04 if sign<0 else cx+w/2-cw-.04
        verts=[];faces=[]
        for i in range(17):
            x=x0+cw*i/16;y=-.3+.025*math.cos(i*math.pi/2)
            verts.extend([(x,y,z-h/2+.06),(x,y,z+h/2-.08)])
            if i:faces.append((2*i-2,2*i,2*i+1,2*i-1))
        mesh('folded linen curtain',verts,faces,'curtain' if rng.random()>.3 else 'blind')
    box('interior painted wall',(cx,-.81,z),(w-.03,.03,h-.02),'interior')
    box('interior picture frame',(cx+w*.2,-.779,z+.2),(.24,.025,.32),'bark')
    box('interior picture',(cx+w*.2,-.762,z+.2),(.19,.009,.26),'blind')

def foliage(x,y,z,r=.4):
    # Separate leaves with open space between them, rather than solid crowns.
    verts=[];faces=[]
    for i in range(420):
        theta=rng.uniform(0,math.tau);u=rng.uniform(-1,1);rad=r*rng.random()**.33
        p=Vector((x+rad*math.sqrt(1-u*u)*math.cos(theta),y+rad*math.sqrt(1-u*u)*math.sin(theta),z+rad*u*.7))
        a=Vector((rng.uniform(-1,1),rng.uniform(-1,1),rng.uniform(-.6,.6))).normalized()*.07
        b=a.cross(Vector((.3,.1,1))).normalized()*.035
        first=len(verts);verts.extend([p-a,p+b,p+a,p-b]);faces.append(tuple(first+j for j in range(4)))
    mesh('individual shrub leaves',verts,faces,'foliage')

def terrace():
    c.frame=frame;c.townhouse()
    # A photographed brick central bay; warm rendered neighbours stay pale.
    for ob in m.parts:
        if ob.name.startswith(('terrace facade','shared chimney')):
            if ob.data.materials[0].name=='accent':ob.data.materials[0]=m.mats['brick']
    for unit in range(3):
        x=(unit-1)*6
        for z,depth,height in [(8.58,.13,.09),(8.7,.25,.1),(8.81,.32,.1),(2.82,.08,.08)]:
            box('projecting cornice',(x,5.5+depth/2,z),(6,depth,height),'stone',.008)
        for dx in [-2.8+i*.28 for i in range(21)]:box('cornice dentil',(x+dx,5.61,8.54),(.1,.16,.14),'stone')
        # Ground-level masonry plinth is interrupted at the front door.
        for low,high in [(-3,-2.12),(-.98,3)]:
            box('stone plinth',(x+(low+high)/2,5.52,.19),(high-low,.12,.38),'stone',.009)
        for dx in [-1.55,1.35]:
            for f in range(3):
                if f==0 and dx<0:continue
                zz=1.5+f*2.9
                for sign in [-1,1]:box('window stone reveal',(x+dx+sign*.61,5.52,zz),(.11,.12,1.85),'stone',.007)
        for dx in [-2.95,2.95]:
            for f in range(18):box('corner quoin',(x+dx,5.525,.6+f*.425),(.1,.07,.39),'stone',.004)
        box('entry lamp bracket',(x-.8,5.66,2.05),(.05,.32,.04),'metal')
        box('entry lantern',(x-.8,5.8,1.96),(.13,.13,.2),'metal',.01)
        box('lantern pane',(x-.8,5.871,1.97),(.085,.009,.13),'lamp')
        # Legible, dimensional address lettering, part of the authored mesh.
        bpy.ops.object.text_add(location=(x-.77,5.59,1.65),rotation=(math.pi/2,0,0))
        ob=bpy.context.object;ob.data.body=str(21+unit*2);ob.data.size=.13;ob.data.extrude=.001;ob.data.materials.append(m.mats['metal']);bpy.ops.object.convert(target='MESH');m.parts.append(bpy.context.object);bpy.context.object.select_set(False)
        # Small entrance courts establish scale and a building/ground junction.
        for a in range(12):
            for b in range(4):
                box('paving slab',(x-2.75+a*.5,5.85+b*.5,.035),(.492,.492,.07),'paving' if (a+b)%3 else 'paving-light',.004)
        for xx in [x-2.85,x+2.85]:
            box('garden boundary base',(xx,6.65,.19),(.15,2.25,.38),'stone',.008)
            for yy in [5.7+i*.15 for i in range(14)]:box('railing picket',(xx,yy,.73),(.018,.018,.8),'metal')
            box('railing top',(xx,6.65,1.14),(.035,2.25,.035),'metal',.005)
        for xx in [x+.5,x+2.15]:
            box('terracotta planter',(xx,6.5,.24),(.65,.65,.42),'accent',.025)
            box('planter rim',(xx,6.5,.46),(.7,.7,.055),'accent',.012)
            box('planter soil',(xx,6.5,.49),(.56,.56,.025),'bark')
            foliage(xx,6.5,.8,.46)
    # Slate overlap relief, consistent subtle variation and real chimney flashing.
    for ob in m.parts:
        if ob.name.startswith('roof courses'):
            for mat in ['slate-0','slate-1','slate-2']:ob.data.materials.append(m.mats[mat])
            for p in ob.data.polygons:p.material_index=rng.randrange(1,4)
    for x in [-3,3]:
        box('chimney apron flashing',(x,-1.7,10.76),(1.07,.88,.055),'metal')
    # End wall relief and a restrained utility installation.
    for sign in [-1,1]:
        box('gable string course',(sign*9.02,0,8.74),(.1,10.95,.12),'stone')
        box('utility cabinet',(sign*9.055,3.8,1.1),(.12,.48,.65),'frame',.02)

def setup(name,mat):
    c.setup(name,mat)
    shader=mat.node_tree.nodes.get('Principled BSDF')
    if name=='glass':
        shader.inputs['Base Color'].default_value=(.72,.8,.82,1)
        shader.inputs['Alpha'].default_value=.28;shader.inputs['Roughness'].default_value=.12
    if name in ['curtain','foliage']:mat.use_backface_culling=False
    if name=='brick':
        for key,socket in [('diffuse','Base Color'),('rough','Roughness'),('nor_gl','Normal')]:
            tex=mat.node_tree.nodes.new('ShaderNodeTexImage')
            tex.image=bpy.data.images.load(str(e.ROOT/f'art/visual-study/fidelity-1/textures/brick_wall_001_{key}_2k.jpg'),check_existing=True);tex.image.pack()
            if key!='diffuse':tex.image.colorspace_settings.name='Non-Color'
            if key=='nor_gl':
                normal=mat.node_tree.nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.5
                mat.node_tree.links.new(tex.outputs['Color'],normal.inputs['Color']);mat.node_tree.links.new(normal.outputs['Normal'],shader.inputs[socket])
            else:mat.node_tree.links.new(tex.outputs['Color'],shader.inputs[socket])

e.OUT=e.ROOT/'art/visual-study/fidelity-1';e.EXPORT=e.ROOT/'src/Borough.Godot/assets/visual-study/fidelity'
e.OUT.mkdir(parents=True,exist_ok=True);e.EXPORT.mkdir(parents=True,exist_ok=True)
e.AUTHORITY=Path(__file__);e.MATERIAL_SETUP=setup;e.UV_SCALE={'brick':1.2}
e.EXTRA_COLORS|={'brick':'ffffff','curtain':'c6bda9','interior':'b0a698','paving':'a6a59e','paving-light':'adaaa2','slate-0':'3c4348','slate-1':'40474b','slate-2':'444a4f'}
e.BUILDS=[('attached-townhouse',terrace)]
if __name__ == '__main__':
    e.export_all()

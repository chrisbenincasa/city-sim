"""A constructed street corner, sharing the approved fidelity material workflow."""
import bpy,bmesh,math,importlib.util
from pathlib import Path
spec=importlib.util.spec_from_file_location('fidelity',Path(__file__).with_name('fidelity-study.py'))
f=importlib.util.module_from_spec(spec);spec.loader.exec_module(f)
c=f.c;e=f.e;m=f.m;box=m.box
c.frame=f.frame

def direct_mesh(name,verts,faces,mat,bevel=0):
    data=bpy.data.meshes.new(name);data.from_pydata(verts,[],faces)
    bm=bmesh.new();bm.from_mesh(data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    if bevel:
        bmesh.ops.bevel(bm,geom=list(bm.edges),offset=bevel,segments=2,affect='EDGES',clamp_overlap=True)
    bm.to_mesh(data);bm.free();data.update()
    ob=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(ob)
    data.materials.append(m.mats[mat]);m.parts.append(ob);return ob

m.mesh=direct_mesh;c.mesh=direct_mesh;f.mesh=direct_mesh;e.mesh=direct_mesh

def text(body,position,size,angle=0):
    bpy.ops.object.text_add(location=position,rotation=(math.pi/2,0,angle))
    ob=bpy.context.object;ob.data.body=body;ob.data.align_x='CENTER';ob.data.size=size;ob.data.extrude=.002;ob.data.materials.append(m.mats['blind'])
    bpy.ops.object.convert(target='MESH');m.parts.append(bpy.context.object);bpy.context.object.select_set(False)

def shop():
    def shop_frame(cx,z,w,h,door=False):
        if w<2.5 or door:return f.frame(cx,z,w,h,door)
        start=len(m.parts);f.old_frame(cx,z,w,h,False)
        for ob in list(m.parts[start:]):
            if ob.name.startswith('part lowered blind'):
                m.parts.remove(ob);bpy.data.objects.remove(ob,do_unlink=True)
            elif ob.name.startswith('room back'):
                for v in ob.data.vertices:v.co.y-=1.1
    c.frame=shop_frame
    holes=[(-4.1,1.6,3.6,2.7,False),(.1,1.6,3.6,2.7,False),(4.8,1.15,1.3,2.3,True)]
    holes += [(x,4.9+floor*3.05,1.25,1.85,False) for floor in range(2) for x in [-4.7,-1.55,1.55,4.7]]
    c.wall('shop street face',14,9.65,holes,0,6,material='brick')
    side=[(x,1.6,3,2.7,False) for x in [-3.5,1]]+[(x,4.9+floor*3.05,1.25,1.85,False) for floor in range(2) for x in [-3.6,0,3.6]]
    c.wall('shop corner flank',12,9.65,side,7,0,-math.pi/2,'brick')
    c.wall('rear elevation',14,9.65,[(x,4.9+floor*3.05,1.25,1.85,False) for floor in range(2) for x in [-4.7,0,4.7]],0,-6,math.pi,'wall')
    c.wall('party wall',12,9.65,[], -7,0,math.pi/2)
    box('shop slab',(0,0,.08),(14,12,.16),'stone')
    for z in [3.35,6.4,9.6]:box('floor diaphragm',(0,0,z),(13.5,11.5,.16),'room')
    for z,d,h in [(3.45,.18,.15),(9.45,.25,.15),(9.62,.4,.13)]:
        box('street cornice',(0,6+d/2,z),(14.4,d,h),'stone',.01)
        box('return cornice',(7+d/2,0,z),(d,12.2,h),'stone',.01)
    for x in [-6.7,-2.05,2.2,6.75]:box('shopfront pilaster',(x,6.04,1.72),(.22,.22,3.4),'stone',.015)
    for x in [-4.1,.1]:
        for dx in [-1.05,0,1.05]:box('storefront mullion',(x+dx,5.975,1.6),(.032,.065,2.7),'frame')
        box('display shelf',(x,5.5,.72),(3.3,.45,.08),'bark')
        for i in range(9):box('display package',(x-1.4+i*.35,5.52,.9),(.22,.2,.28),'accent' if i%3 else 'blind',.008)
        m.mesh('canvas awning',[(x-1.94,6,3.15),(x+1.94,6,3.15),(x+1.94,7.05,2.8),(x-1.94,7.05,2.8)],[(0,1,2,3)],'paint')
        box('awning valance',(x,7.05,2.72),(3.88,.025,.17),'paint')
        for dx in [-1.8,1.8]:e.limb('awning support',(x+dx,6.05,2.35),(x+dx,7,2.8),.018,'metal')
    box('shop fascia',(0,6.04,3.72),(13.6,.12,.38),'paint',.01)
    text('CORNER  &  CO.',(0,6.115,3.61),.26)
    box('flat roof',(0,0,9.76),(14,12,.14),'roof')
    for x in [-6.9,6.9]:box('parapet',(x,0,10),(.2,12,.5),'brick');box('coping',(x,0,10.28),(.32,12.2,.08),'stone')
    for y in [-5.9,5.9]:box('parapet',(0,y,10),(13.6,.2,.5),'brick');box('coping',(0,y,10.28),(13.9,.32,.08),'stone')
    for x in [-3.5,2]:
        box('roof equipment curb',(x,-2,9.98),(2.2,2,.32),'metal')
        box('ventilation unit',(x,-2,10.42),(1.9,1.7,.65),'frame',.025)
        for y in [-2.65+i*.12 for i in range(12)]:box('unit louvre',(x,y,10.76),(1.7,.045,.035),'metal')
    for x in [-6.8,6.8]:c.pipe(x,-6.06,9.8)
    c.frame=f.frame

def tower():
    # Preserve the realistic tower's asymmetric massing and approximate envelope.
    front=[(x,1.65,3,2.9,False) for x in [-8,-4,0,4,8]]
    c.wall('podium frontage',23,3.6,front,0,10,0,'stone')
    for sign in [-1,1]:c.wall('podium side',20,3.6,[(y,1.65,2.7,2.9,False) for y in [-6,-2,2,6]],sign*11.5,0,-sign*math.pi/2,'stone')
    c.wall('podium back',23,3.6,front,0,-10,math.pi,'stone')
    box('podium base',(0,0,.1),(23,20,.2),'stone')
    box('podium terrace',(0,0,3.65),(23.2,20.2,.18),'roof')
    for wing,x,y,w,d,floors in [(False,-2,-.5,13,15,11),(True,6,1,4,13,9)]:
        start=len(m.parts);height=floors*3.25+.8
        xs=[-w*.31,0,w*.31] if not wing else [0]
        holes=[(xx,(1.25 if not wing else 1.5)+floor*3.25,1.65,2.3 if not wing else 2.15,False) for floor in range(floors) for xx in xs]
        for yy,angle in [(y+d/2,0),(y-d/2,math.pi)]:c.wall('tower punched facade',w,height,holes,x,yy,angle,'wall' if not wing else 'brick')
        side=[(yy,1.5+floor*3.25,1.5,2.15,False) for floor in range(floors) for yy in [-d*.3,0,d*.3]]
        for sign in [-1,1]:c.wall('tower flank',d,height,side,x+sign*w/2,y,-sign*math.pi/2,'wall' if not wing else 'brick')
        for ob in m.parts[start:]:
            for v in ob.data.vertices:v.co.z+=3.6
        top=height+3.6
        box('tower roof',(x,y,top),(w+.2,d+.2,.18),'roof')
        for xx in [x-w/2,x+w/2]:box('roof parapet',(xx,y,top+.25),(.16,d,.5),'stone')
        for yy in [y-d/2,y+d/2]:box('roof parapet',(x,yy,top+.25),(w,.16,.5),'stone')
        for floor in range(floors):
            z=3.65+floor*3.25
            if not wing:
                for xx in xs:
                    bx=x+xx;by=y+d/2+.65
                    box('balcony slab',(bx,by,z),(2.65,1.4,.13),'stone',.012)
                    for px in [bx-1.25+i*.25 for i in range(11)]:box('balcony picket',(px,by+.65,z+.57),(.018,.022,1.02),'metal')
                    box('balcony top rail',(bx,by+.65,z+1.1),(2.65,.04,.045),'metal')
                    for sign in [-1,1]:box('balcony side rail',(bx+sign*1.3,by,z+1.1),(.035,1.3,.045),'metal')
    box('penthouse',(-2,-1,41.3),(9,11,2.3),'wall',.025)
    box('penthouse cap',(-2,-1,42.53),(9.4,11.4,.16),'roof')
    for x in [-4,0]:box('rooftop plant',(x,-2,43),(1.5,2,.8),'frame',.025)
    for x in [-9,9]:
        box('podium planter',(x,8,3.94),(1.6,1,.5),'stone',.03)
        f.foliage(x,8,4.45,.75)

def tree():
    e.limb('root trunk',(0,0,0),(.1,0,4.4),.18,'bark')
    for i in range(12):
        a=i*2.4;r=1.15+(i%3)*.28;z=3.6+(i%4)*.5
        x=math.cos(a)*r;y=math.sin(a)*r
        e.limb('tapered branch',(.1,0,2.8),(x,y,z),.055,'bark')
        for j in range(3):
            xx=x+.45*math.cos(a+j*2);yy=y+.45*math.sin(a+j*2)
            e.limb('twig',(x,y,z),(xx,yy,z+.5),.017,'bark');f.foliage(xx,yy,z+.65,.7)

def person():
    def rounded(name,center,scale,mat):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=12,location=center)
        ob=bpy.context.object;ob.name=name;ob.scale=scale;ob.data.materials.append(m.mats[mat])
        for polygon in ob.data.polygons:polygon.use_smooth=True
        m.parts.append(ob);ob.select_set(False)
    e.ellipsoid=rounded;e.walker()

e.OUT=e.ROOT/'art/visual-study/neighbourhood-1';e.EXPORT=e.ROOT/'src/Borough.Godot/assets/visual-study/neighbourhood'
e.OUT.mkdir(parents=True,exist_ok=True);e.EXPORT.mkdir(parents=True,exist_ok=True)
e.AUTHORITY=Path(__file__);e.BUILDS=[('corner-shop',shop),('residential-tower',tower),('street-tree',tree),('walker',person)]
e.export_all()

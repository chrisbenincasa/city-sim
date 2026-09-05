"""Realistic construction extension. Script owns every generated Blender source."""
import bpy, bmesh, math, json, hashlib, importlib.util
from pathlib import Path
from mathutils import Vector
spec=importlib.util.spec_from_file_location('model_round',Path(__file__).with_name('model-round.py'))
m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
ROOT=m.ROOT
OUT=ROOT/'art/visual-study/expanded-kit-1'
EXPORT=ROOT/'src/Borough.Godot/assets/visual-study/expanded'
OUT.mkdir(parents=True,exist_ok=True);EXPORT.mkdir(parents=True,exist_ok=True)
box=m.box;roof=m.roof;mesh=m.mesh

def opening(x,y,z,w,h,angle=0):
    start=len(m.parts);m.window(0,0,0,w,h)
    c=math.cos(angle);s=math.sin(angle)
    for ob in m.parts[start:]:
        for v in ob.data.vertices:
            a,b=v.co.x,v.co.y;v.co=(x+a*c-b*s,y+a*s+b*c,z+v.co.z)

def front_windows(cx,cy,w,d,floors,base=0,step=3.1,bays=4):
    for f in range(floors):
        z=base+1.75+f*step
        for i in range(bays):
            x=cx-w/2+(i+.5)*w/bays
            opening(x,cy+d/2+.02,z,min(1.6,w/bays-.6),1.7)
            opening(x,cy-d/2-.02,z,min(1.6,w/bays-.6),1.7,math.pi)
        for j in range(max(2,round(d/4))):
            y=cy-d/2+(j+.5)*d/max(2,round(d/4))
            opening(cx+w/2+.02,y,z,1.5,1.7,-math.pi/2)
            opening(cx-w/2-.02,y,z,1.5,1.7,math.pi/2)

def mass(name,x,y,w,d,h,mat='wall',base=0):
    box(name,(x,y,base+h/2),(w,d,h),mat,.025)
    box('stone footing',(x,y,base+.14),(w+.2,d+.2,.28),'stone')
    box('roof edge',(x,y,base+h),(w+.3,d+.3,.22),'stone',.015)

def door(x,y,w=1.3,z=0):
    box('entry surround',(x,y,z+1.25),(w+.24,.2,2.5),'stone',.025)
    box('door leaf',(x,y+.12,z+1.18),(w,.04,2.32),'roof',.02)
    box('door upper glass',(x,y+.15,z+1.65),(w-.22,.02,1.15),'glass')
    box('entry handle',(x+w*.3,y+.19,z+1.1),(.035,.035,.28),'frame')

def shopfront(x,y,w,z=0):
    for dx in [-w*.28,w*.28]:opening(x+dx,y,z+1.65,w*.35,2.35)
    door(x,y,1.2,z)
    box('shop fascia',(x,y+.12,z+3.16),(w,.22,.4),'roof',.02)
    box('shop canopy',(x,y+.65,z+2.95),(w+.2,1.4,.14),'roof',.025)

def townhouse():
    for i in range(3):
        x=(i-1)*6
        mass('party wall house',x,0,6,11,9.6,'accent' if i==1 else 'wall')
        roof(x,0,6,11.6,9.7,12)
        for f in range(3):
            for dx in [-1.65,1.65]:
                if f==0 and dx<0:continue
                opening(x+dx,5.53,1.75+f*3.1,1.35,1.9)
                opening(x+dx,-5.53,1.75+f*3.1,1.35,1.9,math.pi)
        door(x-1.65,5.55)
        box('door hood',(x-1.65,5.9,2.65),(1.9,.8,.15),'stone')
        box('chimney',(x+2,-2.7,11),(.7,.9,2.6),'accent')
        box('downpipe',(x+2.8,5.62,4.8),(.09,.1,9.4),'frame')
    for side in [-1,1]:
        for f in range(3):
            for y in [-3,1]:opening(side*9.02,y,1.75+f*3.1,1.4,1.9,-side*math.pi/2)

def walkup():
    mass('apartment block',0,0,20,13,12.6)
    front_windows(0,0,20,13,4,bays=5)
    box('central stair bay',(0,6.65,6.3),(3.2,1.5,12.6),'accent',.03)
    for f in range(4):opening(0,7.42,2+f*3.1,1.7,2.2)
    for f in range(1,4):
        for x in [-7,-3.7,3.7,7]:m.balcony(x,7, f*3.1+.25,2.8)
    door(0,7.44,1.5)
    roof(0,0,21,14,12.8,15.3)
    for x in [-8,8]:box('roof chimney',(x,-3,14),(.8,1,2.8),'accent')

def courtyard():
    # Open U: the court and all inward-facing windows are actual geometry.
    for x in [-11,11]:
        mass('courtyard side wing',x,0,8,26,12.6)
        front_windows(x,0,8,26,4,bays=2)
        roof(x,0,8.5,26.4,12.8,14.6)
    mass('courtyard rear wing',0,-9,14,8,12.6,'accent')
    front_windows(0,-9,14,8,4,bays=4)
    roof(0,-9,14.4,8.5,12.8,14.6)
    door(0,-4.94,1.7)
    for x in [-7.25,7.25]:box('court entrance pier',(x,12.5,1.2),(.45,.6,2.4),'stone')
    box('court paved threshold',(0,11.5,.06),(14,2,.12),'stone')

def office():
    mass('office lobby',0,0,24,20,4,'stone')
    for x in [-8,-4,0,4,8]:opening(x,10.04,2.1,3,3.1)
    door(0,10.17,2.4)
    box('entrance canopy',(0,11.2,3.6),(10,3,.25),'roof')
    for x,y,w,d,z,h in [(0,-1,19,16,4,36),(-2,-2,15,13,40,9)]:
        mass('office setback',x,y,w,d,h,base=z)
        for side in [-1,1]:
            for f in range(round(h/3)):
                for i in range(round(w/2.4)):
                    a=x-w/2+(i+.5)*w/round(w/2.4)
                    opening(a,y+side*d/2+side*.03,z+1.5+f*3,1.9,2.6,0 if side==1 else math.pi)
                for j in range(round(d/2.4)):
                    b=y-d/2+(j+.5)*d/round(d/2.4)
                    opening(x+side*w/2+side*.03,b,z+1.5+f*3,1.9,2.6,-side*math.pi/2)
        box('office roof',(x,y,z+h+.2),(w+.5,d+.5,.4),'roof')
    for x in [-8,8]:box('vertical office fin',(x,7.2,22),(.22,.6,36),'frame')

def mixed():
    mass('two storey retail podium',0,0,26,20,7,'stone')
    for x in [-8,0,8]:shopfront(x,10.03,7)
    for x in [-10,-6,-2,2,6,10]:opening(x,10.03,5.4,2.8,2.2)
    mass('residential shaft',-3,-2,16,14,27.9,base=7)
    front_windows(-3,-2,16,14,9,base=7,bays=4)
    for f in range(9):
        for x in [-8,-3,2]:m.balcony(x,5.7,7.3+f*3.1,3.6)
    box('tower roof',(-3,-2,35.2),(16.6,14.6,.5),'roof')
    mass('rooftop plant',-5,-4,6,5,2,base=35.4)

def smallshop():
    mass('neighbourhood shop',0,0,10,8,4.3)
    shopfront(0,4.04,9)
    roof(0,0,10.6,8.6,4.45,6)
    for side in [-1,1]:opening(side*5.03,0,2,1.5,1.8,-side*math.pi/2)
    door(2,-4.25)

def cornershop():
    ring=[(-7,-6),(7,-6),(7,3),(4,6),(-7,6)]
    mesh('chamfered corner shop',[(x,y,z) for z in [0,7] for x,y in ring],[(4,3,2,1,0),(5,6,7,8,9)]+[(i,(i+1)%5,(i+1)%5+5,i+5) for i in range(5)],'wall')
    for x in [-4,0]:opening(x,6.03,1.7,2.8,2.4);opening(x,6.03,5,1.7,1.9)
    for y in [-3,0]:opening(7.03,y,1.7,2.2,2.4,-math.pi/2);opening(7.03,y,5,1.7,1.9,-math.pi/2)
    opening(5.55,4.55,1.4,2,2.5,-math.pi/4)
    opening(5.55,4.55,5,1.6,1.9,-math.pi/4)
    for z in [3.15,7.1]:
        mesh('corner continuous cornice',[(x*1.02,y*1.02,h) for h in [z,z+.25] for x,y in ring],[(4,3,2,1,0),(5,6,7,8,9)]+[(i,(i+1)%5,(i+1)%5+5,i+5) for i in range(5)],'roof')
    for x in [-4,0,4]:opening(x,-6.03,5,1.7,1.9,math.pi)
    for y in [-3,1,4]:opening(-7.03,y,5,1.7,1.9,math.pi/2)

def factory():
    mass('production hall',-3,-2,30,22,6.5,'accent')
    for y in [-10,-4,2,8]:
        ob=m.prism('sawtooth roof',[(y-3,6.6),(y+3,6.6),(y+3,8.7),(y-3,6.9)],12,-18,'roof')
        for v in ob.data.vertices:v.co.x,v.co.y=v.co.y,v.co.x
        box('clerestory band',(-3,y+2.95,7.7),(29,.08,1.35),'glass')
        for x in range(-16,12,3):box('roof light mullion',(x,y+3,7.7),(.12,.12,1.5),'frame')
    for x in [-13,-5]:
        box('loading door surround',(x,9.08,2.5),(5.3,.2,5),'stone')
        box('roller shutter',(x,9.21,2.4),(4.9,.06,4.7),'frame')
        for z in [i*.3 for i in range(1,16)]:box('shutter rib',(x,9.26,z),(4.9,.04,.03),'roof')
    mass('attached factory office',8,13,12,8,6.4)
    front_windows(8,13,12,8,2,bays=3);door(8,17.05,1.5)
    for side in [-1,1]:
        for y in [-9,-3,3]:opening(-3+side*15.03,y,4.8,3,1.8,-side*math.pi/2)

def school():
    mass('classroom wing',-11,-1,12,22,6.5,'accent')
    mass('classroom wing',11,-1,12,22,6.5)
    for x in [-11,11]:
        front_windows(x,-1,12,22,2,bays=3)
        roof(x,-1,12.5,22.6,6.7,8.9)
    mass('school entrance hall',0,-5,10,10,7.4,'stone')
    for x in [-2.7,0,2.7]:opening(x,.04,4.8,2,3.5)
    door(0,.16,2.2)
    box('entrance canopy',(0,1.1,3),(8,2.6,.25),'roof')
    for x in [-3.5,3.5]:box('canopy column',(x,2,1.5),(.16,.16,3),'frame')
    for y in [3,6]:
        box('forecourt bench',(-3,y,.45),(2,.5,.16),'roof')
        for x in [-3.7,-2.3]:box('bench support',(x,y,.19),(.12,.35,.38),'frame')

def bus():
    # Chamfered footprint, individual glazing bays, front and centre doors.
    ring=[(-1.25,-5.7),(-1.1,-6),(1.1,-6),(1.25,-5.7),(1.25,5.7),(1,6),(-1,6),(-1.25,5.7)]
    mesh('bus body',[(x,y,z) for z in [.4,2.95] for x,y in ring],[tuple(range(7,-1,-1)),tuple(range(8,16))]+[(i,(i+1)%8,(i+1)%8+8,i+8) for i in range(8)],'paint',.035)
    box('bus roof',(0,0,3),(2.45,11.5,.16),'stone',.07)
    box('windscreen',(0,5.98,2.05),(1.95,.08,1.3),'glass',.02)
    box('destination display',(0,6.03,2.83),(1.8,.06,.22),'rubber')
    for sign in [-1,1]:
        for y in [-4.7,-3.1,-1.5,.1,1.7,3.3,4.9]:
            box('bus side glazing',(sign*1.265,y,2.12),(.045,1.35,1.2),'glass')
        for y in [-3.8,3.8]:m.wheel(sign*1.21,y,.5,32)
    for y in [.1,4.9]:
        box('boarding doors',(1.292,y,1.43),(.035,1.3,2.5),'glass')
        box('door seam',(1.315,y,1.43),(.025,.06,2.5),'frame')
    for x in [-.85,.85]:box('bus lamp',(x,5.97,.85),(.3,.1,.22),'lamp')
    box('rear window',(0,-6.03,2.1),(1.9,.06,1.15),'glass')
    box('roof ventilation',(0,-1.5,3.22),(1.7,2.5,.32),'roof',.08)

def ellipsoid(name,center,scale,mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=8,location=center)
    ob=bpy.context.object;ob.name=name;ob.scale=scale;ob.data.materials.append(m.mats[mat]);m.parts.append(ob);ob.select_set(False)

def limb(name,a,b,r,mat):
    a=Vector(a);b=Vector(b)
    bpy.ops.mesh.primitive_cone_add(vertices=10,radius1=r,radius2=r*.8,depth=(b-a).length,location=(a+b)/2)
    ob=bpy.context.object;ob.name=name;ob.rotation_mode='QUATERNION';ob.rotation_quaternion=(b-a).to_track_quat('Z','Y');ob.data.materials.append(m.mats[mat]);m.parts.append(ob);ob.select_set(False)

def walker():
    for x,y in [(-.105,.08),(.105,-.08)]:
        box('shoe',(x,y+.035,.055),(.17,.3,.11),'rubber',.035)
        limb('trouser leg',(x,y,.12),(x,0,.88),.09,'cloth')
    ellipsoid('jacket',(0,0,1.12),(.24,.14,.37),'paint')
    limb('neck',(0,0,1.35),(0,0,1.51),.065,'skin')
    ellipsoid('head',(0,0,1.62),(.115,.105,.15),'skin')
    ellipsoid('hair',(0,-.025,1.72),(.115,.095,.065),'bark')
    for sign in [-1,1]:
        limb('sleeve',(sign*.21,0,1.33),(sign*.29,.03,1.04),.065,'paint')
        limb('forearm',(sign*.29,.03,1.04),(sign*.27,.11,.86),.055,'paint')
        ellipsoid('hand',(sign*.27,.11,.84),(.05,.055,.075),'skin')

def tree(conical=False):
    limb('trunk',(0,0,0),(0,0,5.5),.23,'bark')
    if conical:
        for tier in range(5):
            z=2+tier*.8;r=2.2-tier*.35
            for j in range(7):
                a=j*2*math.pi/7+tier*.4
                ellipsoid('needle branch',(math.cos(a)*r*.45,math.sin(a)*r*.45,z),(r*.65,r*.55,.8),'foliage' if j%2 else 'foliage-dark')
        ellipsoid('leader',(0,0,6),(.5,.5,1),'foliage')
    else:
        for j in range(9):
            a=j*2.4;r=1.4 if j<7 else .5;z=4.4+(j%3)*.65
            limb('branch',(0,0,2.5),(math.cos(a)*r,math.sin(a)*r,z),.1,'bark')
            ellipsoid('leaf crown',(math.cos(a)*r,math.sin(a)*r,z),(1.45,1.25,1.35),'foliage' if j%3 else 'foliage-dark')

BUILDS=[('attached-townhouse',townhouse),('walk-up-apartment',walkup),('courtyard-apartment',courtyard),('office-tower',office),('mixed-use-tower',mixed),('small-shop',smallshop),('corner-shop',cornershop),('factory-with-office',factory),('school',school),('bus',bus),('walker',walker),('round-tree',lambda:tree(False)),('conical-tree',lambda:tree(True))]
AUTHORITY=Path(__file__)
EXTRA_COLORS={}
MATERIAL_SETUP=lambda name,mat:None
UV_SCALE={}

def export_all():
    records=[]
    for specimen,build in BUILDS:
        bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
        for material in list(bpy.data.materials):bpy.data.materials.remove(material)
        m.parts=[];m.mats={};m.style='realistic'
        colors=m.COLORS|{'bark':'514237','foliage':'657b4d','foliage-dark':'455b3a','skin':'b78667','cloth':'3e4652'}|EXTRA_COLORS
        for name,hexcode in colors.items():
            mat=bpy.data.materials.new(name);mat.use_nodes=True
            color=tuple(m.linear(int(hexcode[i:i+2],16)/255) for i in [0,2,4])+(1,)
            shader=mat.node_tree.nodes.get('Principled BSDF');shader.inputs['Base Color'].default_value=color
            shader.inputs['Roughness'].default_value=.24 if name=='glass' else .38 if name=='paint' else .8
            mat.diffuse_color=color;m.mats[name]=mat
            MATERIAL_SETUP(name,mat)
        build()
        bpy.ops.object.select_all(action='DESELECT')
        for ob in m.parts:ob.select_set(True)
        bpy.context.view_layer.objects.active=m.parts[0];bpy.ops.object.join();ob=bpy.context.object;ob.name=specimen
        slots=list(ob.data.materials);unique=[];mapping={}
        for i,mat in enumerate(slots):
            if mat not in unique:unique.append(mat)
            mapping[i]=unique.index(mat)
        indices=[mapping[p.material_index] for p in ob.data.polygons];ob.data.materials.clear()
        for mat in unique:ob.data.materials.append(mat)
        for p,index in zip(ob.data.polygons,indices):p.material_index=index
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
        for v in ob.data.vertices:v.co.y=-v.co.y
        bm=bmesh.new();bm.from_mesh(ob.data);bmesh.ops.dissolve_degenerate(bm,dist=1e-6,edges=list(bm.edges));bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(ob.data);bm.free();ob.data.update()
        uv=ob.data.uv_layers.new(name='world scale')
        ob.data.uv_layers.active=uv;uv.active_render=True
        for polygon in ob.data.polygons:
            axis=max(range(3),key=lambda i:abs(polygon.normal[i]));axes=[i for i in range(3) if i!=axis]
            for loop in polygon.loop_indices:
                co=ob.data.vertices[ob.data.loops[loop].vertex_index].co
                scale=UV_SCALE.get(unique[polygon.material_index].name,2)
                uv.data[loop].uv=(co[axes[0]]/scale,co[axes[1]]/scale)
        assert all(p.area>1e-10 for p in ob.data.polygons),specimen
        lo=[min(v.co[i] for v in ob.data.vertices) for i in range(3)];hi=[max(v.co[i] for v in ob.data.vertices) for i in range(3)]
        assert abs(lo[2])<.025,(specimen,lo)
        geometry=hashlib.sha256(repr(([(tuple(v.co)) for v in ob.data.vertices],[tuple(p.vertices) for p in ob.data.polygons])).encode()).hexdigest()
        bpy.context.preferences.filepaths.save_version=0
        bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(specimen+'.blend')))
        bpy.ops.export_scene.gltf(filepath=str(EXPORT/(specimen+'.glb')),export_format='GLB',use_selection=True,export_yup=True,export_apply=True,export_materials='EXPORT')
        records.append(dict(specimen=specimen,asset=specimen+'.glb',geometry_sha256=geometry,bounds_blender=[lo,hi],polygons=len(ob.data.polygons),materials=[mat.name for mat in unique]))
    (OUT/'manifest.json').write_text(json.dumps(dict(authority=str(AUTHORITY.relative_to(ROOT)),source_sha256=hashlib.sha256(AUTHORITY.read_bytes()).hexdigest(),helper_sha256=hashlib.sha256(Path(m.__file__).read_bytes()).hexdigest(),blender=bpy.app.version_string,units='metres',front='Blender -Y, Godot +Z',assets=records),indent=2)+'\n')
    print(f'EXPANDED_EXPORT_OK: {len(records)} distinct specimens')

if __name__ == '__main__':
    export_all()

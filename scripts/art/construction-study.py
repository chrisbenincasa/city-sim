"""Balanced realism pilot: actual openings, finer joinery and constructed roofs."""
import bpy,math,importlib.util
from pathlib import Path
spec=importlib.util.spec_from_file_location('expanded',Path(__file__).with_name('expanded-kit.py'))
e=importlib.util.module_from_spec(spec);spec.loader.exec_module(e)
m=e.m;box=m.box;mesh=m.mesh

def transformed(start,x,y,angle):
    c=math.cos(angle);s=math.sin(angle)
    for ob in m.parts[start:]:
        for v in ob.data.vertices:
            a,b=v.co.x,v.co.y;v.co.x=x+a*c-b*s;v.co.y=y+a*s+b*c

def frame(cx,z,w,h,door=False):
    # Wall face is Y=0, exterior positive. The glass sits behind the face.
    thickness=.045 if door else .035
    for x in [cx-w/2+thickness/2,cx+w/2-thickness/2]:box('slender jamb',(x,-.075,z),(thickness,.15,h),'frame',.004)
    for zz in [z-h/2+thickness/2,z+h/2-thickness/2]:box('head and bottom rail',(cx,-.075,zz),(w,.15,thickness),'frame',.004)
    if door:
        box('panel door',(cx,-.095,z),(w-.07,.055,h-.07),'roof',.008)
        box('door upper glazing',(cx,-.057,z+h*.22),(w-.2,.016,h*.32),'glass')
        box('door handle',(cx+w*.3,.015,z),(.025,.07,.18),'frame')
    else:
        box('inset glazing',(cx,-.12,z),(w-.055,.012,h-.055),'glass')
        if w>1.1:box('meeting stile',(cx,-.03,z),(.028,.08,h-.05),'frame',.003)
        box('opening transom',(cx,-.035,z+h*.15),(w-.05,.07,.027),'frame')
        box('stone sill',(cx,.035,z-h/2-.035),(w+.12,.28,.07),'stone',.008)
        box('lintel',(cx,-.06,z+h/2+.055),(w+.16,.2,.09),'stone',.006)
        # A shallow room pocket adds real depth behind the glass.
        box('room back',(cx,-.85,z),(w,.05,h),'room')
        for x in [cx-w/2,cx+w/2]:box('reveal return',(x,-.46,z),(.04,.76,h),'wall')
        box('room ceiling',(cx,-.46,z+h/2),(w,.76,.025),'room')
        box('room floor',(cx,-.46,z-h/2),(w,.76,.025),'room')
        if int(round((cx+20)*7+z*11))%3==0:
            box('part lowered blind',(cx,-.19,z+h*.33),(w-.08,.012,h*.28),'blind')

def wall(name,width,height,holes,x,y,angle=0,material='wall'):
    # Split the wall into solid panels around apertures; no wall behind glazing.
    start=len(m.parts)
    xs=sorted(set([-width/2,width/2]+[cx+sign*w/2 for cx,z,w,h,door in holes for sign in [-1,1]]))
    zs=sorted(set([0,height]+[z+sign*h/2 for cx,z,w,h,door in holes for sign in [-1,1]]))
    for a,b in zip(xs,xs[1:]):
        for low,high in zip(zs,zs[1:]):
            cx=(a+b)/2;cz=(low+high)/2
            if any(abs(cx-hx)<w/2-1e-6 and abs(cz-hz)<h/2-1e-6 for hx,hz,w,h,door in holes):continue
            box(name,(cx,-.15,cz),(b-a,.3,high-low),material)
    for cx,z,w,h,door in holes:frame(cx,z,w,h,door)
    transformed(start,x,y,angle)

def roof(x,y,width,depth,base,peak,angle=0):
    start=len(m.parts)
    # Gables are masonry; thin roofing and separate fascia meet them.
    m.prism('masonry gable',[(-width/2+.22,base),(width/2-.22,base),(0,peak-.12)],depth/2-.2,-depth/2+.2,'wall')
    for sign in [-1,1]:
        x0=sign*width/2
        mesh('roof deck',[(0,depth/2,peak),(x0,depth/2,base),(x0,-depth/2,base),(0,-depth/2,peak)],[(0,1,2,3)],'roof')
        courses=max(1,round(width/2/.32));columns=max(1,round(depth/.26))
        tileverts=[];tilefaces=[]
        for r in range(courses):
            a=sign*(r*width/2/courses);b=sign*((r+1)*width/2/courses)
            za=peak-abs(a)*(peak-base)/(width/2)+.025;zb=peak-abs(b)*(peak-base)/(width/2)+.018
            for c in range(columns):
                ya=-depth/2+c*depth/columns+.007;yb=ya+depth/columns-.014
                first=len(tileverts);tileverts.extend([(a,ya,za),(b,ya,zb),(b,yb,zb),(a,yb,za)])
                tilefaces.append(tuple(first+i for i in range(4)))
        mesh('roof courses',tileverts,tilefaces,'roof')
        box('eaves fascia',(x0,0,base-.035),(.07,depth,.13),'frame')
        box('gutter channel',(x0+sign*.065,0,base+.005),(.105,depth,.065),'metal')
    box('ridge capping',(0,0,peak+.05),(.17,depth,.09),'roof',.015)
    transformed(start,x,y,angle)

def pipe(x,y,h):
    box('downpipe',(x,y,h/2),(.065,.065,h),'metal',.008)
    for z in [.6,h*.5,h-.4]:box('pipe bracket',(x,y-.025,z),(.11,.1,.025),'metal')

def townhouse():
    height=8.9
    for i in range(3):
        x=(i-1)*6;holes=[]
        for f in range(3):
            z=1.5+f*2.9
            for dx in [-1.55,1.35]:
                if f==0 and dx<0:holes.append((dx,1.075,1,2.15,True))
                else:holes.append((dx,z,1.1,1.65,False))
        wall('terrace facade',6,height,holes,x,5.5,material='accent' if i==1 else 'wall')
        rear=[(dx,1.5+f*2.9,1.1,1.65,False) for f in range(3) for dx in [-1.5,1.5]]
        wall('rear facade',6,height,rear,x,-5.5,math.pi)
        box('foundation',(x,0,.09),(6,11,.18),'stone')
        for z in [2.8,5.7]:box('floor plate',(x,0,z),(5.7,10.7,.16),'room')
        box('door threshold',(x-1.55,5.62,.045),(1.16,.36,.09),'stone')
        box('door lintel',(x-1.55,5.54,2.25),(1.22,.22,.1),'stone')
        pipe(x+2.86,5.54,height-.05)
    for sign in [-1,1]:wall('end wall',11,height,[],sign*9,0,-sign*math.pi/2)
    # A continuous terrace roof avoids three toy-house roof silhouettes.
    roof(0,0,11.45,18.35,8.95,11.3,math.pi/2)
    for x in [-3,3]:
        box('shared chimney stack',(x,-1.7,10.95),(.9,.65,1.55),'accent',.015)
        box('chimney flaunching',(x,-1.7,11.75),(1.02,.77,.12),'stone')
        for dx in [-.22,.22]:box('chimney pot',(x+dx,-1.7,11.95),(.2,.24,.34),'roof',.015)

def shop():
    width=10;depth=8;height=3.5
    wall('shop frontage',width,height,[(-1.25,1.7,5.7,2.5,False),(3.05,1.125,1.2,2.25,True)],0,4)
    wall('shop rear',width,height,[(2,1.075,1,2.15,True),(-2,2.3,1.5,1,False)],0,-4,math.pi)
    for sign in [-1,1]:wall('shop flank',depth,height,[(0,2.15,1.4,1.3,False)],sign*5,0,-sign*math.pi/2)
    box('shop floor',(0,0,.06),(10,8,.12),'stone')
    for x in [-3.05,-1.25,.55]:box('display mullion',(x,3.97,1.7),(.04,.11,2.5),'frame')
    for x in [-2.7,0]:
        box('window display plinth',(x,3.55,.57),(1.8,.6,.28),'blind')
        for i in range(3):box('display goods',(x-.5+i*.48,3.57,.81),(.32,.25,.2),'stone')
    box('timber fascia',(0,4.025,3.08),(9.85,.18,.36),'roof',.012)
    box('canopy',(0,4.45,2.85),(9.8,1,.07),'metal',.012)
    for x in [-4.3,4.3]:
        e.limb('canopy stay',(x,4.78,2.89),(x,4.04,3.38),.014,'metal')
    roof(0,0,10.4,8.4,3.57,5.4)
    for x in [-4.92,4.92]:pipe(x,4.05,3.5)

def factory():
    # Solid masonry piers, tall loading openings and translucent rooflights.
    height=5.8
    wall('factory front',30,height,[(-10,2.25,4.4,4.5,True),(-2,2.25,4.4,4.5,True)],-3,9,material='accent')
    for sign in [-1,1]:
        holes=[(y,4.3,2.8,1.35,False) for y in [-7,-1,5]]
        wall('factory side',22,height,holes,-3+sign*15,-2,-sign*math.pi/2,'accent')
    wall('factory rear',30,height,[], -3,-13,math.pi, 'accent')
    box('factory floor',(-3,-2,.08),(30,22,.16),'room')
    for x in [-13,-5]:
        box('loading shutter',(x,8.94,2.22),(4.28,.08,4.38),'metal')
        for z in [.14+i*.18 for i in range(24)]:box('shutter lap',(x,9.005,z),(4.24,.03,.025),'frame')
        box('loading lintel',(x,9.015,4.6),(4.7,.23,.15),'stone')
    for x in [-17.85,-9,-1,11.85]:box('brick pier',(x,9.035,2.9),(.3,.2,5.8),'accent')
    for y in [-10.25,-4.75,.75,6.25]:
        a=y-2.75;b=y+2.75
        mesh('sawtooth roof panel',[(-18,a,6),(12,a,6),(12,b,7.7),(-18,b,7.7)],[(0,1,2,3)],'roof')
        for sign in [-1,1]:
            x=-3+sign*15
            mesh('gable infill',[(x,a,5.8),(x,b,5.8),(x,b,7.7),(x,a,6)],[(0,1,2,3)],'accent')
        box('clerestory glazing',(-3,b,6.78),(29.8,.018,1.76),'glass')
        for x in [-18+i*1.5 for i in range(21)]:box('rooflight bar',(x,b+.025,6.78),(.045,.05,1.84),'frame')
        box('rooflight head',(-3,b,7.72),(30.1,.1,.07),'metal')
        for x in range(-17,12,2):
            mesh('standing seam',[(x,a,6.035),(x,b,7.735),(x+.025,b,7.735),(x+.025,a,6.035)],[(0,1,2,3)],'metal')
    # Attached office: smaller punched openings and a separate entry.
    for front,y,angle in [(True,17,0),(False,9,math.pi)]:
        holes=[(x,1.55+f*2.85,1.2,1.65,False) for f in range(2) for x in [-3.7,0,3.7] if not(front and f==0 and x==0)]
        if front:holes.append((0,1.075,1.05,2.15,True))
        wall('office face',12,6.1,holes,8,y,angle)
    for sign in [-1,1]:wall('office side',8,6.1,[(0,1.6+f*2.85,1.2,1.65,False) for f in range(2)],8+sign*6,13,-sign*math.pi/2)
    box('office floor',(8,13,.08),(12,8,.16),'stone')
    box('office roof',(8,13,6.17),(12.2,8.2,.14),'roof')
    for x in [2.15,13.85]:pipe(x,17.045,6.05)
    for x in [-17.8,11.8]:pipe(x,9.13,5.8)

def setup(name,mat):
    shader=mat.node_tree.nodes.get('Principled BSDF')
    if name=='glass':
        shader.inputs['Roughness'].default_value=.16
        shader.inputs['Metallic'].default_value=.12
        shader.inputs['Alpha'].default_value=.76
        mat.surface_render_method='DITHERED'
    if name=='metal':shader.inputs['Metallic'].default_value=.65;shader.inputs['Roughness'].default_value=.45
    if name in ['wall','accent']:
        nodes=mat.node_tree.nodes;links=mat.node_tree.links
        for key in ['normal','roughness']:
            tex=nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(e.ROOT/f'art/visual-study/masonry-{key}.png'),check_existing=True);tex.image.colorspace_settings.name='Non-Color';tex.image.pack()
            if key=='normal':
                normal=nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.25 if name=='wall' else .5
                links.new(tex.outputs['Color'],normal.inputs['Color']);links.new(normal.outputs['Normal'],shader.inputs['Normal'])
            else:links.new(tex.outputs['Color'],shader.inputs['Roughness'])
e.OUT=e.ROOT/'art/visual-study/construction-study-1';e.EXPORT=e.ROOT/'src/Borough.Godot/assets/visual-study/construction'
e.OUT.mkdir(parents=True,exist_ok=True);e.EXPORT.mkdir(parents=True,exist_ok=True)
e.AUTHORITY=Path(__file__)
e.EXTRA_COLORS={'room':'48483e','blind':'b8b0a0','metal':'454b4c','roof-secondary':'62697f'}
e.MATERIAL_SETUP=setup
e.BUILDS=[('attached-townhouse',townhouse),('small-shop',shop),('factory-with-office',factory)]
if __name__ == '__main__':
    e.export_all()

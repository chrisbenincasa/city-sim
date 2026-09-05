"""First geometry-led round. Blender script is the editable authority; Z is up."""
import bpy, bmesh, math, json, hashlib
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'art/visual-study/model-round-1'
EXPORT=ROOT/'src/Borough.Godot/assets/visual-study/kit/models'
OUT.mkdir(parents=True,exist_ok=True); EXPORT.mkdir(parents=True,exist_ok=True)
STYLES=['realistic','city-builder','voxel','low-poly']
COLORS={'wall':'c9af91','stone':'ead6b8','roof':'666c86','glass':'4f727c','paint':'759783','frame':'8e8479','rubber':'30353d','lamp':'ffe8b2','accent':'b87860'}
parts=[]; mats={}; style=''
def linear(v):return v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4

def mesh(name,verts,faces,mat,bevel=0):
    data=bpy.data.meshes.new(name);data.from_pydata(verts,[],faces);data.update()
    obj=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mats[mat]);parts.append(obj)
    bpy.context.view_layer.objects.active=obj;obj.select_set(True)
    # Establish consistent outward winding for arbitrary prism profiles.
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT')
    if bevel and style in ['realistic','city-builder']:
        mod=obj.modifiers.new('edge catches','BEVEL');mod.width=bevel;mod.segments=2 if style=='realistic' else 3
        bpy.ops.object.modifier_apply(modifier=mod.name)
    obj.select_set(False);return obj

def box(name,pos,size,mat,bevel=0):
    x,y,z=pos;a,b,c=[v/2 for v in size]
    return mesh(name,[(x+i*a,y+j*b,z+k*c) for i,j,k in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]],[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],mat,bevel)

def prism(name,profile,front,back,mat,bevel=0):
    # Profile in X/Z, extrusion along Y.
    n=len(profile);v=[(x,y,z) for y in [front,back] for x,z in profile]
    return mesh(name,v,[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)],mat,bevel)

def window(x,y,z,w,h,side=False,bars=True):
    start=len(parts)
    box('window surround',(x,y,z),(w+.2,.18,h+.2),'stone',.025)
    box('recessed glazing',(x,y+.105,z),(w,.035,h),'glass')
    if bars:
        box('mullion',(x,y+.14,z),(.07,.05,h),'frame')
        box('sill',(x,y+.13,z-h/2),(w+.3,.32,.11),'stone')
    if side:
        for ob in parts[start:]:
            for v in ob.data.vertices:
                v.co.x,v.co.y=v.co.y,-v.co.x

def roof(x,y,w,d,base,peak,mat='roof'):
    return prism('pitched roof',[(x-w/2,base),(x+w/2,base),(x,peak)],y+d/2,y-d/2,mat,.035)

def house():
    box('foundation',(0,0,.15),(8.4,10.4,.3),'stone')
    if style=='realistic':
        box('main two-storey volume',(-.7,-.35,3.05),(6.6,9.3,5.8),'wall',.025)
        box('projecting front wing',(2.5,2.9,3.05),(3,4.2,5.8),'wall',.025)
        roof(-.7,-.35,7.2,10,5.95,8.15);roof(2.5,2.9,3.5,4.7,5.95,7.4)
        for z in [1.65,4.45]:
            for x in [-2.4,.2]:window(x,4.34,z,1.35,1.65)
            window(2.5,5.04,z,1.65,1.65)
        for y in [-3,-.4,2.5]:
            for z in [1.65,4.45]:window(-y,2.63 if y<.8 else 4.03,z,1.25,1.65,True)
        for x in [-3.5,1.4]:box('downpipe',(x,4.36,2.95),(.1,.13,5.5),'frame')
        for z in [.65,1,1.35,2,2.35,2.7,3.35,3.7,4.05,4.8,5.15,5.5]:
            box('brick course',(-.7,4.305,z),(6.6,.025,.025),'accent')
        for x in [-3.6,-2.8,-2,-1.2,-.4,.4,1.2,2,2.8,3.6]:
            # Narrow raised seams follow the two slopes on the primary roof.
            if x<2.6:
                z=8.15-abs(x+.7)*2.2/3.6
                box('roof seam',(x,-.35,z+.025),(.025,9.8,.035),'frame')
        box('chimney',(-2.4,-2.7,7.3),(.7,.8,2),'accent',.02)
    elif style=='city-builder':
        box('friendly tall house',(0,0,2.85),(8,9.7,5.4),'wall',.085)
        roof(0,0,9,10.6,5.55,8.65)
        # Exaggerated projecting gable with a large upper window.
        box('bay',(2.25,4.55,2.95),(3.5,1.7,5.6),'accent',.06)
        roof(2.25,4.55,4,2.3,5.75,7.9)
        for z in [1.8,4.5]:
            window(-2.1,4.95,z,2.1,1.9);window(2.25,5.45,z,2.3,1.8)
        for z in [1.8,4.5]:
            for y in [-2.6,1.3]:window(-y,4.06,z,2,1.8,True)
        box('bold eaves',(0,0,5.55),(8.7,10.2,.3),'stone',.04)
        box('chimney',(-2.8,-2.3,7.8),(.95,1.1,2.1),'accent',.05)
    elif style=='voxel':
        box('grid main',(0,0,3),(8,10,5.7),'wall')
        for i in range(6):box('roof stair',(0,0,6+i*.4),(9-i*1.2,10.4,.4),'roof')
        for z in [1.7,4.5]:
            for x in [-2.4,2.4]:window(x,5.05,z,1.6,1.6,bars=False)
            for y in [-2.8,.4,3.2]:window(-y,4.05,z,1.6,1.6,True,False)
        for x in [-3.8,3.8]:
            for z in [.6,1.4,2.2,3,3.8,4.6,5.4]:box('corner pixel',(x,5.05,z),(.4,.2,.4),'stone')
        box('square chimney',(-2.4,-2.4,7.7),(.8,.8,1.8),'accent')
    else:
        prism('clipped asymmetric walls',[(-4,.3),(4,.3),(3.6,5.7),(-3.5,6.2)],5,-5,'wall')
        prism('asymmetric folded roof',[(-4.6,6),(4.5,5.6),(1.2,8.3),(-2.8,7.5)],5.5,-5.5,'roof')
        for z in [1.8,4.55]:
            for x in [-2.15,1.8]:window(x,5.03,z,1.9,1.6,bars=False)
        # Planar side glazing on the sloped facade is inset into a projecting bay.
        box('angular side bay',(3.75,0,2.7),(.7,4,3.3),'accent')
        for y in [-1.1,1.1]:window(-y,4.12,3,1.5,2,True,False)
        prism('faceted chimney',[(-2.9,6.8),(-2.1,6.8),(-2.3,8.6),(-3,8.4)],-2,-2.8,'accent')
    front=5.15 if style!='realistic' else 4.45
    box('entry',(0,front,1.25),(1.25,.2,2.2),'roof',.02)
    box('porch step',(0,front+.65,.12),(2.4,1.8,.24),'stone')
    box('porch canopy',(0,front+.6,2.7),(2.8,1.8,.2),'roof',.02)
    for x in [-1.1,1.1]:box('porch post',(x,front+1.25,1.45),(.14,.14,2.5),'stone')

def balcony(x,y,z,w):
    box('balcony floor',(x,y,z),(w,1.4,.18),'stone',.025)
    box('balcony rail',(x,y+.64,z+.6),(w,.1,.8),'glass')
    for dx in [-w/2,w/2]:box('railing post',(x+dx,y+.65,z+.6),(.055,.06,1),'frame')

def tower():
    box('podium',(0,0,1.8),(23,20,3.6),'stone',.07)
    for x in [-8,-4,0,4,8]:window(x,10.03,1.8,2.5,2.6,bars=style!='voxel')
    if style=='realistic':
        box('main shaft',(-2,-.5,22.2),(13,15,37.2),'wall',.035)
        box('slim projecting wing',(6,1,19.2),(4,13,31.2),'accent',.03)
        box('setback penthouse',(-2,-1,42),(10,12,2.4),'wall',.03)
        box('roof cap',(-2,-1,43.4),(10.7,12.7,.4),'roof',.03)
        for f in range(11):
            z=5+f*3.25
            for x in [-6,-2,2]:
                window(x,7.03,z+1,1.9,2.2);balcony(x,7.6,z-.25,2.8)
            for y in [-4.5,-.4,3.7]:window(-y,8.04 if z+1<34.8 else 4.54,z+1,1.75,2.2,True)
        for x in [-8.4,4.4]:box('vertical pier',(x,7.1,22.2),(.24,.35,37.2),'stone')
    elif style=='city-builder':
        box('broad central shaft',(-1,0,22),(16,15,36.8),'wall',.16)
        box('offset side tower',(7,1,17),(5,14,26.8),'accent',.12)
        box('upper setback',(-1,0,41.1),(12,12,3.2),'wall',.12)
        box('expressive crown',(-1,0,43),(14,14,.8),'roof',.12)
        box('crown lantern',(-1,0,44),(6,7,1.2),'stone',.1)
        for f in range(10):
            z=5.3+f*3.45
            for x in [-5.2,0,4.4]:window(x,7.56,z+1,2.65,2.3)
            balcony(-1,8,z-.25,15.6)
            for y in [-4,1,5]:window(-y,9.55 if z+2.2<30.4 else 7.05,z+1,2.5,2.3,True)
    elif style=='voxel':
        for tier,(w,d,z,h) in enumerate([(18,16,3.6,19.2),(15.6,14.4,22.8,9.6),(12,12,32.4,7.2),(8.4,9.6,39.6,4.8)]):
            box('stepped tower',(0,0,z+h/2),(w,d,h),'wall')
            for f in range(round(h/2.4)):
                level=z+1.2+f*2.4
                for x in range(-int(w/2)+2,int(w/2),3):window(x,d/2+.05,level,1.2,1.2,bars=False)
                for y in range(-int(d/2)+2,int(d/2),3):window(-y,w/2+.05,level,1.2,1.2,True,False)
            box('setback rim',(0,0,z+h),(w+.4,d+.4,.4),'roof')
        for x in [-8.4,8.4]:box('pixel accent pier',(x,8.15,13.2),(.6,.3,19.2),'accent')
    else:
        # Octagonal shaft; windows orient to every facade, including diagonal faces.
        ring=[(-6,-8),(6,-8),(9,-5),(9,5),(6,8),(-6,8),(-9,5),(-9,-5)]
        verts=[(x,y,z) for z in [3.6,40.6] for x,y in ring]
        mesh('octagonal shaft',verts,[tuple(range(7,-1,-1)),tuple(range(8,16))]+[(i,(i+1)%8,(i+1)%8+8,i+8) for i in range(8)],'wall')
        for f in range(11):
            z=5+f*3.2
            for i,p in enumerate(ring):
                q=ring[(i+1)%8];dx=q[0]-p[0];dy=q[1]-p[1];length=math.hypot(dx,dy)
                normal=(dy/length,-dx/length); angle=math.atan2(dy,dx)
                for t in ([.22,.5,.78] if length>8 else [.5]):
                    cx=p[0]+dx*t+normal[0]*.04;cy=p[1]+dy*t+normal[1]*.04
                    ob=box('faceted inset window',(0,0,0),(min(2,length*.45),.12,2.05),'glass')
                    ob.rotation_euler.z=angle;ob.location=(cx,cy,z)
        mesh('sloping crown',[(x*1.04,y*1.04,40.5) for x,y in ring]+[(0,0,44.3)],[(i,(i+1)%8,8) for i in range(8)],'roof')
        for x in [-6,6]:box('angular accent rib',(x,8.1,22),(.45,.2,36.8),'accent')

def wheel(x,y,r,vertices):
    # Axle along X. Circular or deliberately square/polygonal according to model language.
    for name,radius,depth,mat in [('tyre',r,.2,'rubber'),('hub',r*.59,.215,'stone')]:
        verts=[(x+ax*depth/2,y+radius*math.cos(2*math.pi*i/vertices+math.pi/4 if vertices==4 else 2*math.pi*i/vertices),r+radius*math.sin(2*math.pi*i/vertices+math.pi/4 if vertices==4 else 2*math.pi*i/vertices)) for ax in [-1,1] for i in range(vertices)]
        # Square tyres use a rotated profile scaled to preserve exact ground contact.
        if vertices==4:
            for j,(xx,yy,zz) in enumerate(verts):verts[j]=(xx,y+(yy-y)*math.sqrt(2),r+(zz-r)*math.sqrt(2))
        n=vertices
        mesh(name,verts,[tuple(range(n-1,-1,-1)),tuple(range(n,n*2))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)],mat,.015)

def car():
    if style=='voxel':
        box('voxel chassis',(0,0,.55),(1.8,4.4,.5),'paint')
        box('cabin lower step',(0,-.1,1),(1.6,2.4,.4),'glass')
        box('cabin upper step',(0,-.2,1.3),(1.4,1.6,.2),'glass')
        box('pixel roof',(0,-.2,1.45),(1.6,1.6,.1),'paint')
        for x in [-.72,.72]:
            for y in [-.9,.5]:box('grid pillar',(x,y,1.25),(.16,.16,.4),'paint')
    else:
        chunky=style=='city-builder';angular=style=='low-poly'
        width=1.94 if chunky else 1.82
        # Longitudinal sculpting and true wheel-arch cutouts, with a lower central sill.
        profile=[(-2.2,.46),(-2.2,.7),(-1.7,.91),(-.95,.96),(.9,.92),(1.9,.82),(2.2,.65),(2.2,.4),(1.75,.4),(1.65,.62),(1.45,.74),(1.2,.74),(1,.62),(.9,.4),(-.9,.4),(-1,.62),(-1.2,.74),(-1.45,.74),(-1.65,.62),(-1.75,.4)]
        if chunky:
            profile=[(-2.2,.42),(-2.2,.95),(-1.8,1.02),(-.9,1.04),(.9,1.03),(1.8,.95),(2.2,.8),(2.2,.42)]+profile[8:]
        elif angular:
            profile=[(-2.2,.46),(-2.2,.95),(-1.7,1.05),(-.95,.95),(.9,.82),(1.9,.64),(2.2,.55),(2.2,.4)]+profile[8:]
        # Build profile in longitudinal Z plane, then rotate so car length remains Y.
        ob=prism('shaped body with wheel arches',profile,width/2,-width/2,'paint',.055 if chunky else .018)
        for v in ob.data.vertices:v.co.x,v.co.y=v.co.y,v.co.x
        h=1.7 if chunky else 1.32 if angular else 1.46
        rear=-1.65 if chunky else -1.45 if angular else -1.25
        top_rear=-1.4 if chunky else -.9 if angular else -.72
        top_front=.3 if chunky else -.05 if angular else .45
        vertices=[(-width*.46,rear,.96 if chunky else .85),(width*.46,rear,.96 if chunky else .85),(width*.46,1.05,.85),(-width*.46,1.05,.85),(-width*.36,top_rear,h),(width*.36,top_rear,h),(width*.36,top_front,h),(-width*.36,top_front,h)]
        mesh('tapered glazed cabin',vertices,[(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)],'glass',.025 if chunky else 0)
        box('roof',(0,(top_rear+top_front)/2,h+.025),(width*.74,top_front-top_rear+.06,.08),'paint',.035)
        for sign in [-1,1]:
            lower=Vector((sign*width*.46,-.2,.85));upper=Vector((sign*width*.36,-.2,h))
            pillar=box('sloping door pillar',(0,0,0),(.065,.11,(upper-lower).length),'paint',.008)
            pillar.rotation_mode='QUATERNION';pillar.rotation_quaternion=(upper-lower).to_track_quat('Z','Y');pillar.location=(upper+lower)/2
        for x in [-width*.53,width*.53]:box('mirror',(x,.65,1.03),(.19,.28,.13),'paint',.035)
        box('front bumper',(0,2.13,.46),(width*.98,.19,.17),'stone',.055)
        box('rear bumper',(0,-2.13,.47),(width*.98,.15,.16),'stone',.035)
        if style=='realistic':
            for x in [-.92,.92]:
                for y in [-.5,.55]:box('door handle',(x,y,.85),(.03,.22,.04),'stone',.008)
            for z in [.48,.53,.58]:box('grille slat',(0,2.24,z),(.78,.025,.025),'rubber')
        if angular:
            # Triangulation exposes broad flat facets, not rounded body normals.
            for ob in parts:
                mod=ob.modifiers.new('planar facets','TRIANGULATE');bpy.context.view_layer.objects.active=ob;bpy.ops.object.modifier_apply(modifier=mod.name)
    for x in [-.92,.92]:
        for y in [-1.32,1.32]:wheel(x,y,.34,4 if style=='voxel' else 8 if style=='low-poly' else 20 if style=='city-builder' else 32)
    for x in [-.61,.61]:
        box('headlamp',(x,2.205,.68),(.39,.04,.15),'lamp',.035)
        box('tail light',(x,-2.205,.68),(.34,.04,.12),'accent',.015)

if __name__ == '__main__':
    records=[]
    for style in STYLES:
        for specimen,build in [('detached-house',house),('residential-tower',tower),('car',car)]:
            bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
            parts=[];mats={}
            for name,hexcode in COLORS.items():
                mat=bpy.data.materials.new(name);mat.use_nodes=True
                shader=mat.node_tree.nodes.get('Principled BSDF')
                color=tuple(linear(int(hexcode[i:i+2],16)/255) for i in [0,2,4])+(1,)
                shader.inputs['Base Color'].default_value=color
                shader.inputs['Roughness'].default_value=.22 if name=='glass' else .38 if name=='paint' and style=='realistic' else .78
                if name=='wall' and style=='realistic':
                    nodes=mat.node_tree.nodes;links=mat.node_tree.links
                    for mapname in ['normal','roughness']:
                        tex=nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(ROOT/f'art/visual-study/masonry-{mapname}.png'),check_existing=True);tex.image.colorspace_settings.name='Non-Color';tex.image.pack()
                        if mapname=='normal':
                            normal=nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.6;links.new(tex.outputs['Color'],normal.inputs['Color']);links.new(normal.outputs['Normal'],shader.inputs['Normal'])
                        else:links.new(tex.outputs['Color'],shader.inputs['Roughness'])
                mat.diffuse_color=color;mats[name]=mat
            build()
            bpy.ops.object.select_all(action='DESELECT')
            for ob in parts:ob.select_set(True)
            bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join()
            ob=bpy.context.object;ob.name=f'{specimen}-{style}'
            # Consolidate slots by material, avoiding a draw surface per source primitive.
            slots=list(ob.data.materials);unique=[];mapping={}
            for i,mat in enumerate(slots):
                if mat not in unique:unique.append(mat)
                mapping[i]=unique.index(mat)
            indices=[mapping[p.material_index] for p in ob.data.polygons]
            ob.data.materials.clear()
            for mat in unique:ob.data.materials.append(mat)
            for p,index in zip(ob.data.polygons,indices):p.material_index=index
            bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
            # Blender +Y fronts become Godot +Z fronts; preserve the right-hand facade.
            for v in ob.data.vertices:v.co.y=-v.co.y
            bm=bmesh.new();bm.from_mesh(ob.data)
            bmesh.ops.dissolve_degenerate(bm,dist=1e-6,edges=list(bm.edges))
            bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
            bm.to_mesh(ob.data);bm.free();ob.data.update()
            uv=ob.data.uv_layers.new(name="world masonry")
            for polygon in ob.data.polygons:
                axis=max(range(3),key=lambda i:abs(polygon.normal[i]))
                axes=[i for i in range(3) if i!=axis]
                for loop in polygon.loop_indices:
                    co=ob.data.vertices[ob.data.loops[loop].vertex_index].co
                    uv.data[loop].uv=(co[axes[0]]/2,co[axes[1]]/2)
            assert all(p.area>1e-10 for p in ob.data.polygons),(specimen,style,'degenerate')
            minimum=[min(v.co[i] for v in ob.data.vertices) for i in range(3)]
            maximum=[max(v.co[i] for v in ob.data.vertices) for i in range(3)]
            assert abs(minimum[2])<.025,(specimen,style,minimum)
            geometry=hashlib.sha256(repr(([(tuple(v.co)) for v in ob.data.vertices],[tuple(p.vertices) for p in ob.data.polygons])).encode()).hexdigest()
            bpy.context.preferences.filepaths.save_version=0
            bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(ob.name+'.blend')))
            bpy.ops.export_scene.gltf(filepath=str(EXPORT/(ob.name+'.glb')),export_format='GLB',use_selection=True,export_yup=True,export_apply=True,export_materials='EXPORT')
            records.append(dict(specimen=specimen,treatment=style,asset=ob.name+'.glb',geometry_sha256=geometry,bounds_blender=[minimum,maximum],polygons=len(ob.data.polygons)))
    for specimen in ['detached-house','residential-tower','car']:
        assert len({r['geometry_sha256'] for r in records if r['specimen']==specimen})==4
    (OUT/'manifest.json').write_text(json.dumps(dict(authority='scripts/art/model-round.py',source_sha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),comparison='model-round-1',controls='same role, approximate scale, warm material palette; geometry deliberately varies',assets=records),indent=2)+'\n')
    print('MODEL_ROUND_EXPORT_OK: 12 distinct geometry specimens')

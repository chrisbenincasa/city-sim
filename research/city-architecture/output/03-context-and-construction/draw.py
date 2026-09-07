#!/usr/bin/env python3
"""Original research proposals. All geometry in metres; no traced source images."""
from pathlib import Path
from html import escape as E
import math,json
O=Path(__file__).resolve().parent
class SVG:
 def __init__(self,w,h,title,desc):
  self.w=w;self.h=h;self.s=[f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {w} {h}" role="img"><title>{E(title)}</title><desc>{E(desc)}</desc><defs><pattern id="hatch" width="6" height="6" patternUnits="userSpaceOnUse"><path d="M0 6L6 0" stroke="#bbb" stroke-width=".6"/></pattern><marker id="arrow" markerWidth="6" markerHeight="6" refX="5" refY="3" orient="auto"><path d="M0 0L6 3L0 6" fill="none" stroke="#333"/></marker></defs><style>text{{font-family:Arial,sans-serif;fill:#252a2e}} .small{{font-size:12px}} .label{{font-size:15px}} .title{{font-size:26px;font-weight:bold}}</style><rect width="100%" height="100%" fill="#faf9f5"/>']
 def text(self,x,y,t,size=14):self.s.append(f'<text x="{x}" y="{y}" font-size="{size}">{E(str(t))}</text>')
 def line(self,x,y,a,b,stroke='#343a3e',width=1,dash='',arrow=False):self.s.append(f'<line x1="{x}" y1="{y}" x2="{a}" y2="{b}" stroke="{stroke}" stroke-width="{width}"'+(f' stroke-dasharray="{dash}"' if dash else '')+(' marker-end="url(#arrow)"' if arrow else '')+'/>')
 def rect(self,x,y,w,h,fill='#e2e1dc',stroke='#343a3e',sw=1):self.s.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="{fill}" stroke="{stroke}" stroke-width="{sw}"/>')
 def poly(self,pts,fill='#d0d0ca',stroke='#343a3e'):self.s.append('<polygon points="'+' '.join(f'{x},{y}' for x,y in pts)+f'" fill="{fill}" stroke="{stroke}"/>')
 def circle(self,x,y,r,fill='#333'):self.s.append(f'<circle cx="{x}" cy="{y}" r="{r}" fill="{fill}"/>')
 def save(self,name): (O/'drawings'/name).write_text(''.join(self.s)+'</svg>')
 def dim(self,x,y,w,label):
  self.line(x,y,x+w,y);self.line(x,y-4,x,y+4);self.line(x+w,y-4,x+w,y+4);self.text(x,y-8,label,12)
 def human(self,x,y,s):
  self.circle(x,y-1.55*s,.15*s);self.line(x,y-1.4*s,x,y-.6*s,width=2);self.line(x-.3*s,y-1.1*s,x+.3*s,y-1.1*s,width=2);self.line(x,y-.6*s,x-.2*s,y,width=2);self.line(x,y-.6*s,x+.2*s,y,width=2)
 def scale(self,x,y,s):
  self.rect(x,y,5*s,4,'#333');self.rect(x+5*s,y,5*s,4,'white');self.text(x,y+20,'0',12);self.text(x+5*s,y+20,'5',12);self.text(x+10*s,y+20,'10 m',12)

def stairs(v,x,y,w,h,s):
 v.rect(x,y,w*s,h*s,'#f5f4ef');
 for i in range(1,9):v.line(x,y+i*h*s/9,x+w*s,y+i*h*s/9,stroke='#777')
 v.line(x+w*s/2,y+h*s-3,x+w*s/2,y+3,arrow=True)
def elevation(v,x,y,w,floors,s,kind='range',roof='flat',color=False,depth=10,height=None):
 h=height if height is not None else floors*3.2;fh=h/floors;fill=('#c79672' if color else '#e2e1dc');v.rect(x,y-h*s,w*s,h*s,fill)
 if roof in ('gable','low'):
  rise=depth/2*math.tan(math.radians(18 if roof=='gable' else 11))
  v.rect(x,y-(h+rise)*s,w*s,rise*s,'#59636b' if color else '#b8bbb9')
 else:v.rect(x,y-(h+.45)*s,w*s,.45*s,'#59636b' if color else '#c3c6c3')
 bays=max(2,round(w/3))
 for f in range(floors):
  for b in range(bays):
   xx=x+(b+.3)*w*s/bays
   if kind=='row' and f==0 and b%2==0:xx=x+(b/2*6+2)*s
   if kind=='stairs' and any(xx+1.2*s>x+(cx-1.5)*s and xx<x+(cx+1.5)*s for cx in [w/4,w*3/4]):continue
   if f==0 and kind in ('shop','office','workshop','med'):
    v.rect(xx,y-2.7*s,w*s/bays*.65,2.5*s,'#879499' if color else '#bfc6c7');continue
   if f==0 and ((kind=='row' and b%2==0) or (b==bays//2 and kind not in ('corridor','stairs'))):v.rect(xx,y-2.1*s,.9*s,2.1*s,'#756b60' if color else '#9b9e9c')
   else:
    v.rect(xx,y-(f*fh+2.6)*s,1.2*s,1.5*s,'#879499' if color else '#bfc6c7')
    if kind=='med':
     v.rect(xx-.25*s,y-(f*fh+1)*s,2*s,.12*s,'#555');v.line(xx-.25*s,y-(f*fh+1.9)*s,xx+1.75*s,y-(f*fh+1.9)*s);v.line(xx-.25*s,y-(f*fh+1)*s,xx-.25*s,y-(f*fh+1.9)*s)
 if kind=='row':
  for i in range(1,round(w/6)):v.line(x+i*6*s,y,x+i*6*s,y-h*s,width=2)
 if kind=='stairs':
  for cx in [w/4,w*3/4]:
   v.rect(x+(cx-1.5)*s,y-h*s,3*s,h*s,fill)
   v.rect(x+(cx-.45)*s,y-2.1*s,.9*s,2.1*s,'#707777')
   for f in range(1,floors):v.rect(x+(cx-.5)*s,y-(f*fh+1.5)*s,1*s,2*s,'#aab7ba')
 v.line(x,y,x+w*s,y,width=2)

# Controlled housing comparisons: exact gross envelope equal, discrete access plans.
v=SVG(1160,900,'A — Equal area, different access','Original proposed apartments, not measured regional buildings. 1152 square metres gross study envelope each, 3 storeys, 9.6 metre walls.')
v.text(30,38,'A / Same residential programme. Different plans.',26)
v.text(30,65,'PROPOSALS · 12 study dwellings each · 1,152 m² gross envelope · 3 floors · 9.6 m walls · no simulation allocation',14)
for idx,(name,w,d,mode) in enumerate([('A1 / Corridor block · US infill-informed',24,16,'corridor'),('A2 / Two-stair range · Danish-informed',32,12,'stair')]):
 x=40+idx*570;y=170;s=13
 v.text(x,110,name,19);v.text(x,132,'Use / floor area / wall height held; footprint and access change.',12)
 v.rect(x,y,w*s,d*s,'#e0e0d9',sw=2)
 if mode=='corridor':
  v.rect(x,y+7*s,w*s,2*s,'#faf9f5');stairs(v,x+1*s,y+6*s,4,5,s);stairs(v,x+19*s,y+6*s,4,5,s)
  for xx in [12]:v.line(x+xx*s,y,x+xx*s,y+7*s,width=2);v.line(x+xx*s,y+9*s,x+xx*s,y+16*s,width=2)
  v.text(x+6*s,y+8.3*s,'2 m corridor',12);v.line(x-20,y+8*s,x,y+8*s,arrow=True)
  v.text(x,y+d*s+22,'Shared side entrance; two reserved stairs; four flats / floor.',12)
 else:
  for xx in [8,24]:
   stairs(v,x+(xx-2)*s,y+7*s,4,5,s);v.line(x+xx*s,y+12*s+18,x+xx*s,y+12*s,arrow=True)
  v.line(x+16*s,y,x+16*s,y+12*s,width=2)
  for xx in [8,24]:v.line(x+xx*s,y,x+xx*s,y+7*s,width=2)
  v.text(x,y+d*s+22,'Two front stair doors; paired flats; no longitudinal corridor.',12)
 v.dim(x, y-10,w*s,f'{w} m');v.text(x+w*s+8,y+35,f'{d} m',12)
 v.text(x,435,'STREET ELEVATION · same scale as plans',12)
 elevation(v,x,610,w,3,s,'corridor' if idx==0 else 'stairs','flat' if idx==0 else 'low',depth=d);v.human(x+w*s+22,610,s)
 v.text(x,644,'Low-slope membrane / parapet' if idx==0 else 'Low gable / membrane, not a steep tiled roof',13)
 # roof plan
 ry=678;rs=5;v.rect(x,ry,w*rs,d*rs,'#dedfda');v.line(x,ry+d*rs/2,x+w*rs,ry+d*rs/2,width=2)
 for xx in [w*.25,w*.75]:
  v.line(x+xx*rs,ry+d*rs/2-3,x+xx*rs,ry+5,arrow=True);v.line(x+xx*rs,ry+d*rs/2+3,x+xx*rs,ry+d*rs-5,arrow=True)
 v.text(x+w*rs+15,ry+20,'Roof plan · 5 px/m',12);v.text(x+w*rs+15,ry+39,'Drains at low edges;',12);v.text(x+w*rs+15,ry+58,'downpipes to service side.',12)
 v.scale(x,810,s)
v.text(30,862,'Source constraints: South Bend sixplex (US1), Tingbjerg ranges (DK1), roof assemblies (C1/C2). Every drawn dimension is proposed.',12)
v.save('controlled-housing.svg')

# Building dictionaries for contextual streets, all dimensions proposed.
def B(x,y,w,d,n,k='range',r='flat',name=''):return dict(x=x,y=y,w=w,d=d,n=n,k=k,r=r,name=name,height=7 if name=='G001 workplace' else n*3.2)
contexts=[
 ('sacramento','Sacramento / postwar tract + later infill','CA1 / CA2 / CA3 · proposals, not surveyed streets',[
 B(2,10,14,10,1,'house','gable','ranch'),B(22,10,12,12,2,'range','flat','duplex'),B(40,8,6,19,2,'range','gable','court wing'),B(58,8,6,19,2,'range','gable','court wing'),B(46,21,12,6,2,'range','gable','court rear'),B(67,10,12,16,1,'shop','flat','retail'),B(3,43,22,12,1,'workshop','low','workshop')], 'Driveways beside houses; open apartment forecourt; public shop door and side delivery route.'),
 ('south-bend','South Bend / incremental infill','US1 · catalogue-derived relationships, all street dimensions proposed',[
 B(2,5,12,20,2,'range','gable','sixplex analogue'),B(20,5,12,12,2,'row','gable','duplex'),B(38,5,24,12,2,'row','gable','attached row'),B(66,2,14,18,3,'shop','flat','corner shop'),B(4,40,20,12,2,'office','flat','small office')],'Front stoops and side paths; continuous rear lane; corner commerce is a proposed addition.'),
 ('amersfoort','Amersfoort / planned extension','NL1 · beukmaat constrains rhythm, not our proposed external widths',[
 B(2,3,30,10,2,'row','gable','five-house row'),B(38,3,12,10,2,'row','gable','semi pair'),B(56,3,24,12,3,'range','flat','apartment end'),B(2,43,30,10,2,'row','flat','later row'),B(40,43,18,12,2,'office','flat','office')],'Private rear gardens meet a connected path; parking/servicing on block edge, not inside gardens.'),
 ('tingbjerg','Tingbjerg / low ranges + selective infill','DK1 / DK2 · proposals, not the adopted plan reproduced',[
 B(3,12,32,12,3,'stairs','low','range'),B(43,12,32,12,3,'stairs','low','range'),B(3,45,24,10,2,'row','flat','infill row'),B(45,45,24,12,1,'shop','flat','local shop')],'Shared open ground behind ranges; front stair paths; service route stays clear of shared green.'),
 ('valencia','Valencia / compact Mediterranean infill','ES1 / ES2 · evidence limits in atlas; all geometry proposed',[
 B(0,0,28,12,4,'med','flat','shop + flats'),B(28,0,28,12,4,'med','flat','infill'),B(56,0,24,12,4,'med','flat','corner'),B(0,12,12,20,4,'med','flat','side wing'),B(68,12,12,20,4,'med','flat','side wing'),B(24,47,32,12,1,'workshop','flat','separate workshop')],'Three-sided block, open court counted separately; entrances from street; service passage at open end.'),
 ('fictional','Fictional / compact temperate starting street','P1 · designed mixture, not an existing place',[
 B(0,3,24,12,2,'row','gable','attached homes'),B(28,3,24,16,3,'corridor','flat','A1 apartment'),B(56,0,24,16,3,'shop','flat','corner mixed use'),B(0,40,36,20,2,'office','flat','G001 workplace'),B(44,43,32,16,2,'workshop','low','workshop')],'Stable front line with one setback transition; one rear lane; workplace rear faces stay accessible.')]

def iso(v,buildings,ox,oy,sc,color=False):
 def pt(x,y,z):return (ox+(x+y)*sc,oy+(x-y)*sc*.43-z*sc)
 for b in sorted(buildings,key=lambda b:b['x']-b['y']):
  x,y,w,d,n=b['x'],b['y'],b['w'],b['d'],b['n'];h=b['height'];fh=h/n
  a=pt(x,y,0);bb=pt(x+w,y,0);c=pt(x+w,y+d,0);dd=pt(x,y+d,0)
  at=pt(x,y,h);bt=pt(x+w,y,h);ct=pt(x+w,y+d,h);dt=pt(x,y+d,h)
  v.poly([a,bb,bt,at],'#cfa17f' if color else '#d0d0ca');v.poly([bb,c,ct,bt],'#b18469' if color else '#b9bdbb');v.poly([at,bt,ct,dt],'#657079' if color else '#e5e5df')
  if b['r']!='flat':
   rise=d/2*math.tan(math.radians(18 if b['r']=='gable' else 11))
   r1=pt(x,y+d/2,h+rise);r2=pt(x+w,y+d/2,h+rise)
   v.poly([at,bt,r2,r1],'#56636c' if color else '#b5b9b8');v.poly([r1,r2,ct,dt],'#79828a' if color else '#d6d8d4');v.poly([bt,ct,r2],'#b18469' if color else '#b9bdbb')
  def face(xx,z,ww,hh,fill):v.poly([pt(xx,y,z),pt(xx+ww,y,z),pt(xx+ww,y,z+hh),pt(xx,y,z+hh)],fill)
  bays=max(2,round(w/3));kind=b['k'];wall='#cfa17f' if color else '#d0d0ca';glass='#586c76' if color else '#798486'
  for f in range(n):
   for k in range(bays):
    xx=x+(k+.3)*w/bays
    if kind=='row' and f==0 and k%2==0:xx=x+k/2*6+2
    if kind=='stairs' and any(xx+1.2>x+cx-1.5 and xx<x+cx+1.5 for cx in [w/4,w*3/4]):continue
    if f==0 and kind in ('shop','office','workshop','med'):face(xx,.2,w/bays*.65,2.5,glass)
    elif f==0 and ((kind=='row' and k%2==0) or (k==bays//2 and kind not in ('corridor','stairs'))):face(xx,0,.9,2.1,'#687071')
    else:face(xx,f*fh+1.1,1.2,1.5,glass)
  if kind=='stairs':
   for cx in [w/4,w*3/4]:
    face(x+cx-1.5,0,3,h,wall);face(x+cx-.45,0,.9,2.1,'#687071')
    for f in range(1,n):face(x+cx-.5,f*fh-.5,1,2,glass)

for slug,title,sub,bs,note in contexts:
 for color in ([False,True] if slug=='fictional' else [False]):
  v=SVG(1160,1130,title,'Original scaled contextual street proposal with plan, elevation and axonometric. All ground dimensions proposed; no simulation ownership implied.')
  v.text(30,38,title,26);v.text(30,63,sub,14);v.text(30,86,'B / CONTEXTUAL · each plan uses 6 px/m · street frontage 80 m · site depth 64 m · north is up',13)
  x=50;y=145;s=6;v.rect(x,y,80*s,64*s,'#f1f1eb');v.rect(x,y-48,80*s,36,'#dddeda');v.text(x,y-25,'PUBLIC STREET / 6 m carriageway proposal',12)
  v.rect(x-6*s,y-48,6*s,48+64*s,'#dddeda');v.rect(x,y+33*s,80*s,6*s,'#dddeda');v.text(x+15,y+37*s,'CONNECTED SERVICE LANE · 6 m (study reservation)',11)
  if slug=='fictional':
   v.rect(x,y+60*s,36*s,4*s,'#dddeda');v.line(x+18*s,y+60*s,x+18*s,y+62*s,dash='3 3');v.line(x+18*s,y+62*s,x-3*s,y+62*s,dash='3 3')
  for b in bs:
   bx=x+b['x']*s;by=y+b['y']*s;v.rect(bx,by,b['w']*s,b['d']*s,'#cccfc8',sw=1.5)
   if b['k']=='corridor':v.line(bx-12,by+b['d']*s/2,bx,by+b['d']*s/2,arrow=True)
   else:v.line(bx+b['w']*s/2,by-12,bx+b['w']*s/2,by,arrow=True)
   v.line(bx+b['w']*s/2,by if b['y']>=39 else by+b['d']*s,bx+b['w']*s/2,y+36*s,dash='3 3')
   if b['k']=='row':
    for i in range(1,int(b['w']/6)):v.line(bx+i*6*s,by,bx+i*6*s,by+b['d']*s,width=2)
   v.text(bx+3,by+13,b['name'],10)
   if b['r']!='flat':v.line(bx,by+b['d']*s/2,bx+b['w']*s,by+b['d']*s/2)
  v.dim(x,y+64*s+25,80*s,'80 m');v.text(560,155,'Plan key',16)
  for i,t in enumerate(['Solid grey = proposed covered ground','White = explicitly unbuilt ground','Arrow = public threshold','Dashed = service/path relationship','Paths are schematic; turning is untested','No capacity assigned from the windows']):v.text(560,183+i*23,t,13)
  v.text(560,350,'What differs here',16)
  # split note sentences
  for i,line in enumerate(note.split('; ')):v.text(560,377+i*22,line[:78],12)
  v.text(560,475,'All setbacks, plots and access reservations',13);v.text(560,495,'are research ground, not an existing game Lot.',13)
  v.text(30,605,'Street elevation · 8 px/m · all frontmost buildings, viewed from the public street',14)
  for b in bs:
   if b['y']<20:elevation(v,50+b['x']*8,750,b['w'],b['n'],8,b['k'],b['r'],color,depth=b['d'],height=b['height'])
  v.human(715,750,8);v.scale(820,724,8)
  v.text(30,796,'Assembled volume study · axonometric, not a measured perspective',14)
  iso(v,bs,230,925,4.2,color)
  v.text(30,1100,'PROPOSAL · contextual differences are intentional; do not compare density by window count. Material pass is separate.',12)
  v.save(slug+('-materials' if color else '')+'.svg')
(O/'evidence/street-proposals.json').write_text(json.dumps(contexts,indent=2))

# workplace exact game body, both floors retained
v=SVG(1160,980,'C — Workplaces need floor structure and service ground','Original G001 exact-envelope workplace proposal with ground plan, upper plan, roof plan and section.')
v.text(30,38,'C / G001: a workplace, not an oversized house',26)
v.text(30,65,'EXACT ARCHIVED BODY · 36 × 20 m · two full floors · 7 m walls · 1,440 m² counted floor · use interpretation proposed',14)
s=10
for col,(title,mode) in enumerate([('Ground / public front + rear delivery','ground'),('Upper / small offices or light assembly','upper')]):
 x=40+col*550;y=160;v.text(x,115,title,18);v.rect(x,y,360,200,'#e6e5df',sw=2)
 for xx in range(0,37,6):
  for yy in [0,10,20]:v.rect(x+xx*s-2,y+yy*s-2,4,4,'#333')
 v.rect(x,y+8*s,360,2*s,'#faf9f5');stairs(v,x+2*s,y+5*s,4,5,s);stairs(v,x+30*s,y+5*s,4,5,s)
 if mode=='ground':
  v.line(x+18*s,y,x+18*s,y+8*s);v.rect(x+12*s,y+14*s,12*s,6*s,'url(#hatch)');v.text(x+12*s+4,y+17*s,'receiving',12)
  for xx in [9,27]:v.line(x+xx*s,y-22,x+xx*s,y,arrow=True)
  v.line(x+18*s,y+22*s,x+18*s,y+20*s,arrow=True)
 else:
  for xx in [12,24]:v.line(x+xx*s,y,x+xx*s,y+8*s);v.line(x+xx*s,y+10*s,x+xx*s,y+20*s)
 v.dim(x,y-10,360,'36 m · six 6 m frame bays');v.text(x+370,y+45,'20 m',12)
 v.text(x,390,'Column rows at 0 / 10 / 20 m; floor beams require engineering.',12)
 v.text(x,412,'Two 4 × 5 m core reservations; no employee count inferred.',12)
v.text(40,465,'Section / full upper floor, supported independently of the roof',18)
x=40;y=620;s=10;v.rect(x,y-70,360,70,'#e2e1dc');v.line(x,y-35,x+360,y-35,width=3)
for xx in range(0,37,6):v.line(x+xx*s,y,x+xx*s,y-70,width=3)
v.line(x,y-72,x+180,y-75,width=2);v.line(x+180,y-75,x+360,y-72,width=2)
v.rect(x,y-78,360,5,'#b7bcb9');v.human(x+385,y,10);v.text(x,y+25,'3.5 m floor-to-floor, including structure/services; 7 m wall datum.',12)
v.text(590,465,'Roof / real falls behind the parapet',18)
x=590;y=495;v.rect(x,y,360,200,'#d7dad5');v.line(x,y+100,x+360,y+100,width=2)
for xx in [60,300]:
 v.line(x+xx,y+95,x+xx,y+10,arrow=True);v.line(x+xx,y+105,x+xx,y+190,arrow=True)
 v.rect(x+xx-3,y,6,5,'#333');v.rect(x+xx-3,y+195,6,5,'#333')
v.rect(x+145,y+40,70,35,'#adb4b3');v.poly([(x+145,y+75),(x+180,y+92),(x+215,y+75)],'#efefea');v.text(x+225,y+82,'plant + cricket',12)
v.text(x,725,'10 m fall run / 1:60 study slope = 0.167 m rise.',12)
v.text(40,770,'Rear / receiving doors and visible service route',18)
elevation(v,40,870,36,2,10,'office','flat',height=7);v.rect(190,835,35,35,'#999');v.line(208,930,208,877,arrow=True)
v.text(450,800,'Public front and deliveries use different thresholds.',14);v.text(450,825,'A dock is not automatically required: start with van receiving.',14)
v.text(450,850,'No truck court is smuggled inside the 720 m² footprint.',14);v.text(450,875,'CPL A201/A450 informs roof junctions; SCI informs framing.',14)
v.text(30,950,'PROPOSAL · G001 geometry from archived draw; no use/capacity change authorized. Floor-load, egress and service-vehicle checks unperformed.',12)
v.save('workplace.svg')

# Roof assemblies + human/material scale
v=SVG(1160,900,'D — Roof construction and scale','Original explanatory diagrams. Provisional dimensions and source-specific material modules distinguished.')
v.text(30,38,'D / Roofs are assemblies, not national silhouettes',26)
v.text(30,65,'SOURCE-CONSTRAINED DIAGRAMS · structural sizing is not certified · section scale varies only where labelled',14)
for j,(title,span,pitch) in enumerate([('Domestic truss study',10,25),('Low membrane range study',12,11),('Workshop portal study',24,6)]):
 x=40+j*375;y=260;s=11;rise=span/2*math.tan(math.radians(pitch));w=span*s
 v.text(x,110,title,18);v.line(x,y,x,y-60,width=3);v.line(x+w,y,x+w,y-60,width=3)
 v.poly([(x,y-60),(x+w/2,y-60-rise*s),(x+w,y-60)],'#d5d8d3');v.line(x,y-60,x+w,y-60)
 if j==0:
  for i in range(4):v.line(x+i*w/4,y-60,x+(i+.5)*w/4,y-60-rise*s*(1-abs((i+.5)/2-1)))
 if j==2:
  v.line(x,y-60,x+25,y-70,width=5);v.line(x+w,y-60,x+w-25,y-70,width=5)
 v.dim(x,y+20,w,f'{span} m span');v.text(x,320,f'{pitch}° → {rise:.3f} m rise',14);v.text(x,344,'All sections above: 11 px/m',12)
v.text(40,402,'Warm membrane roof / conceptual layer order',18)
for i,(t,c) in enumerate([('membrane / protection','#5c656c'),('insulation to falls','#d4cfb9'),('air/vapour layer','#777'),('deck + supporting joists','#a3aaa6')]):
 v.rect(40,430+i*28,380,20,c);v.text(440,445+i*28,t,14)
v.text(810,402,'Edge junction / schematic',16)
v.rect(865,440,28,105,'#c9cbc3');v.rect(893,522,215,12,'#a3aaa6');v.rect(893,502,215,20,'#d4cfb9')
v.line(893,500,1108,500,width=2);v.line(893,440,893,500,width=2);v.line(860,437,899,437,width=3)
v.line(897,500,850,500,arrow=True);v.line(850,500,850,547,width=2);v.text(918,467,'membrane return',11);v.text(918,487,'outlet through parapet',11)
v.text(40,568,'Return membrane up parapet; cap sheds to roof; outlet and overflow remain visible.',13)
v.text(40,590,'Layers are exploded, not thickness-scaled. C1/C2 and CA5; climate analysis still required.',12)
v.text(40,640,'Human and opening / 70 px per metre',18);v.human(65,820,70);v.rect(115,673,63,147,'#c6cbc7');v.dim(115,844,63,'0.9 m door');v.text(210,716,'1.7 m person',14);v.text(210,742,'2.1 m door height',14);v.text(210,767,'PROPOSED scale controls',12)
v.text(600,640,'Material module / 500 px per metre',18)
for row in range(4):
 for col in range(3):v.rect(600+col*112.5+(row%2)*56.25,675+row*37.5,107.5,32.5,'#b1856d')
v.text(600,855,'215 × 65 mm face + proposed 10 mm joints (MAT1).',12)
v.text(30,883,'Near-future roofs retain access and drainage; PV/green roofs need load and waterproofing design. They are not random decals.',12)
v.save('construction.svg')
print('Created original SVG comparisons, six contextual streets, material variant and dimension data.')

v=SVG(1160,760,'E — What the sources actually dimension','Original dimensional abstractions, with unknown boundaries left open. Source evidence is distinguished from proposed geometry.')
v.text(30,38,'E / Evidence controls: preserve what the drawing does not say',25)
v.text(30,64,'Original numerical abstractions · not copied plans · open the cited source sheet beside each diagram',14)
x=40;y=150;s=12;ft=.3048
v.text(x,108,'US1 / South Bend sixplex proposal',18)
v.poly([(x,y),(x+40*ft*s,y),(x+40*ft*s,y+40*ft*s),(x+34*ft*s,y+40*ft*s),(x+34*ft*s,y+66*ft*s),(x,y+66*ft*s)],'#d9dcd5')
v.dim(x,y-10,40*ft*s,'40 ft / 12.192 m');v.text(x+155,y+160,'66 ft body',12)
v.text(x,410,'Front 40×40 ft + rear 34×26 ft',12);v.text(x,432,'2,484 ft² exterior polygon / floor.',12);v.text(x,454,'Finished-gross schedule differs.',12)
x=400;y=150;s=12
v.text(x,108,'NL1 / Amersfoort Type B module',18)
v.rect(x,y,5.1*s,9.4*s,'#eee');v.line(x,y,x+5.1*s,y,dash='4 3',width=2)
v.dim(x,y-10,5.1*s,'5.1 m');v.text(x+80,y+50,'9.4 m depth',12)
v.text(x,280,'Beukmaat, not measured external width.',12);v.text(x,304,'109 m² GBO is not gross floor.',12)
x=785;y=150
v.text(x,108,'CA2 / Sacramento court diagram',18)
v.rect(x,y,5.4864*10,80,'#d9dcd5');v.rect(x+14.9352*10,y,5.4864*10,80,'#d9dcd5');v.line(x,y+80,x+204.216,y+80,dash='5 4')
v.dim(x,y-10,204.216,'67 ft / 20.4216 m overall');v.text(x,245,'18 ft / 5.4864 m wing fronts',12);v.text(x,269,'Rear depth intentionally unresolved.',12);v.text(x,293,'40 ft refers to front LOT line.',12)
v.text(400,350,'ES1 / Mediterranean roof member',18)
v.rect(400,385,270,35,'#d5d8d2');v.rect(400,376,270,9,'#777');v.text(400,449,'200 mm one-way RC member (p45).',12);v.text(400,472,'Diagram thickness exaggerated; not floor height.',12)
v.text(785,350,'DK1 / Tingbjerg roof pitch',18)
v.poly([(785,420),(845,408.34),(905,420)],'#d5d8d2');v.text(930,420,'11°',16)
v.text(785,449,'Pitch documented; 12 m span proposed.',12);v.text(785,472,'Calculated rise 1.166 m, not measured.',12)
v.text(40,535,'The conflicts change the modelling decision',20)
for i,t in enumerate(['A schedule area is not necessarily an external-envelope polygon. Keep both numbers with their definitions.', 'A narrow facade wing is not an independently occupied house. The courtyard diagram does not authorize extra tenancies.', 'A construction member thickness is not a floor-to-floor height; a roof angle is not a span capacity.', 'These sources constrain proposals. They do not supply a measured regional probability distribution.']):v.text(40,575+i*32,t,15)
v.text(30,730,'Inspection: US1 PDF22/printed21; NL1 PDF15/printed14; CA2 p49; ES1 p45; DK1 p6. Exact URLs and uncertainty in sources.csv.',12)
v.save('evidence-controls.svg')

v=SVG(1160,830,'F — Low housing and service sides','Original proposals for detached, semi-detached and attached houses. Shared scale, different access and roofs.')
v.text(30,38,'F / Low housing: a house, a pair, a range',26)
v.text(30,65,'PROPOSALS · all plan and elevation dimensions in metres · 12 px/m · no measured occupancy asserted',14)
for i,(title,w,d,n,kind) in enumerate([('Detached / postwar family',14,10,1,'house'),('Semi-detached / two entries',12,10,2,'row'),('Attached / four-house range',24,10,2,'row')]):
 x=40+i*375;y=145;s=12;v.text(x,112,title,18);v.rect(x,y,w*s,d*s,'#e2e4dd')
 modules=1 if kind=='house' else int(w/6)
 for k in range(modules):
  mx=x+k*w*s/modules
  if k:v.line(mx,y,mx,y+d*s,width=2)
  stairs(v,mx+1*s,y+1*s,2,3,s) if n>1 else None
  v.line(mx+2*s,y-15,mx+2*s,y,arrow=True);v.line(mx+3*s,y+d*s,mx+3*s,y+d*s+25,arrow=True)
  v.line(mx,y+4*s,mx+w*s/modules,y+4*s)
 v.dim(x,y-8,w*s,f'{w} m');v.text(x,y+d*s+48,'Front entry / service rooms; garden living side.',12)
 elevation(v,x,445,w,n,s,kind,'gable',depth=d);v.human(x+w*s+15,445,s)
 v.text(x,482,'Rear / garden elevation',14)
 v.rect(x,650-n*3.2*s,w*s,n*3.2*s,'#e2e4dd')
 rise=d/2*math.tan(math.radians(18));v.rect(x,650-(n*3.2+rise)*s,w*s,rise*s,'#b8bbb9')
 for px in [x+.2*s,x+(w-.2)*s]:v.line(px,650-n*3.2*s,px,650+4,width=2)

 for k in range(modules):
  mx=x+k*w*s/modules;v.rect(mx+1*s,650-2.2*s,3*s,2.2*s,'#a9b3b4')
  if n>1:v.rect(mx+1*s,650-5.8*s,2.4*s,1.5*s,'#a9b3b4')
 v.line(x,650,x+w*s,650);v.text(x,685,'Garden doors; gutters and downpipes at ends.',12)
v.text(30,735,'DK2 supports front/garden asymmetry and truss logic; US1/NL1 constrain paired/attached organisation. All sizes here are authored.',12)
v.text(30,763,'Domestic modules cannot be multiplied inside one game Building without deciding how architectural homes map to tenancies.',12)
v.scale(40,790,12);v.save('low-housing.svg')

v=SVG(1160,800,'G — Retail layout and accumulated history','Original proposed same shop body on two sites, plus retrofit sequence preserving body dimensions.')
v.text(30,38,'G / Same retail building. Different land and access.',26)
v.text(30,65,'PROPOSAL · 24 × 16 m one-floor shop · 40 × 40 m site · 8 px/m · no parking capacity inferred',14)
for i,(title,by) in enumerate([('Retained roadside layout',14),('Street-edge redevelopment',0)]):
 x=40+i*565;y=135;s=8;v.text(x,108,title,20);v.rect(x,y,40*s,40*s,'#f0f0e9');v.rect(x,y-20,40*s,16,'#ccc');v.text(x+340,y+5,'street',12)
 v.rect(x+2*s,y+by*s,24*s,16*s,'#d1d5ce')
 v.rect(x+28*s,y,6*s,40*s,'#ddd');v.text(x+285,y+230,'side access',12)
 py=y+(2 if by else 20)*s
 for z in range(4):v.rect(x+(2+z*6)*s,py,5*s,6*s,'none')
 v.line(x+14*s,y+by*s-12,x+14*s,y+by*s,arrow=True);v.line(x+32*s,y+36*s,x+25*s,y+(by+14)*s,arrow=True)
 v.text(x,490,'Public threshold and van delivery remain separate.',13)
v.text(30,545,'History without a heritage-dominated street',22)
for i,(title,action) in enumerate([('1960s body','Original roof + broad glazing'),('1990s repair','Changed shopfront; patched roof'),('Near-future retrofit','External shade + roof plant/PV')]):
 x=40+i*370;v.text(x,580,title,17);elevation(v,x,710,24,1,10,'shop','flat',True)
 if i==1:v.rect(x+20,685,48,25,'#728b90')
 if i==2:
  for b in range(4):
   xx=x+20+b*45;v.poly([(xx,660),(xx+32,652),(xx+32,656),(xx,664)],'#566c79');v.line(xx,664,xx,678);v.line(xx+32,656,xx+32,678)
  v.line(x,680,x+240,680,width=3)
 v.text(x,745,action,13)
v.text(30,782,'CA3/CA4 support site/service relationships; C2/CA5 inform roof work. Dates are invented study history, not simulated building ages.',12)
v.save('retail-history.svg')

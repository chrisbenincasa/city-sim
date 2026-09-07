#!/usr/bin/env python3
"""Rebuild research tables and original analytical drawings; standard library only."""
from pathlib import Path
import csv, hashlib, json, math, statistics, subprocess, html
O=Path(__file__).resolve().parent
ROOT=O.parents[2]
DATE='2026-09-06'
def write_csv(name, fields, rows):
    with (O/name).open('w',newline='') as f:
        w=csv.DictWriter(f,fields);w.writeheader();w.writerows(rows)
S=[]
def source(id,title,publisher,url,page,rights,inspection='Source text inspected online'):
    S.append(dict(source_id=id,title=title,publisher_author=publisher,url=url,sheet_page=page,access_date=DATE,reuse_standing=rights,inspection=inspection))
HE='Official entry text OGL v3 except stated exceptions; archive/contributor photographs and maps separately copyrighted; linked only.'
LINK='Copyright or reuse permission not established; linked only; no image redistributed.'
source('S01','Design of Lifetime Homes (2009 study; published July 2012)','Hunt Thomson / HTA Architects for DCLG','https://data.parliament.uk/DepositedPapers/Files/DEP2012-1192/Designoflifetimehomes_final.pdf','PDF pp7-9 LTH 201; pp23-25 LTH 207','Publication information OGL; drawing sheets expressly reserve HTA copyright and prohibit copying/scaling without agreement. Original analytical dimension rectangles only.','Downloaded; plans LTH 201 and LTH 207 visually inspected; schedule dimensions transcribed, not scaled.')
source('S02','33 Porson Road; 19/0859/FUL; committee 6 November 2019','Cambridge City Council / Andy White','https://democracy.cambridge.gov.uk/documents/s48056/190859FUL','PDF pp1-2; p7 comparison table (§8.2)',LINK,'Online report inspected; download and screenshot unavailable; no plan measured.')
source('S03','Land at Mill Road Epsom; 18/00271/FUL; committee 13 December 2018','Epsom and Ewell Borough Council / Tom Bagshaw','https://democracy.epsom-ewell.gov.uk/documents/g569/Public%20reports%20pack%2013th-Dec-2018%2019.30%20Planning%20Committee.pdf?T=10','Printed/PDF pp91-92; §4.2-4.6',LINK,'Downloaded; p92 dimension schedule visually inspected. Proposal dimensions, not verified as-built.')
source('S04','17–23 Constance Street, Saltaire; NHLE 1283154','Historic England','https://historicengland.org.uk/listing/the-list/list-entry/1283154','Details',HE)
source('S05','Goldsmith Street','Mikhail Riches with Cathy Hawley; client Norwich City Council','https://www.mikhailriches.com/project/goldsmith-street/','Project text; 059_N252 photograph',LINK,'Project text and linked street photograph visually inspected; no individual footprint measured.')
source('S06','24–32 Shirley Street and 107 Saltaire Road; NHLE 1133558','Historic England','https://historicengland.org.uk/listing/the-list/list-entry/1133558','Details; IOE01/04549/25 photo link',HE)
source('S07','15–17 Victoria Road; NHLE 1133525','Historic England','https://historicengland.org.uk/listing/the-list/list-entry/1133525','Details; archive photograph',HE)
source('S08','Historic Townscape Characterisation: Lincoln Townscape Assessment','Historic England / Lincoln townscape study','https://historicengland.org.uk/research/results/reports/7032/HistoricTownscapeCharacterisationTheLincolnTownscapeAssessmentacasestudy','West Parade / Beaumont Fee character extract; Figure 5',LINK,'Character text and figure caption inspected; photographic metric extraction not attempted.')
source('S09','Isokon Flats; NHLE 1379280','Historic England','https://historicengland.org.uk/listing/the-list/list-entry/1379280','Details: plan and exterior',HE)
source('S10','Marmalade Lane: Housing Choice and construction','Cambridge Cohousing / Marmalade Lane','https://www.marmaladelane.co.uk/index.html','Housing Choice: types A–D; Precision-Made; Space for Community',LINK,'Text inspected; type D image inspected and is an unscaled axonometric, NOT a floor plan; brochure link returned 404.')
source('S11','Peabody Estate Blackfriars; NHLE 1376595','Historic England','https://historicengland.org.uk/listing/the-list/list-entry/1376595','Details: plan, exterior, access',HE)
source('S12','Lichfield Court; NHLE 1390787','Historic England','https://historicengland.org.uk/listing/the-list/list-entry/1390787','Details',HE)
source('S13','Conserve as Found: the JW Evans Silverware Factory, Birmingham','ASCHB Transactions vol34 (2011), conservation project account','https://www.aschb.org.uk/wp-content/uploads/2022/01/Vol-34.pdf','Printed pp61-71; p63 Fig4 (PDF p65); p64 employment',LINK,'Downloaded; aerial roof/rear/front photograph Fig4 visually inspected. Dimensions not extracted from perspective.')
source('S14','J W Evans roofs, DP045426, 15 May 2008','Historic England Archive / Peter Williams','https://historicengland.org.uk/images-books/photos/item/DP045426','DP045426; from Glass Room 54S04','© Historic England Archive; linked only.','Photo catalogue caption inspected; image not inspected at full resolution.')
source('S15','Coffin Furniture Works; NHLE 1380231','Historic England','https://historicengland.org.uk/listing/the-list/list-entry/1380231','Details: plan; front; rear courtyard; interiors',HE)
source('S16','Conserving Georgian and Victorian terraced housing, HEAG277 (2020)','Historic England','https://historicengland.org.uk/images-books/publications/conserving-georgian-victorian-terraced-housing/heag277-conserving-georgian-and-victorian-terraced-housing/','Printed pp6,8-9; roof and rear-extension sections',LINK,'Online PDF text inspected; no dimensions scaled.')
source('S17','Standard Format Bricks','Wienerberger UK','https://www.wienerberger.co.uk/products/brick/standard-format-bricks.html','What is a standard format brick?',LINK)
source('S18','Change of Roof Pitch; clay plain tile detail','Marley','https://www.marley.co.uk/-/media/066d3695d9a94cb7a8019b1db5175f1f.pdf?rev=27071efb996241869e2faffc511005fe','800CPT35 revision A; sheet1',LINK,'Online figured dimensions read; construction relationship from technical detail; no pixel measurement.')
source('S19','SIGA Natural Slate: A Guide','SIG Roofing / SIGA','https://www.sigaslate.co.uk/wp-content/uploads/documents/SIGA-Slate-Brochure.pdf','PDF pp20-21: exposure, rafter length and headlap tables',LINK,'Text inspected; flattened headlap columns not safe to transcribe as pitch-specific values.')
source('S20','Flat Roof Design Considerations','Bauder','https://www.bauder.co.uk/technical-centre/standards-and-knowledge/flat-roof-design-considerations','Falls / design considerations',LINK,'Historical generic 1:40 design / 1:80 finished advice read; not treated as a universal current requirement.')
source('S21','BS6229:2025 key changes','BauderACE','https://www.bauderace.co.uk/technical-centre/standards-and-knowledge-9835597dbd573da8ab6ee78cd0e3b81f/bs-6229-2025-key-changes','Key changes; search-index text',LINK,'Indexed text says design fall now depends on analysis; full page redirects to login. No current compliance conclusion drawn.')
source('S22','1613 Canal Ring','Amsterdam City Archives','https://www.amsterdam.nl/stadsarchief/canon/windows/12/','Back entrance; canal expansion',LINK)
source('S23','Retrofitting Boston Buildings for Flooding','City of Boston','https://www.boston.gov/sites/default/files/imce-uploads/2017-01/retrofitting_report_10.7.2016.pdf','Triple-Decker typology',LINK,'Indexed city report text; wood frame, stoops and openings; not a measured building example.')
source('S24','Narbonne House','US National Park Service','https://www.nps.gov/places/narbonne-house.htm','Place description; NPS photograph','NPS photo attribution shown; use linked image only until individual image credit checked.','Place description inspected; HABS MA-802 drawings located but inaccessible; no dimensions used.')
source('S25','Goldsmith Street street photograph 059_N252','Mikhail Riches website; project photography credited to Tim Crocker','https://www.mikhailriches.com/wp-content/uploads/2022/10/059_N252_medium-e1750261172175.jpg','059_N252; project page image12',LINK,'Downloaded to temporary storage and visually inspected; no metric inference.')
source('S26','Marmalade Lane Type D diagram','Marmalade Lane / project designers','https://www.marmaladelane.co.uk/uploads/development/images/typeD.jpg','Type D',LINK,'Downloaded and visually inspected: axonometric rendering, not measured evidence.')
source('R01','Checkpoint draw list and repeated draw','city-sim d9addc3','artifacts/visual-study/roof-proportions/daylight-near.tsv','row/building and roof layers','Repository artifact; preserved unchanged; screenshot linked from atlas.','Recomputed with audit_snapshot.py; not a new simulation run.')
source('R02','Geometry and capacity source','city-sim HEAD 7916db4022066a741531087aa1ed6325a448303f','src/Borough.Godot/Main.Massing.cs','Buildings; Wings; CapFor; RoofHeight; World.TryDeclaredOccupancy; BuildingPlan; shopping.toml','Repository source, no changes.','Read at HEAD and compared against d9addc3; see audit for exact owners.')
source('P01','Prototype model briefs and drawing arithmetic','This research session','model-briefs.md','M1–M3','Original research drawing; no third-party image used.','All proposed dimensions, not measurements.')
write_csv('sources.csv',list(S[0]),S)
SM={s['source_id']:s for s in S}
E=[];D=[]
def example(id,family,name,place,period,sources,observed,lesson,gaps,diagram='house',dims=None,standing='Built example; metric survey incomplete',views='Source-linked exterior; rear/roof/section not independently verified'):
    E.append(dict(id=id,family=family,name=name,place=place,period=period,sources=sources,observed=observed,lesson=lesson,gaps=gaps,diagram=diagram,dims=dims,standing=standing,views=views))
def dim(e,s,q,v,unit='m',status='documented',method='Transcribed source value',caveat='',page=''):
    D.append(dict(example_id=e,source_id=s,quantity=q,value=v,units=unit,evidence_status=status,method=method,caveat=caveat,sheet_page=page or SM[s]['sheet_page']))
example('D01','Detached','HTA four-bedroom study house','England; generic private house type','2009 / published 2012',['S01'],'Two storeys; internal envelope 6.9 × 8.2 m (approximate schedule). LTH 207 has front entrance, central stair, rear kitchen/utility exit.','A wide-front house still has a room-and-stair plan, not a window grid.','Unlocated archetype, not independent built survey; roof, setbacks, opening dimensions unknown.',dims=[6.9,8.2],standing='Published design archetype; internal envelope only',views='Ground/first plans LTH 207 inspected; no built photographs or section.')
example('D02','Detached','33 Porson Road, existing house','Cambridge','1950s origin reported in representations; altered',['S02'],'Existing detached house: width 15.9 m, unequal depths 11.4 / 15.2 m; eaves 5.4 m, roof 8.2 m.','Even a large house can be an assembly of unequal wings. A bounding box is not sealed footprint area.','No as-built survey supplied; no opening or roof-span dimensions; do not equate proposal to existing house.',dims=[15.9,15.2],standing='Existing dimensions reported in planning assessment; bounding extent',views='Report table; plans listed but not inspected; rear and roof photographs missing.')
example('D03','Detached','Mill Road Block E','Epsom','2018 proposal',['S03'],'Detached proposal; 9.5 × 9.5 m; eaves 5.5 m and ridge 7.8 m; described as three storeys.','Storey counts can include roof accommodation; eaves height / storeys is not floor-to-floor height.','Completion unverified; section, roof form and opening sizes missing.',dims=[9.5,9.5],standing='Documented proposal, not measured as-built',views='Dimension schedule visually checked; plans/photographs not acquired.')
example('T01','Terraces','17–23 Constance Street','Saltaire, West Yorkshire','Completed by 1861',['S04'],'Two storeys and two bays per house; stone walls, Welsh slate; projecting gabled ends, gutter brackets and a level step.','Keep the row coherent; let end conditions and terrain account for variations.','Metric frontage, pitch, plot and access plan missing. Listing is not a measured survey.',diagram='terrace')
example('T02','Terraces','HTA two-bedroom study house','England; generic private house type','2009 / published 2012',['S01'],'Approximate internal width/depth 3.9 / 8.0 m; two storeys; 62 m² GIA; stated external frontage 4500 mm.','A narrow plan locates stairs and separate front/rear rooms.','Not a northern historic sample; external depth and roof dimensions missing.',diagram='terrace',dims=[3.9,8.0],standing='Published design archetype; internal envelope only',views='Ground/first plans LTH 201 inspected; no photos or roof section.')
example('T03','Terraces','Goldsmith Street','Norwich','Contemporary; 2019 award',['S05','S25'],'Architect specifies 14 m wide streets. Viewed photo shows repeated entrances, deep opening surrounds, rainwater pipes and asymmetric roof lines.','Later infill can keep the street rhythm while changing wall/roof construction.','Street-width endpoints not defined here; not a carriageway width. Individual footprint, heights and pitch unmeasured.',diagram='terrace',views='Street photo visually inspected; linked project images; rear and measured section unavailable.')
example('M01','Corner / mixed use','Shirley Street and 107 Saltaire Road','Saltaire','Completed by 1861',['S06'],'Two-storey row with shopfronts at ends; No107 is a single-storey rear attachment with rounded corner.','A corner can be made by an attached shop; it need not inflate the entire house.','No verified shop/residential internal separation, metric widths or capacity.',diagram='corner')
example('M02','Corner / mixed use','15–17 Victoria Road','Saltaire','Completed by 1868',['S07'],'Two-storey houses and shops; nine bays across the composition; central shop entrance and parapet.','Shop scale and the upper facade composition differ. Parapet is not proof of a flat roof.','Not independently established as a corner; retained as a mixed-use range control. Footprint, stairs and dwelling counts missing.',diagram='shop')
example('M03','Corner / mixed use','1 West Parade / West Parade corner condition','Lincoln','Historic streets with later alterations; precise date not extracted',['S08'],'Character study identifies rounded corner at No1 and larger corner compositions; documents modern shopfront insertions nearby.','Turn the frontage around the junction; later shopfront changes belong to specific bays.','Mixed use at No1 not established; public-house and shop evidence belongs to nearby examples. No metre dimensions.',diagram='corner',standing='Local character example; use and metric gaps')
example('A01','Small apartments','Mill Road Blocks A–C','Epsom','2018 proposal',['S03'],'C: 24 × 7.8 m, eaves 6.5 m, ridge 8.5 m, six flats; B/B1: 18 × 9.8 m; A: 18.5 × 16 m.','Width alone does not make a giant house. Keep the dimensional pairs and access questions together.','Same project, not three independent examples. Roof storeys and stair layouts unverified.',diagram='apartments',dims=[24,7.8],standing='Documented proposal; C drawn, A/B retained in table',views='Dimension schedule visually inspected; not a verified built elevation.')
example('A02','Small apartments','Isokon Flats','Hampstead, London','1933–34; subsequent alterations',['S09'],'Reinforced concrete; flat roof; four storeys and penthouse; gallery access and stair tower; listing describes 36 flats.','Flat roofs belong to an assembly with edges, access and drainage; galleries explain repeated units.','Not ordinary northern vernacular; deliberate construction contrast. Historical listing count not current occupancy; metre dimensions absent.',diagram='gallery')
example('A03','Small apartments','Marmalade Lane apartments','Cambridge','Completed development; 2018/19 period',['S10','S26'],'Documented 75 m² apartment type with shared lobby/lift; separate paired-flat types have their own front doors. Timber construction.','Both shared and individual access exist: choose a plan before placing doors.','Type D illustration has no scale; unit area does not establish whole-block footprint or occupancy.',diagram='apartments',views='Unscaled Type D axonometric inspected; project photos linked; measured plans/sections missing.')
example('C01','Courtyard / grouped blocks','Peabody Estate Blackfriars','London','1871 with later additions',['S11'],'Original four-storey blocks: seven-window court fronts, six-window backs with stair recesses; two connected courts, later five-storey additions.','A courtyard may be formed by separate blocks with gaps, not a continuous hollow rectangle.','Court dimensions, wing depth and dwelling areas missing; no northern replication justified.',diagram='court')
example('C02','Courtyard / grouped blocks','Lichfield Court','Richmond, London','1934–35',['S12'],'211 flats over 17 shops across two blocks; courtyard galleries, multiple stairs/lifts; lower ground on falling site; flat roofs.','Large perimeter forms need an access system and explicit corner/level handling.','Estate-level counts not per wing; no court or structural span dimensions; scale exceeds the initial small block.',diagram='court')
example('C03','Courtyard / grouped blocks','Marmalade Lane shared garden','Cambridge','Contemporary',['S10'],'Terraces and apartments frame shared garden and car-free lane; 42 homes across whole scheme.','Adapt family to grouped garden blocks; a continuous ring is not required.','Same project as A03: not independent evidence. Shared-garden dimensions unavailable.',diagram='court',standing='Built grouped housing; cross-listed, counted once as a project',views='Project garden and lane photographs linked; not a measured site plan.')
example('W01','Workshops','J W Evans, 54–57 Albion Street','Birmingham Jewellery Quarter','1836 houses; later nineteenth-century workshops',['S13','S14'],'Narrow brick/slate rear ranges and later glazed yard roofs; aerial photograph shows several roof assemblies behind domestic fronts.','Add a workshop range because production and access need it; valleys, chimneys and glazed links follow that history.','No measured spans, heights or worker area; perspective photo not scaled.',diagram='workshop',views='Fig4 aerial (front/rear/roof) visually inspected; DP045426 roof photograph linked; section missing.')
example('W02','Workshops','Newman Brothers Coffin Works','13–15 Fleet Street, Birmingham','Listing dates 1892; conservation account says built 1894',['S15'],'Nine-bay, three-storey front; waggon entrance; narrow rear court and workshop ranges; red brick, blue plinth, slate.','Distinguish loading passage, office stair and workroom access. Parallel roofs alone do not describe a factory.','Date discrepancy retained; footprint, pitch, openings and structural dimensions missing.',diagram='workshop')
# Metric schedules: keep internal, envelope and original proposal units distinct.
for e,w,d in [('D01',6.9,8.2),('T02',3.9,8.0)]:
    for q,v in [('approx_max_internal_width',w),('approx_max_internal_depth',d)]:dim(e,'S01',q,v,caveat='Approximate published schedule; not external footprint or statistical range.',page='PDF p23' if e=='D01' else 'PDF p7')
    dim(e,'S01','storeys',2,'storeys',page='PDF p23' if e=='D01' else 'PDF p7')
dim('D01','S01','frontage_width',7520,'mm',page='PDF p24',caveat='Published frontage; do not derive exterior depth from internal area.')
dim('D01','S01','gross_internal_area_schedule',112.2,'m2',page='PDF p23',caveat='Adjacent p24 gives 112.35 m2; discrepancy preserved.')
dim('D01','S01','gross_internal_area_description',112.35,'m2',page='PDF p24',caveat='Schedule p23 gives 112.2 m2; not silently reconciled.')
dim('D01','S01','design_person_places',6,'design persons',page='PDF p24',caveat='Design designation, not surveyed occupancy.')
dim('T02','S01','frontage_width',4500,'mm',page='PDF p8')
dim('T02','S01','gross_internal_area',62,'m2',page='PDF pp7-8')
dim('T02','S01','design_person_places',4,'design persons',page='PDF p8',caveat='Design designation, not surveyed occupancy.')
for q,v in [('width',15.9),('depth_west',11.4),('depth_east',15.2),('eaves_height',5.4),('roof_height',8.2)]:dim('D02','S02',q,v,caveat='Existing house reported in planning table; not independently surveyed.')
for q,v in [('proposed_width',16.6),('proposed_max_depth',17),('proposed_eaves',5.8),('proposed_ridge',9),('proposed_link_roof_height',8),('proposed_west_boundary_gap',.85),('proposed_east_boundary_gap',.8)]:dim('D02','S02',q,v,caveat='2019 proposal; construction not verified. Not the existing house.')
for e,w,d,h,r,st,fl,sec in [('D03',9.5,9.5,5.5,7.8,3,None,'4.6'),('A01-C',24,7.8,6.5,8.5,3,6,'4.4'),('A01-B',18,9.8,6.5,8.5,3,6,'4.3'),('A01-A',18.5,16,9,11.5,4,10,'4.2')]:
    for q,v in [('width',w),('depth',d),('eaves_height',h),('ridge_height',r)]:dim(e,'S03',q,v,caveat='Documented 2018 proposal; as-built not verified.',page='PDF/printed p92 §'+sec)
    dim(e,'S03','storeys',st,'storeys',caveat='Reported count may include roof accommodation; no equal floor-height inference.',page='p92 §'+sec)
    if fl:dim(e,'S03','dwelling_count',fl,'flats',caveat='Per proposed block; B and B1 each six. Not people.',page='p92 §'+sec)
for e,s,st,bay in [('T01','S04',2,2),('M01','S06',2,2),('M02','S07',2,9),('W02','S15',3,9)]:
    dim(e,s,'storeys',st,'storeys');dim(e,s,'facade_bays',bay,'bays',caveat='Per house for T01/M01; whole front range for M02/W02.')
dim('M01','S06','No107_attachment_storeys',1,'storeys')
dim('T03','S05','street_width',14,caveat='Architect description; measurement endpoints not given. Not carriageway width.')
dim('A02','S09','main_storeys',4,'storeys',caveat='Plus penthouse.')
dim('A02','S09','listing_flat_count',36,'flats',caveat='Historical/altered listing count; not current occupied households.')
dim('A03','S10','Type_D_apartment_area',75,'m2',caveat='Published unit area; measurement convention not stated; not block floorplate.')
dim('A03','S10','paired_ground_flat_area',51,'m2',caveat='Separate type; not Type D.')
dim('A03','S10','paired_first_flat_area',61,'m2',caveat='Separate type; not Type D.')
dim('C03','S10','whole_development_homes',42,'homes',caveat='Same project as A03; houses and flats combined.')
dim('C01','S11','original_block_storeys',4,'storeys');dim('C01','S11','court_front_window_range',7,'windows across');dim('C01','S11','back_window_range',6,'windows across')
dim('C02','S12','whole_estate_flats',211,'flats',caveat='Two blocks; no per-wing allocation inferred.');dim('C02','S12','whole_estate_shops',17,'shops',caveat='Original units; some combined.')
dim('C02','S12','storeys',7,'storeys',caveat='Includes lower-ground floor; upper two set back.')
dim('W01','S13','initial_terraced_houses',4,'houses',page='Printed p63');dim('W01','S13','early_rear_range_storeys',2,'storeys',page='Printed p63')
dim('W01','S13','peak_employment_upper_bound',60,'workers',page='Printed p64',caveat='Up to 60 in its heyday; no date or floor-area denominator supplied.')
for q,v in [('brick_length',215),('brick_width',102.5),('brick_height',65)]:dim('MAT01','S17',q,v,'mm',caveat='Modern standard format; not a measurement of historic masonry.')
for q,v in [('plain_tile_length',265),('plain_tile_width',165),('eaves_tile_length',215),('minimum_headlap',65),('batten_width',38),('batten_depth',25)]:dim('MAT02','S18',q,v,'mm',caveat='Specific manufacturer detail 800CPT35 A; not every clay roof.')
dim('MAT02','S18','course_gauge_at_65mm_headlap',100,'mm',method='Arithmetic: (265 - 65) / 2 for double lap',caveat='Derived from this detail; do not expose the entire 265mm tile length.')
dim('MAT03','S19','catalogue_slate_length',500,'mm',caveat='One listed product size; exposure and headlap govern application.')
dim('MAT03','S19','catalogue_slate_width',250,'mm',caveat='One listed size; not measured on atlas roofs.')
for e in E:
    existing={x['quantity'] for x in D if x['example_id']==e['id']}
    for q,u in [('plot_width','m'),('plot_depth','m'),('floor_to_floor_height','m'),('window_width','m'),('window_height','m'),('roof_pitch','degrees'),('roof_clear_span','m')]:
        if q not in existing:dim(e['id'],e['sources'][0],q,'',u,'missing','Not established',e['gaps'])
for e,w,d,h,r in [('M1',4.8,9.6,6,4.8*math.tan(math.radians(35))),('M2',9.6,12,9.6,4.8*math.tan(math.radians(35))),('M3',24,16,10.5,.45)]:
    for q,v in [('footprint_width',w),('footprint_depth',d),('wall_height',h),('roof_rise_or_parapet',round(r,4))]:dim(e,'P01',q,v,status='proposed',method='Model brief parameter / trigonometry',caveat='Research prototype, not measured architecture or approved content.')
for e,q,v,u in [
 ('M1','roof_pitch',35,'degrees'),('M1','floor_to_floor',3,'m'),('M1','house_frontage',4.8,'m'),('M1','house_count',5,'dwellings'),('M1','plot_depth',23,'m'),('M1','front_setback',1.2,'m'),('M1','roof_eaves_projection',.25,'m'),('M1','door_width',.95,'m'),('M1','door_height',2.1,'m'),('M1','window_width',1.05,'m'),('M1','window_height',1.5,'m'),
 ('M2','roof_pitch',35,'degrees'),('M2','ground_storey_height',3.6,'m'),('M2','upper_storey_height',3,'m'),('M2','home_count',2,'dwellings'),('M2','shop_count',1,'shop'),('M2','rear_attachment_width',3.6,'m'),('M2','rear_attachment_depth',4.8,'m'),
 ('M3','floor_to_floor',3.5,'m'),('M3','corridor_width',2,'m'),('M3','circulation_area_per_floor',86,'m2'),('M3','unit_envelope_mean_before_walls',74.5,'m2'),('M3','architectural_dwelling_count',12,'dwellings'),('M3','roof_design_fall_denominator',40,'1:n'),('M3','roof_fall_run',8,'m'),('M3','roof_fall',.2,'m'),('M3','roof_headhouse_height',2.4,'m'),('MAT01','nominal_mortar_joint',10,'mm'),('MAT01','nominal_course_module',75,'mm'),('MAT03','prototype_headlap',100,'mm'),('MAT03','prototype_course_gauge',200,'mm')]:
    dim(e,'P01',q,v,u,'proposed','Model brief parameter or arithmetic','Not a surveyed dimension, code-compliance claim or approved game capacity.')
write_csv('dimensions.csv',list(D[0]),D)
(O/'evidence/catalogue.json').write_text(json.dumps(E,indent=2)+'\n')
print(f'{len(S)} sources; {len(E)} atlas records; {len(D)} dimensional/evidence-gap rows')
subprocess.run(['python3',str(O/'audit_snapshot.py')],check=True)
game=list(csv.DictReader((O/'evidence/game-dimensions.csv').open()))
write_csv('dimensions.csv',list(D[0]),D+game)
esc=html.escape
def link(s,label=None):
    u=SM[s]['url']
    if s=='R01':u='../../../../'+u
    if s=='R02':u='../../../../'+u
    if s=='P01':u='../'+u
    return f'<a href="{esc(u,quote=True)}">{esc(label or s)}</a>'
def diagram(e):
    typ=e['diagram'];parts=[]
    def box(x,y,w,h,c='#be8965'):parts.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="{c}" stroke="#273b43"/>')
    def line(x,y,a,b):parts.append(f'<path d="M{x} {y}L{a} {b}" stroke="#273b43" fill="none"/>')
    def t(x,y,s):parts.append(f'<text x="{x}" y="{y}" font-size="11">{esc(s)}</text>')
    if e['dims']:
        w,d=e['dims'];scale=min(250/w,125/d);x=40;y=24
        box(x,y,w*scale,d*scale)
        t(x,y-7,f'{w:g} m');t(x+w*scale+5,y+18,f'{d:g} m')
        t(40,179,'Internal envelope' if e['id'] in ('D01','T02') else 'Bounding extent only')
        t(40,194,'Units proportional; no invented room or roof geometry')
    elif typ in ('court','workshop'):
        box(45,28,245,25);box(45,53,35,100);box(255,53,35,100)
        if typ=='court':box(105,128,125,25);t(115,92,'open ground');t(100,174,'gaps / gates / stairs')
        else:
            box(108,65,55,88);box(183,65,40,88);line(108,92,163,92);line(183,110,223,110)
            t(105,178,'ranges + yard + loading route')
        t(80,16,'street-facing range');t(43,197,'Topology study only — not measured site layout')
    else:
        n=4 if typ in ('terrace','shop','dutch') else 1
        for i in range(n):
            x=30+i*75;w=75 if n>1 else (130 if typ=='decker' else 280)
            box(x,72,w,90)
            if typ=='decker':
                box(x,32,w,40)
                box(x+w,60,45,102,'#d7e9e9');line(x+w,94,x+w+45,94);line(x+w,128,x+w+45,128)
                box(x-3,22,w+6,10,'#63717b');line(x,76,x+w,76);line(x,118,x+w,118)
            elif typ=='gallery':box(x-5,52,w+10,20,'#63717b');line(x,104,x+w,104);line(x,132,x+w,132)
            elif typ=='terrace':
                box(x,44,w,28,'#63717b')
                if e.get('id')=='T01' and i in (0,3):parts.append(f'<path d="M{x} 72L{x+w/2} 35L{x+w} 72Z" fill="#be8965" stroke="#273b43"/>')
            elif typ=='apartments':box(x-3,62,w+6,10,'#63717b')
            else:parts.append(f'<path d="M{x} 72L{x+w/2} 35L{x+w} 72Z" fill="#63717b" stroke="#273b43"/>')
            for k in range(2 if n>1 else 6):box(x+10+k*(25 if n>1 else 42),90,13,22,'#d7e9e9')
            if typ=='shop':box(x+6,128,w-12,34,'#d7e9e9')
            else:box(x+12,135,14,27,'#273b43')
            if typ=='corner':box(243,117,86,45,'#cd9c75');t(225,185,'low attachment')
        t(30,198,'Family concept only — not a reconstructed elevation')
    return '<svg role="img" aria-label="'+esc(e['name'])+' analytical diagram" viewBox="0 0 380 210">'+''.join(parts)+'</svg>'
style='''body{margin:0;background:#f4f0e6;color:#263941;font:16px/1.55 system-ui,sans-serif}header,main{max-width:1280px;margin:auto;padding:28px}h1{font-size:42px;line-height:1.1}h2{margin-top:42px;border-top:2px solid #bd946e;padding-top:22px}h3{margin:6px 0}a{color:#176772}nav{display:flex;gap:18px;flex-wrap:wrap}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(315px,1fr));gap:20px}article{background:#fffdf8;border:1px solid #d0c3ad;padding:20px;border-radius:5px}article svg{width:100%;background:#f0ece2;margin:10px 0}p{margin:10px 0}.tag{font-size:12px;letter-spacing:.04em;color:#675041}.gap{border-left:3px solid #b56548;padding-left:10px}.small{font-size:13px}button,select{font:inherit;padding:8px}figure{margin:0}table{border-collapse:collapse;width:100%}td,th{text-align:left;vertical-align:top;border-bottom:1px solid #d0c3ad;padding:10px}.legend{background:#e2e8e4;padding:16px}summary{cursor:pointer}@media print{article{break-inside:avoid}nav,select{display:none}}'''
intro='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>City architecture — evidence atlas</title><style>'''+style+'''</style><header><div class="tag">CITY ARCHITECTURE · RESEARCH · 6 SEPTEMBER 2026</div><h1>Buildings have plans.<br>Streets have neighbours.</h1><p>A provisional northern English context, tested against historic construction and contemporary housing. No architectural direction has been approved.</p><nav><a href="../REPORT.md">Report</a><a href="../scale-comparison.svg">Scaled comparison</a><a href="../model-briefs.md">Model briefs</a><a href="../simulation-audit.md">Game audit</a><a href="../dimensions.csv">Dimensions CSV</a><a href="../sources.csv">Sources and rights CSV</a></nav></header><main><div class="legend"><b>Evidence key:</b> documented = a source states it (including design proposals); estimated = uncertain extraction; proposed = our model choice; missing = no supported value. No perspective-derived dimensions are used. Original diagrams below illustrate relationships; they are not measured surveys, reconstructed elevations or generated photographic evidence. Only labelled envelope rectangles have a numerical scale. Photographs and copyrighted drawings are linked at their sources.</div><h2>Three possible contexts</h2><div class="grid">'''
regions=[('Northern English industrial town','1850–1914 terraces and workshops; interwar additions; contemporary infill','Wet-weather roof junctions, brick or local stone, slate; repeated party walls and service backs. Compatible warm/slate art. Strongest construction fit; local metre-level survey remains thin.','terrace',['S04','S13','S16']),('Dutch / Flemish town','Earlier merchant streets with nineteenth-century expansion and later infill','Narrow plots, gabled identities and working back streets offer a different rhythm. Amsterdam evidence supports service relationships; Flemish traditions were not independently surveyed. This is an alternative to investigate, not one generic European style.','dutch',['S22']),('Northeastern US town','Late nineteenth/early twentieth-century timber housing, brick commerce; later infill','Separate timber volumes, porches/stoops and small apartment buildings would make colour and entrances prominent. Requires a distinct kit of side gaps and framing. Older Salem provides a construction contrast, not the period baseline.','decker',['S23','S24'])]
for title,era,body,typ,ss in regions:
    e=dict(name=title,diagram=typ,dims=None)
    intro+=f'<article><h3>{esc(title)}</h3><p class="tag">{esc(era)}</p>{diagram(e)}<p>{esc(body)}</p><p>{" · ".join(link(s) for s in ss)}</p></article>'
intro+='''</div><p><b>Provisional recommendation:</b> start with a northern English industrial town, borrowing explicitly marked dimensional controls from elsewhere in England. Keep stone terraces local to their district and test brick for the first model street. Climate descriptions are qualitative design context, not a meteorological or structural calculation.</p><h2>Atlas</h2><p>17 records; not 17 independent measured buildings. Marmalade Lane appears under two families, HTA contributes two related archetypes, and Mill Road contributes related proposals. Workshops have two examples; a third reliable survey was not obtained. Mixed-use controls include a shop range and a corner whose internal use is unresolved. These gaps are retained rather than padded.</p><label>Show family <select id="family"><option value="all">All families</option>'''
for f in dict.fromkeys(e['family'] for e in E):intro+=f'<option>{esc(f)}</option>'
intro+='</select></label>'
for f in dict.fromkeys(e['family'] for e in E):
    intro+=f'<section data-family="{esc(f)}"><h2>{esc(f)}</h2><div class="grid">'
    for e in E:
        if e['family']!=f:continue
        intro+=f'<article id="{e["id"]}"><div class="tag">{e["id"]} · {esc(e["standing"])}</div><h3>{esc(e["name"])}</h3><p class="small">{esc(e["place"])} · {esc(e["period"])}</p>{diagram(e)}<p>{esc(e["observed"])}</p><p><b>Model implication:</b> {esc(e["lesson"])}</p><p class="gap"><b>Missing / uncertain:</b> {esc(e["gaps"])}</p><p class="small"><b>Views:</b> {esc(e["views"])}</p><p>{" · ".join(link(s) for s in e["sources"])}</p><details><summary>Dimensions and evidence gaps</summary><table><tr><th>Quantity</th><th>Value / units</th><th>Standing</th></tr>'
        for d in D:
            if d['example_id']==e['id'] or (e['id']=='A01' and d['example_id'].startswith('A01-')):
                intro+=f'<tr><td>{esc(d["example_id"]+" "+d["quantity"])}</td><td>{esc(str(d["value"]) if d["value"]!="" else "—")} {esc(d["units"])}</td><td>{esc(d["evidence_status"])}</td></tr>'
        intro+='</table></details></article>'
    intro+='</div></section>'
intro+='''<h2>What to inspect together</h2><p><a href="https://www.aschb.org.uk/wp-content/uploads/2022/01/Vol-34.pdf#page=65">Evans: inspected front/rear/roof aerial, printed p63 Fig4</a> · <a href="https://data.parliament.uk/DepositedPapers/Files/DEP2012-1192/Designoflifetimehomes_final.pdf#page=9">HTA: inspected two-bedroom ground/first plans, LTH201</a> · <a href="https://data.parliament.uk/DepositedPapers/Files/DEP2012-1192/Designoflifetimehomes_final.pdf#page=25">HTA: inspected four-bedroom ground/first plans, LTH207</a> · <a href="https://www.mikhailriches.com/wp-content/uploads/2022/10/059_N252_medium-e1750261172175.jpg">Goldsmith: inspected street photograph</a></p><p>The Evans photo is particularly useful for distinguishing a roof over a front range from the narrower ranges, glazed links and yards behind it. Roof staining and repair patches are spatially local; their dimensions were not measured.</p><h2>Material and junction controls</h2><p>Modern brick: 215 × 102.5 × 65 mm (S17). Marley clay plain tile: 265 × 165 mm; the specified 65 mm minimum headlap gives a 100 mm double-lap course gauge (S18). A 500 × 250 mm slate is one catalogue size, not a 500 mm exposed roof course (S19). Repetition scale comes from covered modules, not the texture file name.</p><p>Use the <a href="../scale-comparison.svg">scale sheet</a> for the human, brick, tile and roof sections. These original diagrams reproduce numerical relationships, not the copyrighted drawings.</p><h2>The checkpoint in context</h2><figure><a href="../../../../artifacts/visual-study/roof-proportions/daylight-near.png"><img style="width:100%;height:auto" src="../evidence/checkpoint.png" alt="Existing checkpoint city, with long buildings and paired gables"></a><figcaption class="small">Repository screenshot from d9addc3 research input; Tick600 / shopping fixture / 400 Citizens. Viewed in this session; not a new driven capture. See audit for geometry and capacity reconstruction.</figcaption></figure><h2>Reuse and inspection record</h2><p>Third-party pictures remain links. No assumption that a public planning portal or archive grants image reuse. HTA drawing-sheet restrictions are stricter than the report’s general OGL notice, so the bundle does not copy its sheets. The dimensional diagrams and scale sheet are original analytical drawings. Dates, exact pages, access limitations and individual source standing are in <a href="../sources.csv">sources.csv</a>.</p></main><script>document.querySelector('#family').addEventListener('change',e=>document.querySelectorAll('section[data-family]').forEach(s=>s.hidden=e.target.value!=='all'&&s.dataset.family!==e.target.value));</script></html>'''
(O/'atlas/index.html').write_text(intro)
# Original numerical comparison. Geometry is in SVG units at 10 units/metre,
# except the explicitly enlarged material strip (300 units/metre).
V=['<svg xmlns="http://www.w3.org/2000/svg" width="1440" height="1680" viewBox="0 0 1440 1680" role="img" aria-labelledby="title desc"><title id="title">Building scale, roof spans and access</title><desc id="desc">A recorded 24 by 16 metre game Building, the same footprint as a proposed apartment block, a documented apartment proposal, and an internal house envelope, with separately labelled proposed model elevations and enlarged construction modules.</desc><rect width="1440" height="1680" fill="#f5f1e8"/><style>text{font-family:Arial,sans-serif;fill:#263941;font-size:14px}.head{font-size:28px;font-weight:bold}.sub{font-size:19px;font-weight:bold}.tiny{font-size:12px}.dim{stroke:#263941;stroke-width:1;fill:none}.body{fill:#bf916f;stroke:#263941;stroke-width:1.3}.roof{fill:#667582;stroke:#263941;stroke-width:1.3}.proposed{fill:#94b8ae;stroke:#245b51;stroke-width:1.3}.limit{fill:none;stroke:#537c89;stroke-dasharray:5 4}</style>']
def tx(x,y,s,cl=''):V.append(f'<text x="{x}" y="{y}" class="{cl}">{esc(s)}</text>')
def rect(x,y,w,h,cl='body'):V.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" class="{cl}"/>')
def path(d,cl='dim'):V.append(f'<path d="{d}" class="{cl}"/>')
def human(x,y):
    # 1.75 m total height: head top at -17.5 units; not an observed person.
    V.append(f'<circle cx="{x}" cy="{y-15.5}" r="2" fill="#263941"/>')
    path(f'M{x} {y-13.5}v7 M{x-4} {y-9}l4 -4 4 4 M{x} {y-6.5}l-3 6.5 M{x} {y-6.5}l3 6.5')
def plan(x,y,w,d,label,cl='body'):
    rect(x,y,w*10,d*10,cl);tx(x,y-12,label);tx(x,y+d*10+22,f'{w:g} × {d:g} m')
    path(f'M{x} {y+d*10+30}h{w*10}');tx(x,y+d*10+47,'plan / common 10 units per metre','tiny')
def sect(x,base,w,h,r,cl='body',roof=True,eave=.0):
    rect(x,base-h*10,w*10,h*10,cl)
    if roof:path(f'M{x-eave*10} {base-h*10}L{x+w*5} {base-(h+r)*10}L{x+(w+eave)*10} {base-h*10}Z','roof')
    human(x+w*10+15,base);path(f'M{x-8} {base}h{w*10+42}')
tx(40,42,'A roof correction is also a building-type question','head')
tx(40,69,'Orange = recorded game geometry · blue outline = documented reference envelope · green = PROPOSED model')
tx(40,93,'All building plans and sections use 10 SVG units per metre. No perspective measurements. Figure source IDs match dimensions.csv.','tiny')
for x,title,sub in [(40,'G003 · checkpoint','One Building; d9addc3 / Tick 600'),(405,'M3 · same-ground test','PROPOSED small apartment block'),(770,'A01-C · Mill Road','2018 design proposal, not as-built'),(1110,'T02 · HTA study','Published INTERNAL envelope')]:
    tx(x,133,title,'sub');tx(x,155,sub,'tiny')
plan(40,192,24,16,'24 × 16 m outer footprint')
plan(405,192,24,16,'Exact same 384 m² footprint','proposed')
# Proposed circulation: two stair reservations, corridor and front lobby.
rect(405,262,240,20,'limit');rect(405,247,40,50,'limit');rect(605,247,40,50,'limit');rect(515,282,20,70,'limit')
path('M525 192v70 M525 282v70');tx(454,276,'corridor 2 m','tiny');tx(408,242,'stair','tiny');tx(606,242,'stair','tiny');tx(540,338,'entry','tiny')
plan(770,192,24,7.8,'Same width; different depth','limit')
plan(1110,192,3.9,8,'External frontage 4.5 m','limit')
tx(1110,340,'62 m² GIA across two floors','tiny');tx(1110,359,'Height and external depth missing','tiny')
tx(40,446,'Roof sections / height controls','sub')
sect(40,628,16,10.5,4.125,eave=.35)
tx(40,651,'G003: wall 10.5 m; roof rise 4.125 m','tiny');tx(40,670,'Across eaves: 16.7 m; pitch ≈26.3°','tiny')
sect(405,628,16,10.5,0,cl='proposed',roof=False)
rect(405,518.5,160,4.5,'proposed');path('M405 523L485 525L565 523')
tx(405,651,'M3: wall 10.5 m; parapet +0.45 m','tiny');tx(405,670,'Roof falls / outlets / overflow specified','tiny')
rect(770,563,78,65,'limit');rect(770,543,78,20,'limit');human(870,628);path('M760 628h128')
tx(770,651,'C: eaves 6.5 m; ridge 8.5 m (S03)','tiny');tx(770,670,'Profile unknown: limit box, not gable','tiny')
tx(1110,534,'No house elevation invented here.','tiny');tx(1110,555,'See PROPOSED M1 below.','tiny')
path('M40 710H1400');tx(40,744,'Three research models — dimensions PROPOSED; openings shown selectively','sub')
# Front elevations, ridge line parallel to street; roof plane seen in elevation.
for x,w,h,r,name,n in [(40,24,6,4.8*math.tan(math.radians(35)),'M1 · five attached houses',10),(485,9.6,9.6,4.8*math.tan(math.radians(35)),'M2 · corner mixed use',4),(865,24,10.5,.45,'M3 · 12-unit apartment hypothesis',8)]:
    base=924;rect(x,base-h*10,w*10,h*10,'proposed')
    if name.startswith('M2'):path(f'M{x} {base-h*10}L{x+w*5} {base-(h+r)*10}L{x+w*10} {base-h*10}Z','roof')
    else:rect(x,base-(h+r)*10,w*10,r*10,'roof')
    if name.startswith('M3'):rect(x+20,base-h*10-24,20,24,'proposed')
    for k in range(n):
        opening=12 if name.startswith('M3') else 10.5
        bx=x+(k+.5)*w*10/n-opening/2
        rect(bx,base-h*10+7,opening,15,'limit')
    if name.startswith('M1'):
        for k in range(5):
            path(f'M{x+k*48} {base}v{-h*10}')
            rect(x+k*48+7.25,base-21,9.5,21,'limit')
            rect(x+k*48+30.75,base-23,10.5,15,'limit')
    elif name.startswith('M2'):
        rect(x+6,base-30,57,27,'limit');rect(x+72,base-22,10,22,'limit')
    else:rect(x+w*5-6,base-22,12,22,'limit')
    human(x+w*10+15,base);path(f'M{x-8} {base}h{w*10+42}')
    tx(x,956,name,'sub');tx(x,979,f'frontage {w:g} m / wall {h:g} m / roof rise or parapet {r:.3f} m','tiny')
    tx(x,1000,'Depth '+('9.6 m; each house 4.8 m wide' if name.startswith('M1') else '12 m; separate shop and home access' if name.startswith('M2') else '16 m; whole footprint retained'),'tiny')
path('M40 1040H1400');tx(40,1074,'Construction references — enlarged to 300 units/metre (30× the building scale)','sub')
# Exactly 1.75m person alongside true module sizes at enlarged strip scale.
rect(40,1104,64.5,19.5);tx(40,1149,'Brick face 215 × 65 mm','tiny');tx(40,1168,'S17 modern product','tiny')
rect(280,1104,49.5,79.5,'roof');path('M280 1134h49.5');tx(345,1120,'Tile 265 × 165 mm (S18)','tiny');tx(345,1142,'65 mm headlap → 100 mm gauge','tiny');tx(345,1164,'Full tile ≠ exposed course','tiny')
rect(680,1104,75,150,'roof');tx(780,1120,'Slate 500 × 250 mm (S19)','tiny');tx(780,1142,'Example proposed headlap 100 mm','tiny');tx(780,1164,'→ proposed gauge 200 mm','tiny');tx(780,1186,'Suitability not a universal rule','tiny')
path('M1080 1110h100 M1080 1104v12 M1130 1104v12 M1180 1104v12');tx(1080,1142,'Building scale bar: 0 / 5 / 10 m','tiny')
human(1090,1192);tx(1110,1192,'1.75 m person at building scale','tiny')
tx(40,1302,'Sources: R01 recorded geometry; S01 HTA LTH201; S03 Epsom p92 §4.4; S17/S18/S19 product dimensions; P01 model-briefs.md.','tiny')
tx(40,1324,'Roof silhouettes in the proposed row are original design choices. M1 is not a reconstruction of HTA or Saltaire. No capacity inferred from windows.','tiny')
path('M40 1370H1400');tx(40,1405,'Same ground, same slope — changing the roof assembly','sub')
# Cross-section of G001, using recorded roof local X=20.7m, rise2.676m.
rect(43.5,1510,200,70)
path('M40 1510L91.75 1483.24L143.5 1510L195.25 1483.24L247 1510Z','roof')
path('M40 1510L143.5 1456.48L247 1510','limit')
human(270,1580);tx(40,1604,'G001 recorded paired roof: two 10.35 m spans, rise 2.676 m','tiny')
tx(40,1625,'Dashed single-span counterfactual: rise 5.352 m; same outer span and pitch','tiny')
tx(530,1478,'The dashed profile is a calculated alternative, NOT an earlier game capture.')
tx(530,1506,'Halving the cross-span halves roof rise at fixed pitch. It does not add doors,')
tx(530,1529,'create a room plan or drain the new valley. Those are separate model tasks.')
tx(530,1572,'The G001 body stays 36 × 20 m in plan and 7 m tall in both cases.')
tx(530,1614,'M3 front elevation includes its proposed roof-access headhouse; the upper section cuts midspan.','tiny')
V.append('</svg>');(O/'scale-comparison.svg').write_text(''.join(V))
print('Wrote atlas, dimension/source tables, scale comparison and audit data.')

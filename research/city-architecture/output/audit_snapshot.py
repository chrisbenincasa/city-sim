#!/usr/bin/env python3
"""Recompute the checkpoint sample; no Godot, gameplay execution or writes outside output."""
from pathlib import Path
import csv,hashlib,json,statistics,subprocess,math
O=Path(__file__).resolve().parent;ROOT=O.parents[2]
REV='d9addc3'
PATH='artifacts/visual-study/roof-proportions/daylight-near.tsv'
def git(path):return subprocess.check_output(['git','show',f'{REV}:{path}'],cwd=ROOT)
def save(name,fields,rows):
    with (O/name).open('w',newline='') as f:
        w=csv.DictWriter(f,fields);w.writeheader();w.writerows(rows)
(O/'evidence/checkpoint.png').write_bytes(git(PATH.replace('.tsv','.png')))
raw=git(PATH);rows=list(csv.reader(raw.decode().splitlines(),delimiter='\t'))
body=[r for r in rows if r[:2]==['row','building']]
roofs={r[3]:r for r in rows if r[:1]==['row'] and r[1] in ('roof','hip','paired-roof')}
assert len(body)==52 and len({r[3] for r in body})==52
assert raw==git(PATH.replace('daylight-near.tsv','daylight-near-repeat.tsv'))
out=[];dim=[]
for r in body:
    ident=int(r[3]);w,h,d=map(float,r[7:10]);n=h/3.5
    assert n==int(n) and float(r[5])==h/2 # ground-founded full-height component
    f=w*d*n/16
    assert f==int(f)
    f=int(f);ten=max(1,f//25);jobs=max(1,(f//ten)//3)
    roof=roofs[r[3]];rx,ry,rz=map(float,roof[7:10]);spans=2 if roof[1]=='paired-roof' else 1
    # Roof mesh X is across slope; roof basis scale is LOCAL, not map-axis width.
    pitch=math.degrees(math.atan2(ry,rx/(2*spans)))
    o=dict(example_id=f'G{ident:03}',draw_index=int(r[2]),building_id=ident,east_west_m=w,south_north_m=d,wall_m=h,storeys=int(n),footprint_m2=w*d,floor_m2=f*16,floor_tiles=f,tenancy_capacity=ten,jobs_per_premised_business=jobs,roof=roof[1],roof_rise_m=ry,roof_cross_span_including_eaves_m=rx/spans,roof_pitch_degrees=round(pitch,3))
    out.append(o)
    for q,u in [('east_west_m','m'),('south_north_m','m'),('wall_m','m'),('floor_m2','m2'),('tenancy_capacity','tenancies'),('jobs_per_premised_business','jobs per Business'),('roof_rise_m','m'),('roof_pitch_degrees','degrees')]:
        derived=q in ('floor_m2','tenancy_capacity','jobs_per_premised_business','roof_pitch_degrees')
        dim.append(dict(example_id=o['example_id'],source_id='R01',quantity=q,value=o[q],units=u,evidence_status='documented',method='Deterministic reconstruction from recorded geometry + code' if derived else 'Recorded draw transform; unit wall/roof mesh',caveat='Checkpoint sample, not new simulation. Output rounded to 0.001 m. Capacity is ceiling, not observed use; jobs per Business not whole Building.',sheet_page=f'{PATH}: draw_index {r[2]}, building_id {ident}'))
save('evidence/simulation-sample.csv',list(out[0]),out)
save('evidence/game-dimensions.csv',list(dim[0]),dim)
files=[PATH,PATH.replace('.tsv','.txt'),PATH.replace('.tsv','.png'),'src/Borough.Godot/Main.Massing.cs','src/Borough.Godot/Main.cs','src/Borough.Godot/Main.Channels.cs','src/Borough.Godot/RoofMeshes.cs','src/Borough.Core/Space/BuildingPlan.cs','src/Borough.Core/Rules/Ruleset.cs','src/Borough.Core/Entities/World.cs','rulesets/shopping.toml']
manifest={'checkpoint':subprocess.check_output(['git','rev-parse',REV],cwd=ROOT,text=True).strip(),'audited_head':subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(),'method':'Historical captured geometry recalculated, not a fresh run','sha256_at_checkpoint':{p:hashlib.sha256(git(p)).hexdigest() for p in files}}
summary={'components':len(body),'distinct_building_ids':len(out),'repeat_draw_byte_identical':True,'axes':{},'roof_counts':{k:sum(o['roof']==k for o in out) for k in ('roof','hip','paired-roof')},'tenancy_capacity_range':[min(o['tenancy_capacity'] for o in out),max(o['tenancy_capacity'] for o in out)],'sample_ids':[1,2,3,4,8]}
for k in ('east_west_m','south_north_m','wall_m','footprint_m2'):
    a=[o[k] for o in out];summary['axes'][k]={'min':min(a),'median':statistics.median(a),'max':max(a)}
(O/'evidence/simulation-summary.json').write_text(json.dumps(summary,indent=2)+'\n')
(O/'evidence/reproduction.json').write_text(json.dumps(manifest,indent=2)+'\n')
print(json.dumps(summary))

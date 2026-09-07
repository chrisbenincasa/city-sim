#!/usr/bin/env python3
"""Recalculate archived geometry; never execute the game or alter its files."""
from pathlib import Path
import csv, hashlib, io, json, subprocess
OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[3]
REV='d9addc3'
DRAW='artifacts/visual-study/roof-proportions/daylight-near.tsv'
def git(*args): return subprocess.check_output(['git',*args],cwd=ROOT)
raw=git('show',f'{REV}:{DRAW}')
assert raw==git('show',f'{REV}:{DRAW.replace("near.tsv","near-repeat.tsv")}')
rows=list(csv.reader(io.StringIO(raw.decode()),delimiter='\t'))
sample=[]
for r in rows:
    if r[:2]!=['row','building']: continue
    width,height,depth=map(float,r[7:10]); floors=height/3.5
    assert floors==int(floors)
    floor_tiles=width*depth*floors/16
    assert floor_tiles==int(floor_tiles)
    cap=max(1,int(floor_tiles)//25)
    sample.append(dict(id=int(r[3]),draw_index=int(r[2]),width_m=width,depth_m=depth,
        wall_m=height,storeys=int(floors),floor_m2=int(floor_tiles)*16,
        tenancies=cap,jobs_per_premised_business=max(1,(int(floor_tiles)//cap)//3)))
assert len(sample)==len({r['id'] for r in sample})==52
with (OUT/'evidence/game-sample.csv').open('w',newline='') as f:
    w=csv.DictWriter(f,list(sample[0]));w.writeheader();w.writerows(sample)
paths=['src/Borough.Godot/Main.Massing.cs','src/Borough.Godot/RoofMeshes.cs',
 'src/Borough.Core/Space/BuildingPlan.cs','src/Borough.Core/Rules/Ruleset.cs',
 'src/Borough.Core/Entities/World.cs','rulesets/shopping.toml']
checks={}
for p in paths:
    old=git('show',f'{REV}:{p}');now=(ROOT/p).read_bytes()
    checks[p]={'checkpoint_sha256':hashlib.sha256(old).hexdigest(),
               'working_sha256':hashlib.sha256(now).hexdigest(),
               'identical':old==now}
manifest={'checkpoint':git('rev-parse',REV).decode().strip(),
          'head':git('rev-parse','HEAD').decode().strip(),
          'draw_sha256':hashlib.sha256(raw).hexdigest(),
          'archived_repeat_identical':True,'fresh_simulation_run':False,
          'files':checks}
(OUT/'evidence/reproduction.json').write_text(json.dumps(manifest,indent=2)+'\n')
print('PASS: archived pair identical; 52 components / 52 Building IDs recalculated.')

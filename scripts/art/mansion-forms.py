"""US mansion forms: eleven styles drawn as a seeded parts grammar on a detached dwelling Lot.

Each style is a main block plus the parts its plan calls for: arms that stand forward or run back, an
angled wing on a hub, a rear ell, a front arcade and a garage. The grammar picks a plan from the
style's list by seed, keeps the first one the footprint can hold, and falls back to a bar.

src/Borough.Appearance/FamilyBodyBuilder.Mansion.cs draws the same plans on the same draw. The export
job writes art/mansion-forms/bodies.json, and MansionBodyTests holds the C# builder to it. Change the
grammar here and there together, then re-export.

The frame is the body's: x along the Street, y back from it, the Street at -y, z up. A storey is 3.5 m.

blender --background --factory-startup --python-exit-code 1 --python scripts/art/mansion-forms.py -- REPO OUT [job...]

Jobs: export, and sheet-<style> for each style, a blockout of its plans on four sites and both grounds.
"""
import json
import math
import sys
from pathlib import Path

ARGS = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else sys.argv[1:]
REPO, OUT, ONLY = Path(ARGS[0]), Path(ARGS[1]), ARGS[2:]

MASK = (1 << 64) - 1
STOREY = 3.5

STYLES = ['colonial', 'tudor', 'queen-anne', 'chateau', 'spanish', 'prairie', 'regency', 'mid-century',
          'farmhouse', 'neo-eclectic', 'california']

PLANS = {
    'colonial': ['u-front', 'h', 't', 'u-rear'],
    'tudor': ['l-front', 'ell', 't'],
    'queen-anne': ['angled', 'ell', 'l-front'],
    'chateau': ['u-front', 'l-front', 'h', 't'],
    'spanish': ['court', 'angled', 'l-front', 't'],
    'prairie': ['t', 'l-front', 'ell'],
    'regency': ['u-front', 'u-rear', 't', 'h'],
    'mid-century': ['l-front', 'ell', 't'],
    'farmhouse': ['l-front', 't', 'ell'],
    'neo-eclectic': ['angled', 'l-front', 'u-front', 't'],
    'california': ['angled', 'l-front', 'ell'],
}

# The least frontage each style keeps for its main block between arms, and how far its porch,
# portico or porte-cochere stands in front of the main block.
LEAST_MAIN = dict(zip(STYLES, [18, 22, 18, 22, 22, 20, 18, 18, 18, 22, 20]))
STOOP = dict(zip(STYLES, [3, 1.6, 2.8, 5, 1.5, 2, 2.8, 1.6, 2.8, 1.6, 1.8]))

# The shallowest main block a plan with wings keeps. Deep yards leave many houses 8-13 m deep.
LEAST_DEPTH = 6.5

GARAGE_ARM = {'colonial', 'regency', 'chateau', 'neo-eclectic'}
LOW_ARMS = {'prairie', 'mid-century', 'california'}


class Draw:
    """SplitMix64, as HouseDraw in FamilyBodyBuilder.House.cs."""

    def __init__(self, seed):
        self.state = ((seed * 0x9E3779B97F4A7C15) ^ 0x5DEECE66D) & MASK

    def next(self):
        self.state = (self.state + 0x9E3779B97F4A7C15) & MASK
        z = self.state
        z = ((z ^ (z >> 30)) * 0xBF58476D1CE4E5B9) & MASK
        z = ((z ^ (z >> 27)) * 0x94D049BB133111EB) & MASK
        return z ^ (z >> 31)

    def unit(self):
        return (self.next() >> 40) / float(1 << 24)

    def range(self, low, high):
        return low + (high - low) * self.unit()

    def pick(self, count):
        return self.next() % count


def mass(part, x0, x1, y0, y1, storeys, turn=0.0, px=0.0, py=0.0):
    return {'part': part, 'box': [x0, x1, y0, y1, turn, px, py], 'storeys': storeys}


def plan_mansion(style, frontage, depth, storeys, seed, single):
    r = Draw(((seed << 8) | 0x4D) & MASK)
    aw, proj, md, rd, ew = r.range(7, 9.5), r.range(3.5, 6), r.range(10, 13), r.range(5, 8), r.range(7, 9.5)
    th, lw, wd = r.range(30, 42) * math.pi / 180, r.range(9.5, 12), r.range(6.5, 8)
    gside = -1 if r.pick(2) == 0 else 1
    drop = -1 if r.pick(3) == 2 else 0

    hw, hd = frontage / 2, depth / 2
    margin = min(3, frontage * .06)
    lane = max(margin, 3.5) if single and frontage >= 32 else margin
    x0 = -hw + (lane if gside < 0 else margin)
    x1 = hw - (lane if gside > 0 else margin)
    span = x1 - x0
    yf, yb = -hd, hd
    least = LEAST_MAIN[style]
    least_u = least + (14 if style == 'chateau' else 0)
    stoop = min(STOOP[style], depth * .25)
    aw = min(aw, max(6, span * .22))
    proj = max(stoop, min(proj, depth * .3))
    hs = wd + 1.2
    sin, cos = math.sin(th), math.cos(th)
    reserve = hs / 2 + (lw + hs / 2) * cos + wd / 2 * sin
    lift = max(stoop, (lw + hs / 2) * sin + wd / 2 * cos - hs / 2)
    rear_room = min(4, depth * .25)

    ok = {
        'u-front': span - 2 * aw >= least_u and depth - proj >= LEAST_DEPTH,
        'h': span - 2 * aw >= least_u and depth >= 12 and depth - proj - rear_room >= LEAST_DEPTH,
        'u-rear': span - 2 * aw >= least_u and depth >= 12 and depth - stoop - rear_room >= LEAST_DEPTH,
        'l-front': span - aw >= least and depth - proj >= LEAST_DEPTH,
        'court': span >= 34 and span - 2 * aw >= least and depth - proj >= LEAST_DEPTH,
        'angled': span - reserve >= least and depth - lift >= LEAST_DEPTH,
        't': depth >= 12 and depth - stoop - rear_room >= LEAST_DEPTH,
        'ell': depth >= 12 and depth - stoop - rear_room >= LEAST_DEPTH,
    }
    order = PLANS[style]
    plan = next((order[(seed + i) % len(order)] for i in range(len(order)) if ok[order[(seed + i) % len(order)]]), 'bar')

    forward = plan in ('u-front', 'h', 'l-front', 'court')
    rear = plan in ('h', 'u-rear', 't', 'ell')
    front = yf + lift if plan == 'angled' else yf + proj if forward else yf + stoop
    back = min(front + md, yb - rear_room if rear else yb)
    if back - front < 6:
        back = min(yb, front + 6)
    rear_end = min(back + rd, yb)
    arm_storeys = max(1, storeys + (-1 if style in LOW_ARMS else drop))

    mx0, mx1 = x0, x1
    arms = []

    def arm_part(side):
        garage = single and side == gside and (style in GARAGE_ARM or plan in ('h', 'u-rear'))
        return 'garage' if garage else 'arm'

    if plan in ('u-front', 'h', 'court'):
        end = rear_end if plan == 'h' else back
        arms.append(mass(arm_part(-1), x0, x0 + aw, yf, end, arm_storeys))
        arms.append(mass(arm_part(1), x1 - aw, x1, yf, end, arm_storeys))
        if plan == 'court':
            arms.append(mass('arcade', x0 + aw, x1 - aw, yf, yf + 3.2, 1))
    elif plan == 'u-rear':
        arms.append(mass(arm_part(-1), x0, x0 + aw, front + .8, rear_end, arm_storeys))
        arms.append(mass(arm_part(1), x1 - aw, x1, front + .8, rear_end, arm_storeys))
    elif plan == 'l-front':
        side = gside if style == 'neo-eclectic' else -gside
        arms.append(mass(arm_part(side), x0, x0 + aw, yf, back, arm_storeys) if side < 0
                    else mass(arm_part(side), x1 - aw, x1, yf, back, arm_storeys))
    elif plan == 'angled':
        side = gside if single else -gside
        xe = x0 + reserve if side < 0 else x1 - reserve
        mx0, mx1 = (xe, x1) if side < 0 else (x0, xe)
        ha, hb = min(xe - side * .3, xe + side * hs), max(xe - side * .3, xe + side * hs)
        pcx, pcy = (ha + hb) / 2, front + hs / 2
        reach = lw + hs / 2
        arms.append(mass('hub', ha, hb, front, front + hs, storeys))
        arms.append(mass('garage' if single else 'wing', min(pcx, pcx + side * reach), max(pcx, pcx + side * reach),
                         pcy - wd / 2, pcy + wd / 2, arm_storeys, -side * th, pcx, pcy))

    if plan in ('t', 'ell') and rear_end - back >= 3:
        width = min(ew, (mx1 - mx0) * .6)
        at = (mx0 + mx1) / 2 - width / 2 if plan == 't' else mx0 + .8 if gside > 0 else mx1 - .8 - width
        arms.append(mass('rear', at, at + width, back - 1.5, rear_end, max(1, storeys - 1)))

    def garaged():
        return any(a['part'] == 'garage' for a in arms)

    if single and not garaged() and yb - back >= 5:
        room = (mx1 - mx0) / 2 - min(ew, (mx1 - mx0) * .6) / 2 - .5 if plan == 't' else 7.5
        wide = min(7.5, room)
        if wide >= 5:
            g0, g1 = (mx1 - wide, mx1) if gside > 0 else (mx0, mx0 + wide)
            arms.append(mass('garage', g0, g1, back - .5, min(back + 7, yb), 1))

    if single and not garaged():
        outer = next((i for i, a in enumerate(arms) if a['part'] == 'arm'
                      and (a['box'][1] >= x1 - .01 if gside > 0 else a['box'][0] <= x0 + .01)), -1)
        if outer >= 0:
            arms[outer]['part'] = 'garage'
        elif mx1 - mx0 - 7.5 >= least:
            g0, g1 = (mx1 - 7.5, mx1) if gside > 0 else (mx0, mx0 + 7.5)
            arms.append(mass('garage', g0, g1, front + 1.5, min(front + 8.5, max(back, front + 6)), 1))
            if gside > 0:
                mx1 = g0
            else:
                mx0 = g1

    entry = (mx0 + mx1) / 2
    if plan == 'l-front':
        a0, a1 = arms[0]['box'][0], arms[0]['box'][1]
        entry = (a1 + mx1) / 2 if a0 <= mx0 + .01 else (mx0 + a0) / 2

    return {'plan': plan, 'garage_side': gside, 'entry': entry, 'front': front,
            'masses': [mass('main', mx0, mx1, front, back, storeys)] + arms}


# Sites span the Lots the presets draw on: 28-84 m frontage, and the 8-24 m of house a deep yard leaves.
SITES = [(28, 8), (36, 12), (40, 11), (48, 16), (52, 20), (84, 24)]
SEEDS = [0, 1, 7, 42]


def storeys_of(style, i):
    return 2 if style in ('prairie', 'mid-century', 'california') else 2 + i % 2


def export():
    plans = []
    for style in STYLES:
        for grounds in ('single', 'apartments'):
            for i, (frontage, depth) in enumerate(SITES):
                for seed in SEEDS:
                    storeys = storeys_of(style, i)
                    p = plan_mansion(style, frontage, depth, storeys, seed, grounds == 'single')
                    plans.append({'style': style, 'grounds': grounds, 'frontage': frontage, 'depth': depth,
                                  'storeys': storeys, 'seed': seed, 'plan': p['plan'], 'garage_side': p['garage_side'],
                                  'entry': round(p['entry'], 4),
                                  'masses': [{'part': m['part'], 'box': [round(v, 4) for v in m['box']], 'storeys': m['storeys']}
                                             for m in p['masses']]})
    target = REPO / 'art/mansion-forms/bodies.json'
    target.parent.mkdir(parents=True, exist_ok=True)
    lines = ',\n'.join(json.dumps(p, separators=(',', ':')) for p in plans)
    target.write_text('{"source":"scripts/art/mansion-forms.py","plans":[\n' + lines + '\n]}\n')
    print(f'wrote {len(plans)} plans to {target}')


PART_COLOR = {'main': (.85, .82, .74), 'arm': (.72, .70, .62), 'garage': (.45, .50, .58), 'rear': (.66, .60, .52),
              'hub': (.80, .66, .52), 'wing': (.70, .58, .48), 'arcade': (.86, .76, .60)}


def sheet(style):
    import bpy
    import bmesh

    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.display.shading.light = 'STUDIO'
    scene.display.shading.color_type = 'MATERIAL'
    scene.display.shading.show_shadows = True
    scene.render.resolution_x, scene.render.resolution_y = 2400, 1400
    world = bpy.data.worlds.new('w')
    scene.world = world
    world.color = (.93, .93, .91)

    def material(name, rgb):
        mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
        mat.diffuse_color = (*rgb, 1)
        return mat

    def box(name, corners, z0, z1, mat):
        mesh = bpy.data.meshes.new(name)
        bm = bmesh.new()
        low = [bm.verts.new((x, y, z0)) for x, y in corners]
        high = [bm.verts.new((x, y, z1)) for x, y in corners]
        bm.faces.new(list(reversed(low)))
        bm.faces.new(high)
        for i in range(4):
            j = (i + 1) % 4
            bm.faces.new([low[i], low[j], high[j], high[i]])
        bm.to_mesh(mesh)
        bm.free()
        mesh.materials.append(mat)
        obj = bpy.data.objects.new(name, mesh)
        scene.collection.objects.link(obj)

    sites = [(36, 12), (48, 16), (52, 20), (84, 24)]
    cx = 0.0
    for col, (frontage, depth) in enumerate(sites):
        cx += frontage / 2 + (8 if col else 0)
        for row, single in enumerate((True, False)):
            oy = row * 52.0
            yard = 18.0
            box(f'lot{col}{row}', [(cx - frontage / 2, oy - depth / 2 - yard), (cx + frontage / 2, oy - depth / 2 - yard),
                                   (cx + frontage / 2, oy + depth / 2), (cx - frontage / 2, oy + depth / 2)],
                -.2, 0, material('lawn', (.52, .64, .42)))
            p = plan_mansion(style, frontage, depth, storeys_of(style, col), 7 + col, single)
            for k, m in enumerate(p['masses']):
                x0, x1, y0, y1, turn, px, py = m['box']
                c, s = math.cos(turn), math.sin(turn)

                def at(x, y):
                    dx, dy = x - px, y - py
                    return (cx + px + dx * c - dy * s, oy + py + dx * s + dy * c) if turn else (cx + x, oy + y)

                box(f'm{col}{row}{k}', [at(x0, y0), at(x1, y0), at(x1, y1), at(x0, y1)], 0, m['storeys'] * STOREY,
                    material(m['part'], PART_COLOR[m['part']]))
        cx += frontage / 2

    camera = bpy.data.objects.new('cam', bpy.data.cameras.new('cam'))
    scene.collection.objects.link(camera)
    scene.camera = camera
    camera.data.type = 'ORTHO'
    camera.data.ortho_scale = cx * 1.05
    camera.location = (cx / 2, -120, 140)
    camera.rotation_euler = (math.radians(48), 0, 0)
    OUT.mkdir(parents=True, exist_ok=True)
    scene.render.filepath = str(OUT / f'sheet-{style}.png')
    bpy.ops.render.render(write_still=True)


JOBS = {'export': export, **{f'sheet-{s}': (lambda s=s: sheet(s)) for s in STYLES}}
for job in ONLY or list(JOBS):
    if job in JOBS:
        JOBS[job]()

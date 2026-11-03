"""Contemporary and mid-century rowhouse forms. Each draws one house between its two side walls, so it
closes against a neighbor of any form: a blank wall on each shared side, its own roof end over a shared
wall, and a party upstand where its roof meets one. The looks come from wide-house-forms.py and the
traditional forms from rowhouse-forms.py, which this script loads against the same house-families.py.

blender --background --factory-startup --python-exit-code 1 --python scripts/art/rowhouse-periods.py -- REPO OUT [job...]

Jobs: tile-<form> and junction-<form> for each new, re-dressed and traditional form; street-now (the six
traditional forms); and street-mixed, street-b and street-c, the three share options over the recommended
forms (20 / 9 / 7, 16 / 9 / 11 and 13 / 9 / 14 contemporary, mid-century and traditional houses of 36).
"""
import importlib.util
import math
import random
import sys
import types
from pathlib import Path

ARGS = sys.argv[sys.argv.index('--') + 1:]
REPO, OUT, ONLY = Path(ARGS[0]), Path(ARGS[1]), ARGS[2:]

_argv = sys.argv
sys.argv = _argv[:_argv.index('--') + 3] + ['(none)']
spec = importlib.util.spec_from_file_location('whf', REPO / 'scripts/art/wide-house-forms.py')
whf = importlib.util.module_from_spec(spec)
spec.loader.exec_module(whf)

# rowhouse-forms.py loads its own house-families.py, which would give it a second scene state. It
# runs here against wide-house-forms.py's instead.
_path = REPO / 'scripts/art/rowhouse-forms.py'
_src = _path.read_text()
_src = _src[:_src.index("spec = importlib.util.spec_from_file_location('hf'")] + _src[_src.index('ts, S = hf.ts, hf.S'):]
rf = types.ModuleType('rf')
rf.__file__ = str(_path)
rf.hf = whf.hf
exec(compile(_src, str(_path), 'exec'), rf.__dict__)
sys.argv = _argv

hf, ts, S, poly = whf.hf, whf.ts, whf.S, whf.poly
import bpy

PERIOD = {'parapet-infill': 'contemporary', 'mansard-zinc': 'contemporary', 'bay-picture': 'mid-century',
          'eaveless-gable': 'contemporary', 'roof-terrace': 'contemporary', 'sawtooth-row': 'contemporary',
          'box-front': 'contemporary', 'cross-wall': 'mid-century', 'street-monopitch': 'mid-century',
          'butterfly-row': 'mid-century', 'tile-hung': 'mid-century'}
whf.PERIOD.update(PERIOD)
whf.PITCH.update({'parapet-infill': (0, 0), 'mansard-zinc': (0, 0), 'bay-picture': (20, 28), 'eaveless-gable': (38, 45), 'roof-terrace': (0, 0), 'sawtooth-row': (14, 22), 'box-front': (0, 0),
                  'cross-wall': (12, 16), 'street-monopitch': (8, 12), 'butterfly-row': (10, 15), 'tile-hung': (18, 24)})
STEP = 1.2
SETBACK = 2.2


def terrace_look(form, seed, w, d, storeys):
    s = whf.new_look(seed, w, d, storeys, form)
    s['dir'] = random.Random(seed + 11).choice([-1, 1])
    if form == 'box-front':
        s['variant'] = 'full'
    if form in ('street-monopitch', 'butterfly-row', 'tile-hung'):
        s['panels'] = False
    if form == 'street-monopitch':
        s['accent'] = hf.textured('wood-siding-005')
    if form == 'butterfly-row':
        s['upper'] = hf.textured('painted-plaster-wall', 'd8d6d0')
        metal = random.Random(seed + 13).choice(whf.METAL)
        s['roof'] = hf.textured('standing-seam', metal)
        s['trim'] = hf.flat(metal, .4)
    if form == 'bay-picture':
        s['upper'] = s['ground']
    if form == 'tile-hung':
        s['upper'] = random.Random(seed + 12).choice([hf.textured('clay-roof-tiles-02'), hf.textured('wood-siding-005')])
    return s


def walls(h, y0=None, storeys=None, door=True, z0=0.0):
    """The street and garden walls by the look's period, windows on a free end and a blank wall on each shared side."""
    s = h['s']
    x0, x1, y1 = h['x0'], h['x1'], h['y1']
    y0 = h['y0'] if y0 is None else y0
    st = storeys or h['storeys']
    free = tuple(e for e, shared in ((-1, h['left']), (1, h['right'])) if not shared)
    doors = whf.mrow(s, 1, x1 - x0, x0, y0, y1, st, ends=free, door=door, z0=z0, first=h['pos'])
    for shared, face in ((h['left'], ts.Face((x0, y1, z0), (0, -1, 0), (-1, 0, 0))),
                         (h['right'], ts.Face((x1, y0, z0), (0, 1, 0), (1, 0, 0)))):
        if shared:
            ts.facade('party wall', face, y1 - y0, st * S, [], s['end'])
    return doors


def upstand(h, profile):
    for shared, x in ((h['left'], h['x0']), (h['right'], h['x1'])):
        if shared:
            rf.upstand(h, x, profile)


def eaveless_gable(h):
    """A steep front gable to each house with no eaves, glazed in the gable. Every other house stands back."""
    s, x0, x1, y1 = h['s'], h['x0'], h['x1'], h['y1']
    H = h['storeys'] * S
    y0 = h['y0'] + (STEP if h['pos'] % 2 else 0)
    doors = walls(h, y0=y0)
    cx, cy, A, L = (x0 + x1) / 2, (y0 + y1) / 2, (x1 - x0) / 2, (y1 - y0) / 2
    whf.hipgable(s, 'y', cx, cy, A, L, H, h['pitch'], eaves=s['eaves'])
    whf.gable_edges(s, 'y', cx, cy, A, L, H, h['pitch'], s['eaves'])
    whf.gable_glass(s, cx, y0, H, A, h['pitch'])
    return doors


def roof_terrace(h):
    """A flat roof with the top storey set back behind a glass rail and screens between the terraces."""
    s, x0, x1, y0, y1 = h['s'], h['x0'], h['x1'], h['y0'], h['y1']
    low = h['storeys'] - 1
    Hl = low * S
    doors = walls(h, storeys=low)
    whf.parapet(s, x0, x1, y0, y1, Hl)
    keep = s['ground'], s['upper'], s['variant']
    s['ground'] = s['upper'] = s['accent']
    s['variant'] = 'full'
    walls(h, y0=y0 + SETBACK, storeys=1, door=False, z0=Hl)
    s['ground'], s['upper'], s['variant'] = keep
    xa, xb = x0 - (0 if h['left'] else .3), x1 + (0 if h['right'] else .3)
    ts.box('roof', (xa, y0 + SETBACK - .4, Hl + S), (xb, y1 + .3, Hl + S + .25), 'trim')
    ts.box('terrace glass', (x0 + .3, y0 + .1, Hl + .5), (x1 - .3, y0 + .14, Hl + 1.05), 'glass')
    for shared, a, b in ((h['left'], x0 + .25, x0 + .33), (h['right'], x1 - .33, x1 - .25)):
        if shared:
            ts.box('terrace screen', (a, y0 + .25, Hl), (b, y0 + SETBACK, Hl + 1.8), s['accent'])
    return doors


def sawtooth_row(h):
    """A mono-pitch to each house, every house of the terrace rising the same way, glazed in its step."""
    s, x0, x1, y0, y1 = h['s'], h['x0'], h['x1'], h['y0'], h['y1']
    H, d, e = h['storeys'] * S, s['dir'], s['eaves']
    rise = (x1 - x0) * math.tan(math.radians(h['pitch']))
    doors = walls(h)
    lo, hi = (x0, x1) if d > 0 else (x1, x0)
    poly('mono', [(lo, y0 - e, H), (hi, y0 - e, H + rise), (hi, y1 + e, H + rise), (lo, y1 + e, H)], 'roof')
    whf.slope_edges(s, (lo, y0 - e, H), (lo, y1 + e, H), (hi, y0 - e, H + rise), (hi, y1 + e, H + rise))
    for y in (y0, y1):
        poly('mono end', [(lo, y, H), (hi, y, H), (hi, y, H + rise)], 'wall')
    poly('step', [(hi, y0, H), (hi, y1, H), (hi, y1, H + rise), (hi, y0, H + rise)], 'wall')
    if rise > 1.0:
        x = hi + d * .02
        ts.box('clerestory', (min(x, x + d * .03), y0 + .6, H + .3), (max(x, x + d * .03), y1 - .6, H + rise - .3), 'glass')
    ts.box('step coping', (hi - .12, y0 - e, H + rise), (hi + .12, y1 + e, H + rise + .08), s['edges']['cap'])
    return doors


def box_front(h):
    """A flat roof behind a thin coping, the upper storeys framed by a box standing out from the street wall."""
    s, x0, x1, y0, y1 = h['s'], h['x0'], h['x1'], h['y0'], h['y1']
    H, w = h['storeys'] * S, x1 - x0
    doors = walls(h)
    whf.parapet(s, x0, x1, y0, y1, H, height=.3)
    face = ts.Face((x0, y0, 0), (1, 0, 0), (0, -1, 0))
    a, b, v0, v1, t = .3, w - .3, S - .1, H - .25, .22
    for u0, u1, z0, z1 in ((a, b, v0, v0 + t), (a, b, v1 - t, v1), (a, a + t, v0, v1), (b - t, b, v0, v1)):
        face.slab('box frame', u0, u1, z0, z1, -.65, 0, s['accent'])
    return doors


def cross_wall(h):
    """A shallow side gable with deep eaves, brick cross walls standing forward on the party lines, and
    panels under ribbon windows."""
    s, x0, x1, y0, y1 = h['s'], h['x0'], h['x1'], h['y0'], h['y1']
    H = h['storeys'] * S
    doors = walls(h)
    side_gable(h, y0, y1, H, s['eaves'])
    for shared, a, b in ((h['left'], x0, x0 + (.15 if h['left'] else .26)), (h['right'], x1 - (.15 if h['right'] else .26), x1)):
        ts.box('cross wall', (a, y0 - .45, 0), (b, y0, H - .15), s['fin'])
        ts.box('cross wall', (a, y1, 0), (b, y1 + .3, H - .15), s['fin'])
    return doors


def side_gable(h, y0, y1, H, e, hips=False):
    """A ridge along the street over the house. A free end overhangs, or hips where `hips`; a shared end
    stops on the party wall, carries an upstand and shows its gable above a lower neighbor."""
    s, x0, x1 = h['s'], h['x0'], h['x1']
    t = math.tan(math.radians(h['pitch']))
    A, cy = (y1 - y0) / 2, (y0 + y1) / 2
    top, low = H + A * t, H - e * t
    xa = x0 - (0 if h['left'] else (e if hips else .3))
    xb = x1 + (0 if h['right'] else (e if hips else .3))
    ra = xa + (A + e if hips and not h['left'] else 0)
    rb = xb - (A + e if hips and not h['right'] else 0)
    if ra > rb:
        ra = rb = (ra + rb) / 2
    for side in (-1, 1):
        ye = cy + side * (A + e)
        poly('roof slope', [(xa, ye, low), (xb, ye, low), (rb, cy, top), (ra, cy, top)], 'roof')
    for free, x, r in ((not h['left'], xa, ra), (not h['right'], xb, rb)):
        if hips and free:
            poly('hip', [(x, cy - A - e, low), (x, cy + A + e, low), (r, cy, top)], 'roof')
    for shared, x in ((h['left'], x0), (h['right'], x1)):
        if shared or not hips:
            poly('gable end', [(x, y0, H), (x, y1, H), (x, cy, top)], 'wall')
    edge = A + .15
    upstand(h, [(cy - edge, H - .3), (cy + edge, H - .3), (cy + edge, top - edge * t + .25), (cy, top + .25),
                (cy - edge, top - edge * t + .25)])
    for ye, f in ((cy - A - e, -1), (cy + A + e, 1)):
        ts.box('fascia', (xa, ye + f * .03 - .03, low - .25), (xb, ye + f * .03 + .03, low + .03), 'trim')


def street_monopitch(h):
    """One low mono-pitch rising toward the street over a timber clerestory band, a deep fascia at its high edge."""
    s, x0, x1, y0, y1 = h['s'], h['x0'], h['x1'], h['y0'], h['y1']
    H, w, D = h['storeys'] * S, x1 - x0, y1 - y0
    t = min(math.tan(math.radians(h['pitch'])), 3.0 / D)
    rise = D * t
    doors = walls(h)
    face = ts.Face((x0, y0, H), (1, 0, 0), (0, -1, 0))
    ops = [(.4, .3, w - .8, rise - .6, 'window')] if rise > 1.2 else []
    ts.facade('clerestory', face, w, rise, ops, s['accent'])
    for x in (x0, x1):
        poly('end', [(x, y0, H), (x, y1, H), (x, y0, H + rise)], 'wall')
    ef, eb = .7, .35
    xa, xb = x0 - (0 if h['left'] else .3), x1 + (0 if h['right'] else .3)
    zf, zb = H + rise + ef * t, H - eb * t
    poly('mono', [(xa, y0 - ef, zf), (xb, y0 - ef, zf), (xb, y1 + eb, zb), (xa, y1 + eb, zb)], 'roof')
    ts.box('fascia', (xa, y0 - ef - .04, zf - .35), (xb, y0 - ef, zf + .02), 'trim')
    upstand(h, [(y0 - .1, H - .3), (y1 + .1, H - .3), (y1 + .1, H - .1 * t + .25), (y0 - .1, H + rise + .1 * t + .25)])
    return doors


def butterfly_row(h):
    """An inverted roof to each house, falling from its party walls to a valley on its center line."""
    s, x0, x1, y0, y1 = h['s'], h['x0'], h['x1'], h['y0'], h['y1']
    H, e = h['storeys'] * S, .45
    t = math.tan(math.radians(h['pitch']))
    c = (x0 + x1) / 2
    rise = (c - x0) * t
    doors = walls(h)
    ea, eb = (0 if h['left'] else e), (0 if h['right'] else e)
    za, zb = H + rise + ea * t, H + rise + eb * t
    poly('butterfly', [(x0 - ea, y0 - e, za), (c, y0 - e, H), (c, y1 + e, H), (x0 - ea, y1 + e, za)], 'roof')
    poly('butterfly', [(c, y0 - e, H), (x1 + eb, y0 - e, zb), (x1 + eb, y1 + e, zb), (c, y1 + e, H)], 'roof')
    for y in (y0, y1):
        poly('butterfly wall', [(x0, y, H), (c, y, H), (x0, y, H + rise)], 'wall')
        poly('butterfly wall', [(c, y, H), (x1, y, H), (x1, y, H + rise)], 'wall')
        f = -1 if y == y0 else 1
        yo = y + f * e
        poly('fascia', [(x0 - ea, yo, za - .3), (c, yo, H - .3), (c, yo, H + .02), (x0 - ea, yo, za + .02)], 'trim')
        poly('fascia', [(c, yo, H - .3), (x1 + eb, yo, zb - .3), (x1 + eb, yo, zb + .02), (c, yo, H + .02)], 'trim')
    for x in (x0, x1):
        poly('end', [(x, y0, H), (x, y1, H), (x, y1, H + rise), (x, y0, H + rise)], 'wall')
    top = H + rise
    upstand(h, [(y0 - e, top - .3), (y1 + e, top - .3), (y1 + e, top + .2), (y0 - e, top + .2)])
    return doors


def tile_hung(h):
    """A low roof along the street with deep eaves, hipped at a free end, over an upper storey hung with
    tiles or boards and a canopy along the ground floor."""
    s, x0, x1, y0, y1 = h['s'], h['x0'], h['x1'], h['y0'], h['y1']
    H = h['storeys'] * S
    doors = walls(h)
    side_gable(h, y0, y1, H, .75, hips=True)
    ts.box('canopy', (x0 + (0 if h['left'] else .2), y0 - 1.0, 2.55), (x1 - (0 if h['right'] else .2), y0, 2.67), 'trim')
    return doors


def parapet_infill(h):
    """The parapet form's massing, re-dressed as contemporary infill: tall openings, a plain parapet in the
    wall material and a thin metal coping, no cornice or brackets. The roof deck stands inside the walls,
    so its edges never share a wall's face."""
    s, x0, x1, y0, y1 = h['s'], h['x0'], h['x1'], h['y0'], h['y1']
    H = h['storeys'] * S
    doors = walls(h)
    top = H + .9
    ts.box('parapet', (x0, y0, H), (x1, y0 + .25, top), s['upper'])
    ts.box('back parapet', (x0, y1 - .25, H), (x1, y1, H + .5), s['upper'])
    ts.box('roof deck', (x0 + .15, y0 + .25, H - .2), (x1 - .15, y1 - .25, H + .05), s['flat'])
    for shared, x, inward in ((h['left'], x0, 1), (h['right'], x1, -1)):
        a, b = sorted((x, x + inward * (.15 if shared else .25)))
        ts.box('side parapet', (a, y0, H), (b, y1, H + (.6 if shared else .5)), s['end'])
    ts.box('coping', (x0, y0 - .04, top), (x1, y0 + .29, top + .06), s['edges']['cap'])
    return doors


def mansard_zinc(h):
    """The mansard form's massing, re-dressed as contemporary: the top storey clad in standing-seam metal,
    lit by flush framed boxes instead of hooded dormers, with no cornice."""
    s, x0, x1, y0, y1 = h['s'], h['x0'], h['x1'], h['y0'], h['y1']
    H2 = (h['storeys'] - 1) * S
    Ht, k, cy = H2 + S, 1.1, (y0 + y1) / 2
    crown = Ht + .35
    doors = walls(h, storeys=h['storeys'] - 1)
    xa, xb = x0 - (0 if h['left'] else .1), x1 + (0 if h['right'] else .1)
    poly('mansard front', [(xa, y0 - .05, H2), (xb, y0 - .05, H2), (xb, y0 + k, Ht), (xa, y0 + k, Ht)], 'roof')
    poly('mansard back', [(xb, y1 + .05, H2), (xa, y1 + .05, H2), (xa, y1 - k, Ht), (xb, y1 - k, Ht)], 'roof')
    poly('crown front', [(xa, y0 + k, Ht), (xb, y0 + k, Ht), (xb, cy, crown), (xa, cy, crown)], 'roof')
    poly('crown back', [(xb, y1 - k, Ht), (xa, y1 - k, Ht), (xa, cy, crown), (xb, cy, crown)], 'roof')
    for x in (x0, x1):
        poly('mansard end', [(x, y0, H2), (x, y1, H2), (x, y1 - k, Ht), (x, cy, crown), (x, y0 + k, Ht)], 'wall')
    ts.box('eaves band', (x0, y0 - .1, H2 - .12), (x1, y0, H2), s['edges']['cap'])
    w = x1 - x0
    n = max(1, round(w / 4.0))
    b = w / n
    for i in range(n):
        c = x0 + (i + .5) * b
        half = min(1.3, b / 2 - .35)
        yf = y0 + .15
        ts.box('box dormer', (c - half, yf, H2 + .15), (c + half, y0 + k + .3, H2 + 2.75), s['edges']['cap'])
        ts.box('dormer glass', (c - half + .12, yf - .03, H2 + .3), (c + half - .12, yf, H2 + 2.6), 'glass')
    upstand(h, [(y0 - .1, H2 - .3), (y1 + .1, H2 - .3), (y1 + .1, H2 + .25), (y1 - k + .05, Ht + .25), (cy, crown + .25),
                (y0 + k - .05, Ht + .25), (y0 - .1, H2 + .25)])
    return doors


def bay_picture(h):
    """The bay front's massing, re-dressed as mid-century: the canted bay becomes a squared two-storey box
    with a picture window over a colored panel, under a lower side gable."""
    s, x0, x1, y0, y1 = h['s'], h['x0'], h['x1'], h['y0'], h['y1']
    w, H = x1 - x0, h['storeys'] * S
    n = max(1, round(w / rf.BAY))
    if n == 1:
        doors = walls(h)
        side_gable(h, y0, y1, H, s['eaves'])
        return doors
    yf = y0 + .9
    doors = walls(h, y0=yf)
    for x, inward in ((x0, 1), (x1, -1)):
        a, c = sorted((x, x + inward * .25))
        ts.box('pier', (a, y0, 0), (c, yf, H), s['ground'])
    b = w / n
    at = n - 2 if h['pos'] % 2 else 1
    bc, bw = x0 + (at + .5) * b, min(3.4, b - .4)
    top = min(2 * S, H)
    ts.box('bay', (bc - bw / 2, y0, .3), (bc + bw / 2, yf, top), s['ground'])
    ts.box('bay base', (bc - bw / 2, y0, 0), (bc + bw / 2, yf, .3), 'plinth')
    for z0, z1 in ((.75, 2.6), (S + .9, S + 2.6)):
        ts.box('picture window', (bc - bw / 2 + .15, y0 - .03, z0), (bc + bw / 2 - .15, y0, z1), 'glass')
        ts.box('mullion', (bc - .03, y0 - .06, z0), (bc + .03, y0 - .03, z1), 'frame')
    ts.box('bay panel', (bc - bw / 2 + .15, y0 - .03, 2.75), (bc + bw / 2 - .15, y0, S + .75), s['accent'])
    ts.box('bay lid', (bc - bw / 2 - .1, y0 - .12, top), (bc + bw / 2 + .1, yf, top + .18), 'trim')
    side_gable(h, yf, y1, H, s['eaves'])
    return doors


NEW = {'eaveless-gable': eaveless_gable, 'roof-terrace': roof_terrace, 'sawtooth-row': sawtooth_row,
       'box-front': box_front, 'cross-wall': cross_wall, 'street-monopitch': street_monopitch,
       'butterfly-row': butterfly_row, 'tile-hung': tile_hung, 'parapet-infill': parapet_infill,
       'mansard-zinc': mansard_zinc, 'bay-picture': bay_picture}
TRADITIONAL = ['side-gable', 'parapet', 'mansard', 'front-gable', 'paired-gable', 'bay-front']


def terrace(form, widths, storeys, r, d=12.0):
    """A terrace of one form: its houses share a look and pitch."""
    seed = r.randrange(1 << 30)
    if form in NEW:
        look = terrace_look(form, seed, widths[0], d, max(storeys))
        pitch = look['pitch']
    else:
        look = rf.terrace_look(seed, widths[0], d, max(storeys))
        rng = rf.pitch_range(form, widths[0])
        pitch = r.uniform(*rng) if rng else 0
    return [{'form': form, 'w': w, 'st': st, 'look': look, 'seed': r.randrange(1 << 30), 'pitch': pitch}
            for w, st in zip(widths, storeys)]


def row(houses, x, d, y_front):
    """Adjacent houses are attached. A house's place counts from the start of its terrace."""
    for i, p in enumerate(houses):
        start = i
        while start > 0 and houses[start - 1]['look'] is p['look']:
            start -= 1
        end = i
        while end + 1 < len(houses) and houses[end + 1]['look'] is p['look']:
            end += 1
        pos = i - start
        h = {'x0': x, 'x1': x + p['w'], 'y0': y_front, 'y1': y_front + d, 'storeys': p['st'], 'left': i > 0,
             'right': i + 1 < len(houses), 'mirror': pos % 2 == 1, 'pitch': p['pitch'], 'pos': pos,
             'wing': 1 if pos % 2 == 0 else -1, 'pair': 0}
        form = p['form']
        if form in NEW:
            h['s'] = p['look']
            whf.bind(h['s'])
            NEW[form](h)
        else:
            h['s'] = rf.house_look(p['look'], p['seed'], p['w'])
            if form == 'paired-gable':
                h['pair'] = 1 if pos % 2 == 0 and i < end else -1 if pos % 2 == 1 else 0
            (rf.front_gable if form == 'paired-gable' and h['pair'] == 0 else rf.FORMS[form][0])(h)
        x += p['w']
    return x


def finish(camera, shots):
    for part in ts.parts:
        ts.project_uvs(part)
    for name, eye, target in shots:
        hf.shoot(camera, name, eye, target)


def tile(form, seed=404):
    """The form at 5, 6 and 8 m: a terrace of two storeys and one of three."""
    hf.fresh()
    camera = hf.stage(road_y=(-14.0, -6.0))
    r = random.Random(seed)
    x = -22.0
    for st in (2, 3):
        x = row(terrace(form, [5.0, 6.0, 8.0], [st] * 3, r), x, 12.0, -3.0) + 6
    finish(camera, [(f'tile-{form}', (-10, -58, 30), (-1, 3, 4))])


def junction(form, seed=505):
    """The form between a three-storey side-gabled terrace and a two-storey parapet terrace."""
    hf.fresh()
    camera = hf.stage(road_y=(-14.0, -6.0))
    r = random.Random(seed)
    houses = (terrace('side-gable', [6.0, 6.0], [3, 3], r) + terrace(form, [6.0, 5.0, 8.0], [2, 3, 3], r)
              + terrace('parapet', [6.0, 6.0], [2, 2], r))
    row(houses, -21.5, 12.0, -3.0)
    finish(camera, [(f'junction-{form}', (-6, -44, 24), (0, 3, 6))])


def street(name, sides, seed):
    """Eighteen 6 m houses a side, each side a list of (form, houses) terraces."""
    hf.fresh()
    camera = hf.stage(road_y=(-5.0, 5.0))
    r = random.Random(seed)
    for (turned, y), plan in zip(((False, 8.0), (True, -8.0)), sides):
        houses = []
        for form, n in plan:
            st = r.choice([2, 3, 3])
            houses += terrace(form, [6.0] * n, [st if r.random() < .8 else 5 - st for _ in range(n)], r)
            print('STREET', name, turned, form, PERIOD.get(form, 'traditional'), n)
        start = len(ts.parts)
        row(houses, -54.0, 12.0, 0.0)
        for part in ts.parts[start:]:
            if turned:
                part.rotation_euler[2] = math.pi
            part.location = (0, y, 0)
    finish(camera, [(f'{name}-game', (0, -110, 95), (0, 8, 0)), (f'{name}-street', (-60, -3, 5), (-20, 12, 8))])


# The shipped mix: six traditional forms. The target mix: 20 contemporary, 9 mid-century and 7
# traditional houses of 36, every recommended form at least once.
NOW = [[('front-gable', 6), ('parapet', 3), ('bay-front', 5), ('side-gable', 4)],
       [('front-gable', 5), ('side-gable', 3), ('paired-gable', 4), ('mansard', 3), ('parapet', 3)]]
MIXED = [[('eaveless-gable', 4), ('cross-wall', 3), ('sawtooth-row', 4), ('bay-front', 3), ('roof-terrace', 4)],
         [('box-front', 4), ('tile-hung', 3), ('parapet', 4), ('butterfly-row', 3), ('eaveless-gable', 4)]]
SHARES_B = [[('eaveless-gable', 4), ('side-gable', 4), ('cross-wall', 3), ('roof-terrace', 4), ('bay-front', 3)],
            [('sawtooth-row', 4), ('parapet', 4), ('butterfly-row', 3), ('box-front', 4), ('tile-hung', 3)]]
SHARES_C = [[('front-gable', 4), ('eaveless-gable', 4), ('cross-wall', 3), ('mansard', 3), ('roof-terrace', 4)],
            [('bay-front', 4), ('sawtooth-row', 5), ('butterfly-row', 3), ('parapet', 3), ('tile-hung', 3)]]


JOBS = {f'tile-{f}': (lambda f=f: tile(f)) for f in list(NEW) + TRADITIONAL}
JOBS.update({f'junction-{f}': (lambda f=f: junction(f)) for f in list(NEW) + TRADITIONAL})
JOBS['street-now'] = lambda: street('street-now', NOW, 83)
JOBS['street-mixed'] = lambda: street('street-mixed', MIXED, 83)
JOBS['street-b'] = lambda: street('street-b', SHARES_B, 83)
JOBS['street-c'] = lambda: street('street-c', SHARES_C, 83)

for name, job in JOBS.items():
    if not ONLY or name in ONLY:
        job()
        print('DONE', name)

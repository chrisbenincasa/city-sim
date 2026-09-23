"""Measure generated Building shells, or profile the opening camera, in the shell.

The band suite takes research/procedural-buildings REPORT §3 measurements 2-4. The opening suite
hides layer groups at the whole-city camera to attribute its cost. Build the shell in Release first:
  dotnet build src/Borough.Godot -c Release -p:OutputPath=$PWD/src/Borough.Godot/.godot/mono/temp/bin/Debug/
"""
import argparse
import csv
import hashlib
import json
import os
from pathlib import Path
import socket
import statistics
import subprocess
import time

# (name, focus command or None for the opening camera, cases)
# A case is (label, render probe, shell-band arguments).
BAND = [
    ('boxes', 'baseline', '0 0 0 on'),
    ('boxes-sun-off', 'shadows-off', '0 0 0 on'),
    ('r150', 'baseline', '150 64 15 on'),
    ('r250', 'baseline', '250 64 15 on'),
    ('r500', 'baseline', '500 64 15 on'),
    ('r250-shells-cast-off', 'baseline', '250 64 15 off'),
    ('r500-shells-cast-off', 'baseline', '500 64 15 off'),
    ('r500-sun-off', 'shadows-off', '500 64 15 on'),
    ('r250-per-building', 'baseline', '250 0 15 on'),
    ('r250-no-kit', 'baseline', '250 64 0 on'),
]
LAYERS = {
    'trees': 'tree,rock',
    'streets': 'road,footway,kerb',
    'buildings': 'building,roof,hip,paired-roof,parapet,yard',
    'ground': 'ground,hazard,water,flood,cell,plot,zone',
}
OPENING = [
    ('boxes', 'baseline', '0 0 0 on'),
    ('sun-off', 'shadows-off', '0 0 0 on'),
    ('no-3d', 'no-3d', '0 0 0 on'),
    *[(f'hide-{group}', f'hide-{layers}', '0 0 0 on') for group, layers in LAYERS.items()],
    ('hide-all', 'hide-' + ','.join(LAYERS.values()), '0 0 0 on'),
]
SUITES = {'band': [
    ('opening', None, [BAND[0], BAND[1]]),
    ('street', 'focus 1781 1656 150', BAND),
    ('district', 'focus 1781 1656 600', [BAND[0], BAND[1], BAND[3], BAND[4], BAND[6], BAND[7]]),
], 'opening': [('opening', None, OPENING)]}

parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
parser.add_argument('--output', type=Path, required=True)
parser.add_argument('--ruleset', default='rulesets/stress-shopping.toml')
parser.add_argument('--citizens', type=int, default=1_000_000)
parser.add_argument('--start-at', type=int, default=600)
parser.add_argument('--warmup', type=int, default=4)
parser.add_argument('--seconds', type=int, default=8)
parser.add_argument('--suite', choices=SUITES, default='band')
parser.add_argument('--chunk-metres', choices=['256', '512', '1024'], default='256')
args = parser.parse_args()

root = Path(__file__).resolve().parents[1]
output = args.output.resolve()
output.mkdir(parents=True, exist_ok=True)
(output / 'boot.drive').write_text(f'{args.start_at} pause\n')
address = str(Path(os.environ.get('XDG_RUNTIME_DIR', '/tmp')) / f'borough-band-{os.getpid()}.sock')
command = [os.environ.get('GODOT_BIN', 'godot'), '--path', str(root / 'src/Borough.Godot'),
           '--disable-vsync', '--max-fps', '0', '--', '--ruleset', args.ruleset,
           '--citizens', str(args.citizens), '--start-at', str(args.start_at),
           '--drive', str(output / 'boot.drive'), '--listen', address]
env = dict(os.environ, BOROUGH_RENDER_PROFILE='1', BOROUGH_RENDER_CHUNK_METRES=args.chunk_metres,
           BOROUGH_PERFORMANCE_LOG=str(output / 'samples.csv'))
assembly = root / 'src/Borough.Godot/.godot/mono/temp/bin/Debug/Borough.Godot.dll'
manifest = {
    'command': command,
    'revision': subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root, text=True).strip(),
    'dirty': bool(subprocess.check_output(['git', 'status', '--porcelain'], cwd=root, text=True).strip()),
    'assembly_sha256': hashlib.sha256(assembly.read_bytes()).hexdigest(),
    'gpu': subprocess.check_output(['nvidia-smi', '--query-gpu=name,driver_version',
                                    '--format=csv,noheader'], text=True).strip(),
    'warmup_seconds': args.warmup, 'sample_seconds': args.seconds,
    'statistic': 'median of one-second samples',
    'suite': args.suite, 'chunk_metres': int(args.chunk_metres),
    'load_average_at_start': os.getloadavg(),
}


wire = None


def send(text):
    wire.write((text + '\n').encode())
    reply = wire.readline().decode()
    if not reply.startswith('ok\t'):
        raise RuntimeError(f'{text}: {reply}')
    return reply


def rows():
    with (output / 'samples.csv').open() as data:
        return list(csv.DictReader(data))


def state(path):
    """Reads the shell's UI state and keeps only the fields a measurement cites."""
    send(f'ui read {path}')
    full = json.loads(path.read_text())
    kept = {k: full[k] for k in ('Tick', 'Hash', 'Camera', 'Rendering', 'Viewport')}
    path.write_text(json.dumps(kept, indent=2) + '\n')
    return kept


def counts(path):
    """Objects, draw calls and primitives in the last frame, per render pass."""
    found = {}
    for line in Path(str(path) + '.profile.tsv').read_text().splitlines():
        if line.startswith('render_info\t'):
            _, render_pass, objects, draws, primitives = line.split('\t')
            found[render_pass] = {'objects': int(objects), 'draw_calls': int(draws), 'primitives': int(primitives)}
    return found


def profile(path):
    for line in Path(str(path) + '.profile.tsv').read_text().splitlines():
        if line.startswith('shell_band\t'):
            names = ['radius', 'chunk', 'kit', 'shadows', 'buildings', 'chunks', 'vertices', 'triangles',
                     'kit_pieces', 'collect_ms', 'generate_ms', 'upload_ms', 'slowest_upload_ms']
            return dict(zip(names, (float(v) if v.replace('.', '').isdigit() else v for v in line.split('\t')[1:])))
    return None


results = []
with (output / 'game.log').open('w') as log:
    process = subprocess.Popen(command, cwd=root, env=env, stdout=log, stderr=subprocess.STDOUT)
    try:
        deadline = time.monotonic() + 120
        while not Path(address).exists():
            if process.poll() is not None or time.monotonic() > deadline:
                raise RuntimeError(f'Game did not start; see {output / "game.log"}')
            time.sleep(.5)
        with socket.socket(socket.AF_UNIX) as connection:
            connection.settimeout(1800)
            connection.connect(address)
            wire = connection.makefile('rwb', buffering=0)

            while f'Tick {args.start_at} ' not in send(''):
                time.sleep(5)
            initial = state(output / 'initial.json')
            manifest['rendering'] = initial['Rendering']
            assert initial['Rendering']['Configuration'] == 'Release', 'Build the shell in Release'
            assert initial['Rendering']['Vsync'] == 'Disabled' and initial['Rendering']['FrameLimit'] == 0
            send('ui debug off')

            for view, focus, cases in SUITES[args.suite]:
                if focus:
                    send(focus)
                    send('tilt 35')
                state(output / f'{view}-camera.json')
                for label, probe, band in cases:
                    name = f'{view}-{label}'
                    send(f'ui render-probe {probe}')
                    send(f'ui shell-band {band}')
                    send(f'draw {output / (name + ".tsv")}')
                    upload = profile(output / (name + '.tsv'))
                    (output / (name + '.tsv')).unlink()
                    time.sleep(args.warmup)
                    start = len(rows())
                    time.sleep(args.seconds)
                    window = rows()[start:]
                    send(f'draw {output / (name + ".tsv")}')
                    drawn = counts(output / (name + '.tsv'))
                    (output / (name + '.tsv')).unlink()
                    send(f'shoot {output / (name + ".png")}')
                    Path(output / (name + '.txt')).unlink(missing_ok=True)

                    def median(key):
                        return statistics.median(float(r[key]) for r in window)

                    result = {'view': view, 'case': label, 'probe': probe, 'band': band,
                              'samples': len(window), 'fps': median('fps'),
                              'frame_ms': 1000 / median('fps'), 'gpu_ms': median('gpu_ms'),
                              'render_cpu_ms': median('render_cpu_ms'), 'upload': upload,
                              'counts': drawn}
                    results.append(result)
                    print(json.dumps(result), flush=True)
            send('ui shell-band 0 0 0 on')
            send('quit')
        process.wait(timeout=120)
    finally:
        if process.poll() is None:
            process.kill()

manifest['load_average_at_end'] = os.getloadavg()
(output / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
(output / 'summary.json').write_text(json.dumps(results, indent=2) + '\n')

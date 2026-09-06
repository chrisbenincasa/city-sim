#!/usr/bin/env python3
"""Check retained large-world counts and camera-independent Building uploads (build Godot first)."""
import os
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
GODOT = os.environ.get('GODOT_BIN', 'godot')


def run(folder, ruleset, citizens, lines, verify=False):
    script = folder / (ruleset + '.drive')
    script.write_text('\n'.join(lines) + '\n')
    log = folder / (ruleset + '.log')
    environment = dict(os.environ, BOROUGH_RENDER_PROFILE='1')
    if verify:
        environment['BOROUGH_RENDER_VERIFY'] = '1'
    with log.open('w') as output:
        subprocess.run([GODOT, '--headless', '--path', str(ROOT / 'src/Borough.Godot'), '--',
                        '--ruleset', f'rulesets/{ruleset}.toml', '--citizens', str(citizens),
                        '--drive', str(script)], cwd=ROOT, env=environment, stdout=output,
                       stderr=subprocess.STDOUT, check=True, timeout=180)
    assert 'ERROR:' not in log.read_text(), log


def rows(path, kind):
    return {parts[1]: parts[2:] for line in path.read_text().splitlines()
            if (parts := line.split('\t'))[0] == kind}


def main():
    folder = Path(tempfile.mkdtemp(prefix='borough-renderer-'))
    print(f'Renderer scene artifacts: {folder}', flush=True)
    large = folder / 'large.tsv'
    repeated = folder / 'repeated.tsv'
    run(folder, 'bordered', 100, ['256 pause', f'256 draw {large}', f'256 draw {repeated}', '256 quit'])
    assert large.read_bytes() == repeated.read_bytes(), 'paused geometry changed'
    retained = rows(large, 'retained')
    for name in ['road', 'tree']:
        assert int(retained[name][0]) > 65536, (name, retained[name])
    for name in ['footway', 'kerb']:
        assert int(retained[name][0]) > 262144, (name, retained[name])
    initial = rows(Path(str(large) + '.profile.tsv'), 'render')
    again = rows(Path(str(repeated) + '.profile.tsv'), 'render')
    assert initial == again, 'paused draw uploaded unchanged geometry'

    before, turned, far, after = [folder / (name + '.tsv') for name in ['before', 'turned', 'far', 'after']]
    run(folder, 'pictured', 1000, ['256 pause', '256 focus 11650 4850 700', f'256 draw {before}',
        '256 turn left', f'256 draw {turned}', '256 focus 8200 8200 62000', f'256 draw {far}',
        '256 focus 11650 4850 700', f'256 draw {after}', '256 speed 8', '272 quit'], verify=True)
    original = rows(Path(str(before) + '.profile.tsv'), 'render')
    for path in [turned, far, after]:
        current = rows(Path(str(path) + '.profile.tsv'), 'render')
        for name in ['building', 'roof', 'hip', 'mansard', 'yard', 'road']:
            assert original[name] == current[name], (name, 'camera movement uploaded standing geometry')
    assert int(rows(far, 'layer')['tree'][0]) == 0, 'far detail buffers remained resident'
    assert int(rows(after, 'layer')['tree'][0]) > 0, 'returning camera lost foliage'
    print('PASS: old caps exceeded, paused drawing stable, camera does not rebuild standing geometry, detail returns.')


if __name__ == '__main__':
    main()

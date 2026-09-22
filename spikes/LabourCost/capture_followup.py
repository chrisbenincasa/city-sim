"""Capture the guarded-combining/storage experiment without overwriting the first report."""
from pathlib import Path
import datetime
import gzip
import hashlib
import json
import os
import platform
import subprocess
import time

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'plans/evidence/private-production-and-labour/combined'
OUT.mkdir(exist_ok=True)
os.chdir(ROOT)

def read(path):
    try:
        return Path(path).read_text().strip()
    except OSError:
        return None

def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()

def sample():
    return {'utc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
            'loadavg': read('/proc/loadavg'),
            'cpu': [s for s in read('/proc/stat').splitlines() if s.startswith('cpu')],
            'cpu2_khz': read('/sys/devices/system/cpu/cpu2/cpufreq/scaling_cur_freq'),
            'memory': read('/proc/meminfo')}

checkpoint = Path('/tmp/labour-followup-aged.borough')
checkpoint.write_bytes(gzip.decompress((OUT.parent / 'aged-10000.borough.gz').read_bytes()))
metadata = {'revision': subprocess.check_output(['git', 'rev-parse', 'HEAD'], text=True).strip(),
            'hostname': platform.node(), 'platform': platform.platform(),
            'lscpu': subprocess.check_output(['lscpu'], text=True),
            'governor': read('/sys/devices/system/cpu/cpu2/cpufreq/scaling_governor'),
            'sibling': read('/sys/devices/system/cpu/cpu2/topology/thread_siblings_list'),
            'no_turbo': read('/sys/devices/system/cpu/intel_pstate/no_turbo'),
            'dotnet': subprocess.check_output(['dotnet', '--info'], text=True),
            'checkpointSha256': digest(checkpoint),
            'rulesetSha256': digest(ROOT / 'rulesets/stress-shopping.toml'),
            'source_sha256': {str(p.relative_to(ROOT)): digest(p) for p in
                list((ROOT / 'spikes/LabourCost').glob('*.cs')) + [ROOT / 'spikes/LabourCost/LabourCost.csproj']}}
(OUT / 'machine.json').write_text(json.dumps(metadata, indent=2) + '\n')
cases = [('aged-10000', ['10000', '0', '4097', '-1', str(checkpoint)]),
         ('100000-seed0-high', ['100000', '0', '4097', '50']),
         ('1000000-seed0-high', ['1000000', '0', '4097', '50']),
         ('1000000-seed1-high', ['1000000', '1', '4097', '50']),
         ('1000000-seed0-low', ['1000000', '0', '16', '50'])]
for label, arguments in cases:
    cmd = ['taskset', '-c', '2', 'env', 'DOTNET_TieredCompilation=0', 'dotnet',
           'spikes/LabourCost/bin/Release/net10.0/LabourCost.dll', 'combined'] + arguments
    print(label, flush=True)
    with (OUT / f'{label}.jsonl').open('w') as output, (OUT / f'{label}-host.jsonl').open('w') as host:
        host.write(json.dumps({'command': cmd}) + '\n')
        with (OUT / f'{label}.stderr.txt').open('w') as errors:
            process = subprocess.Popen(cmd, stdout=output, stderr=errors)
            while process.poll() is None:
                host.write(json.dumps(sample()) + '\n'); host.flush()
                time.sleep(1)
            host.write(json.dumps({'exitCode': process.returncode, **sample()}) + '\n')
            if process.returncode:
                raise SystemExit(f'{label} failed: {process.returncode}')

#!/usr/bin/env python3
"""Check attribution boundaries with an inlined Step and unresolved native leaf frames."""
import json
from pathlib import Path
import subprocess
import sys
import tempfile

names = ['host', 'Borough.Headless.ProfileDump.TimedStep(sim)', 'ShoppingEngine.Step(tick)',
         'WalkScratch.Search(graph)', 'BuildingResidency.NthIn(area)', 'UNMANAGED_CODE_TIME',
         'System.Diagnostics.Stopwatch.GetTimestamp()', 'reporting',
         'Borough.Core.Simulation.Layers()', 'Borough.Core.Space.LineSourceQueries.Level(graph)',
         'TripEngine.Start(citizen)', 'CommuteEngine.Generate(tick)', 'TripEngine.AdvanceTravellers(tick)',
         'CPU_TIME']
events = []
def interval(start, end, frames):
    events.extend({'type': 'O', 'at': start, 'frame': frame} for frame in frames)
    events.extend({'type': 'C', 'at': end, 'frame': frame} for frame in reversed(frames))
interval(0, 100, [0, 7])
interval(100, 120, [0, 1, 2, 3, 5])
interval(120, 130, [0, 1, 2, 4, 5])
interval(130, 230, [0, 1, 6, 5])
interval(230, 240, [0, 1, 8, 9, 5])
interval(240, 255, [0, 1, 2, 10, 3, 5])
interval(255, 280, [0, 1, 11, 10, 3, 13])
interval(280, 285, [0, 1, 12, 5])
source = {'shared': {'frames': [{'name': name} for name in names]},
          'profiles': [{'type': 'evented', 'unit': 'milliseconds', 'startValue': 0, 'endValue': 285, 'events': events}]}
with tempfile.TemporaryDirectory(prefix='borough-profile-summary-test-') as folder:
    path = Path(folder)
    (path / 'input.json').write_text(json.dumps(source))
    subprocess.run([sys.executable, str(Path(__file__).with_name('summarize-simulation-profile.py')),
                    str(path / 'input.json'), '--output', str(path / 'result.json')],
                   stdout=subprocess.DEVNULL, check=True)
    result = json.loads((path / 'result.json').read_text())
    assert result['step_weight_ms'] == 85
    assert result['route_weight_ms_by_caller'] == {'shopping': 35, 'commuting': 25}
    assert result['building_selection_weight_ms_by_caller'] == {'shopping': 10}
    leaf = {row['method']: row['self_weight_ms'] for row in result['methods']}
    assert leaf['WalkScratch.Search(graph)'] == 60
    assert leaf['BuildingResidency.NthIn(area)'] == 10
    assert 'reporting' not in leaf and 'UNMANAGED_CODE_TIME' not in leaf
    assert 'CPU_TIME' not in leaf
    assert 'System.Diagnostics.Stopwatch.GetTimestamp()' not in leaf
    subprocess.run([sys.executable, str(Path(__file__).with_name('summarize-simulation-profile.py')),
                    str(path / 'input.json'), '--scope', 'layers', '--output', str(path / 'layers.json')],
                   stdout=subprocess.DEVNULL, check=True)
    layers = json.loads((path / 'layers.json').read_text())
    assert layers['step_weight_ms'] == 10
    assert layers['route_weight_ms_by_caller'] == {}
    assert layers['building_selection_weight_ms_by_caller'] == {}
    assert {row['method']: row['self_weight_ms'] for row in layers['methods']}[
        'Borough.Core.Space.LineSourceQueries.Level(graph)'] == 10
    subprocess.run([sys.executable, str(Path(__file__).with_name('summarize-simulation-profile.py')),
                    str(path / 'input.json'), '--scope', 'move', '--output', str(path / 'move.json')],
                   stdout=subprocess.DEVNULL, check=True)
    move = json.loads((path / 'move.json').read_text())
    assert move['step_weight_ms'] == 75
    assert move['move_weight_ms_by_work'] == {
        'shopping_estimate_search': 20, 'recorded_shopping_search': 15,
        'other_recorded_search': 25, 'non_search_move': 15}
    subprocess.run([sys.executable, str(Path(__file__).with_name('summarize-simulation-profile.py')),
                    str(path / 'input.json'), '--scope', 'commute', '--output', str(path / 'commute.json')],
                   stdout=subprocess.DEVNULL, check=True)
    commute = json.loads((path / 'commute.json').read_text())
    assert commute['step_weight_ms'] == 25
    assert commute['route_weight_ms_by_caller'] == {'commuting': 25}
    source['shared']['frames'][11]['name'] = 'WorkSchedule.Accrue(world,tick)'
    (path / 'payroll-input.json').write_text(json.dumps(source))
    subprocess.run([sys.executable, str(Path(__file__).with_name('summarize-simulation-profile.py')),
                    str(path / 'payroll-input.json'), '--scope', 'payroll', '--output', str(path / 'payroll.json')],
                   stdout=subprocess.DEVNULL, check=True)
    payroll = json.loads((path / 'payroll.json').read_text())
    assert payroll['step_weight_ms'] == 25
    assert all(row['method'] != 'CPU_TIME' for row in payroll['methods'])
print('PASS: excludes reporting and timestamps; handles inlined Step and native leaf markers.')

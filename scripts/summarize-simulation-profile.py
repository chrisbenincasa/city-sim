#!/usr/bin/env python3
"""Attribute Speedscope evented stack weights only beneath ProfileDump.TimedStep."""
import argparse
from collections import defaultdict
import json
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('trace', type=Path)
parser.add_argument('--output', type=Path, required=True)
parser.add_argument('--scope', choices=['step', 'layers', 'move', 'payroll', 'commute'], default='step')
args = parser.parse_args()
data = json.loads(args.trace.read_text())
frames = [frame['name'] for frame in data['shared']['frames']]
root_frames = {i for i, name in enumerate(frames) if 'Borough.Headless.ProfileDump.TimedStep(' in name}
if not root_frames:
    raise ValueError('No TimedStep frame; cannot attribute this capture. Rebuild the profiling host.')
exclusive, inclusive = defaultdict(float), defaultdict(float)
route_callers, selection_callers = defaultdict(float), defaultdict(float)
move_work = defaultdict(float)
total = 0.0
for profile in data['profiles']:
    if profile['type'] != 'evented' or profile['unit'] != 'milliseconds':
        raise ValueError('Expected dotnet-trace evented profiles in milliseconds.')
    stack = []
    previous = profile['startValue']
    for event in profile['events']:
        duration = event['at'] - previous
        if duration < 0:
            raise ValueError('Events are not ordered.')
        roots = [i for i, frame in enumerate(stack) if frame in root_frames]
        if duration > 0 and roots:
            active = [frames[i] for i in stack[roots[0]:]
                      if frames[i] not in ('UNMANAGED_CODE_TIME', 'CPU_TIME')]
            in_move = any(marker in name for name in active for marker in
                ('Simulation.Move(', 'CivicEngine.Step(', 'ShoppingEngine.Step(',
                 'CommuteEngine.Generate(', 'ServiceEngine.Attend(', 'TripEngine.Advance(',
                 'TripEngine.AdvanceTravellers(', 'TripEngine.ReleaseEnded(', 'TripEngine.CloseTick(',
                 'WorkSchedule.Accrue('))
            in_scope = args.scope == 'step' or (args.scope == 'move' and in_move) or (
                args.scope == 'payroll' and any('WorkSchedule.Accrue(' in name for name in active)) or (
                args.scope == 'commute' and any('CommuteEngine.Generate(' in name for name in active)) or (
                args.scope == 'layers' and any(marker in name for name in active for marker in
                ('Simulation.Layers(', 'MapLayers.Step(', 'MapLayers.SetLandValueTargets(', 'MapLayers.DriftLandValue(')))
            if in_scope and not any('System.Diagnostics.Stopwatch.GetTimestamp(' in name for name in active):
                total += duration
                exclusive[active[-1]] += duration
                for name in set(active):
                    inclusive[name] += duration
                caller = next((label for marker, label in [
                    ('ShoppingEngine.Step(', 'shopping'), ('CommuteEngine.Generate(', 'commuting'),
                    ('TripEngine.Advance(', 'movement'), ('EmploymentEngine.', 'employment')]
                    if any(marker in name for name in active)), 'other')
                if any('WalkScratch.Search(' in name for name in active):
                    route_callers[caller] += duration
                if in_move:
                    searching = any('WalkScratch.Search(' in name for name in active)
                    recorded = any('TripEngine.Start(' in name for name in active)
                    shopping = any('ShoppingEngine.Step(' in name for name in active)
                    category = ('recorded_shopping_search' if shopping else 'other_recorded_search') if searching and recorded else (
                        'shopping_estimate_search' if searching and shopping else 'other_search' if searching else 'non_search_move')
                    move_work[category] += duration
                if any('BuildingResidency.NthIn(' in name for name in active):
                    selection_callers[caller] += duration
        previous = event['at']
        if event['type'] == 'O':
            stack.append(event['frame'])
        elif event['type'] == 'C' and stack and stack[-1] == event['frame']:
            stack.pop()
        else:
            raise ValueError('Unbalanced stack events.')
    if stack:
        raise ValueError('Profile ended with open frames.')
if total <= 0:
    raise ValueError('No weighted samples beneath TimedStep.')
rows = [{'method': name, 'self_weight_ms': exclusive[name], 'self_percent': 100 * exclusive[name] / total,
         'inclusive_weight_ms': weight, 'inclusive_percent': 100 * weight / total}
        for name, weight in inclusive.items()]
rows.sort(key=lambda row: row['self_weight_ms'], reverse=True)
result = {'source': str(args.trace.resolve()), 'scope': 'Sampled stack weight beneath TimedStep, excluding ending timestamp stacks; not CPU utilization. Self means managed leaf, including unresolved native time. Inclusive rows overlap.',
          'selected_scope': args.scope, 'step_weight_ms': total, 'route_weight_ms_by_caller': dict(route_callers),
          'move_weight_ms_by_work': dict(move_work),
          'building_selection_weight_ms_by_caller': dict(selection_callers), 'methods': rows}
args.output.write_text(json.dumps(result, indent=2))
print(f"Selected scope: {args.scope}. {result['scope']}")
for row in rows[:20]:
    print(f'{row["self_percent"]:6.2f}% self  {row["inclusive_percent"]:6.2f}% inclusive  {row["method"]}')

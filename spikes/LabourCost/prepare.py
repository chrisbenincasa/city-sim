"""Generate the measured wage-pass clone; refuse a changed insertion site."""
from pathlib import Path
import hashlib
root = Path(__file__).resolve().parents[2]
source = (root / 'src/Borough.Core/Movement/WorkSchedule.cs').read_text()
needle = '            world.Citizens.EarnedWage[citizen] = earned > cap ? cap : earned;'
assert source.count(needle) == 1
source = source.replace('public static class WorkSchedule', 'public static class WorkScheduleLabour')
source = source.replace(needle, needle + '\n            LabourCost.Probe.Deposit(world, citizen, job, tick);')
(root / 'spikes/LabourCost/WorkScheduleLabour.g.cs').write_text(source)
print(hashlib.sha256(source.encode()).hexdigest())

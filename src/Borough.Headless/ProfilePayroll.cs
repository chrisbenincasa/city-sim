using Borough.Core.Entities;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;

namespace Borough.Headless;

internal sealed class ProfilePayroll(World world)
{
    private bool[] _employers = new bool[world.Businesses.Rows.SlotCount];
    internal Reading Last { get; private set; }

    // A diagnostic scan at the accrual boundary, not counters inside the payroll implementation.
    internal void Read(Ticks tick)
    {
        Last = default;
        if (!WorkSchedule.Runs(world)) { return; }
        if (_employers.Length < world.Businesses.Rows.SlotCount)
        { Array.Resize(ref _employers, world.Businesses.Rows.SlotCount); }
        Array.Clear(_employers);
        int live = 0, atWork = 0, ill = 0, declared = 0, employers = 0, rostered = 0, onDuty = 0;
        for (int c = 0; c < world.Citizens.Rows.SlotCount; c++)
        {
            if (!world.Citizens.Rows.IsLive(c)) { continue; }
            live++;
            if ((CitizenActivity)world.Citizens.Activity[c] != CitizenActivity.AtWork) { continue; }
            atWork++;
            if (CivicEngine.TooIllToWork(world, c)) { ill++; continue; }
            if (!world.Businesses.Rows.TryResolve(world.Citizens.Workplace[c], out int job)
                || !world.Rules.DeclaresBusiness(world.Businesses.Kind[job])) { continue; }
            declared++;
            if (!_employers[job]) { _employers[job] = true; employers++; }
            if (world.Citizens.CommuteBucket[c] != 0) { rostered++; }
            if (WorkSchedule.OnDuty(world, c, tick)) { onDuty++; }
        }
        Last = new(world.Citizens.Rows.SlotCount, live, atWork, ill, declared, employers, rostered, onDuty);
    }

    internal readonly record struct Reading(int CitizenSlots, int LiveCitizens, int AtWork, int TooIll,
        int DeclaredWorkers, int DistinctEmployers, int RosteredWorkers, int OnDuty);
}

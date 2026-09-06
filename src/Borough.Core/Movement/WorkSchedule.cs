using Borough.Core.Arithmetic;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;

namespace Borough.Core.Movement;

public static class WorkSchedule
{
    public static bool Runs(World world)
    {
        if (world.Rules.Shopping.Runs) { return true; }
        for (int kind = 1; kind <= world.Rules.BusinessKindCount; kind++)
        { if (world.Rules.BusinessKind((byte)kind).WorkDays != 0) { return true; } }
        return false;
    }

    public static bool OnDuty(World world, int citizen, Ticks tick)
    {
        if (!world.Businesses.Rows.TryResolve(world.Citizens.Workplace[citizen], out int job)
            || !world.Rules.DeclaresBusiness(world.Businesses.Kind[job])) { return false; }
        BusinessKindDefinition trade = world.Rules.BusinessKind(world.Businesses.Kind[job]);
        long day = IntegerMath.FloorDiv((long)tick.Raw, Ticks.PerDay);
        int start = CommuteRoster.ShiftStartOf(world.Key, world.Businesses.Rows.IdAt(job), trade);
        int phase = (int)(tick.Raw % Ticks.PerDay);
        int elapsed = phase - start;
        if (elapsed < 0) { elapsed += Ticks.PerDay; day--; }
        int weekday = WeeklyHours.DayOf((long)tick.Raw - elapsed);
        int days = trade.WorkDays == 0 ? 127 : trade.WorkDays;
        return (days & IntegerMath.ShiftLeft(1, weekday)) != 0
            && (ulong)elapsed < world.Rules.Jobs.ShiftLengthOf(world.Key, world.Citizens.Rows.IdAt(citizen)).Raw;
    }

    public static bool AwayTime(World world, int citizen, Ticks tick)
    {
        if (!CommuteRoster.TryPhasesOf(world.Citizens, world.Buildings, world.Businesses, world.Rules,
            world.Key, citizen, out int departure, out int home)) { return false; }
        int phase = (int)(tick.Raw % Ticks.PerDay);
        return departure <= home ? phase >= departure && phase < home : phase >= departure || phase < home;
    }

    public static bool DepartsToday(World world, int citizen, Ticks tick)
    {
        if (!world.Businesses.Rows.TryResolve(world.Citizens.Workplace[citizen], out int job)
            || !world.Rules.DeclaresBusiness(world.Businesses.Kind[job])) { return false; }
        BusinessKindDefinition trade = world.Rules.BusinessKind(world.Businesses.Kind[job]);
        long day = IntegerMath.FloorDiv((long)tick.Raw, Ticks.PerDay);
        int start = CommuteRoster.ShiftStartOf(world.Key, world.Businesses.Rows.IdAt(job), trade);
        if ((int)(tick.Raw % Ticks.PerDay) > start) { day++; }
        int days = trade.WorkDays == 0 ? 127 : trade.WorkDays;
        return (days & IntegerMath.ShiftLeft(1, WeeklyHours.DayOf(day * Ticks.PerDay + start))) != 0;
    }

    public static void Accrue(World world, Ticks tick)
    {
        if (!Runs(world)) { return; }
        for (int citizen = 0; citizen < world.Citizens.Rows.SlotCount; citizen++)
        {
            if (!world.Citizens.Rows.IsLive(citizen)
                || (CitizenActivity)world.Citizens.Activity[citizen] != CitizenActivity.AtWork
                || !OnDuty(world, citizen, tick)) { continue; }
            int job = world.Businesses.Rows.Resolve(world.Citizens.Workplace[citizen]);
            BusinessKindDefinition trade = world.Rules.BusinessKind(world.Businesses.Kind[job]);
            long length = (long)world.Rules.Jobs.ShiftLengthOf(world.Key, world.Citizens.Rows.IdAt(citizen)).Raw;
            if (length <= 0) { continue; }
            long scaled = world.Citizens.WageRemainder[citizen] + trade.WagePerDay;
            long whole = IntegerMath.FloorDiv(scaled, length);
            world.Citizens.WageRemainder[citizen] = scaled % length;
            long cap = (long)trade.WagePerDay * trade.PayPeriodDays;
            long earned = world.Citizens.EarnedWage[citizen] + whole;
            world.Citizens.EarnedWage[citizen] = earned > cap ? cap : earned;
        }
    }
}

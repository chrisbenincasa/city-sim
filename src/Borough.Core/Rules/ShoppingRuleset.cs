using Borough.Core.Quantities;

namespace Borough.Core.Rules;

public readonly record struct ShoppingRuleset(
    int Interval, int LowDays, int TargetDays, int SevereNeed,
    int KnownShops, int SearchCandidates, int RetryTicks)
{
    public bool Runs => Interval > 0;
}

public readonly record struct WeeklyHours(int Days, int Opens, int Closes)
{
    public bool Includes(int day) => (Days & Borough.Core.Arithmetic.IntegerMath.ShiftLeft(1, day)) != 0;

    public static int DayOf(long tick)
    {
        long day = Borough.Core.Arithmetic.IntegerMath.FloorDiv(tick, Ticks.PerDay);
        long phase = tick - day * Ticks.PerDay;
        if (phase >= Ticks.AtClock(0)) { day++; }
        return (int)((day % 7 + 7) % 7);
    }

    public bool IsOpen(Ticks tick)
    {
        int phase = (int)(tick.Raw % Ticks.PerDay);
        int elapsed = (phase - Ticks.AtClock(Opens) + Ticks.PerDay) % Ticks.PerDay;
        int end = Closes == 24 ? Ticks.AtClock(0) : Ticks.AtClock(Closes);
        int duration = Closes - Opens == 24 ? Ticks.PerDay
            : (end - Ticks.AtClock(Opens) + Ticks.PerDay) % Ticks.PerDay;
        return Includes(DayOf((long)tick.Raw)) && elapsed < duration;
    }
}

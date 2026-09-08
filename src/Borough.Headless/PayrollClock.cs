using System.Diagnostics;
using Borough.Core.Movement;
using Borough.Core.Quantities;

namespace Borough.Headless;

internal sealed class PayrollClock
{
    private readonly Action<PayrollStage> _mark;
    private long _begin, _part, _schedule, _wage;
    private int _scheduleCalls, _wageCalls;
    internal Reading? Last { get; private set; }

    internal PayrollClock() { _mark = Mark; }

    // Rotate through Day phases rather than repeatedly choosing the same power-of-two offsets.
    internal Action<PayrollStage>? Select(Ticks tick)
    {
        Last = null;
        return tick.Raw % 67 == 0 ? _mark : null;
    }

    // Same boundary count with empty bodies, outside Step. A calibration, not an exact correction.
    internal Reading? Calibrate()
    {
        if (Last is not { } actual) { return null; }
        _mark(PayrollStage.Begin);
        for (int i = 0; i < actual.ScheduleCalls; i++)
        { _mark(PayrollStage.ScheduleBegin); _mark(PayrollStage.ScheduleEnd); }
        for (int i = 0; i < actual.WageCalls; i++)
        { _mark(PayrollStage.WageBegin); _mark(PayrollStage.WageEnd); }
        _mark(PayrollStage.End);
        Reading? empty = Last;
        Last = actual;
        return empty;
    }

    private void Mark(PayrollStage stage)
    {
        long now = Stopwatch.GetTimestamp();
        switch (stage)
        {
            case PayrollStage.Begin:
                _begin = now; _schedule = 0; _wage = 0; _scheduleCalls = 0; _wageCalls = 0;
                break;
            case PayrollStage.ScheduleBegin: _part = now; _scheduleCalls++; break;
            case PayrollStage.ScheduleEnd: _schedule += now - _part; break;
            case PayrollStage.WageBegin: _part = now; _wageCalls++; break;
            case PayrollStage.WageEnd: _wage += now - _part; break;
            case PayrollStage.End:
                long total = now - _begin;
                double ms = 1000.0 / Stopwatch.Frequency;
                Last = new(total * ms, _schedule * ms, _wage * ms,
                    (total - _schedule - _wage) * ms, _scheduleCalls, _wageCalls);
                break;
        }
    }

    internal readonly record struct Reading(double TotalMs, double ScheduleMs, double WageMs,
        double ScanResidualMs, int ScheduleCalls, int WageCalls);
}

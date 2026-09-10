using Borough.Core.Arithmetic;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;

namespace Borough.Core.Movement;

public static class WorkSchedule
{
    public static bool Runs(World world)
    {
        if (world.Rules.Shopping.Runs || world.Rules.Care.Runs) { return true; }
        for (int kind = 1; kind <= world.Rules.BusinessKindCount; kind++)
        { if (world.Rules.BusinessKind((byte)kind).WorkDays != 0) { return true; } }
        return false;
    }

    /// <summary>
    /// What one Day of this Citizen's work is worth: the trade's posted rate, graded by Skill Tier
    /// and by the experience they have earned inside their own band.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The trade still posts ONE rate and this is what a person is worth against it.</b> Before
    /// this, every worker at a trade earned identically and a Citizen's history bought them nothing —
    /// ***a labour market with one kind of labour is a payroll rather than a market***.
    /// </para>
    /// <para>
    /// <b>Two factors and they are different things.</b> The tier percentage is a <em>category</em>
    /// premium: what a credential is worth, and it steps. The experience premium is
    /// <em>continuous with a ceiling</em> and is the design's only source of productivity growth
    /// (<c>CONTEXT.md</c> → <i>Skill Tier</i>) — without it a city of 10,000 produces the same on Day
    /// 100 and Day 5,000.
    /// </para>
    /// <para>
    /// ⚠ <b>It is NOT <c>adr/0026</c>'s posted wage.</b> That ADR has a Business post a rate and move
    /// it by its own fill rate — a price that clears. This grades a flat declared rate by who is
    /// standing in the post, and when the posted wage ships this grading is what it multiplies.
    /// </para>
    /// </remarks>
    public static long Graded(World world, int citizen, long posted)
    {
        JobRuleset jobs = world.Rules.Jobs;

        if (posted <= 0 || (!jobs.Grades && jobs.ExperiencePremiumPercent <= 0))
        {
            return posted;
        }

        long graded = IntegerMath.FloorDiv(
            posted * jobs.WagePercentOf(world.Citizens.SkillTier[citizen]), 100);

        long premium = jobs.PremiumPercent(world.Citizens.Experience[citizen]);

        return premium <= 0 ? graded : IntegerMath.FloorDiv(graded * (100 + premium), 100);
    }

    public static bool PayrollAttributionEnabled =>
#if PAYROLL_ATTRIBUTION
        true;
#else
        false;
#endif

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
#if PAYROLL_ATTRIBUTION
        => AccrueMeasured(world, tick, null);

    public static void AccrueMeasured(World world, Ticks tick, Action<PayrollStage>? observe)
#endif
    {
        if (!Runs(world)) { return; }
#if PAYROLL_ATTRIBUTION
        observe?.Invoke(PayrollStage.Begin);
#endif
        for (int citizen = 0; citizen < world.Citizens.Rows.SlotCount; citizen++)
        {
            if (!world.Citizens.Rows.IsLive(citizen)
                || (CitizenActivity)world.Citizens.Activity[citizen] != CitizenActivity.AtWork
                || CivicEngine.TooIllToWork(world, citizen)
#if !PAYROLL_ATTRIBUTION
                || !OnDuty(world, citizen, tick)
#endif
                ) { continue; }
#if PAYROLL_ATTRIBUTION
            observe?.Invoke(PayrollStage.ScheduleBegin);
            bool onDuty = OnDuty(world, citizen, tick);
            observe?.Invoke(PayrollStage.ScheduleEnd);
            if (!onDuty) { continue; }
            observe?.Invoke(PayrollStage.WageBegin);
#endif
            int job = world.Businesses.Rows.Resolve(world.Citizens.Workplace[citizen]);
            BusinessKindDefinition trade = world.Rules.BusinessKind(world.Businesses.Kind[job]);
            long length = (long)world.Rules.Jobs.ShiftLengthOf(world.Key, world.Citizens.Rows.IdAt(citizen)).Raw;
            if (length <= 0)
            {
#if PAYROLL_ATTRIBUTION
                observe?.Invoke(PayrollStage.WageEnd);
#endif
                continue;
            }
            long rate = Graded(world, citizen, trade.WagePerDay);
            long scaled = world.Citizens.WageRemainder[citizen] + rate;
            long whole = IntegerMath.FloorDiv(scaled, length);
            world.Citizens.WageRemainder[citizen] = scaled % length;
            long cap = rate * trade.PayPeriodDays;
            long earned = world.Citizens.EarnedWage[citizen] + whole;
            world.Citizens.EarnedWage[citizen] = earned > cap ? cap : earned;
#if PAYROLL_ATTRIBUTION
            observe?.Invoke(PayrollStage.WageEnd);
#endif
        }
#if PAYROLL_ATTRIBUTION
        observe?.Invoke(PayrollStage.End);
#endif
    }
}

public enum PayrollStage : byte { Begin, ScheduleBegin, ScheduleEnd, WageBegin, WageEnd, End }

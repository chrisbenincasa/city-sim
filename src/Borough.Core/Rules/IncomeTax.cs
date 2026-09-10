// SPDX-License-Identifier: MIT
namespace Borough.Core.Rules;

using Borough.Core.Arithmetic;

/// <summary>
/// The three marginal bands a Citizen's earnings for one Day are taxed against: a tax-free
/// allowance, a middle band running to <see cref="UpperThresholdPerDay"/>, and an upper band
/// above it.
/// </summary>
/// <remarks>
/// <para>
/// ⚠ <b>The thresholds are earnings per Day and never a Citizen's wealth</b>, per
/// <c>plans/0072</c> D2. A payment covering several Days is attributed across them before any
/// band is consulted.
/// </para>
/// </remarks>
public readonly record struct IncomeTaxSchedule(
    long AllowancePerDay,
    long UpperThresholdPerDay,
    int MiddleRatePercent,
    int UpperRatePercent)
{
    /// <summary>A world whose Ruleset states no <c>[income_tax]</c>. Nothing is withheld.</summary>
    public static IncomeTaxSchedule None => new(0, 0, 0, 0);

    /// <summary>Whether this schedule can ever take anything.</summary>
    public bool Levies => MiddleRatePercent > 0 || UpperRatePercent > 0;
}

/// <summary>
/// What a Citizen owes on one Day's earnings, and what an employer withholds from one payment.
/// </summary>
public static class IncomeTax
{
    /// <summary>
    /// The tax due on <paramref name="grossForDay"/> earned in a single Day.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each rate applies only to earnings inside its own band, so crossing a threshold never
    /// reprices what came before it and take-home income never falls as gross rises
    /// (<c>plans/0072</c> D2). Every division floors, which rounds in the Citizen's favour.
    /// </para>
    /// </remarks>
    public static long DueOn(long grossForDay, in IncomeTaxSchedule schedule)
    {
        if (grossForDay <= schedule.AllowancePerDay)
        {
            return 0;
        }

        long ceiling = schedule.UpperThresholdPerDay < schedule.AllowancePerDay
            ? schedule.AllowancePerDay
            : schedule.UpperThresholdPerDay;

        long middle = (grossForDay < ceiling ? grossForDay : ceiling) - schedule.AllowancePerDay;
        long due = IntegerMath.FloorDiv(middle * schedule.MiddleRatePercent, 100);

        if (grossForDay > ceiling)
        {
            due += IntegerMath.FloorDiv(
                (grossForDay - ceiling) * schedule.UpperRatePercent, 100);
        }

        return due;
    }

    /// <summary>
    /// What to withhold from a payment of <paramref name="addition"/> when
    /// <paramref name="alreadyAttributed"/> has already been paid and taxed against the same
    /// earning Day.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>This is a difference of two totals rather than a fresh assessment</b>, which is what
    /// stops the allowance being granted a second time when one Day is paid in instalments —
    /// <c>plans/0072</c> D5, and F2 is the shipped path that produces them. Splitting a payment
    /// can cost the treasury at most one floored unit across the whole Day rather than one per
    /// instalment.
    /// </para>
    /// <para>
    /// Non-negative for any schedule the loader admits, because the upper rate is at or above the
    /// middle rate (D7) and <see cref="DueOn"/> is therefore non-decreasing in gross.
    /// </para>
    /// </remarks>
    public static long WithholdingOn(
        long alreadyAttributed, long addition, in IncomeTaxSchedule schedule)
    {
        if (addition <= 0)
        {
            return 0;
        }

        long before = DueOn(alreadyAttributed, schedule);
        long after = DueOn(alreadyAttributed + addition, schedule);
        long withheld = after - before;

        if (withheld <= 0)
        {
            return 0;
        }

        return withheld > addition ? addition : withheld;
    }
}

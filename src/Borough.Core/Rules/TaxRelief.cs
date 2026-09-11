using Borough.Core.Arithmetic;

namespace Borough.Core.Rules;

/// <summary>
/// What a Business's targeted reliefs take off its profit tax — <c>plans/0072</c> D13 and D14.
/// </summary>
/// <remarks>
/// <para>
/// <b>A relief forgoes revenue and never pays anybody</b> (D10). It reduces a bill that has already
/// been worked out against the marginal bands, so a Business owing no profit tax gets nothing from
/// any relief however many it qualifies for. ***That is the line between this and a subsidy***, and
/// it is why a relief has no funding ceiling and no Money movement of its own.
/// </para>
/// <para>
/// ⚠ <b>Overlapping reliefs add and the sum is capped at the whole bill, so application order cannot
/// change the result</b> (D14). Applying two 25% reliefs one after the other would take 25% of the
/// bill and then 25% of what was left — 43.75% in total, and a different figure again if they were
/// applied the other way round when the arithmetic floors. Summing the percentages first makes the
/// order unobservable, which is the property D14 asks for rather than a convenience.
/// </para>
/// <para>
/// ⚠ <b>The cap is on the PERCENTAGE and not on the Money.</b> Capping the relieved amount at the
/// bill would let a sum above 100% pass unnoticed for as long as some other relief happened to be
/// small, and start behaving differently on the Day one of them was raised.
/// </para>
/// </remarks>
public static class TaxRelief
{
    /// <summary>The share of one Business's profit tax that its reliefs forgo, in percent.</summary>
    /// <param name="rules">The Ruleset in force, which owns the catalogue.</param>
    /// <param name="governed">The player's decisions, which win over what the Ruleset authored.</param>
    /// <param name="trade">The Business's kind.</param>
    public static int PercentFor(Ruleset rules, Entities.PolicyTable governed, byte trade)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(governed);

        int percent = 0;

        for (int policy = 0; policy < rules.Policies.Length; policy++)
        {
            ref readonly PolicyDefinition definition = ref rules.Policies[policy];

            if (definition.Tool != PolicyTool.Relief)
            {
                continue;
            }

            if (definition.Trade != TradeKind.Any && definition.Trade != trade)
            {
                continue;
            }

            percent += governed.AmountOf(policy, definition);
        }

        if (percent <= 0)
        {
            return 0;
        }

        return percent > 100 ? 100 : percent;
    }

    /// <summary>What a bill becomes once its reliefs are taken off.</summary>
    /// <remarks>
    /// ⚠ <b>The relieved amount is floored, so the rounding favours the treasury.</b> A bill of 7
    /// against a 50% relief is relieved by 3 and leaves 4. The alternative rounds a relief up and
    /// makes a 100% relief reachable by a 99% one on small bills.
    /// </remarks>
    public static long Relieved(long bill, int percent)
    {
        if (bill <= 0 || percent <= 0)
        {
            return bill > 0 ? bill : 0;
        }

        long forgone = IntegerMath.FloorDiv(bill * percent, 100);

        return bill - forgone;
    }
}

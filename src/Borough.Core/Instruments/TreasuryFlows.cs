namespace Borough.Core.Instruments;

using Borough.Core.Rules;

/// <summary>
/// Every unit of Money that crossed the treasury's edge over an interval, by the mechanism that
/// moved it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Seven magnitudes and no eighth that nets any of them.</b> <c>MoneyFlowCounter.FromTreasury</c>
/// carries the argument and <c>Borough.Headless.IncomeDump</c> prints it: a net cannot say whether a
/// city taxed nothing and paid nothing or taxed heavily and paid it all back. Within the income the
/// same argument holds one level down — a withholding, a profit tax, a <c>[[policy]]</c> and a
/// <c>[[rule]]</c> are one arrival through four levers, and a city that swapped one for another
/// would show a flat total under a changed file.
/// </para>
/// <para>
/// <b>The same figures a Census carries, in a different shape of carrier.</b> A Census keeps a
/// series over a ring and a <see cref="MoneyFlow.Peak"/> beside every <see cref="MoneyFlow.Sum"/>;
/// this is one interval's sums, small enough to add to another and to hold beside a panel.
/// ⚠ <b>A reading of either DRAINS the engines</b>, so a run has exactly one drainer:
/// <c>Borough.Headless</c> observes through a Census and <c>Borough.Shell</c> accounts through
/// <see cref="Simulation.DrainTreasuryFlows"/>, and a process doing both would see each movement
/// once, through whichever asked first.
/// </para>
/// <para>
/// ⚠ <b><see cref="Income"/> less <see cref="Expenditure"/> is the change in the treasury balance
/// only where nothing else reaches it.</b> <c>World.Dissolve</c> passes a dissolving Household's
/// balance to the treasury and no flow counts it (<c>plans/0072</c> F12), so on a Ruleset declaring
/// both <c>[[life_stage]]</c> and a money Resource the identity is short by the estates. A reader
/// holding this against a balance must print the residual rather than assume it away.
/// </para>
/// </remarks>
/// <param name="Withheld">Citizen income tax the employers took at the paydays.</param>
/// <param name="ProfitTax">Profit tax the Day-boundary collection took off Businesses' tills.</param>
/// <param name="PolicyIn">
/// What <c>PolicyEngine</c> moved into the treasury — a plain transfer and a <c>tool = "charge"</c>
/// alike, which it folds into one accumulator (<c>plans/0072</c> F28).
/// </param>
/// <param name="RuleIn">What a Bin Rule naming <c>scope = "global"</c> paid in off a premises.</param>
/// <param name="PolicyOut">What <c>PolicyEngine</c> moved out of the treasury.</param>
/// <param name="RuleOut">What a Bin Rule naming <c>scope = "global"</c> drew out.</param>
/// <param name="Subsidy">What the subsidy sweeps apportioned out against their ceilings.</param>
public readonly record struct TreasuryFlows(
    long Withheld,
    long ProfitTax,
    long PolicyIn,
    long RuleIn,
    long PolicyOut,
    long RuleOut,
    long Subsidy)
{
    /// <summary>Everything that arrived, through all four levers.</summary>
    public long Income => Withheld + ProfitTax + PolicyIn + RuleIn;

    /// <summary>Everything that left, through all three paths.</summary>
    public long Expenditure => PolicyOut + RuleOut + Subsidy;

    /// <summary>Adds another interval's flows to this one, column by column.</summary>
    /// <param name="other">The interval to add.</param>
    /// <returns>The flows over both intervals.</returns>
    public TreasuryFlows Add(in TreasuryFlows other) => new(
        Withheld + other.Withheld,
        ProfitTax + other.ProfitTax,
        PolicyIn + other.PolicyIn,
        RuleIn + other.RuleIn,
        PolicyOut + other.PolicyOut,
        RuleOut + other.RuleOut,
        Subsidy + other.Subsidy);
}

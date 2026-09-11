namespace Borough.Core.Rules;

/// <summary>
/// Which of the three things a <c>[[policy]]</c> does — <c>plans/0072</c> D10.
/// </summary>
/// <remarks>
/// <para>
/// <b>The three are not variations on one transfer and cannot be written as one.</b> A
/// <see cref="Charge"/> moves Money from a liable payer to the treasury and is exactly what a
/// <c>[[policy]]</c> already was. A <see cref="Relief"/> moves no Money anywhere: it reduces a tax
/// bill, so it has no source, no destination and no Resource, and its whole effect is visible only
/// inside the profit assessment. A <see cref="Subsidy"/> moves Money out of the treasury against a
/// ceiling, and when claims exceed that ceiling every claimant is cut in proportion — which the
/// all-or-nothing payment a transfer performs cannot express.
/// </para>
/// <para>
/// ⚠ <b>Relief forgoes revenue and a subsidy spends it, and the design keeps them apart on
/// purpose</b> (D10). A relief can never pay a Business that owes no tax; a payment beyond the tax
/// otherwise owed is expenditure and has to be funded as such. ***Neither may create Money.***
/// </para>
/// </remarks>
public enum PolicyTool : byte
{
    /// <summary>An unconditional transfer — what every <c>[[policy]]</c> was before the catalogue.</summary>
    Transfer = 0,

    /// <summary>Money from a liable payer to the treasury, priced on a quantity it is liable for.</summary>
    Charge = 1,

    /// <summary>A reduction of qualifying profit tax. Moves nothing and can pay nobody.</summary>
    Relief = 2,

    /// <summary>Money from the treasury to an eligible recipient, rationed by a daily ceiling.</summary>
    Subsidy = 3,
}

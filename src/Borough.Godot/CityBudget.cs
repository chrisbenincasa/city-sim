using Borough.Core.Instruments;
using Borough.Core.Quantities;

namespace Borough.Shell;

/// <summary>
/// The treasury's account as a player reads it: what moved over the Day just closed, what has moved
/// since the shell picked the world up, and whether the balance is explained by either.
/// </summary>
/// <remarks>
/// <para>
/// <b>Godot-free on purpose, and that is what makes the arithmetic testable.</b> The panel that
/// shows this is a <c>Control</c> and is invisible to every test in the suite; the rolling, the
/// running totals and the residual are not, and they are the half that can be wrong quietly.
/// <c>CitySave</c> and <c>CityPreparation</c> are split out of the shell for the same reason and
/// are linked into <c>Borough.Tests</c> the same way.
/// </para>
/// <para>
/// <b>A Day and not a frame, and the cadence is derived rather than chosen.</b> A payday and a
/// profit-tax collection both fall on a Day boundary and nowhere else, so a Day is the shortest
/// interval over which four of the seven columns can differ from zero. <c>IncomeDump</c> reads on
/// the same cadence for the same reason, and a row stamped at a boundary Tick INCLUDES that Tick's
/// own sweeps — so the two reports name the same Day.
/// </para>
/// <para>
/// 🔴 <b>The running totals start where the shell started and not where the world did.</b> Flows
/// are an instrument's business and are not saved, so a reloaded world arrives with a treasury
/// balance and no history of how it got one. ***Printing a running total against an assumed opening
/// of zero would report the save's whole past as this session's income*** — so the opening balance
/// is captured at construction and the identity is stated from there.
/// </para>
/// <para>
/// ⚠ <b><see cref="Residual"/> exists because the identity is not guaranteed to hold.</b>
/// <c>World.Dissolve</c> hands a dissolving Household's balance to the treasury and no flow counts
/// it (<c>plans/0072</c> F12), so on a Ruleset declaring both <c>[[life_stage]]</c> and a money
/// Resource the flows are short by the estates. A budget that assumed zero would be wrong without
/// saying so, which is the one thing the row exists to stop.
/// </para>
/// </remarks>
internal sealed class CityBudget
{
    private readonly ulong _openedAt;
    private readonly long _opening;

    private TreasuryFlows _open;
    private TreasuryFlows _closed;
    private TreasuryFlows _running;
    private ulong _closedAt;
    private int _days;

    /// <summary>Opens an account against a world the shell has just taken hold of.</summary>
    /// <param name="at">The Tick the accounting starts from.</param>
    /// <param name="opening">What the treasury held at that Tick.</param>
    internal CityBudget(Ticks at, long opening)
    {
        _openedAt = at.Raw;
        _closedAt = at.Raw;
        _opening = opening;
    }

    /// <summary>The Tick the account was opened at.</summary>
    internal ulong OpenedAt => _openedAt;

    /// <summary>What the treasury held when the account was opened.</summary>
    internal long Opening => _opening;

    /// <summary>The Tick the most recently closed Day ended on.</summary>
    internal ulong ClosedAt => _closedAt;

    /// <summary>How many whole Days have closed since the account was opened.</summary>
    internal int Days => _days;

    /// <summary>What moved over the Day just closed, or zeroes before the first one has.</summary>
    internal TreasuryFlows Yesterday => _closed;

    /// <summary>What has moved since the account was opened, the open Day included.</summary>
    internal TreasuryFlows Running => _running;

    /// <summary>
    /// Folds one Tick's flows in, closing the Day when the Tick is a Day boundary.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Called once per Tick and not once per frame.</b> The shell steps up to four Ticks in a
    /// batch, so a frame can cross a Day boundary — and a fold that happened after the batch would
    /// put part of one Day into another with nothing to notice it.
    /// </remarks>
    /// <param name="flows">What crossed the treasury's edge on this Tick.</param>
    /// <param name="at">The Tick that has just been stepped.</param>
    internal void Account(in TreasuryFlows flows, Ticks at)
    {
        _open = _open.Add(flows);
        _running = _running.Add(flows);

        if (at.Raw == _openedAt || at.Raw % (ulong)Ticks.PerDay != 0)
        {
            return;
        }

        _closed = _open;
        _closedAt = at.Raw;
        _open = default;
        _days++;
    }

    /// <summary>
    /// What the treasury holds that the flows cannot account for.
    /// </summary>
    /// <remarks>
    /// <b>Zero is the reading the budget exists to earn</b>, and a non-zero one names a path into
    /// the treasury that no column here watches. The known one is a dissolving Household's estate;
    /// there is no reason to believe it is the only one, which is why this is subtracted rather than
    /// attributed.
    /// </remarks>
    /// <param name="treasury">What the treasury holds now.</param>
    /// <returns>The balance, less what the opening balance and the flows explain.</returns>
    internal long Residual(long treasury) =>
        treasury - (_opening + _running.Income - _running.Expenditure);
}

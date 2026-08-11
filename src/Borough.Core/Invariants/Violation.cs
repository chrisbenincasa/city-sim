using System.Globalization;
using Borough.Core.Quantities;

namespace Borough.Core.Invariants;

/// <summary>
/// One invariant, found broken: which one, where, and when.
/// </summary>
/// <remarks>
/// <b>Ids and numbers, never a sentence</b> (<c>adr/0002</c>). Everything here is what the shell
/// needs to say something useful — the invariant, the rows it was about, and the Tick — and nothing
/// here is the saying of it.
/// </remarks>
/// <param name="Invariant">Which invariant.</param>
/// <param name="Tick">The Tick the run was on.</param>
/// <param name="Slot">The row the violation was found at, or <c>-1</c>.</param>
/// <param name="Other">
/// The second row involved, where there is one, or the quantity that would have broken the invariant,
/// or <c>-1</c>. <b>A <see cref="long"/> because a Bin's level is one</b> (<c>adr/0065</c>):
/// <see cref="Invariant.BinLevelIsWithinCapacity"/> reports the amount that overran, and a detail
/// field narrower than the quantity it details would be a truncating lie in exactly the case worth
/// reading.
/// </param>
public readonly record struct Violation(Invariant Invariant, Ticks Tick, int Slot, long Other)
{
    /// <summary>Nothing to report.</summary>
    public static Violation None => new(Invariant.None, default, -1, -1);

    /// <summary>Whether this is a real violation rather than the absence of one.</summary>
    public bool Broken => Invariant != Invariant.None;
}

/// <summary>
/// Thrown when an invariant is violated and the run is not collecting.
/// </summary>
/// <remarks>
/// <para>
/// <b>Throwing is what makes task 8's crash artifact worth having.</b> The artifact catches at the
/// Tick boundary and emits the Input Log plus the Tick the panic landed on; you then replay to the
/// Tick before and single-step into the failure under a debugger. A violation that were merely
/// recorded would name a Tick reached long after the world stopped making sense, and the trail from
/// there is cold.
/// </para>
/// <para>
/// <b>The message is assembled here even though the core does not own words</b> (<c>adr/0002</c>).
/// The precedent is already set by <c>StaleHandleException</c>: an exception message is a diagnostic
/// for whoever is holding the debugger, not a Readout resolved through the Ruleset for a panel, and
/// the leak vector that rule names is a method that <em>returns</em> a formatted string because a
/// panel wanted one. <see cref="Violation"/> carries the ids, so anything that wants to say this
/// differently can.
/// </para>
/// </remarks>
public sealed class InvariantViolationException : Exception
{
    /// <param name="violation">What broke.</param>
    public InvariantViolationException(Violation violation)
        : base(Describe(violation)) => Violation = violation;

    /// <summary>For the framework. Prefer the constructor that names a violation.</summary>
    public InvariantViolationException()
        : base("an invariant was violated.")
    {
    }

    /// <inheritdoc cref="InvariantViolationException()"/>
    public InvariantViolationException(string message)
        : base(message)
    {
    }

    /// <inheritdoc cref="InvariantViolationException()"/>
    public InvariantViolationException(string message, Exception inner)
        : base(message, inner)
    {
    }

    /// <summary>What broke.</summary>
    public Violation Violation { get; }

    private static string Describe(Violation violation) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"invariant {violation.Invariant} violated at Tick {violation.Tick.Raw}, "
            + $"row {violation.Slot}, row {violation.Other}.");
}

namespace Borough.Core;

/// <summary>
/// The one exception to <c>05 §4</c> lint 7 — this struct may hold references, because it is not in
/// a Tick.
/// </summary>
/// <remarks>
/// <para>
/// <b>The exception is an axis, not a list.</b> adr/0036 said the exceptions to the unmanaged rule
/// "are to be listed deliberately before the analyser ships" and named two candidates — the Ruleset
/// interpreter and the <c>Evidence</c> surface. Listing them by name would have created a place to
/// put anything inconvenient, which is the failure adr/0031 avoided by finding the axis underneath
/// its exceptions instead of granting them one at a time. The axis here is the one adr/0002 already
/// established and <c>05 §10</c> uses:
/// </para>
/// <para>
/// <b>The hot path runs inside <c>step()</c>, at the 1M target, every Tick — it allocates nothing and
/// holds no references. The cold path runs on a click, a reload or a save — it may do both, because
/// the cost is paid once by a human who is waiting anyway.</b> A type is entitled to this attribute
/// when no code path from <c>step()</c> reaches it. Both of adr/0036's candidates pass: the Ruleset
/// interpreter is loaded from data and reloaded per adr/0015, and <c>Evidence</c> (<c>02 §9</c>) is
/// assembled when a panel asks.
/// </para>
/// <para>
/// <b>The reason is required and is not documentation.</b> It is the argument, made at the point of
/// use, that the type is cold by the test above. An exception nobody had to argue for is an
/// exception nobody can audit later, and adr/0036 names this lint being waived as its own revisit
/// trigger.
/// </para>
/// <para>
/// It does not apply to a <c>class</c>: a class is already a reference and lint 7 never reached it.
/// The rule is about a struct that <em>looks</em> like flat state and is not.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class ColdPathAttribute : Attribute
{
    /// <param name="reason">
    /// Why no path from <c>step()</c> reaches this type. Read by a person, never by the simulation.
    /// </param>
    public ColdPathAttribute(string reason) => Reason = reason;

    /// <summary>
    /// The argument given at the declaration. Present because CA1019 requires an attribute argument
    /// to be readable; nothing in <c>Borough.Core</c> reads it, and it is not a Readout — adr/0002's
    /// rule is about strings a panel shows a player, and this one is addressed to whoever is
    /// auditing the exception.
    /// </summary>
    public string Reason { get; }
}

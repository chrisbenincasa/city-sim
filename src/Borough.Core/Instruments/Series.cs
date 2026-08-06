using Borough.Core.Quantities;

namespace Borough.Core.Instruments;

/// <summary>One reading of one <see cref="Metric"/>, stamped with the Tick it was taken on.</summary>
/// <remarks>
/// <b>The Tick is carried rather than implied by position.</b> A caller that inferred the Tick from
/// a sample's index would be assuming the cadence never changed and that no sample was missed, and
/// both assumptions are the caller's to make wrongly. It also makes a series self-describing when it
/// reaches a shell that did not run the session.
/// </remarks>
/// <param name="Tick">When the reading was taken.</param>
/// <param name="Value">
/// The counter's value. Widened to 64 bits because <c>02 §10</c>'s claim is about collections
/// <em>and magnitudes</em>, and a magnitude is a <c>Money</c> or a <c>Fixed</c> rather than a count.
/// </param>
public readonly record struct CensusSample(Ticks Tick, long Value);

/// <summary>
/// The answer to <c>series(metric, window)</c>: a metric's readings over a stretch of a run.
/// </summary>
/// <remarks>
/// <para>
/// <b>Cold, and allocating.</b> <c>05 §2</c> splits the core's surface on the cadence of the caller,
/// and this is called on a click or at the end of a run rather than inside <c>step()</c> — so it may
/// hold a reference and hand back a copy, which is what <see cref="ColdPathAttribute"/> records.
/// Nothing on the hot path can reach it.
/// </para>
/// <para>
/// <b><see cref="Complete"/> exists because the alternative is a wrong answer that looks right.</b>
/// The Census ring is finite by construction, so a window longer than the ring's reach can only be
/// answered over the part of it that survives. Silently returning the shorter span would let a caller
/// conclude <em>flat over the last 100,000 Ticks</em> from the last 1,000 — a claim about a run that
/// the data does not support and that nothing downstream could catch.
/// </para>
/// </remarks>
[ColdPath("series(metric, window) is 05 §2's cold API: a panel on a click, a runner at end of run.")]
public readonly struct Series
{
    internal Series(Metric metric, CensusSample[] samples, bool complete)
    {
        Metric = metric;
        Samples = samples;
        Complete = complete;
    }

    /// <summary>What this is a series of.</summary>
    public Metric Metric { get; }

    /// <summary>The readings, oldest first.</summary>
    public ReadOnlyMemory<CensusSample> Samples { get; }

    /// <summary>
    /// Whether the window asked for was covered in full.
    /// </summary>
    /// <remarks>
    /// False when the Census had already discarded readings older than the window's start. The
    /// samples returned are still correct; what is not established is that they are all of them.
    /// </remarks>
    public bool Complete { get; }

    /// <summary>The number of readings.</summary>
    public int Count => Samples.Length;
}

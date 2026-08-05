using Borough.Core.Arithmetic;
using Borough.Core.Entities;
using Borough.Core.Quantities;

namespace Borough.Core.Invariants;

/// <summary>An <see cref="InvariantTier.Staggered"/> check, over one slice of the world.</summary>
/// <param name="world">The world.</param>
/// <param name="slice">Which slice, from 0 to <paramref name="slices"/> - 1.</param>
/// <param name="slices">How many slices the world is cut into.</param>
/// <param name="report">Where to report a violation.</param>
public delegate void Sweep(World world, int slice, int slices, InvariantRegistry report);

/// <summary>An <see cref="InvariantTier.EndOfRun"/> check, over the whole world.</summary>
/// <param name="world">The world.</param>
/// <param name="report">Where to report a violation.</param>
public delegate void Walk(World world, InvariantRegistry report);

/// <summary>
/// The three registries of <c>02 §10</c>, and the one channel a violation is reported through.
/// </summary>
/// <remarks>
/// <para>
/// <b>The shape matters more than the contents at this stage.</b> Most of what <c>02 §10</c> names
/// does not exist — there are no Bins, no Trips and no parking before slice 7 — so the per-Tick tier
/// ships with its channel and no checks. What this buys is that the next mechanism has a tier to
/// register into and cannot default into a debug-only check, which is the way this class of
/// assertion has historically been lost.
/// </para>
/// <para>
/// <b>The per-Tick tier is not a registry of sweeps and the other two are.</b> <c>02 §10</c> puts
/// the cheap checks <em>at the write site</em>, which is what keeps them <c>O(changed)</c> and what
/// makes the failure point at the code that caused it. So a write site calls
/// <see cref="Require"/> directly; there is nothing to register and nothing walked.
/// </para>
/// </remarks>
public sealed class InvariantRegistry
{
    /// <summary>The default number of Ticks the staggered sweep takes to cover the world.</summary>
    private const int DefaultSlices = 64;

    private readonly List<Sweep> _staggered = [];
    private readonly List<Walk> _endOfRun = [];
    private readonly List<Violation> _collected = [];

    private int _slices = DefaultSlices;

    /// <summary>
    /// Whether a violation is recorded and the run continues, rather than thrown.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Throwing is the default, and it is what composes with the crash artifact.</b> A run that
    /// stops at the Tick the world stopped making sense can be replayed to the Tick before and
    /// single-stepped into; a run that carried on has a cold trail and a pile of violations that may
    /// all be consequences of the first.
    /// </para>
    /// <para>
    /// <b>Collecting exists for the balance run</b>, which is millions of Ticks long and would rather
    /// finish and report than die on the first bad Household. A run that sets this is asking a
    /// different question — <em>what is wrong with this city</em> rather than <em>where did this go
    /// wrong</em> — and the answers it gets after the first violation are worth less, in the way
    /// compiler errors after the first are.
    /// </para>
    /// <para>
    /// <b>A runtime switch rather than a build configuration</b>, for the reason <c>02 §10</c> gives
    /// about the tiers themselves: the runs that surface these bugs are release builds.
    /// </para>
    /// </remarks>
    public bool Collect { get; set; }

    /// <summary>
    /// How many Ticks the staggered sweep takes to cover the world.
    /// </summary>
    /// <remarks>
    /// <b>Not a Ruleset value, and not a <c>const</c> either.</b> <c>05 §4</c>'s test settles which
    /// it is: invariants only read, so no setting of this changes the State Hash, so by the corpus's
    /// own rule it is an optimisation rather than a design change — a performance knob, not a number
    /// a designer would ever want. There is a test asserting the hash is identical across two very
    /// different settings, because that argument is worth checking rather than asserting.
    /// </remarks>
    public int Slices
    {
        get => _slices;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            _slices = value;
        }
    }

    /// <summary>The Tick a violation will be stamped with. Set by the Simulation each Tick.</summary>
    public Ticks Tick { get; set; }

    /// <summary>What was found, when <see cref="Collect"/> is set. Empty otherwise.</summary>
    public IReadOnlyList<Violation> Collected => _collected;

    /// <summary>Registers a check in a tier.</summary>
    /// <exception cref="ArgumentException"><see cref="InvariantTier.PerTick"/>, which has no registry.</exception>
    public void Register(InvariantTier tier, Sweep check)
    {
        ArgumentNullException.ThrowIfNull(check);

        if (tier != InvariantTier.Staggered)
        {
            throw new ArgumentException(
                "only the staggered tier sweeps slices. Per-Tick checks are made at the write site, "
                + "and end-of-run checks walk the whole world.",
                nameof(tier));
        }

        _staggered.Add(check);
    }

    /// <inheritdoc cref="Register(InvariantTier, Sweep)"/>
    public void Register(InvariantTier tier, Walk check)
    {
        ArgumentNullException.ThrowIfNull(check);

        if (tier != InvariantTier.EndOfRun)
        {
            throw new ArgumentException(
                "only the end-of-run tier walks the whole world.", nameof(tier));
        }

        _endOfRun.Add(check);
    }

    /// <summary>
    /// The per-Tick tier: a write site asserting something about the mutation it is making.
    /// </summary>
    /// <param name="held">The thing that must be true.</param>
    /// <param name="invariant">Which invariant, if it is not.</param>
    /// <param name="slot">The row being written.</param>
    /// <param name="other">A second row involved, where there is one.</param>
    public void Require(bool held, Invariant invariant, int slot = -1, int other = -1)
    {
        if (!held)
        {
            Report(invariant, slot, other);
        }
    }

    /// <summary>Records or throws, according to <see cref="Collect"/>.</summary>
    /// <exception cref="InvariantViolationException">A violation, and the run is not collecting.</exception>
    public void Report(Invariant invariant, int slot = -1, int other = -1)
    {
        var violation = new Violation(invariant, Tick, slot, other);

        if (!Collect)
        {
            throw new InvariantViolationException(violation);
        }

        _collected.Add(violation);
    }

    /// <summary>
    /// Runs the slice of the staggered tier that belongs to <paramref name="tick"/>.
    /// </summary>
    /// <remarks>
    /// <b>The slice is a function of the Tick and nothing else</b>, so two runs of one log check the
    /// same rows on the same Ticks. An invariant suite whose coverage varied between runs would be a
    /// suite that failed on one machine and passed on another, which is the failure mode that makes
    /// people stop believing tests.
    /// </remarks>
    public void RunStaggered(World world, Ticks tick)
    {
        ArgumentNullException.ThrowIfNull(world);

        Tick = tick;
        int slice = (int)(tick.Raw % (ulong)_slices);

        foreach (Sweep check in _staggered)
        {
            check(world, slice, _slices, this);
        }
    }

    /// <summary>Runs the whole end-of-run tier.</summary>
    public void RunEndOfRun(World world, Ticks tick)
    {
        ArgumentNullException.ThrowIfNull(world);

        Tick = tick;

        foreach (Walk check in _endOfRun)
        {
            check(world, this);
        }
    }

    /// <summary>
    /// The half-open range of rows a slice covers, for a table of <paramref name="count"/> slots.
    /// </summary>
    /// <remarks>
    /// Every row lands in exactly one slice and no row is missed, including when the table is smaller
    /// than the slice count — the arithmetic is a partition rather than a stride, so a table of three
    /// rows cut into sixty-four slices gives three non-empty slices rather than three rows checked
    /// sixty-four times or, worse, a row that no slice covers.
    /// </remarks>
    public static (int From, int To) Range(int slice, int slices, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(slices);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        int from = (int)IntegerMath.FloorDiv((long)count * slice, slices);
        int to = (int)IntegerMath.FloorDiv((long)count * (slice + 1), slices);

        return (from, to);
    }
}

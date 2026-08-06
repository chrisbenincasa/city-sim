using Borough.Core.Arithmetic;

namespace S2.Routing.Matrix;

/// <summary>
/// The sun arc's five named phases. <c>02 §1.2</c> and <c>01 §7</c> name them and size none of them.
/// </summary>
/// <remarks>
/// <para>
/// <b>R1 does not choose the widths and must not be read as having done so.</b>
/// <c>plans/0010</c> files the phase widths as decision 5a — <i>"the corpus names and never sizes"</i>
/// them, so no peaking factor exists anywhere and every load figure in the corpus is a Day-average of
/// a Day that has a rush hour. What R1 needs from the arc is not a width but an <i>ordering</i>: five
/// distinct congestion states, one of which is directionally lopsided in one sense and another in the
/// other. The widths decide how much load lands in a phase; they do not decide what a phase's matrix
/// looks like, which is all this file supplies.
/// </para>
/// <para>
/// <b>What each phase carries is an intensity and a direction, and only the direction is structural.</b>
/// Intensity scales the whole congestion field; direction says which way the lopsidedness points, as a
/// signed multiplier on the swept imbalance. Morning is inbound and evening is outbound by definition
/// — that is what makes them peaks rather than busy periods — and the two must be opposite in sign or
/// a Day-average matrix would not cancel them, which is precisely the effect R1 exists to show.
/// </para>
/// </remarks>
internal enum Phase
{
    Dawn,
    MorningPeak,
    Midday,
    EveningPeak,
    Night,
}

/// <summary>
/// A phase's congestion state: how busy, and which way the lopsidedness points.
/// </summary>
/// <param name="Intensity">
/// Q16.16 multiplier on the base <c>volume / capacity</c>. Relative, never absolute — the absolute
/// level is <see cref="CongestionParameters.BaseVolumeCapacity"/>, which is itself swept.
/// </param>
/// <param name="Direction">
/// Q16.16 in <c>[-1, +1]</c>. <c>+1</c> is fully inbound, <c>-1</c> fully outbound, <c>0</c> balanced.
/// Multiplied by the swept imbalance to give the phase's effective lopsidedness.
/// </param>
internal readonly record struct PhaseProfile(int Intensity, int Direction)
{
    // Q16.16 literals rather than divisions of Fixed.One, because BOR0203 is loaded on this
    // directory and is right to be: a constant-folded division is still a division whose rounding
    // nobody stated. The decimal each one spells is in the comment beside it.
    private const int ThirtyFivePercent = 22_938;   // 0.35
    private const int TwentyFivePercent = 16_384;   // 0.25
    private const int FiftyFivePercent = 36_045;    // 0.55
    private const int NinetyFivePercent = 62_259;   // 0.95
    private const int TwentyPercent = 13_107;       // 0.20

    /// <summary>
    /// The five profiles. <b>Ratios between phases, not sizes of them</b> — see <see cref="Phase"/>.
    /// </summary>
    public static PhaseProfile Of(Phase phase) => phase switch
    {
        Phase.Dawn => new(Intensity: ThirtyFivePercent, Direction: TwentyFivePercent),
        Phase.MorningPeak => new(Intensity: Fixed.One, Direction: Fixed.One),
        Phase.Midday => new(Intensity: FiftyFivePercent, Direction: 0),
        Phase.EveningPeak => new(Intensity: NinetyFivePercent, Direction: -Fixed.One),
        Phase.Night => new(Intensity: TwentyPercent, Direction: 0),
        _ => new(Intensity: Fixed.One, Direction: 0),
    };

    /// <summary>Every phase, in arc order. Iterated for the per-phase resolution rung.</summary>
    public static Phase[] All =>
        [Phase.Dawn, Phase.MorningPeak, Phase.Midday, Phase.EveningPeak, Phase.Night];
}

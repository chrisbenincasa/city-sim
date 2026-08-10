using Borough.Core.Arithmetic;

namespace Borough.Core.Space;

/// <summary>
/// A symmetric 1-D integer kernel, applied once per axis. Bounded support, stated normalisation.
/// </summary>
/// <remarks>
/// <para>
/// <b>Separable: two 1-D passes rather than one 2-D pass</b> (<c>02 §2.4</c>, <c>05 §9</c>). At radius
/// <em>r</em> that is <c>2(2r+1)</c> multiply-adds per Cell instead of <c>(2r+1)²</c> — 34 against 289
/// at <em>r</em> = 8. It is exact rather than an approximation: the 2-D kernel <em>is</em> the outer
/// product of this one with itself, by construction, so nothing is being traded for the speed.
/// </para>
/// <para>
/// <b>Bounded support is what makes incremental re-diffusion legal</b> (<c>adr/0034 §3</c>). An output
/// Cell reads no source further than <see cref="Radius"/> away, so a changed source can only move
/// output within that halo — which is the difference between an <em>exact</em> incremental scheme and
/// an approximate one, and therefore between saves that agree and saves that diverge for reasons
/// nobody could find.
/// </para>
/// </remarks>
public sealed class SeparableKernel
{
    private readonly int[] _weights;

    private SeparableKernel(int[] weights, Cells radius, int gain)
    {
        _weights = weights;
        Radius = radius;
        Gain = gain;
        Scale = gain * gain;
    }

    /// <summary>The furthest Cell the kernel reaches. The halo radius of the incremental scheme.</summary>
    public Cells Radius { get; }

    /// <summary>The sum of the 1-D weights — what one pass multiplies the field by.</summary>
    public int Gain { get; }

    /// <summary>
    /// What <em>two</em> passes multiply the field by, and the divisor <see cref="Normalise"/> states.
    /// </summary>
    /// <remarks>
    /// <b>A diffused Layer is stored pre-normalised, at this scale.</b> See <see cref="Normalise"/> for
    /// why the division is not folded into the passes.
    /// </remarks>
    public int Scale { get; }

    /// <summary>
    /// The largest source magnitude that survives two passes inside an <c>i32</c>.
    /// </summary>
    /// <remarks>
    /// <b>The Layer's real ceiling is the kernel's, not the integer's</b> — roughly 327,000 at radius
    /// 8, against <see cref="int.MaxValue"/>. It lives here rather than at the invariant that checks it
    /// because it is a property of the kernel: change the radius and the ceiling moves with it, and a
    /// bound restated at the check site would go stale silently the first time somebody did.
    /// </remarks>
    public int SourceCeiling => IntegerMath.FloorDiv(int.MaxValue, Scale);

    /// <summary>The weights, from offset <c>−Radius</c> to <c>+Radius</c>.</summary>
    public ReadOnlySpan<int> Weights => _weights;

    /// <summary>
    /// A triangular kernel of the given radius: weights <c>r+1−|i|</c>, gain <c>(r+1)²</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// At <em>r</em> = 8 the weights are <c>1 2 3 4 5 6 7 8 9 8 7 6 5 4 3 2 1</c> and the gain is 81.
    /// A tent convolved with itself is the 2-D pyramid, which is the cheapest bounded kernel with a
    /// monotone falloff and no ringing — a box has a flat plateau and a hard edge, and an edge in a
    /// pollution field reads to a player as a wall somebody authored.
    /// </para>
    /// <para>
    /// <b>The shape is unratified.</b> <c>plans/0009</c> files the kernel as owed and requires it be
    /// recorded as such. What is defensible today is that it is bounded, separable, integer, monotone,
    /// and normalises without a hidden rounding step; what is <em>not</em> established is that a tent
    /// is the right falloff for a plume, which is an empirical question about dispersion and not one
    /// this slice can settle. See <see cref="LayerKernels"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The radius is negative.</exception>
    public static SeparableKernel Tent(Cells radius)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(radius.Raw, nameof(radius));

        int r = radius.Raw;
        int[] weights = new int[(2 * r) + 1];

        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] = r + 1 - IntegerMath.Abs(i - r);
        }

        return new SeparableKernel(weights, radius, (r + 1) * (r + 1));
    }

    /// <summary>The weight at an offset in <c>[−Radius, +Radius]</c>.</summary>
    public int Weight(int offset) => _weights[offset + Radius.Raw];

    /// <summary>
    /// Divides a two-pass accumulation by <see cref="Scale"/>, rounding half away from zero.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the only rounding in the whole scheme, and it lives at the point of use rather than
    /// inside the passes. That placement is forced, not a preference.</b> <c>plans/0009</c> asks for
    /// <em>superposition exact — twenty sources diffused together equal the sum of twenty diffused
    /// separately, bit for bit</em>, and rounding inside a pass destroys it: <c>RoundDiv(41,81)</c> is
    /// 1 and so is <c>RoundDiv(82,81)</c>, so two sources of 41 diffuse to 2 apart and to 1 together.
    /// Integer division is not linear, and superposition is a statement that the operator <em>is</em>.
    /// </para>
    /// <para>
    /// <b>What follows from it is the whole reason the property was asked for.</b> Exact superposition
    /// is what makes incremental re-diffusion exact rather than approximate (<c>adr/0034 §3</c>), and
    /// an approximate incremental scheme is a relaxation wearing a convolution's name — one changed
    /// source perturbs the whole field and saves diverge. The rounding had to go somewhere; the only
    /// place it costs nothing is after the last addition.
    /// </para>
    /// <para>
    /// <b>The cost is that a stored Layer is in kernel units.</b> Every reader normalises, which is one
    /// division at a read site rather than one per Cell per pass — cheaper as well as exact — and it is
    /// stated here rather than left for each reader to guess at.
    /// </para>
    /// </remarks>
    public int Normalise(long accumulated) => (int)IntegerMath.RoundDiv(accumulated, Scale);
}

/// <summary>
/// The kernel each diffused Layer is convolved with, authored in metres.
/// </summary>
/// <remarks>
/// <para>
/// <b>Stated as a distance before it is stated as a Cell count</b>, which <c>plans/0009</c> requires
/// and <c>02 §2.5</c> question 2 is: <em>what is its actionable range in metres, and can you defend the
/// figure from reality?</em> A radius that only ever existed as a Cell count has no answer, and
/// <em>author in domain units, never in utility units</em> applies to the machinery as much as to the
/// balance constants.
/// </para>
/// <para>
/// <b>A world-creation constant</b> — <c>adr/0015</c>'s frozen-per-world category, which
/// <c>adr/0034</c> already added the Cell to. It lives in the Ruleset like everything else and is
/// <em>read</em> from it; what it may not do is change during a run. It earns that by meeting the
/// category's stated test — <em>was existing simulation state recorded in units of the constant?</em>
/// — because a Cell is <b>stored pre-normalised in kernel units</b> and divided by this kernel's
/// <see cref="SeparableKernel.Scale"/> at the point of use. Change the radius and every Cell not
/// re-diffused is read at the wrong scale.
/// </para>
/// <para>
/// <b>The diffusion cadence is not in this category, and the difference is instructive</b>
/// (<c>adr/0044</c>). It is hash-bearing too, but no stored state is denominated in it, so a designer
/// may move it mid-run and lose nothing — the dirty set still holds the Cells whose sources changed.
/// Hash-bearing means <em>not the profiler's to move</em>; it does not by itself mean frozen.
/// </para>
/// </remarks>
public static class LayerKernels
{
    /// <summary>
    /// Industrial pollution's kernel: a tent reaching the Ruleset's declared radius.
    /// </summary>
    /// <remarks>
    /// <b>The metres used to be a <c>const</c> here, and that was a defect rather than a
    /// shortcut.</b> <c>adr/0015</c>'s world-creation category freezes a number <em>per world</em>;
    /// it does not move it into the binary — its own words are that these constants <em>"live in the
    /// Ruleset like everything else and are read from it"</em>. The value now lives in
    /// <see cref="LayerConstants"/>, is declared in the Ruleset file, and is built into a kernel once
    /// per world. Slice 8 task 3 found it, and it mattered rather than being tidy: the world-creation
    /// refusal has nothing to refuse while a designer has no file in which to change the number.
    /// </remarks>
    public static SeparableKernel IndustrialPollution(LayerConstants constants) =>
        SeparableKernel.Tent(CellGrid.FromMetres(constants.IndustrialPollutionMetres));
}

using Borough.Core.Arithmetic;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Xunit.Abstractions;

namespace Borough.Tests.Space;

/// <summary>
/// How long a fully-sealed Cell of each terrain type takes to reach bare ground.
/// </summary>
/// <remarks>
/// <para>
/// <b>This exists because the number a designer reads and the number the file states are not the
/// same number, and until this ran nobody knew the ratio between them.</b>
/// <c>rulesets/varied.toml</c> authors a <c>sealing_decay_tau</c>, which is a time constant;
/// <c>CONTEXT.md</c> → Sealing states an intent — <em>"floodplain may recover over hundreds of
/// Days"</em> — which is a duration. ***A time constant is not a duration***, and
/// <c>plans/0002</c> §D1 quotes a multiplier of about <b>7.4×</b> between them. That figure was
/// derived on paper from <c>tau × ln(1024) + tau ÷ 2</c>; this is the machine that checks it.
/// </para>
/// <para>
/// ⚠ <b>It is an instrument and asserts almost nothing.</b> Its one assertion is that every
/// non-zero tau terminates — which is the property that was <em>false</em> until milestone 24 task
/// 4 floored the step, and the reason this class exists rather than a comment. The figures it
/// prints are what a document may quote, and they move the moment a tau is retuned.
/// </para>
/// <para>
/// 🔴 <b>It ratifies nothing.</b> <c>plans/0002</c> §D1 names a long run on
/// <c>rulesets/varied.toml</c> as the ratifier, and its quantity is Days from a Cell's last
/// demolition to bare ground in a city that is actually building and condemning. ***A Cell sealed
/// by hand and left alone is the decay curve with no city on it***, which is worth knowing and is
/// not the reading that was asked for.
/// </para>
/// </remarks>
[Trait(Tier.Key, Tier.Instrument)]
public sealed class SealingRecoveryMeasurementTests(ITestOutputHelper output)
{
    /// <summary>The five taus <c>rulesets/varied.toml</c> states, in the order the types are numbered.</summary>
    private static readonly (TerrainKind Kind, int Tau)[] Shipped =
    [
        (TerrainKind.Ordinary, 96),
        (TerrainKind.Rock, 0),
        (TerrainKind.Floodplain, 48),
        (TerrainKind.Marsh, 64),
        (TerrainKind.ThinSoil, 160),
    ];

    [Fact]
    public void A_fully_sealed_Cell_reaches_bare_ground_and_this_is_how_long_it_takes()
    {
        output.WriteLine(
            $"A Cell sealed to all {CellGrid.TilesInCell} Tiles, decayed on its own cadence of one Day.");
        output.WriteLine("");
        output.WriteLine("type          tau    Days to bare   ratio   half gone at");
        output.WriteLine("------------------------------------------------------------");

        foreach ((TerrainKind kind, int tau) in Shipped)
        {
            if (tau == 0)
            {
                output.WriteLine($"{kind,-12}  {tau,4}          never       -              -");
                continue;
            }

            MapLayers layers = new(LayerRuleset.Default);
            Cells east = new(3);
            Cells north = new(4);
            TerrainRuleset terrain = Uniform(tau);

            layers.Seal(east, north, CellGrid.TilesInCell);

            int days = 0;
            int half = 0;

            while (layers.Sealing(east, north) > 0)
            {
                layers.DecaySealing(terrain);
                days++;

                if (half == 0 && layers.Sealing(east, north) <= CellGrid.TilesInCell / 2)
                {
                    half = days;
                }

                Assert.True(days < 1_000_000, $"{kind} at tau {tau} did not reach bare ground.");
            }

            int ratio = IntegerMath.RoundDiv(days * 10, tau);

            output.WriteLine(
                $"{kind,-12}  {tau,4}      {days,6}      {ratio / 10}.{ratio % 10}       {half,6}");
        }
    }

    /// <summary>
    /// One tau on every type. ⚠ <b>The type column above is a LABEL for which shipped type states
    /// that tau, and not a property of the Cell being measured</b> — the Cell is
    /// <see cref="TerrainKind.Ordinary"/>, because a bare <see cref="MapLayers"/> lays no terrain and
    /// zero is that enum's first member. ***The curve is a function of the tau alone***, so keying
    /// the Ruleset by type here would measure the lookup rather than the decay.
    /// </summary>
    private static TerrainRuleset Uniform(int tau) => TerrainRuleset.From(
        Fixed.One, Fixed.One, Fixed.One, Fixed.One, Fixed.One,
        ordinaryDecayTau: tau,
        rockDecayTau: tau,
        floodplainDecayTau: tau,
        marshDecayTau: tau,
        thinSoilDecayTau: tau);
}

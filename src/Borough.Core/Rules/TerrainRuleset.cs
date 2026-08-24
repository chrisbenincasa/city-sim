using Borough.Core.Arithmetic;
using Borough.Core.Space;

namespace Borough.Core.Rules;

/// <summary>
/// What the ground is worth, per terrain type — the <c>[[terrain]]</c> table.
/// </summary>
/// <remarks>
/// <para>
/// <c>adr/0157</c>. <b>One Base Fertility per <see cref="TerrainKind"/></b>. Base Fertility is
/// <b>Ruleset data keyed by terrain type and never stored per Cell</b> (<c>adr/0154</c>) — the Cell
/// stores the <em>type</em>, and this is the table it keys into.
/// </para>
/// <para>
/// <b>Q16.16, with <see cref="Fixed.One"/> meaning fully fertile</b> (<c>adr/0155</c>), so Fertility
/// composes as a <b>proportion</b> and <c>adr/0022</c>'s <em>"41% — ground sealed 12%"</em> panel
/// falls out with no conversion. <b>Authored as an integer percent</b>, because <c>adr/0048</c>
/// refuses an unquoted decimal anywhere on the path into the simulation.
/// </para>
/// <para>
/// 🔴 <b>The five values were chosen against NO consumer</b> — <c>MapLayers.Fertility</c> throws, and
/// no milestone in <c>06</c> builds a farm — so they are a stated starting point with a named
/// ratifier and <em>not</em> a balance. <c>plans/0002</c> §D1 carries one row for the five, and
/// <b>the first farm reopens every one</b>.
/// </para>
/// <para>
/// ⚠ <b>The Sealing decay rate is NOT here yet, and that is <c>adr/0052</c> working rather than an
/// omission.</b> It is the second value keyed by terrain type (<c>CONTEXT.md</c> → Terrain), but
/// <c>plans/0042</c> decision 5 — what ratifies its cadence and rate — is <b>open</b>, and a
/// hash-bearing number is chosen with a named ratifier or not at all. It arrives with task 4,
/// alongside the per-type replacement for <c>[layers] sealing_decay_tau</c>, a single global today.
/// </para>
/// </remarks>
public readonly record struct TerrainRuleset(
    bool Stated,
    int Ordinary,
    int Rock,
    int Floodplain,
    int Marsh,
    int ThinSoil,
    int OrdinaryDecayTau,
    int RockDecayTau,
    int FloodplainDecayTau,
    int MarshDecayTau,
    int ThinSoilDecayTau)
{
    /// <summary>
    /// How many terrain types a Ruleset states, which is all of them.
    /// </summary>
    /// <remarks>
    /// <b>A file that states <c>[[terrain]]</c> states every type</b>, and a missing one is refused
    /// at load. The alternative is worse than it looks: the generator places all five from the
    /// <c>WorldKey</c> regardless of what the Ruleset says, so an unstated type would be ground the
    /// world contains and the file prices at <b>zero</b> — a silent sterile band rather than an
    /// error. <c>adr/0048</c>'s rule that a Ruleset is validated where it is parsed.
    /// </remarks>
    public const int Kinds = 5;

    /// <summary>The five Base Fertilities and the five decay rates, from a file that states them.</summary>
    public static TerrainRuleset From(
        int ordinary,
        int rock,
        int floodplain,
        int marsh,
        int thinSoil,
        int ordinaryDecayTau,
        int rockDecayTau,
        int floodplainDecayTau,
        int marshDecayTau,
        int thinSoilDecayTau) =>
        new(
            true,
            ordinary,
            rock,
            floodplain,
            marsh,
            thinSoil,
            ordinaryDecayTau,
            rockDecayTau,
            floodplainDecayTau,
            marshDecayTau,
            thinSoilDecayTau);

    /// <summary>
    /// A Ruleset that says nothing about what its ground is worth.
    /// </summary>
    /// <remarks>
    /// <b>Absence is the unset spelling</b>, on <see cref="ParkingRuleset.None"/>'s rule: every Base
    /// Fertility in range means something, <b>including zero</b>, so no value inside the range can do
    /// duty as <em>unset</em>. ⚠ <b>The terrain column is generated either way</b> — every world has
    /// ground (<c>adr/0021</c>) — so this is the Ruleset declining to price its ground, and never a
    /// world without terrain in it.
    /// </remarks>
    public static TerrainRuleset None => default;

    /// <summary>
    /// Base Fertility for a type, Q16.16 — <b>Fertility's ceiling</b>, before Sealing and pollution.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not a terrain type.</exception>
    /// <exception cref="InvalidOperationException">This Ruleset states no <c>[[terrain]]</c>.</exception>
    public int BaseFertility(TerrainKind kind)
    {
        if (!Stated)
        {
            throw new InvalidOperationException(
                "this Ruleset states no [[terrain]], so it has no Base Fertility to look up. A "
                + "generated world still has terrain -- the type column is written from the WorldKey "
                + "either way -- but what that ground is WORTH is Ruleset data (adr/0154), and this "
                + "file declines to say. adr/0157.");
        }

        return kind switch
        {
            TerrainKind.Ordinary => Ordinary,
            TerrainKind.Rock => Rock,
            TerrainKind.Floodplain => Floodplain,
            TerrainKind.Marsh => Marsh,
            TerrainKind.ThinSoil => ThinSoil,
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    /// <summary>
    /// How many scheduled updates this ground takes to shed its Sealing. <b>Zero means never.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>adr/0044</c>, milestone 24 task 4. <b>Keyed by terrain type, which is what <c>02 §2.4</c> and
    /// <c>CONTEXT.md</c> → Sealing have always said</b> — *"rock may never recover, floodplain may
    /// recover over hundreds of Days"* — and what the single global <c>[layers] sealing_decay_tau</c>
    /// could not express. That key is <b>gone</b>, not defaulted: a rate keyed by terrain type belongs
    /// beside the terrain type.
    /// </para>
    /// <para>
    /// ⚠ <b>Zero is a real answer here and not an unset one.</b> It is <c>rock</c>'s answer, and it is
    /// the state every shipped Ruleset was in before this task. A file stating <c>[[terrain]]</c>
    /// states all five, so a type with no rate is refused rather than silently permanent.
    /// </para>
    /// <para>
    /// <b>It counts UPDATES and not Ticks or Days</b>, so it is read against
    /// <c>[layers] sealing_decay_period</c> and means nothing alone. ***A duration is the product of
    /// the two***, which is why neither can be quoted without the other.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not a terrain type.</exception>
    /// <exception cref="InvalidOperationException">This Ruleset states no <c>[[terrain]]</c>.</exception>
    public int SealingDecayTau(TerrainKind kind)
    {
        if (!Stated)
        {
            throw new InvalidOperationException(
                "this Ruleset states no [[terrain]], so it has no Sealing decay rate to look up. A "
                + "generated world still has terrain, but how fast its ground RECOVERS is keyed by "
                + "terrain type (02 section 2.4), so a file that declines to describe its ground "
                + "cannot say how fast that ground comes back. Its Sealing only accumulates, which is "
                + "the state every shipped Ruleset was in before milestone 24 task 4. adr/0044.");
        }

        return kind switch
        {
            TerrainKind.Ordinary => OrdinaryDecayTau,
            TerrainKind.Rock => RockDecayTau,
            TerrainKind.Floodplain => FloodplainDecayTau,
            TerrainKind.Marsh => MarshDecayTau,
            TerrainKind.ThinSoil => ThinSoilDecayTau,
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}

using Borough.Core.Quantities;

namespace Borough.Core.Rules;

/// <summary>
/// How often the world floods, and how long a flood takes — <c>[disasters]</c>.
/// </summary>
/// <remarks>
/// <para>
/// <c>CONTEXT.md</c> → Disaster, <c>01 §5.2</c>, <c>01 §5.3</c>. <b>Three durations and no
/// magnitude at all</b>, which is <c>01 §5.2</c>'s own sentence taken literally: <em>"No severity
/// constant is authored anywhere; the only constants are a frequency interval and a spread rate,
/// both durations, both scale-free."</em> How far a flood reaches and what it does when it gets
/// there is derived from the Hazard Region's depths and from where the world seeded it.
/// </para>
/// <para>
/// ⚠ <b>Required to sit beside <c>[water] flood_level_percent</c> and refused without it.</b> A
/// frequency for an event with nowhere to happen is a key that reads as a decision and derives
/// nothing (<c>adr/0123</c>), and the Hazard Region is generated from that key alone.
/// </para>
/// <para>
/// 🔴 <b>All three values are PROVISIONAL and no <c>plans/0002</c> §D row stands behind them</b> —
/// the amnesty suspends <c>adr/0052</c>. What would settle them is a person watching a coast flood
/// and saying whether it felt like weather or like a punishment.
/// </para>
/// </remarks>
/// <param name="Stated">Whether the Ruleset declares the table at all.</param>
/// <param name="FloodEveryDays">The Acts of God interval — how often a flood begins.</param>
/// <param name="FloodRisesOverDays">How long the surge takes to reach its peak.</param>
/// <param name="FloodRecedesOverDays">How long the water takes to leave once it has peaked.</param>
public readonly record struct DisasterRuleset(
    bool Stated,
    int FloodEveryDays,
    int FloodRisesOverDays,
    int FloodRecedesOverDays)
{
    /// <summary>A world in which nothing ever happens to the ground. Every shipped file but one.</summary>
    public static DisasterRuleset None => default;

    /// <summary>The three durations, refused where one of them would describe half a mechanism.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A duration is not positive.</exception>
    public static DisasterRuleset From(int everyDays, int risesOverDays, int recedesOverDays)
    {
        // All three at once, and none of them defaulted. A flood with no interval never fires, a
        // flood with no rise is a step function that inundates its whole reach on one Tick, and a
        // flood with no recession never leaves -- three different worlds, none of which is what an
        // author omitting one key would mean. adr/0123: the absence of the TABLE is the spelling for
        // a world with no disasters, and a partial table is not a second spelling of it.
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(everyDays);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(risesOverDays);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(recedesOverDays);

        return new DisasterRuleset(true, everyDays, risesOverDays, recedesOverDays);
    }

    /// <summary>The interval in Ticks, which is the unit the engine schedules on.</summary>
    /// <remarks>
    /// <b>The designer authors Days and the engine never sees one</b>, which is <c>adr/0048</c>'s
    /// division and <c>adr/0059</c>'s <em>state the duration, derive the rate</em>. Zero when the
    /// table is absent, and the engine reads zero as <em>never</em>.
    /// </remarks>
    public int FloodEveryTicks => Stated ? FloodEveryDays * Ticks.PerDay : 0;

    /// <summary>How many Ticks the surge spends rising.</summary>
    public int FloodRisesOverTicks => Stated ? FloodRisesOverDays * Ticks.PerDay : 0;

    /// <summary>How many Ticks the water spends leaving.</summary>
    public int FloodRecedesOverTicks => Stated ? FloodRecedesOverDays * Ticks.PerDay : 0;
}

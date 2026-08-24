namespace Borough.Core.Rules;

/// <summary>
/// Where the sea stands — the <c>[water]</c> table.
/// </summary>
/// <remarks>
/// <para>
/// <c>adr/0159</c>, milestone 24 task 6a. <b>One key, because a generator only needs to be told how
/// high the water is.</b> Everything else about a Water Body is derived: its extent is the ground
/// below this level, its identity is a connected component of that extent, and its outflow is where
/// it spills. ⚠ <b>There is no water-coverage key and that absence is the decision</b> — a share of
/// the map is an <em>outcome</em>, and authoring it would make the generator solve for a number
/// instead of laying a world.
/// </para>
/// <para>
/// <b>Absent means the world has no water at all</b>, reached by omitting the table rather than by a
/// defaulted key — <c>adr/0098</c>'s spelling for <c>[households]</c>, and <c>[traffic]</c>'s. ⚠ <b>A
/// world with no water is a legitimate world</b>, not a degraded one: it is an inland city, and ten of
/// the shipped Rulesets are one.
/// </para>
/// <para>
/// 🔴 <b>The value is UNRATIFIED and <c>plans/0002</c> §D1 carries its row.</b> Water has no consumer
/// until the <c>− w₅·shoreline</c> term ships at task 7 — roads do not avoid water (<c>adr/0021</c>),
/// so a coastline changes no Trip, no Lot and no Bin today. ***A number whose only effect is on a
/// mechanism nobody has built cannot be ratified, and saying so is the row's whole content.***
/// </para>
/// </remarks>
public readonly record struct WaterRuleset(bool Stated, int SeaLevelPercent)
{
    /// <summary>A Ruleset whose world has no water.</summary>
    /// <remarks>
    /// <b>Absence is the unset spelling</b>, on <see cref="TerrainRuleset.None"/>'s rule. Unlike
    /// terrain — which every world has whatever the file says — water is genuinely optional, so this
    /// is a world without a coastline and not a file declining to describe one.
    /// </remarks>
    public static WaterRuleset None => default;

    /// <summary>A Ruleset that states where the sea stands.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="seaLevelPercent"/> is not between 1 and 99.
    /// </exception>
    public static WaterRuleset From(int seaLevelPercent)
    {
        // Refused at BOTH ends, and neither is a range check for its own sake. 0 would put the sea at
        // the lowest Cell on the map, which is a world with no water -- a second spelling of the
        // absent table, and a designer who wrote it would mean something by it that the generator
        // could not hear. 100 would put every Cell under water, which is not a city.
        if (seaLevelPercent is < 1 or > 99)
        {
            throw new ArgumentOutOfRangeException(
                nameof(seaLevelPercent),
                seaLevelPercent,
                "[water] sea_level_percent is a fraction of the height range this world realised, and "
                + "must be between 1 and 99. Zero would mean a world with no water, which is what "
                + "omitting [water] already says; 100 would mean a world that is entirely water. "
                + "adr/0159.");
        }

        return new WaterRuleset(true, seaLevelPercent);
    }

    /// <summary>
    /// How high the sea stands, as a percent of the height range <b>this world realised</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>A percent of the realised range and not of the possible one</b>, which is
    /// <see cref="Space.TerrainGenerator"/>'s choice and is made here for its reason: the noise sums
    /// to a bell-shaped field, so a level fixed against the theoretical ceiling would drown one key
    /// and leave the next one dry. Against the realised range, <b>every world has a coast</b>.
    /// </para>
    /// <para>
    /// ⚠ <b>It is a LEVEL and not a coverage.</b> How much of the map ends up wet depends on the shape
    /// of the key's field as well as on this number, and two worlds at the same sea level will differ.
    /// ***That is the generator being a generator***, and a document quoting a water share must name
    /// the world it measured.
    /// </para>
    /// </remarks>
    public int SeaLevelPercent { get; } = SeaLevelPercent;
}

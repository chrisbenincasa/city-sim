using Borough.Core.Tables;

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
public readonly record struct WaterRuleset(
    bool Stated,
    int SeaLevelPercent,
    int FloodLevelPercent,
    ResourceId Carries,
    int CapacityPerCell,
    int OutflowPerExitPerDay,
    int RunoffPerSealedCellPerDay)
{
    /// <summary>A Ruleset whose world has no water.</summary>
    /// <remarks>
    /// <b>Absence is the unset spelling</b>, on <see cref="TerrainRuleset.None"/>'s rule. Unlike
    /// terrain — which every world has whatever the file says — water is genuinely optional, so this
    /// is a world without a coastline and not a file declining to describe one.
    /// </remarks>
    public static WaterRuleset None => default;

    /// <summary>A Ruleset that states where the sea stands, and optionally how high it rises.</summary>
    /// <param name="seaLevelPercent">Where the sea stands. Between 1 and 99.</param>
    /// <param name="floodLevelPercent">
    /// How high a flood reaches, on the same scale. <b>Zero means no floodplain</b>, which is a steep
    /// coast and a legitimate world.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="seaLevelPercent"/> is not between 1 and 99, or
    /// <paramref name="floodLevelPercent"/> is neither zero nor above the sea and below 100.
    /// </exception>
    public static WaterRuleset From(int seaLevelPercent, int floodLevelPercent = 0)
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

        // Refused at both ends for the sea level's own reasons, one rung along. AT OR BELOW the sea
        // is ground that is already under water, so the floodplain it describes is empty -- a key
        // that reads as a decision and does nothing, which is adr/0123's failure written into a
        // loader. 100 would put the flood at the highest Cell on the map, which is not a floodplain
        // but a drowning.
        if (floodLevelPercent != 0 && (floodLevelPercent <= seaLevelPercent || floodLevelPercent > 99))
        {
            throw new ArgumentOutOfRangeException(
                nameof(floodLevelPercent),
                floodLevelPercent,
                $"[water] flood_level_percent is how high a flood reaches on the same scale as "
                + $"sea_level_percent, so it must be above it ({seaLevelPercent}) and below 100. "
                + "Omit it for a world with no floodplain -- a steep coast is a world. adr/0156.");
        }

        return new WaterRuleset(
            true, seaLevelPercent, floodLevelPercent, default, 0, 0, 0);
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

    /// <summary>Whether this world has ground a flood can reach.</summary>
    /// <remarks>
    /// ⚠ <b>Absent rather than zero, on <c>adr/0123</c>.</b> A world with no floodplain has no rows in
    /// <see cref="Space.FloodCellTable"/> at all, rather than rows whose depth is zero — the same
    /// distinction <see cref="Stated"/> makes one level up.
    /// </remarks>
    public bool HasFloodplain => Stated && FloodLevelPercent > 0;

    /// <summary>The same Ruleset with a Bin on every Water Body.</summary>
    /// <param name="carries">
    /// The one Resource a Water Body holds. <b>Must be <c>Utility</c> family</b> — <c>adr/0160</c>.
    /// </param>
    /// <param name="capacityPerCell">How much one wet Cell of a body holds. Positive.</param>
    /// <param name="outflowPerExitPerDay">How much leaves per exit per Day. Positive.</param>
    /// <param name="runoffPerSealedCellPerDay">
    /// What a <b>fully sealed</b> Cell sheds into the body it drains to, per Day. Positive, and scaled
    /// down by how much of the Cell is actually sealed.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">Either quantity is not positive.</exception>
    /// <exception cref="ArgumentException"><paramref name="carries"/> names nothing.</exception>
    public WaterRuleset WithBin(
        ResourceId carries, int capacityPerCell, int outflowPerExitPerDay, int runoffPerSealedCellPerDay)
    {
        if (carries.Raw == 0)
        {
            throw new ArgumentException(
                "a Water Body's Bin needs a Resource to hold. adr/0160.", nameof(carries));
        }

        // Both positive rather than non-negative, and for one reason each. A capacity of 0 is a body
        // that can hold nothing, which is an infinite sink wearing the opposite spelling -- CONTEXT.md
        // -> Water Body's "nothing is an infinite sink" is the whole point of there being a capacity.
        // An outflow of 0 is a world where NO body drains, which is what omitting the keys says.
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacityPerCell);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(outflowPerExitPerDay);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(runoffPerSealedCellPerDay);

        return this with
        {
            Carries = carries,
            CapacityPerCell = capacityPerCell,
            OutflowPerExitPerDay = outflowPerExitPerDay,
            RunoffPerSealedCellPerDay = runoffPerSealedCellPerDay,
        };
    }

    /// <summary>Whether every Water Body in this world owns a Bin.</summary>
    /// <remarks>
    /// ⚠ <b>Absent rather than zero, on <c>adr/0123</c>.</b> A world whose <c>[water]</c> states no
    /// Bin has bodies with no level at all, rather than bodies whose level is permanently zero — the
    /// same distinction <see cref="Stated"/> and <see cref="HasFloodplain"/> make.
    /// </remarks>
    public bool HasBin => Stated && Carries.Raw != 0;
}

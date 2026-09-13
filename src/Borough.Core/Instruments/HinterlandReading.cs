namespace Borough.Core.Instruments;

using Borough.Core.Entities;
using Borough.Core.Space;

/// <summary>What one edge's Outside did over a single Day, counter by counter.</summary>
/// <remarks>
/// <para>
/// <b>A fresh occasion has exactly one of four outcomes</b> — <paramref name="NoConnection"/>,
/// <paramref name="NoSample"/>, <paramref name="StayedOutside"/> or <paramref name="Willing"/> — so
/// those four sum to <paramref name="Occasions"/> and a reader may say so. The rest do not partition
/// anything: <paramref name="Reviewed"/> counts retries of families already waiting and must never be
/// added to the fresh interest, and <paramref name="Replenished"/> and <paramref name="Turnover"/> are
/// the Outside's own arithmetic rather than anybody's decision.
/// </para>
/// <para>
/// ⚠ <b><paramref name="Requested"/> is the subset of <paramref name="Occasions"/> a player asked
/// for</b> with an <c>Arrive</c> command, not a separate series beside them.
/// </para>
/// </remarks>
/// <param name="Occasions">Fresh occasions the edge generated, asked-for ones included.</param>
/// <param name="Requested">Of those, the ones an <c>Arrive</c> command asked for.</param>
/// <param name="NoConnection">Occasions that found no gate on the edge.</param>
/// <param name="NoSample">Occasions that found no feasible home to compare against.</param>
/// <param name="StayedOutside">Occasions that compared and preferred the Outside.</param>
/// <param name="Willing">Occasions that compared and wanted to come.</param>
/// <param name="Admitted">Households let through a gate, fresh and queued alike.</param>
/// <param name="Queued">Willing Households that found every door full and waited.</param>
/// <param name="Expired">Waiting Households whose authored wait ran out.</param>
/// <param name="Reviewed">Scheduled reviews performed for Households already waiting.</param>
/// <param name="ChangedMind">Waiting Households that reconsidered and went home.</param>
/// <param name="ConnectionLost">Waiting Households cancelled because the edge lost every gate.</param>
/// <param name="Replenished">Households the Outside added, recovering towards its resting count.</param>
/// <param name="Turnover">Households the Outside dropped for standing above its resting count.</param>
public readonly record struct HinterlandFlows(
    int Occasions,
    int Requested,
    int NoConnection,
    int NoSample,
    int StayedOutside,
    int Willing,
    int Admitted,
    int Queued,
    int Expired,
    int Reviewed,
    int ChangedMind,
    int ConnectionLost,
    int Replenished,
    int Turnover);

/// <summary>
/// One edge's Outside as it stands, with the Day it is in and the last complete Day beside it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Reading changes nothing.</b> No draw is consumed, no meter is reset and no row is written, so a
/// panel refreshing every frame and a headless dump reading once see the same city. That is why the
/// placement and give-up figures the inspection contract also asks for are <em>not</em> here: they
/// live in <c>PlacementActivity</c>, which a Census <em>drains</em>, and a reading that drained them
/// would take them from whoever asked second.
/// </para>
/// <para>
/// ⚠ <b>Every figure is per edge and never per gate</b>, stock included. Two doors on one edge share
/// one Outside, so a reader must not present a gate's admissions as its own market.
/// </para>
/// </remarks>
/// <param name="Edge">Which edge this is.</param>
/// <param name="Day">The Day <paramref name="Today"/> counts, and the Day the gate meters count.</param>
/// <param name="Gates">How many Outside Connections stand on this edge.</param>
/// <param name="StockHouseholds">Households standing behind the edge, reserved ones included.</param>
/// <param name="StockPeople">The people in them.</param>
/// <param name="ReservedHouseholds">Of the stock, the Households promised to a waiting place.</param>
/// <param name="RestingHouseholds">The count the Outside recovers towards.</param>
/// <param name="Compositions">How many composition groups stand behind the edge.</param>
/// <param name="QueueHouseholds">Households waiting outside a full door.</param>
/// <param name="QueuePeople">The people in them.</param>
/// <param name="OldestWait">How long the longest-waiting Household has waited, in Ticks.</param>
/// <param name="AdmittedToday">Households admitted through this edge's doors today.</param>
/// <param name="RemainingToday">What is left of those doors' quotas today.</param>
/// <param name="Today">The Day in progress.</param>
/// <param name="Yesterday">The last complete Day.</param>
public readonly record struct HinterlandReading(
    MapEdge Edge,
    int Day,
    int Gates,
    int StockHouseholds,
    long StockPeople,
    int ReservedHouseholds,
    int RestingHouseholds,
    int Compositions,
    int QueueHouseholds,
    long QueuePeople,
    ulong OldestWait,
    int AdmittedToday,
    int RemainingToday,
    HinterlandFlows Today,
    HinterlandFlows Yesterday)
{
    /// <summary>Stock nobody has promised to a waiting Household.</summary>
    public int AvailableHouseholds => StockHouseholds - ReservedHouseholds;

    /// <summary>Reads one edge.</summary>
    /// <param name="world">The world to read, which this leaves exactly as it found it.</param>
    /// <param name="edge">The edge to read.</param>
    /// <returns>The edge as it stands.</returns>
    public static HinterlandReading Of(World world, MapEdge edge)
    {
        ArgumentNullException.ThrowIfNull(world);

        int slot = HinterlandTable.SlotOf(edge);
        HinterlandTable edges = world.Hinterlands;
        HinterlandPopulationTable groups = world.HinterlandPopulation;

        int stock = 0;
        int reserved = 0;
        int resting = 0;
        int compositions = 0;
        long people = 0;

        foreach (int group in groups.Groups(edges).Walk(slot))
        {
            stock += groups.Stock[group];
            reserved += groups.Reserved[group];
            resting += groups.Target[group];
            people += groups.People(group);
            compositions++;
        }

        // FlowDay rather than the Tick divided out: Simulation rolls it at the first Tick of every
        // Day, so it already names the Day both these counters and the gate meters are denominated in.
        int day = edges.FlowDay[slot];
        int gates = 0;
        int admitted = 0;
        int remaining = 0;

        foreach (int gate in world.Buildings.Gates(edges).Walk(slot))
        {
            gates++;

            if (!world.TryArrivalsPerDay(world.Buildings.Kind[gate], out int ceiling))
            {
                continue;
            }

            int spent = SpentToday(world, gate, day);

            admitted += spent;
            remaining += ceiling - spent;
        }

        HinterlandQueueTable queue = world.HinterlandQueue;
        int waiting = 0;
        long waitingPeople = 0;
        ulong oldest = 0;

        foreach (int row in queue.Admissions(edges).Walk(slot))
        {
            waiting++;

            if (groups.Rows.TryResolve(queue.Group[row], out int group))
            {
                waitingPeople += groups.Members(group);
            }

            ulong waited = world.Tick.Raw - queue.Since[row].Raw;

            if (waited > oldest)
            {
                oldest = waited;
            }
        }

        return new HinterlandReading(
            edge,
            day,
            gates,
            stock,
            people,
            reserved,
            resting,
            compositions,
            waiting,
            waitingPeople,
            oldest,
            admitted,
            remaining,
            Flows(edges, slot, today: true),
            Flows(edges, slot, today: false));
    }

    internal static int SpentToday(World world, int gate, int day) =>
        world.Buildings.ArrivalDay[gate] == day ? world.Buildings.ArrivalsToday[gate] : 0;

    private static HinterlandFlows Flows(HinterlandTable edges, int slot, bool today) => new(
        today ? edges.OccasionsToday[slot] : edges.OccasionsYesterday[slot],
        today ? edges.RequestedToday[slot] : edges.RequestedYesterday[slot],
        today ? edges.NoConnectionToday[slot] : edges.NoConnectionYesterday[slot],
        today ? edges.NoSampleToday[slot] : edges.NoSampleYesterday[slot],
        today ? edges.StayedOutsideToday[slot] : edges.StayedOutsideYesterday[slot],
        today ? edges.WillingToday[slot] : edges.WillingYesterday[slot],
        today ? edges.AdmittedToday[slot] : edges.AdmittedYesterday[slot],
        today ? edges.QueuedToday[slot] : edges.QueuedYesterday[slot],
        today ? edges.ExpiredToday[slot] : edges.ExpiredYesterday[slot],
        today ? edges.ReviewedToday[slot] : edges.ReviewedYesterday[slot],
        today ? edges.ChangedMindToday[slot] : edges.ChangedMindYesterday[slot],
        today ? edges.ConnectionLostToday[slot] : edges.ConnectionLostYesterday[slot],
        today ? edges.ReplenishedToday[slot] : edges.ReplenishedYesterday[slot],
        today ? edges.TurnoverToday[slot] : edges.TurnoverYesterday[slot]);
}

/// <summary>One Outside Connection's own quota, on the Day the meter counts.</summary>
/// <remarks>
/// ⚠ <b>A gate has a quota and it does not have a market.</b> The stock it draws on belongs to the
/// edge and is shared with every other door on it, so a reader showing this beside
/// <see cref="HinterlandReading.StockHouseholds"/> must say which of the two the number belongs to.
/// </remarks>
/// <param name="Building">The gate's monotonic id, which survives a slot being reused.</param>
/// <param name="Edge">The edge it stands on.</param>
/// <param name="Kind">The Building kind it was raised as.</param>
/// <param name="Ceiling">Households it may admit in a Day — <c>[[building]] arrivals_per_day</c>.</param>
/// <param name="AdmittedToday">How many it has admitted today.</param>
public readonly record struct HinterlandGateReading(
    ulong Building,
    MapEdge Edge,
    byte Kind,
    int Ceiling,
    int AdmittedToday)
{
    /// <summary>What is left of today's quota.</summary>
    public int RemainingToday => Ceiling - AdmittedToday;

    /// <summary>Reads every gate on one edge, in the order the engine offers families to them.</summary>
    /// <param name="world">The world to read, which this leaves exactly as it found it.</param>
    /// <param name="edge">The edge whose doors to read.</param>
    /// <param name="into">Where to put them. A shorter buffer is filled and the rest are skipped.</param>
    /// <returns>How many were written, which is at most <see cref="HinterlandReading.Gates"/>.</returns>
    public static int Of(World world, MapEdge edge, Span<HinterlandGateReading> into)
    {
        ArgumentNullException.ThrowIfNull(world);

        int slot = HinterlandTable.SlotOf(edge);
        int day = world.Hinterlands.FlowDay[slot];
        int written = 0;

        foreach (int gate in world.Buildings.Gates(world.Hinterlands).Walk(slot))
        {
            if (written == into.Length)
            {
                break;
            }

            byte kind = world.Buildings.Kind[gate];

            into[written++] = new HinterlandGateReading(
                world.Buildings.Rows.IdAt(gate),
                edge,
                kind,
                world.TryArrivalsPerDay(kind, out int ceiling) ? ceiling : 0,
                HinterlandReading.SpentToday(world, gate, day));
        }

        return written;
    }
}

/// <summary>One composition of Households standing behind an edge.</summary>
/// <remarks>
/// ⚠ <b>A composition is a storage key rather than a group that decides anything.</b> The Households
/// in it are counted together because they are identical, and they are split the moment one of them
/// crosses — which is <c>CONTEXT.md</c>'s reason for banning a Cohort and not an exception to it.
/// </remarks>
/// <param name="Edge">The edge it stands behind.</param>
/// <param name="Composition">Who one of these Households is made of.</param>
/// <param name="Authored">Whether a Ruleset declared it, as against a return having created it.</param>
/// <param name="Target">The count the Outside recovers this group towards.</param>
/// <param name="Stock">Households standing in it now, reserved ones included.</param>
/// <param name="Reserved">Of those, the ones promised to a waiting place.</param>
/// <param name="Replenished">Households added to it over the world's life.</param>
/// <param name="Returned">Households credited to it by an emigration.</param>
/// <param name="Admitted">Households it has lost to the city.</param>
/// <param name="Turnover">Households dropped from it for standing above <paramref name="Target"/>.</param>
public readonly record struct HinterlandGroupReading(
    MapEdge Edge,
    HinterlandComposition Composition,
    bool Authored,
    int Target,
    int Stock,
    int Reserved,
    long Replenished,
    long Returned,
    long Admitted,
    long Turnover)
{
    /// <summary>Households in this group nobody has promised to a waiting place.</summary>
    public int Free => Stock - Reserved;

    /// <summary>The people standing in it.</summary>
    public long People => (long)Stock * Composition.Members;

    /// <summary>Reads every composition standing behind one edge, in ascending slot order.</summary>
    /// <param name="world">The world to read, which this leaves exactly as it found it.</param>
    /// <param name="edge">The edge whose compositions to read.</param>
    /// <param name="into">Where to put them. A shorter buffer is filled and the rest are skipped.</param>
    /// <returns>How many were written, at most <see cref="HinterlandReading.Compositions"/>.</returns>
    public static int Of(World world, MapEdge edge, Span<HinterlandGroupReading> into)
    {
        ArgumentNullException.ThrowIfNull(world);

        HinterlandPopulationTable groups = world.HinterlandPopulation;
        int slot = HinterlandTable.SlotOf(edge);
        int written = 0;

        foreach (int group in groups.Groups(world.Hinterlands).Walk(slot))
        {
            if (written == into.Length)
            {
                break;
            }

            into[written++] = new HinterlandGroupReading(
                edge,
                groups.CompositionAt(group),
                groups.Authored[group] != 0,
                groups.Target[group],
                groups.Stock[group],
                groups.Reserved[group],
                groups.Replenished[group],
                groups.Returned[group],
                groups.Admitted[group],
                groups.Turnover[group]);
        }

        return written;
    }
}

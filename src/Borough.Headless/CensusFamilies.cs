using Borough.Core.Instruments;

namespace Borough.Headless;

/// <summary>
/// The Census's seven families, each as the counters it holds and the name a human reads for each.
/// </summary>
/// <remarks>
/// <para>
/// <b>One copy, because there are now two readers.</b> These tables began inside
/// <see cref="CensusReport"/>, which was the only thing that printed a census. <see cref="SeriesReport"/>
/// prints the same families in a different shape, and a second copy of *which counters exist and what
/// they are called* is <c>plans/0012</c> <b>Cause 1</b> exactly — one fact in two files, where the
/// copy that drifts is whichever one the next family is not added to.
/// </para>
/// <para>
/// <b>The shell owns every string a human reads</b> (<c>adr/0002</c>), which is why the names are here
/// and not on the enums.
/// </para>
/// </remarks>
internal static class CensusFamilies
{
    /// <summary>The counters, in the order <c>CensusCounter</c> declares them.</summary>
    public static readonly (CensusCounter Counter, string Name)[] Counters =
    [
        (CensusCounter.Live, "live"),
        (CensusCounter.Slots, "slots"),
        (CensusCounter.Capacity, "capacity"),
    ];

    /// <summary>
    /// The Rule engine's counters, each read twice.
    /// </summary>
    /// <remarks>
    /// <b>These rows mean something different from the table ones, and the labels have to carry
    /// that.</b> A table row is a size at the moment of the reading; a Rule row is a total <em>between</em>
    /// readings, so two readings are two intervals rather than two instants and <c>high</c> is the
    /// busiest interval rather than the largest the collection ever got. The <c>peak</c> rows are the
    /// finer question the sums cannot answer — the worst single Tick inside an interval, which is the
    /// figure a per-Tick budget is held against.
    /// </remarks>
    public static readonly (RuleCounter Counter, Aggregate Aggregate, string Name)[] RuleCounters =
    [
        (RuleCounter.Due, Aggregate.Sum, "due"),
        (RuleCounter.Due, Aggregate.Peak, "due peak"),
        (RuleCounter.Evaluations, Aggregate.Sum, "evaluations"),
        (RuleCounter.Evaluations, Aggregate.Peak, "evaluations peak"),
        (RuleCounter.ChainRungs, Aggregate.Sum, "chain rungs"),
        (RuleCounter.ChainRungs, Aggregate.Peak, "chain rungs peak"),
    ];

    /// <summary>
    /// The Sweep family's counters, on the same terms as the block above.
    /// </summary>
    /// <remarks>
    /// <b>Its own family for <c>adr/0033</c>'s reason.</b> A Zone Rule's trigger and a Bin Rule's due
    /// row are not the same kind of event — the two families differ in when their effect becomes
    /// visible within a Tick — so printing them together would be the layout claiming a
    /// commensurability the design denies. <c>evaluated</c> is <c>vacant + occupied</c> and is the
    /// quantity <c>02 §5.7</c>'s claim is about, which is why both halves are printed rather than
    /// their sum: their ratio is how full the city is, and no sum recovers it.
    /// </remarks>
    public static readonly (ZoneCounter Counter, Aggregate Aggregate, string Name)[] ZoneCounters =
    [
        (ZoneCounter.Triggers, Aggregate.Sum, "triggers"),
        (ZoneCounter.Vacant, Aggregate.Sum, "vacant"),
        (ZoneCounter.Occupied, Aggregate.Sum, "occupied"),
        (ZoneCounter.Created, Aggregate.Sum, "created"),
        (ZoneCounter.Created, Aggregate.Peak, "created peak"),
        (ZoneCounter.Demolished, Aggregate.Sum, "demolished"),
        (ZoneCounter.Demolished, Aggregate.Peak, "demolished peak"),
    ];

    /// <summary>
    /// The placement pass's counters (<c>adr/0069</c>), on the same terms as the family above.
    /// </summary>
    /// <remarks>
    /// <b>Both are printed because the gap between them is the reading.</b> <c>considered</c> without
    /// <c>placed</c> is a queue being looked at and not housed, which is a city out of dwellings;
    /// <c>placed</c> alone cannot tell that from a pass that has stopped running.
    /// </remarks>
    public static readonly (PlacementCounter Counter, Aggregate Aggregate, string Name)[]
        PlacementCounters =
    [
        (PlacementCounter.Considered, Aggregate.Sum, "considered"),
        (PlacementCounter.Placed, Aggregate.Sum, "placed"),
        (PlacementCounter.Placed, Aggregate.Peak, "placed peak"),
    ];

    /// <summary>
    /// The job assignment pass's counters, all four (<c>adr/0081</c>).
    /// </summary>
    /// <remarks>
    /// <b>All four rather than a selection, because each pair of them is a different subtraction and
    /// every one of those subtractions is a reading.</b> <c>considered − seeking</c> is what sampling
    /// the whole population costs against keeping a list of the unemployed; <c>seeking − employed</c>
    /// is the job shortage; and <c>beyond</c> is the only line in either report that describes the
    /// <em>network</em> — vacancies that existed, were found, and could not be walked to inside the
    /// ceiling. Printing three of the four would leave one of those unavailable.
    /// </remarks>
    /// <remarks>
    /// <b>The three rungs are printed beside <c>employed</c>, which they sum to</b> (<c>adr/0095</c>).
    /// They are the whole readable output of the grading: two of the three edges refuse nothing, so
    /// if these lines are not printed the only trace a graded Budget leaves in a run is the one
    /// number a binary Budget already left. <b>That is 5b-bis task 6's finding taken as an
    /// instruction</b> — <c>TripCounter</c> was built, wired, tested and printed nowhere for a whole
    /// milestone, so for a milestone its only reader was the suite.
    /// </remarks>
    public static readonly (JobCounter Counter, Aggregate Aggregate, string Name)[] JobCounters =
    [
        (JobCounter.Considered, Aggregate.Sum, "considered"),
        (JobCounter.Seeking, Aggregate.Sum, "seeking"),
        (JobCounter.Employed, Aggregate.Sum, "employed"),
        (JobCounter.Employed, Aggregate.Peak, "employed peak"),
        (JobCounter.Fast, Aggregate.Sum, "fast"),
        (JobCounter.Moderate, Aggregate.Sum, "moderate"),
        (JobCounter.Unsavoury, Aggregate.Sum, "unsavoury"),
        (JobCounter.Beyond, Aggregate.Sum, "beyond ceiling"),
    ];

    /// <summary>
    /// The Policy sweeps' six counters — <c>02 §4.2</c>'s Flow Policies (<c>plans/0033</c> task 5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Printed on the day the family was built</b>, which is 5b-bis task 6's finding taken as an
    /// instruction rather than as a story: a Census family whose only reader is the suite is a family
    /// no operator can see.
    /// </para>
    /// <para>
    /// ⚠ <b><c>floored to zero</c> is the row the milestone owes and is the reason the others are
    /// here.</b> <c>adr/0115</c> makes it the named ratifier for money's unit — <em>a discipline the
    /// loader cannot check needs a counter or it is a comment</em> — and it is unreadable without
    /// <c>considered</c> beside it, since a count of floorings means nothing without the population
    /// it is a share of.
    /// </para>
    /// <para>
    /// <b><c>exhausted</c> counts sweeps and everything else counts members</b>, so the two are not
    /// comparable and the row says <em>sweeps</em> in its own name. A reader who sums this column
    /// gets a number that is about nothing.
    /// </para>
    /// </remarks>
    public static readonly (PolicyCounter Counter, Aggregate Aggregate, string Name)[] PolicyCounters =
    [
        (PolicyCounter.Triggers, Aggregate.Sum, "triggers"),
        (PolicyCounter.Considered, Aggregate.Sum, "considered"),
        (PolicyCounter.Applied, Aggregate.Sum, "applied"),
        (PolicyCounter.Applied, Aggregate.Peak, "applied peak"),
        (PolicyCounter.Floored, Aggregate.Sum, "floored to zero"),
        (PolicyCounter.Unaffordable, Aggregate.Sum, "could not pay"),
        (PolicyCounter.Exhausted, Aggregate.Sum, "sweeps run dry"),
    ];

    /// <summary>
    /// The four Trip Fates and the two Leg-mode counters, and <b>the Fates were built in 5b and
    /// printed by nothing until 5b-bis task 6.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>A Census family with no reader is a family nobody can see.</b> Milestone 5b added
    /// <c>TripCounter</c>, wired all four flows through <c>Census.Observe</c> and tested them — and
    /// left the report untouched, so the only way to read a Trip Fate was to write a test. That is
    /// the shape of <c>adr/0064</c>'s finding on a different axis: not a fact with two copies that
    /// drifted, but a mechanism whose <em>only</em> consumer was the suite, which is a consumer no
    /// operator has.
    /// </para>
    /// <para>
    /// <b>All four Fates, including the two a walking-only city cannot produce.</b> <c>adr/0076</c>
    /// closes the Fate set at four, so a row each is the shape that needs no edit when the missing
    /// conditions arrive — and a zero beside a nonzero is informative in a way a missing row is not.
    /// </para>
    /// </remarks>
    /// <summary>
    /// The money aggregates, ordered so the report reads as a balance sheet.
    /// </summary>
    /// <remarks>
    /// <b><c>supply</c> and <c>held</c> lead, and they lead in that order</b>, because the pair is
    /// <c>Invariant.MoneyIsConserved</c> printed: what was issued against what the walk found. The
    /// four below decompose <c>held</c>, and <c>01 §5.1</c>'s separation is why <c>treasury</c> is a
    /// row of its own rather than a share of <c>supply</c> — the two are different trajectories, and
    /// a picture showing one hides the one the endgame turns on.
    /// </remarks>
    public static readonly (MoneyCounter Counter, string Name)[] MoneyCounters =
    [
        (MoneyCounter.Supply, "supply"),
        (MoneyCounter.Held, "held"),
        (MoneyCounter.Treasury, "treasury"),
        (MoneyCounter.Households, "households"),
        (MoneyCounter.Businesses, "businesses"),
        (MoneyCounter.Elsewhere, "elsewhere"),
    ];

    /// <summary>What the Policy sweeps moved, by direction relative to the treasury.</summary>
    public static readonly (MoneyFlowCounter Counter, Aggregate Aggregate, string Name)[]
        MoneyFlowCounters =
    [
        (MoneyFlowCounter.ToTreasury, Aggregate.Sum, "to treasury"),
        (MoneyFlowCounter.ToTreasury, Aggregate.Peak, "to treasury peak"),
        (MoneyFlowCounter.FromTreasury, Aggregate.Sum, "from treasury"),
        (MoneyFlowCounter.FromTreasury, Aggregate.Peak, "from treasury peak"),
    ];

    public static readonly (TripCounter Counter, Aggregate Aggregate, string Name)[] TripCounters =
    [
        (TripCounter.Completed, Aggregate.Sum, "completed"),
        (TripCounter.Completed, Aggregate.Peak, "completed peak"),
        (TripCounter.NoRouteFound, Aggregate.Sum, "no route"),
        (TripCounter.ExceededCommuteBudget, Aggregate.Sum, "over budget"),
        (TripCounter.Stranded, Aggregate.Sum, "stranded"),
        (TripCounter.WalkLegs, Aggregate.Sum, "walk legs"),
        (TripCounter.DriveLegs, Aggregate.Sum, "drive legs"),
    ];

    /// <summary>
    /// The Trip cost histogram, one column per band of clock minutes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Sums only, and the omission is the point.</b> A bucket's peak is <em>the busiest single Tick
    /// in that band</em>, which is a statement about the departure window rather than about how far
    /// anybody walks — and the departure window is already stated by the Ruleset. Printing fourteen
    /// columns to say what <c>commute_peak_factor</c> says would be the report competing with the file.
    /// </para>
    /// <para>
    /// ⚠ <b>This is not the distribution the Commute Budget is a percentile of.</b> A commute exists
    /// only because the assignment pass already accepted the job at the other end of it, inside the
    /// Budget — so the ceiling is <em>upstream</em> and this distribution is censored by the number it
    /// would be used to ratify. The uncensored one is <c>--trips</c>, which walks every Building pair
    /// and had to be taken before a Budget existed.
    /// </para>
    /// </remarks>
    public static readonly (TripCostBucket Bucket, string Name)[] TripCosts =
    [
        (TripCostBucket.UnderOneMinute, "under 1 min"),
        (TripCostBucket.UnderTwoMinutes, "1-2 min"),
        (TripCostBucket.UnderFourMinutes, "2-4 min"),
        (TripCostBucket.UnderEightMinutes, "4-8 min"),
        (TripCostBucket.UnderSixteenMinutes, "8-16 min"),
        (TripCostBucket.UnderThirtyTwoMinutes, "16-32 min"),
        (TripCostBucket.ThirtyTwoMinutesOrMore, "32 min or none"),
    ];
}

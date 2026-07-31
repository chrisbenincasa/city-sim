using System.Globalization;
using System.Text;

namespace S4.Kernels.Kernels;

/// <summary>
/// The row schema and the row counts derived in S4 task 2, expressed as data so K0 can allocate
/// against them and K1-K5 can size against the same thing.
///
/// Two properties of this file matter more than its contents.
///
/// **Counts are per 1,000 Citizens, never absolute.** 00-vision commits to *at least* a million, so
/// a table sized to exactly 1M encodes a cap the design does not have. Population is a parameter
/// here for the same reason docs/spike-results.md records ratios.
///
/// **Column widths come from 05 §3 and nowhere else** — Money and accumulators i64, counts i32,
/// Ticks u64, handles {u32,u32} = 8 B. Structure-of-arrays means a row costs the sum of its column
/// widths with no per-row padding, which is why nothing here rounds up to an alignment boundary.
/// </summary>
internal static class WorldSchema
{
    /// <summary>Which table a column belongs to. The split is on the caller's cadence (05 §3).</summary>
    public enum Tier
    {
        /// <summary>Touched on an ordinary Tick — the Event Wheel drain and Phase 4 Move.</summary>
        PerTick,

        /// <summary>Touched when the entity wakes. The working set a decision actually costs.</summary>
        Wake,

        /// <summary>Touched on a transaction or a click.</summary>
        Cold,
    }

    public readonly record struct Column(string Name, int Bytes, Tier Tier);

    public sealed record Table(string Name, double RowsPer1000Citizens, Column[] Columns)
    {
        public long Rows(long citizens) => (long)(RowsPer1000Citizens * citizens / 1000.0);

        public int BytesPerRow(Tier tier)
        {
            var total = 0;
            foreach (var column in Columns)
            {
                if (column.Tier == tier)
                {
                    total += column.Bytes;
                }
            }

            return total;
        }

        public int BytesPerRow() => Columns.Sum(c => c.Bytes);
    }

    private const int Handle = 8;   // { index: u32, generation: u32 }
    private const int Index = 4;    // an intrusive list link or a bare row index
    private const int Tick = 8;     // u64
    private const int Accum = 8;    // i64 — money and accumulators
    private const int Count = 4;    // i32
    private const int Enum8 = 1;

    /// <summary>
    /// The Citizen, recomputed. 05 §3's "on the order of 40 bytes hot" is admitted stale and, worse,
    /// was never given a definition — the per-Tick and wake figures are 4x apart and 40 matches
    /// neither. Ownership follows task 2's rule: a field lives at the level at which it can differ,
    /// so home, money and the Provider List are the Household's and are not here.
    /// </summary>
    public static readonly Table Citizens = new("Citizens", 1000, [
        new("next_event_tick", Tick, Tier.PerTick),
        new("wheel_next", Index, Tier.PerTick),
        new("activity", Enum8, Tier.PerTick),

        new("entity_id", Count, Tier.Wake),
        new("generation", Count, Tier.Wake),
        new("household", Handle, Tier.Wake),
        new("workplace", Handle, Tier.Wake),
        new("experience", Accum, Tier.Wake),
        new("skill_tier", Enum8, Tier.Wake),
        new("employment", Enum8, Tier.Wake),
        new("occupant_next", Index, Tier.Wake),

        new("age", Enum8, Tier.Cold),
        new("health", Count, Tier.Cold),
    ]);

    /// <summary>
    /// 360 per 1,000 Citizens — a mean household size of 2.8, derived from adr/0011's own stage
    /// compositions rather than from the asserted ~400k. Household economics (income, expenses,
    /// savings, purchases missed) is deferred.md's "planned next layer" and is sized here because
    /// that entry requires the record accommodate it without restructuring.
    /// </summary>
    public static readonly Table Households = new("Households", 360, [
        new("next_event_tick", Tick, Tier.PerTick),
        new("wheel_next", Index, Tier.PerTick),

        new("entity_id", Count, Tier.Wake),
        new("generation", Count, Tier.Wake),
        new("dwelling", Handle, Tier.Wake),
        new("money", Accum, Tier.Wake),
        new("life_stage", Enum8, Tier.Wake),
        new("in_education", Enum8, Tier.Wake),
        new("car_owned", Enum8, Tier.Wake),
        new("adults", Enum8, Tier.Wake),
        new("children", Enum8, Tier.Wake),
        new("citizen_head", Index, Tier.Wake),
        new("occupant_next", Index, Tier.Wake),
        new("needs[3]", 3 * Count, Tier.Wake),
        new("taste[4]", 4 * Count, Tier.Wake),
        new("provider_list[8]", 8 * (Handle + Enum8 + Count), Tier.Wake),

        new("schooling", Accum, Tier.Cold),
        new("savings", Accum, Tier.Cold),
        new("income", Accum, Tier.Cold),
        new("expenses", Accum, Tier.Cold),
        new("purchases_missed", Count, Tier.Cold),
        new("failed_attempts", Enum8, Tier.Cold),
        new("refusal_reason", Enum8, Tier.Cold),
    ]);

    /// <summary>
    /// Provisional throughout — no Households-per-Building figure exists anywhere in the corpus.
    /// The Bins are the ResourceMap: a sorted array, at most nine entries under adr/0031, each
    /// { resource, amount, capacity }.
    /// </summary>
    public static readonly Table Buildings = new("Buildings", 150, [
        new("next_event_tick", Tick, Tier.PerTick),
        new("wheel_next", Index, Tier.PerTick),
        new("bins[9]", 9 * (Enum8 + Count + Count), Tier.Wake),

        new("entity_id", Count, Tier.Wake),
        new("generation", Count, Tier.Wake),
        new("lot", Handle, Tier.Wake),
        new("kind", Count, Tier.Wake),
        new("occupant_head", Index, Tier.Wake),
        new("access_pedestrian", Count, Tier.Wake),
        new("access_vehicle", Count, Tier.Wake),

        new("footprint_origin", Count, Tier.Cold),
        new("footprint_extent", Count, Tier.Cold),
        new("occupancy", Count, Tier.Cold),
        new("quality", Count, Tier.Cold),
        new("failure_pressure", Count, Tier.Cold),
        new("failure_reason", Enum8, Tier.Cold),
        new("last_rule", Count, Tier.Cold),
        new("derelict", Enum8, Tier.Cold),
    ]);

    public static readonly Table Businesses = new("Businesses", 50, [
        new("next_event_tick", Tick, Tier.PerTick),
        new("wheel_next", Index, Tier.PerTick),
        new("bins[9]", 9 * (Enum8 + Count + Count), Tier.Wake),

        new("entity_id", Count, Tier.Wake),
        new("generation", Count, Tier.Wake),
        new("building", Handle, Tier.Wake),
        new("balance", Accum, Tier.Wake),
        new("posted_wage", Accum, Tier.Wake),
        new("positions", Count, Tier.Wake),
        new("filled", Count, Tier.Wake),
        new("employee_head", Index, Tier.Wake),

        new("margin", Accum, Tier.Cold),
        new("fill_rate", Count, Tier.Cold),
    ]);

    public static readonly Table Lots = new("Lots", 225, [
        new("generation", Count, Tier.Wake),
        new("building", Handle, Tier.Wake),
        new("zone", Enum8, Tier.Wake),
        new("density_band", Enum8, Tier.Wake),

        new("frontage", Count, Tier.Cold),
        new("origin", Count, Tier.Cold),
        new("extent", Count, Tier.Cold),
        new("price", Accum, Tier.Cold),
        new("vacancy_reason", Enum8, Tier.Cold),
    ]);

    /// <summary>
    /// ~56 in flight per 1,000 Citizens, from ~1.9M Trips/Day at a mean 240 Ticks. Both the trip
    /// rate and the mean duration are provisional; the corpus's own ~400k/Day is one Trip per
    /// Household per Day and omits the journey home.
    /// </summary>
    public static readonly Table Trips = new("Trips in flight", 56, [
        new("departure_tick", Tick, Tier.PerTick),
        new("arrival_tick", Tick, Tier.PerTick),
        new("leg_head", Index, Tier.PerTick),
        new("current_leg", Index, Tier.PerTick),

        new("traveller", Handle, Tier.Wake),
        new("origin", Count, Tier.Wake),
        new("destination", Count, Tier.Wake),
        new("purpose", Enum8, Tier.Wake),
        new("accrued_cost", Count, Tier.Wake),
        new("parked_at", Handle, Tier.Wake),

        new("fate", Enum8, Tier.Cold),
    ]);

    /// <summary>2.5 Legs per Trip — adr/0008's walk-drive-walk is never fewer than three.</summary>
    public static readonly Table Legs = new("Legs in flight", 140, [
        new("arrival_tick", Tick, Tier.PerTick),
        new("next_leg", Index, Tier.PerTick),

        new("mode", Enum8, Tier.Wake),
        new("from_access", Count, Tier.Wake),
        new("to_access", Count, Tier.Wake),
        new("cost", Count, Tier.Wake),
    ]);

    public static readonly Table Segments = new("Segments", 30, [
        new("volume", Count, Tier.PerTick),

        new("generation", Count, Tier.Wake),
        new("capacity", Count, Tier.Wake),
        new("free_flow_speed", Count, Tier.Wake),
        new("fidelity", Enum8, Tier.Wake),
        new("lane_head", Index, Tier.Wake),
        new("length", Count, Tier.Wake),

        new("complexity_factor", Count, Tier.Cold),
        new("accumulated_wear", Accum, Tier.Cold),
        new("parking_bin", Count + Count, Tier.Cold),
        new("parking_permission", Enum8, Tier.Cold),
    ]);

    public static readonly Table[] All =
        [Citizens, Households, Buildings, Businesses, Lots, Trips, Legs, Segments];

    /// <summary>
    /// Lanes and Vehicles are absent from All deliberately. Both are sized by the **Microscopic
    /// Cap**, which is a fixed world constant with no value — a Vehicle exists only on a Microscopic
    /// Segment, and a Statistical Segment has no Lanes at all. K0 reports them as a function of the
    /// Cap rather than picking a number, which also makes K0 the place that informs what the Cap
    /// should be.
    /// </summary>
    public static readonly Column[] LaneColumns =
    [
        new("queue_head", Index, Tier.PerTick),
        new("generation", Count, Tier.Wake),
        new("overlap_head", Index, Tier.Wake),
        new("mode_mask", Enum8, Tier.Wake),
        new("successor", Index, Tier.Wake),
        new("length", Count, Tier.Wake),
    ];

    /// <summary>
    /// A Vehicle is addressed as (Lane, index) and never by a global handle (05 §3), so it carries
    /// no generation. Position is Q16.16 in sub-Tile units — i32 — and is the design's only
    /// genuinely continuous quantity.
    /// </summary>
    public static readonly Column[] VehicleColumns =
    [
        new("position_q16_16", Count, Tier.PerTick),
        new("velocity", Count, Tier.PerTick),
        new("desired_speed", Count, Tier.PerTick),
        new("next_in_queue", Index, Tier.PerTick),
        new("trip", Handle, Tier.PerTick),
    ];

    /// <summary>Average Lanes per Microscopic Segment, and Vehicles per Lane at jam density.</summary>
    public const int LanesPerMicroscopicSegment = 4;
    public const int VehiclesPerLaneAtJam = 32;

    public static string ToMarkdown()
    {
        var sb = new StringBuilder();
        var c = CultureInfo.InvariantCulture;

        sb.AppendLine("| Table | Rows / 1,000 Citizens | Per-Tick B/row | Wake B/row | Cold B/row | Total B/row |");
        sb.AppendLine("|---|---:|---:|---:|---:|---:|");
        foreach (var t in All)
        {
            sb.Append(c, $"| {t.Name} | {t.RowsPer1000Citizens:F0} | {t.BytesPerRow(Tier.PerTick)} | ");
            sb.AppendLine(c, $"{t.BytesPerRow(Tier.Wake)} | {t.BytesPerRow(Tier.Cold)} | {t.BytesPerRow()} |");
        }

        return sb.ToString();
    }
}

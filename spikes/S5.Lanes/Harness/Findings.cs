namespace S5.Lanes.Harness;

/// <summary>
/// The handful of numbers L4 composes into the derived product, carried between reports.
/// </summary>
/// <remarks>
/// A view over the sections that produced them and never a source — the same discipline
/// <c>plans/0013</c> states for the Tick budget. If a section did not run, its field is zero and L4
/// says so rather than printing a product of a missing measurement.
/// </remarks>
internal static class Findings
{
    /// <summary>L0. Picoseconds per Vehicle for the bare walk.</summary>
    public static long DenominatorPicoseconds { get; set; }

    /// <summary>L1. Picoseconds per Vehicle for car-following on a Segment-sized queue.</summary>
    public static long QueuePicoseconds { get; set; }

    /// <summary>L1. Vehicles per Lane the L1 headline was taken at.</summary>
    public static long QueueLength { get; set; }

    /// <summary>L1. Picoseconds per Vehicle with the two constant-denominator divisions removed.</summary>
    public static long ReciprocalPicoseconds { get; set; }

    /// <summary>L2. Picoseconds per Vehicle at the self-consistent network rung.</summary>
    public static long NetworkPicoseconds { get; set; }

    /// <summary>L2. Vehicles in the network at that rung.</summary>
    public static long NetworkVehicles { get; set; }

    /// <summary>L2. Picoseconds per Vehicle with Overlaps exchanged by cursor.</summary>
    public static long OverlapPicoseconds { get; set; }

    /// <summary>L2. Overlaps per Lane the figure above was taken at.</summary>
    public static long OverlapsPerLane { get; set; }

    /// <summary>L3. Picoseconds per Vehicle promoted.</summary>
    public static long PromotePicoseconds { get; set; }

    /// <summary>L3. Picoseconds per Vehicle demoted.</summary>
    public static long DemotePicoseconds { get; set; }
}

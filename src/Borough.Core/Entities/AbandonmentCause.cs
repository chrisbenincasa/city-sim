namespace Borough.Core.Entities;

/// <summary>
/// Why the city abandoned a Building, as <see cref="BuildingTable.AbandonedCause"/> stores it.
/// </summary>
public enum AbandonmentCause : byte
{
    /// <summary>Not abandoned, or abandoned before the cause was recorded.</summary>
    Unrecorded = 0,

    /// <summary>The premises' own Rules failed for <c>condemn_after_days</c>.</summary>
    Condemned = 1,

    /// <summary>Nobody lived here for <c>abandoned_when_empty_after_days</c>.</summary>
    StoodEmpty = 2,

    /// <summary>No road reached an Outside Connection for <c>abandoned_after_cut_off_days</c>.</summary>
    CutOff = 3,

    /// <summary>A disaster emptied it.</summary>
    Disaster = 4,
}

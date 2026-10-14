namespace Borough.Core.Entities;

/// <summary>
/// How a Citizen's most recent run of failed outbound commutes ended, as
/// <see cref="CitizenTable.CommuteEpisodeOutcome"/> stores it.
/// </summary>
public enum CommuteEpisodeOutcome : byte
{
    /// <summary>No run has ended since the Citizen was created.</summary>
    None = 0,

    /// <summary>An outbound commute completed before the deadline.</summary>
    Recovered = 1,

    /// <summary>The deadline passed and the Citizen lost the job.</summary>
    Dismissed = 2,
}

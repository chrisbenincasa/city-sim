namespace Borough.Core.Rules;

/// <summary>
/// The trade a targeted Policy applies to — <c>plans/0072</c> D28.
/// </summary>
public static class TradeKind
{
    /// <summary>
    /// The Policy names no trade and applies to every member of its subject population.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>It is 255 and not 0 so that a zero-filled definition reaches NOBODY rather than
    /// everybody.</b> Declared trades are numbered from 1 and 0 already means <em>no trade</em>, so
    /// zero is unused and would have served — but <c>default(PolicyDefinition)</c> zeroes this field,
    /// and a sentinel of zero would turn every zero-initialised definition into a Policy aimed at the
    /// whole city. ***A charge is the one to picture***: it would levy every Business in the world
    /// and read, in the file and in the panel alike, as a targeting decision somebody made. At 255 the
    /// same mistake matches no Business at all. The loader refuses a Ruleset declaring 255 or more
    /// Business kinds, so the highest trade any world can hold is 254.
    /// </remarks>
    public const byte Any = 255;
}

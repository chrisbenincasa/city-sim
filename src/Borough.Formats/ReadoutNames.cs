namespace Borough.Formats;

using Borough.Core.Rules;

/// <summary>
/// The names a Ruleset author writes for the simulation's declared Readouts.
/// </summary>
/// <remarks>
/// <para>
/// <b>The set is declared in <c>Borough.Core.Rules.Readout</c>; only the spelling lives here.</b>
/// <c>adr/0002</c> forbids the core from holding a string a human reads, and a Readout's name is
/// exactly that — so the enum is the declaration and this table is its vocabulary. The split is
/// <c>adr/0048</c>'s: names are resolved to ids where the file is parsed, and nothing but integers
/// crosses into the core.
/// </para>
/// <para>
/// <b>Drift between the two is a test, not a runtime validator</b>, for the same reason
/// <c>adr/0048</c> gives: a second validator is a second error surface. The loader refuses an unknown
/// <em>name</em>, <see cref="Readouts.Read"/> refuses an unknown <em>id</em>, and a test asserts the
/// two sets are the same one. Adding an enum member without a name here fails that test rather than
/// producing a Readout no Ruleset can reach.
/// </para>
/// </remarks>
public static class ReadoutNames
{
    // Not a Dictionary: 05 §4 lint 3 bans enumerating one, and a handful of entries scans faster
    // than hashing. The array is also what gives Declared a stable, sorted-by-declaration order.
    private static readonly (string Name, Readout Readout)[] Table =
    [
        ("occupancy", Readout.Occupancy),
    ];

    /// <summary>Every declared name, comma-separated, for a refusal to quote back at the author.</summary>
    public static string Declared => string.Join(", ", Table.Select(entry => entry.Name));

    /// <summary>Resolves a Ruleset's Readout name to the id the core understands.</summary>
    /// <param name="name">The name as written in the file.</param>
    /// <param name="id">The resolved id, or <see cref="ReadoutId.None"/>.</param>
    /// <returns>Whether the name is declared.</returns>
    public static bool TryResolve(string name, out ReadoutId id)
    {
        foreach ((string candidate, Readout readout) in Table)
        {
            if (string.Equals(candidate, name, StringComparison.Ordinal))
            {
                id = new ReadoutId((ushort)readout);
                return true;
            }
        }

        id = ReadoutId.None;
        return false;
    }

    /// <summary>The name for a declared Readout, for a report a human reads.</summary>
    public static string NameOf(Readout readout)
    {
        foreach ((string name, Readout candidate) in Table)
        {
            if (candidate == readout)
            {
                return name;
            }
        }

        throw new ArgumentOutOfRangeException(
            nameof(readout),
            readout,
            "no name is declared for this Readout. Borough.Core declares the set and this table "
            + "spells it; the two have drifted, which ReadoutNamesTests exists to prevent.");
    }
}

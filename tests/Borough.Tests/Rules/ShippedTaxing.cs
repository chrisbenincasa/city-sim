namespace Borough.Tests.Rules;

/// <summary>
/// <c>rulesets/taxing.toml</c> as a test fixture, with the targeted catalogue taken back out.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>A shipped demonstration and a unit fixture want opposite things from the same file, and
/// this is where the two are reconciled.</b> <c>taxing.toml</c> now declares changes 8, 9 and 10 —
/// an emissions charge, a 25% profit-tax relief and an employment subsidy — because
/// <c>plans/0072</c>'s catalogue needs a world that exercises all three. Every one of them moves
/// money in a world built from that file, so a test asserting <em>what the profit-tax bands charge</em>
/// or <em>what one subsidy pays</em> against the unedited text is measuring the bands plus a relief,
/// or one subsidy plus another.
/// </para>
/// <para>
/// ⚠ <b>It strips by <c>tool</c> and not by name</b>, so a fourth catalogue Policy added to the file
/// tomorrow is stripped too and neither fixture has to be edited. <c>public_works</c> states no
/// <c>tool</c> and survives, which is what keeps these worlds spending money as they did.
/// </para>
/// <para>
/// ⚠ <b>The alternative was an inline Ruleset and it was refused for the reason both fixtures
/// already state</b>: <c>taxing.toml</c> is the only shipped world declaring <c>[business_tax]</c>
/// and the one a reader will run, and a file authored here would only ever contain what whoever
/// wrote it remembered.
/// </para>
/// </remarks>
internal static class ShippedTaxing
{
    /// <summary>The shipped file's text, as copied beside the test binary.</summary>
    public static string Text() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "taxing.toml"));

    /// <summary>
    /// The same text with every <c>[[policy]]</c> stating a <c>tool</c> removed.
    /// </summary>
    /// <param name="toml">A Ruleset's text.</param>
    public static string WithoutCatalogue(string toml)
    {
        ArgumentNullException.ThrowIfNull(toml);

        string[] lines = toml.Split('\n');
        List<string> kept = [];
        int at = 0;

        while (at < lines.Length)
        {
            if (lines[at].Trim() != "[[policy]]")
            {
                kept.Add(lines[at]);
                at++;
                continue;
            }

            int end = at + 1;

            while (end < lines.Length
                && !lines[end].StartsWith('[')
                && !lines[end].StartsWith("# ===", StringComparison.Ordinal))
            {
                end++;
            }

            bool catalogued = false;

            for (int line = at; line < end; line++)
            {
                if (lines[line].StartsWith("tool ", StringComparison.Ordinal)
                    || lines[line].StartsWith("tool=", StringComparison.Ordinal))
                {
                    catalogued = true;
                    break;
                }
            }

            if (catalogued)
            {
                // The comment block introducing it goes with it, so the remaining file does not
                // describe a Policy that is no longer in it.
                while (kept.Count > 0 && kept[^1].StartsWith('#'))
                {
                    kept.RemoveAt(kept.Count - 1);
                }
            }
            else
            {
                for (int line = at; line < end; line++)
                {
                    kept.Add(lines[line]);
                }
            }

            at = end;
        }

        return string.Join('\n', kept);
    }
}

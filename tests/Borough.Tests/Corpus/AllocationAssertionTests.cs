using System.Text.RegularExpressions;

namespace Borough.Tests.Corpus;

/// <summary>
/// The eight exact-equality allocation assertions, and the one call that makes them countable.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b><c>plans/0002</c> §B names this set and named it wrongly for months</b> — <i>"there are
/// eight … and only the first spelling was ever counted"</i>. The sites wrote the same property two
/// ways, <c>Assert.Equal(before, after)</c> and <c>Assert.Equal(0, after - before)</c>, so a grep for
/// either found half of them and the row said <b>four</b>. ***A property asserted in two spellings is
/// a property nothing can enumerate***, and a ledger row that names a machine has to be able to
/// enumerate the machine.
/// </para>
/// <para>
/// <b>So the count is asserted here rather than restated in prose.</b> The row may now say eight and
/// be checked, and a ninth site added without going through <see cref="AllocationProbe.Check"/> goes
/// red with a message saying which file it is in.
/// </para>
/// <para>
/// ⚠ <b>Nothing here calls <see cref="AllocationProbe.Check"/>, and the omission is the point.</b>
/// A test that called it would appear in <c>alloc-probe.csv</c> as a **ninth reading**, and
/// <c>plans/0002</c> §B's arithmetic is *eight sites × N runs* — so a synthetic row breaks the
/// denominator of the open question this class exists to serve. The happy path is therefore left
/// untested here and is covered ~700 times over by every real run in the file.
/// ***A test for an instrument must not appear in the instrument's output.***
/// </para>
/// </remarks>
public sealed class AllocationAssertionTests
{
    /// <summary>
    /// <b>The diagnostic fires, and its message names the file the evidence went to.</b>
    /// </summary>
    /// <remarks>
    /// <c>CLAUDE.md</c>'s rule that every diagnostic ships with a test that writes the violation and
    /// watches it fire. ⚠ <b>The message is the point of the method</b>, not the throw: a firing
    /// <em>already</em> failed the suite before this change, and what was missing was anything telling
    /// the reader that the run had just written the sample <c>plans/0002</c> §B was waiting for.
    /// </remarks>
    [Fact]
    public void A_non_zero_reading_throws_and_names_the_probe_file()
    {
        // ⚠ Explain rather than Check, and the distinction is not stylistic: Check RECORDS, so a
        // test calling it with a fabricated delta would append a synthetic firing to the evidence
        // file on every run. Six real samples are on record; one fake per run buries them.
        string message = AllocationProbe.Explain(
            "AllocationAssertionTests.A_deliberate_firing", 4_096, 0, 0, 0);

        Assert.Contains("4096 bytes", message, StringComparison.Ordinal);
        Assert.Contains("alloc-probe.csv", message, StringComparison.Ordinal);
        Assert.Contains("plans/0002", message, StringComparison.Ordinal);

        // The band that separates the intermittent from a regression, because a reader who does not
        // know it will read any firing as a bug in the code under test.
        Assert.Contains("8,192", message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 🔴 <b>Every allocation assertion goes through the one call, so the set can be counted.</b>
    /// </summary>
    /// <remarks>
    /// ⚠ <b>It counts <see cref="AllocationProbe.Check"/> call sites and refuses the raw spellings.</b>
    /// A site that measured the counter and asserted on it by hand would be invisible to
    /// <c>plans/0002</c> §B's machine, which is the failure this test exists to make impossible
    /// rather than merely unlikely.
    /// </remarks>
    [Fact]
    public void Every_allocation_assertion_goes_through_the_probe()
    {
        var checks = new List<string>();
        var raw = new List<string>();

        foreach (string file in Directory.EnumerateFiles(Tree, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal)
                || Path.GetFileName(file) == "AllocationProbe.cs"
                || Path.GetFileName(file) == "AllocationAssertionTests.cs")
            {
                continue;
            }

            string text = File.ReadAllText(file);
            string name = Path.GetFileName(file);

            checks.AddRange(
                Regex.Matches(text, @"AllocationProbe\.Check\(").Select(_ => name));

            if (!text.Contains("GetAllocatedBytesForCurrentThread", StringComparison.Ordinal))
            {
                continue;
            }

            // A site that reads the counter and then asserts on it without going through Check.
            foreach (Match match in Regex.Matches(
                text, @"Assert\.\w+\([^;]*\bafter\b[^;]*\);"))
            {
                raw.Add($"{name}: {match.Value.Trim()}");
            }
        }

        Assert.True(
            raw.Count == 0,
            "an allocation assertion is written by hand rather than through AllocationProbe.Check, "
            + "so plans/0002 §B's machine cannot see it and a firing there records no sample:\n  "
            + string.Join("\n  ", raw));

        Assert.Equal(8, checks.Count);
    }

    /// <summary>The test tree, found from the assembly's own location.</summary>
    private static string Tree
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory is not null && directory.Name != "Borough.Tests")
            {
                directory = directory.Parent;
            }

            Assert.NotNull(directory);

            return directory.FullName;
        }
    }
}

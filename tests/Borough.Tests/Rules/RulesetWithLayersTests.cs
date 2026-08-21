namespace Borough.Tests.Rules;

using System.Reflection;
using System.Text.RegularExpressions;
using Borough.Core.Rules;

/// <summary>
/// <see cref="Ruleset.WithLayers"/> carries every property this class has, and this is what holds it
/// to them.
/// </summary>
/// <remarks>
/// <para>
/// <b>The failure it exists for has happened twice.</b> <see cref="Ruleset"/> is a class rather than
/// a record, so <c>with</c> is spelled by hand — and a hand-spelled <c>with</c> makes adding a
/// property a two-site edit whose second site nothing points at. Milestone 10 task 5 found seven
/// missing at once. Milestone 11 task 2 found <c>Parking</c> missing, added a milestone earlier,
/// with the paragraph warning about exactly this already sitting beside the list.
/// </para>
/// <para>
/// <b>What the omission costs is silence.</b> A Ruleset put through <see cref="Ruleset.WithLayers"/>
/// came back with its Parking Shed at <c>ParkingRuleset.None</c> — no refusal, no throw, a city
/// where arrival simply never parks. And <see cref="RulesetShape"/> cannot catch it: that compares
/// <em>structure</em> under <c>adr/0015</c>, so two Rulesets differing only in a radius compare
/// equal, which is correct for its own question and useless for this one.
/// </para>
/// <para>
/// <b>It reads the source rather than round-tripping values</b>, which is
/// <c>RefusalCountTests</c>' shape one level in — code against code where that one is a document
/// against code. The alternative was to set every property to a non-default and assert it survives,
/// and that needs a distinguishable value per property type invented by hand: a fixture that has to
/// be extended in exactly the place this test exists to stop being forgotten. ***A guard maintained
/// the same way as the thing it guards fails the same day.*** The property list comes from
/// reflection, so a new property is covered without anybody touching this file.
/// </para>
/// <para>
/// ⚠ <b>It checks that each name is assigned, not that it is assigned from itself.</b>
/// <c>Roads = Lots</c> would pass. That is a narrower guard than it could be, and it is the whole of
/// the failure actually observed twice — an omission, never a mistyped source.
/// </para>
/// </remarks>
public sealed class RulesetWithLayersTests
{
    /// <summary>The property <see cref="Ruleset.WithLayers"/> exists to replace.</summary>
    private const string Replaced = nameof(Ruleset.Layers);

    [Fact]
    public void Every_settable_property_is_carried_across()
    {
        string body = WithLayersBody();

        string[] missing = [.. typeof(Ruleset)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.SetMethod is not null)
            .Select(property => property.Name)
            .Where(name => name != Replaced)
            .Where(name => !Regex.IsMatch(body, $@"\b{name}\s*=", RegexOptions.None))
            .Order(StringComparer.Ordinal)];

        Assert.True(
            missing.Length == 0,
            $"Ruleset.WithLayers does not carry {string.Join(", ", missing)}. A Ruleset put through "
            + "it would come back with those at their defaults, in silence. Add each to the object "
            + "initialiser -- this is a hand-spelled `with` and the second site is the one nothing "
            + "points at.");
    }

    /// <summary>The object initialiser in <see cref="Ruleset.WithLayers"/>, as text.</summary>
    private static string WithLayersBody()
    {
        string source = File.ReadAllText(
            Path.Combine(RepoRoot(), "src", "Borough.Core", "Rules", "Ruleset.cs"));

        int start = source.IndexOf("public Ruleset WithLayers(", StringComparison.Ordinal);

        Assert.True(
            start >= 0,
            "Ruleset.WithLayers is not there under that name. This test reads it from source, so a "
            + "rename moves the anchor with it.");

        int end = source.IndexOf("};", start, StringComparison.Ordinal);

        Assert.True(end > start, "Ruleset.WithLayers' object initialiser does not close.");

        return source[start..end];
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "docs", "adr")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "no directory above the test assembly contains docs/adr. This test reads the source "
            + "from disk, so it cannot run from a detached output directory.");
    }
}

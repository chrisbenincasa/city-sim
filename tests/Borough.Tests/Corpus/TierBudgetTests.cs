namespace Borough.Tests.Corpus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

/// <summary>
/// No assertion-tier test costs more than <see cref="TierBudget.PerTest"/>.
/// </summary>
/// <remarks>
/// <para>
/// ⚠ <b><c>plans/0032</c> Q2's check, and it is the half that keeps the tiering true.</b> Tagging
/// says what a test is <em>for</em>; this says whether the fast tier is still <em>fast</em>. The
/// failure it exists for is not a mislabelled test but an <b>untagged</b> one — a nine-minute sweep
/// landing in the default tier, where it is green, correct, and quietly the new critical path.
/// ***A tier nothing times degrades silently, because every test in it stays green while it gets
/// slower.***
/// </para>
/// <para>
/// ⚠ <b>It runs last by construction and that is a property of the mechanism, not a schedule.</b>
/// <see cref="TierTimingFramework"/> records a duration when a test <em>finishes</em>, so this test
/// can only see tests that completed before it. What it therefore cannot catch is a slow test that
/// happens to run after it — which is why the number it prints matters as much as the assertion, and
/// why the run instructions in <c>CLAUDE.md</c> name the full Release run as the pre-commit gate
/// rather than this test.
/// </para>
/// </remarks>
public sealed class TierBudgetTests(ITestOutputHelper output)
{
    /// <summary>Nothing in the default tier is instrument-shaped.</summary>
    /// <remarks>
    /// <b>The message names the two exits and neither is <em>raise the budget</em>.</b> A test over
    /// budget is either an instrument that forgot to say so — tag it — or a genuine assertion that
    /// has become too expensive, which is a real regression in the city rather than in the suite.
    /// ***Raising a tripwire to meet the thing that tripped it is how a tripwire stops existing.***
    /// </remarks>
    [Fact]
    [Trait(Tier.Key, Tier.Assertion)]
    public void No_assertion_costs_more_than_its_budget()
    {
        IReadOnlyList<(string Test, TimeSpan Taken)> slowest = TierBudget.Slowest(10);

        if (slowest.Count > 0)
        {
            output.WriteLine("Slowest tests this run:");

            foreach ((string test, TimeSpan taken) in slowest)
            {
                output.WriteLine($"  {taken.TotalSeconds,8:F1}s  {test}{(IsInstrument(test) ? "  [instrument]" : string.Empty)}");
            }
        }

        IReadOnlyList<(string Test, TimeSpan Taken)> over = TierBudget.OverBudget(IsInstrument);

        Assert.True(
            over.Count == 0,
            $"{over.Count} assertion-tier test(s) took longer than {TierBudget.PerTest.TotalMinutes:F0} "
            + "minutes:\n  "
            + string.Join(
                "\n  ",
                over.Select(pair => $"{pair.Taken.TotalMinutes:F1}m  {pair.Test}"))
            + "\n\nEither it is an instrument -- it produces a figure for a document rather than "
            + "failing when the city changes -- in which case add [Trait(Tier.Key, Tier.Instrument)] "
            + "and record its number where the document that quotes it lives; or it is a real "
            + "assertion that has become this expensive, which is a regression in the city and not "
            + "in the suite. Raising the budget is neither.");
    }

    /// <summary>Whether a test opted out of the budget, by name.</summary>
    /// <remarks>
    /// ⚠ <b>Resolved from the <em>type</em> rather than from the recorded string</b>, because the
    /// framework records a display name and the trait lives on the member. Method first and class
    /// second, matching <see cref="TierDeclarationTests"/> and xUnit's own precedence: the case that
    /// needs it is one instrument inside a class of assertions, which
    /// <c>ParkingArrivalStreamTests</c> is.
    /// </remarks>
    private static bool IsInstrument(string test)
    {
        int split = test.LastIndexOf('.');

        if (split <= 0)
        {
            return false;
        }

        string typeName = test[..split];
        string methodName = test[(split + 1)..];

        Type? type = typeof(TierBudgetTests).Assembly.GetType(typeName);
        MethodInfo? method = type?.GetMethod(methodName);

        if (method is null)
        {
            return false;
        }

        return Declares(method.GetCustomAttributesData())
            ?? Declares(type!.GetCustomAttributesData())
            ?? false;
    }

    /// <summary>Whether these attributes name a tier, and if so whether it is the instrument one.</summary>
    private static bool? Declares(IEnumerable<CustomAttributeData> attributes)
    {
        string[] tiers =
        [.. attributes
            .Where(attribute => typeof(TraitAttribute).IsAssignableFrom(attribute.AttributeType))
            .Where(attribute => attribute.ConstructorArguments.Count == 2)
            .Where(attribute => attribute.ConstructorArguments[0].Value as string == Tier.Key)
            .Select(attribute => attribute.ConstructorArguments[1].Value as string)
            .Where(value => value is not null)
            .Select(value => value!)];

        return tiers.Length == 0 ? null : tiers.Contains(Tier.Instrument);
    }
}

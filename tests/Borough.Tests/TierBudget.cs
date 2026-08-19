namespace Borough.Tests;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// What the assertion tier is allowed to cost, and the record of what it did cost.
/// </summary>
/// <remarks>
/// <para>
/// ⚠ <b><c>plans/0032</c> Q2's resolution, and it is the half that does the work: <em>the tier is
/// the purpose, the budget is the check.</em></b> Tagging alone cannot hold the line, because the
/// failure being guarded against is not a mislabelled test — it is a <b>slow test nobody labelled at
/// all</b>. A declaration guard asks whether somebody wrote a word down; a budget asks the question
/// that actually matters, which is whether the fast tier is still fast. ***A tier nothing times is a
/// tier that degrades silently, because every test in it stays green while it gets slower.***
/// </para>
/// <para>
/// <b>The default is <see cref="Tier.Assertion"/> by absence, which is what makes this bearable.</b>
/// A new test needs no attribute and is held to the budget automatically; only an
/// <see cref="Tier.Instrument"/> opts out, and opting out is a deliberate act with a name on it. The
/// alternative — every one of 142 files declaring a tier — was refused for the reason
/// <c>adr/0018</c> gives about bespoke infrastructure generally: the friction is paid on every future
/// file, for ever, to protect against a case the budget already catches.
/// </para>
/// <para>
/// ⚠ <b>The budget is a tripwire and not a target.</b> It is set well above the slowest honest
/// assertion so that it fires on a <em>class</em> change — an instrument landing untagged — rather
/// than on a slow afternoon. A budget tight enough to catch a 20% regression would fire on a busy
/// machine, and ***a check that cries wolf is uninstalled by the third person who sees it***.
/// </para>
/// </remarks>
public static class TierBudget
{
    /// <summary>
    /// The longest a single assertion-tier test may take.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Derived from the slowest honest assertion in the suite rather than chosen.</b> That is
    /// <c>SaveLongRunTests.The_hundred_thousand_Tick_save_run</c>, which the Definition of done
    /// requires — 100,000+ Ticks with no collection and no magnitude trending upward — and which
    /// <c>plans/0032</c> measured at <b>1m34s</b> in Release. The budget is that, doubled and
    /// rounded, so a long-run acceptance test on a loaded machine has room and a nine-minute
    /// parameter sweep does not.
    /// </para>
    /// <para>
    /// ⚠ <b>It is denominated in one test, never in the suite.</b> A suite total cannot be a budget
    /// here, because <c>plans/0030</c> already established that <em>a suite's wall clock is its
    /// longest test and not the sum of its tests</em> — xUnit runs collections in parallel, so a
    /// suite total moves when the machine's core count does. A per-test duration is a property of
    /// the test.
    /// </para>
    /// </remarks>
    public static readonly TimeSpan PerTest = TimeSpan.FromMinutes(4);

    private static readonly ConcurrentDictionary<string, TimeSpan> Recorded = new();

    /// <summary>Records what one test cost. Called by <see cref="TierTimingFramework"/>.</summary>
    public static void Record(string test, TimeSpan taken) => Recorded[test] = taken;

    /// <summary>Assertion-tier tests that ran over budget, slowest first.</summary>
    public static IReadOnlyList<(string Test, TimeSpan Taken)> OverBudget(
        Func<string, bool> isInstrument) =>
        [.. Recorded
            .Where(pair => pair.Value > PerTest)
            .Where(pair => !isInstrument(pair.Key))
            .Select(pair => (pair.Key, pair.Value))
            .OrderByDescending(pair => pair.Value)];

    /// <summary>Everything timed so far, slowest first. The instrument this tiering is for.</summary>
    public static IReadOnlyList<(string Test, TimeSpan Taken)> Slowest(int take) =>
        [.. Recorded
            .Select(pair => (pair.Key, pair.Value))
            .OrderByDescending(pair => pair.Value)
            .Take(take)];
}

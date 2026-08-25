using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Golden;

namespace Borough.Tests.Rules;

/// <summary>
/// <c>adr/0097</c>: a reach failure is counted on the Citizen, and a Space refusal is not remembered
/// at all. Milestone 6 task 3.
/// </summary>
/// <remarks>
/// <para>
/// <b>The lever throughout is the Commute Budget's ceiling rather than the city</b>, and that is
/// forced rather than chosen. Nothing this project can generate refuses a commute at the shipped
/// fifty minutes — <c>EmploymentRungTests</c> records why, and it is the same reason in a different
/// currency: the paved extent is derived from the population, so a bigger fixture is a bigger city
/// with the same commutes in it. Tightening the ceiling is the only way to put a reach refusal in
/// front of the mechanism without inventing a world.
/// </para>
/// <para>
/// ⚠ <b>The unit under test is the <em>occasion</em>, and <c>adr/0097</c>'s title says
/// <em>candidate</em>.</b> Settled the other way when the column was built, for the reason
/// <see cref="CitizenTable.ReachFailures"/> gives; the test that could have caught the wrong choice
/// is <see cref="The_count_is_denominated_in_occasions_rather_than_candidates"/>, and it is the only
/// one here that would fail if the increment moved into the candidate loop.
/// </para>
/// </remarks>
public sealed class ReachFailureTests
{
    /// <summary>Thirty-two passes at <c>[jobs] interval = 32</c>.</summary>
    private const int RunTicks = 1_024;

    private const int Interval = 32;

    /// <summary>As rarely as <see cref="Replay.Trace"/> permits: nothing here reads a State Hash.</summary>
    private const int HashEvery = 1_024;

    /// <summary>
    /// <b>The negative, written down so that it cannot rot</b>, and with the value at which it stops
    /// being true beside it.
    /// </summary>
    /// <remarks>
    /// 5b-bis task 4's precedent, and the reason <c>EmploymentRungTests</c> exists: an assertion that
    /// nothing happens is the only thing that reports the day something starts happening. At the
    /// shipped fifty-minute ceiling the golden fixture's whole population commutes inside the Budget,
    /// so this column is exercised by no committed baseline and by no shipped Ruleset — which is a
    /// fact about the cities this project can build rather than about the mechanism. Measured at
    /// 1,024 Ticks: <c>beyond</c> is 0 and nobody carries a history; at a ten-minute ceiling on the
    /// same world, 44 people do.
    /// </remarks>
    [Fact]
    public void The_shipped_ruleset_leaves_every_citizen_without_a_history()
    {
        Reading shipped = Read(GoldenFixtures.Rules());

        Assert.True(shipped.Seeking > 0, "nobody looked for work, so nothing was exercised.");
        Assert.Equal(0, shipped.Beyond);
        Assert.Equal(0, shipped.Carriers);

        Reading tightened = Read(WithCeiling(10));

        Assert.True(
            tightened.Carriers > 0,
            "a ten-minute ceiling refuses nobody on the golden fixture, so the ceiling has stopped "
            + "being the lever this file turns. Either the paved extent shrank, the walk got faster, "
            + "or the rungs moved.");
    }

    /// <summary>
    /// <b>A history accumulates only where the network refuses, and more of it as the ceiling
    /// tightens.</b>
    /// </summary>
    /// <remarks>
    /// The monotonicity is the assertion worth having: a count that appeared at one ceiling and did
    /// not grow at a tighter one would be consistent with the column being written once by something
    /// that is not the refusal. Measured over 1,024 Ticks — 44, 343 and 483 people carrying a history
    /// at ten, five and three minutes.
    /// </remarks>
    [Fact]
    public void A_tighter_ceiling_leaves_more_people_carrying_a_history()
    {
        Reading ten = Read(WithCeiling(10));
        Reading five = Read(WithCeiling(5));
        Reading three = Read(WithCeiling(3));

        Assert.True(ten.Carriers < five.Carriers);
        Assert.True(five.Carriers < three.Carriers);
    }

    /// <summary>
    /// <b>Nobody who holds a job carries a history</b>, which is the whole of <c>adr/0097</c>'s reset
    /// clause and is exact rather than statistical.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The reset lives in <see cref="World.Employ"/> rather than in the assignment pass, so this
    /// holds for every path into employment and not only for the sampled one. A Citizen whose
    /// workplace is later demolished keeps the zero their employment bought and starts again from it,
    /// which is why the converse is deliberately not asserted: an unemployed Citizen may honestly
    /// carry any count, nought included.
    /// </para>
    /// <para>
    /// ⚠ <b>Checked on the tightened world rather than the shipped one.</b> On the shipped Ruleset
    /// every count is zero, so this assertion would pass over a population that never had a history
    /// to clear — the same vacuity slice 5 task 7 withheld an invariant over.
    /// </para>
    /// </remarks>
    [Fact]
    public void An_employed_citizen_carries_no_history()
    {
        World world = Run(WithCeiling(3), out _);

        int employed = 0;

        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (!world.Citizens.Rows.IsLive(slot)
                || !world.Businesses.Rows.IsValid(world.Citizens.Workplace[slot]))
            {
                continue;
            }

            employed++;

            Assert.Equal(0, world.Citizens.ReachFailures[slot]);
        }

        Assert.True(employed > 0, "nobody holds a job, so the reset was never exercised.");
    }

    /// <summary>
    /// <b>An occasion that met nothing but full employers leaves no memory at all</b>, however many
    /// of them it met.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The other half of <c>adr/0097</c>, and the one a count of <em>joblessness</em> would fail. A
    /// kind declaring <c>jobs = 0</c> makes <c>World.HasJob</c> false for every Building in the
    /// world, so every candidate is refused before the Road Graph is asked anything: 3,298 seeking
    /// occasions, nobody employed, and not one byte written. A mechanism that incremented on
    /// <i>found no job</i> rather than on <i>could not reach one</i> would write 3,298 of them.
    /// </para>
    /// <para>
    /// ⚠ <b>The Shift-start band goes with the posts</b> — <c>adr/0101</c>'s loader refusal is
    /// two-way, so a kind at <c>jobs = 0</c> must not state one. <c>EmploymentTests.Employing</c>
    /// meets the same pairing from the other side.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_space_refusal_leaves_no_memory()
    {
        Reading reading = Read(WithoutPosts());

        Assert.True(reading.Seeking > 0, "nobody looked for work, so nothing was refused.");
        Assert.Equal(0, reading.Employed);
        Assert.Equal(0, reading.Beyond);
        Assert.Equal(0, reading.Carriers);
        Assert.Equal(0, reading.Total);
    }

    /// <summary>
    /// <b>One occasion writes one increment, however many candidates it refused</b> — the
    /// discriminating test, and the only shape here that could have caught the decision going the
    /// other way.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It reads the population after exactly <em>one</em> pass, and that is what makes it exact.</b>
    /// Over a long run the two denominations are hard to tell apart from the standing column, because
    /// employment resets it: a Citizen who accumulated four and then took a job reads as nought either
    /// way, and the erasure is heaviest exactly where the refusals are. After one pass almost nobody
    /// has been erased and nobody has been looked at twice, so the column is the raw thing that was
    /// written.
    /// </para>
    /// <para>
    /// <b>Measured both ways rather than argued.</b> Reverting the increment into the candidate loop
    /// and re-running gives the same 36 carriers with <b>every one of them on 3</b> — the histogram is
    /// a single spike at <c>[jobs] candidates</c>, and at two and four passes it is spikes at 3, 6 and
    /// 9. The mechanism as built gives the same 36 carriers on <b>1</b>, and 1, 2, 3 as the passes
    /// accumulate. ⚠ <b>The set of carriers is identical under both</b>, which is why no test that
    /// asks <em>who</em> has a history can tell them apart, and why the first draft of this test —
    /// comparing summed increments against seeking occasions — passed under the mutation and had to
    /// be thrown away.
    /// </para>
    /// <para>
    /// So the two assertions are: <b>somebody carries exactly one</b>, which a per-candidate counter
    /// cannot produce because its smallest nonzero value is <c>candidates</c>; and <b>nobody carries
    /// as many as <c>candidates</c></b>, which a per-candidate counter violates on its very first
    /// carrier. Neither is a tolerance. The second survives the one thing that could legitimately
    /// push a count past 1 in a single pass — the sample draws slots with replacement, so the same
    /// Citizen can be looked at twice and refused twice, which is two occasions and not one.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_count_is_denominated_in_occasions_rather_than_candidates()
    {
        Ruleset rules = WithCeiling(3);
        int candidates = rules.Jobs.Candidates;

        Assert.True(candidates > 1, "with one candidate a look and a candidate are the same thing.");

        Simulation simulation = Start(rules, out InputLog log);
        World world = simulation.World;

        Replay.Trace(simulation, log, new Ticks(Interval), HashEvery, []);

        int carriers = 0;
        int ones = 0;
        int worst = 0;

        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (!world.Citizens.Rows.IsLive(slot))
            {
                continue;
            }

            int count = world.Citizens.ReachFailures[slot];

            if (count == 0)
            {
                continue;
            }

            carriers++;
            worst = Math.Max(worst, count);

            if (count == 1)
            {
                ones++;
            }
        }

        Assert.True(carriers > 0, "one pass refused nobody, so nothing was measured.");

        Assert.True(
            ones > 0,
            $"{carriers} people carry a history after one pass and not one of them carries exactly "
            + "one, so the smallest thing the mechanism can write is bigger than a single occasion. "
            + "That is what a per-candidate increment looks like.");

        Assert.True(
            worst < candidates,
            $"somebody carries {worst} after a single pass against `candidates` of {candidates}, so "
            + "one look is writing one byte per candidate rather than one per occasion. adr/0097's "
            + "title says candidate; CitizenTable.ReachFailures says why the build says occasion.");
    }

    /// <summary>
    /// <b>The count stops at its width instead of wrapping.</b>
    /// </summary>
    /// <remarks>
    /// <c>adr/0003</c>'s no-unbounded-magnitude rule applied to a tally. A wrapping counter would put
    /// a Citizen with the longest history in the corpus at nought, which is the one failure that
    /// would read as the mechanism working. ⚠ <b>No run can reach this</b> — a Citizen is looked at
    /// roughly twice a Day at the shipped <c>revisit_ticks</c>, so the ceiling is on the order of
    /// 32,000 Days against a campaign of 562 — which is deliberate, and is why the saturation point
    /// is a wrap guard rather than a bound anybody chose. It is asserted directly here because
    /// nothing else can assert it at all.
    /// </remarks>
    [Fact]
    public void The_count_saturates_rather_than_wrapping()
    {
        World world = Solitary();

        world.Citizens.ReachFailures[0] = ushort.MaxValue - 1;

        world.RecordReachFailure(0);

        Assert.Equal(ushort.MaxValue, world.Citizens.ReachFailures[0]);

        world.RecordReachFailure(0);

        Assert.Equal(ushort.MaxValue, world.Citizens.ReachFailures[0]);
    }

    /// <summary>What one run of a Ruleset leaves behind, in the four numbers this file reads.</summary>
    private readonly record struct Reading(
        int Carriers, long Total, long Beyond, long Seeking, long Employed);

    private static Reading Read(Ruleset rules)
    {
        World world = Run(rules, out EmploymentActivity activity);

        int carriers = 0;
        long total = 0;

        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (!world.Citizens.Rows.IsLive(slot))
            {
                continue;
            }

            int count = world.Citizens.ReachFailures[slot];

            total += count;

            if (count > 0)
            {
                carriers++;
            }
        }

        return new Reading(
            carriers, total, activity.Beyond.Sum, activity.Seeking.Sum, activity.Employed.Sum);
    }

    private static World Run(Ruleset rules, out EmploymentActivity activity)
    {
        Simulation simulation = Start(rules, out InputLog log);

        Replay.Trace(simulation, log, new Ticks(RunTicks), HashEvery, []);

        activity = simulation.Employment.Drain();

        return simulation.World;
    }

    private static Simulation Start(Ruleset rules, out InputLog log)
    {
        InputLogBuilder builder = new(
            GoldenFixtures.Seed,
            new WorldConfiguration(GoldenFixtures.Population),
            GoldenFixtures.RulesetHash);

        builder.Append(new Ticks(0), new Command(CommandKind.Populate, default, default));

        log = builder.Build();

        return Replay.Start(log, rules);
    }

    /// <summary>
    /// The shipped Ruleset with the ceiling lowered and the two rungs below it moved out of the way.
    /// </summary>
    /// <remarks>
    /// <c>EmploymentRungTests.WithRungs</c>'s shape and its reasoning: an edit to the shipped file
    /// rather than a Ruleset written here, so that what is under test is the city this repository
    /// has. The two lower rungs go to 1 and 2 because the loader requires three strictly increasing
    /// values, which is what puts the floor under an authorable ceiling at 3.
    /// </remarks>
    private static Ruleset WithCeiling(int ceiling) => Edit(
        ("commute_fast_minutes = 20", "commute_fast_minutes = 1"),
        ("commute_moderate_minutes = 40", "commute_moderate_minutes = 2"),
        ("commute_budget_minutes = 50", $"commute_budget_minutes = {ceiling}"));

    /// <summary>The shipped Ruleset with every post deleted, and the Shift band with them.</summary>
    private static Ruleset WithoutPosts() => Edit(
        ("jobs = 8", "jobs = 0"),
        ("shift_start_earliest_hour = 6", ""),
        ("shift_start_latest_hour   = 10", ""));

    private static Ruleset Edit(params (string Key, string Replacement)[] edits)
    {
        string toml = File.ReadAllText(GoldenFixtures.RulesetPath);

        foreach ((string key, string replacement) in edits)
        {
            Assert.Contains(key, toml, StringComparison.Ordinal);
            toml = toml.Replace(key, replacement, StringComparison.Ordinal);
        }

        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    /// <summary>One Citizen in one Building, which is all the saturation check needs.</summary>
    private static World Solitary()
    {
        const string Toml = """
            [[resource]]
            name = "sundries"
            family = "good"

            [[building]]
            name = "dwelling"
            occupants = 3
            bins = [
                { resource = "sundries", capacity = 12 },
            ]
            """;

        RulesetLoadResult result = RulesetLoader.Parse(Toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        var world = new World(16, result.Ruleset!);

        Handle<Lot> lot = world.Lots.Create(new Tiles(0), new Tiles(0), zone: 1);
        Handle<Building> building = world.CreateBuilding(
            lot, kind: 1, Ticks.Zero, WorldKey.FromSeed(0x8000_0003UL));
        Handle<Household> household = world.CreateHousehold(building, lifeStage: 0);

        world.CreateCitizen(household, Ticks.Zero);

        return world;
    }
}

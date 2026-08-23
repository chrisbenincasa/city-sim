using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Golden;

namespace Borough.Tests.Tables;

/// <summary>
/// Milestone 8 task 1 — the whole-world rebuild audit, and it involves no save format.
/// </summary>
/// <remarks>
/// <para>
/// <b>The claim every <see cref="Disposition.Derived"/> column makes is that it is a pure function of
/// saved state, and this is where the claim is tested for all of them at once.</b>
/// <see cref="World.RebuildDerived"/> is what a load will call; <c>Rows.FoldAll</c> and
/// <c>Column.Fold</c> are what can read the answer. Both halves have been in the tree for the life of
/// the derived declaration, and between them they measure milestone 8's named risk — <em>a derived
/// column that does not rebuild to the value it had</em> — <b>directly, immediately, and with no save,
/// no reload and no M further Ticks</b>. The Factorio test measures the same risk indirectly, once a
/// wrong value has propagated into saved state, and needs the whole milestone built first. Both are
/// worth having; they are not the same instrument.
/// </para>
/// <para>
/// <b>⚠ The audit corrupts first, and the brief that scoped this task specified the weaker form.</b>
/// <em>Fold, rebuild, fold, assert equal</em> catches a rebuild that produces the wrong value and is
/// <b>vacuous against a column nothing rebuilds at all</b> — an absent rebuild leaves the column
/// untouched, so the two folds agree and the audit reports success. That is the exact failure mode
/// <c>layer_cell.pollution_pass</c> was raised as, so the specified form could not have failed on the
/// one column that motivated it. <b>Clearing every derived column before the rebuild is what makes an
/// absent rebuild indistinguishable from a wrong one</b>, which is the property the test needs.
/// <em>An audit that cannot fail on its own motivating case is an audit of something else.</em>
/// </para>
/// <para>
/// <b>And a cleared column that was already zero is not exercised by this at all</b>, which is slice
/// 10 task 11's lesson — <em>a change that narrows what a run reaches is invisible in it by
/// construction</em> — so coverage is accumulated across every world below and asserted by name at the
/// end. A derived column that no fixture populates is reported rather than passed.
/// </para>
/// </remarks>
public sealed class DerivedRebuildAuditTests
{
    /// <summary>One column's identity and what its storage folded to.</summary>
    private readonly record struct ColumnFold(string Name, Disposition Disposition, ulong Fold);

    /// <summary>
    /// The audit's verdict over one world: which derived columns failed to come back, and which the
    /// world never populated in the first place.
    /// </summary>
    private readonly record struct Audit(string[] Diverged, string[] Unexercised, string[] Derived);

    /// <summary>
    /// Every derived column in a world stepped for a while comes back from a rebuild unchanged.
    /// </summary>
    /// <remarks>
    /// Over a <b>stepped</b> world rather than a freshly built one, per <c>RoadGraph.cs:55-64</c>: a
    /// derived structure reads as <em>absent</em> rather than <em>stale</em> before its first rebuild,
    /// and absent is the state every guard is written against. The Tick counts are three different
    /// cities — one before the Zone Rules have demolished anything, one after, and one past a whole
    /// Day so the commute roster has been through every bucket.
    /// </remarks>
    [Theory]
    [InlineData(64)]
    [InlineData(512)]
    [InlineData(2048)]
    public void Every_derived_column_rebuilds_to_the_value_it_had(int ticks)
    {
        Audit audit = Run(Stepped(ticks));

        Assert.Empty(audit.Diverged);
    }

    /// <summary>The same claim over the golden fixture, which is built by hand rather than stepped.</summary>
    /// <remarks>
    /// It is the world every hash baseline is recorded from, and it is built to exercise the fold's
    /// awkward cases — both intrusive lists populated, a Household and a Citizen destroyed, handle
    /// columns pointing across tables — which is exactly the shape a rebuild is most likely to get
    /// wrong.
    /// </remarks>
    [Fact]
    public void Every_derived_column_rebuilds_to_the_value_it_had_on_the_golden_fixture()
    {
        Audit audit = Run(GoldenFixtures.Build());

        Assert.Empty(audit.Diverged);
    }

    /// <summary>
    /// A rebuild writes no saved state, which is the other half of the declaration and is free to
    /// check here.
    /// </summary>
    /// <remarks>
    /// <b>A rebuild that moved a saved column would be a defect the State Hash reports only after a
    /// load</b> — the world would be self-consistent and would have quietly become a different city.
    /// The audit already folds every column; asserting the saved ones did not move costs nothing and
    /// covers a failure nothing else in the suite is watching for.
    /// </remarks>
    [Fact]
    public void Rebuilding_writes_no_saved_state()
    {
        World world = Stepped(512);

        ulong before = world.HashState();
        world.RebuildDerived();

        Assert.Equal(before, world.HashState());
    }

    /// <summary>
    /// Every derived column in the build is exercised by at least one of the worlds above — asserted
    /// by name, because a column no fixture populates is a column this audit does not cover.
    /// </summary>
    /// <remarks>
    /// <b>The count is asserted too, and upward as well as downward.</b> A new derived column that no
    /// world here populates fails this test on the day it is declared, which is the point: the
    /// alternative is coverage that quietly shrinks while every test stays green — slice 10 task 11's
    /// finding, and the reason milestone 8 exists third rather than sixteenth. ⚠ <b>The scoping
    /// survey's hand count said 28 derived columns; there were 33, and there are 32 now that
    /// <c>layer_cell.pollution_pass</c> is <see cref="Disposition.Scratch"/></b>. This test is what
    /// produces the number, so nobody has to trust a hand count again.
    /// </remarks>
    [Fact]
    public void Every_derived_column_is_exercised_by_some_world()
    {
        Audit[] audits =
        [
            Run(Stepped(64)),
            Run(Stepped(512)),
            Run(Stepped(2048)),
            Run(Severed()),
            Run(GoldenFixtures.Build()),
        ];

        string[] all = audits[0].Derived;
        Assert.All(audits, audit => Assert.Equal(all, audit.Derived));

        List<string> never = [];
        foreach (string column in all)
        {
            bool exercised = false;
            foreach (Audit audit in audits)
            {
                if (!Array.Exists(audit.Unexercised, name => name == column))
                {
                    exercised = true;
                    break;
                }
            }

            if (!exercised)
            {
                never.Add(column);
            }
        }

        // The one column no world can exercise, named with the test that covers it instead rather
        // than excused. adr/0007's hole: road_segment.fidelity is written to a constant zero
        // everywhere in src/ and tests/, so a zeroing corruption cannot move it and no fixture can
        // make it non-zero until milestone 22. Road_segment_fidelity_is_still_rebuilt_to_a_constant_zero
        // covers it with a NON-zero fill, which is strictly stronger than anything this loop can do.
        Assert.Equal(["road_segment.fidelity"], never);

        // 33 columns were declared Derived before milestone 8 task 1 and 32 after, because
        // layer_cell.pollution_pass became Disposition.Scratch. Both numbers are the machine's; the
        // scoping survey's hand count said 28 across 9 tables, and it is the count that was wrong
        // rather than the tables. Asserting it pins the coverage in both directions -- a new derived
        // column no world here populates fails this test on the day it is declared.
        //
        // 35 as of milestone 7, which brought three: building.car_park and car_park.capacity, both
        // exercised the day they were declared, and car_park.segment_next, which was not.
        //
        // ⚠ The third is why this assertion exists. It is the element side of the Parking Shed's
        // supply index, and it was declared Derived while the structure deriving it -- a
        // CarParkResidency -- lived in the test fixtures rather than in the World. So RebuildDerived
        // did not rebuild it, and every shed in a loaded world would have come back empty. Nothing
        // read the column yet, so no other test in either milestone could have failed; the two
        // branches were green apart and red together. ***A structure that lives outside the world is
        // not derived state however it is declared***, and the coverage assertion earned itself on
        // the first foreign column it ever saw.
        //
        // 38 as of the milestone 7 / milestone 10 merge, 2026-08-19: milestone 10 task 4b brought
        // three more -- building.business_head, building.business_tail and business.building_next --
        // and the two sets are disjoint, so the counts add. ⚠ Each branch wrote 35 and each was right
        // about its own tree, which is the sentence above happening to the assertion rather than to a
        // column: ***a count of a whole is a fact no single branch holds***, and only the merge can
        // take it.
        //
        // 40 as of milestone 25 task 1, 2026-08-23: adr/0143 makes household.balance and
        // business.balance DERIVED, where both were saved. An Occupant now owns a LIST of Bins --
        // adr/0141, a Bin belongs to the Occupant whose leaving would empty it -- and the balance is
        // one entry in it, so a second saved handle to the same Bin would be two saved facts that can
        // disagree. ⚠ The list itself is SAVED and adds nothing here: a tenant-owned Bin names no
        // owner, so its membership is recoverable from nothing and it fails this audit's premise
        // rather than passing it.
        Assert.Equal(40, all.Length);
        Assert.Single(ScratchColumns(Stepped(0)));
    }

    /// <summary>
    /// <c>road_segment.fidelity</c> rebuilds to a constant zero, and the day that stops being true is
    /// a red test rather than a divergence.
    /// </summary>
    /// <remarks>
    /// <c>adr/0007</c>'s named hole. <c>RoadGraph.cs:343</c> writes the zero deliberately <em>"rather
    /// than left alone so that a rebuild is idempotent over a column somebody may later start
    /// writing"</em> — this task's whole premise, reached from the other side. So it passes the audit
    /// today <b>by intent</b>, and it stops passing the moment milestone 22 writes real Stress into
    /// it, at which point the rebuild would silently zero a live value. Asserting the constant now is
    /// what turns that day into a failing test instead of a load that quietly loses the field.
    /// </remarks>
    [Fact]
    public void Road_segment_fidelity_is_still_rebuilt_to_a_constant_zero()
    {
        World world = Stepped(512);
        RoadSegmentTable segments = world.Roads.Segments;

        Assert.True(segments.Rows.SlotCount > 0);

        segments.Fidelity.Span.Fill(0xAB);
        world.RebuildDerived();

        foreach (byte fidelity in segments.Fidelity.Span)
        {
            Assert.Equal(0, fidelity);
        }
    }

    /// <summary>
    /// The obligation <see cref="Disposition.Scratch"/> carries: nothing reads a scratch column
    /// outside the phase that wrote it, so its content cannot reach the State Hash.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is what the rebuild audit's exemption is paid for with.</b> Skipping a column because
    /// its declaration says to is only honest if the declaration is checked, and an exemption list in
    /// a test could never assert this — it would say <em>do not look at this one</em> where this says
    /// <em>it cannot matter what is in it</em>. Filling the column with garbage on every Tick and
    /// getting the same hash trace is the whole claim.
    /// </para>
    /// <para>
    /// <b>The scratch set is asserted by name so this test cannot rot.</b> A second scratch column
    /// declared later fails here rather than being silently unexercised, which forces its author to
    /// extend the fill — the typed write below is deliberate, since a type-erased one is milestone 8
    /// task 2's and this task must not depend on the format.
    /// </para>
    /// </remarks>
    [Fact]
    public void Scratch_content_cannot_reach_the_state_hash()
    {
        Assert.Equal(["layer_cell.pollution_pass"], ScratchColumns(Stepped(0)));

        const int Ticks = 256;

        ulong[] clean = Trace(Ticks, scribble: false);
        ulong[] scribbled = Trace(Ticks, scribble: true);

        Assert.Equal(clean, scribbled);
    }

    /// <summary>Steps a real city, so the derived structures are populated and have been maintained.</summary>
    private static World Stepped(int ticks)
    {
        var key = WorldKey.FromSeed(GoldenFixtures.Seed);
        var world = new World(GoldenFixtures.Population, GoldenFixtures.Rules());

        var simulation = new Simulation(world, key)
        {
            // O(world) twice per Tick against a phase meant to be O(woken); the guard has its own
            // tests and this one is about the rebuild.
            VerifyDecideWritesNothing = false,
        };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        for (int tick = 0; tick < ticks; tick++)
        {
            simulation.Step(default);
        }

        return world;
    }

    /// <summary>
    /// A city the pedestrian network is cut in two by, which is the only world here that puts a
    /// non-zero value in a connectivity label.
    /// </summary>
    /// <remarks>
    /// <b>⚠ The audit's corruption is a zeroing one, so a column whose correct value is all zeroes is
    /// invisible to it — and that is exactly what a connectivity label looks like in a connected
    /// city.</b> <c>RoadConnectivity.Label</c> numbers components from 0, so a city in one piece
    /// labels every live node <c>0</c>: clearing the column changes nothing, rebuilding it changes
    /// nothing, and the audit would report a pass it had not earned. <c>rulesets/severance.toml</c>
    /// exists because <c>minimal.toml</c> cannot demonstrate Severance and that is measured — so the
    /// file written to make a <em>reading</em> possible is the file that makes this <em>test</em>
    /// possible, for the same underlying reason.
    /// </remarks>
    private static World Severed()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Rulesets", "severance.toml");
        RulesetLoadResult loaded = RulesetLoader.Load(path);

        Ruleset rules = loaded.Ruleset
            ?? throw new InvalidOperationException(
                $"{path} was refused, so the severance world cannot be built:\n{loaded.Describe()}");

        var key = WorldKey.FromSeed(GoldenFixtures.Seed);
        var world = new World(GoldenFixtures.Population, rules);

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        // The reading this world exists for. If it ever comes back in one piece the Ruleset has
        // stopped severing, and the coverage assertion above would start passing for the wrong
        // reason -- so it is asserted here rather than assumed.
        Assert.True(
            world.Roads.Connectivity.FootComponents > 1,
            "severance.toml no longer severs, so the connectivity labels are all zero again.");

        return world;
    }

    /// <summary>A hash trace, optionally filling every scratch column with garbage each Tick.</summary>
    private static ulong[] Trace(int ticks, bool scribble)
    {
        var key = WorldKey.FromSeed(GoldenFixtures.Seed);
        var world = new World(GoldenFixtures.Population, GoldenFixtures.Rules());

        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        ulong[] trace = new ulong[ticks];

        for (int tick = 0; tick < ticks; tick++)
        {
            if (scribble)
            {
                // Deliberately not zero and deliberately varying, so a phase that reads this before
                // writing it reads something different on every Tick rather than something stable.
                world.Layers.Cells.PollutionPass.Span.Fill(unchecked(0x5A5A_5A5A + tick));
            }

            simulation.Step(default);
            trace[tick] = world.HashState();
        }

        return trace;
    }

    /// <summary>
    /// Corrupts every derived column, rebuilds, and reports what did not come back.
    /// </summary>
    private static Audit Run(World world)
    {
        ColumnFold[] original = FoldEachColumn(world);

        ClearDerived(world);
        ColumnFold[] cleared = FoldEachColumn(world);

        world.RebuildDerived();
        ColumnFold[] rebuilt = FoldEachColumn(world);

        List<string> diverged = [];
        List<string> unexercised = [];
        List<string> derived = [];

        for (int i = 0; i < original.Length; i++)
        {
            if (original[i].Disposition != Disposition.Derived)
            {
                continue;
            }

            derived.Add(original[i].Name);

            // Clearing a column that was already zero proves nothing about the rebuild, so it is
            // reported rather than counted as a pass.
            if (cleared[i].Fold == original[i].Fold)
            {
                unexercised.Add(original[i].Name);
            }

            if (rebuilt[i].Fold != original[i].Fold)
            {
                diverged.Add(original[i].Name);
            }
        }

        return new Audit([.. diverged], [.. unexercised], [.. derived]);
    }

    /// <summary>Every column of every table, folded on its own so a mismatch has a name.</summary>
    /// <remarks>
    /// <c>Rows.FoldAll</c> folds a whole table into one number, which answers <em>did anything
    /// change</em> and cannot answer <em>what</em>. A per-column fold is the same arithmetic read at
    /// the granularity the audit has to report at.
    /// </remarks>
    private static ColumnFold[] FoldEachColumn(World world)
    {
        List<ColumnFold> folds = [];

        foreach (Rows table in world.Tables)
        {
            foreach (Column column in table.Columns)
            {
                ulong hash = 0;
                column.Fold(ref hash, table.SlotCount);
                folds.Add(new ColumnFold(
                    $"{table.Name}.{column.Name}", column.Disposition, hash));
            }
        }

        return [.. folds];
    }

    /// <summary>Zeroes every derived column, leaving saved and scratch storage alone.</summary>
    private static void ClearDerived(World world)
    {
        foreach (Rows table in world.Tables)
        {
            foreach (Column column in table.Columns)
            {
                if (column.Disposition != Disposition.Derived)
                {
                    continue;
                }

                for (int slot = 0; slot < table.SlotCount; slot++)
                {
                    column.Clear(slot);
                }
            }
        }
    }

    /// <summary>The scratch columns declared anywhere in the world, by qualified name.</summary>
    private static string[] ScratchColumns(World world)
    {
        List<string> names = [];

        foreach (Rows table in world.Tables)
        {
            foreach (Column column in table.Columns)
            {
                if (column.Disposition == Disposition.Scratch)
                {
                    names.Add($"{table.Name}.{column.Name}");
                }
            }
        }

        return [.. names];
    }
}

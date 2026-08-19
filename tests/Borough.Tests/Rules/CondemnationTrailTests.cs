using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Tests.Rules;

/// <summary>
/// Milestone 6 task 1: the condemnation trail, and <c>adr/0006</c>'s sink built with the collection.
/// </summary>
/// <remarks>
/// <para>
/// <b>The claim being tested is <c>02 §9</c>'s hardest one</b> — <em>for a Lot, why it is vacant; not
/// "vacant", but why</em> — and what makes it need a table at all is that the answer has a lifetime of
/// one line. <see cref="World.DestroyBuilding"/> frees the Rule Instances that hold the condition and
/// the Building row that holds the kind, so a world that did not copy them cannot be asked afterwards.
/// These tests are therefore about what a diagnosis can still read <em>after</em> the subject has
/// ceased to exist.
/// </para>
/// <para>
/// <b>The cap is tested by overflowing it rather than by reading the constant back</b>, which is
/// <see cref="RulesetTrailTests"/>'s discipline and the only version that could fail. ⚠ <b>And the
/// aggregate is tested for what it <em>keeps</em>, not only for what it drops</b>: the whole point of
/// folding rather than evicting is that <em>attribution</em> decays to <em>magnitude</em> while the
/// magnitude stays exact for the life of the world.
/// </para>
/// </remarks>
public sealed class CondemnationTrailTests
{
    private const byte Dwelling = 1;
    private const byte Workshop = 2;

    /// <summary>A world with more Lots than the trail can retain, so the cap is reachable.</summary>
    private static World City()
    {
        var world = new World(1_000);

        for (int i = 0; i < CondemnationTrailTable.Retained + 8; i++)
        {
            world.Lots.Create(new Tiles(i), new Tiles(0), zone: 1);
        }

        return world;
    }

    private static Handle<Lot> LotAt(World world, int index) => world.Lots.Rows.At(index);

    /// <summary>
    /// The aggregate is there from world creation, so nothing that reads the trail needs a liveness
    /// branch on slot 0.
    /// </summary>
    [Fact]
    public void A_fresh_world_has_an_aggregate_and_no_entries()
    {
        World world = City();
        CondemnationTrailTable trail = world.CondemnationTrail;

        Assert.Equal(0, trail.Count);
        Assert.Equal(1, trail.Rows.LiveCount);
        Assert.Equal(0, trail.CondemnationsRecorded());
        Assert.Equal(0, trail.Condemnations[CondemnationTrailTable.AggregateSlot]);
    }

    /// <summary>
    /// What the Building took with it is what the trail had to copy: the kind and the condition are
    /// gone from the world by the time anybody asks, and the Lot is not.
    /// </summary>
    [Fact]
    public void An_entry_keeps_the_place_the_kind_and_the_cause()
    {
        World world = City();
        CondemnationTrailTable trail = world.CondemnationTrail;
        Handle<Lot> lot = LotAt(world, 4);

        trail.Record(new Ticks(512), lot, Workshop, new ConditionId(7));

        Assert.Equal(1, trail.Count);

        int slot = trail.EntrySlot(0);

        Assert.Equal(new Ticks(512), trail.Tick[slot]);
        Assert.Equal(lot, trail.Lot[slot]);
        Assert.Equal(Workshop, trail.Kind[slot]);
        Assert.Equal(new ConditionId(7), trail.Condition[slot]);
        Assert.Equal(1, trail.Condemnations[slot]);
        Assert.Equal(1, trail.CondemnationsRecorded());
    }

    /// <summary>
    /// A condemnation with no authored <c>on_fail</c> chain is recorded rather than filtered, which is
    /// the deliberate difference from <see cref="RulesetTrailTable"/>.
    /// </summary>
    /// <remarks>
    /// That trail refuses a transition that cost nothing, because an all-zero entry would push a real
    /// one out of a window sized for diagnosis. A condemnation is never nothing — the Building is gone
    /// either way — and one the Ruleset cannot explain is precisely the case a player most needs to
    /// see, because the alternative is a Building that vanishes with no entry anywhere.
    /// </remarks>
    [Fact]
    public void A_condemnation_with_no_named_condition_is_still_recorded()
    {
        World world = City();
        CondemnationTrailTable trail = world.CondemnationTrail;

        trail.Record(new Ticks(64), LotAt(world, 0), Dwelling, ConditionId.None);

        Assert.Equal(1, trail.Count);
        Assert.Equal(ConditionId.None, trail.Condition[trail.EntrySlot(0)]);
        Assert.Equal(1, trail.CondemnationsRecorded());
    }

    /// <summary>Entries are dense from slot 1 and oldest-first, which is what makes the trail a timeline.</summary>
    /// <remarks>
    /// Index order is hash composition order (<c>02 §8</c>), so a ring buffer with a cursor would make
    /// two worlds that survived the same condemnations hash differently depending on where the cursor
    /// happened to be. The slide is what buys chronology, and this asserts it below the cap.
    /// </remarks>
    [Fact]
    public void Entries_are_dense_and_chronological_below_the_cap()
    {
        World world = City();
        CondemnationTrailTable trail = world.CondemnationTrail;

        for (int i = 0; i < 5; i++)
        {
            trail.Record(new Ticks((ulong)(100 + i)), LotAt(world, i), Dwelling, new ConditionId(1));
        }

        Assert.Equal(5, trail.Count);

        for (int i = 0; i < 5; i++)
        {
            Assert.Equal(i + 1, trail.EntrySlot(i));
            Assert.Equal(new Ticks((ulong)(100 + i)), trail.Tick[trail.EntrySlot(i)]);
            Assert.Equal(LotAt(world, i), trail.Lot[trail.EntrySlot(i)]);
        }
    }

    /// <summary>
    /// <b>The aggregate's count carries past what an <c>int</c> holds.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>This is the only column in the project with no sink at all, so it is the only one whose
    /// width is set by a <em>campaign length</em> rather than by a city size.</b> Every other column
    /// here is overwritten as entries slide down; this one only grows, for the life of the world, and
    /// that is what <em>attribution decays to magnitude</em> means. Measured 2026-08-17 over 100,000
    /// Ticks: a 1,000-Citizen city condemns <b>0.031 Buildings a Tick</b>, which scaled by
    /// <c>World</c>'s own Lot allocation is <b>~57 a Tick</b> at a million — so 32 bits runs out after
    /// roughly <b>162 hours</b> of play at the 4× target. ***A counter with no sink is denominated in
    /// the life of the world, not in the size of the city.***
    /// </para>
    /// <para>
    /// <b>The overflow is seeded rather than reached, and it has to be.</b> Recording two billion
    /// condemnations is not a test anybody can run, so the aggregate is set one short of the boundary
    /// and pushed over it — which tests the arithmetic and the storage and says nothing about the
    /// rate, because the rate is measured elsewhere and quoted above. Without this the width is
    /// <b>decorative</b>: <c>long</c> and <c>int</c> behave identically on every number any test in
    /// this suite produces, so nothing else here could tell them apart.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_aggregate_carries_a_count_wider_than_an_int()
    {
        World world = City();
        CondemnationTrailTable trail = world.CondemnationTrail;

        // Fill the window first, so that the next Record aggregates rather than appending -- the
        // seeded count has to travel through the real fold, not sit in the column being read back.
        for (int i = 0; i <= CondemnationTrailTable.Retained; i++)
        {
            trail.Record(
                new Ticks((ulong)(1_000 + i)),
                LotAt(world, i % (CondemnationTrailTable.Retained + 8)),
                Dwelling,
                new ConditionId(3));
        }

        trail.Condemnations[CondemnationTrailTable.AggregateSlot] = int.MaxValue;

        trail.Record(new Ticks(9_000), LotAt(world, 0), Dwelling, new ConditionId(3));

        long carried = trail.Condemnations[CondemnationTrailTable.AggregateSlot];

        Assert.Equal(int.MaxValue + 1L, carried);
        Assert.True(carried > 0, "the aggregate wrapped negative, so the count is not 64 bits wide.");

        Assert.Equal(
            int.MaxValue + 1L + CondemnationTrailTable.Retained, trail.CondemnationsRecorded());
    }

    /// <summary>
    /// The row count stops climbing at <see cref="CondemnationTrailTable.Retained"/> + 1, and the total
    /// stays exact for ever. A trail that grew would be <c>adr/0006</c>'s defect arriving through the
    /// mechanism written to diagnose it.
    /// </summary>
    [Fact]
    public void The_trail_caps_and_the_total_survives_the_cap()
    {
        const int Condemnations = CondemnationTrailTable.Retained + 97;

        World world = City();
        CondemnationTrailTable trail = world.CondemnationTrail;

        for (int i = 0; i < Condemnations; i++)
        {
            trail.Record(
                new Ticks((ulong)(1_000 + i)),
                LotAt(world, i % (CondemnationTrailTable.Retained + 8)),
                Dwelling,
                new ConditionId(3));

            Assert.Equal(Math.Min(i + 1, CondemnationTrailTable.Retained), trail.Count);
            Assert.Equal(Math.Min(i + 2, CondemnationTrailTable.Retained + 1), trail.Rows.LiveCount);
        }

        Assert.Equal(CondemnationTrailTable.Retained, trail.Count);
        Assert.Equal(CondemnationTrailTable.Retained + 1, trail.Rows.LiveCount);

        // Attribution decays to magnitude: the aggregate carries every entry that aged out, exactly.
        Assert.Equal(Condemnations, trail.CondemnationsRecorded());
        Assert.Equal(
            Condemnations - CondemnationTrailTable.Retained,
            trail.Condemnations[CondemnationTrailTable.AggregateSlot]);

        // The retained window is the newest N, and the aggregate ends where the window begins.
        Assert.Equal(
            new Ticks(1_000 + Condemnations - 1),
            trail.Tick[trail.EntrySlot(CondemnationTrailTable.Retained - 1)]);
        Assert.Equal(
            new Ticks(1_000 + Condemnations - CondemnationTrailTable.Retained - 1),
            trail.Tick[CondemnationTrailTable.AggregateSlot]);
    }

    /// <summary>
    /// The aggregate names no single kind, and says so with the spelling a Building the Ruleset cannot
    /// describe already carries (<c>adr/0057</c>).
    /// </summary>
    [Fact]
    public void The_aggregate_names_no_kind_and_no_place()
    {
        World world = City();
        CondemnationTrailTable trail = world.CondemnationTrail;

        for (int i = 0; i < CondemnationTrailTable.Retained + 2; i++)
        {
            trail.Record(
                new Ticks((ulong)(10 + i)),
                LotAt(world, i % (CondemnationTrailTable.Retained + 8)),
                i % 2 == 0 ? Dwelling : Workshop,
                new ConditionId(2));
        }

        Assert.Equal(0, trail.Kind[CondemnationTrailTable.AggregateSlot]);
        Assert.Equal(ConditionId.None, trail.Condition[CondemnationTrailTable.AggregateSlot]);
        Assert.True(trail.Lot[CondemnationTrailTable.AggregateSlot].IsNone);
    }

    /// <summary>
    /// The trail is saved state, so it is in the State Hash. Declaring a column
    /// <see cref="Rows.Saved{TField}"/> is what puts it in the hash and in the save at once, so there
    /// is no way to have one without the other.
    /// </summary>
    [Fact]
    public void The_trail_folds_into_the_state_hash()
    {
        World world = City();

        ulong before = 0;
        world.CondemnationTrail.Rows.Fold(ref before);

        world.CondemnationTrail.Record(new Ticks(77), LotAt(world, 1), Dwelling, new ConditionId(5));

        ulong after = 0;
        world.CondemnationTrail.Rows.Fold(ref after);

        Assert.NotEqual(before, after);
    }

    /// <summary>
    /// Two worlds that survived the same condemnations agree, which is the property a ring buffer with
    /// a cursor would have cost.
    /// </summary>
    [Fact]
    public void Two_worlds_with_the_same_condemnations_hash_alike()
    {
        static ulong Fold(int condemnations)
        {
            World world = City();

            for (int i = 0; i < condemnations; i++)
            {
                world.CondemnationTrail.Record(
                    new Ticks((ulong)(200 + i)),
                    LotAt(world, i % (CondemnationTrailTable.Retained + 8)),
                    Dwelling,
                    new ConditionId(1));
            }

            ulong hash = 0;
            world.CondemnationTrail.Rows.Fold(ref hash);

            return hash;
        }

        // Below the cap and well past it, so the slide is covered as well as the plain append.
        Assert.Equal(Fold(9), Fold(9));
        Assert.Equal(
            Fold(CondemnationTrailTable.Retained + 40),
            Fold(CondemnationTrailTable.Retained + 40));

        Assert.NotEqual(Fold(9), Fold(10));
    }

    /// <summary>
    /// The trail declares no derived column, so there is nothing for a load to rebuild and nothing that
    /// could rebuild to a value it did not have. That is the residual risk milestone 8 is about, and
    /// this table is deliberately outside it.
    /// </summary>
    [Fact]
    public void Every_column_is_saved_and_cold()
    {
        World world = City();

        ReadOnlySpan<Column> columns = world.CondemnationTrail.Rows.Columns;

        // Through Rows.SavedColumns rather than a filter written here. Milestone 8 task 2 made that
        // the one accessor, because this was the third site to write the same `if` by hand and the
        // save would have been a fourth -- and the set the save writes is the set this asserts.
        Assert.Equal(columns.Length, world.CondemnationTrail.Rows.SavedColumns.Length);

        // The three intrinsic columns Rows declares for itself are not Cold, so only the five this
        // table declares are asserted -- by name, because a count would pass while naming nothing.
        foreach (string name in new[] { "tick", "lot", "condition", "kind", "condemnations" })
        {
            int found = 0;

            foreach (Column column in columns)
            {
                if (column.Name != name)
                {
                    continue;
                }

                found++;
                Assert.Equal(Touch.Cold, column.Touch);
            }

            Assert.Equal(1, found);
        }
    }
}

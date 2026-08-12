using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Tables;

namespace Borough.Tests.Tables;

/// <summary>
/// The State Hash, and the coverage question it exists to answer.
/// </summary>
/// <remarks>
/// <para>
/// <b>The risk this slice retires is a hash with a hole in it.</b> A field that is saved but not
/// hashed is invisible to every tool in the project: two runs diverge on it, the hashes agree, replay
/// reports success, and the save/reload test passes because the field <em>is</em> saved. The oracle
/// certifies a divergence it cannot see.
/// </para>
/// <para>
/// <b>These tests cannot prove coverage, and that is the point.</b> A test for hash coverage has the
/// same blind spot as the thing it tests — it can only check the fields somebody remembered. What
/// proves coverage is the construction: declaring a column is what allocates it, so there is no
/// undeclared column to forget. <c>BOR0901</c> covers the one route around that, and
/// <c>TableLintTests</c> is where it is watched firing. What is left for these tests is the other
/// direction: that the fold actually reads what it was given.
/// </para>
/// </remarks>
public class StateHashTests
{
    private const int Population = 1_000;

    /// <summary>Two identically-built worlds agree. Without this nothing below means anything.</summary>
    [Fact]
    public void The_same_history_produces_the_same_hash()
    {
        Assert.Equal(Build().HashState(), Build().HashState());
    }

    [Fact]
    public void An_empty_world_and_a_populated_one_disagree()
    {
        Assert.NotEqual(new World(Population).HashState(), Build().HashState());
    }

    [Fact]
    public void Changing_a_saved_field_moves_the_hash()
    {
        World world = Build();
        ulong before = world.HashState();

        world.Citizens.Activity[0] = 7;

        Assert.NotEqual(before, world.HashState());
    }

    /// <summary>
    /// A cold column counts as much as a per-Tick one. Household economics is entirely
    /// <see cref="Touch.Cold"/> and entirely <see cref="Disposition.Saved"/>; a hash that skipped it
    /// would go blind precisely where the money is.
    /// </summary>
    [Fact]
    public void Changing_a_cold_saved_field_moves_the_hash()
    {
        World world = Build();
        ulong before = world.HashState();

        world.Households.Savings[0] = new Money(1);

        Assert.NotEqual(before, world.HashState());
    }

    [Fact]
    public void Changing_a_derived_field_does_not_move_the_hash()
    {
        World world = Build();
        ulong before = world.HashState();

        world.Citizens.CommuteNext[0] = 12_345;
        world.Buildings.OccupantHead[0] = 999;

        Assert.Equal(before, world.HashState());
    }

    /// <summary>
    /// The claim every <see cref="Disposition.Derived"/> column makes: it is a pure function of saved
    /// state.
    /// </summary>
    /// <remarks>
    /// This is the test the hash cannot perform, because these fields are outside it by declaration.
    /// Corrupting the lists and rebuilding them must restore exactly what was there — which is also
    /// what a load will do, and the reason the save is allowed not to write them.
    /// </remarks>
    [Fact]
    public void Rebuilding_a_derived_structure_restores_it_exactly()
    {
        World world = Build();

        int[] occupantsBefore = Walk(world.Occupants, owner: 0);
        int[] membersBefore = Walk(world.Members, owner: 0);
        ulong before = world.HashState();

        world.Buildings.OccupantHead.Span.Clear();
        world.Households.MemberHead.Span.Clear();
        world.RebuildDerived();

        Assert.Equal(occupantsBefore, Walk(world.Occupants, owner: 0));
        Assert.Equal(membersBefore, Walk(world.Members, owner: 0));
        Assert.Equal(before, world.HashState());
    }

    /// <summary>
    /// A handle column folds the target's monotonic id, so repointing it at a different row moves the
    /// hash even though the column's bytes could have been identical.
    /// </summary>
    [Fact]
    public void Repointing_a_handle_moves_the_hash()
    {
        World world = Build();
        Handle<Building> other = world.Buildings.Create(world.Lots, default, kind: 2);

        ulong before = world.HashState();
        world.Citizens.Workplace[0] = other;

        Assert.NotEqual(before, world.HashState());
    }

    /// <summary>
    /// A handle whose target has been freed folds as a sentinel rather than throwing — the hash is
    /// the tool you reach for <em>while</em> diagnosing, so it must survive an inconsistent world.
    /// </summary>
    [Fact]
    public void A_dangling_handle_moves_the_hash_without_throwing()
    {
        World world = Build();
        Handle<Building> doomed = world.Buildings.Create(world.Lots, default, kind: 2);
        world.Citizens.Workplace[0] = doomed;

        ulong before = world.HashState();
        world.Buildings.Rows.Free(doomed);

        Assert.NotEqual(before, world.HashState());
    }

    /// <summary>
    /// Freeing a row is a change to the world even when nothing else moves, because the allocator's
    /// own state decides where the next entity lands.
    /// </summary>
    [Fact]
    public void Freeing_a_row_moves_the_hash()
    {
        World world = Build();
        ulong before = world.HashState();

        world.Lots.Rows.Free(world.Lots.Rows.At(0));

        Assert.NotEqual(before, world.HashState());
    }

    /// <summary>
    /// Capacity is a sizing figure and never reaches the hash — two worlds sized for populations an
    /// order of magnitude apart, given the same history, are the same city.
    /// </summary>
    /// <remarks>
    /// The distinction <c>05 §4</c> draws: <em>a change is an optimisation if the State Hash is
    /// unchanged, and a design change otherwise.</em> How much room the arrays were given is the
    /// former, and this is the test that says so.
    /// </remarks>
    [Fact]
    public void Capacity_is_not_a_hash_input()
    {
        World small = Build(Population);
        World large = Build(Population * 10);

        Assert.True(large.Lots.Rows.Capacity > small.Lots.Rows.Capacity);
        Assert.Equal(small.HashState(), large.HashState());
    }

    private static World Build() => Build(Population);

    private static World Build(int population)
    {
        var world = new World(population);

        Handle<Lot> lot = world.Lots.Create(new Tiles(4), new Tiles(6), zone: 1);
        Handle<Building> building = world.Buildings.Create(world.Lots, lot, kind: 3);
        Handle<Household> household = world.CreateHousehold(building, lifeStage: 2);

        world.Households.Money[world.Households.Rows.Resolve(household)] = new Money(500);

        for (int i = 0; i < 3; i++)
        {
            Handle<Citizen> citizen = world.CreateCitizen(household, new Ticks((ulong)(100 + i)));

            // Through the mutator so the worker list matches the handles. The two tests below write
            // the column raw on purpose -- they are about what the hash does with a handle, and one
            // of them wants a deliberately inconsistent world -- but the fixture they start from
            // should not be one.
            world.Employ(citizen, building);
        }

        return world;
    }

    private static int[] Walk(IndexList list, int owner)
    {
        var found = new List<int>();
        foreach (int slot in list.Walk(owner))
        {
            found.Add(slot);
        }

        return [.. found];
    }
}

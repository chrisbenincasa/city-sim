using Borough.Core.Entities;
using Borough.Core.Tables;

namespace Borough.Tests.Tables;

/// <summary>
/// The one shape every variable-length collection in <c>Borough.Core</c> takes.
/// </summary>
/// <remarks>
/// The three consumers — Bin wait lists, Parking Sheds, Event Wheel buckets — arrive in later slices
/// and must not invent their own. These tests are what the pattern has instead of a first consumer.
/// </remarks>
public class IndexListTests
{
    [Fact]
    public void An_untouched_owner_has_an_empty_list()
    {
        Fixture fixture = Fixture.Create();

        Assert.True(fixture.List.IsEmpty(0));
        Assert.Empty(Walk(fixture.List, 0));
        Assert.Equal(Rows.NoSlot, fixture.List.PopFront(0));
    }

    /// <summary>
    /// Zero means empty, and slot 0 is a real slot. Without the plus-one encoding a freshly created
    /// owner would appear to own element 0 — which is the failure this test exists to pin.
    /// </summary>
    [Fact]
    public void Slot_zero_is_a_member_and_not_a_terminator()
    {
        Fixture fixture = Fixture.Create();
        fixture.List.Append(1, 0);

        Assert.False(fixture.List.IsEmpty(1));
        Assert.Equal([0], Walk(fixture.List, 1));
        Assert.True(fixture.List.IsEmpty(0));
    }

    [Fact]
    public void Appending_preserves_arrival_order()
    {
        Fixture fixture = Fixture.Create();

        fixture.List.Append(0, 3);
        fixture.List.Append(0, 1);
        fixture.List.Append(0, 2);

        Assert.Equal([3, 1, 2], Walk(fixture.List, 0));
    }

    [Fact]
    public void Ordered_insertion_sorts_by_slot_whatever_the_arrival_order()
    {
        Fixture fixture = Fixture.Create();

        fixture.List.InsertOrdered(0, 3);
        fixture.List.InsertOrdered(0, 1);
        fixture.List.InsertOrdered(0, 4);
        fixture.List.InsertOrdered(0, 0);

        Assert.Equal([0, 1, 3, 4], Walk(fixture.List, 0));
    }

    [Fact]
    public void Popping_drains_from_the_front()
    {
        Fixture fixture = Fixture.Create();

        fixture.List.Append(0, 2);
        fixture.List.Append(0, 5);

        Assert.Equal(2, fixture.List.PopFront(0));
        Assert.Equal(5, fixture.List.PopFront(0));
        Assert.Equal(Rows.NoSlot, fixture.List.PopFront(0));
        Assert.True(fixture.List.IsEmpty(0));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Removing_relinks_at_any_position(int removeAt)
    {
        Fixture fixture = Fixture.Create();
        int[] nodes = [4, 6, 9];

        foreach (int node in nodes)
        {
            fixture.List.Append(0, node);
        }

        Assert.True(fixture.List.Remove(0, nodes[removeAt]));
        Assert.Equal(nodes.Where((_, i) => i != removeAt), Walk(fixture.List, 0));

        // The tail must have followed. If it did not, the next append writes through a dangling
        // index and the list silently rejoins whatever was removed.
        fixture.List.Append(0, 11);
        Assert.Equal([.. nodes.Where((_, i) => i != removeAt), 11], Walk(fixture.List, 0));
    }

    [Fact]
    public void Removing_something_absent_says_so()
    {
        Fixture fixture = Fixture.Create();
        fixture.List.Append(0, 1);

        Assert.False(fixture.List.Remove(0, 2));
        Assert.Equal([1], Walk(fixture.List, 0));
    }

    [Fact]
    public void Two_owners_do_not_see_each_others_elements()
    {
        Fixture fixture = Fixture.Create();

        fixture.List.Append(0, 1);
        fixture.List.Append(2, 3);

        Assert.Equal([1], Walk(fixture.List, 0));
        Assert.Equal([3], Walk(fixture.List, 2));
    }

    /// <summary>
    /// <b>Inserting a node already in the list is refused, because the alternative is a self-link
    /// and a traversal that never returns.</b>
    /// </summary>
    /// <remarks>
    /// The ordered scan stops at the first element whose slot is not below <c>node</c>'s, which for a
    /// node already present is that node — so <c>_next[node]</c> would be set to <c>node</c>'s own
    /// encoded index. Nothing reads as wrong until the next walk, insert or remove on that owner,
    /// each of which then spins for ever. ⚠ <b>This is not hypothetical: milestone 7's
    /// <c>CarParkResidency</c> reached it</b> — a bulldozed Street severed a Car Park's Address,
    /// the unlist that goes <em>through</em> that Address silently did nothing, the row was freed
    /// still listed, and the recycled slot was inserted into the same Segment's list a second time.
    /// The whole suite hung with no failing test and no stack.
    /// </remarks>
    [Fact]
    public void Ordered_insertion_refuses_a_node_already_in_the_list()
    {
        Fixture fixture = Fixture.Create();

        fixture.List.InsertOrdered(0, 1);
        fixture.List.InsertOrdered(0, 3);

        Fixture captured = fixture;
        InvalidOperationException error =
            Assert.Throws<InvalidOperationException>(() => captured.List.InsertOrdered(0, 3));

        Assert.Contains("already in owner", error.Message, StringComparison.Ordinal);

        // And the list it refused to corrupt is untouched.
        Assert.Equal([1, 3], Walk(fixture.List, 0));
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

    /// <summary>
    /// A world with enough Buildings and Households allocated that both columns have live slots to
    /// thread a list through. The list is the Buildings' occupant list, used here as a bare structure.
    /// </summary>
    private sealed class Fixture
    {
        private Fixture(World world) => World = world;

        internal World World { get; }

        internal IndexList List => World.Occupants;

        internal static Fixture Create()
        {
            var world = new World(1_000);
            Handle<Building> building = default;

            for (int i = 0; i < 16; i++)
            {
                building = world.Buildings.Create(world.Lots, default, kind: 0);
            }

            for (int i = 0; i < 16; i++)
            {
                world.CreateHousehold(building, lifeStage: 0);
            }

            // The Households above linked themselves into the last Building's list; clear every list
            // so the tests below start from an empty structure rather than from that residue.
            world.Buildings.OccupantHead.Span.Clear();
            world.Buildings.OccupantTail.Span.Clear();
            world.Households.DwellingNext.Span.Clear();

            return new Fixture(world);
        }
    }
}

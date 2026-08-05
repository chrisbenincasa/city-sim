using Borough.Core.Tables;

namespace Borough.Tests.Tables;

/// <summary>
/// The sorted array that stands in for the hash map <c>BOR0301</c> forbids.
/// </summary>
/// <remarks>
/// A prohibition only holds if there is somewhere better to go. These tests are what makes this the
/// somewhere: ascending keys, a total order, and a lookup that does not depend on how the entries
/// arrived.
/// </remarks>
public class ResourceMapTests
{
    private const int Capacity = 9;

    [Fact]
    public void An_absent_key_reports_where_it_would_go()
    {
        Span<ResourceId> keys = stackalloc ResourceId[Capacity];
        Span<int> values = stackalloc int[Capacity];
        int count = 0;

        ResourceMap.Set(keys, values, ref count, new ResourceId(4), 40);
        ResourceMap.Set(keys, values, ref count, new ResourceId(8), 80);

        Assert.Equal(~0, ResourceMap.Find(keys[..count], new ResourceId(1)));
        Assert.Equal(~1, ResourceMap.Find(keys[..count], new ResourceId(6)));
        Assert.Equal(~2, ResourceMap.Find(keys[..count], new ResourceId(9)));
    }

    [Fact]
    public void Insertion_order_does_not_reach_the_layout()
    {
        Span<ResourceId> ascending = stackalloc ResourceId[Capacity];
        Span<int> ascendingValues = stackalloc int[Capacity];
        int ascendingCount = 0;

        Span<ResourceId> shuffled = stackalloc ResourceId[Capacity];
        Span<int> shuffledValues = stackalloc int[Capacity];
        int shuffledCount = 0;

        int[] order = [2, 7, 1, 5, 3];

        foreach (int key in order.Order())
        {
            ResourceMap.Set(ascending, ascendingValues, ref ascendingCount, new ResourceId((ushort)key), key * 10);
        }

        foreach (int key in order)
        {
            ResourceMap.Set(shuffled, shuffledValues, ref shuffledCount, new ResourceId((ushort)key), key * 10);
        }

        Assert.Equal(ascendingCount, shuffledCount);
        Assert.True(ascending[..ascendingCount].SequenceEqual(shuffled[..shuffledCount]));
        Assert.True(ascendingValues[..ascendingCount].SequenceEqual(shuffledValues[..shuffledCount]));
    }

    [Fact]
    public void Setting_an_existing_key_overwrites_rather_than_duplicates()
    {
        Span<ResourceId> keys = stackalloc ResourceId[Capacity];
        Span<int> values = stackalloc int[Capacity];
        int count = 0;

        ResourceMap.Set(keys, values, ref count, new ResourceId(3), 30);
        ResourceMap.Set(keys, values, ref count, new ResourceId(3), 31);

        Assert.Equal(1, count);
        Assert.True(ResourceMap.TryGet<int>(keys, values, count, new ResourceId(3), out int value));
        Assert.Equal(31, value);
    }

    [Fact]
    public void Removal_closes_the_gap_and_keeps_the_keys_ascending()
    {
        Span<ResourceId> keys = stackalloc ResourceId[Capacity];
        Span<int> values = stackalloc int[Capacity];
        int count = 0;

        for (ushort key = 1; key <= 5; key++)
        {
            ResourceMap.Set(keys, values, ref count, new ResourceId(key), key * 10);
        }

        Assert.True(ResourceMap.Remove(keys, values, ref count, new ResourceId(3)));
        Assert.Equal(4, count);
        Assert.False(ResourceMap.TryGet<int>(keys, values, count, new ResourceId(3), out _));

        for (int i = 1; i < count; i++)
        {
            Assert.True(keys[i - 1] < keys[i]);
        }
    }

    [Fact]
    public void A_full_map_refuses_a_new_key_and_still_accepts_an_overwrite()
    {
        Span<ResourceId> keys = stackalloc ResourceId[2];
        Span<int> values = stackalloc int[2];
        int count = 0;

        Assert.True(ResourceMap.Set(keys, values, ref count, new ResourceId(1), 10));
        Assert.True(ResourceMap.Set(keys, values, ref count, new ResourceId(2), 20));

        Assert.False(ResourceMap.Set(keys, values, ref count, new ResourceId(3), 30));
        Assert.True(ResourceMap.Set(keys, values, ref count, new ResourceId(2), 21));

        Assert.Equal(2, count);
    }
}

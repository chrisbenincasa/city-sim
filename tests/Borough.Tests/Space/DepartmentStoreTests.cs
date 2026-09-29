using Borough.Core.Space;

namespace Borough.Tests.Space;

public sealed class DepartmentStoreTests
{
    [Fact]
    public void A_store_puts_its_anchor_between_two_corner_Units()
    {
        Span<DepartmentStore.Bay> bays = stackalloc DepartmentStore.Bay[DepartmentStore.MaxUnits];

        int count = DepartmentStore.Units(30, bays);

        Assert.Equal(3, count);
        Assert.Equal(new DepartmentStore.Bay(0, 2, false), bays[0]);
        Assert.Equal(new DepartmentStore.Bay(2, 26, true), bays[1]);
        Assert.Equal(new DepartmentStore.Bay(28, 2, false), bays[2]);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(1)]
    public void A_store_too_narrow_for_corners_is_one_anchor(int wide)
    {
        Span<DepartmentStore.Bay> bays = stackalloc DepartmentStore.Bay[DepartmentStore.MaxUnits];

        Assert.Equal(1, DepartmentStore.Units(wide, bays));
        Assert.Equal(new DepartmentStore.Bay(0, wide, true), bays[0]);
    }

    [Fact]
    public void A_high_street_block_joins_its_south_face_into_one_parcel()
    {
        var plots = new ResidentialPlots(4, 6, 3, 4, 3);
        var ground = new BlockGround(0, 0, 0, 0, 32, 32);
        Span<Parcel> parcels = stackalloc Parcel[plots.Ceiling(ground)];

        int count = plots.Carve(ground, 1, parcels, joinSouth: true);
        int south = 0;

        for (int i = 0; i < count; i++)
        {
            if (parcels[i].Face != BlockFace.South)
            {
                continue;
            }

            south++;
            Assert.Equal(30, parcels[i].Wide.Raw);
            Assert.Equal(6, parcels[i].Deep.Raw);
        }

        Assert.Equal(1, south);
        Assert.Equal(plots.Carve(ground, 1, parcels) - 6, count);
    }
}

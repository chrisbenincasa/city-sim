using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Movement;
using Borough.Core.Quantities;

namespace Borough.Tests.Rules;

public sealed class UnaffordableDemandTests
{
    [Theory]
    [InlineData(ShoppingFailure.Unaffordable)]
    [InlineData(ShoppingFailure.Closed)]
    public void An_outing_another_shop_could_not_serve_raises_no_shop(ShoppingFailure reason)
    {
        Assert.Equal(0, ShopsRaised(reason));
    }

    [Theory]
    [InlineData(ShoppingFailure.Empty)]
    [InlineData(ShoppingFailure.NoKnownShop)]
    public void An_outing_another_shop_could_serve_raises_shops(ShoppingFailure reason)
    {
        Assert.True(ShopsRaised(reason) > 0, $"no shop was raised on outings failing as {reason}.");
    }

    private static int ShopsRaised(ShoppingFailure reason)
    {
        (World world, Simulation sim) = ShoppingTests.Start();
        byte shopfront = world.Rules.ZoneRules[1].Kind;

        for (ulong t = 0; t < Ticks.PerDay; t++)
        {
            sim.Step(default);
        }

        int before = Count(world, shopfront);

        for (ulong t = 0; t < 2 * Ticks.PerDay; t++)
        {
            HoldEveryOuting(world, reason);
            sim.Step(default);
        }

        return Count(world, shopfront) - before;
    }

    private static void HoldEveryOuting(World world, ShoppingFailure reason)
    {
        for (int row = 0; row < world.Shopping.Rows.SlotCount; row++)
        {
            if (!world.Shopping.Rows.IsLive(row) || world.Shopping.Stage[row] != 0)
            {
                continue;
            }

            world.Shopping.NextAt[row] = new Ticks(ulong.MaxValue);
            world.Shopping.Reason[row] = (byte)reason;

            if (world.Shopping.UnservedSince[row].Raw == 0)
            {
                world.Shopping.UnservedSince[row] = world.Tick + new Ticks(1);
            }
        }
    }

    private static int Count(World world, byte kind)
    {
        int count = 0;

        for (int b = 0; b < world.Buildings.Rows.SlotCount; b++)
        {
            if (world.Buildings.Rows.IsLive(b) && world.Buildings.Kind[b] == kind)
            {
                count++;
            }
        }

        return count;
    }
}

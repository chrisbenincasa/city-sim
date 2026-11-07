using System.Reflection;
using Borough.Core.Entities;
using Borough.Core.Tables;

namespace Borough.Tests.Review;

// Review probe B: TableRegistrationTests matches World._tables by Rows<T> TYPE. Three tables share
// Rows<WheelBucket>, so dropping any one of them from _tables leaves the check green.
public sealed class TableRegistrationByTypeProbe
{
    [Fact]
    public void Dropping_one_wheel_bucket_table_is_invisible_to_the_type_based_check()
    {
        var world = new World(1_000, Golden.GoldenFixtures.Rules());

        Rows dropped = world.LifeStages.Buckets.Rows;
        Assert.Contains(dropped, world.Tables.ToArray());

        int sameType = world.Tables.ToArray().Count(rows => rows.GetType() == dropped.GetType());
        Assert.Equal(3, sameType);

        // TableRegistrationTests' own logic, run against a composition with one table removed.
        HashSet<Type> registered = [];
        foreach (Rows rows in world.Tables)
        {
            if (!ReferenceEquals(rows, dropped))
            {
                registered.Add(rows.GetType());
            }
        }

        List<string> missing = [];
        foreach (Type table in typeof(World).Assembly.GetTypes()
                     .Where(type => type.GetCustomAttribute<TableAttribute>() is not null))
        {
            PropertyInfo? property = table.GetProperty("Rows");
            if (property is null || !typeof(Rows).IsAssignableFrom(property.PropertyType))
            {
                continue;
            }

            if (table.Name == "TreasuryTable" || registered.Contains(property.PropertyType))
            {
                continue;
            }

            missing.Add(table.Name);
        }

        // The check reports nothing missing although life_stage buckets are no longer hashed or saved.
        Assert.Empty(missing);
    }
}

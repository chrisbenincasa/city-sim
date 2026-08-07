namespace Borough.Core.Entities;

using Borough.Core.Arithmetic;
using Borough.Core.Quantities;
using Borough.Core.Tables;

/// <summary>
/// Fills a world with a city sized to its configuration, for measuring the simulation at scale.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is an instrument, not a mechanism, and the distinction is the whole reason it is written
/// down here rather than in the runner.</b> A real city arrives through Zone Rules and the Unplaced
/// Pool; nothing about this class is how Citizens are meant to come into existence, and when slice 10
/// lands there is a case for deleting it. What it exists for is spike <c>S0</c>: until a Tick has been
/// run over a million rows, 1M is a hope, and every table sized against it rests on an unvalidated
/// assumption.
/// </para>
/// <para>
/// <b>It lives in <c>Borough.Core</c> because it enters through Phase 0 like every other input.</b>
/// <see cref="Simulation"/> calls it from <see cref="Input.CommandKind.Populate"/>, so the population
/// is described by the Input Log that describes the session, and replay reproduces it by construction
/// rather than by a claim somebody has to keep true. Populating a world from the shell would have been
/// three fewer files and a state change no replay could reproduce and no State Hash divergence could
/// explain — which is the one thing <see cref="Simulation"/>'s only door exists to prevent.
/// </para>
/// <para>
/// <b>It draws no randomness, deliberately.</b> Every value below is index arithmetic, so the city is
/// a pure function of its size and needs no <c>purpose_tag</c> — and therefore cannot correlate itself
/// with a simulation decision that shares a stream. That is a real hazard here rather than a
/// hypothetical one: a fixture is exactly the kind of code somebody reaches for a convenient
/// <c>draw()</c> in, and the correlation it would create is invisible.
/// </para>
/// <para>
/// <b>What it is not is representative.</b> The Lots are laid in a 64-Tile strip, every Household has
/// the same size, and workplaces are assigned by a stride. That is enough to answer <em>what does a
/// Tick over a million rows cost</em> and it is not enough to answer anything spatial or economic. The
/// shape is stated so nobody reads a distribution out of it that was never put in.
/// </para>
/// </remarks>
public static class SyntheticCity
{
    /// <summary>
    /// Households per Building, from S4 task 2's row ratios: 360 Households and ~150 Buildings per
    /// 1,000 Citizens.
    /// </summary>
    private const int HouseholdsPerBuilding = 3;

    /// <summary>Lots per row, before the strip wraps northward.</summary>
    private const int LotsPerRow = 64;

    /// <summary>
    /// Fills <paramref name="world"/> to the Citizen count it was configured with.
    /// </summary>
    /// <remarks>
    /// <b>The size comes from the world rather than from an argument, and that is what keeps one
    /// number in one place.</b> <see cref="Input.WorldConfiguration.Citizens"/> is already in the log,
    /// already sizes every table, and is already what <c>--citizens</c> sets. A count on the command
    /// too would let a log state two populations, which is the same disagreement
    /// <c>Borough.Headless</c> refuses <c>--citizens</c> alongside <c>--log</c> to avoid.
    /// </remarks>
    /// <param name="world">The world to fill. Must have no Citizens in it.</param>
    /// <exception cref="InvalidOperationException">The world already has a population.</exception>
    public static void PopulateInto(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        // Refused rather than added to. Applying the verb twice would produce a city of twice the
        // configured size whose tables had grown past the capacity every footprint figure was derived
        // from — a run that answers the sizing question with the wrong number and reports success.
        if (world.Citizens.Rows.LiveCount != 0)
        {
            throw new InvalidOperationException(
                "the world already has a population, and a synthetic city is not something to add a "
                + "second of. Populate is world creation, so it belongs at Tick 0 and once.");
        }

        int population = world.Citizens.Rows.Capacity;
        int households = IntegerMath.FloorDiv(population * 360, 1_000);
        int buildings = IntegerMath.FloorDiv(households, HouseholdsPerBuilding) + 1;

        for (int i = 0; i < buildings; i++)
        {
            Handle<Lot> lot = world.Lots.Create(
                new Tiles(i % LotsPerRow), new Tiles(IntegerMath.FloorDiv(i, LotsPerRow)), zone: 1);

            world.Buildings.Create(lot, kind: 1);
        }

        for (int i = 0; i < households; i++)
        {
            world.CreateHousehold(Dwelling(world, i % buildings), lifeStage: (byte)(i % 5));
        }

        for (int i = 0; i < population; i++)
        {
            Handle<Citizen> citizen = world.CreateCitizen(
                world.Households.Rows.At(i % households), new Ticks((ulong)i % 8192));

            // A workplace that is not the dwelling, on a stride coprime with the Building count, so
            // that the commute matrix is not the identity. Nothing reads it before Phase 2 of the
            // roadmap; leaving it null would make the first thing that does measure a city where
            // nobody works.
            world.Citizens.Workplace[world.Citizens.Rows.Resolve(citizen)] =
                Dwelling(world, (i * 7) % buildings);
        }
    }

    /// <summary>
    /// The handle of the <paramref name="index"/>th Building.
    /// </summary>
    /// <remarks>
    /// <b>Sound only because the table started empty</b>, which
    /// <see cref="PopulateInto"/> refuses to proceed without: allocation appends while the free list
    /// is empty, so the <c>n</c>th Building is slot <c>n</c>. Holding the handles in an array instead
    /// would be 4 MiB of transient garbage at the 1M target to restate what the allocator already
    /// guarantees.
    /// </remarks>
    private static Handle<Building> Dwelling(World world, int index) =>
        world.Buildings.Rows.At(index);
}

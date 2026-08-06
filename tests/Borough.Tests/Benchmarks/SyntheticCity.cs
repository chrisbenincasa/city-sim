using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Tables;

namespace Borough.Tests.Benchmarks;

/// <summary>
/// A populated world, built by construction, for measuring what a walk over one costs.
/// </summary>
/// <remarks>
/// <para>
/// <b>Deliberately small, and deliberately deletable.</b> <c>S0</c> — the synthetic 1M-Citizen city
/// in <c>Borough.Headless</c>, gated on slices 4–6 — is the corpus's designated synthetic city, and
/// this is not it and must not become it. What this exists for is one question: what does a sweep
/// over <em>n</em> rows cost. When S0 lands, this goes.
/// </para>
/// <para>
/// <b>It is not a session, and that is the point.</b> The obvious way to benchmark a populated world
/// is to give the runner a flag that seeds one — and that is the one thing <c>Replay</c> forbids:
/// <em>the moment world state can arrive from somewhere the log does not describe, the log stops
/// being a complete account of a session and a divergence stops being attributable.</em> Nothing here
/// touches a log, a session or a State Hash, so nothing here can put a number in front of somebody
/// that looks reproducible and is not.
/// </para>
/// <para>
/// <b>Stepping would add nothing to the measurement today.</b> The staggered sweep's cost is a
/// function of row counts and list lengths; it reads state and does not care whether the world is
/// evolving. The one thing a running Tick would contribute is cache competition from the other
/// phases, and every phase except Input is currently empty.
/// </para>
/// </remarks>
internal static class SyntheticCity
{
    /// <summary>Households per Building. Provisional; the corpus states none.</summary>
    private const int HouseholdsPerBuilding = 3;

    /// <summary>
    /// A city of <paramref name="population"/> Citizens, at S4 task 2's ratios.
    /// </summary>
    /// <remarks>
    /// 360 Households, ~150 Buildings and ~225 Lots per 1,000 Citizens — stated per thousand so they
    /// stay correct at any population, because sizing is a derivation rather than a constant.
    /// </remarks>
    public static World Of(int population)
    {
        var world = new World(population);

        int households = (population * 360) / 1_000;
        int buildings = (households / HouseholdsPerBuilding) + 1;

        var dwellings = new Handle<Building>[buildings];

        for (int i = 0; i < buildings; i++)
        {
            Handle<Lot> lot = world.Lots.Create(new Tiles(i % 64), new Tiles(i / 64), zone: 1);
            dwellings[i] = world.Buildings.Create(lot, kind: 1);
        }

        var homes = new Handle<Household>[households];

        for (int i = 0; i < households; i++)
        {
            homes[i] = world.CreateHousehold(dwellings[i % buildings], lifeStage: (byte)(i % 5));
        }

        for (int i = 0; i < population; i++)
        {
            Handle<Citizen> citizen = world.CreateCitizen(
                homes[i % households], new Ticks((ulong)i % 8192));

            world.Citizens.Workplace[world.Citizens.Rows.Resolve(citizen)] =
                dwellings[(i * 7) % buildings];
        }

        return world;
    }
}

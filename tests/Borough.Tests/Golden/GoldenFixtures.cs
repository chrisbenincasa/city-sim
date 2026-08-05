using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Tables;

namespace Borough.Tests.Golden;

/// <summary>
/// The two sessions the committed baselines were recorded from.
/// </summary>
/// <remarks>
/// <para>
/// <b>This file is data that happens to be written in C#.</b> Every number in it is load-bearing:
/// changing one moves a committed hash, and a hash that moves without somebody saying so is the
/// regression the baseline exists to catch. Do not tidy, refactor or "improve" anything here without
/// following the procedure in <c>README.md</c> in this directory.
/// </para>
/// <para>
/// <b>The session is a code fixture only until slice 5 task 5.</b> That task creates
/// <c>Borough.Formats</c> and the line-oriented codec (<c>adr/0039</c>), at which point the session
/// is committed as a <c>.borough</c> file and this builder becomes the thing the codec is checked
/// against: the parsed file must reproduce the same log and therefore the same trace. Writing a
/// second reader here to load a text log today would have created exactly the two implementations
/// <c>adr/0039</c> exists to prevent.
/// </para>
/// </remarks>
internal static class GoldenFixtures
{
    /// <summary>The golden session's world seed.</summary>
    internal const ulong Seed = 0x0B07_0000_0000_0EA1UL;

    /// <summary>The golden session's Citizen sizing, and the golden world's.</summary>
    internal const int Population = 1_000;

    /// <summary>
    /// The Ruleset content hash. Zero until slice 8, which is when a Ruleset first has content.
    /// </summary>
    internal const ulong RulesetHash = 0UL;

    /// <summary>How far the session runs. Well past its last command, which is the ordinary case.</summary>
    internal const int Ticks = 256;

    /// <summary>The trace's sampling cadence.</summary>
    internal const int HashEvery = 8;

    /// <summary>
    /// The golden session: eleven Zone commands, then silence.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The shape is chosen for what it can go wrong on, not for realism.</b> Two commands share a
    /// Tick, so the per-Tick slice's lower bound is exercised rather than assumed; there are gaps, so
    /// a run that applied commands by index rather than by Tick would diverge; the last command lands
    /// at Tick 97 with the run going to 256, so most samples are of a city nobody is touching.
    /// </para>
    /// <para>
    /// <b>Only <see cref="CommandKind.Zone"/> appears, because only Zone is applied.</b> Connect,
    /// Service and Govern are declared and throw on application until slice 7. When they land they
    /// extend this session rather than replace it, and that extension is a deliberate re-baseline.
    /// </para>
    /// </remarks>
    internal static InputLog Session()
    {
        InputLogBuilder builder = new(Seed, new WorldConfiguration(Population), RulesetHash);

        Append(builder, tick: 0, east: 0, north: 0, zone: 1);
        Append(builder, tick: 1, east: 1, north: 0, zone: 1);
        Append(builder, tick: 1, east: 2, north: 0, zone: 2);
        Append(builder, tick: 2, east: 2, north: 1, zone: 2);
        Append(builder, tick: 9, east: 7, north: 3, zone: 3);
        Append(builder, tick: 17, east: 11, north: 5, zone: 1);
        Append(builder, tick: 17, east: 12, north: 5, zone: 1);
        Append(builder, tick: 33, east: 31, north: 29, zone: 4);
        Append(builder, tick: 64, east: 63, north: 0, zone: 2);
        Append(builder, tick: 65, east: 0, north: 63, zone: 3);
        Append(builder, tick: 97, east: 255, north: 255, zone: 5);

        return builder.Build();
    }

    /// <summary>
    /// The golden world: a small, coherent city built by hand, touching every table the session
    /// cannot reach.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This exists because the session covers one table in four.</b> A Zone command creates a Lot
    /// and nothing else — Buildings, Households and Citizens are reachable only through the cold API
    /// until slice 7 gives the player verbs that make them. Without this fixture, three tables' saved
    /// columns would have no committed hash over them at all, and the baseline would be claiming a
    /// coverage it does not have.
    /// </para>
    /// <para>
    /// <b>It is built to exercise the fold's awkward cases rather than to look like a city.</b> Both
    /// intrusive lists are populated; a Household and a Citizen are destroyed, so the allocator's free
    /// head and its monotonic id counter are both off their initial values and both reach the hash;
    /// handle columns point across tables, so the fold's <em>values, never identity</em> rule is under
    /// the baseline rather than merely under a unit test.
    /// </para>
    /// <para>
    /// <b>The world stays coherent.</b> No dangling handle, no Citizen without a Household — because
    /// task 6's invariant tiers will be run over this fixture, and an invariant suite whose reference
    /// world is already broken cannot be trusted to report anything.
    /// </para>
    /// </remarks>
    internal static World Build()
    {
        var world = new World(Population);

        var lots = new Handle<Lot>[6];
        for (int i = 0; i < lots.Length; i++)
        {
            lots[i] = world.Lots.Create(new Tiles(i * 3), new Tiles(i * 5), zone: (byte)(1 + (i % 4)));
        }

        var buildings = new Handle<Building>[4];
        for (int i = 0; i < buildings.Length; i++)
        {
            buildings[i] = world.Buildings.Create(lots[i], kind: (byte)(1 + (i % 3)));
        }

        var households = new Handle<Household>[8];
        for (int i = 0; i < households.Length; i++)
        {
            households[i] = world.CreateHousehold(buildings[i % buildings.Length], lifeStage: (byte)(i % 5));

            int slot = world.Households.Rows.Resolve(households[i]);
            world.Households.Money[slot] = new Money(1_000 + (i * 137));
            world.Households.Savings[slot] = new Money(i * 2_500);
        }

        var citizens = new Handle<Citizen>[20];
        for (int i = 0; i < citizens.Length; i++)
        {
            citizens[i] = world.CreateCitizen(
                households[i % households.Length],
                new Ticks((ulong)((i * 401) % 8192)));

            int slot = world.Citizens.Rows.Resolve(citizens[i]);
            world.Citizens.Workplace[slot] = buildings[(i * 3) % buildings.Length];
            world.Citizens.Activity[slot] = (byte)(i % 7);
            world.Citizens.SkillTier[slot] = (byte)(i % 4);
            world.Citizens.Employment[slot] = (byte)(i % 3);
            world.Citizens.Experience[slot] = i * 1_009L;
            world.Citizens.Age[slot] = (ushort)(6_000 + (i * 211));
            world.Citizens.Health[slot] = (byte)(100 - i);
        }

        // Retirements, so the free list and the never-reused id counter are both off their initial
        // values by the time the hash is taken. Household 5 takes its two members with it.
        world.DestroyCitizen(citizens[13]);
        world.DestroyHousehold(households[5]);

        return world;
    }

    private static void Append(InputLogBuilder builder, ulong tick, int east, int north, ushort zone) =>
        builder.Append(new Ticks(tick), new Command(CommandKind.Zone, new Tiles(east), new Tiles(north), zone));
}

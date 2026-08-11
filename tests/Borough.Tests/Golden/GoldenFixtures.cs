using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;

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
    /// The content hash of <see cref="RulesetPath"/>, as the session records it.
    /// </summary>
    /// <remarks>
    /// <b>A literal rather than a call, and that is the point of it.</b> The committed
    /// <c>session.borough</c> carries this number, so it has to be a number here too — and the pair
    /// is what makes editing the Ruleset a re-baseline rather than a silent change of what the
    /// baseline covers. <c>The_golden_ruleset_is_the_one_the_session_names</c> is the test that says
    /// so, and it fails with the number to paste in.
    /// </remarks>
    internal const ulong RulesetHash = 0x6062_C27D_C2A7_2C7FUL;

    /// <summary>The Ruleset the golden session runs under, beside the test assembly.</summary>
    internal static string RulesetPath =>
        Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml");

    /// <summary>
    /// The content hash of <see cref="TunedRulesetPath"/>, which the session reloads into.
    /// </summary>
    /// <remarks>
    /// A literal for <see cref="RulesetHash"/>'s reason, and it is in <c>session.borough</c> too:
    /// a reload line carries both hashes, so editing either file is a re-baseline of both artefacts.
    /// </remarks>
    internal const ulong TunedRulesetHash = 0x0728_36AF_3B84_6432UL;

    /// <summary>The Ruleset the golden session reloads into at <see cref="ReloadAt"/>.</summary>
    internal static string TunedRulesetPath =>
        Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal-tuned.toml");

    /// <summary>
    /// The Tick the golden session reloads on.
    /// </summary>
    /// <remarks>
    /// <b>Halfway, and on a sampling boundary.</b> Halfway so that the trace holds sixteen samples of
    /// each Ruleset rather than a run and a postscript; on a boundary so the first sample after the
    /// transition is the Tick after it, which is what makes the failure message name the right window
    /// when this moves.
    /// </remarks>
    internal const int ReloadAt = 1_024;

    /// <summary>
    /// How far the session runs. Well past its last command, which is the ordinary case.
    /// </summary>
    /// <remarks>
    /// <b>256 until slice 10 task 11, and it was lengthened to keep coverage rather than to gain
    /// any.</b> <c>adr/0059</c> derives a Zone Rule's sample from the Lot count, and at this fixture's
    /// 132 Lots the shipped one-Day revisit period gives <b>one</b> Lot a trigger where the retired
    /// <c>sample = 4</c> gave four. At 256 Ticks that is eight looks at the city, which condemns and
    /// never happens to land on a Lot that demolition has cleared — so the committed trace would have
    /// stopped covering <see cref="Borough.Core.Rules.ZoneRuleEngine"/>'s create path entirely, and
    /// nothing would have said so. At 2,048 Ticks the session raises twelve Buildings and condemns
    /// forty-nine, so both branches are under the baseline again.
    /// </remarks>
    internal const int Ticks = 2_048;

    /// <summary>
    /// The trace's sampling cadence.
    /// </summary>
    /// <remarks>
    /// <b>Eight until slice 10 task 11, and it moved with <see cref="Ticks"/> to hold the trace at
    /// thirty-two samples.</b> The sample count is what the bisection message is worth reading for;
    /// a longer session at the old cadence would have been a 256-line artefact whose diff nobody
    /// reads, and the window a failure names would have got no narrower for it.
    /// </remarks>
    internal const int HashEvery = 64;

    /// <summary>
    /// The golden session: a population, eleven Zone commands, then silence.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The shape is chosen for what it can go wrong on, not for realism.</b> Two commands share a
    /// Tick, so the per-Tick slice's lower bound is exercised rather than assumed; there are gaps, so
    /// a run that applied commands by index rather than by Tick would diverge; the last command lands
    /// at Tick 97 with the run going to 2,048, so most samples are of a city nobody is touching.
    /// </para>
    /// <para>
    /// <b><see cref="CommandKind.Populate"/> arrives first, and it is what makes this baseline cover
    /// the Rule engine at all.</b> Until slice 7 task 10a the session raised no Building, so no Bin
    /// and no Rule Instance existed to fold and the trace was a hash over an empty world stepping.
    /// Populate is world creation, so it is at Tick 0 and once, ahead of the Zone commands — and it
    /// puts <c>SyntheticCity</c> under the baseline too, which is deliberate: the populator writes
    /// saved state, and saved state that moves without somebody saying so is what this file is for.
    /// </para>
    /// <para>
    /// <b>The Zone commands sit north of the populated rows.</b> The populator lays 121 Lots across
    /// rows 0 and 1, and five of the original coordinates were inside that block — two Lots on one
    /// Tile is a state nothing refuses today and a fixture whose docstring promises coherence should
    /// not be the first to hold one. Nothing else about them moved.
    /// </para>
    /// <para>
    /// <b>Connect, Service and Govern are still declared and still throw on application.</b> When
    /// they land they extend this session rather than replace it, and that extension is a deliberate
    /// re-baseline.
    /// </para>
    /// </remarks>
    internal static InputLog Session() => Session(reloads: true);

    /// <summary>
    /// The golden session, optionally without its transition.
    /// </summary>
    /// <remarks>
    /// <b>The parameter exists for exactly one test</b> — the one that shows the reload <em>does
    /// something</em>. A committed trace covering a transition that moved no hash would be a baseline
    /// claiming coverage it does not have, which is the defect <c>world-hash.txt</c>'s own README
    /// paragraph is about. Nothing else should use it.
    /// </remarks>
    internal static InputLog Session(bool reloads)
    {
        InputLogBuilder builder = new(Seed, new WorldConfiguration(Population), RulesetHash);

        builder.Append(new Ticks(0), new Command(CommandKind.Populate, default, default));

        Append(builder, tick: 0, east: 0, north: 2, zone: 1);
        Append(builder, tick: 1, east: 1, north: 2, zone: 1);
        Append(builder, tick: 1, east: 2, north: 2, zone: 2);
        Append(builder, tick: 2, east: 2, north: 3, zone: 2);
        Append(builder, tick: 9, east: 7, north: 3, zone: 3);
        Append(builder, tick: 17, east: 11, north: 5, zone: 1);
        Append(builder, tick: 17, east: 12, north: 5, zone: 1);
        Append(builder, tick: 33, east: 31, north: 29, zone: 4);
        Append(builder, tick: 64, east: 63, north: 2, zone: 2);
        Append(builder, tick: 65, east: 0, north: 63, zone: 3);
        Append(builder, tick: 97, east: 255, north: 255, zone: 5);

        // Slice 8 task 10. A transition rather than a command -- there is no reload verb, because
        // Command is 12 bytes and could not carry a hash -- so it is appended here and not through
        // Append. It lands after the last Zone command on purpose: the interesting half of a reload
        // is what a city already running does when the numbers under it change, not what a city
        // being built does.
        if (reloads)
        {
            builder.Reload(new Ticks(ReloadAt), TunedRulesetHash);
        }

        return builder.Build();
    }

    /// <summary>The Rules the golden session runs under, parsed from the committed file.</summary>
    /// <remarks>
    /// <b>Loaded rather than built in C#, which is the opposite of what this file does everywhere
    /// else.</b> The session exists twice on purpose — once as an artefact and once as a builder — so
    /// that the codec is checked against a file nothing in the run wrote. A Ruleset built here would
    /// have no such property: it would be a second Ruleset agreeing with the loader by construction,
    /// which is exactly the shape <c>adr/0048</c> refuses a second validator for.
    /// </remarks>
    internal static Ruleset Rules() => Load(RulesetPath);

    /// <summary>
    /// Both Rulesets the golden session names, opening one first.
    /// </summary>
    /// <remarks>
    /// <b>A catalogue rather than a Ruleset, because the session reloads.</b> A replay resolves each
    /// transition's content hash out of this; handed only the opening file it would refuse the
    /// transition rather than run on under the wrong Rules, which is the behaviour slice 8 task 1
    /// chose and <c>adr/0015</c>'s revisit trigger asks for.
    /// </remarks>
    internal static RulesetCatalogue Catalogue() => RulesetCatalogue.Of(
        [RulesetHash, TunedRulesetHash], [Load(RulesetPath), Load(TunedRulesetPath)]);

    private static Ruleset Load(string path)
    {
        RulesetLoadResult result = RulesetLoader.Load(path);

        return result.Ruleset
            ?? throw new InvalidOperationException(
                $"the golden Ruleset {path} was refused, so no baseline can be taken:"
                + $"\n{result.Describe()}");
    }

    /// <summary>
    /// The golden world: a small, coherent city built by hand, touching every table the session
    /// cannot reach.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This existed because the session covered one table in four, and slice 7 task 10a moved
    /// that boundary without dissolving it.</b> The session now opens with
    /// <see cref="CommandKind.Populate"/>, so Buildings, Households and Citizens are under the
    /// committed trace at last. What no session can do yet is <em>destroy</em> a row — so the two
    /// retirements below, and with them the allocator's free head and its never-reused id counter
    /// sitting off their initial values, are still under this hash and no other. It goes when slice
    /// 10's Zone Rules can demolish a Building.
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
            buildings[i] = world.Buildings.Create(world.Lots, lots[i], kind: (byte)(1 + (i % 3)));
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

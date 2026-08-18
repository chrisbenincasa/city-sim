using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
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
    /// <remarks>
    /// <b>1,000 until 2026-08-13, and what raised it was the map rather than the population.</b>
    /// <see cref="Borough.Core.Entities.SyntheticCity"/> now paves the ground its Lots need instead
    /// of the whole map, so the lattice a fixture gets is a function of its Citizen count: at 1,000
    /// it is <b>5×5 blocks</b>, of which the populator subdivides about 13, leaving <b>12</b> — and
    /// this session issues <b>13</b> zone commands. It did not fit, and shrinking the session is the
    /// wrong repair: <see cref="GoldenSessionCoverageTests"/> exists because
    /// <em>a baseline records what a run did, so a change that narrows what the run reaches is
    /// invisible in it by construction</em>, and quietly dropping a command is that failure by hand.
    /// <para>
    /// 4,000 gives a <b>10×10</b> lattice with about 51 free blocks, so the session keeps the spread
    /// it was authored for — adjacent blocks, distant blocks, a far corner, and two road edits with
    /// room around them. <b>It is not the smallest fixture that exercises the verbs any more</b>, and
    /// that is the cost: the committed trace is four times the city it was. 5b-bis task 8 landed on
    /// 4,000 independently, for the unrelated reason that a peak statistic needs a population.
    /// </para>
    /// </remarks>
    internal const int Population = 4_000;

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
    internal const ulong RulesetHash = 0x553B_383E_5C33_A603UL;

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
    internal const ulong TunedRulesetHash = 0x576A_64EB_5910_2E5AUL;

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
    /// <para>
    /// <b>256 until slice 10 task 11, and it was lengthened to keep coverage rather than to gain
    /// any.</b> <c>adr/0059</c> derives a Zone Rule's sample from the Lot count, and at that
    /// fixture's 132 Lots the shipped one-Day revisit period gave <b>one</b> Lot a trigger where the
    /// retired <c>sample = 4</c> gave four. At 256 Ticks that is eight looks at the city, which
    /// condemns and never happens to land on a Lot that demolition has cleared — so the committed
    /// trace would have stopped covering <see cref="Borough.Core.Rules.ZoneRuleEngine"/>'s create
    /// path entirely, and nothing would have said so.
    /// </para>
    /// <para>
    /// <b>5a-bis widened the same margin without touching this number.</b> A real subdivider carves
    /// blocks rather than painting Tiles, so the session now holds <b>247</b> Lots at its peak
    /// against 132, and the derived sample is <b>2</b> rather than 1 — twice the looks per Day at
    /// the same length. <c>The_golden_session_raises_buildings_as_well_as_condemning_them</c> is
    /// still the assertion that makes the length mean something; if you shorten this session, that
    /// is what will tell you.
    /// </para>
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
    /// The Street lattice spacing the golden Ruleset states — <c>[roads] block_tiles</c>.
    /// </summary>
    /// <remarks>
    /// <b>A copy, and it is one on purpose.</b> It could be read off the loaded Ruleset, and then a
    /// change to <c>rulesets/minimal.toml</c> would silently move every command in this session onto
    /// different land while the fixture still read as unchanged. Stated here, that change breaks a
    /// test instead — which is what the whole directory is for. <c>GoldenSessionCoverageTests</c>
    /// holds the two to agreement.
    /// </remarks>
    internal const int BlockTiles = 32;

    /// <summary>
    /// The golden session: a population, twelve Zone commands, seven road edits, then silence.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The shape is chosen for what it can go wrong on, not for realism.</b> Two commands share a
    /// Tick, so the per-Tick slice's lower bound is exercised rather than assumed; there are gaps, so
    /// a run that applied commands by index rather than by Tick would diverge; Tick 400 holds
    /// <em>four</em> commands, which is the first time that slice has been more than a pair; and the
    /// last command lands at Tick 401 with the run going to 2,048, so most samples are of a city
    /// nobody is touching.
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
    /// <b>The Zone commands sit clear of the populated blocks.</b> The populator subdivides along
    /// lattice row 0 and stops as soon as it has land for the Buildings it wants, so every command
    /// here names a block in row 2 or above. Zoning a block the populator already carved is not an
    /// error — the claim mask refuses it face by face — but it is a <em>no-op</em>, and a session
    /// full of no-ops is a baseline that covers nothing while every hash in it still moves.
    /// </para>
    /// <para>
    /// <b>Connect is applied as of 5a-bis; Service and Govern are still declared and still throw.</b>
    /// When they land they extend this session rather than replace it, and that extension is a
    /// deliberate re-baseline — which is exactly what Connect's arrival was.
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

        Append(builder, tick: 0, block: (0, 5), zone: 1);
        Append(builder, tick: 1, block: (1, 5), zone: 1);
        Append(builder, tick: 1, block: (2, 5), zone: 2);
        Append(builder, tick: 2, block: (2, 6), zone: 2);
        Append(builder, tick: 9, block: (7, 6), zone: 3);
        Append(builder, tick: 17, block: (3, 7), zone: 1);
        Append(builder, tick: 17, block: (4, 7), zone: 1);
        Append(builder, tick: 33, block: (8, 8), zone: 4);
        Append(builder, tick: 64, block: (9, 5), zone: 2);
        Append(builder, tick: 65, block: (0, 9), zone: 3);
        Append(builder, tick: 97, block: (9, 9), zone: 5);

        // 5a-bis. The road editor, and it is here for coverage rather than for shape: a baseline
        // records what a run did, so a committed session that never edits a road leaves the Epoch,
        // re-subdivision and the refusal all outside every hash in this directory. Slice 10 task 11
        // is the precedent -- a derived sample of 1 silently stopped reaching the Zone Rule's create
        // branch and every test still passed.
        //
        // Four branches, in the order they can first be reached:
        //
        //   129  a face is bulldozed off a block nobody has zoned      -- the edit path, nothing freed
        //   130  that block is zoned                                   -- 7 Lots, not 10: a short block
        //   200  the face is laid back                                 -- re-subdivision CREATES
        //   300  it is bulldozed again                                 -- re-subdivision FREES
        //   400  all four faces of another block go                    -- four edits in one Tick
        //   401  that block is zoned                                   -- REFUSED, 0 Lots
        //
        // GoldenSessionCoverageTests asserts each of those outcomes against the replayed world, so
        // the coverage is a claim the suite checks rather than a comment.
        Connect(builder, tick: 129, node: (5, 6), StreetAxis.East, ConnectAction.Bulldoze);
        Append(builder, tick: 130, block: (5, 6), zone: 1);
        Connect(builder, tick: 200, node: (5, 6), StreetAxis.East, ConnectAction.Lay);
        Connect(builder, tick: 300, node: (5, 6), StreetAxis.East, ConnectAction.Bulldoze);

        Connect(builder, tick: 400, node: (6, 7), StreetAxis.East, ConnectAction.Bulldoze);
        Connect(builder, tick: 400, node: (6, 8), StreetAxis.East, ConnectAction.Bulldoze);
        Connect(builder, tick: 400, node: (6, 7), StreetAxis.North, ConnectAction.Bulldoze);
        Connect(builder, tick: 400, node: (7, 7), StreetAxis.North, ConnectAction.Bulldoze);
        Append(builder, tick: 401, block: (6, 7), zone: 4);

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

            // Through World.Employ rather than the column, because the worker list is derived from
            // this handle and a direct write leaves the two disagreeing -- which
            // Invariant.CitizenIsInExactlyOneWorkplace reports, and which this fixture exists to be
            // clean of. It moves no hash: the handle written is the same one.
            //
            // The stride stays here where SyntheticCity's went, and the difference is the Ruleset.
            // This world runs on Ruleset.Empty, so every kind is derelict, no ceiling exists, and
            // nothing can dismiss anybody -- where the populator's city runs on a Ruleset that
            // declares `dwelling` and grants it no jobs. A fixture whose whole job is to hold one
            // live row of every shape wants an employed Citizen among them.
            // Qualified because this class has an `int Ticks` const of its own, which shadows the
            // type. Zero is the honest planned commute for a fixture that models no journey.
            world.Employ(
                citizens[i],
                buildings[(i * 3) % buildings.Length],
                Borough.Core.Quantities.Ticks.Zero);
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

    /// <summary>
    /// Zones the block at <paramref name="block"/>, naming the Tile at its centre.
    /// </summary>
    /// <remarks>
    /// <b>The block index is what the fixture states and the Tile is derived from it</b>, because
    /// since 5a-bis zoning a Tile zones the block it falls in and a coordinate written by hand is now
    /// a coordinate that silently collides. Eight of the eleven original commands named Tiles 0–31,
    /// which is <em>one</em> block — so a straight re-record would have turned eight of them into
    /// no-ops and retired the verb from the baseline without a single test going red. The centre
    /// rather than the corner, so that nothing here depends on how <c>SubdivideAt</c> floors.
    /// </remarks>
    private static void Append(InputLogBuilder builder, ulong tick, (int Column, int Row) block, ushort zone) =>
        builder.Append(
            new Ticks(tick),
            new Command(
                CommandKind.Zone,
                new Tiles((block.Column * BlockTiles) + (BlockTiles / 2)),
                new Tiles((block.Row * BlockTiles) + (BlockTiles / 2)),
                zone));

    /// <summary>
    /// Lays or bulldozes the Street leaving lattice intersection <paramref name="node"/>.
    /// </summary>
    /// <remarks>
    /// <b>The command carries the near endpoint and the orientation, and the far endpoint is
    /// derived</b> (<c>adr/0077</c>) — so a fixture names a node and a direction, which is what a
    /// player's drag would resolve to.
    /// </remarks>
    private static void Connect(
        InputLogBuilder builder, ulong tick, (int Column, int Row) node, StreetAxis axis, ConnectAction action) =>
        builder.Append(
            new Ticks(tick),
            new Command(
                CommandKind.Connect,
                new Tiles(node.Column * BlockTiles),
                new Tiles(node.Row * BlockTiles),
                new ConnectPayload(axis, action, RoadKind.Street).Encode()));
}

using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Golden;
using Xunit.Abstractions;

namespace Borough.Tests.Persistence;

/// <summary>
/// Milestone 8 task 7 — the Factorio test, and the structural test <c>adr/0086</c> owes.
/// </summary>
/// <remarks>
/// <para>
/// <b>Run N, save, reload, run M — against a world that ran N+M.</b> This is <c>05 §4</c> invariant 6,
/// one of the two lints that has never had machinery, and it is the milestone's named risk in the only
/// place that risk can be observed. Task 1's audit measures the rebuild <em>directly</em> and this
/// measures it where a wrong derived value has had time to reach saved state: a derived column that
/// rebuilds to the wrong value is invisible at the instant of the load — the hash does not fold it —
/// and shows up only once the world has read it and written the consequence into a column that does
/// fold.
/// </para>
/// <para>
/// ⚠ <b>The round trip alone is not this test and would pass without catching anything.</b>
/// <c>SaveFileTests</c> asserts that the hash comes back; that is the write. What is asserted here is
/// that the two worlds stay equal <b>as they run on</b>, which is why every case steps both worlds in
/// lockstep and compares at every Tick rather than only at the end — a divergence compared only at the
/// end names the wrong Tick, and the Tick it happened on is most of the diagnosis.
/// </para>
/// </remarks>
public sealed class FactorioTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    private const ulong InForce = 0x0BAD_F00D_0BAD_F00DUL;

    /// <summary>
    /// The columns <see cref="Every_saved_column_reaches_the_file_and_no_other_one_does"/> cannot reach,
    /// because their table stands empty in every world the test builds. <b>Empty: every column is
    /// covered.</b> ⚠ <b>The total is deliberately NOT stated here</b> — it read <em>187</em> while
    /// the remarks below read <em>249</em>, which is one count in two places drifting in one of
    /// them. The test prints both numbers on every run; read them from there.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Named rather than counted, and asserted even at nothing.</b> A count would report that the
    /// hole went from none to one and leave the diagnosis to whoever is reading; the names cost nothing
    /// and are the whole of it. An empty literal is the strongest form this can take, and it is only
    /// empty because two of the three worlds scanned exist to make it so.
    /// </para>
    /// <para>
    /// ⚠ <b>The right response to a failure here is to make a fixture reach the table, not to move this
    /// literal.</b> A skip absorbed into a number nobody re-reads is the shape of every coverage hole
    /// this corpus has found.
    /// </para>
    /// </remarks>
    private const string UnreachableColumns = "";

    /// <summary>
    /// The Factorio test over the golden fixture's Ruleset, which churns by construction — every
    /// dwelling is condemnable 64 Ticks after it is raised, so Buildings are being created and
    /// demolished throughout.
    /// </summary>
    /// <remarks>
    /// <b>N is swept because a save is a claim about an arbitrary instant.</b> One N proves the format
    /// works at one Tick; the risk is a derived structure whose rebuild is right in the state one save
    /// happened to catch. 0 is included on purpose: a world that has been populated and never stepped
    /// is the state every guard is written against.
    /// </remarks>
    [Theory]
    [InlineData(0, 128)]
    [InlineData(1, 128)]
    [InlineData(64, 128)]
    [InlineData(129, 128)]
    [InlineData(256, 256)]
    public void A_saved_and_reloaded_world_runs_on_identically(int n, int m) =>
        AssertFactorio(GoldenFixtures.Rules(), GoldenFixtures.Population, n, m, "minimal");

    /// <summary>
    /// ⚠ <b>The same, over a Ruleset that reaches columns <c>minimal.toml</c> never touches.</b>
    /// <c>congested.toml</c> states <c>[traffic]</c> and <c>[households] car_ownership_percent</c>, so
    /// Trips, Legs, Travellers and the volume-delay function are all live — and the Movement tables
    /// joined <c>World._tables</c> in 5b. A save format tested only against the fixture is tested
    /// against the columns that fixture happens to move, which is slice 10 task 11's lesson pointed at
    /// a file rather than at a baseline.
    /// </summary>
    [Theory]
    [InlineData(64, 128)]
    [InlineData(256, 256)]
    public void A_congested_world_reloads_and_runs_on_identically(int n, int m) =>
        AssertFactorio(Congested(), GoldenFixtures.Population, n, m, "congested");

    /// <summary>
    /// ⚠ <b>The structural test <c>adr/0086</c> names in its consequences and asks not to be discovered
    /// later as a gap: the file's column set is the hash's <c>Saved</c> set.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It is asserted by corruption rather than by comparing two lists</b>, for task 1's reason.
    /// Both the writer and <c>Rows.Fold</c> read <c>SavedColumns</c>, so a list-to-list comparison would
    /// be comparing an array with itself and would pass however wrong the writer was. Scribbling a
    /// column and watching the file move is a claim about what the file <em>contains</em>.
    /// </para>
    /// <para>
    /// <b>Both directions, because either alone is half a guarantee.</b> A saved column that does not
    /// reach the file is state that silently vanishes on reload; a derived or scratch column that does
    /// reach it is state the file carries and the rebuild then overwrites, which makes the file bigger
    /// and — worse — makes it look as though the rebuild is being checked when it is not.
    /// </para>
    /// <para>
    /// ⚠ <b>SEVEN worlds, because a corruption test can only speak about a table that has rows in it,
    /// and one fixture covered 170 of the 187 columns there were then.</b> ⚠ <b>Both figures are
    /// dated and neither is the total</b> — the run prints the current one. The golden fixture
    /// leaves <c>route_hop</c> empty
    /// — 5c made the path source opt-in, so a world with nobody driving produces none — and <b>no
    /// shipped Ruleset fills <c>layer_cell</c> at all</b>, since none of the four emits pollution and
    /// <c>SetLandValueTarget</c> has only test callers. ⚠ <b>The fourth is milestone 10 task 4b's</b>:
    /// <c>business</c> has no production writer either, so only <c>GoldenFixtures.Build()</c> holds one.
    /// ***A structural test over one fixture measures the fixture's content as much as the
    /// structure***, and the corollary this keeps re-proving is that <b>a table with no production
    /// writer needs a fixture named for it or its columns are carried by the format and checked by
    /// nothing</b>. The union of the SEVEN is every column of every table, and <see cref="UnreachableColumns"/> pins that
    /// there is no residue.
    /// </para>
    /// </remarks>
    [Fact]
    public void Every_saved_column_reaches_the_file_and_no_other_one_does()
    {
        HashSet<string> reached = [];
        List<string> every = [];

        Scan(Stepped(GoldenFixtures.Rules(), GoldenFixtures.Population, 512).World, reached, every);
        Scan(Stepped(Congested(), GoldenFixtures.Population, 512).World, reached, []);
        Scan(WithLayerCells(512), reached, []);
        var shopping = Borough.Tests.Rules.ShoppingTests.Start();
        for (int tick = 0; tick < 1024; tick++) { shopping.Sim.Step(default); }
        Scan(shopping.World, reached, []);

        // The golden fixture, milestone 10 task 4b, and it is here for the reason the paragraph above
        // gives rather than a new one: `business` has no production writer -- no pass places one,
        // because what would is milestone 13's commercial placement -- so a stepped world leaves the
        // table empty and all FOUR of its saved columns unreachable -- `building`, `kind`, `bin_head`
        // and `bin_tail`. This fixture is the only world that holds a Business.
        //
        // ⚠ The count said `five` until 2026-08-24 and was never right: `business` had THREE saved
        // columns when this fixture was added and milestone 27 task 6's `kind` is the fourth. Counted
        // from BusinessTable's constructor rather than carried forward, which is the only way a
        // number in a comment beside a table stays true.
        Scan(GoldenFixtures.Build(), reached, []);

        // Milestone 12 task 3, and the fourth fixture's reason a second time: the watershed writes
        // `district` and `district_cell` only where the Ruleset states [districts], which one shipped
        // file does. See WithDistricts.
        Scan(WithDistricts(512), reached, []);

        // Milestone 25 task 5, and the SIXTH fixture for the fourth's reason a third time. The
        // unpremised pool has a production writer -- DestroyBuilding -- but it can only fire on a
        // Building that HAS a Business in it, and nothing creates one. So every world above leaves
        // the table empty and all THREE of its saved columns unreachable -- `business`, `gate` and
        // `since`; it was two until milestone 27 task 8 declared `gate` (adr/0145) --
        // exactly as `business` was before GoldenFixtures.Build() was added for it. The `five` here
        // was copied from the paragraph above on the day it was written; UnpremisedTable has only
        // ever had two.
        Scan(WithUnpremised(), reached, []);

        // The seventh: the water graph is laid only on a Ruleset that states [water], and coastal.toml
        // is the only shipped file that does. Milestone 24 task 6a, adr/0160.
        Scan(WithWater(512), reached, []);

        // 🔴 The NINTH, milestone 28, and it is the fourth's reason again with the gate one step
        // earlier. `policy` has a production writer -- PolicyTable's own constructor -- but it
        // allocates one row per declared [[policy]] and SEVEN of the shipped files declare none, the
        // golden fixture among them. So the table is empty in every world above and all six of its
        // saved columns were unreachable the day it landed.
        //
        // ⚠ A row here is created by DECLARATION rather than by anything happening, which is why 256
        // Ticks is plenty: taxed.toml's two Policies exist at Tick 0 and the run only has to reach a
        // save.
        Scan(WithPolicies(256), reached, []);

        // 🔴 The EIGHTH, milestone 17, and it is the fourth's reason arriving from the opposite
        // direction. Every fixture above leaves `unplaced` empty and all SEVEN of its saved columns
        // unreachable -- `id`, `generation`, `free_next`, `household`, `gate`, `since` and
        // `considered` -- because a Household reaches the Unplaced Pool by being turned out of a
        // condemned Building, and adr/0164 moved decline out of minimal.toml.
        //
        // ***What is worth noticing is that the coverage did not shrink by anyone editing this
        // file.*** A Ruleset three directories away stopped demonstrating a mechanism, and seven
        // saved columns quietly stopped reaching the save with every other test still green. That is
        // the same day's finding as DerivedRebuildAuditTests.Declining, on the saved half instead of
        // the derived one, and the pair of them is the argument for asserting coverage BY NAME.
        //
        // ⚠ 8,192 Ticks is derived: declining.toml condemns on a 2-Day threshold and collapses a Day
        // later, so nobody is turned out before 6,144.
        Scan(Stepped(GoldenFixtures.DecliningRules(), GoldenFixtures.Population, 8_192).World, reached, []);

        // 🔴 The TENTH, plans/0045 row 12, and it is the seventh's reason one key further on.
        // `disaster` and `inundation` have rows only while a flood is in progress, and a flood is
        // scheduled only by [disasters] -- which coastal.toml does not state, because a world with a
        // floodplain and no floods is a world. So both tables were empty in every fixture above and
        // all EIGHTEEN of their saved columns were unreachable the day they landed.
        //
        // ⚠ 4,200 Ticks is derived and not chosen: flooded.toml states flood_every_days = 2, and
        // Begin refuses Tick 0, so the first flood in that world starts at Tick 4,096. A run that
        // stopped at 4,095 would leave both tables empty and this whole comment would be about
        // fixtures rather than about a mechanism.
        Scan(WithFloods(4_200), reached, []);

        List<string> unreachable = [.. every.Where(name => !reached.Contains(name))];

        _output.WriteLine($"{reached.Count} of {every.Count} columns corrupted and observed");
        _output.WriteLine(
            unreachable.Count == 0
                ? "every column was reachable"
                : $"{unreachable.Count} unreachable: {string.Join(", ", unreachable)}");

        Assert.True(reached.Count > 100, $"only {reached.Count} columns were exercised.");

        // Pinned by name rather than tolerated. A column corruption cannot reach is a column this test
        // does not cover, and naming them means a NEW one has to be looked at rather than absorbed into
        // a skip nobody counts -- which is the shape of every coverage hole this corpus has found.
        Assert.Equal(UnreachableColumns, string.Join(", ", unreachable));
    }

    /// <summary>
    /// Scribbles every column of one world in turn and records which ones the file noticed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A column whose table is empty cannot be scribbled, so it is neither confirmed nor faulted
    /// here</b> — it is left out of <paramref name="reached"/> and the caller decides what to do about
    /// it. That is why this runs over three worlds: a table empty under one Ruleset may be full under
    /// another, and the coverage is the union rather than either one.
    /// </para>
    /// <para>
    /// ⚠ <b>Both buffers are reused, and that is a correctness requirement of the <em>suite</em> rather
    /// than a tidiness of this test.</b> The obvious shape — write a fresh file per column — produces
    /// 187 files a world and ~300 MB of garbage across the three, and the first version did exactly
    /// that. It passed on its own and made <b>two unrelated allocation assertions fail</b>
    /// (<c>QuantityTests.Arithmetic_on_quantities_allocates_nothing</c>, which measures arithmetic that
    /// cannot allocate at all), because <c>GC.GetAllocatedBytesForCurrentThread</c> is served out of a
    /// per-thread allocation context that a collection on <em>another</em> thread flushes. ***A test
    /// that allocates heavily is not a local decision in a suite that runs in parallel and asserts on
    /// allocation.*** See <c>plans/0030</c>, task 7.
    /// </para>
    /// </remarks>
    private static void Scan(World world, HashSet<string> reached, List<string> every)
    {
        var probe = new WorldSnapshot();
        SaveFile.Write(world, InForce, probe);

        byte[] clean = probe.Bytes.ToArray();
        byte[] before = new byte[Widest(world)];

        foreach (Rows table in world.Tables)
        {
            foreach (Column column in table.Columns)
            {
                string name = $"{table.Name}.{column.Name} ({column.Disposition})";
                every.Add(name);

                // ⚠ A column corruption cannot reach is not a column this scan covers, and a silent
                // skip would read as coverage. Both causes are structural rather than accidental: an
                // empty table has no bytes to scribble, and a zero-width column has none either.
                if (table.SlotCount == 0 || column.BytesPerRow == 0)
                {
                    continue;
                }

                Span<byte> storage = column.StorageBytes(table.SlotCount);
                Span<byte> saved = before.AsSpan(0, storage.Length);

                storage.CopyTo(saved);
                storage.Fill(0x5A);

                probe.Reset();
                SaveFile.Write(world, InForce, probe);

                bool moved = !probe.Bytes.SequenceEqual(clean);

                saved.CopyTo(column.StorageBytes(table.SlotCount));
                reached.Add(name);

                if (column.Disposition == Disposition.Saved)
                {
                    Assert.True(moved, $"saved column '{name}' does not reach the file.");
                }
                else
                {
                    Assert.False(moved, $"'{name}' is {column.Disposition} and reaches the file.");
                }
            }
        }
    }

    /// <summary>
    /// A save of a reloaded world is byte-identical to the save it came from. The strongest statement
    /// available about the rebuild that does not require running on.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>It is strictly weaker than the Factorio test and is here for what it says when it fails.</b>
    /// A derived column that rebuilds wrongly leaves this green, because derived columns are not in the
    /// file. What this catches is a <em>saved</em> column that the rebuild overwrites — a field declared
    /// saved that something rebuilds anyway, which is the declaration being wrong rather than the
    /// rebuild.
    /// </remarks>
    [Fact]
    public void A_save_of_a_reloaded_world_is_byte_identical()
    {
        World world = Stepped(GoldenFixtures.Rules(), GoldenFixtures.Population, 512).World;

        byte[] first = FileOf(world);

        var source = new MemorySave();
        source.Write(first);

        World loaded = SaveFile.Read(source, GoldenFixtures.Rules(), out _);

        Assert.Equal(first, FileOf(loaded));
    }

    private void AssertFactorio(Ruleset rules, int population, int n, int m, string label)
    {
        // The control: one world that runs the whole way.
        (World control, Simulation controlSimulation) = Stepped(rules, population, n);

        // The subject: the same world, saved at N and reloaded.
        (World subject, Simulation subjectSimulation) = Stepped(rules, population, n);

        var file = new MemorySave();
        subjectSimulation.SaveAtEndOfTick(file);
        subjectSimulation.Step(default);
        controlSimulation.Step(default);

        World reloaded = SaveFile.Read(file, rules, out SaveHeader header);
        var resumed = new Simulation(reloaded, header.Key);

        Assert.Equal(control.HashState(), reloaded.HashState());

        for (int tick = 0; tick < m; tick++)
        {
            controlSimulation.Step(default);
            resumed.Step(default);

            Assert.Equal(
                (object)$"tick {n + 1 + tick}: {control.HashState():X16}",
                $"tick {n + 1 + tick}: {reloaded.HashState():X16}");
        }

        _output.WriteLine(
            $"{label}: saved at {n}, ran on {m}, {file.Bytes.Length:N0} B, "
            + $"hash {control.HashState():X16}");

        Assert.Equal(control.HashState(), reloaded.HashState());
    }

    /// <summary>
    /// The widest column in a world, in bytes — the one scratch buffer <see cref="Scan"/> needs, sized
    /// once. <b>Every column and not just the saved ones</b>, because the scan scribbles derived and
    /// scratch columns too and has to put them back.
    /// </summary>
    private static int Widest(World world)
    {
        int widest = 0;

        foreach (Rows table in world.Tables)
        {
            foreach (Column column in table.Columns)
            {
                int width = column.BytesPerRow * table.SlotCount;

                if (width > widest)
                {
                    widest = width;
                }
            }
        }

        return widest;
    }

    private static byte[] FileOf(World world)
    {
        var file = new MemorySave();
        SaveFile.Write(world, InForce, file);

        return file.Bytes;
    }

    /// <summary>
    /// ⚠ <b>A world with Map Layer Cells in it, which no shipped Ruleset produces.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>LayerCellTable</c> is sparse — a Cell gets a row when something writes to it — and
    /// <b>nothing in any of the four shipped Rulesets ever writes to one</b>. Each of them says so in
    /// its own header: <em>nothing in this file emits pollution — a dwelling is not industry — so the
    /// field is zero everywhere</em>. Land value is the same story from the other end, since
    /// <c>MapLayers.SetLandValueTarget</c> has only test callers. So the table stands at zero rows in
    /// every world a Ruleset can build, and its eleven columns are unreachable by corruption on all
    /// of them.
    /// </para>
    /// <para>
    /// <b>The fixture is made to reach the table rather than the hole being pinned</b>, which is the
    /// choice the literal above asks for. A save format tested only against the tables the shipped
    /// content happens to fill is tested against today's content, and the columns it would miss are
    /// exactly the ones nobody is watching — slice 10 task 11's lesson pointed at a save rather than
    /// at a baseline.
    /// </para>
    /// </remarks>
    private static World WithLayerCells(int ticks)
    {
        (World world, Simulation simulation) = Stepped(GoldenFixtures.Rules(), GoldenFixtures.Population, 0);

        var east = new Cells(4);
        var north = new Cells(4);

        world.Layers.EmitPollution(east, north, 4096);
        world.Layers.SetLandValueTarget(east, north, 2048);
        world.Layers.Seal(east, north, 16);

        // Past both cadences -- pollution every 64 Ticks, land value every 256 -- so the derived
        // halves of the table are the ones diffusion and the momentum operator wrote rather than the
        // ones the three calls above did.
        for (int tick = 0; tick < ticks; tick++)
        {
            simulation.Step(default);
        }

        Assert.True(world.Layers.Cells.Rows.SlotCount > 0, "the Layer table is still empty.");

        return world;
    }

    private static (World World, Simulation Simulation) Stepped(Ruleset rules, int population, int ticks)
    {
        var key = WorldKey.FromSeed(GoldenFixtures.Seed);
        var world = new World(population, rules, key);
        var simulation = new Simulation(world, key);

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        for (int tick = 0; tick < ticks; tick++)
        {
            simulation.Step(default);
        }

        return (world, simulation);
    }

    private static Ruleset Congested() => Shipped("congested.toml");

    /// <summary>
    /// A world with Districts in it — the only Ruleset that states <c>[districts]</c>, stepped.
    /// </summary>
    /// <remarks>
    /// <b>A fifth fixture, for the fourth's reason exactly</b>, and the paragraph above predicted it:
    /// <c>district</c> and <c>district_cell</c> have a production writer, but it reads a Ruleset key
    /// only <c>twinned.toml</c> states, so every other world in this test leaves both tables empty and
    /// all eleven of their saved columns unreachable. ⚠ <b>A table whose writer is gated on a Ruleset
    /// key is a table with no production writer, as far as a fixture is concerned.</b>
    /// </remarks>
    private static World WithDistricts(int ticks) =>
        Stepped(Shipped("twinned.toml"), GoldenFixtures.Population, ticks).World;

    /// <summary>A world with a sea in it, so the water graph has rows to corrupt.</summary>
    /// <remarks>
    /// <b>A seventh fixture, and this test's corollary arriving for the fourth time</b>: ***a table
    /// with no production writer needs a fixture named for it.*** Here the writer is real and runs on
    /// every world — <c>WaterGenerator.LayInto</c>, from <c>SyntheticCity.PopulateInto</c> — but it
    /// lays nothing at all unless the Ruleset states <c>[water]</c>, and <c>coastal.toml</c> is the
    /// only shipped file that does (<c>adr/0160</c>). ⚠ <b>Gated on a Ruleset key</b>, which is
    /// <see cref="WithDistricts"/>'s shape exactly rather than a new one.
    /// </remarks>
    private static World WithWater(int ticks) =>
        Stepped(Shipped("coastal.toml"), GoldenFixtures.Population, ticks).World;

    /// <summary>A world with a flood in progress, so the Disaster tables have rows to corrupt.</summary>
    /// <remarks>
    /// <b>A tenth fixture, gated on a Ruleset key</b> — <see cref="WithWater"/>'s shape one key on —
    /// and <c>flooded.toml</c> is the only shipped file stating <c>[disasters]</c> at all. ⚠ <b>The
    /// rows exist only while water is standing</b>, unlike <see cref="WithPolicies"/>'s: a Disaster
    /// is created by the schedule and freed when it has finished receding, so a run that stops
    /// between two floods leaves both tables empty and this fixture buys nothing.
    /// </remarks>
    private static World WithFloods(int ticks) =>
        Stepped(Shipped("flooded.toml"), GoldenFixtures.Population, ticks).World;

    /// <summary>A world that declares Policies, so the governed table has rows to corrupt.</summary>
    /// <remarks>
    /// <b>A ninth fixture, gated on a Ruleset key</b> — <see cref="WithDistricts"/>'s shape — and
    /// <c>taxed.toml</c> is the smallest shipped file declaring a <c>[[policy]]</c> at all. ⚠ <b>The
    /// rows exist whether or not anybody has governed one</b>: <c>PolicyTable</c> allocates per
    /// declaration, so what this fixture buys is that the columns reach the file, not that the verb
    /// was used.
    /// </remarks>
    private static World WithPolicies(int ticks) =>
        Stepped(Shipped("taxed.toml"), GoldenFixtures.Population, ticks).World;

    /// <summary>
    /// A world with a Business in the unpremised pool, its premises demolished under it.
    /// </summary>
    /// <remarks>
    /// <b>A sixth fixture, and this test's own corollary arriving for the third time</b>: ***a table
    /// with no production writer needs a fixture named for it, or its columns are carried by the
    /// format and checked by nothing.*** The writer here is <c>World.DestroyBuilding</c>, which is
    /// real and reachable — but it can only put a row in this table if the Building had a Business in
    /// it, and <c>World.CreateBusiness</c> has no <c>src/</c> caller until milestone <b>27 task 8</b>.
    /// ⚠ <b>A writer gated on a row nothing creates is a writer no fixture reaches</b>, which is
    /// <see cref="WithDistricts"/>'s *gated on a Ruleset key* one step along.
    /// </remarks>
    private static World WithUnpremised()
    {
        var world = new World(GoldenFixtures.Population, GoldenFixtures.Rules());

        Handle<Lot> lot = world.Lots.Create(new Tiles(1), new Tiles(2), zone: 1);
        Handle<Building> premises = world.Buildings.Create(world.Lots, lot, kind: 1);

        world.CreateBusiness(premises);
        world.DestroyBuilding(premises, Ticks.Zero);

        return world;
    }

    private static Ruleset Shipped(string file)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Rulesets", file);
        RulesetLoadResult result = RulesetLoader.Load(path);

        return result.Ruleset
            ?? throw new InvalidOperationException($"{file} was refused:\n{result.Describe()}");
    }
}

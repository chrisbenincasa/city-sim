using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Tests.Golden;

namespace Borough.Tests.Entities;

/// <summary>
/// <c>adr/0006</c>'s long run, aimed at the subdivider: <b>100,000 Ticks of a city whose roads are
/// being edited throughout, with no collection and no magnitude trending upward.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the run <c>RoadLongRunTests</c> could not do and said so.</b> That file opens by
/// conceding that nothing in milestone 5a <em>edits</em> the Road Graph — the generator lays it at
/// world creation and no Tick phase writes a Segment — so its <c>adr/0006</c> question had an answer
/// known in advance. 5a-bis is what supplies the edits, and with them the first way for the graph and
/// the Lot table to grow with elapsed time.
/// </para>
/// <para>
/// <b>The failure this exists to catch is a lay/bulldoze cycle that does not close.</b> Laying a
/// Street carves Lots; bulldozing it frees the vacant ones. If the two are not exact inverses — a
/// claim not released, a Lot created twice, a Segment allocated on every lay while the old one is
/// merely orphaned — the city grows a little on every cycle, and nothing else in the suite would
/// notice: every hash moves, every count is plausible, and the drift is a few rows per edit.
/// </para>
/// <para>
/// <b>The assertion is <em>no upward trend</em> rather than equality across cycles, and the first
/// draft got that wrong in an instructive way.</b> Equality looked right — the period is known, and
/// slice 5 task 7's flow half made exactly that choice — and it failed at 140 against 138. The cause
/// is not a leak: a Building standing on the edited face survives the bulldoze
/// (<c>adr/0079</c>), so its Lot is preserved rather than freed, and the freeing is <b>deferred to
/// whenever the Zone Rules get round to condemning it</b>. Whether that has happened by the next
/// reading depends on the sweep's sample, which is a different clock.
/// </para>
/// <para>
/// <b>So the cycle is closed but not synchronous, and a test that demands synchrony is testing the
/// Zone Rule's cadence while claiming to test the subdivider.</b> <c>adr/0006</c>'s own wording is
/// what this asserts instead: nothing trending upward <em>at steady state</em>. A leak of even one
/// row per cycle shows up as a later quarter exceeding an earlier one over 195 edits; an oscillation
/// with a bounded amplitude does not.
/// </para>
/// </remarks>
public sealed class LotLongRunTests
{
    private const int TickCount = 100_000;
    private const int Population = 1_000;

    /// <summary>Ticks between road edits. Long enough that the Zone Rules run in between.</summary>
    private const int EditEvery = 512;

    /// <summary>The block whose south face is laid and bulldozed for the length of the run.</summary>
    /// <remarks>
    /// <b>(60, 60) until 2026-08-13, and the generator stopped reaching it.</b> Since
    /// <c>SyntheticCity</c> paves the ground its Lots need rather than the map, a 1,000-Citizen world
    /// has a <b>5×5</b> block lattice, and block 60 is bare land — so the lay carved nothing and the
    /// run asserted equality over an edit that never happened. The vacuity guard below caught it,
    /// which is the whole reason that guard exists. (3, 3) is inside the lattice and outside the
    /// blocks the populator subdivides, which walks row-major and stops partway through row 2.
    /// </remarks>
    private const int Column = 3;

    /// <summary>The lattice row of that face.</summary>
    private const int Row = 3;

    /// <summary>
    /// A hundred thousand Ticks of road editing leaves the city no larger than one cycle did.
    /// </summary>
    [Fact]
    public void The_hundred_thousand_Tick_lot_run()
    {
        int[] afterBulldoze = [];
        int[] afterLay = [];

        Cycle(out World world, out int openingSegmentSlots, ref afterBulldoze, ref afterLay);

        Assert.NotEmpty(afterLay);
        Assert.NotEmpty(afterBulldoze);

        // Vacuity: the edits did something. Without this the equalities below hold trivially over a
        // command the simulation refused, which is the shape adr/0070 warns about from the other end.
        //
        // ⚠ READ OVER THE WHOLE SERIES SINCE 2026-08-13, AND IT WAS afterLay[0] > afterBulldoze[0]
        // BEFORE. A lay carves a Lot back only where the bulldoze freed one, and a bulldoze frees
        // nothing on a face whose Lots are occupied -- adr/0079, a Building outlives its frontage. So
        // whether the FIRST edit moves anything is a question about what the Zone Rule had built by
        // then, which is a cadence. adr/0094 took Ticks.PerDay to 2048, revisit_ticks with it, and the
        // derived sample quadrupled: the block now has Buildings on that face by the first edit and
        // this guard fired on a run where 41 of 97 edits carved Lots perfectly well. A GUARD AGAINST
        // VACUITY THAT READS ONE SAMPLE CAN ITSELF BE DEFEATED BY TIMING -- the fourth time in two
        // days that a single draw has been mistaken for a property.
        int moved = 0;

        for (int i = 0; i < afterLay.Length; i++)
        {
            if (afterLay[i] > afterBulldoze[i])
            {
                moved++;
            }
        }

        Assert.True(
            moved * 4 >= afterLay.Length,
            $"laying the Street carved Lots on only {moved} of {afterLay.Length} edits, so this run "
            + "mostly asserts equality over an edit that did nothing. Either the editor stopped "
            + "carving or the face is occupied throughout, and the second is a statement about the "
            + "Zone Rule rather than about the subdivider.");

        // No upward trend, stated as a comparison of quarters. A leak of k rows per cycle makes the
        // last quarter exceed the first by roughly k times a quarter of the edits, which is well
        // clear of the oscillation the deferred freeing produces.
        // ⚠ THE OSCILLATION'S OWN AMPLITUDE IS ALLOWED FOR, since 2026-08-13, and it should have been
        // from the start: this file declares LotsOnAFace as "the whole amplitude this run's
        // oscillation can have" and then compared two maxima with no tolerance at all, so it was
        // asserting that the larger of the two happened to land in the first half. It did, until the
        // generator stopped paving the map and the edited block moved from (60, 60) to (3, 3); then
        // it read 133 against 132 and called a one-row oscillation a leak. A LEAK IS NOT A ROW: the
        // comment below states the arithmetic -- k rows per cycle puts the last quarter roughly
        // k x 48 above the first over this run's 195 edits -- so a bound of 3 costs the tripwire
        // nothing and removes an assertion about which half a maximum fell in.
        Assert.True(
            Peak(afterLay, half: true) <= Peak(afterLay, half: false) + LotsOnAFace,
            $"the Lot count after a lay peaks at {Peak(afterLay, half: true)} in the second half of "
            + $"the run against {Peak(afterLay, half: false)} in the first. A lay/bulldoze cycle that "
            + "does not close leaks a few rows per edit, and this is the only test that can see it.");

        Assert.True(
            Peak(afterBulldoze, half: true) <= Peak(afterBulldoze, half: false) + LotsOnAFace,
            $"the Lot count after a bulldoze peaks at {Peak(afterBulldoze, half: true)} in the second "
            + $"half against {Peak(afterBulldoze, half: false)} in the first.");

        // And the amplitude is bounded by the face itself, so the oscillation above is the deferred
        // freeing and not something larger hiding inside a trend that happens to be flat.
        foreach (int reading in afterLay)
        {
            Assert.InRange(reading, afterLay[0] - LotsOnAFace, afterLay[0] + LotsOnAFace);
        }

        // The graph itself, for the same reason: a lay that allocated a fresh Segment every time
        // while orphaning the last would keep the Lot counts perfectly stable.
        //
        // Against the graph as generated, not against its live count. The generator already leaves
        // free slots behind -- an Arterial destroys the Streets it runs over, which is Severance and
        // is the mechanism 5a exists for -- so "slots == live + 1" was never true of this table and
        // asserting it measured the generator rather than the editor. What the editor owes is that
        // the table does not GROW: a bulldoze frees a slot and the next lay takes that slot back.
        Assert.Equal(afterLay.Length + afterBulldoze.Length, Edits(TickCount));
        Assert.Equal(openingSegmentSlots, world.Roads.Segments.Rows.SlotCount);
    }

    /// <summary>
    /// The whole-world invariants still hold after a run of road editing.
    /// </summary>
    /// <remarks>
    /// <b><c>VacantLotHasFrontage</c> is the one this run exists for</b>, and it cannot be checked on
    /// a world nobody has edited: a Lot loses its Street only when somebody bulldozes one, so before
    /// 5a-bis the invariant was true for want of a mechanism that could break it (<c>adr/0070</c>).
    /// </remarks>
    [Fact]
    public void The_world_is_still_coherent_after_a_run_of_road_editing()
    {
        int[] afterBulldoze = [];
        int[] afterLay = [];

        Cycle(out World world, out _, ref afterBulldoze, ref afterLay);

        // Throws with the violated invariant's id and the rows it names. Asserting on a returned
        // report would need a second reporting path; the panic is the one the crash artifact carries.
        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// Lots on one side of one Segment — the whole amplitude this run's oscillation can have.
    /// </summary>
    /// <remarks>
    /// A block takes the Left side of its south face, and Left is the even indices {0, 2, 4} of five
    /// (<c>adr/0078</c>). Three, not two and a half: the sides alternate and five is odd.
    /// </remarks>
    private const int LotsOnAFace = 3;

    /// <summary>The largest reading in one half of a series.</summary>
    private static int Peak(int[] readings, bool half)
    {
        int from = half ? readings.Length / 2 : 0;
        int to = half ? readings.Length : readings.Length / 2;
        int peak = int.MinValue;

        for (int i = from; i < to; i++)
        {
            if (readings[i] > peak)
            {
                peak = readings[i];
            }
        }

        return peak;
    }

    /// <summary>How many road edits a run of <paramref name="ticks"/> Ticks issues.</summary>
    private static int Edits(int ticks) => ticks / EditEvery;

    /// <summary>
    /// Runs the session, reading the Lot count after every edit.
    /// </summary>
    /// <remarks>
    /// <b>The same face, alternately laid and bulldozed.</b> A random walk over the lattice would
    /// cover more of the editor and would make the readings incomparable, which is the wrong trade for
    /// a test whose whole claim is that two numbers do not move.
    /// </remarks>
    private static void Cycle(
        out World world, out int openingSegmentSlots, ref int[] afterBulldoze, ref int[] afterLay)
    {
        var key = WorldKey.FromSeed(GoldenFixtures.Seed);

        world = new World(Population, GoldenFixtures.Rules());

        var simulation = new Simulation(world, key)
        {
            VerifyDecideWritesNothing = false,
        };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        int block = world.Roads.Streets.BlockTiles;

        openingSegmentSlots = world.Roads.Segments.Rows.SlotCount;

        // Zoned once, at the start. The block keeps its permission set through every cycle because
        // the Lots on its other three faces survive -- which is also the mechanism under test: a
        // block that lost every Lot would forget it was zoned, and the run would go quiet.
        simulation.Step(new TickInput(
            [
                new Command(
                    CommandKind.Zone,
                    new Tiles((Column * block) + (block / 2)),
                    new Tiles((Row * block) + (block / 2)),
                    zone: 1),
            ],
            rulesetHash: 0));

        List<int> laid = [];
        List<int> cleared = [];

        for (int tick = 1; tick < TickCount; tick++)
        {
            if (tick % EditEvery != 0)
            {
                simulation.Step(default);
                continue;
            }

            bool bulldoze = (tick / EditEvery % 2) == 1;

            simulation.Step(new TickInput([Edit(block, bulldoze)], rulesetHash: 0));

            (bulldoze ? cleared : laid).Add(world.Lots.Rows.LiveCount);
        }

        afterLay = [.. laid];
        afterBulldoze = [.. cleared];
    }

    /// <summary>The road edit, in one direction or the other.</summary>
    private static Command Edit(int block, bool bulldoze) =>
        new(
            CommandKind.Connect,
            new Tiles(Column * block),
            new Tiles(Row * block),
            new ConnectPayload(
                StreetAxis.East,
                bulldoze ? ConnectAction.Bulldoze : ConnectAction.Lay,
                RoadKind.Street).Encode());
}

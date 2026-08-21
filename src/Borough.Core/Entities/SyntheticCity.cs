namespace Borough.Core.Entities;

using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
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
    /// <summary>The kind this populator raises, and the only one it knows.</summary>
    private const byte DwellingKind = 1;

    /// <summary>
    /// Households per Building where the Ruleset declares no occupancy for
    /// <see cref="DwellingKind"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Not a tuning number, and it is the reason this is not one</b> (<c>CLAUDE.md</c>: *no tuning
    /// number is a <c>const</c> in simulation source*). Under <c>adr/0068</c> the figure comes from
    /// the file; ~~<c>HouseholdsPerBuilding = 3</c>~~ used to live here as S4 task 2's row ratio and
    /// was the second half of the disagreement that entry records — the populator put **3** in every
    /// Building and a Zone Rule put **1**, and neither number was expressible.
    /// </para>
    /// <para>
    /// <b>One is the arithmetic floor rather than a choice.</b> A populator must house what it
    /// creates, so where the Ruleset states nothing the only assumption that houses everybody is the
    /// smallest one: you cannot put two families in a thing that does not say it holds two. It is
    /// reached only by a world running <see cref="Ruleset.Empty"/> — <c>--citizens</c> with no
    /// <c>--ruleset</c>, which is S0a's footprint capture — and there the city is degenerate already,
    /// since kind 1 gets no Bins and no Rules either.
    /// </para>
    /// </remarks>
    private const int UndeclaredOccupancy = 1;

    /// <summary>Lots per row, before the strip wraps northward.</summary>
    private const int LotsPerRow = 64;

    /// <summary>
    /// Fills <paramref name="world"/> to the Citizen count it was configured with, laying the land
    /// first.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The size comes from the world rather than from an argument, and that is what keeps one
    /// number in one place.</b> <see cref="Input.WorldConfiguration.Citizens"/> is already in the log,
    /// already sizes every table, and is already what <c>--citizens</c> sets. A count on the command
    /// too would let a log state two populations, which is the same disagreement
    /// <c>Borough.Headless</c> refuses <c>--citizens</c> alongside <c>--log</c> to avoid.
    /// </para>
    /// <para>
    /// <b>It does two jobs, and since 2026-08-15 a caller can ask for the second alone.</b> It makes
    /// <em>land</em> — <see cref="RoadGenerator.LayInto"/> and then <c>Subdivide</c> — and it makes
    /// <em>people</em>, which is <see cref="PeopleInto"/>. A city whose Streets were laid by
    /// <c>CommandKind.Connect</c> wants the people half without the land half and could not ask for
    /// it, because the generator <em>throws</em> on a world that already has Segments; see
    /// <see cref="PeopleInto"/> for why that refusal is correct and the welding was the defect.
    /// </para>
    /// </remarks>
    /// <param name="world">The world to fill. Must have no Citizens in it.</param>
    /// <param name="key">The world key, which the Rule arming stagger draws against.</param>
    /// <param name="now">The Tick the population arrives on, which arming is relative to.</param>
    /// <exception cref="InvalidOperationException">The world already has a population.</exception>
    public static void PopulateInto(World world, WorldKey key, Ticks now)
    {
        ArgumentNullException.ThrowIfNull(world);

        RefuseIfPopulated(world);
        LayLand(world, key);
        PeopleInto(world, key, now);
    }

    /// <summary>
    /// Raises Buildings, Households and Citizens on whatever Lots already stand.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="PopulateInto"/>'s people half, and the door through which a player-shaped network
    /// gets a population.</b> <see cref="RoadGenerator.LayInto"/> throws on a world that already has
    /// Segments and <see cref="PopulateInto"/> called it unconditionally, before it built a row — so a
    /// city laid by <c>CommandKind.Connect</c> could not be populated in either order, and the one
    /// fixture that needed such a city grew its own population by copying the three loops below.
    /// <b>The refusal was never the defect</b>: the generator is a world-creation pass and editing a
    /// standing graph is <c>Connect</c>, exactly as it says. The defect was that no caller could ask
    /// for one of this class's two jobs. <c>plans/0003</c> hash-moving queue item 9.
    /// </para>
    /// <para>
    /// ⚠ <b>Dropping the road pass alone would not have been the repair, which is why the split is a
    /// second entry point rather than a flag on the first.</b> <c>Subdivide</c> is keyed on
    /// <see cref="StreetGrid.Blocks"/>, which is <c>WorldTiles ÷ block_tiles + 1</c> — a property of
    /// the <em>map</em> and the Ruleset, and never of what is laid — so on a Connect-laid world it
    /// would sweep the whole map's lattice and carve zoned Lots against whatever Segments it happened
    /// to find. <b>The land half is wrong for such a world twice over, and only the first failure
    /// announces itself by throwing.</b> Here it is not reached at all: a Connect-laid city's Lots are
    /// the ones that verb re-subdivided as it laid them (<c>adr/0078</c>).
    /// </para>
    /// <para>
    /// <b>It is an instrument and not a second verb, so the verb count does not move.</b>
    /// <c>CommandKind.Populate</c> is expected to be deleted when the player can grow a city instead
    /// of declaring one, so a payload on it would rest on something already scheduled to go. This is
    /// <c>adr/0080</c>'s precedent for <c>TripPurpose.Commanded</c> — a test affordance rather than
    /// the only door — and a world populated this way is a <em>measurement</em> rather than a session,
    /// because nothing about it is in the Input Log.
    /// </para>
    /// </remarks>
    /// <param name="world">The world to fill. Must have no Citizens in it.</param>
    /// <param name="key">The world key, which the Rule arming stagger draws against.</param>
    /// <param name="now">The Tick the population arrives on, which arming is relative to.</param>
    /// <exception cref="InvalidOperationException">The world already has a population.</exception>
    public static void PeopleInto(World world, WorldKey key, Ticks now)
    {
        ArgumentNullException.ThrowIfNull(world);

        RefuseIfPopulated(world);

        int population = world.Citizens.Rows.Capacity;
        int households = Households(world);
        int buildings = WantedBuildings(world);

        // The populator must house what it creates, so the Building count follows the land that
        // actually stands rather than the other way round. On the shipped [roads] a generated map
        // yields far more Lots than 1M Citizens need, so this binds only where the street network is
        // too sparse for its population -- which is a real city the fixture should be able to express
        // rather than an error, and on a Connect-laid one it is the ordinary case.
        //
        // It reads the table rather than Subdivide's return value, so that the two entry points share
        // one expression. They agree because the Lot table starts empty at world creation, which is
        // the same premise Dwelling's remark rests on; if they ever stopped agreeing the golden
        // baselines would move, which is what the acceptance run checks.
        int lots = world.Lots.Rows.LiveCount;

        // ⚠ Refused rather than degraded, and opening this door is what made the case reachable.
        // PopulateInto cannot get here -- Subdivide's degenerate branch lays `wanted` Lots when there
        // is no lattice, so it always returns at least one -- but a caller who has laid Streets and
        // not zoned them can, and clamping to zero would divide by zero in the Household loop.
        //
        // Silence is the wrong answer for the reason Subdivide states about itself: a populator that
        // makes no rows answers the sizing question with an empty world and reports success. The land
        // is the caller's to lay, so the caller is told it laid none.
        if (lots <= 0)
        {
            throw new InvalidOperationException(
                "this world has no Lots, so there is nowhere to put anybody. A city laid by "
                + "CommandKind.Connect gets its Lots from Zone, which carves against the Street "
                + "faces that are standing -- so lay the Streets, zone the blocks, then populate.");
        }

        // THE GATES GO UP FIRST, and the order is forced rather than tidy. An Outside Connection is
        // constrained to a map edge (adr/0088) and an edge Lot is an EARLY Lot -- Subdivide walks
        // blocks in lattice order from the origin corner -- so by the time the dwelling loop below
        // has taken the Lots it wants, every Lot on an edge is built on and no gate can be placed.
        //
        // It costs the offset in Dwelling: gates occupy Building slots 0..gates-1, so the nth
        // DWELLING is slot gates + n. On a Ruleset declaring no gate kind this returns 0, no Lot is
        // taken, the loop below walks exactly the Lots it always did and no State Hash moves.
        int gates = RaiseGates(world, now, key);

        if (lots - gates < buildings)
        {
            buildings = lots - gates;
        }

        int raised = 0;

        for (int slot = 0; slot < world.Lots.Rows.SlotCount && raised < buildings; slot++)
        {
            // Vacancy rather than `slot < buildings`, because a gate may have taken this one. With
            // no gates the two are the same walk: the Lot table started empty, so the nth Lot is
            // slot n and none of them is built on.
            if (!world.Lots.Rows.IsLive(slot) || !world.Lots.IsVacant(slot))
            {
                continue;
            }

            // Through World's door rather than the table's, so the Building arrives with its kind's
            // Bins and its chain heads armed. Before this the populator built bare Buildings and the
            // Ruleset described a shape nothing constructed.
            world.CreateBuilding(world.Lots.Rows.At(slot), DwellingKind, now, key);
            raised++;
        }

        HouseholdRuleset rules = world.Rules.Households;

        for (int i = 0; i < households; i++)
        {
            Handle<Household> household =
                world.CreateHousehold(Dwelling(world, i % buildings, gates), lifeStage: (byte)(i % 5));

            // THE ONLY PRODUCTION ISSUANCE OF MONEY IN THE BUILD, and it is here rather than
            // anywhere a player can reach. adr/0024 makes the Outside Connection money's only source
            // and that is milestone 11; adr/0116 deferred the treasury's founding balance for want of
            // a denominator. So a generated city would hold no money at all, and every mechanism
            // milestone 10 builds -- the tax, the transfer, adr/0115's floor-to-zero instrument,
            // the conservation equality -- would be exercised over zero and pass vacuously.
            //
            // What makes this legitimate rather than a founding balance smuggled in early is WHOSE
            // act it is. This populator is reached only by CommandKind.Populate, a verb no player
            // has (adr/0090's generator/player line), and it already invents Households, Buildings
            // and Lots from nothing. Issuing their pocket money is the same act, and it goes through
            // World.Endow, which records it in MoneySupply.Issued -- so conservation stays exactly
            // checkable ACROSS the issuance rather than in spite of it.
            //
            // A BAND rather than a figure, and the band is what the instrument needs: a percentage
            // that floors to zero is a distributional artefact, so a city where everybody holds the
            // same amount reads 0% or 100% and measures nothing.
            if (rules.Endows)
            {
                world.Endow(
                    household,
                    rules.OpeningBalance(key, world.Households.Rows.IdAt(world.Households.Rows.Resolve(household))));
            }
        }

        for (int i = 0; i < population; i++)
        {
            // NOBODY IS GIVEN A WORKPLACE HERE, and the deletion is 5b-bis task 2's finding rather
            // than task 4's plan. This loop used to assign one on a stride coprime with the Building
            // count -- `(i * 7) % buildings` -- so that the commute matrix would not be the
            // identity, on the ground that nothing read the column before Phase 2 and that leaving
            // it null would make the first thing that did measure a city where nobody works.
            //
            // THE CEILING IS WHAT DELETED IT. `[[building]] jobs` makes employment a quantity the
            // Ruleset grants, and no shipped Ruleset grants any: `dwelling` declares no jobs, so
            // every one of those strided workplaces was a job that did not exist. It was invisible
            // while nothing could count jobs, and the first Ruleset adoption after the ceiling
            // arrived dismissed all 1,000 of them at once -- which is the mechanism reporting the
            // fixture correctly, at the first moment it was able to.
            //
            // So the argument for the stride survives and the stride does not: task 4 acquires a
            // Workplace by a mechanism, and two ways to get a job is plans/0012 Cause 1 with both
            // copies executing. Until then a Citizen's Workplace is the unset handle, which is
            // exactly what CONTEXT.md -> Unemployment describes and is an honest state rather than a
            // hole.
            world.CreateCitizen(
                world.Households.Rows.At(i % households), new Ticks((ulong)i % 8192));
        }
    }

    /// <summary>
    /// Lays the road lattice and carves the Lots — <see cref="PopulateInto"/>'s land half.
    /// </summary>
    private static void LayLand(World world, WorldKey key)
    {
        // The roads, laid over the area this city will occupy rather than over the map. Laid through
        // Phase 0 for S0a's reason: a verb applied through the Input Log means replay reproduces the
        // network by construction rather than by a second generator agreeing with the first. It
        // no-ops on a Ruleset that declares no [roads].
        //
        // The extent is derived from what is about to be built, which is why the Building count is a
        // shared expression rather than a local: it used to be the whole map unconditionally.
        RoadGenerator.LayInto(world.Roads, key, PavedTiles(world));

        Subdivide(world, WantedBuildings(world));
    }

    /// <summary>
    /// Refuses a world that already holds people.
    /// </summary>
    /// <remarks>
    /// Refused rather than added to. Applying the verb twice would produce a city of twice the
    /// configured size whose tables had grown past the capacity every footprint figure was derived
    /// from — a run that answers the sizing question with the wrong number and reports success.
    /// </remarks>
    private static void RefuseIfPopulated(World world)
    {
        if (world.Citizens.Rows.LiveCount != 0)
        {
            throw new InvalidOperationException(
                "the world already has a population, and a synthetic city is not something to add a "
                + "second of. Populate is world creation, so it belongs at Tick 0 and once.");
        }
    }

    /// <summary>
    /// Households the configured population comes in, at S4 task 2's ratio.
    /// </summary>
    private static int Households(World world) =>
        IntegerMath.FloorDiv(world.Citizens.Rows.Capacity * 360, 1_000);

    /// <summary>
    /// Buildings those Households want, before the standing land clamps it.
    /// </summary>
    /// <remarks>
    /// adr/0068: the occupancy figure is the Ruleset's, and this is the site that used to hold the
    /// other half of a disagreement nothing could reconcile. A declared zero is treated as undeclared
    /// rather than refused, because it would mean a populator that houses nobody and reports success
    /// — and the honest floor is one per Building, not an empty city.
    /// </remarks>
    private static int WantedBuildings(World world)
    {
        int occupancy =
            world.TryDeclaredOccupancy(DwellingKind, out int declared) && declared > 0
                ? declared
                : UndeclaredOccupancy;

        return IntegerMath.FloorDiv(Households(world), occupancy) + 1;
    }

    /// <summary>
    /// How far the Street lattice reaches from the origin corner, in Tiles: enough ground to carry
    /// the Lots this world was allocated for.
    /// </summary>
    /// <remarks>
    /// <b>Derived, with no free number in it — and the number it is derived from is the one the
    /// world already allocated.</b> <see cref="LotSubdivider"/> carves a block against its four faces
    /// and claims one <em>side</em> of each, and <c>[lots] lots_per_segment</c> splits between the two
    /// sides of a Segment, so a <c>B×B</c> block lattice carries <c>2 × lots_per_segment × B²</c>
    /// Lots. This solves that for the smallest <c>B</c> holding <see cref="LotTable"/>'s capacity,
    /// which <see cref="World"/> sets at <b>225 Lots per 1,000 Citizens</b>.
    /// <para>
    /// <b>Sizing this to the Building count instead is the first thing that was tried, and
    /// <c>PlacementLongRunTests</c> refuted it.</b> A lattice holding exactly the Buildings a city
    /// raises leaves <b>three</b> spare Lots at 1,000 Citizens where the whole map leaves nine — and
    /// a Ruleset that demolishes continuously rebuilds onto <em>vacant</em> Lots, so the pass falls
    /// behind and the Unplaced Pool climbs 9% over the tail of a 100,000-Tick run.
    /// <b>A city paves the ground it develops into, not the ground it stands on</b>, and
    /// <c>adr/0021</c>'s <i>developed area</i> reads as the second when the mechanism needs the first.
    /// Taking the figure from <see cref="LotTable"/>'s capacity rather than inventing a headroom
    /// factor keeps a hash-bearing number out of this file: the allocator had already answered
    /// <i>how much land does a city of N want</i>, and the generator was disagreeing with it in
    /// silence.
    /// </para>
    /// <para>
    /// <b>Why this exists at all.</b> The generator paved the whole map on every call until
    /// 2026-08-13 — <c>(WorldTiles ÷ block_tiles + 1)²</c> nodes regardless of the population — which
    /// is <c>adr/0021</c>'s <i>scale with developed area, not map area</i> being false in the one
    /// place nothing measured. It was invisible at 128 Cells because a 1M city wants <b>150</b> blocks
    /// against the 128 the map has, so the requirement exceeded the map and the clamp below was the
    /// whole behaviour. <b>Nothing at target scale moves; what moves is every city smaller than the
    /// map</b>, which until now is every city anybody has run.
    /// </para>
    /// <para>
    /// <b>This assumes no Arterial destroys a Street it crosses</b>, which holds because the shipped
    /// Rulesets declare none. <c>rulesets/severance.toml</c> does declare them, and its own tests lay
    /// the whole map deliberately rather than coming through here — an Arterial grants no frontage
    /// (<c>adr/0014</c>), so in a lattice sized to its Lots an Arterial can only take Lots away.
    /// </para>
    /// </remarks>
    internal static int PavedTiles(World world)
    {
        int block = world.Rules.Roads.BlockTiles;
        int perSegment = world.Rules.Lots.LotsPerSegment;
        int wanted = world.Lots.Rows.Capacity;

        if (block <= 0 || perSegment <= 0 || wanted <= 0)
        {
            return CellGrid.WorldTiles;
        }

        // SqrtFloor and then step up, rather than a ceiling division that would need a float. One
        // step is enough because the floor is out by at most one.
        long perBlock = 2L * perSegment;
        int blocks = (int)IntegerMath.SqrtFloor(IntegerMath.FloorDiv(wanted, perBlock));

        while (perBlock * blocks * blocks < wanted)
        {
            blocks++;
        }

        int tiles = blocks * block;

        return tiles < CellGrid.WorldTiles ? tiles : CellGrid.WorldTiles;
    }

    /// <summary>
    /// Carves enough zoned land to hold <paramref name="wanted"/> Buildings, block by block.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Through the real subdivider, so the fixture and the game agree about what a Lot is.</b>
    /// Until 5a-bis this laid a 64-Tile strip of painted Lots, and that strip was the second half of
    /// the disagreement <c>plans/0000</c> records — <i>the synthetic city fixture and <c>World</c>'s
    /// table sizing disagree and nothing checks that they do</i>. It still does not check, but the
    /// Lots are now the ones the subdivider would have produced, which is the half that was inventable.
    /// </para>
    /// <para>
    /// <b>Blocks in lattice order, and it stops as soon as it has enough.</b> Adjacent blocks claim
    /// <em>opposite</em> sides of the Segment they share, so walking in order costs nothing — a block
    /// never finds its faces already taken by its neighbour.
    /// </para>
    /// </remarks>
    private static int Subdivide(World world, int wanted)
    {
        int blocks = world.Roads.Streets.Blocks;

        // ⚠ The fixture's one dishonest path, and it is named rather than hidden. With no [roads] or
        // no [lots] there is no Street lattice, so there is no honest way to make a Lot at all --
        // frontage is the geometric precondition for a Lot existing (CONTEXT.md → Frontage). But
        // S0a's footprint capture is `--citizens` with no `--ruleset`, and a populator that made no
        // rows would answer the sizing question with an empty world and report success. So it lays
        // rows with no frontage and no Address, which is what they are: storage for measuring a
        // table's footprint, and not a city. Nothing that reads frontage will find any here.
        if (blocks <= 0 || !world.Rules.Lots.Runs)
        {
            for (int i = 0; i < wanted; i++)
            {
                world.Lots.Create(
                    new Tiles(i % LotsPerRow), new Tiles(IntegerMath.FloorDiv(i, LotsPerRow)), zone: 1);
            }

            return wanted;
        }

        int made = 0;

        for (int block = 0; block < blocks * blocks && made < wanted; block++)
        {
            made += LotSubdivider.SubdivideBlock(
                world, block % blocks, IntegerMath.FloorDiv(block, blocks), zone: 1);
        }

        return made;
    }

    /// <summary>
    /// Raises one Outside Connection on every map edge the lattice actually reaches
    /// (<c>adr/0088</c>, <c>adr/0131</c>, milestone 11 task 3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It exists because milestone 9 shipped a producer nothing could observe</b>
    /// (<c>plans/0034</c> <b>F17</b>): the land value field was correct and read zero in every world,
    /// for want of Ruleset <em>content</em> rather than for want of code. A gate kind that no world
    /// stands a Building of is that failure again, so placing one is a task rather than an assumption.
    /// </para>
    /// <para>
    /// 🔴 ⚠ <b>Two of the four edges are unreachable in every world this build can generate, and it
    /// is <see cref="PavedTiles"/> that makes them so.</b> The lattice is sized to the Lots the world
    /// was allocated for, not to the map — so it runs from the origin corner and stops. Its Lots
    /// therefore touch <see cref="MapEdge.West"/> (<c>east = 0</c>) and <see cref="MapEdge.South"/>
    /// (<c>north = 0</c>) and never the far two: reaching <see cref="CellGrid.WorldTiles"/> takes
    /// roughly 2.6 million Lots. Measured on <c>minimal.toml</c> at 1,000 Citizens — 160 paved Tiles
    /// of 16,384, 124 Lots, <b>6 on the west edge and 15 on the south</b>, none on a corner.
    /// ***An edge a generator cannot reach is a market nothing can arrive from***, and a Hinterland
    /// authored behind one is a number no run can refute (<c>adr/0052</c>, <c>adr/0125</c>). The
    /// generator's real count and siting is <c>plans/0002</c> §D2's gap, owned by milestone <b>24</b>.
    /// </para>
    /// <para>
    /// <b>One per reachable edge, which is derived rather than chosen.</b> A count would be a
    /// hash-bearing world-creation number needing a ratifier, and <c>adr/0131</c> put that number at
    /// 24 with the generator. *How many edges does the land touch* is a property of the land, so it
    /// needs no key — the same move <see cref="PavedTiles"/> makes for the extent and
    /// <c>adr/0059</c>'s sample makes for a Zone Rule. ⚠ <b>It also gives the milestone two markets
    /// rather than one</b>, which is the arrangement <c>CONTEXT.md</c> → Hinterland says makes the
    /// Outside legible at all: *four comparable markets are each other's referent*.
    /// </para>
    /// <para>
    /// <b>A corner is skipped rather than resolved</b>, because <see cref="World.EdgeOf"/> reports
    /// one there and <see cref="Invariant.OutsideConnectionStandsOnOneEdge"/> refuses it — a gate
    /// touching two edges sits in two Hinterlands with nothing to say which its emigrants came from.
    /// On the shipped lattice the corner Lot is at the origin and there is exactly one.
    /// </para>
    /// </remarks>
    /// <returns>How many gates were raised, and therefore how many Building slots precede the dwellings.</returns>
    private static int RaiseGates(World world, Ticks now, WorldKey key)
    {
        if (!TryGateKind(world, out byte kind))
        {
            return 0;
        }

        int raised = 0;

        // Ascending MapEdge order, which is west, east, south, north. It is an order rather than a
        // preference: nothing here scores one edge above another, and a stable one is what keeps the
        // State Hash reproducible.
        for (MapEdge edge = MapEdge.West; edge <= MapEdge.North; edge++)
        {
            if (!TryEdgeLot(world, edge, out int lotSlot))
            {
                continue;
            }

            world.CreateBuilding(world.Lots.Rows.At(lotSlot), kind, now, key);
            raised++;
        }

        return raised;
    }

    /// <summary>
    /// The first Building kind this Ruleset declares as an Outside Connection, if any.
    /// </summary>
    /// <remarks>
    /// <b>Declaration order, on <see cref="DwellingKind"/>'s own convention.</b> That constant is a
    /// hardcoded <c>1</c> because this class has always assumed the first kind is the housing; a gate
    /// cannot be assumed into a fixed slot the same way, because most Rulesets declare none — so it
    /// is looked up, and the lookup is the thing that keeps every gateless Ruleset behaving exactly
    /// as it did. ⚠ <b>A second gate kind is ignored rather than refused</b>: choosing between two
    /// would be siting policy, which is milestone 24's.
    /// </remarks>
    private static bool TryGateKind(World world, out byte kind)
    {
        for (int declared = 1; declared <= world.Rules.KindCount; declared++)
        {
            if (world.IsOutsideConnection((byte)declared))
            {
                kind = (byte)declared;
                return true;
            }
        }

        kind = 0;
        return false;
    }

    /// <summary>
    /// The lowest-numbered vacant Lot standing on <paramref name="edge"/>.
    /// </summary>
    /// <remarks>
    /// <b>Lowest slot rather than a draw</b>, because a gate is placed deliberately and nothing about
    /// where it goes is a sampled outcome. It runs before the dwelling loop, so every Lot is vacant
    /// and the vacancy test only matters for the second gate — which cannot collide with the first
    /// anyway, since they are on different edges.
    /// </remarks>
    private static bool TryEdgeLot(World world, MapEdge edge, out int lotSlot)
    {
        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (world.Lots.Rows.IsLive(slot)
                && world.Lots.IsVacant(slot)
                && world.EdgeOf(slot) == edge)
            {
                lotSlot = slot;
                return true;
            }
        }

        lotSlot = 0;
        return false;
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
    private static Handle<Building> Dwelling(World world, int index, int gates) =>
        world.Buildings.Rows.At(gates + index);
}

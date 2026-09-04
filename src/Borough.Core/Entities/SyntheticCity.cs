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

    /// <summary>Land where a dwelling may be raised. <see cref="LotTable.Housing"/>.</summary>
    private const ushort Housing = LotTable.Housing;

    /// <summary>
    /// Land where a trade's premises may be raised, and <b>where a dwelling may not</b>
    /// (<c>adr/0165</c>).
    /// </summary>
    /// <remarks>
    /// <b>Exclusive, which is <c>CONTEXT.md</c> → Zone's own definition of a permission set</b> —
    /// *"it lists the uses allowed there and forbids every other"* — and this is the first time this
    /// class obeys it. It painted <see cref="Housing"/> on every Lot it carved, so the design's rule
    /// was *nothing is permitted by default* and the generator's behaviour was *everything is
    /// permitted, for houses.*
    /// </remarks>
    private const ushort Trade = LotTable.Trade;

    /// <summary>
    /// One block in this many carries <see cref="Trade"/> rather than <see cref="Housing"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>A fixture constant, and it ratifies nothing</b> (<c>adr/0165</c>, <c>adr/0164</c>): no
    /// designer touches this class, so there is no <c>[lots]</c> key, no <c>plans/0002</c> §D row and
    /// nothing to tune. ***It moves the State Hash of every generated world and is still not a design
    /// object***, which is the distinction <c>adr/0100</c> draws — a hash move is an attribution
    /// question rather than a scheduling one.
    /// </para>
    /// <para>
    /// ⚠ <b>A generated city will look wrong and the Ruleset headers must say so.</b> Commercial
    /// blocks appear at a fixed stride rather than along a corridor, which is not how any city is
    /// zoned. ***A demonstration file's job is to exercise a mechanism, not to resemble a city.***
    /// </para>
    /// <para>
    /// <b>Why not keyed on distance from the centre</b>, which would look immediately right — shops
    /// in the middle, houses out. <c>adr/0165</c> refuses it by name: it would encode a location
    /// theory into world creation at the same moment
    /// <c>adr/0163</c> puts location in *demand's* hands, and would make the demand mechanism
    /// unfalsifiable in the only world that exercises it.
    /// </para>
    /// <para>
    /// 🔴 ⚠ <b>EIGHT, AND WHAT PICKED IT WAS A FULL-SUITE SWEEP RATHER THAN AN ARGUMENT ABOUT
    /// CITIES.</b> Nothing here claims one block in eight is a plausible commercial share. It is the
    /// value at which the suite is least broken, measured across a 2×2 of this constant against
    /// <see cref="PavedTiles"/>' compensation: <b>stride 8 with the compensation fails 6</b>, stride
    /// 8 without it fails 11, stride 4 without it fails 8, stride 4 with it fails 9.
    /// </para>
    /// <para>
    /// ⚠ <b>THE STRIDE SATURATES, so most values of it are the same world.</b> The diagonal spans
    /// <c>0..2(n−1)</c> on a grid of <em>n</em> blocks a side, and the golden fixture is a handful of
    /// blocks — so strides <b>8, 12, 16 and 32 all produce 124 housing Lots and 10 trade Lots, byte
    /// for byte</b>, and a run on each returns identical numbers. ***Above 8 this is a constant
    /// wearing a parameter's name.*** Only 4 and below change anything.
    /// </para>
    /// <para>
    /// ⚠ <b>A SMALLER stride yields MORE housing land, which is the opposite of what carving blocks
    /// out sounds like it does</b> — the lattice is sized from the housing Lots it must hold, so a
    /// denser commercial grid forces a wider city: 166 Lots = 126 housing + 40 trade at stride 4,
    /// against 134 = 124 + 10 here. <b>Stride 4 was adopted for four hours on that strength and
    /// withdrawn</b>: it passes the placement long-run test untouched, and it introduces an
    /// <c>adr/0006</c> trend in <c>EvidenceLongRunTests</c> — people carrying a reach-failure history
    /// rising 32.9 across the tail against a 3-sigma band of 23.2. ***A bound traded for an invariant
    /// is not a trade this project makes.***
    /// </para>
    /// <para>
    /// ⚠ <b>What this number is NOT evidence about.</b> An <c>adr/0055</c> reading — that commercial
    /// land dilutes the Zone Rule's sample and slows construction — was proposed and is
    /// <b>refuted</b> by the same sweep, since stride 4 carries four times the trade land and
    /// produces <em>lower</em> vacancy. <b>The cost is land, not wasted looks</b>, and vacancy tracks
    /// housing Lot count monotonically: 134 Lots → 18.5%, 126 → 21.9%, 124 → 25.0%.
    /// </para>
    /// <para>
    /// 🔴 <b>UNRATIFIED, hash-bearing world-creation data, and in the generator rather than in any
    /// Ruleset</b> — so it is in every world the populator builds. <c>plans/0002</c> §D1 holds the
    /// row and names what would settle it. ⚠ <b>A <c>const</c> where <c>adr/0015</c> says Ruleset
    /// data belongs</b>, on <c>TICKS_PER_DAY</c>'s precedent and filed in <c>plans/0012</c> for the
    /// same reason. <b>Reopens the moment the golden fixture grows</b>: every number above is a
    /// property of a 134-Lot world.
    /// </para>
    /// </remarks>
    private const int TradeBlockStride = 8;

    /// <summary>The world every Ruleset described before <c>[[lattice]]</c> existed.</summary>
    private static readonly LatticeDefinition[] OneAtTheOrigin = [new LatticeDefinition(0, 0)];

    /// <summary>
    /// Makes the ground and nothing that stands on it — terrain, Woodland, water and the Hazard
    /// Regions. <b><c>adr/0090</c>'s generator remit, exactly.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THIS WAS WELDED INSIDE <see cref="PopulateInto"/> AND THE WELD HID THE ONE LINE THE
    /// DESIGN DRAWS.</b> <c>adr/0090</c> gives the generator <em>"terrain, Woodland, hazard regions,
    /// and the Outside Connections with their stubs. Nothing else"</em>, and gives the player every
    /// road. The four passes below are that list; <see cref="LayLand"/> and <see cref="PeopleInto"/>
    /// are the part the same ADR refuses. One method did both, so <b>there was no way to ask for the
    /// world the design describes</b> — the only worlds reachable were a full synthetic city or a
    /// bare map with no ground on it at all.
    /// </para>
    /// <para>
    /// <b>It was found by building the flag rather than by reading the ADR.</b>
    /// <c>Borough.Godot</c>'s <c>--empty</c> landed first, skipping the whole Command, and a driven
    /// run on <c>rulesets/flooded.toml</c> at 500 Citizens reported hazard <b>0</b>, water <b>0</b>
    /// and tree <b>0</b> where the populated world reported <b>11,063</b>, <b>5,058</b> and
    /// <b>3,512</b>. ***A world with no ground is not the world an ADR describes, and a reading
    /// taken on it is a reading of nowhere.***
    /// </para>
    /// <para>
    /// ⚠ <b>The Outside Connections are NOT here and that is not an omission of this method's.</b>
    /// A gate is a <c>[[building]]</c> kind carrying <c>arrivals_per_day</c>, so it is raised on a
    /// Lot by <see cref="PeopleInto"/> and there is no Lot until something carves one. <b>Whether a
    /// gate can stand on a world the player has not built yet is open</b>, and it is the question
    /// <c>adr/0090</c>'s stub — unset in <c>plans/0002</c> §D2 — is waiting on.
    /// </para>
    /// </remarks>
    /// <param name="world">The world to lay ground in. Must have no ground in it.</param>
    /// <param name="key">The world key, which every generator below draws against.</param>
    /// <exception cref="InvalidOperationException">The world already has ground.</exception>
    public static void GroundInto(World world, WorldKey key)
    {
        ArgumentNullException.ThrowIfNull(world);

        RefuseIfGrounded(world);

        // The ground, before anything stands on it. plans/0042 decision 3, adr/0158. Nothing in
        // LayLand consults it -- roads do not avoid water (adr/0021) and buildable grade does not
        // ship (adr/0157) -- so this is first because it is the ground rather than because anything
        // below reads it. ⚠ It computes a height field and keeps none of it (adr/0157).
        world.Layers.LayTerrain(key);

        // The forest, on the ground and before the roads. adr/0159, milestone 24 task 8a. ⚠ THE
        // ORDER AGAINST LayLand IS LOAD-BEARING and the order against LayTerrain is not: every Cell
        // is unsealed at this instant, so the Sealing ceiling is trivially satisfied and this pass
        // never consults it -- but run it after LayLand and it plants forest on top of the roads.
        // MapLayers.Seal is what takes the forest back as the city arrives.
        //
        // ⚠ AND THE ORDER SURVIVES THE SPLIT ONLY BECAUSE THE SPLIT IS AT LayLand. Every pass in
        // this method runs before every pass that puts something on the ground, whichever Command
        // asked for it -- which is why Ground and Populate are alternatives rather than a sequence.
        world.Layers.LayWoodland(key);

        // The water, between the ground and the roads. adr/0034, adr/0160, milestone 24 task 6a. ⚠
        // THE ORDER AGAINST LayLand IS NOT LOAD-BEARING TODAY AND WILL BE: roads do not avoid water
        // (adr/0021), so a lattice laid after this pass runs straight across a lake and nothing
        // refuses it. That is the build being honest about what it has not decided rather than a
        // defect -- a bridge is "a buildability exception plus a rendering variant, not a system"
        // (adr/0021), and neither exists. It goes here so that the day something does read the
        // water, the water is already there.
        WaterGenerator.LayInto(
            world.Water,
            world.WaterCells,
            world.WaterInCells,
            world.Catchment,
            world.Flood,
            world.Rules.Water,
            key);

        // The Hazard Region's index, from the rows that pass just wrote. Rebuilt rather than filled
        // as the rows are made, unlike WaterInCells above -- the floodplain pass is a single walk of
        // the height field with no handles to thread, and threading an index through it to save one
        // O(rows) pass on a Ruleset that mostly declares no water would be structure with nothing
        // behind it. plans/0045 row 12.
        world.FloodInCells.Rebuild(world.Flood);

        // The Bins, once the bodies exist -- FitDistrictPools' rule, and the constructor cannot do it
        // because a world under construction has no Water Bodies. milestone 24 task 6b, adr/0161.
        world.FitWaterBins();
    }

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

        // adr/0090'S GENERATOR REMIT, AND IT IS A CALL NOW RATHER THAN FOUR PASSES INLINE. The
        // ground is the half of this method the design keeps; everything below it is the half the
        // design gives to the player. See GroundInto for what the weld was hiding.
        GroundInto(world, key);

        LayLand(world, key);
        PeopleInto(world, key, now);

        // The Districts, once the ground has Buildings on it -- there is nothing for a watershed to
        // find over an empty field. adr/0134, milestone 12 task 3.
        //
        // HERE AND NOT AT THE COMMAND THAT CALLS THIS. The first version hung it off
        // CommandKind.Populate in Simulation, which is one CALLER of this method rather than the
        // thing that builds the city -- and every fixture that populates a world directly, which is
        // most of the suite, got a city with no Districts in it while the Ruleset said it should have
        // two. FactorioTests found it as eleven saved columns no corruption could reach.
        //
        // It does NOT run on a load, and must not: RebuildDerived restores the Districts the world
        // HAD, and milestone 12 task 4 is what makes those two answers differ.
        world.EvaluateDistricts();
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

        // 🔴 NO BUILDING COUNT IS COMPUTED HERE ANY MORE (plans/0053). It was WantedBuildings, the
        // population over the one ceiling every Building shared. Occupancy divides the ground now, so
        // there is no shared ceiling to divide by -- and the count could not be recovered here even
        // in principle, because the room a Building adds depends on which Lot it lands on and only
        // RaiseDwellings knows that. It is passed the POPULATION and raises until the ground it has
        // taken holds them. WantedBuildings survives one caller: PopulateLand, sizing the land before
        // any of it exists.

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

        // ONE LATTICE AT A TIME, and the box is what makes the second one a CENTRE rather than an
        // overflow. The Lots are carved lattice by lattice and a block overshoots the share it was
        // asked for, so a single slot-ordered walk would fill the first lattice's spare Lots before
        // reaching the second's -- a city with one concentration and a hamlet, which is the world
        // this one exists not to be. At one lattice the box is the whole map and this is the walk it
        // always was.
        LatticeDefinition[] lattices = Lattices(world);
        int extentTiles = lattices.Length == 1 ? CellGrid.WorldTiles : PavedTiles(world);
        int raised = 0;

        for (int lattice = 0; lattice < lattices.Length; lattice++)
        {
            raised += RaiseDwellings(
                world,
                now,
                key,
                Share(households, lattices.Length, lattice),
                lattices[lattice],
                extentTiles);
        }

        // A lattice can come up short of its share where its own blocks carved fewer Lots than it was
        // asked for, and the Household loop below indexes Building slots directly -- so the count it
        // divides by is what was RAISED and not what was wanted. The refusal is the `lots <= 0` one
        // arriving a step later: a city with no Buildings houses nobody, and saying so beats a
        // DivideByZeroException from `i % buildings`.
        if (raised <= 0)
        {
            throw new InvalidOperationException(
                "no Building was raised, so there is nowhere to put anybody. Every Lot this world "
                + "holds is outside every [[lattice]] box or already built on -- check that the "
                + "origins are the ones the Streets were laid from.");
        }

        int buildings = raised;

        HouseholdRuleset rules = world.Rules.Households;

        // 🔴 A CURSOR AND NO LONGER A ROUND ROBIN (plans/0053). `i % buildings` was sound for exactly
        // as long as every Building held the same number: WantedBuildings sized the loop against that
        // one ceiling, so dealing Households out in turn could not overfill anybody. Occupancy divides
        // the GROUND now -- a slab's parcel holds several times a detached plot's -- so a flat deal
        // overfills the small Buildings and leaves the large ones half empty, which is not a smaller
        // version of the old behaviour but a different city.
        //
        // ⚠ IT FILLS RATHER THAN SPREADS, and that is the honest reading of what this fixture is.
        // A populator is a world-creation pass and not a housing market: PlacementEngine is what
        // chooses WHERE somebody lives, against a Pool and a look (adr/0069), and a fixture that
        // spread its population artfully would be simulating that pass badly rather than standing
        // out of its way. What this owes is that nobody is over a ceiling and nobody is left over.
        int cursor = 0;

        for (int i = 0; i < households; i++)
        {
            // 🔴 THIS READ `(byte)(i % 5)` UNTIL plans/0046 STAGE 1, AND IT WAS WRITING A STAGE INTO
            // WORLDS THAT HAVE NONE. adr/0011 names five stages, so a 0-based cycle over five looked
            // right for as long as `life_stage` was a byte nothing read -- and it stopped being right
            // the moment stages became RULESET DATA with ids running from 1, because 0 now means
            // *this world has no demographics* rather than *stage zero*. The old spelling therefore
            // did two wrong things at once: it left a fifth of every city stageless in a world that
            // HAS stages, and it stamped a stage id on every Household in the thirteen shipped files
            // that declare none. ***A column nothing reads cannot be wrong, which is exactly how it
            // stays wrong until something reads it.***
            //
            // It moves the State Hash on every world (adr/0100 -- and nobody is carrying a save).
            byte stage = world.Rules.DeclaresLifeStages
                ? (byte)(1 + (i % world.Rules.LifeStageCount))
                : (byte)0;

            // Walk to the next Building with room. It cannot run off the end while WantedBuildings
            // and the Building loop agree about how much room the city has -- and where they do not,
            // the last Building takes the remainder and the whole-world occupancy invariant reports
            // it, which is louder and more diagnosable than a silent wrap.
            while (cursor < buildings - 1
                && world.Occupants.Length(world.Buildings.Rows.Resolve(Dwelling(world, cursor, gates)))
                    >= Room(world, Dwelling(world, cursor, gates)))
            {
                cursor++;
            }

            Handle<Household> household =
                world.CreateHousehold(Dwelling(world, cursor, gates), lifeStage: stage);

            // 🔴 THE GROUND CAN RUN OUT, AND THE POOL IS WHERE THE SURPLUS GOES (plans/0053). It
            // could not before: the Building count was the population divided by one shared ceiling,
            // so the room always fitted by arithmetic. Room is a property of the ground now, and a
            // world whose Lots are all zoned away, boxed out or built on has less of it than its
            // population needs. ***Cramming would be a lie the occupancy invariant then reports as a
            // defect in the city rather than in the fixture***, where the Unplaced Pool is what a
            // shortage of housing IS in this design (adr/0069: the Pool is the demand signal).
            if (world.Occupants.Length(world.Buildings.Rows.Resolve(Dwelling(world, cursor, gates)))
                > Room(world, Dwelling(world, cursor, gates)))
            {
                world.Unplace(household);
            }

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
                world.Households.Rows.At(i % households));
        }
    }

    /// <summary>
    /// Whether this block's land is permitted to a trade rather than to dwellings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every <see cref="TradeBlockStride"/>th block AROUND EACH RING, counted from the middle.</b>
    /// <see cref="Shell"/> lays the lattice out as concentric Chebyshev shells and numbers each one
    /// from its south-west corner; this is that numbering read backwards, so a shell of <c>8r</c>
    /// blocks yields exactly <c>r</c> of them to trade and the share is the stride's by construction
    /// at every radius. ⚠ <b>The middle block is on the trade side</b>, which is step 0 of ring 0 —
    /// a consequence of the arithmetic that this time says something, since
    /// <see cref="Subdivide"/> builds outward from that block and <see cref="BandAt"/> gives it the
    /// densest band.
    /// </para>
    /// <para>
    /// 🔴 <b>IT WAS AN ANTI-DIAGONAL FROM THE MAP'S ORIGIN, AND BOTH HALVES OF THAT WERE WRONG ONCE
    /// THE CITY GREW FROM ITS MIDDLE.</b> The origin keying put a shop in the dense core only by
    /// parity: measured on <c>banded.toml</c> at 4,000 Citizens the densest band is <b>6 blocks of
    /// 33</b>, and the diagonal missed every one — <b>24 trade Lots, all suburban.</b> Phase-locking
    /// the same diagonal to the middle fixed that and broke the share instead, because ***a straight
    /// line crosses a small square ring far more often, per block of its perimeter, than a large
    /// one***: ring 1 gave <b>2 blocks of 8</b> where the stride says one in eight. Measured on
    /// <c>provisioned.toml</c> at 2,000 Citizens — <b>16 trade Lots before, 72 after</b>, on a city
    /// of 42 blocks where the stride promises about five. ***A land-use split that changes with the
    /// city's size is not a split.*** Counting round the ring is exact at every radius.
    /// </para>
    /// <para>
    /// ⚠ <b>Two blocks a ring apart on the same spoke are NOT both trade</b>, which a radius-keyed
    /// rule would have given and which was tried and rejected: a Manhattan distance from the middle
    /// turns the stride into concentric rings of shops, so a city whose radius is under the stride
    /// gets exactly ONE trade block. On <c>banded.toml</c> at 4,000 Citizens — 36 blocks, radius 3,
    /// stride 8 — that was <b>2 trade Lots in the whole city.</b>
    /// </para>
    /// <para>
    /// <b>Keyed on the block's ABSOLUTE position and not on the loop index</b>, which matters in
    /// exactly one shipped world and would have been invisible everywhere else: <c>twinned.toml</c>
    /// carves two lattices with different origins, and an index-keyed stride would give the same
    /// ground a different use depending on which walk reached it. ***A block's use is a property of
    /// the ground, not of the order it was visited in.*** ⚠ <b>The centre is a property of the
    /// lattice and not of the walk</b>, so that still holds with two lattices and two centres.
    /// </para>
    /// <para>
    /// ⚠ <b>The stride saturates at the city's radius rather than at its diagonal</b>: a shell of
    /// <c>8r</c> blocks yields none at all once the stride passes it, so every value above
    /// <c>8 &#215; radius</c> marks the middle block and nothing else. See
    /// <see cref="TradeBlockStride"/>, which carries the measurement. <b>A stride is only a dial
    /// while it is smaller than the city.</b>
    /// </para>
    /// <para>
    /// <b>It draws no randomness</b>, which is this class's own standing rule: every value it
    /// produces is index arithmetic, so the city is a pure function of its size and needs no
    /// <c>purpose_tag</c> — and therefore cannot correlate itself with a simulation decision sharing
    /// a stream. <c>adr/0165</c> quotes that sentence to refuse a drawn share.
    /// </para>
    /// </remarks>
    private static bool IsTradeBlock(int column, int row, int centreColumn, int centreRow) =>
        Step(column - centreColumn, row - centreRow) % TradeBlockStride == 0;

    /// <summary>
    /// Where a block sits in <see cref="Shell"/>'s numbering of its own ring — <b>that method read
    /// backwards.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The four sides, and each corner belongs to exactly one of them</b>, matching the walk that
    /// produced the numbering: the south side stops one short of the south-east corner, which the
    /// east side takes, and so on round. Getting that wrong would number two blocks the same and
    /// leave a third unreachable — and since the only reader takes a remainder, it would show up as
    /// a land-use split that is subtly out rather than as anything failing.
    /// </para>
    /// <para>
    /// ⚠ <b>It is the inverse of <see cref="Shell"/> and the two must be read together.</b> They are
    /// a pair with no test between them; what checks them is that
    /// <see cref="IsTradeBlock"/>'s share comes out at the stride, which is asserted on a real
    /// lattice rather than on the arithmetic.
    /// </para>
    /// </remarks>
    private static int Step(int east, int north)
    {
        int acrossEast = east < 0 ? -east : east;
        int acrossNorth = north < 0 ? -north : north;
        int ring = acrossEast > acrossNorth ? acrossEast : acrossNorth;

        if (ring == 0)
        {
            return 0;
        }

        int side = 2 * ring;

        if (north == -ring && east < ring)
        {
            return east + ring;
        }

        if (east == ring && north < ring)
        {
            return side + north + ring;
        }

        if (north == ring && east > -ring)
        {
            return (2 * side) + ring - east;
        }

        return (3 * side) + ring - north;
    }

    /// <summary>
    /// Raises up to <paramref name="wanted"/> dwellings on the vacant Lots inside one lattice's box.
    /// </summary>
    /// <remarks>
    /// <b>A box test rather than a slot range, because <see cref="PeopleInto"/> is reached without
    /// <c>LayLand</c> having run.</b> A Connect-laid world's Lots were carved by the <c>zone</c> verb
    /// and this class never saw them, so there are no per-lattice slot boundaries to have recorded.
    /// Geometry is the one thing both paths have.
    /// </remarks>
    private static int RaiseDwellings(
        World world, Ticks now, WorldKey key, int wanted, LatticeDefinition lattice, int extentTiles)
    {
        // One block wider than the lattice, for Subdivide's reason: the block beyond the east edge
        // has that edge's Segments as its west face, so it carries Lots and they are this lattice's.
        //
        // ⚠ A block as a LENGTH -- a margin on a box -- so the WIDEST block and not the mean, which
        // is what keeps the box covering on a lattice whose lines are not evenly spaced.
        int reach = extentTiles + world.Roads.Lattice.Widest;
        int fromEast = lattice.OriginEastTiles;
        int fromNorth = lattice.OriginNorthTiles;
        int toEast = fromEast + reach;
        int toNorth = fromNorth + reach;

        int raised = 0;

        int room = 0;
        int blockTiles = world.Rules.Roads.BlockTiles;
        int crossed = int.MinValue;
        int spare = int.MinValue;

        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            // The block this Lot stands on, packed into one comparable int. Zero everywhere on a
            // world with no lattice, which makes the two tests below inert and builds every row --
            // the degenerate path, where a Lot is one Tile and there are no blocks to finish.
            int here = blockTiles > 0
                ? (world.Roads.Lattice.LineAt(world.Lots.East[slot].Raw) * CellGrid.WorldCells)
                    + world.Roads.Lattice.LineAt(world.Lots.North[slot].Raw)
                : 0;

            // 🔴 THE POPULATION IS HOUSED, THEN ONE MORE STREET GOES UP (plans/0053). Two rules, and
            // the second is where the city's VACANCY comes from.
            //
            // ⚠ The first is that `wanted` is a POPULATION and this loop raises BUILDINGS, which are
            // no longer the same quantity: occupancy divides the ground, so how many Buildings house
            // a Household count is a property of the Lots this walk meets and only the walk that
            // raises them knows it. A Building count marched the shipped world's dwellings out to the
            // map edge on a Ruleset with a door, because it paved for one Household each and spread.
            //
            // ⚠ The second is that a generator does not stop mid-street. It finishes the block that
            // houses the last Household and then lays the next one out too -- which is how a
            // subdivision is actually built, the plat recorded ahead of the houses sold. ***Those
            // spare places are not slack in the fixture: they are where a later arrival moves in and
            // where a founded Business takes premises*** (adr/0069, adr/0147). A city built to
            // exactly its own population accepts nobody, and ArrivalTests and FoundingTests are what
            // say so on the day -- ***vacancy is a property of a city and not an accident of one.***
            if (room >= wanted)
            {
                if (crossed == int.MinValue)
                {
                    crossed = here;
                }
                else if (here != crossed)
                {
                    if (spare == int.MinValue)
                    {
                        spare = here;
                    }
                    else if (here != spare)
                    {
                        break;
                    }
                }
            }

            // Vacancy rather than `slot < buildings`, because a gate may have taken this one -- and
            // now because a lattice laid before this one may have. With no gates and one lattice the
            // two are the same walk: the Lot table started empty, so the nth Lot is slot n and none
            // of them is built on.
            if (!world.Lots.Rows.IsLive(slot) || !world.Lots.IsVacant(slot))
            {
                continue;
            }

            // 🔴 THE HALF adr/0165 DOES NOT MENTION AND WITHOUT WHICH THE SPLIT IS A NO-OP. This
            // method raises dwellings DIRECTLY rather than through a Zone Rule, so it never consulted
            // a permission set at all -- and a generator that paints a trade bit and then builds
            // houses on it obeys the rule with one hand and breaks it with the other.
            //
            // What that would cost is not untidiness, it is a FALSE NEGATIVE: the commercial land
            // would arrive already built on, the trade's Zone Rule would find no vacant permitted Lot,
            // and a run of the Provider Ruleset would produce no shops and look like a demand-signal
            // failure. ***That is plans/0044 P6 -- the split's failure mode is silent, and it is
            // silent in the one world that matters*** -- reached through the populator instead of
            // through the painter.
            if ((world.Lots.Zone[slot] & Housing) == 0)
            {
                continue;
            }

            int east = world.Lots.East[slot].Raw;
            int north = world.Lots.North[slot].Raw;

            if (east < fromEast || east > toEast || north < fromNorth || north > toNorth)
            {
                continue;
            }


            // Through World's door rather than the table's, so the Building arrives with its kind's
            // Bins and its chain heads armed. Before this the populator built bare Buildings and the
            // Ruleset described a shape nothing constructed.
            Handle<Building> raisedHere =
                world.CreateBuilding(world.Lots.Rows.At(slot), DwellingKind, now, key);

            raised++;
            room += Room(world, raisedHere);
        }

        return raised;
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
        // A world with a door paves to the map's boundary, because that is where a door has to be.
        LatticeDefinition[] lattices = Lattices(world);
        bool boundary = ReachesTheBoundary(world);

        // The two are incompatible TODAY and the refusal says which half gives way. A world with a
        // door paves the whole map, and a whole-map lattice leaves no ground for a second one to
        // stand on -- so the gap that is a [[lattice]] file's entire content would not exist. It is
        // thrown here rather than refused at load because neither fact is a property of the file
        // alone: whether the lattices collide depends on the population the world was allocated for.
        if (boundary && lattices.Length > 1)
        {
            throw new InvalidOperationException(
                $"this Ruleset declares {lattices.Length} lattices and a Building kind carrying "
                + "arrivals_per_day. A gate stands on a map edge, so a world with a door paves the "
                + "lattice to the boundary -- which leaves nowhere for a second lattice and no gap "
                + "between them, and the gap is what a [[lattice]] file exists to author. A world "
                + "with two centres AND a door needs the extent to stop being all-or-nothing.");
        }

        int extentTiles = boundary ? CellGrid.WorldTiles : PavedTiles(world);

        RoadGenerator.LayInto(world.Roads, key, lattices, extentTiles, world.Layers);

        // 🔴 THE POPULATION AND NOT A BUILDING COUNT (plans/0053), which is the same correction
        // RaiseDwellings took and for the same reason. Occupancy divides the ground now, so a Lot
        // count is no longer a proxy for room -- and the count this used to compute was
        // `households / typical`, where `typical` had to be estimated from a lattice with no Lots on
        // it yet. ***An estimate of a floored quantity is short by whatever every floor threw away***,
        // and on the shipped lattice at a coarse rate it left 23 Households of 360 with nowhere to
        // live. Subdivide counts the room it carves and stops when the room covers the people.
        int wanted = Households(world);

        // Carved lattice by lattice rather than in one map-wide walk, and the two are the same walk
        // wherever there is one lattice -- blocks outside it hold no Segments, so SubdivideBlock
        // makes nothing there and the Lot sequence is identical. What the box buys is the world with
        // two of them: the ground BETWEEN two lattices carries the corridor joining them, and a
        // map-wide walk would carve Lots along it. The saddle in the density field would then have
        // Buildings in it, which is the one thing this world exists not to have.
        for (int lattice = 0; lattice < lattices.Length; lattice++)
        {
            Subdivide(world, lattices[lattice], extentTiles, Share(wanted, lattices.Length, lattice));
        }
    }

    /// <summary>
    /// Where this world's Street lattices stand — the <c>[[lattice]]</c> tables, or the origin corner
    /// where a Ruleset states none.
    /// </summary>
    /// <remarks>
    /// <b>The absence is one lattice at (0, 0)</b>, which is the only world this build could generate
    /// before milestone 12 task 1 — so every Ruleset in <c>rulesets/</c> but the one authoring a gap
    /// takes exactly the path it always took, and no committed State Hash moves.
    /// </remarks>
    private static LatticeDefinition[] Lattices(World world) =>
        world.Rules.Lattices.Length == 0 ? OneAtTheOrigin : world.Rules.Lattices;

    /// <summary>
    /// One lattice's share of a whole-city quantity — <b>an equal split, remainder to the first</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Derived rather than authored, and the derivation is the argument for it.</b> A
    /// <c>share_percent</c> key would be a hash-bearing number per table with nothing to ratify it
    /// (<c>adr/0052</c>), in a file whose whole content is a distance. An equal split is the same kind
    /// of answer <see cref="UndeclaredOccupancy"/> is: the one that needs nothing chosen.
    /// </para>
    /// <para>
    /// ⚠ <b>Equal is what makes the world unambiguous, which is what task 1 was asked for.</b> Two
    /// concentrations of the same height both clear any prominence threshold a sane person would pick,
    /// so the world <em>demonstrates</em> the derivation rather than calibrating it — and the
    /// threshold is not chosen until task 3.
    /// </para>
    /// </remarks>
    private static int Share(int total, int lattices, int index) =>
        IntegerMath.FloorDiv(total, lattices) + (index < total % lattices ? 1 : 0);

    /// <summary>
    /// Refuses a world whose ground is already laid, on the one piece of it a repeat would
    /// duplicate.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Water Bodies are the whole signal, and the first version of this guard also asked about
    /// Woodland and was wrong twice over.</b> <see cref="Space.WoodlandGenerator"/> <em>writes every
    /// row</em> from the <see cref="WorldKey"/> alone and says in its own file that it needs no
    /// already-laid refusal — so a fresh world's Woodland table is fully live before anything has
    /// run, which made the check fire on every boot, and re-running it would have been harmless
    /// anyway. <c>LayTerrain</c> is the same shape and keeps no height field at all
    /// (<c>adr/0157</c>). ***A table being full is not evidence that a pass filled it***, and a
    /// generator that overwrites is not one that duplicates.
    /// </para>
    /// <para>
    /// <b><see cref="Space.WaterGenerator"/> is the exception, and it is the only one:</b> it
    /// <em>creates rows</em>, so a second call lays a second coastline over the same ground. That is
    /// the corruption this refuses, and it is reachable exactly one way — a world that took
    /// <c>CommandKind.Ground</c> and then <see cref="PopulateInto"/>, which lays ground itself.
    /// </para>
    /// <para>
    /// ⚠ <b>So a Ruleset declaring no water takes both verbs without complaint, and the resulting
    /// world is correct rather than merely unrefused.</b> Terrain and Woodland are rewritten
    /// identically and the city is then built on them. ***The guard fires wherever a repeat would do
    /// damage, which is a narrower claim than fires whenever the passes have run*** — and the
    /// narrower one is what this method can honestly make.
    /// </para>
    /// </remarks>
    private static void RefuseIfGrounded(World world)
    {
        if (world.Water.Rows.LiveCount == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            "this world already has Water Bodies, so its ground has been laid once. Ground and "
            + "Populate are alternatives and not a sequence: Populate lays the ground itself, so a "
            + "world that took Ground first must not take Populate after it. adr/0090 gives the "
            + "generator the ground and the player every road; pick which of the two verbs made "
            + "this world at Tick 0.");
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

        // ONE LATTICE'S share and not the world's, and index 0 because it carries the remainder --
        // every lattice is laid to one extent, so the extent has to hold the largest share. At one
        // lattice this is the whole capacity and the expression this always was.
        int wanted = Share(world.Lots.Rows.Capacity, Lattices(world).Length, 0);

        if (block <= 0 || perSegment <= 0 || wanted <= 0)
        {
            return CellGrid.WorldTiles;
        }


        // ⚠ AND THE GROUND THE SPLIT TOOK AWAY, which adr/0165 made a real quantity. One block in
        // TradeBlockStride is permitted to a trade, so a lattice paved for exactly `wanted` Lots
        // leaves fewer than `wanted` a family can live on.
        //
        // 🔴 ⚠ WHAT THIS ACTUALLY DOES IS STEP THE LATTICE UP BY ONE RING, AND NOT WHAT THE
        // ARITHMETIC LOOKS LIKE. The extent below is quantised to whole blocks a side --
        // `blocks = sqrt(wanted / perBlock)`, an integer -- so on a city this small a 14% bump in
        // `wanted` either moves that integer or does nothing at all. MEASURED: it does not move the
        // Lot counts by one Lot (134 = 124 housing + 10 trade, with it and without it). What it
        // moves is the paved EXTENT, and that is load-bearing: without it five TripCommandTests
        // fixtures and CarOwnershipTests fail, because their probes need ground the city has not
        // otherwise paved.
        //
        // ***So this is a compensation for the split whose effect arrives through the extent rather
        // than through the Lot count, and the first version of this comment claimed the second.***
        // It was recorded as a Lot-supply fix, measured, and found to supply no Lots.

        wanted = IntegerMath.FloorDiv(wanted * TradeBlockStride, TradeBlockStride - 1) + 1;

        // SqrtFloor and then step up, rather than a ceiling division that would need a float. One
        // step is enough because the floor is out by at most one.
        long perBlock = 2L * perSegment;
        int blocks = (int)IntegerMath.SqrtFloor(IntegerMath.FloorDiv(wanted, perBlock));

        while (perBlock * blocks * blocks < wanted)
        {
            blocks++;
        }

        // ⚠ HOW FAR `blocks` BLOCKS REACH FROM THE ORIGIN, which is a walk and not a multiply --
        // the extent this returns is ground and the blocks it counts need not be equal.
        int tiles = world.Roads.Lattice.EdgeOf(blocks) - world.Roads.Lattice.EdgeOf(0);

        return tiles < CellGrid.WorldTiles ? tiles : CellGrid.WorldTiles;
    }

    /// <summary>
    /// Whether this world's land must reach the map's boundary, rather than only its population.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A gate stands on a map edge (<c>adr/0088</c>), so a world with gates needs land at its
    /// edges — and that is the whole rule.</b> It states no number: *does this Ruleset declare a
    /// door* is a property of the Ruleset, exactly as <see cref="PavedTiles"/>' extent is a property
    /// of the population. ⚠ <b>Without it two of the four edges are unreachable in every world this
    /// build can generate</b>, because the ordinary extent is sized to the Lots wanted and runs from
    /// the origin corner: at 1,000 Citizens that is <b>640 m of a 65,536 m map</b>, so the lattice
    /// touches <see cref="MapEdge.West"/> and <see cref="MapEdge.South"/> and never the far two.
    /// </para>
    /// <para>
    /// ⚠ <b>It costs a much larger graph and no allocation at all.</b>
    /// <c>RoadGraph.ExpectedNodes</c> already sizes both tables for the whole map — it is
    /// <c>(WorldTiles ÷ block_tiles + 1)²</c> and has never read the extent — so the capacity was
    /// always reserved and only the live rows change. Measured at <c>block_tiles = 32</c>: <b>36
    /// nodes and 61 Segments</b> at the ordinary extent against <b>263,169 and 535,817</b> at the
    /// map's, laid in <b>150 ms</b>.
    /// </para>
    /// <para>
    /// 🔴 ⚠ <b>Paving to the edge is necessary and nowhere near sufficient, which is the finding
    /// worth carrying.</b> A gate at the far edge is <b>64,896 m</b> from a city in the origin
    /// corner. Measured on <c>bordered.toml</c> at 1,000 Citizens, gate to nearest dwelling by car:
    /// west and south <b>0 minutes</b>, east <b>62</b>, north <b>73</b> — against a Commute Budget
    /// ceiling of <b>49</b>. With <c>arterial_count = 0</c> the far two read <b>78</b> and <b>80</b>,
    /// so sixteen Arterials buy 16 minutes on one edge and 7 on the other and neither reaches the
    /// ceiling. <b>A pure-Arterial run of that distance is 43 minutes and no route is pure</b>, which
    /// is why the ceiling is not reachable by tuning the roads. So a world that paves to the edge has
    /// a far gate that <em>stands</em>, that is <em>routable</em>, and that no Trip <em>to the
    /// existing city</em> can complete — <c>TripEngine</c> judges the Budget on every Trip and not
    /// only on a commute. ***What makes a far gate usable is a dwelling beside it***, and the carved
    /// block leaves vacant Lots for exactly that. See <see cref="RaiseGates"/>.
    /// </para>
    /// </remarks>
    private static bool ReachesTheBoundary(World world) => TryGateKind(world, out _);

    /// <summary>
    /// Carves enough zoned land to hold <paramref name="wanted"/> Households, block by block.
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
    /// <para>
    /// 🔴 <b>It counts ROOM and no longer Lots</b> (<c>plans/0053</c>), which is
    /// <see cref="RaiseDwellings"/>'s change arriving one pass earlier and for the same reason. A Lot
    /// count was a proxy for room while every Building held what its kind declared; occupancy divides
    /// the ground now, so <em>Lots differ</em> — and a target of <c>households ÷ typical</c>
    /// under-paved by however much the floor in <c>Holds</c> threw away, Lot by Lot. ***Only the pass
    /// that carves the ground knows how much room it carved.***
    /// </para>
    /// <para>
    /// ⚠ <b>The new rows are the ones above the old slot count</b>, and that is safe here rather than
    /// generally: this runs at world creation on a table nothing has freed a row from, so no slot is
    /// recycled underneath the walk.
    /// </para>
    /// </remarks>
    private static int Subdivide(
        World world, LatticeDefinition lattice, int extentTiles, int wanted)
    {
        int blocks = world.Roads.Streets.Blocks;

        // ⚠ The fixture's one dishonest path, and it is named rather than hidden. With no [roads] or
        // no [lots] there is no Street lattice, so there is no honest way to make a Lot at all --
        // frontage is the geometric precondition for a Lot existing (CONTEXT.md → Frontage). But
        // S0a's footprint capture is `--citizens` with no `--ruleset`, and a populator that made no
        // rows would answer the sizing question with an empty world and report success. So it lays
        // rows with no frontage and no Address, which is what they are: storage for measuring a
        // table's footprint, and not a city. Nothing that reads frontage will find any here.
        // A one-Tile row holds exactly one Household at every rate, because `Holds` floors at one --
        // so on this path a Household target and a Lot target are the same number.
        if (blocks <= 0 || !world.Rules.Lots.Runs)
        {
            for (int i = 0; i < wanted; i++)
            {
                world.Lots.Create(
                    new Tiles(i % LotsPerRow), new Tiles(IntegerMath.FloorDiv(i, LotsPerRow)), Housing);
            }

            world.LotsAdmitting.Invalidate();

            return wanted;
        }

        int firstColumn = world.Roads.Lattice.LineAt(lattice.OriginEastTiles);
        int firstRow = world.Roads.Lattice.LineAt(lattice.OriginNorthTiles);
        // ⚠ ONE BLOCK WIDER THAN THE LATTICE, and it is measured rather than reasoned. A lattice of
        // n blocks has n+1 Node columns, so the block SITTING BEYOND its east edge still has that
        // last column of vertical Segments as its west face -- and a face is all SubdivideBlock
        // needs. The map-wide walk this replaced carved those Lots, and a box of exactly n dropped
        // them: every golden trace moved, and GoldenSessionCoverageTests named it exactly ("carved
        // 118 Lots where 117 were expected"). ***A lattice's Lots do not stop at its extent, because
        // a Segment has two sides.***
        int span = world.Roads.Lattice.LinesIn(lattice.OriginEastTiles, extentTiles);

        int rate = world.Rules.Capacity.FloorTilesPerOccupant;
        bool trades = world.Rules.Declares(DwellingKind)
            && world.Rules.Kind(DwellingKind).Business != 0;

        int made = 0;
        int room = 0;

        // 🔴 OUTWARD FROM THE MIDDLE, AND IT WAS READING ORDER. This walked `b % span` and
        // `b / span` -- west to east, south to north, from the lattice's origin corner -- and it
        // stops the moment it has room, so what it built was a STRIP along the south edge of a
        // square lattice. That is plans/0049 F8 and F41, filed twice from a Lot count and from the
        // Sealing overlay, and it is here rather than in the drawing. ***A city that grows in
        // reading order rather than from its middle has no middle***, and BandAt paints its rings
        // concentrically about one -- so the two passes disagreed about where the city was.
        //
        // 🔴 WHAT IT COST WAS THE TOP OF THE DENSITY LADDER. Measured on `platted.toml`, the file
        // whose whole job is to draw all five block patterns in one city: at 10,000 Citizens the
        // lattice paves 0-544 Tiles square and the built part reaches north 158, never within 114
        // Tiles of the centre row; at 40,000 it paves 0-1,056 and reaches 286. The drawn storey
        // counts were 2, 3, 4 and 5 in both runs -- ***four times the population and the same three
        // rungs*** -- because bands 4 and 5, the courtyard and the slab, were painted on ground
        // nothing ever subdivided. The file's own header claims a slab in the middle.
        //
        // ⚠ THE ORDER IS A CHEBYSHEV RING WALK ABOUT `BandAt`'s OWN CENTRE, which is the point: one
        // centre read by both passes rather than two conventions that happen to agree on a full
        // lattice. Ring 0 is the middle square and ring r is the shell of 8r squares around it, so
        // the densest band is subdivided first and the city thins outward.
        //
        // ⚠ IT IS SAFE TO REORDER AND THIS IS WHY. Adjacent blocks claim OPPOSITE sides of the
        // Segment they share, and which side is a property of a block's own coordinates rather than
        // of who reached it first -- the paragraph above already says so, and it said it about a
        // walk that never tested it.
        int half = IntegerMath.FloorDiv(span, 2);
        int centreColumn = firstColumn + half;
        int centreRow = firstRow + half;
        // The furthest corner of the box from the middle, and not `span`. A ring costs 8r steps, so
        // a bound twice as large as it needs to be does four times the work -- every step of it
        // clipped by the test below, which is the kind of correct that reads as deliberate.
        int reach = half > span - 1 - half ? half : span - 1 - half;

        for (int ring = 0; ring <= reach && room < wanted; ring++)
        {
            int around = ring == 0 ? 1 : 8 * ring;

            for (int step = 0; step < around && room < wanted; step++)
            {
                Shell(centreColumn, centreRow, ring, step, out int column, out int row);

                if (column < firstColumn || row < firstRow
                    || column >= firstColumn + span || row >= firstRow + span
                    || column >= blocks || row >= blocks)
                {
                    continue;
                }

                // plans/0053 step 2. The generator paints the band the way it paints the Zone, which
                // is adr/0025's "the player sets a ceiling" with the generator standing in for the
                // player -- NOT a cap derived from conditions, which that ADR rejects by name. In a
                // Ruleset with no [[band]] this is 0 on every block and nothing anywhere reads it.
                world.BandBlock(column, row, BandAt(world, room, wanted));

                // adr/0165's split, at the loop that already had `column` and `row` in hand -- the
                // ADR's own words are that the change is WHAT THAT VALUE IS and not where it comes
                // from.
                if (IsTradeBlock(column, row, centreColumn, centreRow))
                {
                    // Carved and NOT counted, which is what keeps `wanted` meaning what this
                    // method's summary says it means: enough land to hold `wanted` DWELLINGS.
                    // Counting a commercial block toward a housing target would silently shrink
                    // every generated city by the trade's share -- a population change wearing a
                    // zoning change's clothes.
                    LotSubdivider.SubdivideBlock(world, column, row, Trade);
                    continue;
                }

                int before = world.Lots.Rows.SlotCount;

                made += LotSubdivider.SubdivideBlock(world, column, row, Housing);

                for (int slot = before; slot < world.Lots.Rows.SlotCount; slot++)
                {
                    room += RoomOn(world, slot, rate, trades);
                }
            }
        }

        return made;
    }

    /// <summary>
    /// The <paramref name="step"/>th lattice square on the Chebyshev shell at
    /// <paramref name="ring"/> from a centre — <b>ring 0 being the centre square itself.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Eight squares a ring, so a shell holds <c>8 × ring</c> of them</b>, walked from its
    /// south-west corner: east along the south side, north up the east, west along the north, south
    /// down the west. ⚠ <b>The order within a shell is arbitrary and is recorded as arbitrary</b> —
    /// what is not arbitrary is that a shell is finished before the next one is started, which is
    /// the whole of what <see cref="Subdivide"/> needs from it.
    /// </para>
    /// <para>
    /// ⚠ <b>It bounds nothing.</b> A shell runs past the lattice on every side once the ring exceeds
    /// the half-span, and the caller is what refuses those — because a square outside the lattice
    /// and a square past the far edge of the map are two different refusals and only one of them is
    /// this function's business.
    /// </para>
    /// </remarks>
    private static void Shell(
        int centreColumn, int centreRow, int ring, int step, out int column, out int row)
    {
        if (ring <= 0)
        {
            column = centreColumn;
            row = centreRow;
            return;
        }

        int side = 2 * ring;

        if (step < side)
        {
            column = centreColumn - ring + step;
            row = centreRow - ring;
        }
        else if (step < 2 * side)
        {
            column = centreColumn + ring;
            row = centreRow - ring + (step - side);
        }
        else if (step < 3 * side)
        {
            column = centreColumn + ring - (step - (2 * side));
            row = centreRow + ring;
        }
        else
        {
            column = centreColumn - ring;
            row = centreRow + ring - (step - (3 * side));
        }
    }

    /// <summary>How many Households one carved Lot has room for.</summary>
    /// <remarks>
    /// ⚠ <b>A dwelling that comes with a trade spends one of its places on it</b> (<c>adr/0148</c>):
    /// one ceiling counts both kinds of tenant, so a pass sizing the city by the ceiling would build
    /// too few and queue the difference for ever. ⚠ <b>Taken off a one-place Building too</b>, which
    /// leaves zero — the ground is what it is, and a Building whose single tenancy is the trade
    /// houses nobody. See <see cref="Room"/>, where reading that zero as one overfilled seven
    /// Buildings.
    /// </remarks>
    private static int RoomOn(World world, int slot, int rate, bool trades)
    {
        if (!world.Lots.Rows.IsLive(slot))
        {
            return 0;
        }

        int floor = world.Lots.FloorTiles(slot);

        if (floor <= 0)
        {
            return 0;
        }

        // No rate is not a rate of nothing -- see Room, which draws the same distinction one pass
        // later. A world with no [capacity] holds one Household per Building.
        if (rate <= 0)
        {
            return UndeclaredOccupancy;
        }

        int holds = Rules.CapacityRuleset.Holds(floor, rate);

        if (trades)
        {
            holds--;
        }

        return holds > 0 ? holds : 0;
    }

    /// <summary>
    /// Which density band the generator paints on the block it is about to carve — <b>a share of the
    /// population apiece, densest first.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The generator standing in for the player, which is what <c>adr/0025</c> asks for</b>: the
    /// player sets a ceiling, and a player would set a high one in the middle. ⚠ <b>It is NOT a cap
    /// derived from conditions</b> — that ADR rejects the road-derived cap by name, and reading land
    /// value here instead of road tier would not change the objection. This reads only how much of
    /// the population is already housed, which is the pass's own progress and nothing about the city.
    /// </para>
    /// <para>
    /// 🔴 <b>IT WAS A CHEBYSHEV RING OVER THE LATTICE'S HALF-SPAN, AND THE GRADIENT WAS NEVER
    /// CROSSED AT ANY POPULATION.</b> The two extents in that division are unrelated quantities:
    /// <see cref="PavedTiles"/> sizes the lattice from <c>Lots.Rows.Capacity</c> — a TABLE's capacity
    /// — while <see cref="Subdivide"/> stops at the population's room, so the band a block got was
    /// the ratio between a memory sizing and a city sizing. ***Measured on <c>platted.toml</c>, the
    /// file whose whole job is to draw all five patterns in one city: under the old reading-order
    /// walk the drawn storeys were 2, 3, 4, 5 at 10,000 Citizens and 2, 3, 4, 5 at 40,000; under a
    /// centre-out walk against the same divisor they were 5, 6, 7 at 10,000, at 40,000 AND at
    /// 160,000.*** Sixteen times the population and the same three rungs, from the other end. The
    /// divisor grew exactly as fast as the built radius did, so no city ever crossed a band boundary.
    /// </para>
    /// <para>
    /// ✅ <b>EACH DECLARED BAND HOUSES AN EQUAL SHARE OF THE POPULATION, and the ground each needs
    /// falls out of how dense it is.</b> That is why this introduces no tuning number and nothing
    /// here is ratifiable under <c>adr/0052</c>: the only figure is the Ruleset's own band count.
    /// ⚠ <b>The rings come out UNEQUAL and that is the correction rather than a side effect</b> — a
    /// slab block houses an order of magnitude more people than a detached one, so a fifth of the
    /// population fits in a couple of blocks at the middle and wants dozens at the rim. ***A city
    /// thins outward because the ground each band needs differs, which is the thing the equal rings
    /// were asserting away.***
    /// </para>
    /// <para>
    /// ⚠ <b>The rings are still concentric and are no longer GEOMETRY.</b>
    /// <see cref="Subdivide"/> walks outward one Chebyshev shell at a time and the progress below
    /// only rises, so the band it hands out is non-increasing in the ring — which is what makes the
    /// picture concentric. ⚠ <b>A shell may carry two bands</b>, where the share is crossed part-way
    /// round it, and that is left alone: a boundary that falls on a ring edge is a boundary somebody
    /// drew.
    /// </para>
    /// <para>
    /// ⚠ <b>Zero when the Ruleset declares no band</b>, which is every shipped file but the two that
    /// demonstrate this. Band 0 is <em>no band</em> and admits everything.
    /// </para>
    /// </remarks>
    private static byte BandAt(World world, int room, int wanted)
    {
        int bands = world.Rules.Bands.Length;

        if (bands <= 0)
        {
            return 0;
        }

        // A target of nothing is not a target of one: with no population to divide there is no
        // share to be part-way through, and the middle is where the pass starts.
        if (wanted <= 0)
        {
            return (byte)bands;
        }

        int crossed = IntegerMath.FloorDiv(room * bands, wanted);

        if (crossed >= bands)
        {
            crossed = bands - 1;
        }

        // Counted from 1, and inverted so that the FIRST ground carved -- the middle -- takes the
        // last band declared. Declaration order is intensity order, lowest first (BandDefinition),
        // so the densest band is the one written last and it lands where a player would have
        // painted it.
        return (byte)(bands - crossed);
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
    /// <b>All four edges get one, and it takes two passes rather than one.</b>
    /// <see cref="ReachesTheBoundary"/> pushes the lattice out to <see cref="CellGrid.WorldTiles"/>
    /// so there is a Street on every edge; <see cref="CarveEdgeBlock"/> then subdivides the one block
    /// carrying each edge, because <c>Subdivide</c> walks blocks from the origin and stops as soon as
    /// it has Lots for the population — so ***paving to the boundary puts a Street on the edge and no
    /// Lot beside it***. Before both, the lattice ran 160 Tiles of 16,384 at 1,000 Citizens and
    /// touched <see cref="MapEdge.West"/> and <see cref="MapEdge.South"/> only.
    /// </para>
    /// <para>
    /// 🔴 ⚠ <b>A far gate stands and is routable, and it is still further than the Commute Budget
    /// from the city in the corner</b> — east <b>62</b> minutes by car and north <b>73</b>, against a
    /// ceiling of <b>49</b>. <c>TripEngine</c> judges that Budget on <em>every</em> Trip, so a move-in
    /// from a far gate to a corner dwelling fails with
    /// <c>TripFate.ExceededCommuteBudget</c>. <b>That is the map working as designed rather than a
    /// defect</b>: <c>adr/0089</c> sizes the map by how many Commute Budgets fit across it, so a map
    /// several budgets wide puts its far edge outside one by construction. ***What a far gate needs
    /// is a dwelling beside it, not a faster road***, and the carved block leaves vacant Lots for one.
    /// Whether an arrival is placed in reach of the gate it came through is milestone 11 <b>task
    /// 6</b>'s; the generator's real gate count and siting stays milestone <b>24</b>'s.
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
                // Nothing stands on this edge yet, so carve the land it would stand on. Subdivide
                // walks blocks from the origin and stops as soon as it has enough Lots for the
                // population, so a paved map is still a city in one corner -- ***paving to the
                // boundary puts a Street on the edge and no Lot beside it.***
                CarveEdgeBlock(world, edge);

                if (!TryEdgeLot(world, edge, out lotSlot))
                {
                    continue;
                }
            }

            world.CreateBuilding(world.Lots.Rows.At(lotSlot), kind, now, key);
            raised++;
        }

        return raised;
    }

    /// <summary>
    /// Subdivides the one lattice block that carries <paramref name="edge"/>, so a gate has ground.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The block index is derived from the lattice and states no number.</b>
    /// <see cref="StreetGrid.Blocks"/> is <c>Span - 1</c> — blocks, not lattice lines — so the last
    /// block is <c>Blocks - 1</c> and its far face is the last line, which sits on the map's
    /// boundary exactly. A block at column 0 carries the west edge, at row 0 the south, and the two
    /// far ones carry east and north.
    /// </para>
    /// <para>
    /// <b>The far edges are carved at the near end of the other axis</b> — <c>(Blocks - 1, 0)</c> and
    /// <c>(0, Blocks - 1)</c> rather than the far corner — so that neither gate lands on a Lot
    /// touching two edges. <see cref="World.EdgeOf"/> reports <see cref="MapEdge.None"/> there and
    /// <see cref="Invariant.OutsideConnectionStandsOnOneEdge"/> refuses it, because a gate on a corner
    /// sits in two Hinterlands with nothing to say which its emigrants came from.
    /// </para>
    /// <para>
    /// ⚠ <b>It is zoned <see cref="Housing"/> unconditionally, and never <see cref="Trade"/></b> —
    /// it does not consult <see cref="IsTradeBlock"/>, which is a decision rather than an omission:
    /// what makes a far gate usable is <em>a dwelling beside it</em> (see the Commute Budget note on
    /// <see cref="ReachesTheBoundary"/>), so putting a gate's own block on the trade side would
    /// forbid the one thing the carve exists to permit. So a
    /// Zone Rule may later raise a dwelling on whatever the gate does not take. That is deliberate:
    /// land at the edge is land, and reserving it would be siting policy — milestone 24's.
    /// </para>
    /// </remarks>
    private static void CarveEdgeBlock(World world, MapEdge edge)
    {
        int blocks = world.Roads.Streets.Blocks;

        if (blocks < 1)
        {
            return;
        }

        int far = blocks - 1;

        (int column, int row) = edge switch
        {
            MapEdge.West => (0, 0),
            MapEdge.South => (0, 0),
            MapEdge.East => (far, 0),
            MapEdge.North => (0, far),
            _ => (-1, -1),
        };

        if (column >= 0)
        {
            LotSubdivider.SubdivideBlock(world, column, row, Housing);
        }
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

    /// <summary>How many Households one Building has room for, on the ground it stands on.</summary>
    /// <remarks>
    /// <para>
    /// <b><c>World.DeclaredHousing</c>'s question asked of a Building rather than of a kind</b>
    /// (<c>plans/0053</c>), which is where it has to be asked now that the ceiling divides floor
    /// area. It is HOUSING and not TENANCY, so a Building that came with a trade has one place
    /// fewer (<c>adr/0148</c>) — the mistake that once left the Unplaced Pool climbing 6.5% a
    /// reading, arriving here as an ordinary call rather than as arithmetic to get wrong again.
    /// </para>
    /// <para>
    /// 🔴 <b>ZERO IS A REAL ANSWER AND THIS RETURNED ONE.</b> A floor of one read as <em>every
    /// Building houses somebody</em>, and on the smallest parcels it is false: a Building whose
    /// ground divides into a single tenancy, with a trade already in it, houses <b>nobody</b>. The
    /// populator filled seven such Buildings on <c>bordered.toml</c> and nothing failed, because
    /// <c>World.EvictOverflow</c> only runs on a Ruleset swap — so the overfill sat there until
    /// <c>ArrivalTests</c> adopted a file and watched seven Households fall into the Pool.
    /// ***A ceiling counts tenants of any kind (<c>adr/0147</c>), so a generator that counts only
    /// Households is measuring a different quantity from the one that evicts.***
    /// </para>
    /// </remarks>
    private static int Room(World world, Handle<Building> building)
    {
        int slot = world.Buildings.Rows.Resolve(building);

        // ⚠ UNDECLARED IS NOT ZERO, and collapsing the two emptied a city. A world that states no
        // [capacity] -- `Ruleset.Empty`, and every fixture standing on it -- declares no ceiling at
        // all, and the honest answer there is one per Building rather than nobody anywhere
        // (UndeclaredOccupancy). ***A DECLARED ceiling of one already spent on a trade is a real
        // zero***, and only the second of those may be returned as one.
        if (!world.TryDeclaredOccupancy(world.Buildings.Kind[slot], slot, out int allowed))
        {
            return UndeclaredOccupancy;
        }

        int housing = allowed - world.BuildingBusinesses.Length(slot);

        return housing > 0 ? housing : 0;
    }
}

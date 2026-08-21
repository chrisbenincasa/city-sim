using System.Globalization;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Invariants;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Entities;

/// <summary>
/// Milestone 5b-bis task 2: a Building's employment is declared by its kind, and an over-capacity
/// Building dismisses.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>adr/0068</c>'s rule on a second axis, and it transplants because the reason transplants
/// rather than because the shape does.</b> A Bin that goes over its ceiling is left to drain, and
/// that turns entirely on a Bin having a consumer. A job has a holder and no consumer — nothing in
/// the city spends a Building's surplus employment down — so a Building left over its <c>jobs</c>
/// ceiling would sit there for the life of the city employing a number its kind denies.
/// </para>
/// <para>
/// <b>The half that is not a copy is the worker list.</b> Occupancy already had a Building-to-
/// Households reverse index; employment had none, which is why <c>CitizenTable.Workplace</c> was
/// declared <see cref="Reference.Severable"/> and why its comment said so. Every test here that
/// looks at who works where is also a test that the maintained list and
/// <see cref="World.RebuildDerived"/> agree — and that agreement is checked by nothing else, because
/// a derived list folds into no hash and is therefore invisible to replay, to the golden baseline
/// and to save/reload alike.
/// </para>
/// </remarks>
public sealed class EmploymentTests
{
    private const ulong HashA = 0x3333_3333_3333_3333UL;
    private const ulong HashB = 0x4444_4444_4444_4444UL;

    private const byte Workplace = 1;

    private static readonly WorldKey Key = WorldKey.FromSeed(0x8000_0002UL);

    /// <summary>A workplace, with the ceiling left as a token for <see cref="Employing"/>.</summary>
    private const string Template = """
        [[resource]]
        name = "sundries"
        family = "good"

        [[building]]
        name = "workplace"
        jobs = POSTS
        bins = [
            { resource = "sundries", capacity = 12 },
        ]

        [[rule]]
        name    = "restock"
        kind    = "workplace"
        rate    = 8
        apply   = { min = 1, max = 4 }
        inputs  = []
        outputs = [ { scope = "local", resource = "sundries", amount = 1 } ]
        """;

    /// <summary>A workplace employing <paramref name="jobs"/> Citizens.</summary>
    /// <remarks>
    /// ⚠ <b>The Shift-start band appears only when the kind employs somebody</b>, because
    /// <c>adr/0101</c>'s loader refusal is two-way: a kind with <c>jobs &gt; 0</c> must state one and a
    /// kind with <c>jobs = 0</c> must not. This fixture is called with zero, so it has to honour both
    /// halves — which is the pairing working as designed rather than an awkwardness in the test.
    /// </remarks>
    private static string Employing(int jobs) => Template.Replace(
        "POSTS",
        jobs > 0
            ? jobs.ToString(CultureInfo.InvariantCulture)
                + "\nshift_start_earliest_hour = 6\nshift_start_latest_hour = 10"
            : jobs.ToString(CultureInfo.InvariantCulture),
        StringComparison.Ordinal);

    /// <summary>The same file with no <c>[[building]]</c> at all: every Building is derelict.</summary>
    private const string NoKinds = """
        [[resource]]
        name = "sundries"
        family = "good"
        """;

    private static Ruleset Load(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    /// <summary>
    /// One Building employing <paramref name="workers"/> Citizens, under a Ruleset that allows them.
    /// </summary>
    /// <remarks>
    /// The Citizens get a Household because <c>CitizenTable.HouseholdOf</c> is not severable and a
    /// Citizen with a dangling one is a broken world rather than an unemployed person. Where they
    /// live is deliberately the same Building they work in — this fixture is about the worker list,
    /// and a second Building would make a failure ambiguous between the two lists.
    /// </remarks>
    private static World City(int workers, int jobs, WorldKey? key = null)
    {
        var world = new World(1_000, Load(Employing(jobs)));

        Handle<Lot> lot = world.Lots.Create(new Tiles(0), new Tiles(0), zone: 1);
        Handle<Building> building = world.CreateBuilding(lot, Workplace, Ticks.Zero, key ?? Key);
        Handle<Household> household = world.CreateHousehold(building, lifeStage: 0);

        for (int i = 0; i < workers; i++)
        {
            world.Employ(world.CreateCitizen(household, Ticks.Zero), building, Ticks.Zero);
        }

        return world;
    }

    /// <summary>
    /// A <see cref="City"/> whose employed Citizens are also on the <b>commute roster</b>.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The <c>[jobs]</c> table is the difference and it is not decoration.</b>
    /// <c>CommuteRoster.TryPhasesOf</c> returns false unless <c>rules.Jobs.Runs</c>, because both
    /// Shift phases are drawn against <c>shift_hours_min</c>/<c>max</c> — so every fixture in this
    /// file employs Citizens who are on no roster at all, and a test about the roster written on
    /// <see cref="City"/> asserts nothing. ***A structure that populates only under a table the
    /// fixture does not state is a structure a fixture silently opts out of.***
    /// </remarks>
    private static World Rostering(int workers, int jobs)
    {
        string toml = Employing(jobs)
            + "\n[trips]\ncrossing_seconds = 30\ncommute_fast_minutes = 20\n"
            + "commute_moderate_minutes = 40\ncommute_budget_minutes = 50\n"
            + "\n[jobs]\ninterval = 32\nrevisit_ticks = 1024\ncandidates = 3\n"
            + "shift_hours_min = 6\nshift_hours_max = 10\narrive_early_max_minutes = 15\n";

        var world = new World(1_000, Load(toml));

        Handle<Lot> lot = world.Lots.Create(new Tiles(0), new Tiles(0), zone: 1);
        Handle<Building> building = world.CreateBuilding(lot, Workplace, Ticks.Zero, Key);
        Handle<Household> household = world.CreateHousehold(building, lifeStage: 0);

        for (int i = 0; i < workers; i++)
        {
            world.Employ(world.CreateCitizen(household, Ticks.Zero), building, Ticks.Zero);
        }

        return world;
    }

    /// <summary>The only Building in a <see cref="City"/> fixture.</summary>
    private static int TheBuilding(World world) => world.Buildings.Rows.Resolve(
        world.Buildings.Rows.At(0));

    /// <summary>Which Citizens work in <paramref name="buildingSlot"/>, by monotonic id, in list order.</summary>
    private static ulong[] Staff(World world, int buildingSlot)
    {
        var staff = new List<ulong>();

        foreach (int citizen in world.Workers.Walk(buildingSlot))
        {
            staff.Add(world.Citizens.Rows.IdAt(citizen));
        }

        return [.. staff];
    }

    /// <summary>Every Citizen whose Workplace handle resolves, by monotonic id, in slot order.</summary>
    private static ulong[] Employed(World world) =>
        [.. Enumerable.Range(0, world.Citizens.Rows.SlotCount)
            .Where(world.Citizens.Rows.IsLive)
            .Where(slot => world.Buildings.Rows.TryResolve(world.Citizens.Workplace[slot], out _))
            .Select(world.Citizens.Rows.IdAt)];

    // ---- the ceiling reaches Buildings already standing ------------------------------------------

    /// <summary>
    /// <b>Lowering a kind's <c>jobs</c> dismisses the overflow.</b>
    /// </summary>
    /// <remarks>
    /// The acceptance test for the task, and note what it is <em>not</em>: no migration and no
    /// degradation. Employment is a <c>RulesetChange.None</c> edit exactly as a ceiling is, so this
    /// runs on the path a designer uses a hundred times in a sitting — which is the path
    /// <c>adr/0068</c> found a Bin ceiling had never reached.
    /// </remarks>
    [Fact]
    public void Lowering_the_ceiling_dismisses_the_overflow()
    {
        World world = City(workers: 4, jobs: 4);

        Assert.Equal(4, Employed(world).Length);

        world.Adopt(Load(Employing(1)), HashB, new Ticks(64), Key);

        Assert.Single(Employed(world));
        Assert.Single(Staff(world, TheBuilding(world)));
    }

    /// <summary>
    /// <b>A dismissed Citizen's Workplace is cleared, not left severed.</b>
    /// </summary>
    /// <remarks>
    /// The distinction demolition makes and this does not: a Building bulldozed out from under a
    /// worker leaves a handle pointing at a freed row, which reads as <em>the job stopped
    /// existing</em> and is the fact. A ceiling lowered under one leaves the employer standing, so a
    /// severed handle would be a lie about a Building the player can still see. Clearing is also
    /// what puts the Citizen back in front of task 4's assignment pass.
    /// </remarks>
    [Fact]
    public void A_dismissed_citizen_holds_no_workplace_handle_at_all()
    {
        World world = City(workers: 3, jobs: 3);

        world.Adopt(Load(Employing(0)), HashB, new Ticks(64), Key);

        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (world.Citizens.Rows.IsLive(slot))
            {
                Assert.Equal(default, world.Citizens.Workplace[slot]);
            }
        }

        Assert.Empty(Staff(world, TheBuilding(world)));
    }

    /// <summary>Raising the ceiling dismisses nobody.</summary>
    [Fact]
    public void Raising_the_ceiling_dismisses_nobody()
    {
        World world = City(workers: 2, jobs: 2);
        ulong[] before = Employed(world);

        world.Adopt(Load(Employing(8)), HashB, new Ticks(64), Key);

        Assert.Equal(before, Employed(world));
    }

    /// <summary>
    /// <b>A Building whose kind the incoming Ruleset dropped keeps its workers.</b>
    /// </summary>
    /// <remarks>
    /// The two negative cases kept apart, on the employment axis. A declared kind at <c>jobs = 0</c>
    /// employs nobody; a kind the Ruleset does not mention has no ceiling at all. Collapsing them
    /// would make a designer deleting a paragraph sack a District — the loudest possible consequence
    /// for the quietest possible edit, and the one <c>02 §4.3</c> calls dereliction rather than
    /// abandonment.
    /// </remarks>
    [Fact]
    public void A_kind_the_ruleset_dropped_keeps_its_workers()
    {
        World world = City(workers: 3, jobs: 3);

        world.Adopt(Load(NoKinds), HashB, new Ticks(64), Key);

        Assert.Equal(3, Employed(world).Length);
        Assert.Equal(3, Staff(world, TheBuilding(world)).Length);
    }

    /// <summary>A derelict Building takes nobody new either.</summary>
    [Fact]
    public void A_derelict_building_employs_nobody_new()
    {
        World world = City(workers: 0, jobs: 4);

        Assert.True(world.HasJob(TheBuilding(world)));

        world.Adopt(Load(NoKinds), HashB, new Ticks(64), Key);

        Assert.False(world.HasJob(TheBuilding(world)));
    }

    // ---- the predicate ---------------------------------------------------------------------------

    /// <summary>
    /// A fully-staffed Building has no job, and a dismissal opens one.
    /// </summary>
    /// <remarks>
    /// <see cref="World.HasJob"/> is the predicate task 4's sampling pass asks of every candidate,
    /// and a full employer is an ordinary answer rather than a fault — which is why there is no
    /// invariant behind it. <see cref="World.HasRoom"/> is the same shape for the same reason.
    /// </remarks>
    [Fact]
    public void A_full_building_has_no_job_and_a_dismissal_opens_one()
    {
        World world = City(workers: 2, jobs: 2);
        int building = TheBuilding(world);

        Assert.False(world.HasJob(building));

        world.Dismiss(world.Citizens.Rows.At(0));

        Assert.True(world.HasJob(building));
    }

    // ---- the worker list -------------------------------------------------------------------------

    /// <summary>
    /// <b>Taking a job leaves the previous employer's list.</b>
    /// </summary>
    /// <remarks>
    /// The failure a one-directional write produces: a Citizen listed at two Buildings walks as two
    /// workers, fills one job twice, and is reported by nothing until the whole-world count runs.
    /// This is why <see cref="World.Employ"/> is the only door onto the handle.
    /// </remarks>
    [Fact]
    public void Changing_employer_leaves_the_first_list()
    {
        World world = City(workers: 1, jobs: 4);

        Handle<Lot> second = world.Lots.Create(new Tiles(64), new Tiles(0), zone: 1);
        Handle<Building> elsewhere = world.CreateBuilding(second, Workplace, Ticks.Zero, Key);

        world.Employ(world.Citizens.Rows.At(0), elsewhere, Ticks.Zero);

        Assert.Empty(Staff(world, TheBuilding(world)));
        Assert.Single(Staff(world, world.Buildings.Rows.Resolve(elsewhere)));
    }

    /// <summary>
    /// <b>Demolishing a workplace unlists its staff and leaves their handles severed.</b>
    /// </summary>
    /// <remarks>
    /// <b>The handle is deliberately not cleared, and the reason is the State Hash.</b> Clearing it
    /// would make demolition write a saved column for a reason that has nothing to do with the
    /// demolition. The handle is <see cref="Reference.Severable"/> precisely so a dangling one can
    /// mean <em>the job stopped existing</em>, and unlisting alone is what makes the write path
    /// agree with the rebuild, whose <c>TryResolve</c> drops exactly these Citizens.
    /// </remarks>
    [Fact]
    public void Demolition_unlists_the_workers_and_severs_their_handles()
    {
        World world = City(workers: 3, jobs: 3);
        Handle<Building> building = world.Buildings.Rows.At(0);

        world.DestroyBuilding(building, new Ticks(64));

        Assert.Empty(Employed(world));

        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (world.Citizens.Rows.IsLive(slot))
            {
                Assert.NotEqual(default, world.Citizens.Workplace[slot]);
            }
        }
    }

    /// <summary>
    /// <b>Retiring a Citizen takes them off their employer's list before the row is freed.</b>
    /// </summary>
    /// <remarks>
    /// A freed row still threaded into a live list is <c>adr/0006</c>'s unreachable-and-unfreeable
    /// row arriving by the back door, and the next allocation of the slot would be inserted into a
    /// list it is already in.
    /// </remarks>
    [Fact]
    public void Retiring_a_citizen_takes_them_off_the_worker_list()
    {
        World world = City(workers: 3, jobs: 3);

        world.DestroyCitizen(world.Citizens.Rows.At(1));

        Assert.Equal(2, Staff(world, TheBuilding(world)).Length);
    }

    /// <summary>
    /// <b>Retiring a Household takes its members off the commute roster, and not only off the worker
    /// list.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b><c>plans/0035</c> <b>F29</b>, and the asymmetry is one line.</b>
    /// <see cref="World.DestroyCitizen"/> calls <c>Commutes.Remove</c> and then <c>Unlist</c>;
    /// <see cref="World.DestroyHousehold"/> called only <c>Unlist</c> on each member before freeing
    /// its row. The worker list was maintained and the <em>commute roster</em> was not, so every
    /// Household destroyed with an employed member left two dangling bucket entries behind it.
    /// </para>
    /// <para>
    /// ⚠ <b>It is asserted through the recycled slot, because that is the only place it presents.</b>
    /// A dangling entry costs nothing until the slot is allocated again — and then
    /// <c>InsertOrdered</c> finds the row already in the list it is being put into and throws, which
    /// is <c>CommuteRoster.Remove</c>'s own doc-comment describing the defect it was written to
    /// prevent. ***A structure whose corruption is silent until an unrelated allocation is one whose
    /// test has to perform the allocation.***
    /// </para>
    /// <para>
    /// ⚠ <b>Nothing in the invariant tier could have caught it.</b>
    /// <see cref="Invariant.NoFreedRowIsStillLinked"/> walks the lists the tables declare; the
    /// commute roster is <see cref="Disposition.Derived"/> and lives in its own structure, so a freed
    /// row threaded into it is outside what that check can see. It was found by a 100,000-Tick
    /// acceptance run on <c>rulesets/crowded.toml</c> — milestone 11 task 9 — and by nothing else in
    /// eleven milestones.
    /// </para>
    /// </remarks>
    [Fact]
    public void Retiring_a_household_takes_its_members_off_the_commute_roster()
    {
        World world = Rostering(workers: 2, jobs: 8);
        Handle<Building> building = world.Buildings.Rows.At(0);
        Handle<Household> household = world.Households.Rows.At(0);

        // Vacuity, and it is load-bearing: the roster holds only Citizens whose Workplace yields two
        // Shift phases, so a fixture whose kind stated no Shift band would satisfy everything below
        // by never rostering anybody at all.
        Assert.True(
            world.Citizens.CommuteBucket[world.Citizens.Rows.Resolve(world.Citizens.Rows.At(0))] != 0,
            "no Citizen in this fixture is on the commute roster, so the assertion below is about a "
            + "structure the fixture never populated.");

        world.DestroyHousehold(household);

        // The slots just freed are the ones the next Citizens are allocated into, so employing two
        // more is what walks them back into the buckets the destroyed pair was left in.
        Handle<Household> second = world.CreateHousehold(building, lifeStage: 0);

        world.Employ(world.CreateCitizen(second, Ticks.Zero), building, Ticks.Zero);
        world.Employ(world.CreateCitizen(second, Ticks.Zero), building, Ticks.Zero);

        Assert.Equal(2, Staff(world, TheBuilding(world)).Length);
    }

    /// <summary>
    /// <b>And the buckets are empty afterwards, which is the same claim without the recycling.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The pair above and this one fail differently and both are worth keeping.</b> This one names
    /// the state that is wrong at the moment it becomes wrong; the one above names what a player
    /// would see, which is a throw on an allocation that has nothing to do with the Household that
    /// left.
    /// </para>
    /// <para>
    /// ⚠ <b>It counts the buckets rather than reading <c>CommuteBucket</c>, and the first draft read
    /// the column.</b> That draft passed against the defect: <c>Rows.Free</c> clears a derived column
    /// on the way out, so the column said <em>not rostered</em> while the list still threaded the
    /// row. ***Asking the row whether it is in a list is asking the wrong end*** — which is the
    /// warning <c>CommuteRoster.Remove</c> already carries about recomputing a bucket instead of
    /// reading where the row was put, arriving at a test from the other side.
    /// </para>
    /// </remarks>
    [Fact]
    public void Retiring_a_household_empties_the_buckets_its_members_were_in()
    {
        World world = Rostering(workers: 2, jobs: 8);

        int rostered = 0;

        for (int phase = 0; phase < Ticks.PerDay; phase++)
        {
            rostered += world.Commutes.CountAt(world.Citizens, phase)
                + world.Commutes.ReturningCountAt(world.Citizens, phase);
        }

        // Two Citizens, each in an outbound bucket and a homeward one.
        Assert.Equal(4, rostered);

        world.DestroyHousehold(world.Households.Rows.At(0));

        int left = 0;

        for (int phase = 0; phase < Ticks.PerDay; phase++)
        {
            left += world.Commutes.CountAt(world.Citizens, phase)
                + world.Commutes.ReturningCountAt(world.Citizens, phase);
        }

        Assert.Equal(0, left);
    }

    // ---- the declaration -------------------------------------------------------------------------

    /// <summary>
    /// <b>A rebuild reproduces the worker list exactly, across a recycled slot.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The test that licenses <c>CitizenTable.WorkerNext</c>'s
    /// <see cref="Disposition.Derived"/> declaration, and the only one here that discriminates.</b>
    /// <c>05 §3</c>'s rule is that a derived list's <em>order</em> must be recoverable and not merely
    /// its membership, and a maintained list built with <c>Append</c> passes every membership
    /// assertion in this file and fails only this: create A and B, free A, create C, and appending
    /// gives <c>B, C</c> where a rebuild — which has only slot order to walk in — gives <c>C, B</c>.
    /// </para>
    /// <para>
    /// In production it would present as a saved-and-reloaded city draining a worker list in a
    /// different order from a continuously-run one, <b>with nothing to report it</b>, because the
    /// list is derived and therefore folded into no hash.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_rebuild_reproduces_the_worker_list_across_a_recycled_slot()
    {
        World world = City(workers: 2, jobs: 8);
        Handle<Building> building = world.Buildings.Rows.At(0);
        Handle<Household> household = world.Households.Rows.At(0);

        world.DestroyCitizen(world.Citizens.Rows.At(0));
        world.Employ(world.CreateCitizen(household, Ticks.Zero), building, Ticks.Zero);

        int slot = world.Buildings.Rows.Resolve(building);
        ulong[] maintained = Staff(world, slot);

        world.RebuildDerived();

        Assert.Equal(2, maintained.Length);
        Assert.Equal(maintained, Staff(world, slot));
    }

    /// <summary>
    /// A rebuild lists nobody for a Citizen whose employer was demolished.
    /// </summary>
    /// <remarks>
    /// The other half of the demolition decision: the write path unlists and the rebuild's handle
    /// resolve drops. Either one alone would make a reloaded city disagree with a running one about
    /// who works at a Building that is not there.
    /// </remarks>
    [Fact]
    public void A_rebuild_drops_a_worker_whose_employer_is_gone()
    {
        World world = City(workers: 3, jobs: 3);

        world.DestroyBuilding(world.Buildings.Rows.At(0), new Ticks(64));
        world.RebuildDerived();

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            Assert.Empty(Staff(world, slot));
        }
    }

    // ---- the draw --------------------------------------------------------------------------------

    /// <summary>
    /// <b>Who is dismissed is drawn, not taken from the end of the list.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two assertions in one, because either alone is satisfiable by the wrong implementation. Two
    /// worlds with one key dismiss the same people, without which no replay reproduces the patch;
    /// two worlds with different keys dismiss different people, without which the draw is list order
    /// wearing a hash — and <c>02 §8</c> rule 5 is what that would break, since the same Citizens
    /// would lose their jobs in every Building on every patch with nothing to say why.
    /// </para>
    /// <para>
    /// The second half is a property of a specific pair of seeds rather than of all pairs, which is
    /// what makes it a test rather than a proof. An implementation ignoring the key fails it every
    /// time.
    /// </para>
    /// </remarks>
    [Fact]
    public void Who_is_dismissed_is_drawn_rather_than_taken_from_the_end_of_the_list()
    {
        var other = WorldKey.FromSeed(0x9000_000AUL);

        World first = City(workers: 6, jobs: 6);
        World again = City(workers: 6, jobs: 6);
        World elsewhere = City(workers: 6, jobs: 6, key: other);

        first.Adopt(Load(Employing(2)), HashB, new Ticks(64), Key);
        again.Adopt(Load(Employing(2)), HashB, new Ticks(64), Key);
        elsewhere.Adopt(Load(Employing(2)), HashB, new Ticks(64), other);

        Assert.Equal(Employed(first), Employed(again));
        Assert.NotEqual(Employed(first).Order(), Employed(elsewhere).Order());
    }

    /// <summary>
    /// <b>A patch lowering both ceilings does not turn the same people out of both.</b>
    /// </summary>
    /// <remarks>
    /// <see cref="PurposeTag.JobEviction"/> earning its number. Sharing
    /// <see cref="PurposeTag.OverflowEviction"/> would tie <em>who is sacked</em> to <em>who is
    /// evicted from their home</em>, so a single patch would produce two misfortunes with one hidden
    /// cause — a correlation the city never modelled and no readout could explain. The two draws are
    /// over different tables here, so what the test can check is that the tag reaches the draw at
    /// all: the same Citizen ids under two tags must not select the same rank.
    /// </remarks>
    [Fact]
    public void The_job_draw_does_not_share_the_eviction_tag()
    {
        World world = City(workers: 8, jobs: 8);

        var sacked = new List<ulong>();
        var evicted = new List<ulong>();

        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (!world.Citizens.Rows.IsLive(slot))
            {
                continue;
            }

            ulong id = world.Citizens.Rows.IdAt(slot);

            sacked.Add(Randomness.Draw(Key, id, new Ticks(64), PurposeTag.JobEviction));
            evicted.Add(Randomness.Draw(Key, id, new Ticks(64), PurposeTag.OverflowEviction));
        }

        Assert.NotEqual(sacked, evicted);
    }

    // ---- the populator ---------------------------------------------------------------------------

    /// <summary>
    /// <b>The populator gives nobody a job, because no shipped Ruleset grants one.</b>
    /// </summary>
    /// <remarks>
    /// <b>5b-bis task 2's finding, kept as a test rather than as a paragraph.</b>
    /// <c>SyntheticCity</c> assigned a Workplace on a <c>(i * 7)</c> stride into the dwelling list,
    /// so that the commute matrix would not be the identity — employment that no
    /// <c>[[building]]</c> declaration granted, and that nothing could contradict while jobs were
    /// uncountable. The first Ruleset adoption after this key existed dismissed all 1,000 of them at
    /// once. The stride is deleted; task 4 acquires a Workplace by a mechanism, and *two ways to get
    /// a job* is <c>plans/0012</c> Cause 1 with both copies executing.
    /// </remarks>
    [Fact]
    public void The_populator_gives_nobody_a_workplace()
    {
        var world = new World(1_000, Load(Employing(0)));

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        Assert.NotEqual(0, world.Citizens.Rows.SlotCount);
        Assert.Empty(Employed(world));
    }
}

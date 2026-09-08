using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// <c>plans/0045</c> row 28: the choice model reached through the engine rather than through its own
/// arithmetic.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>ChoiceTests</c> asserts the distribution and this asserts that a city is drawn from it.</b>
/// The two are worth keeping apart: a softmax no Household ever calls would pass every test in that
/// class, which is the shape row 28 was opened about — <c>Transcendental.Exp</c> built, tested,
/// documented and never called.
/// </para>
/// <para>
/// ⚠ <b>The control is μ raised rather than μ removed</b>, and that is the design of these
/// assertions. Removing μ removes the utility function with it, so the two runs would differ in what
/// they score as well as in how sharply they act on it. Raising μ holds the scores fixed and moves
/// only the sharpness, which is the one variable 02 section 5.4 says to reach for.
/// </para>
/// <para>
/// 🔴 <b>Nothing here measures rent, and that is a finding rather than a gap in the tests.</b>
/// <c>chosen.toml</c>'s header records it: rent is per kind, no shipped world stands two housing
/// kinds, and a term identical across every candidate cancels in a softmax. The rent scale is read
/// and has no difference to weigh.
/// </para>
/// </remarks>
public sealed class PlacementChoiceTests
{
    private const int Citizens = 12_000;
    private const ulong Ticks = 20_480;

    private static readonly WorldKey Key = WorldKey.FromSeed(0);

    private static string Source() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "chosen.toml"));

    /// <summary>chosen.toml with its scale parameter replaced, and nothing else.</summary>
    private static Ruleset At(int muPercent)
    {
        string toml = Source().Replace(
            "mu_percent                = 100",
            $"mu_percent                = {muPercent}",
            StringComparison.Ordinal);

        RulesetLoadResult result = RulesetLoader.Parse(toml, "chosen.toml");

        return result.Ruleset
            ?? throw new InvalidOperationException($"chosen.toml was refused:\n{result.Describe()}");
    }

    private static World Run(Ruleset rules)
    {
        World world = new(Citizens, rules, Key);
        Simulation simulation = new(world, Key);

        SyntheticCity.PopulateInto(world, Key, Core.Quantities.Ticks.Zero);

        for (ulong tick = 0; tick < Ticks; tick++)
        {
            simulation.Step(default);
        }

        return world;
    }

    /// <summary>
    /// The mean walk to the centre over the Households that want to be near it.
    /// </summary>
    /// <remarks>
    /// <b><c>TasteTests</c>' quantity, read against μ rather than against taste.</b> That class
    /// asks whether a preference reaches the map at all; this asks whether acting on it harder
    /// changes where people end up.
    /// </remarks>
    private static long CentreMeanWalk(World world)
    {
        long walked = 0;
        int counted = 0;

        for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
        {
            if (!world.Households.Rows.IsLive(slot)
                || !world.Buildings.Rows.TryResolve(world.Households.Dwelling[slot], out int building))
            {
                continue;
            }

            int taste = world.Rules.CentralityTaste(
                Key, world.Households.Rows.IdAt(slot), world.Households.LifeStage[slot]);

            if (taste <= Core.Arithmetic.Fixed.One / 2)
            {
                continue;
            }

            if (!world.Lots.Rows.TryResolve(world.Buildings.Lot[building], out int lot))
            {
                continue;
            }

            long east = world.Lots.East[lot].Raw;
            long north = world.Lots.North[lot].Raw;

            walked += (east < 0 ? -east : east) + (north < 0 ? -north : north);
            counted++;
        }

        Assert.True(counted > 0, "no housed Household wanted the centre, so nothing was measured.");

        return walked / counted;
    }

    /// <summary>
    /// Acting harder on the scores puts the families who want the centre nearer to it.
    /// </summary>
    /// <remarks>
    /// <b>This is the assertion that could not exist before the model did.</b> An argmax has one
    /// behaviour; a distribution has a family of them, and μ is what indexes it. A city that barely
    /// reads its own scores and one that obeys them absolutely are both reachable from one Ruleset
    /// key, and the shipped value sits between them.
    /// </remarks>
    [Fact]
    public void A_sharper_mu_puts_centre_seekers_nearer_the_centre()
    {
        long loose = CentreMeanWalk(Run(At(10)));
        long sharp = CentreMeanWalk(Run(At(2_000)));

        Assert.True(
            sharp < loose,
            $"mu = 20 left centre-seeking Households a mean {sharp} Tiles out and mu = 0.1 left "
            + $"them {loose}. A sharper choice must act harder on the scores, or nothing reads them.");
    }

    /// <summary>The draw is deterministic, which a hash-bearing choice has to be.</summary>
    [Fact]
    public void The_choice_is_reproducible()
    {
        Assert.Equal(Run(At(100)).HashState(), Run(At(100)).HashState());
    }

    /// <summary>
    /// μ moves the State Hash, so the key reaches the city rather than merely loading.
    /// </summary>
    [Fact]
    public void Mu_is_hash_bearing()
    {
        Assert.NotEqual(Run(At(100)).HashState(), Run(At(2_000)).HashState());
    }

    /// <summary>
    /// A world stating no μ is untouched by any of this, which is what kept every hash still.
    /// </summary>
    [Fact]
    public void A_world_that_states_no_mu_keeps_the_argmax()
    {
        Assert.True(At(100).Placement.Chooses);

        RulesetLoadResult control = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "choosy.toml"));

        Assert.False(control.Ruleset!.Placement.Chooses);
    }
}

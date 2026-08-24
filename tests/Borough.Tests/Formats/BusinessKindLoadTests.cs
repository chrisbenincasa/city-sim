using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// Milestone 27 task 6: the <c>[[business]]</c> kind table — the Ruleset's <em>second</em> kind
/// namespace (<c>adr/0141</c>).
/// </summary>
/// <remarks>
/// <para>
/// <b>The whole point of this class is the namespace independence</b>, and it is the one thing
/// <c>rulesets/tenanted.toml</c> deliberately does not demonstrate. The sharpest case is a
/// <c>[[business]]</c> and a <c>[[building]]</c> carrying the <em>same name</em> — which a shipped
/// demonstration file must not do, because it would be content making a joke, and which a test must,
/// because <c>adr/0141</c>'s entire argument is that the premises and the trade are uncorrelated.
/// </para>
/// <para>
/// <b>A Business kind declares nothing but its name and there is nothing else to assert.</b> There is
/// no <c>jobs</c>, no shift band and no wage until milestone 27 task 7, so every test here is about
/// registration, ids and identity across a reload. ⚠ <b>A class this thin is the honest shape</b>
/// rather than a gap: the kind buys identity today and behaviour later, and asserting behaviour it
/// does not have would be asserting the test's own fixture.
/// </para>
/// </remarks>
public sealed class BusinessKindLoadTests
{
    /// <summary>The smallest complete Ruleset. A Business kind needs no Rule and no road.</summary>
    private const string Nothing = """
        [[resource]]
        name = "flour"
        family = "good"
        """;

    private static Ruleset Accepted(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    private static RulesetLoadResult Parse(string toml) => RulesetLoader.Parse(toml, "test.toml");

    private static RulesetRefusal Refused(string toml)
    {
        RulesetLoadResult result = Parse(toml);

        Assert.False(result.Ok, "expected a refusal and the Ruleset loaded.");

        return result.Refusals[0];
    }

    [Fact]
    public void A_ruleset_that_names_no_trade_declares_none()
    {
        Ruleset ruleset = Accepted(Nothing);

        Assert.Equal(0, ruleset.BusinessKindCount);
        Assert.False(ruleset.DeclaresBusiness(1));
    }

    [Fact]
    public void Trades_take_ids_in_declaration_order_starting_at_one()
    {
        Ruleset ruleset = Accepted($"""
            {Nothing}

            [[business]]
            name = "bakery"

            [[business]]
            name = "barber"
            """);

        Assert.Equal(2, ruleset.BusinessKindCount);
        Assert.True(ruleset.DeclaresBusiness(1));
        Assert.True(ruleset.DeclaresBusiness(2));

        // Zero is reserved for `names no trade` throughout, exactly as it is for a Building kind.
        Assert.False(ruleset.DeclaresBusiness(0));
        Assert.False(ruleset.DeclaresBusiness(3));
    }

    /// <summary>
    /// <b>The load-bearing test in this class.</b> A trade and a premises kind may share a name.
    /// </summary>
    /// <remarks>
    /// <c>adr/0141</c>: <em>"the premises and the trade are not correlated, and nothing in a single
    /// kind table can express that."</em> If these two shared a namespace, the second declaration
    /// would be refused as a duplicate — so this test failing means the two tables have been merged,
    /// whatever the code says it is doing.
    /// </remarks>
    [Fact]
    public void A_trade_and_a_premises_kind_may_carry_the_same_name()
    {
        Ruleset ruleset = Accepted($"""
            {Nothing}

            [[building]]
            name = "dwelling"

            [[business]]
            name = "dwelling"
            """);

        Assert.Equal(1, ruleset.KindCount);
        Assert.Equal(1, ruleset.BusinessKindCount);

        // Same id, two namespaces, two declarations.
        Assert.True(ruleset.Declares(1));
        Assert.True(ruleset.DeclaresBusiness(1));
    }

    [Fact]
    public void A_second_trade_of_one_name_is_refused()
    {
        RulesetRefusal refusal = Refused($"""
            {Nothing}

            [[business]]
            name = "bakery"

            [[business]]
            name = "bakery"
            """);

        Assert.Contains("a second [[business]] is named 'bakery'", refusal.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void A_trade_with_no_name_is_refused()
    {
        RulesetRefusal refusal = Refused($"""
            {Nothing}

            [[business]]
            """);

        Assert.Contains("name", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>The names reach the shell, and the two namespaces resolve separately.</summary>
    /// <remarks>
    /// <c>Borough.Core</c> returns ids and never strings (<c>05 §1</c>), so this is the only path by
    /// which a trade's name is readable at all.
    /// </remarks>
    [Fact]
    public void The_shell_can_name_a_trade_and_a_premises_kind_of_the_same_id_separately()
    {
        RulesetLoadResult result = Parse($"""
            {Nothing}

            [[building]]
            name = "dwelling"

            [[business]]
            name = "bakery"
            """);

        Assert.True(result.Ok, result.Describe());

        Assert.Equal("dwelling", result.Names.Kind(1));
        Assert.Equal("bakery", result.Names.BusinessKind(1));

        // An id nobody declared has no word, which is a fact rather than a placeholder for one.
        Assert.Null(result.Names.BusinessKind(2));
        Assert.Null(result.Names.BusinessKind(0));
    }

    /// <summary>Reordering the trades is a change of identity, not of shape.</summary>
    /// <remarks>
    /// <b>The failure this catches is silent and total</b>, and it is <c>RulesetChange.KindIdentity</c>
    /// arriving on the second namespace: every count stays equal while every live <c>Business</c> row
    /// starts naming the other trade.
    /// </remarks>
    [Fact]
    public void Swapping_two_trades_is_caught_as_a_change_of_identity()
    {
        Ruleset before = Accepted($"""
            {Nothing}

            [[business]]
            name = "bakery"

            [[business]]
            name = "barber"
            """);

        Ruleset after = Accepted($"""
            {Nothing}

            [[business]]
            name = "barber"

            [[business]]
            name = "bakery"
            """);

        Assert.Equal(RulesetChange.BusinessKindIdentity, RulesetShape.Compare(before, after));
    }

    [Fact]
    public void Adding_a_trade_is_caught_as_a_change_of_count()
    {
        Ruleset before = Accepted($"""
            {Nothing}

            [[business]]
            name = "bakery"
            """);

        Ruleset after = Accepted($"""
            {Nothing}

            [[business]]
            name = "bakery"

            [[business]]
            name = "barber"
            """);

        Assert.Equal(RulesetChange.BusinessKindCount, RulesetShape.Compare(before, after));
    }

    /// <summary>A reordered file remaps a live row's trade rather than repointing it.</summary>
    /// <remarks>
    /// <b>This is what <c>Ruleset.BusinessKindKeys</c> exists for.</b> The ids swap and the keys do
    /// not, so a Business holding id 1 — <em>bakery</em> — comes out holding id 2, which is
    /// <em>bakery</em> in the new file.
    /// </remarks>
    [Fact]
    public void A_reordered_file_remaps_a_trade_to_where_its_name_went()
    {
        Ruleset before = Accepted($"""
            {Nothing}

            [[business]]
            name = "bakery"

            [[business]]
            name = "barber"
            """);

        Ruleset after = Accepted($"""
            {Nothing}

            [[business]]
            name = "barber"

            [[business]]
            name = "bakery"
            """);

        RulesetMigration migration = RulesetMigration.Between(before, after);

        Assert.Equal(2, migration.BusinessKind(1));
        Assert.Equal(1, migration.BusinessKind(2));
    }

    /// <summary>A trade the new file does not name becomes zero, which is the word being lost.</summary>
    [Fact]
    public void A_deleted_trade_migrates_to_nothing()
    {
        Ruleset before = Accepted($"""
            {Nothing}

            [[business]]
            name = "bakery"
            """);

        Ruleset after = Accepted(Nothing);

        RulesetMigration migration = RulesetMigration.Between(before, after);

        Assert.Equal(0, migration.BusinessKind(1));
    }

    /// <summary>The section is named in the refusal that lists what a Ruleset section may be.</summary>
    /// <remarks>
    /// ⚠ <b>A list a reader is sent to that omits a legal section is worse than no list</b>, because
    /// it reads as exhaustive. This is the one assertion here that would have failed silently.
    /// </remarks>
    [Fact]
    public void The_section_list_names_business()
    {
        RulesetRefusal refusal = Refused($"""
            {Nothing}

            [[nonsense]]
            name = "x"
            """);

        Assert.Contains("is not a Ruleset section", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("[[business]]", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// <c>rulesets/tenanted.toml</c> names two trades, in the order its header says, and instantiates
    /// neither.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Milestone 27 task 6's shipped file, and the reason it has a test at all.</b> Every other test
    /// here writes its own Ruleset; this one asserts that a file a person can actually run still shows
    /// what it claims to. ⚠ <b>Without it the file is a demonstration nothing defends</b> — which is
    /// exactly how milestone 25 reached its closing task and found the thing to look at did not exist
    /// in any shipped world (<c>plans/0040</c> F43).
    /// </para>
    /// <para>
    /// 🔴 <b>The last assertion is the honest half.</b> Nothing creates a Business, so this world runs
    /// with two trades declared and no Business wearing either. ***Asserting that the table is empty is
    /// asserting what the file is FOR*** — it demonstrates the namespace, not the actor — and it is the
    /// assertion that will fail, correctly, on the day milestone 27 task 8 ships a pass that tenants
    /// one.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_shipped_tenanted_ruleset_names_two_trades_and_instantiates_neither()
    {
        RulesetLoadResult loaded = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "tenanted.toml"));

        Assert.True(loaded.Ok, loaded.Describe());

        Ruleset ruleset = loaded.Ruleset!;

        Assert.Equal(2, ruleset.BusinessKindCount);
        Assert.Equal("bakery", loaded.Names.BusinessKind(1));
        Assert.Equal("barber", loaded.Names.BusinessKind(2));

        // The premises namespace is untouched by any of it: this is still minimal.toml's one kind.
        Assert.Equal(1, ruleset.KindCount);
        Assert.Equal("dwelling", loaded.Names.Kind(1));

        var world = new Core.Entities.World(1_000, ruleset);

        Assert.Equal(0, world.Businesses.Rows.LiveCount);
    }

    /// <summary>
    /// <c>tenanted.toml</c> and <c>minimal.toml</c> produce the identical city, Tick for Tick.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the file's demonstration, and it is an equality rather than a behaviour.</b> The two
    /// files differ by two <c>[[business]]</c> blocks and by nothing else. Nothing creates a Business,
    /// so those blocks reach no column, no Bin and no Rule — and the claim <em>a Business kind declares
    /// nothing until task 7</em> is only worth the words if the State Hash agrees. ***It does, and this
    /// asserts it rather than the header claiming it.***
    /// </para>
    /// <para>
    /// ⚠ <b>It is a TRIPWIRE and is meant to go red.</b> The day milestone 27 task 8 ships a pass that
    /// tenants a Business, these two worlds diverge and this test fails — correctly, and it is the
    /// clearest signal available that the actor started existing. <b>The response then is to delete this
    /// test, not to loosen it</b>; an equality weakened to a tolerance would be asserting nothing.
    /// </para>
    /// <para>
    /// ⚠ <b>It carries its own control, because an equality assertion can pass by comparing
    /// nothing.</b> Two traces of four zeroes are equal. So <c>evicted.toml</c> — <c>minimal.toml</c>
    /// with two Rules deleted — is traced as well and asserted <em>different</em>: if that one matches
    /// too, this method cannot tell two cities apart and the real assertion below it is vacuous.
    /// </para>
    /// <para>
    /// ⚠ <b>The Ruleset content hashes differ and that is not a divergence.</b> A content hash is a file
    /// fingerprint covering the comments; a State Hash is the city. The two answer different questions
    /// and only the second one is compared here.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_two_trades_change_nothing_about_the_city()
    {
        var key = WorldKey.FromSeed(0xB0A0_0C6E_A7E0_0027UL);

        ulong[] tenanted = Trace("tenanted.toml", key);
        ulong[] minimal = Trace("minimal.toml", key);

        // The control, and an equality is worth nothing without one. A Trace that returned four
        // zeroes -- an unpopulated world, a Simulation that never stepped -- would satisfy the
        // assertion below while comparing nothing at all. `evicted.toml` is minimal.toml with two
        // Rules deleted, so if these two ALSO match, this method cannot tell two cities apart and
        // the real assertion is vacuous.
        Assert.NotEqual(minimal, Trace("evicted.toml", key));

        Assert.Equal(minimal, tenanted);
    }

    /// <summary>Populates one shipped file the way the runner does and samples the State Hash.</summary>
    private static ulong[] Trace(string file, WorldKey key)
    {
        RulesetLoadResult loaded = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

        Assert.True(loaded.Ok, loaded.Describe());

        var world = new Core.Entities.World(1_000, loaded.Ruleset!);

        Core.Entities.SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        var simulation = new Simulation(world, key);
        ulong[] samples = new ulong[4];

        for (int sample = 0; sample < samples.Length; sample++)
        {
            for (int tick = 0; tick < 64; tick++)
            {
                simulation.Step(TickInput.Empty);
            }

            samples[sample] = world.HashState();
        }

        return samples;
    }
}

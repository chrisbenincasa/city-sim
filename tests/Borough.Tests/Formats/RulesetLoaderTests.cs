using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// Slice 7 task 3: the Ruleset loader, and its refusals in one load-time walk.
/// </summary>
/// <remarks>
/// <b>Every refusal has a test that writes the malformed Ruleset and watches it fire</b>, which is
/// the discipline the analyser suite and the invariant suite are both held to. A refusal nobody has
/// seen fire is indistinguishable from a Ruleset that is correct — and here the stakes are
/// <c>adr/0015</c>'s, where a refusal that does not name a file, a line and a rule is a refusal a
/// designer cannot act on.
/// </remarks>
public sealed class RulesetLoaderTests
{
    /// <summary>
    /// A chain that works: the head fails on flour, the pool rescues it, the terminal reports.
    /// </summary>
    /// <remarks>
    /// Deliberately <c>02 §4.3</c>'s own worked example, extended with the <c>[[resource]]</c> and
    /// <c>[[building]]</c> sections it elides. A loader that refuses the corpus's example is not a
    /// loader.
    /// </remarks>
    private const string Bakery = """
        [[resource]]
        name = "flour"
        family = "good"

        [[resource]]
        name = "bread"
        family = "good"

        [[building]]
        name = "bakery"
        bins = [
          { resource = "flour", capacity = 60 },
          { resource = "bread", capacity = 20 },
        ]

        [[rule]]
        name    = "bake_bread"
        kind    = "bakery"
        rate    = 10
        apply   = { min = 1, max = 4 }
        on_fail = "draw_flour_from_pool"
        inputs  = [ { scope = "local", resource = "flour", amount = 6 } ]
        outputs = [
          { scope = "local", resource = "bread",     amount = 1 },
          { scope = "map",   layer    = "pollution", amount = 2 },
        ]

        [[rule]]
        name    = "draw_flour_from_pool"
        kind    = "bakery"
        rate    = 10
        apply   = { min = 1, max = 1 }
        on_fail = "request_shipment"
        inputs  = [ { scope = "pool",  resource = "flour", amount = 6 } ]
        outputs = [ { scope = "local", resource = "flour", amount = 6 } ]

        [[rule]]
        name    = "request_shipment"
        kind    = "bakery"
        rate    = 10
        apply   = { min = 1, max = 1 }
        on_fail = "mark_input_starved"
        fills   = { scope = "local", resource = "flour" }
        inputs  = []
        outputs = []

        [[rule]]
        name    = "mark_input_starved"
        kind    = "bakery"
        rate    = 10
        apply   = { min = 1, max = 1 }
        reports = "input_starved"
        inputs  = []
        outputs = []
        """;

    private static Ruleset Accepted(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    private static RulesetRefusal Refused(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.False(result.Ok, "the Ruleset was accepted.");

        return result.Refusals[0];
    }

    [Fact]
    public void The_worked_example_of_the_corpus_loads()
    {
        Ruleset ruleset = Accepted(Bakery);

        Assert.Equal(2, ruleset.ResourceCount);
        Assert.Equal(1, ruleset.KindCount);
        Assert.Equal(4, ruleset.RuleCount);

        var bake = new RuleId(1);
        RuleDefinition rule = ruleset.Rule(bake);

        Assert.Equal(10u, rule.Rate);
        Assert.Equal(1, rule.Apply.Min);
        Assert.Equal(4, rule.Apply.Max);
        Assert.False(rule.Apply.IsDerived);
        Assert.Equal(new RuleId(2), rule.OnFail);

        // The map term went to the emissions rather than to the Bin outputs, which is what keeps a
        // Layer write out of the subscription question entirely.
        Assert.Equal(1, ruleset.Outputs(bake).Length);
        Assert.Equal(
            new MapEmission(Layer.IndustrialPollution, 2), ruleset.Emissions(bake)[0]);

        Assert.Equal(
            new Term(new BinRef(Scope.Local, new ResourceId(1)), 6), ruleset.Inputs(bake)[0]);
    }

    /// <summary>A kind's Bins and Rules are reachable from the kind, which is what a build needs.</summary>
    [Fact]
    public void A_building_kind_carries_its_bins_and_its_rules()
    {
        Ruleset ruleset = Accepted(Bakery);

        Assert.Equal(
            [new BinDeclaration(new ResourceId(1), BinCapacity.Of(60)), new BinDeclaration(new ResourceId(2), BinCapacity.Of(20))],
            ruleset.BinsOf(1).ToArray());

        // One, not four. The bakery declares a four-deep chain and only its head is armed: the other
        // three are links, reached by walking a chain that failed. Arming a link would run it
        // independently of the head it exists to rescue, and the terminal would report at its own
        // rate for ever -- adr/0045's polling defect, arriving through the Rule Instance table
        // instead of through the walk.
        Assert.Equal([new RuleId(1)], ruleset.RulesOf(1).ToArray());
    }

    /// <summary>The terminal is the discriminator the <c>fills</c> check exempts.</summary>
    [Fact]
    public void A_rule_that_reports_a_condition_is_a_terminal()
    {
        Ruleset ruleset = Accepted(Bakery);

        Assert.False(ruleset.Rule(new RuleId(1)).IsTerminal);
        Assert.True(ruleset.Rule(new RuleId(4)).IsTerminal);
    }

    // ---- refusal 1: the on_fail cycle ---------------------------------------------------------

    [Fact]
    public void A_cycle_in_the_on_fail_graph_is_refused()
    {
        RulesetRefusal refusal = Refused(Bakery.Replace(
            """
            name    = "mark_input_starved"
            kind    = "bakery"
            rate    = 10
            apply   = { min = 1, max = 1 }
            reports = "input_starved"
            """,
            """
            name    = "mark_input_starved"
            kind    = "bakery"
            rate    = 10
            apply   = { min = 1, max = 1 }
            on_fail = "bake_bread"
            """,
            StringComparison.Ordinal));

        Assert.Contains("cycle", refusal.Reason, StringComparison.Ordinal);
        Assert.Equal("test.toml", refusal.File);
        Assert.NotNull(refusal.Rule);
        Assert.True(refusal.Line > 0);
    }

    /// <summary>A Rule whose <c>on_fail</c> is itself is the shortest cycle there is.</summary>
    [Fact]
    public void A_rule_that_falls_back_to_itself_is_refused()
    {
        RulesetRefusal refusal = Refused("""
            [[resource]]
            name = "flour"
            family = "good"

            [[building]]
            name = "bakery"
            bins = [ { resource = "flour", capacity = 60 } ]

            [[rule]]
            name    = "loop"
            kind    = "bakery"
            rate    = 10
            apply   = { min = 1, max = 1 }
            on_fail = "loop"
            inputs  = [ { scope = "local", resource = "flour", amount = 1 } ]
            """);

        Assert.Contains("cycle", refusal.Reason, StringComparison.Ordinal);
        Assert.Equal("loop", refusal.Rule);
    }

    // ---- refusal 2: fills ----------------------------------------------------------------------

    /// <summary>
    /// An asynchronous link that does not declare what it fills rescues nothing detectable.
    /// </summary>
    /// <remarks>
    /// <c>request_shipment</c> dispatches a Shipment and outputs nothing this Tick. Strip its
    /// <c>fills</c> and it becomes indistinguishable from a link that does not rescue the head's Bin
    /// at all — which is precisely the case <c>02 §4.1</c> says the declaration exists for.
    /// </remarks>
    [Fact]
    public void A_link_whose_rescue_arrives_later_and_declares_nothing_is_refused()
    {
        RulesetRefusal refusal = Refused(Bakery.Replace(
            "fills   = { scope = \"local\", resource = \"flour\" }\n",
            string.Empty,
            StringComparison.Ordinal));

        Assert.Contains("relieved by every link", refusal.Reason, StringComparison.Ordinal);
        Assert.Equal("bake_bread", refusal.Rule);
    }

    /// <remarks>
    /// The last link of a chain is a reporting terminal: it names a condition and leaves the chain
    /// failed. Strip <c>reports</c> from the corpus's own terminal and the chain simply ends, which
    /// leaves the Building failed with nothing for a player to read — the silent non-event
    /// <c>02 §4.1</c> bans predicates for. Task 3 declined to add this refusal on the grounds that a
    /// loader would be making a design claim; the claim is <c>02 §4.1</c>'s and the loader applies
    /// it rather than inventing it.
    /// <para>
    /// The stripped link also fails the <c>fills</c> walk, since a non-terminal that relieves
    /// nothing empties the intersection. That refusal names the <em>head</em>, and the defect is in
    /// the tail — which is why the terminal check runs first.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_chain_that_ends_without_a_terminal_is_refused()
    {
        RulesetRefusal refusal = Refused(Bakery.Replace(
            "reports = \"input_starved\"\n",
            string.Empty,
            StringComparison.Ordinal));

        Assert.Contains("without recording anything", refusal.Reason, StringComparison.Ordinal);
        Assert.Equal("mark_input_starved", refusal.Rule);
    }

    /// <summary>A link that rescues a different Bin from the one the head fails on is refused.</summary>
    [Fact]
    public void A_chain_whose_links_relieve_different_bins_is_refused()
    {
        RulesetRefusal refusal = Refused("""
            [[resource]]
            name = "flour"
            family = "good"

            [[resource]]
            name = "sugar"
            family = "good"

            [[building]]
            name = "bakery"
            bins = [
              { resource = "flour", capacity = 60 },
              { resource = "sugar", capacity = 60 },
            ]

            [[rule]]
            name    = "bake"
            kind    = "bakery"
            rate    = 10
            apply   = { min = 1, max = 1 }
            on_fail = "fetch_sugar"
            inputs  = [ { scope = "local", resource = "flour", amount = 6 } ]

            [[rule]]
            name    = "fetch_sugar"
            kind    = "bakery"
            rate    = 10
            apply   = { min = 1, max = 1 }
            on_fail = "mark_starved"
            inputs  = [ { scope = "pool",  resource = "sugar", amount = 6 } ]
            outputs = [ { scope = "local", resource = "sugar", amount = 6 } ]

            [[rule]]
            name    = "mark_starved"
            kind    = "bakery"
            rate    = 10
            apply   = { min = 1, max = 1 }
            reports = "input_starved"
            inputs  = []
            outputs = []
            """);

        Assert.Contains("relieved by every link", refusal.Reason, StringComparison.Ordinal);
        Assert.Equal("bake", refusal.Rule);
    }

    // ---- refusal 3: the unquoted decimal --------------------------------------------------------

    [Fact]
    public void An_unquoted_decimal_is_refused_by_name()
    {
        RulesetRefusal refusal = Refused("""
            [[resource]]
            name = "flour"
            family = "good"

            [[building]]
            name = "bakery"
            decline_rate = 0.15
            bins = [ { resource = "flour", capacity = 60 } ]
            """);

        Assert.Contains("unquoted decimal", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("\"0.15\"", refusal.Reason, StringComparison.Ordinal);
        Assert.Equal(7, refusal.Line);
    }

    /// <summary>
    /// The refusal is lexical, so it reaches a decimal in a section this build does not interpret.
    /// </summary>
    /// <remarks>
    /// The hazard <c>adr/0048</c> names is not that <em>this</em> number reaches the simulation as a
    /// <c>double</c> — it is that the file format admits one at all. A check that only looked at keys
    /// it understood would let the next slice's keys in unguarded.
    /// </remarks>
    [Fact]
    public void An_unquoted_decimal_is_refused_even_where_nothing_reads_it()
    {
        RulesetRefusal refusal = Refused("""
            [[resource]]
            name = "flour"
            family = "good"

            [tuning]
            something_no_slice_reads_yet = 2.5
            """);

        Assert.Contains("unquoted decimal", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>The quoted form is what a designer writes instead, and it parses as a string.</summary>
    [Fact]
    public void A_quoted_decimal_is_not_refused()
    {
        RulesetLoadResult result = RulesetLoader.Parse("""
            [[resource]]
            name = "flour"
            family = "good"

            [[building]]
            name = "bakery"
            decline_rate = "0.15"
            bins = [ { resource = "flour", capacity = 60 } ]
            """, "test.toml");

        Assert.True(result.Ok, result.Describe());
    }

    // ---- the ordinary refusals ------------------------------------------------------------------

    [Fact]
    public void An_unknown_resource_name_is_refused()
    {
        RulesetRefusal refusal = Refused("""
            [[resource]]
            name = "flour"
            family = "good"

            [[building]]
            name = "bakery"
            bins = [ { resource = "yeast", capacity = 60 } ]
            """);

        Assert.Contains("'yeast' is not a declared", refusal.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void A_rule_declaring_both_a_band_and_a_derived_count_is_refused()
    {
        RulesetRefusal refusal = Refused("""
            [[resource]]
            name = "flour"
            family = "good"

            [[building]]
            name = "bakery"
            bins = [ { resource = "flour", capacity = 60 } ]

            [[rule]]
            name   = "bake"
            kind   = "bakery"
            rate   = 10
            apply  = { min = 1, max = 4, derived = "fertility" }
            inputs = [ { scope = "local", resource = "flour", amount = 6 } ]
            """);

        Assert.Contains("never both", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// No Readout is declared yet, so no derived apply count can name one.
    /// </summary>
    /// <remarks>
    /// <b>A correct refusal rather than a provisional one.</b> The readable set is declared in the
    /// simulation (<c>02 §4.1</c>) and slice 7 task 7 is where it is populated; until then the honest
    /// answer to <c>derived = "anything"</c> is that the set is empty.
    /// </remarks>
    /// <summary>
    /// An undeclared Readout is refused **by name**, and the refusal quotes the set that does exist.
    /// </summary>
    /// <remarks>
    /// <c>fertility</c> is one <c>CONTEXT</c> names as a Readout and the simulation does not declare,
    /// which is the case worth testing: the plausible name, not a typo.
    /// </remarks>
    [Fact]
    public void An_undeclared_readout_is_refused_and_the_refusal_names_the_declared_set()
    {
        RulesetRefusal refusal = Refused("""
            [[resource]]
            name = "produce"
            family = "good"

            [[building]]
            name = "farm"
            bins = [ { resource = "produce", capacity = 60 } ]

            [[rule]]
            name    = "grow"
            kind    = "farm"
            rate    = 64
            apply   = { derived = "fertility" }
            outputs = [ { scope = "local", resource = "produce", amount = 1 } ]
            """);

        Assert.Contains("'fertility' is not a declared Readout", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("occupancy", refusal.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void A_declared_readout_resolves_to_its_id_and_percent_defaults_to_one_per_unit()
    {
        Ruleset ruleset = Accepted("""
            [[resource]]
            name = "food"
            family = "good"

            [[building]]
            name = "dwelling"
            bins = [ { resource = "food", capacity = 60 } ]

            [[rule]]
            name   = "eat"
            kind   = "dwelling"
            rate   = 64
            apply  = { derived = "occupancy" }
            inputs = [ { scope = "local", resource = "food", amount = 1 } ]
            """);

        ApplyCount apply = ruleset.Rule(new RuleId(1)).Apply;

        Assert.True(apply.IsDerived);
        Assert.Equal((ushort)Readout.Occupancy, apply.Derived.Raw);
        Assert.Equal(100, apply.Percent);
    }

    /// <summary><c>02 §4.1</c>'s <em>"15% of gross income"</em>, in the spelling a designer writes.</summary>
    [Fact]
    public void A_derived_apply_count_carries_its_percentage()
    {
        Ruleset ruleset = Accepted("""
            [[resource]]
            name = "food"
            family = "good"

            [[building]]
            name = "dwelling"
            bins = [ { resource = "food", capacity = 60 } ]

            [[rule]]
            name   = "eat"
            kind   = "dwelling"
            rate   = 64
            apply  = { derived = "occupancy", percent = 15 }
            inputs = [ { scope = "local", resource = "food", amount = 1 } ]
            """);

        Assert.Equal(15, ruleset.Rule(new RuleId(1)).Apply.Percent);
    }

    // ---- refusal 4: money is conserved --------------------------------------------------------------

    /// <summary>
    /// <b><c>02 §4.3</c>'s own worked example, as written, destroys one money per baking.</b>
    /// </summary>
    /// <remarks>
    /// The corpus's bakery draws <c>{ scope = "local", resource = "money", amount = 1 }</c> and returns
    /// no money anywhere. `adr/0024` makes money conserved and the Outside Connection its only sink, so
    /// this is not a cost — it is a leak, and it sat in the document for six slices.
    /// </remarks>
    [Fact]
    public void A_rule_that_draws_money_and_returns_none_is_refused()
    {
        RulesetRefusal refusal = Refused("""
            [[resource]]
            name = "flour"
            family = "good"

            [[resource]]
            name = "money"
            family = "money"

            [[building]]
            name = "bakery"
            bins = [
              { resource = "flour", capacity = 60 },
              { resource = "money" },
            ]

            [[rule]]
            name    = "bake_bread"
            kind    = "bakery"
            rate    = 10
            apply   = { min = 1, max = 1 }
            inputs  = [
              { scope = "local", resource = "flour", amount = 6 },
              { scope = "local", resource = "money", amount = 1 },
            ]
            """);

        Assert.Contains("destroys 1 money", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("conserved", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>Creating money is the same defect with the sign flipped, and reads as inflation.</summary>
    [Fact]
    public void A_rule_that_returns_money_it_never_drew_is_refused()
    {
        RulesetRefusal refusal = Refused("""
            [[resource]]
            name = "money"
            family = "money"

            [[building]]
            name = "mint"
            bins = [ { resource = "money" } ]

            [[rule]]
            name    = "print"
            kind    = "mint"
            rate    = 10
            apply   = { min = 1, max = 1 }
            outputs = [ { scope = "local", resource = "money", amount = 5 } ]
            """);

        Assert.Contains("creates 5 money", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// A tax is the shape that <em>does</em> balance: money leaves one actor and arrives at another,
    /// both named, in one atomic Rule.
    /// </summary>
    [Fact]
    public void A_rule_moving_money_between_two_named_scopes_is_accepted()
    {
        Ruleset ruleset = Accepted("""
            [[resource]]
            name = "money"
            family = "money"

            [[building]]
            name = "house"
            bins = [ { resource = "money" } ]

            [[rule]]
            name    = "pay_tax"
            kind    = "house"
            rate    = 10
            apply   = { min = 1, max = 1 }
            inputs  = [ { scope = "local",  resource = "money", amount = 3 } ]
            outputs = [ { scope = "global", resource = "money", amount = 3 } ]
            """);

        Assert.Equal(1, ruleset.RuleCount);
        Assert.Equal(ResourceFamily.Money, ruleset.Family(new ResourceId(1)));
        Assert.True(ruleset.IsConserved(new ResourceId(1)));
    }

    // ---- the Resource family ------------------------------------------------------------------------

    [Fact]
    public void A_resource_without_a_family_is_refused()
    {
        RulesetRefusal refusal = Refused("""
            [[resource]]
            name = "flour"
            """);

        Assert.Contains("family", refusal.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void A_family_that_is_not_one_of_the_three_is_refused()
    {
        RulesetRefusal refusal = Refused("""
            [[resource]]
            name = "flour"
            family = "goods"
            """);

        Assert.Contains("'goods' is not a Resource family", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>A money Bin has no ceiling</b>, so authoring one is refused rather than ignored — a finite
    /// ceiling on money means an actor too full of money to be paid, and a sale failing on headroom
    /// because the seller is rich.
    /// </summary>
    [Fact]
    public void A_money_bin_declaring_a_capacity_is_refused_and_is_otherwise_unbounded()
    {
        RulesetRefusal refusal = Refused("""
            [[resource]]
            name = "money"
            family = "money"

            [[building]]
            name = "house"
            bins = [ { resource = "money", capacity = 1000 } ]
            """);

        Assert.Contains("money Bin declares no capacity", refusal.Reason, StringComparison.Ordinal);

        Ruleset ruleset = Accepted("""
            [[resource]]
            name = "money"
            family = "money"

            [[building]]
            name = "house"
            bins = [ { resource = "money" } ]
            """);

        Assert.True(ruleset.BinsOf(1)[0].Capacity.IsUnbounded);
    }

    /// <summary>
    /// <b>The second parameter is a named hole, and the test is that it is named.</b> Nothing reads
    /// <c>storage</c>, so accepting the key would hand a designer a Power Resource that warehouses
    /// electricity — the hole hidden inside a plausible number rather than reported.
    /// </summary>
    [Fact]
    public void The_storage_axis_is_refused_rather_than_silently_dropped()
    {
        RulesetRefusal refusal = Refused("""
            [[resource]]
            name = "power"
            family = "utility"
            storage = 0
            """);

        Assert.Contains("storage is not implemented", refusal.Reason, StringComparison.Ordinal);
        Assert.Equal(4, refusal.Line);
    }

    /// <summary><c>map</c> is write-only, so it cannot be an input and cannot be waited on.</summary>
    [Fact]
    public void A_map_term_used_as_an_input_is_refused()
    {
        RulesetRefusal refusal = Refused("""
            [[resource]]
            name = "flour"
            family = "good"

            [[building]]
            name = "bakery"
            bins = [ { resource = "flour", capacity = 60 } ]

            [[rule]]
            name   = "bake"
            kind   = "bakery"
            rate   = 10
            apply  = { min = 1, max = 1 }
            inputs = [ { scope = "map", layer = "pollution", amount = 1 } ]
            """);

        Assert.Contains("write-only", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>There is deliberately no proximity scope: movers choose, Rules transform.</summary>
    [Fact]
    public void A_scope_that_is_not_one_of_the_four_is_refused()
    {
        RulesetRefusal refusal = Refused("""
            [[resource]]
            name = "flour"
            family = "good"

            [[building]]
            name = "bakery"
            bins = [ { resource = "flour", capacity = 60 } ]

            [[rule]]
            name   = "bake"
            kind   = "bakery"
            rate   = 10
            apply  = { min = 1, max = 1 }
            inputs = [ { scope = "nearby", resource = "flour", amount = 6 } ]
            """);

        Assert.Contains("is not a scope", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("movers choose", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>A rate at or beyond the wheel's period would re-arm into the bucket it came off.</summary>
    [Fact]
    public void A_rate_that_would_wrap_the_event_wheel_is_refused()
    {
        RulesetRefusal refusal = Refused($$"""
            [[resource]]
            name = "flour"
            family = "good"

            [[building]]
            name = "bakery"
            bins = [ { resource = "flour", capacity = 60 } ]

            [[rule]]
            name   = "bake"
            kind   = "bakery"
            rate   = {{EventWheel.Size}}
            apply  = { min = 1, max = 1 }
            inputs = [ { scope = "local", resource = "flour", amount = 6 } ]
            """);

        Assert.Contains("WHEEL_SIZE", refusal.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Two_declarations_of_one_name_are_refused()
    {
        RulesetRefusal refusal = Refused("""
            [[resource]]
            name = "flour"
            family = "good"

            [[resource]]
            name = "flour"
            family = "good"
            """);

        Assert.Contains("a second [[resource]]", refusal.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Malformed_toml_is_refused_with_a_line()
    {
        RulesetRefusal refusal = Refused("[[resource]\nname = \"flour\"\n");

        Assert.Equal("test.toml", refusal.File);
        Assert.True(refusal.Line > 0);
    }

    // ---- the previous Ruleset stays live --------------------------------------------------------

    /// <summary>
    /// <c>adr/0015</c>'s error surface: a refused reload leaves the simulation on the Rules it had.
    /// </summary>
    /// <remarks>
    /// <b>The failure this prevents is a designer saving a broken file and the city stopping.</b>
    /// Nothing is applied during a load, so there is nothing to roll back — the swap happens once, at
    /// the end, and only when there is something to swap in.
    /// </remarks>
    [Fact]
    public void A_refused_reload_leaves_the_previous_ruleset_in_force()
    {
        var inForce = new RulesetInForce();

        Assert.Same(Ruleset.Empty, inForce.Current);

        Assert.Empty(inForce.TryReplace(RulesetLoader.Parse(Bakery, "good.toml")));

        Ruleset loaded = inForce.Current;
        Assert.Equal(4, loaded.RuleCount);

        IReadOnlyList<RulesetRefusal> refusals =
            inForce.TryReplace(RulesetLoader.Parse("[[resource]]\nname = 1.5\n", "bad.toml"));

        Assert.NotEmpty(refusals);
        Assert.Same(loaded, inForce.Current);
        Assert.Equal(1, inForce.Refusals);
    }

    /// <summary>Every refusal, not the first — otherwise a fix costs one run per mistake.</summary>
    [Fact]
    public void Every_refusal_is_collected_rather_than_the_first()
    {
        RulesetLoadResult result = RulesetLoader.Parse("""
            [[resource]]
            name = "flour"
            family = "good"

            [[building]]
            name = "bakery"
            bins = [
              { resource = "yeast", capacity = 60 },
              { resource = "sugar", capacity = 60 },
            ]
            """, "test.toml");

        Assert.False(result.Ok);
        Assert.Equal(2, result.Refusals.Count);
    }

    /// <summary>The refusal reads as a file, a line and a rule, which is what adr/0015 promises.</summary>
    [Fact]
    public void A_refusal_names_a_file_a_line_and_a_rule()
    {
        Assert.Equal(
            "bakery.toml:14: rule 'bake_bread': its on_fail chain is a cycle.",
            new RulesetRefusal("bakery.toml", 14, "bake_bread", "its on_fail chain is a cycle.")
                .ToString());
    }

    /// <summary><c>adr/0048</c>'s drift assertion: the interpreter refuses an id it does not know.</summary>
    [Fact]
    public void The_core_refuses_a_rule_id_it_does_not_know()
    {
        Ruleset ruleset = Accepted(Bakery);

        Assert.Throws<ArgumentOutOfRangeException>(() => ruleset.Rule(new RuleId(99)));
        Assert.Throws<ArgumentOutOfRangeException>(() => ruleset.Rule(RuleId.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => ruleset.Kind(9));
    }
}

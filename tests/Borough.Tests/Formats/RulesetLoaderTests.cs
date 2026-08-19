using System.Globalization;
using Borough.Core.Entities;
using Borough.Core.Quantities;
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
    /// ceiling on money means an actor too full of money to be paid, and a sale failing on space
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
    /// <b>One kind, one Resource, one Bin</b> — a duplicate pair is refused, because the second Bin is
    /// unreachable through <c>FindBin</c> and, since <c>adr/0064</c>, the pair is the key the capacity
    /// derivation looks a ceiling up by.
    /// </summary>
    /// <remarks>
    /// <b>Written two slices late, and that is the finding.</b> The refusal has existed since slice 7
    /// task 8 and was the one guard in this loader with no test, so <c>adr/0064</c> read the suite,
    /// found nothing, and recorded in its Consequences that the loader refused nothing of the sort —
    /// a live defect argued into existence from the shape of a missing test. Amended there.
    /// </remarks>
    [Fact]
    public void A_kind_declaring_two_bins_of_one_resource_is_refused()
    {
        RulesetRefusal refusal = Refused("""
            [[resource]]
            name = "flour"
            family = "good"

            [[building]]
            name = "bakery"
            bins = [
              { resource = "flour", capacity = 60 },
              { resource = "flour", capacity = 20 },
            ]
            """);

        Assert.Contains("two Bins for one Resource", refusal.Reason, StringComparison.Ordinal);
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

    // ---- Zone Rules, and refusals 6 to 10 -------------------------------------------------------

    private const string Zoned = """
        [[building]]
        name = "dwelling"
        bins = []

        [[zone_rule]]
        name          = "housing"
        kind          = "dwelling"
        zone          = 3
        interval      = 64
        revisit_ticks = 4096
        """;

    [Fact]
    public void A_zone_rule_loads_as_ids_and_integers()
    {
        Ruleset ruleset = Accepted(Zoned);

        ZoneRuleDefinition zone = Assert.Single(ruleset.ZoneRules.ToArray());

        Assert.Equal(1, zone.Kind);
        Assert.Equal(3, zone.Zone);
        Assert.Equal(64u, zone.Interval);
        Assert.Equal(4096, zone.RevisitTicks);

        // The bit is stored as an index and read as a set, because a Lot's Zone is a set.
        Assert.Equal(0b1000, zone.Admits);
    }

    /// <summary>
    /// The revisit period is optional and its default is one Day (<c>adr/0059</c>).
    /// </summary>
    /// <remarks>
    /// <b>The default is what lets this ship without an <c>adr/0052</c> ratifier</b>, so a test that
    /// only ever loaded files stating the key would be leaving the unratified path uncovered.
    /// </remarks>
    [Fact]
    public void A_zone_rule_stating_no_revisit_period_gets_a_day()
    {
        Ruleset ruleset = Accepted(Zoned.Replace(
            "\nrevisit_ticks = 4096", string.Empty, StringComparison.Ordinal));

        Assert.Equal(Ticks.PerDay, Assert.Single(ruleset.ZoneRules.ToArray()).RevisitTicks);
    }

    /// <summary>
    /// Refusal 6 — a Zone Rule naming a kind the Ruleset does not declare.
    /// </summary>
    /// <remarks>
    /// The failure mode is silence: such a Rule would sample Lots on schedule for ever and build
    /// nothing, which is indistinguishable from a city nobody wants to move to.
    /// </remarks>
    [Fact]
    public void A_zone_rule_naming_an_undeclared_kind_is_refused()
    {
        RulesetRefusal refusal = Refused(Zoned.Replace(
            @"kind          = ""dwelling""", @"kind          = ""tower""", StringComparison.Ordinal));

        Assert.Contains("tower", refusal.Reason, StringComparison.Ordinal);
        Assert.Equal("housing", refusal.Rule);
    }

    /// <summary>
    /// Refusal 7 — a permission bit wider than the Lot's permission set, which no verb can paint.
    /// </summary>
    /// <remarks>
    /// <b>Checked against <see cref="LotTable.ZoneBits"/> rather than a literal</b>, so that widening
    /// the column cannot leave the parser refusing bits that have become paintable.
    /// </remarks>
    [Theory]
    [InlineData(LotTable.ZoneBits)]
    [InlineData(LotTable.ZoneBits + 9)]
    [InlineData(-1)]
    public void A_zone_rule_naming_an_unpaintable_bit_is_refused(int bit)
    {
        RulesetRefusal refusal = Refused(Zoned.Replace(
            "zone          = 3",
            string.Create(CultureInfo.InvariantCulture, $"zone          = {bit}"),
            StringComparison.Ordinal));

        Assert.Contains("permission", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// Refusal 8 — a revisit period of zero is a division rather than a slow sweep.
    /// </summary>
    /// <remarks>
    /// <b>The <c>apply = {min=1,max=4}</c> behaving as <c>{1,1}</c> defect, arriving in the second
    /// family.</b> That one got through because a silent narrowing looks exactly like a quiet design
    /// decision; this refusal exists so the second instance cannot. It was written against
    /// <c>sample = 0</c> and <c>adr/0059</c> moved it one level up, to the number the sample is now
    /// derived from.
    /// </remarks>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData((long)int.MaxValue + 1)]
    public void A_zone_rule_whose_revisit_period_is_not_a_duration_is_refused(long revisit)
    {
        RulesetRefusal refusal = Refused(Zoned.Replace(
            "revisit_ticks = 4096",
            string.Create(CultureInfo.InvariantCulture, $"revisit_ticks = {revisit}"),
            StringComparison.Ordinal));

        Assert.Contains("not a duration", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// Refusal 9 — a revisit period shorter than the interval that would deliver it.
    /// </summary>
    /// <remarks>
    /// <b><c>pollution_decay_ticks</c>'s refusal against a different denominator.</b> A decay shorter
    /// than its cadence rounds to zero updates and reads as <em>never</em>; a revisit period shorter
    /// than its interval asks one trigger for more Lots than the city holds. Neither was found by a
    /// test failing — both are two numbers that are individually sane and jointly are not, which is
    /// the class of refusal a loader has to reason its way to.
    /// </remarks>
    [Fact]
    public void A_zone_rule_revisiting_faster_than_it_triggers_is_refused()
    {
        RulesetRefusal refusal = Refused(Zoned.Replace(
            "revisit_ticks = 4096", "revisit_ticks = 63", StringComparison.Ordinal));

        Assert.Contains("shorter than the interval", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// Refusal 10 — <c>sample</c> is refused by name rather than ignored (<c>adr/0059</c>).
    /// </summary>
    /// <remarks>
    /// <b>Silence is the failure being refused here.</b> Every Ruleset on disk carried a
    /// <c>sample</c> when the key was retired, and a designer who edits a number the loader no longer
    /// reads gets a city that does not change and no sentence saying why — which is the same failure
    /// class as refusals 6 to 8, reached from the direction of a document rather than of a file.
    /// </remarks>
    [Fact]
    public void A_zone_rule_still_carrying_a_sample_is_refused_by_name()
    {
        RulesetRefusal refusal = Refused(Zoned.Replace(
            "revisit_ticks = 4096", "sample        = 4", StringComparison.Ordinal));

        Assert.Contains("sample was replaced by revisit_ticks", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("adr/0059", refusal.Reason, StringComparison.Ordinal);
        Assert.Equal("housing", refusal.Rule);
    }

    /// <summary>An interval is bounded like a rate, and for the same Event Wheel reason.</summary>
    [Fact]
    public void A_zone_rule_triggering_beyond_the_wheel_is_refused()
    {
        RulesetRefusal refusal = Refused(Zoned.Replace(
            "interval      = 64",
            string.Create(CultureInfo.InvariantCulture, $"interval      = {EventWheel.Size}"),
            StringComparison.Ordinal));

        Assert.Contains("WHEEL_SIZE", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// An unknown section is named with the full list, which now has four entries.
    /// </summary>
    [Fact]
    public void An_unknown_section_lists_the_ones_that_exist()
    {
        RulesetRefusal refusal = Refused("""
            [[zoning_rule]]
            name = "typo"
            """);

        Assert.Contains("[[zone_rule]]", refusal.Reason, StringComparison.Ordinal);
    }

    // ---- occupancy, adr/0068 --------------------------------------------------------------------

    /// <summary>
    /// A kind that declares no <c>occupants</c> houses nobody, which is what almost every kind means.
    /// </summary>
    /// <remarks>
    /// <b>The default is the load-bearing half of this pair.</b> Every Ruleset written before
    /// occupancy existed omits the key, and all of them meant *this kind is not a home* — a bakery,
    /// a factory, a farm. Making the absent case zero is what let <c>adr/0068</c> ship without
    /// touching a single existing declaration.
    /// </remarks>
    [Fact]
    public void A_kind_that_declares_no_occupants_houses_nobody()
    {
        Ruleset ruleset = Accepted(Bakery);

        Assert.Equal(0, ruleset.Kind(1).Occupants);
    }

    /// <summary>A kind that declares <c>occupants</c> carries the number through to the core.</summary>
    [Fact]
    public void A_kind_that_declares_occupants_carries_the_number()
    {
        Ruleset ruleset = Accepted(Bakery.Replace(
            "name = \"bakery\"\n",
            "name = \"bakery\"\noccupants = 5\n",
            StringComparison.Ordinal));

        Assert.Equal(5, ruleset.Kind(1).Occupants);
    }

    /// <summary>
    /// A negative <c>occupants</c> is refused rather than clamped.
    /// </summary>
    /// <remarks>
    /// <b>Written because the last thing this loader gained was a guard with no test</b>
    /// (<c>plans/0018</c> → tasks 3 and 4's implementation record, finding 1): the duplicate
    /// <c>(kind, Resource)</c> refusal had existed since slice 7 and <c>adr/0064</c> recorded it as
    /// absent, because this file is where a reader looks to find out what the loader refuses and it
    /// did not say so. The refusal being obvious is not a reason to skip it — that was exactly the
    /// reasoning that produced the hole.
    /// <para>
    /// Clamping to zero would be worse than the negative: it reads as *evict everybody*, and a
    /// Ruleset that emptied every Building it declared is a sentence somebody meant to write and
    /// nobody would guess from the symptom.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_negative_occupants_is_refused()
    {
        RulesetRefusal refusal = Refused(Bakery.Replace(
            "name = \"bakery\"\n",
            "name = \"bakery\"\noccupants = -1\n",
            StringComparison.Ordinal));

        Assert.Contains("occupants is -1", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("houses nobody", refusal.Reason, StringComparison.Ordinal);
    }

    // ---- employment, adr/0068's rule on a second axis (5b-bis task 2) ---------------------------

    /// <summary>
    /// A kind that declares no <c>jobs</c> employs nobody.
    /// </summary>
    /// <remarks>
    /// <b>The default is load-bearing here in a way it was not for occupancy</b>, because it is the
    /// state of every kind in every shipped Ruleset rather than of most of them. It is also what
    /// deleted <c>SyntheticCity</c>'s workplace stride: the populator handed out employment that no
    /// declaration granted, which was expressible only once this key existed to contradict it.
    /// </remarks>
    [Fact]
    public void A_kind_that_declares_no_jobs_employs_nobody()
    {
        Ruleset ruleset = Accepted(Bakery);

        Assert.Equal(0, ruleset.Kind(1).Jobs);
    }

    /// <summary>A kind that declares <c>jobs</c> carries the number through to the core.</summary>
    [Fact]
    public void A_kind_that_declares_jobs_carries_the_number()
    {
        Ruleset ruleset = Accepted(Bakery.Replace(
            "name = \"bakery\"\n",
            "name = \"bakery\"\njobs = 12\n"
            + "shift_start_earliest_hour = 6\nshift_start_latest_hour = 10\n",
            StringComparison.Ordinal));

        Assert.Equal(12, ruleset.Kind(1).Jobs);
    }

    /// <summary>
    /// A negative <c>jobs</c> is refused rather than clamped.
    /// </summary>
    /// <remarks>
    /// Clamped to zero it reads as <em>sack everybody</em>, which is a sentence somebody meant to
    /// write and nobody would guess from the symptom — <c>occupants</c>'s reasoning exactly, and
    /// written rather than inherited because a guard with no test is invisible to the next reader.
    /// </remarks>
    [Fact]
    public void A_negative_jobs_is_refused()
    {
        RulesetRefusal refusal = Refused(Bakery.Replace(
            "name = \"bakery\"\n",
            "name = \"bakery\"\njobs = -1\n",
            StringComparison.Ordinal));

        Assert.Contains("jobs is -1", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("employs nobody", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// A kind declares occupancy and employment independently, and one does not default from
    /// the other.
    /// </summary>
    /// <remarks>
    /// <b>The case a single shared number could not express</b>, and the one that says why this is a
    /// fourth key rather than a reading of the third: a workplace houses nobody and employs a
    /// hundred, a dwelling the reverse, and a mixed-use Building both. Reaching for occupancy where
    /// employment was meant is the kind of collapse a test notices and a reviewer does not.
    /// </remarks>
    [Fact]
    public void Occupancy_and_employment_are_declared_independently()
    {
        Ruleset ruleset = Accepted(Bakery.Replace(
            "name = \"bakery\"\n",
            "name = \"bakery\"\noccupants = 0\njobs = 9\n"
            + "shift_start_earliest_hour = 6\nshift_start_latest_hour = 10\n",
            StringComparison.Ordinal));

        Assert.Equal(0, ruleset.Kind(1).Occupants);
        Assert.Equal(9, ruleset.Kind(1).Jobs);
    }
    // ---- [[building]] parking (adr/0120) ---------------------------------------------------------

    /// <summary>A kind that declares no <c>parking</c> parks nothing.</summary>
    /// <remarks>
    /// <b>Absence means what zero means here, and that is the decision rather than a default.</b>
    /// <c>occupants</c> and <c>jobs</c> both have to keep <em>declared zero</em> and <em>not declared
    /// at all</em> apart, because a kind the Ruleset dropped is <em>derelict</em> and must not be
    /// treated as one declaring none. That distinction lives in <c>World.TryDeclaredParking</c>, on
    /// whether the <em>kind</em> is declared — not on this key, which a kind either states or does
    /// not. A kind saying nothing about parking provides none.
    /// </remarks>
    [Fact]
    public void A_kind_that_declares_no_parking_parks_nothing()
    {
        Ruleset ruleset = Accepted(Bakery);

        Assert.Equal(0, ruleset.Kind(1).Parking);
    }

    /// <summary>A kind that declares <c>parking</c> carries the number through to the core.</summary>
    /// <remarks>
    /// <b>It counts Vehicles, never Citizens and never Households</b> (<c>adr/0119</c>):
    /// <c>World.ModeOf</c> drives every member of a car-owning Household, so the three quantities
    /// differ by construction and a Car Park sized in people would be sized in the wrong currency.
    /// </remarks>
    [Fact]
    public void A_kind_that_declares_parking_carries_the_number()
    {
        Ruleset ruleset = Accepted(Bakery.Replace(
            "name = \"bakery\"\n",
            "name = \"bakery\"\nparking = 24\n",
            StringComparison.Ordinal));

        Assert.Equal(24, ruleset.Kind(1).Parking);
    }

    /// <summary>
    /// A negative <c>parking</c> is refused rather than clamped.
    /// </summary>
    /// <remarks>
    /// <c>jobs</c>' reasoning exactly: clamped to zero it reads as <em>remove spaces that are not
    /// there</em>, which is not a sentence anybody meant to write, and the symptom — a District whose
    /// cars have nowhere to go — names neither the file nor the key. Written rather than inherited,
    /// because a guard with no test is invisible to the next reader (<c>adr/0064</c>).
    /// </remarks>
    [Fact]
    public void A_negative_parking_is_refused()
    {
        RulesetRefusal refusal = Refused(Bakery.Replace(
            "name = \"bakery\"\n",
            "name = \"bakery\"\nparking = -1\n",
            StringComparison.Ordinal));

        Assert.Contains("parking is -1", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("parks none", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Zero parking is a real declaration, and it is the value this key exists to make
    /// authorable.</b>
    /// </summary>
    /// <remarks>
    /// <b>The one of the three ceilings where zero is the interesting value.</b> A tower with no
    /// parking is <c>adr/0009</c>'s own second player-tool row — <em>a detached house carries a
    /// driveway, a tower may not</em> — where a dwelling employing nobody is merely the common case.
    /// So the three are declared independently and none defaults from another.
    /// </remarks>
    [Fact]
    public void Occupancy_employment_and_parking_are_declared_independently()
    {
        Ruleset ruleset = Accepted(Bakery.Replace(
            "name = \"bakery\"\n",
            "name = \"bakery\"\noccupants = 40\njobs = 9\nparking = 0\n"
            + "shift_start_earliest_hour = 6\nshift_start_latest_hour = 10\n",
            StringComparison.Ordinal));

        Assert.Equal(40, ruleset.Kind(1).Occupants);
        Assert.Equal(9, ruleset.Kind(1).Jobs);
        Assert.Equal(0, ruleset.Kind(1).Parking);
    }

    // ---- [placement] (adr/0069) --------------------------------------------------------------------

    /// <summary>A Ruleset with no <c>[placement]</c> loads, and houses nobody.</summary>
    /// <remarks>
    /// <b>The absence is the statement, and it is the opposite of <c>[layers]</c>'s.</b> A defaulted
    /// placement would put three hash-bearing numbers into the binary with nobody having authored them
    /// (<c>adr/0052</c>), and a city housing people at a cadence its designer never wrote is a quiet
    /// failure. A city housing nobody is a loud one.
    /// </remarks>
    [Fact]
    public void A_ruleset_with_no_placement_table_does_not_run_the_pass()
    {
        Assert.False(Accepted(Bakery).Placement.Runs);
    }

    /// <summary>The three numbers reach the Ruleset.</summary>
    [Fact]
    public void A_placement_table_carries_its_three_numbers()
    {
        PlacementRuleset placement = Accepted(
            Bakery + "\n[placement]\ninterval = 32\nrevisit_ticks = 1024\ncandidates = 3\n").Placement;

        Assert.True(placement.Runs);
        Assert.Equal(32u, placement.Interval);
        Assert.Equal(1024, placement.RevisitTicks);
        Assert.Equal(3, placement.Candidates);

        // And the sample the pair derives, which is the number the engine actually uses.
        Assert.Equal(4, placement.SampleFor(128));
    }

    /// <summary>
    /// A revisit period shorter than the interval it is delivered in is refused.
    /// </summary>
    /// <remarks>
    /// <c>adr/0059</c>'s refusal 9 against a third denominator: one trigger would be asked to consider
    /// more seekers than are waiting. Individually sane numbers, jointly not.
    /// </remarks>
    [Fact]
    public void A_placement_revisit_shorter_than_its_interval_is_refused()
    {
        RulesetRefusal refusal = Refused(
            Bakery + "\n[placement]\ninterval = 32\nrevisit_ticks = 16\ncandidates = 3\n");

        Assert.Contains("shorter than the interval", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>A seeker that looks at nothing never moves, so zero candidates is refused.</summary>
    [Fact]
    public void A_placement_with_no_candidates_is_refused()
    {
        RulesetRefusal refusal = Refused(
            Bakery + "\n[placement]\ninterval = 32\nrevisit_ticks = 1024\ncandidates = 0\n");

        Assert.Contains("candidates = 0", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("never moves", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// A second <c>[placement]</c> is refused, on <c>[layers]</c>'s reasoning.
    /// </summary>
    [Fact]
    public void A_second_placement_table_is_refused()
    {
        RulesetRefusal refusal = Refused(
            Bakery
            + "\n[[placement]]\ninterval = 32\nrevisit_ticks = 1024\ncandidates = 3\n"
            + "\n[[placement]]\ninterval = 64\nrevisit_ticks = 1024\ncandidates = 3\n");

        Assert.Contains("second [placement]", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// A <c>[placement]</c> missing a key it has no way to derive is refused.
    /// </summary>
    /// <remarks>
    /// <b>Present-and-incomplete is a different statement from absent</b>, which is why the table is
    /// optional and its contents are not: an author who wrote the section has said the pass runs.
    /// </remarks>
    [Fact]
    public void A_placement_missing_candidates_is_refused()
    {
        Refused(Bakery + "\n[placement]\ninterval = 32\nrevisit_ticks = 1024\n");
    }
}

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

    private static RulesetRefusal Refused(string toml) => AllRefusals(toml)[0];

    /// <summary>
    /// Every refusal, for the tests whose claim is about <em>how many</em> a file produces.
    /// </summary>
    /// <remarks>
    /// <b>One mistake should read as one sentence.</b> A file that trips two checks for one error
    /// sends its author to the second one's line, which is not where the edit goes.
    /// </remarks>
    private static RulesetRefusal[] AllRefusals(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.False(result.Ok, "the Ruleset was accepted.");

        return [.. result.Refusals];
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

    /// <summary>
    /// A quoted decimal is not refused <em>as a decimal</em>, and the only thing wrong with the line
    /// is the key it sits on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This test used to assert the file LOADED, and the reason it could is the defect
    /// <c>plans/0041</c> G31 names.</b> <c>decline_rate</c> is not a key of <c>[[building]]</c> and
    /// never was — nothing reads it, so the loader passed over it in silence and the file was clean.
    /// ⚠ <b>The refusal message for an unquoted decimal ADVERTISED that key</b>, so a designer
    /// following the advice in front of them wrote a line that did nothing. Both are fixed here.
    /// </para>
    /// <para>
    /// 🔴 <b>Exactly one refusal is the assertion, and which one it is carries the point.</b> The
    /// lexical pass runs over the whole document before anything is interpreted, so if a quoted
    /// decimal were being coerced this would come back with two.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_quoted_decimal_is_refused_for_its_key_and_never_for_being_a_decimal()
    {
        RulesetRefusal refusal = Refused("""
            [[resource]]
            name = "flour"
            family = "good"

            [[building]]
            name = "bakery"
            decline_rate = "0.15"
            bins = [ { resource = "flour", capacity = 60 } ]
            """);

        Assert.Contains("'decline_rate' is not a key of [[building]]", refusal.Reason, StringComparison.Ordinal);
        Assert.DoesNotContain("decimal", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// A quoted decimal on a key that IS read loads, which is the original claim, and the quoted form
    /// is what a designer writes instead of a bare decimal.
    /// </summary>
    [Fact]
    public void A_quoted_decimal_is_not_refused()
    {
        RulesetLoadResult result = RulesetLoader.Parse("""
            [[resource]]
            name = "0.15"
            family = "good"

            [[building]]
            name = "bakery"
            bins = [ { resource = "0.15", capacity = 60 } ]
            """, "test.toml");

        Assert.True(result.Ok, result.Describe());
    }


    // ---- the unknown-key refusals ---------------------------------------------------------------

    /// <summary>
    /// A key nothing reads is refused, which is <c>plans/0041</c> G31 and the one class of authoring
    /// mistake this loader could not see.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>It found four keys in all eighteen shipped Rulesets on the day it landed</b>, stranded
    /// above the <c>[layers]</c> header since milestone 9 task 3. ⚠ <b>Every one authored exactly the
    /// loader's default</b>, so no run, no golden trace and no balance run was ever in a position to
    /// notice — ***the city was right and the file was saying nothing to it***.
    /// </para>
    /// <para>
    /// <b>The permitted set is DERIVED and not declared.</b> <c>Find</c> records every key it is
    /// asked for; what nothing asked for is refused. So this test does not need updating when a
    /// reader gains a key, which is the property a hand-authored list would not have had.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_key_no_reader_asks_for_is_refused()
    {
        RulesetRefusal refusal = Refused("""
            [[resource]]
            name = "flour"
            family = "good"

            [[building]]
            name = "bakery"
            occupent = 3
            bins = [ { resource = "flour", capacity = 60 } ]
            """);

        Assert.Contains("'occupent' is not a key of [[building]]", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>The near miss is named, because a typo is the case this refusal exists for.</summary>
    /// <remarks>
    /// ⚠ <b>The fixture is a <c>[[building]]</c> and not a <c>[placement]</c> on purpose.</b> A typo in
    /// <c>[placement]</c> trips that table's missing-key pair FIRST — <c>interval</c> without
    /// <c>revisit_ticks</c> — and reports the better message of the two. ***The first draft of this
    /// test asserted against a refusal the loader was right not to give.***
    /// </remarks>
    [Fact]
    public void A_typo_one_letter_from_a_real_key_is_named_in_the_refusal()
    {
        RulesetRefusal refusal = Refused("""
            [[resource]]
            name = "flour"
            family = "good"

            [[building]]
            name = "bakery"
            occupent = 3
            bins = [ { resource = "flour", capacity = 60 } ]
            """);

        Assert.Contains("Did you mean 'occupants'?", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// A key above the first section header belongs to no table, so no reader could have asked for it.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>This is a SECOND refusal site and not the same one</b>: the key is not merely unread, it
    /// is unreachable, and "not a key of [x]" would have to name a table that does not exist.
    /// </remarks>
    [Fact]
    public void A_key_above_the_first_section_header_is_refused()
    {
        RulesetRefusal refusal = Refused("""
            interval = 32

            [[resource]]
            name = "flour"
            family = "good"

            [[building]]
            name = "bakery"
            bins = [ { resource = "flour", capacity = 60 } ]
            """);

        Assert.Contains("sits above the first section header", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// The stranding that was live in all eighteen shipped Rulesets, written as a fixture.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>This is the exact shape of the defect and it reads as harmless.</b> A <c>[layers]</c> key
    /// written one line too early is inside <c>[placement]</c>, and the file loads, runs and produces
    /// the right city — because the value it states is the value the loader defaults to. ***What the
    /// designer loses is not correctness, it is the ability to change anything.***
    /// </remarks>
    [Fact]
    public void A_layers_key_written_above_the_layers_header_is_refused()
    {
        RulesetRefusal refusal = Refused("""
            [[resource]]
            name = "flour"
            family = "good"

            [[building]]
            name = "bakery"
            bins = [ { resource = "flour", capacity = 60 } ]

            [placement]
            interval = 32
            revisit_ticks = 1024
            candidates = 3
            noise_intensity_percent = 400

            [layers]
            pollution_period = 64
            """);

        Assert.Contains("'noise_intensity_percent' is not a key of [placement]", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// ⚠ <b>The permitted set is unioned across every table of one section shape</b>, because a reader
    /// may ask conditionally — so a key one <c>[[building]]</c> uses is permitted on all of them.
    /// </summary>
    /// <remarks>
    /// <b>This is looser than a hand-authored list and is the direction to be loose in.</b> A false
    /// refusal costs a designer a working file; a missed key costs them a silent number. The second is
    /// the defect this whole check exists for.
    /// </remarks>
    [Fact]
    public void A_key_one_table_of_a_shape_uses_is_permitted_on_all_of_them()
    {
        RulesetLoadResult result = RulesetLoader.Parse("""
            [[resource]]
            name = "flour"
            family = "good"

            [[building]]
            name = "bakery"
            houses = true
            premises = true
            bins = [ { resource = "flour", capacity = 60 } ]

            [[building]]
            name = "shed"
            bins = [ { resource = "flour", capacity = 10 } ]
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

    // ---- the global scope names the treasury --------------------------------------------------------

    /// <summary>
    /// <c>global</c> on a Good is refused, because the treasury holds conserved Resources only.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Milestone 10, and it is <c>02 §4.3</c> acquiring an implementation rather than a new
    /// rule</b> — that section already said <em>"that is the shape the loader accepts"</em>. The
    /// loader accepted every other shape as well for six slices, and nothing noticed, because
    /// <c>Scope.Global</c> threw in the Rule engine before a term could reach a running world.
    /// <b>A scope that throws is a scope nothing has to validate.</b>
    /// </para>
    /// <para>
    /// <b>The term balances, which is the point of testing it here.</b> Refusal 4 sums a Rule's
    /// money terms and this Rule has none, so no arithmetic check could ever have caught it: the
    /// defect is in what the scope <em>names</em>, not in what the amounts add up to.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_global_term_naming_a_good_is_refused()
    {
        RulesetRefusal refusal = Refused("""
            [[resource]]
            name = "flour"
            family = "good"

            [[building]]
            name = "silo"
            bins = [ { resource = "flour", capacity = 60 } ]

            [[rule]]
            name    = "stockpile"
            kind    = "silo"
            rate    = 10
            apply   = { min = 1, max = 1 }
            inputs  = [ { scope = "local",  resource = "flour", amount = 3 } ]
            outputs = [ { scope = "global", resource = "flour", amount = 3 } ]
            """);

        Assert.Contains("treasury", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("'flour' is declared as a good", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// <c>pool</c> on a Good is <em>not</em> refused beside it, and the difference is the whole
    /// argument.
    /// </summary>
    /// <remarks>
    /// <b><c>pool</c> is unbuilt and <c>global</c>-on-a-Good is not a mechanism at all</b>
    /// (<c>adr/0070</c>). The District Pool arrives at milestone 12 and every Good in the design
    /// crosses it, so refusing this file would refuse one that is going to be legal — the Rule
    /// engine's named hole is the right instrument for an absence with a date on it. Asserted
    /// rather than left implicit, because the two sit one line apart in <c>TryScope</c> and the
    /// next person to widen one will read this.
    /// </remarks>
    [Fact]
    public void A_pool_term_naming_a_good_is_accepted_because_that_scope_is_unbuilt_rather_than_wrong()
    {
        Ruleset ruleset = Accepted("""
            [[resource]]
            name = "flour"
            family = "good"

            [[building]]
            name = "silo"
            bins = [ { resource = "flour", capacity = 60 } ]

            [[rule]]
            name    = "deliver"
            kind    = "silo"
            rate    = 10
            apply   = { min = 1, max = 1 }
            inputs  = [ { scope = "pool", resource = "flour", amount = 3 } ]
            outputs = [ { scope = "local", resource = "flour", amount = 3 } ]
            """);

        Assert.Equal(1, ruleset.RuleCount);
    }

    /// <summary>
    /// A Resource whose family is itself refused is not reported twice.
    /// </summary>
    /// <remarks>
    /// <b>The second sentence would point at the Rule</b>, and the line that has to change is the
    /// <c>[[resource]]</c> declaration. One mistake, one refusal, at the line a designer edits.
    /// </remarks>
    [Fact]
    public void A_global_term_on_a_resource_with_no_valid_family_is_refused_once()
    {
        RulesetRefusal[] refusals = AllRefusals("""
            [[resource]]
            name = "flour"
            family = "goods"

            [[building]]
            name = "silo"
            bins = [ { resource = "flour", capacity = 60 } ]

            [[rule]]
            name    = "stockpile"
            kind    = "silo"
            rate    = 10
            apply   = { min = 1, max = 1 }
            inputs  = [ { scope = "local",  resource = "flour", amount = 3 } ]
            outputs = [ { scope = "global", resource = "flour", amount = 3 } ]
            """);

        Assert.Single(refusals);
        Assert.Contains("is not a Resource family", refusals[0].Reason, StringComparison.Ordinal);
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

    /// <summary>
    /// A Layer that exists and cannot be emitted into is refused at the parse site, not on the first
    /// application.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Queue item 12.</b> <c>TryLayer</c> resolves all three Layer names and nothing downstream
    /// asked whether the one named could be emitted into, so a Ruleset naming <c>land-value</c> loaded
    /// clean and <c>RuleEngine.Emit</c> threw the first time the Rule fired. ***A refusal in the engine
    /// is a refusal the designer meets as a crash***, and <c>adr/0048</c> puts validation where the
    /// Ruleset is parsed.
    /// </para>
    /// <para>
    /// <b>It is a different sentence from the unknown-name refusal, because it is a different
    /// mistake.</b> <c>land-value</c> is not a typo for a Layer — it is a Layer, and the reason it
    /// cannot appear here is that it is chased towards a target rather than accumulated from a source.
    /// Telling the author it "is not a Map Layer" would be false.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData("land-value")]
    [InlineData("sealing")]
    public void A_layer_that_cannot_be_emitted_into_is_refused_at_load(string layer)
    {
        RulesetRefusal refusal = Refused("""
            [[resource]]
            name = "flour"
            family = "good"

            [[building]]
            name = "bakery"
            bins = [ { resource = "flour", capacity = 60 } ]

            [[rule]]
            name    = "bake"
            kind    = "bakery"
            rate    = 10
            apply   = { min = 1, max = 1 }
            outputs = [ { scope = "map", layer = "LAYER_NAME", amount = 1 } ]
            """.Replace("LAYER_NAME", layer, StringComparison.Ordinal));

        Assert.Contains("cannot emit into", refusal.Reason, StringComparison.Ordinal);

        // The distinction the two messages carry: this one is a Layer, and an unknown name is not.
        Assert.DoesNotContain("is not a Map Layer", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>Pollution is the one a Rule may emit into, and it still loads.</summary>
    [Fact]
    public void The_one_emittable_layer_is_still_accepted()
    {
        Ruleset ruleset = Accepted("""
            [[resource]]
            name = "flour"
            family = "good"

            [[building]]
            name = "bakery"
            bins = [ { resource = "flour", capacity = 60 } ]

            [[rule]]
            name    = "bake"
            kind    = "bakery"
            rate    = 10
            apply   = { min = 1, max = 1 }
            outputs = [ { scope = "map", layer = "pollution", amount = 1 } ]
            """);

        Assert.Equal(Layer.IndustrialPollution, ruleset.Emissions(new RuleId(1))[0].Layer);
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
    /// <remarks>
    /// ⚠ <b>The period this names is the COARSE wheel's as of <c>plans/0046</c> stage 0, and the
    /// number moved from 2,048 to 260,096.</b> The claim is unchanged and the refusal is still a wrap
    /// — what changed is that a rate between a Day and 127 Days now has a tier to live on. That
    /// mattered: <c>rulesets/provisioned.toml</c>'s <c>rates</c> levy carries a header saying
    /// <i>rate = 1024 IS HALF A DAY BECAUSE A DAY IS NOT EXPRESSIBLE</i>, and <c>WageEngine</c>
    /// implements a weekly payday as a modulo over a daily sweep for the same reason. ***Two shipped
    /// mechanisms had already worked around this line***, which is what <c>plans/0036</c> decision 2
    /// asked for evidence of.
    /// </remarks>
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
            rate   = {{EventWheel.CoarseCeilingTicks}}
            apply  = { min = 1, max = 1 }
            inputs = [ { scope = "local", resource = "flour", amount = 6 } ]
            """);

        Assert.Contains("coarse wheel's period", refusal.Reason, StringComparison.Ordinal);
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

    /// <summary>
    /// ⚠ <b>A Sweep Rule may trigger at any period, and the bound that said otherwise was about a
    /// mechanism no Sweep Rule uses.</b>
    /// </summary>
    /// <remarks>
    /// <b>This test read <c>A_zone_rule_triggering_beyond_the_wheel_is_refused</c> until milestone 10
    /// task 5, and it asserted a refusal with no ground.</b> The loader bounded <c>interval</c> below
    /// <c>WHEEL_SIZE</c> on the reason that one at or beyond it <em>"would re-arm into the bucket it
    /// just came off"</em> — true of a Bin Rule's <c>rate</c>, and false of every caller of the
    /// check: a Zone Rule, the placement pass, the job pass and a Policy all test
    /// <c>tick % interval</c> and none of them touches the Event Wheel. It was found by the first
    /// interval that wanted to be a <b>Day</b>, which is <c>Ticks.PerDay</c> and therefore exactly
    /// <c>WHEEL_SIZE</c>. ***A bound is inherited with a word, not with a mechanism.***
    /// <para>
    /// Kept as the positive case rather than deleted, because a relaxation with no test is a refusal
    /// that can come back by accident.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_zone_rule_may_trigger_once_a_day()
    {
        Ruleset ruleset = Accepted(Zoned.Replace(
            "interval      = 64",
            string.Create(CultureInfo.InvariantCulture, $"interval      = {Ticks.PerDay}"),
            StringComparison.Ordinal));

        Assert.Equal((uint)Ticks.PerDay, ruleset.ZoneRules[0].Interval);
    }

    /// <summary>An interval that does not fit the field it is stored in is refused.</summary>
    /// <remarks>
    /// <b>The representation is the only true ceiling left</b>, and it is a real one: the interval is
    /// stored as a <c>uint</c>, so a larger figure would be cast rather than kept. Everything between
    /// 1 and that is a well-defined period, and one longer than the run fires once.
    /// </remarks>
    [Fact]
    public void An_interval_past_the_field_it_is_stored_in_is_refused()
    {
        RulesetRefusal refusal = Refused(Zoned.Replace(
            "interval      = 64",
            string.Create(CultureInfo.InvariantCulture, $"interval      = {(long)uint.MaxValue + 1}"),
            StringComparison.Ordinal));

        Assert.Contains("does not fit the field", refusal.Reason, StringComparison.Ordinal);
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

    // ---- tenancy, adr/0068 as plans/0053 left it -------------------------------------------------

    /// <summary>
    /// A kind that says nothing about tenancy admits nobody of either sort.
    /// </summary>
    /// <remarks>
    /// <b>The default is the load-bearing half of this pair</b>, and it has survived the key
    /// changing shape twice: every Ruleset written before occupancy existed omitted
    /// <c>occupants</c> and meant *this kind is not a home*. Both predicates inherit that, and
    /// inherit it more cleanly, because an absent predicate is simply <em>no</em> where an absent
    /// count had to be argued into meaning zero.
    /// </remarks>
    [Fact]
    public void A_kind_that_says_nothing_about_tenancy_admits_nobody()
    {
        Ruleset ruleset = Accepted(Bakery);

        Assert.False(ruleset.Kind(1).Houses);
        Assert.False(ruleset.Kind(1).Premises);
    }

    /// <summary><c>houses</c> admits a Household and does NOT admit a trade.</summary>
    /// <remarks>
    /// 🔴 <b>The independence is the whole of <c>plans/0054</c> F1.</b> One boolean meant tenants of
    /// any kind, so a kind wanting a trade had to declare itself housing — and then Households were
    /// placed into the office. ***Asserting that one key does not grant the other is asserting that
    /// an office is now writable***, which is the thing that was not.
    /// </remarks>
    [Fact]
    public void Houses_admits_a_household_and_not_a_trade()
    {
        Ruleset ruleset = Accepted(Bakery.Replace(
            "name = \"bakery\"\n",
            "name = \"bakery\"\nhouses = true\n",
            StringComparison.Ordinal));

        Assert.True(ruleset.Kind(1).Houses);
        Assert.False(ruleset.Kind(1).Premises);
    }

    /// <summary><c>premises</c> admits a trade and does NOT admit a Household.</summary>
    /// <remarks>
    /// <b>The office, and the reason this work happened.</b> A warehouse, a depot and an office are
    /// all this row, and none of them was expressible before — <c>tenanted = true</c> was the only
    /// way to hold a Business and it housed families as a side effect.
    /// </remarks>
    [Fact]
    public void Premises_admits_a_trade_and_not_a_household()
    {
        Ruleset ruleset = Accepted(Bakery.Replace(
            "name = \"bakery\"\n",
            "name = \"bakery\"\npremises = true\n",
            StringComparison.Ordinal));

        Assert.False(ruleset.Kind(1).Houses);
        Assert.True(ruleset.Kind(1).Premises);
    }

    /// <summary>A stated <c>false</c> is the same city as saying nothing, for both keys.</summary>
    /// <remarks>
    /// <b>Which is what makes writing the word optional rather than ceremonial.</b> The count key
    /// these replaced could not have this property — <c>occupants = 0</c> and no <c>occupants</c>
    /// had to stay apart, because a kind the Ruleset dropped is <em>derelict</em> and must not read
    /// as one declaring none. A predicate carries no such second meaning.
    /// </remarks>
    [Fact]
    public void A_stated_false_tenancy_is_the_same_as_none()
    {
        Ruleset ruleset = Accepted(Bakery.Replace(
            "name = \"bakery\"\n",
            "name = \"bakery\"\nhouses = false\npremises = false\n",
            StringComparison.Ordinal));

        Assert.False(ruleset.Kind(1).Houses);
        Assert.False(ruleset.Kind(1).Premises);
    }

    /// <summary>A numeric permission is refused, for both keys.</summary>
    /// <remarks>
    /// <b>The likeliest wrong value is the key they replaced.</b> An author reaching for
    /// <c>houses = 4</c> is somebody who half-remembers <c>occupants</c>, and TOML would otherwise
    /// hand the loader an integer where it asked a question.
    /// </remarks>
    [Theory]
    [InlineData("houses")]
    [InlineData("premises")]
    public void A_numeric_tenancy_is_refused(string key)
    {
        RulesetRefusal refusal = Refused(Bakery.Replace(
            "name = \"bakery\"\n",
            $"name = \"bakery\"\n{key} = 4\n",
            StringComparison.Ordinal));

        Assert.Contains("must be true or false", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// <c>tenanted</c> is refused by name, and the refusal says which of the two it became.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>Refused rather than read as <c>houses</c>, and the choice is the finding.</b> Every
    /// shipped file that wrote <c>tenanted = true</c> on a kind carrying a trade meant BOTH keys, so
    /// quietly keeping the housing half would have left every shop in the game unable to hold the
    /// shop — a Ruleset loading clean and doing less than it says. ***A key that changed meaning is
    /// more dangerous than a key that was deleted***, which is <c>RefuseRetired</c>'s whole job.
    /// </remarks>
    [Fact]
    public void The_retired_tenanted_key_is_refused_and_names_both_halves()
    {
        RulesetRefusal refusal = Refused(Bakery.Replace(
            "name = \"bakery\"\n",
            "name = \"bakery\"\ntenanted = true\n",
            StringComparison.Ordinal));

        Assert.Contains("houses = true", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("premises = true", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// The two retired capacity counts are refused on a <c>[[building]]</c>, and told where they
    /// went.
    /// </summary>
    /// <remarks>
    /// <b>The message is the point rather than the refusal</b> — <c>adr/0148</c>'s reasoning on a
    /// third axis. These keys were legal in every shipped Ruleset until <c>plans/0053</c>, so the
    /// author most likely to write one is copying a file that predates the move, and a bare *unknown
    /// key* would send them looking for a typo. ⚠ <b>Each refusal names <c>[capacity]</c> and says
    /// the quantity is DERIVED</b>, because the reader's next question is not *where did it go* but
    /// *then what decides it*.
    /// </remarks>
    [Theory]
    [InlineData("occupants = 5", "floor_tiles_per_occupant")]
    [InlineData("parking = 24", "floor_tiles_per_parking_space")]
    public void A_retired_capacity_count_is_refused_and_names_what_replaced_it(string key, string went)
    {
        RulesetRefusal refusal = Refused(Bakery.Replace(
            "name = \"bakery\"\n",
            $"name = \"bakery\"\n{key}\n",
            StringComparison.Ordinal));

        Assert.Contains(went, refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("DERIVED", refusal.Reason, StringComparison.Ordinal);
    }

    // ---- employment, adr/0068's rule on a second axis (5b-bis task 2) ---------------------------

    /// <summary>
    /// A kind that declares no trade comes with none.
    /// </summary>
    /// <remarks>
    /// <b>The default is load-bearing here in a way it was not for occupancy</b>, because it is the
    /// state of every kind that ever shipped before <c>adr/0148</c> rather than of most of them —
    /// and <c>0</c> is unambiguous, since Business kind ids are one-based exactly as Building kind
    /// ids are.
    /// </remarks>
    [Fact]
    public void A_kind_that_declares_no_trade_comes_with_none()
    {
        Ruleset ruleset = Accepted(Bakery);

        Assert.Equal(0, ruleset.Kind(1).Business);
    }

    /// <summary>
    /// The three employment keys are refused on a <c>[[building]]</c>, and told where they went.
    /// </summary>
    /// <remarks>
    /// <b><c>adr/0148</c>, and the message is the point rather than the refusal.</b> These keys were
    /// legal in every shipped Ruleset until milestone 27, so the author most likely to write one is
    /// somebody copying a file that predates the move. ***A bare "unknown key" would send them
    /// looking for a typo***, so the refusal names <c>[[business]]</c> and the <c>business</c> key.
    /// </remarks>
    [Theory]
    [InlineData("jobs = 8")]
    [InlineData("shift_start_earliest_hour = 6")]
    [InlineData("shift_start_latest_hour = 10")]
    public void An_employment_key_on_a_building_kind_is_refused_and_says_where_it_went(string key)
    {
        RulesetRefusal refusal = Refused(Bakery.Replace(
            "name = \"bakery\"\n",
            $"name = \"bakery\"\n{key}\n",
            StringComparison.Ordinal));

        Assert.Contains("[[business]]", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("A Building employs nobody", refusal.Reason, StringComparison.Ordinal);
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
    public void A_kind_naming_an_undeclared_trade_is_refused()
    {
        RulesetRefusal refusal = Refused(Bakery.Replace(
            "name = \"bakery\"\n",
            "name = \"bakery\"\nbusiness = \"greengrocer\"\n",
            StringComparison.Ordinal));

        Assert.Contains("no [[business]] declares that trade", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("employs nobody", refusal.Reason, StringComparison.Ordinal);
    }

    // ---- [capacity] (plans/0053) -----------------------------------------------------------------

    /// <summary>A Ruleset with no <c>[capacity]</c> holds nobody, employs nobody, parks nothing.</summary>
    /// <remarks>
    /// <b>Absence is a city in each case, and they are three different cities.</b> No table means no
    /// Building holds anybody; no <c>floor_tiles_per_job</c> means nobody is employed anywhere; no
    /// <c>floor_tiles_per_parking_space</c> means the city has no parking. That is <c>[placement]</c>'s
    /// polarity rather than <c>[layers]</c>'s — a defaulted rate would put three hash-bearing numbers
    /// into the binary with nobody having authored them.
    /// </remarks>
    [Fact]
    public void A_ruleset_with_no_capacity_table_derives_nothing()
    {
        Ruleset ruleset = Accepted(Bakery);

        Assert.Equal(0, ruleset.Capacity.FloorTilesPerOccupant);
        Assert.Equal(0, ruleset.Capacity.FloorTilesPerJob);
        Assert.Equal(0, ruleset.Capacity.FloorTilesPerParkingSpace);
    }

    /// <summary>The three rates reach the Ruleset.</summary>
    [Fact]
    public void The_capacity_rates_reach_the_ruleset()
    {
        Ruleset ruleset = Accepted(Bakery + """

            [capacity]
            floor_tiles_per_occupant      = 6
            floor_tiles_per_job           = 1
            floor_tiles_per_parking_space = 6
            """);

        Assert.Equal(6, ruleset.Capacity.FloorTilesPerOccupant);
        Assert.Equal(1, ruleset.Capacity.FloorTilesPerJob);
        Assert.Equal(6, ruleset.Capacity.FloorTilesPerParkingSpace);
    }

    /// <summary>Each rate is independent, and one does not default from another.</summary>
    /// <remarks>
    /// <b>The case a single shared number could not express</b>, and the one that says why these are
    /// three keys rather than one: a workplace houses nobody and employs a hundred, a dwelling the
    /// reverse, and a mixed-use Building both. The <em>form</em> is shared — all three divide the same
    /// floor area — and what differs is how much of it one of the thing takes.
    /// </remarks>
    [Fact]
    public void The_capacity_rates_are_stated_independently()
    {
        Ruleset ruleset = Accepted(Bakery + """

            [capacity]
            floor_tiles_per_job = 1
            """);

        Assert.Equal(0, ruleset.Capacity.FloorTilesPerOccupant);
        Assert.Equal(1, ruleset.Capacity.FloorTilesPerJob);
        Assert.Equal(0, ruleset.Capacity.FloorTilesPerParkingSpace);
    }

    // ---- [[building]] rent (02 §5.2 step 2b) -------------------------------------------------------

    [Fact]
    public void A_kind_that_declares_rent_carries_the_amount()
    {
        Ruleset ruleset = Accepted(Bakery.Replace(
            "name = \"bakery\"\n",
            "name = \"bakery\"\nrent = 50\n",
            StringComparison.Ordinal));

        Assert.Equal(50, ruleset.Kind(1).Rent.Raw);
    }

    [Fact]
    public void A_kind_with_no_rent_is_free()
    {
        Ruleset ruleset = Accepted(Bakery);

        Assert.Equal(0, ruleset.Kind(1).Rent.Raw);
    }

    [Fact]
    public void A_negative_rent_is_refused()
    {
        RulesetRefusal refusal = Refused(Bakery.Replace(
            "name = \"bakery\"\n",
            "name = \"bakery\"\nrent = -1\n",
            StringComparison.Ordinal));

        Assert.Contains("rent is -1", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>A stated zero is refused, and absence is how <em>none</em> is meant.</summary>
    /// <remarks>
    /// <b>A rate of zero would divide by nothing</b>, and a key written to mean <em>none</em> reads
    /// as an author who thought they were setting a quantity. The refusal says which spelling they
    /// wanted, because the symptom — a city that employs nobody — names neither the file nor the key.
    /// </remarks>
    [Theory]
    [InlineData("floor_tiles_per_occupant")]
    [InlineData("floor_tiles_per_job")]
    [InlineData("floor_tiles_per_parking_space")]
    public void A_zero_capacity_rate_is_refused(string key)
    {
        RulesetRefusal refusal = Refused(Bakery + $"""

            [capacity]
            {key} = 0
            """);

        Assert.Contains("out of range", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("Omit the key", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>A rate larger than a Cell is refused.</summary>
    /// <remarks>
    /// <b>1,024 Tiles is 16,384 m²</b>, and a rate past that is a quantity nothing in any city could
    /// satisfy — every Building would hold one of whatever it is, which is the floor
    /// <c>CapacityRuleset.Holds</c> already gives. So the key would be inert rather than extreme,
    /// which is the failure mode this corpus refuses by name.
    /// </remarks>
    [Fact]
    public void A_capacity_rate_larger_than_a_cell_is_refused()
    {
        RulesetRefusal refusal = Refused(Bakery + """

            [capacity]
            floor_tiles_per_occupant = 1025
            """);

        Assert.Contains("out of range", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("at most a Cell", refusal.Reason, StringComparison.Ordinal);
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

    /// <summary>
    /// A Ruleset that declares a gate and no give-up duration is refused.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b><c>adr/0130</c>'s argument made mechanical, and this is the check that makes it one.</b>
    /// <c>CONTEXT.md</c> → Unplaced Pool says *"whoever builds the gate owes the give-up rule in the
    /// same milestone"*, and until now that was a sentence somebody had to remember. A gate is an
    /// inflow into the Pool; a Pool with an inflow and no sink is a collection that grows with
    /// elapsed time, which <c>adr/0006</c> forbids.
    /// </para>
    /// <para>
    /// ⚠ <b>What the loader can see is a gate <em>kind</em>, not a gate</b>, and the line matters:
    /// milestone 11 task 5 established that the loader cannot see a <em>world</em>, which is why the
    /// gate↔Hinterland pairing had to happen at arrival instead. A declared kind is a fact about the
    /// file, so this check is on the right side of that line.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_ruleset_with_a_gate_and_no_give_up_duration_is_refused()
    {
        RulesetRefusal refusal = Refused(
            Bakery
            + "\n[[building]]\nname = \"port\"\narrivals_per_day = 4\n"
            + "\n[placement]\ninterval = 32\nrevisit_ticks = 1024\ncandidates = 3\n");

        Assert.Contains("gives_up_after_days", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// A Ruleset with no gate may omit the give-up duration, and nine shipped files do.
    /// </summary>
    /// <remarks>
    /// <b>The half that keeps the refusal from being a blanket requirement.</b> Without a gate
    /// nothing creates a Household after world creation, so the Pool is a subset of a population
    /// fixed at that moment and cannot grow with elapsed time whatever it does — <c>adr/0054</c>'s
    /// reasoning, still standing for every file with no door in it. Requiring the key there would put
    /// a hash-bearing number in nine files to no effect, and ***an inert number in a Ruleset is one a
    /// designer tunes expecting an effect.***
    /// </remarks>
    [Fact]
    public void A_ruleset_with_no_gate_may_omit_the_give_up_duration()
    {
        Ruleset ruleset = Accepted(
            Bakery + "\n[placement]\ninterval = 32\nrevisit_ticks = 1024\ncandidates = 3\n");

        Assert.False(ruleset.Placement.GivesUp);
    }

    /// <summary>
    /// A Ruleset that declares a gate and no <c>[placement]</c> table at all is refused.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>The wider door, and milestone 11 task 7 stopped short of it</b> — <c>plans/0035</c>
    /// <b>F28</b>. That task refused a file stating <c>[placement]</c> <em>without</em>
    /// <c>gives_up_after_days</c>, and said nothing about a file stating no <c>[placement]</c> at
    /// all — which has an inflow into the Pool, no housing <em>and</em> no sink. ***A guard written
    /// against a missing key does not cover a missing table***, and the case it missed is the worse
    /// of the two.
    /// </remarks>
    [Fact]
    public void A_ruleset_with_a_gate_and_no_placement_table_is_refused()
    {
        RulesetRefusal refusal = Refused(
            Bakery + "\n[[building]]\nname = \"port\"\narrivals_per_day = 4\n");

        Assert.Contains("no [placement] table at all", refusal.Reason, StringComparison.Ordinal);
    }
}

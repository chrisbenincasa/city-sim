using System.Text;
using Borough.Core.Determinism;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;
using static Borough.Tests.Formats.SourcePackage;

namespace Borough.Tests.Formats;

/// <summary>
/// Plan 0077's foundation: typed declarations collected across members, deterministic resolution
/// and source-located diagnostics.
/// </summary>
public sealed class RulesetSourceTests
{
    [Fact]
    public void A_package_resolves_forward_references_across_members()
    {
        RulesetSourceResult result = Accepted(Base);

        Assert.Equal(2, result.Ruleset!.BinsOf(1).Length);
        Assert.Equal(RulesetSourceMode.Source, result.Capture!.Mode);
    }

    [Fact]
    public void Dense_ids_follow_source_ids_rather_than_file_placement()
    {
        RulesetSourceResult result = Accepted(Base);

        Assert.Equal(
            ["building:dwelling", "resource:repairs", "resource:sundries", "rule:consume", "rule:upkeep"],
            result.Declarations.Select(d => $"{d.Section}:{d.Id}"));
        Assert.Equal("repairs", result.Names.Resource(new ResourceId(1)));
        Assert.Equal("consume", result.Names.Rule(new RuleId(1)));
        Assert.Equal("upkeep", result.Names.Rule(new RuleId(2)));
    }

    [Fact]
    public void An_explicit_order_ranks_rules_before_their_ids_do()
    {
        string ordered = Consumption.Replace(
            "id      = \"consume\"", "id      = \"consume\"\norder   = 1", StringComparison.Ordinal);

        RulesetSourceResult result = Accepted(
            ("dwelling.toml", Dwelling), ("goods.toml", Goods), ("rules.toml", ordered));

        Assert.Equal("upkeep", result.Names.Rule(new RuleId(1)));
        Assert.Equal("consume", result.Names.Rule(new RuleId(2)));
        Assert.Equal(1, result.Declarations.Single(d => d.Id == "consume").Order);
    }

    [Fact]
    public void Identity_keys_fold_the_id_and_labels_are_only_display_text()
    {
        RulesetSourceResult result = Accepted(Base);

        Assert.Equal(ContentHash.Of(Encoding.UTF8.GetBytes("dwelling")), result.Ruleset!.KindKeys[0]);
        Assert.Equal("Terraced house", result.Names.Kind(1));
        Assert.Equal("Sundries", result.Names.Resource(new ResourceId(2)));
        Assert.Equal("Terraced house", result.Declarations.Single(d => d.Id == "dwelling").Label);
    }

    [Fact]
    public void A_label_may_be_unicode_while_an_id_may_not()
    {
        string labelled = Goods.Replace("\"Sundries\"", "\"Épicerie\"", StringComparison.Ordinal);

        Assert.Equal("Épicerie", Accepted(
            ("dwelling.toml", Dwelling), ("goods.toml", labelled), ("rules.toml", Consumption))
            .Names.Resource(new ResourceId(2)));

        string renamed = Goods.Replace("id = \"repairs\"", "id = \"réparations\"", StringComparison.Ordinal);

        RulesetDiagnostic refused = Refused(RulesetDiagnosticCode.Id,
            ("dwelling.toml", Dwelling), ("goods.toml", renamed), ("rules.toml", Consumption));

        Assert.Equal(("goods.toml", 7, 1), (refused.Path, refused.Line, refused.Column));
    }

    [Fact]
    public void Moving_a_declaration_between_members_keeps_the_resolved_ruleset_but_not_the_identity()
    {
        RulesetSourceResult original = Accepted(Base);

        string sundriesOnly = Goods[..Goods.IndexOf("[[resource]]", 10, StringComparison.Ordinal)];
        string repairs = Goods[Goods.IndexOf("[[resource]]", 10, StringComparison.Ordinal)..];

        RulesetSourceResult moved = Accepted(
            ("dwelling.toml", repairs + "\n\n" + Dwelling), ("goods.toml", sundriesOnly), ("rules.toml", Consumption));

        AssertSameResolution(original, moved);
        Assert.NotEqual(original.Capture!.ContentHash, moved.Capture!.ContentHash);
    }

    [Fact]
    public void Reordering_the_manifest_keeps_the_resolved_ruleset_but_not_the_identity()
    {
        RulesetSourceResult listed = Accepted(Base);
        RulesetSourceResult reordered = RulesetSource.Resolve(
            Capture(Manifest("rules.toml", "goods.toml", "dwelling.toml"), Base));

        Assert.True(reordered.Ok, reordered.Describe());
        AssertSameResolution(listed, reordered);
        Assert.NotEqual(listed.Capture!.ContentHash, reordered.Capture!.ContentHash);
    }

    [Fact]
    public void The_field_comparison_sees_a_changed_value_and_a_changed_order()
    {
        RulesetSourceResult original = Accepted(Base);
        RulesetSourceResult capacity = Accepted(
            ("dwelling.toml", Dwelling.Replace("capacity = 48", "capacity = 49", StringComparison.Ordinal)),
            ("goods.toml", Goods), ("rules.toml", Consumption));
        RulesetSourceResult ordered = Accepted(
            ("dwelling.toml", Dwelling), ("goods.toml", Goods),
            ("rules.toml", Consumption.Replace("id      = \"consume\"", "id      = \"consume\"\norder   = 1",
                StringComparison.Ordinal)));

        Assert.ThrowsAny<Exception>(() => AssertSameFields(original.Ruleset, capacity.Ruleset));
        Assert.ThrowsAny<Exception>(() => AssertSameFields(original.Ruleset, ordered.Ruleset));
    }

    [Fact]
    public void Enumerating_the_same_captured_bytes_in_another_order_changes_nothing()
    {
        string manifest = Manifest("dwelling.toml", "goods.toml", "rules.toml");
        RulesetSourceResult forward = RulesetSource.Resolve(Capture(manifest, Base));
        RulesetSourceResult backward = RulesetSource.Resolve(Capture(manifest, [.. Base.Reverse()]));

        Assert.True(forward.Ok, forward.Describe());
        Assert.Equal(forward.Capture!.ContentHash, backward.Capture!.ContentHash);
        AssertSameResolution(forward, backward);

        (string, string)[] broken =
            [("dwelling.toml", Dwelling + "\norder = 2"), ("goods.toml", Goods), ("rules.toml", "[[rule]]\nkind = 1")];

        Assert.Equal(
            RulesetSource.Resolve(Capture(manifest, broken)).Describe(),
            RulesetSource.Resolve(Capture(manifest, [.. broken.Reverse()])).Describe());
    }

    [Fact]
    public void A_duplicate_id_names_both_declarations_even_when_they_agree()
    {
        const string Again = """
            # the same Good, stated twice
            [[resource]]
            id = "repairs"
            family = "good"
            """;

        RulesetDiagnostic refused = Refused(RulesetDiagnosticCode.DuplicateId,
            [.. Base, ("more-goods.toml", Again)]);

        Assert.Equal(("more-goods.toml", 2, 1), (refused.Path, refused.Line, refused.Column));
        Assert.Equal(("resource", "repairs"), (refused.Section, refused.Id));
        Assert.Contains("goods.toml:6:1", refused.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void The_same_id_in_different_sections_is_not_a_duplicate()
    {
        const string Trade = """
            [[business]]
            id = "dwelling"
            """;

        Accepted([.. Base, ("trades.toml", Trade)]);
    }

    [Fact]
    public void A_singleton_section_has_one_owning_member()
    {
        RulesetDiagnostic refused = Refused(RulesetDiagnosticCode.SingletonOwner,
            [.. Base, ("a.toml", "[jobs]\ninterval = 32\n"), ("b.toml", "\n[jobs]\n")]);

        Assert.Equal(("b.toml", 2, 1), (refused.Path, refused.Line, refused.Column));
        Assert.Contains("a.toml:1:1", refused.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void A_section_cannot_be_both_a_table_and_an_array_of_tables()
    {
        Refused(RulesetDiagnosticCode.DeclarationKind,
            [.. Base, ("a.toml", "[jobs]\n"), ("b.toml", "[[jobs]]\nid = \"x\"\n")]);
    }

    [Fact]
    public void A_refusal_from_the_single_file_reader_is_reported_at_its_member_line()
    {
        // A scope is a closed set the reader parses, not a reference the resolver can match.
        string misspelt = Consumption.Replace("\"local\", resource = \"sundries\"",
            "\"locall\", resource = \"sundries\"", StringComparison.Ordinal);

        RulesetSourceResult result = Resolve(
            ("dwelling.toml", Dwelling), ("goods.toml", Goods), ("rules.toml", misspelt));

        RulesetDiagnostic refused = Assert.Single(result.Diagnostics);

        Assert.Equal(RulesetDiagnosticCode.Ruleset, refused.Code);
        Assert.Equal(("rules.toml", "rule", "consume"), (refused.Path, refused.Section, refused.Id));
        Assert.InRange(refused.Line, 9, 14);

        RulesetRefusal shape = Assert.Single(result.ToLoadResult().Refusals);
        Assert.Equal(("rules.toml", refused.Line), (shape.File, shape.Line));
    }

    [Fact]
    public void Independent_diagnostics_in_different_members_are_all_reported()
    {
        RulesetSourceResult result = Resolve(
            ("dwelling.toml", Dwelling.Replace("id = \"dwelling\"", "", StringComparison.Ordinal)),
            ("goods.toml", Goods + "\norder = 3"),
            ("rules.toml", Consumption));

        Assert.Equal(
            [("dwelling.toml", RulesetDiagnosticCode.Id), ("goods.toml", RulesetDiagnosticCode.Order)],
            result.Diagnostics.Select(d => (d.Path, d.Code)));
    }

    [Theory]
    [InlineData("[[resource]]\nfamily = \"good\"\n", "no id")]
    [InlineData("[[resource]]\nid = 7\nfamily = \"good\"\n", "quoted string")]
    [InlineData("[[resource]]\nid = \"\"\nfamily = \"good\"\n", "quoted string")]
    [InlineData("[[resource]]\nid = \"-lead\"\nfamily = \"good\"\n", "quoted string")]
    [InlineData("[[resource]]\nid = \"two words\"\nfamily = \"good\"\n", "quoted string")]
    [InlineData("[[resource]]\nid = \"coal\"\nname = \"coal\"\nfamily = \"good\"\n", "label")]
    public void An_id_is_required_well_formed_and_replaces_name(string member, string said)
    {
        RulesetDiagnostic refused = Refused(RulesetDiagnosticCode.Id, [.. Base, ("coal.toml", member)]);

        Assert.Equal("coal.toml", refused.Path);
        Assert.Contains(said, refused.Reason, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("[[resource]]\nid = \"coal\"\norder = 1\nfamily = \"good\"\n", RulesetDiagnosticCode.Order)]
    [InlineData("[[rule]]\nid = \"burn\"\norder = -1\n", RulesetDiagnosticCode.Order)]
    [InlineData("[[rule]]\nid = \"burn\"\norder = \"1\"\n", RulesetDiagnosticCode.Order)]
    [InlineData("[[resource]]\nid = \"coal\"\nlabel = 3\nfamily = \"good\"\n", RulesetDiagnosticCode.Label)]
    public void Order_and_label_are_typed(string member, string code)
    {
        Assert.Equal(3, Refused(code, [.. Base, ("coal.toml", member)]).Line);
    }

    [Fact]
    public void A_nested_table_stays_with_its_owner_in_one_member()
    {
        RulesetDiagnostic refused = Refused(RulesetDiagnosticCode.MemberShape,
            [.. Base, ("edges.toml", "[[hinterland]]\nid = \"west\"\n"), ("people.toml", "[[hinterland.population]]\n")]);

        Assert.Equal(("people.toml", 1, 1), (refused.Path, refused.Line, refused.Column));
    }

    [Theory]
    [InlineData("rate = 3\n")]
    [InlineData("[source]\nversion = 1\nmembers = []\n")]
    public void A_member_contains_only_declarations(string member)
    {
        Assert.Equal("stray.toml",
            Refused(RulesetDiagnosticCode.MemberShape, [.. Base, ("stray.toml", member)]).Path);
    }

    [Fact]
    public void A_syntax_error_is_located_in_its_member()
    {
        RulesetSourceResult result = Resolve([.. Base, ("broken.toml", "\n\n[[resource]\nid = \"x\"\n")]);

        RulesetDiagnostic refused = Assert.Single(result.Diagnostics);

        Assert.Equal(("broken.toml", RulesetDiagnosticCode.Syntax, 3), (refused.Path, refused.Code, refused.Line));
    }

    [Theory]
    [InlineData("[[recipe]]\nid = \"bake\"\n")]
    [InlineData("[[rule]]\nid = \"bake\"\nkind = \"dwelling\"\nrecipe = \"bake\"\n")]
    public void Shared_definitions_without_runtime_support_are_refused_by_name(string member)
    {
        RulesetDiagnostic refused = Refused(RulesetDiagnosticCode.Unimplemented, [.. Base, ("shared.toml", member)]);

        Assert.Contains("not implement", refused.Reason, StringComparison.Ordinal);
    }

    private static void AssertSameResolution(RulesetSourceResult expected, RulesetSourceResult actual)
    {
        AssertSameFields(expected.Ruleset, actual.Ruleset);
        Assert.Equal(
            expected.Declarations.Select(d => (d.Section, d.Id, d.Label, d.Order)),
            actual.Declarations.Select(d => (d.Section, d.Id, d.Label, d.Order)));

        for (byte kind = 0; kind < 4; kind++)
        {
            Assert.Equal(expected.Names.Kind(kind), actual.Names.Kind(kind));
        }

        for (ushort id = 0; id < 4; id++)
        {
            Assert.Equal(expected.Names.Resource(new ResourceId(id)), actual.Names.Resource(new ResourceId(id)));
            Assert.Equal(expected.Names.Rule(new RuleId(id)), actual.Names.Rule(new RuleId(id)));
        }
    }
}

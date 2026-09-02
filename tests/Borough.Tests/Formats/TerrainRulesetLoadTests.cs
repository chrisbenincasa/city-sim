using Borough.Core.Arithmetic;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// Milestone 24 task 2: the <c>[[terrain]]</c> tables and every refusal they state.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every refusal has a test that writes the malformed Ruleset and watches it fire</b>, on
/// <see cref="ParkingRulesetLoadTests"/>' discipline and for <c>adr/0064</c>'s reason.
/// </para>
/// <para>
/// ⚠ <b>The all-five refusal is the one worth reading first.</b> Every other table in the loader is
/// optional key by key; this one is optional only as a <em>set</em>, and
/// <see cref="A_file_that_prices_only_some_of_the_ground_is_refused"/> is where the argument for that
/// is enforced rather than asserted. <c>adr/0158</c>.
/// </para>
/// <para>
/// <b>The key carries its denomination in its name</b> — <c>base_fertility_percent</c> rather than
/// <c>base_fertility</c> — on <c>radius_metres</c>' reasoning: nothing in the file distinguishes a
/// percentage from a Q16.16 fraction, and the second reading of <c>100</c> is a Cell 65,536 times too
/// fertile with no symptom that names the key.
/// </para>
/// </remarks>
public sealed class TerrainRulesetLoadTests
{
    /// <summary>The smallest complete Ruleset. Nothing here needs a Rule or a road to exist.</summary>
    private const string Nothing = """
        [[resource]]
        name = "flour"
        family = "good"
        """;

    /// <summary>
    /// The five types with the two keys <c>rulesets/varied.toml</c> states for each.
    /// </summary>
    private const string Ground = """
        [[terrain]]
        name = "ordinary"
        base_fertility_percent = 100
        sealing_decay_tau = 96

        [[terrain]]
        name = "rock"
        base_fertility_percent = 20
        sealing_decay_tau = 0

        [[terrain]]
        name = "floodplain"
        base_fertility_percent = 100
        sealing_decay_tau = 48

        [[terrain]]
        name = "marsh"
        base_fertility_percent = 50
        sealing_decay_tau = 64

        [[terrain]]
        name = "thin_soil"
        base_fertility_percent = 60
        sealing_decay_tau = 160
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

    /// <summary>One <c>[[terrain]]</c> table.</summary>
    private static string Table(string name, string body) => $"[[terrain]]\nname = \"{name}\"\n{body}";

    /// <summary>
    /// The five tables with one of them replaced, so a test about one type stays about that type.
    /// </summary>
    private static string Excepting(string name, string? replacement)
    {
        var kept = new List<string>();

        foreach (string table in Ground.Split("\n\n"))
        {
            if (!table.Contains($"\"{name}\"", StringComparison.Ordinal))
            {
                kept.Add(table);
            }
        }

        if (replacement is not null)
        {
            kept.Add(replacement);
        }

        return $"{Nothing}\n\n{string.Join("\n\n", kept)}";
    }

    // ---- the absent set ---------------------------------------------------------------------------

    /// <summary>
    /// A Ruleset with no <c>[[terrain]]</c> prices no ground, and that is a statement.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>It is not a world without terrain in it.</b> That is the polarity that separates this from
    /// <c>[parking]</c>: absence there is a city with no Parking Shed, and absence here is a city whose
    /// ground the file declines to value. <c>TerrainGenerator</c> writes the type column from the
    /// <c>WorldKey</c> either way, which is what
    /// <see cref="A_file_that_prices_only_some_of_the_ground_is_refused"/> rests on.
    /// </remarks>
    [Fact]
    public void A_ruleset_with_no_terrain_tables_prices_no_ground()
    {
        Ruleset ruleset = Accepted(Nothing);

        Assert.Equal(TerrainRuleset.None, ruleset.Terrain);
        Assert.False(ruleset.Terrain.Stated);
    }

    /// <summary>
    /// An unpriced Ruleset <b>throws</b> on a lookup rather than answering zero.
    /// </summary>
    /// <remarks>
    /// A placeholder zero is a value that would be read, believed and tuned around —
    /// <c>MapLayers.Fertility</c>'s own reasoning, one level down. Zero is also a Base Fertility a
    /// file may legitimately state, so it cannot do duty as <em>unset</em>.
    /// </remarks>
    [Fact]
    public void An_unpriced_ruleset_refuses_a_lookup_rather_than_answering_zero()
    {
        Ruleset ruleset = Accepted(Nothing);

        InvalidOperationException thrown = Assert.Throws<InvalidOperationException>(
            () => ruleset.Terrain.BaseFertility(TerrainKind.Rock));

        Assert.Contains("[[terrain]]", thrown.Message, StringComparison.Ordinal);
    }

    // ---- what a stated set carries ------------------------------------------------------------------

    /// <summary>The five Base Fertilities reach the core, as Q16.16.</summary>
    /// <remarks>
    /// <b>Authored as a percent and stored as a fraction</b> — <c>adr/0048</c> refuses an unquoted
    /// decimal on the path in, and <c>adr/0156</c> makes <see cref="Fixed.One"/> mean fully fertile so
    /// that Fertility composes as a proportion.
    /// </remarks>
    [Theory]
    [InlineData(TerrainKind.Ordinary, 100)]
    [InlineData(TerrainKind.Rock, 20)]
    [InlineData(TerrainKind.Floodplain, 100)]
    [InlineData(TerrainKind.Marsh, 50)]
    [InlineData(TerrainKind.ThinSoil, 60)]
    public void A_stated_set_carries_every_base_fertility(TerrainKind kind, int percent)
    {
        Ruleset ruleset = Accepted($"{Nothing}\n\n{Ground}");

        Assert.True(ruleset.Terrain.Stated);
        Assert.Equal(
            IntegerMath.RoundDiv(Fixed.FromInt(percent), 100), ruleset.Terrain.BaseFertility(kind));
    }

    /// <summary><c>100</c> is fully fertile, which is the scale's own top.</summary>
    [Fact]
    public void A_hundred_percent_is_the_top_of_the_scale()
    {
        Ruleset ruleset = Accepted($"{Nothing}\n\n{Ground}");

        Assert.Equal(Fixed.One, ruleset.Terrain.BaseFertility(TerrainKind.Ordinary));
    }

    /// <summary>Zero is a Ruleset a file may write, and it is not <em>unset</em>.</summary>
    /// <remarks>
    /// The pair of this and <see cref="A_ruleset_with_no_terrain_tables_prices_no_ground"/> is what
    /// makes absence the only spelling of unset. ⚠ <b>Barren ground is not a wall</b> — <c>adr/0022</c>
    /// puts the gradient argument on <c>rock</c>'s value, which is a Ruleset choice, and not on what
    /// the loader will accept.
    /// </remarks>
    [Fact]
    public void Barren_ground_is_a_ruleset_a_file_may_write()
    {
        Ruleset ruleset = Accepted(
            Excepting("rock", Table("rock", "base_fertility_percent = 0\nsealing_decay_tau = 8")));

        Assert.True(ruleset.Terrain.Stated);
        Assert.Equal(0, ruleset.Terrain.BaseFertility(TerrainKind.Rock));
    }

    // ---- the refusals -------------------------------------------------------------------------------

    /// <summary>
    /// A file that prices four of the five types is refused, and the refusal names the missing one.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>This is the refusal the whole table shape exists for.</b> The generator places every type
    /// from the <c>WorldKey</c> whatever the Ruleset says, so an unstated type is ground the world
    /// <em>contains</em> and the file values at nothing — a silent sterile band rather than an error.
    /// <c>TerrainRuleset.Kinds</c> carries the argument.
    /// </remarks>
    [Theory]
    [InlineData("ordinary")]
    [InlineData("rock")]
    [InlineData("floodplain")]
    [InlineData("marsh")]
    [InlineData("thin_soil")]
    public void A_file_that_prices_only_some_of_the_ground_is_refused(string missing)
    {
        RulesetRefusal refusal = Refused(Excepting(missing, replacement: null));

        Assert.Contains($"'{missing}'", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("prices all of it", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>A name that is not one of the five is refused, and the refusal lists them.</summary>
    /// <remarks>
    /// <b>A <c>[[terrain]]</c> name selects a member of a closed set rather than declaring one</b>,
    /// which is what separates it from every other <c>name</c> the loader reads. A file cannot add a
    /// sixth type; <c>adr/0158</c> makes appending one a code change and a re-baseline.
    /// </remarks>
    [Fact]
    public void A_name_that_is_not_a_terrain_type_is_refused()
    {
        RulesetRefusal refusal = Refused(
            Excepting("marsh", Table("swamp", "base_fertility_percent = 50\nsealing_decay_tau = 8")));

        Assert.Contains("'swamp' is not a terrain type", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("thin_soil", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>Two tables for one type are refused.</summary>
    /// <remarks>
    /// <b>Ambiguous rather than additive</b>, on <c>[[hinterland]]</c>'s duplicate-edge reasoning:
    /// a terrain type has one Base Fertility shared by every Cell of that ground, and nothing says
    /// which of two a Cell would read.
    /// </remarks>
    [Fact]
    public void A_second_table_for_one_type_is_refused()
    {
        RulesetRefusal refusal = Refused(
            $"{Nothing}\n\n{Ground}\n\n{Table("rock", "base_fertility_percent = 40\nsealing_decay_tau = 8")}");

        Assert.Contains("a second [[terrain]]", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("'rock'", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>A table that states no Base Fertility is refused.</summary>
    /// <remarks>
    /// <b>Required rather than defaulted</b>, on <c>[districts]</c>' rule that a stated table states
    /// its keys: a defaulted Base Fertility is a hash-bearing number nobody chose.
    /// </remarks>
    [Fact]
    public void A_table_that_states_no_base_fertility_is_refused()
    {
        RulesetRefusal refusal = Refused(Excepting("marsh", Table("marsh", "# nothing")));

        Assert.Contains("base_fertility_percent", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>A Base Fertility past the top of the scale is refused rather than clamped.</summary>
    /// <remarks>
    /// <c>adr/0156</c> makes <c>1.0</c> fully fertile, so <b>100 is the scale's own top rather than a
    /// tuning choice</b>. A file above it is not a very good field; it is a file whose author believes
    /// the units are something else, and clamping would hide exactly that.
    /// </remarks>
    [Theory]
    [InlineData(101)]
    [InlineData(1_000)]
    [InlineData(-1)]
    public void A_base_fertility_off_the_scale_is_refused(int percent)
    {
        RulesetRefusal refusal = Refused(
            Excepting("thin_soil", Table("thin_soil", $"base_fertility_percent = {percent}\nsealing_decay_tau = 8")));

        Assert.Contains($"base_fertility_percent is {percent}", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("adr/0156", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// The second key: how long this ground takes to shed its Sealing, in scheduled updates.
    /// </summary>
    /// <remarks>
    /// Milestone 24 task 4. <c>02 §2.4</c> keys the rate <b>by terrain type</b>, so it sits beside
    /// the type rather than in <c>[layers]</c> — where it lived as one global pinned at zero, and
    /// where it is now refused by name.
    /// </remarks>
    [Theory]
    [InlineData(TerrainKind.Ordinary, 96)]
    [InlineData(TerrainKind.Rock, 0)]
    [InlineData(TerrainKind.Floodplain, 48)]
    [InlineData(TerrainKind.Marsh, 64)]
    [InlineData(TerrainKind.ThinSoil, 160)]
    public void A_stated_set_carries_every_sealing_decay_tau(TerrainKind kind, int tau)
    {
        Ruleset rules = Accepted($"{Nothing}\n\n{Ground}");

        Assert.Equal(tau, rules.Terrain.SealingDecayTau(kind));
    }

    /// <summary>A table that states no decay rate is refused, exactly as an unpriced one is.</summary>
    /// <remarks>
    /// <b>Zero cannot double as unset and that is why the key is required</b>: zero means <em>never
    /// recovers</em>, which is rock's real answer (<c>CONTEXT.md</c> → Sealing), so defaulting to it
    /// would make every silence say <em>never</em> in the voice of a decision.
    /// </remarks>
    [Fact]
    public void A_table_that_states_no_sealing_decay_tau_is_refused()
    {
        RulesetRefusal refusal = Refused(
            Excepting("marsh", Table("marsh", "base_fertility_percent = 50")));

        Assert.Contains("sealing_decay_tau", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// 🔴 A tau past twice a Cell's Tile count is refused, and the ceiling is not arbitrary.
    /// </summary>
    /// <remarks>
    /// The decay step is <c>value ÷ tau</c>, so above <c>2 × TilesInCell</c> that step rounds to
    /// nothing on a <em>full</em> Cell — ***a rate so slow it is silently the same as zero***, which
    /// is the one value a designer must not be able to write by accident. Zero itself is admissible
    /// and means never, said out loud.
    /// </remarks>
    [Theory]
    [InlineData(-1)]
    [InlineData(2_049)]
    [InlineData(100_000)]
    public void A_sealing_decay_tau_off_the_scale_is_refused(int tau)
    {
        RulesetRefusal refusal = Refused(Excepting(
            "floodplain",
            Table("floodplain", $"base_fertility_percent = 100\nsealing_decay_tau = {tau}")));

        Assert.Contains($"sealing_decay_tau is {tau}", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>A misspelled section is refused, and the refusal lists <c>[[terrain]]</c>.</summary>
    /// <remarks>
    /// The unknown-section list is one string that every section has to be added to, and a section
    /// missing from it is a valid file refused. This is the test that notices.
    /// </remarks>
    [Fact]
    public void The_unknown_section_refusal_names_terrain()
    {
        RulesetRefusal refusal = Refused($"{Nothing}\n\n[[terain]]\nname = \"rock\"");

        Assert.Contains("[[terrain]]", refusal.Reason, StringComparison.Ordinal);
    }

    // ---- the shipped file ----------------------------------------------------------------------------

    /// <summary><c>rulesets/varied.toml</c> loads, and it is the only shipped file that prices ground.</summary>
    /// <remarks>
    /// ⚠ <b>It is the only file with a <c>[[terrain]]</c> table, and NOT the only world with varied
    /// terrain</b> — every world's terrain is varied, because the generator does not consult the
    /// Ruleset. See <c>TerrainGeneratorTests</c>, and read the file's own header before reading
    /// anything into a run on it.
    /// </remarks>
    [Fact]
    public void Varied_is_the_only_shipped_file_that_prices_its_ground()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "Rulesets");
        var priced = new List<string>();

        foreach (string file in Directory.GetFiles(directory, "*.toml"))
        {
            RulesetLoadResult result = RulesetLoader.Load(file);

            Assert.True(result.Ok, $"{Path.GetFileName(file)} was refused:\n{result.Describe()}");

            if (result.Ruleset!.Terrain.Stated)
            {
                priced.Add(Path.GetFileName(file));
            }
        }

        // pictured.toml states [[terrain]] because it states everything -- plans/0051 row 1 is a
        // world to photograph rather than a demonstration of one mechanism, and
        // TwinLatticeTests carries the whole argument for exempting it from a test of this shape.
        // ⚠ varied.toml is still the only file that DEMONSTRATES priced ground: this one turns it
        // on beside sixteen other mechanisms, so nothing measured here could be attributed to it.
        Assert.Equal(["pictured.toml", "varied.toml"], priced);
    }
}

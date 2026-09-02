using System.Globalization;
using System.Text;
using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Tomlyn.Parsing;
using Tomlyn.Syntax;

namespace Borough.Formats;

/// <summary>
/// Reads a Ruleset: Tomlyn in, ids and integers out, and every refusal in one walk.
/// </summary>
/// <remarks>
/// <para>
/// <b>The validator lives with the parser because a refusal's whole output is a sentence</b>
/// (<c>adr/0048</c>). A cycle check walks <c>on_fail</c> names; a <c>fills</c> check compares a
/// declared Resource against a Bin's; both need names and both must report one, and <c>adr/0002</c>
/// forbids the core from producing a string a human reads. So the names stop here and the core
/// receives ids.
/// </para>
/// <para>
/// <b>It works from the syntax tree rather than from the model, and that is the whole reason Tomlyn
/// was chosen.</b> <c>Toml.ToModel()</c> would be shorter and would throw away every source span,
/// leaving refusals that name a rule and not a line — which is two thirds of what <c>adr/0015</c>
/// promises. Walking the tree costs more code and buys the third.
/// </para>
/// <para>
/// <b>A tuning number is a bare integer or a quoted decimal, and an unquoted decimal is refused by
/// name.</b> Tomlyn's float path is never taken: a <c>double</c> that existed for microseconds would
/// fail as a State Hash divergence between two machines with no diff to look at, which is the class
/// of bug <c>05 §7</c> says costs days. Quote marks make it structurally impossible instead of
/// carefully managed.
/// </para>
/// </remarks>
public static class RulesetLoader
{
    /// <summary>Loads the Ruleset at <paramref name="path"/>, for a world that does not exist yet.</summary>
    public static RulesetLoadResult Load(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        return Parse(File.ReadAllText(path), path);
    }

    /// <summary>
    /// Every section this build reads, every key it reads there, and what shape it expects — keyed
    /// the way the file writes it: <c>[layers]</c>, <c>[[building]]</c>, <c>[[rule]] inputs</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the loader describing itself, and it exists so that nothing else has to.</b> The
    /// permitted key set is already derived from the readers rather than authored beside them —
    /// that is what <c>RefuseUnknownKeys</c> is built on — and an editor schema authored by hand
    /// would be the first second copy of it. <c>plans/0012</c> <b>Cause 1</b>: ***every document
    /// that stores what another document owns drifted.***
    /// </para>
    /// <para>
    /// ⚠ <b>The surface is bounded by what <paramref name="text"/> declares.</b> A reader only asks
    /// a table that exists, so a section this file omits contributes nothing — the caller unions
    /// across every shipped Ruleset, and even then a section no file demonstrates stays invisible.
    /// ***A schema built from this must therefore permit unknown keys***, which costs nothing: the
    /// loader refuses them at the parse site with a spelling suggestion, which is a better message
    /// than a schema can produce.
    /// </para>
    /// <para>
    /// ⚠ <b>It reads a refused file too.</b> <c>Read</c> stops adding refusals but every reader has
    /// still run and recorded what it asked for, so the surface is complete either way — which
    /// matters, because a caller generating a schema should not be stopped by one bad file.
    /// </para>
    /// </remarks>
    public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, RulesetKeyKind>> KeySurface(
        string text, string fileName)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentException.ThrowIfNullOrEmpty(fileName);

        var reader = new Reader(text, fileName, frozen: null);

        reader.Read();

        var surface = new Dictionary<string, IReadOnlyDictionary<string, RulesetKeyKind>>(
            StringComparer.Ordinal);

        foreach (KeyValuePair<string, Dictionary<string, RulesetKeyKind>> section in reader.Surface())
        {
            surface[section.Key] = section.Value;
        }

        return surface;
    }

    /// <summary>Loads a Ruleset from text, naming <paramref name="fileName"/> in every refusal.</summary>
    public static RulesetLoadResult Parse(string text, string fileName)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentException.ThrowIfNullOrEmpty(fileName);

        return new Reader(text, fileName, frozen: null).Read();
    }

    /// <inheritdoc cref="Reload(string, string, LayerConstants)"/>
    public static RulesetLoadResult Load(string path, LayerConstants frozen)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        return Reload(File.ReadAllText(path), path, frozen);
    }

    /// <summary>
    /// Loads a Ruleset <b>for a world that already exists</b>, refusing any change to a
    /// world-creation constant.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The second entry point, and the reason there has to be one</b> (<c>plans/0015</c>, item 4).
    /// Every other refusal is a property of the file alone, which is what let <c>adr/0048</c> put them
    /// all in one walk and be done. This one is a property of the file <em>against a particular
    /// world</em>: <c>adr/0015</c>'s world-creation constants are read from the Ruleset but frozen when
    /// the world is created, and whether a value changed is only knowable against the world it would
    /// be applied to. <b>Refused rather than warned about and rather than silently ignored</b> —
    /// silent ignoring is the failure mode <c>adr/0015</c>'s own revisit trigger names.
    /// </para>
    /// <para>
    /// <b><see cref="LayerConstants"/> and nothing else, which is smaller than the category and that
    /// is a finding rather than an omission.</b> <c>adr/0015</c> enumerates four members —
    /// <c>TICKS_PER_DAY</c>, <c>WHEEL_SIZE</c>, the Cell, and the industrial pollution kernel radius —
    /// and says they <em>"live in the Ruleset like everything else and are read from it"</em>. After
    /// slice 8 task 3 that is true of exactly one of them; the other three are <c>const</c>s in the
    /// binary that no Ruleset has ever been able to state. A file that cannot say a number cannot
    /// change it, so a refusal over the other three would be a check with no writer on the other side.
    /// Each joins this signature on the day it becomes authorable. Filed to <c>plans/0012</c>.
    /// </para>
    /// <para>
    /// <b>The comparison is on the effective value, not the authored one.</b> The kernel is stated in
    /// metres and used in Cells; 1,024 m and 1,100 m are both 8 Cells, and refusing the second would be
    /// refusing a reload that changes nothing a Cell was ever recorded in — which is the membership
    /// test itself, applied to the units the state is actually in.
    /// </para>
    /// </remarks>
    /// <param name="text">The Ruleset text.</param>
    /// <param name="fileName">Named in every refusal.</param>
    /// <param name="frozen">The world's Layer constants, as fixed when it was created.</param>
    public static RulesetLoadResult Reload(string text, string fileName, LayerConstants frozen)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentException.ThrowIfNullOrEmpty(fileName);

        return new Reader(text, fileName, frozen).Read();
    }

    /// <summary>
    /// One load. Everything is accumulated and nothing is applied, so a refusal costs nothing.
    /// </summary>
    private sealed class Reader(string text, string fileName, LayerConstants? frozen)
    {
        private readonly List<RulesetRefusal> _refusals = [];

        /// <summary>
        /// Which keys each table or inline table was <em>asked</em> for, by reference to the holder.
        /// </summary>
        /// <remarks>
        /// <b><c>plans/0041</c> G31's fix, in the shape the finding did not propose.</b> G31 says
        /// closing it means "every table declaring its permitted key set" — twenty-two hand-authored
        /// name lists. ***A hand-authored name list in this file is a thing that drifts***: the
        /// unknown-<em>section</em> list did exactly that, naming eighteen tables on one branch and
        /// nineteen on another and <c>[water]</c> on neither (<c>adr/0048</c>, the merge bullet).
        /// Recording what <see cref="Find"/> was asked for derives the same set from the reader, so
        /// there is nothing to keep in step and nothing to forget.
        /// </remarks>
        private readonly Dictionary<SyntaxNode, HashSet<string>> _consulted =
            new(ReferenceEqualityComparer.Instance);

        /// <summary>What each consulted key was expected to hold, where a reader said so.</summary>
        /// <remarks>
        /// <b>Deliberately parallel to <see cref="_consulted"/> rather than folded into it.</b> The
        /// permitted set is what refuses an unknown key and it must stay exactly the set of names
        /// asked for; this is a second, smaller thing — what shape the asking reader wanted — and it
        /// exists for <see cref="RulesetLoader.KeySurface"/> to generate an editor schema from. ⚠
        /// <b>Nothing in the loader reads it</b>, so a gap here refuses nothing and costs a
        /// <c>type</c> line in a generated schema.
        /// </remarks>
        private readonly Dictionary<SyntaxNode, Dictionary<string, RulesetKeyKind>> _keyKinds =
            new(ReferenceEqualityComparer.Instance);

        /// <summary>Keys a reader asks for only in order to refuse them.</summary>
        /// <remarks>
        /// <para>
        /// 🔴 <b>A retired key is PERMITTED and must never be OFFERED, and one set was doing both
        /// jobs.</b> <see cref="_consulted"/> is the permitted set and it is also what
        /// <see cref="RulesetLoader.KeySurface"/> publishes as an editor schema — so
        /// <c>condemn_after</c>, <c>sample</c>, <c>wage</c>, <c>storage</c> and the three keys
        /// <c>adr/0148</c> moved to <c>[[business]]</c> arrived in
        /// <c>rulesets/ruleset.schema.json</c> as valid completions. ***Eight keys whose only
        /// behaviour is to refuse the file were advertised as the way to do the thing the loader
        /// will not let you do.***
        /// </para>
        /// <para>
        /// ⚠ <b>The refusals are right and the advertisement was wrong.</b> An author writing
        /// <c>condemn_after</c> is told the key was renamed <em>and that its units changed</em>,
        /// which no unknown-key message could carry — so the name stays permitted, stays consulted,
        /// and is subtracted from the surface alone.
        /// </para>
        /// <para>
        /// ⚠ <b>Written only by <see cref="RefuseRetired"/></b>, which is also the only thing that
        /// refuses one. ***A retirement and its refusal are one edit or they drift***, which is
        /// <c>plans/0012</c> <b>Cause 1</b> and the reason this is not a hand-authored list of
        /// eight names.
        /// </para>
        /// </remarks>
        private readonly Dictionary<SyntaxNode, HashSet<string>> _retired =
            new(ReferenceEqualityComparer.Instance);

        private readonly Dictionary<string, ushort> _resources = new(StringComparer.Ordinal);

        // Parallel to _resources by declaration order, which is what makes ResourceId.Raw - 1 index
        // it. A Dictionary would be the obvious home and 05 section 4 lint 3 bans walking one.
        private readonly List<ResourceFamily> _families = [];

        // Parallel to _families, milestone 28. adr/0103's Need lives on the RESOURCE because 04
        // section 1 pairs the Good with the Need -- Food feeds Sustenance, Consumer Goods feed
        // Satisfaction -- so the thing consumed decides, not the Rule consuming it.
        private readonly List<Need> _resourceNeeds = [];
        private readonly Dictionary<string, byte> _kinds = new(StringComparer.Ordinal);

        // A SECOND kind namespace, not a widening of the first (adr/0141). The premises and the trade
        // are uncorrelated, so a file may name a [[building]] and a [[business]] the same word and
        // mean two different things -- which one map could not express.
        private readonly Dictionary<string, byte> _businessKinds = new(StringComparer.Ordinal);
        private readonly Dictionary<string, byte> _lifeStages = new(StringComparer.Ordinal);
        /// <summary>
        /// The three <c>[[building]]</c> keys <c>adr/0148</c> moved to <c>[[business]]</c>, refused
        /// by name here so an author is told where they went rather than that they are unknown.
        /// </summary>
        private static readonly string[] EmploymentKeysThatMoved =
            ["jobs", "shift_start_earliest_hour", "shift_start_latest_hour"];

        private readonly Dictionary<string, ushort> _rules = new(StringComparer.Ordinal);
        private readonly Dictionary<string, ushort> _conditions = new(StringComparer.Ordinal);

        private readonly List<TableSyntaxBase> _kindTables = [];
        private readonly List<TableSyntaxBase> _businessKindTables = [];
        private readonly List<TableSyntaxBase> _lifeStageTables = [];
        private readonly List<TableSyntaxBase> _ruleTables = [];
        private readonly List<TableSyntaxBase> _zoneRuleTables = [];

        private readonly List<TableSyntaxBase> _bandTables = [];
        private readonly List<TableSyntaxBase> _policyTables = [];

        /// <summary>
        /// What each <c>[[policy]]</c> called itself, in declaration order. <b>For a panel, never
        /// for the city.</b>
        /// </summary>
        /// <remarks>
        /// ⚠ <b>The hash in <c>Ruleset.PolicyKeys</c> is the identity and this is not.</b> Saved
        /// state points at the hash; this is the string a person reads beside a dial, and
        /// <c>05 §1</c> has the shell resolving every such string through the Ruleset. An unnamed
        /// table stays <c>null</c> here, which is the same fact its zero key states.
        /// </remarks>
        private readonly List<string?> _policyNames = [];

        /// <summary>What each <c>[[zone_rule]]</c> called itself, in declaration order.</summary>
        /// <remarks>
        /// ⚠ <b>Carried for the same reason <see cref="_policyNames"/> is, and it was missing for the
        /// same reason.</b> Nothing inside a Ruleset refers to a Zone Rule, so it needs no id and the
        /// loader had no reason to keep the string — and then a panel offering a player a choice of
        /// brush could only say <em>zone rule 0, zone rule 1</em>. ***A name is needed by the person
        /// and not by the file.*** An unnamed table stays <c>null</c>; unlike a Policy that is not a
        /// refusal, because a brush is addressed by its permission word rather than by its name.
        /// </remarks>
        private readonly List<string?> _zoneRuleNames = [];
        private readonly List<TableSyntaxBase> _hinterlandTables = [];
        private readonly List<TableSyntaxBase> _latticeTables = [];
        private readonly List<TableSyntaxBase> _terrainTables = [];

        private TableSyntaxBase? _layersTable;
        private TableSyntaxBase? _placementTable;
        private TableSyntaxBase? _foundingTable;

        private TableSyntaxBase? _roadsTable;
        private TableSyntaxBase? _lotsTable;
        private TableSyntaxBase? _capacityTable;
        private TableSyntaxBase? _tripsTable;
        private TableSyntaxBase? _jobsTable;
        private TableSyntaxBase? _householdsTable;
        private TableSyntaxBase? _trafficTable;
        private TableSyntaxBase? _parkingTable;

        private TableSyntaxBase? _waterTable;
        private TableSyntaxBase? _disastersTable;
        private TableSyntaxBase? _districtsTable;

        private TableSyntaxBase? _marketTable;

        private TableSyntaxBase? _needsTable;

        /// <summary>The parsed document, kept only so <see cref="Surface"/> can walk it after.</summary>
        /// <remarks>
        /// ⚠ <b>A reference and not a second walk.</b> The key surface is wanted by one caller that
        /// runs off the hot path, so building it eagerly would put a whole-document traversal into
        /// every hot reload for nobody. Holding the reference costs the parse's own memory, which
        /// the caller is holding anyway until <c>Read</c> returns.
        /// </remarks>
        private DocumentSyntax? _document;

        public RulesetLoadResult Read()
        {
            DocumentSyntax document = SyntaxParser.Parse(text, fileName, validate: true);

            _document = document;

            foreach (DiagnosticMessage message in document.Diagnostics)
            {
                Refuse(message.Span.Start.Line + 1, null, message.Message);
            }

            if (document.HasErrors)
            {
                return RulesetLoadResult.Refused(_refusals);
            }

            // Refusal 3, and it runs over the whole document before anything is interpreted. A float
            // is a lexical fact rather than a semantic one, so nothing is gained by waiting until the
            // key it sits under is understood -- and a decimal in a section this build does not read
            // yet is still a decimal.
            RefuseUnquotedDecimals(document);

            // Readouts are declared simulation-side (02 §4.1), so their names would be registered
            // here and their ids live in ReadoutId. Slice 7 task 7 is where that happens; until then
            // every `derived` name refuses as unknown, which is the correct answer rather than a
            // provisional one -- no Readouts are declared, so none can be named.
            Enumerate(document);

            // FIRST, because ReadBusinessKinds asks it whether this Ruleset employs anybody before
            // it decides whether a shift band and a wage are owed. It reads no other table.
            CapacityRuleset capacity = ReadCapacity();

            RuleDefinition[] rules = ReadRules(out Term[] inputs, out Term[] outputs,
                out MapEmission[] emissions);
            KindDefinition[] kinds = ReadKinds(rules, inputs, outputs, out BinDeclaration[] bins,
                out RuleId[] kindRules);
            BusinessKindDefinition[] businessKinds = ReadBusinessKinds(capacity);
            LifeStageDefinition[] lifeStages = ReadLifeStages();
            ZoneRuleDefinition[] zoneRules = ReadZoneRules();
            BandDefinition[] bands = ReadBands();
            PolicyDefinition[] policies = ReadPolicies(out ulong[] policyKeys);
            HinterlandDefinition[] hinterlands = ReadHinterlands(out Money[] hinterlandPrices);
            LayerRuleset layers = ReadLayers();
            PlacementRuleset placement = ReadPlacement(kinds);
            RoadRuleset roads = ReadRoads();

            // After ReadRoads and not before: every refusal here is a property of an origin against
            // `block_tiles`, so the lattice tables cannot be read until the block is known.
            LatticeDefinition[] lattices = ReadLattices(roads);
            LotRuleset lots = ReadLots(roads);
            TripRuleset trips = ReadTrips();
            JobRuleset jobs = ReadJobs(trips);
            HouseholdRuleset households = ReadHouseholds();
            TrafficRuleset traffic = ReadTraffic();
            ParkingRuleset parking = ReadParking();
            TerrainRuleset terrain = ReadTerrain();
            WaterRuleset water = ReadWater();
            DisasterRuleset disasters = ReadDisasters(water);
            DistrictRuleset districts = ReadDistricts();
            MarketRuleset market = ReadMarket();

            // After ReadKinds, because whether an ATTENDED Need's rates are required is a property of
            // the pair: they are owed by a file declaring a kind that serves one and refused of a file
            // that declares none (adr/0130's rule for gives_up_after_days, one table along).
            NeedRuleset needs = ReadNeeds(kinds);
            // After ReadPlacement, because the founding pass rides its trigger and the refusal for a
            // file stating [founding] with no [placement] is a property of the pair.
            FoundingRuleset founding = ReadFounding(placement, businessKinds, capacity);

            // After both, because it is a property of the pair: a file with Districts in it has a
            // Pool to price, and adr/0050's anchor is the only thing bounding what that price
            // reaches. Neither table can see the defect alone.
            RefuseUnpricedGoods(districts, hinterlands, hinterlandPrices);

            if (_refusals.Count == 0)
            {
                // After every reader has run and before anything else is checked, because the
                // permitted set IS what the readers asked for -- there is nothing to compare against
                // until they have all been. Gated on a clean slate for the usual reason: a key on a
                // section that was itself refused would report the same mistake twice.
                RefuseUnknownKeys(document);
            }

            if (_refusals.Count == 0)
            {
                RefuseIncompleteGeneration(lifeStages, placement);
            }

            if (_refusals.Count == 0)
            {
                // Cycles first, and it is not a nicety: three of the checks below walk a chain to
                // its end, and a chain with a cycle in it is a walk that does not terminate.
                RefuseCycles(rules);
            }

            if (_refusals.Count == 0)
            {
                // The terminal check precedes the fills check, and the order carries a diagnosis. A
                // chain missing its terminal also fails the fills walk -- the last link relieves
                // nothing, so the intersection empties -- and "no Bin is relieved" would name the
                // head for a defect that belongs to the tail. The specific cause is reported first.
                RefuseUnterminatedChains(rules);
                RefuseUnrelievedChains(rules, inputs, outputs);
                RefuseUnbalancedMoney(rules, inputs, outputs);
            }

            if (_refusals.Count > 0)
            {
                return RulesetLoadResult.Refused(_refusals);
            }

            // The names are kept here and nowhere else, because here is the only place they exist.
            // 05 §1 has the shell resolving every human-readable string "through the Ruleset", and a
            // Ruleset is Borough.Core and holds none -- so these four maps were built while parsing
            // and dropped, and the resolution path the architecture assumes had no implementation.
            // See RulesetNames.
            var names = new RulesetNames(
                _kinds, _businessKinds, _conditions, _resources, _rules, _lifeStages, _policyNames,
                _zoneRuleNames);

            return RulesetLoadResult.Accepted(new Ruleset(
                    [.. _families], rules, kinds, inputs, outputs, emissions, bins, kindRules,
                    zoneRules)
                {
                    Layers = layers,
                    Placement = placement,
                    Roads = roads,
                    Lattices = lattices,
                    Lots = lots,
                    Capacity = capacity,
                    Bands = bands,
                    Trips = trips,
                    Jobs = jobs,
                    Households = households,
                    Traffic = traffic,
                    Policies = policies,
                    PolicyKeys = policyKeys,
                    Hinterlands = hinterlands,
                    HinterlandPrices = hinterlandPrices,
                    Parking = parking,
                    Terrain = terrain,
                    Water = water,
                    Disasters = disasters,
                    Districts = districts,
                    Market = market,
                    Needs = needs,
                    ResourceNeeds = [.. _resourceNeeds],
                    Founding = founding,
                    ResourceKeys = Keys(_resources),
                    KindKeys = Keys(_kinds),
                    BusinessKindCount = _businessKinds.Count,
                    BusinessKindKeys = Keys(_businessKinds),
                    LifeStageCount = _lifeStages.Count,
                    LifeStageKeys = Keys(_lifeStages),
                    LifeStages = lifeStages,
                    BusinessKinds = businessKinds,
                },
                names);
        }

        // ---- the walk -------------------------------------------------------------------------

        /// <summary>Assigns every name an id, so that the second pass can resolve forward references.</summary>
        /// <remarks>
        /// <b>Two passes, because <c>on_fail</c> points forward as often as backward.</b> A single
        /// pass would refuse a chain written top-down, which is the order a designer writes one in.
        /// </remarks>
        private void Enumerate(DocumentSyntax document)
        {
            foreach (TableSyntaxBase table in document.Tables)
            {
                string section = NameOf(table.Name);

                switch (section)
                {
                    case "resource":
                        Register(_resources, table, "resource", (ushort)(_resources.Count + 1));
                        _families.Add(ReadFamily(table));
                        _resourceNeeds.Add(ReadNeed(table));
                        RefuseStorage(table);
                        break;

                    case "building":
                        if (_kinds.Count >= byte.MaxValue)
                        {
                            Refuse(LineOf(table), null,
                                $"more than {byte.MaxValue - 1} Building kinds are declared, and a "
                                + "Building's kind column is one byte wide.");
                            break;
                        }

                        _kindTables.Add(table);
                        Register(_kinds, table, "building", (byte)(_kinds.Count + 1));
                        break;

                    case "business":
                        if (_businessKinds.Count >= byte.MaxValue)
                        {
                            Refuse(LineOf(table), null,
                                $"more than {byte.MaxValue - 1} Business kinds are declared, and a "
                                + "Business's kind column is one byte wide.");
                            break;
                        }

                        // Kept for a second pass as of milestone 27 task 7. Until then a [[business]]
                        // declared nothing but its name and what it bought was IDENTITY -- a Business
                        // row naming its trade and keeping that name across a reload -- so no table
                        // was retained and ReadBusinessKinds would have walked over nothing. It now
                        // reads TWO of adr/0141's three: `jobs` and the Shift band. The wage is
                        // adr/0026 at milestone 15 (06:99), so stating one here is refused BY NAME,
                        // with a message saying where the wage went. ⚠ This comment said "refused as
                        // an unknown key" while NO UNKNOWN-KEY CHECK EXISTED (plans/0041 G31, closed
                        // 2026-08-25) -- it described the outcome it wanted rather than the build,
                        // which is adr/0093's failure being wrong about the TRIGGER.
                        _businessKindTables.Add(table);
                        Register(_businessKinds, table, "business", (byte)(_businessKinds.Count + 1));
                        break;

                    case "life_stage":
                        if (_lifeStages.Count >= byte.MaxValue)
                        {
                            Refuse(LineOf(table), null,
                                $"more than {byte.MaxValue - 1} Life Stages are declared, and a "
                                + "Household's life_stage column is one byte wide.");
                            break;
                        }

                        // Registered into a name table, unlike [[zone_rule]] and [[policy]], because
                        // a stage IS referred to from inside the Ruleset: `next` names the stage this
                        // one exits to. adr/0011's chain is not a line -- Young exits to Family or to
                        // Childless -- so the successor is authored rather than taken from the order
                        // these tables appear in, and a name is what it is authored as.
                        _lifeStageTables.Add(table);
                        Register(_lifeStages, table, "life_stage", (byte)(_lifeStages.Count + 1));
                        break;

                    case "rule":
                        _ruleTables.Add(table);
                        Register(_rules, table, "rule", (ushort)(_rules.Count + 1));
                        break;

                    case "policy":
                        // Not registered into a name table, for the reason zone_rule is not: nothing
                        // in a Ruleset ever refers to a Policy, so it needs no id and a duplicate
                        // name is not an ambiguity. The name is carried into refusals only.
                        _policyTables.Add(table);
                        break;

                    case "zone_rule":
                        // Not registered into a name table: nothing in a Ruleset ever refers to a
                        // Zone Rule, so it needs no id. The name is carried for refusal messages
                        // only, which is why a duplicate one is not an ambiguity here.
                        _zoneRuleTables.Add(table);
                        break;

                    case "band":
                        // Not registered into a name table, for [[zone_rule]]'s reason: nothing in a
                        // Ruleset refers to a band by name. A band's identity is its POSITION, which
                        // is what a block's byte carries -- so the name is for refusal messages and
                        // for the person reading the file, and a duplicate name is not an ambiguity.
                        _bandTables.Add(table);
                        break;

                    case "layers":
                        // Singular and optional. A second one is TOML's own error for [layers] and
                        // a legal array-of-table for [[layers]], so the ambiguity is caught here
                        // rather than left to whichever of the two the reader happened to see last.
                        if (_layersTable is not null)
                        {
                            Refuse(LineOf(table), null,
                                "a second [layers] is declared. There is one set of Map Layers, so "
                                + "two tables of numbers for them is ambiguous rather than additive.");
                            break;
                        }

                        _layersTable = table;
                        break;

                    case "placement":
                        // Singular and optional, on [layers]' reasoning. A city has one way of
                        // housing the people waiting in it, so two tables of numbers for it is
                        // ambiguous rather than additive.
                        if (_placementTable is not null)
                        {
                            Refuse(LineOf(table), null,
                                "a second [placement] is declared. There is one placement pass, so "
                                + "two tables of numbers for it is ambiguous rather than additive.");
                            break;
                        }

                        _placementTable = table;
                        break;

                    case "roads":
                        // Singular and optional, on [placement]'s reasoning. A world has one road
                        // network, so two tables of numbers for it is ambiguous rather than additive.
                        if (_roadsTable is not null)
                        {
                            Refuse(LineOf(table), null,
                                "a second [roads] is declared. There is one Road Graph, so two "
                                + "tables of numbers for it is ambiguous rather than additive.");
                            break;
                        }

                        _roadsTable = table;
                        break;

                    case "capacity":
                        // Singular and optional, on [lots]'s reasoning exactly. A city holds one set
                        // of rates, so two tables of them is ambiguous rather than additive.
                        if (_capacityTable is not null)
                        {
                            Refuse(LineOf(table), null,
                                "a second [capacity] is declared. A city holds one set of rates, so "
                                + "two tables of them is ambiguous rather than additive.");
                            break;
                        }

                        _capacityTable = table;
                        break;

                    case "lots":
                        // Singular and optional, on [roads]'s reasoning exactly. Land is subdivided
                        // one way, so two tables of numbers for it is ambiguous rather than additive.
                        if (_lotsTable is not null)
                        {
                            Refuse(LineOf(table), null,
                                "a second [lots] is declared. Land is subdivided one way, so two "
                                + "tables of numbers for it is ambiguous rather than additive.");
                            break;
                        }

                        _lotsTable = table;
                        break;

                    case "trips":
                        // Singular and optional, on [lots]'s reasoning exactly. A city has one Trip
                        // model, so two tables of numbers for it is ambiguous rather than additive.
                        if (_tripsTable is not null)
                        {
                            Refuse(LineOf(table), null,
                                "a second [trips] is declared. There is one Trip model, so two "
                                + "tables of numbers for it is ambiguous rather than additive.");
                            break;
                        }

                        _tripsTable = table;
                        break;

                    case "jobs":
                        // Singular and optional, on [placement]'s reasoning exactly. There is one
                        // assignment pass, so two cadences for it is ambiguous rather than additive.
                        if (_jobsTable is not null)
                        {
                            Refuse(LineOf(table), null,
                                "a second [jobs] is declared. There is one job assignment pass, so "
                                + "two tables of numbers for it is ambiguous rather than additive.");
                            break;
                        }

                        _jobsTable = table;
                        break;

                    case "households":
                        // Singular and optional, on [jobs]' reasoning exactly.
                        if (_householdsTable is not null)
                        {
                            Refuse(LineOf(table), null,
                                "a second [households] is declared. There is one population, so two "
                                + "tables of numbers for it is ambiguous rather than additive.");
                            break;
                        }

                        _householdsTable = table;
                        break;

                    case "traffic":
                        // Singular and optional, on [jobs]' reasoning exactly.
                        if (_trafficTable is not null)
                        {
                            Refuse(LineOf(table), null,
                                "a second [traffic] is declared. There is one volume-delay function, "
                                + "so two tables of numbers for it is ambiguous rather than additive.");
                            break;
                        }

                        _trafficTable = table;
                        break;

                    case "hinterland":
                        // Not registered into a name table, for [[policy]]'s reason: nothing in a
                        // Ruleset refers to a Hinterland. It is reached by the map edge a gate
                        // stands on, and the duplicate that matters is a duplicate EDGE rather than
                        // a duplicate name -- which is not visible until the key is read, so it is
                        // refused in ReadHinterlands and not here.
                        _hinterlandTables.Add(table);
                        break;

                    case "lattice":
                        // Not registered into a name table, on [[hinterland]]'s reasoning, and a
                        // Lattice does not even carry a name to register. What it carries is an
                        // origin, and a duplicate origin is not visible until block_tiles is known --
                        // so it is refused in ReadLattices and not here.
                        _latticeTables.Add(table);
                        break;

                    case "terrain":
                        // Not registered into a name table, on [[lattice]]'s reasoning: nothing in a
                        // Ruleset refers to a terrain type by name. The name here selects a member of
                        // a CLOSED enum rather than declaring one -- TerrainKind ships five and a
                        // file cannot add a sixth -- so an unknown name and a duplicate name are both
                        // refused in ReadTerrain, where the set to check against is in scope.
                        _terrainTables.Add(table);
                        break;

                    case "parking":
                        // Singular and optional, on [traffic]' reasoning exactly.
                        if (_parkingTable is not null)
                        {
                            Refuse(LineOf(table), null,
                                "a second [parking] is declared. There is one Parking Shed radius, "
                                + "so two tables of numbers for it is ambiguous rather than additive.");
                            break;
                        }

                        _parkingTable = table;
                        break;

                    case "water":
                        // Singular and optional, on [parking]'s reasoning exactly. There is one sea
                        // level, so two tables stating it is ambiguous rather than additive -- and a
                        // world with two seas at different heights is not a thing adr/0034's two
                        // numbers can describe.
                        if (_waterTable is not null)
                        {
                            Refuse(LineOf(table), null,
                                "a second [water] is declared. There is one sea level, so two tables "
                                + "of numbers for it is ambiguous rather than additive.");
                            break;
                        }

                        _waterTable = table;
                        break;

                    case "disasters":
                        // Singular and optional, on [water]'s reasoning one rung along. There is one
                        // Acts of God interval (01 §5.4), so two tables stating it is ambiguous
                        // rather than additive. ⚠ PLURAL IN THE KEY AND SINGULAR AS A TABLE, like
                        // [districts]: it configures the whole class of them and authors none, because
                        // a Disaster is scheduled by the world rather than declared by a designer.
                        if (_disastersTable is not null)
                        {
                            Refuse(LineOf(table), null,
                                "a second [disasters] is declared. There is one Acts of God interval, "
                                + "so two tables of numbers for it is ambiguous rather than additive.");
                            break;
                        }

                        _disastersTable = table;
                        break;

                    case "districts":
                        // Singular and optional, on [parking]'s reasoning exactly. Plural in the key
                        // because the table describes the whole set of them and authors none: there
                        // is no [[district]] and there must not be one, since adr/0134 makes a
                        // District a thing the city is found to have rather than a thing it is told.
                        if (_districtsTable is not null)
                        {
                            Refuse(LineOf(table), null,
                                "a second [districts] is declared. There is one prominence threshold, "
                                + "so two tables of numbers for it is ambiguous rather than additive.");
                            break;
                        }

                        _districtsTable = table;
                        break;

                    case "founding":
                        // Singular and optional. ⚠ NAMED [founding] AND NOT [business] BECAUSE THE
                        // SWITCH IS ON THE NAME ALONE: [[business]] already declares a trade, and a
                        // singular [business] would land in that case and be read as a kind with no
                        // name. The section is named for the MECHANISM rather than the entity, which
                        // also reads better -- it configures a channel, not a shop.
                        if (_foundingTable is not null)
                        {
                            Refuse(LineOf(table), null,
                                "a second [founding] is declared. There is one founding channel, so "
                                + "two tables of numbers for it is ambiguous rather than additive.");
                            break;
                        }

                        _foundingTable = table;
                        break;

                    case "needs":
                        // Singular and optional, on [market]'s reasoning. A Need's rates are a
                        // property of the city rather than of a District or a Household, so a second
                        // table would be a second tempo for one accumulator.
                        if (_needsTable is not null)
                        {
                            Refuse(LineOf(table), null,
                                "a second [needs] is declared. There is one set of Need rates, so "
                                + "two tables of numbers for it is ambiguous rather than additive.");
                            break;
                        }

                        _needsTable = table;
                        break;

                    case "market":
                        // Singular and optional, on [districts]' reasoning exactly. There is one
                        // damping, shared by every District and every Good: adr/0135 makes the price
                        // per (District, Good) and the SPEED of it a property of the city, so a
                        // second table would be a second tempo for one market.
                        if (_marketTable is not null)
                        {
                            Refuse(LineOf(table), null,
                                "a second [market] is declared. There is one damping, so two tables "
                                + "of numbers for it is ambiguous rather than additive.");
                            break;
                        }

                        _marketTable = table;
                        break;

                    default:
                        Refuse(LineOf(table), null,
                            $"'{section}' is not a Ruleset section. The sections are "
                            + "[needs], "
                            + "[[resource]], [[building]], [[business]], [[rule]], [[zone_rule]], "
                            + "[[policy]], [[hinterland]], [[lattice]], [[terrain]], [layers], "
                            + "[placement], [roads], [lots], [trips], [jobs], [households], "
                            + "[traffic], [parking], [water], [districts], [market] and "
                            + "[founding]. A trade is declared with [[business]] and the founding "
                            + "channel is configured with [founding]; they are different tables.");
                        break;
                }
            }
        }

        private void Register<TId>(
            Dictionary<string, TId> into, TableSyntaxBase table, string section, TId id)
            where TId : struct
        {
            if (!TryString(table, "name", out string? name, required: true))
            {
                return;
            }

            if (!into.TryAdd(name!, id))
            {
                Refuse(LineOf(table), name,
                    $"a second [[{section}]] is named '{name}'. Names are how a Ruleset refers to "
                    + "itself, so two of one name is ambiguous rather than redundant.");
            }
        }

        // ---- rules ----------------------------------------------------------------------------

        private RuleDefinition[] ReadRules(
            out Term[] inputs, out Term[] outputs, out MapEmission[] emissions)
        {
            var definitions = new RuleDefinition[_ruleTables.Count];
            List<Term> allInputs = [];
            List<Term> allOutputs = [];
            List<MapEmission> allEmissions = [];

            for (int i = 0; i < _ruleTables.Count; i++)
            {
                TableSyntaxBase table = _ruleTables[i];
                string? name = TryString(table, "name", out string? found, required: false)
                    ? found
                    : null;

                byte kind = 0;

                if (TryString(table, "kind", out string? kindName, required: true, rule: name)
                    && !_kinds.TryGetValue(kindName!, out kind))
                {
                    Refuse(LineOf(table), name,
                        $"kind '{kindName}' is not a declared [[building]].");
                }

                int inputFirst = allInputs.Count;
                ReadTerms(table, "inputs", name, allInputs, emissions: null);

                int outputFirst = allOutputs.Count;
                int emissionFirst = allEmissions.Count;
                ReadTerms(table, "outputs", name, allOutputs, allEmissions);

                definitions[i] = new RuleDefinition(
                    Kind: kind,
                    Rate: ReadRate(table, name),
                    Apply: ReadApply(table, name),
                    OnFail: ReadOnFail(table, name),
                    HasFills: TryReadFills(table, name, out BinRef fills),
                    Fills: fills,
                    Reports: ReadReports(table, name),
                    InputFirst: inputFirst,
                    InputCount: allInputs.Count - inputFirst,
                    OutputFirst: outputFirst,
                    OutputCount: allOutputs.Count - outputFirst,
                    EmissionFirst: emissionFirst,
                    EmissionCount: allEmissions.Count - emissionFirst);
            }

            inputs = [.. allInputs];
            outputs = [.. allOutputs];
            emissions = [.. allEmissions];

            return definitions;
        }

        /// <summary>
        /// Reads <c>wage_per_day</c> and <c>pay_period_days</c>, which are required together or not
        /// at all.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The pairing is <see cref="ReadShiftStartBand"/>'s, for the same reason and with one
        /// difference.</b> A rate with no period never pays and a period with no rate pays nothing,
        /// so each alone is half a mechanism — and unlike the Shift band, <b>zero is not ambiguous
        /// here</b>: a trade that pays nothing is a real trade, so the keys are optional together and
        /// their absence means exactly that. ***What is refused is stating one and not the other.***
        /// </para>
        /// <para>
        /// ⚠ <b>A wage with no <c>jobs</c> is refused</b>, on the band's argument word for word: a
        /// rate is what a job pays, so it means nothing on a trade that employs nobody.
        /// </para>
        /// </remarks>
        private (int WagePerDay, int PayPeriodDays) ReadWage(
            TableSyntaxBase table, string? name, int jobs)
        {
            bool hasRate = TryInteger(table, "wage_per_day", out long rate, required: false, name);
            bool hasPeriod = TryInteger(
                table, "pay_period_days", out long period, required: false, name);

            if (!hasRate && !hasPeriod)
            {
                return (0, 0);
            }

            if (jobs <= 0)
            {
                Refuse(LineOf((SyntaxNodeBase?)Find(table, "wage_per_day") ?? table), name,
                    "this trade states a wage and employs nobody. A wage is what one job pays for "
                    + "one Day worked, so it means nothing without a `jobs` above zero -- state "
                    + "one, or delete the wage.");

                return (0, 0);
            }

            if (!hasRate || !hasPeriod)
            {
                Refuse(LineOf(table), name,
                    "this trade states one of `wage_per_day` and `pay_period_days` and not the "
                    + "other. They are required together: a rate with no period never pays anybody "
                    + "and a period with no rate pays nothing, so each on its own is half a "
                    + "mechanism that loads clean and does nothing.");

                return (0, 0);
            }

            if (rate < 0)
            {
                Refuse(LineOf((SyntaxNodeBase?)Find(table, "wage_per_day") ?? table), name,
                    $"wage_per_day is {rate}. It is what one job pays for one Day worked, so it "
                    + "cannot be negative -- a negative wage is the worker paying the employer. "
                    + "Omit both keys for a trade that pays nothing.");

                return (0, 0);
            }

            if (period <= 0)
            {
                Refuse(LineOf((SyntaxNodeBase?)Find(table, "pay_period_days") ?? table), name,
                    $"pay_period_days is {period}. It is how many Days pass between paydays, so it "
                    + "is at least 1 (paid daily); 0 is a payday that never comes round.");

                return (0, 0);
            }

            return ((int)(rate > int.MaxValue ? int.MaxValue : rate),
                (int)(period > int.MaxValue ? int.MaxValue : period));
        }

        private uint ReadRate(TableSyntaxBase table, string? rule)
        {
            if (!TryInteger(table, "rate", out long rate, required: true, rule))
            {
                return 1;
            }

            // ✅ RAISED from WHEEL_SIZE to the coarse wheel's ceiling, plans/0046 stage 0. The old
            // bound was a Day, so a Ruleset could not state a rule that runs weekly -- which is why
            // provisioned.toml's `rates` levy carries a header explaining that `rate = 1024` IS HALF A
            // DAY BECAUSE A DAY IS NOT EXPRESSIBLE, and why WageEngine implements a weekly payday as a
            // modulo over a daily sweep instead of as an arming. ***Both are workarounds for this
            // line***, and plans/0036 decision 2 asked whether a multi-Day rate was real demand or
            // invented. They are the answer: two shipped mechanisms already wanted it.
            if (rate < 1 || rate >= Borough.Core.Rules.EventWheel.CoarseCeilingTicks)
            {
                Refuse(LineOf((SyntaxNodeBase?)Find(table, "rate") ?? table), rule,
                    $"rate {rate} is outside 1..{Borough.Core.Rules.EventWheel.CoarseCeilingTicks - 1}"
                    + " Ticks. A rate is a reschedule interval, and one at or beyond the coarse "
                    + $"wheel's period ({Borough.Core.Rules.EventWheel.CoarseDays - 1} Days) would "
                    + "re-arm into the bucket it just came off.");
                return 1;
            }

            return (uint)rate;
        }

        /// <summary>
        /// Which Need a Building of this kind is attended for — <c>serves</c>, optional.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The mirror of <see cref="ReadNeed"/>, and between them they are <c>04 §1</c>'s Good →
        /// Need diagram made total.</b> A Need is fed by <em>buying</em> or by <em>attending</em>
        /// (<c>adr/0032</c>), so every Need has exactly one door and each of these two refuses the
        /// other's by name. ***A designer who writes the wrong one is told which one they wanted,
        /// which is the whole reason both refusals are named rather than generic.***
        /// </para>
        /// <para>
        /// ⚠ <b><c>fire</c>, <c>police</c>, <c>power</c>, <c>water</c> and <c>sewage</c> are refused
        /// by name and they are not Needs at all.</b> <c>adr/0032</c> sorts Services by <em>who
        /// moves</em>: those five are Dispatched or Networked, nobody attends them, and a Household
        /// carries no scalar for any of them. A generic <em>not a Need</em> would read as
        /// <em>not yet</em>, which under <c>adr/0070</c> is the difference between evidence and an
        /// absence.
        /// </para>
        /// <para>
        /// ⚠ <b><c>recreation</c> is Attended and is still refused</b>, because <c>adr/0032</c> puts
        /// it somewhere else: <em>"Recreation needs no new machinery at all … widen Business to
        /// destination and a park is an Amenity entry."</em> It is a term in desirability, not a
        /// Household scalar, so a column here would be a second home for one quantity.
        /// </para>
        /// </remarks>
        private Need ReadServes(TableSyntaxBase table, string? name)
        {
            if (!TryString(table, "serves", out string? serves, required: false, name))
            {
                return Need.None;
            }

            switch (serves)
            {
                case "education":
                    return Need.Education;

                case "health":
                    return Need.Health;

                case "sustenance":
                case "satisfaction":
                    Refuse(LineOf((SyntaxNodeBase?)Find(table, "serves") ?? table), name,
                        $"serves is \"{serves}\", which is a Need a Household BUYS rather than one it "
                        + "attends (adr/0032 sorts Services by who moves). It is fed by a Good, so it "
                        + "is declared on the Resource as [[resource]] need and reaches the Household "
                        + "through a Rule firing. The two a Building can be attended for are "
                        + "education and health.");
                    return Need.None;

                case "fire":
                case "police":
                    Refuse(LineOf((SyntaxNodeBase?)Find(table, "serves") ?? table), name,
                        $"serves is \"{serves}\", which is a DISPATCHED Service and not a Need. "
                        + "adr/0032 sorts Services by who moves: nobody attends a fire station, the "
                        + "station travels to the incident, so no Household carries a scalar for it "
                        + "and there is nothing here for it to feed. Its successor is adr/0030's "
                        + "dispatch Trip, which is unbuilt (adr/0070) rather than refused. The two a "
                        + "Building can be attended for are education and health.");
                    return Need.None;

                case "power":
                case "water":
                case "sewage":
                    Refuse(LineOf((SyntaxNodeBase?)Find(table, "serves") ?? table), name,
                        $"serves is \"{serves}\", which is a NETWORKED Service and not a Need. "
                        + "adr/0032: nobody moves -- it flows over the District graph as adr/0031's "
                        + "one Resource abstraction -- so it is a Good reaching a Building and never "
                        + "a scalar a Household carries. The two a Building can be attended for are "
                        + "education and health.");
                    return Need.None;

                case "recreation":
                    Refuse(LineOf((SyntaxNodeBase?)Find(table, "serves") ?? table), name,
                        "serves is \"recreation\". It IS attended, and adr/0032 still puts it "
                        + "somewhere else: \"Recreation needs no new machinery at all -- widen "
                        + "Business to destination and a park is an Amenity entry.\" It is a term in "
                        + "desirability rather than a Household scalar, so a Need column for it "
                        + "would be a second home for one quantity. The two a Building can be "
                        + "attended for are education and health.");
                    return Need.None;

                default:
                    Refuse(LineOf((SyntaxNodeBase?)Find(table, "serves") ?? table), name,
                        $"serves is \"{serves}\", which is not a Need. Stating it is what makes a "
                        + "kind a service Building, and adr/0103 closes the Need set at four: "
                        + "sustenance, satisfaction, education and health. Only the last two are "
                        + "attended (adr/0032), so only they may be declared here -- omit the key "
                        + "for a kind that is not a service.");
                    return Need.None;
            }
        }

        /// <summary>Which of <c>adr/0103</c>'s Needs this Resource feeds — <c>need</c>, optional.</summary>
        /// <remarks>
        /// <para>
        /// <b>Optional, and absent is the ordinary case</b>: a Resource feeds no Need unless a
        /// designer says it does, which is every Resource in every file shipped before milestone 28.
        /// </para>
        /// <para>
        /// ⚠ <b><c>education</c> and <c>health</c> are refused BY NAME rather than falling through to
        /// the unknown-value message.</b> They are two of the four Needs and a designer who writes one
        /// has read <c>adr/0103</c>, so the refusal owes them the reason: those two are consumed by
        /// attending rather than by buying. ***A generic "not a Need" would tell them the opposite of
        /// the truth.***
        /// </para>
        /// <para>
        /// 🔴 <b>The refusal SURVIVED and its reason did not.</b> It used to end <em>"its degradation
        /// rule is owed and DELIBERATELY UNDESIGNED … a Resource cannot feed it until somebody designs
        /// what attending costs"</em>, and <c>docs/deferred.md</c> named the trigger that would end
        /// that: a civic Building a Household draws on. <see cref="ReadServes"/> is it. So the key is
        /// refused here for the same reason as ever — a Resource is the wrong door — and the message
        /// now names the right one instead of naming an absence. ***A refusal that points somewhere is
        /// a different sentence from one that only says no.***
        /// </para>
        /// </remarks>
        private Need ReadNeed(TableSyntaxBase table)
        {
            if (!TryString(table, "need", out string? need, required: false))
            {
                return Need.None;
            }

            switch (need)
            {
                case "sustenance":
                    return Need.Sustenance;

                case "satisfaction":
                    return Need.Satisfaction;

                case "education":
                case "health":
                    Refuse(LineOf(table), need,
                        $"'{need}' is one of adr/0103's four Needs and it has no Good behind it. It "
                        + "is consumed by ATTENDING rather than by buying, so 04 section 1's Good to "
                        + "Need diagram is not a total map -- and the door it wants is the other "
                        + $"one: declare a service Building with [[building]] serves = \"{need}\", "
                        + "and a Household's Trip to it is the occasion (adr/0032). The two a Good "
                        + "can feed are sustenance and satisfaction.");
                    return Need.None;

                default:
                    Refuse(LineOf(table), need,
                        $"'{need}' is not a Need. A Need exists for each exhaustible thing a "
                        + "Household consumes and can be refused on arrival, and adr/0103 closes the "
                        + "set at four: sustenance, satisfaction, education and health. Only the "
                        + "first two have a Good behind them and only they may be declared here. "
                        + "Everything standing -- the commute, the rent, the neighbourhood -- is a "
                        + "term in 02 section 5.4's utility and never a Need.");
                    return Need.None;
            }
        }

        /// <summary>The <c>[needs]</c> table — how far a Need moves, and how deep it may go.</summary>
        /// <remarks>
        /// <para>
        /// <b>Every BOUGHT key required of a file that states the table</b>, on <c>[market]</c>'s and
        /// <c>[districts]</c>' rule: a stated table states its keys, and a defaulted rate is a
        /// hash-bearing number nobody chose. ⚠ <b>Absent means no Household in this city has a Need
        /// at all</b>, which is reached by omitting the table rather than by zeroing it.
        /// </para>
        /// <para>
        /// ⚠ <b>The two ATTENDED pairs are conditional instead — required of a file that declares a
        /// kind serving them, and refused of one that does not.</b> That is
        /// <c>[placement] gives_up_after_days</c>'s rule taken whole (<c>adr/0130</c>): required of
        /// any Ruleset declaring a gate kind and refused elsewhere. ***The unconditional form would
        /// have made <c>rulesets/hungry.toml</c> author a school's rates to demonstrate hunger***,
        /// and a number authored to satisfy a loader is the one nobody chose.
        /// </para>
        /// </remarks>
        private NeedRuleset ReadNeeds(KindDefinition[] kinds)
        {
            if (_needsTable is null)
            {
                return NeedRuleset.None;
            }

            if (!TryInteger(_needsTable, "sustenance_degrade", out long sd, required: true)
                || !TryInteger(_needsTable, "sustenance_recover", out long sr, required: true)
                || !TryInteger(_needsTable, "satisfaction_degrade", out long ad, required: true)
                || !TryInteger(_needsTable, "satisfaction_recover", out long ar, required: true)
                || !TryInteger(_needsTable, "floor", out long floor, required: true))
            {
                return NeedRuleset.None;
            }

            if (!TryAttendedRates(kinds, Need.Education, "education", out long ed, out long er)
                || !TryAttendedRates(kinds, Need.Health, "health", out long hd, out long hr))
            {
                return NeedRuleset.None;
            }

            foreach ((string key, long value) in
                new[] { ("sustenance_degrade", sd), ("sustenance_recover", sr),
                        ("satisfaction_degrade", ad), ("satisfaction_recover", ar) })
            {
                if (value <= 0)
                {
                    Refuse(LineOfNeeds(key), null,
                        $"{key} is {value}. It is how far a Need moves on one occasion, stated as a "
                        + "positive step whose DIRECTION is the mechanism's rather than the file's: a "
                        + "degrade always falls and a recover always rises. Zero would be a Need that "
                        + "never moves, which is the table omitted, and a negative would be a failed "
                        + "occasion that fed somebody.");

                    return NeedRuleset.None;
                }
            }

            // A Need is a relative scalar where 0 is ideal and negative is deficit (CONTEXT.md), so
            // the floor is the DEEPEST it goes and is therefore negative. It is required because an
            // unbounded magnitude is adr/0006 as adr/0003 extends it to quantities -- the one thing a
            // long run is written to catch, and it would fail on the Need rather than on this file.
            if (floor >= 0)
            {
                Refuse(LineOfNeeds("floor"), null,
                    $"floor is {floor}. A Need is a relative scalar where 0 is ideal and negative "
                    + "values are deficit, so the floor is the deepest deficit reachable and is "
                    + "negative. Zero would mean no Household can ever be short of anything, which is "
                    + "the table omitted.");

                return NeedRuleset.None;
            }

            return new NeedRuleset(
                (int)sd, (int)sr, (int)ad, (int)ar, (int)ed, (int)er, (int)hd, (int)hr, (int)floor);
        }

        /// <summary>
        /// One Attended Need's pair of rates — required where a kind serves it, refused where none
        /// does.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Both directions are load-bearing and neither is tidiness.</b> A file declaring a school
        /// and stating no <c>education_degrade</c> is a city whose schools cannot matter — the
        /// Building stands, the Trips are made, and nothing moves — which is exactly the
        /// <em>loads clean and does nothing</em> class <c>plans/0014</c> task 3 established. A file
        /// stating the rates and declaring no school is the mirror: a hash-bearing number nobody can
        /// reach, which reads on the page as a mechanism the world has.
        /// </para>
        /// <para>
        /// ⚠ <b>The kinds are walked rather than a flag being consulted, because this runs before
        /// there is a <c>Ruleset</c> to ask.</b> <c>ReadKinds</c> has returned by here and
        /// <c>Ruleset.ServesAny</c> has not been built yet, so this is the same walk one construction
        /// step early — and the two must agree, which is what
        /// <c>ServiceRulesetLoadTests</c> pins.
        /// </para>
        /// </remarks>
        private bool TryAttendedRates(
            KindDefinition[] kinds, Need need, string name, out long degrade, out long recover)
        {
            degrade = 0;
            recover = 0;

            bool served = false;

            for (int i = 0; i < kinds.Length; i++)
            {
                if (kinds[i].Serves == need)
                {
                    served = true;
                    break;
                }
            }

            string degradeKey = name + "_degrade";
            string recoverKey = name + "_recover";

            if (!served)
            {
                foreach (string key in new[] { degradeKey, recoverKey })
                {
                    if (Find(_needsTable!, key) is not null)
                    {
                        Refuse(LineOfNeeds(key), null,
                            $"{key} is stated and no [[building]] declares serves = \"{name}\". "
                            + $"{name} is an ATTENDED Need (adr/0032) -- it moves when a Household "
                            + "travels to a service Building and there is none to travel to -- so "
                            + "this rate is a hash-bearing number nothing can reach, which reads on "
                            + "the page as a mechanism this world has. Declare the kind, or drop the "
                            + "key.");

                        return false;
                    }
                }

                return true;
            }

            if (!TryInteger(_needsTable!, degradeKey, out degrade, required: true)
                || !TryInteger(_needsTable!, recoverKey, out recover, required: true))
            {
                return false;
            }

            foreach ((string key, long value) in new[] { (degradeKey, degrade), (recoverKey, recover) })
            {
                if (value <= 0)
                {
                    Refuse(LineOfNeeds(key), null,
                        $"{key} is {value}. It is how far {name} moves, stated as a positive step "
                        + "whose DIRECTION is the mechanism's rather than the file's: a degrade "
                        + "always falls and a recover always rises. Zero would be a Need that never "
                        + "moves, and this Ruleset declares a kind serving it -- so the Building "
                        + "would stand, the Trips would be made, and nothing would happen.");

                    return false;
                }
            }

            return true;
        }

        private int LineOfNeeds(string key) =>
            _needsTable is null ? 0
            : Find(_needsTable, key) is KeyValueSyntax entry ? LineOf(entry)
            : LineOf(_needsTable);

        /// <summary>
        /// Reads a Resource's family, which is what tells the loader money from flour.
        /// </summary>
        /// <remarks>
        /// <b>Required, with no default</b>, and <c>good</c> is the tempting one to default to. It
        /// would be wrong in the direction that does not announce itself: a Resource silently filed as
        /// a Good is conserved by nothing, ceilinged like a warehouse, and shipped by Vehicle — three
        /// behaviours a designer never asked for and would debug as balance.
        /// </remarks>
        private ResourceFamily ReadFamily(TableSyntaxBase table)
        {
            if (!TryString(table, "family", out string? family, required: true))
            {
                return ResourceFamily.None;
            }

            switch (family)
            {
                case "good":
                    return ResourceFamily.Good;

                case "utility":
                    return ResourceFamily.Utility;

                case "money":
                    return ResourceFamily.Money;

                default:
                    Refuse(LineOf(table), family,
                        $"'{family}' is not a Resource family. The families are good (moves as a "
                        + "Shipment, on the Road Graph, in the traffic), utility (flows along the "
                        + "District adjacency graph) and money (conserved, and does not move at all). "
                        + "The family decides transport and whether the Bin has a ceiling, so there "
                        + "is no default.");
                    return ResourceFamily.None;
            }
        }

        /// <summary>
        /// <b>The named hole for <c>CONTEXT</c> → Resource's second distinguishing parameter.</b>
        /// </summary>
        /// <remarks>
        /// <b>Storage — whether a Bin carries over between periods — is the axis this slice did not
        /// build, and refusing it is the house form for a hole rather than ignoring it.</b> Zero for
        /// Power, which is what <em>"there is no electricity warehouse"</em> means; large for Water;
        /// filling for Sewage and Waste. Every one of those is a per-Tick behaviour on the Bin, and
        /// with none of them implemented an accepted-and-dropped <c>storage = 0</c> would give a
        /// designer a Power Resource that warehouses electricity for ever and reads as a balance bug.
        /// </remarks>
        private void RefuseStorage(TableSyntaxBase table)
        {
            RefuseRetired(table, "storage", "storage",
                "storage is not implemented. It is CONTEXT → Resource's second parameter — whether a "
                + "Bin carries over between periods, zero for Power and filling for Waste — and "
                + "nothing reads it yet, so accepting it would silently give every Resource the "
                + "carry-over of a warehouse. Remove the key; the hole is named rather than hidden.");
        }

        /// <summary>Spells the entities a Readout is readable against, for a refusal message.</summary>
        /// <remarks>
        /// ⚠ <b>It lives here rather than beside <see cref="Readouts.IsReadableAgainst"/> because
        /// <c>adr/0002</c> puts it here.</b> It was written in <c>Borough.Core</c> first, as a
        /// <c>string</c> property next to the predicate, and <c>BoundaryTests</c> refused it: the shell
        /// owns every string a human reads, and a refusal message is read by a human. What crosses the
        /// boundary is <see cref="Readouts.Scopes"/> and the predicate; the sentence is built where the
        /// sentence is read.
        /// </remarks>
        private static string ScopesOf(ReadoutId id)
        {
            List<string> scopes = [];

            foreach (ReadoutScope scope in Readouts.Scopes)
            {
                if (Readouts.IsReadableAgainst(id, scope))
                {
                    scopes.Add(scope.ToString());
                }
            }

            return scopes.Count == 0 ? "nothing" : string.Join(", ", scopes);
        }

        /// <summary>
        /// An <c>apply</c> count, checked against the scope of the entity the Rule is attached to.
        /// </summary>
        /// <remarks>
        /// <b><paramref name="scope"/> is refusal 69, and it exists because the Sweep family gave a
        /// Readout a second entity to hang off.</b> Every Readout before <c>balance</c> was
        /// Building-scoped, so a Bin Rule could name any declared one and the question did not arise.
        /// A Policy sweeps Households, so <c>occupancy</c> in a <c>[[policy]]</c> and <c>balance</c>
        /// in a <c>[[rule]]</c> are both names of real Readouts with no row to read them from. The
        /// interpreter throws on either; the loader refuses it with a file and a line, which is
        /// <c>adr/0048</c>'s division of labour.
        /// </remarks>
        private ApplyCount ReadApply(
            TableSyntaxBase table, string? rule, ReadoutScope scope = ReadoutScope.Building)
        {
            KeyValueSyntax? entry = Find(table, "apply", RulesetKeyKind.Table);

            if (entry is null)
            {
                Refuse(LineOf(table), rule, "no apply count. A Rule declares apply = { min, max } "
                    + "or apply = { derived = \"<readout>\" }.");
                return ApplyCount.Band(1, 1);
            }

            if (entry.Value is not InlineTableSyntax inline)
            {
                Refuse(LineOf(entry), rule,
                    "apply must be an inline table: { min = 1, max = 4 } or { derived = \"...\" }.");
                return ApplyCount.Band(1, 1);
            }

            bool hasBand = Find(inline, "min") is not null || Find(inline, "max") is not null;
            KeyValueSyntax? derived = Find(inline, "derived");

            if (hasBand && derived is not null)
            {
                Refuse(LineOf(entry), rule,
                    "apply declares both a band and a derived count. 02 section 4.1: either "
                    + "{ min, max } or derived, never both -- below min is a failure and a derived "
                    + "zero is a success, so admitting both would collide their failure semantics.");
                return ApplyCount.Band(1, 1);
            }

            if (derived is not null)
            {
                if (!TryString(inline, "derived", out string? readout, required: true, rule))
                {
                    return ApplyCount.Band(1, 1);
                }

                if (!ReadoutNames.TryResolve(readout!, out ReadoutId id))
                {
                    Refuse(LineOf(derived), rule,
                        $"'{readout}' is not a declared Readout. The readable set is declared in the "
                        + "simulation (02 section 4.1), not in the Ruleset, so a name is refused here "
                        + $"rather than defaulted. Declared: {ReadoutNames.Declared}.");

                    return ApplyCount.Band(1, 1);
                }

                if (!Readouts.IsReadableAgainst(id, scope))
                {
                    Refuse(LineOf(derived), rule,
                        $"'{readout}' is not readable against a {scope}, and this Rule is attached to "
                        + $"one. The entities a Readout hangs off are part of its declaration (02 "
                        + "section 4.1), so this names a real quantity with no row here to read it "
                        + "from -- a Bin Rule runs on a Building and a Policy sweeps a population. "
                        + $"Readable against: {ScopesOf(id)}.");

                    return ApplyCount.Band(1, 1);
                }

                long percent = 100;

                if (Find(inline, "percent") is not null
                    && !TryInteger(inline, "percent", out percent, required: true, rule))
                {
                    return ApplyCount.Band(1, 1);
                }

                if (percent < 0 || percent > int.MaxValue)
                {
                    Refuse(LineOf(derived), rule,
                        $"percent = {percent} is not a percentage. It is at least 0, and 100 means "
                        + "one application per unit of the Readout.");

                    return ApplyCount.Band(1, 1);
                }

                return ApplyCount.From(id, (int)percent);
            }

            if (!TryInteger(inline, "min", out long min, required: true, rule)
                | !TryInteger(inline, "max", out long max, required: true, rule))
            {
                return ApplyCount.Band(1, 1);
            }

            if (min < 1 || max < min || max > int.MaxValue)
            {
                Refuse(LineOf(entry), rule,
                    $"apply band {{ min = {min}, max = {max} }} is not a band. min is at least 1 and "
                    + "max is at least min; min = max is the fixed case rather than a second form.");
                return ApplyCount.Band(1, 1);
            }

            return ApplyCount.Band((int)min, (int)max);
        }

        private RuleId ReadOnFail(TableSyntaxBase table, string? rule)
        {
            if (!TryString(table, "on_fail", out string? target, required: false, rule))
            {
                return RuleId.None;
            }

            if (!_rules.TryGetValue(target!, out ushort id))
            {
                Refuse(LineOf(Find(table, "on_fail")!), rule,
                    $"on_fail names '{target}', which is not a declared [[rule]].");
                return RuleId.None;
            }

            return new RuleId(id);
        }

        private ConditionId ReadReports(TableSyntaxBase table, string? rule)
        {
            if (!TryString(table, "reports", out string? condition, required: false, rule))
            {
                return ConditionId.None;
            }

            if (!_conditions.TryGetValue(condition!, out ushort id))
            {
                id = (ushort)(_conditions.Count + 1);
                _conditions[condition!] = id;
            }

            return new ConditionId(id);
        }

        private bool TryReadFills(TableSyntaxBase table, string? rule, out BinRef fills)
        {
            fills = default;
            KeyValueSyntax? entry = Find(table, "fills", RulesetKeyKind.Table);

            if (entry is null)
            {
                return false;
            }

            if (entry.Value is not InlineTableSyntax inline)
            {
                Refuse(LineOf(entry), rule,
                    "fills must be an inline table: { scope = \"local\", resource = \"flour\" }.");
                return false;
            }

            if (!TryScope(inline, rule, out Scope scope)
                || !TryResource(inline, rule, LineOf(entry), out ResourceId resource))
            {
                return false;
            }

            if (scope == Scope.Map)
            {
                Refuse(LineOf(entry), rule,
                    "fills names the map scope. A Map Layer cell is write-only and has no capacity, "
                    + "so nothing ever waits on one and nothing can rescue a wait on one.");
                return false;
            }

            fills = new BinRef(scope, resource);
            return true;
        }

        /// <summary>Reads one <c>inputs</c> or <c>outputs</c> array, splitting map writes out of it.</summary>
        private void ReadTerms(
            TableSyntaxBase table, string key, string? rule,
            List<Term> into, List<MapEmission>? emissions)
        {
            KeyValueSyntax? entry = Find(table, key, RulesetKeyKind.Array);

            if (entry is null)
            {
                return;
            }

            if (entry.Value is not ArraySyntax array)
            {
                Refuse(LineOf(entry), rule, $"{key} must be an array of inline tables.");
                return;
            }

            foreach (ArrayItemSyntax item in array.Items)
            {
                if (item.Value is not InlineTableSyntax inline)
                {
                    Refuse(LineOf(item), rule, $"every entry of {key} must be an inline table.");
                    continue;
                }

                if (!TryScope(inline, rule, out Scope scope)
                    || !TryInteger(inline, "amount", out long amount, required: true, rule))
                {
                    continue;
                }

                if (amount < 1 || amount > int.MaxValue)
                {
                    Refuse(LineOf(inline), rule,
                        $"amount {amount} is not a positive quantity. A Rule that moves nothing is "
                        + "a Rule with no term rather than a term of zero.");
                    continue;
                }

                if (scope == Scope.Map)
                {
                    ReadEmission(inline, key, rule, (int)amount, emissions);
                    continue;
                }

                if (!TryResource(inline, rule, LineOf(inline), out ResourceId resource))
                {
                    continue;
                }

                if (!GlobalNamesAConservedResource(inline, rule, scope, resource))
                {
                    continue;
                }

                into.Add(new Term(new BinRef(scope, resource), (int)amount));
            }
        }

        /// <summary>
        /// Refusal — a <c>global</c> term names the treasury, so its Resource must be a conserved one.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>This is <c>02 §4.3</c> enforced literally rather than a new rule.</b> That section says
        /// <c>global</c> <em>"names the treasury, and it appears only as the far end of an explicit
        /// transfer… That is the shape the loader accepts"</em> — and until milestone 10 the loader
        /// accepted every other shape as well, because <c>Scope.Global</c> threw in the Rule engine
        /// before any of them could reach a running world. <b>A scope that throws is a scope nothing
        /// has to validate</b>, and the throw had been doing the validating by accident. Making
        /// <c>global</c> resolve is what turned that into a hole.
        /// </para>
        /// <para>
        /// <b>Only the family half of that sentence is enforced.</b> It also says <em>local money
        /// out, global money in</em>, which is one direction, and the mechanism has two: a Policy
        /// paying out of the treasury is a <c>global</c> <em>input</em>, and <c>02 §4.2</c> asks for
        /// exactly that. So the direction is not checked — <b>a sentence describing the first use of
        /// a shape is not a specification of the shape</b>.
        /// </para>
        /// <para>
        /// <b><c>pool</c> is deliberately not refused beside it.</b> That scope is <em>unbuilt</em>
        /// rather than wrong (<c>adr/0070</c>) — it arrives with the District Pool — so refusing it
        /// here would refuse a file that is going to be legal, and the Rule engine's named hole is
        /// the right instrument for an absence with a date on it. This one is different in kind: a
        /// city-wide store of a Good is not a mechanism waiting to be built, and the treasury is
        /// fitted from the conserved Resources alone, so there is nothing for such a term to resolve
        /// to in any world this design describes.
        /// </para>
        /// </remarks>
        private bool GlobalNamesAConservedResource(
            InlineTableSyntax inline, string? rule, Scope scope, ResourceId resource)
        {
            if (scope != Scope.Global)
            {
                return true;
            }

            ResourceFamily family = _families[resource.Raw - 1];

            // None means this [[resource]] was already refused for its family. Reporting it again
            // here would name one mistake twice, and the second sentence would point at the Rule
            // rather than at the declaration that has to change.
            if (family is ResourceFamily.Money or ResourceFamily.None)
            {
                return true;
            }

            TryString(inline, "resource", out string? name, required: false, rule);

            Refuse(LineOf(inline), rule,
                "the global scope names the treasury, and a treasury holds conserved Resources only "
                + $"-- money and nothing else (adr/0024). '{name}' is declared as a "
                + $"{(family == ResourceFamily.Utility ? "utility" : "good")}, so no global Bin is "
                + "fitted for it at world creation and this term could never resolve. 02 section 4.3 "
                + "says what the global scope is: the far end of a transfer whose counterparty is "
                + "not a market.");

            return false;
        }

        private void ReadEmission(
            InlineTableSyntax inline, string key, string? rule, int amount,
            List<MapEmission>? emissions)
        {
            if (emissions is null)
            {
                Refuse(LineOf(inline), rule,
                    $"a map term appears in {key}. 02 section 4.1: the map scope is write-only, so a "
                    + "Rule can emit to a Layer and can never read or wait on one.");
                return;
            }

            if (!TryString(inline, "layer", out string? name, required: true, rule))
            {
                return;
            }

            if (!TryLayer(name!, out Layer layer))
            {
                Refuse(LineOf(inline), rule,
                    $"'{name}' is not a Map Layer. The Layers are pollution, land-value and sealing.");
                return;
            }

            // Naming a Layer that exists and cannot be emitted into is a different mistake from
            // naming one that does not exist, and it gets its own sentence. adr/0048 puts the
            // refusal here; RuleEngine.Emit threw on the first application, which is a refusal the
            // designer met as a crash on a file that had already loaded clean.
            if (!MapEmission.IsEmittable(layer))
            {
                Refuse(LineOf(inline), rule,
                    $"'{name}' is a Map Layer a Rule cannot emit into. Only pollution accumulates from "
                    + "a source; land value is chased towards a target and Sealing is a property of a "
                    + "footprint, so neither is a quantity a Rule adds per application.");
                return;
            }

            emissions.Add(new MapEmission(layer, amount));
        }

        // ---- building kinds -------------------------------------------------------------------

        /// <summary>
        /// A designer's duration in Days as the engine's duration in Ticks, saturating rather than
        /// wrapping.
        /// </summary>
        /// <remarks>
        /// <b>The whole of <c>adr/0048</c>'s division for the decline thresholds.</b> A Ruleset
        /// authors the felt quantity — <c>condemn_after_days</c>, <c>tenancy_ends_after_days</c> —
        /// and <c>ZoneRuleEngine</c> compares Ticks, so the multiplication happens exactly once, at
        /// the parse site, and no designer unit ever crosses into the core.
        /// <para>
        /// ⚠ <b>Saturating and not wrapping.</b> <c>int.MaxValue / Ticks.PerDay</c> is about 1.05
        /// million Days, or roughly 2,870 in-world years; a Ruleset authoring more than that means
        /// <i>never</i>, and clamping says so where an overflow would say <i>immediately</i> — the
        /// exact inversion the negative-value refusals above exist to prevent.
        /// </para>
        /// </remarks>
        private static int InTicks(long days)
        {
            long ticks = days * Ticks.PerDay;

            return ticks > int.MaxValue ? int.MaxValue : (int)ticks;
        }

        private KindDefinition[] ReadKinds(
            RuleDefinition[] rules, Term[] inputs, Term[] outputs,
            out BinDeclaration[] bins, out RuleId[] kindRules)
        {
            var definitions = new KindDefinition[_kindTables.Count];
            List<BinDeclaration> allBins = [];
            List<RuleId> allRules = [];

            var isLink = new bool[rules.Length];

            foreach (RuleDefinition rule in rules)
            {
                if (!rule.OnFail.IsNone)
                {
                    isLink[rule.OnFail.Raw - 1] = true;
                }
            }

            for (int i = 0; i < _kindTables.Count; i++)
            {
                TableSyntaxBase table = _kindTables[i];
                string? name = TryString(table, "name", out string? found, required: false)
                    ? found
                    : null;

                int binFirst = allBins.Count;
                ReadBins(table, name, allBins);

                // Whose Rule each of this kind's Rules is, derived from the Bins just read. It runs
                // here rather than in ReadRules because that pass has no kind to ask -- a Rule names
                // its kind, and the kind names the Bins, so the answer only exists once both have
                // been read.
                ApplyTenancies(rules, inputs, outputs, allBins, binFirst, i);

                // The kind's Rules are not declared on the kind: a Rule already names the kind it
                // runs on, and a second list would be the same fact twice with nothing keeping the
                // two in step.
                //
                // Heads only. A Rule that is some other Rule's on_fail is a link, and a link is
                // reached by walking a chain that failed -- never armed on its own rate. Arming one
                // would fire it independently of the head it exists to rescue, and a reporting
                // terminal armed this way reports for ever at its rate, which is adr/0045's polling
                // defect arriving through the back door.
                int ruleFirst = allRules.Count;

                for (int r = 0; r < rules.Length; r++)
                {
                    if (rules[r].Kind == i + 1 && !isLink[r])
                    {
                        allRules.Add(new RuleId((ushort)(r + 1)));
                    }
                }

                // 🔴 THE RENAMED KEY IS REFUSED BY NAME, AND THIS IS THE WHOLE REASON THE RENAME IS
                // SAFE. `condemn_after` counted missed firings (adr/0053); `condemn_after_days` is a
                // duration. The two units differ by the Rule's rate -- a factor of 16 on the shipped
                // upkeep Rule -- so a file that kept the old key and the old value would load clean
                // and decline SIXTEEN TIMES too slowly, which is a change nobody would attribute to
                // a rename. A silent unit change is plans/0012 Cause 5 arriving in content rather
                // than in prose, so the parse site says so out loud (adr/0048).
                RefuseRetired(table, "condemn_after", name,
                    $"'{name}' states condemn_after, which was renamed to condemn_after_days in "
                    + "milestone 17 AND CHANGED UNITS. It counted firings a starved Rule may "
                    + "miss; it is now how many Days a Rule may starve continuously. Four missed "
                    + "firings of a rate-16 Rule was 64 Ticks — 45 in-world minutes — so the old "
                    + "value does not carry over. Choose the duration you meant.");

                // The premises' threshold, in Days (adr/0059, adr/0130). Optional, and absent means
                // this kind never declines — which is what every Ruleset written before decline
                // existed already meant. A negative one is refused rather than clamped: it reads as
                // "condemn immediately", and a Ruleset that abandoned every Building it built would
                // be a sentence somebody meant to write and nobody would guess from the symptom.
                int condemnAfterTicks = 0;

                if (TryInteger(table, "condemn_after_days", out long declines, required: false, name))
                {
                    if (declines < 0)
                    {
                        Refuse(LineOf((SyntaxNodeBase?)Find(table, "condemn_after_days") ?? table), name,
                            $"condemn_after_days is {declines}. It is how many Days a Rule of this "
                            + "kind may starve continuously before the premises are condemned, so it "
                            + "cannot be negative; omit it for a kind that never declines.");
                    }
                    else
                    {
                        condemnAfterTicks = InTicks(declines);
                    }
                }

                // The TENANT's threshold, and it is deliberately independent of the one above
                // (adr/0141). A kind may state either, both or neither: `evicted.toml` states this
                // one alone, because its premises never fail and its tenants always do, and until
                // milestone 17 split the key that world was unwritable.
                int tenancyEndsAfterTicks = 0;

                if (TryInteger(table, "tenancy_ends_after_days", out long evicts, required: false, name))
                {
                    if (evicts < 0)
                    {
                        Refuse(LineOf((SyntaxNodeBase?)Find(table, "tenancy_ends_after_days") ?? table), name,
                            $"tenancy_ends_after_days is {evicts}. It is how many Days a tenant's own "
                            + "Rule may starve continuously before the tenancy ends, so it cannot be "
                            + "negative; omit it for a kind whose tenancies never end.");
                    }
                    else
                    {
                        tenancyEndsAfterTicks = InTicks(evicts);
                    }
                }

                // THE FIRST THRESHOLD (CONTEXT.md → Failure Pressure). Optional, and absent means a
                // Building of this kind loses no occupancy on its way down -- which is what every
                // Ruleset written before milestone 17 task 3 meant.
                //
                // ⚠ IT IS REFUSED AT OR ABOVE condemn_after_days, and that relation is the whole of
                // the check. The premises verdict is taken first, so a first threshold at or past the
                // second could never fire: abandonment empties the Building before the shedding
                // rung is ever reached, and the key would load clean and do nothing. ***A key that
                // can be authored into inertness is a key that will be*** -- adr/0130's reason for
                // refusing a stated zero, arriving as a relation between two numbers rather than as a
                // range on one, which is why it cannot be checked where the value is parsed.
                //
                // It pairs with the PREMISES key alone, for collapses_after_days' reason: shedding is
                // a verdict on the premises' own Rules (adr/0141), and a kind whose tenants fail
                // sheds nothing.
                int shedsAfterTicks = 0;

                bool statesSheds =
                    TryInteger(table, "sheds_occupant_after_days", out long sheds, required: false, name);

                if (statesSheds)
                {
                    if (sheds <= 0)
                    {
                        Refuse(LineOf((SyntaxNodeBase?)Find(table, "sheds_occupant_after_days") ?? table), name,
                            $"sheds_occupant_after_days is {sheds}. It is how many Days the premises "
                            + "may starve continuously before the Building sheds one Occupant, so it "
                            + "must be positive; omit it for a kind that sheds nobody.");
                    }
                    else if (condemnAfterTicks == 0)
                    {
                        Refuse(LineOf((SyntaxNodeBase?)Find(table, "sheds_occupant_after_days") ?? table), name,
                            $"'{name}' states sheds_occupant_after_days and no condemn_after_days. "
                            + "Shedding is paced off the premises' Failure Pressure, and a kind with "
                            + "no decline threshold has no pressure that is ever read — so the "
                            + "duration would never be consulted. State the threshold that makes it "
                            + "mean something, or omit this.");
                    }
                    else if (InTicks(sheds) >= condemnAfterTicks)
                    {
                        Refuse(LineOf((SyntaxNodeBase?)Find(table, "sheds_occupant_after_days") ?? table), name,
                            $"sheds_occupant_after_days is {sheds} against a condemn_after_days of "
                            + $"{declines}. The premises verdict is taken first, so a Building is "
                            + "abandoned before it could shed anything and this key would load clean "
                            + "and never fire. It must be strictly shorter than condemn_after_days.");
                    }
                    else
                    {
                        shedsAfterTicks = InTicks(sheds);
                    }
                }

                // The sink for what condemn_after_days creates. REQUIRED of a kind that can be
                // abandoned and REFUSED of one that cannot, which is adr/0130's disposition for
                // gives_up_after_days arriving on the other collection abandonment fills.
                //
                // ⚠ IT PAIRS WITH THE PREMISES KEY ONLY. A tenancy that ends leaves the Building
                // standing and occupied by nobody new until placement acts, so it creates no shell
                // and needs no sink -- which is why evicted.toml states neither this nor the
                // threshold above and is still bounded.
                //
                // 🔴 Measured rather than argued. Milestone 17 task 1 left the shell standing with no
                // sink, and a long run does not degrade -- it STOPS: every Building eventually fails,
                // every failure is permanent, and the city converts to dead shells. Zero jobs, a land
                // value field peaking at zero, a divide-by-zero in the placement pass. A Ruleset that
                // can author that is a Ruleset that can author adr/0006, so the refusal is here at
                // the parse site (adr/0048) rather than in an invariant that fires at Tick 100,000.
                // THE STOCK'S DEMAND-SIDE SINK, and it is deliberately NOT a second spelling of
                // condemn_after_days. That key reads Failure Pressure, so a kind stating it declines
                // whether anybody wants it or not; this one reads OCCUPANCY, so only surplus stock
                // dies. It is adr/0069's build predicate mirrored -- a developer builds while the
                // Unplaced Pool is non-empty and gives up on a Building the Pool never came for --
                // and it is what 02 §5.5 calls redevelopment's floor, the case where nobody wants the
                // land.
                //
                // ⚠ IT COUNTS HOUSEHOLDS AND NOT TENANTS. adr/0147 made `occupants` count tenants of
                // any kind and adr/0148 gives a dwelling a trade at the moment it is raised, so a
                // tenant-counting clock would never start in any world declaring `business` -- which
                // is most of them, and all of the ones this key exists for. A shop is not a resident.
                //
                // Absent means a Building of this kind stands empty for ever, which is what every
                // Ruleset written before this key meant and what all but one still mean.
                int emptyAfterTicks = 0;

                bool statesEmpty = TryInteger(
                    table, "abandoned_when_empty_after_days", out long empties, required: false, name);

                if (statesEmpty && empties <= 0)
                {
                    Refuse(
                        LineOf((SyntaxNodeBase?)Find(table, "abandoned_when_empty_after_days") ?? table),
                        name,
                        $"abandoned_when_empty_after_days is {empties}. It is how many Days a Building "
                        + "of this kind may house nobody before the city gives up on it, so it must "
                        + "be positive -- zero would abandon every Building on the sweep after it was "
                        + "raised, since adr/0069 has construction house nobody. Omit it for a kind "
                        + "that stands empty for ever.");
                    statesEmpty = false;
                }
                else if (statesEmpty)
                {
                    emptyAfterTicks = InTicks(empties);
                }

                int collapsesAfterDays = 0;

                bool statesCollapse =
                    TryInteger(table, "collapses_after_days", out long stands, required: false, name);

                // ⚠ THE PAIR IS NOW A TRIPLE ON ONE SIDE AND STILL A PAIR ON THE OTHER. Two keys can
                // abandon a Building and either of them leaves a shell, so the sink is owed if
                // EITHER is stated; it is refused only when NEITHER is, because then nothing can
                // abandon anything and the duration would never be read.
                bool canBeAbandoned = condemnAfterTicks > 0 || emptyAfterTicks > 0;

                if (canBeAbandoned && !statesCollapse)
                {
                    Refuse(
                        LineOf((SyntaxNodeBase?)Find(table, "condemn_after_days")
                            ?? (SyntaxNodeBase?)Find(table, "abandoned_when_empty_after_days")
                            ?? table),
                        name,
                        $"'{name}' can abandon a Building and states no collapses_after_days. A kind "
                        + "that can be abandoned accumulates standing shells, and a collection with "
                        + "an inflow and no sink is adr/0006 — so the duration a shell stands before "
                        + "it collapses is required here, in Days.");
                }

                if (!canBeAbandoned && statesCollapse)
                {
                    Refuse(LineOf((SyntaxNodeBase?)Find(table, "collapses_after_days") ?? table), name,
                        $"'{name}' states collapses_after_days and neither condemn_after_days nor "
                        + "abandoned_when_empty_after_days, so nothing can ever abandon a Building of "
                        + "this kind and the duration would never be read. Omit it, or state a "
                        + "threshold that makes it mean something.");
                }

                if (statesCollapse)
                {
                    if (stands <= 0)
                    {
                        Refuse(LineOf((SyntaxNodeBase?)Find(table, "collapses_after_days") ?? table), name,
                            $"collapses_after_days is {stands}. It is how many Days an abandoned "
                            + "Building stands before it collapses, so it must be positive; zero "
                            + "would clear a shell on the sweep that found it and there would be no "
                            + "abandoned state to see.");
                    }
                    else
                    {
                        collapsesAfterDays = stands > int.MaxValue ? int.MaxValue : (int)stands;
                    }
                }

                // adr/0068's occupancy. Optional, and absent means this kind houses nobody -- which
                // is what almost every kind means and what every Ruleset written before occupancy
                // existed already meant. A negative one is refused rather than clamped, on
                // condemn_after's reasoning: it reads as "evict everybody", and a Ruleset that
                // emptied every Building it declared would be a sentence somebody meant to write and
                // nobody would guess from the symptom.
                // plans/0053: `occupants` is RETIRED and what replaced it is a BOOLEAN. Thirty-nine
                // declarations across the shipped files carried two kinds and three values, and a
                // game meant to run to a million people cannot ask an author to write a count down
                // for every combination of form and use. The count is floor area over a rate now --
                // [capacity] floor_tiles_per_occupant -- and what a kind still declares is WHETHER it
                // takes tenants at all. Capacity is geometry; behaviour is content.
                RefuseRetired(table, "occupants", name,
                    "occupants is stated on a [[building]] kind. How many tenants a Building holds "
                    + "is DERIVED now -- its floor area over [capacity] floor_tiles_per_occupant -- "
                    + "so it differs Building by Building with the ground each stands on "
                    + "(plans/0053). Write `tenanted = true` for a kind that takes tenants at all, "
                    + "and make a Building hold more by drawing bigger blocks.");

                bool tenanted = false;

                if (TryBoolean(table, "tenanted", out bool takes, name))
                {
                    tenanted = takes;
                }

                // plans/0052 stage 1: footprint_tiles is RETIRED and the ground it named is now
                // DERIVED. A Building covers its Lot's parcel, which is a partition of the block the
                // player drew -- adr/0025's own line is that block geometry determining parcel size
                // "is not a rule at all; it is arithmetic over what the player drew", and an authored
                // constant was standing exactly where the design says arithmetic belongs.
                //
                // Refused by name rather than dropped, because a file still stating it would
                // otherwise load clean and seal a different amount of ground than it says.
                RefuseRetired(table, "footprint_tiles", name,
                    "footprint_tiles is stated on a [[building]] kind. A Building now covers its "
                    + "Lot's PARCEL -- the block's ground, partitioned by the block's subdivision "
                    + "pattern -- so the ground it seals is derived from what the player drew and "
                    + "is no longer a property of the kind (plans/0052, adr/0025). Remove the key; "
                    + "make the ground bigger by drawing bigger blocks.");

                // adr/0141 gave `jobs` and the Shift band to the TRADE, and adr/0148 removed them
                // from here rather than leaving them parsed and unread. A key nothing reads is this
                // corpus's own named failure mode, so all three are refused by name -- and the
                // message says where they went, because a bare "unknown key" would send an author
                // looking for a typo.
                foreach (string moved in EmploymentKeysThatMoved)
                {
                    RefuseRetired(table, moved, name,
                        $"{moved} is stated on a [[building]] kind. Employment belongs to the "
                    + "TRADE and not to the premises (adr/0141), so it moved to [[business]] "
                    + "at milestone 27 -- state it there, and name the trade on this kind "
                    + "with `business = \"<name>\"` so a Building of it comes with one "
                    + "(adr/0148). A Building employs nobody.");
                }

                // adr/0068's rule applied to parking (adr/0120, milestone 7 task 1). Optional and
                // refused negative on `jobs`' reasoning exactly -- a negative reads as "remove spaces
                // that are not there", which is not a sentence anybody meant to write.
                //
                // Unlike the Shift band below this needs NO paired key and no *unset* spelling, and
                // that is the decision rather than an omission: zero is the interesting value here.
                // A tower with no parking is adr/0009's own second player-tool row -- "a detached
                // house carries a driveway, a tower may not" -- so every value in range means
                // something and absence means what zero means.
                // plans/0053: `parking` is RETIRED for `occupants`' reason and one of its own. A
                // PARKING MINIMUM IS A PROPERTY OF A CITY AND NOT OF A BUILDING -- it is where the
                // quantity sits in every real planning code -- so it is [capacity]
                // floor_tiles_per_parking_space, stated once, and the spaces a Building gets divide
                // its own floor area.
                bool parked = false;

                if (TryBoolean(table, "parked", out bool parks, name))
                {
                    parked = parks;
                }

                RefuseRetired(table, "parking", name,
                    "parking is stated on a [[building]] kind. How many Vehicles a Building can park "
                    + "is DERIVED now -- its floor area over [capacity] "
                    + "floor_tiles_per_parking_space -- because a parking minimum is a property of a "
                    + "city rather than of a building (plans/0053). Write `parked = true` for a "
                    + "kind that carries parking at all, and omit the whole [capacity] key for a "
                    + "city with no parking.");

                // adr/0088's throughput ceiling, and it is the key that makes the kind an Outside
                // Connection (milestone 11 task 1). Optional, because almost no kind is a gate.
                //
                // A STATED ZERO IS REFUSED, and this is the one of these five keys where that is
                // right. `parking` takes zero because a tower with no parking is a real building;
                // `occupants` and `jobs` take it because housing and employing nobody is what almost
                // every kind does. A gate admitting nobody is none of those -- it is a door that
                // never opens, which is a Ruleset that loads clean and does nothing, and that is the
                // refusal class established by task 3 of plans/0014. Refusing it is also what lets
                // the key double as the declaration: absent means "not a gate" unambiguously,
                // because no gate can spell itself zero.
                int arrivalsPerDay = 0;

                if (TryInteger(table, "arrivals_per_day", out long admits, required: false, name))
                {
                    if (admits <= 0)
                    {
                        Refuse(LineOf((SyntaxNodeBase?)Find(table, "arrivals_per_day") ?? table), name,
                            $"arrivals_per_day is {admits}. It counts Households a Building of this "
                            + "kind admits from the Outside per Day, and stating it is what makes "
                            + "the kind an Outside Connection -- so a zero or a negative is a gate "
                            + "that never opens, which loads clean and does nothing. State a "
                            + "throughput, or omit the key for a kind that is not a gate.");
                    }
                    else
                    {
                        arrivalsPerDay = admits > int.MaxValue ? int.MaxValue : (int)admits;
                    }
                }

                // adr/0032's Attended services. `serves` is both the declaration and the content --
                // arrivals_per_day's shape -- so a kind cannot be a service and decline to say what
                // for, and there is no is_service flag for it to disagree with.
                Need serves = ReadServes(table, name);

                // adr/0148: the trade a Building of this kind comes with. Optional, because almost
                // every kind that has ever shipped comes with none -- and resolved to an id HERE
                // rather than deferred, because [[business]] registration runs in the table walk
                // above and the name -> id direction is discarded when Read() returns.
                byte business = 0;

                if (TryString(table, "business", out string? trade, required: false, name)
                    && trade is not null)
                {
                    if (!_businessKinds.TryGetValue(trade, out business))
                    {
                        Refuse(LineOf((SyntaxNodeBase?)Find(table, "business") ?? table), name,
                            $"business is \"{trade}\", and no [[business]] declares that trade. A "
                            + "kind naming a trade nothing declares would raise Buildings that come "
                            + "with nothing, which loads clean and employs nobody.");
                    }
                    else if (!tenanted)
                    {
                        // A premises with no room for the shop it comes with is half a sentence.
                        // adr/0147 counts one ceiling over both kinds of tenant, so a declared trade
                        // needs a slot to sit in exactly as a Household does. ⚠ The COUNT is derived
                        // now and cannot be checked here -- it depends on ground no Ruleset has seen
                        // -- so what is checkable is that the kind takes tenants at all.
                        Refuse(LineOf((SyntaxNodeBase?)Find(table, "business") ?? table), name,
                            $"business is \"{trade}\" and this kind is not tenanted. A Building of "
                            + "this kind comes with a trade and has nowhere to hold it -- one "
                            + "ceiling counts both kinds of tenant (adr/0147), so write "
                            + "`tenanted = true`, or drop the trade.");
                    }
                }

                definitions[i] = new KindDefinition(
                    binFirst, allBins.Count - binFirst, ruleFirst, allRules.Count - ruleFirst)
                {
                    CondemnAfterTicks = condemnAfterTicks,
                    TenancyEndsAfterTicks = tenancyEndsAfterTicks,
                    ShedsOccupantAfterTicks = shedsAfterTicks,
                    CollapsesAfterDays = collapsesAfterDays,
                    AbandonedWhenEmptyAfterTicks = emptyAfterTicks,
                    Tenanted = tenanted,
                    Parked = parked,
                    Business = business,
                    ArrivalsPerDay = arrivalsPerDay,
                    Serves = serves,
                };
            }

            bins = [.. allBins];
            kindRules = [.. allRules];

            return definitions;
        }

        /// <summary>
        /// A kind's Shift-start band, in whole in-world hours, refused unless it agrees with
        /// <c>jobs</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Required exactly when the kind employs somebody, and refused when it does not</b>
        /// (<c>adr/0101</c>). That two-way pairing is what lets every read site treat the band as
        /// meaningful without a third field saying whether it was stated: a kind with
        /// <c>jobs &gt; 0</c> always has one, a kind with <c>jobs = 0</c> never does, and the
        /// defaulted <c>0, 0</c> is unreachable rather than ambiguous.
        /// </para>
        /// <para>
        /// ⚠ <b>Without the second half of the pairing, zero would mean two things.</b> Midnight is a
        /// legitimate start hour, so a kind that omitted the keys and a kind that authored the night
        /// shift would be indistinguishable — session F's placeholder trap, and the reason
        /// <c>adr/0098</c> reached for an omitted <em>table</em> where this reaches for a refusal.
        /// </para>
        /// <para>
        /// <b>23 rather than 24 is the ceiling</b>, because this is an hour of the Day and not a
        /// duration: hour 24 is hour 0 of the following Day, and permitting both would give one
        /// instant two spellings that draw differently.
        /// </para>
        /// </remarks>
        private (int From, int To) ReadShiftStartBand(TableSyntaxBase table, string? name, int jobs)
        {
            bool hasFrom = TryInteger(
                table, "shift_start_earliest_hour", out long from, required: false, name);
            bool hasTo = TryInteger(
                table, "shift_start_latest_hour", out long to, required: false, name);

            if (jobs <= 0)
            {
                if (hasFrom || hasTo)
                {
                    Refuse(LineOf((SyntaxNodeBase?)Find(table, "shift_start_earliest_hour") ?? table),
                        name,
                        "this kind states a Shift-start band and employs nobody. The band is when a "
                        + "kind's jobs begin, so it means nothing without a `jobs` above zero -- "
                        + "state one, or delete the band.");
                }

                return (0, 0);
            }

            if (!hasFrom || !hasTo)
            {
                Refuse(LineOf(table), name,
                    "this kind employs Citizens and states no Shift-start band. Both "
                    + "shift_start_earliest_hour and shift_start_latest_hour are required wherever "
                    + "`jobs` is above zero, because a commute is timed from the hour its job begins "
                    + "and there is no default hour that is not also a real one -- a defaulted 0 "
                    + "would be midnight, which somebody may genuinely mean.");
                return (0, 0);
            }

            if (from < 0 || from >= Ticks.HoursPerDay)
            {
                Refuse(LineOf((SyntaxNodeBase?)Find(table, "shift_start_earliest_hour") ?? table),
                    name,
                    $"shift_start_earliest_hour = {from} is out of range. It is an hour of the Day, "
                    + $"so it runs 0 (midnight) to {Ticks.HoursPerDay - 1}; hour {Ticks.HoursPerDay} "
                    + "is hour 0 of the next Day and would give one instant two spellings.");
                return (0, 0);
            }

            if (to < from || to >= Ticks.HoursPerDay)
            {
                Refuse(LineOf((SyntaxNodeBase?)Find(table, "shift_start_latest_hour") ?? table), name,
                    $"shift_start_latest_hour = {to} is out of range. It is the latest hour a job of "
                    + $"this kind starts at, so it is at least shift_start_earliest_hour ({from}) and "
                    + $"at most {Ticks.HoursPerDay - 1}. Equal bounds are allowed and mean a kind "
                    + "whose shifts all start together.");
                return (0, 0);
            }

            return ((int)from, (int)to);
        }

        /// <summary>
        /// Reads what each <c>[[business]]</c> trade declares: <c>jobs</c> and the Shift band.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Two of <c>adr/0141</c>'s three.</b> That ADR gives the trade <c>jobs</c>, shift hours
        /// and the wage; the wage is <c>adr/0026</c> at milestone 15 (<c>06:99</c>), so no value is
        /// read for it and <b>the key is refused by name</b>. ⚠ <b>By name, because there is no
        /// unknown-key check in this loader</b> — every table reads what it wants and ignores the
        /// rest, so a stray key is silent everywhere (<c>plans/0041</c> <b>G31</b>). <c>wage</c> earns
        /// a named refusal because <c>adr/0141</c> gives a designer positive reason to write it, and
        /// a key that loads clean and does nothing is the class this loader refuses elsewhere.
        /// </para>
        /// <para>
        /// <b><see cref="ReadShiftStartBand"/> is reused rather than mirrored</b>, and every one of
        /// its refusals transfers word for word: the band is meaningless without <c>jobs</c>, both
        /// bounds are required wherever <c>jobs</c> is above zero, and an hour outside the Day is out
        /// of range. ***Nothing about those messages is specific to premises*** — they say
        /// <em>this kind</em>, which a trade is. ⚠ <b>So the band contributes no new refusal site</b>;
        /// this pass adds exactly <b>two</b>, the negative <c>jobs</c> and the wage.
        /// </para>
        /// </remarks>
        private BusinessKindDefinition[] ReadBusinessKinds(CapacityRuleset capacity)
        {
            var definitions = new BusinessKindDefinition[_businessKindTables.Count];

            for (int i = 0; i < _businessKindTables.Count; i++)
            {
                TableSyntaxBase table = _businessKindTables[i];
                string? name = TryString(table, "name", out string? found, required: false)
                    ? found
                    : null;

                // KindDefinition.Jobs' rule unchanged (adr/0068 by way of milestone 5b-bis task 2):
                // optional, because a trade employing nobody is a coherent thing to declare, and
                // refused negative because it reads as "sack everybody".
                // plans/0053: `jobs` is RETIRED. A Business takes one of its premises' tenancies
                // (adr/0141), so what it employs is that share of the floor over [capacity]
                // floor_tiles_per_job -- which makes a shop in a slab a bigger employer than the same
                // trade in a terrace, without anybody declaring two trades to say so.
                RefuseRetired(table, "jobs", name,
                    "jobs is stated on a [[business]]. How many Citizens a Business employs is "
                    + "DERIVED now -- its share of its premises' floor area over [capacity] "
                    + "floor_tiles_per_job -- so one trade employs differently in a terrace and in a "
                    + "slab (plans/0053). Omit the whole [capacity] key for a city that employs "
                    + "nobody.");

                // adr/0141's third Declares member, refused by NAME because it is the one key a
                // designer has positive reason to write. That ADR tells them the trade declares the
                // wage; the wage is adr/0026 and arrives at milestone 15 (06:99). Without this, `wage
                // = 100` loads clean and does nothing for ever -- the refusal class plans/0014 task 3
                // established, and the one this loader is least willing to ship.
                //
                // ⚠ It STAYS a named refusal now that RefuseUnknownKeys exists, and the reason is
                // the message rather than the coverage. The unknown-key check would catch `wage` for
                // free -- it is a key no reader asks for -- and would say "not a key of [[business]]",
                // which is TRUE AND USELESS to somebody who read adr/0141 and wrote what it told
                // them to. ***A key a designer has positive reason to write deserves the sentence
                // saying where it went***, and a general check cannot know that. The gap this
                // comment used to describe (plans/0041 G31) closed 2026-08-25; what did not change
                // is that nine keys are worth naming individually.
                RefuseRetired(table, "wage", name,
                    "this trade states a wage. adr/0141 does give the trade `jobs`, shift hours "
                    + "AND the wage -- and adr/0026's wage is not a declared number, it MOVES: "
                    + "each Business posts one and adjusts it by its own fill rate. That is "
                    + "still unbuilt. What exists is `wage_per_day`, a flat rate per Day worked, "
                    + "paired with `pay_period_days` -- write those two.");

                // The two readers took `jobs` to ask "does this trade employ anybody" before
                // deciding whether a shift band and a wage were owed. Employment is a property of the
                // WORLD now -- [capacity] floor_tiles_per_job -- so the question they were really
                // asking is whether this Ruleset employs anybody at all.
                int employs = capacity.FloorTilesPerJob > 0 ? 1 : 0;

                (int shiftFrom, int shiftTo) = ReadShiftStartBand(table, name, employs);
                (int wagePerDay, int payPeriodDays) = ReadWage(table, name, employs);

                definitions[i] = new BusinessKindDefinition
                {
                    ShiftStartEarliestHour = shiftFrom,
                    ShiftStartLatestHour = shiftTo,
                    WagePerDay = wagePerDay,
                    PayPeriodDays = payPeriodDays,
                };
            }

            return definitions;
        }

        /// <summary>
        /// Resolves an optional <c>[[life_stage]]</c> key naming another stage, or 0.
        /// </summary>
        /// <remarks>
        /// <b>`next`'s two refusals, factored out when the third and fourth key wanted them.</b> An
        /// undeclared name and a self-reference are the same two mistakes whichever key makes them,
        /// and the second is the one worth having: a stage naming itself is indistinguishable from a
        /// typo and expensive in a way nothing reports. ⚠ <b><c>next</c> itself still reads inline</b>
        /// — its refusal text is asserted verbatim by a test, and rewording it to share this one
        /// would move a string the corpus quotes.
        /// </remarks>
        private byte Stage(TableSyntaxBase table, string key, string? name, int self)
        {
            if (!TryString(table, key, out string? named, required: false) || named is null)
            {
                return 0;
            }

            if (!_lifeStages.TryGetValue(named, out byte stage))
            {
                Refuse(LineOf((SyntaxNodeBase?)Find(table, key) ?? table), name,
                    $"{key} is \"{named}\", and no [[life_stage]] declares that name. A stage names "
                    + "another stage by name; omit the key to mean there is no such destination.");

                return 0;
            }

            if (stage == (byte)(self + 1))
            {
                Refuse(LineOf((SyntaxNodeBase?)Find(table, key) ?? table), name,
                    $"{key} is \"{named}\", which is this stage. A destination that is its own "
                    + "origin is a Household that transitions for ever and arrives back where it "
                    + "started -- a wake every countdown, for no change.");

                return 0;
            }

            return stage;
        }

        /// <summary>
        /// The two refusals that are properties of the stage table as a whole rather than of a row.
        /// </summary>
        /// <remarks>
        /// <para>
        /// 🔴 <b>The second one is <c>adr/0006</c> arriving at a door nobody had built when the first
        /// one was written.</b> <see cref="ReadGivesUpAfterDays"/> refuses a Ruleset with a gate kind
        /// and no give-up rule, because ***a Pool with a door into it and no sink grows without
        /// bound***. <c>plans/0046</c> stage 3 opens a <b>second door</b>: a Household leaving the
        /// spawning stage sends its children into the Unplaced Pool, and nothing about that is an
        /// arrival through a gate. ***So the obligation is a property of anything that fills the
        /// Pool, and it was written as a property of gates.***
        /// </para>
        /// <para>
        /// ⚠ <b>It is the dynamic <c>adr/0011</c> already describes, not a new one.</b> That ADR's
        /// own sentence is <em>"an unaffordable city fails to house its own children and they become
        /// Departures — you raised them and priced them out"</em>, and the derived metric it names is
        /// <b>retention</b>. A generating Ruleset with no give-up rule cannot express either: the
        /// children simply queue for ever.
        /// </para>
        /// <para>
        /// <b>The first is narrower.</b> A stage children become adults in has to say what age they
        /// carry, because <c>Citizens.Age</c> is drawn on formation and static — there is no default
        /// that is not a guess about a population.
        /// </para>
        /// </remarks>
        private void RefuseIncompleteGeneration(
            LifeStageDefinition[] stages, PlacementRuleset placement)
        {
            bool spawns = false;

            for (int i = 0; i < stages.Length; i++)
            {
                byte become = stages[i].ChildrenBecome;

                if (become == 0)
                {
                    continue;
                }

                spawns = true;

                if (stages[become - 1].AdultAgeMaxDays == 0)
                {
                    Refuse(LineOf(_lifeStageTables[become - 1]), null,
                        "this stage is named by a children_become and states no adult age band, so "
                        + "a Citizen forming a Household here would have no age to carry. An age is "
                        + "DRAWN on formation and never advances (adr/0011), so there is no default "
                        + "that is not a guess about a population. State adult_age_min_days and "
                        + "adult_age_max_days.");
                }
            }

            if (spawns && !placement.GivesUp)
            {
                Refuse(1, null,
                    "this Ruleset declares a [[life_stage]] whose children_become sends children "
                    + "into the Unplaced Pool, and [placement] states no gives_up_after_days -- so "
                    + "nothing ever leaves the Pool except by being housed. Internal generation is a "
                    + "door into the Pool exactly as a gate kind is, and a Pool with a door and no "
                    + "give-up rule grows without bound, which adr/0006 forbids. State how long a "
                    + "Household keeps looking, in Days. adr/0011 wants this anyway: an unaffordable "
                    + "city failing to house its own children is what makes retention a reading.");
            }
        }

        /// <summary>
        /// Reads <c>[[life_stage]]</c>: a countdown floor, its window, and the stage it exits to.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>A second pass, because <c>next</c> names a stage that may be declared below this
        /// one.</b> <c>adr/0011</c>'s chain runs Young → Family → Mature Family → Empty Nest with
        /// Young also able to exit sideways to Childless, and a file is free to author the terminals
        /// first. Resolving names in the pass that collects them would make the file's line order a
        /// constraint on what a designer may write, which is the same objection that made the
        /// successor authored rather than derived.
        /// </para>
        /// <para>
        /// <b>Four refusals, and the one worth explaining is the self-reference.</b> A stage naming
        /// itself is a Household that transitions for ever and arrives back where it started — it
        /// costs a wake every countdown, it is indistinguishable from a typo, and nothing in the
        /// build reports it. Terminal is spelled by omitting <c>next</c>, which cannot be mistyped
        /// into a loop.
        /// </para>
        /// </remarks>
        private LifeStageDefinition[] ReadLifeStages()
        {
            var definitions = new LifeStageDefinition[_lifeStageTables.Count];

            for (int i = 0; i < _lifeStageTables.Count; i++)
            {
                TableSyntaxBase table = _lifeStageTables[i];
                string? name = TryString(table, "name", out string? found, required: false)
                    ? found
                    : null;

                int durationDays = 1;

                if (TryInteger(table, "duration_days", out long duration, required: true, name))
                {
                    if (duration < 1)
                    {
                        Refuse(LineOf((SyntaxNodeBase?)Find(table, "duration_days") ?? table), name,
                            $"duration_days is {duration}. It is the FEWEST Days a Household spends "
                            + "in this stage, so it is at least 1: a stage of zero Days is entered "
                            + "and left on the same Day, and a chain of them collapses a whole life "
                            + "into one Tick.");
                    }
                    else
                    {
                        durationDays = duration > int.MaxValue ? int.MaxValue : (int)duration;
                    }
                }

                // Required with no default, and zero is ALLOWED. adr/0011's W is the load-bearing
                // half -- without it every Household created at Tick 0 leaves every stage on the same
                // Day for the whole run, and the founding generation's echo reads as a demographic
                // mechanism rather than as an artefact of world creation. A designer who wants that
                // world states 0 and gets it; a designer who forgets the key is asked, because a
                // defaulted W would silently choose one of the two cities.
                int spreadDays = 0;

                if (TryInteger(table, "spread_days", out long spread, required: true, name))
                {
                    if (spread < 0)
                    {
                        Refuse(LineOf((SyntaxNodeBase?)Find(table, "spread_days") ?? table), name,
                            $"spread_days is {spread}. It is the width of the window the countdown "
                            + "is drawn over, uniform on [duration_days, duration_days + "
                            + "spread_days), so a negative one is a window that runs backwards. "
                            + "Zero is allowed and means every Household in this stage leaves it on "
                            + "the same Day.");
                    }
                    else
                    {
                        spreadDays = spread > int.MaxValue ? int.MaxValue : (int)spread;
                    }
                }

                // adr/0027's base and width, and BOTH are optional with a neutral default -- which
                // is the opposite of spread_days directly above, on purpose. W is required there
                // because its zero is one of two real cities and a defaulted W would silently pick
                // one. Here the neutral pair is not a city at all: it is the placement this build
                // already had, so an author who says nothing is not being defaulted into a choice,
                // they are declining to make one. ***A default is dangerous when zero is an answer
                // and harmless when absence is.***
                int centralityBase = Borough.Core.Rules.Ruleset.CentralityNeutralPercent;
                int centralitySpread = 0;

                if (TryInteger(table, "centrality_base_percent", out long centre, required: false, name))
                {
                    if (centre is < 0 or > 100)
                    {
                        Refuse(
                            LineOf((SyntaxNodeBase?)Find(table, "centrality_base_percent") ?? table),
                            name,
                            $"centrality_base_percent is {centre}. It is where this stage sits on the "
                            + "space-against-centrality axis, so it is a percent: 0 wants room, 100 "
                            + "wants the middle of the city, and "
                            + $"{Borough.Core.Rules.Ruleset.CentralityNeutralPercent} has no opinion "
                            + "and is what a stage stating nothing gets.");
                    }
                    else
                    {
                        centralityBase = (int)centre;
                    }
                }

                if (TryInteger(
                    table, "centrality_spread_percent", out long width, required: false, name))
                {
                    if (width is < 0 or > 100)
                    {
                        Refuse(
                            LineOf((SyntaxNodeBase?)Find(table, "centrality_spread_percent") ?? table),
                            name,
                            $"centrality_spread_percent is {width}. It is the width of the band this "
                            + "stage's Households draw their own position from, so it is a percent. "
                            + "Zero is allowed and means the stage agrees with itself completely -- "
                            + "every Household in it draws the base exactly.");
                    }
                    else
                    {
                        centralitySpread = (int)width;
                    }
                }

                // The band has to fit on the axis, and this is the only refusal of the pair that
                // neither key can be checked for alone. Without it a base of 80 and a width of 40
                // would put the top of the band at 120% -- a Household wanting the centre harder
                // than the centre exists, which reads downstream as a saturated preference rather
                // than as a Ruleset that does not add up.
                if (centralityBase + centralitySpread > 100)
                {
                    Refuse(
                        LineOf((SyntaxNodeBase?)Find(table, "centrality_spread_percent") ?? table),
                        name,
                        $"centrality_base_percent {centralityBase} and centrality_spread_percent "
                        + $"{centralitySpread} put the top of this stage's band at "
                        + $"{centralityBase + centralitySpread}%, past the end of the axis. The band "
                        + "is [base, base + spread] and it has to fit in [0, 100]: lower the base, "
                        + "or narrow the width.");
                }

                byte next = 0;

                if (TryString(table, "next", out string? successor, required: false)
                    && successor is not null)
                {
                    if (!_lifeStages.TryGetValue(successor, out next))
                    {
                        Refuse(LineOf((SyntaxNodeBase?)Find(table, "next") ?? table), name,
                            $"next is \"{successor}\", and no [[life_stage]] declares that name. "
                            + "A stage exits to another stage by name; omit the key for a stage "
                            + "nothing follows.");

                        next = 0;
                    }
                    else if (next == (byte)(i + 1))
                    {
                        Refuse(LineOf((SyntaxNodeBase?)Find(table, "next") ?? table), name,
                            $"next is \"{successor}\", which is this stage. A Household would "
                            + "transition for ever and arrive back where it started -- a wake every "
                            + "countdown, for no change. Omit `next` for a terminal stage.");

                        next = 0;
                    }
                }

                byte childless = Stage(table, "childless", name, i);
                byte become = Stage(table, "children_become", name, i);

                int childrenMin = 0;
                int childrenMax = 0;
                bool bandStated =
                    Find(table, "children_min") is not null || Find(table, "children_max") is not null;

                if (bandStated)
                {
                    TryInteger(table, "children_min", out long low, required: true, name);
                    TryInteger(table, "children_max", out long high, required: true, name);

                    if (low < 0)
                    {
                        Refuse(LineOf((SyntaxNodeBase?)Find(table, "children_min") ?? table), name,
                            $"children_min is {low}. It is the fewest children a Household bears on "
                            + "leaving this stage, so it is at least 0 -- and 0 is the answer that "
                            + "matters: adr/0011 sends a Household drawing zero to the childless "
                            + "stage, which is what makes one reachable at all.");
                    }
                    else if (high < low)
                    {
                        Refuse(LineOf((SyntaxNodeBase?)Find(table, "children_max") ?? table), name,
                            $"children_max is {high} and children_min is {low}. The band is "
                            + "inclusive at both ends, so the top is at least the bottom. Equal ends "
                            + "are allowed and mean every Household bears the same number -- the "
                            + "lockstep answer spread_days exists to avoid one stage along.");
                    }
                    else
                    {
                        childrenMin = (int)Math.Min(low, int.MaxValue);
                        childrenMax = (int)Math.Min(high, int.MaxValue);
                    }
                }

                // Both halves of the fertility decision or neither, and the refusal runs in both
                // directions because each half alone is a Ruleset that loads clean and does nothing.
                // A band with no childless successor draws zero and has nowhere to send anybody; a
                // childless successor with no band is a stage nothing can ever route into, which is
                // exactly the state childless was in before plans/0046 stage 3.
                if (bandStated && childless == 0)
                {
                    Refuse(LineOf((SyntaxNodeBase?)Find(table, "children_min") ?? table), name,
                        "this stage states a child band and no `childless` successor. A Household "
                        + "drawing zero children has nowhere to go -- adr/0011's Young exit is "
                        + "\"zero sends the Household to Childless; otherwise to Family\", and both "
                        + "destinations are authored. Name one, or drop the band.");
                }
                else if (!bandStated && childless != 0)
                {
                    Refuse(LineOf((SyntaxNodeBase?)Find(table, "childless") ?? table), name,
                        "this stage names a `childless` successor and states no child band, so "
                        + "nothing ever draws zero and that successor is unreachable. State "
                        + "children_min and children_max, or drop the key.");
                }

                int adultMin = 0;
                int adultMax = 0;
                bool ageStated = Find(table, "adult_age_min_days") is not null
                    || Find(table, "adult_age_max_days") is not null;

                if (ageStated)
                {
                    TryInteger(table, "adult_age_min_days", out long young, required: true, name);
                    TryInteger(table, "adult_age_max_days", out long old, required: true, name);

                    if (young < 1 || old < young || old > ushort.MaxValue)
                    {
                        Refuse(LineOf((SyntaxNodeBase?)Find(table, "adult_age_min_days") ?? table),
                            name,
                            $"the adult age band is {young}..{old} Days. It is the age a Citizen "
                            + "carries when it becomes an adult in this stage -- drawn on formation "
                            + "and static, because Citizens do not age (adr/0011) -- so the bottom "
                            + $"is at least 1, the top is at least the bottom and at most "
                            + $"{ushort.MaxValue}, which is what the column holds. Zero is not "
                            + "available: it is what a CHILD carries, and it is the only marker of "
                            + "childhood there is.");
                    }
                    else
                    {
                        adultMin = (int)young;
                        adultMax = (int)old;
                    }
                }

                // The wheel's period, checked at LOAD rather than at the arming. LifeStageWheel.Arm
                // throws past CoarseDays - 1 Days ahead, and a stage drawing up to duration + spread
                // - 1 Days would reach it -- so without this the refusal arrives as a crash on
                // whichever Household happened to draw the top of the window, which is a Ruleset
                // defect reported as an engine one. ***A bound a Ruleset can violate belongs in the
                // loader***, which is plans/0014 task 3's whole rule.
                int longest = durationDays + spreadDays - 1;

                if (longest >= Borough.Core.Rules.EventWheel.CoarseDays)
                {
                    Refuse(LineOf((SyntaxNodeBase?)Find(table, "duration_days") ?? table), name,
                        $"duration_days {durationDays} and spread_days {spreadDays} draw a countdown "
                        + $"of up to {longest} Days, and the Life Stage wheel reaches "
                        + $"{Borough.Core.Rules.EventWheel.CoarseDays - 1}. A longer stage needs a "
                        + "third wheel tier, which nothing has asked for yet.");
                }

                definitions[i] = new LifeStageDefinition
                {
                    DurationDays = durationDays,
                    SpreadDays = spreadDays,
                    NextStage = next,
                    ChildrenMin = childrenMin,
                    ChildrenMax = childrenMax,
                    ChildlessStage = childless,
                    ChildrenBecome = become,
                    AdultAgeMinDays = adultMin,
                    AdultAgeMaxDays = adultMax,
                    CentralityBasePercent = centralityBase,
                    CentralitySpreadPercent = centralitySpread,
                };
            }

            return definitions;
        }

        // ---- bands ----------------------------------------------------------------------------

        /// <summary>
        /// Reads the <c>[[band]]</c> tables — <c>adr/0025</c>'s density bands, in declaration order.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>A band admits ZONE BITS and its identity is its position in the file, counted from 1.</b>
        /// The bit is the same integer a <c>[[zone_rule]]</c>'s <c>zone</c> key names, because a Lot's
        /// permission set and a band's are the same sixteen bits meaning the same thing — which is
        /// <c>adr/0025</c>'s own mechanism: <em>"a band expresses itself as which kinds a Lot
        /// permits"</em>.
        /// </para>
        /// <para>
        /// ⚠ <b>An empty <c>admits</c> is REFUSED rather than read as <em>nothing may build</em>.</b>
        /// A band is a cap, and a cap that admits nothing is land nobody can ever develop — expressible
        /// already by not zoning it, and reachable here only by writing a key that looks like it does
        /// something. That is the shape refusals 6 to 10 exist to stop: a file that loads clean and
        /// does nothing.
        /// </para>
        /// <para>
        /// ⚠ <b>A duplicate bit inside one band is refused for the same reason</b> — the second one
        /// changes nothing, so it is either a typo for another bit or a misunderstanding of what the
        /// key means, and both are worth a line number.
        /// </para>
        /// </remarks>
        private BandDefinition[] ReadBands()
        {
            var definitions = new List<BandDefinition>(_bandTables.Count);

            foreach (TableSyntaxBase table in _bandTables)
            {
                string? name = TryString(table, "name", out string? declared, required: false)
                    ? declared
                    : null;

                KeyValueSyntax? entry = Find(table, "admits", RulesetKeyKind.Numbers);

                if (entry is null)
                {
                    Refuse(LineOf(table), name,
                        "a [[band]] states no admits. A band is adr/0025's CAP -- the set of zone "
                        + "bits it lets a Lot keep -- and a band with no set is not a permissive "
                        + "band, it is a table with no content.");
                    continue;
                }

                if (entry.Value is not ArraySyntax array)
                {
                    Refuse(LineOf(entry), name, "admits must be an array of zone bits.");
                    continue;
                }

                ushort admits = 0;
                bool refused = false;

                foreach (ArrayItemSyntax item in array.Items)
                {
                    if (item.Value is not IntegerValueSyntax integer)
                    {
                        Refuse(LineOf(item), name,
                            "every entry of admits is a zone bit, which is the same integer a "
                            + "[[zone_rule]]'s `zone` key names.");
                        refused = true;
                        continue;
                    }

                    long bit = integer.Value;

                    if (bit < 0 || bit >= LotTable.ZoneBits)
                    {
                        Refuse(LineOf(item), name,
                            $"zone bit {bit} is outside 0..{LotTable.ZoneBits - 1}. A Lot's "
                            + $"permission set is {LotTable.ZoneBits} bits wide, so no zone command "
                            + "can paint this bit and admitting it would let this band cap nothing.");
                        refused = true;
                        continue;
                    }

                    ushort mask = (ushort)(1 << (int)bit);

                    if ((admits & mask) != 0)
                    {
                        Refuse(LineOf(item), name,
                            $"zone bit {bit} is admitted twice by one band. The second one changes "
                            + "nothing, so it is either a typo for another bit or a misreading of "
                            + "what admits means.");
                        refused = true;
                        continue;
                    }

                    admits |= mask;
                }

                if (refused)
                {
                    continue;
                }

                if (admits == 0)
                {
                    Refuse(LineOf(entry), name,
                        "a band admits no zone bit at all. Land nothing may ever build on is land "
                        + "the player did not zone, and it is already expressible that way -- so "
                        + "this key loads clean and does nothing, which is what refusals 6 to 10 "
                        + "exist to stop.");
                    continue;
                }

                definitions.Add(new BandDefinition { Admits = admits });
            }

            return [.. definitions];
        }

        // ---- zone rules -----------------------------------------------------------------------

        /// <summary>
        /// Reads the <c>[[zone_rule]]</c> tables, and runs refusals 6 to 10.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>On the same walk as the other five</b> (<c>adr/0048</c>): one load-time pass, one error
        /// surface, and the core receives ids and integers with no string among them.
        /// </para>
        /// <para>
        /// <b>Every refusal here is a Ruleset that would load clean and do nothing</b>, which is the
        /// class this build has already been bitten by — <c>apply = {min=1,max=4}</c> behaving as
        /// <c>{1,1}</c> got through because a silent narrowing is indistinguishable from a quiet
        /// design. A Zone Rule naming an undeclared kind, an unpaintable bit, a revisit period of zero
        /// or a retired <c>sample</c> key all produce the same symptom: a city that never grows, and no
        /// sentence anywhere saying why.
        /// </para>
        /// </remarks>
        private ZoneRuleDefinition[] ReadZoneRules()
        {
            var definitions = new List<ZoneRuleDefinition>(_zoneRuleTables.Count);

            foreach (TableSyntaxBase table in _zoneRuleTables)
            {
                string? name = TryString(table, "name", out string? found, required: false)
                    ? found
                    : null;

                _zoneRuleNames.Add(name);

                // Refusal 6 — a kind the Ruleset does not declare. The same two-sided check the rest
                // of the loader makes: the loader refuses an unknown name, the interpreter refuses an
                // unknown id, and neither trusts the other.
                byte kind = 0;

                if (TryString(table, "kind", out string? kindName, required: true, name))
                {
                    if (!_kinds.TryGetValue(kindName!, out kind))
                    {
                        Refuse(LineOf((SyntaxNodeBase?)Find(table, "kind") ?? table), name,
                            $"no [[building]] is named '{kindName}'. A Zone Rule builds a declared "
                            + "kind, and one naming a kind that does not exist would sample Lots for "
                            + "ever and build nothing.");
                    }
                }

                // Refusal 7 — a permission bit no `zone` verb can paint. The Lot's permission set is
                // LotTable.ZoneBits wide, so a higher bit can never be set by any command, and the
                // Rule's create predicate can never pass. Checked against the column's own constant
                // rather than a number copied into this file.
                byte zone = 0;

                if (TryInteger(table, "zone", out long bit, required: true, name))
                {
                    if (bit < 0 || bit >= LotTable.ZoneBits)
                    {
                        Refuse(LineOf((SyntaxNodeBase?)Find(table, "zone") ?? table), name,
                            $"zone bit {bit} is outside 0..{LotTable.ZoneBits - 1}. A Lot's permission "
                            + $"set is {LotTable.ZoneBits} bits wide, so no zone command can paint "
                            + "this bit and no Lot can ever admit this Rule.");
                    }
                    else
                    {
                        zone = (byte)bit;
                    }
                }

                uint interval = ReadInterval(table, name);
                int revisit = ReadRevisit(table, name, interval);

                // adr/0163 tier 1, both optional and both in DAYS (adr/0168's direction). Absent
                // means the Rule keeps the tier-0 predicate, which is every shipped file but the
                // Provider's -- so a Ruleset written before this existed is unchanged, and a Ruleset
                // that wants demand says so.
                int thresholdDays = 0;
                int cooldownDays = 0;

                if (TryInteger(table, "build_threshold_days", out long threshold, required: false, name))
                {
                    if (threshold < 1)
                    {
                        Refuse(LineOf((SyntaxNodeBase?)Find(table, "build_threshold_days") ?? table), name,
                            $"a build threshold of {threshold} household-Days is not a threshold. "
                            + "adr/0170 makes this the ENTRY COST for a trade -- raising a Building "
                            + "costs nobody anything today, so it is the only brake on birth, and a "
                            + "city with no brake builds until it runs out of Lots. Omit the key to "
                            + "keep the tier-0 predicate instead.");
                    }
                    else
                    {
                        thresholdDays = (int)threshold;
                    }
                }

                if (TryInteger(table, "cooldown_days", out long cooldown, required: false, name))
                {
                    if (cooldown < 0)
                    {
                        Refuse(LineOf((SyntaxNodeBase?)Find(table, "cooldown_days") ?? table), name,
                            $"a cooldown of {cooldown} Days is negative.");
                    }
                    else if (thresholdDays == 0)
                    {
                        Refuse(LineOf((SyntaxNodeBase?)Find(table, "cooldown_days") ?? table), name,
                            "a cooldown is stated without build_threshold_days. It damps how fast a "
                            + "District raises Buildings on the tier-1 demand signal, and a Rule with "
                            + "no threshold reads no demand -- so this key would load clean and do "
                            + "nothing, which is exactly what refusals 6 to 10 exist to stop.");
                    }
                    else
                    {
                        cooldownDays = (int)cooldown;
                    }
                }

                definitions.Add(new ZoneRuleDefinition(kind, zone, interval, revisit)
                {
                    BuildThresholdDays = thresholdDays,
                    CooldownDays = cooldownDays,
                });
            }

            return [.. definitions];
        }

        /// <summary>
        /// A Sweep Rule's trigger interval — <c>[[zone_rule]]</c>, <c>[placement]</c>, <c>[jobs]</c>
        /// and <c>[[policy]]</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Not <see cref="ReadRate"/>, because the key is spelled differently and that is
        /// deliberate.</b> <c>rate</c> is how often a Building's Rule re-arms; <c>interval</c> is how
        /// often the city sweeps. Sharing a word would invite the reading that a Zone Rule is armed
        /// per Lot, which is exactly the Bin Rule shape it is not.
        /// </para>
        /// <para>
        /// ⚠ <b>The ceiling was <c>WHEEL_SIZE</c> and it had no ground under any caller, which
        /// milestone 10 task 5 found by writing the first interval that wanted to be a Day.</b> The
        /// old reason was that an interval <em>"at or beyond WHEEL_SIZE would re-arm into the bucket
        /// it just came off"</em> — true of a Bin Rule's <c>rate</c>, and about a mechanism <b>none
        /// of this method's four callers uses</b>: a Sweep Rule has no wheel entry, no subscription
        /// and no arming, and all four test <c>tick % interval</c>. This method's own summary said so
        /// two sentences above the check. ***A bound is inherited with a word, not with a mechanism***
        /// — <c>interval</c> was bounded like <c>rate</c> because the doc-comment said it was bounded
        /// <em>like a Bin Rule's rate</em>, which is <c>adr/0093</c> running inside one method.
        /// </para>
        /// <para>
        /// <b>What replaces it is the representation and nothing else, because there is no other true
        /// bound.</b> An interval is a period; a sweep at any period is well-defined and one longer
        /// than the run simply fires once. Inventing a ceiling to keep a refusal would be choosing a
        /// number with nothing behind it (<c>adr/0052</c>), and the check that mattered — a modulus of
        /// zero — is the floor, which is unchanged. **This is a relaxation and it reaches all four
        /// callers**, because the ground was never specific to one.
        /// </para>
        /// </remarks>
        private uint ReadInterval(TableSyntaxBase table, string? name)
        {
            if (!TryInteger(table, "interval", out long interval, required: true, name))
            {
                return 1;
            }

            if (interval < 1 || interval > uint.MaxValue)
            {
                Refuse(LineOf((SyntaxNodeBase?)Find(table, "interval") ?? table), name,
                    $"interval {interval} is outside 1..{uint.MaxValue}. An interval is a period in "
                    + "Ticks: below 1 it is a modulus of zero, and above the range it does not fit "
                    + "the field it is stored in.");
                return 1;
            }

            return (uint)interval;
        }

        /// <summary>
        /// A Zone Rule's revisit period, and refusals 8, 9 and 10 (<c>adr/0059</c>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Optional, defaulting to <see cref="Ticks.PerDay"/>.</b> The default is <em>derived</em>
        /// rather than picked — a Day is the period the rest of the simulation is denominated in, and
        /// every <c>rate</c> a Ruleset states is 8–32 Ticks, so a Day is comfortably the coarser scale.
        /// That is what lets this ship without an <c>adr/0052</c> ratifier: there is no free parameter
        /// to ratify. A designer may still author one, because <em>how often the industry surveys the
        /// city</em> is legitimately a feel decision.
        /// </para>
        /// <para>
        /// <b>Refusal 9 is <c>pollution_decay_ticks</c>'s refusal against a different denominator.</b>
        /// A decay shorter than the cadence it runs at rounds to zero updates; a revisit period shorter
        /// than the interval it is delivered in asks for more Lots per trigger than the city holds. The
        /// second is the less obvious of the two and was found by reasoning rather than by a test
        /// failing, which is the case for copying it.
        /// </para>
        /// <para>
        /// <b>Refusal 10 refuses the retired key by name, and that is the whole point of it.</b> Every
        /// Ruleset on disk when <c>adr/0059</c> landed carried a <c>sample</c>, and a key the loader
        /// silently ignores is how a designer's tuning stops taking effect with nothing saying so —
        /// which is the same failure class as refusals 6 to 8, arriving from the direction of a
        /// document rather than of a file.
        /// </para>
        /// </remarks>
        private int ReadRevisit(TableSyntaxBase table, string? name, uint interval)
        {
            // Refusal 10 — the key adr/0059 retired. Checked first, because an author who wrote a
            // sample has not written a revisit period, and refusal 8 would otherwise fire on the
            // default and say something confusing about a key they never touched.
            RefuseRetired(table, "sample", name,
                "sample was replaced by revisit_ticks (adr/0059) and is no longer read. It was an "
                + "absolute count of Lots per trigger, so the fraction of the city a Zone Rule "
                + "covered per cycle shrank as the city grew -- at 1,000,000 Citizens a Lot was "
                + $"visited once per 117 Days. Write revisit_ticks = {Ticks.PerDay} (one Day, the "
                + "default) for how long the development industry takes to look at every Lot once.");

            long revisit = Ticks.PerDay;

            if (Find(table, "revisit_ticks") is not null
                && !TryInteger(table, "revisit_ticks", out revisit, required: true, name))
            {
                return Ticks.PerDay;
            }

            // Refusal 8 -- the old sample-of-zero refusal, one level up: a revisit period of zero is
            // a division rather than a slow sweep. The upper bound is the representation's rather
            // than the design's, and it is checked because TOML integers are 64-bit and this is not.
            if (revisit < 1 || revisit > int.MaxValue)
            {
                Refuse(LineOf((SyntaxNodeBase?)Find(table, "revisit_ticks") ?? table), name,
                    $"a revisit period of {revisit} is not a duration this world can hold. It is how "
                    + $"long this Zone Rule takes to look at every Lot once, in Ticks, so it is at "
                    + $"least 1 and at most {int.MaxValue} -- the engine divides by it. If the intent "
                    + "is to disable the Rule, delete it.");
                return Ticks.PerDay;
            }

            // Refusal 9 -- the two numbers are individually sane and jointly are not.
            if (revisit < interval)
            {
                Refuse(LineOf((SyntaxNodeBase?)Find(table, "revisit_ticks") ?? table), name,
                    $"revisit_ticks = {revisit} is shorter than the interval of {interval} it would be "
                    + "delivered in, so one trigger would be asked to evaluate more Lots than the city "
                    + "holds. A revisit period is a duration spread over triggers; shorten the "
                    + "interval or lengthen the period.");
                return Ticks.PerDay;
            }

            return (int)revisit;
        }

        /// <summary>
        /// Stamps every Rule of one kind with whose Rule it is, and refuses one that cannot answer.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Derived from the Rule's own <c>local</c> terms and never authored</b> (<c>adr/0141</c>,
        /// which declined to split a Rule from its Bins: <em>a Rule whose Bins all belong to a tenant
        /// is a tenant's Rule wearing the premises' name</em>). An <c>owner</c> key on a
        /// <c>[[rule]]</c> would state a second time what the terms already state, and two statements
        /// of one fact drift.
        /// </para>
        /// <para>
        /// <b><c>fills</c> counts as a term here, which is what makes a fallback chain answerable.</b>
        /// <c>adr/0045</c> makes a chain a source ladder over <em>one</em> Bin, so a link that relieves
        /// a tenant's Bin from a premises Bin is the mixed case arriving one link along — and it is
        /// caught by the same comparison rather than needing a walk over the chain.
        /// </para>
        /// <para>
        /// ⚠ <b>A <c>local</c> term naming a Resource the kind declares no Bin for reads as the
        /// premises'</b>, because that is what it has always been. It is <em>also</em> a Rule that can
        /// never fire, and nothing refuses it — filed as a finding rather than fixed here, since
        /// refusing it is a change to what loads and not to what a tenancy is.
        /// </para>
        /// </remarks>
        private void ApplyTenancies(
            RuleDefinition[] rules,
            Term[] inputs,
            Term[] outputs,
            List<BinDeclaration> bins,
            int binFirst,
            int kindIndex)
        {
            for (int r = 0; r < rules.Length; r++)
            {
                RuleDefinition rule = rules[r];

                if (rule.Kind != kindIndex + 1)
                {
                    continue;
                }

                string? name = TryString(_ruleTables[r], "name", out string? found, required: false)
                    ? found
                    : null;

                bool decided = false;
                BinTenancy tenancy = BinTenancy.Premises;
                int terms = rule.InputCount + rule.OutputCount + (rule.HasFills ? 1 : 0);

                for (int t = 0; t < terms; t++)
                {
                    BinRef addressed =
                        t < rule.InputCount
                            ? inputs[rule.InputFirst + t].Bin
                            : t < rule.InputCount + rule.OutputCount
                                ? outputs[rule.OutputFirst + t - rule.InputCount].Bin
                                : rule.Fills;

                    if (addressed.Scope != Scope.Local)
                    {
                        continue;
                    }

                    BinTenancy holds = BinTenancy.Premises;

                    for (int b = binFirst; b < bins.Count; b++)
                    {
                        if (bins[b].Resource == addressed.Resource)
                        {
                            holds = bins[b].Tenancy;
                            break;
                        }
                    }

                    if (!decided)
                    {
                        tenancy = holds;
                        decided = true;
                        continue;
                    }

                    if (holds != tenancy)
                    {
                        Refuse(LineOf(_ruleTables[r]), name,
                            "this Rule's local terms address both the premises' Bins and the "
                            + "Occupant's. A local term is free because the Bin already belongs to "
                            + "whoever runs the Rule, so a Rule with two owners has no subject to "
                            + "run on; a term crossing an ownership boundary is a trade, which is "
                            + "scope = \"pool\".");
                        break;
                    }
                }

                rules[r] = rule with { Tenancy = tenancy };
            }
        }

        private void ReadBins(TableSyntaxBase table, string? kind, List<BinDeclaration> into)
        {
            KeyValueSyntax? entry = Find(table, "bins", RulesetKeyKind.Array);

            if (entry is null)
            {
                return;
            }

            if (entry.Value is not ArraySyntax array)
            {
                Refuse(LineOf(entry), kind, "bins must be an array of inline tables.");
                return;
            }

            int first = into.Count;

            foreach (ArrayItemSyntax item in array.Items)
            {
                if (item.Value is not InlineTableSyntax inline)
                {
                    Refuse(LineOf(item), kind, "every entry of bins must be an inline table.");
                    continue;
                }

                if (!TryResource(inline, kind, LineOf(inline), out ResourceId resource))
                {
                    continue;
                }

                // A Money Bin has no ceiling (CONTEXT -> Resource), so authoring one is refused
                // rather than ignored: a number the loader silently drops is a number a designer
                // will tune and then wonder about.
                bool unbounded = _families[resource.Raw - 1] == ResourceFamily.Money;
                BinCapacity declared;

                if (unbounded)
                {
                    if (Find(inline, "capacity") is not null)
                    {
                        Refuse(LineOf(inline), kind,
                            "a money Bin declares no capacity. Money has no physical ceiling, and a "
                            + "finite one would mean an actor too full of money to be paid -- a sale "
                            + "failing on space because the seller is rich.");
                        continue;
                    }

                    declared = BinCapacity.Unbounded;
                }
                else
                {
                    if (!TryInteger(inline, "capacity", out long capacity, required: true, kind))
                    {
                        continue;
                    }

                    // The upper bound is the representation's and it moved with adr/0065: a Bin holds
                    // a long, so a ceiling authored at long.MaxValue is indistinguishable from an
                    // unbounded one -- which that ADR permits rather than refuses, because there is no
                    // unbounded, only a ceiling far enough away that approaching it is a defect.
                    if (capacity < 1)
                    {
                        Refuse(LineOf(inline), kind,
                            $"capacity {capacity} is not a positive quantity. A Bin of capacity zero "
                            + "can never be deposited into, which is a Bin the kind should not "
                            + "declare.");
                        continue;
                    }

                    declared = BinCapacity.Of(capacity);
                }

                if (into.Skip(first).Any(b => b.Resource == resource))
                {
                    Refuse(LineOf(inline), kind,
                        "this kind declares two Bins for one Resource. One Bin, one Resource: two "
                        + "would make the local scope draw from whichever the list reached first, "
                        + "which is a balance outcome decided by allocation order.");
                    continue;
                }

                if (!TryTenancy(inline, kind, out BinTenancy tenancy))
                {
                    continue;
                }

                into.Add(new BinDeclaration(resource, declared, tenancy));
            }
        }

        // ---- the refusals ---------------------------------------------------------------

        /// <summary>
        /// Refusals 171 and 172 — a key nothing reads is refused, and so is a key above the first
        /// section header.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b><c>plans/0041</c> G31.</b> A misspelled key was silently ignored: the loader asked for
        /// the name it wanted, did not find it, and used the default. ***A typo and an omission were
        /// the same file to this parser***, which is the one class of authoring mistake a Ruleset
        /// could make without being told.
        /// </para>
        /// <para>
        /// ⚠ <b>The permitted set is derived, not declared, and that is the whole design.</b> G31's
        /// own proposed fix was "every table declaring its permitted key set" — twenty-two lists
        /// hand-written beside twenty-two readers, kept in step by nothing. This build instead
        /// records what <see cref="Find"/> was <em>asked</em> for and treats that as the permission.
        /// Adding a key to a reader adds it to the permitted set in the same edit; there is no second
        /// place to forget.
        /// </para>
        /// <para>
        /// 🔴 <b>The union is per section shape and not per table, because a reader may ask
        /// conditionally.</b> <c>[[building]]</c> is asked for <c>business</c> only when the file has
        /// businesses in it, so a document whose single Building never triggered that read would
        /// otherwise refuse a key the loader plainly supports. Unioning across every table of one
        /// shape means a key permitted anywhere in a section is permitted throughout it — which is
        /// looser than a hand-authored list and is the direction to be loose in.
        /// </para>
        /// <para>
        /// ⚠ <b>It runs only when nothing else has refused</b>, under <c>Read</c>'s existing staging.
        /// A key on a section that is itself unknown would otherwise report twice, and
        /// <c>adr/0048</c>'s rule is that one mistake reads as one sentence.
        /// </para>
        /// </remarks>
        private void RefuseUnknownKeys(DocumentSyntax document)
        {
            var permitted = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            GatherPermitted(document, string.Empty, permitted);

            // A key above the first section header belongs to no table, so no reader has ever been
            // in a position to ask for it. It is not merely unread -- it is unreachable.
            foreach (KeyValueSyntax bare in document.KeyValues)
            {
                Refuse(LineOf(bare), null,
                    $"'{NameOf(bare.Key)}' sits above the first section header, where a Ruleset has "
                    + "no keys at all. Every key belongs to a section -- move it under the one that "
                    + "reads it.");
            }

            RefuseUnknownKeysIn(document, string.Empty, permitted);
        }

        /// <summary>
        /// Every section this build can read, every key it reads there, and what shape it wants.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The same walk <see cref="RefuseUnknownKeys"/> does, carrying the type as well as the
        /// name.</b> It is a separate method rather than a widening of that one because the refusal
        /// path must keep working on a file with refusals in it, and this must work on a file
        /// <em>without</em> — it is called after <c>Read</c> has finished, for a caller that wants
        /// to describe the format rather than to check a file.
        /// </para>
        /// <para>
        /// ⚠ <b>It describes what the READERS ask for, not what the file contains</b> — so a key the
        /// shipped Rulesets never use is still reported, provided some file declares the section it
        /// lives in. 🔴 <b>What it cannot report is a section no file declares at all</b>: a reader
        /// only ever asks a table that exists, so a section nothing demonstrates is invisible here.
        /// ***That is why the generator unions across every shipped Ruleset rather than reading
        /// one*** — and why the schema it produces must leave unknown keys permitted.
        /// </para>
        /// </remarks>
        public Dictionary<string, Dictionary<string, RulesetKeyKind>> Surface()
        {
            var surface = new Dictionary<string, Dictionary<string, RulesetKeyKind>>(
                StringComparer.Ordinal);

            if (_document is not null)
            {
                GatherSurface(_document, string.Empty, surface);
            }

            return surface;
        }

        private void GatherSurface(
            SyntaxNode node, string context, Dictionary<string, Dictionary<string, RulesetKeyKind>> into)
        {
            string here = ContextOf(node, context);

            if (_consulted.TryGetValue(node, out HashSet<string>? asked))
            {
                if (!into.TryGetValue(here, out Dictionary<string, RulesetKeyKind>? union))
                {
                    union = new Dictionary<string, RulesetKeyKind>(StringComparer.Ordinal);
                    into[here] = union;
                }

                _keyKinds.TryGetValue(node, out Dictionary<string, RulesetKeyKind>? typed);
                _retired.TryGetValue(node, out HashSet<string>? gone);

                foreach (string key in Ordered(asked))
                {
                    // A retired key is permitted and is not on offer. It stays in `asked`, so
                    // RefuseUnknownKeys still lets an author write it and still gets the sentence
                    // saying where it went -- and it leaves here, so no editor completes it.
                    if (gone is not null && gone.Contains(key))
                    {
                        continue;
                    }

                    RulesetKeyKind kind = typed is not null && typed.TryGetValue(key, out RulesetKeyKind found)
                        ? found
                        : RulesetKeyKind.Unknown;

                    // One holder may be untyped where another was typed -- the same section appears
                    // once per [[table]] in the file -- so a known kind is never lost to a later
                    // Unknown.
                    if (!union.TryGetValue(key, out RulesetKeyKind already)
                        || already == RulesetKeyKind.Unknown)
                    {
                        union[key] = kind;
                    }
                }
            }

            for (int i = 0; i < node.ChildrenCount; i++)
            {
                if (node.GetChild(i) is SyntaxNode child)
                {
                    GatherSurface(child, here, into);
                }
            }
        }

        /// <summary>Unions what every holder of one section shape was asked for.</summary>
        private void GatherPermitted(
            SyntaxNode node, string context, Dictionary<string, HashSet<string>> into)
        {
            string here = ContextOf(node, context);

            if (_consulted.TryGetValue(node, out HashSet<string>? asked))
            {
                if (!into.TryGetValue(here, out HashSet<string>? union))
                {
                    union = new HashSet<string>(StringComparer.Ordinal);
                    into[here] = union;
                }

                union.UnionWith(asked);
            }

            for (int i = 0; i < node.ChildrenCount; i++)
            {
                if (node.GetChild(i) is SyntaxNode child)
                {
                    GatherPermitted(child, here, into);
                }
            }
        }

        private void RefuseUnknownKeysIn(
            SyntaxNode node, string context, Dictionary<string, HashSet<string>> permitted)
        {
            string here = ContextOf(node, context);

            if (node is TableSyntaxBase or InlineTableSyntax)
            {
                permitted.TryGetValue(here, out HashSet<string>? union);

                foreach (KeyValueSyntax pair in KeyValuesOf(node))
                {
                    string key = NameOf(pair.Key);

                    if (union?.Contains(key) == true)
                    {
                        continue;
                    }

                    Refuse(LineOf(pair), null,
                        $"'{key}' is not a key of {here}, so nothing would read it. "
                        + Suggestion(key, union)
                        + $" The keys of {here} are {Listed(union)}.");
                }
            }

            for (int i = 0; i < node.ChildrenCount; i++)
            {
                if (node.GetChild(i) is SyntaxNode child)
                {
                    RefuseUnknownKeysIn(child, here, permitted);
                }
            }
        }

        /// <summary>Names a holder the way the file writes it, so a refusal can be searched for.</summary>
        private static string ContextOf(SyntaxNode node, string outer) => node switch
        {
            TableArraySyntax table => $"[[{NameOf(table.Name)}]]",
            TableSyntax table => $"[{NameOf(table.Name)}]",
            InlineTableSyntax => outer,
            KeyValueSyntax pair => outer.Length == 0
                ? NameOf(pair.Key)
                : $"{outer} {NameOf(pair.Key)}",
            _ => outer,
        };

        private static IEnumerable<KeyValueSyntax> KeyValuesOf(SyntaxNode node)
        {
            switch (node)
            {
                case TableSyntaxBase table:
                    foreach (KeyValueSyntax item in table.Items)
                    {
                        yield return item;
                    }

                    break;

                case InlineTableSyntax inline:
                    foreach (InlineTableItemSyntax item in inline.Items)
                    {
                        if (item.KeyValue is { } pair)
                        {
                            yield return pair;
                        }
                    }

                    break;
            }
        }

        /// <summary>
        /// Names the near miss when there is exactly one, and says nothing when there is not.
        /// </summary>
        /// <remarks>
        /// <b>A typo is the case this refusal exists for</b>, so the message earns its length by
        /// naming the key that was probably meant. The distance is a plain edit distance bounded at
        /// two, and ⚠ <b>a tie offers nothing</b> — two equally-near keys is a guess rather than a
        /// suggestion, and the full list below it is the better answer.
        /// </remarks>
        private static string Suggestion(string key, HashSet<string>? union)
        {
            if (union is null)
            {
                return string.Empty;
            }

            string? nearest = null;
            int best = 3;
            bool tied = false;

            foreach (string candidate in Ordered(union))
            {
                int distance = Distance(key, candidate);

                if (distance < best)
                {
                    best = distance;
                    nearest = candidate;
                    tied = false;
                }
                else if (distance == best)
                {
                    tied = true;
                }
            }

            return nearest is null || tied ? string.Empty : $"Did you mean '{nearest}'?";
        }

        /// <summary>Edit distance, bounded — it is spelling advice and never a hot path.</summary>
        private static int Distance(string left, string right)
        {
            int[] previous = new int[right.Length + 1];
            int[] current = new int[right.Length + 1];

            for (int column = 0; column <= right.Length; column++)
            {
                previous[column] = column;
            }

            for (int row = 1; row <= left.Length; row++)
            {
                current[0] = row;

                for (int column = 1; column <= right.Length; column++)
                {
                    int substitution = previous[column - 1]
                        + (left[row - 1] == right[column - 1] ? 0 : 1);
                    int deletion = previous[column] + 1;
                    int insertion = current[column - 1] + 1;
                    current[column] = substitution < deletion
                        ? (substitution < insertion ? substitution : insertion)
                        : (deletion < insertion ? deletion : insertion);
                }

                (previous, current) = (current, previous);
            }

            return previous[right.Length];
        }

        private static string Listed(HashSet<string>? union) =>
            union is null || union.Count == 0
                ? "not yet known -- nothing in this build reads that section"
                : string.Join(", ", Ordered(union));

        private static string[] Ordered(HashSet<string> union)
        {
            string[] keys = [.. union];
            Array.Sort(keys, StringComparer.Ordinal);
            return keys;
        }

        /// <summary>
        /// Refusal 3 — an unquoted decimal is refused by name, never coerced.
        /// </summary>
        /// <remarks>
        /// <b>The whole document, including sections this build does not read.</b> A decimal is a
        /// lexical fact, and the hazard <c>adr/0048</c> names is not that <em>this</em> number reaches
        /// the simulation as a <c>double</c> — it is that the file format admits one at all.
        /// </remarks>
        private void RefuseUnquotedDecimals(SyntaxNode node)
        {
            if (node is FloatValueSyntax number)
            {
                Refuse(LineOf(number), null,
                    $"{number.Token?.Text ?? "a decimal"} is an unquoted decimal. Write it as a "
                    + "quoted string -- \"0.15\" -- so that no floating-point value ever exists on "
                    + "the path into the simulation. adr/0048.");
                return;
            }

            for (int i = 0; i < node.ChildrenCount; i++)
            {
                if (node.GetChild(i) is SyntaxNode child)
                {
                    RefuseUnquotedDecimals(child);
                }
            }
        }

        /// <summary>
        /// Refusal 1 — an <c>on_fail</c> cycle.
        /// </summary>
        /// <remarks>
        /// <b>The graph has out-degree 1, so a chain is a path and a cycle is a malformed Ruleset
        /// rather than a runtime hazard</b> (<c>adr/0045</c>). Out-degree 1 is also what makes the
        /// check this cheap: from each rule, follow the single edge and stop when a rule is met twice
        /// on this walk. No colouring, no stack, no general cycle algorithm.
        /// </remarks>
        private void RefuseCycles(RuleDefinition[] rules)
        {
            var walked = new int[rules.Length];

            for (int start = 0; start < rules.Length; start++)
            {
                RuleId at = rules[start].OnFail;

                while (!at.IsNone)
                {
                    int index = at.Raw - 1;

                    if (index == start || walked[index] == start + 1)
                    {
                        Refuse(LineOf(_ruleTables[start]), NameOfRule(start),
                            "its on_fail chain is a cycle. A chain is a source ladder over one Bin "
                            + "and must end -- in a reporting terminal, or in nothing -- so a cycle "
                            + "is a Ruleset that would walk for ever the first time it was short.");
                        return;
                    }

                    walked[index] = start + 1;
                    at = rules[index].OnFail;
                }
            }
        }

        /// <summary>
        /// Refusal 4 — a Rule's explicit money terms must balance, because money is conserved.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Money is the only Resource that never becomes anything</b> (<c>adr/0024</c>): flour
        /// legitimately becomes bread, and a pound only ever changes hands. So a Rule drawing money
        /// and returning none has not spent it — it has <em>destroyed</em> it, and the Outside
        /// Connection is the city's only sink. <c>02 §4.3</c>'s own worked example did exactly this
        /// for six slices, which is the argument for a refusal rather than a convention: it is
        /// invisible in review and silent at run time, and it shows up as a money supply that drains
        /// with no balance-of-payments story behind it.
        /// </para>
        /// <para>
        /// <b>It counts the <em>explicit</em> terms only, and that is not a loophole.</b> Under
        /// <c>adr/0050</c> a term crossing an ownership boundary settles its own payment implicitly and
        /// generates a balanced pair by construction, so there is nothing here to check. What this
        /// catches is money a designer wrote down with nowhere for it to go.
        /// </para>
        /// <para>
        /// <b>It refuses more than it needs to today, deliberately.</b> A wage — a Business paying the
        /// Household that works there — and an import payment both have real counterparties that no
        /// scope can currently name, so both would be refused. Neither is writeable anyway, and a
        /// refusal that says so is better than a leak that does not.
        /// </para>
        /// </remarks>
        private void RefuseUnbalancedMoney(RuleDefinition[] rules, Term[] inputs, Term[] outputs)
        {
            for (int i = 0; i < rules.Length; i++)
            {
                RuleDefinition rule = rules[i];
                long drawn = MoneyIn(inputs, rule.InputFirst, rule.InputCount);
                long returned = MoneyIn(outputs, rule.OutputFirst, rule.OutputCount);

                if (drawn == returned)
                {
                    continue;
                }

                string verb = drawn > returned ? "destroys" : "creates";
                long leak = drawn > returned ? drawn - returned : returned - drawn;

                Refuse(LineOf(_ruleTables[i]), NameOfRule(i),
                    $"this rule {verb} {leak} money per application. Money is conserved -- never "
                    + "created or destroyed inside the city, the Outside Connection being its only "
                    + "source and sink -- so every money term needs a counterparty. A cost paid to "
                    + "nobody is a leak, not a cost. If the counterparty is a market, do not write "
                    + "the payment at all: a pool term settles its own (adr/0050).");
            }
        }

        /// <summary>Sums the money amounts in a slice of a Rule's terms.</summary>
        /// <remarks>
        /// <b>Named <c>MoneyIn</c> rather than <c>Money</c>, and the rename is the finding.</b> A
        /// private method named after a type makes that type unnameable everywhere inside the class:
        /// <c>Money.Zero</c> and <c>new Money(x)</c> both resolve to this method and fail to compile,
        /// so the next reader to want the quantity in this file reaches for a fully-qualified name and
        /// leaves it there. Found on milestone 10 task 5, which wanted a <c>Money</c> in
        /// <see cref="ReadOpeningBalance"/> five sentences away. ***A helper named after a type does
        /// not shadow one call site, it shadows the whole class.***
        /// </remarks>
        private long MoneyIn(Term[] terms, int first, int count)
        {
            long total = 0;

            for (int i = first; i < first + count; i++)
            {
                if (_families[terms[i].Bin.Resource.Raw - 1] == ResourceFamily.Money)
                {
                    total += terms[i].Amount;
                }
            }

            return total;
        }

        /// <summary>
        /// Refusal 2 — every link of a chain relieves the same Bin the head failed on.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Computed as set arithmetic rather than declared</b>, which is what <c>02 §4.1</c>
        /// describes: <em>every link in a well-formed chain rescues by relieving the same Bin the head
        /// failed on.</em> The candidate set starts as the head's own Bins — inputs and outputs both,
        /// because <c>adr/0045</c>'s <em>blocking</em> generalises over a short input and a full
        /// output — and each link intersects it with what that link relieves. An empty intersection
        /// is a chain that cannot rescue anything its head could fail on.
        /// </para>
        /// <para>
        /// <b>A link relieves a Bin by outputting to it, by drawing from it, or by declaring
        /// <c>fills</c>.</b> The third is what an asynchronous rescue needs: <c>request_shipment</c>
        /// dispatches a Shipment and outputs nothing this Tick, so without the declaration it is
        /// indistinguishable from a link that rescues nothing.
        /// </para>
        /// <para>
        /// <b>A reporting terminal is exempt, and that is not a loophole.</b> It rescues nothing by
        /// design — it records a condition and leaves the chain failed — so requiring it to relieve
        /// the head's Bin would refuse every chain the corpus's own worked example contains.
        /// </para>
        /// </remarks>
        private void RefuseUnrelievedChains(RuleDefinition[] rules, Term[] inputs, Term[] outputs)
        {
            var isTarget = new bool[rules.Length];

            foreach (RuleDefinition rule in rules)
            {
                if (!rule.OnFail.IsNone)
                {
                    isTarget[rule.OnFail.Raw - 1] = true;
                }
            }

            for (int head = 0; head < rules.Length; head++)
            {
                if (rules[head].OnFail.IsNone || isTarget[head])
                {
                    continue;
                }

                HashSet<BinRef> candidates = BinsOf(rules[head], inputs, outputs);

                for (RuleId at = rules[head].OnFail; !at.IsNone; at = rules[at.Raw - 1].OnFail)
                {
                    RuleDefinition link = rules[at.Raw - 1];

                    if (link.IsTerminal)
                    {
                        continue;
                    }

                    HashSet<BinRef> relieved = BinsOf(link, inputs, outputs);

                    if (link.HasFills)
                    {
                        relieved.Add(link.Fills);
                    }

                    candidates.IntersectWith(relieved);
                }

                if (candidates.Count == 0)
                {
                    Refuse(LineOf(_ruleTables[head]), NameOfRule(head),
                        "no Bin is relieved by every link of its on_fail chain. A chain is a source "
                        + "ladder over one Bin (adr/0045), so each link must output to that Bin, "
                        + "draw from it, or declare fills = { scope, resource } when its rescue "
                        + "arrives later. A link whose rescue arrives later and does not declare it "
                        + "is indistinguishable from one that rescues nothing.");
                }
            }
        }

        private void RefuseUnterminatedChains(RuleDefinition[] rules)
        {
            var isTarget = new bool[rules.Length];

            foreach (RuleDefinition rule in rules)
            {
                if (!rule.OnFail.IsNone)
                {
                    isTarget[rule.OnFail.Raw - 1] = true;
                }
            }

            for (int head = 0; head < rules.Length; head++)
            {
                if (rules[head].OnFail.IsNone || isTarget[head])
                {
                    continue;
                }

                int last = head;

                for (RuleId at = rules[head].OnFail; !at.IsNone; at = rules[at.Raw - 1].OnFail)
                {
                    last = at.Raw - 1;
                }

                if (!rules[last].IsTerminal)
                {
                    Refuse(LineOf(_ruleTables[last]), NameOfRule(last),
                        "ends an on_fail chain without recording anything. The last link of a chain "
                        + "is a reporting terminal (02 §4.1, adr/0045): it names a condition through "
                        + "reports and leaves the chain failed. A chain that simply ends leaves the "
                        + "Building failed with nothing for a player to read, which is the silent "
                        + "non-event that section bans predicates for.");
                }
            }
        }

        private static HashSet<BinRef> BinsOf(RuleDefinition rule, Term[] inputs, Term[] outputs)
        {
            var bins = new HashSet<BinRef>();

            for (int i = 0; i < rule.InputCount; i++)
            {
                bins.Add(inputs[rule.InputFirst + i].Bin);
            }

            for (int i = 0; i < rule.OutputCount; i++)
            {
                bins.Add(outputs[rule.OutputFirst + i].Bin);
            }

            return bins;
        }

        // ---- reading one value ----------------------------------------------------------------

        private bool TryScope(InlineTableSyntax inline, string? rule, out Scope scope)
        {
            scope = Scope.Local;

            if (!TryString(inline, "scope", out string? name, required: true, rule))
            {
                return false;
            }

            switch (name)
            {
                case "local": scope = Scope.Local; return true;
                case "pool": scope = Scope.Pool; return true;
                case "global": scope = Scope.Global; return true;
                case "map": scope = Scope.Map; return true;

                default:
                    Refuse(LineOf(inline), rule,
                        $"'{name}' is not a scope. The scopes are local, pool, global and map, and "
                        + "there is deliberately no proximity scope: movers choose, Rules transform.");
                    return false;
            }
        }

        /// <summary>
        /// Whose level a declared Bin holds. Absent means the premises', which is what every Ruleset
        /// written before <c>adr/0141</c> already meant.
        /// </summary>
        /// <remarks>
        /// <b>The key is <c>owner</c> and the values are <c>CONTEXT.md</c>'s two words for the two
        /// sides of a tenancy</b>, so that a Ruleset says the thing the vocabulary says. A third
        /// value is refused by name rather than defaulted: a Bin whose owner a designer meant to
        /// state and mis-spelled would otherwise stay on the premises silently, and the symptom —
        /// stock that does not follow a tenant out — is several milestones away from the typo.
        /// </remarks>
        private bool TryTenancy(InlineTableSyntax inline, string? kind, out BinTenancy tenancy)
        {
            tenancy = BinTenancy.Premises;

            if (Find(inline, "owner") is null)
            {
                return true;
            }

            if (!TryString(inline, "owner", out string? name, required: true, kind))
            {
                return false;
            }

            switch (name)
            {
                case "premises": tenancy = BinTenancy.Premises; return true;
                case "occupant": tenancy = BinTenancy.Occupant; return true;
                case "business": tenancy = BinTenancy.Business; return true;

                default:
                    Refuse(LineOf(inline), kind,
                        $"'{name}' is not a Bin owner. The owners are premises, occupant and "
                        + "business, and the test is whether the Bin empties when the tenant "
                        + "leaves: flour goes with the baker, the roof does not. Occupant is the "
                        + "HOUSEHOLD tenanting the premises and business is the TRADE tenanting "
                        + "them, and they are separate because one kind holds both -- a dwelling "
                        + "that comes with a shop houses a family and a Business in the same "
                        + "Building. The premises declare the capacity in all three cases.");
                    return false;
            }
        }

        private bool TryResource(
            SyntaxNode holder, string? rule, int line, out ResourceId resource)
        {
            resource = default;

            if (!TryString(holder, "resource", out string? name, required: true, rule))
            {
                return false;
            }

            if (!_resources.TryGetValue(name!, out ushort id))
            {
                Refuse(line, rule, $"'{name}' is not a declared [[resource]].");
                return false;
            }

            resource = new ResourceId(id);
            return true;
        }

        private bool TryString(
            SyntaxNode holder, string key, out string? value, bool required, string? rule = null)
        {
            value = null;
            KeyValueSyntax? entry = Find(holder, key, RulesetKeyKind.Quoted);

            if (entry is null)
            {
                if (required)
                {
                    Refuse(LineOf(holder), rule, $"no {key}.");
                }

                return false;
            }

            if (entry.Value is not StringValueSyntax text)
            {
                Refuse(LineOf(entry), rule, $"{key} must be a quoted string.");
                return false;
            }

            value = text.Value;
            return true;
        }

        /// <summary>Reads a <c>true</c>/<c>false</c> key, absent meaning <c>false</c>.</summary>
        /// <remarks>
        /// <b>The one predicate in this surface</b>, and it takes no <c>required</c> parameter for
        /// that reason: every other key answers <em>how much</em> and can be missing in a way that
        /// means something else, where an absent predicate is simply <em>no</em>. ⚠ <b>A stated
        /// <c>false</c> and an absent key are the same city</b>, which is what makes writing the
        /// word optional rather than ceremonial.
        /// </remarks>
        private bool TryBoolean(SyntaxNode holder, string key, out bool value, string? rule = null)
        {
            value = false;
            KeyValueSyntax? entry = Find(holder, key, RulesetKeyKind.Truth);

            if (entry is null)
            {
                return false;
            }

            if (entry.Value is not BooleanValueSyntax truth)
            {
                Refuse(LineOf(entry), rule, $"{key} must be true or false.");
                return false;
            }

            value = truth.Value;
            return true;
        }

        private bool TryInteger(
            SyntaxNode holder, string key, out long value, bool required, string? rule = null)
        {
            value = 0;
            KeyValueSyntax? entry = Find(holder, key, RulesetKeyKind.Whole);

            if (entry is null)
            {
                if (required)
                {
                    Refuse(LineOf(holder), rule, $"no {key}.");
                }

                return false;
            }

            if (entry.Value is not IntegerValueSyntax number)
            {
                Refuse(LineOf(entry), rule, $"{key} must be a whole number.");
                return false;
            }

            value = number.Value;
            return true;
        }

        // ---- identity -------------------------------------------------------------------------

        /// <summary>
        /// One key per declaration, in id order: the content hash of the name it was declared under.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>This is where a declaration stops being its position in a file.</b> Ids are assigned by
        /// declaration order, so deleting one <c>[[resource]]</c> shifts every id below it — and a
        /// live Bin row holds an id, not a name. The core cannot see the names (<c>adr/0048</c>), so
        /// it is handed the one thing that survives the boundary and still distinguishes two
        /// declarations: a number derived from the name.
        /// </para>
        /// <para>
        /// <b>Hashed rather than interned, because the two Rulesets are never in one process
        /// together as text.</b> A reload compares the Ruleset in force — which may have been loaded
        /// from a file that has since been overwritten — against the new one, so any scheme that
        /// resolves through a shared string table would have to keep the old file's strings alive.
        /// A hash is the same 8 bytes whenever it is computed.
        /// </para>
        /// <para>
        /// <b>A collision would silently merge two declarations</b>, and it is not defended against
        /// here: <see cref="ContentHash"/> is the function the State Hash itself is built from, and a
        /// 64-bit collision between two short identifiers in one Ruleset is not a risk this project
        /// treats differently from the hash trace it already trusts.
        /// </para>
        /// </remarks>
        private static ulong[] Keys<TId>(Dictionary<string, TId> named)
            where TId : struct, IConvertible
        {
            var keys = new ulong[named.Count];

            // The dictionary is a lookup and 05 section 4 lint 3 bans walking one for a simulation
            // decision. This is not one: the loader runs on a keystroke, outside the Tick, and the
            // result is written by id so the enumeration order cannot reach the output.
            foreach (KeyValuePair<string, TId> entry in named)
            {
                keys[entry.Value.ToInt32(CultureInfo.InvariantCulture) - 1] =
                    ContentHash.Of(Encoding.UTF8.GetBytes(entry.Key));
            }

            return keys;
        }

        // ---- policies ---------------------------------------------------------------------------

        /// <summary>
        /// Every <c>[[policy]]</c> table — refusals 60 to 68.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Shaped on <see cref="ReadZoneRules"/>, because a Policy is that section's sibling</b>
        /// (<c>02 §4.2</c>: the Sweep family has two members). Same idiom throughout — the name is
        /// read first and non-required, every failed key leaves a sentinel and the loop continues, and
        /// nothing aborts on a refusal.
        /// </para>
        /// <para>
        /// ⚠ <b>There is no money-balance refusal here and that is structural rather than an
        /// omission.</b> A <c>[[rule]]</c>'s money terms are two free-form lists, so <b>refusal 4</b>
        /// has to walk them and check that what is drawn is returned. A Policy states a
        /// <c>from</c> and a <c>to</c>, so the same quantity leaves one Bin and enters the other by
        /// construction. ***A transfer written as a direction cannot leak; one written as two lists
        /// has to be checked.***
        /// </para>
        /// </remarks>
        private PolicyDefinition[] ReadPolicies(out ulong[] keys)
        {
            var definitions = new List<PolicyDefinition>(_policyTables.Count);
            var names = new ulong[_policyTables.Count];

            foreach (TableSyntaxBase table in _policyTables)
            {
                string? name = TryString(table, "name", out string? found, required: false)
                    ? found
                    : null;

                // Govern names a Policy by this and by nothing else, so an unnamed table keys to zero
                // and is unaddressable rather than addressable-by-position. Ruleset.PolicyKeys carries
                // why: a position is not an identity once saved state points at one.
                names[definitions.Count] = name is null
                    ? 0
                    : ContentHash.Of(Encoding.UTF8.GetBytes(name));

                _policyNames.Add(name);

                PolicySubject subject = ReadSubject(table, name);
                uint interval = ReadInterval(table, name);
                ApplyCount apply = ReadApply(table, name, ScopeFor(subject));
                (Scope from, Scope to, ResourceId resource, int amount) = ReadTransfer(table, name);

                definitions.Add(new PolicyDefinition(subject, interval, apply, from, to, resource, amount));
            }

            keys = names;

            return [.. definitions];
        }

        /// <summary>Which Readout scope a Policy over <paramref name="subject"/> reads against.</summary>
        private static ReadoutScope ScopeFor(PolicySubject subject) =>
            subject switch
            {
                PolicySubject.Business => ReadoutScope.Business,
                PolicySubject.Building => ReadoutScope.Building,
                _ => ReadoutScope.Household,
            };

        /// <summary>The <c>sweeps</c> key — refusals 60 and 61.</summary>
        /// <remarks>
        /// <para>
        /// <b>Refusal 61 refuses a population the engine has, and that is the unusual half.</b>
        /// <c>building</c> is declared in <see cref="PolicySubject"/> because <c>02 §4.2</c> names
        /// three, and a Ruleset may not author it because nothing sweeps it. Accepting one would
        /// produce a Policy that triggers, reaches nobody and reports nothing — the silent non-event
        /// <c>02 §4.1</c> bans — where a refusal names the milestone.
        /// </para>
        /// <para>
        /// ⚠ <b><c>business</c> was refused here too until milestone 27 task 9, and the refusal's own
        /// sentence is what the task built</b>: <em>a Business has a balance and no pass that moves
        /// it</em>. <c>adr/0149</c> supplied the pass. ***The Building half is untouched and is not the
        /// same kind of absence*** — a Business population is every live row, and a Building
        /// population is whichever rows a predicate picks, so what is missing there is a mechanism
        /// rather than a loop.
        /// </para>
        /// </remarks>
        private PolicySubject ReadSubject(TableSyntaxBase table, string? name)
        {
            if (!TryString(table, "sweeps", out string? subject, required: true, name))
            {
                return PolicySubject.Household;
            }

            switch (subject)
            {
                case "household":
                    return PolicySubject.Household;

                case "business":
                    return PolicySubject.Business;

                case "building":
                    Refuse(LineOf((SyntaxNodeBase?)Find(table, "sweeps") ?? table), name,
                        "sweeps = \"building\" names a population 02 section 4.2 declares and this "
                        + "build does not sweep. A Building population is whichever standing "
                        + "Buildings a predicate selects, and there is no predicate -- so this is a "
                        + "missing mechanism rather than a missing loop. The populations that sweep "
                        + "are \"household\" and \"business\".");

                    return PolicySubject.Household;

                default:
                    Refuse(LineOf((SyntaxNodeBase?)Find(table, "sweeps") ?? table), name,
                        $"sweeps = \"{subject}\" is not a population. 02 section 4.2 names three -- "
                        + "\"household\", \"business\", \"building\" -- of which the first two are "
                        + "the ones this build sweeps.");

                    return PolicySubject.Household;
            }
        }

        /// <summary>The <c>transfer</c> inline table — refusals 62 to 68.</summary>
        /// <remarks>
        /// <para>
        /// <b>Four keys, all required, and the pair of scopes is what makes this a transfer rather
        /// than a term list.</b> <c>02 §4.3</c>: <em>"global names the treasury, and it appears only
        /// as the far end of an explicit transfer — local money out, global money in, balancing
        /// within the one atomic Rule."</em> This is that sentence as a syntax.
        /// </para>
        /// <para>
        /// <b><c>pool</c> and <c>map</c> are refused by name rather than falling through.</b> A pool
        /// term is a <em>purchase</em> whose payment is implicit at the prevailing price
        /// (<c>adr/0050</c>), so a Policy writing one would author the money side of a trade the
        /// design says is never authored; a map term is a <c>MapEmission</c> and has no capacity to
        /// draw from. Both would otherwise land in <c>RuleEngine.Bin</c>'s named holes at run time,
        /// which is a crash where a file and a line will do.
        /// </para>
        /// </remarks>
        private (Scope From, Scope To, ResourceId Resource, int Amount) ReadTransfer(
            TableSyntaxBase table, string? name)
        {
            var fallback = (Scope.Local, Scope.Global, default(ResourceId), 1);

            KeyValueSyntax? entry = Find(table, "transfer");

            if (entry is null)
            {
                Refuse(LineOf(table), name,
                    "no transfer. A Policy of the flow kind moves money, and it states which way: "
                    + "transfer = { from = \"local\", to = \"global\", resource = \"money\", "
                    + "amount = 1 }.");

                return fallback;
            }

            if (entry.Value is not InlineTableSyntax inline)
            {
                Refuse(LineOf(entry), name,
                    "transfer must be an inline table: { from, to, resource, amount }.");

                return fallback;
            }

            Scope from = ReadTransferScope(inline, "from", name);
            Scope to = ReadTransferScope(inline, "to", name);

            if (from == to)
            {
                Refuse(LineOf(entry), name,
                    $"transfer goes from \"{Spell(from)}\" to \"{Spell(to)}\". A transfer between one "
                    + "Bin and itself nets to zero and bounds nothing (02 section 4.3), so it is a "
                    + "Policy that triggers and cannot be observed to have run.");
            }

            ResourceId resource = ReadTransferResource(inline, name, entry);
            int amount = ReadTransferAmount(inline, name, entry);

            return (from, to, resource, amount);
        }

        /// <summary>One end of a transfer — refusals 62 and 63.</summary>
        private Scope ReadTransferScope(InlineTableSyntax inline, string key, string? name)
        {
            if (!TryString(inline, key, out string? scope, required: true, name))
            {
                return key == "from" ? Scope.Local : Scope.Global;
            }

            switch (scope)
            {
                case "local":
                    return Scope.Local;

                case "global":
                    return Scope.Global;

                default:
                    Refuse(LineOf((SyntaxNodeBase?)Find(inline, key) ?? inline), name,
                        $"{key} = \"{scope}\" is not an end of a transfer. The two are \"local\" -- "
                        + "the swept actor's own balance -- and \"global\", the treasury. \"pool\" is "
                        + "a market, so its payment is implicit at the prevailing price and is never "
                        + "authored (adr/0050); \"map\" is a Layer emission and holds nothing.");

                    return key == "from" ? Scope.Local : Scope.Global;
            }
        }

        /// <summary>What a transfer moves — refusals 64, 65 and 66.</summary>
        /// <remarks>
        /// <b>Conserved or refused.</b> <c>adr/0024</c> makes money the conserved family, and a
        /// transfer of Goods between a Household and the treasury is a city-wide larder — a mechanism
        /// nothing in the corpus has designed, so accepting it would be inventing one in a switch
        /// statement. It is the same refusal <c>global</c> already carries on a <c>[[rule]]</c>,
        /// arriving from the other family.
        /// </remarks>
        private ResourceId ReadTransferResource(InlineTableSyntax inline, string? name, KeyValueSyntax entry)
        {
            if (!TryString(inline, "resource", out string? resource, required: true, name))
            {
                return default(ResourceId);
            }

            if (!_resources.TryGetValue(resource!, out ushort id))
            {
                Refuse(LineOf((SyntaxNodeBase?)Find(inline, "resource") ?? entry), name,
                    $"no [[resource]] is named '{resource}'.");

                return default(ResourceId);
            }

            if (_families[id - 1] != ResourceFamily.Money)
            {
                Refuse(LineOf((SyntaxNodeBase?)Find(inline, "resource") ?? entry), name,
                    $"'{resource}' is not money, so a Policy cannot transfer it. A transfer names the "
                    + "treasury at one end (02 section 4.3) and the treasury holds one Bin per "
                    + "CONSERVED Resource; a city-wide larder of a Good is a different mechanism and "
                    + "nothing has designed it.");

                return default(ResourceId);
            }

            return new ResourceId(id);
        }

        /// <summary>How much one application moves — refusals 67 and 68.</summary>
        private int ReadTransferAmount(InlineTableSyntax inline, string? name, KeyValueSyntax entry)
        {
            if (!TryInteger(inline, "amount", out long amount, required: true, name))
            {
                return 1;
            }

            if (amount < 1 || amount > int.MaxValue)
            {
                Refuse(LineOf((SyntaxNodeBase?)Find(inline, "amount") ?? entry), name,
                    $"amount = {amount} is not a quantity per application. It is at least 1 -- a "
                    + "transfer of nothing is a Policy that cannot be observed to have run, and the "
                    + "way to move less is a smaller apply count.");

                return 1;
            }

            return (int)amount;
        }

        /// <summary>A scope as a Ruleset author spells it, for a refusal to quote back.</summary>
        private static string Spell(Scope scope) => scope switch
        {
            Scope.Local => "local",
            Scope.Pool => "pool",
            Scope.Global => "global",
            _ => "map",
        };

        // ---- hinterlands ------------------------------------------------------------------------

        /// <summary>
        /// Every <c>[[hinterland]]</c> table — the economy behind one map edge (<c>adr/0131</c>,
        /// milestone 11 task 2).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Three required keys and no optional ones, which is not <see cref="ReadPolicies"/>'
        /// shape and not <c>[households]</c>'s either.</b> At milestone 11 a Hinterland <em>is</em>
        /// an edge and a band (<c>adr/0131</c>: <i>a Hinterland field is authored in the milestone
        /// that reads it</i>), so there is no field left over to carry meaning if the band is
        /// omitted. A <c>[[hinterland]]</c> stating only its edge would declare that an economy
        /// exists and say nothing about it — <c>adr/0048</c>'s <i>loads clean and does nothing</i>
        /// class, in the shape where the whole object is the thing that does nothing.
        /// </para>
        /// <para>
        /// ⚠ <b>A band of zero is accepted, where <c>arrivals_per_day = 0</c> is refused, and the
        /// two look alike.</b> A gate admitting nobody is a door that never opens. A Hinterland whose
        /// emigrants carry nothing is a poor economy — its Households still arrive, still enter the
        /// Unplaced Pool and still have to be housed. ***A zero that is a real answer is not the same
        /// zero as one that disables the mechanism stating it.***
        /// </para>
        /// <para>
        /// <b>Duplicates are refused on the <em>edge</em>, not on the name</b>, which is why nothing
        /// is registered in <see cref="Enumerate"/>. <c>CONTEXT.md</c> → Hinterland is *"the economy
        /// behind one map edge, shared by every Outside Connection on that edge"*, so two tables for
        /// one edge is ambiguous rather than additive — <c>[layers]</c>'s wording, arriving at an
        /// array-of-tables where the collision is in a value.
        /// </para>
        /// </remarks>
        private HinterlandDefinition[] ReadHinterlands(out Money[] prices)
        {
            var definitions = new List<HinterlandDefinition>(_hinterlandTables.Count);
            var authored = new List<Money>(_hinterlandTables.Count * _families.Count);

            foreach (TableSyntaxBase table in _hinterlandTables)
            {
                if (!TryEdge(table, out MapEdge edge))
                {
                    continue;
                }

                bool duplicate = false;

                foreach (HinterlandDefinition declared in definitions)
                {
                    if (declared.Edge == edge)
                    {
                        Refuse(
                            LineOf((SyntaxNodeBase?)Find(table, "edge") ?? table), null,
                            $"a second hinterland is declared for the {Spelling(edge)} edge. A "
                            + "Hinterland is the economy behind one map edge, shared by every "
                            + "Outside Connection on it, so two of them for one edge is ambiguous "
                            + "rather than additive -- there is no rule saying which market an "
                            + "arriving Household came from.");

                        duplicate = true;
                        break;
                    }
                }

                if (duplicate)
                {
                    continue;
                }

                if (!TryEmigrantBalance(table, out Money min, out Money max))
                {
                    continue;
                }

                definitions.Add(new HinterlandDefinition(edge, min, max));

                // AFTER the Add and never before, because the two lists are parallel by position and
                // ReadPrices appends exactly one stride whatever it finds. Every `continue` above
                // skips both, which is what keeps the stride honest.
                ReadPrices(table, authored);
            }

            prices = [.. authored];
            return [.. definitions];
        }

        /// <summary>
        /// One <c>[[hinterland]]</c> table's <c>prices</c> array, appended as one stride of
        /// <see cref="Ruleset.HinterlandPrices"/>.
        /// </summary>
        /// <remarks>
        /// <b>It appends <c>_families.Count</c> entries on every path, including the refusing ones</b>
        /// — the array is indexed rather than searched, so a short stride would silently re-point
        /// every later Hinterland's prices at the wrong Resource. ***A parallel array's invariant is
        /// that the failure path writes too***, which is the opposite of every other reader here.
        /// </remarks>
        private void ReadPrices(TableSyntaxBase table, List<Money> into)
        {
            int first = into.Count;

            for (int resource = 0; resource < _families.Count; resource++)
            {
                into.Add(Money.Zero);
            }

            KeyValueSyntax? entry = Find(table, "prices", RulesetKeyKind.Array);

            if (entry is null)
            {
                return;
            }

            if (entry.Value is not ArraySyntax array)
            {
                Refuse(LineOf(entry), null, "prices must be an array of inline tables.");
                return;
            }

            foreach (ArrayItemSyntax item in array.Items)
            {
                if (item.Value is not InlineTableSyntax inline)
                {
                    Refuse(LineOf(item), null, "every entry of prices must be an inline table.");
                    continue;
                }

                if (!TryResource(inline, null, LineOf(inline), out ResourceId resource))
                {
                    continue;
                }

                // Only a Good. A utility is not stocked and a money Resource is the DENOMINATION of
                // a price rather than a thing with one, so both would be a number the market never
                // reads -- and a key the loader silently drops is a key a designer tunes and then
                // wonders about, which is `bins`' capacity refusal one table over.
                if (_families[resource.Raw - 1] != ResourceFamily.Good)
                {
                    Refuse(LineOf(inline), null,
                        $"'{NameOfResource(resource)}' is not a good, so it has no import price. A "
                        + "Hinterland prices what crosses the map's edge as cargo: a utility is not "
                        + "stocked and money is what a price is denominated IN rather than a thing "
                        + "that has one.");

                    continue;
                }

                if (!TryInteger(inline, "price", out long price, required: true))
                {
                    continue;
                }

                // Refused at zero rather than defaulted, on [districts] prominence_percent's reason.
                // A zero import price is not 'unpriced': it is a ceiling of nothing, and adr/0135's
                // tâtonnement clamps every Pool price to [0, ceiling] -- so the whole market would be
                // free while the file appeared to be pricing it.
                if (price < 1)
                {
                    Refuse(LineOf(inline), null,
                        $"price {price} for '{NameOfResource(resource)}' is not a positive amount. It "
                        + "is the ceiling on what this Good can cost inside the city, so zero makes "
                        + "it free everywhere for ever rather than leaving it unpriced. Delete the "
                        + "entry to leave a Good unpriced.");

                    continue;
                }

                if (into[first + resource.Raw - 1] != Money.Zero)
                {
                    Refuse(LineOf(inline), null,
                        $"this hinterland prices '{NameOfResource(resource)}' twice. One market, one "
                        + "price per Good: two would make the ceiling depend on which entry the "
                        + "reader reached first.");

                    continue;
                }

                into[first + resource.Raw - 1] = new Money(price);
            }
        }

        /// <summary>How a Resource is spelled in this file, for a refusal message.</summary>
        private string NameOfResource(ResourceId resource)
        {
            foreach (KeyValuePair<string, ushort> declared in _resources)
            {
                if (declared.Value == resource.Raw)
                {
                    return declared.Key;
                }
            }

            return resource.Raw.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        // ---- lattices -------------------------------------------------------------------------

        /// <summary>
        /// The <c>[[lattice]]</c> tables — <b>where the generator lays Street lattices</b>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Optional, and its absence is one Lattice at the origin corner</b> — which is the world
        /// every Ruleset in <c>rulesets/</c> described before this key existed, so no State Hash moves
        /// by the key arriving. That is <c>[layers]</c>'s polarity rather than <c>[roads]</c>'s, and
        /// legitimately: there is an earlier behaviour here and it is exactly one lattice at (0, 0).
        /// </para>
        /// <para>
        /// <b>Two numbers per table, because the extent and the population share are derived</b>
        /// (<see cref="LatticeDefinition"/>). What a file authors is <em>where</em>, and the gap
        /// between two origins is the entire content of a two-Lattice world.
        /// </para>
        /// </remarks>
        private LatticeDefinition[] ReadLattices(RoadRuleset roads)
        {
            if (_latticeTables.Count == 0)
            {
                return [];
            }

            if (!roads.Runs)
            {
                Refuse(LineOf(_latticeTables[0]), null,
                    "a [[lattice]] is declared and there is no [roads] table. A Lattice IS a Street "
                    + "lattice -- an origin the generator lays a grid-snapped network from -- so a "
                    + "world with no roads has nothing for one to be, and the origin would name "
                    + "ground nothing is ever built on.");

                return [];
            }

            // Arterials are laid per lattice and severance is addressed by position within one, so
            // two lattices would each get `arterial_count` of them and the file would read as a
            // count of Arterials crossing the map while behaving as a count per settlement. Refused
            // rather than divided or silently doubled: which of the two a designer meant is not
            // recoverable from the file, and adr/0090 has Arterials as a player tool that does not
            // belong in a generator at all -- every city-modelling Ruleset states 0.
            if (_latticeTables.Count > 1 && roads.ArterialCount > 0)
            {
                Refuse(LineOf(_latticeTables[1]), null,
                    $"{_latticeTables.Count} [[lattice]] tables are declared and arterial_count is "
                    + $"{roads.ArterialCount}. An Arterial is laid per lattice, so the file says "
                    + $"\"{roads.ArterialCount} Arterials cross the map\" and would mean "
                    + $"\"{roads.ArterialCount} in each of {_latticeTables.Count} lattices\". Set "
                    + "arterial_count = 0, which is what every city-modelling Ruleset states.");

                return [];
            }

            var definitions = new List<LatticeDefinition>(_latticeTables.Count);

            foreach (TableSyntaxBase table in _latticeTables)
            {
                if (!TryOrigin(table, roads, "origin_east_tiles", out int east)
                    || !TryOrigin(table, roads, "origin_north_tiles", out int north))
                {
                    continue;
                }

                bool duplicate = false;

                foreach (LatticeDefinition declared in definitions)
                {
                    if (declared.OriginEastTiles == east && declared.OriginNorthTiles == north)
                    {
                        Refuse(LineOf(table), null,
                            $"a second lattice is declared at ({east}, {north}). Two lattices on one "
                            + "origin are one lattice laid twice, and the second one throws -- the "
                            + "generator is a world-creation pass and refuses ground that already "
                            + "has Segments on it.");

                        duplicate = true;
                        break;
                    }
                }

                if (!duplicate)
                {
                    definitions.Add(new LatticeDefinition(east, north));
                }
            }

            return [.. definitions];
        }

        /// <summary>
        /// One of a <c>[[lattice]]</c>'s two origin keys, in Tiles and on the block grid.
        /// </summary>
        /// <remarks>
        /// <b>The multiple-of-<c>block_tiles</c> refusal is what keeps the whole world on one grid.</b>
        /// The corridor joining two Lattices is laid in whole blocks from a Node of the first to a
        /// Node of the second, so an origin off the grid would leave the last step of that run short
        /// and put a Node a fraction of a block from another one. Refused rather than snapped: a
        /// snapped origin is a file whose number is not the number the world was built from.
        /// </remarks>
        private bool TryOrigin(TableSyntaxBase table, RoadRuleset roads, string key, out int origin)
        {
            origin = 0;

            if (!TryInteger(table, key, out long value, required: true))
            {
                return false;
            }

            if (value < 0 || value >= CellGrid.WorldTiles)
            {
                Refuse(LineOf((SyntaxNodeBase?)Find(table, key) ?? table), null,
                    $"{key} = {value} is off the map. The map is bounded (adr/0021) and is "
                    + $"{CellGrid.WorldTiles} Tiles a side, so an origin is between 0 and "
                    + $"{CellGrid.WorldTiles - 1}.");

                return false;
            }

            if (value % roads.BlockTiles != 0)
            {
                Refuse(LineOf((SyntaxNodeBase?)Find(table, key) ?? table), null,
                    $"{key} = {value} is not a multiple of block_tiles = {roads.BlockTiles}. Every "
                    + "Node in a generated world sits on one block grid, and the corridor joining "
                    + "two lattices is laid in whole blocks -- an origin off the grid would end that "
                    + "run a fraction of a block short of the lattice it is joining.");

                return false;
            }

            origin = (int)value;
            return true;
        }

        /// <summary>The <c>edge</c> key, which is the Hinterland's identity.</summary>
        /// <remarks>
        /// <b>Four names and no fifth</b>, on <see cref="ReadSubject"/>'s shape.
        /// <see cref="MapEdge.None"/> has no spelling and must not acquire one: it is the answer
        /// <see cref="MapEdges.Touching"/> gives for *not on the boundary* and for *on a corner*, so
        /// a Ruleset able to write it would be authoring an economy behind no edge.
        /// </remarks>
        private bool TryEdge(TableSyntaxBase table, out MapEdge edge)
        {
            edge = MapEdge.None;

            if (!TryString(table, "edge", out string? stated, required: true))
            {
                return false;
            }

            switch (stated)
            {
                case "north":
                    edge = MapEdge.North;
                    return true;

                case "south":
                    edge = MapEdge.South;
                    return true;

                case "east":
                    edge = MapEdge.East;
                    return true;

                case "west":
                    edge = MapEdge.West;
                    return true;

                default:
                    Refuse(
                        LineOf((SyntaxNodeBase?)Find(table, "edge") ?? table), null,
                        $"edge = \"{stated}\" is not a map edge. The map is bounded (adr/0021) and "
                        + "it has four sides -- \"north\", \"south\", \"east\", \"west\" -- one "
                        + "Hinterland behind each. The edge is what a Hinterland IS, so there is no "
                        + "default to fall back to.");

                    return false;
            }
        }

        /// <summary>The <c>emigrant_balance_min</c>/<c>max</c> band.</summary>
        /// <remarks>
        /// <para>
        /// <b>A band rather than one figure, for
        /// <see cref="HouseholdRuleset.OpeningBalance"/>'s reason.</b> A single figure has no
        /// distribution, so every arriving Household is identically rich and any instrument reading
        /// a spread reads 0% or 100% and says nothing about the city.
        /// </para>
        /// <para>
        /// ⚠ <b>Both required, where <c>[households]</c>'s pair is both-or-neither.</b> There the
        /// band is optional because omitting it means <em>the populator endows nobody</em>, which is
        /// what every Ruleset written before milestone 10 meant by saying nothing. Here the object
        /// has no other field, so omission has nothing to mean.
        /// </para>
        /// <para>
        /// ⚠ <b>This is NOT <c>[households] opening_balance_min</c>/<c>max</c> reused, and the
        /// separation is the decision</b> (<c>adr/0131</c>). That key is <b>world-founding</b> —
        /// what the city's own first Households were endowed with. Drawing arrivals from it would
        /// make the four edges interchangeable as money sources while each separately authors an
        /// economy its own emigrants did not come from. ***An anchor that does not reach the thing
        /// it anchors is decoration.***
        /// </para>
        /// </remarks>
        private bool TryEmigrantBalance(
            TableSyntaxBase table, out Money min, out Money max)
        {
            min = Money.Zero;
            max = Money.Zero;

            if (!TryInteger(table, "emigrant_balance_min", out long low, required: true)
                || !TryInteger(table, "emigrant_balance_max", out long high, required: true))
            {
                return false;
            }

            if (low < 0)
            {
                Refuse(
                    LineOf((SyntaxNodeBase?)Find(table, "emigrant_balance_min") ?? table), null,
                    $"emigrant_balance_min is {low}. A balance is a stock and a stock is never "
                    + "negative -- a debt is not negative money (adr/0003), and a Household "
                    + "arriving in arrears is a mechanism nobody has designed.");

                return false;
            }

            if (high < low)
            {
                Refuse(
                    LineOf((SyntaxNodeBase?)Find(table, "emigrant_balance_max") ?? table), null,
                    $"emigrant_balance_max is {high}, below emigrant_balance_min of {low}. A band "
                    + "is drawn inclusive of both ends, so an inverted one is empty rather than "
                    + "narrow.");

                return false;
            }

            if (high > 0 && !_families.Contains(ResourceFamily.Money))
            {
                Refuse(
                    LineOf((SyntaxNodeBase?)Find(table, "emigrant_balance_max") ?? table), null,
                    "this hinterland's emigrants carry money and this file names none. A balance is "
                    + "a Bin and a Bin exists only for a declared Resource (adr/0114), so an "
                    + "arriving Household would have nowhere to put it. Add a [[resource]] block "
                    + "with family = \"money\".");

                return false;
            }

            min = new Money(low);
            max = new Money(high);

            return true;
        }

        /// <summary>How a <see cref="MapEdge"/> is spelled in a Ruleset, for refusal messages.</summary>
        /// <remarks>
        /// <b>It lives here and never in <c>Borough.Core</c></b> — <c>adr/0048</c>: only integers and
        /// strings cross into the core, and <c>05 §1</c> puts every string a human reads on this side
        /// of the boundary.
        /// </remarks>
        private static string Spelling(MapEdge edge) => edge switch
        {
            MapEdge.North => "north",
            MapEdge.South => "south",
            MapEdge.East => "east",
            MapEdge.West => "west",
            _ => "unplaced",
        };

        // ---- layers ---------------------------------------------------------------------------

        /// <summary>
        /// Reads <c>[layers]</c>: the cadence, the rates, and the one world-creation constant.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Every key is optional and the defaults are <see cref="LayerRuleset.Default"/>'s</b>, so a
        /// Ruleset written before this section existed is still a complete Ruleset. The alternative —
        /// requiring the table — would refuse every file in the repository to gain nothing, since the
        /// numbers it holds have documented values that <c>02 §2.4</c> states.
        /// </para>
        /// <para>
        /// <b>The decay is authored as a duration and the tau is derived</b> (<c>plans/0015</c>
        /// decision owed 2). <see cref="LayerRates.PollutionTau"/> counts <em>scheduled updates</em>,
        /// so a file stating it as a literal would silently change meaning when the cadence it is
        /// counted in was reloaded. The file states Ticks; <see cref="LayerRates.From"/> divides.
        /// </para>
        /// </remarks>
        private LayerRuleset ReadLayers()
        {
            LayerSchedule schedule = LayerSchedule.Default;
            LayerRates rates = LayerRates.Default;
            LayerConstants constants = LayerConstants.Default;
            DesirabilityWeights desirability = LayerRuleset.Default.Desirability;

            int pollutionPeriod = Cadence("pollution", schedule.IndustrialPollution,
                out int pollutionOffset);
            int landValuePeriod = Cadence("land_value", schedule.LandValue, out int landValueOffset);
            int sealingPeriod = Cadence("sealing_decay", schedule.Sealing, out int sealingOffset);
            int woodlandPeriod = Cadence("woodland_regrowth", schedule.Woodland, out int woodlandOffset);

            // Absent means NEVER, and a stated zero is refused because it means the opposite. This is
            // a DURATION rather than a time constant, so 0 Days reads as "instantly" -- the one value
            // a designer must not be able to reach by writing a number that looks like an absence.
            // adr/0160's rule about a second spelling of an existing state, arriving where the two
            // spellings would mean different things. The ceiling is the Cell: past TilesInCell the
            // derived step rounds to nothing and the floor takes over, so the authored duration would
            // silently not be the duration. Milestone 24 task 8b, adr/0022.
            int regrowthDays = Number("woodland_regrowth_days", rates.WoodlandRegrowthDays,
                minimum: 1,
                "It is how many Days a Cell cleared of all its forest takes to grow back to what the "
                + $"seed laid, so it runs 1 to {CellGrid.TilesInCell}. Omit the key for a world where "
                + "forest never returns, which is what every Ruleset said by saying nothing. Zero is "
                + "refused because it would mean INSTANTLY rather than never.");

            if (regrowthDays > CellGrid.TilesInCell)
            {
                Refuse(LineOf("woodland_regrowth_days"), null,
                    $"woodland_regrowth_days is {regrowthDays}, and the ceiling is "
                    + $"{CellGrid.TilesInCell} -- a Cell's Tile count. One pass puts back "
                    + "TilesInCell / days Tiles, so past the Cell that is less than one Tile and the "
                    + "floor of one takes over: the ground would recover in TilesInCell Days whatever "
                    + "this said. A duration the mechanism cannot express is worse than one it "
                    + "refuses (adr/0022).");

                regrowthDays = rates.WoodlandRegrowthDays;
            }

            int metres = Number("kernel_metres", constants.IndustrialPollutionMetres, minimum: 1,
                "A kernel with no reach is not a diffused field.");
            int decayTicks = Number("pollution_decay_ticks", LayerRates.DefaultPollutionDecayTicks,
                minimum: 0, "It is a duration in Ticks, and 0 means the plume never fades.");
            int landValueTau = Number("land_value_tau", rates.LandValueTau, minimum: 1,
                "A time constant divides, so 0 is not one; land value with no momentum is a period "
                + "of 1 rather than a tau of 0.");
            // The key MOVED and is refused where it used to be, rather than ignored. A file carrying
            // it means something by it, and silently reading nothing would leave a designer's stated
            // rate doing nothing with no way to tell. Milestone 24 task 4, adr/0044.
            RefuseRetired(_layersTable, "sealing_decay_tau", null,
                "sealing_decay_tau has moved out of [layers]. It is keyed BY TERRAIN TYPE "
                + "(02 section 2.4: rock may never recover, floodplain may recover over hundreds "
                + "of Days), so it is now a key on each [[terrain]] table and there is no global "
                + "one. A file with no [[terrain]] has ground that never recovers, which is what "
                + "every shipped Ruleset said by writing 0 here.");

            // The desirability composition's numbers. All authored as PERCENTS or METRES rather
            // than as Q16.16, because 02 §2.5 question 2 says author in domain units and because the
            // corpus already spells a fraction that way -- [traffic] alpha and car_ownership_percent.
            // Every one of them is unratified and each owes two plans/0002 §D1 entries (adr/0125).
            int noiseRange = Number("noise_range_metres", desirability.NoiseSource.Range.Raw * Tiles.Metres,
                minimum: 1,
                "A line source with no reach is not a source. 02 §2.4 says 50–300 m, and that band is "
                + "six times wide — the shipped 300 is its outer end, not a derivation.");
            int noiseIntensity = Number("noise_intensity_percent", 400, minimum: 1,
                "It is the intensity one Vehicle per Tick radiates at one Tile, as a percent. ⚠ It is "
                + "NOT a scale: the level is log(1+x), which is linear below unity and logarithmic "
                + "above it, so this decides which regime the city sits in.");
            int pollutionWeight = Number("desirability_pollution_percent", 100, minimum: 0,
                "w₂, as a percent. It subtracts; 0 removes the term rather than defaulting it.");
            int noiseWeight = Number("desirability_noise_percent", 100, minimum: 0,
                "w₃, as a percent. It subtracts; 0 removes the term rather than defaulting it.");

            // The shoreline source's three numbers, milestone 24 task 7. ⚠ THE RANGE IS NOT NOISE'S:
            // 02 §2.4 states no shoreline band at all, and 400 m is amenity's walkable one, taken
            // because CONTEXT.md -> Water Body ties this term to the same event -- a fouled beach both
            // degrades land value and removes a walkable destination.
            int shorelineRange = Number(
                "shoreline_range_metres", desirability.ShorelineSource.Range.Raw * Tiles.Metres,
                minimum: 1,
                "How far from the water's edge a fouled body reaches. A source with no reach is not a "
                + "source; a world with no water is spelled by omitting [water], not by writing 0.");
            int shorelineIntensity = Number("shoreline_intensity_percent", 600, minimum: 1,
                "It is what a COMPLETELY fouled body radiates at one Tile from its edge, as a percent. "
                + "⚠ Its multiplicand is a FILL FRACTION in [0, 1] and noise's is an unbounded flow, so "
                + "the two intensities are not comparable and this being the larger means nothing.");
            int shorelineWeight = Number("desirability_shoreline_percent", 100, minimum: 0,
                "w₅, as a percent. It subtracts; 0 removes the term rather than defaulting it.");

            // Fertility's one weight, and it is in [layers] rather than [[terrain]] because
            // [[terrain]] is an array of tables keyed BY TYPE and this is not per-type -- it weighs
            // the pollution term for every Cell alike. Its sibling w₂ is two lines up, which is the
            // other half of the argument: two Layer compositions, two weights, one table.
            //
            // ⚠ There is deliberately NO fertility_sealing_percent. adr/0156: a Cell at
            // CellGrid.TilesInCell has every Tile built on and therefore no farmland, which PINS that
            // coefficient -- and offering it as a key invites a Ruleset to state that a fully paved
            // Cell still farms.
            int fertilityWeight = Number("fertility_pollution_percent", 4, minimum: 0,
                "w_p, as a percent of fully fertile per unit of pollution. It subtracts; 0 removes "
                + "the term rather than defaulting it. 4 is anchored on adr/0022's Evidence specimen "
                + "against a measured plume of about 12 kernel units, and is UNRATIFIED.");

            desirability = new DesirabilityWeights(
                IntegerMath.RoundDiv(Fixed.FromInt(pollutionWeight), 100),
                IntegerMath.RoundDiv(Fixed.FromInt(noiseWeight), 100),
                new LineSource(Tiles.FromMetres(noiseRange), IntegerMath.RoundDiv(Fixed.FromInt(noiseIntensity), 100)),
                IntegerMath.RoundDiv(Fixed.FromInt(shorelineWeight), 100),
                new ShorelineSource(
                    Tiles.FromMetres(shorelineRange),
                    IntegerMath.RoundDiv(Fixed.FromInt(shorelineIntensity), 100)));

            var fertility = new FertilityWeights(
                IntegerMath.RoundDiv(Fixed.FromInt(fertilityWeight), 100));

            // The one refusal that is a property of the two numbers together rather than of either.
            // A decay shorter than the period it runs at rounds to zero updates, and zero means
            // *never* -- so the file would read as "fades fast" and behave as "never fades".
            if (decayTicks > 0 && IntegerMath.RoundDiv(decayTicks, pollutionPeriod) == 0)
            {
                Refuse(LineOf("pollution_decay_ticks"), null,
                    $"pollution_decay_ticks = {decayTicks} rounds to zero scheduled updates at a "
                    + $"pollution_period of {pollutionPeriod}, and zero means never — the opposite of "
                    + "what a short decay asks for. Shorten the period or lengthen the decay.");
            }

            LayerConstants stated = new(metres);
            RefuseFrozenChange(stated);

            return new LayerRuleset(
                new LayerSchedule(
                    new LayerCadence(pollutionPeriod, pollutionOffset),
                    new LayerCadence(landValuePeriod, landValueOffset),
                    new LayerCadence(sealingPeriod, sealingOffset),
                    new LayerCadence(woodlandPeriod, woodlandOffset)),
                LayerRates.From(landValueTau, decayTicks, pollutionPeriod, regrowthDays),
                stated,
                desirability,
                fertility);
        }

        /// <summary>One Layer's period and offset, with the offset checked against the period.</summary>
        /// <remarks>
        /// <b>An offset at or beyond its period never fires</b> — <c>tick % period == offset</c> has no
        /// solution — so the Layer freezes and the Ruleset loads clean. That is the same symptom slice
        /// 10's three <c>[[zone_rule]]</c> refusals share, and it is refused here for the same reason.
        /// </remarks>
        private int Cadence(string layer, LayerCadence fallback, out int offset)
        {
            int period = Number($"{layer}_period", fallback.Period, minimum: 1,
                "A period is Ticks between recomputations; a Layer that is never recomputed is not "
                + "expressible as a period, and 0 would freeze the field with nothing to say so.");

            offset = Number($"{layer}_offset", fallback.Offset, minimum: 0,
                "An offset is which Tick of the cycle the Layer fires on.");

            if (offset >= period)
            {
                Refuse(LineOf($"{layer}_offset"), null,
                    $"{layer}_offset = {offset} is not inside a {layer}_period of {period}, so this "
                    + "Layer would never be recomputed. The offset is a position in the cycle, and "
                    + "the cycle is 0 to period - 1.");

                offset = fallback.Offset < period ? fallback.Offset : 0;
            }

            return period;
        }

        /// <summary>An optional <c>[layers]</c> integer, refused when it is outside its range.</summary>
        private int Number(string key, int fallback, int minimum, string range)
        {
            if (_layersTable is null
                || !TryInteger(_layersTable, key, out long value, required: false))
            {
                return fallback;
            }

            if (value < minimum || value > int.MaxValue)
            {
                Refuse(LineOf(key), null, $"{key} = {value} is out of range. {range}");
                return fallback;
            }

            return (int)value;
        }

        // ---- placement ------------------------------------------------------------------------

        /// <summary>
        /// Reads <c>[placement]</c>: how often the Unplaced Pool is drained into standing dwellings,
        /// how long it takes to look at everybody waiting, and how many dwellings one family sees
        /// (<c>adr/0069</c>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The whole table is optional and its absence means the pass does not run</b>, which is the
        /// opposite of <c>[layers]</c>'s default-bearing absence and is deliberate. A default here would
        /// put three hash-bearing numbers in the binary with nobody having authored them
        /// (<c>adr/0052</c>), and the failure it would hide is quiet: a Ruleset that houses people at a
        /// cadence its author never wrote. A Ruleset that houses nobody is loud — the Pool grows without
        /// bound and the Census says so — which is <c>HONEST DEGRADATION</c> choosing the visible
        /// failure.
        /// </para>
        /// <para>
        /// <b>Once the table is present every key inside it is required</b>, for the same reason: the
        /// author has said the pass runs, and the pass has no number it can derive. <c>revisit_ticks</c>
        /// is the exception and is <c>adr/0059</c>'s exception — a Day is derived from the scale the
        /// rest of the Ruleset is denominated in rather than picked.
        /// </para>
        /// </remarks>
        private PlacementRuleset ReadPlacement(KindDefinition[] kinds)
        {
            bool gated = false;

            foreach (KindDefinition kind in kinds)
            {
                if (kind.ArrivalsPerDay > 0)
                {
                    gated = true;
                    break;
                }
            }

            if (_placementTable is null)
            {
                // 🔴 A gate with NO [placement] at all is refused too, and this branch is where task 7
                // stopped short. That task refused a file stating [placement] without
                // gives_up_after_days and said nothing about a file stating no [placement] -- which
                // has an inflow into the Pool, no housing AND no sink, so it is the same adr/0006
                // hole reached through the wider door. Found 2026-08-21 while building a variant for
                // the tick-cost diagnosis; plans/0035 F28.
                if (gated)
                {
                    // Line 1, on LineOf(string)'s precedent: the defect is the ABSENCE of a
                    // table, and an absence has no line of its own.
                    Refuse(1, null,
                        "this Ruleset declares a kind with arrivals_per_day, so Households can enter "
                        + "the Unplaced Pool from outside, and it states no [placement] table at all "
                        + "-- so nobody is ever housed AND nobody ever gives up, and the Pool grows "
                        + "without bound. adr/0006 forbids that. State [placement] with a "
                        + "gives_up_after_days, or remove the gate kind.");
                }

                return PlacementRuleset.None;
            }

            uint interval = ReadInterval(_placementTable, null);
            int revisit = ReadPlacementRevisit(interval);
            int candidates = ReadCandidates();
            int givesUp = ReadGivesUpAfterDays(gated);

            RefuseEmptyClockUnderTheRevisit(kinds, revisit);

            return new PlacementRuleset(interval, revisit, candidates, givesUp);
        }

        /// <summary>
        /// Refuses a kind whose empty clock is shorter than the pass that would fill it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b><c>abandoned_when_empty_after_days</c> against <c>[placement] revisit_ticks</c>, and
        /// the relation is the whole check.</b> <c>adr/0069</c> has construction house nobody, so
        /// every Building is empty from the Tick it is raised and the placement pass is the only
        /// thing that fills one. <c>revisit_ticks</c> is authored as *how long that pass takes to
        /// look at everybody waiting once* (<c>adr/0059</c>) — so a kind whose clock expires inside
        /// one period is abandoning stock the pass has not yet had a chance to look at, and its
        /// lifetime is drawn from the CADENCE rather than from the Ruleset.
        /// </para>
        /// <para>
        /// ⚠ <b>One period is a floor and not a comfortable margin.</b> The sample is drawn with
        /// replacement, so about <c>1/e</c> of the Pool goes unlooked-at in any one period and a
        /// clock set just over the line still abandons Buildings somebody would have taken. What the
        /// loader can refuse is the case that is wrong *by construction*; how much headroom past it
        /// is taste, and it is the author's.
        /// </para>
        /// <para>
        /// ⚠ <b>Here rather than in <c>ReadKinds</c>, because it is a property of the PAIR</b> — the
        /// kind cannot see the cadence and the cadence cannot see the kind. It is
        /// <c>RefuseUnpricedGoods</c>'s shape one table along.
        /// </para>
        /// </remarks>
        private void RefuseEmptyClockUnderTheRevisit(KindDefinition[] kinds, int revisit)
        {
            for (int kind = 0; kind < kinds.Length; kind++)
            {
                int empties = kinds[kind].AbandonedWhenEmptyAfterTicks;

                if (empties == 0 || empties > revisit)
                {
                    continue;
                }

                Refuse(LineOf(_placementTable!), null,
                    $"a [[building]] states abandoned_when_empty_after_days worth {empties} Ticks "
                    + $"against a [placement] revisit_ticks of {revisit}. Construction houses nobody "
                    + "(adr/0069), so a Building is empty from the Tick it is raised and the "
                    + "placement pass is the only thing that fills one -- and revisit_ticks is how "
                    + "long that pass takes to look at everybody waiting once. A clock that expires "
                    + "inside one period gives the Building a lifetime drawn from the cadence rather "
                    + "than from this file. Lengthen the clock, or shorten the revisit period.");
            }
        }

        /// <summary>
        /// How long a Household keeps looking before it gives up and leaves, in Days.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Optional in general and required of a Ruleset that declares a gate</b>, which is
        /// <see href="../../docs/adr/0130-the-pools-bound-is-a-duration-and-the-unhoused-channel-ships-with-the-gate.md">adr/0130</see>'s
        /// argument made mechanical. A Pool with an inflow and no sink is a collection that grows
        /// with elapsed time (<c>adr/0006</c>), and a gate is exactly the inflow — so *whoever builds
        /// the gate owes the give-up rule* stops being a sentence somebody has to remember.
        /// </para>
        /// <para>
        /// ⚠ <b>A file with no gate may omit it, and that is not laxity.</b> Without a gate nothing
        /// creates a Household after world creation, so the Pool is a subset of a population fixed at
        /// that moment and cannot grow with elapsed time whatever it does — <c>adr/0054</c>'s
        /// reasoning, still standing for every Ruleset that has no door in it. Requiring the key
        /// there would put a hash-bearing number in nine files to no effect, and ***an inert number
        /// in a Ruleset is one a designer tunes expecting an effect.***
        /// </para>
        /// <para>
        /// ⚠ <b>What the loader can see is a gate <em>kind</em>, not a gate.</b> Whether a world ever
        /// places one is a property of the world, and milestone 11 task 5 established that the loader
        /// cannot see a world — which is why the gate↔Hinterland pairing happens at arrival instead.
        /// This check is on the right side of that line: a declared kind is a fact about the file.
        /// </para>
        /// </remarks>
        private int ReadGivesUpAfterDays(bool gated)
        {
            if (!TryInteger(_placementTable!, "gives_up_after_days", out long days, required: false))
            {
                if (gated)
                {
                    Refuse(LineOf(_placementTable!), null,
                        "this Ruleset declares a kind with arrivals_per_day, so Households can enter "
                        + "the Unplaced Pool from outside, and [placement] does not state "
                        + "gives_up_after_days -- so nothing ever leaves the Pool except by being "
                        + "housed. A Pool with a door into it and no give-up rule grows without "
                        + "bound, which adr/0006 forbids. State how long a Household keeps looking, "
                        + "in Days, or remove the gate kind.");
                }

                return 0;
            }

            if (days < 1 || days > int.MaxValue / Ticks.PerDay)
            {
                Refuse(LineOf((SyntaxNodeBase?)Find(_placementTable!, "gives_up_after_days")
                        ?? _placementTable!), null,
                    $"gives_up_after_days is {days}. It is how long a Household keeps looking for a "
                    + "home before it gives up and leaves, in Days, so it is at least 1 and at most "
                    + $"{int.MaxValue / Ticks.PerDay} -- the engine holds it in Ticks. To mean that "
                    + "nobody ever gives up, omit the key; a Ruleset with a gate in it may not.");
                return 0;
            }

            return (int)days;
        }

        /// <summary>
        /// How long the placement pass takes to look at everybody waiting, in Ticks.
        /// </summary>
        /// <remarks>
        /// <b><c>adr/0059</c> a second time, and the sampled quantity is the Pool rather than the Lot
        /// grid.</b> An absolute count of seekers per trigger would house a fixed number of families a
        /// Day however many were waiting, so the fraction of a housing queue cleared per cycle would
        /// shrink as the queue grew — which is the failure mode <c>adr/0059</c> measured on Lots, in a
        /// collection whose growth is the thing being fixed.
        /// </remarks>
        private int ReadPlacementRevisit(uint interval)
        {
            if (!TryInteger(_placementTable!, "revisit_ticks", out long revisit, required: true))
            {
                return Ticks.PerDay;
            }

            if (revisit < 1 || revisit > int.MaxValue)
            {
                Refuse(LineOf((SyntaxNodeBase?)Find(_placementTable!, "revisit_ticks")
                        ?? _placementTable!), null,
                    $"a revisit period of {revisit} is not a duration this world can hold. It is how "
                    + "long the placement pass takes to look at everybody in the Unplaced Pool once, "
                    + $"in Ticks, so it is at least 1 and at most {int.MaxValue} -- the engine divides "
                    + "by it. If the intent is to house nobody, delete the [placement] table.");
                return Ticks.PerDay;
            }

            // Refusal 9's shape against a third denominator: a revisit period shorter than the
            // interval it is delivered in asks one trigger to consider more seekers than are waiting.
            if (revisit < interval)
            {
                Refuse(LineOf((SyntaxNodeBase?)Find(_placementTable!, "revisit_ticks")
                        ?? _placementTable!), null,
                    $"revisit_ticks = {revisit} is shorter than the interval of {interval} it would be "
                    + "delivered in, so one trigger would be asked to consider more seekers than the "
                    + "Pool holds. A revisit period is a duration spread over triggers; shorten the "
                    + "interval or lengthen the period.");
                return (int)interval;
            }

            return (int)revisit;
        }

        /// <summary>How many dwellings one Household looks at before waiting for its next occasion.</summary>
        /// <remarks>
        /// <b><c>02 §5.3</c>'s N, and it is a behaviour model rather than a budget</b> — a family that
        /// sees three flats and takes the first with room is not an optimiser being approximated. It is
        /// <b>hash-bearing and unratified</b>: <c>0002</c> §D carries the row, and the ratifier is the
        /// first time the housing queue is looked at with a choice model in front of it, because until
        /// then every candidate scores the same and the number is only a rate.
        /// </remarks>
        private int ReadCandidates()
        {
            if (!TryInteger(_placementTable!, "candidates", out long candidates, required: true))
            {
                return 1;
            }

            if (candidates < 1 || candidates > int.MaxValue)
            {
                Refuse(LineOf((SyntaxNodeBase?)Find(_placementTable!, "candidates")
                        ?? _placementTable!), null,
                    $"candidates = {candidates} is out of range. It is how many dwellings a Household "
                    + "looks at on one occasion, so it is at least 1 -- a seeker that looks at none "
                    + "never moves, and the pass would run at full cost housing nobody.");
                return 1;
            }

            return (int)candidates;
        }

        // ---- roads ----------------------------------------------------------------------------

        /// <summary>
        /// The <c>[roads]</c> table — the shape and the speed of the road network.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The whole table is optional and its absence means there are no roads</b>, which is
        /// <c>[placement]</c>'s polarity rather than <c>[layers]</c>'s and is chosen for the same
        /// reason. A default would put eight hash-bearing numbers in the binary that nobody authored
        /// (<c>adr/0052</c>), and the failure it hides is quiet: a city laced with roads its designer
        /// never asked for, at a density that decides Segment count and therefore every routing figure
        /// downstream. A world with no roads is loud — <c>--roads</c> refuses, no Lot has frontage, and
        /// every catchment query still throws by name.
        /// </para>
        /// <para>
        /// <b>Once the table is present every key inside it is required</b>, for the same reason: the
        /// author has said the world has roads, and there is no number here the engine can derive.
        /// That is the whole difference from <c>[layers]</c>, where every key defaults because a
        /// Layer's behaviour predates the table that states it.
        /// </para>
        /// <para>
        /// <b>Speeds are authored in km/h and capacities in Vehicles per hour, converted exactly
        /// here.</b> <c>02 §2</c> is categorical that there are no seconds in the library and no
        /// metres, so the conversion happens where a human authors a number and never at runtime —
        /// which is the arrangement <c>adr/0071</c> carries over from the spike unchanged.
        /// </para>
        /// </remarks>
        private RoadRuleset ReadRoads()
        {
            if (_roadsTable is null)
            {
                return RoadRuleset.None;
            }

            int block = RoadNumber("block_tiles", minimum: 1, maximum: CellGrid.WorldTiles,
                "It is the Street grid spacing in Tiles — the block — so it is at least 1 Tile and "
                + "at most the width of the map. A Cell is " + CellGrid.TilesPerCell
                + " Tiles, so that is one Street on every Cell boundary.");

            int arterials = RoadNumber("arterial_count", minimum: 0, maximum: 1_024,
                "It is how many freeform Arterials cross the map, so 0 is legal and means a city of "
                + "Streets alone — which is a city where Severance cannot happen.");

            int junctions = RoadNumber("arterial_junction_tiles", minimum: 1,
                maximum: CellGrid.WorldTiles,
                "It is the Tiles of Arterial between two authored Junction pieces, which is that "
                + "Arterial's Segment length, so it is at least 1 Tile.");

            int crossings = RoadNumber("foot_crossing_every", minimum: 0, maximum: int.MaxValue,
                "It keeps a pedestrian crossing at every nth Street an Arterial severs, so 0 means "
                + "no crossings at all and a network cut in two for anybody on foot.");

            int paths = RoadNumber("foot_paths_per_thousand_blocks", minimum: 0, maximum: 1_000,
                "It is a count per thousand blocks, so it is between 0 and 1,000 inclusive; 1,000 "
                + "would give every block a cut-through.");

            Speed street = RoadSpeed("street_speed_kph");
            Speed arterial = RoadSpeed("arterial_speed_kph");
            Speed walk = RoadSpeed("walk_speed_kph");

            int streetCapacity = RoadCapacity("street_capacity_per_hour");
            int arterialCapacity = RoadCapacity("arterial_capacity_per_hour");
            int pathCapacity = RoadCapacity("foot_path_capacity_per_hour");

            // The one refusal that is a property of two numbers together rather than of either. An
            // Arterial whose Junction spacing exceeds the map cannot place a second Junction, and an
            // Arterial with one Junction on it has no Segment at all -- so the file would read as
            // "rare Arterials" and behave as "no Arterials", with a Segment count that looks healthy
            // because the Streets are all still there. That is the failure S2's first capture had and
            // did not notice: a graph with no Arterials in it is still a graph.
            if (arterials > 0 && junctions >= CellGrid.WorldTiles)
            {
                Refuse(LineOfRoad("arterial_junction_tiles"), null,
                    $"arterial_junction_tiles = {junctions} is at least the map's width of "
                    + $"{CellGrid.WorldTiles} Tiles, so no Arterial can reach a second Junction "
                    + "piece and none of them will have a Segment. The Arterials would be laid, "
                    + "counted, and absent from the graph.");
            }

            return new RoadRuleset(
                block, arterials, junctions, crossings, paths,
                street, arterial, walk,
                streetCapacity, arterialCapacity, pathCapacity);
        }

        // ---- lots -----------------------------------------------------------------------------

        /// <summary>
        /// The <c>[lots]</c> table — how zoned land is carved into parcels (<c>adr/0078</c>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Optional, and its absence means land cannot be subdivided at all</b> — <c>[roads]</c>'s
        /// polarity, for <c>[roads]</c>'s reason. A default would put a hash-bearing world-creation
        /// number in the binary that nobody authored (<c>adr/0052</c>), and this one decides how many
        /// Buildings a city can hold. The failure a default hides would be quiet; the failure this
        /// causes is loud, because a world with no <c>[lots]</c> grows nothing and says so at the
        /// first <c>zone</c>.
        /// </para>
        /// <para>
        /// <b>One key, because there is only one number</b>. <c>02 §2.2</c> asks for depth and width
        /// targets; the width is derived from <c>CONTEXT.md</c> → Address's <i>five Buildings share a
        /// Segment</i>, and <b>depth does not exist</b> — a Lot has no extent, so a depth would be a
        /// number chosen for a consumer nobody has designed. <c>plans/0022</c> predicted two or more
        /// numbers here and there is one.
        /// </para>
        /// </remarks>
        private LotRuleset ReadLots(RoadRuleset roads)
        {
            if (_lotsTable is null)
            {
                return LotRuleset.None;
            }

            if (!TryInteger(_lotsTable, "lots_per_segment", out long value, required: true))
            {
                return LotRuleset.None;
            }

            // The ceiling is the block, not an arbitrary cap: a Segment cannot carry more Lots than
            // it has Tiles of frontage, because two Lots would land on one Tile and the derivation
            // that recovers a Lot's Segment from its position would stop being one-to-one.
            long ceiling = roads.Runs ? roads.BlockTiles : int.MaxValue;

            if (value < 1 || value > ceiling)
            {
                Refuse(LineOfLot("lots_per_segment"), null,
                    $"lots_per_segment = {value} is out of range. It is how many Lots one Street "
                    + "Segment carries, both sides together, so it is at least 1 and at most the "
                    + $"block's length in Tiles ({ceiling}) — beyond that two Lots would share a "
                    + "Tile and a Lot's frontage would stop being recoverable from its position.");

                return LotRuleset.None;
            }

            // setback_tiles: how much ground a Building leaves on each side of its parcel. REQUIRED,
            // and not defaulted, because a default would have been the number that used to live in
            // Borough.Godot as four drawing constants -- silently, where no Ruleset could retune it
            // and no State Hash could see it. A file that carves land says how much of it gets built
            // on.
            if (!TryInteger(_lotsTable, "setback_tiles", out long setback, required: true))
            {
                return LotRuleset.None;
            }

            // Half the block, because two setbacks meet: a parcel is at most a block deep, so a
            // setback past half of one leaves no footprint on any parcel in the world.
            long widest = roads.Runs ? IntegerMath.FloorDiv(roads.BlockTiles, 2) : int.MaxValue;

            if (setback < 0 || setback > widest)
            {
                Refuse(LineOfLot("setback_tiles"), null,
                    $"setback_tiles = {setback} is out of range. It is the most ground a Building "
                    + "leaves on each side of its parcel, so it is at least 0 — a wall on the "
                    + "pavement, which a terrace is — and at most half the block "
                    + $"({widest}), beyond which two setbacks meet and no parcel in the world has a "
                    + "footprint left on it.");

                return LotRuleset.None;
            }

            return new LotRuleset((int)value, (int)setback);
        }

        /// <summary>Reads <c>[capacity]</c> — how much floor one tenancy, job or car takes.</summary>
        /// <remarks>
        /// <b>Three rates and no counts.</b> Absence of the table is a city in which nothing holds
        /// anybody; absence of a key within it is a city with none of that thing. ⚠ <b>A stated zero
        /// is refused</b> and absence is how you mean it — a rate of zero would divide by nothing,
        /// and a key written to mean <em>none</em> reads as an author who thought they were setting a
        /// quantity.
        /// </remarks>
        private CapacityRuleset ReadCapacity()
        {
            if (_capacityTable is null)
            {
                return CapacityRuleset.None;
            }

            int occupant = Rate("floor_tiles_per_occupant", "one tenancy");
            int job = Rate("floor_tiles_per_job", "one job");
            int space = Rate("floor_tiles_per_parking_space", "one parking space");

            return new CapacityRuleset(occupant, job, space);

            int Rate(string key, string what)
            {
                if (!TryInteger(_capacityTable, key, out long value, required: false))
                {
                    return 0;
                }

                // The ceiling is a Cell: 1,024 Tiles is 16,384 m2, and a rate past that is a
                // quantity nothing in any city could satisfy -- every Building would hold one of
                // whatever it is, which is the floor Holds already gives.
                if (value < 1 || value > CellGrid.TilesInCell)
                {
                    Refuse(LineOfCapacity(key), null,
                        $"{key} = {value} is out of range. It is how much floor {what} takes, in "
                        + $"Tiles, so it is at least 1 and at most a Cell ({CellGrid.TilesInCell}). "
                        + "Omit the key for a city that has none of that thing — a stated zero "
                        + "reads as somebody setting a quantity when they meant an absence.");

                    return 0;
                }

                return (int)value;
            }
        }

        /// <summary>The line a <c>[capacity]</c> key is on, or the table's.</summary>
        private int LineOfCapacity(string key) =>
            LineOf((SyntaxNodeBase?)Find(_capacityTable!, key) ?? _capacityTable!);

        /// <summary>The line a <c>[lots]</c> key is on, or the table's.</summary>
        private int LineOfLot(string key) =>
            LineOf((SyntaxNodeBase?)Find(_lotsTable!, key) ?? _lotsTable!);

        /// <summary>
        /// The <c>[trips]</c> table: what a crossing costs and where the Commute Budget falls.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The whole table is optional and its absence means the city does not travel</b>, which
        /// is <c>[roads]</c>'s polarity. The <c>trip</c> command and <c>--trips</c> refuse against
        /// such a Ruleset rather than costing a journey against numbers nobody authored — the
        /// <c>Scope.Pool</c> and <c>--zones</c> precedent, and the loud failure rather than the quiet
        /// one.
        /// </para>
        /// <para>
        /// <b>Once the table is present <c>crossing_seconds</c> is required and the Budget is
        /// not</b>, which departs from <c>[placement]</c>'s every-key-required rule and does so on a
        /// stated ground: the two are unset in different senses. A crossing cost is <em>chosen with a
        /// named ratifier</em> under <c>adr/0052</c>; a Commute Budget is a <b>percentile of a cost
        /// distribution</b>, and no distribution exists until commutes do. <b>An omitted budget is
        /// therefore a city with no ceiling</b> — a coherent city, and the only one whose cost
        /// distribution is uncensored.
        /// </para>
        /// <para>
        /// <b>The Budget is three keys and they are all-or-nothing</b> (<c>adr/0095</c>). State every
        /// rung or state none: a ceiling with no grading is the binary Budget that ADR replaces, and
        /// a <em>default</em> for either lower rung would be a hash-bearing number chosen because the
        /// schema wanted one. <b>So the optionality is a property of the group rather than of each
        /// key</b>, which is why the guard below counts how many arrived rather than testing them one
        /// at a time — a Ruleset stating two of three is a mistake, and a per-key <c>required</c>
        /// flag cannot express that.
        /// </para>
        /// </remarks>
        private TripRuleset ReadTrips()
        {
            if (_tripsTable is null)
            {
                return TripRuleset.None;
            }

            TravelTime crossing = ReadCrossingCost();
            (TravelTime fast, TravelTime moderate, TravelTime ceiling) = ReadCommuteRungs();

            return new TripRuleset(crossing, fast, moderate, ceiling);
        }

        /// <summary>
        /// The Commute Budget's three rungs, or three <see cref="TravelTime.Impassable"/> for a city
        /// with no ceiling.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Two refusals, and they are different failures.</b> A partial set is an author who
        /// stated some of a group; a set out of order is an author who stated all of it and meant
        /// something impossible. Reporting them separately is what makes the message actionable —
        /// <c>adr/0064</c>'s standing lesson is that a loader guard nobody can see is a guard nobody
        /// believes exists, so both ship with tests.
        /// </para>
        /// <para>
        /// <b>Strictly increasing rather than merely non-decreasing.</b> Two equal rungs are a band
        /// no commute can ever fall in, which is a rung that exists in the file and not in the city —
        /// and the reading that band produces, a Census counter pinned at zero, is indistinguishable
        /// from a mechanism that has stopped. That is the same argument <c>[[building]] jobs</c> made
        /// for putting full employment out of reach: <b>a number nothing can occupy reports the same
        /// thing whether it is right or broken.</b>
        /// </para>
        /// </remarks>
        private (TravelTime Fast, TravelTime Moderate, TravelTime Ceiling) ReadCommuteRungs()
        {
            TravelTime fast = ReadRung("commute_fast_minutes");
            TravelTime moderate = ReadRung("commute_moderate_minutes");
            TravelTime ceiling = ReadCommuteBudget();

            int stated = (fast.IsImpassable ? 0 : 1)
                + (moderate.IsImpassable ? 0 : 1)
                + (ceiling.IsImpassable ? 0 : 1);

            if (stated == 0)
            {
                return (TravelTime.Impassable, TravelTime.Impassable, TravelTime.Impassable);
            }

            if (stated < 3)
            {
                Refuse(LineOfTrip("commute_budget_minutes"), null,
                    "[trips] states some of the Commute Budget's three rungs and not all of them. "
                    + "commute_fast_minutes, commute_moderate_minutes and commute_budget_minutes are "
                    + "one decision in three keys (adr/0095): the first two grade a commute that "
                    + "happens anyway and the last is the ceiling, which is the only edge that "
                    + "refuses a Trip. State all three, or delete all three for a city whose "
                    + "commutes are never refused for their length and never graded.");

                return (TravelTime.Impassable, TravelTime.Impassable, TravelTime.Impassable);
            }

            if (fast >= moderate || moderate >= ceiling)
            {
                Refuse(LineOfTrip("commute_moderate_minutes"), null,
                    "the Commute Budget's rungs are not strictly increasing. A commute is fast up to "
                    + "commute_fast_minutes, moderate up to commute_moderate_minutes and unsavoury "
                    + "up to commute_budget_minutes, so each must be larger than the one before it. "
                    + "Two equal rungs are a band no commute can fall in, and a band nothing can "
                    + "occupy reads in the Census exactly like a mechanism that has stopped.");

                return (TravelTime.Impassable, TravelTime.Impassable, TravelTime.Impassable);
            }

            return (fast, moderate, ceiling);
        }

        /// <summary>
        /// One of the two lower rungs, in in-world clock minutes, or
        /// <see cref="TravelTime.Impassable"/> when the key is absent.
        /// </summary>
        /// <remarks>
        /// <b>The range is the ceiling's, and the ordering guard is what actually constrains
        /// these.</b> A lower rung has no meaningful bound of its own — how fast a fast commute is
        /// depends entirely on where the ceiling sits — so bounding it independently would be
        /// inventing a second opinion about the same number.
        /// </remarks>
        private TravelTime ReadRung(string key)
        {
            if (!TryInteger(_tripsTable!, key, out long value, required: false))
            {
                return TravelTime.Impassable;
            }

            if (value < 1 || value > MaximumBudgetMinutes)
            {
                Refuse(LineOfTrip(key), null,
                    $"{key} = {value} is out of range. It is a rung of the Commute Budget, in "
                    + $"in-world clock minutes, so it is at least 1 and at most "
                    + $"{MaximumBudgetMinutes}, which is what a travel time can hold. It must also "
                    + "be strictly below commute_budget_minutes, which is the ceiling.");

                return TravelTime.Impassable;
            }

            return TravelTime.FromMinutes((int)value);
        }

        /// <summary>
        /// What it costs on foot to reach the other side of a Segment, authored in in-world seconds.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Hash-bearing and unratified</b> (<c>adr/0074</c>, <c>plans/0002</c> §D): it changes a
        /// walk Leg's cost, therefore the Commute Budget, therefore a Trip Fate, therefore the city.
        /// The named ratifier is the first run that reports the walk-Leg cost distribution with the
        /// term at zero and at a candidate value, which is this milestone's own long run.
        /// </para>
        /// <para>
        /// <b>Zero is legitimate and is not an absence.</b> It is rung 1 of <c>adr/0074</c>'s table —
        /// what the corpus had by omission, a city where a shop opposite is the shop next door — so
        /// it is authorable and says so. What cannot be authored is the absence, which is why the
        /// key is required once the table exists.
        /// </para>
        /// <para>
        /// <b>The ceiling is an in-world hour, and the refusal names the mechanism that belongs
        /// instead.</b> Rung 2's whole approximation is that a Street may be crossed wherever you
        /// like at a constant cost; a road you wait an hour to cross is not that road, and the design
        /// already models the uncrossable road — it is an <b>Arterial</b>, whose Arcs carry no foot
        /// bit at all and whose crossings are authored Junction pieces. That is <c>adr/0074</c>'s own
        /// revisit trigger read forwards.
        /// </para>
        /// </remarks>
        private TravelTime ReadCrossingCost()
        {
            if (!TryInteger(_tripsTable!, "crossing_seconds", out long value, required: true))
            {
                return TravelTime.Zero;
            }

            if (value < 0 || value > MaximumCrossingSeconds)
            {
                Refuse(LineOfTrip("crossing_seconds"), null,
                    $"crossing_seconds = {value} is out of range. It is what it costs on foot to "
                    + "reach the other side of a Segment, in in-world seconds, so it is at least 0 "
                    + "-- a city where crossing is free is a city -- and at most "
                    + $"{MaximumCrossingSeconds}. A road somebody waits longer than an hour to cross "
                    + "is not a Street that may be crossed at will; it is an Arterial, which carries "
                    + "no pedestrian Arcs between its Junction pieces and is how this design models "
                    + "a road you cannot walk over.");

                return TravelTime.Zero;
            }

            return TravelTime.FromSeconds((int)value);
        }

        /// <summary>
        /// The line between a Trip that completes and one whose Fate is <i>exceeded commute
        /// budget</i>, authored in in-world clock minutes — or nothing, for a city with no such line.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The one optional key in a table that is otherwise required, and the reason is that this
        /// number cannot be authored yet.</b> It is a percentile of a Trip-cost distribution
        /// (<c>plans/0002</c> §D, session F) and is meaningless before one exists, so a value here
        /// today would be the thing <c>adr/0052</c> forbids: a hash-bearing number chosen because the
        /// schema wanted one.
        /// </para>
        /// <para>
        /// <b>Minutes, one currency across every mode, and there is no per-mode weight</b>
        /// (<c>CONTEXT.md</c> → Commute Budget). A weight would make the quantity scored differ from
        /// the quantity displayed, which is SC4's unlearnability failure and the thing the Budget
        /// exists to prevent; distaste for walking belongs to the choice model.
        /// </para>
        /// <para>
        /// <b>Zero is refused rather than read as <i>nobody travels</i>.</b> A ceiling of no minutes
        /// fails every Trip in the city, which is not a city anybody meant to author — and the intent
        /// it would most likely be reaching for, <i>length never refuses a Trip</i>, is what omitting
        /// the key already says.
        /// </para>
        /// </remarks>
        private TravelTime ReadCommuteBudget()
        {
            if (!TryInteger(_tripsTable!, "commute_budget_minutes", out long value, required: false))
            {
                // No ceiling. Not a default and not a placeholder: it is outside the range of
                // legitimate minute counts, so nothing downstream can mistake it for a chosen value,
                // and it is the only city whose cost distribution is uncensored by the number it is
                // waiting for.
                return TravelTime.Impassable;
            }

            if (value < 1 || value > MaximumBudgetMinutes)
            {
                Refuse(LineOfTrip("commute_budget_minutes"), null,
                    $"commute_budget_minutes = {value} is out of range. It is how long a journey may "
                    + "take before the person making it gives up, in in-world clock minutes, so it "
                    + $"is at least 1 and at most {MaximumBudgetMinutes} -- just under four Days, "
                    + "which is what a travel time can hold. If the intent is that a Trip's length "
                    + "never refuses it, delete the key: an omitted commute_budget_minutes is a city "
                    + "with no Commute Budget, which is stated rather than defaulted.");

                return TravelTime.Impassable;
            }

            return TravelTime.FromMinutes((int)value);
        }

        /// <summary>The line a <c>[trips]</c> key is on, or the table's.</summary>
        private int LineOfTrip(string key) =>
            LineOf((SyntaxNodeBase?)Find(_tripsTable!, key) ?? _tripsTable!);

        // ---- jobs -------------------------------------------------------------------------------

        /// <summary>
        /// The <c>[jobs]</c> table: how often a Citizen with no Workplace looks for one, how long the
        /// pass takes to look at everybody, and how many places one person sees (<c>adr/0081</c>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b><c>[placement]</c>'s three keys, <c>[placement]</c>'s polarity and
        /// <c>[placement]</c>'s reasoning</b>, because this is the same shape of pass over a different
        /// population: a sampled sweep looking for something with room. The absence means nobody is
        /// ever assigned work, which is loud — no Citizen has a Workplace and the Census says so —
        /// rather than a city employing people at a cadence its author never wrote.
        /// </para>
        /// <para>
        /// <b>A <c>[jobs]</c> table with no <c>[trips] commute_budget_minutes</c> above it is
        /// refused, and that cross-table refusal is the decision this reader carries.</b> The pass has
        /// no search radius of its own: the box it draws candidates from is <em>what a walk within the
        /// Commute Budget can reach</em>, derived from the Budget and the walking speed. Take the
        /// Budget away and there is no bound at all — and the bound that would have to be invented in
        /// its place is exactly the fabricated number this milestone exists to avoid, because an
        /// unbounded draw is S2 R4's uniform origin-destination distribution, which R4 measured is a
        /// different city rather than a noisier one. <c>ReadLots(roads)</c> is the precedent for
        /// reading one table against another; this is the stronger form, where the second table is not
        /// merely a ceiling but the only source of the first's geometry.
        /// </para>
        /// </remarks>
        private JobRuleset ReadJobs(TripRuleset trips)
        {
            if (_jobsTable is null)
            {
                return JobRuleset.None;
            }

            if (!trips.HasCommuteBudget)
            {
                Refuse(LineOf(_jobsTable), null,
                    "[jobs] is declared in a Ruleset with no [trips] commute_budget_minutes. The "
                    + "assignment pass has no search radius of its own -- the places a Citizen looks "
                    + "at are the ones a walk within the Commute Budget can reach -- so with no "
                    + "Budget stated there is no bound on the search, and an unbounded one draws a "
                    + "workplace from anywhere in the city. State a commute_budget_minutes, or "
                    + "delete [jobs] and let the city assign nobody.");

                return JobRuleset.None;
            }

            uint interval = ReadInterval(_jobsTable, null);
            int revisit = ReadJobRevisit(interval);
            int candidates = ReadJobCandidates();
            (int shiftMin, int shiftMax) = ReadShiftHours();
            int early = ReadArriveEarly();

            RefuseShiftShorterThanTheBudget(shiftMin);

            return new JobRuleset(interval, revisit, candidates, shiftMin, shiftMax, early);
        }

        // ---- households -------------------------------------------------------------------------

        /// <summary>
        /// The <c>[households]</c> table: what share of Households keeps a car.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The table is optional and its one key is required</b>, which is <c>[jobs]</c>' shape
        /// rather than <c>[trips]</c>'. Omitting the table is a city where nobody drives and the file
        /// has said so by omission; stating the table and omitting the rate would be a placeholder
        /// sitting inside the range of legitimate answers, and session F's rule is that such a
        /// placeholder cannot announce itself.
        /// </para>
        /// <para>
        /// ⚠ <b>There is no refusal tying this to <c>[trips]</c>, and the asymmetry with
        /// <see cref="ReadJobs"/> is deliberate.</b> <c>[jobs]</c> is refused without a Commute Budget
        /// because the assignment pass would have no bound at all. A car with no Trip model is
        /// harmless: nothing asks what mode anybody travels in until a Trip exists, so the rate is
        /// inert rather than unbounded, and refusing it would be inventing a dependency to look
        /// symmetrical.
        /// </para>
        /// </remarks>
        private HouseholdRuleset ReadHouseholds()
        {
            if (_householdsTable is null)
            {
                return HouseholdRuleset.None;
            }

            if (!TryInteger(_householdsTable, "car_ownership_percent", out long percent, required: true))
            {
                return HouseholdRuleset.None;
            }

            if (percent < 0 || percent > HouseholdRuleset.MaxPercent)
            {
                Refuse(
                    LineOf((SyntaxNodeBase?)Find(_householdsTable, "car_ownership_percent")
                        ?? _householdsTable),
                    null,
                    $"car_ownership_percent is {percent}. It is the share of Households keeping a "
                    + "car, so it is a whole percentage in 0..100 -- 0 is a city before the motor "
                    + "car and 100 is one where every Household has one. A share outside that range "
                    + "is not a smaller or larger city, it is a quantity that is not a share.");

                return HouseholdRuleset.None;
            }

            (Money min, Money max) = ReadOpeningBalance();

            return new HouseholdRuleset((int)percent, min, max);
        }

        /// <summary>
        /// The <c>[households]</c> table's optional opening-balance band.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Optional, and omission means the populator endows nobody</b> — which is what every
        /// Ruleset written before milestone 10 task 5 meant by saying nothing, so omission is
        /// behaviour-preserving. That is <c>[traffic]</c>'s argument rather than
        /// <c>car_ownership_percent</c>'s: zero is a legitimate answer here <em>and</em> it is the
        /// standing one, so a defaulted zero cannot be mistaken for a placeholder nobody set.
        /// </para>
        /// <para>
        /// <b>Both keys or neither</b>, on <c>[trips]</c>' rung precedent — a band with one end
        /// authored is a range whose other end somebody has to guess, and the two guesses available
        /// (the other end, or zero) are a fixed endowment and a band starting at destitute. Those are
        /// different cities.
        /// </para>
        /// <para>
        /// ⚠ <b>A band in a Ruleset that names no money is refused rather than ignored.</b> A balance
        /// is a Bin and a Bin exists only for a declared Resource (<c>adr/0114</c>), so
        /// <c>World.Endow</c> would throw on the first Household. The loader can see both halves —
        /// the families are read in the first pass — so this is a refusal at load with a file and a
        /// line rather than a crash at world creation.
        /// </para>
        /// </remarks>
        private (Money Min, Money Max) ReadOpeningBalance()
        {
            KeyValueSyntax? low = Find(_householdsTable!, "opening_balance_min");
            KeyValueSyntax? high = Find(_householdsTable!, "opening_balance_max");

            if (low is null && high is null)
            {
                return (Money.Zero, Money.Zero);
            }

            if (low is null || high is null)
            {
                Refuse(
                    LineOf((SyntaxNodeBase?)low ?? (SyntaxNodeBase?)high ?? _householdsTable!),
                    null,
                    "an opening balance is a band and this file states one end of it. Write both "
                    + "opening_balance_min and opening_balance_max, or neither. One end alone has "
                    + "two readings -- a fixed endowment, or a band reaching down to destitute -- "
                    + "and they are different cities.");

                return (Money.Zero, Money.Zero);
            }

            if (!TryInteger(_householdsTable!, "opening_balance_min", out long min, required: true)
                || !TryInteger(_householdsTable!, "opening_balance_max", out long max, required: true))
            {
                return (Money.Zero, Money.Zero);
            }

            if (min < 0)
            {
                Refuse(
                    LineOf(low), null,
                    $"opening_balance_min is {min}. A balance is a stock and a stock is never "
                    + "negative -- a debt is not negative money (adr/0003), and founding a city in "
                    + "arrears is a mechanism nobody has designed.");

                return (Money.Zero, Money.Zero);
            }

            if (max < min)
            {
                Refuse(
                    LineOf(high), null,
                    $"opening_balance_max is {max}, below opening_balance_min of {min}. A band is "
                    + "drawn inclusive of both ends, so an inverted one is empty rather than "
                    + "narrow.");

                return (Money.Zero, Money.Zero);
            }

            if (max > 0 && !_families.Contains(ResourceFamily.Money))
            {
                Refuse(
                    LineOf(high), null,
                    "this file endows Households and names no money. A balance is a Bin and a Bin "
                    + "exists only for a declared Resource (adr/0114), so there would be nowhere to "
                    + "put it. Add a [[resource]] block with family = \"money\".");

                return (Money.Zero, Money.Zero);
            }

            return (new Money(min), new Money(max));
        }

        // ---- parking ----------------------------------------------------------------------------

        /// <summary>
        /// The <c>[parking]</c> table: how far a driver will walk from a Car Park.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Two required keys in an optional table</b>, on <c>[households]</c>' shape. Omitting the
        /// table is a city with no Parking Shed — which is every city this project described before
        /// milestone 7, so omission is behaviour-preserving rather than a placeholder.
        /// </para>
        /// <para>
        /// ⚠ <b><c>shed_keeps</c> belongs here and nowhere else, and that is a correctness constraint
        /// rather than a filing preference.</b> A per-<em>kind</em> cap is a plausible design — a
        /// shopping centre's shed reaching further than a house's — and it would go on
        /// <c>[[building]]</c> beside <c>parking</c>, where it looks like it belongs. It must not.
        /// <c>CarParkTable.Capacity</c> is <c>(derived AND rebuilt)</c> from the Ruleset in force
        /// (<c>adr/0064</c>, <c>adr/0068</c>), so a key on the kind moves the <em>standing</em> city's
        /// State Hash with no code change and no mechanism reading it. In <c>[parking]</c> it moves
        /// only the Ruleset content hash. <b>What protects the hash is the key's location, not the
        /// absence of a reader.</b>
        /// </para>
        /// <para>
        /// ⚠ <b>The radius is authored in <em>metres</em> and every consumer sees Tiles</b>, which is
        /// <c>[layers] kernel_metres</c>' shape and was chosen against a key in minutes. Minutes would
        /// have put the radius in the Commute Budget's own currency — but a key in minutes invites the
        /// one derivation <c>adr/0083</c> forbids by name, and it would make shed membership move
        /// whenever somebody retuned <c>walk_speed_kph</c>. <b>A radius in metres is the same set of
        /// Car Parks however fast anybody walks.</b>
        /// </para>
        /// <para>
        /// ⚠ <b><c>adr/0083</c>'s upper bound is <em>not</em> enforced here, and that is a decision.</b>
        /// A shed wider than the Commute Budget's walk allowance has outer Car Parks that can never be
        /// taken, and a guard was written and withdrawn: the Budget is a ceiling on a <b>whole
        /// journey</b> and a parking walk is one <b>Leg</b> inside it, so the only non-arbitrary
        /// threshold available — the whole Budget — is far looser than the real constraint, and it
        /// refuses nothing a designer would plausibly write while breaking every fixture that tightens
        /// a Budget for reasons of its own. ***A bound stated as a constraint on choosing a number is
        /// not thereby a predicate over two files.*** It lives in <c>plans/0002</c> §D2 and in
        /// <c>minimal.toml</c>'s own header, where <c>adr/0083</c> put it.
        /// </para>
        /// </remarks>
        private ParkingRuleset ReadParking()
        {
            if (_parkingTable is null)
            {
                return ParkingRuleset.None;
            }

            if (!TryInteger(_parkingTable, "radius_metres", out long metres, required: true))
            {
                return ParkingRuleset.None;
            }

            // Refused at zero rather than defaulted, because zero is not "no parking" here -- the
            // supply is a kind's `parked` against [capacity] floor_tiles_per_parking_space, and this
            // is the reach. A shed of zero metres is a city
            // whose Car Parks all exist and none can be walked to from anywhere, which is a sentence
            // nobody meant to write and whose symptom names neither this key nor that one.
            if (metres < 1)
            {
                Refuse(LineOfParking("radius_metres"), null,
                    $"radius_metres is {metres}. It is how far a driver will walk from a Car Park, "
                    + "so a shed with no reach finds nothing and every arrival fails to park. Delete "
                    + "the [parking] table for a city with no Parking Shed at all.");

                return ParkingRuleset.None;
            }

            if (metres > int.MaxValue)
            {
                metres = int.MaxValue;
            }

            if (!TryInteger(_parkingTable, "shed_keeps", out long keeps, required: true))
            {
                return ParkingRuleset.None;
            }

            // Refused at zero for radius_metres' reason and one of its own. A shed that keeps nothing
            // is a city whose arrivals all fail to park, which nobody means to write -- and zero is
            // also the one value that disables ParkingShed's early exit, so a file could silently buy
            // itself the exhaustive ball. A performance cliff must not be reachable by a value that
            // reads as "off".
            if (keeps < 1)
            {
                Refuse(LineOfParking("shed_keeps"), null,
                    $"shed_keeps is {keeps}. It is how many Car Parks a Building's Parking Shed holds, "
                    + "so a shed that keeps none finds nothing and every arrival fails to park. Delete "
                    + "the [parking] table for a city with no Parking Shed at all.");

                return ParkingRuleset.None;
            }

            // The ceiling is arithmetic rather than taste, on [traffic] beta's precedent. A shed is
            // materialised per Building at a fixed width, so this multiplies the whole city: at the
            // 1M target's 84,320 Buildings a keep of 24 is 8.0 MiB and 4,096 would be 1.3 GiB. The
            // rung refused here is the one where a plausible typo stops being a tuning choice and
            // becomes an allocation failure with no line number.
            if (keeps > 1024)
            {
                Refuse(LineOfParking("shed_keeps"), null,
                    $"shed_keeps is {keeps}. A shed is stored for every Building at this width, so "
                    + "this is a per-city cost rather than a per-query one; 1024 is already far past "
                    + "any radius this project has measured a shed at.");

                return ParkingRuleset.None;
            }

            return new ParkingRuleset((int)metres, (int)keeps);
        }

        /// <summary>Reads <c>[water]</c>, or answers that the world has none.</summary>
        private WaterRuleset ReadWater()
        {
            if (_waterTable is null)
            {
                return WaterRuleset.None;
            }

            if (!TryInteger(_waterTable, "sea_level_percent", out long percent, required: true))
            {
                return WaterRuleset.None;
            }

            // Refused at BOTH ends, and neither is a range check for its own sake. Zero puts the sea
            // at the lowest Cell on the map, which is a world with no water -- a second spelling of
            // the absent table, and a designer who wrote it would mean something the generator cannot
            // hear. A hundred puts every Cell under it, which is not a city. adr/0160.
            if (percent is < 1 or > 99)
            {
                Refuse(LineOfWater("sea_level_percent"), null,
                    $"sea_level_percent is {percent}. It is how high the sea stands as a fraction of "
                    + "the height range this world realised, so it must be between 1 and 99. Delete "
                    + "the [water] table for an inland world with no coast at all; 100 would put the "
                    + "whole map under water.");

                return WaterRuleset.None;
            }

            // Absent means no floodplain, which is a steep coast and a world. adr/0123: the absence
            // is the spelling, so nothing here defaults it to a number that would read as a decision.
            if (!TryInteger(_waterTable, "flood_level_percent", out long flood, required: false))
            {
                return WaterRuleset.From((int)percent);
            }

            // AT OR BELOW the sea is ground already under water, so the Hazard Region it describes is
            // empty -- a key that reads as a decision and derives nothing, which is adr/0123's failure
            // arriving in a loader. 100 puts the flood at the map's highest Cell, which is a drowning
            // rather than a floodplain. adr/0157.
            if (flood <= percent || flood > 99)
            {
                Refuse(LineOfWater("flood_level_percent"), null,
                    $"flood_level_percent is {flood} and sea_level_percent is {percent}. It is how "
                    + "high a flood reaches on the same scale, so it must be above the sea and below "
                    + "100. Omit it for a world with no floodplain -- a steep coast is a world, and a "
                    + "flood at or below the sea would describe ground that is already under it.");

                return WaterRuleset.None;
            }

            return WithBin(WaterRuleset.From((int)percent, (int)flood));
        }

        /// <summary>Reads <c>[water]</c>'s three Bin keys, or answers that the bodies hold nothing.</summary>
        /// <remarks>
        /// <b>Three keys that stand or fall together.</b> A Bin needs a Resource to hold, a capacity
        /// and an outflow; any one alone describes half a mechanism, so stating one and not the others
        /// is refused rather than defaulted. Absent altogether is water with no level, which is every
        /// shipped file but <c>coastal.toml</c> and is what <c>adr/0123</c> calls the honest spelling.
        /// </remarks>
        private WaterRuleset WithBin(WaterRuleset water)
        {
            SyntaxNodeBase? carries = Find(_waterTable!, "carries");
            bool hasCapacity = Find(_waterTable!, "capacity_per_cell") is not null;
            bool hasOutflow = Find(_waterTable!, "outflow_per_exit_per_day") is not null;
            bool hasRunoff = Find(_waterTable!, "runoff_per_sealed_cell_per_day") is not null;

            if (carries is null && !hasCapacity && !hasOutflow && !hasRunoff)
            {
                return water;
            }

            if (carries is null || !hasCapacity || !hasOutflow || !hasRunoff)
            {
                Refuse(LineOfWater("carries"), null,
                    "[water] states some of carries, capacity_per_cell, outflow_per_exit_per_day "
                    + "and runoff_per_sealed_cell_per_day but not all four. A Water Body's Bin needs "
                    + "a Resource to hold, a capacity, a way out and a way in; any one of them alone "
                    + "describes part of a mechanism. Omit all four for water with no level. "
                    + "adr/0161.");

                return WaterRuleset.None;
            }

            if (!TryString(_waterTable!, "carries", out string? name, required: true)
                || !TryInteger(_waterTable!, "capacity_per_cell", out long capacity, required: true)
                || !TryInteger(
                       _waterTable!, "outflow_per_exit_per_day", out long outflow, required: true)
                || !TryInteger(
                       _waterTable!, "runoff_per_sealed_cell_per_day", out long runoff, required: true))
            {
                return WaterRuleset.None;
            }

            if (!_resources.TryGetValue(name!, out ushort id))
            {
                Refuse(LineOfWater("carries"), null,
                    $"[water] carries = \"{name}\" and no [[resource]] declares that name. A Water "
                    + "Body holds a Resource the Ruleset declared, not a name it invented.");

                return WaterRuleset.None;
            }

            var resource = new ResourceId(id);

            // adr/0161, and it is the whole of that decision expressed as a check. A Water Body moves
            // its contents along an edge of the water graph, with no Vehicle -- and adr/0031 defines a
            // Good as a Resource whose movement between Districts REQUIRES one. So a Good here would
            // be a counterexample to the definition of Good sitting inside a loaded world.
            if (_families[id - 1] != ResourceFamily.Utility)
            {
                Refuse(LineOfWater("carries"), null,
                    $"[water] carries = \"{name}\", whose family is "
                    + $"{_families[id - 1].ToString().ToLowerInvariant()}. A Water Body's Bin holds "
                    + "a utility-family Resource: it moves its contents along an edge of the water "
                    + "graph with no Vehicle, and a good is by definition a Resource whose movement "
                    + "between Districts requires one. adr/0161, adr/0031.");

                return WaterRuleset.None;
            }

            if (capacity is < 1 or > int.MaxValue)
            {
                Refuse(LineOfWater("capacity_per_cell"), null,
                    $"capacity_per_cell is {capacity}. It is how much one wet Cell of a body holds, "
                    + "so it must be at least 1 -- a body that holds nothing is an infinite sink "
                    + "wearing the opposite spelling, and CONTEXT.md -> Water Body's \"nothing is an "
                    + "infinite sink\" is the reason a capacity exists at all.");

                return WaterRuleset.None;
            }

            if (outflow is < 1 or > int.MaxValue)
            {
                Refuse(LineOfWater("outflow_per_exit_per_day"), null,
                    $"outflow_per_exit_per_day is {outflow}. It is how much leaves through one exit "
                    + "in a Day, so it must be at least 1. Zero would mean no body anywhere drains, "
                    + "which is what omitting these three keys already says.");

                return WaterRuleset.None;
            }

            if (runoff is < 1 or > int.MaxValue)
            {
                Refuse(LineOfWater("runoff_per_sealed_cell_per_day"), null,
                    $"runoff_per_sealed_cell_per_day is {runoff}. It is what a FULLY sealed Cell "
                    + "sheds into the body it drains to in a Day, so it must be at least 1. Zero "
                    + "would leave every Bin at zero for ever, which is a level nothing can move and "
                    + "therefore a shoreline term that is present and permanently zero -- adr/0123.");

                return WaterRuleset.None;
            }

            return water.WithBin(resource, (int)capacity, (int)outflow, (int)runoff);
        }

        /// <summary>Reads <c>[disasters]</c>, or answers that nothing ever happens to the ground.</summary>
        /// <remarks>
        /// <para>
        /// <b>Three durations, all required together.</b> <c>01 §5.2</c>: <em>"No severity constant
        /// is authored anywhere; the only constants are a frequency interval and a spread rate, both
        /// durations, both scale-free."</em> A partial table describes half a mechanism — a flood
        /// with no interval never fires, one with no rise inundates its whole reach on a single
        /// Tick, one with no recession never leaves — and none of those is what an author omitting a
        /// key would have meant. <c>adr/0123</c>: the absence of the <em>table</em> is the spelling
        /// for a world with no Disasters in it, and a partial table is not a second spelling of it.
        /// </para>
        /// <para>
        /// ⚠ <b>Refused without <c>[water] flood_level_percent</c>, and the pairing is one-way.</b>
        /// The Hazard Region is generated from that key alone, so a schedule without it is an event
        /// with nowhere to happen — a table that reads as a decision and derives nothing. The
        /// converse is a world: <c>coastal.toml</c> has the floodplain and no schedule over it, which
        /// is <c>01 §5.3</c>'s posted price before the first Act of God.
        /// </para>
        /// </remarks>
        private DisasterRuleset ReadDisasters(WaterRuleset water)
        {
            if (_disastersTable is null)
            {
                return DisasterRuleset.None;
            }

            if (!water.HasFloodplain)
            {
                Refuse(LineOfDisasters("flood_every_days"), null,
                    "[disasters] is stated and [water] flood_level_percent is not. A flood happens "
                    + "on the Hazard Region, which is generated from that key alone -- so this table "
                    + "schedules an event with nowhere to occur. State a flood level, or delete this "
                    + "table for a world where nothing happens to the ground.");

                return DisasterRuleset.None;
            }

            if (!TryInteger(_disastersTable, "flood_every_days", out long every, required: true)
                | !TryInteger(_disastersTable, "flood_rises_over_days", out long rises, required: true)
                | !TryInteger(
                    _disastersTable, "flood_recedes_over_days", out long recedes, required: true))
            {
                // Non-shortcutting on purpose, so an author missing two keys is told about both. The
                // three stand or fall together, so reporting them one run at a time is three runs.
                return DisasterRuleset.None;
            }

            if (every <= 0 || rises <= 0 || recedes <= 0)
            {
                Refuse(LineOfDisasters("flood_every_days"), null,
                    $"[disasters] states flood_every_days {every}, flood_rises_over_days {rises} and "
                    + $"flood_recedes_over_days {recedes}. All three are durations in Days and must "
                    + "be positive. Zero is not a spelling for 'never' here -- delete the table.");

                return DisasterRuleset.None;
            }

            if (rises + recedes > every)
            {
                // NOT REFUSED, and this is the one thing here a designer might mean. A lifetime longer
                // than the interval is overlapping floods, which is a world -- the table holds rows
                // and DisasterEngine advances each -- so the loader says nothing about it. Recorded
                // here because the first reading of this code will wonder whether it was checked.
            }

            return DisasterRuleset.From((int)every, (int)rises, (int)recedes);
        }

        /// <summary>The line a <c>[disasters]</c> key is on, or the table's.</summary>
        private int LineOfDisasters(string key) =>
            LineOf((SyntaxNodeBase?)Find(_disastersTable!, key) ?? _disastersTable!);

        /// <summary>The line a <c>[water]</c> key is on, or the table's.</summary>
        private int LineOfWater(string key) =>
            LineOf((SyntaxNodeBase?)Find(_waterTable!, key) ?? _waterTable!);

        /// <summary>The line a <c>[parking]</c> key is on, or the table's.</summary>
        private int LineOfParking(string key) =>
            LineOf((SyntaxNodeBase?)Find(_parkingTable!, key) ?? _parkingTable!);

        // ---- terrain ----------------------------------------------------------------------------

        /// <summary>
        /// The <c>[[terrain]]</c> tables: what each sort of ground is worth before anything is built.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <c>adr/0158</c>, milestone 24 task 2. <b>Optional as a set and all-or-nothing within it</b>
        /// — a file states five <c>[[terrain]]</c> tables or none. Absence is
        /// <see cref="TerrainRuleset.None"/>, a Ruleset declining to price its ground, and never a
        /// world without terrain in it: <b>the type column is written from the <c>WorldKey</c>
        /// either way</b> (<c>adr/0021</c>).
        /// </para>
        /// <para>
        /// <b>Plural and array-of-tables rather than one <c>[terrain]</c> with five keys</b>, on
        /// <c>[[hinterland]]</c>'s shape: the row is per member of a set, so the file names the member
        /// it is pricing. It also makes the all-five check a count rather than five key lookups, and
        /// gives a missing type a line number of its own to be reported against.
        /// </para>
        /// <para>
        /// ⚠ <b>A missing type is refused rather than defaulted, and <see cref="TerrainRuleset.Kinds"/>
        /// carries the argument</b>: the generator places all five whatever the file says, so an
        /// unstated type would be ground the world contains and the file prices at zero — a silent
        /// sterile band rather than an error.
        /// </para>
        /// </remarks>
        private TerrainRuleset ReadTerrain()
        {
            if (_terrainTables.Count == 0)
            {
                return TerrainRuleset.None;
            }

            // Sentinel rather than zero, because zero is a Base Fertility a file may legitimately
            // state: adr/0022's scale runs from barren to fully fertile and the bottom of it means
            // something. A stated-ness flag per type would be the same fact in a second array.
            const int Unstated = -1;

            var fertilities = new int[TerrainRuleset.Kinds];
            Array.Fill(fertilities, Unstated);

            // The same sentinel and the same reason: zero is rock's real answer -- never recovers --
            // so no value in range can double as unset. Milestone 24 task 4, adr/0044.
            var decays = new int[TerrainRuleset.Kinds];
            Array.Fill(decays, Unstated);

            foreach (TableSyntaxBase table in _terrainTables)
            {
                if (!TryString(table, "name", out string? name, required: true)
                    || !TryTerrainKind(table, name!, out TerrainKind kind))
                {
                    continue;
                }

                if (fertilities[(int)kind] != Unstated)
                {
                    Refuse(LineOf((SyntaxNodeBase?)Find(table, "name") ?? table), null,
                        $"a second [[terrain]] is declared for '{name}'. A terrain type has one Base "
                        + "Fertility, shared by every Cell of that ground, so two tables for one type "
                        + "is ambiguous rather than additive -- there is no rule saying which of them "
                        + "a Cell reads.");

                    continue;
                }

                if (!TryInteger(table, "base_fertility_percent", out long percent, required: true))
                {
                    continue;
                }

                // Refused above 100 and not clamped. adr/0156 makes 1.0 mean FULLY fertile, so the
                // top of the scale is the scale's own rather than a chosen bound -- and Fertility
                // composes as a proportion against it. A file above the top is not a very good field;
                // it is a file whose author believes the units are something else.
                if (percent is < 0 or > 100)
                {
                    Refuse(LineOf((SyntaxNodeBase?)Find(table, "base_fertility_percent") ?? table),
                        null,
                        $"base_fertility_percent is {percent} for '{name}'. It is the ceiling this "
                        + "ground's Fertility starts at, as a percentage of fully fertile, so the "
                        + "scale runs 0 to 100 and 100 is its top rather than a tuning choice "
                        + "(adr/0156).");

                    continue;
                }

                fertilities[(int)kind] = IntegerMath.RoundDiv(Fixed.FromInt((int)percent), 100);

                if (!TryInteger(table, "sealing_decay_tau", out long tau, required: true))
                {
                    continue;
                }

                // Refused below zero and above what the operator can express. Zero MEANS never and is
                // rock's answer. The ceiling is not arbitrary: the decay step is value/tau, so a tau
                // past twice a Cell's Tile count rounds that step to nothing on a FULL Cell and the
                // ground never moves at all -- a rate so slow it is silently the same as zero, which
                // is the one value a designer must not be able to write by accident.
                if (tau is < 0 or > (CellGrid.TilesInCell * 2))
                {
                    Refuse(LineOf((SyntaxNodeBase?)Find(table, "sealing_decay_tau") ?? table), null,
                        $"sealing_decay_tau is {tau} for '{name}'. It is how many scheduled updates "
                        + "this ground takes to shed its Sealing, so it runs 0 to "
                        + $"{CellGrid.TilesInCell * 2}. Zero means NEVER, which is a real answer and "
                        + "is rock's. A value above the top would be slower than the operator can "
                        + "represent and would read as never without saying so.");

                    continue;
                }

                decays[(int)kind] = (int)tau;
            }

            // Reported per missing type rather than as one "some are missing", because the file's
            // author has to find each one anyway and a count names none of them. Against the table
            // the file DID state, since a set with a hole in it has no line of its own.
            bool complete = true;

            for (int kind = 0; kind < fertilities.Length; kind++)
            {
                if (fertilities[kind] != Unstated && decays[kind] != Unstated)
                {
                    continue;
                }

                complete = false;

                Refuse(LineOf(_terrainTables[0]), null,
                    $"no [[terrain]] fully states '{SpellingOfTerrain((TerrainKind)kind)}' -- it "
                    + "needs both base_fertility_percent and sealing_decay_tau. A file that prices "
                    + "its ground prices all of it: the generator places every terrain type from the "
                    + "WorldKey whatever this file says, so an unstated one is ground the world "
                    + "contains and this file values at nothing (adr/0158), or ground whose recovery "
                    + "rate nothing states (adr/0044).");
            }

            if (!complete || _refusals.Count > 0)
            {
                return TerrainRuleset.None;
            }

            return TerrainRuleset.From(
                fertilities[(int)TerrainKind.Ordinary],
                fertilities[(int)TerrainKind.Rock],
                fertilities[(int)TerrainKind.Floodplain],
                fertilities[(int)TerrainKind.Marsh],
                fertilities[(int)TerrainKind.ThinSoil],
                decays[(int)TerrainKind.Ordinary],
                decays[(int)TerrainKind.Rock],
                decays[(int)TerrainKind.Floodplain],
                decays[(int)TerrainKind.Marsh],
                decays[(int)TerrainKind.ThinSoil]);
        }

        /// <summary>Resolves a <c>[[terrain]]</c> <c>name</c> to its <see cref="TerrainKind"/>.</summary>
        /// <remarks>
        /// <b>The set is closed and a file cannot extend it</b>, which is what separates this from
        /// every other <c>name</c> the loader reads: a <c>[[resource]]</c> name declares a Resource,
        /// and this one selects ground the generator already places. <c>adr/0158</c>.
        /// </remarks>
        private bool TryTerrainKind(TableSyntaxBase table, string name, out TerrainKind kind)
        {
            switch (name)
            {
                case "ordinary": kind = TerrainKind.Ordinary; return true;
                case "rock": kind = TerrainKind.Rock; return true;
                case "floodplain": kind = TerrainKind.Floodplain; return true;
                case "marsh": kind = TerrainKind.Marsh; return true;
                case "thin_soil": kind = TerrainKind.ThinSoil; return true;

                default:
                    Refuse(LineOf((SyntaxNodeBase?)Find(table, "name") ?? table), null,
                        $"'{name}' is not a terrain type. The types are 'ordinary', 'rock', "
                        + "'floodplain', 'marsh' and 'thin_soil', and a Ruleset selects among them "
                        + "rather than declaring one -- the generator places the five it knows "
                        + "(adr/0158).");

                    kind = default;
                    return false;
            }
        }

        /// <summary>How a <see cref="TerrainKind"/> is spelled in a Ruleset.</summary>
        private static string SpellingOfTerrain(TerrainKind kind) => kind switch
        {
            TerrainKind.Ordinary => "ordinary",
            TerrainKind.Rock => "rock",
            TerrainKind.Floodplain => "floodplain",
            TerrainKind.Marsh => "marsh",
            TerrainKind.ThinSoil => "thin_soil",
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

        private DistrictRuleset ReadDistricts()
        {
            if (_districtsTable is null)
            {
                return DistrictRuleset.None;
            }

            if (!TryInteger(_districtsTable, "prominence_percent", out long percent, required: true))
            {
                return DistrictRuleset.None;
            }

            // Refused at zero rather than defaulted, on [parking] radius_metres' reason. Zero is not
            // "one District": it is a threshold every dip in the field clears, so every local bump
            // becomes a centre and the city fragments into as many Districts as it has Cells. A file
            // that wants no Districts deletes the table, which is a sentence somebody meant to write.
            if (percent < 1)
            {
                Refuse(LineOfDistricts("prominence_percent"), null,
                    $"prominence_percent is {percent}. It is how far a peak must stand above its "
                    + "saddle before it is a centre of its own, as a percentage of its own height, so "
                    + "zero makes every bump a District and the city has as many as it has Cells. "
                    + "Delete the [districts] table for a city with no Districts at all.");

                return DistrictRuleset.None;
            }

            // A hundred is the whole of a peak's height, which is the prominence of a hill nothing
            // touches. Above it the test is unsatisfiable: no saddle is below zero, so the only
            // Districts left are the connected components, and the watershed is doing nothing while
            // appearing to be configured. A knob whose top end silently disables it is refused.
            if (percent > 100)
            {
                Refuse(LineOfDistricts("prominence_percent"), null,
                    $"prominence_percent is {percent}. It is a percentage of the peak's own height, "
                    + "so 100 already means a peak that rises from nothing; above that no peak can "
                    + "ever qualify and the Districts collapse to the road components, which is the "
                    + "clip working alone rather than a threshold doing anything.");

                return DistrictRuleset.None;
            }

            if (!TryInteger(_districtsTable, "revisit_ticks", out long revisit, required: true)
                || !TryInteger(_districtsTable, "hysteresis_percent", out long band, required: true)
                || !TryInteger(_districtsTable, "migrate_cells", out long migrate, required: true))
            {
                return DistrictRuleset.None;
            }

            // All three are REQUIRED of a file that states the table, on [parking]'s rule: a stated
            // table states its keys. None of them has a defensible default -- a defaulted cadence is a
            // hash-bearing number nobody chose, and a defaulted band or bound would be adr/0134's
            // stability mechanisms arriving switched to a setting no designer picked.
            if (revisit < 1)
            {
                Refuse(LineOfDistricts("revisit_ticks"), null,
                    $"revisit_ticks is {revisit}. It is how often the Districts are re-derived, so a "
                    + "period of zero or less is not 'never' -- it is a period, and there is no "
                    + "spelling here for a city whose Districts are found once and frozen. Delete the "
                    + "[districts] table for a city with no Districts at all.");

                return DistrictRuleset.None;
            }

            // Refused at zero because zero is 'a Cell moves on a tie', which adr/0134 forbids in the
            // sentence this key exists to implement -- and a tie is the one case where the watershed's
            // answer is a scan order rather than a finding. Above 100 no margin can ever clear the band
            // and the boundary is frozen for ever, which is [districts] prominence_percent's ceiling
            // arriving on the second key: a knob whose top end silently disables the mechanism.
            if (band < 1 || band > 100)
            {
                Refuse(LineOfDistricts("hysteresis_percent"), null,
                    $"hysteresis_percent is {band}. It is how decisively the field must favour a new "
                    + "District before a Cell changes, as a percentage of the level its own basin "
                    + "reaches it at. Zero moves a Cell on a tie, which is the case the band exists "
                    + "for; above 100 no Cell can ever move and the boundaries are frozen while the "
                    + "file appears to be tuning them.");

                return DistrictRuleset.None;
            }

            // Refused at zero for the band's first reason: a bound of none is a re-evaluation that
            // computes an answer and applies nothing, which is the mechanism switched off by a value
            // that reads as a setting. THERE IS NO CEILING, deliberately -- a large bound is an
            // undamped boundary, which is a legitimate thing to author and announces itself.
            if (migrate < 1)
            {
                Refuse(LineOfDistricts("migrate_cells"), null,
                    $"migrate_cells is {migrate}. It is the most Cells that may change District in "
                    + "one re-evaluation, so a bound of none computes the new boundary and then "
                    + "refuses to move to it. A large value is how you say 'undamped'.");

                return DistrictRuleset.None;
            }

            if (revisit > int.MaxValue)
            {
                revisit = int.MaxValue;
            }

            if (migrate > int.MaxValue)
            {
                migrate = int.MaxValue;
            }

            return new DistrictRuleset(
                (int)percent, (int)revisit, (int)band, (int)migrate);
        }

        /// <summary>The line a <c>[districts]</c> key is on, or the table's.</summary>
        private int LineOfDistricts(string key) =>
            LineOf((SyntaxNodeBase?)Find(_districtsTable!, key) ?? _districtsTable!);

        // ---- market -----------------------------------------------------------------------------

        /// <summary>
        /// The <c>[market]</c> table: how fast a Pool price is allowed to move (<c>adr/0135</c>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Optional, and its absence is every trade clearing at the ceiling for ever</b> — which
        /// is the city every Ruleset in <c>rulesets/</c> described before this key existed, so no
        /// State Hash moves by the key arriving. <c>[layers]</c>'s polarity rather than
        /// <c>[roads]</c>'s, and legitimately: there is an earlier behaviour here and it is exactly
        /// a constant price.
        /// </para>
        /// <para>
        /// <b>No price is authored here and that is the shape of the decision.</b> A Pool opens at
        /// <c>Ruleset.ImportCeiling</c> and moves from there, so the seed <c>adr/0135</c> allowed for
        /// — *"an initial price if the tâtonnement needs a seed"* — is not needed and does not exist.
        /// ***A key that was predicted and then was not required is worth an absence note***, because
        /// otherwise the next reader adds it back.
        /// </para>
        /// </remarks>
        private MarketRuleset ReadMarket()
        {
            if (_marketTable is null)
            {
                return MarketRuleset.None;
            }

            // Both REQUIRED of a file that states the table, on [districts]' rule: a stated table
            // states its keys. Neither has a defensible default -- a defaulted damping is a
            // hash-bearing number nobody chose, arriving at a setting no designer picked.
            if (!TryInteger(_marketTable, "decay_percent", out long decay, required: true)
                || !TryInteger(_marketTable, "move_cap_percent", out long cap, required: true))
            {
                return MarketRuleset.None;
            }

            // Zero is ACCEPTED and it means no smoothing: the rate is the Day's own consumption,
            // which is a twitchy market and a real one. A hundred is different in kind -- the
            // standing rate would never take on a new Day at all, so it stays at the zero it was
            // created with for ever and the price never moves. That is the mechanism switched off by
            // a value that reads as a setting, which is [districts]' ceiling arriving on a third key.
            if (decay < 0 || decay > 99)
            {
                Refuse(LineOfMarket("decay_percent"), null,
                    $"decay_percent is {decay}. It is how much of the standing consumption rate "
                    + "survives one Day, so it is a percentage: below zero is not a weighting, and "
                    + "100 means the rate never takes on a new Day and stays at zero for ever, which "
                    + "is the price frozen while the file appears to be damping it. Zero is allowed "
                    + "and means no smoothing at all.");

                return MarketRuleset.None;
            }

            // Refused at zero because zero is the omitted table: a price that may not move. Refused
            // above 100 because the price is clamped to [0, ceiling], so a cap wider than the ceiling
            // can never bind -- a knob with no effect at the top of its range, which is the same
            // refusal [districts] hysteresis_percent makes.
            if (cap < 1 || cap > 100)
            {
                Refuse(LineOfMarket("move_cap_percent"), null,
                    $"move_cap_percent is {cap}. It is the furthest a price may travel in one Day, as "
                    + "a percentage of the import ceiling, so zero means it never moves -- delete the "
                    + "[market] table to say that -- and above 100 it can never bind, because a price "
                    + "never leaves the range between nothing and the ceiling anyway.");

                return MarketRuleset.None;
            }

            return new MarketRuleset((int)decay, (int)cap);
        }

        /// <summary>The line a <c>[market]</c> key is on, or the table's.</summary>
        private int LineOfMarket(string key) =>
            LineOf((SyntaxNodeBase?)Find(_marketTable!, key) ?? _marketTable!);

        /// <summary>
        /// The <c>[founding]</c> table — <c>adr/0145</c>'s founding channel.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Five refusals, and every one of them is a world that would run and mean nothing.</b> A
        /// founding channel needs a trade to found, money to found it with, a trigger to run on and a
        /// SINK to drain into; the table states none of those four and depends on all of them, so
        /// each absence is checked against the file rather than left to fail quietly at Tick 0.
        /// </para>
        /// <para>
        /// 🔴 <b>The sink one is the one that matters, and its reason is NOT that nothing tenants a
        /// Business.</b> This said so until 2026-08-26 and <c>PlacementEngine.Tenant</c> had been
        /// tenanting them since <c>adr/0147</c>. ***The refusal survives its own stale argument***:
        /// tenanting drains the pool only while vacant premises exist, so it is the city cooperating
        /// rather than a bound, and a file stating <c>[founding]</c> without
        /// <c>gives_up_after_days</c> still grows a collection with elapsed time the moment the city
        /// runs out of room — <c>adr/0006</c>. <c>adr/0130</c>'s <em>whoever builds the gate owes the
        /// give-up rule</em> is the same sentence about the other pool.
        /// </para>
        /// <para>
        /// ⚠ <b>There is no demand key and its absence is the decision</b> (<c>adr/0145</c>'s
        /// amendment). A Household founds on its own means; a key that read shop count or vacancy
        /// would be the RCI meter this design refuses.
        /// </para>
        /// </remarks>
        private FoundingRuleset ReadFounding(
            PlacementRuleset placement, BusinessKindDefinition[] businessKinds,
            CapacityRuleset capacity)
        {
            if (_foundingTable is null)
            {
                return FoundingRuleset.None;
            }

            // A trade to found. adr/0141 gives a Business its kind from [[business]], and a file that
            // founds shops naming no trade would create rows whose kind is zero -- derelict from
            // birth, which is a legal state for a RELOADED Business and an absurd one for a new one.
            if (_businessKinds.Count == 0)
            {
                Refuse(LineOf(_foundingTable), null,
                    "this Ruleset states [founding], so Households found Businesses, and it declares "
                    + "no [[business]] at all -- so every shop founded would name no trade. Declare a "
                    + "[[business]], or remove [founding].");

                return FoundingRuleset.None;
            }

            // Money to found with. The band moves from the founder's Bin to the shop's, and a file
            // with no money Resource has no Bin to move it out of -- so the channel would run, draw
            // its sample, and found nothing, for ever, silently. 02 §4.1's silent non-event.
            if (!DeclaresMoney())
            {
                Refuse(LineOf(_foundingTable), null,
                    "this Ruleset states [founding] and declares no Resource with family = \"money\". "
                    + "Founding moves a band from the founder's balance into the shop's, so with no "
                    + "money in the file nothing can ever be founded and the channel is inert.");

                return FoundingRuleset.None;
            }

            // A trigger to run on. The founding pass rides [placement]'s interval rather than owning
            // a cadence of its own -- see FoundingRuleset.SampleFor -- so a file stating [founding]
            // and no [placement] states a rate with nothing to multiply it by.
            if (!placement.Runs)
            {
                Refuse(LineOf(_foundingTable), null,
                    "this Ruleset states [founding] and no [placement] table. The founding pass runs "
                    + "on placement's trigger rather than owning one, so without it nothing ever "
                    + "considers founding. State [placement], or remove [founding].");

                return FoundingRuleset.None;
            }

            // 🔴 A SINK. This is adr/0130's argument reaching a SECOND pool, and it is the refusal
            // that matters most here: [founding] is an inflow into the unpremised pool, exactly as a
            // gate kind is an inflow into the Unplaced Pool. ⚠ THE POOL HAS A SECOND EXIT AND IT IS
            // NOT A BOUND -- PlacementEngine.Tenant premises pool members into standing Buildings
            // (adr/0147), and it drains the pool only while there is room. So a city that runs out of
            // vacant premises has an inflow and no outflow, which adr/0006 forbids outright, and the
            // give-up bound is the exit that holds when the cooperating one stops. ⚠ This comment
            // said "nothing tenants a Business until milestone 27's placement half" until 2026-08-26,
            // which was true when written and would have read as a reason to DROP this refusal once
            // tenanting shipped. ⚠ The gate check above it says the same sentence about the other
            // pool; neither can see this one.
            if (!placement.GivesUp)
            {
                Refuse(LineOf(_foundingTable), null,
                    "this Ruleset states [founding], so Households found Businesses into the "
                    + "unpremised pool, and [placement] states no gives_up_after_days -- so nothing "
                    + "ever leaves that pool and it grows without bound, which adr/0006 forbids. "
                    + "State how long a Business keeps looking for premises, in Days, or remove "
                    + "[founding].");

                return FoundingRuleset.None;
            }

            // 🔴 A JOB TO DO. adr/0146: the founder becomes the Business's first worker, which is the
            // whole of the labour cost milestone 27 ships -- so a trade declaring no `jobs` is a shop
            // its own founder cannot work at. The pass draws UNIFORMLY over the declared trades, so
            // this is every trade rather than at least one: a jobless trade in the draw would found a
            // shop whose founder is over the ceiling from the instant they are hired, and
            // EvictOverflow would sack them on the next sweep. ⚠ It is conditional on [founding]
            // exactly as gives_up_after_days is conditional on a gate -- a trade nobody founds may
            // employ nobody, and several shipped files have one.
            // ⚠ IT WAS A LOOP OVER TRADES AND IT IS ONE QUESTION NOW. `jobs` was declared per
            // [[business]], so a jobless trade was a property of one paragraph; employment divides
            // floor area now, so "this Ruleset employs nobody" is one fact about the file and the
            // loop below reports it against the first trade rather than against all of them.
            if (capacity.FloorTilesPerJob <= 0 && businessKinds.Length > 0)
            {
                TryString(_businessKindTables[0], "name", out string? trade, required: false);

                Refuse(LineOf(_businessKindTables[0]), trade,
                    "this Ruleset states [founding] and states no [capacity] floor_tiles_per_job, so "
                    + "nobody in this city is employed anywhere. A founder becomes their Business's "
                    + "first worker (adr/0146), so a trade nobody can work at is one nobody can "
                    + "found. State the rate, or remove [founding].");

                return FoundingRuleset.None;
            }

            // Both REQUIRED of a file that states the table, on [market]'s rule: a stated table states
            // its keys, and a defaulted hash-bearing number is one no designer chose.
            if (!TryInteger(_foundingTable, "founding_band", out long band, required: true)
                || !TryInteger(_foundingTable, "reconsider_ticks", out long reconsider, required: true))
            {
                return FoundingRuleset.None;
            }

            // Zero is refused rather than read as `free`. A shop founded for nothing is one every
            // Household can always afford, so the affordability filter -- the whole of adr/0145's
            // `means and not need` -- stops discriminating and the channel becomes a pure rate.
            if (band < 1)
            {
                Refuse(LineOfFounding("founding_band"), null,
                    $"founding_band is {band}. It is what a Household spends to capitalise a shop, and "
                    + "at zero every Household can always afford one -- so the means test that is the "
                    + "whole of the trigger stops discriminating and founding becomes a bare rate.");

                return FoundingRuleset.None;
            }

            // Below the interval it cannot divide into a sample, which is adr/0059's own bound
            // arriving on a second duration. At or above it, the sample is at least one.
            if (reconsider < placement.Interval)
            {
                Refuse(LineOfFounding("reconsider_ticks"), null,
                    $"reconsider_ticks is {reconsider} and the [placement] interval is "
                    + $"{placement.Interval}. It is how long every Household takes to consider "
                    + "founding once, so it must be at least one trigger long -- below that it does "
                    + "not divide into a sample. adr/0059 states the duration and derives the count.");

                return FoundingRuleset.None;
            }

            return new FoundingRuleset(new Money(band), (int)reconsider);
        }

        /// <summary>Whether any declared Resource is money, which founding needs to move a band.</summary>
        private bool DeclaresMoney()
        {
            foreach (ResourceFamily family in _families)
            {
                if (family == ResourceFamily.Money)
                {
                    return true;
                }
            }

            return false;
        }

        private int LineOfFounding(string key) =>
            LineOf((SyntaxNodeBase?)Find(_foundingTable!, key) ?? _foundingTable!);

        /// <summary>
        /// <b>A file with Districts in it prices every Good at some Hinterland</b> (<c>adr/0050</c>,
        /// <c>adr/0135</c>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The anchor is the refusal's whole subject.</b> <c>CONTEXT.md</c> → Hinterland makes a
        /// Hinterland *"the one authored anchor under every price in the design"*, and
        /// <c>adr/0050</c> gives the reason rather than the rule: *"an emergent price needs an anchor
        /// or it can run away."* A District opens a Pool per Good; an unpriced Good's Pool has a
        /// ceiling of zero, so its price is pinned to nothing and every purchase of it is free.
        /// ***An absent anchor does not fail loudly on its own*** — it produces a market in which one
        /// Good costs nothing, which reads as a balance problem rather than as a missing key.
        /// </para>
        /// <para>
        /// ⚠ <b>Gated on <c>[districts]</c> and not on <c>[[hinterland]]</c>, and the asymmetry is
        /// deliberate.</b> A gate kind with no Hinterland behind it is <em>not</em> refusable here —
        /// <see cref="HinterlandDefinition"/> records why: which edge a gate stands on is a property
        /// of where it was placed, and the loader cannot see a world. A Pool is different. Whether a
        /// city has Districts is stated in the file, so this defect is visible at parse time and
        /// belongs to <c>adr/0048</c>.
        /// </para>
        /// </remarks>
        private void RefuseUnpricedGoods(
            DistrictRuleset districts, HinterlandDefinition[] hinterlands, Money[] prices)
        {
            if (!districts.Runs)
            {
                return;
            }

            int stride = _families.Count;

            for (int resource = 0; resource < stride; resource++)
            {
                if (_families[resource] != ResourceFamily.Good)
                {
                    continue;
                }

                bool priced = false;

                for (int hinterland = 0; hinterland < hinterlands.Length; hinterland++)
                {
                    if (prices[(hinterland * stride) + resource].Raw > 0)
                    {
                        priced = true;
                        break;
                    }
                }

                if (priced)
                {
                    continue;
                }

                Refuse(LineOf(_districtsTable!), null,
                    $"'{NameOfResource(new ResourceId((ushort)(resource + 1)))}' is a good and no "
                    + "[[hinterland]] gives it a price, in a file that states [districts]. A District "
                    + "opens a Pool for every good and the Hinterland's price is the only ceiling on "
                    + "what that Pool can charge, so an unpriced good is not merely unanchored -- it "
                    + "is free everywhere, for ever. Add a prices entry for it to some [[hinterland]], "
                    + "or delete the [districts] table.");
            }
        }

        // ---- traffic ----------------------------------------------------------------------------

        /// <summary>
        /// The <c>[traffic]</c> table: BPR's two parameters and the ceiling on what it will read.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Three required keys in an optional table</b>, on <c>[households]</c>' shape. Omitting the
        /// table is a city whose roads never slow down — which is also every city this project
        /// described before 5c task 6, so omission is behaviour-preserving rather than a placeholder.
        /// </para>
        /// <para>
        /// ⚠ <b>α and the clamp are authored as <em>percentages</em> because this file has no
        /// decimals</b>, and 400% is how a <c>v/c</c> ratio of four is ordinarily said out loud. A key
        /// named <c>alpha</c> holding <c>15</c> would be off by two orders of magnitude with nothing to
        /// notice it, which is the failure <c>adr/0094</c>'s <c>Speed.PerKilometrePerHour</c> literal
        /// actually committed — ***the name of a quantity is not its denomination***, so the name
        /// carries the denomination.
        /// </para>
        /// </remarks>
        private TrafficRuleset ReadTraffic()
        {
            if (_trafficTable is null)
            {
                return TrafficRuleset.None;
            }

            if (!TryInteger(_trafficTable, "alpha_percent", out long alpha, required: true)
                || !TryInteger(_trafficTable, "beta", out long beta, required: true)
                || !TryInteger(_trafficTable, "clamp_percent", out long clamp, required: true))
            {
                return TrafficRuleset.None;
            }

            if (alpha < 0 || alpha > 10_000)
            {
                Refuse(LineOf((SyntaxNodeBase?)Find(_trafficTable, "alpha_percent") ?? _trafficTable),
                    null,
                    $"alpha_percent is {alpha}. It is BPR's alpha as a percentage -- how much slower a "
                    + "Segment is at exactly its capacity, so 15 is the textbook value and means 15% "
                    + "slower. Zero is a road that never slows down, which is what omitting [traffic] "
                    + "says; above 10000 the delay at capacity exceeds a hundredfold and the clamp is "
                    + "the term doing the work rather than the curve.");

                return TrafficRuleset.None;
            }

            // Four is textbook and eight already overflows Q16.16 at the clamp: 4^8 is 65,536, which
            // is exactly Fixed.One's whole range. The ceiling is arithmetic rather than taste.
            if (beta < 1 || beta > 6)
            {
                Refuse(LineOf((SyntaxNodeBase?)Find(_trafficTable, "beta") ?? _trafficTable), null,
                    $"beta is {beta}. It is BPR's exponent, a small whole number -- 4 is textbook and "
                    + "is what spike S2 ran. Below 1 the function is not increasing in volume; above 6 "
                    + "a clamped ratio overflows the Q16.16 the multiplication is done in, so the "
                    + "curve stops being computable before it stops being plausible.");

                return TrafficRuleset.None;
            }

            if (clamp < 100 || clamp > 1_000)
            {
                Refuse(LineOf((SyntaxNodeBase?)Find(_trafficTable, "clamp_percent") ?? _trafficTable),
                    null,
                    $"clamp_percent is {clamp}. It is the largest volume/capacity the function will "
                    + "read, as a percentage -- 400 means four times capacity and is what S2 ran. "
                    + "Below 100 the clamp binds before a Segment is even full, so the VDF would be "
                    + "constant across the whole range 03 SS3.2 says it is strong in; above 1000 the "
                    + "delay multiplier runs to five figures and the router is comparing noise.");

                return TrafficRuleset.None;
            }

            return new TrafficRuleset(
                Ratio.FromFraction((int)alpha, 100), (int)beta, Ratio.FromFraction((int)clamp, 100));
        }

        // ---- jobs, continued --------------------------------------------------------------------

        /// <summary>How long the assignment pass takes to look at every Citizen once, in Ticks.</summary>
        /// <remarks>
        /// <b><c>adr/0059</c> a third time</b>, and required rather than defaulted to a Day:
        /// <c>ReadZoneRevisit</c> may derive a Day because a Day is the scale a Zone Rule's
        /// <c>rate</c> keys are denominated in, and nothing here is. How often a person without work
        /// looks for some is a feel decision with no derivation behind it, so it is authored or the
        /// table is absent.
        /// </remarks>
        private int ReadJobRevisit(uint interval)
        {
            if (!TryInteger(_jobsTable!, "revisit_ticks", out long revisit, required: true))
            {
                return Ticks.PerDay;
            }

            if (revisit < 1 || revisit > int.MaxValue)
            {
                Refuse(LineOfJob("revisit_ticks"), null,
                    $"a revisit period of {revisit} is not a duration this world can hold. It is how "
                    + "long the assignment pass takes to look at every Citizen once, in Ticks, so it "
                    + $"is at least 1 and at most {int.MaxValue} -- the engine divides by it. If the "
                    + "intent is to employ nobody, delete the [jobs] table.");
                return Ticks.PerDay;
            }

            if (revisit < interval)
            {
                Refuse(LineOfJob("revisit_ticks"), null,
                    $"revisit_ticks = {revisit} is shorter than the interval of {interval} it would be "
                    + "delivered in, so one trigger would be asked to consider more Citizens than the "
                    + "city holds. A revisit period is a duration spread over triggers; shorten the "
                    + "interval or lengthen the period.");
                return (int)interval;
            }

            return (int)revisit;
        }

        /// <summary>How many workplaces one Citizen looks at before waiting for the next occasion.</summary>
        /// <remarks>
        /// <b><c>02 §5.3</c>'s N on the employment axis, and <c>adr/0017</c>'s satisficing rule is
        /// what makes it a behaviour model rather than a budget</b>: a person who sees three places
        /// with a vacancy and takes the first one near enough to walk to is not an optimiser being
        /// approximated. <b>Hash-bearing and unratified</b> — <c>0002</c> §D carries the row, and its
        /// ratifier is the first run that reports what fraction of looks find nothing, because a
        /// number that never fails to find work is a number doing no work.
        /// </remarks>
        private int ReadJobCandidates()
        {
            if (!TryInteger(_jobsTable!, "candidates", out long candidates, required: true))
            {
                return 1;
            }

            if (candidates < 1 || candidates > int.MaxValue)
            {
                Refuse(LineOfJob("candidates"), null,
                    $"candidates = {candidates} is out of range. It is how many workplaces a Citizen "
                    + "looks at on one occasion, so it is at least 1 -- a seeker that looks at none "
                    + "never finds work, and the pass would run at full cost employing nobody.");
                return 1;
            }

            return (int)candidates;
        }

        /// <summary>
        /// The band a Citizen's Shift length is drawn from, in whole in-world hours.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>This replaced <c>commute_peak_factor</c>, and it is a retirement rather than a rename</b>
        /// (<c>adr/0101</c>). That key authored a departure window and the engine derived the morning
        /// peak from it; under a Day with a shape the peak is a <em>reading</em> — what the profile
        /// comes out as, given where the jobs are and how long people work. ⚠ <b>A dial that states
        /// its own answer cannot be ratified by measuring the answer</b>, which is why that key's
        /// stated refuting number had to be re-derived twice before it could refute anything.
        /// </para>
        /// <para>
        /// <b>It is the evening peak's whole width.</b> A Workplace's staff share a start hour and so
        /// arrive together; they leave apart, spread over exactly this band. A narrow band gives a
        /// sharp evening and a wide one gives a flat afternoon, and <b>nothing in the corpus has
        /// measured a distribution of working hours</b> — so the draw is uniform and the band is
        /// authored, which is the arrangement that lets a measured profile say <em>narrower</em>
        /// rather than needing a shape invented at this write site.
        /// </para>
        /// <para>
        /// <b>Both bounds are required and the minimum carries a second job</b>: it is the gap between
        /// a Citizen's two departures, so it is what keeps somebody from still being in flight when
        /// their return falls due. That refusal is <see cref="RefuseShiftShorterThanTheBudget"/> and it
        /// replaces a guarantee that used to be free — under one journey a Day the gap was a whole Day.
        /// </para>
        /// </remarks>
        private (int Min, int Max) ReadShiftHours()
        {
            if (!TryInteger(_jobsTable!, "shift_hours_min", out long min, required: true)
                || !TryInteger(_jobsTable!, "shift_hours_max", out long max, required: true))
            {
                return (1, 1);
            }

            if (min < 1 || min > Ticks.HoursPerDay)
            {
                Refuse(LineOfJob("shift_hours_min"), null,
                    $"shift_hours_min = {min} is out of range. It is the shortest working day in this "
                    + $"city, in whole in-world hours, so it is at least 1 and at most "
                    + $"{Ticks.HoursPerDay}. A Shift of zero would put a Citizen's departure and their "
                    + "return on the same Tick of the Day.");
                return (1, 1);
            }

            if (max < min || max > Ticks.HoursPerDay)
            {
                Refuse(LineOfJob("shift_hours_max"), null,
                    $"shift_hours_max = {max} is out of range. It is the longest working day in this "
                    + $"city and must be at least shift_hours_min ({min}) and at most "
                    + $"{Ticks.HoursPerDay}. Equal bounds are allowed and mean a city where everybody "
                    + "works the same hours -- which makes the evening peak the morning one "
                    + "translated, and is a control rather than a mistake.");
                return (1, 1);
            }

            return ((int)min, (int)max);
        }

        /// <summary>
        /// How far ahead of their Shift a Citizen may aim to arrive, in whole in-world minutes.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The continuous term, and it was added because the first measured profile demanded
        /// one</b> (<c>adr/0101</c>). Start hours are on the hour, deliberately — workplaces do open
        /// on the hour — so with nothing sub-hour in the arithmetic the whole city departs on a
        /// handful of Ticks and the morning comes out a <em>plateau</em> of equal bars. The only
        /// other continuous quantity is the commute itself, and in a small city that is about four
        /// minutes against an hour of 85 Ticks.
        /// </para>
        /// <para>
        /// <b>Required, like every other key inside a present table.</b> Zero is a legitimate authored
        /// value — a city where everybody leaves exactly as late as they can — and is the control this
        /// can be measured against, so a default could not announce itself.
        /// </para>
        /// <para>
        /// <b>Capped at an hour</b>, because beyond that the margin exceeds the spacing between start
        /// hours and the anchor stops being an anchor: the profile would be indistinguishable from
        /// one drawn uniformly over the band, which is the texture this whole arrangement exists to
        /// keep.
        /// </para>
        /// </remarks>
        private int ReadArriveEarly()
        {
            if (!TryInteger(_jobsTable!, "arrive_early_max_minutes", out long minutes, required: true))
            {
                return 0;
            }

            if (minutes < 0 || minutes > 60)
            {
                Refuse(LineOfJob("arrive_early_max_minutes"), null,
                    $"arrive_early_max_minutes = {minutes} is out of range. It is how far ahead of "
                    + "their Shift a Citizen may aim to arrive, so it runs 0 (everybody cuts it fine) "
                    + "to 60. Beyond an hour the margin is wider than the gap between start hours, "
                    + "and a Workplace's opening time stops being an anchor at all.");
                return 0;
            }

            return (int)minutes;
        }

        /// <summary>
        /// Refuses a Ruleset whose shortest Shift is no longer than the Commute Budget's ceiling.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The overlap guard, and it exists because <c>adr/0101</c> spent the accident that used to
        /// supply it.</b> A Citizen leaves home, and their return is armed for <c>Shift length</c>
        /// later. If a journey may take longer than the Shift, a Citizen can still be walking to work
        /// when the roster says they leave it — which is not merely odd, it is a Trip whose origin the
        /// Traveller has not reached.
        /// </para>
        /// <para>
        /// ⚠ <b>Under one journey a Day this was arithmetically unreachable and nobody had to state
        /// it</b>: the gap between departures was 24 in-world hours and the Budget bounds a journey in
        /// minutes. <c>CommuteEngine</c> said so in its own remark, correctly, and the property was a
        /// consequence of there being one journey rather than of anything anybody had decided.
        /// <em>An invariant nothing enforces survives exactly as long as the structure that made it
        /// free.</em>
        /// </para>
        /// <para>
        /// <b>Strictly longer, not merely as long.</b> Equal would put the return's departure on the
        /// same Tick as the outbound arrival, which is a Citizen who works for no time at all.
        /// </para>
        /// </remarks>
        private void RefuseShiftShorterThanTheBudget(int shiftHoursMin)
        {
            if (!TryInteger(_tripsTable!, "commute_budget_minutes", out long budgetMinutes, false))
            {
                return;
            }

            int shiftMinutes = shiftHoursMin * 60;

            if (shiftMinutes > budgetMinutes)
            {
                return;
            }

            Refuse(LineOfJob("shift_hours_min"), null,
                $"shift_hours_min = {shiftHoursMin} is {shiftMinutes} minutes, and "
                + $"[trips] commute_budget_minutes is {budgetMinutes}. A Citizen's return journey is "
                + "armed one Shift after they leave home, so a Shift no longer than the longest "
                + "journey the Budget permits lets somebody leave work before they have got there. "
                + "Raise shift_hours_min above the Budget, or lower the Budget.");
        }

        /// <summary>The line a <c>[jobs]</c> key is on, or the table's.</summary>
        private int LineOfJob(string key) =>
            LineOf((SyntaxNodeBase?)Find(_jobsTable!, key) ?? _jobsTable!);

        /// <summary>A required <c>[roads]</c> integer, refused when it is outside its range.</summary>
        private int RoadNumber(string key, int minimum, int maximum, string range)
        {
            if (!TryInteger(_roadsTable!, key, out long value, required: true))
            {
                return minimum;
            }

            if (value < minimum || value > maximum)
            {
                Refuse(LineOfRoad(key), null, $"{key} = {value} is out of range. {range}");
                return minimum;
            }

            return (int)value;
        }

        /// <summary>
        /// A required <c>[roads]</c> speed, authored in km/h and stored as Q16.16 Tiles/Tick.
        /// </summary>
        /// <remarks>
        /// <b>Hash-bearing and unratified, every one of them.</b> 50 km/h for a Street, 90 for an
        /// Arterial and 5 for a walk are the figures S2 ran on, and <c>adr/0071</c> chose their
        /// <em>representation</em> while stating in terms that choosing one ratifies no value.
        /// <c>plans/0002</c> §D carries the rows.
        /// </remarks>
        private Speed RoadSpeed(string key)
        {
            if (!TryInteger(_roadsTable!, key, out long value, required: true))
            {
                return Speed.FromKilometresPerHour(1);
            }

            if (value < 1 || value > MaximumSpeedKph)
            {
                Refuse(LineOfRoad(key), null,
                    $"{key} = {value} is out of range. It is a free-flow speed in km/h, so it is at "
                    + $"least 1 — a road nobody can move along is a mode mask rather than a speed — "
                    + $"and at most {MaximumSpeedKph}, where Q16.16 Tiles per Tick runs out (adr/0071).");
                return Speed.FromKilometresPerHour(1);
            }

            return Speed.FromKilometresPerHour((int)value);
        }

        /// <summary>
        /// A required <c>[roads]</c> flow capacity, authored in Vehicles per hour and stored per Day.
        /// </summary>
        /// <remarks>
        /// <b>The conversion is exact and the unit is deliberate.</b> A Day is <c>CONTEXT.md</c>'s
        /// only time unit above the Tick, so <c>× 24</c> loses nothing — where Q16.16 Vehicles per
        /// Tick, which the spike used, would be a fourth quantity at a scale <c>adr/0071</c>
        /// enumerates exactly three for.
        /// </remarks>
        private int RoadCapacity(string key)
        {
            if (!TryInteger(_roadsTable!, key, out long value, required: true))
            {
                return HoursPerDay;
            }

            if (value < 1 || value > IntegerMath.FloorDiv(int.MaxValue, HoursPerDay))
            {
                Refuse(LineOfRoad(key), null,
                    $"{key} = {value} is out of range. It is a flow capacity in Vehicles per hour, so "
                    + "it is at least 1 — a capacity of zero is a division by zero wherever "
                    + "volume/capacity is evaluated, which is every Segment the volume-delay function "
                    + "will touch — and at most "
                    + $"{IntegerMath.FloorDiv(int.MaxValue, HoursPerDay)}, where the per-Day figure "
                    + "stops fitting.");
                return HoursPerDay;
            }

            return (int)value * HoursPerDay;
        }

        /// <summary>Hours in a Day. The exchange rate between an authored capacity and a stored one.</summary>
        private const int HoursPerDay = 24;

        /// <summary>
        /// The fastest speed Q16.16 Tiles per Tick can hold, in km/h. ~682, and no road is near it.
        /// </summary>
        private const int MaximumSpeedKph = 682;

        /// <summary>
        /// The longest crossing this loader accepts, in in-world seconds. An hour — beyond it the
        /// road being described is an Arterial, and the mechanism is Severance rather than a cost
        /// term.
        /// </summary>
        private const int MaximumCrossingSeconds = 3_600;

        /// <summary>
        /// The longest Commute Budget a travel time can hold, in in-world clock minutes. Just under
        /// four Days, and it is a format ceiling rather than a statement about commuting.
        /// </summary>
        private const int MaximumBudgetMinutes = 5_759;

        /// <summary>
        /// The largest peaking factor a Day can express. <b>A Day over this is 16 Ticks, which is
        /// under three in-world minutes</b> — shorter than the shortest walk in any measured band, so
        /// the window would no longer bound the in-flight count and the reciprocal derivation would
        /// stop holding. It is a limit of the arithmetic rather than a statement about cities.
        /// </summary>
        private const int MaximumCommutePeak = 512;

        /// <summary>The line a <c>[roads]</c> key is on, or the table's.</summary>
        private int LineOfRoad(string key) =>
            LineOf((SyntaxNodeBase?)Find(_roadsTable!, key) ?? _roadsTable!);

        /// <summary>
        /// <c>adr/0015</c>'s world-creation refusal, and it runs only on the reload entry point.
        /// </summary>
        /// <remarks>
        /// <b>Compared in Cells rather than in metres</b>, because Cells are the units the stored field
        /// is in and the membership test is stated in exactly those terms: <em>was existing simulation
        /// state recorded in units of the constant?</em> 1,024 m and 1,100 m are both 8 Cells, so
        /// refusing the second would refuse a reload that reinterprets nothing.
        /// </remarks>
        private void RefuseFrozenChange(LayerConstants stated)
        {
            if (frozen is not { } fixedConstants)
            {
                return;
            }

            Cells was = CellGrid.FromMetres(fixedConstants.IndustrialPollutionMetres);
            Cells now = CellGrid.FromMetres(stated.IndustrialPollutionMetres);

            if (was == now)
            {
                return;
            }

            Refuse(LineOf("kernel_metres"), null,
                $"kernel_metres = {stated.IndustrialPollutionMetres} is {now.Raw} Cells, and this "
                + $"world was created with {fixedConstants.IndustrialPollutionMetres} m — "
                + $"{was.Raw} Cells. adr/0015: the kernel radius is a world-creation constant, "
                + "because every Cell's stored pollution is a convolution through it, so changing it "
                + "would reinterpret the whole map rather than retune it. Refused rather than "
                + "applied; the Ruleset already in force stays live.");
        }

        /// <summary>The line a <c>[layers]</c> key is on, or the table's, or the file's first.</summary>
        private int LineOf(string key)
        {
            if (_layersTable is null)
            {
                return 1;
            }

            return Find(_layersTable, key) is { } entry ? LineOf(entry) : LineOf(_layersTable);
        }

        // ---- syntax helpers -------------------------------------------------------------------

        /// <summary>
        /// The key a reader asked a table for, or <c>null</c>. <b>Every key read in this file comes
        /// through here</b>, which is what <see cref="RefuseUnknownKeys"/> is built on.
        /// </summary>
        /// <remarks>
        /// <para>
        /// ⚠ <b>It records the consult whether or not the key is present, and that is the point.</b>
        /// A reader asking for an optional key it does not find has still told us the key is
        /// <em>permitted here</em> — so the permitted set is the set of things asked for, and it is
        /// derived from the code that does the reading rather than authored beside it.
        /// </para>
        /// <para>
        /// 🔴 <b>It stopped being <c>static</c> for that recording and for nothing else.</b>
        /// </para>
        /// </remarks>
        private KeyValueSyntax? Find(
            SyntaxNode holder, string key, RulesetKeyKind kind = RulesetKeyKind.Unknown)
        {
            // A holder is null when the section is absent altogether, and a reader still asks: it
            // wants the key so that it can fall back to the default. There is no table to permit
            // anything on, so there is nothing to record.
            if (holder is null)
            {
                return null;
            }

            if (!_consulted.TryGetValue(holder, out HashSet<string>? asked))
            {
                asked = new HashSet<string>(StringComparer.Ordinal);
                _consulted[holder] = asked;
            }

            asked.Add(key);

            // A key reached from a line-number helper records Unknown, and a later typed ask must
            // not be overwritten by it -- so the first reader with an expectation wins and the
            // untyped ones only ever add the name.
            if (kind != RulesetKeyKind.Unknown)
            {
                if (!_keyKinds.TryGetValue(holder, out Dictionary<string, RulesetKeyKind>? typed))
                {
                    typed = new Dictionary<string, RulesetKeyKind>(StringComparer.Ordinal);
                    _keyKinds[holder] = typed;
                }

                typed.TryAdd(key, kind);
            }

            switch (holder)
            {
                case TableSyntaxBase table:
                    foreach (KeyValueSyntax item in table.Items)
                    {
                        if (NameOf(item.Key) == key)
                        {
                            return item;
                        }
                    }

                    return null;

                case InlineTableSyntax inline:
                    foreach (InlineTableItemSyntax item in inline.Items)
                    {
                        if (item.KeyValue is { } pair && NameOf(pair.Key) == key)
                        {
                            return pair;
                        }
                    }

                    return null;

                default:
                    return null;
            }
        }

        private static string NameOf(KeySyntax? key) => key?.ToString().Trim() ?? string.Empty;

        private static int LineOf(SyntaxNodeBase node) => node.Span.Start.Line + 1;

        private string? NameOfRule(int index)
        {
            foreach (KeyValuePair<string, ushort> pair in _rules)
            {
                if (pair.Value == index + 1)
                {
                    return pair.Key;
                }
            }

            return null;
        }

        /// <summary>
        /// Refuses a key this build has retired, and records that it is not on offer.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>It records the retirement whether or not the key is present</b>, for
        /// <see cref="Find"/>'s reason exactly: the surface describes what the readers ask for
        /// rather than what a file contains, and no shipped Ruleset states any of these — so a
        /// retirement recorded only when the key appears would never be recorded at all.
        /// </para>
        /// <para>
        /// ⚠ <b>It consults through <see cref="Find"/> rather than around it</b>, so the name stays
        /// in the permitted set and <c>RefuseUnknownKeys</c> is untouched. The retirement is
        /// recorded <em>on top of</em> the consult and subtracted from the schema alone.
        /// </para>
        /// </remarks>
        private void RefuseRetired(SyntaxNode? holder, string key, string? name, string reason)
        {
            if (holder is null)
            {
                return;
            }

            if (!_retired.TryGetValue(holder, out HashSet<string>? gone))
            {
                gone = new HashSet<string>(StringComparer.Ordinal);
                _retired[holder] = gone;
            }

            gone.Add(key);

            if (Find(holder, key) is { } stated)
            {
                Refuse(LineOf(stated), name, reason);
            }
        }

        private void Refuse(int line, string? rule, string reason) =>
            _refusals.Add(new RulesetRefusal(fileName, line, rule, reason));
    }

    private static bool TryLayer(string name, out Layer layer)
    {
        switch (name)
        {
            case "pollution": layer = Layer.IndustrialPollution; return true;
            case "land-value": layer = Layer.LandValue; return true;
            case "sealing": layer = Layer.Sealing; return true;

            default:
                layer = default;
                return false;
        }
    }
}

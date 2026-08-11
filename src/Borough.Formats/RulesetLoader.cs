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

        private readonly Dictionary<string, ushort> _resources = new(StringComparer.Ordinal);

        // Parallel to _resources by declaration order, which is what makes ResourceId.Raw - 1 index
        // it. A Dictionary would be the obvious home and 05 section 4 lint 3 bans walking one.
        private readonly List<ResourceFamily> _families = [];
        private readonly Dictionary<string, byte> _kinds = new(StringComparer.Ordinal);
        private readonly Dictionary<string, ushort> _rules = new(StringComparer.Ordinal);
        private readonly Dictionary<string, ushort> _conditions = new(StringComparer.Ordinal);

        private readonly List<TableSyntaxBase> _kindTables = [];
        private readonly List<TableSyntaxBase> _ruleTables = [];
        private readonly List<TableSyntaxBase> _zoneRuleTables = [];

        private TableSyntaxBase? _layersTable;
        private TableSyntaxBase? _placementTable;

        private TableSyntaxBase? _roadsTable;

        public RulesetLoadResult Read()
        {
            DocumentSyntax document = SyntaxParser.Parse(text, fileName, validate: true);

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

            RuleDefinition[] rules = ReadRules(out Term[] inputs, out Term[] outputs,
                out MapEmission[] emissions);
            KindDefinition[] kinds = ReadKinds(rules, out BinDeclaration[] bins,
                out RuleId[] kindRules);
            ZoneRuleDefinition[] zoneRules = ReadZoneRules();
            LayerRuleset layers = ReadLayers();
            PlacementRuleset placement = ReadPlacement();
            RoadRuleset roads = ReadRoads();

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

            return _refusals.Count > 0
                ? RulesetLoadResult.Refused(_refusals)
                : RulesetLoadResult.Accepted(new Ruleset(
                    [.. _families], rules, kinds, inputs, outputs, emissions, bins, kindRules,
                    zoneRules)
                {
                    Layers = layers,
                    Placement = placement,
                    Roads = roads,
                    ResourceKeys = Keys(_resources),
                    KindKeys = Keys(_kinds),
                });
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

                    case "rule":
                        _ruleTables.Add(table);
                        Register(_rules, table, "rule", (ushort)(_rules.Count + 1));
                        break;

                    case "zone_rule":
                        // Not registered into a name table: nothing in a Ruleset ever refers to a
                        // Zone Rule, so it needs no id. The name is carried for refusal messages
                        // only, which is why a duplicate one is not an ambiguity here.
                        _zoneRuleTables.Add(table);
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

                    default:
                        Refuse(LineOf(table), null,
                            $"'{section}' is not a Ruleset section. The sections are "
                            + "[[resource]], [[building]], [[rule]], [[zone_rule]], [layers], "
                            + "[placement] and [roads].");
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

        private uint ReadRate(TableSyntaxBase table, string? rule)
        {
            if (!TryInteger(table, "rate", out long rate, required: true, rule))
            {
                return 1;
            }

            if (rate < 1 || rate >= Borough.Core.Rules.EventWheel.Size)
            {
                Refuse(LineOf((SyntaxNodeBase?)Find(table, "rate") ?? table), rule,
                    $"rate {rate} is outside 1..{Borough.Core.Rules.EventWheel.Size - 1}. A rate is a "
                    + "reschedule interval in Ticks, and one at or beyond WHEEL_SIZE would re-arm "
                    + "into the Event Wheel bucket it just came off.");
                return 1;
            }

            return (uint)rate;
        }

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
            if (Find(table, "storage") is not KeyValueSyntax entry)
            {
                return;
            }

            Refuse(LineOf(entry), "storage",
                "storage is not implemented. It is CONTEXT → Resource's second parameter — whether a "
                + "Bin carries over between periods, zero for Power and filling for Waste — and "
                + "nothing reads it yet, so accepting it would silently give every Resource the "
                + "carry-over of a warehouse. Remove the key; the hole is named rather than hidden.");
        }

        private ApplyCount ReadApply(TableSyntaxBase table, string? rule)
        {
            KeyValueSyntax? entry = Find(table, "apply");

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
            KeyValueSyntax? entry = Find(table, "fills");

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
            KeyValueSyntax? entry = Find(table, key);

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

                if (TryResource(inline, rule, LineOf(inline), out ResourceId resource))
                {
                    into.Add(new Term(new BinRef(scope, resource), (int)amount));
                }
            }
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

            emissions.Add(new MapEmission(layer, amount));
        }

        // ---- building kinds -------------------------------------------------------------------

        private KindDefinition[] ReadKinds(
            RuleDefinition[] rules, out BinDeclaration[] bins, out RuleId[] kindRules)
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

                // adr/0053's threshold, in missed firings. Optional, and absent means this kind never
                // declines — which is what every Ruleset written before decline existed already
                // meant. A negative one is refused rather than clamped: it reads as "condemn
                // immediately", and a Ruleset that demolished every Building it built would be a
                // sentence somebody meant to write and nobody would guess from the symptom.
                int condemnAfter = 0;

                if (TryInteger(table, "condemn_after", out long missed, required: false, name))
                {
                    if (missed < 0)
                    {
                        Refuse(LineOf((SyntaxNodeBase?)Find(table, "condemn_after") ?? table), name,
                            $"condemn_after is {missed}. It counts firings a starved Rule may miss "
                            + "before the Building is condemned, so it cannot be negative; omit it "
                            + "for a kind that never declines.");
                    }
                    else
                    {
                        condemnAfter = missed > int.MaxValue ? int.MaxValue : (int)missed;
                    }
                }

                // adr/0068's occupancy. Optional, and absent means this kind houses nobody -- which
                // is what almost every kind means and what every Ruleset written before occupancy
                // existed already meant. A negative one is refused rather than clamped, on
                // condemn_after's reasoning: it reads as "evict everybody", and a Ruleset that
                // emptied every Building it declared would be a sentence somebody meant to write and
                // nobody would guess from the symptom.
                int occupants = 0;

                if (TryInteger(table, "occupants", out long holds, required: false, name))
                {
                    if (holds < 0)
                    {
                        Refuse(LineOf((SyntaxNodeBase?)Find(table, "occupants") ?? table), name,
                            $"occupants is {holds}. It counts Households a Building of this kind may "
                            + "hold, so it cannot be negative; omit it for a kind that houses "
                            + "nobody.");
                    }
                    else
                    {
                        occupants = holds > int.MaxValue ? int.MaxValue : (int)holds;
                    }
                }

                definitions[i] = new KindDefinition(
                    binFirst, allBins.Count - binFirst, ruleFirst, allRules.Count - ruleFirst)
                {
                    CondemnAfter = condemnAfter,
                    Occupants = occupants,
                };
            }

            bins = [.. allBins];
            kindRules = [.. allRules];

            return definitions;
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

                definitions.Add(new ZoneRuleDefinition(kind, zone, interval, revisit));
            }

            return [.. definitions];
        }

        /// <summary>
        /// A Zone Rule's trigger interval, bounded like a Bin Rule's rate and for the same reason.
        /// </summary>
        /// <remarks>
        /// <b>Not <see cref="ReadRate"/>, because the key is spelled differently and that is
        /// deliberate.</b> <c>rate</c> is how often a Building's Rule re-arms; <c>interval</c> is how
        /// often the city sweeps. Sharing a word would invite the reading that a Zone Rule is armed
        /// per Lot, which is exactly the Bin Rule shape it is not.
        /// </remarks>
        private uint ReadInterval(TableSyntaxBase table, string? name)
        {
            if (!TryInteger(table, "interval", out long interval, required: true, name))
            {
                return 1;
            }

            if (interval < 1 || interval >= Borough.Core.Rules.EventWheel.Size)
            {
                Refuse(LineOf((SyntaxNodeBase?)Find(table, "interval") ?? table), name,
                    $"interval {interval} is outside 1..{Borough.Core.Rules.EventWheel.Size - 1}. An "
                    + "interval is a reschedule in Ticks, and one at or beyond WHEEL_SIZE would "
                    + "re-arm into the bucket it just came off.");
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
            if (Find(table, "sample") is { } retired)
            {
                Refuse(LineOf(retired), name,
                    "sample was replaced by revisit_ticks (adr/0059) and is no longer read. It was an "
                    + "absolute count of Lots per trigger, so the fraction of the city a Zone Rule "
                    + "covered per cycle shrank as the city grew -- at 1,000,000 Citizens a Lot was "
                    + $"visited once per 117 Days. Write revisit_ticks = {Ticks.PerDay} (one Day, the "
                    + "default) for how long the development industry takes to look at every Lot once.");
            }

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

        private void ReadBins(TableSyntaxBase table, string? kind, List<BinDeclaration> into)
        {
            KeyValueSyntax? entry = Find(table, "bins");

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
                            + "failing on headroom because the seller is rich.");
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

                into.Add(new BinDeclaration(resource, declared));
            }
        }

        // ---- the refusals ---------------------------------------------------------------

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
                    + "quoted string -- decline_rate = \"0.15\" -- so that no floating-point value "
                    + "ever exists on the path into the simulation. adr/0048.");
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
                long drawn = Money(inputs, rule.InputFirst, rule.InputCount);
                long returned = Money(outputs, rule.OutputFirst, rule.OutputCount);

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

        private long Money(Term[] terms, int first, int count)
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
            KeyValueSyntax? entry = Find(holder, key);

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

        private bool TryInteger(
            SyntaxNode holder, string key, out long value, bool required, string? rule = null)
        {
            value = 0;
            KeyValueSyntax? entry = Find(holder, key);

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

            int pollutionPeriod = Cadence("pollution", schedule.IndustrialPollution,
                out int pollutionOffset);
            int landValuePeriod = Cadence("land_value", schedule.LandValue, out int landValueOffset);

            int metres = Number("kernel_metres", constants.IndustrialPollutionMetres, minimum: 1,
                "A kernel with no reach is not a diffused field.");
            int decayTicks = Number("pollution_decay_ticks", LayerRates.DefaultPollutionDecayTicks,
                minimum: 0, "It is a duration in Ticks, and 0 means the plume never fades.");
            int landValueTau = Number("land_value_tau", rates.LandValueTau, minimum: 1,
                "A time constant divides, so 0 is not one; land value with no momentum is a period "
                + "of 1 rather than a tau of 0.");
            int sealingTau = Number("sealing_decay_tau", rates.SealingDecayTau, minimum: 0,
                "It counts scheduled updates, and 0 means Sealing never decays — Phase 1's value, "
                + "because there is no terrain to key a rate off yet.");

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
                    new LayerCadence(landValuePeriod, landValueOffset)),
                LayerRates.From(landValueTau, sealingTau, decayTicks, pollutionPeriod),
                stated);
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
        private PlacementRuleset ReadPlacement()
        {
            if (_placementTable is null)
            {
                return PlacementRuleset.None;
            }

            uint interval = ReadInterval(_placementTable, null);
            int revisit = ReadPlacementRevisit(interval);
            int candidates = ReadCandidates();

            return new PlacementRuleset(interval, revisit, candidates);
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

        private static KeyValueSyntax? Find(SyntaxNode holder, string key)
        {
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

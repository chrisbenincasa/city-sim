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
    /// <summary>Loads the Ruleset at <paramref name="path"/>.</summary>
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

        return new Reader(text, fileName).Read();
    }

    /// <summary>
    /// One load. Everything is accumulated and nothing is applied, so a refusal costs nothing.
    /// </summary>
    private sealed class Reader(string text, string fileName)
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
                    [.. _families], rules, kinds, inputs, outputs, emissions, bins, kindRules));
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

                    default:
                        Refuse(LineOf(table), null,
                            $"'{section}' is not a Ruleset section. The sections are "
                            + "[[resource]], [[building]] and [[rule]].");
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

                definitions[i] = new KindDefinition(
                    binFirst, allBins.Count - binFirst, ruleFirst, allRules.Count - ruleFirst);
            }

            bins = [.. allBins];
            kindRules = [.. allRules];

            return definitions;
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

                    if (capacity < 1 || capacity > int.MaxValue)
                    {
                        Refuse(LineOf(inline), kind,
                            $"capacity {capacity} is not a positive quantity. A Bin of capacity zero "
                            + "can never be deposited into, which is a Bin the kind should not "
                            + "declare.");
                        continue;
                    }

                    declared = BinCapacity.Of((int)capacity);
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

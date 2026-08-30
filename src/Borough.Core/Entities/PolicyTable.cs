namespace Borough.Core.Entities;

using Borough.Core.Rules;
using Borough.Core.Tables;

/// <summary>
/// What the player has done to each declared Policy: one row per <c>[[policy]]</c>, in declaration
/// order.
/// </summary>
/// <remarks>
/// <para>
/// <b>It exists because <c>Govern</c> is the first verb whose effect outlives the command.</b>
/// <c>01 §2</c>'s verb is <em>set a parameter on a Rule the city then obeys</em>, and the Rule is
/// Ruleset data — hot-reloadable, replaced wholesale on a reload (<c>adr/0015</c>). A governed amount
/// written back into the Ruleset would be undone by the next reload and would never reach a save.
/// ***What the player decided is state, and the Ruleset is what they decided it against.***
/// </para>
/// <para>
/// ⚠ <b><see cref="Amount"/> means nothing unless <see cref="Governed"/> is 1.</b> An ungoverned row
/// is not <em>zero</em> and is not a copy of the Ruleset's figure — it is the absence of a decision,
/// and <c>PolicyEngine</c> falls through to <c>PolicyDefinition.Amount</c>. Seeding this column from
/// the Ruleset would put a second, staler spelling of the designer's number in the save, which is the
/// duplication <see cref="TreasuryTable"/> declines for its balance.
/// </para>
/// <para>
/// <b>The flag is what makes a hot reload still work.</b> Retuning a tax rate in the TOML is
/// <c>adr/0015</c>'s own acceptance test, and <c>RulesetShape</c> lets a Policy's amount move freely
/// for exactly that reason. An ungoverned Policy therefore follows the designer; a governed one keeps
/// the player's figure. ***Without the flag the two are indistinguishable and one of them has to lose
/// silently.***
/// </para>
/// <para>
/// ⚠ <b><see cref="Key"/> is saved, and it is the row's identity rather than its position.</b> A
/// Policy is addressed by the content hash of its declared name (<c>Ruleset.PolicyKeys</c>); inserting
/// a <c>[[policy]]</c> above another shifts every index below it, and <c>RulesetShape</c> reports that
/// as <c>RulesetChange.PolicyCount</c> with nothing acting on it. <b>A governed row that remembered
/// only its index would re-attach to a different Policy on the next reload</b>, which is the defect
/// this column exists to make impossible.
/// </para>
/// <para>
/// ⚠ <b>Zero is a real key and means <em>unnameable</em></b> — a Policy whose table states no
/// <c>name</c>, or a Ruleset built in code. Such a row can never be matched across a reload and can
/// never be governed, and <c>Simulation</c> refuses the verb against it by name.
/// </para>
/// </remarks>
[Table]
public sealed class PolicyTable
{
    private readonly Rows<Policy> _rows;

    /// <summary>Builds the table and allocates one row per declared Policy.</summary>
    /// <remarks>
    /// <b>Rows are allocated in the constructor, on <see cref="TreasuryTable"/>'s precedent.</b> The
    /// set of Policies is fixed for as long as one Ruleset is in force, so slot <c>i</c> means the same
    /// thing for the life of that Ruleset and nothing reading this table needs a liveness branch.
    /// ⚠ <b>A reload may change the set</b>, which is <c>World.Adopt</c>'s business and not this
    /// type's.
    /// </remarks>
    /// <param name="rules">The Ruleset in force, which decides how many rows there are.</param>
    public PolicyTable(Ruleset rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        int policies = rules.Policies.Length;

        _rows = new Rows<Policy>("policy", policies, Buffering.OneCopy);

        Key = _rows.Saved<ulong>("key", Touch.Cold);
        Amount = _rows.Saved<int>("amount", Touch.Cold);
        Governed = _rows.Saved<byte>("governed", Touch.Cold);

        _rows.Seal();

        for (int policy = 0; policy < policies; policy++)
        {
            // Slot equals declaration index by construction: the allocator starts empty, this is the
            // only caller, and nothing here ever frees. Going through Allocate rather than writing the
            // columns directly is ClockTable's reason -- the row count and the columns have to agree.
            _rows.Allocate();

            Key[policy] = rules.PolicyKey(policy);
        }
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<Policy> Rows => _rows;

    /// <summary>Which Policy this row is, comparable across Rulesets. Zero means unnameable.</summary>
    public Column<ulong> Key { get; }

    /// <summary>The amount the player has set. Meaningless unless <see cref="Governed"/> is 1.</summary>
    public Column<int> Amount { get; }

    /// <summary>Whether the player has governed this Policy at all.</summary>
    public Column<byte> Governed { get; }

    /// <summary>What one application of this Policy moves, the player's decision winning.</summary>
    /// <remarks>
    /// <b>The one place the fall-through is spelled</b>, so a caller cannot read
    /// <see cref="Amount"/> without asking <see cref="Governed"/> first.
    /// </remarks>
    public int AmountOf(int policy, in PolicyDefinition definition) =>
        policy >= 0 && policy < _rows.SlotCount && Governed[policy] != 0
            ? Amount[policy]
            : definition.Amount;

    /// <summary>Records the player's decision against one Policy.</summary>
    public void Govern(int policy, int amount)
    {
        Amount[policy] = amount;
        Governed[policy] = 1;
    }

    /// <summary>Re-points every row at a newly adopted Ruleset, carrying governed amounts by name.</summary>
    /// <remarks>
    /// <para>
    /// <b>The row count never changes and that is the decision.</b> The allocator grows on demand and
    /// recycles freed slots through a free list, so resizing here would break the one invariant this
    /// table rests on — <em>slot equals declaration index</em> — for a reload that changes how many
    /// <c>[[policy]]</c> tables a file declares. ***Rows beyond the new Ruleset's set key to zero***,
    /// which is not a placeholder but the truth: they name no Policy that exists.
    /// </para>
    /// <para>
    /// ⚠ <b>A governed Policy whose name is gone loses its amount, silently and on purpose.</b> The
    /// alternative is holding a decision against a Policy the designer deleted, and re-attaching it by
    /// position is the defect <see cref="Key"/> exists to prevent. <b>Renaming a <c>[[policy]]</c> is
    /// therefore a destructive edit to a governed world</b>, which is the same standing that a renamed
    /// Resource already has.
    /// </para>
    /// <para>
    /// <b>Reordering, by contrast, is carried exactly.</b> A governed amount follows its name to
    /// whatever index that name now sits at, which is the whole reason the column holds a key rather
    /// than an index.
    /// </para>
    /// </remarks>
    /// <param name="rules">The Ruleset now in force.</param>
    public void Adopt(Ruleset rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        int slots = _rows.SlotCount;

        // Cold path: a reload runs on a keystroke, not in a Tick's steady state, and RulesetMigration
        // allocates on the same path for the same reason.
        var wasKey = new ulong[slots];
        var wasAmount = new int[slots];
        var wasGoverned = new byte[slots];

        for (int policy = 0; policy < slots; policy++)
        {
            wasKey[policy] = Key[policy];
            wasAmount[policy] = Amount[policy];
            wasGoverned[policy] = Governed[policy];

            Key[policy] = policy < rules.Policies.Length ? rules.PolicyKey(policy) : 0;
            Amount[policy] = 0;
            Governed[policy] = 0;
        }

        for (int policy = 0; policy < slots; policy++)
        {
            if (wasGoverned[policy] == 0)
            {
                continue;
            }

            int now = Find(wasKey[policy]);

            if (now != Tables.Rows.NoSlot)
            {
                Govern(now, wasAmount[policy]);
            }
        }
    }

    /// <summary>Which row carries this key, or <see cref="Tables.Rows.NoSlot"/>.</summary>
    /// <remarks>
    /// ⚠ <b>Zero never matches.</b> An unnameable Policy is not the same Policy as another unnameable
    /// one, and treating them as equal is how a governed amount would cross between two sweeps that
    /// share nothing but the absence of a name.
    /// </remarks>
    public int Find(ulong key)
    {
        if (key == 0)
        {
            return Tables.Rows.NoSlot;
        }

        for (int policy = 0; policy < _rows.SlotCount; policy++)
        {
            if (Key[policy] == key)
            {
                return policy;
            }
        }

        return Tables.Rows.NoSlot;
    }
}

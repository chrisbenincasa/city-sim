namespace Borough.Core.Rules;

using Borough.Core.Tables;

/// <summary>
/// What one reload cost the city, as counts. <b>Ids and numbers; the shell writes the sentence.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b><c>02 §4.3</c> and <c>adr/0015</c> both say the degradations happen <em>with a logged
/// warning</em>, and <c>adr/0002</c> forbids the core from producing one.</b> So the core produces
/// what a warning is made of — how many Buildings lost their kind, how many Bins named a Resource
/// that went, how many Rule Instances were re-armed — and the shell resolves those into the names a
/// designer reads.
/// </para>
/// <para>
/// <b>A return value rather than state, today.</b> <c>05 §7</c> promotes this to a saved provenance
/// trail with a cap on the last <c>N</c> transitions, which is task 7 and a hash-bearing number under
/// <c>adr/0052</c>. Reporting the counts first is what lets that arrive as a collection with a sink
/// already argued rather than as a collection somebody caps afterwards.
/// </para>
/// </remarks>
/// <param name="BuildingsDerelicted">
/// Buildings whose kind stopped being declared. They stand, with their Bins and their occupants, and
/// run nothing (<c>02 §4.3</c>: <em>derelict rather than deleted</em>).
/// </param>
/// <param name="BinsDropped">Bins whose Resource is not declared by the incoming Ruleset.</param>
/// <param name="RuleInstancesRearmed">
/// Rule Instances created by the refit — which is every one the city runs afterwards, because the
/// migration drops all of them first.
/// </param>
public readonly record struct RulesetDegradation(
    int BuildingsDerelicted, int BinsDropped, int RuleInstancesRearmed)
{
    /// <summary>Whether this reload cost the city nothing at all.</summary>
    public bool IsNothing =>
        BuildingsDerelicted == 0 && BinsDropped == 0 && RuleInstancesRearmed == 0;
}

/// <summary>
/// Which declaration in the incoming Ruleset is the one a live row is already pointing at.
/// </summary>
/// <remarks>
/// <para>
/// <b>The map exists because an id is not an identity.</b> A <see cref="ResourceId"/> and a Building
/// kind are declaration order, so deleting one <c>[[resource]]</c> shifts every id below it — and the
/// rows that hold those ids are <see cref="BinTable.Resource"/> and
/// <see cref="Entities.BuildingTable.Kind"/>, neither of which knows the file moved. Sub-task A gave
/// every declaration a key hashed from its name; this is what that key was for.
/// </para>
/// <para>
/// <b>It is computed at the swap, when both Rulesets are in hand, and no row grows a column.</b> The
/// alternative — storing the key on the Bin and on the Building — pays four bytes a row for ever to
/// avoid one walk on a designer's keystroke, and it would make every save carry a second spelling of
/// the same fact.
/// </para>
/// <para>
/// <b>Zero means gone</b>, for both maps, which is the value <see cref="Ruleset.Declares"/> already
/// answers <see langword="false"/> for and the value a Building carries when the Ruleset cannot
/// describe it. There is no third state: a declaration is in the new file under some id, or it is
/// not, and <em>renamed</em> is not expressible because a name is the only identity a TOML file
/// carries.
/// </para>
/// <para>
/// <b>A linear scan per declaration, deliberately.</b> A Ruleset holds tens of Resources and kinds,
/// this runs once per reload on the cold path, and the obvious alternative is a hash map — which
/// <c>BOR0301</c> permits building and looking up in but not walking, and which would buy nothing
/// measurable here while adding a container to a path that must stay obviously deterministic.
/// </para>
/// </remarks>
public sealed class RulesetMigration
{
    private readonly ResourceId[] _resources;
    private readonly byte[] _kinds;

    private RulesetMigration(ResourceId[] resources, byte[] kinds, ResourceId familyChanged)
    {
        _resources = resources;
        _kinds = kinds;
        FamilyChanged = familyChanged;
    }

    /// <summary>
    /// The first surviving Resource whose family moved, or zero. <b>The one residual refusal.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A Good becoming Money with live Bins holding stock is <c>adr/0024</c> conservation, not a
    /// degradation.</b> Conservation is a property the whole Rule engine enforces — the loader refuses
    /// a Rule whose money terms do not balance — so a Resource that changes family retroactively makes
    /// every unit already in a Bin either created or destroyed by an edit. There is no honest
    /// degradation for that, and inventing one would be inventing an economy.
    /// </para>
    /// <para>
    /// <b>It is computed here rather than read off <see cref="RulesetShape.Compare"/>, and that is a
    /// correction rather than a preference.</b> <c>Compare</c> returns the <em>first</em> way two
    /// Rulesets differ, so a file that adds a Resource <em>and</em> refamilies another reports
    /// <see cref="RulesetChange.ResourceCount"/> and never reaches the family check. That was harmless
    /// while every structural difference was refused and is a silent hole the moment they are not.
    /// </para>
    /// </remarks>
    public ResourceId FamilyChanged { get; }

    /// <summary>Builds the map from the Ruleset in force to the one replacing it.</summary>
    public static RulesetMigration Between(Ruleset current, Ruleset replacement)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(replacement);

        var resources = new ResourceId[current.ResourceCount];
        ResourceId familyChanged = default;

        for (int i = 1; i <= current.ResourceCount; i++)
        {
            var was = new ResourceId((ushort)i);
            ResourceId now = default;
            ulong key = current.ResourceKey(was);

            for (int j = 1; j <= replacement.ResourceCount; j++)
            {
                var candidate = new ResourceId((ushort)j);

                if (replacement.ResourceKey(candidate) == key)
                {
                    now = candidate;
                    break;
                }
            }

            resources[i - 1] = now;

            if (now.Raw != 0
                && familyChanged.Raw == 0
                && current.Family(was) != replacement.Family(now))
            {
                familyChanged = was;
            }
        }

        var kinds = new byte[current.KindCount];

        for (int i = 1; i <= current.KindCount; i++)
        {
            ulong key = current.KindKey((byte)i);

            for (int j = 1; j <= replacement.KindCount; j++)
            {
                if (replacement.KindKey((byte)j) == key)
                {
                    kinds[i - 1] = (byte)j;
                    break;
                }
            }
        }

        return new RulesetMigration(resources, kinds, familyChanged);
    }

    /// <summary>
    /// What a live Bin's Resource id becomes, or zero when that Resource is not declared any more.
    /// </summary>
    /// <remarks>
    /// <b>An id past the outgoing Ruleset's count is <em>gone</em> rather than a throw</b>, and it is
    /// reachable: a world running on <see cref="Ruleset.Empty"/> can hold Bins created directly, which
    /// is what most of this project's tests and every figure taken before slice 7 look like. Such a
    /// Bin names nothing under either Ruleset, so the answer the migration gives it is the same answer
    /// a declaration that was deleted gets.
    /// </remarks>
    public ResourceId Resource(ResourceId was) =>
        was.Raw == 0 || was.Raw > _resources.Length ? default : _resources[was.Raw - 1];

    /// <summary>
    /// What a live Building's kind becomes, or zero — which is dereliction.
    /// </summary>
    /// <inheritdoc cref="Resource" path="/remarks"/>
    public byte Kind(byte was) =>
        was == 0 || was > _kinds.Length ? (byte)0 : _kinds[was - 1];
}

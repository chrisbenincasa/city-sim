using Borough.Core.Tables;

namespace Borough.Core.Rules;

/// <summary>
/// The first way two Rulesets differ structurally, or <see cref="None"/> when they differ only in
/// their numbers.
/// </summary>
/// <remarks>
/// <b>A typed answer rather than a sentence, because <c>Core</c> returns ids and numbers</b>
/// (<c>adr/0002</c>). The shell turns this into something a designer reads; the leak vector that ADR
/// actually names is a method returning a formatted string because a panel wanted one, and a
/// diagnostic is exactly where that starts.
/// </remarks>
public enum RulesetChange
{
    /// <summary>The two declare the same structure. Only numbers moved.</summary>
    None = 0,

    /// <summary>A Resource was added or removed.</summary>
    ResourceCount,

    /// <summary>
    /// A Resource id names a different declaration than it did. The file was reordered.
    /// </summary>
    /// <remarks>
    /// <b>The failure this catches is silent and total.</b> Ids are declaration order, so swapping two
    /// <c>[[resource]]</c> blocks leaves every count and every shape identical while every live Bin
    /// row starts naming the other Good. Without a key per declaration the comparison below cannot
    /// see it, and the reload reads as <em>numbers only</em>.
    /// </remarks>
    ResourceIdentity,

    /// <summary>A Resource changed family — which changes whether it is conserved (<c>adr/0024</c>).</summary>
    ResourceFamily,

    /// <summary>A Building kind was added or removed.</summary>
    KindCount,

    /// <summary>
    /// A kind id names a different declaration than it did. The file was reordered.
    /// </summary>
    /// <remarks>
    /// <inheritdoc cref="ResourceIdentity" path="/remarks"/>
    /// The Building's version of the same failure: every Building of kind 2 becomes a Building of
    /// whatever kind 2 now is, keeping its Bins and its Rule Instances, and nothing in the world
    /// records that it changed species.
    /// </remarks>
    KindIdentity,

    /// <summary>A kind's Bins changed: a different Resource, or a different number of them.</summary>
    KindBins,

    /// <summary>A kind's Rules changed: a different Rule, or a different number of them.</summary>
    KindRules,

    /// <summary>A Rule was added or removed.</summary>
    RuleCount,

    /// <summary>A Rule's structure changed: its kind, its chain, its terms' Resources or Layers.</summary>
    RuleShape,

    /// <summary>A Zone Rule was added or removed.</summary>
    ZoneRuleCount,

    /// <summary>A Zone Rule's kind or permission bit changed.</summary>
    ZoneRuleShape,

    /// <summary>A Policy was added or removed.</summary>
    /// <remarks>
    /// <b>On <see cref="ZoneRuleCount"/>'s precedent rather than on this file's stated test</b>, and
    /// the divergence is worth knowing. The test is <em>what does live state point at</em>, and
    /// nothing points at a Policy: no saved row holds a Policy id, and a Policy allocates nothing, so
    /// adding one needs no degradation. What puts it here is the same thing that puts a Zone Rule
    /// here — a Policy's identity is its position in declaration order, and that index is a
    /// coordinate of the scan-start draw, so inserting one silently repoints every other Policy's
    /// rotation.
    /// </remarks>
    PolicyCount,

    /// <summary>A Policy's population, its transfer's direction, or the Resource it moves changed.</summary>
    PolicyShape,
}

/// <summary>
/// Whether one Ruleset can replace another without anything in the world having to degrade.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the line between a reload that is a tuning change and one that is a migration</b>, and
/// slice 8 task 1 builds only the first. `adr/0015`'s acceptance test is one sentence — *changing a
/// production ratio and seeing the effect must take seconds* — and a production ratio is a **number**.
/// Two Rulesets that declare the same Resources, kinds, Bins, Rules and Zone Rules, and differ only in
/// rates, capacities, quantities, apply bands, thresholds and intervals, can be swapped with nothing
/// in the world touched: every live Bin still names a Resource that exists, every armed Rule Instance
/// still names a Rule with the same id, and the next firing simply uses the new numbers.
/// </para>
/// <para>
/// <b>Everything else needs the degradations, and they are tasks 4–6 rather than an oversight.</b> A
/// kind that stops being declared makes its Buildings **derelict rather than deleted**; a Bin whose
/// Resource is gone is dropped; wait lists are dropped and every Rule re-armed on slice 7's stagger.
/// Until those exist a structural reload is <b>refused</b>, which is <c>adr/0015</c>'s own polarity:
/// its revisit trigger names *silently ignoring* as the failure mode, so a swap this build cannot
/// perform correctly must say so rather than half-happen.
/// </para>
/// <para>
/// <b>What counts as structure is decided by what live state points at.</b> A Bin row holds a
/// Resource id; a Rule Instance holds a Rule id; a Building holds a kind. Anything a row can point at
/// is structure, and anything only ever read *through* one — a rate, a capacity, a quantity — is a
/// number. That test is why the apply band and the Bin capacity are on the safe side and the Bin's
/// Resource is not, and it is the test to re-run when the Ruleset grows a field.
/// </para>
/// <para>
/// <b>And an id is not an identity, which this comparison assumed for two tasks.</b> Ids are
/// declaration order, so a designer who reorders a file changes every id below the edit while every
/// count and every shape stays identical — a reload this would have called <em>numbers only</em>
/// while every live Bin started naming a different Good. <see cref="Ruleset.ResourceKeys"/> is what
/// closes it: a key per declaration, hashed from its name by the loader, compared before anything
/// else about that id is compared. **A comparison of two declarations that are not the same
/// declaration produces true sentences about the wrong pair**, which is why the key check comes
/// first in both loops rather than alongside.
/// </para>
/// </remarks>
public static class RulesetShape
{
    /// <summary>
    /// How <paramref name="replacement"/> differs structurally from <paramref name="current"/>.
    /// </summary>
    /// <returns><see cref="RulesetChange.None"/> when only numbers moved.</returns>
    public static RulesetChange Compare(Ruleset current, Ruleset replacement)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(replacement);

        if (current.ResourceCount != replacement.ResourceCount)
        {
            return RulesetChange.ResourceCount;
        }

        for (int i = 1; i <= current.ResourceCount; i++)
        {
            var resource = new ResourceId((ushort)i);

            // Identity before family, because a mismatched key makes every comparison below it a
            // comparison of two unrelated declarations -- and "the family changed" would be a true
            // sentence about the wrong pair.
            if (current.ResourceKey(resource) != replacement.ResourceKey(resource))
            {
                return RulesetChange.ResourceIdentity;
            }

            // A Good becoming Money is not a tuning change: adr/0024 makes conservation a property
            // the whole Rule engine enforces, so the same Bin would start refusing what it accepted.
            if (current.Family(resource) != replacement.Family(resource))
            {
                return RulesetChange.ResourceFamily;
            }
        }

        if (current.RuleCount != replacement.RuleCount)
        {
            return RulesetChange.RuleCount;
        }

        for (int i = 1; i <= current.RuleCount; i++)
        {
            RulesetChange change = CompareRule(current, replacement, new RuleId((ushort)i));

            if (change != RulesetChange.None)
            {
                return change;
            }
        }

        if (current.KindCount != replacement.KindCount)
        {
            return RulesetChange.KindCount;
        }

        for (int i = 1; i <= current.KindCount; i++)
        {
            RulesetChange change = CompareKind(current, replacement, (byte)i);

            if (change != RulesetChange.None)
            {
                return change;
            }
        }

        if (current.ZoneRules.Length != replacement.ZoneRules.Length)
        {
            return RulesetChange.ZoneRuleCount;
        }

        for (int i = 0; i < current.ZoneRules.Length; i++)
        {
            ZoneRuleDefinition was = current.ZoneRules[i];
            ZoneRuleDefinition now = replacement.ZoneRules[i];

            // Interval and revisit period are pacing and may move freely; the kind it raises and the
            // bit it admits decide *which city* it builds, and nothing in the world points at them.
            // They are here because a Zone Rule's identity is its position in declaration order
            // (02 §4.2's tie-break), so silently repurposing entry 3 would move the tie-break too.
            if (was.Kind != now.Kind || was.Zone != now.Zone)
            {
                return RulesetChange.ZoneRuleShape;
            }
        }

        if (current.Policies.Length != replacement.Policies.Length)
        {
            return RulesetChange.PolicyCount;
        }

        for (int i = 0; i < current.Policies.Length; i++)
        {
            PolicyDefinition was = current.Policies[i];
            PolicyDefinition now = replacement.Policies[i];

            // The RATE is a number and moves freely, which is the point: a tax rate is exactly what
            // adr/0015's acceptance test is about, and it is the apply count. What is structure here
            // is the Resource -- a live Bin holds a Resource id, so a Policy repointed at a different
            // one moves money into Bins that hold something else -- and the two scopes, because
            // local->global and global->local are opposite cities and neither is a tuning of the
            // other. The subject is structure for the same reason a Rule's kind is.
            if (was.Subject != now.Subject
                || was.From != now.From
                || was.To != now.To
                || current.ResourceKey(was.Resource) != replacement.ResourceKey(now.Resource))
            {
                return RulesetChange.PolicyShape;
            }
        }

        return RulesetChange.None;
    }

    private static RulesetChange CompareRule(Ruleset current, Ruleset replacement, RuleId id)
    {
        RuleDefinition was = current.Rule(id);
        RuleDefinition now = replacement.Rule(id);

        // Rate and Apply are absent on purpose: they are the numbers adr/0015's acceptance test is
        // about. Everything here is either an id a row can hold or a shape the engine branches on.
        if (was.Kind != now.Kind
            || was.OnFail != now.OnFail
            || was.Reports != now.Reports
            || was.HasFills != now.HasFills
            || (was.HasFills && was.Fills != now.Fills))
        {
            return RulesetChange.RuleShape;
        }

        return SameResources(current.Inputs(id), replacement.Inputs(id))
            && SameResources(current.Outputs(id), replacement.Outputs(id))
            && SameLayers(current.Emissions(id), replacement.Emissions(id))
                ? RulesetChange.None
                : RulesetChange.RuleShape;
    }

    private static RulesetChange CompareKind(Ruleset current, Ruleset replacement, byte kind)
    {
        if (current.KindKey(kind) != replacement.KindKey(kind))
        {
            return RulesetChange.KindIdentity;
        }

        ReadOnlySpan<BinDeclaration> wasBins = current.BinsOf(kind);
        ReadOnlySpan<BinDeclaration> nowBins = replacement.BinsOf(kind);

        if (wasBins.Length != nowBins.Length)
        {
            return RulesetChange.KindBins;
        }

        for (int i = 0; i < wasBins.Length; i++)
        {
            // The Resource is structure — a live Bin row holds it. The capacity is a number, and it
            // stayed one when adr/0064 made it derived: no row points at it, the swap rebuilds it,
            // and a Bin left over its new ceiling drains rather than being a reason to refuse.
            if (wasBins[i].Resource != nowBins[i].Resource)
            {
                return RulesetChange.KindBins;
            }
        }

        ReadOnlySpan<RuleId> wasRules = current.RulesOf(kind);
        ReadOnlySpan<RuleId> nowRules = replacement.RulesOf(kind);

        if (wasRules.Length != nowRules.Length)
        {
            return RulesetChange.KindRules;
        }

        for (int i = 0; i < wasRules.Length; i++)
        {
            if (wasRules[i] != nowRules[i])
            {
                return RulesetChange.KindRules;
            }
        }

        return RulesetChange.None;
    }

    /// <summary>The Bins the terms name, ignoring the quantities, which are tuning.</summary>
    private static bool SameResources(ReadOnlySpan<Term> was, ReadOnlySpan<Term> now)
    {
        if (was.Length != now.Length)
        {
            return false;
        }

        for (int i = 0; i < was.Length; i++)
        {
            if (was[i].Bin != now[i].Bin)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>The Layers the emissions write to, ignoring the amounts.</summary>
    private static bool SameLayers(ReadOnlySpan<MapEmission> was, ReadOnlySpan<MapEmission> now)
    {
        if (was.Length != now.Length)
        {
            return false;
        }

        for (int i = 0; i < was.Length; i++)
        {
            if (was[i].Layer != now[i].Layer)
            {
                return false;
            }
        }

        return true;
    }
}

namespace Borough.Headless;

using Borough.Core.Entities;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

/// <summary>
/// How far the Day's admissions are from what a per-school rule would have decided.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>THE NUMBER THIS EXISTS TO PRODUCE IS A GAP IN A REPAIR, AND THE POINT IS THAT NOBODY KNEW
/// ITS SIZE.</b> <c>ServiceEngine</c> admits nearest-first over <em>occasions</em>: each family is
/// keyed by the distance to <b>its own</b> nearest service Building, and the queue is served in that
/// order. The stronger rule — <em>a school's places go to the applicants nearest THAT school</em> —
/// is deferred acceptance, and the two come apart exactly when a family whose nearest school is full
/// is admitted at a further one ahead of somebody who lives closer to the further one.
/// </para>
/// <para>
/// <b>An INVERSION is that case, counted.</b> A family turned away at a full door, which could have
/// reached some school inside the Commute Budget, and whose walk to that school is <em>shorter</em>
/// than the longest walk among the families that school actually admitted. ***Zero inversions means
/// the cheap ordering and the expensive one agree on this world***, and the expensive one is a
/// mechanism nobody has to build.
/// </para>
/// <para>
/// ⚠ <b>IT IS A LOWER BOUND ON THE DIFFERENCE AND NOT THE DIFFERENCE.</b> Under deferred acceptance
/// a displaced family proposes onward and can displace a third, so one inversion here may stand for
/// a longer chain. What it is exact about is the direction: ***a world with no inversions has no
/// chain to start.*** A count is a summons to build the mechanism, not a measurement of what
/// building it would move.
/// </para>
/// <para>
/// ⚠ <b>It reads <c>ServiceEngine.LastHouseholds</c>, which is the LAST DAY's pass and not the
/// run's.</b> A Day is the unit an admission happens in, so a run-long total would need the engine
/// to keep a history it has no other use for. <c>plans/0054</c> <b>F6</b> owns the finding.
/// </para>
/// </remarks>
internal static class ServiceAdmission
{
    /// <summary>What one Day's admissions cost the families that lost them.</summary>
    /// <param name="Admitted">Families that got a place.</param>
    /// <param name="TurnedAway">Families that reached the queue and found every door full.</param>
    /// <param name="Inverted">
    /// Of those, how many were nearer to some school than that school's furthest admitted family.
    /// </param>
    /// <param name="WorstMargin">
    /// The largest such gap, as a walk. <b>Zero when <paramref name="Inverted"/> is zero</b>, and it
    /// is what tells a marginal mis-ordering from a gross one — ***a count of inversions says the
    /// rule was broken and never says by how much.***
    /// </param>
    internal readonly record struct Reading(
        int Admitted, int TurnedAway, int Inverted, TravelTime WorstMargin);

    /// <summary>Reads the Day's assignment off the engine and counts the inversions in it.</summary>
    /// <remarks>
    /// <para>
    /// <b>Two stages, and the second is the cheap one on purpose.</b> Stage one routes each admitted
    /// family to the school it got and keeps, per school, the <em>furthest</em> of them — that is one
    /// route per admitted family and the only figure a school needs, since a nearer applicant beats
    /// the furthest incumbent or it beats nobody. Stage two routes each turned-away family to each
    /// school that admitted anybody. ***Recomputing every admitted family per candidate would be the
    /// same answer at the product of the two counts.***
    /// </para>
    /// <para>
    /// ⚠ <b>The Commute Budget is applied to the turned-away family and not to the incumbent.</b> An
    /// admitted family is inside it by construction — <c>ServiceEngine.Reach</c> would not have
    /// offered the school otherwise — where a turned-away family is being asked about a school it
    /// may never have been able to reach at all.
    /// </para>
    /// </remarks>
    internal static Reading Measure(World world, ServiceEngine services)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(services);

        ReadOnlySpan<int> households = services.LastHouseholds;
        ReadOnlySpan<int> providers = services.LastProviders;

        var scratch = new WalkScratch();
        Dictionary<int, TravelTime> furthest = [];

        int admitted = 0;
        int turnedAway = 0;

        for (int i = 0; i < households.Length; i++)
        {
            if (providers[i] == Rows.NoSlot)
            {
                turnedAway++;
                continue;
            }

            admitted++;

            if (!Walk(world, services, households[i], providers[i], scratch, out TravelTime cost))
            {
                continue;
            }

            if (!furthest.TryGetValue(providers[i], out TravelTime standing) || cost > standing)
            {
                furthest[providers[i]] = cost;
            }
        }

        int inverted = 0;
        TravelTime worst = TravelTime.Zero;

        for (int i = 0; i < households.Length; i++)
        {
            if (providers[i] != Rows.NoSlot)
            {
                continue;
            }

            TravelTime margin = Margin(world, services, households[i], furthest, scratch);

            if (margin > TravelTime.Zero)
            {
                inverted++;

                if (margin > worst)
                {
                    worst = margin;
                }
            }
        }

        return new Reading(admitted, turnedAway, inverted, worst);
    }

    /// <summary>
    /// By how much this turned-away family beats the furthest family admitted anywhere it could
    /// have gone, or zero if it beats nobody.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The BEST such gap across every school, and not the first found.</b> A family may be
    /// inside the Budget of two full schools and outrank an incumbent at both; the larger gap is the
    /// one that says how wrong the ordering was.
    /// </remarks>
    private static TravelTime Margin(
        World world,
        ServiceEngine services,
        int household,
        Dictionary<int, TravelTime> furthest,
        WalkScratch scratch)
    {
        TravelTime best = TravelTime.Zero;

        // A Dictionary walked in the instrument and never in the simulation, which is what 05 §4's
        // lint 3 is about: this runs once at the end of a run, off a snapshot, and its ORDER cannot
        // reach a State Hash because it writes nothing. The comparison it feeds is a max, which is
        // order-independent anyway.
        foreach (KeyValuePair<int, TravelTime> school in furthest)
        {
            if (!Walk(world, services, household, school.Key, scratch, out TravelTime cost))
            {
                continue;
            }

            if (!world.Rules.Trips.TryRung(cost, out _) || cost >= school.Value)
            {
                continue;
            }

            TravelTime margin = school.Value - cost;

            if (margin > best)
            {
                best = margin;
            }
        }

        return best;
    }

    /// <summary>What the school run costs this Household, on the traveller the engine would send.</summary>
    /// <remarks>
    /// <b><c>ServiceEngine.Traveller</c>'s rule restated, and restating it is the risk this method
    /// carries.</b> A child for Education and anybody for Health; the head of the member list either
    /// way. ***An instrument that models the mechanism it measures is wrong the day the mechanism
    /// moves*** — what keeps this honest is that it is checked against the engine's own counters by
    /// <c>ServiceAdmissionTests</c> rather than trusted.
    /// </remarks>
    private static bool Walk(
        World world,
        ServiceEngine services,
        int household,
        int school,
        WalkScratch scratch,
        out TravelTime cost)
    {
        cost = TravelTime.Impassable;

        int traveller = Traveller(world, services.LastNeed, household);

        if (traveller < 0
            || !world.Buildings.Rows.TryResolve(world.Households.Dwelling[household], out int home))
        {
            return false;
        }

        TravelMode mode = world.ModeOf(traveller);

        cost = WalkRouting.Cost(
            world.Roads, mode, world.AccessPoint(home, mode), world.AccessPoint(school, mode),
            world.Rules.Trips.CrossingCost, scratch);

        return !cost.IsImpassable;
    }

    private static int Traveller(World world, Need need, int household)
    {
        bool wantsChild = need == Need.Education;

        foreach (int member in world.Members.Walk(household))
        {
            if (!wantsChild || world.Citizens.Age[member] == 0)
            {
                return member;
            }
        }

        return -1;
    }
}

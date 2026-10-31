namespace Borough.Core.Instruments;

using Borough.Core.Entities;
using Borough.Core.Evidence;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

/// <summary>Why the Households in the Unplaced Pool have no home.</summary>
/// <param name="Looking">Households in the Unplaced Pool.</param>
/// <param name="NoRoom">Households for which no standing home has room.</param>
/// <param name="CannotAfford">Households that cannot pay the cheapest home with room.</param>
/// <param name="CanAfford">Households that can pay for a home with room and have not yet moved in.</param>
/// <param name="VacantHomes">Standing, unabandoned Buildings with room for another Household.</param>
/// <param name="LeftYesterday">Households that emigrated over the last complete Day.</param>
public readonly record struct HomeNeeds(
    int Looking, int NoRoom, int CannotAfford, int CanAfford, int VacantHomes, long LeftYesterday);

/// <summary>Why working-age Citizens without a Workplace have none.</summary>
/// <param name="Employed">Citizens holding a job.</param>
/// <param name="NoVacancy">Citizens whose reachable posts were all full.</param>
/// <param name="BeyondReach">Citizens whose only vacancies lie beyond the Commute Budget.</param>
/// <param name="BelowCredential">Citizens lacking the Skill Tier a reachable vacancy asks for.</param>
/// <param name="OpenPosts">Unfilled posts at Businesses that can afford to hire.</param>
/// <param name="BusinessesLooking">Businesses in the Unpremised Pool, waiting for premises.</param>
public readonly record struct JobNeeds(
    int Employed, int NoVacancy, int BeyondReach, int BelowCredential, int OpenPosts, int BusinessesLooking);

/// <summary>Households whose errand for one Good is failing, for one reason.</summary>
/// <param name="Good">The Good they set out to buy.</param>
/// <param name="Reason">Why the last attempt failed.</param>
/// <param name="Households">How many errands are failing this way.</param>
public readonly record struct ShoppingNeed(ResourceId Good, ShoppingFailure Reason, int Households);

/// <summary>Vacant Lots by the reasons <see cref="LotEvidence"/> gives. A Lot may carry several.</summary>
/// <param name="Vacant">Vacant Lots.</param>
/// <param name="NoFrontage">Vacant Lots touching no Street.</param>
/// <param name="NobodySeeking">Vacant Lots that no Household is waiting to build on.</param>
/// <param name="NotZoned">Vacant Lots no declared Zone Rule admits.</param>
/// <param name="NoCapital">Vacant Lots no Household can finance.</param>
/// <param name="Unexplained">Vacant Lots with no reason, usually ones the Zone Rule has not sampled yet.</param>
public readonly record struct LotNeeds(
    int Vacant, int NoFrontage, int NobodySeeking, int NotZoned, int NoCapital, int Unexplained);

/// <summary>
/// Counts of Households, Citizens, Businesses and Lots that want something the city does not give.
/// </summary>
/// <remarks>
/// Every figure counts subjects, never a quantity of a Resource. Reading changes no state.
/// </remarks>
public static class NeedsReading
{
    /// <summary>Classifies every Household in the Unplaced Pool against the homes standing now.</summary>
    public static HomeNeeds Homes(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        int vacant = 0;
        Money cheapest = default;

        for (int building = 0; building < world.Buildings.Rows.SlotCount; building++)
        {
            if (!world.Buildings.Rows.IsLive(building) || world.Buildings.IsAbandoned(building)
                || !world.HasRoomForHousehold(building))
            {
                continue;
            }

            Money rent = world.HouseholdRent(building);

            if (vacant == 0 || rent.Raw < cheapest.Raw)
            {
                cheapest = rent;
            }

            vacant++;
        }

        UnplacedTable pool = world.UnplacedPool;
        int noRoom = 0, cannotAfford = 0, canAfford = 0;

        for (int position = 0; position < pool.Count; position++)
        {
            if (vacant == 0)
            {
                noRoom++;
            }
            else if (HousingUtility.Affordable(world, position, cheapest))
            {
                canAfford++;
            }
            else
            {
                cannotAfford++;
            }
        }

        long left = PopulationReading.Of(world).Yesterday.HouseholdsDeparted;

        return new(pool.Count, noRoom, cannotAfford, canAfford, vacant, left);
    }

    /// <summary>Counts Citizens by employment state and the posts nobody holds.</summary>
    public static JobNeeds Jobs(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        int employed = 0, noVacancy = 0, beyond = 0, below = 0;

        for (int citizen = 0; citizen < world.Citizens.Rows.SlotCount; citizen++)
        {
            if (!world.Citizens.Rows.IsLive(citizen))
            {
                continue;
            }

            switch ((EmploymentState)world.Citizens.Employment[citizen])
            {
                case EmploymentState.Employed: employed++; break;
                case EmploymentState.NoVacancy: noVacancy++; break;
                case EmploymentState.BeyondReach: beyond++; break;
                case EmploymentState.BelowCredential: below++; break;
            }
        }

        int open = 0;

        for (int business = 0; business < world.Businesses.Rows.SlotCount; business++)
        {
            if (world.Businesses.Rows.IsLive(business) && world.HasJob(business))
            {
                open += world.DeclaredJobs(business) - world.Workers.Length(business);
            }
        }

        return new(employed, noVacancy, beyond, below, open, world.UnpremisedPool.Count);
    }

    /// <summary>
    /// Groups failing errands by Good and reason into <paramref name="into"/>, most Households first.
    /// </summary>
    /// <returns>How many groups were written. Groups past the buffer's length are dropped.</returns>
    public static int Shopping(World world, Span<ShoppingNeed> into)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (!world.Rules.Shopping.Runs)
        {
            return 0;
        }

        ShoppingTable outings = world.Shopping;
        int written = 0;

        for (int row = 0; row < outings.Rows.SlotCount; row++)
        {
            if (!outings.Rows.IsLive(row) || outings.UnservedSince[row].Raw == 0
                || world.Tick.Raw + 1 < outings.UnservedSince[row].Raw
                || (ShoppingFailure)outings.Reason[row] == ShoppingFailure.None)
            {
                continue;
            }

            ResourceId good = outings.Good[row];
            var reason = (ShoppingFailure)outings.Reason[row];
            int at = 0;

            while (at < written && (into[at].Good != good || into[at].Reason != reason))
            {
                at++;
            }

            if (at < written)
            {
                into[at] = into[at] with { Households = into[at].Households + 1 };
            }
            else if (written < into.Length)
            {
                into[written++] = new(good, reason, 1);
            }
        }

        for (int sorted = 1; sorted < written; sorted++)
        {
            ShoppingNeed moving = into[sorted];
            int at = sorted - 1;

            while (at >= 0 && Before(moving, into[at]))
            {
                into[at + 1] = into[at];
                at--;
            }

            into[at + 1] = moving;
        }

        return written;
    }

    /// <summary>Counts vacant Lots by reason.</summary>
    /// <remarks>Reads <see cref="Borough.Core.Evidence.Evidence.OfLots"/>, which is cold and allocating.</remarks>
    public static LotNeeds Lots(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        int vacant = 0, frontage = 0, seeking = 0, zoned = 0, capital = 0, unexplained = 0;

        foreach (LotEvidence lot in Borough.Core.Evidence.Evidence.OfLots(world))
        {
            if (!lot.IsVacant)
            {
                continue;
            }

            vacant++;

            if (lot.Reason == VacancyReason.None) unexplained++;
            if ((lot.Reason & VacancyReason.NoFrontage) != 0) frontage++;
            if ((lot.Reason & VacancyReason.NobodySeeking) != 0) seeking++;
            if ((lot.Reason & VacancyReason.NotZoned) != 0) zoned++;
            if ((lot.Reason & VacancyReason.NoCapital) != 0) capital++;
        }

        return new(vacant, frontage, seeking, zoned, capital, unexplained);
    }

    private static bool Before(ShoppingNeed a, ShoppingNeed b) =>
        a.Households != b.Households
            ? a.Households > b.Households
            : a.Good != b.Good ? a.Good < b.Good : a.Reason < b.Reason;
}

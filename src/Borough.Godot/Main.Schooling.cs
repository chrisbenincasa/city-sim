using System.Collections.Generic;
using Borough.Core.Entities;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
namespace Borough.Shell;

public partial class Main
{
    /// <summary>
    /// A school as a FACILITY — the places it reaches today, who attended, and the staff behind both.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>Nothing in the shell said what a school holds.</b> <c>World.DeclaredPlaces</c> is the
    /// number a family is turned away against, and it appeared in no panel, no hover and no readout —
    /// so a school whose teachers had gone and a school at full strength presented identically.
    /// ***A capacity a player cannot read is a capacity that cannot be managed.***
    /// </para>
    /// <para>
    /// ⚠ <b>Both place counts, because the fall is the reading.</b> The floor's answer is fixed by
    /// the ground and the staffed answer moves with the payroll, so the pair says <em>this school was
    /// built for twelve and reaches five</em> where either alone says nothing.
    /// </para>
    /// <para>
    /// ⚠ <b>Attendance is today's or nothing.</b> <c>Buildings.AttendedToday</c> is reset lazily by
    /// <c>World.TakeServicePlace</c>, so a count left over from an earlier Day is stale until somebody
    /// attends — the Day stamp is what makes it safe to show.
    /// </para>
    /// </remarks>
    private void AddFacilitySchooling(List<InformationSection> sections, int building)
    {
        if (_world.Rules.ServedBy(_world.Buildings.Kind[building]) != Need.Education)
        {
            return;
        }

        int today = (int)(_simulation.Tick.Raw / (ulong)Ticks.PerDay);
        int attended = _world.Buildings.AttendedDay[building] == today
            ? _world.Buildings.AttendedToday[building]
            : 0;

        var rows = new List<InformationRow>
        {
            new(_world.Rules.Capacity.FloorTilesPerPlace <= 0
                ? "Places: unbounded — this Ruleset sets no floor area a place"
                : $"Places: {_world.DeclaredPlaces(building):N0} a Day, on a floor built for "
                    + $"{_world.FloorServicePlaces(building):N0}"),
            new($"Attended today: {attended:N0}"),
            new(_world.ServiceStaffing(building, out int teachers, out int posts)
                ? $"Staff: {teachers:N0} of {posts:N0} posts filled"
                : "Staff: this Building holds no trade of its own, so it teaches nobody"),
        };

        sections.Add(new("places", "Teaching capacity", true, rows));
    }

    private void AddHouseholdSchooling(List<InformationSection> sections, int household)
    {
        if (!_world.Rules.Schooling.Runs) { return; }

        var rows = new List<InformationRow>();
        var state = (HouseholdState)_world.Households.State[household];

        if (state == HouseholdState.InEducation)
        {
            int today = (int)(_simulation.Tick.Raw / (ulong)Ticks.PerDay);
            int left = _world.Households.StateEndsDay[household] - today;
            string where = _world.Buildings.Rows.IsValid(_world.Households.University[household])
                ? $"Building {RowId(_world.Buildings.Rows, _world.Households.University[household])}"
                : "a university that no longer stands";

            rows.Add(new($"In Education at {where}\n{left:N0} Days to a degree. Supplies no labour."
                + $"{TuitionSentence(household)}"));
        }
        else if (state == HouseholdState.Considering)
        {
            rows.Add(new("Qualified for a university and not yet housed.\n"
                + "The decision is taken on the first Day this Household has a dwelling."));
        }

        foreach (int member in _world.Members.Walk(household))
        {
            if (_world.Citizens.Age[member] == 0)
            {
                rows.Add(new($"Citizen {_world.Citizens.Rows.IdAt(member)} · a child\n"
                    + $"Primary {_world.Citizens.SchoolingPrimary[member]:N0} Days · "
                    + $"secondary {_world.Citizens.SchoolingSecondary[member]:N0} Days attended"));

                continue;
            }

            rows.Add(new($"Citizen {_world.Citizens.Rows.IdAt(member)} · {TierSentence(member)}\n"
                + $"{ChildhoodSentence(member)}\n{WorkSentence(member)}"));
        }

        if (rows.Count == 0) { rows.Add(new("Nothing about this Household's education is known.")); }

        sections.Add(new("schooling", "Education and work", true, rows));
    }

    private string TuitionSentence(int household)
    {
        if (!_world.Buildings.Rows.TryResolve(_world.Households.University[household], out int at))
        {
            return string.Empty;
        }

        foreach (int tenant in _world.BuildingBusinesses.Walk(at))
        {
            BusinessKindDefinition trade = _world.Rules.BusinessKind(_world.Businesses.Kind[tenant]);

            if (trade.Charges)
            {
                return $"\nPrivate: {trade.TuitionPerDay:N0} a Day, taken from this Household's balance."
                    + "\nA Day it cannot pay ends the course without a degree.";
            }
        }

        return "\nPublic: free, and rationed by places rather than by money.";
    }

    private string TierSentence(int citizen)
    {
        byte tier = _world.Citizens.SkillTier[citizen];
        string name = tier >= SchoolingRuleset.TopTier ? "Tier 3, an analyst"
            : tier == 2 ? "Tier 2, a technician"
            : "Tier 1, an apprentice";

        long experience = _world.Citizens.Experience[citizen];
        long premium = _world.Rules.Jobs.PremiumPercent(experience);

        return tier >= SchoolingRuleset.TopTier
            ? $"{name} · experience {experience:N0}, worth {premium:N0}% more pay"
            : $"{name} · experience {experience:N0} of {_world.Rules.Jobs.Tier2Experience:N0}"
                + $", worth {premium:N0}% more pay";
    }

    private string ChildhoodSentence(int citizen)
    {
        byte score = _world.Citizens.ChildhoodScore[citizen];
        string schooled = _world.IsSchooled(citizen)
            ? "cleared the schooling cut"
            : "missed it, so experience accrues slower";

        return $"Childhood scored {score:N0} of 100 — {schooled}."
            + $" Attended {_world.Citizens.SchoolingPrimary[citizen]:N0} Days of primary"
            + $" and {_world.Citizens.SchoolingSecondary[citizen]:N0} of secondary.";
    }

    private string WorkSentence(int citizen)
    {
        var state = (EmploymentState)_world.Citizens.Employment[citizen];

        if (!_world.Businesses.Rows.TryResolve(_world.Citizens.Workplace[citizen], out int job))
        {
            return state switch
            {
                EmploymentState.BelowCredential =>
                    "Unemployed: a post stood in reach and wanted a Skill Tier this Citizen does not hold.",
                EmploymentState.BeyondReach =>
                    "Unemployed: a vacancy stood in the box and no route delivers it inside the Commute Budget.",
                EmploymentState.NoVacancy =>
                    "Unemployed: posts stood in reach and every one of them was full.",
                EmploymentState.Studying => "Studying, and supplying no labour by design.",
                _ => "No job, and nothing has been concluded about why yet.",
            };
        }

        BusinessKindDefinition trade = _world.Rules.BusinessKind(_world.Businesses.Kind[job]);
        long graded = WorkSchedule.Graded(_world, citizen, trade.WagePerDay);
        string wants = trade.RequiresTier > 0 ? $", which wants Tier {trade.RequiresTier}" : string.Empty;

        return $"Works at Business {_world.Businesses.Rows.IdAt(job)}{wants}."
            + $"\nEarns {graded:N0} a Day against a posted {trade.WagePerDay:N0}.";
    }
}

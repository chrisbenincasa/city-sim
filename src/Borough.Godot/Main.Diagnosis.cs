using System.Collections.Generic;
using Borough.Core.Entities;
using Borough.Core.Evidence;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Shell;

public partial class Main
{
    private Handle<Business> _selectedBusiness;
    private int _businessScroll;

    private string SupplySummary(SupplyEvidence reading)
    {
        if (!reading.Available) return "Supply explanation unavailable";
        if (reading.Shortfalls > 0)
        {
            string cause = reading.Primary.WaitingBin.IsNone ? "Supply explanation unavailable"
                : $"Waiting for {_names.Resource(reading.Primary.WaitingFor) ?? "an unnamed Resource"}";
            return cause + (reading.Shortfalls > 1 ? $" · +{reading.Shortfalls - 1} other shortfalls" : "")
                + (reading.Unavailable > 0 ? " · incomplete evidence" : "");
        }
        if (reading.Unavailable > 0) return "Supply explanation unavailable";
        return reading.RoutineWaits > 0 ? "Routine waiting · no supply shortfalls reported" : "No supply shortfalls reported";
    }

    private void BusinessInformation(List<InformationSection> sections, out string title, out string identity)
    {
        title = "Business no longer exists";
        identity = "SELECTED BUSINESS";
        if (!_world.Businesses.Rows.TryResolve(_selectedBusiness, out int slot)) return;
        title = _names.BusinessKind(_world.Businesses.Kind[slot]) ?? "Business";
        identity += $" · {_world.Businesses.Rows.IdAt(slot)}";
        var home = _world.Businesses.Building[slot];
        var rows = new List<InformationRow>();
        if (_world.Buildings.Rows.IsValid(home))
        {
            ulong id = RowId(_world.Buildings.Rows, home);
            rows.Add(new($"Premises: Building {id} →", $"building {id}"));
        }
        else rows.Add(new("No current premises"));
        sections.Add(new("premises", "Location", true, rows));
        if (_world.Buildings.Rows.IsValid(home))
        {
            sections.Insert(0, new("summary", "Current condition", true,
                [new(SupplySummary(Evidence.SupplyOfBuilding(_world, home, business: _selectedBusiness)))]));
            BuildingEvidence evidence = Evidence.OfBuilding(_world, home);
            sections.Add(Attention(evidence, default, false, business: _selectedBusiness));
            sections.Add(Attention(evidence, default, false, true, _selectedBusiness));
        }
        AddFailingCommuters(sections);
        var stocks = new List<InformationRow>();
        var bin = _world.Businesses.BinHead[slot];
        while (_world.Bins.Rows.TryResolve(bin, out int at))
        {
            if (!_world.Rules.IsConserved(_world.Bins.Resource[at]))
                stocks.Add(new($"{_names.Resource(_world.Bins.Resource[at]) ?? "Resource"}: {_world.Bins.LevelAt(at):N0} / {_world.Bins.Capacity[at]:N0} units", Icon: UiIcons.Resource(_names.Resource(_world.Bins.Resource[at]))));
            bin = _world.Bins.OwnerNext[at];
        }
        if (_world.Bins.Rows.TryResolve(_world.Businesses.Balance[slot], out int balance))
            stocks.Add(new($"Balance: {_world.Bins.LevelAt(balance):N0} money units"));
        if (stocks.Count == 0) stocks.Add(new("No stocks reported"));
        sections.Add(new("stocks", "Business stocks", true, stocks));
    }

    /// <summary>
    /// The staffing half of a service Building's condition, so full stocks cannot read as a working
    /// service. Null for a Building that serves no Need or whose trade declares no posts.
    /// </summary>
    private string? StaffingSummary(int building)
    {
        if (_world.Rules.ServedBy(_world.Buildings.Kind[building]) == Need.None) return null;
        if (!_world.ServiceStaffing(building, out int workers, out int posts))
            return "No staff · this Building has no trade of its own, so it serves nobody";
        if (posts <= 0) return null;
        return workers == 0 ? $"No staff · 0 of {posts:N0} posts filled, so it serves nobody"
            : $"Staff: {workers:N0} of {posts:N0} posts filled";
    }

    /// <summary>A Day as the HUD clock numbers it, counting from 1.</summary>
    private static long ClockDay(ulong tick) => (long)(tick / (ulong)Ticks.PerDay) + 1;

    private static string DayCount(long days) => days == 1 ? "1 Day" : $"{days:N0} Days";

    private string? CommuteReading(CitizenEvidence who)
    {
        if (who.FailingCommute is { } failing)
        {
            long since = ClockDay(failing.Since.Raw);
            long elapsed = ClockDay(_world.Tick.Raw) - since;
            long left = failing.DismissalDue.Raw <= _world.Tick.Raw ? 0
                : (long)((failing.DismissalDue.Raw - _world.Tick.Raw + (ulong)Ticks.PerDay - 1) / (ulong)Ticks.PerDay);
            string fate = who.LastTrip?.Fate switch
            {
                TripFate.NoRouteFound => "no route to work",
                TripFate.ExceededCommuteBudget => "the commute exceeds the budget",
                _ => "the commute failed",
            };
            return $"Unable to reach work since Day {since:N0}"
                + (elapsed > 0 ? $", {DayCount(elapsed)} ago" : ", today") + $" · {fate}\n"
                + (left == 0 ? "Dismissed at the next failed commute" : $"Dismissal in {DayCount(left)} if no commute completes");
        }
        if (who.LastCommuteEpisode is { } episode)
        {
            long end = episode.EndDay + 1L;
            string run = DayCount(episode.EndDay - episode.StartDay);
            return episode.Outcome == CommuteEpisodeOutcome.Recovered
                ? $"Commute restored on Day {end:N0} after {run} of failed commutes"
                : who.Workplace.IsNone ? $"Dismissed on Day {end:N0} after {run} of failed commutes"
                : $"Lost the previous job on Day {end:N0} after {run} of failed commutes";
        }
        return null;
    }

    private void AddFailingCommuters(List<InformationSection> sections)
    {
        if (!_world.Rules.Jobs.DismissesFailedCommuters) return;
        ReadOnlySpan<Handle<Citizen>> failing = Evidence.FailingCommutersOf(_world, _selectedBusiness).Span;
        if (failing.IsEmpty) return;
        var rows = new List<InformationRow>();
        ulong earliest = ulong.MaxValue;
        foreach (Handle<Citizen> worker in failing)
        {
            int citizen = _world.Citizens.Rows.Resolve(worker);
            earliest = Math.Min(earliest, _world.Citizens.CommuteFailedSince[citizen].Raw - 1);
            ulong household = RowId(_world.Households.Rows, _world.Citizens.HouseholdOf[citizen]);
            rows.Add(new($"Citizen {_world.Citizens.Rows.IdAt(citizen)} · Household {household} →", $"household {household}"));
        }
        rows.Insert(0, new($"{failing.Length:N0} {(failing.Length == 1 ? "worker cannot" : "workers cannot")} reach this workplace, "
            + $"the first since Day {ClockDay(earliest):N0}"));
        sections.Add(new("commutes", "Workers who cannot reach work", true, rows));
    }
}

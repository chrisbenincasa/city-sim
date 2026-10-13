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

    private string? CommuteReading(CitizenEvidence who)
    {
        long today = (long)(_world.Tick.Raw / (ulong)Ticks.PerDay);
        if (who.FailingCommute is { } failing)
        {
            long since = (long)(failing.Since.Raw / (ulong)Ticks.PerDay);
            long left = failing.DismissalDue.Raw <= _world.Tick.Raw ? 0
                : (long)((failing.DismissalDue.Raw - _world.Tick.Raw + (ulong)Ticks.PerDay - 1) / (ulong)Ticks.PerDay);
            string fate = who.LastTrip?.Fate switch
            {
                TripFate.NoRouteFound => "no route to work",
                TripFate.ExceededCommuteBudget => "the commute exceeds the budget",
                _ => "the commute failed",
            };
            return $"Unable to reach work for {today - since:N0} Days, since Day {since:N0} · {fate}\n"
                + (left == 0 ? "Dismissed at the next failed commute" : $"Dismissal in {left:N0} Days if no commute completes");
        }
        if (who.LastCommuteEpisode is { } episode)
        {
            int days = episode.EndDay - episode.StartDay;
            return episode.Outcome == CommuteEpisodeOutcome.Recovered
                ? $"Commute restored on Day {episode.EndDay:N0} after {days:N0} Days of failed commutes"
                : $"Dismissed on Day {episode.EndDay:N0} after {days:N0} Days of failed commutes";
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
            + $"the first since Day {earliest / (ulong)Ticks.PerDay:N0}"));
        sections.Add(new("commutes", "Workers who cannot reach work", true, rows));
    }
}

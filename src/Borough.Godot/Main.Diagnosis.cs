using System.Collections.Generic;
using Borough.Core.Entities;
using Borough.Core.Evidence;
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
}

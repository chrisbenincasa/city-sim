using System;
using System.Linq;
using Borough.Core.Rules;
using Godot;

namespace Borough.Shell;

public partial class Main
{
    private sealed record ToolOption(string Label, int Choice);
    private sealed record ToolDefinition(string Id, string Label, string Category, Key Key,
        string Hint, bool Available, int NextChoice, ToolOption[] Options, Action<int> Select);

    private ToolDefinition[] ToolDefinitions() =>
    [
        new("look", "Look / cancel tool", "Roads", Key.V, "Inspect the city", true, 0, [],
            i => Apply(Held("look", i))),
        new("zone", "Zone / next permission", "Zoning", Key.Z,
            "Drag opposite corners; release to paint whole blocks", _world.Rules.ZoneRules.Length > 0,
            _verb == Verb.Zone && _world.Rules.ZoneRules.Length > 0 ? (_zoneChoice + 1) % _world.Rules.ZoneRules.Length : 0,
            Enumerable.Range(0, _world.Rules.ZoneRules.Length).Select(i => new ToolOption(_names.ZoneRule(i) ?? $"Permission {i + 1}", i)).ToArray(),
            i => Apply(Held("zone", i))),
        new("erase", "Erase zoning", "Zoning", Key.None, "Remove permissions; keep existing Buildings", true, 0,
            [new("Erase zoning", 0)], i => Apply(Held("erase", i))),
        new("street", "Street", "Roads", Key.X, "Lay one Street; Shift-click removes it", true, 0,
            [new("Street", 0)], i => Apply(Held("street", i))),
        new("demolish", "Demolish", "Roads", Key.B, "Clear an abandoned Building", true, 0,
            [new("Demolish", 0)], i => Apply(Held("demolish", i))),
        new("service", "Service / next kind", "Services", Key.S, "Place a service on a vacant Lot", NextService(0) != 0,
            NextService(_verb == Verb.Service ? _serviceKind : (byte)0),
            Enumerable.Range(1, _world.Rules.KindCount).Where(i => _world.Rules.Declares((byte)i)
                && _world.Rules.Kind((byte)i).Serves != Need.None)
                .Select(i => new ToolOption(_names.Kind((byte)i) ?? $"Service {i}", i)).ToArray(),
            i => Apply(Held("service", i))),
        new("policies", "Policies", "Policies", Key.P, "Open city Policies", _world.Rules.Policies.Length > 0,
            0, [], _ => Govern()),
    ];
}

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
        new("look", "Look / cancel tool", "Connections", Key.V, "Inspect the city", true, 0, [],
            i => Apply(Held("look", i))),
        new("zone", "Zone / next permission", "Zoning", Key.Z,
            "Click a parcel or drag an area; choose parcel or block selection below", _world.Rules.ZoneRules.Length > 0,
            _verb == Verb.Zone && _world.Rules.ZoneRules.Length > 0 ? (_zoneChoice + 1) % _world.Rules.ZoneRules.Length : 0,
            Enumerable.Range(0, _world.Rules.ZoneRules.Length).Select(i => new ToolOption(_names.ZoneRule(i) ?? $"Permission {i + 1}", i)).ToArray(),
            i => Apply(Held("zone", i))),
        new("zone-size", "Selection size", "Zoning", Key.None, "Choose how much ground one click paints", true, 0,
            [new("Parcels", 0), new("Whole blocks", 1)], i => Ui(i == 0 ? "zone-size parcels" : "zone-size blocks")),
        new("erase", "Erase zoning", "Zoning", Key.None, "Remove permissions; keep existing Buildings", true, 0,
            [new("Erase zoning", 0)], i => Apply(Held("erase", i))),
        new("street", "Street / next mode", "Connections", Key.X,
            "Straight: start, end. Simple curve: start, bend point, end. Continuous: each Street leaves the last one's end along its direction. Grid: two corners, then drag sideways for rows of blocks. Hold Ctrl to lay a curve straight; U adds a parallel Street and I flips its side. N switches snapping, K the length snap, J the angle snap, H the angle step; Shift-click removes a grid Street",
            true, _verb == Verb.Connect ? ((int)_streetMode + 1) % StreetModeNames.Length : 0,
            StreetModeNames.Select((name, i) => new ToolOption(name, i)).ToArray(), i => Apply(Held("street", i))),
        new("demolish", "Demolish", "Demolish", Key.B, "Clear a Building; occupants are paid its land value", true, 0,
            [new("Demolish", 0)], i => Apply(Held("demolish", i))),
        new("service", "Service / next kind", "Municipal", Key.S, "Place a service on a vacant Lot", NextService(0) != 0,
            NextService(_verb == Verb.Service ? _serviceKind : (byte)0),
            Enumerable.Range(1, _world.Rules.KindCount).Where(i => _world.Rules.Declares((byte)i)
                && _world.Rules.Kind((byte)i).Serves != Need.None)
                .Select(i => new ToolOption(_names.Kind((byte)i) ?? $"Service {i}", i)).ToArray(),
            i => Apply(Held("service", i))),
        new("gate", "Outside Connection / next kind", "Connections", Key.None,
            "Place a gate on a vacant edge Lot; Shift-click removes it", NextGate(0) != 0,
            NextGate(_verb == Verb.Gate ? _gateKind : (byte)0),
            Enumerable.Range(1, _world.Rules.KindCount).Where(i => _world.IsOutsideConnection((byte)i))
                .Select(i => new ToolOption(_names.Kind((byte)i) ?? $"Gate {i}", i)).ToArray(),
            i => Apply(Held("gate", i))),
        new("policies", "Policies", "Policies", Key.P, "Open city Policies", _world.Rules.Policies.Length > 0,
            0, [], _ => Govern()),
    ];
}

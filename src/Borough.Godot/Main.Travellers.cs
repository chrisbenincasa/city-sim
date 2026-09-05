using System.Collections.Generic;
using Borough.Core.Entities;
using Borough.Core.Movement;
using Borough.Core.Tables;
using Borough.Core.Space;
using Godot;

namespace Borough.Shell;

public partial class Main
{
    private readonly Dictionary<Handle<Citizen>, float> _travellerHeadings = [];
    private static readonly Color[] TravellerPaint =
    [new(0.72f, 0.25f, 0.16f), new(0.25f, 0.42f, 0.57f), new(0.82f, 0.78f, 0.65f),
     new(0.31f, 0.39f, 0.32f), new(0.68f, 0.65f, 0.58f), new(0.30f, 0.31f, 0.34f)];

    private void TravellerHeadings()
    {
        _travellerHeadings.Clear();
        var travellers = _world.Travellers;
        var hops = _world.RouteHops;
        var segments = _world.Roads.Segments;
        var nodes = _world.Roads.Nodes;
        for (int slot = 0; slot < travellers.Rows.SlotCount; slot++)
        {
            if (!travellers.Rows.IsLive(slot)) continue;
            int hop = travellers.CurrentHop[slot];
            if (hop == Rows.NoSlot || !segments.Rows.TryResolve(hops.Segment[hop], out int segment)
                || !nodes.Rows.TryResolve(segments.NodeA[segment], out int a)
                || !nodes.Rows.TryResolve(segments.NodeB[segment], out int b)) continue;
            float east = nodes.East[b].Raw - nodes.East[a].Raw;
            float north = nodes.North[b].Raw - nodes.North[a].Raw;
            float direction = hops.Forward[hop] != 0 ? 1f : -1f;
            _travellerHeadings[travellers.Citizen[slot]] = Mathf.Atan2(east * direction, -north * direction);
        }
    }

    private IEnumerable<(ulong Id, Transform3D Where, Color What)> Travellers(int found, bool cars)
    {
        for (int at = 0; at < found; at++)
        {
            var traveller = _agents[at];
            if ((traveller.Mode == TravelMode.Car) != cars) continue;
            ulong id = _world.Citizens.Rows.TryResolve(traveller.Citizen, out int slot)
                ? _world.Citizens.Rows.IdAt(slot) : 0UL;
            _travellerHeadings.TryGetValue(traveller.Citizen, out float yaw);
            var where = new Vector3(traveller.East.Raw * MetresPerTile / 65_536f,
                0.06f, -traveller.North.Raw * MetresPerTile / 65_536f);
            // Preserve the query's position. Drivers are still located at their hop's midpoint;
            // a more detailed mesh does not supply lane positions or continuous driving motion.
            yield return (id, new Transform3D(Basis.FromEuler(new Vector3(0f, yaw, 0f)), where),
                TravellerPaint[(int)(Scramble(id) % (ulong)TravellerPaint.Length)].SrgbToLinear());
        }
    }
}

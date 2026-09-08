using System;
using System.Collections.Generic;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;
using Godot;

namespace Borough.Shell;

public partial class Main
{
    private Handle<RoadSegment> _selectedRoad, _roadParent;
    private int _roadScroll;
    private MeshInstance3D? _roadSelection, _roadHover;
    private readonly Dictionary<Mesh, Vector3[]> _pickingFaces = new();
    private (Handle<Building> Building, Handle<RoadSegment> Road) _picked;
    private Vector3 _pickOrigin, _pickDirection;
    private ulong _pickTick = ulong.MaxValue;
    private ulong _pickAt;

    private static Handle<T> InformationHandle<T>(Rows<T> rows, ulong id) where T : unmanaged
    {
        if (id == 0) return default;
        for (int i = 0; i < rows.SlotCount; i++)
            if (rows.IsLive(i) && rows.IdAt(i) == id) return rows.At(i);
        return default;
    }

    private (Handle<Building> Building, Handle<RoadSegment> Road) PickInformation(bool force = false, Vector2? screen = null)
    {
        Vector2 pointer = screen ?? GetViewport().GetMousePosition();
        Vector3 origin = _aimed is { } at
            ? new Vector3(at.East.Raw * MetresPerTile, 10000, -at.North.Raw * MetresPerTile)
            : _camera.ProjectRayOrigin(pointer);
        Vector3 direction = _aimed is not null ? Vector3.Down : _camera.ProjectRayNormal(pointer);
        ulong now = Time.GetTicksMsec();
        if (!force && origin == _pickOrigin && direction == _pickDirection
            && _pickTick == _world.Tick.Raw && now - _pickAt < 250) return _picked;
        _pickOrigin = origin;
        _pickDirection = direction;
        _pickTick = _world.Tick.Raw;
        _pickAt = now;
        float distance = float.PositiveInfinity;
        ulong building = 0, road = 0;
        Hit(_buildings, _buildingIds, false);
        Hit(_roofs, _roofIds, false);
        Hit(_hips, _hipIds, false);
        Hit(_pairedRoofs, _pairedRoofIds, false);
        Hit(_parapets, _parapetIds, false);
        Hit(_yards, _yardIds, false);
        Hit(_roads, _roadIds, true);
        Hit(_footways, _footwayIds, true);
        Hit(_kerbs, _kerbIds, true);
        _picked = (InformationHandle(_world.Buildings.Rows, building), InformationHandle(_world.Roads.Segments.Rows, road));
        return _picked;

        void Hit(InstanceLayer layer, List<ulong> ids, bool isRoad)
        {
            if (!layer.IsVisibleInTree() || layer.Multimesh.VisibleInstanceCount == 0) return;
            InstanceBuffer instances = layer.Multimesh;
            Mesh mesh = instances.Mesh;
            if (!_pickingFaces.TryGetValue(mesh, out Vector3[]? faces))
                _pickingFaces[mesh] = faces = mesh.GetFaces();
            Aabb bounds = mesh.GetAabb();
            foreach (var batch in instances.Batches)
            {
                Transform3D inverseLayer = layer.GlobalTransform.AffineInverse();
                if (!RayBounds(inverseLayer * origin, inverseLayer.Basis * direction, batch.Bounds, distance)) continue;
                foreach (var entry in batch.Instances)
                {
                int i = entry.Index;
                Transform3D transform = layer.GlobalTransform * entry.Transform;
                if (Math.Abs(transform.Basis.Determinant()) < .000001f) continue;
                Transform3D inverse = transform.AffineInverse();
                Vector3 from = inverse * origin, along = inverse.Basis * direction;
                if (!RayBounds(from, along, bounds, distance)) continue;
                for (int face = 0; face + 2 < faces.Length; face += 3)
                {
                    Variant hit = Geometry3D.RayIntersectsTriangle(from, along, faces[face], faces[face + 1], faces[face + 2]);
                    if (hit.VariantType != Variant.Type.Vector3) continue;
                    float depth = ((transform * hit.AsVector3()) - origin).Dot(direction);
                    if (depth < 0 || depth > distance + .001f) continue;
                    // Shared junction surfaces have a stable tie; a Building wins a coplanar tie.
                    if (Math.Abs(depth - distance) <= .001f && (building != 0 || isRoad && road != 0 && instances.IdAt(i) >= road)) continue;
                    distance = depth;
                    building = isRoad ? 0 : instances.IdAt(i);
                    road = isRoad ? instances.IdAt(i) : 0;
                }
                }
            }
        }
    }

    private static bool RayBounds(Vector3 origin, Vector3 direction, Aabb bounds, float far)
    {
        float near = 0;
        for (int axis = 0; axis < 3; axis++)
        {
            if (Math.Abs(direction[axis]) < .000001f)
            {
                if (origin[axis] < bounds.Position[axis] || origin[axis] > bounds.End[axis]) return false;
                continue;
            }
            float a = (bounds.Position[axis] - origin[axis]) / direction[axis];
            float b = (bounds.End[axis] - origin[axis]) / direction[axis];
            near = Math.Max(near, Math.Min(a, b));
            far = Math.Min(far, Math.Max(a, b));
            if (near > far) return false;
        }
        return true;
    }

    private void SelectInformation(Handle<Building> building, Handle<RoadSegment> road, (Tiles East, Tiles North)? ground = null)
    {
        _healthInspection = false;
        if (building == _selectedBuilding && road == _selectedRoad && (!building.IsNone || !road.IsNone)
            && _selectedHousehold.IsNone) return;
        _selectedBuilding = building;
        _selectedRoad = road;
        _selectedHousehold = default;
        _roadParent = default;
        _selectedGround = ground ?? Aim() ?? (new Tiles(0), new Tiles(0));
        _expanded.Clear();
        _buildingScroll = 0;
        _inspectionSignature = string.Empty;
        RefreshInspection(true);
        RestoreInspectionScroll(0);
        LayoutInformation();
    }

    private string RoadName(int slot) => (RoadKind)_world.Roads.Segments.Kind[slot] switch
    {
        RoadKind.FootPath => "Foot Path",
        RoadKind.Arterial => "Arterial",
        _ => "Street",
    };

    private static string RoadModes(byte modes) => (TravelMode)modes switch
    {
        TravelMode.Any => "Walking and driving",
        TravelMode.Foot => "Walking only",
        TravelMode.Car => "Driving only",
        _ => "Closed",
    };

    private string RoadSynopsis(int slot)
    {
        RoadSegmentTable roads = _world.Roads.Segments;
        bool cars = ((roads.ModesForward[slot] | roads.ModesBackward[slot]) & (byte)TravelMode.Car) != 0;
        int present = roads.VolumeForward[slot] + roads.VolumeBackward[slot];
        return $"road Segment · {RoadName(slot)} · {roads.LengthTiles[slot].Raw * MetresPerTile:N0} m · "
            + (cars ? $"{present:N0} {(present == 1 ? "Vehicle" : "Vehicles")} present" : "No driving permitted")
            + " — click for directions and connections";
    }

    private void RoadInformation(List<InformationSection> sections, out string title, out string identity)
    {
        RoadSegmentTable roads = _world.Roads.Segments;
        RoadNodeTable nodes = _world.Roads.Nodes;
        identity = "ROAD SEGMENT";
        if (!roads.Rows.TryResolve(_selectedRoad, out int slot))
        {
            title = "Road no longer exists";
            return;
        }
        title = RoadName(slot);
        identity += $" {roads.Rows.IdAt(slot)}";
        sections.Add(new("summary", "Current condition", true,
            [new($"{roads.LengthTiles[slot].Raw * MetresPerTile:N0} m · {RoadModes((byte)(roads.ModesForward[slot] | roads.ModesBackward[slot]))}")]));
        var travel = new List<InformationRow>();
        Direction(true);
        Direction(false);
        sections.Add(new("travel", "Travel conditions", true, travel));
        var connections = new List<InformationRow>();
        Endpoint(roads.NodeA[slot], "A");
        Endpoint(roads.NodeB[slot], "B");
        sections.Add(new("connections", "Endpoint connections", false, connections));
        var frontage = new List<InformationRow>();
        int lots = 0, vacant = 0;
        for (int lot = 0; lot < _world.Lots.Rows.SlotCount; lot++)
        {
            if (!_world.Lots.Rows.IsLive(lot)) continue;
            Address address = _world.Lots.AddressOf(lot);
            if (!address.Exists || address.Segment != slot) continue;
            lots++;
            int building = _world.Lots.BuildingOn(lot);
            if (building == Rows.NoSlot || !_world.Buildings.Rows.IsLive(building)) { vacant++; continue; }
            ulong id = _world.Buildings.Rows.IdAt(building);
            frontage.Add(new($"Building {id} · {_names.Kind(_world.Buildings.Kind[building]) ?? "Building"}", $"frontage {id}"));
        }
        frontage.Insert(0, new($"{lots} frontage Lots · {vacant} vacant"));
        sections.Add(new("frontage", "Frontage", false, frontage));

        void Direction(bool forward)
        {
            byte modes = forward ? roads.ModesForward[slot] : roads.ModesBackward[slot];
            string line = $"{(forward ? "A → B" : "B → A")} · {RoadModes(modes)}";
            if ((modes & (byte)TravelMode.Car) != 0)
            {
                int volume = forward ? roads.VolumeForward[slot] : roads.VolumeBackward[slot];
                TravelTime free = roads.FreeFlowOver(slot);
                TravelTime entry = _world.Rules.Traffic.Apply(free, roads.LoadOf(slot, volume + 1, free));
                line += $"\n{volume:N0} Vehicles present\nDriving: {RoadSeconds(entry)} on next entry\nFree flow: {RoadSeconds(free)}";
            }
            if ((modes & (byte)TravelMode.Foot) != 0
                && nodes.Rows.TryResolve(forward ? roads.NodeA[slot] : roads.NodeB[slot], out int node))
                for (int arc = nodes.ArcStart[node]; arc < nodes.ArcStart[node] + nodes.ArcCount[node]; arc++)
                    if (_world.Roads.Arcs.Segment[arc] == slot)
                    {
                        line += $"\nWalking: {RoadSeconds(_world.Roads.Arcs.FootTime[arc])}";
                        break;
                    }
            travel.Add(new(line));
        }

        void Endpoint(Handle<RoadNode> handle, string name)
        {
            if (!nodes.Rows.TryResolve(handle, out int node)) { connections.Add(new($"{name} · endpoint unavailable")); return; }
            connections.Add(new($"{name} · Tile {nodes.East[node].Raw}, {nodes.North[node].Raw}"));
            int count = 0;
            var seen = new HashSet<int>();
            for (int arc = nodes.ArcStart[node]; arc < nodes.ArcStart[node] + nodes.ArcCount[node]; arc++)
            {
                int other = _world.Roads.Arcs.Segment[arc];
                if (other == slot || !roads.Rows.IsLive(other) || !seen.Add(other)) continue;
                count++;
                ulong id = roads.Rows.IdAt(other);
                connections.Add(new($"{RoadName(other)} {id} · {RoadModes(_world.Roads.Arcs.Modes[arc])}", $"road {id}"));
            }
            if (count == 0) connections.Add(new("No onward connections at this endpoint."));
        }
    }

    private static string RoadSeconds(TravelTime time) => time.IsImpassable ? "impassable"
        : $"{time.Raw / 65536.0 * 86400 / Ticks.PerDay:0.#} s";

    private object[] InformationMapTargets()
    {
        var targets = new List<object>();
        var roadNames = new Dictionary<ulong, string>();
        Rows<RoadSegment> roads = _world.Roads.Segments.Rows;
        for (int i = 0; i < roads.SlotCount; i++)
            if (roads.IsLive(i)) roadNames.Add(roads.IdAt(i), RoadName(i));
        Add(_roads, _roadIds, "road", Vector3.Zero);
        Add(_buildings, _buildingIds, "building", new Vector3(0, .5f, 0));
        return targets.ToArray();

        void Add(InstanceLayer layer, List<ulong> ids, string kind, Vector3 local)
        {
            for (int i = 0; i < layer.Multimesh.VisibleInstanceCount; i++)
            {
                Vector3 point = layer.GlobalTransform * (layer.Multimesh.GetInstanceTransform(i) * local);
                if (_camera.IsPositionBehind(point)) continue;
                Vector2 pixel = _camera.UnprojectPosition(point);
                string name = kind == "road" ? roadNames.GetValueOrDefault(layer.Multimesh.IdAt(i), kind) : kind;
                targets.Add(new { Kind = kind, Name = name, Id = layer.Multimesh.IdAt(i), X = pixel.X, Y = pixel.Y,
                    East = point.X / MetresPerTile, North = -point.Z / MetresPerTile });
            }
        }
    }

    private string RoadDebug()
    {
        var picked = PickInformation();
        if (!_world.Roads.Segments.Rows.TryResolve(picked.Road, out int slot)) return string.Empty;
        RoadSegmentTable roads = _world.Roads.Segments;
        return $"\nSegment {roads.Rows.IdAt(slot)} · endpoints {RowId(_world.Roads.Nodes.Rows, roads.NodeA[slot])} → {RowId(_world.Roads.Nodes.Rows, roads.NodeB[slot])}"
            + $"\nepoch {roads.Epoch[slot]} · modes {roads.ModesForward[slot]}/{roads.ModesBackward[slot]}"
            + $"\nfree-flow raw {roads.FreeFlow[slot].Raw} · capacity {roads.CapacityPerDay[slot]} Vehicles/Day";
    }

    private void MarkRoad(ref MeshInstance3D? marker, Handle<RoadSegment> handle, bool selected)
    {
        if (marker is not null) marker.Visible = false;
        if (_photographing || !_roads.IsVisibleInTree() || !_world.Roads.Segments.Rows.TryResolve(handle, out int slot)) return;
        RoadSegmentTable roads = _world.Roads.Segments;
        RoadNodeTable nodes = _world.Roads.Nodes;
        if (!nodes.Rows.TryResolve(roads.NodeA[slot], out int a) || !nodes.Rows.TryResolve(roads.NodeB[slot], out int b)) return;
        if (marker is null)
        {
            marker = new MeshInstance3D { Mesh = new BoxMesh(), CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                MaterialOverride = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha, AlbedoColor = selected ? new Color(.45f, .88f, .74f, .4f) : new Color(1, .95f, .8f, .25f) } };
            AddChild(marker);
        }
        Vector3 from = new(nodes.East[a].Raw * MetresPerTile, .12f, -nodes.North[a].Raw * MetresPerTile);
        Vector3 to = new(nodes.East[b].Raw * MetresPerTile, .12f, -nodes.North[b].Raw * MetresPerTile);
        float width = (RoadKind)roads.Kind[slot] switch { RoadKind.Arterial => ArterialWidthMetres, RoadKind.FootPath => FootPathWidthMetres, _ => CarriagewayWidthMetres };
        marker.Transform = new Transform3D(new Basis(Vector3.Up, Mathf.Atan2(to.X - from.X, to.Z - from.Z))
            * Basis.FromScale(new Vector3(width, .04f, Math.Max(.01f, from.DistanceTo(to)))), from.Lerp(to, .5f));
        marker.Visible = true;
    }
}

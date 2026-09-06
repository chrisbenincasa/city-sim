using System;
using System.Collections.Generic;
using Borough.Core.Entities;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Godot;

namespace Borough.Shell;

public partial class Main
{
    private readonly record struct MovingBounds(int Slot, Aabb Bounds);
    private readonly Dictionary<Vector2I, List<MovingBounds>> _movingChunks = [];
    private readonly List<MovingBounds> _wideMovers = [];
    private readonly HashSet<int> _movingSeen = [];
    private World? _movingWorld;
    private ulong _movingTick = ulong.MaxValue;
    private int _movingTotal;
    private (World? World, ulong Tick, int Alpha, Transform3D Camera, float Fov, Vector2 Viewport) _movementDrawKey;

    private static Vector3 MovingPoint(VisibleAgent person) =>
        new(person.East.Raw * MetresPerTile / 65536f, .06f, -person.North.Raw * MetresPerTile / 65536f);

    private void IndexMovement()
    {
        if (_movingWorld == _world && _movingTick == _world.Tick.Raw) return;
        _movingWorld = _world;
        _movingTick = _world.Tick.Raw;
        _movingTotal = 0;
        // Reuse bucket lists; discard keys after this snapshot so a moving population has no trail of empty buckets.
        foreach (var bucket in _movingChunks.Values) bucket.Clear();
        _wideMovers.Clear();
        var travellers = _world.Travellers;
        for (int slot = 0; slot < travellers.Rows.SlotCount; slot++)
        {
            _movementIndexVisits++;
            if (!VisibleAgents.TryGet(_world, slot, Ratio.Zero, out var person)) continue;
            _movingTotal++;
            Vector3 start = MovingPoint(person);
            int leg = travellers.CurrentLeg[slot];
            float length = 0;
            bool broken = false;
            if (person.Mode == TravelMode.Foot || travellers.CurrentHop[slot] == Borough.Core.Tables.Rows.NoSlot)
                foreach (int hop in _world.Legs.Route(_world.RouteHops).Walk(leg))
                    if (_world.Roads.Segments.Rows.TryResolve(_world.RouteHops.Segment[hop], out int segment))
                        length += _world.Roads.Segments.LengthTiles[segment].Raw * MetresPerTile;
                    else broken = true;
            int time = _world.Legs.Time[leg].Raw;
            // Route distance bounds every bend during this Tick, whereas interpolating endpoints could cut a corner.
            float reach = time > 0 ? Math.Min(length, length * 65536f / time) : length;
            Vector3 end = VisibleAgents.TryGet(_world, slot, Ratio.One, out var next) ? MovingPoint(next) : start;
            reach = Math.Max(reach, start.DistanceTo(end)) + 8f;
            var bounds = new Aabb(start - new Vector3(reach, 8, reach), new Vector3(reach * 2, 16, reach * 2));
            if (broken)
            {
                // TryAlongRoute skips removed hops. The resulting jump need not fit a walking-distance bound.
                var segments = _world.Roads.Segments;
                var nodes = _world.Roads.Nodes;
                foreach (int hop in _world.Legs.Route(_world.RouteHops).Walk(leg))
                {
                    if (!segments.Rows.TryResolve(_world.RouteHops.Segment[hop], out int segment)) continue;
                    if (nodes.Rows.TryResolve(segments.NodeA[segment], out int a))
                        bounds = bounds.Expand(new Vector3(nodes.East[a].Raw * MetresPerTile, .06f, -nodes.North[a].Raw * MetresPerTile));
                    if (nodes.Rows.TryResolve(segments.NodeB[segment], out int b))
                        bounds = bounds.Expand(new Vector3(nodes.East[b].Raw * MetresPerTile, .06f, -nodes.North[b].Raw * MetresPerTile));
                }
                bounds = bounds.Grow(8f);
            }
            var moving = new MovingBounds(slot, bounds);
            if (broken || reach > InstanceLayer.ChunkMetres) { _wideMovers.Add(moving); continue; }
            int left = Mathf.FloorToInt(bounds.Position.X / InstanceLayer.ChunkMetres);
            int right = Mathf.FloorToInt(bounds.End.X / InstanceLayer.ChunkMetres);
            int bottom = Mathf.FloorToInt(bounds.Position.Z / InstanceLayer.ChunkMetres);
            int top = Mathf.FloorToInt(bounds.End.Z / InstanceLayer.ChunkMetres);
            for (int z = bottom; z <= top; z++)
            for (int x = left; x <= right; x++)
            {
                var key = new Vector2I(x, z);
                if (!_movingChunks.TryGetValue(key, out var bucket)) _movingChunks.Add(key, bucket = []);
                bucket.Add(moving);
            }
        }
        var empty = new List<Vector2I>();
        foreach (var pair in _movingChunks) if (pair.Value.Count == 0) empty.Add(pair.Key);
        foreach (var key in empty) _movingChunks.Remove(key);
    }

    private int DrawMovement(Ratio alpha)
    {
        var key = (_world, _world.Tick.Raw, alpha.Raw, _camera.GlobalTransform, _camera.Fov, GetViewport().GetVisibleRect().Size);
        if (_movementDrawKey == key) return _movingTotal;
        _movementDrawKey = key;
        IndexMovement();
        var planes = _camera.GetFrustum();
        Vector3 eye = _camera.GlobalPosition;
        Vector3 inside = _camera.GlobalPosition - _camera.GlobalBasis.Z * ((_camera.Near + _camera.Far) * .5f);
        _movingSeen.Clear();
        int written = 0;
        foreach (var pair in _movingChunks)
        {
            var chunk = new Aabb(new Vector3(pair.Key.X * InstanceLayer.ChunkMetres, -8, pair.Key.Y * InstanceLayer.ChunkMetres),
                new Vector3(InstanceLayer.ChunkMetres, 16, InstanceLayer.ChunkMetres));
            if (!IntersectsView(chunk, planes, inside)) continue;
            foreach (var moving in pair.Value) Consider(moving);
        }
        foreach (var moving in _wideMovers) Consider(moving);
        if (_verifyRendering)
        {
            for (int slot = 0; slot < _world.Travellers.Rows.SlotCount; slot++)
            {
                if (!VisibleAgents.TryGet(_world, slot, alpha, out var person)) continue;
                Vector3 point = MovingPoint(person);
                float distance = person.Mode == TravelMode.Car ? _cars.DetailDistance : _travellers.DetailDistance;
                if (_camera.IsPositionInFrustum(point) && (distance <= 0 || eye.DistanceTo(point) < distance)
                    && !_movingSeen.Contains(slot)) throw new InvalidOperationException("Spatial movement query omitted a visible Traveller.");
            }
        }
        Fill(_travellers, Travellers(written, false), _travellerIds);
        Fill(_cars, Travellers(written, true), _carIds);
        return _movingTotal;

        void Consider(MovingBounds moving)
        {
            float distance = (TravelMode)_world.Legs.Mode[_world.Travellers.CurrentLeg[moving.Slot]] == TravelMode.Car
                ? _cars.DetailDistance : _travellers.DetailDistance;
            if (distance > 0 && eye.DistanceSquaredTo(eye.Clamp(moving.Bounds.Position, moving.Bounds.End)) > distance * distance) return;
            if (!_movingSeen.Add(moving.Slot) || !IntersectsView(moving.Bounds, planes, inside)) return;
            _movementQueries++;
            if (!VisibleAgents.TryGet(_world, moving.Slot, alpha, out var person)) return;
            if (written == _agents.Length) Array.Resize(ref _agents, Math.Max(16, _agents.Length * 2));
            _agents[written++] = person;
        }
    }

    private static bool IntersectsView(Aabb bounds, Godot.Collections.Array<Plane> planes, Vector3 inside)
    {
        Vector3 half = bounds.Size * .5f;
        Vector3 centre = bounds.Position + half;
        foreach (Plane plane in planes)
        {
            float radius = plane.Normal.Abs().Dot(half);
            float side = plane.DistanceTo(centre);
            if (plane.DistanceTo(inside) >= 0 ? side < -radius : side > radius) return false;
        }
        return true;
    }
}

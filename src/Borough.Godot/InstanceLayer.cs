using System;
using System.Collections.Generic;
using Godot;

namespace Borough.Shell;

// Disposable shell storage. An entity/component key survives enumeration order changes.
public partial class InstanceLayer : Node3D
{
    // PROVISIONAL: eight Cells per Chunk; never a limit on the world or on a batch.
    public const float ChunkMetres = 1024f;
    public float DetailDistance { get; set; }
    public InstanceBuffer Multimesh { get; }
    public InstanceLayer() => Multimesh = new InstanceBuffer(this);
    private Material? _material;
    private GeometryInstance3D.ShadowCastingSetting _shadows = GeometryInstance3D.ShadowCastingSetting.On;
    public Material? MaterialOverride
    {
        get => _material;
        set { _material = value; foreach (var batch in Multimesh.Batches) batch.Node.MaterialOverride = value; }
    }
    public GeometryInstance3D.ShadowCastingSetting CastShadow
    {
        get => _shadows;
        set { _shadows = value; foreach (var batch in Multimesh.Batches) batch.Node.CastShadow = value; }
    }
    internal MultiMeshInstance3D CreateBatch(MultiMesh mesh)
    {
        var node = new MultiMeshInstance3D { Multimesh = mesh, MaterialOverride = _material, CastShadow = _shadows };
        AddChild(node);
        return node;
    }
}

public readonly record struct InstanceValue(Transform3D Transform, Color Colour, Color Custom = default);

public sealed class InstanceBuffer
{
    public sealed class Entry
    {
        internal (ulong Id, int Part) Key;
        internal Batch Batch = null!;
        internal int Slot;
        internal ulong Seen;
        public int Index { get; internal set; }
        public Transform3D Transform { get; internal set; }
        public Color Colour { get; internal set; } = Colors.White;
        public Color Custom { get; internal set; }
    }
    public sealed class Batch
    {
        internal Vector2I Key;
        internal readonly List<Entry> Entries = [];
        internal float[] Buffer = [];
        internal bool BoundsDirty = true;
        public MultiMeshInstance3D Node { get; internal set; } = null!;
        public bool Resident { get; internal set; } = true;
        public Aabb Bounds { get; internal set; }
        public IReadOnlyList<Entry> Instances => Entries;
    }
    private readonly InstanceLayer _owner;
    private readonly Dictionary<Vector2I, Batch> _batches = [];
    private readonly Dictionary<(ulong Id, int Part), Entry> _entries = [];
    private readonly Dictionary<ulong, int> _parts = [];
    private readonly HashSet<Batch> _dirty = [];
    private List<Entry> _order = [], _previous = [];
    private ulong _generation;
    private bool _writing;
    private Vector3? _eye;
    private float _detailDistance;
    public Mesh Mesh { get; set; } = null!;
    public bool UseColors { get; set; }
    public bool UseCustomData { get; set; }
    public IEnumerable<Batch> Batches => _batches.Values;
    public int PendingBatches => _dirty.Count;
    public int BatchCount => _batches.Count;
    public int ResidentInstances { get; private set; }
    public int ResidentBatches { get; private set; }
    public int InstanceCount { get; private set; }
    public long Uploads { get; private set; }
    public long UploadedInstances { get; private set; }
    public long UploadedBytes { get; private set; }
    public double UploadMilliseconds { get; private set; }
    public int VisibleInstanceCount
    {
        get => _order.Count;
        set => Finish(value);
    }
    internal InstanceBuffer(InstanceLayer owner) => _owner = owner;
    private void Begin()
    {
        (_order, _previous) = (_previous, _order);
        _order.Clear();
        _parts.Clear();
        _generation++;
        _writing = true;
    }
    public void Identity(int index, ulong id)
    {
        if (!_writing) Begin();
        int part = id == 0 ? index : _parts.GetValueOrDefault(id);
        if (id != 0) _parts[id] = part + 1;
        Acquire(index, (id, part));
    }
    private Entry Acquire(int index, (ulong Id, int Part) key)
    {
        if (index != _order.Count) throw new InvalidOperationException("Instances must be written consecutively.");
        if (!_entries.TryGetValue(key, out var entry))
        {
            entry = new Entry { Key = key };
            _entries.Add(key, entry);
        }
        entry.Seen = _generation;
        entry.Index = index;
        _order.Add(entry);
        return entry;
    }
    private Entry Writing(int index)
    {
        if (!_writing) Begin();
        return index < _order.Count ? _order[index] : Acquire(index, (0, index));
    }
    public void SetInstanceTransform(int index, Transform3D transform)
    {
        SetTransform(Writing(index), transform);
    }
    private void SetTransform(Entry entry, Transform3D transform)
    {
        var key = new Vector2I(Mathf.FloorToInt(transform.Origin.X / InstanceLayer.ChunkMetres),
            Mathf.FloorToInt(transform.Origin.Z / InstanceLayer.ChunkMetres));
        if (entry.Batch is null || entry.Batch.Key != key)
        {
            if (entry.Batch is not null) Remove(entry);
            if (!_batches.TryGetValue(key, out var batch))
            {
                var mesh = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
                    UseColors = UseColors, UseCustomData = UseCustomData, Mesh = Mesh };
                batch = new Batch { Key = key, Node = _owner.CreateBatch(mesh) };
                _batches.Add(key, batch);
            }
            entry.Batch = batch;
            entry.Slot = batch.Entries.Count;
            batch.Entries.Add(entry);
            batch.BoundsDirty = true;
            _dirty.Add(batch);
        }
        if (entry.Transform != transform) { entry.Transform = transform; entry.Batch.BoundsDirty = true; _dirty.Add(entry.Batch); }
    }
    public void SetInstanceColor(int index, Color colour)
    {
        Entry entry = Writing(index);
        if (entry.Colour != colour) { entry.Colour = colour; _dirty.Add(entry.Batch); }
    }
    public void SetInstanceCustomData(int index, Color custom)
    {
        Entry entry = Writing(index);
        if (entry.Custom != custom) { entry.Custom = custom; _dirty.Add(entry.Batch); }
    }
    private void Remove(Entry entry)
    {
        Batch batch = entry.Batch;
        Entry last = batch.Entries[^1];
        batch.Entries[entry.Slot] = last;
        last.Slot = entry.Slot;
        batch.Entries.RemoveAt(batch.Entries.Count - 1);
        batch.BoundsDirty = true;
        _dirty.Add(batch);
    }
    private void Finish(int count)
    {
        if (!_writing) Begin();
        if (count != _order.Count) throw new InvalidOperationException("Instance count disagrees with writes.");
        foreach (Entry old in _previous)
        {
            if (old.Seen == _generation) continue;
            Remove(old);
            _entries.Remove(old.Key);
        }
        _previous.Clear();
        _writing = false;
    }
    public bool Replace(ulong id, IReadOnlyList<InstanceValue> values)
    {
        if (_writing || id == 0) throw new InvalidOperationException("Entity edits require a completed layer and a nonzero id.");
        bool geometry = false;
        for (int part = 0; part < values.Count; part++)
        {
            var key = (id, part);
            InstanceValue value = values[part];
            if (!_entries.TryGetValue(key, out Entry? entry))
            {
                entry = new Entry { Key = key, Index = _order.Count };
                _entries.Add(key, entry);
                _order.Add(entry);
                geometry = true;
            }
            geometry |= entry.Transform != value.Transform;
            SetTransform(entry, value.Transform);
            if (entry.Colour != value.Colour || entry.Custom != value.Custom)
            {
                entry.Colour = value.Colour;
                entry.Custom = value.Custom;
                _dirty.Add(entry.Batch);
            }
        }
        for (int part = values.Count; _entries.Remove((id, part), out Entry? entry); part++)
        {
            Remove(entry);
            Entry last = _order[^1];
            _order[entry.Index] = last;
            last.Index = entry.Index;
            _order.RemoveAt(_order.Count - 1);
            geometry = true;
        }
        return geometry;
    }

    public Dictionary<(ulong Id, int Part), InstanceValue> Snapshot()
    {
        var snapshot = new Dictionary<(ulong, int), InstanceValue>(_order.Count);
        foreach (Entry entry in _order) snapshot.Add(entry.Key, new(entry.Transform, entry.Colour, entry.Custom));
        return snapshot;
    }
    public bool IsResident(int index) => _order[index].Batch.Resident && _order[index].Slot < _order[index].Batch.Node.Multimesh.InstanceCount;
    public ulong IdAt(int index) => _order[index].Key.Id;
    public Transform3D GetInstanceTransform(int index) => _order[index].Transform;
    public Color GetInstanceColor(int index) => _order[index].Colour;
    public Transform3D UploadedTransform(int index)
    {
        Entry entry = _order[index];
        return entry.Batch.Node.Multimesh.GetInstanceTransform(entry.Slot);
    }
    public Color UploadedColour(int index)
    {
        Entry entry = _order[index];
        return entry.Batch.Node.Multimesh.GetInstanceColor(entry.Slot);
    }
    public void Flush(Vector3? eye = null, float detailDistance = 0, long maxBytes = long.MaxValue, bool allowOversize = true)
    {
        if (eye != _eye || detailDistance != _detailDistance)
        {
            bool partitioned = detailDistance > 0 || _detailDistance > 0;
            _eye = eye;
            _detailDistance = detailDistance;
            if (partitioned)
            foreach (Batch batch in _batches.Values)
            {
                bool resident = Wants(batch.Bounds, batch.Resident);
                if (resident != batch.Resident) _dirty.Add(batch);
            }
        }
        if (_dirty.Count == 0) return;
        long start = System.Diagnostics.Stopwatch.GetTimestamp();
        int stride = 12 + (UseColors ? 4 : 0) + (UseCustomData ? 4 : 0);
        Aabb meshBounds = Mesh.GetAabb();
        var pending = new List<Batch>(_dirty);
        if (eye is { } camera)
            pending.Sort((a, b) => camera.DistanceSquaredTo(a.BoundsDirty && a.Entries.Count > 0 ? a.Entries[0].Transform.Origin : a.Bounds.GetCenter())
                .CompareTo(camera.DistanceSquaredTo(b.BoundsDirty && b.Entries.Count > 0 ? b.Entries[0].Transform.Origin : b.Bounds.GetCenter())));
        long used = 0;
        foreach (Batch batch in pending)
        {
            MultiMesh mesh = batch.Node.Multimesh;
            int count = batch.Entries.Count;
            if (count == 0)
            {
                InstanceCount -= mesh.InstanceCount;
                _batches.Remove(batch.Key);
                batch.Node.QueueFree();
                _dirty.Remove(batch);
                continue;
            }
            if (batch.BoundsDirty)
            {
                Aabb changed = batch.Entries[0].Transform * meshBounds;
                foreach (Entry entry in batch.Entries) changed = changed.Merge(entry.Transform * meshBounds);
                batch.Bounds = changed;
                batch.BoundsDirty = false;
            }
            Aabb bounds = batch.Bounds;
            batch.Resident = Wants(bounds, batch.Resident);
            if (!batch.Resident)
            {
                InstanceCount -= mesh.InstanceCount;
                mesh.InstanceCount = 0;
                batch.Buffer = [];
                _dirty.Remove(batch);
                continue;
            }
            int capacity = mesh.InstanceCount;
            int wanted = capacity;
            if (capacity < count || capacity > Math.Max(32, count * 4))
            {
                wanted = 16;
                while (wanted < count) wanted = checked(wanted * 2);
            }
            long bytes = (long)wanted * stride * sizeof(float);
            if (bytes > maxBytes - used && !(allowOversize && used == 0 && maxBytes > 0)) continue;
            if (capacity < count || capacity > Math.Max(32, count * 4))
            {
                int next = 16;
                while (next < count) next = checked(next * 2);
                InstanceCount += next - capacity;
                mesh.InstanceCount = next;
                batch.Buffer = new float[checked(next * stride)];
            }
            for (int i = 0; i < count; i++)
            {
                Entry entry = batch.Entries[i];
                Transform3D t = entry.Transform;
                int at = i * stride;
                float[] b = batch.Buffer;
                b[at++] = t.Basis.X.X; b[at++] = t.Basis.Y.X; b[at++] = t.Basis.Z.X; b[at++] = t.Origin.X;
                b[at++] = t.Basis.X.Y; b[at++] = t.Basis.Y.Y; b[at++] = t.Basis.Z.Y; b[at++] = t.Origin.Y;
                b[at++] = t.Basis.X.Z; b[at++] = t.Basis.Y.Z; b[at++] = t.Basis.Z.Z; b[at++] = t.Origin.Z;
                if (UseColors) Put(b, ref at, entry.Colour);
                if (UseCustomData) Put(b, ref at, entry.Custom);
            }
            batch.Bounds = bounds;
            mesh.CustomAabb = bounds;
            mesh.Buffer = batch.Buffer;
            mesh.VisibleInstanceCount = count;
            Uploads++;
            UploadedInstances += count;
            UploadedBytes += batch.Buffer.Length * sizeof(float);
            used += bytes;
            _dirty.Remove(batch);
        }
        ResidentInstances = 0;
        ResidentBatches = 0;
        foreach (Batch batch in _batches.Values)
        {
            if (!batch.Resident || batch.Node.Multimesh.InstanceCount == 0) continue;
            ResidentBatches++;
            ResidentInstances += batch.Node.Multimesh.VisibleInstanceCount;
        }
        UploadMilliseconds += System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;
    }
    private bool Wants(Aabb bounds, bool resident)
    {
        if (_eye is not { } eye || _detailDistance <= 0) return true;
        Vector3 nearest = eye.Clamp(bounds.Position, bounds.End);
        // Hysteresis prevents reallocating buffers while the camera hovers at a detail boundary.
        float distance = _detailDistance * (resident ? 1.15f : 1f);
        return eye.DistanceSquaredTo(nearest) <= distance * distance;
    }

    private static void Put(float[] buffer, ref int at, Color value)
    {
        buffer[at++] = value.R; buffer[at++] = value.G; buffer[at++] = value.B; buffer[at++] = value.A;
    }
}

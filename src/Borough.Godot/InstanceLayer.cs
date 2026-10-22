using System;
using System.Collections.Generic;
using Godot;

namespace Borough.Shell;

// Disposable shell storage. An entity/component key survives enumeration order changes.
public partial class InstanceLayer : Node3D
{
    // Eight Cells per Chunk; never a limit on the world or on a batch. Each chunk is one draw per
    // layer, so smaller chunks cost CPU per draw at a whole-city camera (plans/0066).
    public static readonly float ChunkMetres = ComparisonChunkMetres();

    private static float ComparisonChunkMetres()
    {
        if (System.Environment.GetEnvironmentVariable("BOROUGH_RENDER_PROFILE") is null) return 1024f;
        return System.Environment.GetEnvironmentVariable("BOROUGH_RENDER_CHUNK_METRES") switch
        {
            null or "1024" => 1024f,
            "256" => 256f,
            "512" => 512f,
            _ => throw new ArgumentException("BOROUGH_RENDER_CHUNK_METRES must be 256, 512 or 1024."),
        };
    }
    public float DetailDistance { get; set; }

    /// <summary>
    /// Whether the layer holds only instances its camera could draw. A producer calls
    /// <see cref="BuildFrom"/> before writing, skips what <see cref="Reaches"/> rejects, and writes
    /// the layer again once <see cref="Stale"/> says the camera has left the slack it built with.
    /// </summary>
    public bool Gated { get; set; }
    private bool _gating;
    private Vector3? _builtFrom;
    private float _builtDistance;
    private float Slack => DetailDistance * 0.25f;
    private float Reach => DetailDistance * InstanceBuffer.Hysteresis + Slack;

    /// <summary>Starts a build from the camera's position. Before there is a camera a gated build is empty.</summary>
    public void BuildFrom()
    {
        _builtDistance = DetailDistance;
        _gating = Gated && DetailDistance > 0;
        _builtFrom = _gating && IsInsideTree() ? GetViewport().GetCamera3D()?.GlobalPosition : null;
    }

    /// <summary>Whether the build's camera could draw something within a cube of half-size <paramref name="extent"/>.</summary>
    public bool Reaches(Vector3 point, float extent = 32f)
    {
        if (!_gating) return true;
        if (_builtFrom is not { } eye) return false;
        float x = Gap(point.X - eye.X, extent), y = Gap(point.Y - eye.Y, extent), z = Gap(point.Z - eye.Z, extent);
        float reach = Reach;
        return x * x + y * y + z * z <= reach * reach;

        static float Gap(float offset, float extent) => MathF.Max(0f, MathF.Abs(offset) - extent);
    }

    /// <summary>Whether the build's camera could draw a unit box, or a module of up to 32 m, so placed.</summary>
    public bool Reaches(in Transform3D where)
    {
        if (!_gating) return true;

        // Millions of garden pieces pass through here per rebuild, and Godot's Basis column and
        // vector methods are not inlined. The sum of every component's magnitude bounds the sum of
        // the column lengths without a root.
        ref readonly Basis b = ref where.Basis;
        float columns = MathF.Abs(b.Row0.X) + MathF.Abs(b.Row0.Y) + MathF.Abs(b.Row0.Z)
            + MathF.Abs(b.Row1.X) + MathF.Abs(b.Row1.Y) + MathF.Abs(b.Row1.Z)
            + MathF.Abs(b.Row2.X) + MathF.Abs(b.Row2.Y) + MathF.Abs(b.Row2.Z);
        return Reaches(where.Origin, MathF.Max(32f, 0.5f * columns));
    }

    /// <summary>
    /// The ground, as east and north metres, that holds every point <see cref="Reaches"/> accepts at
    /// <paramref name="extent"/>, or null for a whole build. It has no area when nothing reaches.
    /// </summary>
    public Rect2? Ground(float extent = 32f)
    {
        if (!_gating) return null;
        if (_builtFrom is not { } eye) return new Rect2();
        float height = Mathf.Max(0f, Mathf.Abs(eye.Y) - extent);
        if (height >= Reach) return new Rect2(eye.X, -eye.Z, 0f, 0f);
        float radius = Mathf.Sqrt(Reach * Reach - height * height) + extent;
        return new Rect2(eye.X - radius, -eye.Z - radius, radius * 2f, radius * 2f);
    }

    public bool Stale(Vector3 eye) =>
        Gated && DetailDistance > 0
            && (_builtFrom is not { } from || _builtDistance != DetailDistance || from.DistanceSquaredTo(eye) > Slack * Slack);

    /// <summary>
    /// Meshes drawn instead of the buffer's mesh, nearest band first. A chunk draws the first mesh
    /// whose distance reaches the nearest point of its bounds, and the buffer's mesh beyond them all.
    /// </summary>
    public IReadOnlyList<(float Within, Mesh Mesh)> Details { get; set; } = [];
    public InstanceBuffer Multimesh { get; }
    public InstanceLayer() => Multimesh = new InstanceBuffer(this);
    private Material? _material;
    private Material? _overlay;
    private GeometryInstance3D.ShadowCastingSetting _shadows = GeometryInstance3D.ShadowCastingSetting.On;
    public Material? MaterialOverride
    {
        get => _material;
        set { _material = value; foreach (var batch in Multimesh.Batches) batch.Node.MaterialOverride = value; }
    }
    public Material? MaterialOverlay
    {
        get => _overlay;
        set { _overlay = value; foreach (var batch in Multimesh.Batches) batch.Node.MaterialOverlay = value; }
    }

    /// <summary>Instance shader parameters every batch node carries.</summary>
    public Dictionary<StringName, Variant> InstanceParameters { get; } = [];
    public GeometryInstance3D.ShadowCastingSetting CastShadow
    {
        get => _shadows;
        set { _shadows = value; foreach (var batch in Multimesh.Batches) batch.Node.CastShadow = value; }
    }
    internal MultiMeshInstance3D CreateBatch(MultiMesh mesh)
    {
        var node = new MultiMeshInstance3D { Multimesh = mesh, MaterialOverride = _material, MaterialOverlay = _overlay, CastShadow = _shadows };
        foreach ((StringName name, Variant value) in InstanceParameters) node.SetInstanceShaderParameter(name, value);
        AddChild(node);
        return node;
    }
}

/// <param name="Far">Drawn only while its chunk is not <see cref="InstanceBuffer.Near"/>.</param>
public readonly record struct InstanceValue(Transform3D Transform, Color Colour, Color Custom = default, bool Far = false);

public sealed class InstanceBuffer
{
    /// <summary>How far past its detail distance a resident chunk stays, so a hovering camera does not churn buffers.</summary>
    internal const float Hysteresis = 1.15f;

    // Instances live in one array of plain structs, addressed by handle. Millions of per-instance
    // objects made every garbage collection during a city's first frame trace them all.
    private struct Entry
    {
        internal (ulong Id, int Part) Key;
        internal Batch? Batch;
        internal int Slot;
        internal ulong Seen;
        internal int Index;
        internal Transform3D Transform;
        internal Color Colour;
        internal Color Custom;
        internal bool Far;
    }
    public readonly record struct Instance(int Index, Transform3D Transform);
    public sealed class Batch
    {
        internal Vector2I Key;
        internal readonly List<int> Entries = [];
        internal InstanceBuffer Owner = null!;
        internal float[] Buffer = [];
        internal bool BoundsDirty = true;
        internal bool Near;

        // Mirrors of the MultiMesh's counts, which cost a native call to read.
        internal int Capacity;
        internal int Visible;
        public MultiMeshInstance3D Node { get; internal set; } = null!;
        public bool Resident { get; internal set; } = true;
        /// <summary>The index into <see cref="InstanceLayer.Details"/> drawn, or -1 for the buffer's mesh.</summary>
        public int Detail { get; internal set; } = -1;
        public Aabb Bounds { get; internal set; }
        public IEnumerable<Instance> Instances
        {
            get
            {
                for (int i = 0; i < Entries.Count; i++)
                {
                    Entry entry = Owner._data[Entries[i]];
                    yield return new Instance(entry.Index, entry.Transform);
                }
            }
        }
    }
    private readonly InstanceLayer _owner;
    private readonly Dictionary<Vector2I, Batch> _batches = [];
    private readonly Dictionary<(ulong Id, int Part), int> _entries = [];
    private readonly Dictionary<ulong, int> _parts = [];
    private readonly HashSet<Batch> _dirty = [];
    private Entry[] _data = new Entry[16];
    private int _dataCount;
    private readonly Stack<int> _free = [];
    private List<int> _order = [], _previous = [];
    private ulong _generation;
    private bool _writing;
    private Vector3? _eye;
    private float _detailDistance;
    public Mesh Mesh { get; set; } = null!;
    public bool UseColors { get; set; }
    public bool UseCustomData { get; set; }

    /// <summary>
    /// Whether a chunk is inside the near band. A <see cref="NearOnly"/> layer draws only near
    /// chunks; any other layer hides its <see cref="InstanceValue.Far"/> instances in near chunks.
    /// </summary>
    public Func<Vector2I, bool>? Near { get; set; }
    public bool NearOnly { get; set; }

    /// <summary>
    /// Whether a batch is still wanted, asked of the batch rather than of its chunk. A layer whose
    /// instances fade out one at a time uses it to drop a batch none of them is left in, and such a
    /// predicate reads <see cref="Batch.Instances"/> because the fade measures an instance's own
    /// origin. It runs once per batch per residency pass, so it costs one pass over the layer.
    /// </summary>
    public Func<Batch, bool>? Needed { get; set; }

    /// <summary>Re-evaluates every chunk's residency at the next flush, as a camera move does.</summary>
    public void Repartition() => _eye = null;
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
    private int Allocate((ulong Id, int Part) key, int index)
    {
        int handle;
        if (_free.Count > 0) handle = _free.Pop();
        else
        {
            if (_dataCount == _data.Length) Array.Resize(ref _data, _data.Length * 2);
            handle = _dataCount++;
        }
        _data[handle] = new Entry { Key = key, Index = index, Colour = Colors.White };
        return handle;
    }
    private void Release(int handle)
    {
        _data[handle] = default;
        _free.Push(handle);
    }
    private int Acquire(int index, (ulong Id, int Part) key)
    {
        if (index != _order.Count) throw new InvalidOperationException("Instances must be written consecutively.");
        if (!_entries.TryGetValue(key, out int handle))
        {
            handle = Allocate(key, index);
            _entries.Add(key, handle);
        }
        ref Entry entry = ref _data[handle];
        entry.Seen = _generation;
        entry.Index = index;
        _order.Add(handle);
        return handle;
    }
    private int Writing(int index)
    {
        if (!_writing) Begin();
        return index < _order.Count ? _order[index] : Acquire(index, (0, index));
    }
    public void SetInstanceTransform(int index, Transform3D transform)
    {
        SetTransform(Writing(index), transform);
    }
    private void SetTransform(int handle, Transform3D transform)
    {
        var key = new Vector2I(Mathf.FloorToInt(transform.Origin.X / InstanceLayer.ChunkMetres),
            Mathf.FloorToInt(transform.Origin.Z / InstanceLayer.ChunkMetres));
        Batch? current = _data[handle].Batch;
        if (current is null || current.Key != key)
        {
            if (current is not null) Remove(handle);
            if (!_batches.TryGetValue(key, out var batch))
            {
                var mesh = new MultiMesh
                {
                    TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
                    UseColors = UseColors,
                    UseCustomData = UseCustomData,
                    Mesh = Mesh
                };
                batch = new Batch { Key = key, Owner = this, Node = _owner.CreateBatch(mesh) };
                _batches.Add(key, batch);
            }
            ref Entry moved = ref _data[handle];
            moved.Batch = batch;
            moved.Slot = batch.Entries.Count;
            batch.Entries.Add(handle);
            batch.BoundsDirty = true;
            _dirty.Add(batch);
        }
        ref Entry entry = ref _data[handle];
        if (entry.Transform != transform) { entry.Transform = transform; entry.Batch!.BoundsDirty = true; _dirty.Add(entry.Batch); }
    }
    public void SetInstanceColor(int index, Color colour)
    {
        ref Entry entry = ref _data[Writing(index)];
        if (entry.Colour != colour) { entry.Colour = colour; _dirty.Add(entry.Batch!); }
    }
    public void SetInstanceCustomData(int index, Color custom)
    {
        ref Entry entry = ref _data[Writing(index)];
        if (entry.Custom != custom) { entry.Custom = custom; _dirty.Add(entry.Batch!); }
    }
    public void SetInstanceFar(int index, bool far)
    {
        ref Entry entry = ref _data[Writing(index)];
        if (entry.Far != far) { entry.Far = far; _dirty.Add(entry.Batch!); }
    }
    private void Remove(int handle)
    {
        Batch batch = _data[handle].Batch!;
        int slot = _data[handle].Slot;
        int last = batch.Entries[^1];
        batch.Entries[slot] = last;
        _data[last].Slot = slot;
        batch.Entries.RemoveAt(batch.Entries.Count - 1);
        batch.BoundsDirty = true;
        _dirty.Add(batch);
    }
    private void Finish(int count)
    {
        if (!_writing) Begin();
        if (count != _order.Count) throw new InvalidOperationException("Instance count disagrees with writes.");
        foreach (int old in _previous)
        {
            if (_data[old].Seen == _generation) continue;
            Remove(old);
            _entries.Remove(_data[old].Key);
            Release(old);
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
            if (!_entries.TryGetValue(key, out int handle))
            {
                handle = Allocate(key, _order.Count);
                _entries.Add(key, handle);
                _order.Add(handle);
                geometry = true;
            }
            geometry |= _data[handle].Transform != value.Transform;
            SetTransform(handle, value.Transform);
            ref Entry entry = ref _data[handle];
            if (entry.Colour != value.Colour || entry.Custom != value.Custom || entry.Far != value.Far)
            {
                entry.Colour = value.Colour;
                entry.Custom = value.Custom;
                entry.Far = value.Far;
                _dirty.Add(entry.Batch!);
            }
        }
        for (int part = values.Count; _entries.Remove((id, part), out int handle); part++)
        {
            Remove(handle);
            int index = _data[handle].Index;
            int last = _order[^1];
            _order[index] = last;
            _data[last].Index = index;
            _order.RemoveAt(_order.Count - 1);
            Release(handle);
            geometry = true;
        }
        return geometry;
    }

    public Dictionary<(ulong Id, int Part), InstanceValue> Snapshot()
    {
        var snapshot = new Dictionary<(ulong, int), InstanceValue>(_order.Count);
        foreach (int handle in _order)
        {
            ref Entry entry = ref _data[handle];
            snapshot.Add(entry.Key, new(entry.Transform, entry.Colour, entry.Custom, entry.Far));
        }
        return snapshot;
    }
    public bool IsResident(int index)
    {
        ref Entry entry = ref _data[_order[index]];
        return entry.Batch!.Resident && entry.Slot < entry.Batch.Capacity && !(entry.Far && entry.Batch.Near);
    }
    public ulong IdAt(int index) => _data[_order[index]].Key.Id;
    public Transform3D GetInstanceTransform(int index) => _data[_order[index]].Transform;
    public Color GetInstanceColor(int index) => _data[_order[index]].Colour;
    public Transform3D UploadedTransform(int index)
    {
        ref Entry entry = ref _data[_order[index]];
        return entry.Batch!.Node.Multimesh.GetInstanceTransform(entry.Slot);
    }
    public Color UploadedColour(int index)
    {
        ref Entry entry = ref _data[_order[index]];
        return entry.Batch!.Node.Multimesh.GetInstanceColor(entry.Slot);
    }
    public void Flush(Vector3? eye = null, float detailDistance = 0, long maxBytes = long.MaxValue, bool allowOversize = true)
    {
        if (eye != _eye || detailDistance != _detailDistance)
        {
            bool partitioned = detailDistance > 0 || _detailDistance > 0 || Near is not null || Needed is not null;
            _eye = eye;
            _detailDistance = detailDistance;
            if (partitioned)
                foreach (Batch batch in _batches.Values)
                {
                    if (Wants(batch) != batch.Resident || Hides(batch) != batch.Near) _dirty.Add(batch);
                }
            foreach (Batch batch in _batches.Values) Choose(batch);
        }
        if (_dirty.Count == 0) return;
        long start = System.Diagnostics.Stopwatch.GetTimestamp();
        int stride = 12 + (UseColors ? 4 : 0) + (UseCustomData ? 4 : 0);
        Aabb meshBounds = Mesh.GetAabb();
        var pending = new List<Batch>(_dirty);
        if (eye is { } camera)
            pending.Sort((a, b) => camera.DistanceSquaredTo(a.BoundsDirty && a.Entries.Count > 0 ? _data[a.Entries[0]].Transform.Origin : a.Bounds.GetCenter())
                .CompareTo(camera.DistanceSquaredTo(b.BoundsDirty && b.Entries.Count > 0 ? _data[b.Entries[0]].Transform.Origin : b.Bounds.GetCenter())));
        long used = 0;
        foreach (Batch batch in pending)
        {
            MultiMesh mesh = batch.Node.Multimesh;
            int count = batch.Entries.Count;
            if (count == 0)
            {
                InstanceCount -= batch.Capacity;
                _batches.Remove(batch.Key);
                batch.Node.QueueFree();
                _dirty.Remove(batch);
                continue;
            }
            if (batch.BoundsDirty)
            {
                Aabb first = Transformed(_data[batch.Entries[0]].Transform, meshBounds);
                Vector3 low = first.Position, high = first.End;
                foreach (int handle in batch.Entries)
                {
                    Aabb box = Transformed(_data[handle].Transform, meshBounds);
                    low = low.Min(box.Position);
                    high = high.Max(box.End);
                }
                batch.Bounds = new Aabb(low, high - low);
                batch.BoundsDirty = false;
            }
            Aabb bounds = batch.Bounds;
            batch.Resident = Wants(batch);
            batch.Near = Hides(batch);
            if (!batch.Resident)
            {
                InstanceCount -= batch.Capacity;
                mesh.InstanceCount = 0;
                batch.Capacity = 0;
                batch.Buffer = [];

                // An emptied MultiMesh keeps the CustomAabb of its last upload, which the culler
                // still accepts, so a dropped batch would go on costing a draw call with nothing
                // in it. A batch that has never been uploaded has no bounds and is already culled.
                batch.Node.Visible = false;
                _dirty.Remove(batch);
                continue;
            }
            int capacity = batch.Capacity;
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
                batch.Capacity = next;
                batch.Buffer = new float[checked(next * stride)];
            }
            for (int i = 0; i < count; i++)
            {
                ref Entry entry = ref _data[batch.Entries[i]];
                Transform3D t = entry.Far && batch.Near ? new Transform3D(new Basis(Vector3.Zero, Vector3.Zero, Vector3.Zero), entry.Transform.Origin) : entry.Transform;
                int at = i * stride;
                float[] b = batch.Buffer;
                b[at++] = t.Basis.X.X; b[at++] = t.Basis.Y.X; b[at++] = t.Basis.Z.X; b[at++] = t.Origin.X;
                b[at++] = t.Basis.X.Y; b[at++] = t.Basis.Y.Y; b[at++] = t.Basis.Z.Y; b[at++] = t.Origin.Y;
                b[at++] = t.Basis.X.Z; b[at++] = t.Basis.Y.Z; b[at++] = t.Basis.Z.Z; b[at++] = t.Origin.Z;
                if (UseColors) Put(b, ref at, entry.Colour);
                if (UseCustomData) Put(b, ref at, entry.Custom);
            }
            batch.Bounds = bounds;
            Choose(batch);
            mesh.CustomAabb = bounds;
            mesh.Buffer = batch.Buffer;
            mesh.VisibleInstanceCount = count;
            batch.Visible = count;
            batch.Node.Visible = true;
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
            if (!batch.Resident || batch.Capacity == 0) continue;
            ResidentBatches++;
            ResidentInstances += batch.Visible;
        }
        UploadMilliseconds += System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;
    }
    private bool Wants(Batch batch)
    {
        if (NearOnly && Near is not null) return Near(batch.Key);
        if (Needed is not null && !Needed(batch)) return false;
        if (_eye is not { } eye || _detailDistance <= 0) return true;
        Vector3 nearest = eye.Clamp(batch.Bounds.Position, batch.Bounds.End);
        float distance = _detailDistance * (batch.Resident ? Hysteresis : 1f);
        return eye.DistanceSquaredTo(nearest) <= distance * distance;
    }

    // Swapping the mesh keeps the uploaded instances, so a chunk changes detail without a transfer.
    // A band the chunk already stands inside keeps it until 15% beyond, as residency does.
    private void Choose(Batch batch)
    {
        var details = _owner.Details;
        int detail = -1;
        if (details.Count > 0 && _eye is { } eye)
        {
            float distance = eye.DistanceTo(eye.Clamp(batch.Bounds.Position, batch.Bounds.End));
            for (int i = 0; i < details.Count && detail < 0; i++)
            {
                bool inside = batch.Detail >= 0 && batch.Detail <= i;
                if (distance <= details[i].Within * (inside ? Hysteresis : 1f)) detail = i;
            }
        }
        if (detail == batch.Detail) return;
        batch.Detail = detail;
        batch.Node.Multimesh.Mesh = detail < 0 ? Mesh : details[detail].Mesh;
    }

    private bool Hides(Batch batch) => !NearOnly && Near is not null && Near(batch.Key);

    // Godot's `Transform3D * Aabb`, with the same arithmetic in the same order. The operator reads
    // through Basis and Vector3 indexers, which cost seconds across a million instances.
    private static Aabb Transformed(in Transform3D transform, Aabb box)
    {
        Vector3 min = box.Position, max = box.Position + box.Size;
        Basis basis = transform.Basis;
        Vector3 origin = transform.Origin;
        float lowX = origin.X, highX = origin.X, lowY = origin.Y, highY = origin.Y, lowZ = origin.Z, highZ = origin.Z;
        Row(ref lowX, ref highX, basis.X.X, basis.Y.X, basis.Z.X, min, max);
        Row(ref lowY, ref highY, basis.X.Y, basis.Y.Y, basis.Z.Y, min, max);
        Row(ref lowZ, ref highZ, basis.X.Z, basis.Y.Z, basis.Z.Z, min, max);
        return new Aabb(new Vector3(lowX, lowY, lowZ), new Vector3(highX - lowX, highY - lowY, highZ - lowZ));

        static void Row(ref float low, ref float high, float x, float y, float z, Vector3 min, Vector3 max)
        {
            Term(ref low, ref high, x * min.X, x * max.X);
            Term(ref low, ref high, y * min.Y, y * max.Y);
            Term(ref low, ref high, z * min.Z, z * max.Z);
        }

        static void Term(ref float low, ref float high, float e, float f)
        {
            if (e < f) { low += e; high += f; }
            else { low += f; high += e; }
        }
    }

    private static void Put(float[] buffer, ref int at, Color value)
    {
        buffer[at++] = value.R; buffer[at++] = value.G; buffer[at++] = value.B; buffer[at++] = value.A;
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Godot;

namespace Borough.Shell;

/// <param name="Middle">With <paramref name="Extent"/>, a cube holding every piece the owner can draw.</param>
public readonly record struct PreparedOwner(long Owner, Vector3 Middle, float Extent);

public readonly record struct PreparedPiece(ulong Id, Transform3D Where, Color What);

/// <summary>
/// Plans and writes the owners of reach-gated layers near the camera, a frame's allowance at a
/// time, nearest first. A plan is one owner's pieces in the whole-city walk's order. Each layer
/// is written by owner and lists owners by key, so a drained layer holds exactly what a
/// whole-city write from the same camera holds.
/// </summary>
public sealed class ScenePreparation(bool verify = false)
{
    public const float SquareMetres = 256f;

    // PROVISIONAL. Plans survive out to this multiple of a layer's reach. Past the cap, the
    // farthest squares lose their plans first.
    private const float EvictScale = 1.5f;
    private const long PlanCap = 64L * 1024 * 1024;

    private static readonly int PieceBytes = Unsafe.SizeOf<PreparedPiece>();

    private sealed class Owner
    {
        public PreparedOwner Where;
        public PreparedPiece[]? Plan;
        public int Pieces;
        public Aabb Origins;
        public bool Written, Complete, Queued;
    }

    private sealed class Square
    {
        public readonly List<Owner> Owners = [];
        public Vector3 Low = new(float.MaxValue, float.MaxValue, float.MaxValue);
        public Vector3 High = new(float.MinValue, float.MinValue, float.MinValue);
        public Vector3 Middle => (Low + High) * 0.5f;
        public float Extent => Mathf.Max(High.X - Low.X, Mathf.Max(High.Y - Low.Y, High.Z - Low.Z)) * 0.5f;
    }

    private sealed class Component(string name, InstanceLayer layer, Action<List<PreparedOwner>> owners, Func<long, PreparedPiece[]> plan)
    {
        public readonly string Name = name;
        public readonly InstanceLayer Layer = layer;
        public readonly Action<List<PreparedOwner>> ListOwners = owners;
        public readonly Func<long, PreparedPiece[]> Plan = plan;
        public Dictionary<long, Owner> Owners = [];
        public readonly Dictionary<Vector2I, Square> Squares = [];
        public bool Pending = true;
        public long PlanBytes, Plans, Writes, Dropped;
        public double PlanMilliseconds, WriteMilliseconds;
    }

    private readonly List<Component> _components = [];
    private readonly List<(float Distance, Component Component, Owner Owner)> _queue = [];
    private readonly List<PreparedOwner> _listed = [];
    private readonly List<InstanceValue> _values = [];
    private readonly List<ulong> _ids = [];
    private int _next;
    private Vector3 _eye;
    private bool _unverified;
    private long _frames;
    private double _milliseconds, _maximum;

    public int Queued => _queue.Count - _next;

    /// <summary>Adds a component whose layer it writes by owner from now on.</summary>
    public int Register(string name, InstanceLayer layer, Action<List<PreparedOwner>> owners, Func<long, PreparedPiece[]> plan)
    {
        _components.Add(new Component(name, layer, owners, plan));
        return _components.Count - 1;
    }

    /// <summary>
    /// Lists the component's owners again and discards every plan. Vanished owners lose their
    /// instances now; the rest keep theirs until the next <see cref="Regate"/> rewrites them.
    /// </summary>
    public void Reset(int component)
    {
        Component c = _components[component];
        Purge(c);
        _listed.Clear();
        c.ListOwners(_listed);
        var live = new Dictionary<long, Owner>(_listed.Count);
        c.Squares.Clear();
        foreach (PreparedOwner where in _listed)
        {
            if (c.Owners.Remove(where.Owner, out Owner? owner)) Drop(c, owner);
            else owner = new Owner();
            owner.Where = where;
            owner.Complete = owner.Queued = false;
            live.Add(where.Owner, owner);

            var key = new Vector2I(Mathf.FloorToInt(where.Middle.X / SquareMetres), Mathf.FloorToInt(where.Middle.Z / SquareMetres));
            if (!c.Squares.TryGetValue(key, out Square? square)) c.Squares.Add(key, square = new Square());
            square.Owners.Add(owner);
            var extent = new Vector3(where.Extent, where.Extent, where.Extent);
            square.Low = square.Low.Min(where.Middle - extent);
            square.High = square.High.Max(where.Middle + extent);
        }
        foreach ((long key, Owner gone) in c.Owners)
        {
            if (gone.Written) c.Layer.Multimesh.Remove(key);
            Drop(c, gone);
        }
        c.Owners = live;
        c.Pending = true;
    }

    /// <summary>
    /// For each component that is stale or reset, starts a build from the camera, removes owners
    /// now out of reach, drops plans beyond evict reach and queues every reached owner whose
    /// instances could change. An owner written whole stays when the new camera reaches all of it.
    /// </summary>
    public bool Regate(Vector3 eye)
    {
        bool any = false;
        _eye = eye;
        foreach (Component c in _components)
        {
            InstanceLayer layer = c.Layer;
            if (!c.Pending && !layer.Stale(eye)) continue;
            c.Pending = false;
            any = true;
            layer.BuildFrom();
            Purge(c);
            foreach (Square square in c.Squares.Values)
            {
                bool reached = layer.Reaches(square.Middle, square.Extent);
                bool kept = reached || layer.Reaches(square.Middle, square.Extent, EvictScale);
                foreach (Owner owner in square.Owners)
                {
                    owner.Queued = false;
                    if (!kept) Drop(c, owner);
                    if (!reached || !layer.Reaches(owner.Where.Middle, owner.Where.Extent))
                    {
                        Unwrite(c, owner);
                        continue;
                    }
                    if (owner.Written && owner.Complete && (owner.Pieces == 0 || layer.Holds(owner.Origins))) continue;
                    owner.Queued = true;
                    _queue.Add((owner.Where.Middle.DistanceTo(eye), c, owner));
                }
            }
        }
        if (any)
        {
            _queue.Sort(_next, _queue.Count - _next, Comparer<(float Distance, Component Component, Owner Owner)>.Create(
                static (a, b) => a.Distance.CompareTo(b.Distance)));
            Cap();
        }
        return any;
    }

    /// <summary>Plans and writes queued owners, nearest first, until the allowance is spent. Always makes progress.</summary>
    public void Spend(double milliseconds)
    {
        long started = Stopwatch.GetTimestamp();
        bool worked = false;
        while (_next < _queue.Count)
        {
            if (worked && Stopwatch.GetElapsedTime(started).TotalMilliseconds >= milliseconds) break;
            (float _, Component c, Owner owner) = _queue[_next++];
            if (!owner.Queued) continue;
            owner.Queued = false;
            Prepare(c, owner);
            worked = true;
        }
        if (_next == _queue.Count)
        {
            _queue.Clear();
            _next = 0;
        }
        if (worked)
        {
            double elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            _frames++;
            _milliseconds += elapsed;
            _maximum = Math.Max(_maximum, elapsed);
            _unverified = true;
            Cap();
        }
        if (verify && _unverified && _queue.Count == 0)
        {
            _unverified = false;
            Verify();
        }
    }

    public void Drain() => Spend(double.PositiveInfinity);

    public void Statistics(StringBuilder text)
    {
        text.Append("# render_prepare\tcomponent\towners\tplanned\twritten\tqueued\tplan_bytes\tplans\tplan_ms\twrites\twrite_ms\tdropped\n");
        foreach (Component c in _components)
        {
            int planned = 0, written = 0, queued = 0;
            foreach (Owner owner in c.Owners.Values)
            {
                if (owner.Plan is not null) planned++;
                if (owner.Written) written++;
                if (owner.Queued) queued++;
            }
            text.Append(CultureInfo.InvariantCulture,
                $"render_prepare\t{c.Name}\t{c.Owners.Count}\t{planned}\t{written}\t{queued}\t{c.PlanBytes}\t{c.Plans}\t{c.PlanMilliseconds:F3}\t{c.Writes}\t{c.WriteMilliseconds:F3}\t{c.Dropped}\n");
        }
        text.Append(CultureInfo.InvariantCulture, $"render_prepare_frames\t{_frames}\t{_milliseconds:F3}\t{_maximum:F3}\n");
    }

    private void Prepare(Component c, Owner owner)
    {
        long key = owner.Where.Owner;
        long started = Stopwatch.GetTimestamp();
        if (owner.Plan is null)
        {
            PreparedPiece[] plan = c.Plan(key);
            owner.Plan = plan;
            owner.Pieces = plan.Length;
            owner.Origins = Origins(plan);
            c.PlanBytes += Bytes(plan);
            c.Plans++;
            long planned = Stopwatch.GetTimestamp();
            c.PlanMilliseconds += Stopwatch.GetElapsedTime(started, planned).TotalMilliseconds;
            started = planned;
        }

        InstanceLayer layer = c.Layer;
        _values.Clear();
        _ids.Clear();
        foreach (ref readonly PreparedPiece piece in owner.Plan.AsSpan())
        {
            if (!layer.Reaches(piece.Where)) continue;
            _values.Add(new InstanceValue(piece.Where, piece.What));
            _ids.Add(piece.Id);
        }
        layer.Multimesh.Write(key, _values, _ids);
        owner.Written = true;
        owner.Complete = _values.Count == owner.Pieces;
        c.Writes++;
        c.WriteMilliseconds += Stopwatch.GetElapsedTime(started).TotalMilliseconds;
    }

    private static void Unwrite(Component c, Owner owner)
    {
        if (!owner.Written) return;
        c.Layer.Multimesh.Remove(owner.Where.Owner);
        owner.Written = owner.Complete = false;
    }

    private static void Drop(Component c, Owner owner)
    {
        if (owner.Plan is null) return;
        c.PlanBytes -= Bytes(owner.Plan);
        owner.Plan = null;
        c.Dropped++;
    }

    private void Purge(Component c)
    {
        _queue.RemoveRange(0, _next);
        _next = 0;
        _queue.RemoveAll(entry => entry.Component == c);
    }

    private void Cap()
    {
        long bytes = 0;
        foreach (Component c in _components) bytes += c.PlanBytes;
        if (bytes <= PlanCap) return;

        var squares = new List<(float Distance, Component Component, Square Square)>();
        foreach (Component c in _components)
            foreach (Square square in c.Squares.Values)
                squares.Add((square.Middle.DistanceTo(_eye), c, square));
        squares.Sort(static (a, b) => b.Distance.CompareTo(a.Distance));
        foreach ((float _, Component c, Square square) in squares)
        {
            foreach (Owner owner in square.Owners)
            {
                if (owner.Queued || owner.Plan is null) continue;
                bytes -= Bytes(owner.Plan);
                Drop(c, owner);
            }
            if (bytes <= PlanCap) return;
        }
    }

    // Every layer must equal a whole-city plan of every reached owner, filtered from the same camera,
    // in canonical order.
    private void Verify()
    {
        foreach (Component c in _components)
        {
            InstanceLayer layer = c.Layer;
            InstanceBuffer buffer = layer.Multimesh;
            int[] canonical = buffer.Canonical();
            int at = 0;
            _listed.Clear();
            c.ListOwners(_listed);
            _listed.Sort(static (a, b) => a.Owner.CompareTo(b.Owner));
            foreach (PreparedOwner where in _listed)
            {
                if (!layer.Reaches(where.Middle, where.Extent)) continue;
                foreach (PreparedPiece piece in c.Plan(where.Owner))
                {
                    if (!layer.Reaches(piece.Where)) continue;
                    int index = at < canonical.Length ? canonical[at] : -1;
                    at++;
                    if (index < 0 || buffer.IdAt(index) != piece.Id || buffer.GetInstanceTransform(index) != piece.Where
                        || buffer.GetInstanceColor(index) != piece.What)
                    {
                        throw new InvalidOperationException($"Prepared {c.Name} owner {where.Owner} differs from a whole-city plan.");
                    }
                }
            }
            if (at != canonical.Length)
                throw new InvalidOperationException($"Prepared {c.Name} holds {canonical.Length} instances where a whole-city plan holds {at}.");
        }
    }

    private static Aabb Origins(PreparedPiece[] plan)
    {
        if (plan.Length == 0) return default;
        var box = new Aabb(plan[0].Where.Origin, Vector3.Zero);
        foreach (ref readonly PreparedPiece piece in plan.AsSpan()) box = box.Expand(piece.Where.Origin);
        return box;
    }

    private static long Bytes(PreparedPiece[] plan) => (long)plan.Length * PieceBytes;
}

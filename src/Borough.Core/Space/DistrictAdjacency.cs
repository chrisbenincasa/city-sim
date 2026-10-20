using Borough.Core.Entities;
using Borough.Core.Tables;

namespace Borough.Core.Space;

public sealed class DistrictAdjacency
{
    public readonly record struct Link(int Lower, int Higher, int Crossings);

    private readonly record struct Boundary(int Component, int District, int Segments);

    private int[] _districtOfNode = [];
    private int[] _parent = [];
    private long[] _roadTiles = [];
    private Boundary[] _boundaries = [];
    private Link[] _links = [];
    private int _linkCount;
    private int _districtSlots;

    public ReadOnlySpan<Link> Links => _links.AsSpan(0, _linkCount);

    public long RoadTilesOf(int districtSlot) =>
        (uint)districtSlot < (uint)_districtSlots ? _roadTiles[districtSlot] : 0;

    public void Compute(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        _linkCount = 0;
        _districtSlots = world.Districts.Rows.SlotCount;
        Grow(ref _roadTiles, _districtSlots);
        Array.Clear(_roadTiles);

        if (!world.Rules.Districts.Runs || world.Districts.Rows.LiveCount == 0)
        {
            return;
        }

        RoadNodeTable nodes = world.Roads.Nodes;
        RoadSegmentTable segments = world.Roads.Segments;
        Grow(ref _districtOfNode, nodes.Rows.SlotCount);
        Grow(ref _parent, nodes.Rows.SlotCount);
        Grow(ref _boundaries, segments.Rows.SlotCount);

        for (int node = 0; node < nodes.Rows.SlotCount; node++)
        {
            _parent[node] = -1;
            _districtOfNode[node] = Rows.NoSlot;

            if (nodes.Rows.IsLive(node)
                && world.Districts.Rows.TryResolve(
                    world.DistrictsInCells.Of(
                        world.DistrictCells,
                        CellGrid.ToCells(nodes.East[node]),
                        CellGrid.ToCells(nodes.North[node])),
                    out int district))
            {
                _districtOfNode[node] = district;
            }
        }

        for (int segment = 0; segment < segments.Rows.SlotCount; segment++)
        {
            if (!segments.Rows.IsLive(segment))
            {
                continue;
            }

            int a = nodes.Rows.Resolve(segments.NodeA[segment]);
            int b = nodes.Rows.Resolve(segments.NodeB[segment]);

            if (_districtOfNode[a] == Rows.NoSlot && _districtOfNode[b] == Rows.NoSlot)
            {
                Union(a, b);
            }
        }

        int boundaryCount = 0;

        for (int segment = 0; segment < segments.Rows.SlotCount; segment++)
        {
            if (!segments.Rows.IsLive(segment))
            {
                continue;
            }

            int a = nodes.Rows.Resolve(segments.NodeA[segment]);
            int b = nodes.Rows.Resolve(segments.NodeB[segment]);
            int districtA = _districtOfNode[a];
            int districtB = _districtOfNode[b];
            int credited = districtA != Rows.NoSlot ? districtA : districtB;

            if (credited != Rows.NoSlot)
            {
                _roadTiles[credited] += segments.LengthTiles[segment].Raw;
            }

            if (districtA == districtB)
            {
                continue;
            }

            if (districtA != Rows.NoSlot && districtB != Rows.NoSlot)
            {
                AddLink(districtA, districtB, 1);
            }
            else
            {
                int component = Find(districtA == Rows.NoSlot ? a : b);
                _boundaries[boundaryCount++] = new Boundary(component, credited, 1);
            }
        }

        _boundaries.AsSpan(0, boundaryCount).Sort(static (a, b) =>
        {
            int order = a.Component.CompareTo(b.Component);
            return order != 0 ? order : a.District.CompareTo(b.District);
        });

        int touching = 0;

        for (int start = 0; start < boundaryCount;)
        {
            Boundary boundary = _boundaries[start++];
            int count = boundary.Segments;

            while (start < boundaryCount
                && _boundaries[start].Component == boundary.Component
                && _boundaries[start].District == boundary.District)
            {
                count += _boundaries[start++].Segments;
            }

            _boundaries[touching++] = boundary with { Segments = count };
        }

        for (int start = 0; start < touching;)
        {
            int end = start + 1;

            while (end < touching && _boundaries[end].Component == _boundaries[start].Component)
            {
                end++;
            }

            for (int a = start; a < end; a++)
            {
                for (int b = a + 1; b < end; b++)
                {
                    Boundary first = _boundaries[a];
                    Boundary second = _boundaries[b];
                    AddLink(first.District, second.District,
                        first.Segments < second.Segments ? first.Segments : second.Segments);
                }
            }

            start = end;
        }

        _links.AsSpan(0, _linkCount).Sort(static (a, b) =>
        {
            int order = a.Lower.CompareTo(b.Lower);
            return order != 0 ? order : a.Higher.CompareTo(b.Higher);
        });

        int unique = 0;

        for (int start = 0; start < _linkCount;)
        {
            Link link = _links[start++];
            int crossings = link.Crossings;

            while (start < _linkCount
                && _links[start].Lower == link.Lower
                && _links[start].Higher == link.Higher)
            {
                crossings += _links[start++].Crossings;
            }

            _links[unique++] = link with { Crossings = crossings };
        }

        _linkCount = unique;
    }

    private void AddLink(int a, int b, int crossings)
    {
        Grow(ref _links, _linkCount + 1);
        _links[_linkCount++] = a < b ? new Link(a, b, crossings) : new Link(b, a, crossings);
    }

    private int Find(int node)
    {
        int root = node;

        while (_parent[root] >= 0)
        {
            root = _parent[root];
        }

        while (node != root)
        {
            int next = _parent[node];
            _parent[node] = root;
            node = next;
        }

        return root;
    }

    private void Union(int a, int b)
    {
        a = Find(a);
        b = Find(b);

        if (a == b)
        {
            return;
        }

        // Negative root entries hold component sizes.
        if (_parent[a] > _parent[b])
        {
            (a, b) = (b, a);
        }

        _parent[a] += _parent[b];
        _parent[b] = a;
    }

    private static void Grow<T>(ref T[] buffer, int count)
    {
        if (buffer.Length < count)
        {
            int doubled = buffer.Length * 2;
            Array.Resize(ref buffer, doubled > count ? doubled : count);
        }
    }
}

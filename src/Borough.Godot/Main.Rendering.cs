using System.Globalization;
using System.Collections.Generic;
using Borough.Core.Entities;
using System.Text;

namespace Borough.Shell;

public partial class Main
{
    private readonly Dictionary<int, (ulong Id, bool Drawn)> _renderedBuildings = [];
    private readonly List<InstanceValue> _bodyEdits = [], _roofEdits = [], _hipEdits = [], _pairedRoofEdits = [], _parapetEdits = [], _yardEdits = [];
    private Wash _visualWash;
    private long _fullBuildingPasses, _buildingEdits, _movementQueries, _movementIndexVisits;
    private long _frameCount;
    private double _frameMilliseconds, _frameMaximum;
    private readonly bool _verifyRendering = System.Environment.GetEnvironmentVariable("BOROUGH_RENDER_VERIFY") is not null;

    private void UpdateBuildings()
    {
        CheckBodyKitAtStartup();
        WorldChanges changes = _world.Changes!;
        if (changes.Full || _visualWash != _washing || _washing == Wash.Age
            || _troubleRepaint && _washing == Wash.Trouble)
        {
            _troubleRepaint = false;
            _fullBuildingPasses++;
            _visualWash = _washing;
            if (changes.Full) PlaceFamilyBodies();
            else RefreshFamilyBodies(changes.Buildings);
            _drawnBuildings = Massings(Buildings());
            _vacantLots = Fill(_plots, Plots(), _plotIds);
            FillCarParks();
            _renderedBuildings.Clear();
            var drawn = new HashSet<ulong>();
            for (int i = 0; i < _buildings.Multimesh.VisibleInstanceCount; i++) drawn.Add(_buildings.Multimesh.IdAt(i));
            var rows = _world.Buildings.Rows;
            for (int slot = 0; slot < rows.SlotCount; slot++)
            {
                if (!rows.IsLive(slot)) continue;
                ulong id = rows.IdAt(slot);
                _renderedBuildings[slot] = (id, drawn.Contains(id) || _placedBodies.ContainsKey(id) || _exactBodies.ContainsKey(id) || _shelled.Contains(id) || _moduleDrawnIds.Contains(id));
            }
        }
        else
        {
            bool geometry = false;
            bool fronts = false;
            var changedIds = new HashSet<ulong>();
            var placed = new List<int>();
            foreach (int slot in changes.Buildings)
            {
                _buildingEdits++;
                _bodyEdits.Clear(); _roofEdits.Clear(); _hipEdits.Clear(); _pairedRoofEdits.Clear(); _parapetEdits.Clear(); _yardEdits.Clear();
                bool live = _world.Buildings.Rows.IsLive(slot);
                ulong id = live ? _world.Buildings.Rows.IdAt(slot) : 0;
                if (_renderedBuildings.Remove(slot, out var old))
                {
                    if (old.Drawn) _drawnBuildings--;

                    // A bodied Building writes no massing box, so a gone one is news only here.
                    // A body under the same id is left standing for PlaceFamilyBody to compare with.
                    if (old.Id != id) geometry |= ReplaceBuilding(old.Id) | RemoveFamilyBody(old.Id);
                    changedIds.Add(old.Id);
                }
                if (!live) continue;
                changedIds.Add(id);
                placed.Add(slot);
                bool bodied = PlaceFamilyBody(slot, out bool bodyChanged);
                geometry |= bodyChanged;
                PlacedBody bodyPlacement = bodied ? _placedBodies[id] : default;
                BodyShape? shape = bodied ? bodyPlacement.NearShape : null;
                bool detailedFar = bodyPlacement.FarShape is not null;
                bool modules = _world.IsSupermarket(slot) || _world.IsDepartmentStore(slot) || _world.IsPadSite(slot)
                    || _world.IsSalesYard(slot) || _world.IsPrecinct(slot) || _world.IsMarketHall(slot);
                if (modules) _moduleDrawnIds.Add(id); else _moduleDrawnIds.Remove(id);
                fronts |= modules || _world.IsTradeCentre(slot);
                foreach (Massing each in modules || detailedFar ? [] : Buildings(slot))
                {
                    Massing one = bodied ? FarMassing(each, shape!.Body) : each;
                    _bodyEdits.Add(new(one.Body, bodied ? FarPaint(one, shape!.Wall, one.Paint) : one.Paint, one.Reads, bodied));
                    if (one.Outhoused && !bodied) _yardEdits.Add(new(one.Yard,
                        YardPaint(one)));
                    var roof = one.Cap switch { Cap.Gable => _roofEdits, Cap.Hip => _hipEdits, Cap.PairedGable => _pairedRoofEdits, Cap.Parapet => _parapetEdits, _ => null };
                    roof?.Add(new(one.Roof, bodied ? FarPaint(one, shape!.Roof, RoofPaint(one)) : RoofPaint(one), RoofWall(one), bodied));
                }
                geometry |= ReplaceBuilding(id);
                bool drawn = bodied || modules || _bodyEdits.Count != 0;
                _renderedBuildings[slot] = (id, drawn);
                if (drawn) _drawnBuildings++;
            }
            if (_familyBodies) RefreshNeighbourBodies(changedIds, placed);
            if (geometry)
            {
                int at = 0;
                foreach (Massing one in Buildings())
                {
                    FoliageFootprint(one.Body, at++);
                    if (one.Outhoused) FoliageFootprint(one.Yard, at++);
                }
                foreach ((_, var surface) in CarParkSurfaces()) FoliageFootprint(surface, at++);
                RefreshFoliage(at);
                _vacantLots = Fill(_plots, Plots(), _plotIds);
                FillCarParks();
            }
            else if (fronts)
            {
                FillCarParks();
            }
        }
        changes.Clear();
        if (_verifyRendering) VerifyBuildingDrawing();
    }

    private void VerifyBuildingDrawing()
    {
        InstanceLayer[] layers = [_buildings, _roofs, _hips, _pairedRoofs, _parapets, _yards];
        var before = new List<Dictionary<(ulong Id, int Part), InstanceValue>>();
        foreach (var layer in layers) before.Add(layer.Multimesh.Snapshot());
        int fresh = Massings(Buildings());
        if (fresh != _drawnBuildings) throw new System.InvalidOperationException("Incremental Building count differs from a full rebuild.");
        for (int i = 0; i < layers.Length; i++)
        {
            var after = layers[i].Multimesh.Snapshot();
            if (after.Count != before[i].Count) throw new System.InvalidOperationException("Incremental component count differs from a full rebuild.");
            foreach (var entry in before[i])
                if (!after.TryGetValue(entry.Key, out var value) || value != entry.Value)
                    throw new System.InvalidOperationException($"Incremental Building {entry.Key} differs from a full rebuild at Tick {_world.Tick.Raw}.");
        }

        foreach ((ulong id, BodyNeighbours seen) in _bodyNeighbours)
        {
            if (IdAt(Covering(seen.Slot, seen.LeftProbe)) != seen.LeftId || IdAt(Covering(seen.Slot, seen.RightProbe)) != seen.RightId)
                throw new System.InvalidOperationException($"Body of Building {id} was drawn against neighbours that have changed, at Tick {_world.Tick.Raw}.");
        }
    }

    private bool ReplaceBuilding(ulong id)
    {
        bool geometry = _buildings.Multimesh.Replace(id, _bodyEdits);
        geometry |= _yards.Multimesh.Replace(id, _yardEdits);
        _roofs.Multimesh.Replace(id, _roofEdits);
        _hips.Multimesh.Replace(id, _hipEdits);
        _pairedRoofs.Multimesh.Replace(id, _pairedRoofEdits);
        _parapets.Multimesh.Replace(id, _parapetEdits);
        return geometry;
    }

    private void FlushInstances(bool immediate = false)
    {
        // PROVISIONAL transfer allowance. A single larger batch is admitted alone, so it cannot starve.
        const long allowance = 8L * 1024 * 1024;
        long remaining = immediate ? long.MaxValue : allowance;
        foreach (var layer in Layers())
        {
            InstanceBuffer buffer = layer.Layer.Multimesh;
            long before = buffer.UploadedBytes;
            buffer.Flush(_camera.GlobalPosition, layer.Layer.DetailDistance, remaining, immediate || remaining == allowance);
            remaining = System.Math.Max(0, remaining - (buffer.UploadedBytes - before));
        }
        _cursor.Multimesh.Flush();
    }

    private void RendererStatistics(StringBuilder text)
    {
        if (System.Environment.GetEnvironmentVariable("BOROUGH_RENDER_PROFILE") is null) return;
        text.Append($"render_probe\t{_renderProbe}\n");
        text.Append(CultureInfo.InvariantCulture, $"render_work\t{_fullBuildingPasses}\t{_buildingEdits}\t{_movementIndexVisits}\t{_movementQueries}\n");
        text.Append(CultureInfo.InvariantCulture, $"render_frames\t{_frameCount}\t{_frameMilliseconds:F3}\t{_frameMaximum:F3}\n");
        Godot.Rid viewport = GetViewport().GetViewportRid();
        text.Append(CultureInfo.InvariantCulture, $"render_times\t{Godot.RenderingServer.ViewportGetMeasuredRenderTimeCpu(viewport):F3}\t{Godot.RenderingServer.ViewportGetMeasuredRenderTimeGpu(viewport):F3}\n");
        long Info(Godot.RenderingServer.ViewportRenderInfoType pass, Godot.RenderingServer.ViewportRenderInfo what) =>
            Godot.RenderingServer.ViewportGetRenderInfo(viewport, pass, what);
        text.Append("# render_info\tpass\tobjects\tdraw_calls\tprimitives\n");
        foreach (var pass in new[] { Godot.RenderingServer.ViewportRenderInfoType.Visible, Godot.RenderingServer.ViewportRenderInfoType.Shadow })
        {
            text.Append(CultureInfo.InvariantCulture,
                $"render_info\t{pass}\t{Info(pass, Godot.RenderingServer.ViewportRenderInfo.ObjectsInFrame)}\t{Info(pass, Godot.RenderingServer.ViewportRenderInfo.DrawCallsInFrame)}\t{Info(pass, Godot.RenderingServer.ViewportRenderInfo.PrimitivesInFrame)}\n");
        }

        text.Append("# shell_band\tradius\tchunk\tkit\tshadows\tbuildings\tchunks\tvertices\ttriangles\tkit_pieces\tcollect_ms\tgenerate_ms\tupload_ms\tslowest_upload_ms\n");
        text.Append(_shellBandReport);
        text.Append("# render\tlayer\tbatches\tuploads\tuploaded_instances\tuploaded_bytes\tupload_ms\tpending\n");
        foreach (var layer in Layers())
        {
            InstanceBuffer buffer = layer.Layer.Multimesh;
            text.Append(CultureInfo.InvariantCulture,
                $"render\t{layer.Name}\t{buffer.BatchCount}\t{buffer.Uploads}\t{buffer.UploadedInstances}\t{buffer.UploadedBytes}\t{buffer.UploadMilliseconds:F3}\t{buffer.PendingBatches}\n");
        }
    }
}

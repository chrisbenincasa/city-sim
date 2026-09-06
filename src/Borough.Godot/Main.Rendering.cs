using System.Globalization;
using System.Collections.Generic;
using Borough.Core.Entities;
using System.Text;

namespace Borough.Shell;

public partial class Main
{
    private readonly Dictionary<int, (ulong Id, bool Drawn)> _renderedBuildings = [];
    private readonly List<InstanceValue> _bodyEdits = [], _roofEdits = [], _hipEdits = [], _mansardEdits = [], _yardEdits = [];
    private Wash _visualWash;
    private long _fullBuildingPasses, _buildingEdits, _movementQueries, _movementIndexVisits;
    private long _frameCount;
    private double _frameMilliseconds, _frameMaximum;
    private readonly bool _verifyRendering = System.Environment.GetEnvironmentVariable("BOROUGH_RENDER_VERIFY") is not null;

    private void UpdateBuildings()
    {
        WorldChanges changes = _world.Changes!;
        if (changes.Full || _visualWash != _washing || _washing == Wash.Age)
        {
            _fullBuildingPasses++;
            _visualWash = _washing;
            _drawnBuildings = Massings(Buildings());
            _vacantLots = Fill(_plots, Plots(), _plotIds);
            _renderedBuildings.Clear();
            var drawn = new HashSet<ulong>();
            for (int i = 0; i < _buildings.Multimesh.VisibleInstanceCount; i++) drawn.Add(_buildings.Multimesh.IdAt(i));
            var rows = _world.Buildings.Rows;
            for (int slot = 0; slot < rows.SlotCount; slot++)
                if (rows.IsLive(slot)) _renderedBuildings[slot] = (rows.IdAt(slot), drawn.Contains(rows.IdAt(slot)));
        }
        else
        {
            bool geometry = false;
            foreach (int slot in changes.Buildings)
            {
                _buildingEdits++;
                _bodyEdits.Clear(); _roofEdits.Clear(); _hipEdits.Clear(); _mansardEdits.Clear(); _yardEdits.Clear();
                bool live = _world.Buildings.Rows.IsLive(slot);
                ulong id = live ? _world.Buildings.Rows.IdAt(slot) : 0;
                if (_renderedBuildings.Remove(slot, out var old))
                {
                    if (old.Drawn) _drawnBuildings--;
                    if (old.Id != id) geometry |= ReplaceBuilding(old.Id);
                }
                if (!live) continue;
                foreach (Massing one in Buildings(slot))
                {
                    _bodyEdits.Add(new(one.Body, one.Paint, one.Reads));
                    if (one.Outhoused) _yardEdits.Add(new(one.Yard,
                        one.Paint == Derelict.SrgbToLinear() ? one.Paint : Outbuilding.SrgbToLinear()));
                    var roof = one.Cap switch { Cap.Gable => _roofEdits, Cap.Hip => _hipEdits, Cap.Mansard => _mansardEdits, _ => null };
                    roof?.Add(new(one.Roof, one.Paint == Derelict.SrgbToLinear() ? one.Paint : one.Slate));
                }
                geometry |= ReplaceBuilding(id);
                bool drawn = _bodyEdits.Count != 0;
                _renderedBuildings[slot] = (id, drawn);
                if (drawn) _drawnBuildings++;
            }
            if (geometry)
            {
                int at = 0;
                foreach (var batch in _buildings.Multimesh.Batches)
                    foreach (var entry in batch.Instances) FoliageFootprint(entry.Transform, at++);
                foreach (var batch in _yards.Multimesh.Batches)
                    foreach (var entry in batch.Instances) FoliageFootprint(entry.Transform, at++);
                RefreshFoliage(at);
                _vacantLots = Fill(_plots, Plots(), _plotIds);
            }
        }
        changes.Clear();
        if (_verifyRendering) VerifyBuildingDrawing();
    }

    private void VerifyBuildingDrawing()
    {
        InstanceLayer[] layers = [_buildings, _roofs, _hips, _mansards, _yards];
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
    }

    private bool ReplaceBuilding(ulong id)
    {
        bool geometry = _buildings.Multimesh.Replace(id, _bodyEdits);
        geometry |= _yards.Multimesh.Replace(id, _yardEdits);
        _roofs.Multimesh.Replace(id, _roofEdits);
        _hips.Multimesh.Replace(id, _hipEdits);
        _mansards.Multimesh.Replace(id, _mansardEdits);
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
        text.Append(CultureInfo.InvariantCulture, $"render_work\t{_fullBuildingPasses}\t{_buildingEdits}\t{_movementIndexVisits}\t{_movementQueries}\n");
        text.Append(CultureInfo.InvariantCulture, $"render_frames\t{_frameCount}\t{_frameMilliseconds:F3}\t{_frameMaximum:F3}\n");
        text.Append("# render\tlayer\tbatches\tuploads\tuploaded_instances\tuploaded_bytes\tupload_ms\tpending\n");
        foreach (var layer in Layers())
        {
            InstanceBuffer buffer = layer.Layer.Multimesh;
            text.Append(CultureInfo.InvariantCulture,
                $"render\t{layer.Name}\t{buffer.BatchCount}\t{buffer.Uploads}\t{buffer.UploadedInstances}\t{buffer.UploadedBytes}\t{buffer.UploadMilliseconds:F3}\t{buffer.PendingBatches}\n");
        }
    }
}

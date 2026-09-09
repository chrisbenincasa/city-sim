using System;
using System.Collections.Generic;
using System.Linq;
using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Evidence;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Godot;

namespace Borough.Shell;

public partial class Main
{
    private readonly Dictionary<Handle<Building>, Button> _supplyMarks = new();
    private World? _supplyWorld;
    private ulong _supplyReadAt;
    private Handle<Business> _selectedBusiness;
    private int _businessScroll;

    private string SupplySummary(SupplyEvidence reading)
    {
        if (!reading.Available) return "Supply explanation unavailable";
        if (reading.Shortfalls > 0)
        {
            string cause = reading.Primary.WaitingBin.IsNone ? "Supply explanation unavailable"
                : $"Waiting for {_names.Resource(reading.Primary.WaitingFor) ?? "an unnamed Resource"}";
            return cause + (reading.Shortfalls > 1 ? $" · +{reading.Shortfalls - 1} other shortfalls" : "")
                + (reading.Unavailable > 0 ? " · incomplete evidence" : "");
        }
        if (reading.Unavailable > 0) return "Supply explanation unavailable";
        return reading.RoutineWaits > 0 ? "Routine waiting · no supply shortfalls reported" : "No supply shortfalls reported";
    }

    private void RefreshSupplyMarks()
    {
        bool changedWorld = _supplyWorld != _world;
        if (changedWorld)
        {
            foreach (Button mark in _supplyMarks.Values) mark.QueueFree();
            _supplyMarks.Clear();
            _supplyWorld = _world;
        }
        ulong now = Time.GetTicksMsec();
        if (changedWorld || now - _supplyReadAt >= 250)
        {
            _supplyReadAt = now;
            var live = new HashSet<Handle<Building>>();
            for (int b = 0; b < _world.Buildings.Rows.SlotCount; b++)
            {
                if (!_world.Buildings.Rows.IsLive(b)) continue;
                if (!_world.Lots.Rows.TryResolve(_world.Buildings.Lot[b], out int lot)) continue;
                Vector3 point = new(_world.Lots.East[lot].Raw * MetresPerTile,
                    Math.Max(1, (int)_world.Lots.Storeys[lot]) * StoreyMetres + 4,
                    -_world.Lots.North[lot].Raw * MetresPerTile);
                if (_camera.IsPositionBehind(point)
                    || !GetViewport().GetVisibleRect().HasPoint(_camera.UnprojectPosition(point))) continue;
                var building = _world.Buildings.Rows.At(b);
                SupplyEvidence reading = Evidence.SupplyOfBuilding(_world, building);
                if (reading.Available && reading.Shortfalls == 0 && reading.Unavailable == 0) continue;
                live.Add(building);
                if (!_supplyMarks.TryGetValue(building, out Button? mark))
                {
                    World markerWorld = _world;
                    mark = InformationButton("!", () =>
                    {
                        if (_world != markerWorld || !_world.Buildings.Rows.IsValid(building)) return;
                        SelectInformation(building, default);
                        _expanded[SectionKey("attention")] = true;
                        _inspectionSignature = string.Empty;
                        RefreshInspection(true);
                    });
                    mark.Theme = _type;
                    mark.ThemeTypeVariation = "SupplyMark";
                    mark.CustomMinimumSize = new Vector2(36, 36);
                    _supplyMarks.Add(building, mark);
                    _hud.AddChild(mark);
                    _hud.MoveChild(mark, 0);
                }
                UiIcons.Glyph(mark, reading.Shortfalls > 0 ? "!" : "?", reading.Shortfalls > 0 ? "trouble" : "unavailable");
                mark.TooltipText = $"Building {RowId(_world.Buildings.Rows, building)} · {SupplySummary(reading)} · click to inspect";
            }
            foreach (var key in _supplyMarks.Keys.Where(k => !live.Contains(k)).ToArray())
            {
                _supplyMarks[key].QueueFree();
                _supplyMarks.Remove(key);
            }
        }
        var occupied = new List<Rect2>();
        foreach (var (building, mark) in _supplyMarks)
        {
            mark.AddThemeFontSizeOverride("font_size", Typed(18));
            if (_photographing || _menuOpen || !_world.Buildings.Rows.TryResolve(building, out int b)
                || !_world.Lots.Rows.TryResolve(_world.Buildings.Lot[b], out int lot)) { mark.Visible = false; continue; }
            Vector3 point = new(_world.Lots.East[lot].Raw * MetresPerTile,
                Math.Max(1, (int)_world.Lots.Storeys[lot]) * StoreyMetres + 4,
                -_world.Lots.North[lot].Raw * MetresPerTile);
            if (_camera.IsPositionBehind(point)) { mark.Visible = false; continue; }
            Vector2 screen = _camera.UnprojectPosition(point);
            Vector2 size = mark.GetCombinedMinimumSize().Max(new Vector2(36, 36));
            var rect = new Rect2(screen - new Vector2(size.X / 2, size.Y), size);
            if (!GetViewport().GetVisibleRect().Encloses(rect)
                || _informationPanels.Any(p => p.Visible && p.GetGlobalRect().Intersects(rect))
                || _toolsShown && _palette.GetGlobalRect().Intersects(rect)
                || _helpPanel.Visible || _tuner.Visible || _governing
                || occupied.Any(r => r.Intersects(rect))) { mark.Visible = false; continue; }
            mark.Position = rect.Position;
            mark.Size = rect.Size;
            mark.Visible = true;
            occupied.Add(rect);
        }
    }

    private void BusinessInformation(List<InformationSection> sections, out string title, out string identity)
    {
        title = "Business no longer exists";
        identity = "SELECTED BUSINESS";
        if (!_world.Businesses.Rows.TryResolve(_selectedBusiness, out int slot)) return;
        title = _names.BusinessKind(_world.Businesses.Kind[slot]) ?? "Business";
        identity += $" · {_world.Businesses.Rows.IdAt(slot)}";
        var home = _world.Businesses.Building[slot];
        var rows = new List<InformationRow>();
        if (_world.Buildings.Rows.IsValid(home))
        {
            ulong id = RowId(_world.Buildings.Rows, home);
            rows.Add(new($"Premises: Building {id} →", $"building {id}"));
        }
        else rows.Add(new("No current premises"));
        sections.Add(new("premises", "Location", true, rows));
        if (_world.Buildings.Rows.IsValid(home))
        {
            sections.Insert(0, new("summary", "Current condition", true,
                [new(SupplySummary(Evidence.SupplyOfBuilding(_world, home, business: _selectedBusiness)))]));
            BuildingEvidence evidence = Evidence.OfBuilding(_world, home);
            sections.Add(Attention(evidence, default, false, business: _selectedBusiness));
            sections.Add(Attention(evidence, default, false, true, _selectedBusiness));
        }
        var stocks = new List<InformationRow>();
        var bin = _world.Businesses.BinHead[slot];
        while (_world.Bins.Rows.TryResolve(bin, out int at))
        {
            if (!_world.Rules.IsConserved(_world.Bins.Resource[at]))
                stocks.Add(new($"{_names.Resource(_world.Bins.Resource[at]) ?? "Resource"}: {_world.Bins.LevelAt(at):N0} / {_world.Bins.Capacity[at]:N0} units", Icon: UiIcons.Resource(_names.Resource(_world.Bins.Resource[at]))));
            bin = _world.Bins.OwnerNext[at];
        }
        if (_world.Bins.Rows.TryResolve(_world.Businesses.Balance[slot], out int balance))
            stocks.Add(new($"Balance: {_world.Bins.LevelAt(balance):N0} money units"));
        if (stocks.Count == 0) stocks.Add(new("No stocks reported"));
        sections.Add(new("stocks", "Business stocks", true, stocks));
    }
}

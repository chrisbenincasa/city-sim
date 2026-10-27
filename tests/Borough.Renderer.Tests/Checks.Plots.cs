using System.Collections.Generic;
using System.IO;
using System.Linq;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Formats;
using Borough.Shell;

namespace Borough.Renderer.Tests;

public partial class Checks
{
    private static void CheckPlotTouches()
    {
        var loaded = RulesetLoader.Load(Path.GetFullPath(Path.Combine(Godot.ProjectSettings.GlobalizePath("res://"), "../../rulesets/shopping.toml")));
        Require(loaded.Ok, loaded.Describe());
        var key = WorldKey.FromSeed(0);
        var world = new World(400, loaded.Ruleset!, key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);
        var changes = new WorldChanges();
        world.Changes = changes;
        changes.Clear();
        var touches = new PlotTouches();
        touches.Reset(world);

        LotTable lots = world.Lots;
        int lot = -1;
        for (int slot = 0; slot < lots.Rows.SlotCount && lot < 0; slot++)
        {
            if (lots.Rows.IsLive(slot) && !lots.IsVacant(slot) && lots.Parcel(slot).IsValid
                && lots.Parcel(slot).AxisNorthQ16 == 0) lot = slot;
        }
        Require(lot >= 0, "fixture holds an occupied Lot");
        LandRectangle ground = lots.ParcelBounds(lot);
        var lotsTouched = new SortedSet<int>();
        var zonesTouched = new SortedSet<int>();
        void Find((long West, long South, long East, long North)[] boxes)
        {
            lotsTouched.Clear();
            zonesTouched.Clear();
            touches.Find(world, changes.Lots, changes.PermissionRectangles, boxes, lotsTouched, zonesTouched);
            changes.Clear();
        }

        var corner = new LandRectangle(ground.X, ground.Y, 1, 1);
        Require(world.LandPermissions.PaintUses(corner, LotTable.Trade, world.Rules.PermissionRecordLimit) == PermissionRefusal.None,
            "the fixture accepts a zoning edit");
        int[] painted = [.. changes.PermissionRectangles];
        Find([]);
        Require(painted.Length > 0 && zonesTouched.SetEquals(painted) && lotsTouched.Count == 0,
            "a zoning edit touches only its rectangles");

        var under = new SortedSet<int>();
        world.LandPermissions.Overlapping(ground, under);
        Require(under.Overlaps(painted), "the painted rectangle lies under the Lot");
        int building = lots.BuildingOn(lot);
        lots.Vacate(lot);
        Find([]);
        Require(lotsTouched.SetEquals([lot]) && zonesTouched.SetEquals(under), "a vacated Lot touches the zones under its old ground");
        lots.Vacate(lot);
        Find([]);
        Require(lotsTouched.SetEquals([lot]) && zonesTouched.Count == 0, "a vacant Lot's change touches no zone");
        lots.Occupy(lot, building);
        Find([]);
        Require(lotsTouched.SetEquals([lot]) && zonesTouched.SetEquals(under), "an occupied Lot touches the zones under its new ground");

        Find([(lots.East[lot].Raw, lots.North[lot].Raw, lots.East[lot].Raw + 1, lots.North[lot].Raw + 1)]);
        Require(lotsTouched.Contains(lot) && zonesTouched.IsSupersetOf(under), "a road edit's ground touches the Lots on it and their zones");
        Find([]);
        Require(lotsTouched.Count == 0 && zonesTouched.Count == 0, "a quiet Tick touches nothing");
    }
}

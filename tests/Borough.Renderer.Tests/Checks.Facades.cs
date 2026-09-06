using System;
using System.IO;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Formats;
using Borough.Shell;

namespace Borough.Renderer.Tests;

public partial class Checks
{
    private static void CheckFacades()
    {
        var loaded = RulesetLoader.Load(Path.GetFullPath(Path.Combine(Godot.ProjectSettings.GlobalizePath("res://"), "../../rulesets/shopping.toml")));
        Require(loaded.Ok, loaded.Describe());
        var key = WorldKey.FromSeed(0);
        var world = new World(400, loaded.Ruleset!, key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);
        int sample = -1;
        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (!world.Buildings.Rows.IsLive(slot)) continue;
            int room = world.DeclaredOccupancy(slot);
            int tenants = world.Tenants(slot);
            if (room > tenants && tenants > 0 && world.BuildingBusinesses.Length(slot) > 0)
            { sample = slot; break; }
        }
        Require(sample >= 0, "fixture contains partially occupied mixed premises");
        float expected = world.Tenants(sample) / (float)world.DeclaredOccupancy(sample);
        var hash = world.HashState();
        Require(FacadeAppearance.Occupancy(world, sample) == expected, "Business takes facade capacity alongside Households");
        Require(!FacadeAppearance.HasShopfront(world, sample), "work premises without commercial Goods storage do not invent a shopfront");
        Require(world.HashState() == hash, "reading facade appearance changes no State Hash");
        var building = world.Buildings.Rows.At(sample);
        world.AbandonBuilding(building, new Ticks(1));
        Require(FacadeAppearance.Occupancy(world, sample) == 0, "abandoned facade has no occupied openings");

        // The shader decodes this contract independently of paint or debug wash.
        foreach (byte seed in new byte[] { 0, 97, 255 })
        foreach (bool shop in new[] { false, true })
        foreach (bool ruin in new[] { false, true })
        foreach (bool upper in new[] { false, true })
        {
            float packed = FacadeAppearance.Pack(seed, shop, ruin) + (upper ? FacadeAppearance.UpperPart : 0);
            int flags = (int)MathF.Floor(packed / 256);
            Require(packed % 256 == seed, "seed survives flag packing including its endpoints");
            Require((flags % 2 == 1 && flags < 4) == (shop && !upper), "tower shaft cannot grow a shopfront");
            Require((flags / 2 % 2 == 1) == ruin, "abandonment is independent of palette and geometry component");
        }
    }
}

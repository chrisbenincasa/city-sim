using Borough.Core.Entities;
using Borough.Core.Rules;

namespace Borough.Shell;

public static class FacadeAppearance
{
    public const int UpperPart = 1024;

    // Float instance storage represents these small integers exactly; the shader decodes them.
    public static float Pack(byte seed, bool shopfront, bool abandoned) =>
        seed + (shopfront ? 256 : 0) + (abandoned ? 512 : 0);

    public static float Occupancy(World world, int building)
    {
        if (world.Buildings.IsAbandoned(building)) return 0;
        int room = world.DeclaredOccupancy(building);
        return room > 0 ? Godot.Mathf.Clamp(world.Tenants(building) / (float)room, 0, 1) : 1;
    }

    // Commercial premises retain their frontage while vacant. This describes space, not stock.
    public static bool HasShopfront(World world, int building)
    {
        foreach (BinDeclaration bin in world.Rules.BinsOf(world.Buildings.Kind[building]))
            if (bin.Tenancy == BinTenancy.Business && !world.Rules.IsConserved(bin.Resource)) return true;
        return false;
    }
}

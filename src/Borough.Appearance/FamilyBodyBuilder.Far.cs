namespace Borough.Appearance;

/// <summary>One far-facade cell and the near facade module it stands for.</summary>
public sealed record FamilyBodyFarCell(
    int Id,
    string Name,
    string PaintPart,
    float BayMetres,
    float WindowWidth,
    float WindowHeight,
    float WindowSill);

/// <summary>The far-facade cells used by one body variant.</summary>
public sealed record FamilyBodyFarFacade(string Variant, IReadOnlyList<FamilyBodyFarCell> Cells);

public static partial class FamilyBodyBuilder
{
    public static FamilyBodyFarFacade FarFacadeDescription(FamilyBody body)
    {
        ArgumentNullException.ThrowIfNull(body);
        if (body.Midrise is { } midrise)
        {
            return midrise.Variant switch
            {
                MidriseVariant.Mansion => MansionFarFacade(midrise),
                MidriseVariant.PanelSlab => SlabFarFacade(midrise),
                _ => throw new ArgumentOutOfRangeException(nameof(body)),
            };
        }

        if (body.Tower is { } tower) return TowerFarFacade(tower.Variant);
        throw new ArgumentException("Only mid-rise and tower bodies have a far facade.", nameof(body));
    }

    private static FamilyBodyFarFacade MansionFarFacade(MidriseBody body)
    {
        MansionLook look = Mansion with { Bay = body.ModuleMetres ?? Mansion.Bay };
        return new FamilyBodyFarFacade("mansion",
        [
            new(0, "mansion-party", "wall", look.Bay, 0f, 0f, 0f),
            new(1, "mansion-ground", "wall-end", look.Bay,
                look.GroundWindow.W, look.GroundWindow.H, look.GroundWindow.Sill),
            new(2, "mansion-typical", "wall", look.Bay,
                look.Window.W, look.Window.H, look.Window.Sill),
            new(3, "mansion-attic", "wall-end", look.Bay,
                look.AtticWindow.W, look.AtticWindow.H, look.AtticWindow.Sill),
            new(4, "mansion-end", "wall", look.Bay,
                look.Window.W, look.Window.H, look.Window.Sill),
        ]);
    }

    private static FamilyBodyFarFacade SlabFarFacade(MidriseBody body)
    {
        SlabLook look = PanelSlab with { Bay = body.ModuleMetres ?? PanelSlab.Bay };
        return new FamilyBodyFarFacade("panel-slab",
        [
            new(0, "slab-party", "wall", look.Bay, 0f, 0f, 0f),
            new(5, "slab-ground", "wall-end", look.Bay, 2.8f, 2.2f, .6f),
            new(6, "slab-typical", "wall", look.Bay,
                look.Window.W, look.Window.H, look.Window.Sill),
            new(7, "slab-end", "wall", look.Bay,
                .9f, look.Window.H, look.Window.Sill),
        ]);
    }

    private static FamilyBodyFarFacade TowerFarFacade(TowerVariant variant)
    {
        (string name, int first, string style) = variant switch
        {
            TowerVariant.Point => ("point", 1, "banded"),
            TowerVariant.SteppedPoint => ("stepped-point", 4, "curtain"),
            TowerVariant.L => ("l", 7, "ribbon"),
            TowerVariant.H => ("h", 10, "curtain"),
            _ => throw new ArgumentOutOfRangeException(nameof(variant)),
        };
        TowerFacadeParameters shaft = TowerFacades[style];
        TowerFacadeParameters podium = TowerFacades["podium"];
        return new FamilyBodyFarFacade(name,
        [
            new(0, "blank", "wall", 1f, 0f, 0f, 0f),
            new(first, $"{name}-podium-ground", "wall-end", 3f, 2.8f, 2.7f, .3f),
            new(first + 1, $"{name}-podium-typical", "wall-end", podium.FinEvery,
                podium.FinEvery - podium.FinWidth, Storey - podium.Spandrel, podium.Spandrel),
            new(first + 2, $"{name}-shaft", "wall", shaft.FinEvery,
                shaft.FinEvery - shaft.FinWidth, Storey - shaft.Spandrel, shaft.Spandrel),
            new(13, "crown", "wall", 1f, 0f, 0f, 0f),
        ]);
    }
}

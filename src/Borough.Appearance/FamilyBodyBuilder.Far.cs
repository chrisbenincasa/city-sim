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
                _ when IsCivic(midrise.Variant) => CivicFarFacade(midrise),
                _ when IsPerimeter(midrise.Variant) => PerimeterFarFacade(midrise),
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

    /// <summary>A perimeter form's cells. They take the mansion block's ids, and each window stands for the form's typical opening.</summary>
    private static FamilyBodyFarFacade PerimeterFarFacade(MidriseBody body)
    {
        PerimeterLook look = PerimeterLookOf(body);
        (string name, string wall, (float W, float H, float Sill) window, (float W, float H, float Sill) ground) = look.Form switch
        {
            MidriseVariant.WhiteRender => ("white-render", "wall", (1.5f, look.WindowHeight, look.Sill), (look.Bay - .7f, 2.4f, .6f)),
            MidriseVariant.Ribbon => ("ribbon", "spandrel", (look.Bay - .1f, look.Ribbon.High - look.Ribbon.Low, look.Ribbon.Low), (look.Bay - .3f, 2.7f, .3f)),
            MidriseVariant.Crosswall => ("crosswall", "panel", look.Window, (look.Bay - .7f, 2.4f, .6f)),
            MidriseVariant.BrickLoggia => ("brick-loggia", "wall", look.Window, (look.Bay - .7f, 2.4f, .6f)),
            MidriseVariant.TimberFrame => ("timber-frame", "wall", look.Window, (look.Bay - .7f, 2.4f, .6f)),
            _ => ("gable-front", "wall", look.Window, (look.Bay - .7f, 2.4f, .6f)),
        };
        float attic = MathF.Min(2.6f, look.Bay - .5f);
        return new FamilyBodyFarFacade(name,
        [
            new(0, $"{name}-party", "wall", look.Bay, 0f, 0f, 0f),
            new(1, $"{name}-ground", "wall-end", look.Bay, ground.W, ground.H, ground.Sill),
            new(2, $"{name}-typical", wall, look.Bay, window.W, window.H, window.Sill),
            new(MansionAtticFarCell, $"{name}-attic", "attic", look.Bay, attic, 2.6f, .2f),
            new(4, $"{name}-end", wall, look.Bay, window.W, window.H, window.Sill),
        ]);
    }

    /// <summary>A School or Gate form's cells. They take the mansion block's ids without the attic, and each window stands for the form's typical opening.</summary>
    private static FamilyBodyFarFacade CivicFarFacade(MidriseBody body)
    {
        CivicLook look = CivicLookOf(body);
        (string name, string wall, string groundWall, (float W, float H, float Sill) window, (float W, float H, float Sill) ground, (float W, float H, float Sill) end) = look.Form switch
        {
            MidriseVariant.SchoolRoofPlay => ("school-roof-play", "wall", "wall", look.Window, look.Window, (look.EndWindow, look.Window.H, look.Window.Sill)),
            MidriseVariant.SchoolRibbon => ("school-ribbon", "spandrel", "spandrel", (look.Bay - .1f, look.Ribbon.High - look.Ribbon.Low, look.Ribbon.Low),
                (look.Bay - .1f, look.Ribbon.High - look.Ribbon.Low, look.Ribbon.Low), default),
            MidriseVariant.SchoolBoard => ("school-board", "wall", "wall", look.Window, look.Window, look.Window),
            MidriseVariant.GateInterchange => ("gate-interchange", "wall", "wall-end", (look.Bay - .1f, look.Ribbon.High - look.Ribbon.Low, look.Ribbon.Low),
                (look.Bay - .2f, 2.6f, .3f), (look.Bay - .1f, look.Ribbon.High - look.Ribbon.Low, look.Ribbon.Low)),
            MidriseVariant.GateHall => ("gate-hall", "wall", "wall", (2f, 1.8f, .9f), (2f, 1.8f, .9f), (look.Bay - .1f, look.Ribbon.High - look.Ribbon.Low, look.Ribbon.Low)),
            _ => ("gate-station", "wall", "wall", look.Window, look.Window, look.Window),
        };
        return new FamilyBodyFarFacade(name,
        [
            new(0, $"{name}-party", "wall", look.Bay, 0f, 0f, 0f),
            new(1, $"{name}-ground", groundWall, look.Bay, ground.W, ground.H, ground.Sill),
            new(2, $"{name}-typical", wall, look.Bay, window.W, window.H, window.Sill),
            new(4, $"{name}-end", look.Form == MidriseVariant.SchoolRibbon ? "wall-end" : wall, look.Bay, end.W, end.H, end.Sill),
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

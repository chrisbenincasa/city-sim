using Borough.Core.Arithmetic;
using Borough.Core.Space;

namespace Borough.Core.Rules;

/// <summary>An authored form envelope, independent of geographic intensity permission.</summary>
public readonly record struct HousingForm(BlockPattern Pattern, int MinFrontage, int MaxFrontage,
    int MinDepth, int MaxDepth, byte Storeys, int Setback, int Weight);

/// <summary>Bounded, opt-in capacity-shortage construction. Absence preserves existing fixtures.</summary>
public sealed class HousingConstructionRuleset
{
    public HousingConstructionRuleset(int maxSeekers, int maxBuildingSlots, int maxLotSlots,
        int maxSources, int maxCandidates, int surplusPercent, int maxSurplus, HousingForm[] forms,
        int floorTilesPerOccupant, int alignmentBonus = 0, int sameFormBonus = 0)
    {
        ArgumentNullException.ThrowIfNull(forms);
        if (maxSeekers is < 1 or > 256 || maxBuildingSlots is < 1 or > 1048576
            || maxLotSlots is < 1 or > 1048576 || maxSources is < 1 or > 16
            || maxCandidates is < 1 or > 64 || surplusPercent is < 0 or > 100
            || maxSurplus is < 0 or > 256 || alignmentBonus is < 0 or > 1000
            || sameFormBonus < 0 || sameFormBonus > alignmentBonus || forms.Length is < 1 or > 32 || floorTilesPerOccupant <= 0)
        { throw new ArgumentException("Invalid housing construction bounds."); }
        MaxSeekers = maxSeekers; MaxBuildingSlots = maxBuildingSlots; MaxLotSlots = maxLotSlots;
        MaxSources = maxSources; MaxCandidates = maxCandidates; SurplusPercent = surplusPercent; MaxSurplus = maxSurplus;
        AlignmentBonus = alignmentBonus; SameFormBonus = sameFormBonus;
        _forms = (HousingForm[])forms.Clone();
        foreach (HousingForm form in _forms)
        {
            if ((uint)form.Pattern >= BlockPatterns.Count || form.MinFrontage < 1 || form.MaxFrontage < form.MinFrontage
                || form.MinDepth < 1 || form.MaxDepth < form.MinDepth || form.MaxFrontage > CellGrid.WorldTiles
                || form.MaxDepth > CellGrid.WorldTiles || form.Storeys == 0 || form.Setback < 0 || form.Setback > CellGrid.WorldTiles
                || form.Setback * 2 >= form.MinFrontage || form.Setback * 2 >= form.MinDepth
                || form.Weight is < 1 or > 1000
                || !BuildingPlan.TryFloorTiles(form.Pattern, form.MaxFrontage - form.Setback * 2,
                    form.MaxDepth - form.Setback * 2, form.Storeys, out int floor)
                || CapacityRuleset.Holds(floor, floorTilesPerOccupant) > Supported(maxSeekers))
            { throw new ArgumentException("A housing form is invalid or exceeds the assessment's maximum supported capacity."); }
        }
    }

    private readonly HousingForm[] _forms;
    public ReadOnlySpan<HousingForm> Forms => _forms;
    public int AlignmentBonus { get; }
    public int SameFormBonus { get; }
    public int MaxSeekers { get; }
    public int MaxBuildingSlots { get; }
    public int MaxLotSlots { get; }
    public int MaxSources { get; }
    public int MaxCandidates { get; }
    public int SurplusPercent { get; }
    public int MaxSurplus { get; }
    public int Supported(int seekers)
    {
        if (seekers <= 0) { return 0; }
        int surplus = (int)IntegerMath.CeilDiv((long)seekers * SurplusPercent, 100);
        return seekers + (surplus < MaxSurplus ? surplus : MaxSurplus);
    }
}

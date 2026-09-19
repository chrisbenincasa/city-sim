using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Core.Rules;

/// <summary>Bounded one- and two-Building comparisons; only the selected first Building is committed.</summary>
public static class HousingConstruction
{
    public static LocalLayoutProposal? Select(World world, WorldKey key, Handle<Lot> seed, byte kind)
    {
        HousingConstructionRuleset? rules = world.Rules.HousingConstruction;
        if (rules is null || !world.Lots.Rows.TryResolve(seed, out int seedRow)
            || !world.Lots.IsVacant(seedRow) || world.UnplacedPool.Count == 0
            || world.Lots.Rows.SlotCount > rules.MaxLotSlots || world.Buildings.Rows.SlotCount > rules.MaxBuildingSlots)
        { return null; }
        int seekers = world.UnplacedPool.Count < rules.MaxSeekers ? world.UnplacedPool.Count : rules.MaxSeekers;
        LocalLot origin = LocalLot.Read(world.Lots, seedRow);
        int street = Frontage.Locate(world.Roads.Streets, new Tiles(origin.East), new Tiles(origin.North), out _);
        if (street == Rows.NoSlot) { return null; }
        int nodeA = world.Roads.Nodes.Rows.Resolve(world.Roads.Segments.NodeA[street]);
        int nodeB = world.Roads.Nodes.Rows.Resolve(world.Roads.Segments.NodeB[street]);
        bool horizontal = world.Roads.Nodes.North[nodeA] == world.Roads.Nodes.North[nodeB];
        var evidence = new HousingNeedAssessment.Assessment(world, key, seekers);
        // Extend through whole vacant neighbours from the sampled address. Occupied or missing
        // ground terminates the window. The next trigger can enter the same run from another seed.
        var sources = new Handle<Lot>[rules.MaxSources];
        sources[0] = seed;
        int sourceCount = 1;
        LandRectangle end = origin.Parcel;
        while (sourceCount < sources.Length)
        {
            int next = Rows.NoSlot;
            for (int row = 0; row < world.Lots.Rows.SlotCount; row++)
            {
                if (!world.Lots.Rows.IsLive(row) || !world.Lots.IsVacant(row)) { continue; }
                LocalLot lot = LocalLot.Read(world.Lots, row);
                bool adjacent = horizontal
                    ? lot.Parcel.Y == end.Y && lot.Parcel.Height == end.Height && lot.Parcel.X == end.X + end.Width
                    : lot.Parcel.X == end.X && lot.Parcel.Width == end.Width && lot.Parcel.Y == end.Y + end.Height;
                if (adjacent && lot.Side == origin.Side
                    && Frontage.Locate(world.Roads.Streets, new Tiles(lot.East), new Tiles(lot.North), out _) == street)
                { next = row; break; }
            }
            if (next == Rows.NoSlot) { break; }
            sources[sourceCount++] = world.Lots.Rows.At(next);
            end = world.LotGround(next);
        }
        var candidates = new LocalLayoutProposal?[rules.MaxCandidates];
        var weights = new int[rules.MaxCandidates];
        int count = 0, attempts = 0;
        for (int from = 0; from < sourceCount; from++)
        {
            LandRectangle site = world.LotGround(world.Lots.Rows.Resolve(sources[from]));
            for (int length = 1; from + length <= sourceCount; length++)
            {
                if (length > 1)
                {
                    LandRectangle added = world.LotGround(world.Lots.Rows.Resolve(sources[from + length - 1]));
                    site = horizontal ? site with { Width = site.Width + added.Width } : site with { Height = site.Height + added.Height };
                }
                foreach (HousingForm form in rules.Forms)
                {
                    // The attempt budget bounds failed geometries as well as retained proposals.
                    if (attempts++ >= rules.MaxCandidates) { goto Collected; }
                    int frontage = horizontal ? site.Width : site.Height, depth = horizontal ? site.Height : site.Width;
                    if (frontage < form.MinFrontage || frontage > form.MaxFrontage || depth < form.MinDepth || depth > form.MaxDepth) { continue; }
                    var footprint = new LandRectangle(site.X + form.Setback, site.Y + form.Setback,
                        site.Width - 2 * form.Setback, site.Height - 2 * form.Setback);
                    var plan = new LocalBuildingPlan(kind, form.Pattern, footprint, form.Storeys);
                    if (!LocalLayout.Evaluate(world, sources.AsSpan(from, length), plan, out var proposal).Accepted) { continue; }
                    bool duplicate = false;
                    for (int i = 0; i < count; i++)
                    { if (candidates[i]!.Site == site && candidates[i]!.Building == plan) { duplicate = true; break; } }
                    if (duplicate || !evidence.Run(rules, proposal!).Accepted) { continue; }
                    candidates[count] = proposal;
                    weights[count++] = form.Weight + History(world, proposal!, horizontal, rules);
                }
            }
        }
    Collected:
        LocalLayoutProposal? selected = null;
        int bestServed = 0;
        ulong totalWeight = 0;
        for (int i = 0; i < count; i++)
        {
            LocalLayoutProposal first = candidates[i]!;
            bool includesSeed = false;
            foreach (LocalLot source in first.Sources) { if (source.Handle == seed) { includesSeed = true; break; } }
            if (!includesSeed) { continue; }
            int served = evidence.Run(rules, first).Served;
            for (int j = 0; j < count; j++)
            {
                LocalLayoutProposal second = candidates[j]!;
                if (Overlaps(first.Site, second.Site)) { continue; }
                HousingNeed arrangement = evidence.Run(rules, first, second);
                if (arrangement.Accepted && arrangement.Served > served) { served = arrangement.Served; }
            }
            if (served < bestServed) { continue; }
            if (served > bestServed) { bestServed = served; totalWeight = 0; }
            totalWeight += (ulong)(uint)weights[i];
            ulong entity = Randomness.Mix(world.Lots.Rows.IdAt(seedRow) ^ ((ulong)(uint)i << 32));
            if (Randomness.Draw(key, entity, world.Tick, PurposeTag.HousingConstructionForm) % totalWeight < (ulong)(uint)weights[i])
            { selected = first; }
        }
        return selected;
    }

    /// <summary>Fresh evidence and site checks immediately before the atomic transition.</summary>
    internal static LocalLayoutCheck Commit(World world, WorldKey key, LocalLayoutProposal proposal, out Handle<Building> building)
    {
        building = default;
        LocalLayoutCheck site = LocalLayout.Revalidate(world, proposal);
        if (!site.Accepted) { return site; }
        if (!AdmitsForm(world, proposal)) { return new(LocalLayoutRefusal.InvalidGeometry); }
        if (!HousingNeedAssessment.Evaluate(world, key, proposal).Accepted) { return new(LocalLayoutRefusal.HousingNeed); }
        return LocalLayoutCommit.Apply(world, proposal, key, out building);
    }

    private static bool AdmitsForm(World world, LocalLayoutProposal proposal)
    {
        if (world.Rules.HousingConstruction is not { } rules) { return false; }
        int street = world.Roads.Segments.Rows.Resolve(proposal.Street);
        int a = world.Roads.Nodes.Rows.Resolve(world.Roads.Segments.NodeA[street]);
        int b = world.Roads.Nodes.Rows.Resolve(world.Roads.Segments.NodeB[street]);
        bool horizontal = world.Roads.Nodes.North[a] == world.Roads.Nodes.North[b];
        LandRectangle site = proposal.Site;
        int frontage = horizontal ? site.Width : site.Height, depth = horizontal ? site.Height : site.Width;
        foreach (HousingForm form in rules.Forms)
        {
            if (form.Pattern == proposal.Building.Form && form.Storeys == proposal.Building.Storeys
                && frontage >= form.MinFrontage && frontage <= form.MaxFrontage && depth >= form.MinDepth && depth <= form.MaxDepth
                && proposal.Building.Footprint == new LandRectangle(site.X + form.Setback, site.Y + form.Setback,
                    site.Width - form.Setback * 2, site.Height - form.Setback * 2)) { return true; }
        }
        return false;
    }

    private static int History(World world, LocalLayoutProposal proposal, bool horizontal, HousingConstructionRuleset rules)
    {
        bool aligned = false, sameForm = false;
        LandRectangle site = proposal.Site, footprint = proposal.Building.Footprint;
        LocalLot source = proposal.Sources[0];
        for (int row = 0; row < world.Lots.Rows.SlotCount; row++)
        {
            if (!world.Lots.Rows.IsLive(row) || world.Lots.IsVacant(row)) { continue; }
            LocalLot neighbour = LocalLot.Read(world.Lots, row);
            if (neighbour.Side != source.Side || (horizontal ? neighbour.North != source.North : neighbour.East != source.East)) { continue; }
            LandRectangle ground = neighbour.Parcel;
            bool touches = horizontal
                ? ground.Y == site.Y && ground.Height == site.Height && (ground.X + ground.Width == site.X || site.X + site.Width == ground.X)
                : ground.X == site.X && ground.Width == site.Width && (ground.Y + ground.Height == site.Y || site.Y + site.Height == ground.Y);
            if (!touches) { continue; }
            bool front = horizontal
                ? (source.Side == (byte)StreetSide.Left ? footprint.Y == neighbour.Footprint.Y
                    : footprint.Y + footprint.Height == neighbour.Footprint.Y + neighbour.Footprint.Height)
                : (source.Side == (byte)StreetSide.Right ? footprint.X == neighbour.Footprint.X
                    : footprint.X + footprint.Width == neighbour.Footprint.X + neighbour.Footprint.Width);
            aligned |= front;
            sameForm |= front && neighbour.Pattern == (byte)proposal.Building.Form;
        }
        return (aligned ? rules.AlignmentBonus : 0) + (sameForm ? rules.SameFormBonus : 0);
    }

    private static bool Overlaps(LandRectangle a, LandRectangle b) => a.X < b.X + b.Width && b.X < a.X + a.Width
        && a.Y < b.Y + b.Height && b.Y < a.Y + a.Height;
}

using System.Text;
using S5.Lanes.Lanes;

namespace S5.Lanes.Harness;

/// <summary>
/// L3 — promotion and demotion, which is `adr/0016`'s own named revisit trigger and the only one
/// in the ADR with no machine beside it.
/// </summary>
internal static class PromotionReport
{
    /// <summary>
    /// S2 R2's count of Segments over an 80% volume/capacity threshold, on the **uniform** O-D
    /// rung. Quoted here as a fixture size and never as a stressed-Segment count: R2's own text
    /// says its `v/c` is unbounded because a Traveller passes through a Segment regardless of
    /// load, and `plans/0017` forbids citing any S2 figure without naming its rung.
    /// </summary>
    private const int Segments = 2_592;

    public static string Run()
    {
        var text = new StringBuilder();

        text.AppendLine("## L3 — promotion and demotion");
        text.AppendLine();
        text.AppendLine(
            "`adr/0016` names *\"promotion cost dominating the traffic budget\"* as a condition "
            + "that would reopen it and names no instrument. The condition is a ratio, so the "
            + "answer is one: promotion plus demotion per Vehicle, divided by the cost of running "
            + "that Vehicle for one Tick, gives a **break-even residency** in Ticks — the number of "
            + "Ticks a Segment must stay Microscopic for the queue to have been worth "
            + "materialising.");
        text.AppendLine();
        text.AppendLine(
            $"{Format.Count(Segments)} Segments of {Units.LanesPerSegment} Lanes, "
            + $"{Units.LanesPerSegment * Units.VehiclesPerLaneAtJam} in-flight Travellers each, "
            + "found through an intrusive index list threaded in arrival order rather than in "
            + "memory order — which is the structure `CLAUDE.md` mandates and is what a promotion "
            + "will actually walk.");
        text.AppendLine();

        int travellersPerSegment = Units.LanesPerSegment * Units.VehiclesPerLaneAtJam;
        var fixture = PromotionFixture.Build(
            Segments, Units.LanesPerSegment, travellersPerSegment, 0x5EEDUL);

        var promote = Timing.Measure(() => Fidelity.Promote(fixture), fixture.Travellers, 250, 9);

        // Demotion needs a populated network, and the promotion that populates it must be outside
        // the timer: demotion rewrites the Traveller list into queue order, so a repeated pair
        // would credit demotion with the saving that gives the *promotion* next time round.
        // Settled first: a queue straight out of promotion is at free flow everywhere, and
        // demotion of a free-flowing queue is the easy half of the job. 256 Ticks is enough for
        // the ring to form waves, which is the state a Segment demotes out of.
        const int settleTicks = 256;

        var demote = Timing.MeasureWithSetup(
            () =>
            {
                Fidelity.Promote(fixture);
                for (int tick = 0; tick < settleTicks; tick++)
                {
                    Idm.StepQueues(fixture.Network);
                }
            },
            () => Fidelity.Demote(fixture, 1000),
            fixture.Travellers,
            1,
            5);

        long demoteOnly = demote.PicosecondsPerUnit;

        Findings.PromotePicoseconds = promote.PicosecondsPerUnit;
        Findings.DemotePicoseconds = demoteOnly;

        text.AppendLine("| Conversion | ns/Vehicle | Vehicles in 15.6 ms |");
        text.AppendLine("|---|---:|---:|");
        text.AppendLine(
            $"| Promotion | {Format.Nanoseconds(promote.PicosecondsPerUnit)} "
            + $"| {Format.Count(promote.UnitsPerTickBudget)} |");
        text.AppendLine(
            $"| Demotion | {Format.Nanoseconds(demoteOnly)} "
            + $"| {Format.Count(demote.UnitsPerTickBudget)} |");
        text.AppendLine(
            $"| **Round trip** | **{Format.Nanoseconds(promote.PicosecondsPerUnit + demoteOnly)}** "
            + $"| {Format.Count(promote.PicosecondsPerUnit + demoteOnly == 0 ? 0 : (Units.TickBudgetNanoseconds * 1000L) / (promote.PicosecondsPerUnit + demoteOnly))} |");
        text.AppendLine();

        long residency = 0;
        if (Findings.NetworkPicoseconds > 0)
        {
            residency = (promote.PicosecondsPerUnit + demoteOnly) / Findings.NetworkPicoseconds;
        }

        text.AppendLine(
            $"**Break-even residency: {Format.Count(residency)} Ticks.** Below this, a Segment "
            + "spends more on changing representation than on being simulated. Against it: a "
            + $"Vehicle crosses a 128 m Segment at free flow in {Units.SegmentLengthTiles * 100 / 105} "
            + "Ticks, so a residency requirement above that means the average promoted Segment is "
            + "paying for a conversion it does not use.");
        text.AppendLine();

        text.AppendLine(
            $"**{Format.Count(fixture.Stalled)} of {Format.Count(fixture.Travellers)} Vehicles had "
            + "no arrival Tick to convert to** on the last demotion — they were at rest, and "
            + "`distance / speed` is undefined for them. `03` invariant 3 requires what demotion "
            + "discards to be enumerated, and this is a class of discard the corpus does not name: "
            + "not queue position or headway, which are listed, but the arrival time itself. A "
            + "Segment demoted while jammed cannot say when its Vehicles arrive.");
        text.AppendLine();
        text.AppendLine(
            "**The proportion is the fixture's and the phenomenon is not.** This Segment carries "
            + $"{Units.LanesPerSegment * Units.VehiclesPerLaneAtJam} Travellers across "
            + $"{Units.LanesPerSegment} Lanes of {Units.SegmentLengthTiles} Tiles, which is exactly "
            + "jam density, so after settling almost everything is stopped — a real state and the "
            + "worst one. Read the count as *this class of discard exists and is not enumerated*, "
            + "never as a rate.");
        text.AppendLine();

        return text.ToString();
    }
}

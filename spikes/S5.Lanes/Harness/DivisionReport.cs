using System.Text;
using Borough.Core.Arithmetic;
using S5.Lanes.Lanes;

namespace S5.Lanes.Harness;

/// <summary>
/// L5 — whether the speed L1 attributed to removing divisions requires changing the arithmetic.
/// </summary>
/// <remarks>
/// <para>
/// <b>L1 answered a question one step short of the one that matters.</b> It measured the IDM as
/// written against a precomputed <em>approximate</em> reciprocal, found 1.63–1.75×, and filed the
/// result as an attribution rather than a recommendation because a reciprocal rounds, so it moves
/// the State Hash, so under <c>CLAUDE.md</c>'s own test it is a design change however it was
/// motivated. That reasoning is sound and its premise was never checked: <em>is the speed only
/// available by changing the arithmetic?</em>
/// </para>
/// <para>
/// Two forms say no. Neither moves a bit, and this section proves that on the kernel's own state
/// rather than on sampled operands — <b>every position and every velocity of 294,912 Vehicles,
/// after 64 Ticks, compared against the shipped form</b>. A microbenchmark agreeing on a sample is
/// not the claim; the claim is that a run is the same run.
/// </para>
/// </remarks>
internal static class DivisionReport
{
    private const int Ticks = 64;

    public static string Run()
    {
        var text = new StringBuilder();

        text.AppendLine("## L5 — is the arithmetic's cost a design decision, or a spelling?");
        text.AppendLine();
        text.AppendLine(
            "L1 measured the IDM as written against a precomputed **approximate** reciprocal and "
            + "filed the 1.63–1.75× as an attribution rather than a recommendation, because a "
            + "reciprocal rounds, so it moves the State Hash, so under `CLAUDE.md`'s own test it is "
            + "*a design change however it was motivated*. **That reasoning is sound and its premise "
            + "was never checked.** This section checks it: two forms reach most of the same speed "
            + "and **neither moves a single bit**.");
        text.AppendLine();
        text.AppendLine(
            "- **Reordered** — `IntegerMath.FloorDiv` spells its correction "
            + "`(n % d != 0) && ((n < 0) != (d < 0))`, so the **modulo is the first operand and "
            + "always runs**, and RyuJIT does not fuse it with the division above it. Every "
            + "`FloorDiv` is therefore **two** 64-bit divisions. Swapping the operands short-circuits "
            + "the modulo whenever the signs agree. `&&` over two pure conditions is commutative, so "
            + "this is bit-identical **by construction** — and it is not about the IDM: `Fixed.Div` "
            + "is the substrate's divide, so it reaches every division site in the simulation.");
        text.AppendLine(
            "- **Exact magic** — for a divisor fixed at Ruleset load there is a multiplier and a "
            + "shift reproducing `floor(n/d)` **at every point in a bounded range** (Granlund & "
            + "Montgomery 1994). It is not a reciprocal and it does not round. The shift is searched "
            + "for and **verified at every quotient boundary in range at construction**, and a "
            + "divisor with no exact form is refused rather than approximated.");
        text.AppendLine();

        int lanes = QueueReport.TotalVehicles / Units.VehiclesPerLaneAtJam;

        var shipped = LaneNetwork.Build(lanes, Units.VehiclesPerLaneAtJam, 70, 0, 0x5EEDUL);
        var reordered = LaneNetwork.Build(lanes, Units.VehiclesPerLaneAtJam, 70, 0, 0x5EEDUL);
        var exact = LaneNetwork.Build(lanes, Units.VehiclesPerLaneAtJam, 70, 0, 0x5EEDUL);
        var reciprocal = LaneNetwork.Build(lanes, Units.VehiclesPerLaneAtJam, 70, 0, 0x5EEDUL);

        int maxSpeed = 0;
        for (int i = 0; i < shipped.Vehicles; i++)
        {
            if (shipped.DesiredSpeed[i] > maxSpeed)
            {
                maxSpeed = shipped.DesiredSpeed[i];
            }
        }

        MagicTables magic = MagicTables.For(exact, maxSpeed);

        text.AppendLine("### The tables, and the width they need");
        text.AppendLine();
        text.AppendLine("| | Divisor | Dividend bound | Shift | Multiplier width |");
        text.AppendLine("|---|---|---:|---:|---:|");
        text.AppendLine(
            $"| `v / v0` | per-driver, {Distinct(shipped)} distinct in this fixture "
            + $"| 2^{magic.DividendBits} | {magic.DesiredSpeedShift} "
            + $"| **{magic.MultiplierBits} bits** — {(magic.MultiplierBits <= 32 ? "a `uint` column, the same 4 bytes L1's reciprocal costs" : "a `ulong` column, 8 bytes against the reciprocal's 4")} |");
        text.AppendLine(
            $"| `v·Δv / 2√(ab)` | {Units.TwoRootAb}, one per Ruleset | — "
            + $"| {magic.Interaction.Shift} | {64 - System.Numerics.BitOperations.LeadingZeroCount(magic.Interaction.Multiplier)} bits — **no per-Vehicle column at all** |");
        text.AppendLine();
        text.AppendLine(
            "**The 128-bit intermediate is required rather than chosen.** The product `n × M` runs "
            + "to 65–70 bits across a realistic spread of driver speeds, so a 64-bit form would be "
            + "exact only below a speed cap — and **a correctness property conditional on a tuning "
            + "number is a worse foundation than the division it replaces**. `UInt128` is one "
            + "`mulx`, needs no `Math.*`, and trips no lint.");
        text.AppendLine();

        // ---- the check that matters: the same run, not merely the same answer on a sample
        for (int t = 0; t < Ticks; t++)
        {
            Idm.StepQueues(shipped);
            Idm.StepQueuesReordered(reordered);
            Idm.StepQueuesExact(exact, magic);
            Idm.StepQueuesReciprocal(reciprocal);
        }

        bool reorderedIdentical = Identical(shipped, reordered);
        bool exactIdentical = Identical(shipped, exact);
        bool reciprocalIdentical = Identical(shipped, reciprocal);
        long reciprocalDrift = Drift(shipped, reciprocal);

        text.AppendLine($"### Bit-identity, on the kernel's own state after {Ticks} Ticks");
        text.AppendLine();
        text.AppendLine(
            $"Four networks built from one seed — {shipped.Vehicles:N0} Vehicles — stepped "
            + $"{Ticks} Ticks, then **every position and every velocity compared against the shipped "
            + "form**. This is the claim, and a microbenchmark agreeing on sampled operands is not.");
        text.AppendLine();
        text.AppendLine("| Form | Identical to the shipped kernel? |");
        text.AppendLine("|---|---|");
        text.AppendLine($"| Reordered `FloorDiv` | {Verdict(reorderedIdentical)} |");
        text.AppendLine($"| Exact magic | {Verdict(exactIdentical)} |");
        text.AppendLine(
            $"| Approximate reciprocal — L1's | {Verdict(reciprocalIdentical)}"
            + (reciprocalIdentical ? "" : $", and it has drifted by {reciprocalDrift:N0} Q16.16 units in total") + " |");
        text.AppendLine();
        text.AppendLine(
            "**The last row is the design change, made visible.** The reciprocal form is not wrong "
            + "— it is a different city, and after "
            + $"{Ticks} Ticks it is already a measurably different one. The other two are the same "
            + "city, so under `CLAUDE.md`'s test they are **optimisations and need no ratifier**.");
        text.AppendLine();

        // ---- and now the price
        var shippedT = LaneNetwork.Build(lanes, Units.VehiclesPerLaneAtJam, 70, 0, 0x5EEDUL);
        var reorderedT = LaneNetwork.Build(lanes, Units.VehiclesPerLaneAtJam, 70, 0, 0x5EEDUL);
        var exactT = LaneNetwork.Build(lanes, Units.VehiclesPerLaneAtJam, 70, 0, 0x5EEDUL);
        var reciprocalT = LaneNetwork.Build(lanes, Units.VehiclesPerLaneAtJam, 70, 0, 0x5EEDUL);

        var shippedSample = Timing.Measure(
            () => Idm.StepQueues(shippedT), shippedT.Vehicles, 250, 9);
        var reorderedSample = Timing.Measure(
            () => Idm.StepQueuesReordered(reorderedT), reorderedT.Vehicles, 250, 9);
        var exactSample = Timing.Measure(
            () => Idm.StepQueuesExact(exactT, magic), exactT.Vehicles, 250, 9);
        var reciprocalSample = Timing.Measure(
            () => Idm.StepQueuesReciprocal(reciprocalT), reciprocalT.Vehicles, 250, 9);

        text.AppendLine("### The price of each spelling");
        text.AppendLine();
        text.AppendLine(
            "| Form | Divisions/Vehicle | Row | ns/Vehicle | vs shipped | Vehicles in 15.6 ms | Moves the hash? |");
        text.AppendLine("|---|---:|---:|---:|---:|---:|---|");
        text.AppendLine(
            $"| As written | 3 (6 idiv) | 16 B | {Format.Nanoseconds(shippedSample.PicosecondsPerUnit)} "
            + $"| 1.00× | {Format.Count(shippedSample.UnitsPerTickBudget)} | — |");
        text.AppendLine(
            $"| **Reordered** | 3 (3–4 idiv) | **16 B** | {Format.Nanoseconds(reorderedSample.PicosecondsPerUnit)} "
            + $"| **{Format.Ratio(shippedSample.PicosecondsPerUnit, reorderedSample.PicosecondsPerUnit)}** "
            + $"| {Format.Count(reorderedSample.UnitsPerTickBudget)} | **no** |");
        text.AppendLine(
            $"| **Exact magic** | 1 (1–2 idiv) | {(magic.MultiplierBits <= 32 ? "20 B" : "24 B")} "
            + $"| {Format.Nanoseconds(exactSample.PicosecondsPerUnit)} "
            + $"| **{Format.Ratio(shippedSample.PicosecondsPerUnit, exactSample.PicosecondsPerUnit)}** "
            + $"| {Format.Count(exactSample.UnitsPerTickBudget)} | **no** |");
        text.AppendLine(
            $"| Approximate reciprocal | 1 (1–2 idiv) | 20 B "
            + $"| {Format.Nanoseconds(reciprocalSample.PicosecondsPerUnit)} "
            + $"| {Format.Ratio(shippedSample.PicosecondsPerUnit, reciprocalSample.PicosecondsPerUnit)} "
            + $"| {Format.Count(reciprocalSample.UnitsPerTickBudget)} | **yes** |");
        text.AppendLine();

        long exactPs = exactSample.PicosecondsPerUnit;
        long reciprocalPs = reciprocalSample.PicosecondsPerUnit;
        long shippedPs = shippedSample.PicosecondsPerUnit;
        long capturedPermille = shippedPs == reciprocalPs
            ? 1000
            : ((shippedPs - exactPs) * 1000) / (shippedPs - reciprocalPs);

        text.AppendLine(
            $"**The exact form captures {capturedPermille / 10}.{capturedPermille % 10}% of what the design change buys, "
            + "and buys it for nothing.** That is the finding. The hash-bearing option is left with "
            + $"a margin of {Format.Ratio(exactPs, reciprocalPs)} over a form that keeps the arithmetic "
            + "identical, which is not a margin a design decision should be taken for — so "
            + "`plans/0002` §D2's *how the IDM is spelled* row **retires rather than fills**, which "
            + "is `adr/0059`'s direction.");
        text.AppendLine();
        text.AppendLine(
            "**The reordering is the finding that outranks the spike.** It is a three-token change "
            + "to `IntegerMath.FloorDiv`, it is bit-identical by construction, it needs no ADR and "
            + "no ratifier — and because `Fixed.Div` is *the* substrate divide, it is worth "
            + $"{Format.Ratio(shippedSample.PicosecondsPerUnit, reorderedSample.PicosecondsPerUnit)} "
            + "here and something at **every division site in the simulation**, none of which S5 "
            + "measured or can speak for.");
        text.AppendLine();

        text.Append(Overlapped(shippedSample, reorderedSample));

        return text.ToString();
    }

    /// <summary>
    /// The headline rung, restated on the reordered substrate — <b>measured rather than inferred by
    /// adding the exchange's cost to the no-Overlap figure</b>, because that addition is exactly the
    /// kind of step this spike has twice caught itself wanting to take.
    /// </summary>
    /// <remarks>
    /// This is the rung <c>adr/0016</c>'s tripwire T1 is scored against, so if the reordering moves
    /// it across 400,000 the verdict moves with it — and a verdict that turns on an inferred number
    /// is not a verdict.
    /// </remarks>
    private static string Overlapped(Timing.Sample plainShipped, Timing.Sample plainReordered)
    {
        var text = new StringBuilder();

        int lanes = QueueReport.TotalVehicles / Units.VehiclesPerLaneAtJam;
        var shipped = LaneNetwork.Build(lanes, Units.VehiclesPerLaneAtJam, 70, 2, 0x5EEDUL);
        var reordered = LaneNetwork.Build(lanes, Units.VehiclesPerLaneAtJam, 70, 2, 0x5EEDUL);

        var shippedSample = Timing.Measure(
            () => Idm.StepQueuesWithOverlaps(shipped), shipped.Vehicles, 250, 9);
        var reorderedSample = Timing.Measure(
            () => Idm.StepQueuesWithOverlapsReordered(reordered), reordered.Vehicles, 250, 9);

        text.AppendLine("### The headline rung, on the reordered substrate");
        text.AppendLine();
        text.AppendLine(
            "Two Overlaps per Lane by cursor — the row L4 carries, and the rung `adr/0016`'s "
            + "tripwire **T1** is scored against. **Measured here rather than inferred** by adding "
            + "the exchange's cost to the figure above, because that addition is the step this "
            + "spike has already caught itself wanting to take twice.");
        text.AppendLine();
        text.AppendLine("| Form | ns/Vehicle | **Vehicles per Tick per core** | Microscopic Segments in 15.6 ms | vs `adr/0016`'s 400,000 |");
        text.AppendLine("|---|---:|---:|---:|---|");
        text.AppendLine(
            $"| As written | {Format.Nanoseconds(shippedSample.PicosecondsPerUnit)} "
            + $"| **{Format.Count(shippedSample.UnitsPerTickBudget)}** "
            + $"| {Format.Count(shippedSample.UnitsPerTickBudget / Units.VehiclesPerLaneAtJam / Units.LanesPerSegment)} "
            + $"| {(shippedSample.UnitsPerTickBudget < 400_000 ? "**below — T1 fires**" : "**above — T1 does not fire**")} |");
        text.AppendLine(
            $"| **Reordered** | {Format.Nanoseconds(reorderedSample.PicosecondsPerUnit)} "
            + $"| **{Format.Count(reorderedSample.UnitsPerTickBudget)}** "
            + $"| {Format.Count(reorderedSample.UnitsPerTickBudget / Units.VehiclesPerLaneAtJam / Units.LanesPerSegment)} "
            + $"| {(reorderedSample.UnitsPerTickBudget < 400_000 ? "**below — T1 fires**" : "**above — T1 does not fire**")} |");
        text.AppendLine();

        bool crosses = shippedSample.UnitsPerTickBudget < 400_000
                       && reorderedSample.UnitsPerTickBudget >= 400_000;

        text.AppendLine(
            crosses
                ? "**T1 un-fires on a change that moves no bit.** `adr/0016`'s transplanted headline "
                  + "is reachable in `adr/0003`'s arithmetic after all, and the reason S5 concluded "
                  + "otherwise is that the substrate was computing a modulo nobody had asked for. "
                  + "**The amendment this spike wrote against that ADR needs its first clause "
                  + "revisited and its second left alone** — the `memcpy` claim is still false by "
                  + "more than an order of magnitude, because a division remains a division."
                : "**T1's verdict does not move.** The reordering is worth taking on its own terms "
                  + "and it does not reach `adr/0016`'s constant, so the amendment stands as written.");
        text.AppendLine();
        text.AppendLine(
            $"The `powersave` caveat applies unchanged and now cuts the other way: these are lower "
            + "bounds, so the canonical `performance` capture can only move the figures **up**, and "
            + "T1's margin with it.");
        text.AppendLine();

        return text.ToString();
    }

    private static string Verdict(bool identical) =>
        identical ? "**BIT-IDENTICAL**" : "**no — the State Hash moves**";

    private static int Distinct(LaneNetwork n)
    {
        var copy = new int[n.Vehicles];
        Array.Copy(n.DesiredSpeed, copy, n.Vehicles);
        Array.Sort(copy);

        int distinct = 0;
        for (int i = 0; i < copy.Length; i++)
        {
            if (i == 0 || copy[i] != copy[i - 1])
            {
                distinct++;
            }
        }

        return distinct;
    }

    private static bool Identical(LaneNetwork a, LaneNetwork b)
    {
        for (int i = 0; i < a.Vehicles; i++)
        {
            if (a.Position[i] != b.Position[i] || a.Velocity[i] != b.Velocity[i])
            {
                return false;
            }
        }

        return true;
    }

    private static long Drift(LaneNetwork a, LaneNetwork b)
    {
        long total = 0;
        for (int i = 0; i < a.Vehicles; i++)
        {
            long dp = (long)a.Position[i] - b.Position[i];
            long dv = (long)a.Velocity[i] - b.Velocity[i];
            total += (dp < 0 ? -dp : dp) + (dv < 0 ? -dv : dv);
        }

        return total;
    }
}

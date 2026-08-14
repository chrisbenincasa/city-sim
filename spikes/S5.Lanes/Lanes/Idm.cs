using Borough.Core.Arithmetic;

namespace S5.Lanes.Lanes;

/// <summary>
/// The kernel under test: one pass over a Lane's sorted queue, car-following by the Intelligent
/// Driver Model (Treiber, Hennecke &amp; Helbing 2000), in Q16.16.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every arithmetic operation here goes through <c>Borough.Core.Arithmetic.Fixed</c>, including
/// its <c>checked</c> narrowing.</b> That is the measurement. <c>adr/0016</c> takes its
/// order-of-magnitude claim from an engine that computed this in floating point, and the question
/// S5 exists to answer is what the same structure costs once <c>adr/0003</c> has been applied to
/// it. Reaching for a raw <c>long</c> multiply here to make the number look better would delete the
/// spike's subject.
/// </para>
/// <para>
/// <b>The follower reads the leader's Past.</b> The leader's position and velocity are captured
/// before they are overwritten, so the pass is a single sweep that nevertheless evaluates every
/// Vehicle against the same Tick's opening state — which is what <c>adr/0016</c> means by
/// <i>"Overlap exchange happens once per Tick against the Past"</i>, applied within the queue as
/// well as across it. It costs two registers and no second array.
/// </para>
/// </remarks>
internal static class Idm
{
    /// <summary>
    /// One Tick of car-following over every Lane, with no Overlaps. This is the pure
    /// <c>adr/0016</c> claim: O(n) down a sorted queue, no spatial index, no indirection.
    /// </summary>
    public static void StepQueues(LaneNetwork n)
    {
        int lanes = n.Lanes;
        int[] position = n.Position;
        int[] velocity = n.Velocity;
        int[] desired = n.DesiredSpeed;

        for (int lane = 0; lane < lanes; lane++)
        {
            int count = n.Count[lane];
            if (count == 0)
            {
                continue;
            }

            int block = n.BlockStart[lane];
            int length = n.LaneLength[lane];
            int head = n.Head[lane];

            int tail = head + count - 1;
            if (tail >= count)
            {
                tail -= count;
            }

            // The head's leader is the tail, one lap ahead. A ring has no free leader, which is
            // what keeps the kernel in the regime the Microscopic tier exists for.
            int leadPosition = position[block + tail] + length;
            int leadVelocity = velocity[block + tail];

            int i = head;
            for (int k = 0; k < count; k++)
            {
                int slot = block + i;
                int p = position[slot];
                int v = velocity[slot];

                int advanced = Advance(v, desired[slot], leadPosition - p, leadVelocity);

                position[slot] = p + advanced;
                velocity[slot] = advanced;

                leadPosition = p;
                leadVelocity = v;

                i++;
                if (i == count)
                {
                    i = 0;
                }
            }

            Wrap(n, lane, block, head, count, length);
        }
    }

    /// <summary>
    /// One Tick of car-following with the Overlap obstacles merged in. The obstacle list is sorted
    /// descending by construction, so the merge is one pointer and never a search.
    /// </summary>
    public static void StepQueuesWithOverlaps(LaneNetwork n)
    {
        int lanes = n.Lanes;
        int[] position = n.Position;
        int[] velocity = n.Velocity;
        int[] desired = n.DesiredSpeed;

        for (int lane = 0; lane < lanes; lane++)
        {
            int count = n.Count[lane];
            if (count == 0)
            {
                continue;
            }

            int block = n.BlockStart[lane];
            int length = n.LaneLength[lane];
            int head = n.Head[lane];
            int obstacleBase = lane * n.OverlapsPerLane;
            int obstacles = n.ObstacleCount[lane];
            int nextObstacle = 0;

            int tail = head + count - 1;
            if (tail >= count)
            {
                tail -= count;
            }

            int leadPosition = position[block + tail] + length;
            int leadVelocity = velocity[block + tail];

            int i = head;
            for (int k = 0; k < count; k++)
            {
                int slot = block + i;
                int p = position[slot];
                int v = velocity[slot];

                int effectivePosition = leadPosition;
                int effectiveVelocity = leadVelocity;

                // Obstacles above the leader are already covered by the leader and are dropped for
                // good: leadPosition decreases monotonically down the queue, so a skipped obstacle
                // can never become relevant again.
                while (nextObstacle < obstacles
                       && n.ObstaclePosition[obstacleBase + nextObstacle] >= leadPosition)
                {
                    nextObstacle++;
                }

                if (nextObstacle < obstacles
                    && n.ObstaclePosition[obstacleBase + nextObstacle] > p)
                {
                    effectivePosition = n.ObstaclePosition[obstacleBase + nextObstacle];
                    effectiveVelocity = n.ObstacleVelocity[obstacleBase + nextObstacle];
                }

                int advanced = Advance(v, desired[slot], effectivePosition - p, effectiveVelocity);

                position[slot] = p + advanced;
                velocity[slot] = advanced;

                leadPosition = p;
                leadVelocity = v;

                i++;
                if (i == count)
                {
                    i = 0;
                }
            }

            Wrap(n, lane, block, head, count, length);
        }
    }

    /// <summary>
    /// One Tick of car-following over a contiguous <em>range</em> of Lanes. The unit a thread is
    /// handed, and the body of <see cref="StepQueues"/> with its loop bounds made parameters.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="StepQueues"/> is deliberately left duplicating this rather than delegating to
    /// it.</b> Every one-core figure S5 has published came off that method as written, and routing a
    /// measured kernel through a new call boundary changes what the JIT may inline — so a delegating
    /// rewrite would move the published number by an unknown amount while looking like a tidy-up.
    /// L6 measures the two side by side at one thread precisely so the cost of this duplication is a
    /// row in the table instead of an assumption. <em>A refactor of the instrument is a change to the
    /// measurement until it has been shown not to be.</em>
    /// </para>
    /// <para>
    /// <b>The range is the whole of what makes threading safe here, and it is a property of the data
    /// layout rather than of this method.</b> Every read and every write below is inside
    /// <c>BlockStart[lane] .. +Count[lane]</c> or is <c>Head[lane]</c>; no Lane touches another
    /// Lane's rows, and there is no accumulator. Disjoint ranges therefore cannot race, and the
    /// result cannot depend on how they were scheduled — which is <c>05 §4</c> lint 4 holding by
    /// construction rather than by test, though L6 tests it anyway.
    /// </para>
    /// </remarks>
    private static void StepQueueRange(LaneNetwork n, int from, int to)
    {
        int[] position = n.Position;
        int[] velocity = n.Velocity;
        int[] desired = n.DesiredSpeed;

        for (int lane = from; lane < to; lane++)
        {
            int count = n.Count[lane];
            if (count == 0)
            {
                continue;
            }

            int block = n.BlockStart[lane];
            int length = n.LaneLength[lane];
            int head = n.Head[lane];

            int tail = head + count - 1;
            if (tail >= count)
            {
                tail -= count;
            }

            int leadPosition = position[block + tail] + length;
            int leadVelocity = velocity[block + tail];

            int i = head;
            for (int k = 0; k < count; k++)
            {
                int slot = block + i;
                int p = position[slot];
                int v = velocity[slot];

                int advanced = Advance(v, desired[slot], leadPosition - p, leadVelocity);

                position[slot] = p + advanced;
                velocity[slot] = advanced;

                leadPosition = p;
                leadVelocity = v;

                i++;
                if (i == count)
                {
                    i = 0;
                }
            }

            Wrap(n, lane, block, head, count, length);
        }
    }

    /// <summary>
    /// One Tick of car-following over every Lane, split across <paramref name="threads"/> contiguous
    /// Lane ranges. <paramref name="threads"/> of 1 is the control and runs the identical body on the
    /// calling thread, so the scaling ratio is taken over one implementation rather than two.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The partition is contiguous and static, which is the honest shape for this kernel.</b>
    /// Every Lane in the measured networks holds the same number of Vehicles, so an equal split of
    /// Lanes is an equal split of work and a work-stealing scheduler would be measuring its own
    /// load balancer. A city's Lanes are <em>not</em> equal, and that is a real limit on how far this
    /// number carries — recorded in L6 rather than smoothed over here.
    /// </para>
    /// <para>
    /// <b>The result is bit-identical to <see cref="StepQueues"/> at every thread count</b>, because
    /// the ranges are disjoint and every write is Lane-local. That is asserted rather than asserted
    /// about: L6 compares the full <c>Position</c>, <c>Velocity</c> and <c>Head</c> arrays against a
    /// serial run before it reports a single timing.
    /// </para>
    /// </remarks>
    public static void StepQueuesThreaded(LaneNetwork n, int threads)
    {
        if (threads <= 1)
        {
            StepQueueRange(n, 0, n.Lanes);
            return;
        }

        int lanes = n.Lanes;

        System.Threading.Tasks.Parallel.For(
            0,
            threads,
            new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = threads },
            slice =>
            {
                // Both operands are non-negative, so the rounding is not in doubt — but BOR0203
                // requires it stated rather than inferred, and a partition that disagreed with
                // itself at a boundary would drop or double a Lane's worth of Vehicles.
                int from = (int)Borough.Core.Arithmetic.IntegerMath.FloorDiv(
                    (long)lanes * slice, threads);
                int to = (int)Borough.Core.Arithmetic.IntegerMath.FloorDiv(
                    (long)lanes * (slice + 1), threads);
                StepQueueRange(n, from, to);
            });
    }

    /// <summary>
    /// The same pass with the two <em>constant-denominator</em> divisions replaced by precomputed
    /// reciprocal multiplies.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is an attribution, not an optimisation, and the distinction is the whole point.</b>
    /// The IDM as written divides three times per Vehicle per Tick, and two of those denominators
    /// never vary: <c>2√(ab)</c> is a constant of the Ruleset, and <c>v0</c> is a constant of the
    /// driver. A 64-bit integer division is tens of cycles and does not pipeline, where a float
    /// division is a handful and does — so this is precisely where the transplant from Citybound's
    /// engine can be expected to cost, and measuring the two forms says how much of the gap is the
    /// arithmetic rather than the structure.
    /// </para>
    /// <para>
    /// The third division, <c>s* / s</c>, has a denominator that is the gap to the vehicle in front.
    /// It varies every Tick for every Vehicle and no reciprocal exists for it, so this variant is
    /// the floor of what the substitution can buy rather than an alternative implementation.
    /// </para>
    /// <para>
    /// It costs a fifth column — <c>1/v0</c> per Vehicle — which takes the row from 16 bytes to 20
    /// and is stated in the report rather than hidden, because a 25% wider row is not free at the
    /// rungs where the sweep is bandwidth-bound.
    /// </para>
    /// </remarks>
    public static void StepQueuesReciprocal(LaneNetwork n)
    {
        int lanes = n.Lanes;
        int[] position = n.Position;
        int[] velocity = n.Velocity;
        int[] inverseDesired = n.InverseDesiredSpeed;

        for (int lane = 0; lane < lanes; lane++)
        {
            int count = n.Count[lane];
            if (count == 0)
            {
                continue;
            }

            int block = n.BlockStart[lane];
            int length = n.LaneLength[lane];
            int head = n.Head[lane];

            int tail = head + count - 1;
            if (tail >= count)
            {
                tail -= count;
            }

            int leadPosition = position[block + tail] + length;
            int leadVelocity = velocity[block + tail];

            int i = head;
            for (int k = 0; k < count; k++)
            {
                int slot = block + i;
                int p = position[slot];
                int v = velocity[slot];

                int advanced = AdvanceReciprocal(
                    v, inverseDesired[slot], leadPosition - p, leadVelocity);

                position[slot] = p + advanced;
                velocity[slot] = advanced;

                leadPosition = p;
                leadVelocity = v;

                i++;
                if (i == count)
                {
                    i = 0;
                }
            }

            Wrap(n, lane, block, head, count, length);
        }
    }

    private static int AdvanceReciprocal(
        int v, int inverseDesiredSpeed, int separation, int leadVelocity)
    {
        int gap = separation - Units.VehicleLength;
        if (gap < Units.GapFloor)
        {
            gap = Units.GapFloor;
        }

        int closing = v - leadVelocity;
        int interaction = Fixed.Mul(v, Units.DesiredHeadwayTicks)
            + Fixed.Mul(Fixed.Mul(v, closing), Units.InverseTwoRootAb);
        if (interaction < 0)
        {
            interaction = 0;
        }

        int desiredGap = Units.MinimumGap + interaction;

        int ratio = Fixed.Div(desiredGap, gap);
        if (ratio > Units.MaxGapRatio)
        {
            ratio = Units.MaxGapRatio;
        }

        int braking = Fixed.Mul(ratio, ratio);

        int speedRatio = Fixed.Mul(v, inverseDesiredSpeed);
        int squared = Fixed.Mul(speedRatio, speedRatio);
        int fourth = Fixed.Mul(squared, squared);

        int acceleration = Fixed.Mul(Units.MaxAcceleration, Fixed.One - fourth - braking);

        int next = v + acceleration;
        if (next < 0)
        {
            next = 0;
        }

        return next;
    }

    /// <summary>
    /// The same pass with <c>FloorDiv</c>'s correction reordered. <b>Nothing else changes, and the
    /// output is bit-identical to <see cref="StepQueues"/>.</b>
    /// </summary>
    /// <remarks>
    /// <c>IntegerMath.FloorDiv</c> evaluates <c>n % d</c> as the first operand of its <c>&amp;&amp;</c>,
    /// so the modulo runs on every call, and RyuJIT does not fuse it with the division above it. All
    /// three of the IDM's divisions have a non-negative numerator and a positive divisor except the
    /// interaction term, whose numerator straddles zero — so two of the three skip the modulo
    /// outright and the third skips it about half the time. This variant exists to separate *what
    /// the substrate spells badly* from *what integer arithmetic costs*, which L1 measured together.
    /// </remarks>
    public static void StepQueuesReordered(LaneNetwork n)
    {
        int lanes = n.Lanes;
        int[] position = n.Position;
        int[] velocity = n.Velocity;
        int[] desired = n.DesiredSpeed;

        for (int lane = 0; lane < lanes; lane++)
        {
            int count = n.Count[lane];
            if (count == 0)
            {
                continue;
            }

            int block = n.BlockStart[lane];
            int length = n.LaneLength[lane];
            int head = n.Head[lane];

            int tail = head + count - 1;
            if (tail >= count)
            {
                tail -= count;
            }

            int leadPosition = position[block + tail] + length;
            int leadVelocity = velocity[block + tail];

            int i = head;
            for (int k = 0; k < count; k++)
            {
                int slot = block + i;
                int p = position[slot];
                int v = velocity[slot];

                int advanced = AdvanceReordered(v, desired[slot], leadPosition - p, leadVelocity);

                position[slot] = p + advanced;
                velocity[slot] = advanced;

                leadPosition = p;
                leadVelocity = v;

                i++;
                if (i == count)
                {
                    i = 0;
                }
            }

            Wrap(n, lane, block, head, count, length);
        }
    }

    private static int AdvanceReordered(int v, int desiredSpeed, int separation, int leadVelocity)
    {
        int gap = separation - Units.VehicleLength;
        if (gap < Units.GapFloor)
        {
            gap = Units.GapFloor;
        }

        int closing = v - leadVelocity;
        int interaction = Fixed.Mul(v, Units.DesiredHeadwayTicks)
            + ExactDivision.DivReordered(Fixed.Mul(v, closing), Units.TwoRootAb);
        if (interaction < 0)
        {
            interaction = 0;
        }

        int desiredGap = Units.MinimumGap + interaction;

        int ratio = ExactDivision.DivReordered(desiredGap, gap);
        if (ratio > Units.MaxGapRatio)
        {
            ratio = Units.MaxGapRatio;
        }

        int braking = Fixed.Mul(ratio, ratio);

        int speedRatio = ExactDivision.DivReordered(v, desiredSpeed);
        int squared = Fixed.Mul(speedRatio, speedRatio);
        int fourth = Fixed.Mul(squared, squared);

        int acceleration = Fixed.Mul(Units.MaxAcceleration, Fixed.One - fourth - braking);

        int next = v + acceleration;
        if (next < 0)
        {
            next = 0;
        }

        return next;
    }

    /// <summary>
    /// <see cref="StepQueuesWithOverlaps"/> with <c>FloorDiv</c>'s correction reordered, so that the
    /// headline figure — which carries Overlaps — can be stated on the reordered substrate rather
    /// than inferred from the no-Overlap rung by addition.
    /// </summary>
    public static void StepQueuesWithOverlapsReordered(LaneNetwork n)
    {
        int lanes = n.Lanes;
        int[] position = n.Position;
        int[] velocity = n.Velocity;
        int[] desired = n.DesiredSpeed;

        for (int lane = 0; lane < lanes; lane++)
        {
            int count = n.Count[lane];
            if (count == 0)
            {
                continue;
            }

            int block = n.BlockStart[lane];
            int length = n.LaneLength[lane];
            int head = n.Head[lane];
            int obstacleBase = lane * n.OverlapsPerLane;
            int obstacles = n.ObstacleCount[lane];
            int nextObstacle = 0;

            int tail = head + count - 1;
            if (tail >= count)
            {
                tail -= count;
            }

            int leadPosition = position[block + tail] + length;
            int leadVelocity = velocity[block + tail];

            int i = head;
            for (int k = 0; k < count; k++)
            {
                int slot = block + i;
                int p = position[slot];
                int v = velocity[slot];

                int effectivePosition = leadPosition;
                int effectiveVelocity = leadVelocity;

                while (nextObstacle < obstacles
                       && n.ObstaclePosition[obstacleBase + nextObstacle] >= leadPosition)
                {
                    nextObstacle++;
                }

                if (nextObstacle < obstacles
                    && n.ObstaclePosition[obstacleBase + nextObstacle] > p)
                {
                    effectivePosition = n.ObstaclePosition[obstacleBase + nextObstacle];
                    effectiveVelocity = n.ObstacleVelocity[obstacleBase + nextObstacle];
                }

                int advanced = AdvanceReordered(
                    v, desired[slot], effectivePosition - p, effectiveVelocity);

                position[slot] = p + advanced;
                velocity[slot] = advanced;

                leadPosition = p;
                leadVelocity = v;

                i++;
                if (i == count)
                {
                    i = 0;
                }
            }

            Wrap(n, lane, block, head, count, length);
        }
    }

    /// <summary>
    /// The same pass with the two constant-denominator divisions replaced by <b>exact</b>
    /// multiplier-and-shift forms, and the third division's correction reordered.
    /// <b>The output is bit-identical to <see cref="StepQueues"/>.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the variant that decides whether the speed L1 attributed is a design change or
    /// not.</b> L1's reciprocal is approximate — it rounds twice, so it moves the State Hash, so
    /// under <c>CLAUDE.md</c>'s own test it is a design change however it was motivated. A magic
    /// divisor is not approximate: it reproduces <c>floor(n/d)</c> at every point in a bounded
    /// range, verified at construction. If this variant lands near the reciprocal's cost, the
    /// hash-bearing choice is not worth making and <c>plans/0002</c> §D2's row retires rather than
    /// fills.
    /// </para>
    /// <para>
    /// The per-Vehicle column is a multiplier and the shift is shared, so the row grows by the same
    /// 4 or 8 bytes the reciprocal form grows by — reported by <see cref="MagicTables"/> rather
    /// than assumed, because that width is the honest half of the comparison.
    /// </para>
    /// </remarks>
    public static void StepQueuesExact(LaneNetwork n, MagicTables magic)
    {
        int lanes = n.Lanes;
        int[] position = n.Position;
        int[] velocity = n.Velocity;
        ulong[] speedMultiplier = magic.DesiredSpeedMultiplier;
        int speedShift = magic.DesiredSpeedShift;
        MagicDivisor interaction = magic.Interaction;

        for (int lane = 0; lane < lanes; lane++)
        {
            int count = n.Count[lane];
            if (count == 0)
            {
                continue;
            }

            int block = n.BlockStart[lane];
            int length = n.LaneLength[lane];
            int head = n.Head[lane];

            int tail = head + count - 1;
            if (tail >= count)
            {
                tail -= count;
            }

            int leadPosition = position[block + tail] + length;
            int leadVelocity = velocity[block + tail];

            int i = head;
            for (int k = 0; k < count; k++)
            {
                int slot = block + i;
                int p = position[slot];
                int v = velocity[slot];

                int advanced = AdvanceExact(
                    v, speedMultiplier[slot], speedShift, leadPosition - p, leadVelocity, interaction);

                position[slot] = p + advanced;
                velocity[slot] = advanced;

                leadPosition = p;
                leadVelocity = v;

                i++;
                if (i == count)
                {
                    i = 0;
                }
            }

            Wrap(n, lane, block, head, count, length);
        }
    }

    private static int AdvanceExact(
        int v,
        ulong speedMultiplier,
        int speedShift,
        int separation,
        int leadVelocity,
        MagicDivisor interactionMagic)
    {
        int gap = separation - Units.VehicleLength;
        if (gap < Units.GapFloor)
        {
            gap = Units.GapFloor;
        }

        int closing = v - leadVelocity;
        int interaction = Fixed.Mul(v, Units.DesiredHeadwayTicks)
            + interactionMagic.DivideFixed(Fixed.Mul(v, closing));
        if (interaction < 0)
        {
            interaction = 0;
        }

        int desiredGap = Units.MinimumGap + interaction;

        // The gap in front is the one denominator that varies, so it keeps a real division — with
        // the correction reordered, which is free. This is the floor S5's L1 already identified.
        int ratio = ExactDivision.DivReordered(desiredGap, gap);
        if (ratio > Units.MaxGapRatio)
        {
            ratio = Units.MaxGapRatio;
        }

        int braking = Fixed.Mul(ratio, ratio);

        // v is never negative — the pass floors it — so this site needs no sign correction at all.
        int speedRatio = checked((int)(long)(
            ((UInt128)speedMultiplier * (UInt128)(ulong)((long)v << 16)) >> speedShift));
        int squared = Fixed.Mul(speedRatio, speedRatio);
        int fourth = Fixed.Mul(squared, squared);

        int acceleration = Fixed.Mul(Units.MaxAcceleration, Fixed.One - fourth - braking);

        int next = v + acceleration;
        if (next < 0)
        {
            next = 0;
        }

        return next;
    }

    /// <summary>
    /// The IDM itself: returns the Vehicle's velocity after one Tick, which — the Tick being the
    /// integration step — is also the distance it advances.
    /// </summary>
    /// <param name="v">Q16.16 Tiles per Tick.</param>
    /// <param name="desiredSpeed">Q16.16 Tiles per Tick, this driver's own <c>v0</c>.</param>
    /// <param name="separation">Q16.16 Tiles between the two positions, before the leader's length.</param>
    /// <param name="leadVelocity">Q16.16 Tiles per Tick.</param>
    private static int Advance(int v, int desiredSpeed, int separation, int leadVelocity)
    {
        int gap = separation - Units.VehicleLength;
        if (gap < Units.GapFloor)
        {
            gap = Units.GapFloor;
        }

        // s* = s0 + max(0, v·T + v·Δv / 2√(ab)). 2√(ab) is a constant of the parameter set, so
        // there is no square root in the inner loop and the integer transplant costs nothing here.
        int closing = v - leadVelocity;
        int interaction = Fixed.Mul(v, Units.DesiredHeadwayTicks)
            + Fixed.Div(Fixed.Mul(v, closing), Units.TwoRootAb);
        if (interaction < 0)
        {
            interaction = 0;
        }

        int desiredGap = Units.MinimumGap + interaction;

        int ratio = Fixed.Div(desiredGap, gap);
        if (ratio > Units.MaxGapRatio)
        {
            ratio = Units.MaxGapRatio;
        }

        int braking = Fixed.Mul(ratio, ratio);

        // (v/v0)^4 — the acceleration exponent δ = 4, which is two squarings and no power function.
        int speedRatio = Fixed.Div(v, desiredSpeed);
        int squared = Fixed.Mul(speedRatio, speedRatio);
        int fourth = Fixed.Mul(squared, squared);

        int acceleration = Fixed.Mul(Units.MaxAcceleration, Fixed.One - fourth - braking);

        int next = v + acceleration;
        if (next < 0)
        {
            next = 0;
        }

        return next;
    }

    /// <summary>
    /// Rotates the ring if the leading Vehicle crossed the far end. One Vehicle at most can cross
    /// in a Tick: free-flow displacement is 1.05 Tiles and jam spacing is 1.75.
    /// </summary>
    /// <summary>
    /// Rotates the ring for every leading Vehicle that crossed the far end.
    /// </summary>
    /// <remarks>
    /// In steady state at most one Vehicle crosses per Tick — free-flow displacement is 1.05 Tiles
    /// against a jam spacing of 1.75 — so the loop runs once. It is a loop rather than a test
    /// because a queue that has just been materialised from in-flight Trips has whatever spacing
    /// the Trips had, and the sorted invariant must survive the first Tick after a promotion as
    /// well as the thousandth.
    /// </remarks>
    private static void Wrap(LaneNetwork n, int lane, int block, int head, int count, int length)
    {
        for (int guard = 0; guard < count; guard++)
        {
            int slot = block + head;
            if (n.Position[slot] < length)
            {
                break;
            }

            n.Position[slot] -= length;
            head++;
            if (head == count)
            {
                head = 0;
            }
        }

        n.Head[lane] = head;
    }
}

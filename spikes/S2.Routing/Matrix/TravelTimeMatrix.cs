using Borough.Core.Arithmetic;
using S2.Routing.Graph;

namespace S2.Routing.Matrix;

/// <summary>
/// The District-to-District travel-time matrix: <c>n²</c> Q16.16 Ticks, row-major, asymmetric.
/// </summary>
/// <remarks>
/// <para>
/// <b>The prescribed first measurement, and the one that may dissolve the headline question.</b>
/// <c>references.md §2</c>, quoted by <c>plans/0010</c> as <i>"the single most important sentence for
/// planning this spike"</i>: if this matrix carries the choice loop, <i>"the detailed-tier router only
/// handles vehicle steering, and the many-to-many argument for distance-vector largely evaporates"</i>
/// — which is why R4 is conditional on what R1 and R2 find.
/// </para>
/// <para>
/// <b>Row-major, and the layout is load-bearing rather than incidental.</b> <c>references.md §2</c>
/// describes the choice loop twice in one sentence — <i>"what is the commute from this candidate
/// dwelling to any job?"</i>, which is one origin against many destinations and therefore a
/// <b>sequential row scan</b>, and <i>"many-to-many, evaluated tens of thousands of times per
/// cycle"</i>, which reads as scattered. Under this layout the first is a stream and the second is a
/// miss per read, and <c>plans/0010</c> requires both measured because <i>"the two differ by an order
/// of magnitude at 2,000 Districts and are indistinguishable at 100"</i>.
/// </para>
/// <para>
/// <b>It carries no Epoch, and R1 is required to say so.</b> <c>plans/0010</c> R5 flags that the
/// corpus holds two unrelated invalidation mechanisms: routes invalidate against a <b>scalar</b>
/// Epoch, while the matrix is described in <c>02 §6</c> as rebuilt at a <i>slow cadence, dirty regions
/// only</i> — a <b>spatial</b> one. <i>"Two invalidation mechanisms are in the corpus and nothing
/// relates them, so the matrix and the cache can disagree about what the network currently is."</i>
/// This type deliberately has no version field, so that disagreement is visible in the code rather
/// than papered over by a counter that would imply a relationship nobody has argued.
/// </para>
/// </remarks>
internal sealed class TravelTimeMatrix
{
    private TravelTimeMatrix(int count, int[] entries)
    {
        Count = count;
        Entries = entries;
    }

    /// <summary>Districts. The matrix's <c>n</c>.</summary>
    public int Count { get; }

    /// <summary>
    /// <c>n²</c> Q16.16 Ticks, row-major, <see cref="OneToAll.Unreachable"/> where no route exists.
    /// Exposed as the bare array because the read benchmark must measure the read and not a property.
    /// </summary>
    public int[] Entries { get; }

    /// <summary>Bytes resident. The figure the L3 ceiling is read against.</summary>
    public long ResidentBytes => (long)Count * Count * sizeof(int);

    public int this[int from, int to] => Entries[(from * Count) + to];

    /// <summary>An independent copy, so an incremental rebuild can be checked against a full one.</summary>
    public TravelTimeMatrix Clone() => new(Count, (int[])Entries.Clone());

    /// <summary>
    /// A cold build: one forward one-to-all per District, filling that District's whole row.
    /// </summary>
    public static TravelTimeMatrix Build(Districts districts, int[] arcTicks, OneToAll search)
    {
        int count = districts.Count;
        var entries = new int[count * count];

        for (int from = 0; from < count; from++)
        {
            int source = districts.Representative[from];
            int row = from * count;

            if (source < 0)
            {
                Array.Fill(entries, OneToAll.Unreachable, row, count);
                continue;
            }

            search.Run(source, arcTicks);

            for (int to = 0; to < count; to++)
            {
                int target = districts.Representative[to];
                entries[row + to] = target < 0 ? OneToAll.Unreachable : search.CostOf(target);
            }
        }

        return new TravelTimeMatrix(count, entries);
    }

    /// <summary>
    /// Rebuilds only the rows the predicate marks, into an existing matrix. The incremental path.
    /// </summary>
    public static void Rebuild(
        TravelTimeMatrix matrix, Districts districts, int[] arcTicks, OneToAll search, bool[] rows)
    {
        for (int from = 0; from < matrix.Count; from++)
        {
            if (!rows[from])
            {
                continue;
            }

            int source = districts.Representative[from];
            int row = from * matrix.Count;

            if (source < 0)
            {
                Array.Fill(matrix.Entries, OneToAll.Unreachable, row, matrix.Count);
                continue;
            }

            search.Run(source, arcTicks);

            for (int to = 0; to < matrix.Count; to++)
            {
                int target = districts.Representative[to];
                matrix.Entries[row + to] =
                    target < 0 ? OneToAll.Unreachable : search.CostOf(target);
            }
        }
    }

    /// <summary>Entries that differ between two matrices of the same size. The rebuild's ground truth.</summary>
    public static int Differences(TravelTimeMatrix a, TravelTimeMatrix b)
    {
        int differing = 0;
        for (int i = 0; i < a.Entries.Length; i++)
        {
            if (a.Entries[i] != b.Entries[i])
            {
                differing++;
            }
        }

        return differing;
    }

    /// <summary>
    /// The asymmetry distribution: <c>|m[i][j] − m[j][i]|</c> over ordered pairs, in Q16.16 Ticks,
    /// with the relative figure taken against the mean of the two directions.
    /// </summary>
    /// <remarks>
    /// Unreachable in either direction is excluded rather than counted as infinite asymmetry — a pair
    /// with no route at all is a Severance finding and not an asymmetry one, and mixing them would
    /// let the connectivity of the graph move a number about its congestion.
    /// </remarks>
    public Asymmetry MeasureAsymmetry()
    {
        var absolute = new List<int>();
        var relative = new List<int>();

        for (int i = 0; i < Count; i++)
        {
            for (int j = i + 1; j < Count; j++)
            {
                int forward = this[i, j];
                int backward = this[j, i];

                if (forward >= OneToAll.Unreachable || backward >= OneToAll.Unreachable)
                {
                    continue;
                }

                int difference = IntegerMath.Abs(forward - backward);
                absolute.Add(difference);

                // Both operands are already Q16.16, so this is a ratio of two fixed-point values and
                // not a conversion. Fixed.FromInt here would overflow on any real travel time.
                int mean = IntegerMath.RoundDiv(forward + backward, 2);
                relative.Add(mean == 0 ? 0 : Fixed.Div(difference, mean));
            }
        }

        absolute.Sort();
        relative.Sort();

        return new Asymmetry(
            Pairs: absolute.Count,
            MeanTicks: Mean(absolute),
            MedianTicks: Quantile(absolute, 50),
            NinetiethTicks: Quantile(absolute, 90),
            MaximumTicks: absolute.Count == 0 ? 0 : absolute[^1],
            MeanRelative: Mean(relative),
            NinetiethRelative: Quantile(relative, 90));
    }

    /// <summary>Entries with no route at all. Reported beside every average taken over the matrix.</summary>
    public int Unreachable()
    {
        int count = 0;
        foreach (int entry in Entries)
        {
            if (entry >= OneToAll.Unreachable)
            {
                count++;
            }
        }

        return count;
    }

    private static int Mean(List<int> sorted)
    {
        if (sorted.Count == 0)
        {
            return 0;
        }

        long total = 0;
        foreach (int value in sorted)
        {
            total += value;
        }

        return (int)IntegerMath.FloorDiv(total, sorted.Count);
    }

    private static int Quantile(List<int> sorted, int percent)
    {
        if (sorted.Count == 0)
        {
            return 0;
        }

        int index = IntegerMath.FloorDiv((sorted.Count - 1) * percent, 100);
        return sorted[index];
    }
}

/// <param name="Pairs">Unordered pairs with a route in both directions.</param>
/// <param name="MeanTicks">Mean <c>|m[i][j] − m[j][i]|</c>, Q16.16 Ticks.</param>
/// <param name="MedianTicks">Median, Q16.16 Ticks.</param>
/// <param name="NinetiethTicks">90th percentile, Q16.16 Ticks.</param>
/// <param name="MaximumTicks">The worst pair on the map.</param>
/// <param name="MeanRelative">Mean asymmetry as a Q16.16 fraction of the pair's mean travel time.</param>
/// <param name="NinetiethRelative">90th percentile of the same fraction.</param>
internal readonly record struct Asymmetry(
    int Pairs,
    int MeanTicks,
    int MedianTicks,
    int NinetiethTicks,
    int MaximumTicks,
    int MeanRelative,
    int NinetiethRelative);

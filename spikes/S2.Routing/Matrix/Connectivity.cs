namespace S2.Routing.Matrix;

/// <summary>
/// Settlements two ways: <c>adr/0020</c>'s union-find, and the strongly connected components
/// <c>CONTEXT.md</c> → Settlement actually describes.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the exposure <c>plans/0010</c> recorded before the numbers arrived so it could not be
/// reasoned around afterwards.</b>
/// <c>adr/0020</c> computes a Settlement as <i>"a <b>connected component</b> of the District graph…
/// <b>a union-find</b> over data already being maintained, at effectively no cost"</i>, while
/// <c>CONTEXT.md</c> → Settlement defines one as <i>"a maximal set of Districts <b>mutually</b>
/// reachable within the Commute Budget."</i> <b>Union-find computes weak connectivity; "mutually
/// reachable" is strong connectivity, and the two coincide only on a symmetric matrix.</b>
/// </para>
/// <para>
/// <b>So the honest test is not the asymmetry distribution — it is whether the two algorithms
/// disagree about the city.</b> An asymmetry figure is a number about travel times; a Settlement count
/// is the object the game is made of, and a Building's Trips fail or do not depending on which one is
/// right. Both are reported, at a swept Commute Budget, because the Budget has no value anywhere in
/// the corpus and the disagreement is a function of it: at a Budget longer than any commute on the
/// map everything is one Settlement under both, and at a Budget shorter than any of them everything
/// is its own. <b>The divergence lives in the middle and the middle is where the game is played.</b>
/// </para>
/// <para>
/// <b>The headline case is the one the plan named</b> — inbound within budget at the morning peak,
/// outbound not — so <see cref="OneWayPairs"/> is reported as its own count: District pairs reachable
/// in exactly one direction. Those are precisely the pairs union-find merges and Tarjan does not.
/// </para>
/// </remarks>
internal static class Connectivity
{
    /// <summary>Settlements under both readings, at one Commute Budget.</summary>
    public static Settlements Measure(TravelTimeMatrix matrix, int budgetTicks)
    {
        int n = matrix.Count;

        int oneWay = 0;
        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                bool forward = matrix[i, j] <= budgetTicks;
                bool backward = matrix[j, i] <= budgetTicks;
                if (forward != backward)
                {
                    oneWay++;
                }
            }
        }

        return new Settlements(
            BudgetTicks: budgetTicks,
            Weak: WeakComponents(matrix, budgetTicks, out int largestWeak),
            Strong: StrongComponents(matrix, budgetTicks, out int largestStrong),
            LargestWeak: largestWeak,
            LargestStrong: largestStrong,
            OneWayPairs: oneWay);
    }

    /// <summary>
    /// <c>adr/0020</c>'s reading: union-find over the District graph, an edge existing if either
    /// direction is within budget.
    /// </summary>
    private static int WeakComponents(TravelTimeMatrix matrix, int budgetTicks, out int largest)
    {
        int n = matrix.Count;
        var parent = new int[n];
        for (int i = 0; i < n; i++)
        {
            parent[i] = i;
        }

        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                if (matrix[i, j] <= budgetTicks || matrix[j, i] <= budgetTicks)
                {
                    Union(parent, i, j);
                }
            }
        }

        var size = new int[n];
        for (int i = 0; i < n; i++)
        {
            size[Find(parent, i)]++;
        }

        return CountAndLargest(size, out largest);
    }

    /// <summary>
    /// <c>CONTEXT.md</c>'s reading: strongly connected components of the directed within-budget
    /// relation. Tarjan, iterative — the recursive form stack-overflows at 4,096 Districts and the
    /// spike would have reported that as a routing finding.
    /// </summary>
    private static int StrongComponents(TravelTimeMatrix matrix, int budgetTicks, out int largest)
    {
        int n = matrix.Count;

        var index = new int[n];
        var low = new int[n];
        var onStack = new bool[n];
        var componentOf = new int[n];

        Array.Fill(index, -1);
        Array.Fill(componentOf, -1);

        var stack = new int[n];
        int stackTop = 0;

        var frameNode = new int[n];
        var frameNext = new int[n];

        int nextIndex = 0;
        int components = 0;

        for (int root = 0; root < n; root++)
        {
            if (index[root] >= 0)
            {
                continue;
            }

            int depth = 0;
            frameNode[0] = root;
            frameNext[0] = 0;
            index[root] = low[root] = nextIndex++;
            stack[stackTop++] = root;
            onStack[root] = true;

            while (depth >= 0)
            {
                int node = frameNode[depth];

                if (frameNext[depth] < n)
                {
                    int neighbour = frameNext[depth]++;

                    if (neighbour == node || matrix[node, neighbour] > budgetTicks)
                    {
                        continue;
                    }

                    if (index[neighbour] < 0)
                    {
                        index[neighbour] = low[neighbour] = nextIndex++;
                        stack[stackTop++] = neighbour;
                        onStack[neighbour] = true;

                        depth++;
                        frameNode[depth] = neighbour;
                        frameNext[depth] = 0;
                    }
                    else if (onStack[neighbour] && index[neighbour] < low[node])
                    {
                        low[node] = index[neighbour];
                    }

                    continue;
                }

                if (low[node] == index[node])
                {
                    while (true)
                    {
                        int member = stack[--stackTop];
                        onStack[member] = false;
                        componentOf[member] = components;

                        if (member == node)
                        {
                            break;
                        }
                    }

                    components++;
                }

                depth--;
                if (depth >= 0 && low[node] < low[frameNode[depth]])
                {
                    low[frameNode[depth]] = low[node];
                }
            }
        }

        var size = new int[components == 0 ? 1 : components];
        foreach (int component in componentOf)
        {
            if (component >= 0)
            {
                size[component]++;
            }
        }

        return CountAndLargest(size, out largest);
    }

    private static int CountAndLargest(int[] size, out int largest)
    {
        int count = 0;
        largest = 0;

        foreach (int members in size)
        {
            if (members == 0)
            {
                continue;
            }

            count++;
            if (members > largest)
            {
                largest = members;
            }
        }

        return count;
    }

    private static int Find(int[] parent, int node)
    {
        while (parent[node] != node)
        {
            parent[node] = parent[parent[node]];
            node = parent[node];
        }

        return node;
    }

    private static void Union(int[] parent, int a, int b)
    {
        int rootA = Find(parent, a);
        int rootB = Find(parent, b);

        if (rootA != rootB)
        {
            parent[rootB] = rootA;
        }
    }
}

/// <param name="BudgetTicks">The Commute Budget this row was measured at, Q16.16 Ticks.</param>
/// <param name="Weak">Settlements under <c>adr/0020</c>'s union-find.</param>
/// <param name="Strong">Settlements under <c>CONTEXT.md</c>'s "mutually reachable".</param>
/// <param name="LargestWeak">Districts in the largest weak component.</param>
/// <param name="LargestStrong">Districts in the largest strong component.</param>
/// <param name="OneWayPairs">
/// District pairs within budget in exactly one direction — the pairs union-find merges and Tarjan
/// does not, and the ADR's exposure in one number.
/// </param>
internal readonly record struct Settlements(
    int BudgetTicks,
    int Weak,
    int Strong,
    int LargestWeak,
    int LargestStrong,
    int OneWayPairs);

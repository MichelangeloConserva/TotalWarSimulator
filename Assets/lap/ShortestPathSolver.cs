using System;
using System.Collections.Generic;
using System.Linq;

namespace LinearAssignment
{
    /// <summary>
    /// Solver for the linear assignment problem based on shortest augmenting paths. Concretely,
    /// we implement the pseudo-code from
    /// 
    ///     DF Crouse. On implementing 2D rectangular assignment algorithms.
    ///     IEEE Transactions on Aerospace and Electronic Systems
    ///     52(4):1679-1696, August 2016
    ///     doi: 10.1109/TAES.2016.140952
    ///
    /// which in turn is based closely on Section 4.4 of
    ///
    ///     Rainer Burkard, Mauro Dell'Amico, Silvano Martello.
    ///     Assignment Problems - Revised Reprint
    ///     Society for Industrial and Applied Mathematics, Philadelphia, 2012
    ///
    /// This is a C# port of the C++ implementation of the algorithm by Peter Mahler Larsen included
    /// in the Python library scipy.optimize. https://github.com/scipy/scipy/pull/10296/
    ///
    /// The solver for sparse inputs is what is referred to as LAPJVsp, which was described in
    ///
    ///     R. Jonker and A. Volgenant. A Shortest Augmenting Path Algorithm for
    ///     Dense and Sparse Linear Assignment Problems. *Computing*, 38:325-340
    ///     December 1987.
    ///
    /// and our implementation is a port of the Pascal code currently available at
    /// http://www.assignmentproblems.com/LAPJV.htm
    /// All comments from the original code are preserved.
    /// </summary>
    public class ShortestPathSolver : ISolver
    {
        public Assignment Solve(double[,] cost)
        {
            var nr = cost.GetLength(0);
            var nc = cost.GetLength(1);

            // Initialize working arrays
            var u = new double[nr];
            var v = new double[nc];
            var shortestPathCosts = new double[nc];
            var path = Enumerable.Repeat(-1, nc).ToArray();
            var x = Enumerable.Repeat(-1, nr).ToArray();
            var y = Enumerable.Repeat(-1, nc).ToArray();
            var sr = new bool[nr];
            var sc = new bool[nc];

            // Find a matching one vertex at a time
            for (var curRow = 0; curRow < nr; curRow++)
            {
                double minVal = 0;
                var i = curRow;
                // Reset working arrays
                var remaining = Enumerable.Repeat(0, nc).ToList();
                var numRemaining = nc;
                for (var jp = 0; jp < nc; jp++)
                {
                    remaining[jp] = jp;
                    shortestPathCosts[jp] = double.PositiveInfinity;
                }
                Array.Clear(sr, 0, sr.Length);
                Array.Clear(sc, 0, sc.Length);

                // Start finding augmenting path
                var sink = -1;
                while (sink == -1)
                {
                    sr[i] = true;
                    var indexLowest = -1;
                    var lowest = double.PositiveInfinity;
                    for (var jk = 0; jk < numRemaining; jk++)
                    {
                        var jl = remaining[jk];
                        // Note that this is the main bottleneck of this method; looking up the cost array
                        // is costly. Some obvious attempts to improve performance include swapping rows and
                        // columns, and disabling CLR bounds checking by using pointers to access the elements
                        // instead. We do not seem to get any significant improvements over the simpler
                        // approach below though.
                        var r = minVal + cost[i, jl] - u[i] - v[jl];
                        if (r < shortestPathCosts[jl])
                        {
                            path[jl] = i;
                            shortestPathCosts[jl] = r;
                        }

                        if (shortestPathCosts[jl] < lowest || shortestPathCosts[jl] == lowest && y[jl] == -1)
                        {
                            lowest = shortestPathCosts[jl];
                            indexLowest = jk;
                        }
                    }

                    minVal = lowest;
                    var jp = remaining[indexLowest];
                    if (double.IsPositiveInfinity(minVal))
                        throw new InvalidOperationException("No feasible solution.");
                    if (y[jp] == -1)
                        sink = jp;
                    else
                        i = y[jp];

                    sc[jp] = true;
                    remaining[indexLowest] = remaining[--numRemaining];
                    remaining.RemoveAt(numRemaining);
                }

                if (sink < 0)
                    throw new InvalidOperationException("No feasible solution.");

                // Update dual variables
                u[curRow] += minVal;
                for (var ip = 0; ip < nr; ip++)
                    if (sr[ip] && ip != curRow)
                        u[ip] += minVal - shortestPathCosts[x[ip]];

                for (var jp = 0; jp < nc; jp++)
                    if (sc[jp])
                        v[jp] -= minVal - shortestPathCosts[jp];

                // Augment previous solution
                var j = sink;
                while (true)
                {
                    var ip = path[j];
                    y[j] = ip;
                    var tmp = j;
                    j = x[ip];
                    x[ip] = tmp;
                    if (ip == curRow)
                        break;
                }
            }

            return new AssignmentWithDuals(x, y, u, v);
        }

        public Assignment Solve(int[,] cost)
        {
            // Note that it would be possible to reimplement the above method using only
            // integer arithmetic. Doing so does provide a very slight performance improvement
            // but there's no nice way of implementing the method for ints and doubles at once
            // without duplicating code or moving to something like T4 templates. This would
            // work but would also increase the maintenance load, so for now we just keep this
            // simple and use the floating-point version directly.
            var nr = cost.GetLength(0);
            var nc = cost.GetLength(1);
            var doubleCost = new double[nr, nc];
            for (var i = 0; i < nr; i++)
            for (var j = 0; j < nc; j++)
            {
                doubleCost[i, j] = cost[i, j] == int.MaxValue ? double.PositiveInfinity : cost[i, j];
            }

            return Solve(doubleCost);
        }

        public Assignment Solve(SparseMatrixInt cost) =>
            Solve(new SparseMatrixDouble(cost));

        public Assignment Solve(SparseMatrixDouble cost)
        {
            var nr = cost.NumRows;
            var nc = cost.NumColumns;
            var v = new double[nc];
            var x = Enumerable.Repeat(-1, nr).ToArray();
            var y = Enumerable.Repeat(-1, nc).ToArray();
            var u = new double[nr];
            var d = new double[nc];
            var ok = new bool[nc];
            var xinv = new bool[nr];
            var free = Enumerable.Repeat(-1, nr).ToArray();
            var todo = Enumerable.Repeat(-1, nc).ToArray();
            var lab = new int[nc];
            var first = cost.IA;
            var kk = cost.CA;
            var cc = cost.A;
            int l0;

            // The initialization steps of LAPJVsp only make sense for square matrices
            if (nr == nc)
            {
                for (var jp = 0; jp < nc; jp++)
                    v[jp] = double.PositiveInfinity;
                for (var i = 0; i < nr; i++)
                {
                    for (var t = first[i]; t < first[i + 1]; t++)
                    {
                        var jp = kk[t];
                        if (cc[t] < v[jp])
                        {
                            v[jp] = cc[t];
                            y[jp] = i;
                        }
                    }
                }

                for (var jp = nc - 1; jp >= 0; jp--)
                {
                    var i = y[jp];
                    if (x[i] == -1) x[i] = jp;
                    else
                    {
                        y[jp] = -1;
                        // Here, the original Pascal code simply inverts the sign of x; as that
                        // doesn't play too well with zero-indexing, we explicitly keep track of
                        // uniqueness instead.
                        xinv[i] = true;
                    }
                }

                var lp = 0;
                for (var i = 0; i < nr; i++)
                {
                    if (xinv[i]) continue;
                    if (x[i] != -1)
                    {
                        var min = double.PositiveInfinity;
                        var j1 = x[i];
                        for (var t = first[i]; t < first[i + 1]; t++)
                        {
                            var jp = kk[t];
                            if (jp != j1)
                            {
                                if (cc[t] - v[jp] < min)
                                {
                                    min = cc[t] - v[jp];
                                }
                            }
                        }

                        u[i] = min;
                        var tp = first[i];
                        while (kk[tp] != j1) tp++;
                        v[j1] = cc[tp] - min;
                    }
                    else
                    {
                        free[lp++] = i;
                    }
                }

                for (var tel = 0; tel < 2; tel++)
                {
                    var h = 0;
                    var l0p = lp;
                    lp = 0;
                    while (h < l0p)
                    {
                        // Note: In the original Pascal code, the indices of the lowest
                        // and second-lowest reduced costs are never reset. This can
                        // cause issues for infeasible problems; see https://stackoverflow.com/q/62875232/5085211
                        var i = free[h++];
                        var j0p = -1;
                        var j1p = -1;
                        var v0 = double.PositiveInfinity;
                        var vj = double.PositiveInfinity;
                        for (var t = first[i]; t < first[i + 1]; t++)
                        {
                            var jp = kk[t];
                            var dj = cc[t] - v[jp];
                            if (dj < vj)
                            {
                                if (dj >= v0)
                                {
                                    vj = dj;
                                    j1p = jp;
                                }
                                else
                                {
                                    vj = v0;
                                    v0 = dj;
                                    j1p = j0p;
                                    j0p = jp;
                                }
                            }
                        }

                        // If the index of the column with the largest reduced cost has not been
                        // set, no assignment is possible for this row.
                        if (j0p < 0)
                            throw new InvalidOperationException("No feasible solution.");
                        var i0 = y[j0p];
                        u[i] = vj;
                        if (v0 < vj)
                        {
                            v[j0p] += v0 - vj;
                        }
                        else if (i0 != -1)
                        {
                            j0p = j1p;
                            i0 = y[j0p];
                        }

                        x[i] = j0p;
                        y[j0p] = i;
                        if (i0 != -1)
                        {
                            if (v0 < vj) free[--h] = i0;
                            else free[lp++] = i0;
                        }
                    }
                }

                l0 = lp;
            }
            else
            {
                l0 = nr;
                for (int i = 0; i < nr; i++) free[i] = i;
            }

            var td1 = -1;
            for (var l = 0; l < l0; l++)
            {
                td1 = SolveForOneL(l, nc, d, ok, free, first, kk, cc, v, lab, todo, y, x, td1);
            }
            return new Assignment(x, y);
        }

        private int SolveForOneL(int l, int nc, double[] d, bool[] ok, int[] free,
            List<int> first, List<int> kk,
            List<double> cc, double[] v, int[] lab, int[] todo, int[] y, int[] x, int td1)
        {
            for (var jp = 0; jp < nc; jp++)
            {
                d[jp] = double.PositiveInfinity;
                ok[jp] = false;
            }

            var min = double.PositiveInfinity;
            var i0 = free[l];
            int j;
            for (var t = first[i0]; t < first[i0 + 1]; t++)
            {
                j = kk[t];
                var dj = cc[t] - v[j];
                d[j] = dj;
                lab[j] = i0;
                if (dj <= min)
                {
                    if (dj < min)
                    {
                        td1 = -1;
                        min = dj;
                    }

                    todo[++td1] = j;
                }
            }

            for (var hp = 0; hp <= td1; hp++)
            {
                j = todo[hp];
                if (y[j] == -1)
                {
                    UpdateAssignments(lab, y, x, j, i0);
                    return td1;
                }
                ok[j] = true;
            }

            var td2 = nc - 1;
            var last = nc;
            while (true)
            {
                if (td1 < 0)
                    throw new InvalidOperationException("No feasible solution.");
                var j0 = todo[td1--];
                var i = y[j0];
                todo[td2--] = j0;
                var tp = first[i];
                while (kk[tp] != j0) tp++;
                var h = cc[tp] - v[j0] - min;
                for (var t = first[i]; t < first[i + 1]; t++)
                {
                    j = kk[t];
                    if (!ok[j])
                    {
                        var vj = cc[t] - v[j] - h;
                        if (vj < d[j])
                        {
                            d[j] = vj;
                            lab[j] = i;
                            if (vj == min)
                            {
                                if (y[j] == -1)
                                {
                                    UpdateDual(nc, d, v, todo, last, min);
                                    UpdateAssignments(lab, y, x, j, i0);
                                    return td1;
                                }
                                todo[++td1] = j;
                                ok[j] = true;
                            }
                        }
                    }
                }

                if (td1 == -1)
                {
                    // The original Pascal code uses finite numbers instead of double.PositiveInfinity
                    // so we need to adjust slightly here.
                    min = double.PositiveInfinity;
                    last = td2 + 1;
                    for (var jp = 0; jp < nc; jp++)
                    {
                        if (!double.IsPositiveInfinity(d[jp]) && d[jp] <= min && !ok[jp])
                        {
                            if (d[jp] < min)
                            {
                                td1 = -1;
                                min = d[jp];
                            }

                            todo[++td1] = jp;
                        }
                    }

                    for (var hp = 0; hp <= td1; hp++)
                    {
                        j = todo[hp];
                        if (y[j] == -1)
                        {
                            UpdateDual(nc, d, v, todo, last, min);
                            UpdateAssignments(lab, y, x, j, i0);
                            return td1;
                        }
                        ok[j] = true;
                    }
                }
            }
        }

        private static void UpdateDual(int nc, double[] d, double[] v, int[] todo, int last, double min)
        {
            for (var k = last; k < nc; k++)
            {
                var j0 = todo[k];
                v[j0] += d[j0] - min;
            }
        }

        private static void UpdateAssignments(int[] lab, int[] y, int[] x, int j, int i0)
        {
            while (true)
            {
                var i = lab[j];
                y[j] = i;
                var k = j;
                j = x[i];
                x[i] = k;
                if (i == i0)
                    return;
            }
        }
    }
}
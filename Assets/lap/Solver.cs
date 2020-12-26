using System.Collections.Generic;
using System.Linq;

namespace LinearAssignment
{
    /// <summary>
    /// General solver of the linear assignment problem that provides sane choices
    /// of defaults for algorithms. This is the intended entry point for general
    /// purpose usage of the library.
    /// </summary>
    public static class Solver
    {
        // There is an unfortunate amount of code duplication going on between the floating point and
        // integral cases below. It would be possible to take care of some of this by applying T4 templates
        // but on the other hand, that doesn't make maintenance much simpler.
        /// <summary>
        /// Solves an instance of the linear assignment problem with floating point costs.
        /// </summary>
        /// <param name="cost">The weights of the edges of the bipartite graph representing
        /// the problem. Edges can be removed by specifying a weight of <see cref="double.PositiveInfinity"/>
        /// when minimizing and <see cref="double.NegativeInfinity"/> when maximizing.</param>
        /// <param name="maximize">Whether or not to maximize total cost rather than minimize it.</param>
        /// <param name="solver">The solver to use. If not given, this defaults to <see cref="ShortestPathSolver"/>.</param>
        /// <param name="allowOverwrite">Allows the entries <paramref name="cost"/> to be changed; setting this to
        /// <see langword="true" /> can give performance improvements in certain cases.</param>
        /// <returns>An <see cref="Assignment"/> representing the solution.</returns>
        public static Assignment Solve(double[,] cost, bool maximize = false,
            ISolver solver = null, bool allowOverwrite = false)
        {
            var transpose = Transpose(ref cost);
            allowOverwrite = transpose || allowOverwrite;
            var nr = cost.GetLength(0);
            var nc = cost.GetLength(1);
            if (nr == 0 || nc == 0) return AssignmentWithDuals.Empty;

            // We handle maximization by changing all signs in the given cost, then
            // minimizing the result. At the end of the day, we also make sure to
            // update the dual variables accordingly.
            if (maximize)
            {
                if (allowOverwrite)
                {
                    for (var i = 0; i < nr; i++)
                    for (var j = 0; j < nc; j++)
                        cost[i, j] = -cost[i, j];
                }
                else
                {
                    var tmpCost = new double[nr, nc];
                    for (var i = 0; i < nr; i++)
                    for (var j = 0; j < nc; j++)
                        tmpCost[i, j] = -cost[i, j];
                    cost = tmpCost;
                    allowOverwrite = true;
                }
            }

            // Ensure that all values are positive
            var min = double.PositiveInfinity;
            for (var i = 0; i < nr; i++)
            for (var j = 0; j < nc; j++)
                if (cost[i, j] < min)
                    min = cost[i, j];

            if (min < 0)
            {
                if (allowOverwrite)
                {
                    for (var i = 0; i < nr; i++)
                    for (var j = 0; j < nc; j++)
                        cost[i, j] -= min;
                }
                else
                {
                    var tmpCost = new double[nr, nc];
                    for (var i = 0; i < nr; i++)
                    for (var j = 0; j < nc; j++)
                        tmpCost[i, j] = cost[i, j] - min;
                    cost = tmpCost;
                }
            }
            else
                min = 0;
            if (solver == null)
                solver = new ShortestPathSolver();
            var solution = solver.Solve(cost);

            if (solution is AssignmentWithDuals solutionWithDuals)
            {
                if (min != 0)
                    for (var ip = 0; ip < nr; ip++)
                        solutionWithDuals.DualU[ip] += min;
                if (maximize) FlipDualSigns(solutionWithDuals.DualU, solutionWithDuals.DualV);
            }

            if (transpose) solution = solution.Transpose();

            return solution;
        }

        /// <summary>
        /// Solves an instance of the linear assignment problem with integral costs.
        /// </summary>
        /// <param name="cost">The weights of the edges of the bipartite graph representing
        /// the problem. Edges can be removed by specifying a weight of <see cref="int.MaxValue"/>
        /// when minimizing and <see cref="int.MinValue"/> when maximizing.</param>
        /// <param name="maximize">Whether or not to maximize total cost rather than minimize it.</param>
        /// <param name="solver">The solver to use. If not given, this defaults to <see cref="PseudoflowSolver"/>
        /// for square costs and <see cref="ShortestPathSolver"/> for rectangular ones.</param>
        /// <param name="allowOverwrite">Allows the entries <paramref name="cost"/> to be changed; setting this to
        /// <see langword="true" /> can give performance improvements in certain cases.</param>
        /// <returns>An <see cref="Assignment"/> representing the solution.</returns>
        public static Assignment Solve(int[,] cost, bool maximize = false,
            ISolver solver = null, bool allowOverwrite = false)
        {
            var transpose = Transpose(ref cost);
            allowOverwrite = transpose || allowOverwrite;
            var nr = cost.GetLength(0);
            var nc = cost.GetLength(1);
            if (nr == 0 || nc == 0) return AssignmentWithDuals.Empty;

            // As in the double case, maximization is handled by flipping the sign; here, we need
            // to take special care when dealing with "infinities".
            if (maximize)
            {
                if (allowOverwrite)
                {
                    for (var i = 0; i < nr; i++)
                    for (var j = 0; j < nc; j++)
                        cost[i, j] = cost[i, j] == int.MinValue ? int.MaxValue : -cost[i, j];
                }
                else
                {
                    var tmpCost = new int[nr, nc];
                    for (var i = 0; i < nr; i++)
                    for (var j = 0; j < nc; j++)
                        tmpCost[i, j] = cost[i, j] == int.MinValue ? int.MaxValue : -cost[i, j];
                    cost = tmpCost;
                    allowOverwrite = true;
                }
            }

            // Ensure that all values are positive
            var min = int.MaxValue;
            for (var i = 0; i < nr; i++)
            for (var j = 0; j < nc; j++)
                if (cost[i, j] < min)
                    min = cost[i, j];

            if (min < 0)
            {
                if (allowOverwrite)
                {
                    for (var i = 0; i < nr; i++)
                    for (var j = 0; j < nc; j++)
                        cost[i, j] = cost[i, j] == int.MaxValue ? int.MaxValue : cost[i, j] - min;
                }
                else
                {
                    var tmpCost = new int[nr, nc];
                    for (var i = 0; i < nr; i++)
                    for (var j = 0; j < nc; j++)
                        tmpCost[i, j] = cost[i, j] == int.MaxValue ? int.MaxValue : cost[i, j] - min;
                    cost = tmpCost;
                }
            }
            else
            {
                min = 0;
            }

            if (solver == null)
                solver = nr == nc ? (ISolver) new PseudoflowSolver() : new ShortestPathSolver();
            var solution = solver.Solve(cost);

            if (solution is AssignmentWithDuals solutionWithDuals)
            {
                if (min != 0)
                    for (var ip = 0; ip < solutionWithDuals.DualU.Length; ip++)
                        solutionWithDuals.DualU[ip] += min;

                if (maximize) FlipDualSigns(solutionWithDuals.DualU, solutionWithDuals.DualV);
            }

            if (transpose) solution = solution.Transpose();

            return solution;
        }

        private static bool Transpose<T>(ref T[,] cost)
        {
            // In our solution, we will assume that nr <= nc. If this isn't the case,
            // we transpose the entire matrix and make sure to fix up the results at
            // the end of the day.
            var nr = cost.GetLength(0);
            var nc = cost.GetLength(1);
            if (nr <= nc) return false;
            var tmpCost = new T[nc, nr];
            for (var i = 0; i < nc; i++)
            for (var j = 0; j < nr; j++)
                tmpCost[i, j] = cost[j, i];
            cost = tmpCost;
            return true;
        }

        private static void FlipDualSigns(IList<double> u, IList<double> v)
        {
            for (var i = 0; i < u.Count; i++) u[i] = -u[i];
            for (var j = 0; j < v.Count; j++) v[j] = -v[j];
        }

        /// <summary>
        /// Solves a sparse instance of the linear assignment problem with floating point costs.
        /// </summary>
        /// <param name="cost">The weights of the edges of the bipartite graph representing the problem.</param>
        /// <param name="maximize">Whether or not to maximize total cost rather than minimize it.</param>
        /// <param name="solver">The solver to use. If not given, this defaults to <see cref="ShortestPathSolver"/>.</param>
        /// <param name="allowOverwrite">Allows the entries <paramref name="cost"/> to be changed; setting this to
        /// <see langword="true" /> can give performance improvements in certain cases.</param>
        /// <returns>An <see cref="Assignment"/> representing the solution.</returns>
        public static Assignment Solve(SparseMatrixDouble cost, bool maximize = false,
            ISolver solver = null, bool allowOverwrite = false)
        {
            var transpose = false;
            if (cost.NumRows > cost.NumColumns)
            {
                cost = cost.Transpose();
                allowOverwrite = true;
                transpose = true;
            }
            var nr = cost.NumRows;
            var nc = cost.NumColumns;
            if (nr == 0 || nc == 0) return AssignmentWithDuals.Empty;

            // We handle maximization by changing all signs in the given cost, then
            // minimizing the result. At the end of the day, we also make sure to
            // update the dual variables accordingly.
            if (maximize)
            {
                if (!allowOverwrite)
                {
                    cost = new SparseMatrixDouble(cost);
                    allowOverwrite = true;
                }
                for (var i = 0; i < cost.A.Count; i++)
                    cost.A[i] = -cost.A[i];
            }

            // Ensure that all values are positive
            var min = cost.A.Any() ? cost.A.Min() : double.PositiveInfinity;

            if (min < 0)
            {
                if (!allowOverwrite)
                    cost = new SparseMatrixDouble(cost);

                for (var i = 0; i < cost.A.Count; i++)
                    cost.A[i] -= min;
            }
            else
                min = 0;

            if (solver == null)
                solver = new ShortestPathSolver();
            var solution = solver.Solve(cost);

            if (solution is AssignmentWithDuals solutionWithDuals)
            {
                if (min != 0)
                    for (var ip = 0; ip < nr; ip++)
                        solutionWithDuals.DualU[ip] += min;
                if (maximize) FlipDualSigns(solutionWithDuals.DualU, solutionWithDuals.DualV);
            }

            if (transpose) solution = solution.Transpose();

            return solution;
        }

        /// <summary>
        /// Solves a sparse instance of the linear assignment problem with integer costs.
        /// </summary>
        /// <param name="cost">The weights of the edges of the bipartite graph representing the problem.</param>
        /// <param name="maximize">Whether or not to maximize total cost rather than minimize it.</param>
        /// <param name="solver">The solver to use. If not given, this defaults to <see cref="ShortestPathSolver"/>.</param>
        /// <param name="allowOverwrite">Allows the entries <paramref name="cost"/> to be changed; setting this to
        /// <see langword="true" /> can give performance improvements in certain cases.</param>
        /// <returns>An <see cref="Assignment"/> representing the solution.</returns>
        public static Assignment Solve(SparseMatrixInt cost, bool maximize = false,
            ISolver solver = null, bool allowOverwrite = false)
        {
            // Currently, the implemented solvers just translate to the floating point
            // equivalents, so there's no reason to distinguish here.
            return Solve(new SparseMatrixDouble(cost), maximize, solver, true);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace LinearAssignment
{
    /// <summary>
    /// Solver for the linear assignment problem based on the pseudoflow approach to solving
    /// minimum cost flow problems. This is closely based on Section 4.6.4 of
    ///
    ///     Rainer Burkard, Mauro Dell'Amico, Silvano Martello.
    ///     Assignment Problems - Revised Reprint
    ///     Society for Industrial and Applied Mathematics, Philadelphia, 2012
    ///
    /// which in turn is based on the cost-scaling assignment (CSA) approach of
    ///
    ///     A.V. Goldberg and R. Kennedy.
    ///     An efficient cost scaling algorithm for the assignment problem.
    ///     Math. Program., 71:153–177, 1995
    ///
    /// in which the push-relabel step is performed using the "double push" algorithm. We adopt
    /// a few minor changes to the pseudo-code of Burkard--Dell'Amico--Martello in the interest
    /// of improving performance. In particular, we index row and column assignments by the vertices.
    ///
    /// Note that no attempt is made to detect whether feasible solutions exist. As such,
    /// the solver may run forever if no feasible solutions exist.
    /// </summary>
    public class PseudoflowSolver : ISolver
    {
        private readonly double _alpha;
        private readonly double? _initialEpsilon;

        /// <summary>
        /// Initializes a new instance of the <see cref="PseudoflowSolver"/> class.
        /// </summary>
        /// <param name="alpha">The cost-scaling reduction factor.</param>
        /// <param name="initialEpsilon">Initial cost-scaling. If undefined, this will be
        /// calculated to be the largest cost. Set this if you know the ballpark magnitude
        /// of the costs to avoid having to determine the largest cost.</param>
        public PseudoflowSolver(double alpha = 10, double? initialEpsilon = null)
        {
            _alpha = alpha;
            _initialEpsilon = initialEpsilon;
        }

        public Assignment Solve(double[,] cost) =>
            throw new NotImplementedException("The pseudoflow solver can only be used with integer costs");

        public Assignment Solve(int[,] cost) => Solve(cost, null);

        public Assignment Solve(SparseMatrixInt cost) => Solve(null, cost);

        public Assignment Solve(SparseMatrixDouble cost) =>
            throw new NotImplementedException("The pseudoflow solver can only be used with integer costs");

        private Assignment Solve(int[,] costDense, SparseMatrixInt costSparse)
        {
            // The signature here is kind of nasty; it would be much nicer if we could split
            // out the method into one for the dense case, and one for the sparse case. However,
            // if we want to avoid code duplication, some sort of abstractions need to be in
            // place for the (substantial) parts of the algorithm that they have in common, and
            // including such abstractions is impossible without incurring a performance penalty
            // in some of our hot loops, which is an even worse alternative.
            if (costDense != null && costSparse != null)
                throw new ArgumentException("Only one parameter may be non-null.");

            var isSparse = costSparse != null;
            List<int> ia = null;
            List<int> a = null;
            List<int> ca = null;
            if (isSparse)
            {
                ia = costSparse.IA;
                a = costSparse.A;
                ca = costSparse.CA;
            }

            // TODO: Allow rectangular inputs
            var nr = isSparse ? costSparse.NumRows : costDense.GetLength(0);
            var nc = isSparse ? costSparse.NumColumns : costDense.GetLength(1);
            if (nr != nc)
                throw new NotImplementedException("Pseudoflow is only implemented for square matrices");
            var n = nr;

            // Initialize cost-scaling to be the configured value if given, and
            // otherwise let it be the largest given cost.
            double epsilon;
            if (_initialEpsilon.HasValue) epsilon = _initialEpsilon.Value;
            else if (isSparse) epsilon = costSparse.MaxValue;
            else
            {
                epsilon = double.NegativeInfinity;
                for (var i = 0; i < n; i++)
                for (var j = 0; j < n; j++)
                    if (costDense[i, j] > epsilon && costDense[i, j] != int.MaxValue)
                        epsilon = costDense[i, j];
            }

            // Initialize dual variables and assignment variables keeping track of
            // assignments as we move along: col maps a given row to the column
            // it's assigned to, and conversely row maps a given column to its
            // assigned row.
            var v = new double[n];
            var col = new int[n];
            var row = new int[n];

            while (epsilon >= 1d / n)
            {
                epsilon /= _alpha;
                // A value in -1 in row and col corresponds to "unassigned"
                for (var i = 0; i < n; i++) col[i] = -1;
                for (var j = 0; j < n; j++) row[j] = -1;
                // We also maintain a stack of rows that have not been assigned. We
                // could get this information from the variable col, but being able to
                // just pop the stack to get new unassigned rows is much faster. We put
                // lower numbers at the top of the stack simply because that feels a bit
                // more natural; we could get rid of the Reverse if we wanted to.
                var unassigned = new Stack<int>(Enumerable.Range(1, n - 1).Reverse());
                var k = 0;
                // At this point, Burkard--Dell'Amico--Martello would update
                // the dual variable u. However, as they note, and as is noted in
                // Goldberg--Kennedy, the variable is only used to evaluate the reduced costs
                // when determining the smallest and second smallest reduced costs in the first
                // part of the double-push. However, we actually only care about the argmin and
                // the arg-second-min, which end up being independent of the value of u. This is
                // also apparent from the original implementation of CSA-Q, in which only the
                // partial reduced costs are used. The bottom-line is that we never need to
                // calculate u or any of the reduced costs; we can do with only v and partial
                // reduced costs instead; doing so provides a large speed increase over the
                // naive implementation -- and simpler code.
                while (true)
                {
                    // Perform double-push. The halting condition is that all rows
                    // have been assigned, which corresponds to the stack of unassigned
                    // rows having been emptied.
                    var smallest = double.PositiveInfinity;
                    var j = -1;
                    var secondSmallest = double.PositiveInfinity;
                    // Again, below we find some duplication in the gap calculation, but this
                    // is all in the name of performance.
                    if (isSparse)
                    {
                        for (var m = ia[k]; m < ia[k + 1]; m++)
                        {
                            var jp = ca[m];
                            var partialReducedCost = a[m] - v[jp];
                            if (partialReducedCost <= secondSmallest)
                            {
                                if (partialReducedCost <= smallest)
                                {
                                    secondSmallest = smallest;
                                    smallest = partialReducedCost;
                                    j = jp;
                                }
                                else
                                {
                                    secondSmallest = partialReducedCost;
                                }
                            }
                        }
                    }
                    else
                    {
                        for (var jp = 0; jp < n; jp++)
                        {
                            var partialReducedCost = costDense[k, jp] - v[jp];
                            if (partialReducedCost <= secondSmallest)
                            {
                                if (partialReducedCost <= smallest)
                                {
                                    secondSmallest = smallest;
                                    smallest = partialReducedCost;
                                    j = jp;
                                }
                                else
                                {
                                    secondSmallest = partialReducedCost;
                                }
                            }
                        }
                    }

                    col[k] = j;
                    // TODO: Detect infeasibility by investigating dual updates.
                    if (row[j] != -1)
                    {
                        var i = row[j];
                        row[j] = k;
                        // The Burkard--Dell'Amico--Martello updates v[j] to be cost[k, j] - u[k] - epsilon,
                        // but cost[k, j] - u[k] = v[j] + smallest - secondSmallest; using this instead avoids a
                        // few costly cost lookups.
                        // Note moreover that if k only has a single incident egde, then secondSmallest becomes
                        // double.NegativeInfinity. This in turn means that j will never be picked as the smallest
                        // or second smallest element again, which guarantees that j will be forever assigned to k,
                        // which is exactly what we want.
                        v[j] += smallest - secondSmallest - epsilon;

                        col[i] = -1;
                        k = i;
                    }
                    else
                    {
                        row[j] = k;
                        if (unassigned.Count == 0) break;
                        k = unassigned.Pop();
                    }

                }
            }

            return new Assignment(col, row);
        }
    }
}

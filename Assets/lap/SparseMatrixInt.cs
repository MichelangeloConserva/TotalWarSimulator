using System.Collections.Generic;
using System.Linq;

namespace LinearAssignment
{
    /// <summary>
    /// Represents a sparse matrix in compressed sparse row (CSR) format whose elements
    /// are integers.
    /// </summary>
    public class SparseMatrixInt
    {
        public SparseMatrixInt(List<int> A, List<int> IA, List<int> CA, int numColumns)
        {
            this.A = A;
            this.IA = IA;
            this.CA = CA;
            NumRows = IA.Count - 1;
            NumColumns = numColumns;
        }

        public SparseMatrixInt(SparseMatrixInt matrix) :
            this(new List<int>(matrix.A), new List<int>(matrix.IA), new List<int>(matrix.CA), matrix.NumColumns)
        { }

        public SparseMatrixInt(int[,] dense, int empty = int.MaxValue)
        {
            A = new List<int>();
            IA = new List<int>();
            CA = new List<int>();
            IA.Add(0);
            var nonInfinite = 0;
            var nr = dense.GetLength(0);
            var nc = dense.GetLength(1);
            for (var i = 0; i < nr; i++)
            {
                for (var j = 0; j < nc; j++)
                {
                    var entry = dense[i, j];
                    if (entry != empty)
                    {
                        nonInfinite++;
                        A.Add(entry);
                        CA.Add(j);
                        if (entry > _max)
                            _max = entry;
                    }
                }
                IA.Add(nonInfinite);
            }

            NumRows = nr;
            NumColumns = nc;
        }

        public SparseMatrixInt Transpose()
        {
            var n = A.Count;
            var newA = new int[n];
            var newIA = new int[NumColumns + 1];
            var newCA = new int[n];

            for (var i = 0; i < n; i++) newIA[CA[i] + 1]++;

            for (var i = 2; i < NumColumns + 1; i++) newIA[i] += newIA[i - 1];

            for (var i = 0; i < NumRows; i++)
            {
                for (var j = IA[i]; j < IA[i + 1]; j++)
                {
                    var col = CA[j];
                    var dest = newIA[col];
                    newCA[dest] = i;
                    newA[dest] = A[j];
                    newIA[col]++;
                }
            }

            for (int i = 0, last = 0; i < NumColumns + 1; i++)
            {
                var tmp = newIA[i];
                newIA[i] = last;
                last = tmp;
            }

            return new SparseMatrixInt(newA.ToList(), newIA.ToList(), newCA.ToList(), NumRows);
        }

        private int _max = int.MinValue;
        public int MaxValue => _max != int.MinValue ? _max : _max = A.Max();
        public List<int> A { get; }
        public List<int> IA { get; }
        public List<int> CA { get; }

        public int NumRows { get; }
        public int NumColumns { get; }
    }
}

namespace LinearAssignment
{
    public interface ISolver
    {
        Assignment Solve(double[,] cost);
        Assignment Solve(int[,] cost);
        Assignment Solve(SparseMatrixDouble cost);
        Assignment Solve(SparseMatrixInt cost);
    }
}
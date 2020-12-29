namespace LinearAssignment
{
    public class Assignment
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Assignment"/> struct.
        /// </summary>
        public Assignment(int[] columnAssignment, int[] rowAssignment)
        {
            ColumnAssignment = columnAssignment;
            RowAssignment = rowAssignment;
        }

        /// <summary>
        /// The collection of columns assigned to each row. That is, if this
        /// is {0, 3, 2}, that means that the three rows of a given problem
        /// have been assigned to the first, fourth and third column respectively.
        /// </summary>
        public int[] ColumnAssignment { get; }

        /// <summary>
        /// The collection of rows assigned to each column. That is, if this
        /// is {2, 0, 1}, that means that the three columns of a given problem
        /// have been assigned to the third, first and second row respectively.
        /// In the case of non-square costs, a value of -1 indicates that no
        /// row has been assigned to the column.
        /// </summary>
        public int[] RowAssignment { get; }

        public virtual Assignment Transpose() => new Assignment(RowAssignment, ColumnAssignment);
    }
}

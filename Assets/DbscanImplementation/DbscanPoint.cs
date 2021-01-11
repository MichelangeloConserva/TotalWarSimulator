namespace DbscanImplementation
{
    /// <summary>
    /// Algorithm point definition
    /// </summary>
    /// <typeparam name="TF">Feature data contribute into algorithm</typeparam>
    public class DbscanPoint<TF>
    {
        public TF Feature { get; internal set; }

        public int? ClusterId { get; internal set; }

        public PointType? PointType { get; internal set; }

        public DbscanPoint(TF feature)
        {
            Feature = feature;
            ClusterId = null;
            PointType = null;
        }
    }
}
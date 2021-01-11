using System.Collections.Generic;

namespace DbscanImplementation.ResultBuilding
{
    public class DbscanResultBuilder<TF>
    {
        public DbscanResult<TF> Result { get; private set; }

        public DbscanResultBuilder()
        {
            Result = new DbscanResult<TF>();
        }
         
        public void Process(DbscanPoint<TF> point)
        {
            if (point.ClusterId.HasValue && !Result.Clusters.ContainsKey(point.ClusterId.Value))
            {
                Result.Clusters.Add(point.ClusterId.Value, new List<DbscanPoint<TF>>());
            }

            switch (point)
            {
                case DbscanPoint<TF> core when core.PointType == PointType.Core:
                    Result.Clusters[core.ClusterId.Value].Add(core);
                    break;

                case DbscanPoint<TF> border when border.PointType == PointType.Border:
                    Result.Clusters[border.ClusterId.Value].Add(border);
                    break;

                case DbscanPoint<TF> noise when noise.PointType == PointType.Noise:
                    Result.Noise.Add(noise);
                    break;
            }
        }
    }
}
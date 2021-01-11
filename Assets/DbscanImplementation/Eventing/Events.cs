
using System;

namespace DbscanImplementation.Eventing
{
    public class ComputeFinished
    {
        public Guid ComputeId { get; }

        public ComputeFinished(Guid computeId)
        {
            ComputeId = computeId;
        }
    }

    public class ComputeStarted
    {
        public Guid ComputeId { get; }

        public ComputeStarted(Guid computeId)
        {
            ComputeId = computeId;
        }
    }

    public class PointProcessFinished<TF>
    {
        public DbscanPoint<TF> Point { get; }

        public PointProcessFinished(DbscanPoint<TF> point)
        {
            Point = point;
        }
    }

    public class ClusteringFinished<TF>
    {
        public DbscanPoint<TF> Point { get; }

        public DbscanPoint<TF>[] NeighborPoints { get; }

        public int ClusterId { get; }

        public double Epsilon { get; }

        public int MinimumPoints { get; }

        public ClusteringFinished(DbscanPoint<TF> point, DbscanPoint<TF>[] neighborPoints,
            int clusterId, double epsilon, int minimumPoints)
        {
            Point = point;
            NeighborPoints = neighborPoints;
            ClusterId = clusterId;
            Epsilon = epsilon;
            MinimumPoints = minimumPoints;
        }
    }

    public class ClusteringStarted<TF>
    {

        public DbscanPoint<TF> Point { get; }

        public DbscanPoint<TF>[] NeighborPoints { get; }

        public int ClusterId { get; }

        public double Epsilon { get; }

        public int MinimumPoints { get; }

        public ClusteringStarted(DbscanPoint<TF> point, DbscanPoint<TF>[] neighborPoints,
            int clusterId, double epsilon, int minimumPoints)
        {
            Point = point;
            NeighborPoints = neighborPoints;
            ClusterId = clusterId;
            Epsilon = epsilon;
            MinimumPoints = minimumPoints;
        }
    }

    public class PointTypeAssigned<TF>
    {
        public DbscanPoint<TF> Point { get; }

        public PointType AssignedType { get; }

        public PointTypeAssigned(DbscanPoint<TF> point, PointType assignedType)
        {
            Point = point;
            AssignedType = assignedType;
        }
    }

    public class RegionQueryFinished<TF>
    {
        public DbscanPoint<TF> Point { get; }

        public DbscanPoint<TF>[] NeighborPoints { get; }

        public RegionQueryFinished(DbscanPoint<TF> point, DbscanPoint<TF>[] neighborPoints)
        {
            Point = point;
            NeighborPoints = neighborPoints;
        }
    }

    public class RegionQueryStarted<TF>
    {
        public DbscanPoint<TF> Point { get; private set; }

        public double Epsilon { get; }

        public int MinimumPoints { get; }

        public RegionQueryStarted(DbscanPoint<TF> point, double epsilon, int minimumPoints)
        {
            Point = point;
            Epsilon = epsilon;
            MinimumPoints = minimumPoints;
        }
    }

    public class PointAlreadyProcessed<TF>
    {
        public DbscanPoint<TF> Point { get; private set; }

        public PointAlreadyProcessed(DbscanPoint<TF> point)
        {
            Point = point;
        }
    }

    public class PointProcessStarted<TF>
    {
        public DbscanPoint<TF> Point { get; private set; }

        public PointProcessStarted(DbscanPoint<TF> point)
        {
            Point = point;
        }
    }
}
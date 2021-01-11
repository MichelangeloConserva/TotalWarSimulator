using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DbscanImplementation.Eventing;
using DbscanImplementation.ResultBuilding;

namespace DbscanImplementation
{
    /// <summary>
    /// DBSCAN algorithm implementation type
    /// </summary>
    /// <typeparam name="TF">Takes dataset item row (features, preferences, vector)</typeparam>
    public class DbscanAlgorithm<TF>
    {
        /// <summary>
        /// distance calculation metric function between two feature
        /// </summary>
        public readonly Func<TF, TF, double> MetricFunction;

        /// <summary>
        /// Curried Function that checking two feature as neighbor 
        /// </summary>
        public readonly Func<TF, double, Func<DbscanPoint<TF>, bool>> RegionQueryPredicate;

        private readonly IDbscanEventPublisher publisher;

        /// <summary>
        /// Takes metric function to compute distances between two <see cref="TF"/>
        /// </summary>
        /// <param name="metricFunc"></param>
        public DbscanAlgorithm(Func<TF, TF, double> metricFunc)
        {
            MetricFunction = metricFunc ?? throw new ArgumentNullException(nameof(metricFunc));

            RegionQueryPredicate =
                (mainFeature, epsilon) => relatedPoint => MetricFunction(mainFeature, relatedPoint.Feature) <= epsilon;

            publisher = new EmptyDbscanEventPublisher();
        }

        public DbscanAlgorithm(Func<TF, TF, double> metricFunc, IDbscanEventPublisher publisher)
            : this(metricFunc)
        {
            this.publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        }

        public Task<DbscanResult<TF>> ComputeClusterDbscanAsync(TF[] allPoints, double epsilon, int minimumPoints,
            CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() =>
                ComputeClusterDbscan(allPoints, epsilon, minimumPoints),
                    cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }

        /// <summary>
        /// Performs the DBSCAN clustering algorithm.
        /// </summary>
        /// <param name="allPoints">feature set</param>
        /// <param name="epsilon">Desired region ball radius</param>
        /// <param name="minimumPoints">Minimum number of points to be in a region</param>
        /// <returns>Overall result of cluster compute operation</returns>
        public DbscanResult<TF> ComputeClusterDbscan(TF[] allPoints, double epsilon, int minimumPoints)
        {
            if (epsilon <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(epsilon), "Must be greater than zero");
            }

            if (minimumPoints <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumPoints), "Must be greater than zero");
            }

            var allPointsDbscan = allPoints.Select(x => new DbscanPoint<TF>(x)).ToArray();

            int clusterId = 0;

            var computeId = Guid.NewGuid();

            publisher.Publish(new ComputeStarted(computeId));

            for (int i = 0; i < allPointsDbscan.Length; i++)
            {
                var currentPoint = allPointsDbscan[i];

                if (currentPoint.PointType.HasValue)
                {
                    publisher.Publish(new PointAlreadyProcessed<TF>(currentPoint));

                    continue;
                }

                publisher.Publish(new PointProcessStarted<TF>(currentPoint));

                publisher.Publish(new RegionQueryStarted<TF>(currentPoint, epsilon, minimumPoints));

                var neighborPoints = RegionQuery(allPointsDbscan, currentPoint.Feature, epsilon);

                publisher.Publish(new RegionQueryFinished<TF>(currentPoint, neighborPoints));

                if (neighborPoints.Length < minimumPoints)
                {
                    currentPoint.PointType = PointType.Noise;

                    publisher.Publish(new PointTypeAssigned<TF>(currentPoint, PointType.Noise));

                    publisher.Publish(new PointProcessFinished<TF>(currentPoint));

                    continue;
                }

                clusterId++;

                currentPoint.ClusterId = clusterId;

                currentPoint.PointType = PointType.Core;

                publisher.Publish(new PointTypeAssigned<TF>(currentPoint, PointType.Core));

                publisher.Publish(new PointProcessFinished<TF>(currentPoint));

                publisher.Publish(
                    new ClusteringStarted<TF>(currentPoint, neighborPoints, clusterId, epsilon, minimumPoints));

                ExpandCluster(allPointsDbscan, neighborPoints, clusterId, epsilon, minimumPoints);

                publisher.Publish(
                    new ClusteringFinished<TF>(currentPoint, neighborPoints, clusterId, epsilon, minimumPoints));
            }

            publisher.Publish(new ComputeFinished(computeId));

            var resultBuilder = new DbscanResultBuilder<TF>();

            foreach (var p in allPointsDbscan)
            {
                resultBuilder.Process(p);
            }

            return resultBuilder.Result;
        }

        /// <summary>
        /// Checks current cluster for expanding it
        /// </summary>
        /// <param name="allPoints">Dataset</param>
        /// <param name="neighborPoints">other points in same region</param>
        /// <param name="clusterId">given clusterId</param>
        /// <param name="epsilon">Desired region ball radius</param>
        /// <param name="minimumPoints">Minimum number of points to be in a region</param>
        private void ExpandCluster(DbscanPoint<TF>[] allPoints, DbscanPoint<TF>[] neighborPoints,
            int clusterId, double epsilon, int minimumPoints)
        {
            for (int i = 0; i < neighborPoints.Length; i++)
            {
                var currentPoint = neighborPoints[i];

                publisher.Publish(new PointProcessStarted<TF>(currentPoint));

                if (currentPoint.PointType == PointType.Noise)
                {
                    currentPoint.ClusterId = clusterId;

                    currentPoint.PointType = PointType.Border;

                    publisher.Publish(new PointTypeAssigned<TF>(currentPoint, PointType.Border));

                    publisher.Publish(new PointProcessFinished<TF>(currentPoint));

                    continue;
                }

                if (currentPoint.PointType.HasValue)
                {
                    publisher.Publish(new PointAlreadyProcessed<TF>(currentPoint));

                    continue;
                }

                currentPoint.ClusterId = clusterId;

                publisher.Publish(new RegionQueryStarted<TF>(currentPoint, epsilon, minimumPoints));

                var otherNeighborPoints = RegionQuery(allPoints, currentPoint.Feature, epsilon);

                publisher.Publish(new RegionQueryFinished<TF>(currentPoint, otherNeighborPoints));

                if (otherNeighborPoints.Length < minimumPoints)
                {
                    currentPoint.PointType = PointType.Border;

                    publisher.Publish(new PointTypeAssigned<TF>(currentPoint, PointType.Border));

                    publisher.Publish(new PointProcessFinished<TF>(currentPoint));

                    continue;
                }

                currentPoint.PointType = PointType.Core;

                publisher.Publish(new PointTypeAssigned<TF>(currentPoint, PointType.Core));

                publisher.Publish(new PointProcessFinished<TF>(currentPoint));

                neighborPoints = neighborPoints.Union(otherNeighborPoints).ToArray();
            }
        }

        /// <summary>
        /// Checks and searchs neighbor points for given point
        /// </summary>
        /// <param name="allPoints">Dbscan points converted from feature set</param>
        /// <param name="mainFeature">Focused feature to be searched neighbors</param>
        /// <param name="epsilon">Desired region ball radius</param>
        /// <returns>Calculated neighbor points</returns>
        public DbscanPoint<TF>[] RegionQuery(DbscanPoint<TF>[] allPoints, TF mainFeature, double epsilon)
        {
            return allPoints.Where(RegionQueryPredicate(mainFeature, epsilon)).ToArray();
        }
    }
}
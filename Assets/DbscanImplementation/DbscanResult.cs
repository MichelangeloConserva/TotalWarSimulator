using System.Collections.Generic;

namespace DbscanImplementation
{
    /// <summary>
    /// Result object of algorithm after clusters computed
    /// </summary>
    /// <typeparam name="TFeature">Feature data contribute into algorithm</typeparam>
    public class DbscanResult<TFeature>
    {
        public DbscanResult()
        {
            Noise = new List<DbscanPoint<TFeature>>();
            Clusters = new Dictionary<int, List<DbscanPoint<TFeature>>>();
        }

        public Dictionary<int, List<DbscanPoint<TFeature>>> Clusters { get; private set; }

        public List<DbscanPoint<TFeature>> Noise { get; private set; }
    }
}
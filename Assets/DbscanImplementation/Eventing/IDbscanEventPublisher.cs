namespace DbscanImplementation.Eventing
{
    /// <summary>
    /// A basic interface for publishing occurring events inside dbscan algorithm.
    /// </summary>
    public interface IDbscanEventPublisher
    {
        /// <summary>
        /// Use for publishing single event
        /// </summary>
        void Publish<TObj>(TObj @event);
    }
}
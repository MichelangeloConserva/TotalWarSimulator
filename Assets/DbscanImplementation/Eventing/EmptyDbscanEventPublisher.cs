namespace DbscanImplementation.Eventing
{
    public class EmptyDbscanEventPublisher : IDbscanEventPublisher
    {
        public void Publish<TQ>(TQ @event)
        {

        }
    }
}
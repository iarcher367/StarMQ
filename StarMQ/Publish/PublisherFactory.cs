namespace StarMQ.Publish
{
    using Core;
    using log4net;

    public class PublisherFactory
    {
        public static IPublisher CreatePublisher(IConnectionConfiguration configuration, ILog log)
        {
            return configuration.PublisherConfirms
                ? (IPublisher) new ConfirmPublisher(configuration, log)
                : new BasicPublisher(log);
        }
    }
}

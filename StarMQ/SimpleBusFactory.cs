namespace StarMQ
{
    using Core;
    using SimpleInjector;

    public class SimpleBusFactory
    {
        /// <summary>
        /// Creates a SimpleBus instance with the default configuration.
        ///
        /// heartbeat=10;host=localhost;password=guest;port=5672;publisherconfirms=false;
        /// timeout=10;username=guest;virtualhost=/
        /// </summary>
        public static ISimpleBus GetBus()
        {
            var container = BuildObjectGraph();

            return container.GetInstance<ISimpleBus>();
        }

        /// <summary>
        /// Creates a SimpleBus instance with defaults overridden by any provided values.
        /// </summary>
        public static ISimpleBus GetBus(string connectionString)
        {
            var container = BuildObjectGraph();

            var configuration = container.GetInstance<IConnectionConfiguration>();
            Global.ParseConfiguration(configuration, connectionString);

            return container.GetInstance<ISimpleBus>();
        }

        private static Container BuildObjectGraph()
        {
            var container = Registration.RegisterServices();

//            Registration.EnableCompression(container);

            return container;
        }
    }
}
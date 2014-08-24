namespace StarMQ
{
    using Core;

    public class SimpleBusFactory
    {
        /// <summary>
        /// Creates a SimpleBus instance with the default configuration.
        ///
        /// host=localhost;password=guest;port=5672;username=guest;virtualhost=/
        /// </summary>
        public static ISimpleBus GetBus()
        {
            var container = Registration.RegisterServices();

            return container.GetInstance<ISimpleBus>();
        }

        /// <summary>
        /// Creates a SimpleBus instance with defaults overridden by any provided values.
        /// </summary>
        public static ISimpleBus GetBus(string connectionString)
        {
            var container = Registration.RegisterServices();

            var configuration = container.GetInstance<IConnectionConfiguration>();
            Global.ParseConfiguration(configuration, connectionString);

            return container.GetInstance<ISimpleBus>();
        }
    }
}
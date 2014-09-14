namespace StarMQ.Test
{
    using Exception;
    using NUnit.Framework;
    using StarMQ.Core;
    using System;

    public class GlobalTest
    {
        private IConnectionConfiguration _configuration;

        [SetUp]
        public void Setup()
        {
            _configuration = new ConnectionConfiguration();
        }

        #region ParseConfiguration
        [Test]
        public void ParseConfigurationShouldSetSetCancelOnHaFailover()
        {
            const string format = "cancelonhafailover={0}";
            const bool cancelOnHaFailover = true;

            var connectionString = String.Format(format, cancelOnHaFailover);

            Global.ParseConfiguration(_configuration, connectionString);

            Assert.That(_configuration.CancelOnHaFailover, Is.EqualTo(cancelOnHaFailover));
        }

        [Test]
        public void ParseConfigurationShouldSetHeartbeat()
        {
            const string format = "heartbeat={0}";
            const int heartbeat = 53;

            var connectionString = String.Format(format, heartbeat);

            Global.ParseConfiguration(_configuration, connectionString);

            Assert.That(_configuration.Heartbeat, Is.EqualTo(heartbeat));
        }

        [Test]
        [ExpectedException(typeof(OverflowException))]
        public void ParseConfigurationShouldThrowExceptionIfHeartBeatIsInvalid()
        {
            const string format = "heartbeat={0}";
            const int heartbeat = -1;

            var connectionString = String.Format(format, heartbeat);

            Global.ParseConfiguration(_configuration, connectionString);

            Assert.That(_configuration.Heartbeat, Is.EqualTo(heartbeat));
        }

        [Test]
        public void ParseConfigurationShouldSetHost()
        {
            const string format = "host={0}";
            const string host = "space";

            var connectionString = String.Format(format, host);

            Global.ParseConfiguration(_configuration, connectionString);

            Assert.That(_configuration.Host, Is.EqualTo(host));
        }

        [Test]
        public void ParseConfigurationShouldSetPassword()
        {
            const string format = "password={0}";
            const string password = "stars";

            var connectionString = String.Format(format, password);

            Global.ParseConfiguration(_configuration, connectionString);

            Assert.That(_configuration.Password, Is.EqualTo(password));
        }

        [Test]
        public void ParseConfigurationShouldSetPort()
        {
            const string format = "port={0}";
            const int port = 50000;

            var connectionString = String.Format(format, port);

            Global.ParseConfiguration(_configuration, connectionString);

            Assert.That(_configuration.Port, Is.EqualTo(port));
        }

        [Test]
        [ExpectedException(typeof(OverflowException))]
        public void ParseConfigurationShouldThrowExceptionIfPortIsInvalid()
        {
            const string format = "port={0}";
            const int port = -1;

            var connectionString = String.Format(format, port);

            Global.ParseConfiguration(_configuration, connectionString);

            Assert.That(_configuration.Port, Is.EqualTo(port));
        }

        [Test]
        public void ParseConfigurationShouldSetPrefetchCount()
        {
            const string format = "prefetchcount={0}";
            const int prefetchCount = 101;

            var connectionString = String.Format(format, prefetchCount);

            Global.ParseConfiguration(_configuration, connectionString);

            Assert.That(_configuration.PrefetchCount, Is.EqualTo(prefetchCount));
        }

        [Test]
        [ExpectedException(typeof(OverflowException))]
        public void ParseConfigurationShouldThrowExceptionIfPrefetchCountIsInvalid()
        {
            const string format = "prefetchcount={0}";
            const int prefetchCount = -1;

            var connectionString = String.Format(format, prefetchCount);

            Global.ParseConfiguration(_configuration, connectionString);

            Assert.That(_configuration.PrefetchCount, Is.EqualTo(prefetchCount));
        }

        [Test]
        public void ParseConfigurationShouldSetSetPublisherConfirms()
        {
            const string format = "publisherconfirms={0}";
            const bool publisherConfirms = true;

            var connectionString = String.Format(format, publisherConfirms);

            Global.ParseConfiguration(_configuration, connectionString);

            Assert.That(_configuration.PublisherConfirms, Is.EqualTo(publisherConfirms));
        }

        [Test]
        public void ParseConfigurationShouldSetTimeout()
        {
            const string format = "timeout={0}";
            const int timeout = 10000;

            var connectionString = String.Format(format, timeout);

            Global.ParseConfiguration(_configuration, connectionString);

            Assert.That(_configuration.Timeout, Is.EqualTo(timeout));
        }

        [Test]
        public void ParseConfigurationShouldSetUsername()
        {
            const string format = "username={0}";
            const string username = "admin";

            var connectionString = String.Format(format, username);

            Global.ParseConfiguration(_configuration, connectionString);

            Assert.That(_configuration.Username, Is.EqualTo(username));
        }

        [Test]
        public void ParseConfigurationShouldSetVirtualHost()
        {
            const string format = "virtualHost={0}";
            const string virtualHost = "galaxy";

            var connectionString = String.Format(format, virtualHost);

            Global.ParseConfiguration(_configuration, connectionString);

            Assert.That(_configuration.VirtualHost, Is.EqualTo(virtualHost));
        }

        [Test]
        public void ParseConfigurationShouldBeCaseInsensitive()
        {
            const string format = "USERNAME={0};poRT={1};PassWOrd={2};";
            const string username = "guest";
            const int port = 42;
            const string password = "guest";

            var connectionString = String.Format(format, username, port, password);

            Global.ParseConfiguration(_configuration, connectionString);

            Assert.That(_configuration.Username, Is.EqualTo(username));
            Assert.That(_configuration.Port, Is.EqualTo(port));
            Assert.That(_configuration.Password, Is.EqualTo(password));
        }

        [Test]
        public void ParseConfigurationShouldBeHandleEmptyPairs()
        {
            const string format = ";host={0};;";
            const string host = "space";

            var connectionString = String.Format(format, host);

            Global.ParseConfiguration(_configuration, connectionString);

            Assert.That(_configuration.Host, Is.EqualTo(host));
        }

        [Test]
        [ExpectedException(typeof(StarMqException))]
        public void ParseConfigurationShouldThrowExceptionIfGivenUnknownSetting()
        {
            const string connectionString = "pword=guest";

            Global.ParseConfiguration(_configuration, connectionString);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfConfigurationIsNull()
        {
            Global.ParseConfiguration(null, String.Empty);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfConnectionStringIsNull()
        {
            Global.ParseConfiguration(new ConnectionConfiguration(), null);
        }
        #endregion

        [Test]
        public void ShouldReturnValue()
        {
            const string value = "StarMQ.Master";

            var actual = Global.Validate("name", value);

            Assert.That(actual, Is.EqualTo(value));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ValidateShouldThrowExceptionIfFieldIsNull()
        {
            Global.Validate(null, String.Empty);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ValidateShouldThrowExceptionIfValueIsNull()
        {
            Global.Validate(String.Empty, null);
        }

        [Test]
        [ExpectedException(typeof(MaxLengthException))]
        public void ValidateShouldThrowExceptionIfValueIsTooLong()
        {
            Global.Validate(String.Empty, new string('*', 256));
        }
    }
}
#region Apache License v2.0
//Copyright 2014 Stephen Yu

//Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except
//in compliance with the License. You may obtain a copy of the License at

//http://www.apache.org/licenses/LICENSE-2.0

//Unless required by applicable law or agreed to in writing, software distributed under the License
//is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
//or implied. See the License for the specific language governing permissions and limitations under
//the License.
#endregion

namespace StarMQ.Publish
{
    using Model;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using System;
    using System.Threading.Tasks;
    using IConnection = Core.IConnection;

    public delegate void BasicReturnHandler(object sender, EventArgs args);

    public interface IPublisher : IDisposable
    {
        IModel Model { get; }
        Task Publish<T>(IMessage<T> message, Action<IModel, IBasicProperties, byte[]> action) where T : class;

        event BasicReturnHandler BasicReturn;
    }

    /// <summary>
    /// All publishes are done over a single channel and on a single thread to enforce clear ownership
    /// of thread-unsafe IModel instances; see RabbitMQ .NET client documentation section 2.10. 
    /// </summary>
    internal abstract class BasePublisher : IPublisher
    {
        protected readonly IConnection Connection;
        protected readonly ILog Log;

        public event BasicReturnHandler BasicReturn;

        public IModel Model { get; private set; }

        protected BasePublisher(IConnection connection, ILog log)
        {
            Connection = connection;
            Log = log;

            Connection.OnConnected += OnConnected;
            Connection.OnDisconnected += OnDisconnected;

            OnConnected();
        }

        protected void OnConnected()
        {
            Model = Connection.CreateModel();
            Model.BasicReturn += HandleBasicReturn;

            Log.Info("Channel opened.");
        }

        protected void OnDisconnected()
        {
            Model.BasicReturn -= HandleBasicReturn;
        }

        private void HandleBasicReturn(IModel model, BasicReturnEventArgs args)
        {
            const string format = "Basic.Return received for message with correlationId '{0}' " +
                                  "from exchange '{1}' with code '{2}:{3}'";

            Log.Warn(String.Format(format, args.BasicProperties.CorrelationId, args.Exchange,
                args.ReplyCode, args.ReplyText));

            var basicReturn = BasicReturn;
            if (basicReturn != null)
                basicReturn(model, args);
        }

        public abstract Task Publish<T>(IMessage<T> message, Action<IModel, IBasicProperties, byte[]> action) where T : class;

        public void Dispose()
        {
            Model.Dispose();
        }
    }
}

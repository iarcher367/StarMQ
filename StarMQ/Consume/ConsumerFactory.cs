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

namespace StarMQ.Consume
{
    using Core;
    using log4net;
    using Message;
    using Model;
    using System;

    public class ConsumerFactory    // TODO: refactor into DI
    {
        public static IConsumer CreateConsumer(Queue queue, Action<IHandlerRegistrar> configure,
            IConnectionConfiguration configuration, IConnection connection, IOutboundDispatcher dispatcher,
            INamingStrategy namingStrategy, IPipeline pipeline, ISerializationStrategy serializationStrategy)
        {
            if (queue == null)
                throw new ArgumentNullException("queue");
            if (configure == null)
                throw new ArgumentNullException("configure");

            var log = LogManager.GetLogger(typeof(HandlerManager));

            var handlerManager = new HandlerManager(log);   // TODO: cleanup, DI-compatible?
            configure(handlerManager);
            handlerManager.Validate();

            log = LogManager.GetLogger(typeof(BasicConsumer));
            var consumer = new BasicConsumer(configuration, connection, dispatcher, handlerManager, log,
                namingStrategy, pipeline, serializationStrategy);

            return queue.Exclusive
                ? (IConsumer)consumer
                : new PersistentConsumerDecorator(consumer, connection);
        }
    }
}

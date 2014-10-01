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
    using Exception;
    using Model;
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public interface IHandlerRegistrar
    {
        /// <summary>
        /// Handler sends a nack to the broker for unhandled exceptions and an ack otherwise.
        /// </summary>
        IHandlerRegistrar Add<T>(Action<T> handler);

        /// <summary>
        /// Allows custom responses to be sent to the broker.
        /// </summary>
        IHandlerRegistrar Add<T>(Func<T, BaseResponse> handler);
    }

    public interface IHandlerManager : IHandlerRegistrar
    {
        Type Default { get; }
        dynamic Get(Type type);
        IHandlerManager Validate();
    }

    internal class HandlerManager : IHandlerManager
    {
        private readonly Dictionary<Type, object> _handlerMap = new Dictionary<Type, object>();

        private readonly ILog _log;

        public Type Default { get; private set; }

        public HandlerManager(ILog log)
        {
            _log = log;
        }

        public IHandlerRegistrar Add<T>(Action<T> handler)
        {
            if (handler == null)
                throw new ArgumentNullException("handler");

            Func<T, BaseResponse> func = x =>
            {
                try
                {
                    handler(x);

                    return new AckResponse();
                }
                catch (Exception)
                {
                    return new NackResponse();
                }
            };

            AddToMap(func);
            return this;
        }

        public IHandlerRegistrar Add<T>(Func<T, BaseResponse> handler)
        {
            if (handler == null)
                throw new ArgumentNullException("handler");

            Func<T, BaseResponse> func = x =>
            {
                try
                {
                    return handler(x);
                }
                catch (Exception ex)
                {
                    _log.Error("Unhandled exception from message handler.", ex);
                    return new NackResponse();
                }
            };

            AddToMap(func);
            return this;
        }

        private void AddToMap<T>(Func<T, BaseResponse> handler)
        {
            var type = typeof(T);

            if (_handlerMap.ContainsKey(type))
                throw new StarMqException("Cannot register multiple handlers for type {0}", type.FullName);

            SetDefault(type);

            _handlerMap.Add(type, handler);
        }

        private void SetDefault(Type type)
        {
            if (_handlerMap.Count == 0)
                Default = type;
        }

        public dynamic Get(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            var method = GetType().GetMethod("GetFunc", BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGenericMethod(type);
            return method.Invoke(this, null);
        }

        /// <summary>
        /// Called via reflection by HandlerManager.Get(type);
        /// </summary>
        private Func<T, BaseResponse> GetFunc<T>() where T : class
        {
            return (Func<T, BaseResponse>)_handlerMap[typeof(T)];
        }

        public IHandlerManager Validate()
        {
            if (_handlerMap.Count == 0)
                throw new StarMqException("At least one handler must be configured.");
            return this;
        }
    }
}
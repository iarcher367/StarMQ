namespace StarMQ.Consume
{
    using Exception;
    using log4net;
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

    public class HandlerManager : IHandlerManager
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
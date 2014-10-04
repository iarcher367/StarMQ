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

namespace StarMQ
{
    using Exception;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public interface IContainer
    {
        IContainer Register<TInterface>(Func<Context, TInterface> generator) where TInterface : class;
        IContainer Register<TInterface, TImplementation>(bool isSingleton = false)
            where TInterface : class
            where TImplementation : class, TInterface;

        /// <summary>
        /// Decorators are applied in order of registration.
        /// </summary>
        IContainer RegisterDecorator<TInterface, TDecorator>(Func<bool> condition)
            where TInterface : class
            where TDecorator : class, TInterface;
        TInterface Resolve<TInterface>() where TInterface : class;
    }

    public class Context
    {
        public Type ConcreteType { get; set; }
    }

    internal class Container : IContainer
    {
        private readonly List<Decorator> _decorators = new List<Decorator>();
        private readonly IDictionary<Type, Func<Context, object>> _generators = new Dictionary<Type, Func<Context, object>>();
        private readonly IDictionary<Type, Info> _mappings = new Dictionary<Type, Info>();
        private readonly IDictionary<Type, object> _singletons = new Dictionary<Type, object>();

        private class Info
        {
            public bool IsSingleton { get; set; }
            public Type Type { get; set; }
        }

        private class Decorator
        {
            public Type Interface { get; set; }
            public Type Concrete { get; set; }
            public Func<bool> Condition { get; set; }
        }

        public IContainer Register<TInterface>(Func<Context, TInterface> generator) where TInterface : class
        {
            var key = typeof(TInterface);

            _generators[key] = generator;
            _mappings[key] = new Info();
            return this;
        }

        public IContainer Register<TInterface, TConcrete>(bool isSingleton = false)
            where TInterface : class
            where TConcrete : class, TInterface
        {
            var count = typeof(TConcrete).GetConstructors().Length;
            var key = typeof(TInterface);

            if (count > 1)
                throw new StarMqException(
                    String.Format("Concrete type has {0} constructors but only 1 is allowed.", count));

            _generators.Remove(key);
            _mappings[key] = new Info
            {
                IsSingleton = isSingleton,
                Type = typeof(TConcrete)
            };
            return this;
        }

        public IContainer RegisterDecorator<TInterface, TDecorator>(Func<bool> condition)
            where TInterface : class
            where TDecorator : class, TInterface
        {
            _decorators.Add(new Decorator
            {
                Interface = typeof(TInterface),
                Concrete = typeof(TDecorator),
                Condition = condition
            });
            return this;
        }

        public TInterface Resolve<TInterface>() where TInterface : class
        {
            var type = typeof(TInterface);
            var instance = GetInstance(type, new Context());

            return (TInterface)ApplyDecorators(instance, type);
        }

        private object GetInstance(Type interfaceType, Context context)
        {
            Info info;
            if (!_mappings.TryGetValue(interfaceType, out info))
                throw new StarMqException("Type not found.");

            if (_singletons.ContainsKey(interfaceType))
                return _singletons[interfaceType];

            var instance = _generators.ContainsKey(interfaceType)
                ? _generators[interfaceType](context)
                : BuildType(info.Type);

            if (info.IsSingleton)
                _singletons.Add(interfaceType, instance);

            return instance;
        }

        private object BuildType(Type concreteType, Type interfaceType = null, object decoratee = null)
        {
            var constructor = concreteType.GetConstructors()[0];
            var parameters = constructor
                .GetParameters()
                .Select(x =>
                {
                    if (x.ParameterType == interfaceType)
                        return decoratee;

                    var instance = GetInstance(x.ParameterType, new Context { ConcreteType = concreteType });
                    return ApplyDecorators(instance, x.ParameterType);
                })
                .ToArray();

            return constructor.Invoke(parameters);
        }

        private object ApplyDecorators(object instance, Type type)
        {
            return _decorators.FindAll(x => x.Interface == type && x.Condition())
                .Aggregate(instance, (acc, f) => BuildType(f.Concrete, type, acc));
        }
    }
}

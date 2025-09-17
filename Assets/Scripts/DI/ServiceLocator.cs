using System;
using System.Collections.Generic;

namespace IdleFramework.DI
{
    /// <summary>
    /// Lightweight service locator used to wire runtime services without relying on static singletons.
    /// </summary>
    public sealed class ServiceLocator
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        /// <summary>
        /// Gets the active service locator for the application.
        /// </summary>
        public static ServiceLocator? Current { get; private set; }

        /// <summary>
        /// Assigns the globally accessible locator instance.
        /// </summary>
        /// <param name="locator">Locator to use.</param>
        public static void SetLocator(ServiceLocator locator)
        {
            Current = locator ?? throw new ArgumentNullException(nameof(locator));
        }

        /// <summary>
        /// Registers a service instance for later resolution.
        /// </summary>
        public void Register<TService>(TService instance) where TService : class
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            _services[typeof(TService)] = instance;
        }

        /// <summary>
        /// Attempts to resolve a service of the given type.
        /// </summary>
        public bool TryResolve<TService>(out TService service) where TService : class
        {
            if (_services.TryGetValue(typeof(TService), out var value) && value is TService typed)
            {
                service = typed;
                return true;
            }

            service = null!;
            return false;
        }

        /// <summary>
        /// Resolves a service of the requested type or throws if missing.
        /// </summary>
        public TService Resolve<TService>() where TService : class
        {
            if (TryResolve<TService>(out var service))
            {
                return service;
            }

            throw new InvalidOperationException($"Service of type {typeof(TService).Name} is not registered.");
        }
    }
}

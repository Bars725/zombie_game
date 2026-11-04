using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Minimal service registry (composition root fills it at Boot).
    /// Register by interface where it helps testing: Services.Register&lt;ISaveService&gt;(new JsonSaveService()).
    /// Keep the number of services small; this is not a DI container.
    /// </summary>
    public static class Services
    {
        private static readonly Dictionary<Type, object> s_Services = new Dictionary<Type, object>();

        public static void Register<T>(T service) where T : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            Type key = typeof(T);
            if (s_Services.ContainsKey(key))
            {
                throw new InvalidOperationException(
                    $"Service {key.Name} is already registered. Unregister it first or check for duplicate bootstrap.");
            }
            s_Services.Add(key, service);
        }

        /// <summary>Removes the service only if the registered instance is this one.</summary>
        public static void Unregister<T>(T service) where T : class
        {
            Type key = typeof(T);
            if (s_Services.TryGetValue(key, out object current) && ReferenceEquals(current, service))
            {
                s_Services.Remove(key);
            }
        }

        public static T Get<T>() where T : class
        {
            if (s_Services.TryGetValue(typeof(T), out object service)) return (T)service;
            throw new InvalidOperationException(
                $"Service {typeof(T).Name} is not registered. Was the Boot scene loaded first?");
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (s_Services.TryGetValue(typeof(T), out object found))
            {
                service = (T)found;
                return true;
            }
            service = null;
            return false;
        }

        public static bool IsRegistered<T>() where T : class => s_Services.ContainsKey(typeof(T));

        public static void Clear() => s_Services.Clear();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayModeStart() => Clear();
    }
}

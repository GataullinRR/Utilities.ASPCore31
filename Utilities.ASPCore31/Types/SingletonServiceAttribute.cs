using Microsoft.Extensions.DependencyInjection;
using System;

namespace Utilities.Types
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class SingletonServiceAttribute : ServiceAttribute
    {
        public bool InstantiateOnFirstRequest { get; }

        public SingletonServiceAttribute(bool instantiateOnFirstRequest = false, 
            RegisterAsPolicy registerAs = RegisterAsPolicy.FirstLevelInterfaces, 
            InstantiationPolicy instantiationPolicy = InstantiationPolicy.OneForAll) 
            : base(ServiceLifetime.Singleton, registerAs, instantiationPolicy)
        {
            InstantiateOnFirstRequest = instantiateOnFirstRequest;
        }
    }
}

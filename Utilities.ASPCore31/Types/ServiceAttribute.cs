using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Utilities.Types
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ServiceAttribute : Attribute
    {
        public ServiceLifetime Lifetime { get; }
        public InstantiationPolicy InstantiationPolicy { get; }
        public RegisterAsPolicy RegisterAsPolicy { get; }

        public ServiceAttribute(ServiceLifetime lifetime,
            RegisterAsPolicy registerAs = RegisterAsPolicy.FirstLevelInterfaces,
            InstantiationPolicy instantiationPolicy = InstantiationPolicy.OneForAll)
        {
            Lifetime = lifetime;
            RegisterAsPolicy = registerAs;
            InstantiationPolicy = instantiationPolicy;
        }
    }
}

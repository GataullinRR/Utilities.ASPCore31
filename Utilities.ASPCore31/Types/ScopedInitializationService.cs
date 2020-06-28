using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Utilities.Extensions;

namespace Utilities.Types
{
    [Service(ServiceLifetime.Scoped, RegisterAsPolicy.Self)]
    public class ScopedInitializationService
    {
        readonly IEnumerable<IInitializibleService> _services;

        public ScopedInitializationService(IEnumerable<IInitializibleService> services)
        {
            _services = services;
        }

        public async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var alreadyInitialized = new List<object>();
            foreach (var service in _services)
            {
                if (service is IInitializibleService initializible
                    && alreadyInitialized.NotContains(service))
                {
                    await initializible.InitializeAsync();
                    alreadyInitialized.Add(service);
                }
            }
        }
    }
}

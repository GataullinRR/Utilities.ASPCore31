using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Utilities.Extensions;
using Utilities.Types;

namespace Utilities.Types
{
    //class ServicesInitializer
    //{
    //    public async Task InitializeAsync(IEnumerable<IInitializibleService> services)
    //    {
    //        var alreadyInitialized = new List<object>();
    //        foreach (var service in services)
    //        {
    //            if (alreadyInitialized.NotContains(service))
    //            {
    //                await service.InitializeAsync();
    //                alreadyInitialized.Add(service);
    //            }
    //        }
    //    }
    //}

    [Service(ServiceLifetime.Singleton, RegisterAsPolicy.Self)]
    public class SingletonInitializationService
    {
        readonly IServiceCollection _services;
        bool _alreadyCalled = false;

        public SingletonInitializationService(IServiceCollection services)
        {
            _services = services;
        }

        public async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            if (_alreadyCalled)
            {
                throw new InvalidOperationException();
            }
            _alreadyCalled = true;

            var alreadyInitialized = new List<object>();
            foreach (var serviceDescription in _services)
            {
                if (serviceDescription.Lifetime == ServiceLifetime.Singleton)
                {
                    object service = null;
                    try
                    {
                        service = serviceProvider.GetService(serviceDescription.ServiceType);
                    }
                    catch // trying to instantiate opened type Microsoft.Extensions.Options.OptionsManager`1[TOptions]'? 
                    {
#warning what is going on?
                    }

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
}

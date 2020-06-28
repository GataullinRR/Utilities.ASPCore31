using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utilities.Types;
using Utilities.Extensions;
using System.Runtime.InteropServices;

namespace Utilities.Extensions
{
    public static class ApplicationEx
    {
        class SingletonInstializationMiddleware
        {
            readonly RequestDelegate _next;
            readonly SingletonInitializationService _initializator;
            readonly SemaphoreSlim _locker = new SemaphoreSlim(1);
            bool _initialized = false;

            public SingletonInstializationMiddleware(RequestDelegate next, SingletonInitializationService initializator)
            {
                _initializator = initializator;
                _next = next;
            }

            public async Task Invoke(HttpContext httpContext, IServiceProvider serviceProvider)
            {
                if (!_initialized)
                {
                    using (await _locker.AcquireAsync())
                    {
                        if (!_initialized)
                        {
                            await _initializator.InitializeAsync(serviceProvider);
                            _initialized = true;
                        }
                    }
                }

                await _next(httpContext);
            }
        }

        public static IApplicationBuilder UseServiceRequiringInstantiation<T>(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ScopedServiceInstantiatorMiddleware<T>>();
        }

        public static IApplicationBuilder UseSingletonInitialization(this IApplicationBuilder app)
        {
            return app.UseMiddleware<SingletonInstializationMiddleware>();
        }
    }
}

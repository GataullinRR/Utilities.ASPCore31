using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Utilities.Types;
using Utilities.Extensions;

namespace Utilities.Extensions
{
    public static class DIEx
    {
        public static IServiceCollection AddAttributeRegisteredServices(this IServiceCollection services)
        {
            Assembly.GetCallingAssembly().FindAndRegisterServicesTo(services);

            return services;
        }

        public static IServiceCollection AddAttributeRegisteredServices(this IServiceCollection services, Assembly assembly)
        {
            assembly.FindAndRegisterServicesTo(services);

            return services;
        }

        public static IServiceCollection AddUtilityServices(this IServiceCollection services)
        {
            Assembly.GetExecutingAssembly().FindAndRegisterServicesTo(services);
            services.AddSingleton(services);

            return services;
        }

#warning think over
        /// <summary>
        /// Registers the service as bunch of its implemented interfaces
        /// </summary>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="services"></param>
        /// <param name="type"></param>
        /// <param name="useSameInstance"></param>
        /// <returns></returns>
        public static IServiceCollection AddAsImplementedInterfaces<TImplementation>(this IServiceCollection services, ServiceLifetime type, bool useSameInstance = false)
            where TImplementation : class
        {
            var implementedServices = typeof(TImplementation).GetInterfaces();
            if (implementedServices.Length == 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (useSameInstance)
            {
                services.Add<TImplementation>(type);
            }

            var register = getRegisterer();
            foreach (var service in implementedServices)
            {
                register(service);
            }

            return services; ///////////////////////////////////////

            Action<Type> getRegisterer()
            {
                switch (type)
                {
                    case ServiceLifetime.Scoped:
                        return t => services.AddScoped(t, typeof(TImplementation));
                    case ServiceLifetime.Singleton:
                        return t => services.AddSingleton(t, typeof(TImplementation));
                    case ServiceLifetime.Transient:
                        if (useSameInstance)
                        {
                            throw new NotSupportedException("SameInstance option is not supported for Transient services by current implementation");
                        }
                        else
                        {
                            return t => services.AddTransient(t, typeof(TImplementation));
                        }

                    default:
                        throw new NotSupportedException();

                        //case ServiceType.SCOPED:
                        //    return t => services.AddScoped(t, sp => sp.GetRequiredService<TImplementation>());
                        //case ServiceType.SINGLETON:
                        //    return t => services.AddSingleton(t, sp => sp.GetRequiredService<TImplementation>());
                        //case ServiceType.TRANSIENT:
                        //    if (useSameInstance)
                        //    {
                        //        throw new NotSupportedException("SameInstance option is not supported for Transient services by current implementation");
                        //    }
                        //    else
                        //    {
                        //        return t => services.AddTransient(t, sp => sp.GetRequiredService<TImplementation>());
                        //    }

                        //default:
                        //    throw new NotSupportedException();
                }
            }
        }

        public static IServiceCollection PutProxyOnSingleton<T>(this IServiceCollection services, Func<T, T> proxyFactory)
            where T : class
        {
            using (var sp = services.BuildServiceProvider())
            {
                var service = sp.GetRequiredService<T>();
                var proxy = proxyFactory(service);

                var serviceDescription = services.Single(sd => sd.ServiceType == typeof(T) && sd.Lifetime == ServiceLifetime.Singleton);
                services.Remove(serviceDescription);

                return services.AddSingleton<T>(proxy);
            }
        }

        public static IServiceCollection Add<T>(this IServiceCollection services, ServiceLifetime type)
            where T : class
        {
            switch (type)
            {
                case ServiceLifetime.Scoped:
                    return services.AddScoped<T>();

                case ServiceLifetime.Singleton:
                    return services.AddSingleton<T>();

                case ServiceLifetime.Transient:
                    return services.AddTransient<T>();

                default:
                    throw new NotSupportedException();
            }
        }



        internal static void FindAndRegisterServicesTo(this Assembly assemblyWithServices, IServiceCollection services)
        {
            var allTypes = assemblyWithServices.DefinedTypes
                .Select(t => t.AsType())
                .ToArray();
            foreach (var implementationType in allTypes)
            {
                var serviceInfo = implementationType.GetCustomAttribute<ServiceAttribute>();
                //var hostedServiceInfo = implementationType.GetCustomAttribute<HostedServiceAttribute>();
                //if (hostedServiceInfo != null)
                //{
                //    var interfaces = implementationType.GetInterfaces();
                //    var serviceTypes = serviceInfo.RegisterAsPolicy switch
                //    {
                //        RegisterAsPolicy.AllInterfaces => interfaces,
                //        RegisterAsPolicy.FirstLevelInterfaces => interfaces
                //            .Where(i => i.GetInterfaces().All(ci => i != ci))
                //            .ToArray(),
                //        RegisterAsPolicy.Self => new Type[] { implementationType },

                //        _ => throw new NotSupportedException()
                //    };

                //    services.AddHostedService
                //}
                 if (serviceInfo != null)
                {
                    var interfaces = implementationType.GetInterfaces();
                    var firstLevelInterfaces = interfaces
                             .Where(i => i.GetInterfaces().All(ci => i != ci))
                             .ToArray();
                    var serviceTypes = serviceInfo.RegisterAsPolicy switch
                    {
                        RegisterAsPolicy.AllInterfaces => interfaces,
                        RegisterAsPolicy.FirstLevelInterfaces => firstLevelInterfaces,
                        RegisterAsPolicy.Self => new Type[] { implementationType },
                        RegisterAsPolicy.SelfAndFirstLevelInterfaces => firstLevelInterfaces.Concat(implementationType),
                        
                        _ => throw new NotSupportedException()
                    };

                    switch (serviceInfo.InstantiationPolicy)
                    {
                        case InstantiationPolicy.OneForAll:
                            if (!services.Any(d => d.ImplementationType == implementationType))
                            {
                                register(implementationType);
                                foreach (var type in serviceTypes.Where(t => t != implementationType)) // fix recursion
                                {
                                    registerThroughFactory(type, sp => sp.GetRequiredService(implementationType));
                                }
                            }
                            break;
                        case InstantiationPolicy.New:
                            foreach (var type in serviceTypes)
                            {
                                register(type);
                            }
                            break;

                        default:
                            throw new NotSupportedException();
                    }

                    void registerThroughFactory(Type serviceType, Func<IServiceProvider, object> factory)
                    {
                        switch (serviceInfo.Lifetime)
                        {
                            case ServiceLifetime.Scoped:
                                services.AddScoped(serviceType, factory);
                                break;
                            case ServiceLifetime.Singleton:
                                services.AddSingleton(serviceType, factory);
                                break;
                            case ServiceLifetime.Transient:
                                services.AddTransient(serviceType, factory);
                                break;

                            default:
                                throw new NotSupportedException();
                        }
                    }
                    void register(Type serviceType)
                    {
                        switch (serviceInfo.Lifetime)
                        {
                            case ServiceLifetime.Scoped:
                                services.AddScoped(serviceType, implementationType);
                                break;
                            case ServiceLifetime.Singleton:
                                services.AddSingleton(serviceType, implementationType);
                                break;
                            case ServiceLifetime.Transient:
                                services.AddTransient(serviceType, implementationType);
                                break;

                            default:
                                throw new NotSupportedException();
                        }
                    }
                }
            }
        }
    }
}

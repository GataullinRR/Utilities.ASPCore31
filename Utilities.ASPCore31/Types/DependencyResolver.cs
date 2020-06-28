using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;
using Utilities.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.Features;

namespace Utilities.Types
{
    [Service(ServiceLifetime.Transient)]
    class DependencyResolver : IDependencyResolver
    {
        readonly IServiceProvider _serviceProvider;

        public DependencyResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public void ResolveProperties(object target, params string[] properties)
        {
            var type = target.GetType();
            var candidateProperties = type
                .GetMembers(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Select(mi => mi.As<PropertyInfo>())
                .SkipNulls()
                .Where(pi => pi.GetCustomAttribute<InjectAttribute>() != null)
                .Where(pi => properties.IsNullOrEmpty() 
                    ? true 
                    : properties.Contains(pi.Name))
                .ToArray();
            var invalidProperties = candidateProperties
                .Where(pi => pi.CanWrite
                    && pi.CanRead
                    && pi.GetGetMethod(true).IsPublic
                    && pi.GetSetMethod(true).IsPublic == false)
                .ToArray();
            if (invalidProperties.Length > 0)
            {
                throw new InvalidOperationException($"Therea are {invalidProperties.Length} properties that can not be resolved. Ensure all accessors are public.");
            }
            foreach (var pi in candidateProperties)
            {
                var value = _serviceProvider.GetRequiredService(pi.PropertyType);
                pi.SetValue(target, value);
            }
        }
    }
}

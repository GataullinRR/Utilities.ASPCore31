using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace Utilities.Types
{
    class ScopedServiceInstantiatorMiddleware<T>
    {
        readonly RequestDelegate _next;

        public ScopedServiceInstantiatorMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext, T service)
        {
            await _next(httpContext);
        }
    }
}

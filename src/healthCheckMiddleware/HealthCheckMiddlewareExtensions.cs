﻿using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessManager.HealthCheckMiddleware
{
    public static class HealthCheckMiddlewareExtensions
    {
        public static void UseHealthCheck(this IApplicationBuilder builder)
        {
            builder.Map($"/_health", appBuilder => appBuilder.UseMiddleware<HealthCheckMiddleware>());
        }
    }
}

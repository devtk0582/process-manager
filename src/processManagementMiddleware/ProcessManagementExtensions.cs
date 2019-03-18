using Microsoft.AspNetCore.Builder;
using ProcessManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessManager.ProcessManagementMiddleware
{
    public static class ProcessManagementExtensions
    {
        public static IApplicationBuilder UseProcessManagement(this IApplicationBuilder builder)
        {
            return builder.Map($"/{ProcessManagementConstants.PROCESS_MANAGEMENT_ROUTE_PREFIX}", appBuilder => appBuilder.UseMiddleware<ProcessManagementMiddleware>());
        }
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using ProcessManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessManager.ProcessManagementMiddleware
{
    public class ProcessManagementMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private IOptions<ProcessManagerOptions> _options;
        private readonly IProcessManager _processManager;

        public ProcessManagementMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IOptions<ProcessManagerOptions> options, IProcessManager processManager)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<ProcessManagementMiddleware>();
            _options = options;
            _processManager = processManager;
        }

        public async Task Invoke(HttpContext context)
        {
            context.Response.Headers[HeaderNames.CacheControl] = "no-cache";
            context.Response.Headers[HeaderNames.Expires] = "0";
            context.Response.Headers[HeaderNames.Pragma] = "no-cache";
            context.Response.Headers[HeaderNames.ContentType] = "text/html";

            if (!_processManager.IsProcessManagementAuthorized(context))
            {
                await Return404(context);
                return;
            }

            var path = context.Request.Path.Value;
            var isMaster = _options.Value.IsMaster;

            if (path.Contains("/id"))
            {
                await ReturnId(context);
            }
            else if (path.Contains("/event") && isMaster)
            {
                await ProcessEvent(context);
            }
            else if (path.Contains("/list") && isMaster)
            {
                await ReturnList(context);
            }
            else
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("Not Found");
            }
        }

        private async Task Return404(HttpContext context)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("404 - Not Found");
        }

        private async Task ReturnId(HttpContext context)
        {
            context.Response.StatusCode = 200;
            var process = Process.GetCurrentProcess();
            await context.Response.WriteAsync(process.Id.ToString());
        }

        private async Task ReturnList(HttpContext context)
        {
            context.Response.StatusCode = 200;
            var html = GenerateProcessList(context);
            await context.Response.WriteAsync(html);
        }

        private async Task ProcessEvent(HttpContext context)
        {
            var sEventType = context.Request.Query.FirstOrDefault(q => q.Key.ToLower() == "eventid")
                .Value.ToString().ToLower();
            ChildProcessEventType eventType;
            Enum.TryParse(sEventType, out eventType);

            var sSiteId = context.Request.Query.FirstOrDefault(q => q.Key.ToLower() == "siteid")
                .Value.ToString().ToLower();
            Guid siteId;
            Guid.TryParse(sSiteId, out siteId);

            await Task.Run(() => _processManager.ProcessEventMessage(siteId, eventType));

            context.Response.StatusCode = 200;
            await context.Response.WriteAsync("OK");
        }


        private string GenerateProcessList(HttpContext context)
        {
            var processes = _processManager.GetProcesses();

            var initCount = processes.Count(p => p.State == ChildProcessState.Initializing);
            var listeningCount = processes.Count(p => p.State == ChildProcessState.Listening);
            var bustingCacheCount = processes.Count(p => p.State == ChildProcessState.BustingCache);
            var stoppedCount = processes.Count(p => p.State == ChildProcessState.Stopped);
            var healthCheckingCount = processes.Count(p => p.State == ChildProcessState.HealthChecking);
            var healthCheckErrorCount = processes.Count(p => p.State == ChildProcessState.HealthCheckError);

            var processList =
                $"<div>{processes.Count} Processes</div>" +
                "<br/>" +
                $"{initCount} Initializing <br/>" +
                $"{listeningCount} Listening <br/>" +
                $"{bustingCacheCount} Busting Cache <br/>" +
                $"{stoppedCount} Stopped <br/>" +
                $"{healthCheckingCount} Health Checking <br/> " +
                $"{healthCheckErrorCount} Health Check Failed <br/><br/>" +
                "<table>";


            for (var x = 0; x < processes.Count; x++)
            {
                ChildProcess process;

                try
                {
                    process = processes[x];
                }
                catch
                {
                    return "Error building process list.  Please refresh.";
                }

                var domain = $"{process.Host?.Domain}:{process.Host?.Port}";
                processList += $"   <tr>" +
                               $"       <td>{domain} - {process.State} - Last Start Time: {process.LastStartTime}</td>" +
                               $"       <td></td>" +
                               $"   </tr>";
                processList += $"   <tr>" +
                               $"       <td></td>" +
                               $"       <td>" +
                               $"           <div style='overflow-y: scroll; height: 300px; '>" +
                               $"               <table>";

                var orderedLogs = process.LogHistory.GetAll().OrderByDescending(l => l.TimeStamp).ToList();

                for (var i = 0; i < orderedLogs.Count; i++)
                {
                    var log = orderedLogs[i];

                    processList += $"               <tr>" +
                                   $"                   <td></td>" +
                                   $"                   <td>{log.TimeStamp} - {log.LogText}</td>" +
                                   $"               </tr>";
                }
                processList += "                </table>" +
                               "            </div>" +
                               "        </td>" +
                               "    </tr>";


            }
            processList += "</table>";

            var html = $@"
                        <html>
	                        <head>
		                        <title>Process Status</title>
	                        </head>
	                        <body>
		                        <h1>Process Status</h1>
                                {processList}
	                        </body>
                        </html>";
            return html;
        }
    }
}

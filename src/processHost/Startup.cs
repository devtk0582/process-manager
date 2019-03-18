using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProcessManager.Core.Models;
using ProcessManager.Core.Services;
using ProcessManager.HealthCheckMiddleware;
using ProcessManager.ProcessManagementMiddleware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace ProcessManager.ProcessHost
{
    public class Startup
    {
        private ILogger _logger;
        private ILoggerFactory _loggerFactory;
        private IServiceProvider _serviceProvider;
        private string[] _args;


        static public IConfigurationRoot Configuration { get; set; }

        public Startup(IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;

            loggerFactory.AddConsole();

            _logger = loggerFactory.CreateLogger<Startup>();

            _args = Environment.GetCommandLineArgs().Skip(1).ToArray();

            _logger.LogDebug($"Startup args: {string.Join(" ", _args)}");

            var builder = new ConfigurationBuilder()
                .AddCommandLine(_args)
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile($"appsettings.json");

            Configuration = builder.Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            ConfigureOptions(services);

            services.AddSingleton<IProcessManagerApiClient, ProcessManagerApiClient>();;
            services.AddSingleton<IProcessManager, ProcessManager.Core.Models.ProcessManager>();
        }

        public void ConfigureOptions(IServiceCollection services)
        {
            services.Configure<HealthCheckOptions>(options =>
            {
                options.ProcessType = ProcessType.Child;
                options.Localhost = IPAddress.Parse("127.0.0.1");
                options.MaxResponseTimeInSeconds = int.Parse(Configuration["ProcessManagement:MaxResponseTimeInSeconds"]);
            });

            services.Configure<ProcessManagerOptions>(options =>
            {
                options.IsMaster = false;
                options.Localhost = IPAddress.Parse("127.0.0.1");
                options.HealthCheckInterval = int.Parse(Configuration["ProcessManagement:HealthCheckInterval"]);
                options.MaxProcessInitMins = int.Parse(Configuration["ProcessManagement:MaxProcessInitMins"]);
                options.MaxResponseTimeInSeconds = int.Parse(Configuration["ProcessManagement:MaxResponseTimeInSeconds"]);
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IServiceProvider serviceProvider, IApplicationLifetime appLifetime, IProcessManagerApiClient processManagerApiClient, IProcessManager processManager)
        {
            _serviceProvider = serviceProvider;

            app.UseExceptionHandler();
            app.UseHealthCheck();
            app.UseProcessManagement();

            app.Run(async context =>
            {
                await context.Response.WriteAsync("Test Response");
            });

            appLifetime.ApplicationStarted.Register(() =>
            {
                var headers = new Dictionary<string, string> { { ProcessManagementConstants.ProcessManagerHeader, processManager.GetUUID() } };

                processManagerApiClient.GetAsync<string, string>($"/{ProcessManagementConstants.PROCESS_MANAGEMENT_ROUTE_PREFIX}/event/?eventId=0", headers);
            });
        }
    }
}

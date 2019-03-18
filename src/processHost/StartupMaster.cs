using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProcessManager.Core.Models;
using ProcessManager.HealthCheckMiddleware;
using ProcessManager.ProcessManagementMiddleware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace ProcessManager.ProcessHost
{
    public class StartupMaster
    {
        private ILogger _logger;
        private ILoggerFactory _loggerFactory;
        private IServiceProvider _serviceProvider;

        static public IConfigurationRoot Configuration { get; set; }

        public StartupMaster(IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<StartupMaster>();

            var builder = new ConfigurationBuilder()
                .AddCommandLine(Environment.GetCommandLineArgs().Skip(1).ToArray())
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile($"appsettings.json");

            Configuration = builder.Build();
        }

        public void ConfigureOptions(IServiceCollection services)
        {
            //TODO: Load Hosts Via External Api
            services.Configure<ProcessManagerOptions>(options =>
            {
                options.IsMaster = true;
                options.Localhost = IPAddress.Parse("127.0.0.1");
                options.HealthCheckInterval = int.Parse(Configuration["ProcessManagement:HealthCheckInterval"]);
                options.MaxProcessInitMins = int.Parse(Configuration["ProcessManagement:MaxProcessInitMins"]);
                options.MaxResponseTimeInSeconds = int.Parse(Configuration["ProcessManagement:MaxResponseTimeInSeconds"]);
                options.Hosts = Configuration.GetSection("ProcessManagement:Hosts").Get<IEnumerable<ProcessHostInfo>>();
            });

            services.Configure<HealthCheckOptions>(options =>
            {
                options.ProcessType = ProcessType.Master;
                options.Localhost = IPAddress.Parse("127.0.0.1");
                options.MaxResponseTimeInSeconds = int.Parse(Configuration["ProcessManagement:MaxResponseTimeInSeconds"]);
            });
        }

        public void Configure(IApplicationBuilder app, 
            IHostingEnvironment env, 
            IApplicationLifetime appLifetime, 
            IServiceProvider serviceProvider, 
            IProcessManager processManager)
        {
            try
            {
                _serviceProvider = serviceProvider;

                _loggerFactory.AddConsole(Microsoft.Extensions.Logging.LogLevel.Debug);

                ConfigureApplicationLifecycleEvents(appLifetime, processManager);

                app.UseProcessManagement();
                app.UseExceptionHandler();
                app.UseHealthCheck();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public void ConfigureApplicationLifecycleEvents(IApplicationLifetime appLifetime, IProcessManager processManager)
        {
            appLifetime.ApplicationStarted.Register(() =>
            {
                try
                {
                    processManager.StartProcesses();
                }
                catch (Exception e)
                {
                    _logger.LogError($"Master Process Exception: {e}");
                }
            });
        }
    }
}

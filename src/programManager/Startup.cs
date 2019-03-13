using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ProcessManager.ProgramManager
{
    public class Startup
    {
        private ILogger _logger;
        private ILoggerFactory _loggerFactory;
        private IServiceProvider _serviceProvider;
        private string _programPath;

        static public IConfigurationRoot Configuration { get; set; }

        public Startup(IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;

            loggerFactory.AddConsole();

            _logger = loggerFactory.CreateLogger<Startup>();

            var args = Environment.GetCommandLineArgs();

            _logger.LogDebug($"Startup args: {string.Join(" ", args)}");

            var builder = new ConfigurationBuilder()
                .AddCommandLine(args)
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile($"appsettings.json")
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json");

            Configuration = builder.Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {

        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IServiceProvider serviceProvider, IApplicationLifetime appLifetime)
        {
            _serviceProvider = serviceProvider;

            var programPath = Configuration["ProgramPath"];

            if (!File.Exists(programPath))
            {
                throw new ArgumentException("Invalid Path To Program");
            }

            _programPath = Path.GetFullPath(programPath);
        }
    }
}

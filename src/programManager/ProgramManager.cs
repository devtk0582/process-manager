using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcessManager.Core.Models;
using ProcessManager.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ProcessManager.ProgramManager
{
    public class ProgramManager : IProgramManager
    {
        private ILogger _logger;
        private Timer _healthCheckTimer;
        private IProcessManagerApiClient _processManagerApiClient;
        private Process masterProcess;
        private IOptions<ProgramManagerOptions> _programManagerOptions;

        public ProgramManager(ILoggerFactory loggerFactory, 
            IProcessManagerApiClient processManagerApiClient, 
            IOptions<ProgramManagerOptions> programManagerOptions)
        {
            loggerFactory.AddConsole();
            _logger = loggerFactory.CreateLogger<ProgramManager>();
            _processManagerApiClient = processManagerApiClient;
            _programManagerOptions = programManagerOptions;
        }

        public void Configure()
        {
            if (string.IsNullOrEmpty(_programManagerOptions?.Value?.FilePath))
            {
                throw new Exception("Program assembly path is required");
            }

            Task.Run(() => StartProcess());
        }

        public void Start()
        {
            _healthCheckTimer = new Timer(_programManagerOptions.Value.HealthCheckInterval);
            _healthCheckTimer.Elapsed += OnHealthCheckTimer;
            _healthCheckTimer.Start();
        }

        private void OnHealthCheckTimer(object sender, EventArgs e)
        {
            _healthCheckTimer.Stop();

            _logger.LogInformation($"STARTING MASTER PROCESS HEALTH CHECK");

            var healthResult = DoHealthCheck();

            if (!healthResult.IsHealthy)
            {
                _logger.LogCritical($"MASTER PROCESS HEALTH CHECK FAILURE");
                RestartProcess();
            }
            else
            {
                _logger.LogInformation($"HEALTH CHECK SUCCESS");
            }

            _healthCheckTimer.Start();
        }

        private HealthCheckResult DoHealthCheck()
        {
            var headers = new Dictionary<string, string> { { ProcessManagementConstants.ProcessManagerHeader, "81d9a0c0-832f-4ac9-beff-60602810d452" } };
            var response = _processManagerApiClient.GetAsync<string, string>($"/{ProcessManagementConstants.HEALTH_CHECK_ROUTE_PREFIX}", headers).Result;

            var isHealthy =
                response.HttpStatusCode == HttpStatusCode.OK
                && response.Result.Contains("(master)");

            return new HealthCheckResult
            {
                IsHealthy = isHealthy,
                HttpResult = response
            };
        }

        private void RestartProcess()
        {
            if (!masterProcess.HasExited)
            {
                masterProcess.Kill();
            }

            _logger.LogInformation($"ATTEMPTING MASTER PROCESS RESTART");
            StartProcess();
        }

        private void StartProcess()
        {
            string imagePath = _programManagerOptions.Value.FilePath;

            var startInfo = new ProcessStartInfo()
            {
                FileName = imagePath,
                UseShellExecute = false
            };

            masterProcess = new Process()
            {
                StartInfo = startInfo
            };

            masterProcess.Start();
        }
    }
}

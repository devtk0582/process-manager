using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Server;
using ProcessManager.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ProcessManager.Core.Models
{
    public interface IProcessManager
    {
        void StartProcesses();
        void StopProcess(Guid hostId);
        void StartProcess(ProcessHostInfo host);
        void StopAllProcesses();
        void ProcessEventMessage(Guid hostId, ChildProcessEventType eventType);
        List<ChildProcess> GetProcesses();
        string GetUUID();
        bool IsHealthcheckAuthorized(HttpContext context);
        bool IsProcessManagementAuthorized(HttpContext context);
    }

    public class ProcessManager : IProcessManager
    {
        private readonly IProcessManagerApiClient _httpClient;
        private readonly ILogger _logger;
        private readonly IOptions<ProcessManagerOptions> _options;
        private readonly List<ChildProcess> _processes;
        private readonly Timer _healthCheckTimer;
        private bool _processesStarted;
        private string _uuid;

        public ProcessManager(
            IProcessManagerApiClient httpClient,
            ILoggerFactory loggerFactory,
            IOptions<ProcessManagerOptions> options)
        {
            _httpClient = httpClient;
            _logger = loggerFactory.CreateLogger<ProcessManager>();
            _options = options;
            _uuid = GenerateUUID();
            _processes = new List<ChildProcess>();

            _healthCheckTimer = new Timer(_options.Value.HealthCheckInterval);
            _healthCheckTimer.Elapsed += OnHealthCheckTimer;
            _healthCheckTimer.Start();
        }

        public List<ChildProcess> GetProcesses()
        {
            return _processes;
        }

        public string GetUUID()
        {
            return _uuid;
        }

        public void StartProcesses()
        {
            if (_processesStarted)
            {
                return;
            }

            _processesStarted = true;
            var hosts = _options.Value.Hosts;
            Task.Run(() =>
            {
                foreach (var host in hosts)
                {
                    StartProcess(host);
                }
            });
        }

        public void StartProcess(ProcessHostInfo host)
        {
            if (string.IsNullOrWhiteSpace(host?.Domain))
            {
                return;
            }

            string startupArgs = $"/urls=\"{UrlPrefix.Create("http", host.Domain, host.Port, string.Empty)}\" /process=child";
            var process = GetOrphanedProcess(startupArgs, host) 
                ?? new ChildProcess(host, startupArgs, _logger);
            process.StartTime = DateTime.Now;
            StartProcess(process);
        }

        private ChildProcess GetOrphanedProcess(string startupArgs, ProcessHostInfo host)
        {
            var isRunning = DoHealthCheck($"{host.Domain}:{host.Port}").IsHealthy;

            if (!isRunning) return null;

            var orphanedProcessId = GetOrphanedProcessId($"{host.Domain}:{host.Port}");

            if (!orphanedProcessId.HasValue) return null;

            var orphanedProcess = Process.GetProcessById(orphanedProcessId.Value);

            var orphanedChildProcess = new ChildProcess(orphanedProcess, host, startupArgs, _logger);

            Log($"ORPHANED PROCESS RECLAIMED: Id - {orphanedProcess.Id}, Domain - {host.Domain}:{host.Port}", orphanedChildProcess, ChildProcessLogLevel.Info);

            return orphanedChildProcess;
        }

        private int? GetOrphanedProcessId(string host)
        {
            var headers = new Dictionary<string, string> { { "host", host }, { ProcessManagementConstants.ProcessManagerHeader, _uuid } };
            var response = _httpClient.GetAsync<string, string>($"/{ProcessManagementConstants.PROCESS_MANAGEMENT_ROUTE_PREFIX}/id", headers).Result;
            int processId;
            var isInt = int.TryParse(response.Result, out processId);
            if (!isInt)
            {
                return null;
            }
            return processId;
        }

        public void RestartProcess(ChildProcess process)
        {
            process.Kill();

            Log($"ATTEMPTING RESTART: {process?.Host?.Domain}:{process?.Host?.Port}", process, ChildProcessLogLevel.Info);

            StartProcess(process);
        }

        private void StartProcess(ChildProcess process)
        {
            if (!_processes.Contains(process))
            {
                _processes.Add(process);
            }

            process.LastStartTime = DateTime.Now;
            process.ProcessExited -= OnChildProcessExited;
            process.ProcessExited += OnChildProcessExited;
            process.ErrorDataReceived -= OnErrorDataReceived;
            process.ErrorDataReceived += OnErrorDataReceived;
            process.Start();

            Log($"INITIALIZING: {process?.Host?.Domain}:{process?.Host.Port}", process, ChildProcessLogLevel.Info);
        }

        public void StopAllProcesses()
        {
            foreach (var process in _processes)
            {
                process.Kill();
            }
            _processes.Clear();
            _processesStarted = false;
        }

        public void StopProcess(Guid hostId)
        {
            var process = _processes.SingleOrDefault(p => p.Host != null && p.Host.Id == hostId);
            StopProcess(process);
        }

        public void StopProcess(ChildProcess process)
        {
            process?.Kill();
        }

        public void RemoveProcess(ChildProcess process)
        {
            process?.Kill();
            _processes.Remove(process);
        }

        public void ProcessEventMessage(Guid hostId, ChildProcessEventType eventType)
        {
            var process = _processes.FirstOrDefault(p => p.Host != null && p.Host.Id == hostId);

            if (process == null) return;

            switch (eventType)
            {
                case ChildProcessEventType.Started:
                    {
                        process.State = ChildProcessState.Listening;

                        Log($"LISTENING: Process {process.Host.Id} ({process.Host.Domain}:{process.Host.Port}) started. ", process, ChildProcessLogLevel.Info);
                        break;
                    }
            }
        }

        private void DoTimerProcessHealthCheck()
        {
            _healthCheckTimer.Stop();

            ChildProcess process = null;
            try
            {
                _healthCheckTimer.Stop();

                for (int x = 0; x < _processes.Count; x++)
                {
                    process = _processes[x];

                    var shouldCheck =
                        process.State == ChildProcessState.Listening
                        ||
                        process.State == ChildProcessState.Stopped
                        ||
                        (
                            process.State == ChildProcessState.Initializing
                            &&
                            DateTime.Now.AddMinutes(-_options.Value.MaxProcessInitMins) > process.LastStartTime
                        );

                    if (shouldCheck)
                    {
                        ProcessHealthCheck(process);

                        Log($"HEALTH CHECK FINISHED: {process?.Host?.Domain}:{process?.Host?.Port}", process, ChildProcessLogLevel.Info);
                    }
                }
            }
            catch (Exception e)
            {
                Log($"HEALTH CHECK EXCEPTION: {process?.Host?.Domain}:{process?.Host?.Port}: {e}", process, ChildProcessLogLevel.Critical);
            }
            finally
            {
                _healthCheckTimer.Start();
            }
        }

        private void ProcessHealthCheck(ChildProcess process)
        {
            process.State = ChildProcessState.HealthChecking;
            var host = $"{process?.Host?.Domain}:{process?.Host.Port}";

            Log($"STARTING HEALTH CHECK: {host}", process, ChildProcessLogLevel.Info);

            var healthResult = DoHealthCheck(host);

            process.State = healthResult.IsHealthy ? ChildProcessState.Listening : ChildProcessState.HealthCheckError;

            if (!healthResult.IsHealthy)
            {
                Log($"HEALTH CHECK FAILURE: {host}", process, ChildProcessLogLevel.Critical);
                RestartProcess(process);
            }
            else
            {
                Log($"HEALTH CHECK SUCCESS: {host}", process, ChildProcessLogLevel.Info);
            }
        }

        private HealthCheckResult DoHealthCheck(string host)
        {
            var headers = new Dictionary<string, string> { { "host", host }, { ProcessManagementConstants.ProcessManagerHeader, _uuid } };
            var response = _httpClient.GetAsync<string, string>($"/{ProcessManagementConstants.HEALTH_CHECK_ROUTE_PREFIX}", headers).Result;

            var isHealthy =
                response.HttpStatusCode == HttpStatusCode.OK
                &&
                response.Result.Contains("(child)");

            return new HealthCheckResult
            {
                IsHealthy = isHealthy,
                HttpResult = response
            };
        }

        private void Log(string logText, ChildProcess process, ChildProcessLogLevel logLevel, int eventId = 0)
        {
            if (!string.IsNullOrEmpty(logText))
            {
                switch (logLevel)
                {
                    case ChildProcessLogLevel.Info:
                        _logger.LogInformation(eventId, logText);
                        break;
                    case ChildProcessLogLevel.Debug:
                        _logger.LogDebug(eventId, logText);
                        break;
                    case ChildProcessLogLevel.Error:
                        _logger.LogError(logText);
                        break;
                    case ChildProcessLogLevel.Critical:
                        _logger.LogCritical(logText);
                        break;
                }

                process.UpdateLogHistory(logText);
            }
        }

        private void OnChildProcessExited(object sender, EventArgs e)
        {
            var process = sender as ChildProcess;

            Log(
                $"Child process ({process?.Host?.Domain}:{process?.Host?.Port}) Exited",
                process,
                ChildProcessLogLevel.Info);
        }

        private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            var process = sender as ChildProcess;

            if (process == null) return;

            Log(
                $"Child process ({process?.Host?.Domain}:{process?.Host?.Port}) - Error data received: {e?.Data}",
                process,
                ChildProcessLogLevel.Error);
        }

        private void OnHealthCheckTimer(object sender, EventArgs e)
        {
            DoTimerProcessHealthCheck();
        }

        private string GenerateUUID()
        {
            //TODO: generate from assmebly
            return "81d9a0c0-832f-4ac9-beff-60602810d452";
            //return (Assembly.GetExecutingAssembly().GetCustomAttribute(typeof(GuidAttribute)) as GuidAttribute)?.Value;
        }

        public bool IsHealthcheckAuthorized(HttpContext context)
        {
            return IsTokenAuthorized(context);
        }

        public bool IsProcessManagementAuthorized(HttpContext context)
        {
            return context.Connection.RemoteIpAddress.Equals(_options.Value.Localhost) && IsTokenAuthorized(context);
        }

        private bool IsTokenAuthorized(HttpContext context)
        {
            return
                (
                    (
                        context.Request.Headers.ContainsKey(ProcessManagementConstants.ProcessManagerHeader)
                        &&
                        context.Request.Headers[ProcessManagementConstants.ProcessManagerHeader].Equals(_uuid)
                    )
                    ||
                    (
                        context.Request.Query.ContainsKey("uuid")
                        &&
                        context.Request.Query["uuid"].Equals(_uuid))
                );
        }
    }
}

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ProcessManager.Core.Models
{
    public class ChildProcess
    {
        public ProcessHostInfo Host { get; set; }
        public ChildProcessState State { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? LastStartTime { get; set; }
        public Process Instance { get; set; }
        public string StartupArgs { get; set; }

        private readonly ILogger _logger;

        public ProcessLogQueue<ChildProcessLogHistoryEntry> LogHistory { get; set; }

        public event EventHandler<EventArgs> ProcessExited;
        public event EventHandler<DataReceivedEventArgs> OutputDataReceived;
        public event EventHandler<DataReceivedEventArgs> ErrorDataReceived;

        public ChildProcess(ProcessHostInfo host, 
            string startupArgs, 
            ILogger logger)
        {
            Host = host;
            StartupArgs = startupArgs;
            _logger = logger;
            State = ChildProcessState.Stopped;
            LogHistory = new ProcessLogQueue<ChildProcessLogHistoryEntry>(50);
        }

        public ChildProcess(Process process, 
            ProcessHostInfo host, 
            string startupArgs, 
            ILogger logger)
        {
            Instance = process;
            Host = host;
            StartupArgs = startupArgs;
            _logger = logger;
            State = ChildProcessState.Listening;
            LogHistory = new ProcessLogQueue<ChildProcessLogHistoryEntry>(50);
        }

        private void Configure()
        {
            string imagePath = Process.GetCurrentProcess().MainModule.FileName;

            var startInfo = new ProcessStartInfo()
            {
                FileName = imagePath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                Arguments = StartupArgs
            };

            Instance = new Process()
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            Instance.Exited += OnChildProcessExited;
            Instance.ErrorDataReceived += OnErrorDataReceived;
            Instance.OutputDataReceived += (s, e) =>
            {
                Console.WriteLine(e.Data);
            };
        }

        public void Start()
        {
            try
            {
                if (State != ChildProcessState.Listening)
                {
                    Configure();
                    State = ChildProcessState.Initializing;
                    Instance.Start();
                    Instance.BeginOutputReadLine();
                    Instance.BeginErrorReadLine();
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical($"Exception starting process ({Host?.Domain}:{Host?.Port}): \r\n {e}");
            }

        }

        public void Kill()
        {
            State = ChildProcessState.Stopping;
            try
            {
                if (!Instance.HasExited)
                {
                    Instance.Kill();
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical($"Exception killing process ({Host?.Domain}:{Host?.Port}): \r\n {e}");
            }
        }

        public void UpdateLogHistory(string logText)
        {
            var newLog = new ChildProcessLogHistoryEntry()
            {
                TimeStamp = DateTime.Now,
                LogText = logText
            };
            LogHistory.Enqueue(newLog);
        }

        private void OnChildProcessExited(object sender, EventArgs e)
        {
            ProcessExited?.Invoke(this, e);
        }

        private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            ErrorDataReceived?.Invoke(this, e);
        }
    }
}

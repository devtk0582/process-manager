using ProcessManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace ProcessManager.HealthCheckMiddleware
{
    public class HealthCheckOptions
    {
        public ProcessType ProcessType { get; set; }
        public IPAddress Localhost { get; set; }
        public int MaxResponseTimeInSeconds { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace ProcessManager.Core.Models
{
    public class ProcessManagerOptions
    {
        public bool IsMaster { get; set; }
        public IPAddress Localhost { get; set; }
        public int HealthCheckInterval { get; set; }
        public int MaxProcessInitMins { get; set; }
        public int MaxResponseTimeInSeconds { get; set; }
    }
}

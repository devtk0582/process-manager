using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessManager.Core.Models
{
    public class HealthCheckResult
    {
        public bool IsHealthy { get; set; }
        public ApiClientResult<string, string> HttpResult { get; set; }
    }
}

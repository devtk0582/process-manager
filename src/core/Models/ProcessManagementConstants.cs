using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessManager.Core.Models
{
    public class ProcessManagementConstants
    {
        public const string BaseUrl = "http://127.0.0.1";
        public const string ProcessManagerHeader = "X-PROCESS-MANAGER";
        public const string HEALTH_CHECK_ROUTE_PREFIX = "process-health-check";
        public const string PROCESS_MANAGEMENT_ROUTE_PREFIX = "process-management";
    }
}

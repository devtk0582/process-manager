using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessManager.Core.Models
{
    public enum ChildProcessState
    {
        Initializing,
        Listening,
        Stopping,
        Stopped,
        BustingCache,
        HealthChecking,
        HealthCheckError
    }
}

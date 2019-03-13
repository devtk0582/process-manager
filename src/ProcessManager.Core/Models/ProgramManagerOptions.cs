using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessManager.Core.Models
{
    public class ProgramManagerOptions
    {
        public string FilePath { get; set; }
        public int HealthCheckInterval { get; set; }
    }
}

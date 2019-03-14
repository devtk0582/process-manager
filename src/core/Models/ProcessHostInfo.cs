using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessManager.Core.Models
{
    public class ProcessHostInfo
    {
        public Guid Id { get; set; }
        public string Domain { get; set; }
        public int Port { get; set; }
    }
}

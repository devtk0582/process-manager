using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessManager.Core.Models
{
    public class ChildProcessLogHistoryEntry
    {
        public DateTime TimeStamp { get; set; }
        public string LogText { get; set; }
    }
}

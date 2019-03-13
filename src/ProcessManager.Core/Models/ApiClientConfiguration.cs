using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessManager.Core.Models
{
    public class ApiClientConfiguration
    {
        public string BaseUrl { get; set; }
        public KeyValuePair<string, string> AcceptHeader { get; set; }
        public KeyValuePair<string, string> IdentityHeader { get; set; }
        public Dictionary<string, string> AdditionalHeaders { get; set; }
        public int TimeOutMinutes { get; set; }
    }
}

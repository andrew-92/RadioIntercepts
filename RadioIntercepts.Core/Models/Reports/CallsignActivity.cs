using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Reports
{
    public class CallsignActivity
    {
        public string Callsign { get; set; } = null!;
        public int MessageCount { get; set; }
        public int InteractionCount { get; set; }
        public List<string> ActiveAreas { get; set; } = new();
        public TimeSpan AverageResponseTime { get; set; }
        public string Role { get; set; } = null!;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Reports
{
    public class KeyObservation
    {
        public string Type { get; set; } = null!; // "high_activity", "new_callsign", "unusual_pattern", "alert"
        public string Description { get; set; } = null!;
        public string Impact { get; set; } = null!; // "low", "medium", "high"
        public DateTime ObservedAt { get; set; }
        public List<string> RelatedCallsigns { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }
}

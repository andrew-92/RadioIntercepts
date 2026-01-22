using RadioIntercepts.Core.Models.Alerts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models
{
    public class AlertSummary
    {
        public string RuleName { get; set; } = null!;
        public int Count { get; set; }
        public AlertSeverity HighestSeverity { get; set; }
        public List<string> AffectedCallsigns { get; set; } = new();
        public DateTime FirstAlert { get; set; }
        public DateTime LastAlert { get; set; }
    }
}

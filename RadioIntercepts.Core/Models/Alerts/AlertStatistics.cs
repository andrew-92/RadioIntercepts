using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Alerts
{
    public class AlertStatistics
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public int TotalAlerts { get; set; }
        public int ActiveAlerts { get; set; }
        public Dictionary<AlertSeverity, int> AlertsBySeverity { get; set; } = new();
        public Dictionary<AlertStatus, int> AlertsByStatus { get; set; } = new();
        public Dictionary<string, int> AlertsByRule { get; set; } = new();
        public TimeSpan AverageResponseTime { get; set; }
        public TimeSpan AverageResolutionTime { get; set; }
    }
}

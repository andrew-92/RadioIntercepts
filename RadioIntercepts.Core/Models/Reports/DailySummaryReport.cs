using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Reports
{
    public class DailySummaryReport
    {
        public DateTime ReportDate { get; set; }
        public int TotalMessages { get; set; }
        public int UniqueCallsigns { get; set; }
        public int ActiveAreas { get; set; }
        public List<CallsignActivity> TopCallsigns { get; set; } = new();
        public List<AreaActivity> TopAreas { get; set; } = new();
        public List<AlertSummary> Alerts { get; set; } = new();
        public List<PatternSummary> DetectedPatterns { get; set; } = new();
        public CommunicationMetrics Metrics { get; set; } = new();
        public List<KeyObservation> Observations { get; set; } = new();
    }
}

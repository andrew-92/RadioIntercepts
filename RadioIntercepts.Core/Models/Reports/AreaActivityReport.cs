using RadioIntercepts.Core.Models.Communication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Reports
{
    public class AreaActivityReport
    {
        public string Area { get; set; } = null!;
        public DateTime ReportPeriodStart { get; set; }
        public DateTime ReportPeriodEnd { get; set; }
        public List<CallsignActivity> ActiveCallsigns { get; set; } = new();
        public ActivityTimeline Timeline { get; set; } = new();
        public List<CommunicationPattern> CommonPatterns { get; set; } = new();
        public List<AlertSummary> AreaAlerts { get; set; } = new();
        public AreaMetrics Metrics { get; set; } = new();
        public List<KeyObservation> Observations { get; set; } = new();
        public List<Recommendation> Recommendations { get; set; } = new();
    }
}

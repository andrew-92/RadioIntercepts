using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RadioIntercepts.Core.Models.Reports;

namespace RadioIntercepts.Analysis.Services.Reports
{
    public class ReportStatistics
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public int TotalReports { get; set; }
        public int CompletedReports { get; set; }
        public int FailedReports { get; set; }
        public int PendingReports { get; set; }
        public Dictionary<string, int> ReportsByTemplate { get; set; } = new();
        //public Dictionary<ReportFormat, int> ReportsByFormat { get; set; } = new();
        public TimeSpan AverageGenerationTime { get; set; }
    }
}

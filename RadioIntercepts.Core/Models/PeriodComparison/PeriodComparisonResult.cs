using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.PeriodComparison
{
    public class PeriodComparisonResult
    {
        public PeriodStats Period1 { get; set; } = new PeriodStats();
        public PeriodStats Period2 { get; set; } = new PeriodStats();
        public ComparisonMetrics Metrics { get; set; } = new ComparisonMetrics();
        public List<CallsignComparison> CallsignComparisons { get; set; } = new List<CallsignComparison>();
        public List<AreaComparison> AreaComparisons { get; set; } = new List<AreaComparison>();
        public List<FrequencyComparison> FrequencyComparisons { get; set; } = new List<FrequencyComparison>();
        public List<MessageTypeComparison> MessageTypeComparisons { get; set; } = new List<MessageTypeComparison>();
    }
}

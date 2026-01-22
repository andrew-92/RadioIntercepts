using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Communication
{
    public class FlowStatistics
    {
        public int TotalMessages { get; set; }
        public int UniqueCallsigns { get; set; }
        public int UniqueLinks { get; set; }
        public double AverageMessagesPerMinute { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public double NetworkDensity { get; set; }
        public List<string> TopCallsigns { get; set; } = new();
        public List<string> CriticalLinks { get; set; } = new(); // Критические связи
        public Dictionary<string, double> CentralityMetrics { get; set; } = new();
    }
}

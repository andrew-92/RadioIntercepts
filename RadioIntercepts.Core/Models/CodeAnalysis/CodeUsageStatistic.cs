using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.CodeAnalysis
{
    public class CodeUsageStatistic
    {
        public string Term { get; set; } = null!;
        public CodeTermCategory Category { get; set; }
        public int TotalUsageCount { get; set; }
        public int UniqueCallsignsCount { get; set; }
        public int UniqueAreasCount { get; set; }
        public DateTime FirstUsage { get; set; }
        public DateTime LastUsage { get; set; }
        public Dictionary<string, int> UsageByCallsign { get; set; } = new();
        public Dictionary<string, int> UsageByArea { get; set; } = new();
        public Dictionary<DateTime, int> UsageOverTime { get; set; } = new();
        public double AverageMessagesPerDay { get; set; }
        public double Trend { get; set; } // Тренд использования (+ увеличение, - уменьшение)
    }
}

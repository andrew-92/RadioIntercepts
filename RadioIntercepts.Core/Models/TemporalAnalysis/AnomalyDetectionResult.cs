using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.TemporalAnalysis
{
    public class AnomalyDetectionResult
    {
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } = null!; // "Необычная активность", "Долгое молчание", "Новый позывной" и т.д.
        public string Description { get; set; } = null!;
        public double Severity { get; set; } // 0-1
        public List<string> RelatedCallsigns { get; set; } = new();
        public List<string> RelatedAreas { get; set; } = new();
    }
}

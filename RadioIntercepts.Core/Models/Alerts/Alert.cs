using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Alerts
{
    public class Alert
    {
        public long Id { get; set; }
        public int RuleId { get; set; }
        public AlertRule Rule { get; set; } = null!;
        public AlertSeverity Severity { get; set; }
        public AlertStatus Status { get; set; } = AlertStatus.Active;
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Details { get; set; } = null!;
        public DateTime DetectedAt { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? AcknowledgedBy { get; set; }
        public string? ResolutionNotes { get; set; }

        // Контекстные данные
        public List<string> RelatedCallsigns { get; set; } = new();
        public List<string> RelatedAreas { get; set; } = new();
        public List<string> RelatedFrequencies { get; set; } = new();
        public List<long> RelatedMessageIds { get; set; } = new();
    }
}

// Core/Models/Reports.cs
namespace RadioIntercepts.Core.Models
{
    public class ReportTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public ReportType Type { get; set; }
        public string TemplatePath { get; set; } = null!;
        public List<ReportParameter> Parameters { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public enum ReportType
    {
        DailySummary,
        CallsignActivity,
        AreaAnalysis,
        CommunicationFlow,
        AlertSummary,
        PatternAnalysis,
        Custom
    }

    public class ReportParameter
    {
        public string Name { get; set; } = null!;
        public string Type { get; set; } = null!; // "date", "string", "int", "bool", "list"
        public string DefaultValue { get; set; } = null!;
        public bool Required { get; set; }
        public List<string> Options { get; set; } = new();
    }

    public class GeneratedReport
    {
        public string ReportId { get; set; } = null!;
        public string TemplateName { get; set; } = null!;
        public DateTime GeneratedAt { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = null!; // "pdf", "html", "csv", "excel", "word"
        public string FileName { get; set; } = null!;
        public ReportStatus Status { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public enum ReportStatus
    {
        Pending,
        Generating,
        Completed,
        Failed
    }

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

    public class CallsignActivity
    {
        public string Callsign { get; set; } = null!;
        public int MessageCount { get; set; }
        public int InteractionCount { get; set; }
        public List<string> ActiveAreas { get; set; } = new();
        public TimeSpan AverageResponseTime { get; set; }
        public string Role { get; set; } = null!;
    }

    public class AreaActivity
    {
        public string Area { get; set; } = null!;
        public int MessageCount { get; set; }
        public int ActiveCallsigns { get; set; }
        public DateTime PeakActivityTime { get; set; }
        public double ActivityLevel { get; set; }
    }

    public class AlertSummary
    {
        public string RuleName { get; set; } = null!;
        public int Count { get; set; }
        public AlertSeverity HighestSeverity { get; set; }
        public List<string> AffectedCallsigns { get; set; } = new();
        public DateTime FirstAlert { get; set; }
        public DateTime LastAlert { get; set; }
    }

    public class PatternSummary
    {
        public string PatternType { get; set; } = null!;
        public int Occurrences { get; set; }
        public double Confidence { get; set; }
        public List<string> ExampleCallsigns { get; set; } = new();
        public string Description { get; set; } = null!;
    }

    public class CommunicationMetrics
    {
        public double NetworkDensity { get; set; }
        public double ResponseRate { get; set; }
        public TimeSpan AverageReactionTime { get; set; }
        public double FlowEfficiency { get; set; }
        public double Centralization { get; set; }
        public Dictionary<string, double> AdditionalMetrics { get; set; } = new();
    }

    public class KeyObservation
    {
        public string Type { get; set; } = null!; // "high_activity", "new_callsign", "unusual_pattern", "alert"
        public string Description { get; set; } = null!;
        public string Impact { get; set; } = null!; // "low", "medium", "high"
        public DateTime ObservedAt { get; set; }
        public List<string> RelatedCallsigns { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    public class CallsignDossier
    {
        public string Callsign { get; set; } = null!;
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
        public int TotalMessages { get; set; }
        public List<string> FrequentInterlocutors { get; set; } = new();
        public List<string> ActiveAreas { get; set; } = new();
        public CommunicationStyle Style { get; set; } = new();
        public RoleAnalysis Role { get; set; } = new();
        public List<KeyInteraction> KeyInteractions { get; set; } = new();
        public List<PatternParticipation> PatternInvolvement { get; set; } = new();
        public List<AlertInvolvement> Alerts { get; set; } = new();
        public List<BehavioralChange> BehavioralChanges { get; set; } = new();
        public List<Recommendation> Recommendations { get; set; } = new();
    }

    public class CommunicationStyle
    {
        public double AverageMessageLength { get; set; }
        public double QuestionRatio { get; set; }
        public double CommandRatio { get; set; }
        public double ReportRatio { get; set; }
        public Dictionary<string, double> StyleMetrics { get; set; } = new();
        public List<string> CharacteristicPhrases { get; set; } = new();
    }

    public class RoleAnalysis
    {
        public string PrimaryRole { get; set; } = null!;
        public double RoleConfidence { get; set; }
        public Dictionary<string, double> RoleProbabilities { get; set; } = new();
        public List<string> RoleIndicators { get; set; } = new();
    }

    public class KeyInteraction
    {
        public string WithCallsign { get; set; } = null!;
        public int InteractionCount { get; set; }
        public DateTime FirstInteraction { get; set; }
        public DateTime LastInteraction { get; set; }
        public string Pattern { get; set; } = null!; // "frequent", "recent", "intense"
        public double Strength { get; set; }
    }

    public class PatternParticipation
    {
        public string PatternType { get; set; } = null!;
        public int ParticipationCount { get; set; }
        public string RoleInPattern { get; set; } = null!;
        public double Frequency { get; set; }
    }

    public class AlertInvolvement
    {
        public string AlertType { get; set; } = null!;
        public int Count { get; set; }
        public DateTime LastInvolvement { get; set; }
        public string Severity { get; set; } = null!;
    }

    public class BehavioralChange
    {
        public DateTime ChangeDate { get; set; }
        public string ChangeType { get; set; } = null!; // "activity_increase", "area_change", "pattern_change"
        public string Description { get; set; } = null!;
        public double Magnitude { get; set; }
        public List<string> PossibleReasons { get; set; } = new();
    }

    public class Recommendation
    {
        public string Type { get; set; } = null!; // "monitoring", "investigation", "analysis"
        public string Description { get; set; } = null!;
        public string Priority { get; set; } = null!; // "low", "medium", "high"
        public List<string> Actions { get; set; } = new();
    }

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

    public class ActivityTimeline
    {
        public List<HourlyActivity> HourlyData { get; set; } = new();
        public List<DailyActivity> DailyData { get; set; } = new();
        public DateTime PeakTime { get; set; }
        public double AverageActivity { get; set; }
    }

    public class HourlyActivity
    {
        public int Hour { get; set; }
        public int MessageCount { get; set; }
        public int CallsignCount { get; set; }
        public double ActivityLevel { get; set; }
    }

    public class DailyActivity
    {
        public DateTime Date { get; set; }
        public int MessageCount { get; set; }
        public int CallsignCount { get; set; }
        public double Trend { get; set; }
    }

    public class AreaMetrics
    {
        public double ActivityDensity { get; set; }
        public double CallsignTurnover { get; set; }
        public double PatternRichness { get; set; }
        public double AlertFrequency { get; set; }
        public Dictionary<string, double> AdditionalMetrics { get; set; } = new();
    }
}
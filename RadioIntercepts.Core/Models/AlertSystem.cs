// Core/Models/AlertSystem.cs
namespace RadioIntercepts.Core.Models
{
    public enum AlertSeverity
    {
        Info,       // Информационное
        Low,        // Низкая важность
        Medium,     // Средняя важность
        High,       // Высокая важность
        Critical    // Критическая важность
    }

    public enum AlertStatus
    {
        Active,     // Активное (не обработанное)
        Acknowledged, // Подтверждено оператором
        Resolved,   // Решено
        FalseAlarm  // Ложное срабатывание
    }

    public class AlertRule
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public AlertSeverity Severity { get; set; }
        public bool IsEnabled { get; set; } = true;
        public string ConditionExpression { get; set; } = null!;
        public TimeSpan CheckInterval { get; set; } = TimeSpan.FromMinutes(5);
        public DateTime? LastChecked { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

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

    public class AlertNotification
    {
        public Alert Alert { get; set; } = null!;
        public bool ShowPopup { get; set; }
        public bool SendEmail { get; set; }
        public bool SendTelegram { get; set; }
        public bool SendWebhook { get; set; }
        public string SoundAlert { get; set; } = string.Empty;
    }

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

    public class AlertHistoryEntry
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = null!;
        public string User { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Details { get; set; } = null!;
    }
}
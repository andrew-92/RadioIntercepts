// Core/Models/AlertSystem.cs
namespace RadioIntercepts.Core.Models.Alerts
{
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
}
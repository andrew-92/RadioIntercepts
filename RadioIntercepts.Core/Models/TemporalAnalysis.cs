// Core/Models/TemporalAnalysis.cs
namespace RadioIntercepts.Core.Models
{
    public class TimeSlotAnalysis
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public List<TimeSlot> Slots { get; set; } = new();
        public TimeSlot PeakSlot { get; set; }
        public TimeSlot QuietSlot { get; set; }
        public double ActivityVariation { get; set; } // Коэффициент вариации активности
    }

    public class TimeSlot
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int MessageCount { get; set; }
        public int ActiveCallsigns { get; set; }
        public double ActivityLevel => MessageCount / (EndTime - StartTime).TotalHours;
        public string Description => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
    }

    public class TemporalPattern
    {
        public string PatternType { get; set; } = null!; // "Утренний пик", "Ночной спад" и т.д.
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public double Confidence { get; set; }
        public List<string> TypicalCallsigns { get; set; } = new();
        public List<string> TypicalAreas { get; set; } = new();
    }

    public class AnomalyDetectionResult
    {
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } = null!; // "Необычная активность", "Долгое молчание", "Новый позывной" и т.д.
        public string Description { get; set; } = null!;
        public double Severity { get; set; } // 0-1
        public List<string> RelatedCallsigns { get; set; } = new();
        public List<string> RelatedAreas { get; set; } = new();
    }

    public class PredictionResult
    {
        public DateTime PredictedTime { get; set; }
        public double Probability { get; set; }
        public string PredictedEvent { get; set; } = null!; // "Активность позывного X", "Сообщение в зоне Y"
        public double Confidence { get; set; }
    }
}
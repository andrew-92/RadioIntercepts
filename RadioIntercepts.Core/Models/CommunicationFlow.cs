// Core/Models/CommunicationFlow.cs
namespace RadioIntercepts.Core.Models
{
    public class CommunicationFlow
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<FlowNode> Nodes { get; set; } = new();
        public List<FlowLink> Links { get; set; } = new();
        public List<MessageFlow> MessageFlows { get; set; } = new();
        public Dictionary<string, FlowStatistics> Statistics { get; set; } = new();
    }

    public class FlowNode
    {
        public string Id { get; set; } = null!;
        public string Label { get; set; } = null!;
        public NodeType Type { get; set; }
        public int Size { get; set; } // Размер узла (количество сообщений/взаимодействий)
        public string Color { get; set; } = "#2196F3";
        public List<string> Groups { get; set; } = new();
        public double Centrality { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public enum NodeType
    {
        Callsign,
        Area,
        Frequency,
        TimeSlot,
        Cluster
    }

    public class FlowLink
    {
        public string SourceId { get; set; } = null!;
        public string TargetId { get; set; } = null!;
        public double Value { get; set; } // Вес связи (количество сообщений)
        public string Color { get; set; } = "#666666";
        public double Strength { get; set; } = 1.0; // Сила связи (нормированная)
        public List<DateTime> InteractionTimes { get; set; } = new();
        public TimeSpan AverageInterval { get; set; }
        public double Consistency { get; set; } // Консистентность взаимодействий
    }

    public class MessageFlow
    {
        public long MessageId { get; set; }
        public DateTime Timestamp { get; set; }
        public List<string> Path { get; set; } = new(); // Путь сообщения через узлы
        public double Duration { get; set; } // Длительность обработки (в секундах)
        public string MessageType { get; set; } = null!;
        public string Content { get; set; } = null!;
        public bool IsComplete { get; set; } // Полный ли поток (есть ответы)
        public List<string> Callsigns { get; set; } = new();
    }

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

    public class TimelineEvent
    {
        public DateTime Time { get; set; }
        public string Label { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Type { get; set; } = null!;
        public List<string> Callsigns { get; set; } = new();
        public string Color { get; set; } = "#2196F3";
        public double Duration { get; set; } // Длительность события в минутах
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class ParallelTimeline
    {
        public List<TimelineTrack> Tracks { get; set; } = new();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public Dictionary<string, TimelineStatistics> TrackStats { get; set; } = new();
    }

    public class TimelineTrack
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Type { get; set; } = null!; // Callsign, Area, Frequency, Group
        public List<TimelineEvent> Events { get; set; } = new();
        public string Color { get; set; } = "#2196F3";
        public double ActivityLevel { get; set; } // Уровень активности (0-1)
    }

    public class TimelineStatistics
    {
        public int EventCount { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public double Density { get; set; } // Плотность событий
        public TimeSpan AverageEventDuration { get; set; }
        public List<DateTime> PeakTimes { get; set; } = new();
    }

    public class CommunicationPattern
    {
        public string PatternType { get; set; } = null!;
        public string Description { get; set; } = null!;
        public List<string> ExampleFlows { get; set; } = new();
        public double Frequency { get; set; }
        public double Confidence { get; set; }
        public List<string> CharacteristicCallsigns { get; set; } = new();
        public TimeSpan TypicalDuration { get; set; }
    }
}
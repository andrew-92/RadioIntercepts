// Core/Models/PeriodComparison.cs
using System;
using System.Collections.Generic;

namespace RadioIntercepts.Core.Models
{
    public class PeriodComparisonRequest
    {
        public DateTime StartDate1 { get; set; }
        public DateTime EndDate1 { get; set; }
        public DateTime StartDate2 { get; set; }
        public DateTime EndDate2 { get; set; }
        public string? Area { get; set; }
        public string? Frequency { get; set; }
        public List<string>? Callsigns { get; set; }
    }

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

    public class PeriodStats
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalMessages { get; set; }
        public int UniqueCallsigns { get; set; }
        public int UniqueAreas { get; set; }
        public int UniqueFrequencies { get; set; }
        public double MessagesPerDay { get; set; }
        public TimeSpan AverageTimeBetweenMessages { get; set; }
        public Dictionary<MessageType, int> MessageTypeDistribution { get; set; } = new Dictionary<MessageType, int>();
        public Dictionary<string, int> TopCallsigns { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> TopAreas { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> TopFrequencies { get; set; } = new Dictionary<string, int>();
    }

    public class ComparisonMetrics
    {
        public double TotalMessagesChange { get; set; } // в процентах
        public double UniqueCallsignsChange { get; set; }
        public double MessagesPerDayChange { get; set; }
        public Dictionary<MessageType, double> MessageTypeChange { get; set; } = new Dictionary<MessageType, double>();
        public List<string> NewCallsigns { get; set; } = new List<string>();
        public List<string> DisappearedCallsigns { get; set; } = new List<string>();
        public List<string> NewAreas { get; set; } = new List<string>();
        public List<string> DisappearedAreas { get; set; } = new List<string>();
        public double ActivityIntensityChange { get; set; } // изменение интенсивности (сообщений в час)
    }

    public class CallsignComparison
    {
        public string Callsign { get; set; } = null!;
        public int CountPeriod1 { get; set; }
        public int CountPeriod2 { get; set; }
        public double ChangePercent { get; set; }
        public double ContributionPeriod1 { get; set; } // вклад в общее количество сообщений в периоде 1
        public double ContributionPeriod2 { get; set; }
    }

    public class AreaComparison
    {
        public string Area { get; set; } = null!;
        public int CountPeriod1 { get; set; }
        public int CountPeriod2 { get; set; }
        public double ChangePercent { get; set; }
    }

    public class FrequencyComparison
    {
        public string Frequency { get; set; } = null!;
        public int CountPeriod1 { get; set; }
        public int CountPeriod2 { get; set; }
        public double ChangePercent { get; set; }
    }

    public class MessageTypeComparison
    {
        public MessageType MessageType { get; set; }
        public int CountPeriod1 { get; set; }
        public int CountPeriod2 { get; set; }
        public double ChangePercent { get; set; }
        public double ContributionPeriod1 { get; set; }
        public double ContributionPeriod2 { get; set; }
    }
}
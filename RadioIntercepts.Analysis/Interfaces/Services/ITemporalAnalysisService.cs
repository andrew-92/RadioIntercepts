using RadioIntercepts.Core.Models.TemporalAnalysis;

namespace RadioIntercepts.Analysis.Interfaces.Services
{
    public interface ITemporalAnalysisService
    {
        Task<TimeSlotAnalysis> AnalyzeActivitySlotsAsync(DateTime? startDate = null, DateTime? endDate = null,
            string? area = null, string? frequency = null, int slotDurationHours = 1);
        Task<List<TemporalPattern>> DetectTemporalPatternsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<List<AnomalyDetectionResult>> DetectAnomaliesAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<List<PredictionResult>> PredictActivityAsync(string callsign, int hoursAhead = 24);
        Task<Dictionary<DayOfWeek, int>> AnalyzeDayOfWeekPatternsAsync(string callsign = null);
        Task<Dictionary<int, int>> AnalyzeHourlyPatternsAsync(string callsign = null); // час -> количество сообщений
        Task<List<DateTime>> FindSilentPeriodsAsync(TimeSpan minDuration, string callsign = null);
        Task<List<DateTime>> FindPeakActivityTimesAsync(int topN = 10, string callsign = null);
    }
}

using RadioIntercepts.Core.Models.Communication;

namespace RadioIntercepts.Analysis.Interfaces.Services
{
    public interface ICommunicationFlowService
    {
        // Sankey-диаграммы потоков
        Task<CommunicationFlow> BuildSankeyDiagramAsync(DateTime startTime, DateTime endTime,
            string? area = null, string? frequency = null, int maxNodes = 50);
        Task<CommunicationFlow> BuildCallsignFlowAsync(string callsign, DateTime startTime, DateTime endTime);
        Task<CommunicationFlow> BuildAreaFlowAsync(string area, DateTime startTime, DateTime endTime);

        // Timeline визуализации
        Task<ParallelTimeline> BuildParallelTimelineAsync(DateTime startTime, DateTime endTime,
            List<string>? callsigns = null, List<string>? areas = null);
        Task<ParallelTimeline> BuildCallsignTimelineAsync(string callsign, DateTime startTime, DateTime endTime);
        Task<ParallelTimeline> BuildConversationTimelineAsync(long startMessageId, int maxMessages = 20);

        // Анализ паттернов
        Task<List<CommunicationPattern>> DetectCommunicationPatternsAsync(DateTime startTime, DateTime endTime);
        Task<List<string>> FindCommonFlowsAsync(int minOccurrences = 3);
        Task<Dictionary<string, double>> CalculateFlowMetricsAsync(DateTime startTime, DateTime endTime);

        // Визуализация групп
        Task<CommunicationFlow> BuildGroupCommunicationFlowAsync(string groupName, DateTime startTime, DateTime endTime);
        Task<List<CommunicationFlow>> CompareTimePeriodsAsync(DateTime period1Start, DateTime period1End,
            DateTime period2Start, DateTime period2End);

        // Экспорт данных для визуализации
        Task<string> ExportFlowDataAsync(CommunicationFlow flow, string format = "json");
        Task<string> ExportTimelineDataAsync(ParallelTimeline timeline, string format = "json");
    }
}

using RadioIntercepts.Core.Models.DialogPatterns;

namespace RadioIntercepts.Core.Interfaces
{
    public interface IDialogPatternAnalyzer
    {
        Task<List<PhrasePattern>> FindCommonPhrasesAsync(int minFrequency = 5);
        Task<List<DialogSequence>> AnalyzeDialogSequencesAsync(int sequenceLength = 3);
        Task<List<RoleAnalysisResult>> AnalyzeRolesAsync();
        Task<Dictionary<string, MessageType>> ClassifyMessagesAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<List<string>> ExtractKeywordsAsync(string dialog, int topN = 10);
        Task<Dictionary<string, double>> CalculateStyleMetricsAsync(string callsign);
    }
}

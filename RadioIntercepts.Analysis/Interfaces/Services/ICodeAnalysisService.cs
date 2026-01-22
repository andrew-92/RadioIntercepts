using RadioIntercepts.Core.Models.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Analysis.Interfaces.Services
{
    public interface ICodeAnalysisService
    {
        // Анализ терминов
        Task<List<CodeTerm>> ExtractCodeTermsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<List<CodeUsageStatistic>> GetCodeUsageStatisticsAsync(string? term = null, CodeTermCategory? category = null);
        Task<Dictionary<string, List<string>>> FindTermAssociationsAsync(string term, int maxAssociations = 10);

        // Сленг и паттерны
        Task<List<SlangPattern>> DetectSlangPatternsAsync(int minFrequency = 3);
        Task<List<SlangPattern>> FindCallsignSpecificSlangAsync(string callsign);

        // Профили лексики
        Task<CallsignVocabularyProfile> GetCallsignVocabularyProfileAsync(string callsign);
        Task<List<CallsignVocabularyProfile>> CompareCallsignVocabulariesAsync(List<string> callsigns);
        Task<CodeSimilarityResult> CalculateVocabularySimilarityAsync(string callsign1, string callsign2);

        // Кластеризация по терминологии
        Task<List<List<string>>> ClusterCallsignsByTerminologyAsync(int minClusterSize = 2);

        // Обнаружение новых терминов
        Task<List<CodeTerm>> DetectNewTermsAsync(DateTime since, double minDistinctiveness = 0.7);

        // Тренды использования
        Task<Dictionary<string, double>> AnalyzeTermTrendsAsync(DateTime startDate, DateTime endDate);

        // Словарь терминов
        Task<Dictionary<string, string>> BuildGlossaryAsync(double minFrequency = 0.001);
    }
}

using RadioIntercepts.Analysis.Services.SemanticSearch;
using RadioIntercepts.Core.Models;
using RadioIntercepts.Core.Models.DialogPatterns;
using RadioIntercepts.Core.Models.SemanticSearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Analysis.Interfaces.Services
{
    public interface ISemanticSearchService
    {
        Task<List<SemanticSearchResult>> SearchAsync(SemanticSearchQuery query);
        Task<List<SemanticSearchResult>> SearchByExampleAsync(SearchByExampleRequest request);
        Task<List<KeywordAnalysis>> AnalyzeKeywordsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<List<MessageCategory>> GetMessageCategoriesAsync();
        Task<Dictionary<MessageType, List<string>>> ExtractTypicalPhrasesAsync(MessageType type, int topN = 10);
        Task<List<Message>> FindSimilarMessagesAsync(long messageId, int maxResults = 10);
        Task<List<MessageCluster>> ClusterMessagesByContentAsync(int numClusters = 5);
        Task<Dictionary<string, double>> CalculateTermFrequencyAsync(string term);
    }
}

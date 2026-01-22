using RadioIntercepts.Analysis.Services.Graphs;

namespace RadioIntercepts.Analysis.Interfaces.Services
{
    public interface IGraphAnalysisService
    {
        Task<InteractionGraph> BuildInteractionGraphAsync(DateTime? startDate = null, DateTime? endDate = null,
            string? area = null, string? frequency = null);
        Task<List<string>> FindKeyPlayersAsync(int topN = 10, double minCentrality = 0.1);
        Task<List<List<string>>> DetectCommunitiesAsync(int minCommunitySize = 3);
        Task<Dictionary<string, double>> CalculateCentralityScoresAsync();
        Task<List<string>> FindBridgesAsync(); // Позывные, соединяющие разные группы
    }
}

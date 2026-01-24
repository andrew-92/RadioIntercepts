using RadioIntercepts.Analysis.Services.Graphs;
using System.Threading.Tasks;

namespace RadioIntercepts.Analysis.Interfaces.Services
{
    public interface IAdvancedGraphAnalysisService : IGraphAnalysisService
    {
        Task<NetworkMetrics> CalculateNetworkMetricsAsync();
        Task<List<CommunityAnalysis>> AnalyzeCommunitiesAsync(int minCommunitySize = 3);
        Task<Dictionary<string, double>> CalculateEigenvectorCentralityAsync();
        Task<List<KeyPlayerAnalysis>> FindKeyPlayersDetailedAsync(int topN = 10);
    }

    public class CommunityAnalysis
    {
        public int Id { get; set; }
        public List<string> Callsigns { get; set; } = new();
        public int Size => Callsigns.Count;
        public double InternalDensity { get; set; }
        public double AverageDegree { get; set; }
        public List<string> KeyPlayers { get; set; } = new();
    }

    public class KeyPlayerAnalysis
    {
        public string Callsign { get; set; } = null!;
        public double Centrality { get; set; }
        public double DegreeCentrality { get; set; }
        public double BetweennessCentrality { get; set; }
        public double ClosenessCentrality { get; set; }
        public int TotalMessages { get; set; }
        public int Degree { get; set; }
        public int CommunityId { get; set; }
    }
}
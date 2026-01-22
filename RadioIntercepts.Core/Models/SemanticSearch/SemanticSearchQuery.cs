using RadioIntercepts.Core.Models.DialogPatterns;

namespace RadioIntercepts.Core.Models.SemanticSearch
{
    public class SemanticSearchQuery
    {
        public string Query { get; set; } = null!;
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? Area { get; set; }
        public string? Frequency { get; set; }
        public List<string>? Callsigns { get; set; }
        public MessageType? MessageType { get; set; }
        public double MinSimilarity { get; set; } = 0.3;
        public int MaxResults { get; set; } = 100;
    }
}
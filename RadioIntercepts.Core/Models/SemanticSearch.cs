// Core/Models/SemanticSearch.cs
namespace RadioIntercepts.Core.Models
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

    public class SemanticSearchResult
    {
        public Message Message { get; set; } = null!;
        public double SimilarityScore { get; set; }
        public List<string> MatchedKeywords { get; set; } = new();
        public MessageType? DetectedType { get; set; }
        public Dictionary<string, double> KeywordWeights { get; set; } = new();
        public string Snippet { get; set; } = null!;
    }

    public class KeywordAnalysis
    {
        public string Keyword { get; set; } = null!;
        public int Frequency { get; set; }
        public double TFIDF { get; set; }
        public List<string> RelatedCallsigns { get; set; } = new();
        public List<string> RelatedAreas { get; set; } = new();
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
    }

    public class MessageCategory
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public List<string> Keywords { get; set; } = new();
        public List<string> ExamplePhrases { get; set; } = new();
        public int MessageCount { get; set; }
    }

    public class SearchByExampleRequest
    {
        public string ExampleText { get; set; } = null!;
        public int MaxSimilarExamples { get; set; } = 5;
        public bool IncludeOpposite { get; set; } = false;
    }
}
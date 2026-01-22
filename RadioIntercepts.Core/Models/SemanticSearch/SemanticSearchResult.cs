using RadioIntercepts.Core.Models.DialogPatterns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.SemanticSearch
{
    public class SemanticSearchResult
    {
        public Message Message { get; set; } = null!;
        public double SimilarityScore { get; set; }
        public List<string> MatchedKeywords { get; set; } = new();
        public MessageType? DetectedType { get; set; }
        public Dictionary<string, double> KeywordWeights { get; set; } = new();
        public string Snippet { get; set; } = null!;
    }
}

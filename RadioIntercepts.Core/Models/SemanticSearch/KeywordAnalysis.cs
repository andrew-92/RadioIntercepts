using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.SemanticSearch
{
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
}

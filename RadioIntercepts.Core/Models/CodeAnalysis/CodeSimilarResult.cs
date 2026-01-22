using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.CodeAnalysis
{
    public class CodeSimilarityResult
    {
        public string Callsign1 { get; set; } = null!;
        public string Callsign2 { get; set; } = null!;
        public double SimilarityScore { get; set; }
        public List<string> CommonTerms { get; set; } = new();
        public List<string> UniqueToCallsign1 { get; set; } = new();
        public List<string> UniqueToCallsign2 { get; set; } = new();
        public Dictionary<CodeTermCategory, double> CategorySimilarity { get; set; } = new();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.CodeAnalysis
{
    public class CallsignVocabularyProfile
    {
        public string Callsign { get; set; } = null!;
        public int TotalWordsUsed { get; set; }
        public int UniqueWordsCount { get; set; }
        public Dictionary<CodeTermCategory, int> CategoryDistribution { get; set; } = new();
        public List<string> MostFrequentTerms { get; set; } = new();
        public List<string> DistinctiveTerms { get; set; } = new(); // Термины, которые редко используют другие
        public double VocabularyRichness { get; set; } // Коэффициент разнообразия лексики
        public Dictionary<string, double> SimilarityScores { get; set; } = new(); // Сходство с другими позывными
    }
}

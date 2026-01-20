// Core/Models/CodeAnalysis.cs
namespace RadioIntercepts.Core.Models
{
    public class CodeTerm
    {
        public int Id { get; set; }
        public string Term { get; set; } = null!;
        public string? Description { get; set; }
        public CodeTermCategory Category { get; set; }
        public string? Origin { get; set; } // Происхождение термина
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
        public bool IsActive { get; set; } = true;
        public double FrequencyScore { get; set; } // Частота использования
        public double DistinctivenessScore { get; set; } // Уникальность термина
        public List<string> TypicalContexts { get; set; } = new(); // Типичные контексты использования
    }

    public enum CodeTermCategory
    {
        Unknown,
        Military,           // Военные термины
        Slang,              // Сленг
        CodeWord,           // Кодовые слова
        Abbreviation,       // Аббревиатуры
        Technical,          // Технические термины
        Location,           // Обозначения мест
        Equipment,          // Оборудование и техника
        Operation,          // Оперативные термины
        CallSignSpecific,   // Специфичные для позывных
        Urgency             // Термины срочности
    }

    public class CodeUsageStatistic
    {
        public string Term { get; set; } = null!;
        public CodeTermCategory Category { get; set; }
        public int TotalUsageCount { get; set; }
        public int UniqueCallsignsCount { get; set; }
        public int UniqueAreasCount { get; set; }
        public DateTime FirstUsage { get; set; }
        public DateTime LastUsage { get; set; }
        public Dictionary<string, int> UsageByCallsign { get; set; } = new();
        public Dictionary<string, int> UsageByArea { get; set; } = new();
        public Dictionary<DateTime, int> UsageOverTime { get; set; } = new();
        public double AverageMessagesPerDay { get; set; }
        public double Trend { get; set; } // Тренд использования (+ увеличение, - уменьшение)
    }

    public class SlangPattern
    {
        public string Pattern { get; set; } = null!;
        public string Meaning { get; set; } = null!;
        public List<string> Examples { get; set; } = new();
        public int ExampleCount { get; set; }
        public double Confidence { get; set; }
        public List<string> AssociatedCallsigns { get; set; } = new();
        public DateTime FirstObserved { get; set; }
        public DateTime LastObserved { get; set; }
    }

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
// Core/Models/CodeAnalysis.cs
namespace RadioIntercepts.Core.Models.CodeAnalysis
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
}
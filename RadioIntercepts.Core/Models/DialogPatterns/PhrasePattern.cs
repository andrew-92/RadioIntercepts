// Core/Models/DialogPatterns.cs
namespace RadioIntercepts.Core.Models.DialogPatterns
{
    public class PhrasePattern
    {
        public string Phrase { get; set; } = null!;
        public MessageType MessageType { get; set; }
        public int Frequency { get; set; }
        public double Confidence { get; set; }
        public List<string> AssociatedCallsigns { get; set; } = new();
    }
}
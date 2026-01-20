// Core/Models/DialogPatterns.cs
namespace RadioIntercepts.Core.Models
{
    public enum MessageType
    {
        Unknown,
        Command,        // Приказ/команда
        Request,        // Запрос
        Report,         // Отчет/доклад
        Confirmation,   // Подтверждение
        Query,          // Вопрос
        Coordination,   // Координация
        Technical,      // Техническое сообщение
        Greeting,       // Приветствие/обращение
        Farewell        // Прощание
    }

    public enum ParticipantRole
    {
        Unknown,
        Commander,      // Командир (отдает приказы)
        Executor,       // Исполнитель (подтверждает, докладывает)
        Observer,       // Наблюдатель (докладывает о ситуации)
        Coordinator,    // Координатор (согласует действия)
        Technician      // Технический специалист
    }

    public class PhrasePattern
    {
        public string Phrase { get; set; } = null!;
        public MessageType MessageType { get; set; }
        public int Frequency { get; set; }
        public double Confidence { get; set; }
        public List<string> AssociatedCallsigns { get; set; } = new();
    }

    public class DialogSequence
    {
        public string SequenceId { get; set; } = null!;
        public List<string> Callsigns { get; set; } = new();
        public List<MessageType> Pattern { get; set; } = new();
        public int Frequency { get; set; }
        public TimeSpan AverageDuration { get; set; }
        public double SuccessRate { get; set; }
    }

    public class RoleAnalysisResult
    {
        public string Callsign { get; set; } = null!;
        public ParticipantRole Role { get; set; }
        public double RoleConfidence { get; set; }
        public Dictionary<MessageType, int> MessageTypeDistribution { get; set; } = new();
        public Dictionary<string, double> RoleFeatures { get; set; } = new();
    }
}